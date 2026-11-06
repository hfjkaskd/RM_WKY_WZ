using System;
using TMPro;
using UnityEngine;

namespace Whitebox.Foundation
{
    // InGameBoosterTutorial and LevelTutorial8. The free hammer is scoped to
    // this authored tutorial attempt and its single permitted target.
    public sealed class OriginalHammerTutorial : MonoBehaviour
    {
        [SerializeField] private Vector2Int targetPoint, strikeDialogPoint;
        [SerializeField] private Vector2 arrowOffset, handOffset;
        [SerializeField] private float selectDialogOffset, dialogTopClearance, fadeSeconds;
        [SerializeField] private float arrowTravel, arrowHalfPeriod;
        [SerializeField] private int highlightOrder;
        [SerializeField] private SpriteRenderer shade;
        [SerializeField] private Transform dialog, hand;
        [SerializeField] private TextMeshPro instruction;
        [SerializeField] private OriginalBakedMeshAnimation handAnimation;
        [SerializeField] private string selectText, strikeText;
        [SerializeField] private string arrowPrefabPath, hammerPrefabPath, freeBadgePrefabPath, selectParticlesPrefabPath;

        private OriginalPlayableBoard game;
        private OriginalInitialBoardView board;
        private OriginalBottomUi bottom;
        private OriginalInGameBoosterSlot slot;
        private OriginalGridCell target;
        private OriginalBoxItemSpriteView boxView;
        private Camera source;
        private Transform arrow;
        private GameObject freeBadge;
        private OriginalHammerBoosterView strike;
        private int canvasOrder, boxLayer, boxOrder;
        private bool boxSortAtRoot, boxGroupEnabled, buttonBound, lockApplied;
        private float shownAt, shadeAlpha;
        private Rect viewport;
        private float aspect, orthographicSize;
        private Vector3 cameraPosition, boardPosition, bottomPosition;
        public bool IsGuiding { get; private set; }
        public bool IsSelected { get; private set; }

        public void Bind(OriginalPlayableBoard game, OriginalInitialBoardView board, OriginalBottomUi bottom, Camera camera)
        {
            this.game = game;
            this.board = board;
            this.bottom = bottom;
            source = camera;
            foreach (var candidate in bottom.Boosters)
                if (candidate.BoosterType == OriginalBoosterType.Hammer) { slot = candidate; break; }
            target = board.Grid.GetCell(targetPoint.x, targetPoint.y);
            boxView = target?.Mediation.CurrentItem?.SwapView as OriginalBoxItemSpriteView;
            if (slot == null || boxView == null) throw new InvalidOperationException("Original hammer tutorial target or button is missing.");

            arrow = Create(arrowPrefabPath, transform).transform;
            freeBadge = Create(freeBadgePrefabPath, slot.transform);
            shadeAlpha = shade.color.a;
            var color = shade.color;
            color.a = 0f;
            shade.color = color;
            shownAt = board.Clock.Time;
            instruction.text = selectText;
            slot.ButtonCanvas.worldCamera = source;
            canvasOrder = slot.ButtonCanvas.sortingOrder;
            slot.ButtonCanvas.sortingOrder = highlightOrder;
            slot.TutorialSorting.enabled = true;
            slot.BindTutorialButton(game.Session.Number, Select);
            buttonBound = true;
            ChangeGridLocks(true, false);
            lockApplied = true;
            IsGuiding = true;
            RefreshLayout();
        }

        private void Select(OriginalBoosterType type, bool locked)
        {
            if (!IsGuiding || IsSelected || locked || !game.CanUserMakeMove()) return;
            IsSelected = true;
            RestoreButton();
            target.EnableTouch();
            var sorting = boxView.sortingGroup;
            boxLayer = sorting.sortingLayerID;
            boxOrder = sorting.sortingOrder;
            boxSortAtRoot = sorting.sortAtRoot;
            boxGroupEnabled = sorting.enabled;
            sorting.sortAtRoot = true;
            sorting.sortingLayerID = shade.sortingLayerID;
            sorting.sortingOrder = highlightOrder;
            sorting.enabled = true;
            arrow.gameObject.SetActive(false);
            Create(selectParticlesPrefabPath, transform).transform.position = slot.transform.position;
            hand.gameObject.SetActive(true);
            handAnimation.Initialize();
            instruction.text = strikeText;
            RefreshLayout();
        }

        internal void Use(OriginalGridCell cell)
        {
            if (!IsSelected || cell != target || !cell.IsTouchEnabled || !game.CanUserMakeMove()) return;
            strike = Create(hammerPrefabPath, board.transform).GetComponent<OriginalHammerBoosterView>();
            RestoreGuide();
            gameObject.SetActive(false);
            strike.Play(game, board, target, bottom.HammerLaunchFramePosition);
        }

        private GameObject Create(string path, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing hammer tutorial prefab: " + path);
            return Instantiate(prefab, parent);
        }

        private void ChangeGridLocks(bool disable, bool skipTarget)
        {
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell == null || (skipTarget && cell == target)) continue;
                if (disable) cell.DisableTouch();
                else cell.EnableTouch();
            }
        }

        private void RefreshLayout()
        {
            viewport = source.pixelRect;
            aspect = source.aspect;
            orthographicSize = source.orthographicSize;
            cameraPosition = source.transform.position;
            boardPosition = board.BoardRoot.position;
            bottomPosition = bottom.transform.position;
            float height = orthographicSize * 2f;
            var center = cameraPosition;
            shade.transform.position = new Vector3(center.x, center.y, 0f);
            shade.size = new Vector2(height * source.aspect, height);
            if (!IsSelected)
            {
                dialog.position = bottomPosition + Vector3.up * selectDialogOffset;
                return;
            }
            hand.position = target.GetViewPosition() + (Vector3)handOffset;
            var anchor = board.Grid.GetCell(strikeDialogPoint.x, strikeDialogPoint.y).GetViewPosition();
            anchor.y = Mathf.Min(anchor.y, center.y + height * .5f - dialogTopClearance);
            dialog.position = anchor;
        }

        private void LateUpdate()
        {
            if (!IsGuiding) return;
            if (viewport != source.pixelRect || aspect != source.aspect || orthographicSize != source.orthographicSize ||
                cameraPosition != source.transform.position || boardPosition != board.BoardRoot.position || bottomPosition != bottom.transform.position)
                RefreshLayout();
            var color = shade.color;
            float t = Mathf.Clamp01((board.Clock.Time - shownAt) / fadeSeconds);
            color.a = shadeAlpha * t * (2f - t);
            shade.color = color;
            if (IsSelected) handAnimation.SampleAnimator();
            else
            {
                float travel = Mathf.PingPong((board.Clock.Time - shownAt) / arrowHalfPeriod, 1f);
                travel = travel * travel * (3f - 2f * travel);
                arrow.position = slot.transform.position + (Vector3)arrowOffset + Vector3.up * (travel * arrowTravel);
            }
        }

        private void RestoreButton()
        {
            if (!buttonBound) return;
            buttonBound = false;
            if (slot == null) return;
            slot.ReleaseTutorialButton(game.Session.Number, Select);
            slot.ButtonCanvas.sortingOrder = canvasOrder;
            slot.TutorialSorting.enabled = false;
        }

        private void RestoreGuide()
        {
            RestoreButton();
            if (freeBadge != null) Destroy(freeBadge);
            if (lockApplied) ChangeGridLocks(false, IsSelected);
            lockApplied = false;
            if (IsSelected && boxView != null)
            {
                var sorting = boxView.sortingGroup;
                sorting.sortingLayerID = boxLayer;
                sorting.sortingOrder = boxOrder;
                sorting.sortAtRoot = boxSortAtRoot;
                sorting.enabled = boxGroupEnabled;
            }
            IsGuiding = IsSelected = false;
        }

        internal void Release()
        {
            RestoreGuide();
            if (strike != null) strike.Cancel();
            Destroy(gameObject);
        }

        private void OnDestroy() => RestoreGuide();
    }
}
