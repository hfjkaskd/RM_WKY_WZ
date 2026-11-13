using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalGameSceneProjection : MonoBehaviour, IOriginalProjectionCamera
    {
        [SerializeField] private OriginalGameBackground background;
        [SerializeField] private string squareFlagCameraPath;
        [SerializeField] private string squareFlagPath;
        [SerializeField] private float squareFlagEdgeInset;
        [SerializeField] private float squareFlagDepth;
        [SerializeField] private bool fitPortraitBoardToViewport;
        [SerializeField] private Rect portraitBoardViewport;
        [SerializeField] private float portraitBoardEdgePadding;
        private Camera sceneCamera;
        private OriginalActiveLevelData session;
        private OriginalInitialBoardView board;
        private Rect fullViewport;
        private Camera squareFlagCamera;
        private Transform leftFlag, rightFlag;
        private SpriteRenderer[] squareFlagRenderers;
        private int width, height;
        private ScreenOrientation screenOrientation;
        private Rect safeArea, screenSafeArea;
        private float safeAreaOffset, safeAreaTopOffset;
        public OriginalCameraProjection Projection { get; } = new OriginalCameraProjection();
        public event Action OnProjectionChanged;
        internal event Action OnOrientationChanged;
        public bool IsOverSquareSideBand(Vector2 screenPosition) =>
            Projection.CroppedViewport && !sceneCamera.pixelRect.Contains(screenPosition);

        internal Vector3 SafeCenter => GetSafeCenter(sceneCamera, safeArea);
        internal Vector3 SafeTopCenter
        {
            get
            {
                var center = SafeCenter;
                center.y += safeAreaOffset + SafeHalfHeight + safeAreaTopOffset;
                return center;
            }
        }
        internal Vector3 SafeBottomCenter
        {
            get
            {
                var center = SafeCenter;
                center.y -= safeAreaOffset + SafeHalfHeight;
                return center;
            }
        }
        private float SafeHalfHeight => safeArea.height * sceneCamera.orthographicSize / height;

        internal static Vector3 GetSafeCenter(Camera camera, Rect area)
        {
            var center = camera.ScreenToWorldPoint(area.center);
            center.x = camera.transform.position.x;
            center.z = 0f;
            return center;
        }

        public void Initialize(Camera camera, OriginalActiveLevelData activeSession,
            OriginalInitialBoardView initialBoard, bool hasHoleInGrid)
        {
            BindCamera(camera);
            session = activeSession ?? throw new ArgumentNullException(nameof(activeSession));
            board = initialBoard != null ? initialBoard : throw new ArgumentNullException(nameof(initialBoard));
            background.Initialize(board.InitialItemAmounts, hasHoleInGrid);
            RefreshProjection();
        }

        internal void InitializeCompletion(Camera camera)
        {
            BindCamera(camera);
            background.InitializeArea();
            RefreshProjection();
        }

        private void BindCamera(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (background == null) throw new InvalidOperationException("The Foundation prefab is missing its game background.");
            sceneCamera = camera;
            fullViewport = camera.rect;
            screenOrientation = Screen.orientation;
        }

        internal void RebindSession(OriginalActiveLevelData activeSession)
        {
            session = activeSession ?? throw new ArgumentNullException(nameof(activeSession));
            RefreshProjection();
        }
        public void RefreshProjection() => RefreshProjection(Screen.orientation);

        public void RefreshProjection(ScreenOrientation orientation)
        {
            if (sceneCamera == null) throw new InvalidOperationException("Projection has no active camera.");
            width = Screen.width; height = Screen.height;
            screenSafeArea = Screen.safeArea;
            // Measure the display before applying the native square-device crop.
            sceneCamera.rect = fullViewport;
            sceneCamera.ResetAspect();
            Projection.UpdateSizeParameters(this, 2, session?.IsTableGame ?? false);
            var region = board != null && board.Grid != null && board.Layout != null
                ? board.Layout.CurrentBoard
                : null;
            float viewportBottom = Mathf.Clamp01(portraitBoardViewport.yMin);
            // Reserve the same cutout space above the board as the portrait HUD.
            float safeTopInset = 1f - Mathf.Clamp01(screenSafeArea.yMax / height);
            float viewportTop = Mathf.Clamp01(portraitBoardViewport.yMax - safeTopInset);
            float edgePadding = Mathf.Max(0f, portraitBoardEdgePadding);
            bool fitBoard = fitPortraitBoardToViewport && region != null &&
                            !OriginalCameraProjection.IsLandscape && viewportTop > viewportBottom;
            if (fitBoard)
            {
                // Height counts cells, so its half extent already includes the
                // half cell beyond each outer center. Padding covers the frame.
                float requiredHeight = (region.Height + edgePadding * 2f) / (viewportTop - viewportBottom);
                Projection.EnsureMinimumHeight(this, requiredHeight);
            }
            RefreshSafeArea(screenSafeArea);
            float portraitAspect = OriginalCameraProjection.GetPortraitAspect(this);
            if (board != null)
            {
                float offset = OriginalCameraProjection.GetPortraitBoardOffset(portraitAspect);
                if (fitBoard)
                {
                    Vector3 bottom = sceneCamera.ViewportToWorldPoint(new Vector3(.5f, viewportBottom, 0f));
                    Vector3 top = sceneCamera.ViewportToWorldPoint(new Vector3(.5f, viewportTop, 0f));
                    var parent = board.BoardRoot.parent;
                    if (parent != null)
                    {
                        bottom = parent.InverseTransformPoint(bottom);
                        top = parent.InverseTransformPoint(top);
                    }
                    float halfExtent = region.Height * .5f + edgePadding;
                    float minimumCenter = bottom.y + halfExtent;
                    float maximumCenter = top.y - halfExtent;
                    offset = minimumCenter <= maximumCenter
                        ? Mathf.Clamp(offset, minimumCenter, maximumCenter)
                        : (minimumCenter + maximumCenter) * .5f;
                }
                board.SetBoardPortraitOffset(offset);
            }
            background.FitViewport(Projection.Width, Projection.Height, portraitAspect);
            RefreshSquareFlags();
            OnProjectionChanged?.Invoke();
            if (IsNewPortraitOrientation(orientation))
            {
                screenOrientation = orientation;
                OnOrientationChanged?.Invoke();
            }
        }

        private void RefreshSafeArea(Rect area)
        {
            safeArea = CalculateReliableSafeArea(area, sceneCamera.pixelWidth, sceneCamera.pixelHeight, Projection.CroppedViewport);
            var platform = Application.platform;
            safeAreaOffset = platform == RuntimePlatform.IPhonePlayer && safeArea.height / height < 1f &&
                OriginalCameraProjection.GetPortraitAspect(this) < .57f ? GetIPhoneSafeAreaOffset() : 0f;
            // SetSafeAreaOffset (0x2004464) adds the excess beyond 165 px
            // back to the Android top anchor in 140 px per world unit.
            float topCutout = height - (safeArea.y + safeArea.height);
            if (platform == RuntimePlatform.Android && topCutout > 165f)
                safeAreaTopOffset = (topCutout - 165f) / 140f;
        }

        // CameraManager.CalculateReliableSafeArea (0x2005360).
        private static Rect CalculateReliableSafeArea(Rect area, int pixelWidth, int pixelHeight, bool croppedViewport)
        {
            float widthRatio = pixelWidth / (area.width + area.x);
            if (croppedViewport || !(widthRatio < .9f || widthRatio > 1.1f)) return area;
            float heightRatio = pixelHeight / (area.height + area.y);
            return new Rect((int)(area.x / widthRatio), (int)(area.y / heightRatio),
                (int)(area.width * widthRatio), (int)(area.height * heightRatio));
        }

        private float GetIPhoneSafeAreaOffset()
        {
            if (height == 1792) return .32f;
            return SystemInfo.deviceModel switch
            {
                "iPhone17,4" => .32f,
                "iPhone15,4" or "iPhone15,5" or "iPhone16,1" or "iPhone16,2" or "iPhone17,2" or "iPhone17,3" or
                    "iPhone18,2" or "iPhone19,3" or "iPhone19,7" => .34f,
                "iPhone15,2" or "iPhone15,3" or "iPhone17,1" or "iPhone18,1" or "iPhone18,3" or "iPhone19,2" => .36f,
                "iPhone18,4" => .28f,
                _ => .42f,
            };
        }

        private void RefreshSquareFlags()
        {
            if (!Projection.CroppedViewport)
            {
                if (squareFlagCamera != null) squareFlagCamera.enabled = false;
                return;
            }

            // CameraManager.CreateSquareDeviceFlags 0x2004a54.
            if (squareFlagCamera == null)
            {
                var cameraPrefab = Resources.Load<GameObject>(squareFlagCameraPath);
                var flagPrefab = Resources.Load<GameObject>(squareFlagPath);
                if (cameraPrefab == null || flagPrefab == null)
                    throw new InvalidOperationException("Original square-device flag prefabs are missing.");
                squareFlagCamera = Instantiate(cameraPrefab.GetComponent<Camera>(), sceneCamera.transform, true);
                leftFlag = Instantiate(flagPrefab, squareFlagCamera.transform).transform;
                rightFlag = Instantiate(flagPrefab, squareFlagCamera.transform).transform;
                rightFlag.localRotation = Quaternion.Euler(0f, 0f, 180f);
                squareFlagRenderers = squareFlagCamera.GetComponentsInChildren<SpriteRenderer>();
            }
            squareFlagCamera.orthographicSize = sceneCamera.orthographicSize;
            squareFlagCamera.clearFlags = sceneCamera.clearFlags;
            squareFlagCamera.backgroundColor = sceneCamera.backgroundColor;
            float edge = Projection.Width * .5f - squareFlagEdgeInset;
            leftFlag.localPosition = new Vector3(-edge, 0f, squareFlagDepth);
            rightFlag.localPosition = new Vector3(edge, 0f, squareFlagDepth);
            // SquareDeviceFlagView.SetView 0x2d926c0 keeps the authored widths.
            foreach (var renderer in squareFlagRenderers)
                renderer.size = new Vector2(renderer.size.x, Projection.Height);
            squareFlagCamera.enabled = true;
        }

        private void Update()
        {
            if (sceneCamera == null) return;
            var orientation = Screen.orientation;
            if (width != Screen.width || height != Screen.height || screenSafeArea != Screen.safeArea ||
                IsNewPortraitOrientation(orientation))
                RefreshProjection(orientation);
        }

        // The original build accepts Portrait and PortraitUpsideDown (0x2d79608).
        private bool IsNewPortraitOrientation(ScreenOrientation orientation) =>
            orientation != screenOrientation &&
            orientation is ScreenOrientation.Portrait or ScreenOrientation.PortraitUpsideDown;

        float IOriginalProjectionCamera.Aspect => sceneCamera.aspect;
        int IOriginalProjectionCamera.PixelWidth => sceneCamera.pixelWidth;
        int IOriginalProjectionCamera.PixelHeight => sceneCamera.pixelHeight;
        void IOriginalProjectionCamera.SetViewport(float x, float y, float w, float h) => sceneCamera.rect = new Rect(x, y, w, h);
        void IOriginalProjectionCamera.SetOrthographicSize(float value) => sceneCamera.orthographicSize = value;
    }
}
