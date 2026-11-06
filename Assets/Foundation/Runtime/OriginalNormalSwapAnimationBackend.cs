using System;

namespace Whitebox.Foundation
{
    // Presentation and resolution must be supplied by the restored Unity
    // services. In particular, flash/particles/explosions have no empty defaults.
    public interface IOriginalNormalSwapServices : IOriginalSwapCompletionServices
    {
        void ClearSinglePropellerWinCalculationData();
        OriginalSwapPoint GetCellLocalPosition(OriginalGridCell cell);
        void PlayCellSwapFlash(OriginalGridCell cell);
        void SetSortingForAnimation(IOriginalSwapAnimationView view, OriginalSwapSorting sorting);
        void MovePropellerComboToLocal(IOriginalSwapAnimationView view, OriginalGridCell destination,
            float duration, Action onComplete);
        OriginalSwapSorting GetSwapStartSorting();
        object PlaySwapParticle(IOriginalSwapAnimationView view, int direction);
        bool IsViewMissing(IOriginalSwapAnimationView view);
    }

    // SwapAction.PlayValidSwapAnimation 0x1ee2cd0, PlayPropellerComboSwapAnimation
    // 0x1ee3b58 and PlayWrongSwapAnimation 0x1ee35f4. One backend per action.
    public sealed class OriginalNormalSwapAnimationBackend : IOriginalSwapAnimationBackend
    {
        private readonly OriginalSwapActionState action;
        private readonly IOriginalNormalSwapServices services;
        private readonly float duration, soundDelay;

        public OriginalNormalSwapAnimationBackend(OriginalSwapActionState action, IOriginalNormalSwapServices services,
            float duration, float soundDelay)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.duration = duration;
            this.soundDelay = soundDelay;
        }

        public void PlayValidSwap(OriginalSwapActionState selectedAction)
        {
            RequireAction(selectedAction);
            var movingItem = action.To.Mediation.CurrentItem;
            var otherItem = action.From.Mediation.CurrentItem;
            RequireConfiguredItems(movingItem, otherItem);
            bool isCombo = otherItem != null && otherItem.IsSpecialItem && movingItem.IsSpecialItem;
            int comboType = otherItem is OriginalSpecialBoardItem other && movingItem is OriginalSpecialBoardItem moving
                ? OriginalPlayableBoard.GetSpecialComboType(other, moving) : 0;
            services.ClearSinglePropellerWinCalculationData();
            services.PlaySfx(254, soundDelay);
            // PlayValidSwapAnimation 0x1ee2fb4..0x1ee30a0 dispatches combo 6
            // before starting the ordinary swap animation or its fall holds.
            if (comboType == 6)
            {
                services.ExplodeSpecialItems(otherItem, movingItem);
                return;
            }

            // Native generated closures belong to this swap. The action can
            // become idle before its secondary callback runs, so a new swap
            // must not overwrite the previous operation's callback context.
            var completion = new OriginalSwapCompletion(action, services, duration, soundDelay);

            action.MarkAnimating();
            var primaryView = services.GetView(movingItem);
            int direction = action.From.Neighbors.GetNeighborType(action.To.X, action.To.Y);
            StartCellSwap(action.From);
            StartCellSwap(action.To);
            action.NotifyAnimationStart();
            var swapSorting = services.GetSwapStartSorting();

            bool propellerCombo = comboType is 8 or 9 or 10;
            IOriginalSwapAnimationView secondaryView = null;
            if (otherItem != null)
            {
                secondaryView = services.GetView(otherItem);
                var secondarySorting = new OriginalSwapSorting(swapSorting.Layer,
                    unchecked(swapSorting.Order - 100), swapSorting.SortEverything);
                // 0x1ee3d8c forces the stationary partner's sorting even during creation.
                if (propellerCombo) services.SetSortingForAnimation(secondaryView, secondarySorting);
                else services.SetSorting(secondaryView, secondarySorting);
                secondaryView.SwapAnimationStarted(true, movingItem.ItemType);
                int moveType = propellerCombo ? 1 : GetSingleSwapMoveType(otherItem, movingItem);
                completion.ConfigureValid(otherItem, movingItem, primaryView, secondaryView, isCombo, moveType);
                if (!isCombo) // 0x1ee3338..0x1ee3424: combo keeps the secondary view in place.
                {
                    int reverse = action.To.Neighbors.GetNeighborType(action.From.X, action.From.Y);
                    services.PlaySwapParticle(secondaryView, reverse);
                    services.MoveToLocal(secondaryView, services.GetCellLocalPosition(action.From), duration, completion.ValidSecondaryComplete);
                }
            }
            else completion.ConfigureValid(null, movingItem, primaryView, null, false, GetSingleSwapMoveType(null, movingItem));

            primaryView.SwapAnimationStarted(true, otherItem == null ? 0 : otherItem.ItemType);
            services.SetSorting(primaryView, swapSorting);
            services.PlaySwapParticle(primaryView, direction);
            if (propellerCombo)
                services.MovePropellerComboToLocal(primaryView, action.To, duration, completion.ValidPrimaryComplete);
            else
                services.MoveToLocal(primaryView, services.GetCellLocalPosition(action.To), duration, completion.ValidPrimaryComplete);
        }

        public void PlayWrongSwap(OriginalSwapActionState selectedAction)
        {
            RequireAction(selectedAction);
            var fromItem = action.From.Mediation.CurrentItem;
            var toItem = action.To.Mediation.CurrentItem;
            RequireConfiguredItems(fromItem, toItem);
            var completion = new OriginalSwapCompletion(action, services, duration, soundDelay);

            services.PlaySfx(254, soundDelay);
            action.MarkAnimating();
            var primaryView = services.GetView(fromItem);
            var secondaryView = toItem == null ? null : services.GetView(toItem);
            var toOrigin = services.GetCellLocalPosition(action.To);
            var fromOrigin = services.GetCellLocalPosition(action.From);
            int direction = action.From.Neighbors.GetNeighborType(action.To.X, action.To.Y);
            StartCellSwap(action.From);
            StartCellSwap(action.To);
            action.NotifyAnimationStart();
            var sorting = services.GetSwapStartSorting();
            services.SetSorting(primaryView, sorting);
            primaryView.SwapAnimationStarted(false, 0);
            object primaryParticle = services.PlaySwapParticle(primaryView, direction);
            completion.ConfigureWrong(fromItem, toItem, primaryView, secondaryView, primaryParticle, null,
                fromOrigin, toOrigin, default(OriginalSwapSorting));
            services.MoveToLocal(primaryView, toOrigin, duration, completion.WrongPrimaryOutComplete);

            // Native Unity Object equality is checked after starting the first
            // move. The second piece keeps its ordinary sorting on the outward leg.
            if (services.IsViewMissing(secondaryView)) return;
            int reverse = action.To.Neighbors.GetNeighborType(action.From.X, action.From.Y);
            var secondarySwapSorting = services.GetSwapStartSorting();
            secondaryView.SwapAnimationStarted(false, 0);
            object secondaryParticle = services.PlaySwapParticle(secondaryView, reverse);
            completion.ConfigureWrong(fromItem, toItem, primaryView, secondaryView, primaryParticle, secondaryParticle,
                fromOrigin, toOrigin, secondarySwapSorting);
            services.MoveToLocal(secondaryView, fromOrigin, duration, completion.WrongSecondaryOutComplete);
        }

        private void StartCellSwap(OriginalGridCell cell)
        {
            cell.FreezeFall(); // CellModel.SwapStarted 0x319db80 starts here.
            services.PlayCellSwapFlash(cell);
        }
        private void RequireAction(OriginalSwapActionState value)
        {
            if (!ReferenceEquals(value, action)) throw new InvalidOperationException("Swap backend belongs to a different action.");
        }
        private static void RequireConfiguredItems(OriginalBoardItem moving, OriginalBoardItem other)
        {
            if (moving == null || moving.SwapView == null || other != null && other.SwapView == null)
                throw new InvalidOperationException("Swap items require configured native views.");
        }
        private static int GetSingleSwapMoveType(OriginalBoardItem other, OriginalBoardItem moving)
        {
            // Non-combo branches of 0x1ee402c. Combo animation/dispatch is
            // restored independently and must not be treated as a normal move.
            if (other != null && other.IsSpecialItem && moving.IsSpecialItem)
            {
                if (other is OriginalLightballBoardItem && moving is OriginalLightballBoardItem) return 10; // Native combo1.
                if (other is OriginalTntBoardItem && moving is OriginalTntBoardItem) return 14; // 0x1ee4114..0x1ee4130.
                if (other is OriginalRocketBoardItem && moving is OriginalRocketBoardItem) return 11; // 0x1ee4134..0x1ee4150.
                if ((other is OriginalLightballBoardItem && moving is OriginalTntBoardItem)
                    || (other is OriginalTntBoardItem && moving is OriginalLightballBoardItem)) return 8;
                if ((other is OriginalLightballBoardItem && moving is OriginalRocketBoardItem)
                    || (other is OriginalRocketBoardItem && moving is OriginalLightballBoardItem)) return 8;
                if ((other is OriginalLightballBoardItem && moving is OriginalPropellerBoardItem)
                    || (other is OriginalPropellerBoardItem && moving is OriginalLightballBoardItem)) return 8;
                throw new NotSupportedException("This special combo swap presentation is not restored yet.");
            }
            if (other is OriginalLightballBoardItem || moving is OriginalLightballBoardItem) return 4;
            if (other is OriginalRocketBoardItem || moving is OriginalRocketBoardItem) return 13;
            return 1;
        }
    }
}
