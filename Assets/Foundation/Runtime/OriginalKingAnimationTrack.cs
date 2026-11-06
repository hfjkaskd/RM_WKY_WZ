using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Single-track player. Each entry owns playback speed, mixing time and rotation winding.
    // The King uses track 0 (First), while the Cannon uses track 1 (Replace).
    public sealed class OriginalKingAnimationTrack
    {
        private enum ChannelBlend : byte { Setup, Previous, Hold, HoldUntilLater }

        private readonly OriginalKingPose pose;
        private readonly OriginalKingRigData.Clip[] clips;
        private readonly HashSet<long>[] clipChannels;
        private readonly HashSet<long> appliedChannels = new HashSet<long>();
        private readonly Stack<Entry> pool = new Stack<Entry>(12);
        private readonly int maxChannels;
        private readonly float mixSeconds;
        private readonly bool replaceTrack;
        private Entry current, tail;
        private bool channelsChanged;

        public int CurrentClip => current.Clip;
        public float Time => current.Time;
        public bool Loops => current.Loop;
        public float Speed { get => current.Speed; set => current.Speed = value; }
        public bool IsMixing => current.Previous != null && current.MixTime < current.MixDuration;

        public OriginalKingAnimationTrack(OriginalKingPose pose, float mixSeconds, bool replaceTrack = false)
        {
            this.pose = pose ?? throw new ArgumentNullException(nameof(pose));
            if (mixSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(mixSeconds));
            this.mixSeconds = mixSeconds;
            this.replaceTrack = replaceTrack;
            clips = pose.Data.Clips;
            clipChannels = new HashSet<long>[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                var timelines = clips[i].Timelines;
                maxChannels = Math.Max(maxChannels, timelines.Length);
                var channels = clipChannels[i] = new HashSet<long>();
                foreach (var timeline in timelines)
                {
                    long channel = Channel(timeline);
                    channels.Add(channel);
                    appliedChannels.Add(channel);
                }
            }
            appliedChannels.Clear();
            for (int i = 0; i < 12; i++) pool.Push(new Entry(maxChannels));
        }

        public void Set(int clip, bool loop = false, float speed = 1f, float? mixDuration = null)
        {
            if (speed <= 0f || mixDuration < 0f) throw new ArgumentOutOfRangeException(nameof(speed));
            if (current != null)
            {
                var queued = current.Next;
                current.Next = null;
                while (queued != null)
                {
                    var next = queued.Next;
                    Recycle(queued);
                    queued = next;
                }
                if (current.LastApplied < 0f)
                {
                    var unapplied = current;
                    current = current.Previous;
                    Recycle(unapplied);
                }
            }
            var entry = Rent(clip, loop);
            entry.Speed = speed;
            entry.MixDuration = mixDuration ?? mixSeconds;
            SetCurrent(entry);
            tail = entry;
        }

        public void Add(int clip, bool loop)
        {
            if (tail == null)
            {
                Set(clip, loop);
                return;
            }
            var entry = Rent(clip, loop);
            float duration = clips[tail.Clip].Duration;
            entry.Delay = duration == 0f ? tail.Time :
                (tail.Loop ? duration * (1 + (int)(tail.Time / duration)) : Math.Max(duration, tail.Time)) - mixSeconds;
            tail.Next = entry;
            tail = entry;
        }

        private Entry Rent(int clip, bool loop)
        {
            var entry = pool.Count == 0 ? new Entry(maxChannels) : pool.Pop();
            entry.Clip = clip;
            entry.Loop = loop;
            entry.Speed = 1f;
            entry.MixDuration = mixSeconds;
            entry.Time = entry.Delay = entry.MixTime = entry.TotalAlpha = 0f;
            entry.LastApplied = -1f;
            entry.InterruptWeight = 1f;
            entry.RotationInitialized = false;
            return entry;
        }

        private void Recycle(Entry entry)
        {
            entry.Previous = entry.Next = entry.MixingTo = null;
            Array.Clear(entry.HoldUntil, 0, entry.HoldUntil.Length);
            pool.Push(entry);
        }

        private void SetCurrent(Entry entry)
        {
            if (current != null)
            {
                entry.Previous = current;
                current.MixingTo = entry;
                if (current.Previous != null) entry.InterruptWeight = MixRatio(current);
                current.RotationInitialized = false;
            }
            current = entry;
            channelsChanged = true;
        }

        public void Advance(float delta)
        {
            float currentDelta = delta * current.Speed;
            var next = current.Next;
            if (next != null && current.LastApplied >= next.Delay)
            {
                next.Time = ((current.LastApplied - next.Delay) / current.Speed + delta) * next.Speed;
                current.Time += currentDelta;
                current.Next = null;
                SetCurrent(next);
                for (var entry = next; entry.Previous != null; entry = entry.Previous) entry.MixTime += delta;
                return;
            }
            if (current.Previous != null && AdvanceMix(current, delta))
            {
                var oldest = current.Previous;
                current.Previous = null;
                while (oldest != null)
                {
                    var previous = oldest.Previous;
                    Recycle(oldest);
                    oldest = previous;
                }
                channelsChanged = true;
            }
            current.Time += currentDelta;
        }

        private bool AdvanceMix(Entry to, float delta)
        {
            var from = to.Previous;
            if (from == null) return true;
            bool olderFinished = AdvanceMix(from, delta);
            if (to.MixTime > 0f && to.MixTime >= to.MixDuration)
            {
                if (from.TotalAlpha == 0f || to.MixDuration == 0f)
                {
                    to.Previous = from.Previous;
                    if (to.Previous != null) to.Previous.MixingTo = to;
                    to.InterruptWeight = from.InterruptWeight;
                    Recycle(from);
                    channelsChanged = true;
                }
                return olderFinished;
            }
            from.Time += delta * from.Speed;
            to.MixTime += delta;
            return false;
        }

        public void Apply()
        {
            if (channelsChanged) UpdateChannelOwnership();
            pose.BeginTrackPose();
            float alpha = current.Previous == null ? 1f : ApplyPrevious(current);
            var timelines = clips[current.Clip].Timelines;
            float time = ClipTime(current);
            bool firstRotation = !current.RotationInitialized;
            for (int i = 0; i < timelines.Length; i++)
            {
                bool fromSetup = (replaceTrack || alpha != 1f) && current.Blends[i] != ChannelBlend.Previous;
                pose.ApplyTrackTimeline(timelines[i], time, alpha, fromSetup, false, current.Angles, i * 2,
                    firstRotation, replaceTrack);
            }
            if (replaceTrack || alpha != 1f) current.RotationInitialized = true;
            current.LastApplied = current.Time;
            pose.FinishTrackPose();
        }

        private float ApplyPrevious(Entry to)
        {
            var from = to.Previous;
            if (from.Previous != null) ApplyPrevious(from);
            float ratio = MixRatio(to);
            float fading = to.InterruptWeight * (1f - ratio);
            var timelines = clips[from.Clip].Timelines;
            float time = ClipTime(from);
            bool firstRotation = !from.RotationInitialized;
            from.TotalAlpha = 0f;
            for (int i = 0; i < timelines.Length; i++)
            {
                ChannelBlend blend = from.Blends[i];
                if (blend == ChannelBlend.Previous && timelines[i].Kind == 7) continue;
                float alpha = fading;
                if (blend == ChannelBlend.Hold) alpha = to.InterruptWeight;
                else if (blend == ChannelBlend.HoldUntilLater)
                    alpha = to.InterruptWeight * (1f - MixRatio(from.HoldUntil[i]));
                from.TotalAlpha += alpha;
                pose.ApplyTrackTimeline(timelines[i], time, alpha, blend != ChannelBlend.Previous,
                    true, from.Angles, i * 2, firstRotation, replaceTrack);
            }
            from.RotationInitialized = true;
            from.LastApplied = from.Time;
            return ratio;
        }

        private float ClipTime(Entry entry)
        {
            float duration = clips[entry.Clip].Duration;
            return entry.Loop && duration > 0f ? entry.Time % duration : Math.Min(entry.Time, duration);
        }

        private static float MixRatio(Entry entry) => entry.MixDuration == 0f ? 1f : Math.Min(1f, entry.MixTime / entry.MixDuration);

        private void UpdateChannelOwnership()
        {
            appliedChannels.Clear();
            var entry = current;
            while (entry.Previous != null) entry = entry.Previous;
            for (; entry != null; entry = entry.MixingTo)
            {
                var timelines = clips[entry.Clip].Timelines;
                var next = entry.MixingTo;
                for (int i = 0; i < timelines.Length; i++)
                {
                    var timeline = timelines[i];
                    long channel = Channel(timeline);
                    entry.HoldUntil[i] = null;
                    if (!appliedChannels.Add(channel)) entry.Blends[i] = ChannelBlend.Previous;
                    else if (next == null || timeline.Kind == 5 || timeline.Kind == 7 || !clipChannels[next.Clip].Contains(channel))
                        entry.Blends[i] = ChannelBlend.Setup;
                    else
                    {
                        var later = next.MixingTo;
                        while (later != null && clipChannels[later.Clip].Contains(channel)) later = later.MixingTo;
                        entry.HoldUntil[i] = later;
                        entry.Blends[i] = later == null ? ChannelBlend.Hold : ChannelBlend.HoldUntilLater;
                    }
                }
            }
            channelsChanged = false;
        }

        private static long Channel(OriginalKingRigData.Timeline timeline) =>
            ((long)timeline.Kind << 48) | ((long)(uint)timeline.Target << 16) |
            (timeline.Kind == 6 ? (ushort)timeline.Attachment : 0u);

        private sealed class Entry
        {
            public readonly ChannelBlend[] Blends;
            public readonly Entry[] HoldUntil;
            public readonly float[] Angles;
            public Entry Previous, Next, MixingTo;
            public int Clip;
            public bool Loop, RotationInitialized;
            public float Time, Speed, Delay, LastApplied, MixTime, MixDuration, InterruptWeight, TotalAlpha;

            public Entry(int channelCount)
            {
                Blends = new ChannelBlend[channelCount];
                HoldUntil = new Entry[channelCount];
                Angles = new float[channelCount * 2];
            }
        }
    }
}
