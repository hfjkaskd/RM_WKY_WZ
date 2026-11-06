using System;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalGameStartManager
    {
        // GameManager.CreateItems 0x1e3bb98. Return value counts initialization
        // attempts, not spawned items. The native cap permits500 retries after
        // the first population, but reports500 when that cap is reached.
        public int CreateItems(IOriginalLevelDataProvider data)
        {
            Required(GridManager).CreateStaticItems(data, CurrentLevelOrigin);
            Required(GridManager).CreateItems(data, CurrentLevelOrigin);
            if (CurrentLevelOrigin == 3)
            {
                Required(Required(ItemFactory).ItemCreator).UpdateCreatedItemsCountExceptPredefined();
                return 1;
            }
            var parser = (IOriginalGameStartParser)(data ?? throw new NullReferenceException());
            int attempts = 1;
            while (true)
            {
                if (!parser.IsLevelRandomized) return attempts;
                if (!Required(ShuffleManager).ShouldTryAgain()) return attempts;
                Required(LevelContext).RemoveLateUnits();
                Required(Required(ItemFactory).ItemCreator).ClearCreatedItemsCountExceptPredefined();
                parser.RandomizeTileGrid();
                Required(GridManager).CreateItems(data, CurrentLevelOrigin);
                attempts++;
                if (attempts == 501) return 500;
            }
        }
    }
}
