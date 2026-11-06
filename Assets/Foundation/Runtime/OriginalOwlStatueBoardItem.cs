namespace Whitebox.Foundation
{
    // OwlStatueItemModel 0x2473270–0x24735d8.
    public sealed class OriginalOwlStatueBoardItem : OriginalBoardItem
    {
        private OriginalPlayableBoard game;
        public OriginalOwlStatueItemView ItemView { get; private set; }
        public override bool IsFallable => false;
        public override bool IsSwappable => false;
        public override bool CanExplodeWithNearMatch => false;

        internal OriginalOwlStatueBoardItem(OriginalItemSettings settings) : base(settings, settings.LayerCount) { }

        internal void BindView(OriginalOwlStatueItemView view, OriginalPlayableBoard owner)
        {
            ItemView = view;
            game = owner;
            BindSwapView(view);
            MarkViewCreated();
        }

        public override bool PlayTouchDownAnimation()
        {
            ItemView.PlayTap(Mediator.CurrentCell.Owner.Y);
            return true;
        }

        protected override void TryExplode(OriginalExplodeData data)
        {
            if (!HasView) return;
            layer--;
            if (layer == 0)
            {
                ItemView.Explode();
                FinalExplodeCompleted(.15f);
            }
            else ItemView.Damage(layer);
            game.UsefulMoveMade();
        }
    }
}
