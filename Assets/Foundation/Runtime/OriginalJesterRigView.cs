using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation
{
    // JesterHatView's non-looping track 1. Original curves and the outgoing
    // entry keep advancing through the portrait exit mix on the Unity clock.
    public sealed class OriginalJesterRigView : MonoBehaviour
    {
        [SerializeField] private string rigPath, texturePath;
        [SerializeField] private string enterClip, exitClip, followerBone;
        [SerializeField] private float portraitMixSeconds;
        [SerializeField] private MeshFilter[] parts;
        [SerializeField] private Transform boneFollower;

        private OriginalKingPose pose;
        private OriginalKingAnimationTrack track;
        private OriginalKingMesh geometry;
        private Mesh[] meshes;
        private Vector3[][] positions;
        private Vector2[][] uvs;
        private Color32[][] colors;
        private Dictionary<Material, Material> materials;
        private float[] followerWorld;
        private int enter, exit;
        private float exitDuration;
        private bool playing;

        public int BoneCount => pose == null ? 0 : pose.Data.Bones.Length;
        public int PartCount => geometry == null ? 0 : geometry.Parts.Length;
        public float ExitDuration => exitDuration;

        internal void Initialize()
        {
            if (pose != null) return;
            var asset = Resources.Load<TextAsset>(rigPath);
            var texture = Resources.Load<Texture2D>(texturePath);
            if (asset == null || texture == null) throw new InvalidOperationException("Jester rig resources are missing.");
            OriginalKingRigData data;
            using (var stream = new MemoryStream(asset.bytes, false)) data = new OriginalKingRigData(stream);
            Resources.UnloadAsset(asset);
            pose = new OriginalKingPose(data);
            track = new OriginalKingAnimationTrack(pose, portraitMixSeconds, replaceTrack: true);
            geometry = new OriginalKingMesh(pose, null, splitBlendRuns: true);
            if (geometry.Parts.Length != parts.Length) throw new InvalidDataException("Jester blend partitions.");
            enter = data.FindClip(enterClip);
            exit = data.FindClip(exitClip);
            exitDuration = data.Clips[exit].Duration;
            int bone = -1;
            for (int i = 0; i < data.Bones.Length; i++)
                if (data.Bones[i].Name == followerBone) { bone = i; break; }
            if (bone < 0 || boneFollower == null) throw new InvalidDataException("Jester particle follower is missing.");
            followerWorld = pose.World[bone];

            meshes = new Mesh[parts.Length];
            positions = new Vector3[parts.Length][];
            uvs = new Vector2[parts.Length][];
            colors = new Color32[parts.Length][];
            materials = new Dictionary<Material, Material>();
            for (int p = 0; p < parts.Length; p++)
            {
                int capacity = geometry.Parts[p].Vertices.Length / 8;
                positions[p] = new Vector3[capacity];
                uvs[p] = new Vector2[capacity];
                colors[p] = new Color32[capacity];
                var mesh = meshes[p] = new Mesh { name = parts[p].name };
                mesh.MarkDynamic();
                parts[p].sharedMesh = mesh;
                var renderer = parts[p].GetComponent<MeshRenderer>();
                var template = renderer.sharedMaterial;
                if (template == null) throw new InvalidDataException("Jester blend material is missing.");
                if (!materials.TryGetValue(template, out var material))
                {
                    material = new Material(template) { mainTexture = texture };
                    materials.Add(template, material);
                }
                renderer.sharedMaterial = material;
            }
            pose.Sample(enter, 0f, false);
            Upload();
            SyncFollower();
        }

        internal void PlayEnter(float speed)
        {
            track.Set(enter, speed: speed, mixDuration: 0f);
            playing = true;
        }

        internal void PlayExit(float speed)
        {
            // SetAnimation on the same track preserves the actual outgoing
            // time, including WaitForSeconds rounding at this frame cadence.
            track.Set(exit, speed: speed, mixDuration: portraitMixSeconds);
        }

        private void Update()
        {
            if (!playing) return;
            track.Advance(Time.deltaTime);
            track.Apply();
            Upload();
        }

        private void Upload()
        {
            geometry.Build();
            for (int p = 0; p < parts.Length; p++)
            {
                var part = geometry.Parts[p];
                var data = part.Vertices;
                for (int i = 0; i < part.VertexCount; i++)
                {
                    int offset = i * 8;
                    positions[p][i] = new Vector3(data[offset], data[offset + 1], 0f);
                    uvs[p][i] = new Vector2(data[offset + 2], data[offset + 3]);
                    colors[p][i] = new Color32(Channel(data[offset + 4]), Channel(data[offset + 5]),
                        Channel(data[offset + 6]), Channel(data[offset + 7]));
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

        private void LateUpdate()
        {
            if (followerWorld != null) SyncFollower();
        }

        private void SyncFollower()
        {
            var point = new Vector3(followerWorld[4], followerWorld[5], 0f);
            float angle = Mathf.Atan2(followerWorld[2], followerWorld[0]) * Mathf.Rad2Deg;
            boneFollower.SetPositionAndRotation(transform.TransformPoint(point),
                transform.rotation * Quaternion.AngleAxis(angle, Vector3.forward));
        }

        private void OnDestroy()
        {
            if (meshes != null)
                for (int i = 0; i < meshes.Length; i++) if (meshes[i] != null) Destroy(meshes[i]);
            if (materials != null) foreach (var material in materials.Values) Destroy(material);
        }
    }
}
