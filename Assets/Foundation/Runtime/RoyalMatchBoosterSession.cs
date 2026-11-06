using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IRoyalMatchBoosterInventory : IOriginalBoosterInventoryPresentation
    {
        event Action Changed;
        bool TryConsumeBooster(OriginalBoosterType type);
        void RequestBooster(OriginalBoosterType type, Action<bool> complete);
    }

    internal sealed class RoyalMatchBoosterSession : IDisposable
    {
        private readonly IRoyalMatchBoosterInventory inventory;
        private readonly OriginalPlayableBoard game;
        private readonly OriginalInitialBoardView board;
        private readonly OriginalBottomUi bottom;
        private readonly OriginalGameplayHud hud;
        private readonly Camera camera;
        private readonly OriginalGameSceneProjection projection;
        private readonly CoreFlowSettings configuration;
        private readonly Func<bool> inputBlocked;
        private readonly OriginalBoardPossibleMoveServices shuffleEligibility;

        private OriginalOptionalBoosterTutorial arrow;
        private OriginalOptionalBoosterTutorial cannon;
        private OriginalOptionalBoosterTutorial jester;
        private OriginalOptionalBoosterTutorial selectedOptional;
        private OriginalHammerTutorial hammerTemplate;
        private OriginalHammerBoosterView hammer;
        private GameObject hammerSelection;
        private bool hammerSelected;
        private bool requesting;
        private bool disposed;
        private OriginalGameTouchListener lockedTouch;

        public bool IsSelected => hammerSelected || (selectedOptional != null && selectedOptional.IsSelected);
        public bool IsActive => IsSelected || requesting;

        public RoyalMatchBoosterSession(IRoyalMatchBoosterInventory inventory, OriginalPlayableBoard game,
            OriginalInitialBoardView board, OriginalBottomUi bottom, OriginalGameplayHud hud, Camera camera,
            OriginalGameSceneProjection projection, CoreFlowSettings configuration, Func<bool> inputBlocked)
        {
            this.inventory = inventory;
            this.game = game;
            this.board = board;
            this.bottom = bottom;
            this.hud = hud;
            this.camera = camera;
            this.projection = projection;
            this.configuration = configuration;
            this.inputBlocked = inputBlocked;
            shuffleEligibility = new OriginalBoardPossibleMoveServices(board.MatchBlocking);

            foreach (var slot in bottom.Boosters)
                slot.BindPaidInventory(game.Session.Number, inventory, OnSlotClicked);
            inventory.Changed += Refresh;
        }

        private bool CanUse() =>
            !disposed && !requesting && !inputBlocked() && !game.HasActiveBoosterGuide &&
            game.IsPlaying() && game.CanUserMakeMove() && game.Fall.IsAllItemsSteady &&
            !game.State.HasAnyOperation();

        private void OnSlotClicked(OriginalBoosterType type, bool locked)
        {
            if (locked || !CanUse()) return;

            bool sameSelection = IsSelected && (hammerSelected
                ? type == OriginalBoosterType.Hammer
                : selectedOptional.BoosterType == type);
            CancelSelection();
            if (sameSelection) return;

            if (!inventory.HasFreeUsage(type) && inventory.GetBooster(type) == 0)
            {
                requesting = true;
                lockedTouch = game.TouchListener;
                lockedTouch?.StopTouch();
                lockedTouch?.DisableTouch();
                try
                {
                    inventory.RequestBooster(type, OnRequestCompleted);
                }
                catch
                {
                    ReleaseRequest();
                    throw;
                }
                return;
            }

            switch (type)
            {
                case OriginalBoosterType.Hammer:
                    SelectHammer();
                    break;
                case OriginalBoosterType.Arrow:
                    selectedOptional = arrow ??= CreateAim(configuration.arrowTutorialPrefabPath, type);
                    break;
                case OriginalBoosterType.Cannon:
                    selectedOptional = cannon ??= CreateAim(configuration.cannonTutorialPrefabPath, type);
                    break;
                case OriginalBoosterType.JesterHat:
                    selectedOptional = jester ??= CreateAim(configuration.jesterTutorialPrefabPath, type);
                    break;
            }

            if (selectedOptional != null) selectedOptional.SelectPaid();
        }

        private OriginalHammerTutorial GetHammerTemplate()
        {
            if (hammerTemplate == null)
                hammerTemplate = Resources.Load<GameObject>(configuration.hammerTutorialPrefabPath)
                    ?.GetComponent<OriginalHammerTutorial>();
            if (hammerTemplate == null) throw new InvalidOperationException("Missing authored hammer tutorial prefab.");
            return hammerTemplate;
        }

        private void SelectHammer()
        {
            if (hammerSelection == null)
            {
                foreach (var slot in bottom.Boosters)
                {
                    if (slot.BoosterType != OriginalBoosterType.Hammer) continue;
                    hammerSelection = GetHammerTemplate().CreatePaidSelection(slot.transform);
                    break;
                }
            }
            if (hammerSelection != null) hammerSelection.SetActive(true);
            hammerSelected = true;
        }

        private OriginalOptionalBoosterTutorial CreateAim(string path, OriginalBoosterType type)
        {
            var template = Resources.Load<GameObject>(path)?.GetComponent<OriginalOptionalBoosterTutorial>();
            if (template == null || template.BoosterType != type)
                throw new InvalidOperationException("Invalid authored booster aim prefab: " + path);

            var aim = UnityEngine.Object.Instantiate(template, bottom.transform);
            aim.BindPaid(game, board, hud, bottom, camera, projection, CanUse,
                () => TryConsume(type));
            return aim;
        }

        private bool TryConsume(OriginalBoosterType type)
        {
            if (!CanUse() || (type == OriginalBoosterType.JesterHat && !CanShuffle())) return false;
            return inventory.HasFreeUsage(type) || inventory.TryConsumeBooster(type);
        }

        private bool CanShuffle()
        {
            int eligible = 0;
            var area = board.Layout.CurrentBoard;
            for (int y = area.Bottom; y <= area.Top; y++)
            for (int x = area.Left; x <= area.Right; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell == null || !shuffleEligibility.CanEnterPossibleMatch(cell)) continue;
                var item = cell.Mediation.CurrentItem;
                if ((item.IsMatchItem || item.IsSpecialItem) && ++eligible >= 2) return true;
            }

            return false;
        }

        public bool ShouldDeselect(OriginalGridCell cell)
        {
            if (hammerSelected) return cell == null || !cell.IsNormalCell;
            return selectedOptional != null && selectedOptional.ShouldDeselect(cell);
        }

        public void Use(OriginalGridCell cell)
        {
            if (!CanUse()) return;

            if (!hammerSelected)
            {
                selectedOptional?.Use(cell);
                return;
            }

            if (!HasTarget(cell) || cell.Reservation.IsEnabled) return;
            var strikePrefab = GetHammerTemplate().GetPaidStrikePrefab();
            CancelSelection();
            if (!TryConsume(OriginalBoosterType.Hammer)) return;

            hammer = UnityEngine.Object.Instantiate(strikePrefab, board.transform);
            hammer.Play(game, board, cell, bottom.HammerLaunchFramePosition);
        }

        public static bool HasTarget(OriginalGridCell cell) =>
            cell != null && cell.IsNormalCell && cell.IsTouchEnabled &&
            (cell.Mediation.HasCurrentItem() || cell.StaticTouch.GetTopMostAboveItem() != null ||
             cell.StaticTouch.GetTopMostBelowItem() != null);

        public void CancelSelection()
        {
            hammerSelected = false;
            if (hammerSelection != null) hammerSelection.SetActive(false);
            selectedOptional?.Deselect();
            selectedOptional = null;
        }

        private void OnRequestCompleted(bool success)
        {
            if (disposed || !requesting) return;
            ReleaseRequest();
            Refresh();
        }

        private void ReleaseRequest()
        {
            requesting = false;
            if (lockedTouch != null && lockedTouch.TouchDisableCount > 0)
                lockedTouch.EnableTouch();
            lockedTouch = null;
        }

        private void Refresh()
        {
            foreach (var slot in bottom.Boosters) slot.RefreshPaidInventory();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            inventory.Changed -= Refresh;
            ReleaseRequest();
            CancelSelection();

            foreach (var slot in bottom.Boosters)
                if (slot != null) slot.ReleasePaidInventory(OnSlotClicked);

            if (arrow != null) arrow.Release();
            if (cannon != null) cannon.Release();
            if (jester != null) jester.Release();
            if (hammer != null) hammer.Cancel();
            if (hammerSelection != null) UnityEngine.Object.Destroy(hammerSelection);
        }
    }
}
