namespace Whitebox.Foundation
{
    // EggItemModel 0x2e4c094–0x2e4c4ac.
    public sealed class OriginalEggBoardItem : OriginalBoardItem, IOriginalItemFallViewDelegate
    {
        private OriginalPlayableBoard game;
        public OriginalEggItemSpriteView ItemView { get; private set; }
        OriginalGridCell IOriginalItemFallViewDelegate.CurrentCell => Mediator.CurrentCell?.Owner;
        public override bool IsFallable => true;
        public override bool IsSwappable => true;
        public override bool CanExplodeWithNearMatch => true;

        internal OriginalEggBoardItem(OriginalItemSettings settings) : base(settings, 1) { }

        internal void BindView(OriginalEggItemSpriteView view, OriginalPlayableBoard owner)
        {
            ItemView = view;
            game = owner;
            BindSwapView(view);
            MarkViewCreated();
        }

        public override bool Tap(OriginalExplodeData data) // 0x2e4c378.
        {
            game.PlaySfx(143, .04f);
            game.PlayHaptic(25, 0, .7f, 0f, false);
            ItemView.PlayTap();
            return false;
        }

        protected override void TryExplode(OriginalExplodeData data) // 0x2e4c23c and 0x2e4c27c.
        {
            if (!HasView) return;
            game.ExplodeEggView(ItemView);
            game.UsefulMoveMade();
            FinalExplodeCompleted(.15f);
        }
    }
}
