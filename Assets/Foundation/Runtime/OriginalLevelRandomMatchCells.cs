namespace Whitebox.Foundation
{
    public sealed partial class OriginalLevelRandomSource
    {
        // 0x1ed98f8. MatchGroup.ShuffleCells 0x12df454 supplies an inline
        // value copy: swaps consume n-1 difficulty draws but do not reorder
        // the group's original Cell14 storage.
        public void ShuffleCellsBasedOnDifficulty(OriginalMatchCells cells)
        {
            int count = cells.Count;
            for (int i = 0; i < count - 1; i++)
            {
                int selected = DifficultyNext(i, count);
                var value = cells.Get(selected);
                var previous = cells.Get(i);
                cells.Set(i, value);
                cells.Set(selected, previous);
            }
        }
    }
}
