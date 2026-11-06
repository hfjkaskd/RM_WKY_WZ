using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCellMask : MonoBehaviour
    {
        [SerializeField] public SpriteRenderer readMask;
        [SerializeField] public SpriteRenderer writeMask;
        [SerializeField] public OriginalCellViewSettings settings;

        // CellMask.SetSorting 0x3446904: write first, ignore SortEverything.
        public void SetSorting(OriginalSwapSorting sorting)
        {
            Apply(writeMask, sorting.Layer, unchecked(sorting.Order - 1));
            Apply(readMask, sorting.Layer, sorting.Order);
        }

        // 0x34469a0: preserve the writer's current layer, not the supplied layer.
        public void SetWriteMaskSortingOrder(OriginalSwapSorting sorting)
        {
            int layer = writeMask.sortingLayerID;
            int previousOrder = writeMask.sortingOrder; // Native GetSorting reads both.
            Apply(writeMask, layer, sorting.Order);
        }

        // 0x3446a08: disabling also resets sorting and both renderer poses.
        public void Enable(bool enable)
        {
            Apply(readMask, settings.readMaskLayer, -1);
            Apply(writeMask, settings.writeMaskLayer, -1);
            gameObject.SetActive(enable);
            ArrangeMaskSize(readMask, settings.maskSize, settings.maskPosition);
            ArrangeMaskSize(writeMask, settings.maskSize, settings.maskPosition);
        }

        // 0x3446b58. Do not prevalidate the transform before changing the size.
        public static void ArrangeMaskSize(SpriteRenderer renderer, Vector2 size, Vector3 position)
        {
            renderer.size = size;
            renderer.transform.localPosition = position;
        }

        private static void Apply(SpriteRenderer renderer, int layer, int order)
        {
            renderer.sortingLayerID = layer;
            renderer.sortingOrder = order;
        }
    }
}
