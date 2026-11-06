using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    // Original analytical rigs or authored mesh clips, rendered with Unity Mesh.
    // Data selects the playback representation on every target.
    public sealed class OriginalBakedMeshAnimation : MonoBehaviour
    {
        [SerializeField] private string dataPath, texturePath;
        [SerializeField] private string rigPath;
        [SerializeField] private string alternateDataPath;
        [SerializeField] private Material materialTemplate;
        [SerializeField] private MeshFilter[] parts;
        [SerializeField] private Transform boneFollower;
        [SerializeField] private Animator playbackAnimator;
        [SerializeField] private string playbackState;
        private Clip primary, alternate;
        private OriginalKingRigData rigData;
        private OriginalKingPose rigPose;
        private OriginalKingMesh rigGeometry;
        private int rigClip;
        private float rigDuration;
        private Mesh[] meshes;
        private Vector3[][] positions;
        private Vector2[][] uvs;
        private Color32[][] colors;
        private Dictionary<Material, Material> materials;
        private const int HorizontalGroupMask = 3, CloseMask = 4;
        private byte[] vertexGroups;
        private int[] vertexGroupStarts;
        private Vector2 horizontalOffsets;
        private bool maskClosedSlots;
        public float Duration => rigData != null ? rigDuration : primary.Duration;
        public int RigBoneCount => rigData == null ? 0 : rigData.Bones.Length;
        public int RigPathConstraintCount => rigData == null ? 0 : rigData.Paths.Length;
        public Animator PlaybackAnimator => playbackAnimator;
        public float PlaybackTime { get; private set; }
        public float AlternateDuration => alternate.Duration;
        public int FrameCount => primary.Frames == null ? 0 : primary.Frames.Length;
        public void SetPartVisible(int index, bool visible) => parts[index].gameObject.SetActive(visible);
        public void Initialize()
        {
            if (primary.Frames != null || rigData != null) return;
            int partCount = parts.Length;
            var maximum = new int[partCount];
            if (!string.IsNullOrEmpty(rigPath))
            {
                if (boneFollower != null || !string.IsNullOrEmpty(alternateDataPath))
                    throw new InvalidDataException("The analytical rig has incompatible clip bindings.");
                var asset = Resources.Load<TextAsset>(rigPath);
                if (asset == null) throw new InvalidOperationException("Original analytical rig data is missing.");
                using (var stream = new MemoryStream(asset.bytes, false)) rigData = new OriginalKingRigData(stream);
                Resources.UnloadAsset(asset);
                rigPose = new OriginalKingPose(rigData);
                rigGeometry = new OriginalKingMesh(rigPose, null);
                rigClip = rigData.FindClip(playbackState);
                rigDuration = rigData.Clips[rigClip].Duration;
                if (rigGeometry.Parts.Length != partCount || rigDuration <= 0f)
                    throw new InvalidDataException("Original analytical rig parts or duration.");
                for (int p = 0; p < partCount; p++) maximum[p] = rigGeometry.Parts[p].Vertices.Length / 8;
            }
            else
            {
                primary = LoadClip(dataPath, maximum);
                if (!string.IsNullOrEmpty(alternateDataPath)) alternate = LoadClip(alternateDataPath, maximum);
            }
            if (boneFollower != null && (primary.Follower == null ||
                (alternate.Frames != null && alternate.Follower == null)))
                throw new InvalidDataException("Baked animation follower poses are missing.");
            meshes = new Mesh[partCount]; positions = new Vector3[partCount][]; uvs = new Vector2[partCount][]; colors = new Color32[partCount][];
            var texture = Resources.Load<Texture2D>(texturePath);
            if (texture == null) throw new InvalidOperationException("Original animation atlas missing.");
            materials = new Dictionary<Material, Material>();
            for (int p = 0; p < partCount; p++)
            {
                positions[p] = new Vector3[maximum[p]]; uvs[p] = new Vector2[maximum[p]]; colors[p] = new Color32[maximum[p]];
                var mesh = new Mesh { name = parts[p].name, indexFormat = IndexFormat.UInt16 };
                mesh.MarkDynamic(); meshes[p] = mesh; parts[p].sharedMesh = mesh;
                var renderer = parts[p].GetComponent<MeshRenderer>();
                var template = materialTemplate != null ? materialTemplate : renderer.sharedMaterial;
                if (template == null) throw new InvalidOperationException("Original animation material missing.");
                if (!materials.TryGetValue(template, out var material))
                {
                    material = new Material(template) { mainTexture = texture };
                    materials.Add(template, material);
                }
                renderer.sharedMaterial = material;
            }
            if (playbackAnimator != null)
            {
                // TutorialHandView.PlayAnimationForState 0x276afb4 initializes
                // the authored Mecanim state with the current Unity delta.
                playbackAnimator.Play(Animator.StringToHash(playbackState), 0, 0f);
                playbackAnimator.Update(Time.deltaTime);
                SampleAnimator();
            }
            else Sample(0f);
        }

        private static Clip LoadClip(string path, int[] maximum)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Original baked animation data missing.");
            using (var stream = new MemoryStream(asset.bytes, false))
            using (var reader = new BinaryReader(stream))
            {
                int signature = reader.ReadInt32(), format = reader.ReadInt32();
                if (signature != 0x524d4c47 || (format < 1 || format > 3)) throw new InvalidDataException("Baked animation format.");
                int fps = reader.ReadInt32(); int count = reader.ReadInt32(); float duration = reader.ReadSingle();
                int partCount = reader.ReadInt32();
                if (partCount != maximum.Length || count < 2 || count > 10000 || fps <= 0) throw new InvalidDataException("Animation parts/frames.");
                var frames = new FramePart[count][];
                var follower = format == 3 ? new FollowerPose[count] : null;
                for (int f = 0; f < count; f++)
                {
                    if (follower != null)
                        follower[f] = new FollowerPose(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    frames[f] = new FramePart[partCount];
                    for (int p = 0; p < partCount; p++)
                    {
                        int vertices = reader.ReadInt32(), indices = reader.ReadInt32();
                        if (vertices < 0 || vertices > 65535 || indices < 0 || indices > 500000 || indices % 3 != 0) throw new InvalidDataException("Animation mesh size.");
                        var previous = f == 0 ? null : frames[f - 1][p];
                        byte shared = format >= 2 ? reader.ReadByte() : (byte)0;
                        var part = new FramePart(vertices, indices, previous, shared);
                        frames[f][p] = part;
                        maximum[p] = Math.Max(maximum[p], vertices);
                        for (int i = 0; i < vertices; i++)
                        {
                            part.positions[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), 0);
                            if ((shared & 1) == 0) part.uvs[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                            if ((shared & 2) == 0) part.colors[i] = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                        }
                        for (int i = 0; i < indices; i++)
                        {
                            int value = (shared & 4) == 0 ? reader.ReadInt32() : part.indices[i];
                            if (value < 0 || value >= vertices) throw new InvalidDataException("Animation mesh index.");
                            if ((shared & 4) == 0) part.indices[i] = value;
                        }
                        if (previous != null) part.matchesPrevious = SameTopology(previous, part);
                    }
                }
                if (stream.Position != stream.Length) throw new InvalidDataException("Animation trailing data.");
                return new Clip(frames, fps, duration, follower);
            }
        }
        private static bool SameTopology(FramePart a, FramePart b)
        {
            if (a.positions.Length != b.positions.Length || a.indices.Length != b.indices.Length) return false;
            if (a.indices == b.indices) return true;
            for (int i = 0; i < a.indices.Length; i++) if (a.indices[i] != b.indices[i]) return false;
            return true;
        }
        internal void SetPrimaryHorizontalOffsets(string groupPath, Vector2 offsets)
        {
            if (offsets != Vector2.zero && vertexGroups == null) LoadVertexGroups(groupPath);
            horizontalOffsets = offsets;
        }

        internal void SetPrimaryCloseMask(string groupPath, bool enabled)
        {
            if (enabled && vertexGroups == null) LoadVertexGroups(groupPath);
            maskClosedSlots = enabled;
        }

        private void LoadVertexGroups(string path)
        {
            const uint groupSignature = 0x524D4858;
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Baked vertex groups are missing.");

            var data = asset.bytes;
            using var stream = new MemoryStream(data, false);
            using var reader = new BinaryReader(stream);
            var frames = primary.Frames;
            if (reader.ReadUInt32() != groupSignature || reader.ReadUInt16() != 2 ||
                reader.ReadUInt16() != frames.Length || reader.ReadUInt16() != parts.Length)
                throw new InvalidDataException("Baked vertex group header.");

            var starts = new int[frames.Length * parts.Length];
            for (int frame = 0; frame < frames.Length; frame++)
            for (int part = 0; part < parts.Length; part++)
            {
                starts[frame * parts.Length + part] = (int)stream.Position;
                int count = reader.ReadUInt16();
                int previousEnd = 0;
                for (int range = 0; range < count; range++)
                {
                    int start = reader.ReadUInt16();
                    int length = reader.ReadUInt16();
                    int flags = reader.ReadByte();
                    if (start < previousEnd || length == 0 || start + length > frames[frame][part].positions.Length ||
                        flags == 0 || flags > 6 || (flags & HorizontalGroupMask) == HorizontalGroupMask)
                        throw new InvalidDataException("Baked vertex group range.");
                    previousEnd = start + length;
                }
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Baked vertex group trailing data.");
            vertexGroups = data;
            vertexGroupStarts = starts;
        }

        private int ReadGroupWord(ref int position)
        {
            int value = vertexGroups[position] | vertexGroups[position + 1] << 8;
            position += 2;
            return value;
        }

        private void ApplyVertexGroups(int frame, int part)
        {
            int position = vertexGroupStarts[frame * parts.Length + part];
            int count = ReadGroupWord(ref position);
            var buffer = positions[part];
            for (int range = 0; range < count; range++)
            {
                int start = ReadGroupWord(ref position);
                int length = ReadGroupWord(ref position);
                int flags = vertexGroups[position++];
                int group = flags & HorizontalGroupMask;
                if (group != 0)
                {
                    float offset = group == 1 ? horizontalOffsets.x : horizontalOffsets.y;
                    if (offset != 0f)
                        for (int vertex = start; vertex < start + length; vertex++) buffer[vertex].x += offset;
                }
                if (maskClosedSlots && (flags & CloseMask) != 0)
                    Array.Clear(colors[part], start, length);
            }
        }

        public void SampleAnimator()
        {
            if (playbackAnimator == null)
                throw new InvalidOperationException("The original animation clock is not configured.");
            float normalized = playbackAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            PlaybackTime = (normalized * Duration) % Duration;
            Sample(PlaybackTime);
        }

        public void Sample(float time)
        {
            if (rigData != null) SampleRig(time);
            else Sample(primary, time);
        }

        private void SampleRig(float time)
        {
            rigPose.Sample(rigClip, time, false);
            rigGeometry.Build();
            for (int p = 0; p < parts.Length; p++)
            {
                var part = rigGeometry.Parts[p];
                var buffer = part.Vertices;
                for (int i = 0; i < part.VertexCount; i++)
                {
                    int offset = i * 8;
                    positions[p][i] = new Vector3(buffer[offset], buffer[offset + 1], 0f);
                    uvs[p][i] = new Vector2(buffer[offset + 2], buffer[offset + 3]);
                    colors[p][i] = new Color32(Channel(buffer[offset + 4]), Channel(buffer[offset + 5]),
                        Channel(buffer[offset + 6]), Channel(buffer[offset + 7]));
                }
                var mesh = meshes[p];
                mesh.Clear(false);
                mesh.SetVertices(positions[p], 0, part.VertexCount);
                mesh.SetUVs(0, uvs[p], 0, part.VertexCount);
                mesh.SetColors(colors[p], 0, part.VertexCount);
                mesh.SetTriangles(part.Indices, 0, part.IndexCount, 0, true);
            }
        }

        private static byte Channel(float value) => (byte)Mathf.Clamp((int)(value * 255f), 0, 255);
        public void SampleAlternate(float time) => Sample(alternate, time);

        private void Sample(Clip clip, float time)
        {
            var frames = clip.Frames;
            if (frames == null) throw new InvalidOperationException("Animation not initialized.");
            float frame = Mathf.Clamp(time, 0, clip.Duration) * clip.Fps;
            int current = Math.Min((int)frame, frames.Length - 1), next = Math.Min(current + 1, frames.Length - 1);
            float t = frame - current;
            // Clips with follower poses record the terminal sample at the exact clip end.
            float endFrame = clip.Duration * clip.Fps;
            if (clip.Follower != null && next == frames.Length - 1 && endFrame > current && endFrame < next)
                t /= endFrame - current;
            if (boneFollower != null)
            {
                var first = clip.Follower[current];
                var second = clip.Follower[next];
                var point = Vector2.LerpUnclamped(first.Position, second.Position, t);
                float angle = Mathf.LerpAngle(first.Angle, second.Angle, t);
                boneFollower.SetPositionAndRotation(transform.TransformPoint(point),
                    transform.rotation * Quaternion.AngleAxis(angle, Vector3.forward));
            }
            for (int p = 0; p < parts.Length; p++)
            {
                var a = frames[current][p]; var b = frames[next][p]; bool interpolate = next != current && b.matchesPrevious;
                for (int i = 0; i < a.positions.Length; i++)
                {
                    positions[p][i] = interpolate ? Vector3.LerpUnclamped(a.positions[i], b.positions[i], t) : a.positions[i];
                    uvs[p][i] = interpolate ? Vector2.LerpUnclamped(a.uvs[i], b.uvs[i], t) : a.uvs[i];
                    colors[p][i] = interpolate ? Color32.LerpUnclamped(a.colors[i], b.colors[i], t) : a.colors[i];
                }
                // Adjacent samples with identical topology have identical group membership.
                if ((horizontalOffsets != Vector2.zero || maskClosedSlots) && ReferenceEquals(frames, primary.Frames))
                    ApplyVertexGroups(current, p);
                var mesh = meshes[p]; mesh.Clear();
                mesh.SetVertices(positions[p], 0, a.positions.Length); mesh.SetUVs(0, uvs[p], 0, a.positions.Length);
                mesh.SetColors(colors[p], 0, a.positions.Length); mesh.SetTriangles(a.indices, 0, true);
            }
        }
        private void OnDestroy()
        {
            if (meshes != null) for (int i = 0; i < meshes.Length; i++) if (meshes[i] != null) Destroy(meshes[i]);
            if (materials != null) foreach (var material in materials.Values) Destroy(material);
        }
        private readonly struct Clip
        {
            public readonly FramePart[][] Frames;
            public readonly float Fps, Duration;
            public readonly FollowerPose[] Follower;
            public Clip(FramePart[][] frames, float fps, float duration, FollowerPose[] follower)
            { Frames = frames; Fps = fps; Duration = duration; Follower = follower; }
        }
        private readonly struct FollowerPose
        {
            public readonly Vector2 Position;
            public readonly float Angle;
            public FollowerPose(float x, float y, float angle) { Position = new Vector2(x, y); Angle = angle; }
        }
        private sealed class FramePart
        {
            public readonly Vector3[] positions; public readonly Vector2[] uvs; public readonly Color32[] colors; public readonly int[] indices;
            public bool matchesPrevious;
            public FramePart(int vertices, int indices, FramePart previous, byte shared)
            {
                if (shared > 7 || (previous == null && shared != 0) ||
                    ((shared & 3) != 0 && previous.positions.Length != vertices) ||
                    ((shared & 4) != 0 && previous.indices.Length != indices))
                    throw new InvalidDataException("Animation shared mesh data.");
                positions = new Vector3[vertices];
                uvs = (shared & 1) != 0 ? previous.uvs : new Vector2[vertices];
                colors = (shared & 2) != 0 ? previous.colors : new Color32[vertices];
                this.indices = (shared & 4) != 0 ? previous.indices : new int[indices];
            }
        }
    }
}
