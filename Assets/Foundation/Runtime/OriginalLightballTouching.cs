namespace Whitebox.Foundation
{
    public partial class OriginalDefaultLightballStrategy
    {
        private void ExplodeTouching(OriginalGridCell cell, OriginalExplodeData data)
        {
            data.Point = new OriginalCellPoint(cell.X, cell.Y);
            // MatchManager's original four cardinal neighbors.
            for (int i = 0; i < OriginalCellNeighbors.TouchingNeighborTypes.Length; i++)
            {
                var neighbor = cell.Neighbors.Get(OriginalCellNeighbors.TouchingNeighborTypes[i]);
                if (neighbor != null) OriginalCellNearMatchExplosion.ExplodeCurrentItemByNearMatch(neighbor, data, statics);
            }
        }
    }
}
