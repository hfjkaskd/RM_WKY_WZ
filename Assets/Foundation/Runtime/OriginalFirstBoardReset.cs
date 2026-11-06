using System;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard : IOriginalCellResetServices
    {
        // CellGridManager.Clear 0x320d678..0x320d700 walks columns before rows.
        // The remaining camera/scroll/event subscriptions require the complete
        // grid-manager owner; this is the currently composed first-board part.
        public void ResetBoundGridViews()
        {
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++) Grid.GetCell(x, y).Reset(this);
        }

        void IOriginalCellResetServices.RecycleCellView(OriginalCellView view) => Pool.Recycle(view);
        void IOriginalCellResetServices.RecycleItemView(OriginalBoardItem item)
        {
            if (item == null) throw new NullReferenceException();
            OriginalItemViewRecycle.Recycle(item, Pool);
        }

        void IOriginalCellResetServices.ResetStaticItems(OriginalStaticTouchMediator statics)
        {
            if (statics == null) throw new NullReferenceException();
            if (statics.AboveItems.Count != 0 || statics.BelowItems.Count != 0)
                throw new NotSupportedException("StaticItemMediator.Reset requires its concrete static model/view owners.");
            // This formal composition rejects all static-item level data before
            // initialization. Empty native lists have no views to return.
            statics.AboveItems.Clear(); statics.BelowItems.Clear();
        }
    }
}
