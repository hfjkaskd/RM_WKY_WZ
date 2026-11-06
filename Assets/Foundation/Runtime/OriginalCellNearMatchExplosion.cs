using System;

namespace Whitebox.Foundation
{
    public interface IOriginalCellStaticExplosionServices
    {
        bool TryExplodeTopMostAboveItem(OriginalGridCell cell, OriginalExplodeData data);
    }
    public partial class OriginalBoardItem
    {
        public virtual bool CanExplodeWithNearMatch => throw new NotSupportedException("Item near-match capability is not restored.");
    }
    public sealed partial class OriginalMatchBoardItem
    {
        public override bool CanExplodeWithNearMatch => false; // 0x21a5a6c.
    }
    public sealed partial class OriginalBoxBoardItem
    {
        public override bool CanExplodeWithNearMatch => true; // 0x2bc5af8.
    }

    public static class OriginalCellNearMatchExplosion
    {
        public static void ExplodeCurrentItemByNearMatch(OriginalGridCell cell, OriginalExplodeData data,
            IOriginalCellStaticExplosionServices services) // 0x319d7a4.
        {
            if (cell == null || services == null) throw new NullReferenceException();
            if (services.TryExplodeTopMostAboveItem(cell, data)) return;
            if (!cell.Mediation.HasCurrentItem()) return;
            var item = cell.Mediation.CurrentItem;
            if (item == null) throw new NullReferenceException();
            if (!item.CanExplodeWithNearMatch) return;
            // Native obtains CurrentItem again after the virtual capability
            // query. A side effect may have replaced or removed that item.
            item = cell.Mediation.CurrentItem;
            if (item == null) throw new NullReferenceException();
            item.Explode(data);
        }
    }
}
