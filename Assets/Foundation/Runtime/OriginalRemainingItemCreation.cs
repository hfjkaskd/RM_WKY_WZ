using System;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalFillSetGeneration
    {
        public void RemainingMovesStarted() // 0x23f0d00. Native does not recalculate global set ratios.
        {
            for (int i = 0; i < sets.Length; i++)
            {
                var set = sets[i] ?? throw new NullReferenceException();
                if (set.ContainsMatchItem()) set.FillFourColors();
                if (set.ContainsSpecialItem()) set.DisableFilling();
            }
        }
    }
}
