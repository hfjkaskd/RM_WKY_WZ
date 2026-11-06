using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalRocketExplosionServices : IOriginalSpecialItemExplosionServices
    {
        OriginalCellGrid Grid { get; }
        bool IsInCurrentBoard(OriginalGridCell cell);
        void UsefulMoveMade();
    }

    public sealed partial class OriginalRocketBoardItem
    {
        private IOriginalRocketExplosionServices explosion;
        private OriginalExplodeData presetExplodeData;
        protected override bool CreatedByLightball => IsCreatedByLightball;
        public void BindExplosion(IOriginalRocketExplosionServices services)
        { explosion = services ?? throw new ArgumentNullException(nameof(services)); BindSpecialExplosion(services); }
        public void RecycleView() // ItemModel 0x2b46db0, forced carried-special cleanup.
        {if(!HasView)return;HasView=false;ItemView.Recycle();}
        public void ExplodeWithPresetData(OriginalExplodeData incoming, OriginalExplodeData preset) // 0x27ba914.
        { presetExplodeData = preset; Explode(incoming); }
        protected override void TryExplode(OriginalExplodeData incoming) // 0x27ba960, normal first-board branch.
        {
            explosion.CollectRemainingMovesCoin(CurrentCell,this); // 0x27baa04..0x27baa4c, before flight/mediation changes.
            var first = new List<OriginalCellPoint>();
            var second = new List<OriginalCellPoint>();
            var center = CurrentCell ?? explosion.Grid.GetCell(incoming.Point.X, incoming.Point.Y);
            if (Orientation == 0) { AddNeighborsRecursive(first, center, 7); AddNeighborsRecursive(second, center, 3); }
            else if (Orientation == 1) { AddNeighborsRecursive(first, center, 1); AddNeighborsRecursive(second, center, 5); }
            var data = incoming.Trigger == 7 || incoming.Trigger == 12 ? presetExplodeData :
                OriginalExploder.Next(6, new OriginalCellPoint(center.X, center.Y));
            // The formal loader rejects all jelly/above-static level data. Their
            // native propagation owner must be composed before admitting them.
            if (unchecked((uint)(incoming.Trigger - 10)) < 3) data.CanSpreadJelly |= incoming.CanSpreadJelly;
            data.IsHorizontalRocket = Orientation == 0;
            ItemView.Play(data, center, first, second);
            if (CurrentCell != null)
            {
                CurrentCell.StaticTouch.ExplodeTopMostBelowItem(data);
                FinalExplodeCompleted(.15f);
            }
            else if (center != null)
            {
                center.ExplodeCurrentItem(data);
                center.StaticTouch.ExplodeTopMostBelowItem(data);
            }
            explosion.UsefulMoveMade();
        }
        private void AddNeighborsRecursive(List<OriginalCellPoint> list, OriginalGridCell cell, int direction) // 0x27bb018.
        {
            while (cell != null)
            {
                cell = cell.Neighbors.Get(direction);
                if (cell == null || !explosion.IsInCurrentBoard(cell)) return;
                list.Add(new OriginalCellPoint(cell.X, cell.Y));
            }
        }
    }
}
