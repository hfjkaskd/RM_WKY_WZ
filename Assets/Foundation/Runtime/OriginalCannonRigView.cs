using System;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCannonRigView : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material materialTemplate;
        [SerializeField] private Transform fuseFollower;
        [SerializeField] private string rigPath, texturePath;
        [SerializeField] private string arrivalClip, fireClip, fuseBone;
        [SerializeField] private float portraitMixSeconds;

        private OriginalKingPose pose;
        private OriginalKingAnimationTrack track;
        private OriginalKingMesh geometry;
        private Mesh mesh;
        private Material material;
        private Vector3[] positions;
        private float[] fuseWorld;
        private int arrival, fire;
        private float fireDuration;
        private bool playing, firing;

        internal void Initialize()
        {
            var asset = Resources.Load<TextAsset>(rigPath);
            var texture = Resources.Load<Texture2D>(texturePath);
            if (asset == null || texture == null) throw new InvalidOperationException("Cannon rig resources are missing.");

            OriginalKingRigData data;
            using (var stream = new MemoryStream(asset.bytes, false))
                data = new OriginalKingRigData(stream);
            Resources.UnloadAsset(asset);
            pose = new OriginalKingPose(data);
            track = new OriginalKingAnimationTrack(pose, portraitMixSeconds, replaceTrack: true);
            geometry = new OriginalKingMesh(pose, null);
            arrival = data.FindClip(arrivalClip);
            fire = data.FindClip(fireClip);
            fireDuration = data.Clips[fire].Duration;
            int fuseIndex = Array.FindIndex(data.Bones, bone => bone.Name == fuseBone);
            if (fuseIndex < 0) throw new InvalidDataException("Cannon fuse bone is missing.");
            fuseWorld = pose.World[fuseIndex];

            geometry.Build();
            var part = geometry.Parts[0];
            positions = new Vector3[part.VertexCount];
            var uv = new Vector2[part.VertexCount];
            var colors = new Color32[part.VertexCount];
            for (int i = 0; i < part.VertexCount; i++)
            {
                int offset = i * 8;
                uv[i] = new Vector2(part.Vertices[offset + 2], part.Vertices[offset + 3]);
                colors[i] = new Color32(Channel(part.Vertices[offset + 4]), Channel(part.Vertices[offset + 5]),
                    Channel(part.Vertices[offset + 6]), Channel(part.Vertices[offset + 7]));
            }
            mesh = new Mesh { name = "Original Cannon" };
            mesh.MarkDynamic();
            UploadVertices(part);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(part.Indices, 0, part.IndexCount, 0);
            meshFilter.sharedMesh = mesh;
            material = new Material(materialTemplate) { mainTexture = texture };
            meshRenderer.sharedMaterial = material;
            SyncFuse();
        }

        internal void PlayArrival()
        {
            track.Set(arrival, loop: true, mixDuration: 0f);
            playing = true;
        }

        // CannonBoosterView.PlayExplodeAnimation 0x3192ac8 configures track 1.
        internal void PlayFire(float speed, bool landscape)
        {
            track.Set(fire, speed: speed, mixDuration: landscape ? 0f : portraitMixSeconds);
            firing = true;
        }

        private void Update()
        {
            if (!playing) return;
            track.Advance(Time.deltaTime);
            track.Apply();
            geometry.Build();
            UploadVertices(geometry.Parts[0]);
            if (firing && track.Time >= fireDuration) playing = false;
        }

        private void UploadVertices(OriginalKingMesh.Part part)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                int offset = i * 8;
                positions[i] = new Vector3(part.Vertices[offset], part.Vertices[offset + 1], 0f);
            }
            mesh.SetVertices(positions);
        }

        private static byte Channel(float value) => (byte)Mathf.Clamp((int)(value * 255f), 0, 255);

        private void LateUpdate()
        {
            if (fuseWorld != null) SyncFuse();
        }

        private void SyncFuse()
        {
            var point = new Vector3(fuseWorld[4], fuseWorld[5], 0f);
            float angle = Mathf.Atan2(fuseWorld[2], fuseWorld[0]) * Mathf.Rad2Deg;
            fuseFollower.SetPositionAndRotation(transform.TransformPoint(point),
                transform.rotation * Quaternion.AngleAxis(angle, Vector3.forward));
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
