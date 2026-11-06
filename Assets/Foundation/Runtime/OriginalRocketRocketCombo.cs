using UnityEngine;

namespace Whitebox.Foundation
{
    // RocketRocketCombo.Prepare/Explode/Clear 0x1e2f16c/0x1e2f314/0x1e30a9c.
    // Charger/event notifications remain at their admitted first-board boundary.
    public sealed class OriginalRocketRocketCombo
    {
        private OriginalRocketBoardItem to, from;
        private OriginalGridCell toCell, emptyCell;
        private OriginalCellPoint toPoint;
        public void Prepare(OriginalRocketBoardItem toItem, OriginalRocketBoardItem fromItem)
        {
            emptyCell = toItem.CurrentCell;
            toCell = fromItem.CurrentCell;
            toPoint = new OriginalCellPoint(toCell.X, toCell.Y);
            to = toItem; from = fromItem;
            from.Mediator.ClearFromAllRegisteredCells();
            to.Mediator.ClearFromAllRegisteredCells();
        }
        public void Explode(OriginalExplodeData data)
        {
            emptyCell.FreezeFallForDuration(.15f);
            data.Point = toPoint;
            from.ChangeOrientation(unchecked((to.Orientation + 1) % 2)); // ItemOrientationExtensions.Opposite 0x2b489ac.
            from.ChangePosition(new Vector3(toPoint.X, toPoint.Y, 0f));
            var trigger = OriginalExploder.Next(7);
            trigger.Point = toPoint;
            var preset = OriginalExploder.Next(6, toPoint);
            from.ExplodeWithPresetData(trigger, preset);
            to.ExplodeWithPresetData(trigger, preset);
            // Native Clear intentionally retains toCell/toPoint.
            emptyCell = null; to = null; from = null;
        }
    }
}
