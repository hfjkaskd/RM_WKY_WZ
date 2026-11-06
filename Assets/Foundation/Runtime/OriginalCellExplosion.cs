namespace Whitebox.Foundation
{
    public partial class OriginalGridCell
    {
        // CellModel.ExplodeCurrentItem 0x3190884. The current item owns its
        // below-static hit. The cell dispatches directly when it is empty.
        public void ExplodeCurrentItem(OriginalExplodeData data)
        {
            if (StaticTouch.ExplodeTopMostAboveItem(data)) return;
            if (!Mediation.HasCurrentItem()) { StaticTouch.ExplodeTopMostBelowItem(data); return; }
            var item = Mediation.CurrentItem;
            var obstacle = OriginalMultiCellObstacle.GetMaster(item);
            if (obstacle != null && item.IsObstacle)
                obstacle.RecordFootprintBelowExploder(data.Id);
            item.Explode(data);
            StaticTouch.RecordBelowExploder(data.Id);
        }
    }

    public abstract class OriginalMultiCellObstacle : OriginalBoardItem
    {
        private OriginalMatchCells footprint;

        protected OriginalMultiCellObstacle(OriginalItemSettings settings) : base(settings, settings.LayerCount) { }

        // Native FakeItemModel.GetAllCells 0x2b21f28 returns the master's footprint.
        internal static OriginalMultiCellObstacle GetMaster(OriginalBoardItem item) => item switch
        {
            OriginalMultiCellObstacle master => master,
            OriginalCupboardFakeBoardItem proxy => proxy.Master,
            OriginalPotionFakeBoardItem proxy => proxy.Master,
            OriginalBushFakeBoardItem proxy => proxy.Master,
            OriginalSafeFakeBoardItem proxy => proxy.Master,
            _ => null,
        };

        internal void AddFootprintCell(OriginalGridCell cell) // MultipleCellItemModel.AddCell 0x2b4bcdc.
        {
            if (footprint.Count == 0) footprint.Add(Mediator.CurrentCell.Owner);
            footprint.Add(cell);
        }

        internal void RecordFootprintBelowExploder(int id)
        {
            // Register before damage can expose the other cells in this wave.
            for (int i = 0; i < footprint.Count; i++)
                footprint.Get(i).StaticTouch.RecordBelowExploder(id);
        }
    }

    public sealed partial class OriginalStaticTouchMediator
    {
        internal void RecordBelowExploder(int id) => belowExploders.AddToExploders(id);
    }
}
