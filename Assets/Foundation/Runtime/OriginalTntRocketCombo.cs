using System;

namespace Whitebox.Foundation
{
    public interface IOriginalTntRocketComboServices
    {
        void StartComboOperation();
        void FinishComboOperation();
        void AnimateTntRocket(OriginalGridCell center, OriginalGridCell empty, OriginalSpecialBoardItem to,
            OriginalSpecialBoardItem from, int swipe, Action completed);
        void PlaySfx(int type, float delay);
        void MarkTntRocketCells(OriginalGridCell center, OriginalExplodeData horizontal, OriginalExplodeData vertical);
        OriginalRocketBoardItem CreateComboRocketAt(int tiledId, OriginalCellPoint point);
        void RecycleComboViews(OriginalTntBoardItem tnt, OriginalRocketBoardItem rocket);
    }

    // TntRocketCombo Prepare/Explode/AnimationCompleted, 0x1e2f644/0x1e2f6f0/0x1e31248.
    // The first-board adapter supplies the inactive charger/event boundary.
    public sealed class OriginalTntRocketCombo
    {
        private readonly IOriginalTntRocketComboServices services;
        private OriginalSpecialBoardItem to, from;
        private int swipe;
        public event Action<OriginalCellPoint, int, int> OnComboMade;
        public OriginalTntRocketCombo(IOriginalTntRocketComboServices services) { this.services = services; }
        public void Prepare(OriginalSpecialBoardItem toItem, OriginalSpecialBoardItem fromItem)
        {
            to = toItem; from = fromItem;
            var a = from.CurrentCell; var b = to.CurrentCell;
            swipe = a.X == b.X ? (a.Y < b.Y ? 1 : 0) : (a.X >= b.X ? 3 : 2);
        }
        public void Explode(OriginalExplodeData data)
        {
            var tnt = (to as OriginalTntBoardItem) ?? (OriginalTntBoardItem)from;
            var rocket = (to as OriginalRocketBoardItem) ?? (OriginalRocketBoardItem)from;
            var center = from.CurrentCell; var empty = to.CurrentCell;
            tnt.Mediator.ClearFromAllRegisteredCells();
            rocket.Mediator.ClearFromAllRegisteredCells();
            services.StartComboOperation();
            services.AnimateTntRocket(center, empty, to, from, swipe, () => AnimationCompleted(center, tnt, rocket));
            services.PlaySfx(316, .04f);
            to = null; from = null; // Clear releases prepared refs; completion retains the original models.
        }
        private void AnimationCompleted(OriginalGridCell center, OriginalTntBoardItem tnt, OriginalRocketBoardItem rocket)
        {
            var trigger = OriginalExploder.Next(12);
            var horizontal = OriginalExploder.Next(6);
            var vertical = OriginalExploder.Next(6);
            horizontal.ComboId = trigger.Id; vertical.ComboId = trigger.Id;
            // The admitted level has no jelly; its propagation stays at the loader boundary.
            services.MarkTntRocketCells(center, horizontal, vertical);
            var p = new OriginalCellPoint(center.X, center.Y);
            OnComboMade?.Invoke(p, horizontal.Id, vertical.Id);
            CreateAndExplodeAt(20, new OriginalCellPoint(p.X - 1, p.Y), trigger, vertical);
            CreateAndExplodeAt(20, new OriginalCellPoint(p.X + 1, p.Y), trigger, vertical);
            CreateAndExplodeAt(20, p, trigger, vertical);
            CreateAndExplodeAt(30, p, trigger, horizontal);
            CreateAndExplodeAt(30, new OriginalCellPoint(p.X, p.Y - 1), trigger, horizontal);
            CreateAndExplodeAt(30, new OriginalCellPoint(p.X, p.Y + 1), trigger, horizontal);
            services.FinishComboOperation();
            services.RecycleComboViews(tnt, rocket);
        }
        private void CreateAndExplodeAt(int tiledId, OriginalCellPoint point, OriginalExplodeData trigger, OriginalExplodeData preset)
        {
            preset.Point = point; trigger.Point = point;
            services.CreateComboRocketAt(tiledId, point).ExplodeWithPresetData(trigger, preset);
        }
    }
}
