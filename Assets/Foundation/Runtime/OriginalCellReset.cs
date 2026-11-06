using System;

namespace Whitebox.Foundation
{
    public interface IOriginalCellResetServices
    {
        void RecycleCellView(OriginalCellView view);
        void RecycleItemView(OriginalBoardItem item);
        void ResetStaticItems(OriginalStaticTouchMediator statics);
    }

    public partial class OriginalGridCell
    {
        // CellModel.Reset 0x319d5a0: recycle the cell view before querying the
        // live target slot; recycle that target before clearing reverse links.
        // Static views belong to their required model owner, not the pool sweep.
        public virtual void Reset(IOriginalCellResetServices services)
        {
            if (CellView != null) services.RecycleCellView(CellView);
            CellView = null;
            if (Mediation.HasTargetItem()) services.RecycleItemView(Mediation.TargetItem);
            Mediation.Reset();
            services.ResetStaticItems(StaticTouch);
        }
    }

    public sealed partial class OriginalFillingCell
    {
        // FillingCellModel.Reset 0x343d6fc: orphan first, base reset next,
        // then dequeue the captured initial queue count. New arrivals survive.
        public override void Reset(IOriginalCellResetServices services)
        {
            if (LastCreatedItem != null && LastCreatedItem.Mediator.TargetCell == null)
            {
                services.RecycleItemView(LastCreatedItem);
                LastCreatedItem = null;
            }
            base.Reset(services);
            int count = topBoardItemQueue == null ? 0 : topBoardItemQueue.Count;
            for (int i = 0; i < count; i++) services.RecycleItemView(topBoardItemQueue.Dequeue());
        }
    }
}
