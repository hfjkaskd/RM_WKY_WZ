using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // CellPoint.GetCellPointFromMousePosition 0x343b7a4; this is arithmetic
    // grid lookup, without physics/colliders or per-cell input components.
    public static class OriginalCellPointInput
    {
        public static OriginalCellPoint FromMousePosition(Vector2 position, bool correctIosEdge, bool isIos)
        {
            float adjustedX = position.x >= 0f ? position.x + .5f : position.x - .5f;
            float adjustedY = position.y >= 0f ? position.y + .5f : position.y - .5f;
            int x = ToCellCoordinate(adjustedX), y = ToCellCoordinate(adjustedY);
            if (correctIosEdge && isIos)
            {
                if (position.x > -.65f && position.x < -.5f) x = 0;
                else if (position.x > 8.5f && position.x < 8.65f) x = 8;
            }
            return new OriginalCellPoint(x, y);
        }
        private static int ToCellCoordinate(float value)
        {
            if (float.IsNaN(value)) return 0;
            if (float.IsPositiveInfinity(value) || value <= int.MinValue) return int.MinValue;
            if (value >= 2147483648f) return int.MaxValue;
            return (int)value;
        }
    }
    // CellGridManager.GetCellFromWorldPosition/GetCellModel 0x320cfa0/fb0.
    // The owning board supplies its native translated world offset and flags.
    public sealed class OriginalCellGridHitTest
    {
        private readonly OriginalCellGrid grid;
        private readonly bool correctIosEdge, isIos;
        public Vector2 WorldOffset { get; set; }
        public OriginalCellGridHitTest(OriginalCellGrid grid, Vector2 worldOffset, bool correctIosEdge, bool isIos)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            WorldOffset = worldOffset; this.correctIosEdge = correctIosEdge; this.isIos = isIos;
        }
        public OriginalGridCell GetCellFromWorldPosition(Vector2 position)
        {
            var point = OriginalCellPointInput.FromMousePosition(position - WorldOffset, correctIosEdge, isIos);
            return grid.GetCell(point.X, point.Y);
        }
    }
}
