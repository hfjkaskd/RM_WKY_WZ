using UnityEngine;

namespace Whitebox.Foundation
{
    // Values recovered from CellBorder..cctor / CellView..cctor and CellMask.Enable.
    // Kept in a shared lightweight asset so the prefab owns its presentation setup.
    [CreateAssetMenu(menuName = "Recovery/Cell View Settings")]
    public sealed class OriginalCellViewSettings : ScriptableObject
    {
        public int readMaskLayer;
        public int writeMaskLayer;
        public Vector2 maskSize;
        public Vector3 maskPosition;
        public Vector2 sideBorderSize;
        public Vector3 topPosition, rightPosition, bottomPosition, leftPosition;
        public Vector2 outerCornerSizeReduction;
        public Vector2 innerCornerSizeReduction;
        public float outerCornerHalfOffset;
        public float innerCornerHalfOffset;
        public Color mainColor;
        public Color altColor;
    }
}
