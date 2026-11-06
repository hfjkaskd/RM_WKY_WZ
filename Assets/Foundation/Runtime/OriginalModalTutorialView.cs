using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    [Serializable]
    public sealed class OriginalModalTutorialProfile
    {
        public int level;
        [TextArea] public string instruction;
        public bool highlightWholeBoard;
        public Vector2Int[] obstacleCells;
        public Vector2Int portraitCell = new Vector2Int(-1, -1);
        public Vector2 portraitOffset, landscapeOffset;
        public bool landscapeAtWorldOrigin;
        public float textWidth, backgroundHalfWidth;
    }

    internal interface IOriginalTutorialHighlightView
    {
        SortingGroup TutorialSortingGroup { get; }
    }

    // The recovered full-board and obstacle-only tutorial steps need no cell masks.
    public sealed class OriginalModalTutorialView : MonoBehaviour
    {
        [SerializeField] private Canvas dimCanvas;
        [SerializeField] private Image dim;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Transform dialog;
        [SerializeField] private TextMeshPro instruction;
        [SerializeField] private SpriteRenderer leftBackground, rightBackground;
        [SerializeField] private float targetAlpha, fadeSeconds, minimumShownSeconds;
        [SerializeField] private float centerAnchorOffset, topClearance;
        [SerializeField] private int boardHighlightOffset, obstacleHighlightOffset;

        private readonly List<SortingSnapshot> raised = new List<SortingSnapshot>();
        private OriginalModalTutorialProfile profile;
        private OriginalInitialBoardView board;
        private OriginalGameplayHud hud;
        private Camera cameraSource;
        private float shownAt, dismissedAt, dismissalAlpha;
        private int screenWidth, screenHeight;
        private int lastPointerPressFrame = -1;
        private bool dismissalQueued, dismissed;

        public bool BlocksBoardInput => board != null && !dismissed;
        public Button DismissButton => dismissButton;

        public void Bind(OriginalModalTutorialProfile entry, OriginalInitialBoardView board, OriginalGameplayHud hud, Camera camera)
        {
            profile = entry;
            this.board = board;
            this.hud = hud;
            cameraSource = camera;
            dimCanvas.worldCamera = camera;
            instruction.text = profile.instruction;
            if (profile.textWidth > 0f)
                instruction.rectTransform.sizeDelta = new Vector2(profile.textWidth, instruction.rectTransform.sizeDelta.y);
            if (profile.backgroundHalfWidth > 0f)
            {
                leftBackground.size = new Vector2(profile.backgroundHalfWidth, leftBackground.size.y);
                rightBackground.size = new Vector2(profile.backgroundHalfWidth, rightBackground.size.y);
            }
            shownAt = board.Clock.Time;
            dismissButton.interactable = false;
            SetDimAlpha(0f);
            RefreshLayout();
            raised.Capacity = profile.highlightWholeBoard ? 2 : profile.obstacleCells.Length + 1;
            Raise(hud.KingVisual.GetComponent<SortingGroup>(), obstacleHighlightOffset);
            if (profile.highlightWholeBoard)
                Raise(board.BoardRoot.GetComponent<SortingGroup>(), boardHighlightOffset);
            else
            {
                foreach (var cell in profile.obstacleCells)
                {
                    var item = board.Grid.GetCell(cell.x, cell.y).Mediation.CurrentItem;
                    if (item is OriginalBushFakeBoardItem proxy) item = proxy.Master;
                    if (item?.SwapView is IOriginalTutorialHighlightView view)
                        Raise(view.TutorialSortingGroup, obstacleHighlightOffset);
                }
            }
            dismissButton.onClick.AddListener(QueueDismissal);
        }

        private void Raise(SortingGroup group, int offset)
        {
            raised.Add(new SortingSnapshot(group));
            group.sortingLayerID = dimCanvas.sortingLayerID;
            group.sortingOrder = dimCanvas.sortingOrder + offset;
            group.sortAtRoot = true;
            group.enabled = true;
        }

        private void RefreshLayout()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            Vector3 center = cameraSource.transform.position;
            center.z = 0;
            dimCanvas.transform.position = center;
            float height = 2f * cameraSource.orthographicSize;
            dim.rectTransform.sizeDelta = new Vector2(height * cameraSource.aspect, height);

            bool landscape = OriginalCameraProjection.IsLandscape;
            var offset = landscape ? profile.landscapeOffset : profile.portraitOffset;
            Vector3 anchor;
            if (landscape && profile.landscapeAtWorldOrigin)
                anchor = Vector3.zero;
            else if (!landscape && profile.portraitCell.x >= 0)
                anchor = board.Grid.GetCell(profile.portraitCell.x, profile.portraitCell.y).GetViewPosition();
            else
            {
                anchor = (board.Grid.GetCell(0, 0).GetViewPosition() +
                    board.Grid.GetCell(board.Grid.Width - 1, board.Grid.Height - 1).GetViewPosition()) * .5f;
                anchor.y += board.Grid.Height * .5f;
            }
            anchor.x += offset.x;
            anchor.y += offset.y + centerAnchorOffset;
            if (!landscape && hud.TutorialTopLimit.y - anchor.y < topClearance)
            {
                anchor = hud.TutorialTopLimit;
                anchor.y -= topClearance;
            }
            dialog.position = anchor;
        }

        private void Update()
        {
            if (board == null) return;
            if (screenWidth != Screen.width || screenHeight != Screen.height) RefreshLayout();
            float now = board.Clock.Time;
            if (dismissed)
            {
                float t = Mathf.Clamp01((now - dismissedAt) / fadeSeconds);
                SetDimAlpha(dismissalAlpha * (1f - EaseOut(t)));
                if (t >= 1f) Destroy(gameObject);
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                    lastPointerPressFrame = Time.frameCount;
                float t = Mathf.Clamp01((now - shownAt) / fadeSeconds);
                SetDimAlpha(targetAlpha * EaseOut(t));
                if (!dismissalQueued && !dismissButton.interactable && now - shownAt >= minimumShownSeconds)
                    dismissButton.interactable = true;
            }
        }

        private static float EaseOut(float t) => t * (2f - t);

        private void QueueDismissal()
        {
            if (!BlocksBoardInput || dismissalQueued || board.Clock.Time - shownAt < minimumShownSeconds) return;
            dismissalQueued = true;
            dismissButton.interactable = false;
            // TutorialDialogView.MouseUp (0x2769d44) waits through the fourth frame
            // after the press and always keeps the board gated on the release frame.
            int heldFrames = lastPointerPressFrame < 0 ? 0 : Time.frameCount - lastPointerPressFrame;
            StartCoroutine(DismissAfterFrames(Mathf.Max(1, 4 - heldFrames)));
        }

        private IEnumerator DismissAfterFrames(int frames)
        {
            for (int frame = 0; frame < frames; frame++)
                yield return null;
            dismissed = true;
            dismissedAt = board.Clock.Time;
            dismissalAlpha = dim.color.a;
            dim.raycastTarget = false;
            dialog.gameObject.SetActive(false);
            RestoreSorting();
        }

        private void SetDimAlpha(float alpha)
        {
            var color = dim.color;
            if (color.a == alpha) return;
            color.a = alpha;
            dim.color = color;
        }

        private void RestoreSorting()
        {
            foreach (var snapshot in raised) snapshot.Restore();
            raised.Clear();
        }

        private void OnDestroy()
        {
            dismissButton.onClick.RemoveListener(QueueDismissal);
            RestoreSorting();
        }

        internal readonly struct SortingSnapshot
        {
            private readonly SortingGroup group;
            private readonly int layer, order;
            private readonly bool enabled, sortAtRoot;

            public SortingSnapshot(SortingGroup group)
            {
                this.group = group;
                layer = group.sortingLayerID;
                order = group.sortingOrder;
                enabled = group.enabled;
                sortAtRoot = group.sortAtRoot;
            }

            public void Restore()
            {
                if (group == null) return;
                group.sortingLayerID = layer;
                group.sortingOrder = order;
                group.sortAtRoot = sortAtRoot;
                group.enabled = enabled;
            }
        }
    }
}

