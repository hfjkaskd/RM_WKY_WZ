using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCoinBoardItem : OriginalBoardItem, IOriginalItemFallViewDelegate
    {
        private OriginalPlayableBoard game;
        public OriginalCoinItemSpriteView ItemView { get; private set; }
        OriginalGridCell IOriginalItemFallViewDelegate.CurrentCell => Mediator.CurrentCell?.Owner;
        public override bool IsFallable => true; // 0x242d0fc.
        public override bool IsSwappable => true; // 0x242d104.
        public override bool CanExplodeWithNearMatch => true; // 0x242d0f4.

        internal OriginalCoinBoardItem(OriginalItemSettings settings) : base(settings, 1) { }

        internal void BindView(OriginalCoinItemSpriteView view, OriginalPlayableBoard game)
        {
            ItemView = view;
            this.game = game;
            BindSwapView(view);
            MarkViewCreated();
        }

        public override bool Tap(OriginalExplodeData data) // 0x242d2b8.
        {
            game.PlayBoardCoinTap(this);
            return false;
        }

        protected override void TryExplode(OriginalExplodeData data) // 0x242d10c.
        {
            if (!HasView) return;
            game.StartBoardCoinCollect(ItemView);
            FinalExplodeCompleted(.15f);
            game.UsefulMoveMade();
        }

        protected override void CollectAsGoal(bool updateUi = true) => game.IncreaseBoardCoinGoal(); // 0x242d1e8.
    }
}
