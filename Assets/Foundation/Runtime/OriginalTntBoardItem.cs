using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalTntBoardItem : OriginalSpecialBoardItem, IOriginalTntItemViewDelegate
    {
        private readonly IOriginalSpecialItemChainedStaticState staticState;
        public OriginalTntItemSpriteView ItemView { get; private set; }
        public OriginalTntExplosionConfig Config { get; private set; }
        public int RemainingMoveCoins { get; } = 3;
        public bool IsCreatedByLightball { get; }
        public override int ItemType => 4; // 0x27b5554.
        public override IOriginalSpecialCreationView View => ItemView;
        public override bool IsSwappable => IsActive && !IsPreparedForSpecialUse; // 0x27b6614.
        public override bool IsAvailableForSwap() => base.IsAvailableForSwap() && !isExploded; // 0x27b65d4.
        public OriginalTntBoardItem(int id, bool createdByLightball, int tiledId, Func<float> levelTime,
            IOriginalSpecialItemChainedStaticState staticState) : base(id, tiledId, levelTime, staticState)
        {
            this.staticState = staticState;
            IsCreatedByLightball = createdByLightball;
        }
        public void CreateView(OriginalLevelPresentation presentation, Transform parent, Vector3 position, IOriginalTntViewServices services)
        {
            ItemView = presentation.SpawnTntView();
            presentation.InitializeTntView(ItemView, this, parent, position, services);
            BindSwapView(ItemView); MarkViewCreated();
            Config = presentation.TntExplosionConfig; // Native PrepareConfig follows HasView (0x27b55a8).
        }
        public bool IsUnderHoney() => staticState.IsUnderHoney(this);
        public bool IsUnderRoof() => staticState.IsUnderRoof(this);
        public bool IsUnderChain() => staticState.IsUnderChain(this);
        public void IncreaseExplosion() // 0x27b65c0; retain every other config field.
        { var config = Config; config.Radius = 4; Config = config; }
        public void DisableExplosion() // 0x27b65cc.
        { var config = Config; config.Radius = 0; Config = config; }
    }
    // Tiled-40 branch of ItemModelFactory + ItemCreator.CreateItemAtCell.
    public static class OriginalTntItemCreation
    {
        public static OriginalTntBoardItem CreateAtCell(int id, OriginalGridCell cell, OriginalLevelPresentation presentation,
            Transform parent, Func<float> levelTime, IOriginalSpecialItemChainedStaticState staticState, IOriginalTntViewServices services)
        {
            if (cell == null) throw new NullReferenceException();
            var model = new OriginalTntBoardItem(id, false, 40, levelTime, staticState);
            model.CreateView(presentation, parent, new Vector3(cell.X, cell.Y, 0f), services);
            cell.Mediation.ClearAllItems(); cell.Mediation.SetAllItems(model);
            return model;
        }
    }
}
