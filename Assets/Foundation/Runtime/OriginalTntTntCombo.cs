using System;

namespace Whitebox.Foundation
{
    // TntTntCombo 0x1e2ecf8/0x1e2ee20 and its completion closure 0x1e32e88.
    // The admitted first-board composition has no unlocked event/charger owner.
    public sealed class OriginalTntTntCombo
    {
        private readonly OriginalLevelPresentation presentation;
        private readonly OriginalGameStateManager state;
        private OriginalTntBoardItem to, from;
        public bool IsComboActive { get; private set; }
        public OriginalTntTntCombo(OriginalLevelPresentation presentation, OriginalGameStateManager state)
        { this.presentation = presentation; this.state = state; }
        public void Prepare(OriginalTntBoardItem toItem, OriginalTntBoardItem fromItem)
        { to = toItem; from = fromItem; }
        public void Explode(OriginalExplodeData data)
        {
            to.DisableExplosion(); from.IncreaseExplosion();
            var exploding = from;
            var neighbor = to;
            var cellTo = to.CurrentCell;
            var cellFrom = from.CurrentCell;
            exploding.SetSpecialItemActive(false);
            cellTo.FreezeFall(); cellFrom.FreezeFall();
            IsComboActive = true;
            var view = presentation.Pool.Spawn<OriginalTntTntComboView>(15, true);
            state.StartSpecialOperation();
            view.Init(presentation.Pool, cellFrom.GetViewPosition(), () =>
            {
                state.FinishSpecialOperation();
                exploding.SetSpecialItemActive(true);
                exploding.Explode(data);
                neighbor.Explode(data);
                cellTo.UnFreezeFall(); cellFrom.UnFreezeFall();
                IsComboActive = false;
            });
            // Native Clear releases only the prepared references, while the
            // per-explosion callback retains both models and their cells.
            to = null; from = null;
        }
    }
}
