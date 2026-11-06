using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalSpecialItemCombo
    {
        bool CanComboWith(OriginalBoardItem other);
    }

    // Connects the restored swap decision to the actual local search. Concrete
    // special item models must supply their combo rules before they are loaded.
    public sealed class OriginalSwapMatchContext : IOriginalSwapMatchContext
    {
        private readonly OriginalMatchManager matches;
        public OriginalSwapMatchContext(OriginalMatchManager matches)
        {
            this.matches = matches ?? throw new ArgumentNullException(nameof(matches));
        }
        public IReadOnlyList<IOriginalSwapMatchGroup> FindExtensiveMatchesInArea(OriginalGridCell center, int radius)
        {
            return matches.FindExtensiveMatchesInArea(center.X, center.Y, radius);
        }
        public bool HasSpecialItem(OriginalGridCell cell)
        {
            var item = cell.Mediation.CurrentItem;
            return item != null && item.IsSpecialItem;
        }
        public bool CanComboWith(OriginalBoardItem special, OriginalBoardItem other)
        {
            if (!(special is IOriginalSpecialItemCombo combo))
                throw new NotSupportedException("Special item combo rules are not restored.");
            return combo.CanComboWith(other);
        }
    }
}
