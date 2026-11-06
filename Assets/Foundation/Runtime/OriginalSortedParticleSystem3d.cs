using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalSortedParticleSystem3d : MonoBehaviour
    {
        [SerializeField] private int zOffset;
        [SerializeField] private Renderer ren;
        [SerializeField] private new ParticleSystem particleSystem;
        public Renderer Renderer => ren;
        public ParticleSystem ParticleSystem => particleSystem;
        public int ZOffset => zOffset;

        internal void SetDepth(float z)
        {
            var position = transform.position;
            position.z = z;
            transform.position = position;
        }
    }

    // Sorted3DObjectList 0x12d6684/0x12d72b4. Update depths from the
    // insertion or first removal, preserving the existing prefix.
    public sealed class OriginalSortedParticleDepth
    {
        private const int StartDepth = 10;
        private readonly List<OriginalSortedParticleSystem3d> active = new List<OriginalSortedParticleSystem3d>();

        public void Add(OriginalSortedParticleSystem3d particle)
        {
            var renderer = particle.Renderer;
            int layer = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            int depth = StartDepth;
            int i = 0;
            for (; i < active.Count; i++)
            {
                var item = active[i];
                var other = item.Renderer;
                if (layer > SortingLayer.GetLayerValueFromID(other.sortingLayerID) ||
                    (renderer.sortingLayerID == other.sortingLayerID && renderer.sortingOrder > other.sortingOrder))
                    break;
                depth += item.ZOffset;
            }

            active.Insert(i, particle);
            for (; i < active.Count; i++)
            {
                var item = active[i];
                item.SetDepth(depth + .5f * item.ZOffset);
                depth += item.ZOffset;
            }
        }

        public void Remove(OriginalSortedParticleSystem3d particle)
        {
            int depth = StartDepth;
            int kept = 0;
            for (int i = 0; i < active.Count; i++)
            {
                var item = active[i];
                if (ReferenceEquals(item, particle)) continue;
                if (kept != i)
                {
                    active[kept] = item;
                    item.SetDepth(depth + .5f * item.ZOffset);
                }
                depth += item.ZOffset;
                kept++;
            }
            if (kept < active.Count) active.RemoveRange(kept, active.Count - kept);
        }
    }
}
