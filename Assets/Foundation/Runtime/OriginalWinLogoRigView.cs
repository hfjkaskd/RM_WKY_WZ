using System;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation
{
    // The original single logo track, evaluated on the owning dialog's Unity
    // clock. Bone changes and one-time slot clears preserve the native state.
    public sealed class OriginalWinLogoRigView : MonoBehaviour
    {
        [SerializeField] private string rigPath, texturePath, clipName, separatorSlot;
        [SerializeField] private string leftBone, rightBone;
        [SerializeField] private string[] closingAlphaSlots, closingAttachmentSlots;
        [SerializeField] private Material materialTemplate;
        [SerializeField] private MeshFilter[] parts;
        private OriginalKingPose pose;
        private OriginalKingAnimationTrack track;
        private OriginalKingMesh geometry;
        private Mesh[] meshes;
        private Vector3[][] positions;
        private Vector2[][] uvs;
        private Color32[][] colors;
        private Material material;
        private int left, right;
        private int[] alphaSlots, attachmentSlots;
        private float initialLeft, initialRight, duration;

        public float Duration => duration;
        public float TrackTime => track.Time;
        public int BoneCount => pose == null ? 0 : pose.Data.Bones.Length;
        public int PartCount => geometry == null ? 0 : geometry.Parts.Length;
        public void SetPartVisible(int index, bool visible) => parts[index].gameObject.SetActive(visible);

        public void Initialize()
        {
            if (pose != null) return;
            var asset = Resources.Load<TextAsset>(rigPath);
            var texture = Resources.Load<Texture2D>(texturePath);
            if (asset == null || texture == null || materialTemplate == null)
                throw new InvalidOperationException("Win logo rig resources are missing.");
            OriginalKingRigData data;
            using (var stream = new MemoryStream(asset.bytes, false)) data = new OriginalKingRigData(stream);
            Resources.UnloadAsset(asset);
            pose = new OriginalKingPose(data);
            track = new OriginalKingAnimationTrack(pose, 0f);
            int clip = data.FindClip(clipName);
            duration = data.Clips[clip].Duration;
            left = Bone(leftBone); right = Bone(rightBone);
            initialLeft = pose.Local[left][0]; initialRight = pose.Local[right][0];
            foreach (var timeline in data.Clips[clip].Timelines)
                if (timeline.Kind == 1 && (timeline.Target == left || timeline.Target == right))
                    throw new InvalidDataException("Logo trumpet root position is animated.");
            alphaSlots = Slots(closingAlphaSlots);
            attachmentSlots = Slots(closingAttachmentSlots);
            geometry = new OriginalKingMesh(pose, separatorSlot);
            if (geometry.Parts.Length != parts.Length) throw new InvalidDataException("Win logo renderer partitions.");
            meshes = new Mesh[parts.Length];
            positions = new Vector3[parts.Length][];
            uvs = new Vector2[parts.Length][];
            colors = new Color32[parts.Length][];
            material = new Material(materialTemplate) { mainTexture = texture };
            for (int p = 0; p < parts.Length; p++)
            {
                int capacity = geometry.Parts[p].Vertices.Length / 8;
                positions[p] = new Vector3[capacity]; uvs[p] = new Vector2[capacity]; colors[p] = new Color32[capacity];
                var mesh = meshes[p] = new Mesh { name = parts[p].name };
                mesh.MarkDynamic(); parts[p].sharedMesh = mesh;
                parts[p].GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            track.Set(clip, mixDuration: 0f);
            track.Apply();
            RefreshMesh();
        }

        private int Bone(string name)
        {
            for (int i = 0; i < pose.Data.Bones.Length; i++) if (pose.Data.Bones[i].Name == name) return i;
            throw new InvalidDataException("Logo bone is missing.");
        }

        private int[] Slots(string[] names)
        {
            var result = new int[names.Length];
            for (int n = 0; n < names.Length; n++)
            {
                int found = -1;
                for (int i = 0; i < pose.Data.Slots.Length; i++) if (pose.Data.Slots[i].Name == names[n]) { found = i; break; }
                if (found < 0) throw new InvalidDataException("Logo closing slot is missing.");
                result[n] = found;
            }
            return result;
        }

        public void SetHorizontalOffsets(Vector2 offsets)
        {
            // SetBonePosition 0x18d16f8 changes Bone.X/Y, then updates world
            // transforms. These root positions have no animation channels.
            pose.Local[left][0] = initialLeft + offsets.x;
            pose.Local[right][0] = initialRight + offsets.y;
            pose.UpdateWorld();
        }

        public void SetSpeed(float speed) => track.Speed = speed;

        public void Advance(float unityDelta)
        {
            track.Advance(unityDelta);
            track.Apply();
            RefreshMesh();
        }

        public void ClearClosingSlots()
        {
            // OnClose 0x18d1f8c/0x18d1fb8 writes two alphas once; the four
            // Slot.Attachment setters follow at 0x18d1fec through 0x18d2088.
            for (int i = 0; i < alphaSlots.Length; i++) pose.Colors[alphaSlots[i]][3] = 0f;
            for (int i = 0; i < attachmentSlots.Length; i++) pose.SetAttachment(attachmentSlots[i], -1);
            RefreshMesh();
        }

        public void RefreshMesh()
        {
            geometry.Build();
            for (int p = 0; p < parts.Length; p++)
            {
                var part = geometry.Parts[p]; var data = part.Vertices;
                for (int i = 0; i < part.VertexCount; i++)
                {
                    int offset = i * 8;
                    positions[p][i] = new Vector3(data[offset], data[offset + 1], 0f);
                    uvs[p][i] = new Vector2(data[offset + 2], data[offset + 3]);
                    colors[p][i] = new Color32(Channel(data[offset + 4]), Channel(data[offset + 5]),
                        Channel(data[offset + 6]), Channel(data[offset + 7]));
                }
                var mesh = meshes[p]; mesh.Clear(false);
                mesh.SetVertices(positions[p], 0, part.VertexCount);
                mesh.SetUVs(0, uvs[p], 0, part.VertexCount);
                mesh.SetColors(colors[p], 0, part.VertexCount);
                mesh.SetTriangles(part.Indices, 0, part.IndexCount, 0, true);
            }
        }

        private static byte Channel(float value) => (byte)Mathf.Clamp((int)(value * 255f), 0, 255);
        private void OnDestroy()
        {
            if (meshes != null) for (int i = 0; i < meshes.Length; i++) if (meshes[i] != null) Destroy(meshes[i]);
            if (material != null) Destroy(material);
        }
    }
}
