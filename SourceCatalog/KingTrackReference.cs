using System;
using System.Globalization;
using Spine;
using Whitebox.Foundation;

// External 3.8.84 reference execution. No Spine assembly is imported in Unity.
static class KingTrackReference
{
    private const float MixSeconds = .2f;
    private static int comparedFrames;
    private static long comparisons;
    private static float worstError;

    public static string Run(SkeletonData original, OriginalKingRigData data)
    {
        var clipNames = new[] { "idle_loop_2", "idle_loop_3", "k-1_idle_5", "idle_connect_1", "idle_connect_2",
            "k-1sevinme_6", "k-1_uzulme", "k-1_kaygi", "k-1_kaygi_loop", "Relax_1", "Heyecan_1" };
        foreach (string first in clipNames)
        foreach (string second in clipNames)
        {
            var oracle = new Scenario(original, data);
            oracle.Set(first, true);
            oracle.Advance(.31f);
            oracle.Advance(.63f);
            oracle.Advance(.47f);
            oracle.Set(second, false);
            for (int step = 0; step < 20; step++) oracle.Advance(step % 3 == 0 ? .011f : .018f);
        }

        var queue = new Scenario(original, data);
        queue.Set("idle_loop_2", false);
        queue.Add("idle_connect_2", false);
        queue.Add("idle_connect_1", true);
        for (int step = 0; step < 700; step++) queue.Advance(1f / 60f);
        queue.Add("idle_loop_3", false);
        for (int step = 0; step < 500; step++) queue.Advance(1f / 60f);

        queue.Set("k-1sevinme_6", false);
        queue.Advance(.033f);
        queue.Set("Relax_1", false);
        queue.Set("k-1_kaygi", false); // Replaces the unapplied entry.
        queue.Advance(.041f);
        queue.Set("k-1_uzulme", false);
        for (int step = 0; step < 35; step++) queue.Advance(.016f);

        var pose = new OriginalKingPose(data);
        var track = new OriginalKingAnimationTrack(pose, MixSeconds);
        var mesh = new OriginalKingMesh(pose, "sol frame");
        int idle = data.FindClip("idle_loop_2"), happy = data.FindClip("k-1sevinme_6");
        track.Set(idle, true);
        track.Apply();
        track.Set(happy);
        track.Advance(.016f);
        track.Apply();
        mesh.Build();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int step = 0; step < 1000; step++)
        {
            if (step % 30 == 0) track.Set(step % 60 == 0 ? idle : happy);
            track.Advance(.016f);
            track.Apply();
            mesh.Build();
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        if (allocated != 0) throw new Exception("Animation track allocated during replay: " + allocated);
        return "{\"passed\":true,\"mix_seconds\":0.2,\"compared_frames\":" + comparedFrames +
            ",\"scalar_comparisons\":" + comparisons + ",\"max_absolute_error\":" + worstError.ToString("R", CultureInfo.InvariantCulture) +
            ",\"queued_and_interrupted_transitions\":true,\"managed_bytes_1000_frames\":" + allocated + "}";
    }

    private sealed class Scenario
    {
        private readonly Skeleton skeleton;
        private readonly AnimationState state;
        private readonly OriginalKingPose pose;
        private readonly OriginalKingAnimationTrack track;
        private readonly OriginalKingMesh mesh;

        public Scenario(SkeletonData original, OriginalKingRigData data)
        {
            skeleton = new Skeleton(original);
            state = new AnimationState(new AnimationStateData(original) { DefaultMix = MixSeconds });
            pose = new OriginalKingPose(data);
            track = new OriginalKingAnimationTrack(pose, MixSeconds);
            mesh = new OriginalKingMesh(pose, "sol frame");
        }

        public void Set(string name, bool loop)
        {
            state.SetAnimation(0, name, loop);
            track.Set(pose.Data.FindClip(name), loop);
        }

        public void Add(string name, bool loop)
        {
            state.AddAnimation(0, name, loop, 0f);
            track.Add(pose.Data.FindClip(name), loop);
        }

        public void Advance(float delta)
        {
            state.Update(delta);
            state.Apply(skeleton);
            skeleton.UpdateWorldTransform();
            track.Advance(delta);
            track.Apply();
            comparedFrames++;
            var active = state.GetCurrent(0);
            string clip = active.Animation.Name;
            if (clip != pose.Data.Clips[track.CurrentClip].Name || active.Loop != track.Loops)
                throw new Exception("Track queue mismatch " + clip);
            Equal(track.Time, active.TrackTime, clip + " track time");
            for (int i = 0; i < pose.Local.Length; i++)
            {
                var bone = skeleton.Bones.Items[i];
                var local = pose.Local[i];
                Equal(local[0], bone.X, clip + " local x " + i);
                Equal(local[1], bone.Y, clip + " local y " + i);
                float difference = local[2] - bone.Rotation;
                Equal(difference - (float)Math.Round(difference / 360f) * 360f, 0f, clip + " rotation " + i);
                Equal(local[3], bone.ScaleX, clip + " scale x " + i);
                Equal(local[4], bone.ScaleY, clip + " scale y " + i);
                Equal(local[5], bone.ShearX, clip + " shear x " + i);
                Equal(local[6], bone.ShearY, clip + " shear y " + i);
            }
            for (int i = 0; i < pose.Attachment.Length; i++)
            {
                var slot = skeleton.Slots.Items[i];
                int attachment = pose.Attachment[i];
                string name = attachment < 0 ? null : pose.Data.Attachments[attachment].Name;
                if (slot.Attachment?.Name != name || skeleton.DrawOrder.Items[i].Data.Index != pose.DrawOrder[i] ||
                    slot.Deform.Count != pose.DeformLength[i])
                    throw new Exception(clip + " attachment/draw order/deform count mismatch at slot " + i);
                Equal(pose.Colors[i][0], slot.R, clip + " slot red");
                Equal(pose.Colors[i][1], slot.G, clip + " slot green");
                Equal(pose.Colors[i][2], slot.B, clip + " slot blue");
                Equal(pose.Colors[i][3], slot.A, clip + " slot alpha");
                for (int v = 0; v < slot.Deform.Count; v++) Equal(pose.Deforms[i][v], slot.Deform.Items[v], clip + " deform");
            }
            if (comparedFrames % 13 == 0) KingMeshReference.Compare(mesh, skeleton);
        }
    }

    private static void Equal(float actual, float expected, string message)
    {
        comparisons++;
        float error = Math.Abs(actual - expected);
        worstError = Math.Max(worstError, error);
        if (float.IsNaN(actual) || error > .00005f)
            throw new Exception(message + ": actual " + actual + ", expected " + expected + ", error " + error + ", frame " + comparedFrames);
    }
}
