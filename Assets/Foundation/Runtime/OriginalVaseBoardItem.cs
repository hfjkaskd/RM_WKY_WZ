namespace Whitebox.Foundation
{
    // VaseItemModel 0x159ff6c–0x15a0450.
    public sealed class OriginalVaseBoardItem : OriginalBoardItem, IOriginalItemFallViewDelegate
    {
        private OriginalPlayableBoard game;
        public OriginalVaseItemSpriteView ItemView { get; private set; }
        OriginalGridCell IOriginalItemFallViewDelegate.CurrentCell => Mediator.CurrentCell?.Owner;
        public override bool IsFallable => true;
        public override bool IsSwappable => true;
        public override bool CanExplodeWithNearMatch => true;

        internal OriginalVaseBoardItem(OriginalItemSettings settings) : base(settings, settings.LayerCount) { }

        internal void BindView(OriginalVaseItemSpriteView view, OriginalPlayableBoard owner)
        {
            ItemView = view;
            game = owner;
            BindSwapView(view);
            MarkViewCreated();
        }

        public override bool Tap(OriginalExplodeData data)
        {
            game.PlaySfx(358, .04f);
            game.PlayHaptic(25, 0, .7f, 0f, false);
            ItemView.PlayTap();
            return false;
        }

        protected override void TryExplode(OriginalExplodeData data)
        {
            if (!HasView) return;
            layer--;
            if (layer != 0) ItemView.Damage(layer, data.Trigger == 14);
            else
            {
                ItemView.Explode();
                FinalExplodeCompleted(.15f);
            }
            game.UsefulMoveMade();
        }
    }
}
