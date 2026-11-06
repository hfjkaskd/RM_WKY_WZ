using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    [Serializable]
    public sealed class OriginalDirectedTutorialProfile
    {
        public int level, lightballTaps;
        public float delay, stableBoardSeconds;
        public string prefabPath;
        public Vector2Int[] inputCells, raisedBoxes;
        public bool highlightWholeBoard;
        public bool showWhileBoardBusy;
    }

    public sealed class OriginalDirectedTutorialView : MonoBehaviour
    {
        private enum DialogAnchor { Cell, GridTop, WorldOrigin }

        [SerializeField] private Camera maskCamera;
        [SerializeField] private SpriteRenderer shade;
        [SerializeField] private Canvas dismissCanvas;
        [SerializeField] private Image dismissGraphic;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Transform maskCells, dialog, hand;
        [SerializeField] private TextMeshPro instruction;
        [SerializeField] private SpriteRenderer leftDialogBackground, rightDialogBackground;
        [SerializeField] private Vector2 landscapeDialogSize;
        [SerializeField] private OriginalBakedMeshAnimation handAnimation;
        [SerializeField] private Transform verticalMaskCells, verticalHand;
        [SerializeField] private OriginalBakedMeshAnimation verticalHandAnimation;
        [SerializeField] private Vector2Int handCell, shortDialogCell, tallDialogCell;
        [SerializeField] private Vector2Int landscapeDialogCell;
        [SerializeField] private DialogAnchor portraitAnchor, landscapeAnchor;
        [SerializeField] private bool handAtLandscapeGridTop;
        [SerializeField] private Vector2 landscapeHandOffset, portraitDialogOffset, landscapeDialogOffset;
        [SerializeField] private float tallDialogOffset, dialogAnchorOffset, dialogTopClearance, fadeSeconds;
        [SerializeField] private float minimumDismissSeconds;
        [SerializeField] private float focusedDialogThreshold, focusedDialogAbove, focusedDialogBelow;
        [SerializeField] private int highlightOffset, boardHighlightOffset;

        private static readonly int AlphaTexture = Shader.PropertyToID("_AlphaTex");
        private static readonly int AlphaScaleOffset = Shader.PropertyToID("_AlphaTex_ST");
        private static readonly int WorldToMask = Shader.PropertyToID("_WorldToMask");
        private readonly List<OriginalGridCell> locked = new List<OriginalGridCell>();
        private readonly List<SortingSnapshot> raised = new List<SortingSnapshot>();
        private OriginalPlayableBoard game;
        private OriginalGridCell focusCell, secondCell;
        private OriginalBoardItem firstTapLock, secondTapLock;
        private bool firstTapWasDisabled, secondTapWasDisabled;
        private Action complete;
        private OriginalInitialBoardView board;
        private OriginalGameplayHud hud;
        private Camera source;
        private RenderTexture target;
        private Material maskMaterial;
        private MaterialPropertyBlock maskProperties;
        private Rect pixelRect;
        private Vector3 viewCenter, boardOrigin, dialogTopLimit;
        private float height, width, targetAlpha, shownAt;
        private int dismissAtFrame = -1;
        private bool shown;

        public bool IsGuiding => shown;
        public Button DismissButton => dismissButton;
        public OriginalGridCell FocusCell => focusCell;
        public OriginalGridCell SecondCell => secondCell;
        private Color ShadeColor => dismissGraphic != null ? dismissGraphic.color : shade.color;

        public void Bind(OriginalDirectedTutorialProfile profile, OriginalPlayableBoard game,
            OriginalInitialBoardView board, OriginalGameplayHud hud, Camera source, OriginalGridCell focusCell = null,
            OriginalGridCell secondCell = null, Action complete = null)
        {
            this.focusCell = focusCell;
            this.secondCell = secondCell;
            this.complete = complete;
            this.game = game;
            this.board = board;
            this.hud = hud;
            this.source = source;
            // LevelTutorial20 sets the text width and the width of each panel half on entry.
            if (landscapeDialogSize.x > 0f && OriginalCameraProjection.IsLandscape)
            {
                var rect = instruction.rectTransform;
                rect.sizeDelta = new Vector2(landscapeDialogSize.x, rect.sizeDelta.y);
                leftDialogBackground.size = new Vector2(landscapeDialogSize.y, leftDialogBackground.size.y);
                rightDialogBackground.size = new Vector2(landscapeDialogSize.y, rightDialogBackground.size.y);
            }
            int openCells = focusCell == null ? profile.inputCells.Length : secondCell == null ? 1 : 2;
            locked.Capacity = board.Grid.Width * board.Grid.Height - openCells;
            raised.Capacity = profile == null ? 1 : profile.raisedBoxes.Length + (profile.highlightWholeBoard ? 2 : 1);
            if (secondCell != null)
            {
                bool vertical = focusCell.X == secondCell.X;
                maskCells.gameObject.SetActive(!vertical);
                verticalMaskCells.gameObject.SetActive(vertical);
                hand.gameObject.SetActive(!vertical);
                verticalHand.gameObject.SetActive(vertical);
                if (vertical)
                {
                    maskCells = verticalMaskCells;
                    hand = verticalHand;
                    handAnimation = verticalHandAnimation;
                }
                firstTapLock = focusCell.Mediation.CurrentItem;
                secondTapLock = secondCell.Mediation.CurrentItem;
                firstTapWasDisabled = firstTapLock.IsTapDisabled;
                secondTapWasDisabled = secondTapLock.IsTapDisabled;
                firstTapLock.IsTapDisabled = secondTapLock.IsTapDisabled = true;
            }
            if (dismissButton != null)
            {
                dismissCanvas.worldCamera = source;
                if (focusCell != null) ((OriginalTutorialDismissImage)dismissGraphic).Bind(this);
                dismissButton.interactable = false;
                dismissButton.onClick.AddListener(QueueDismiss);
                if (maskCamera != null)
                {
                    maskMaterial = new Material(dismissGraphic.material);
                    dismissGraphic.material = maskMaterial;
                }
            }
            else maskProperties = new MaterialPropertyBlock();
            targetAlpha = ShadeColor.a;
            SetShadeAlpha(0f);
            if (handAnimation != null) handAnimation.Initialize();
            RefreshLayout();
            Raise(hud.KingVisual.GetComponent<SortingGroup>(), highlightOffset);
            if (profile != null)
            {
                if (profile.highlightWholeBoard) Raise(board.BoardRoot.GetComponent<SortingGroup>(), boardHighlightOffset);
                foreach (var point in profile.raisedBoxes)
                {
                    var view = (OriginalBoxItemSpriteView)Cell(point).Mediation.CurrentItem.SwapView;
                    Raise(view.sortingGroup, highlightOffset);
                }
            }
            if (focusCell != null && secondCell == null) game.SetTutorialTapCell(focusCell);
            for (int y = 0; y < board.Grid.Height; y++)
                for (int x = 0; x < board.Grid.Width; x++)
                {
                    var point = new Vector2Int(x, y);
                    var cell = Cell(point);
                    if (cell == null || (focusCell != null ? cell == focusCell || cell == secondCell : Array.IndexOf(profile.inputCells, point) >= 0))
                        continue;
                    cell.DisableTouch();
                    locked.Add(cell);
                }
            game.OnSwap += OnSwap;
            game.OnTap += OnTap;
            shownAt = board.Clock.Time;
            shown = true;
        }

        private OriginalGridCell Cell(Vector2Int point) => board.Grid.GetCell(point.x, point.y);

        private void RefreshLayout()
        {
            pixelRect = source.pixelRect;
            viewCenter = source.transform.position;
            height = source.orthographicSize * 2f;
            width = height * source.aspect;
            boardOrigin = board.Grid.GetCell(0, 0).GetViewPosition();
            transform.position = new Vector3(viewCenter.x, viewCenter.y, 0);
            var size = new Vector2(width, height);
            if (dismissGraphic != null) dismissGraphic.rectTransform.sizeDelta = size;
            else shade.size = size;
            if (maskCamera != null) RenderMask();

            bool landscape = OriginalCameraProjection.IsLandscape;
            if (hand != null)
                hand.position = focusCell != null ? focusCell.GetViewPosition()
                    : landscape && handAtLandscapeGridTop ? GridTopCenter() + (Vector3)landscapeHandOffset
                    : Cell(handCell).GetViewPosition();

            if (dialog != null) RefreshDialogLayout(landscape);
        }

        private void RefreshDialogLayout(bool landscape)
        {
            dialogTopLimit = hud.TutorialTopLimit;
            bool tall = Mathf.Min(source.aspect, 1f / source.aspect) < .5f;
            var point = landscape ? landscapeDialogCell : tall ? tallDialogCell : shortDialogCell;
            var anchorMode = landscape ? landscapeAnchor : portraitAnchor;
            var anchor = focusCell != null ? focusCell.GetViewPosition()
                : anchorMode == DialogAnchor.GridTop ? GridTopCenter()
                : anchorMode == DialogAnchor.WorldOrigin ? Vector3.zero : Cell(point).GetViewPosition();
            if (focusCell != null)
            {
                // DynamicComboTutorial 0x224499c and DynamicSpecialItemTapTutorial 0x2248810.
                bool below = anchor.y > focusedDialogThreshold;
                if (below && secondCell != null) anchor = secondCell.GetViewPosition();
                anchor = new Vector3(0f, anchor.y + (below ? focusedDialogBelow : focusedDialogAbove), 0f);
            }
            anchor += (Vector3)(landscape ? landscapeDialogOffset : portraitDialogOffset);
            anchor.y += dialogAnchorOffset;
            if (!landscape && tall) anchor.y += tallDialogOffset;
            if (!landscape && dialogTopLimit.y - anchor.y < dialogTopClearance)
            {
                anchor = dialogTopLimit;
                anchor.y -= dialogTopClearance;
            }
            dialog.position = anchor;
        }

        private Vector3 GridTopCenter()
        {
            int top = board.Grid.Height - 1;
            var center = (board.Grid.GetCell(0, top).GetViewPosition() +
                board.Grid.GetCell(board.Grid.Width - 1, top).GetViewPosition()) * .5f;
            center.y += .5f;
            return center;
        }

        private void RenderMask()
        {
            var origin = focusCell != null ? focusCell.GetViewPosition() : boardOrigin;
            maskCells.position = new Vector3(origin.x, origin.y, maskCells.position.z);
            // TutorialBlackView.SetRenderTexture (0x21945d0) uses the screen height
            // and CameraManager.GetRenderTextureWidth (0x2002e98) truncates the crop.
            int columns = (int)(Screen.width * source.rect.width);
            int rows = Screen.height;
            if (target == null || target.width != columns || target.height != rows)
            {
                ReleaseTarget();
                target = new RenderTexture(columns, rows, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                maskCamera.targetTexture = target;
                maskCamera.aspect = (float)columns / rows;
            }
            maskCamera.orthographicSize = source.orthographicSize;
            maskCamera.Render();
            var mapping = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(width, height, 1f)).inverse;
            var scaleOffset = new Vector4(1f, 1f, 0f, 0f);
            if (maskMaterial != null)
            {
                maskMaterial.SetTexture(AlphaTexture, target);
                maskMaterial.SetVector(AlphaScaleOffset, scaleOffset);
                maskMaterial.SetMatrix(WorldToMask, mapping);
                return;
            }
            maskProperties.SetTexture(AlphaTexture, target);
            maskProperties.SetVector(AlphaScaleOffset, scaleOffset);
            maskProperties.SetMatrix(WorldToMask, mapping);
            shade.SetPropertyBlock(maskProperties);
        }

        private void LateUpdate()
        {
            if (!shown) return;
            float now = board.Clock.Time;
            if (dismissAtFrame >= 0 && Time.frameCount >= dismissAtFrame)
            {
                End();
                return;
            }
            if (pixelRect != source.pixelRect || viewCenter != source.transform.position ||
                height != source.orthographicSize * 2f || width != source.orthographicSize * 2f * source.aspect ||
                boardOrigin != board.Grid.GetCell(0, 0).GetViewPosition())
                RefreshLayout();
            else if (dialog != null && dialogTopLimit != hud.TutorialTopLimit)
                RefreshDialogLayout(OriginalCameraProjection.IsLandscape);
            float elapsed = now - shownAt;
            float fade = Mathf.Clamp01(elapsed / fadeSeconds);
            SetShadeAlpha(targetAlpha * fade * (2f - fade));
            if (handAnimation != null) handAnimation.SampleAnimator();
            if (dismissButton != null && dismissAtFrame < 0 && !dismissButton.interactable && elapsed >= minimumDismissSeconds)
                dismissButton.interactable = true;
        }

        private void Raise(SortingGroup group, int offset)
        {
            raised.Add(new SortingSnapshot(group));
            group.sortingLayerID = dismissCanvas != null ? dismissCanvas.sortingLayerID : shade.sortingLayerID;
            group.sortingOrder = (dismissCanvas != null ? dismissCanvas.sortingOrder : shade.sortingOrder) + offset;
            group.sortAtRoot = true;
            group.enabled = true;
        }

        private void OnSwap(OriginalGridCell from, OriginalGridCell to, bool valid)
        {
            if (valid && (secondCell == null ||
                (from == focusCell && to == secondCell) || (from == secondCell && to == focusCell))) End();
        }

        private void OnTap(OriginalGridCell cell)
        {
            if (secondCell == null && (focusCell == null || cell == focusCell)) End();
        }

        internal bool CanDismissAt(Vector2 screenPoint)
        {
            // The optional backdrop Button leaves its highlighted board cell
            // to the existing world-space game input.
            if (focusCell == null || game == null) return true;
            var cell = game.GetCellFromWorldPosition(source.ScreenToWorldPoint(screenPoint));
            return cell != focusCell && (secondCell == null || cell != secondCell);
        }

        private void QueueDismiss()
        {
            if (!IsGuiding || dismissAtFrame >= 0 || board.Clock.Time - shownAt < minimumDismissSeconds) return;
            int pressFrame = game.TouchListener?.LastPressFrame ?? -1;
            // The native optional step allows the board to consume the same press first.
            dismissAtFrame = Mathf.Max(Time.frameCount + 1, pressFrame + 4);
            dismissButton.interactable = false;
        }

        private void End()
        {
            if (!IsGuiding) return;
            // HideTutorial 0x276c054 clears the highlight, dialog and hand in the same callback.
            var completed = complete;
            complete = null;
            Release();
            completed?.Invoke();
        }

        private void SetShadeAlpha(float alpha)
        {
            var color = ShadeColor;
            color.a = alpha;
            if (dismissGraphic != null) dismissGraphic.color = color;
            else shade.color = color;
        }

        private void DisableDismiss()
        {
            if (dismissButton == null) return;
            dismissButton.interactable = false;
            dismissGraphic.raycastTarget = false;
            dismissButton.onClick.RemoveListener(QueueDismiss);
        }

        private void RestoreBoard()
        {
            if (game != null)
            {
                game.OnSwap -= OnSwap;
                game.OnTap -= OnTap;
                if (focusCell != null && secondCell == null) game.SetTutorialTapCell(null);
                game = null;
            }
            if (firstTapLock != null) firstTapLock.IsTapDisabled = firstTapWasDisabled;
            if (secondTapLock != null) secondTapLock.IsTapDisabled = secondTapWasDisabled;
            firstTapLock = secondTapLock = null;
            foreach (var cell in locked) cell.EnableTouch();
            locked.Clear();
            foreach (var snapshot in raised) snapshot.Restore();
            raised.Clear();
        }

        private void ReleaseTarget()
        {
            if (target == null) return;
            if (maskCamera != null) maskCamera.targetTexture = null;
            target.Release();
            Destroy(target);
            target = null;
        }

        public void Release()
        {
            RestoreBoard();
            shown = false;
            DisableDismiss();
            gameObject.SetActive(false);
            ReleaseTarget();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            RestoreBoard();
            ReleaseTarget();
            if (maskMaterial != null) Destroy(maskMaterial);
        }

        private readonly struct SortingSnapshot
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
