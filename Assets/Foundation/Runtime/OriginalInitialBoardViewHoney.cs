using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalInitialBoardView
    {
        private void CreateHoneyModels(FTiledGrid data) // CellGridManager.CreateStaticItems 0x320f87c.
        {
            if (!InitialItemAmounts.IsThereStaticItemInLevel(OriginalHoneyBoardItem.StaticType)) return;
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                int index = OriginalLevelRules.GetIndexFromPoint(x, y, data.Width);
                bool hasHoney = restoredGrid == null ? data.Cells[index].Honey == OriginalHoneyBoardItem.TiledId :
                    restoredGrid.GetStaticItem(x, y, OriginalHoneyBoardItem.TiledId, OriginalHoneyBoardItem.TiledId) != null;
                if (hasHoney)
                    Grid.GetCell(x, y).AddStaticItem(new OriginalHoneyBoardItem(Goals));
            }
        }

        internal void BindHoneyViews(OriginalGameAudioHost audio, IOriginalLevelSeedRandom random,
            IOriginalBoxExplosionServices useful)
        {
            int amount = InitialItemAmounts.GetStaticItemAmount(OriginalHoneyBoardItem.StaticType);
            if (amount == 0) return;
            presentation.PrepareHoneyPools(amount);
            int itemLayer = SortingLayer.NameToID("Item");
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                var cell = Grid.GetCell(x, y);
                if (!(cell.StaticTouch.GetStaticItem(OriginalHoneyBoardItem.StaticType) is OriginalHoneyBoardItem honey)) continue;
                var view = presentation.SpawnHoneyView();
                view.Initialize(cell, presentation.HoneyAssets, presentation, audio, random,
                    boardRoot, Layout.CurrentBoard.Bottom, itemLayer);
                honey.Bind(view, playable, useful);
                var item = cell.Mediation.CurrentItem;
                var itemView = (item as OriginalSpecialBoardItem)?.FallView ?? item?.SwapView as IOriginalFallItemView;
                itemView?.SetIsUnderNonFallBlockingAboveStaticItem(true);
            }
        }
    }
}
