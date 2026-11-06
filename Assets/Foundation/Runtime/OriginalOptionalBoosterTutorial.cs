using System;
using UnityEngine;
using UnityEngine.UI;
using SortingSnapshot = Whitebox.Foundation.OriginalModalTutorialView.SortingSnapshot;

namespace Whitebox.Foundation
{
    // Optional InGameBoosterTutorial for guided cells or a confirmation button.
    // The optional lesson leaves one free use for the current attempt.
    public sealed class OriginalOptionalBoosterTutorial : MonoBehaviour
    {
        [SerializeField] private OriginalBoosterType boosterType;
        [SerializeField] private Vector2Int targetMin, targetMax;
        [SerializeField] private int highlightOrder, freeAimShadeOrder;
        [SerializeField] private string freeAimShadeLayer;
        [SerializeField] private Canvas dimCanvas;
        [SerializeField] private Image dim;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Transform prompt;
        [SerializeField] private float promptOffset, fadeSeconds, minimumShownSeconds;
        [SerializeField] private Vector2 pointerOffset;
        [SerializeField] private float pointerTravel, pointerHalfPeriod;
        [SerializeField] private string pointerPath, badgePath, selectionPath, infoPath, effectPath;
        [SerializeField] private string confirmationPrefabPath;
        [SerializeField] private Vector2Int confirmationArrowCell;

        private enum Phase { Prompt, GuidedAim, Ready, FreeAim, Spent }

        private OriginalPlayableBoard game;
        private OriginalInitialBoardView board;
        private OriginalGameplayHud hud;
        private OriginalBottomUi bottom;
        private OriginalInGameBoosterSlot slot;
        private Camera source;
        private OriginalGameSceneProjection sceneProjection;
        private Transform pointer, info;
        private GameObject badge, selection;
        private GameObject confirmation;
        private OriginalGameTouchListener aimTouch;
        private OriginalOptionalBoosterEffect effect;
        private SortingSnapshot[] raised;
        private Phase phase = Phase.Spent;
        private bool buttonBound;
        private bool requiresConfirmation;
        private float shownAt, targetAlpha, infoOffsetX;
        private float aimShownAt;
        private int slotOrder, dimLayer, dimOrder;
        private Rect viewport, safeArea;
        private float aspect, orthographicSize;
        private Vector3 cameraPosition, boardPosition, bottomPosition;

        public bool IsGuiding => phase == Phase.Prompt || phase == Phase.GuidedAim;
        public bool IsSelected => phase == Phase.GuidedAim || phase == Phase.FreeAim;
        internal bool IsSpent => phase == Phase.Spent;
        public Button DismissButton => dismissButton;
        public Button ConfirmationButton { get; private set; }

        internal void Bind(OriginalPlayableBoard game, OriginalInitialBoardView board, OriginalGameplayHud hud,
            OriginalBottomUi bottom, Camera camera, OriginalGameSceneProjection projection, bool restoredReady = false)
        {
            this.game = game;
            this.board = board;
            this.hud = hud;
            this.bottom = bottom;
            source = camera;
            sceneProjection = projection;
            foreach (var candidate in bottom.Boosters)
                if (candidate.BoosterType == boosterType) { slot = candidate; break; }
            requiresConfirmation = !string.IsNullOrEmpty(confirmationPrefabPath);
            if (slot == null || (!requiresConfirmation && (targetMin.x < 0 || targetMin.y < 0 ||
                targetMax.x < targetMin.x || targetMax.y < targetMin.y ||
                targetMax.x >= board.Grid.Width || targetMax.y >= board.Grid.Height)))
                throw new InvalidOperationException("The optional booster tutorial button or target region is missing.");

            if (!requiresConfirmation)
                raised = new SortingSnapshot[(targetMax.x - targetMin.x + 1) * (targetMax.y - targetMin.y + 1)];
            pointer = Create(pointerPath, transform).transform;
            badge = Create(badgePath, slot.transform);
            slot.ButtonCanvas.worldCamera = source;
            slotOrder = slot.ButtonCanvas.sortingOrder;
            dimCanvas.worldCamera = source;
            dimLayer = dimCanvas.sortingLayerID;
            dimOrder = dimCanvas.sortingOrder;
            targetAlpha = dim.color.a;
            dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0f);
            shownAt = board.Clock.Time;
            slot.BindTutorialButton(game.Session.Number, Select);
            buttonBound = true;
            dismissButton.onClick.AddListener(Dismiss);
            dismissButton.interactable = false;
            if (restoredReady)
            {
                phase = Phase.Ready;
                HidePrompt();
                dim.gameObject.SetActive(false);
                return;
            }
            ChangeGridLocks(true, false);
            phase = Phase.Prompt;
            RaiseSlot(true);
            RefreshLayout();
        }

        private void Select(OriginalBoosterType type, bool locked)
        {
            if (locked || !game.CanUserMakeMove() || phase == Phase.Spent) return;
            if (IsSelected) { Deselect(); return; }
            bool guided = phase == Phase.Prompt;
            if (guided) HidePrompt();
            if (guided && !requiresConfirmation)
            {
                int index = 0;
                for (int y = targetMin.y; y <= targetMax.y; y++)
                for (int x = targetMin.x; x <= targetMax.x; x++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    cell.EnableTouch();
                    if (cell.Mediation.CurrentItem?.SwapView is OriginalBoxItemSpriteView view)
                    {
                        var group = view.sortingGroup;
                        raised[index++] = new SortingSnapshot(group);
                        group.sortingLayerID = dimLayer;
                        group.sortingOrder = highlightOrder;
                        group.sortAtRoot = true;
                        group.enabled = true;
                    }
                }
            }
            phase = guided ? Phase.GuidedAim : Phase.FreeAim;
            bool shadeAboveBoard = guided || requiresConfirmation;
            dimCanvas.sortingLayerID = shadeAboveBoard ? dimLayer : SortingLayer.NameToID(freeAimShadeLayer);
            dimCanvas.sortingOrder = shadeAboveBoard ? dimOrder : freeAimShadeOrder;
            dim.gameObject.SetActive(true);
            var color = dim.color;
            color.a = targetAlpha;
            dim.color = color;
            RaiseSlot(true);
            if (info == null)
            {
                info = Create(infoPath, transform).transform;
                infoOffsetX = info.localPosition.x;
            }
            info.gameObject.SetActive(true);
            if (selection == null) selection = Create(selectionPath, transform);
            selection.transform.position = slot.transform.position;
            selection.SetActive(true);
            if (requiresConfirmation)
            {
                hud.SetBehindBoosterShade(true);
                if (confirmation == null)
                {
                    confirmation = Create(confirmationPrefabPath, transform);
                    ConfirmationButton = confirmation.GetComponentInChildren<Button>();
                    ConfirmationButton.GetComponent<Canvas>().worldCamera = source;
                    ConfirmationButton.onClick.AddListener(Confirm);
                }
                aimTouch = game.TouchListener;
                aimTouch?.DisableTouch();
                confirmation.SetActive(true);
                pointer.gameObject.SetActive(guided);
                aimShownAt = board.Clock.Time;
            }
            RefreshLayout();
        }

        internal bool ShouldDeselect(OriginalGridCell cell) =>
            IsSelected && (requiresConfirmation || cell == null || (phase == Phase.GuidedAim && !IsGuidedCell(cell.X, cell.Y)));

        internal void Deselect()
        {
            if (!IsSelected) return;
            if (phase == Phase.GuidedAim)
            {
                ChangeGridLocks(false, !requiresConfirmation);
                if (raised != null) foreach (var snapshot in raised) snapshot.Restore();
            }
            if (requiresConfirmation)
            {
                if (hud != null) hud.SetBehindBoosterShade(false);
                aimTouch?.EnableTouch();
                aimTouch = null;
                confirmation.SetActive(false);
                pointer.gameObject.SetActive(false);
            }
            phase = Phase.Ready;
            RaiseSlot(false);
            if (dim != null) dim.gameObject.SetActive(false);
            if (info != null) info.gameObject.SetActive(false);
            if (selection != null) selection.SetActive(false);
        }

        internal void Use(OriginalGridCell cell)
        {
            if (requiresConfirmation || !IsSelected || ShouldDeselect(cell) || !cell.IsTouchEnabled || !game.CanUserMakeMove()) return;
            if (!cell.Mediation.HasCurrentItem() && cell.StaticTouch.GetTopMostAboveItem() == null &&
                cell.StaticTouch.GetTopMostBelowItem() == null) return;
            Spend(cell);
        }

        private void Confirm()
        {
            if (IsSelected && game.CanUserMakeMove() && game.Fall.IsAllItemsSteady && !game.State.HasAnyOperation())
                Spend(null);
        }

        private void Spend(OriginalGridCell cell)
        {
            effect = Create(effectPath, board.BoardRoot).GetComponent<OriginalOptionalBoosterEffect>();
            if (effect == null) throw new InvalidOperationException("The optional booster effect component is missing.");
            Deselect();
            ReleaseButton();
            phase = Phase.Spent;
            effect.Play(game, board, source, bottom, cell);
        }

        private void Dismiss()
        {
            if (phase != Phase.Prompt || board.Clock.Time - shownAt < minimumShownSeconds) return;
            ChangeGridLocks(false, false);
            HidePrompt();
            dim.gameObject.SetActive(false);
            RaiseSlot(false);
            phase = Phase.Ready;
        }

        private void HidePrompt()
        {
            dismissButton.interactable = false;
            dim.raycastTarget = false;
            prompt.gameObject.SetActive(false);
            pointer.gameObject.SetActive(false);
        }

        private void RaiseSlot(bool active)
        {
            if (slot == null) return;
            if (slot.ButtonCanvas != null) slot.ButtonCanvas.sortingOrder = active ? highlightOrder : slotOrder;
            if (slot.TutorialSorting != null) slot.TutorialSorting.enabled = active;
        }

        private bool IsGuidedCell(int x, int y) =>
            x >= targetMin.x && x <= targetMax.x && y >= targetMin.y && y <= targetMax.y;

        private void ChangeGridLocks(bool disable, bool skipGuidedCells)
        {
            for (int y = 0; y < board.Grid.Height; y++)
            {
                for (int x = 0; x < board.Grid.Width; x++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    if (cell == null || (skipGuidedCells && IsGuidedCell(x, y))) continue;
                    if (disable) cell.DisableTouch();
                    else cell.EnableTouch();
                }
            }
        }

        private void RefreshLayout()
        {
            viewport = source.pixelRect;
            safeArea = Screen.safeArea;
            aspect = source.aspect;
            orthographicSize = source.orthographicSize;
            cameraPosition = source.transform.position;
            boardPosition = board.BoardRoot.position;
            bottomPosition = bottom.transform.position;
            Vector3 center = cameraPosition;
            center.z = 0f;
            dimCanvas.transform.position = center;
            float height = 2f * orthographicSize;
            dim.rectTransform.sizeDelta = new Vector2(height * aspect, height);
            if (phase == Phase.Prompt)
                prompt.position = bottomPosition + Vector3.up * promptOffset;
            if (IsSelected)
            {
                float safeTop = sceneProjection != null ? sceneProjection.SafeTopCenter.y
                    : source.ScreenToWorldPoint(new Vector3(0f, safeArea.yMax, 0f)).y;
                // BoosterInfoView.GetPortraitPosition (0x13c56b0).
                if (Application.platform == RuntimePlatform.IPhonePlayer && safeArea.height < Screen.height) safeTop += .3f;
                // GridPositioner.GetGridTopCenterMaxPosition uses the eleven-cell maximum.
                var area = board.Layout.CurrentBoard;
                float gridTop = boardPosition.y + (area.Bottom + area.Top) * .5f + 5.5f;
                info.position = new Vector3(center.x + infoOffsetX * bottom.transform.localScale.x, gridTop + (safeTop - gridTop) * .5f, 0f);
                selection.transform.position = slot.transform.position;
                if (requiresConfirmation)
                    confirmation.transform.position = boardPosition + new Vector3((area.Left + area.Right) * .5f, (area.Bottom + area.Top) * .5f, 0f);
            }
        }

        private void LateUpdate()
        {
            if (!IsGuiding && !IsSelected) return;
            if (viewport != source.pixelRect || safeArea != Screen.safeArea ||
                aspect != source.aspect || orthographicSize != source.orthographicSize ||
                cameraPosition != source.transform.position || boardPosition != board.BoardRoot.position ||
                bottomPosition != bottom.transform.position) RefreshLayout();
            if (phase == Phase.Prompt)
            {
                float elapsed = board.Clock.Time - shownAt;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                var color = dim.color;
                color.a = targetAlpha * t * (2f - t);
                if (dim.color.a != color.a) dim.color = color;
                if (!dismissButton.interactable && elapsed >= minimumShownSeconds) dismissButton.interactable = true;
                MovePointer(slot.transform.position + (Vector3)pointerOffset, elapsed);
            }
            else if (phase == Phase.GuidedAim && requiresConfirmation)
            {
                MovePointer(boardPosition + new Vector3(confirmationArrowCell.x, confirmationArrowCell.y, 0f),
                    board.Clock.Time - aimShownAt);
            }
        }

        private void MovePointer(Vector3 origin, float elapsed)
        {
            float travel = Mathf.PingPong(elapsed / pointerHalfPeriod, 1f);
            travel = travel * travel * (3f - 2f * travel);
            pointer.position = origin + Vector3.up * (travel * pointerTravel);
        }

        private GameObject Create(string path, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing optional booster tutorial prefab: " + path);
            return Instantiate(prefab, parent);
        }

        private void ReleaseButton()
        {
            if (!buttonBound) return;
            buttonBound = false;
            if (slot != null && slot.Button != null) slot.ReleaseTutorialButton(game.Session.Number, Select);
            if (badge != null) Destroy(badge);
        }

        internal void Release()
        {
            Cleanup();
            if (effect != null) effect.Cancel();
            Destroy(gameObject);
        }

        private void Cleanup()
        {
            if (phase == Phase.Prompt)
            {
                ChangeGridLocks(false, false);
                RaiseSlot(false);
            }
            else Deselect();
            phase = Phase.Spent;
            if (dismissButton != null) dismissButton.onClick.RemoveListener(Dismiss);
            if (ConfirmationButton != null) ConfirmationButton.onClick.RemoveListener(Confirm);
            ReleaseButton();
        }

        private void OnDestroy() => Cleanup();
    }
}
