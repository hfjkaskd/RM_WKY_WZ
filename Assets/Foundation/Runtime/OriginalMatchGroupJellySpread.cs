using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchJellyViews
    {
        bool HasJellyView(OriginalGridCell cell);
        void SetSpreadData(OriginalMatchGroupSnapshot group);
    }
    public sealed class OriginalMatchGroupJellySpread
    {
        private readonly OriginalInitialItemAmounts amounts;
        private readonly IOriginalMatchJellyViews views;
        public OriginalMatchGroupJellySpread(OriginalInitialItemAmounts amounts, IOriginalMatchJellyViews views)
        { this.amounts = amounts ?? throw new ArgumentNullException(nameof(amounts)); this.views = views; }
        public bool Init(OriginalMatchGroupSnapshot group, ref OriginalExplodeData data) // 0x12dfba8.
        {
            if (!amounts.IsThereStaticItemInLevel(3)) return false;
            for (int i = 0; i < group.Cells.Count; i++)
            {
                var cell = group.Cells.Get(i);
                if (cell == null) throw new NullReferenceException();
                if (RequireViews().HasJellyView(cell)) data.CanSpreadJelly = true;
            }
            if (!data.CanSpreadJelly) return false;
            RequireViews().SetSpreadData(group);
            return true;
        }
        private IOriginalMatchJellyViews RequireViews() => views ?? throw new InvalidOperationException("Jelly model/view/spread owners are required for a Jelly level.");
    }
}
