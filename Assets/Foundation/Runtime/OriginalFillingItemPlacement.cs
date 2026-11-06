using System;

namespace Whitebox.Foundation
{
    public partial class OriginalBoardItem
    {
        public IOriginalFillingFallComponent FallComponent { get; set; }
    }

    public interface IOriginalFillingFallComponent
    {
        void SetFillingCell(OriginalFillingCell cell);
    }

    public interface IOriginalFillingItemCreation
    {
        OriginalBoardItem CreateItemForFillingCellAt(float x, float y, OriginalFillingCell fillingCell);
    }

    // FallManager.CreateItemFromFillingCell 0x32112d0. Item-specific creation,
    // native prefab/view setup and fall execution are supplied by the caller.
    public static class OriginalFillingItemPlacement
    {
        public static void CreateItemFromFillingCell(OriginalFallRefillColumns columns,
            OriginalFillingCell fillingCell, OriginalGridCell target, IOriginalFillingItemCreation creator)
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            if (fillingCell == null) throw new ArgumentNullException(nameof(fillingCell));
            // This branch checks only disabled columns, not the pending set.
            if (columns.IsTopColumnDisabled(fillingCell.X)) return;
            if (fillingCell.HasCreatedItem)
            {
                OriginalBoardItem existing = fillingCell.LastCreatedItem;
                if (existing.Mediator.TargetCell != null) return;
                if (target == null) throw new ArgumentNullException(nameof(target));
                target.Mediation.SetTargetItem(existing);
                return;
            }
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (target.Mediation.HasTargetItem()) return;
            if (creator == null) throw new ArgumentNullException(nameof(creator));
            OriginalBoardItem item = creator.CreateItemForFillingCellAt(fillingCell.X, (float)fillingCell.Y + 1f, fillingCell);
            if (item == null) return;
            target.Mediation.SetTargetItem(item);
            // Original initializes the fall component even if target binding fails.
            if (item.FallComponent == null) throw new InvalidOperationException("Created item has no fall component.");
            item.FallComponent.SetFillingCell(fillingCell);
        }
    }
}
