using System;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelSwapServices
    {
        bool IsImpossibleMoveDisabledForCell(OriginalCellPoint point);
        void StartImpossibleSwapAnimation(OriginalSwapActionState action, OriginalGridCell from, OriginalCellPoint to);
        void IncreaseFalseSwipeCount();
        void CheckForUsefulMove();
    }
    // LevelTouchManager.SwapCells 0x1ee23c4. Owns the native selection and
    // guard/notification order; animation and session services are mandatory.
    public sealed class OriginalLevelSwapRouter
    {
        private readonly OriginalCellGrid grid;
        private readonly OriginalSwapActionState first, second;
        private readonly IOriginalSwapAnimationBackend firstBackend, secondBackend;
        private readonly IOriginalItemSwapAvailability availability;
        private readonly IOriginalLevelSwapServices services;
        public event Action<OriginalGridCell, OriginalGridCell, bool> OnSwap;
        public OriginalLevelSwapRouter(OriginalCellGrid grid, OriginalSwapActionState first, OriginalSwapActionState second,
            IOriginalSwapAnimationBackend firstBackend, IOriginalSwapAnimationBackend secondBackend,
            IOriginalItemSwapAvailability availability, IOriginalLevelSwapServices services)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.first = first ?? throw new ArgumentNullException(nameof(first));
            this.second = second ?? throw new ArgumentNullException(nameof(second));
            this.firstBackend = firstBackend ?? throw new ArgumentNullException(nameof(firstBackend));
            this.secondBackend = secondBackend ?? throw new ArgumentNullException(nameof(secondBackend));
            this.availability = availability ?? throw new ArgumentNullException(nameof(availability));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public bool SwapCells(OriginalGridCell from, OriginalCellPoint destination)
        {
            var selected = OriginalSwapActionState.Select(first, second);
            if (selected == null) return false;
            selected.Clear();
            var to = grid.GetCell(destination.X, destination.Y);
            if (!OriginalCellTouchRules.CanSwapFrom(from, availability)) return false;
            bool valid;
            if (to == null || !OriginalCellTouchRules.CanSwapTo(to, availability))
            {
                if (!services.IsImpossibleMoveDisabledForCell(new OriginalCellPoint(from.X, from.Y)))
                    services.StartImpossibleSwapAnimation(selected, from, destination);
                services.IncreaseFalseSwipeCount();
                valid = false;
            }
            else
            {
                valid = selected.StartSwapAnimation(from, to, ReferenceEquals(selected, first) ? firstBackend : secondBackend);
                if (valid) services.CheckForUsefulMove();
                else services.IncreaseFalseSwipeCount();
            }
            // The original event is required at this point; a missing subscriber
            // is a binding error rather than a silently dropped move record.
            OnSwap(from, to, valid);
            return valid;
        }
    }
}
