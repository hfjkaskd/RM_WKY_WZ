using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // The original Mecanim states drive a native Unity Mesh sampled from the recovered rig.
    public sealed class OriginalMailRigView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Transform envelopeFollower;
        [SerializeField] private string envelopeBone;
        private OriginalKingPose pose;
        private OriginalKingMesh geometry;
        private Mesh mesh;
        private Vector3[] positions;
        private Vector2[] uv;
        private Color32[] colors;
        private int clip, bone;
        public MeshRenderer Renderer => meshRenderer;
        public Transform Envelope => envelopeFollower;
        public float NormalizedTime { get; private set; }
        internal float PlaybackSpeed => animator.speed;

        public void Bind(OriginalKingRigData data, Material material)
        {
            if (pose != null) return;
            pose = new OriginalKingPose(data);
            geometry = new OriginalKingMesh(pose, null);
            int capacity = geometry.Parts[0].Vertices.Length / 8;
            positions = new Vector3[capacity];
            uv = new Vector2[capacity];
            colors = new Color32[capacity];
            mesh = new Mesh { name = gameObject.name + " mesh" };
            mesh.MarkDynamic();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            if (envelopeFollower != null)
            {
                bone = Array.FindIndex(data.Bones, entry => entry.Name == envelopeBone);
                if (bone < 0) throw new InvalidOperationException("Mailbox envelope bone is missing.");
            }
        }

        public void Play(string name, float speed = 1f, float normalizedTime = 0f, float initialDelta = 0f)
        {
            clip = pose.Data.FindClip(name);
            animator.enabled = true;
            animator.speed = speed;
            animator.Play(Animator.StringToHash("Base Layer." + name), 0, normalizedTime);
            animator.Update(initialDelta);
            if (initialDelta > 0f)
                normalizedTime = Mathf.Clamp01(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            Render(normalizedTime);
            enabled = pose.Data.Clips[clip].Duration > 0f && normalizedTime < 1f;
            if (!enabled) animator.enabled = false;
        }

        private void LateUpdate()
        {
            Render(Mathf.Clamp01(animator.GetCurrentAnimatorStateInfo(0).normalizedTime));
            if (NormalizedTime < 1f) return;
            animator.enabled = false;
            enabled = false;
        }

        private void Render(float time)
        {
            NormalizedTime = time;
            pose.Sample(clip, time * pose.Data.Clips[clip].Duration, false);
            geometry.Build();
            var part = geometry.Parts[0];
            var buffer = part.Vertices;
            for (int i = 0; i < part.VertexCount; i++)
            {
                int j = i * 8;
                positions[i] = new Vector3(buffer[j], buffer[j + 1], 0);
                uv[i] = new Vector2(buffer[j + 2], buffer[j + 3]);
                colors[i] = new Color(buffer[j + 4], buffer[j + 5], buffer[j + 6], buffer[j + 7]);
            }
            mesh.Clear(false);
            mesh.SetVertices(positions, 0, part.VertexCount);
            mesh.SetUVs(0, uv, 0, part.VertexCount);
            mesh.SetColors(colors, 0, part.VertexCount);
            mesh.SetTriangles(part.Indices, 0, part.IndexCount, 0, true);
            if (envelopeFollower == null) return;
            var world = pose.World[bone];
            envelopeFollower.position = transform.TransformPoint(world[4], world[5], 0f);
            envelopeFollower.rotation = transform.rotation * Quaternion.Euler(0f, 0f, Mathf.Atan2(world[2], world[0]) * Mathf.Rad2Deg);
            var local = pose.Local[bone];
            envelopeFollower.localScale = new Vector3(local[3], local[4], 1f);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
