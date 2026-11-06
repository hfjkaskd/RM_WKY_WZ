using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalLightballExplosionServices : IOriginalSpecialItemExplosionServices
    {
        OriginalGameStateManager State { get; }
        OriginalDefaultLightballStrategy CreateDefaultLightballStrategy();
        void UsefulMoveMade();
        void ExplodeLightballView(OriginalLightballItemSpriteView view);
    }
    // Normal native LightballItemModel activation. Combo strategies remain separate.
    public sealed class OriginalLightballBoardItem : OriginalSpecialBoardItem, IOriginalLightballTapItem
    {
        public override int ItemType => 5;
        public override int GetMoveType => 4;
        public override bool CanComboWith(OriginalBoardItem other) => other == null || other.IsSpecialItem || other is OriginalMatchBoardItem; // 0x1f3e8c0.
        public OriginalLightballItemSpriteView ItemView { get; private set; }
        private IOriginalLightballExplosionServices explosion;
        public OriginalDefaultLightballStrategy Strategy { get; private set; }
        public void BindExplosion(IOriginalLightballExplosionServices services)
        { explosion = services; BindSpecialExplosion(services); }
        public override IOriginalSpecialCreationView View => ItemView;
        public OriginalLightballBoardItem(int id, Func<float> time, IOriginalSpecialItemStaticState statics) : base(id, 50, time, statics) { }
        public void CreateView(OriginalLevelPresentation presentation, Transform parent, Vector3 position, IOriginalRocketViewServices services)
        {
            ItemView = presentation.SpawnLightballView();
            ItemView.Initialize(parent, position, services);
            BindSwapView(ItemView); MarkViewCreated();
        }
        protected override void TryExplode(OriginalExplodeData data)
        { Start(null, data); explosion.UsefulMoveMade(); } // 0x1f3de40.
        private bool Start(OriginalBoardItem other, OriginalExplodeData data) // 0x1f3dec8.
        {
            if (Strategy != null && Strategy.DidStart) return true;
            if (Strategy == null) Strategy = explosion.CreateDefaultLightballStrategy();
            Strategy.Start(this, other, data);
            if (!Strategy.DidStart) return false;
            explosion.State.StartSpecialOperation(); return true;
        }
        public override void SwapWith(OriginalBoardItem other, OriginalExplodeData data) => Start(other, data);
        public void ComboWith(OriginalBoardItem other, OriginalDefaultLightballStrategy strategy, OriginalExplodeData data)
        {
            Strategy = strategy;
            Start(other, data); // Native ComboWith 0x1f3e390 delegates to the same operation-owning Start.
        }
        public override bool Tap(OriginalExplodeData data) => !ItemView.IsSwapping && Start(null, data);
        public void ExplodeSelf() // 0x1f3e058, remaining-moves/event providers inactive in first-level composition.
        {
            explosion.CollectRemainingMovesCoin(CurrentCell,this); // 0x1f3e088..0x1f3e0d8.
            explosion.ExplodeLightballView(ItemView);
            CurrentCell.StaticTouch.ExplodeTopMostBelowItem(OriginalExploder.Next(15));
            FinalExplodeCompleted(.15f);
        }
    }
}
