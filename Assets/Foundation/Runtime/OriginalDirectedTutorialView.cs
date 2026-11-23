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
        [SerializeField] private Rect handAvoidanceRect, verticalHandAvoidanceRect;
        [SerializeField] private Vector2 focusedCellHalfSize, focusedScreenPadding;
        [SerializeField] private float focusedDialogGap;
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
        private Rect pixelRect, screenSafeArea;
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
            screenSafeArea = Screen.safeArea;
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
            if (focusCell != null)
                FitFocusedDialog(source, screenSafeArea, dialogTopLimit,
                    focusCell.GetViewPosition(), secondCell == null ? (Vector3?)null : secondCell.GetViewPosition());
        }

        // Use the authored envelope of the whole gesture, rather than moving
        // the dialog with each animation frame. Called only on layout changes.
        public void FitFocusedDialog(Camera camera, Rect safeArea, Vector3 topLimit,
            Vector3 focusPosition, Vector3? secondPosition)
        {
            bool vertical = secondPosition.HasValue && Mathf.Approximately(focusPosition.x, secondPosition.Value.x);
            var gesture = vertical ? verticalHand : hand;
            var avoid = WorldRect(gesture, vertical ? verticalHandAvoidanceRect : handAvoidanceRect);
            var cells = new Bounds(focusPosition, (Vector3)(focusedCellHalfSize * 2f));
            if (secondPosition.HasValue)
                cells.Encapsulate(new Bounds(secondPosition.Value, (Vector3)(focusedCellHalfSize * 2f)));
            avoid = Rect.MinMaxRect(Mathf.Min(avoid.xMin, cells.min.x), Mathf.Min(avoid.yMin, cells.min.y),
                Mathf.Max(avoid.xMax, cells.max.x), Mathf.Max(avoid.yMax, cells.max.y));
            avoid = Rect.MinMaxRect(avoid.xMin - focusedDialogGap, avoid.yMin - focusedDialogGap,
                avoid.xMax + focusedDialogGap, avoid.yMax + focusedDialogGap);

            var bounds = leftDialogBackground.bounds;
            bounds.Encapsulate(rightDialogBackground.bounds);
            var text = WorldRect(instruction.rectTransform, instruction.rectTransform.rect);
            bounds.Encapsulate(new Vector3(text.xMin, text.yMin, bounds.center.z));
            bounds.Encapsulate(new Vector3(text.xMax, text.yMax, bounds.center.z));
            var half = (Vector2)bounds.extents;
            var preferred = (Vector2)bounds.center;
            float depth = camera.WorldToScreenPoint(dialog.position).z;
            var viewport = camera.pixelRect;
            var bottomLeft = camera.ScreenToWorldPoint(new Vector3(
                Mathf.Max(viewport.xMin, safeArea.xMin), Mathf.Max(viewport.yMin, safeArea.yMin), depth));
            var topRight = camera.ScreenToWorldPoint(new Vector3(
                Mathf.Min(viewport.xMax, safeArea.xMax), Mathf.Min(viewport.yMax, safeArea.yMax), depth));
            var available = Rect.MinMaxRect(bottomLeft.x + focusedScreenPadding.x, bottomLeft.y + focusedScreenPadding.y,
                topRight.x - focusedScreenPadding.x, Mathf.Min(topRight.y, topLimit.y) - focusedScreenPadding.y);
            var limits = Rect.MinMaxRect(available.xMin + half.x, available.yMin + half.y,
                available.xMax - half.x, available.yMax - half.y);
            // A narrow viewport centers an oversized axis; normal supported
            // layouts retain the prefab's text and background size.
            if (limits.width < 0f) limits.xMin = limits.xMax = available.center.x;
            if (limits.height < 0f) limits.yMin = limits.yMax = available.center.y;
            Vector2 best = preferred;
            float bestOverlap = float.PositiveInfinity, bestDistance = float.PositiveInfinity;
            ConsiderDialogPosition(preferred, preferred, half, limits, avoid, ref best, ref bestOverlap, ref bestDistance);
            ConsiderDialogPosition(new Vector2(preferred.x, avoid.yMax + half.y), preferred, half, limits, avoid,
                ref best, ref bestOverlap, ref bestDistance);
            ConsiderDialogPosition(new Vector2(preferred.x, avoid.yMin - half.y), preferred, half, limits, avoid,
                ref best, ref bestOverlap, ref bestDistance);
            ConsiderDialogPosition(new Vector2(avoid.xMin - half.x, preferred.y), preferred, half, limits, avoid,
                ref best, ref bestOverlap, ref bestDistance);
            ConsiderDialogPosition(new Vector2(avoid.xMax + half.x, preferred.y), preferred, half, limits, avoid,
                ref best, ref bestOverlap, ref bestDistance);
            dialog.position += (Vector3)(best - preferred);
        }

        private static Rect WorldRect(Transform owner, Rect local)
        {
            var bounds = new Bounds(owner.TransformPoint(new Vector3(local.xMin, local.yMin, 0f)), Vector3.zero);
            bounds.Encapsulate(owner.TransformPoint(new Vector3(local.xMax, local.yMin, 0f)));
            bounds.Encapsulate(owner.TransformPoint(new Vector3(local.xMin, local.yMax, 0f)));
            bounds.Encapsulate(owner.TransformPoint(new Vector3(local.xMax, local.yMax, 0f)));
            return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        }

        private static void ConsiderDialogPosition(Vector2 candidate, Vector2 preferred, Vector2 half, Rect limits, Rect avoid,
            ref Vector2 best, ref float bestOverlap, ref float bestDistance)
        {
            candidate.x = Mathf.Clamp(candidate.x, limits.xMin, limits.xMax);
            candidate.y = Mathf.Clamp(candidate.y, limits.yMin, limits.yMax);
            float overlap = Mathf.Max(0f, Mathf.Min(candidate.x + half.x, avoid.xMax) - Mathf.Max(candidate.x - half.x, avoid.xMin))
                * Mathf.Max(0f, Mathf.Min(candidate.y + half.y, avoid.yMax) - Mathf.Max(candidate.y - half.y, avoid.yMin));
            float distance = (candidate - preferred).sqrMagnitude;
            if (overlap > bestOverlap || (overlap == bestOverlap && distance >= bestDistance)) return;
            best = candidate;
            bestOverlap = overlap;
            bestDistance = distance;
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
            if (pixelRect != source.pixelRect || screenSafeArea != Screen.safeArea || viewCenter != source.transform.position ||
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
