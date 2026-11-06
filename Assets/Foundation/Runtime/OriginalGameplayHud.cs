using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    // Recovered Target/Moves subtrees of GameScene.Top. Their original static
    // renderers, fonts and coordinates live in the prefab; King/background and
    // full camera/session presentation are restored alongside their owners.
    public sealed class OriginalGameplayHud : MonoBehaviour
    {
        public Transform target, moves;
        public TextMeshPro moveText;
        [SerializeField] private TextMeshPro targetTitle;
        [SerializeField] private SpriteRenderer portraitBackground;
        [SerializeField] private OriginalKingVisual kingVisual;
        [SerializeField] private SortingGroup topSorting;
        public OriginalKingVisual KingVisual => kingVisual;
        public Vector3 TutorialTopLimit => portraitBackground.transform.position;
        [SerializeField] private string rewardTitle;
        [SerializeField] private Vector3 rewardTitleOffset;
        [SerializeField] private Vector3 bonusTitleOffset;
        [SerializeField] private float rewardAppearDuration, rewardAppearOvershoot;
        public OriginalGoalPanelUi goalPanel;
        public Vector3 targetPortraitPosition, targetLandscapePosition, movesPortraitPosition, movesLandscapePosition;
        public float portraitScale, landscapeScale, safeTopOffset;
        private OriginalMoveManager moveManager;
        private Camera cameraSource;
        private OriginalGameSceneProjection sceneProjection;
        private int lastWidth, lastHeight;
        private bool orientationEnabled = true;
        private Renderer[] sortingRenderers;
        private int[] sortingLayers, sortingOrders;
        private int defaultTopLayer, defaultTopOrder;
        private bool titleAppearing;
        private float titleElapsed;
        private Vector3 titleScale;
        private Vector3 initialTitlePosition, initialTitleScale;
        private string initialTitle;
        public TextMeshPro TargetTitle => targetTitle;
        private void Awake()
        {
            defaultTopLayer = topSorting.sortingLayerID;
            defaultTopOrder = topSorting.sortingOrder;
            initialTitle = targetTitle.text;
            initialTitlePosition = targetTitle.transform.localPosition;
            initialTitleScale = targetTitle.transform.localScale;
        }
        public void SetRemainingMovesDisplay(int value) { moveText.text = value.ToString(); }
        public void SetOrientationEnabled(bool value) { orientationEnabled = value; }
        public void ResetSorting()
        {
            SetBehindBoosterShade(false);
            for (int i = 0; i < sortingRenderers.Length; i++)
            {
                if (sortingRenderers[i] == null) continue;
                sortingRenderers[i].sortingLayerID = sortingLayers[i]; sortingRenderers[i].sortingOrder = sortingOrders[i];
            }
        }

        // TopUi.UpdateSorting 0x16308cc. Keep the HUD's internal draw order.
        internal void SetWinLogoSorting(int layer, int order)
        {
            topSorting.sortingLayerID = layer;
            topSorting.sortingOrder = order;
            topSorting.enabled = true;
        }

        // InGameBoosterManager 0x13c3ebc.
        internal void SetBehindBoosterShade(bool active)
        {
            topSorting.sortingLayerID = defaultTopLayer;
            topSorting.sortingOrder = defaultTopOrder;
            topSorting.enabled = active;
        }
        public void Initialize(OriginalPlayableBoard game, OriginalGoalManager goals, OriginalGameAudioHost audio, Camera camera,
            OriginalGameSceneProjection projection = null)
        {
            cameraSource = camera ?? throw new ArgumentNullException(nameof(camera));
            sceneProjection = projection;
            if (kingVisual != null)
            {
                kingVisual.Initialize(audio.Random, game.Moves, game.GameCoroutines, !game.Session.IsBonus);
                game.BindKingVisual(kingVisual);
            }
            moveManager = game.Moves; moveManager.OnMoveChanged += UpdateMoves;
            UpdateMoves(moveManager.LeftMoves, 1);
            goalPanel.Initialize(goals, audio.Playback, !game.Session.IsBonus, AnimateTargetTextAsReward, UpdateRewardTitle);
            game.BindGoalPanel(goalPanel, SetOrientationEnabled);
            goalPanel.OnViewsReplaced += CacheSorting;
            CacheSorting(); Arrange();
            if (game.Session.IsBonus) // TopUi.PrepareForBonusLevel 0x162f784, original English portrait.
            {
                targetTitle.text = rewardTitle;
                targetTitle.transform.position += bonusTitleOffset;
            }
        }
        private void CacheSorting()
        {
            sortingRenderers = GetComponentsInChildren<Renderer>(true);
            sortingLayers = new int[sortingRenderers.Length]; sortingOrders = new int[sortingRenderers.Length];
            for (int i = 0; i < sortingRenderers.Length; i++)
            { sortingLayers[i] = sortingRenderers[i].sortingLayerID; sortingOrders[i] = sortingRenderers[i].sortingOrder; }
        }
        private void AnimateTargetTextAsReward() // TopUi 0x1630680, US/English branch of 0x16304a8.
        {
            titleScale = targetTitle.transform.localScale;
            targetTitle.text = rewardTitle;
            targetTitle.transform.position += rewardTitleOffset;
            targetTitle.transform.localScale = Vector3.zero;
            titleElapsed = 0f; titleAppearing = true;
        }
        private void UpdateMoves(int left, int type) { moveText.text = left.ToString(); } // 0x162de24.
        public void RefreshLayout() { if (orientationEnabled) Arrange(); }
        private void Arrange()
        {
            lastWidth = Screen.width; lastHeight = Screen.height;
            bool landscape = OriginalCameraProjection.IsLandscape;
            Vector3 position;
            if (landscape) position = cameraSource.ViewportToWorldPoint(new Vector3(0f, .5f, 0f));
            else
            {
                if (sceneProjection != null) position = sceneProjection.SafeTopCenter;
                else
                {
                    var safe = Screen.safeArea;
                    position = OriginalGameSceneProjection.GetSafeCenter(cameraSource, safe);
                    position.y += safe.height * cameraSource.orthographicSize / lastHeight;
                }
                position.y += safeTopOffset;
            }
            position.z = 0f; transform.position = position;
            target.localPosition = landscape ? targetLandscapePosition : targetPortraitPosition;
            moves.localPosition = landscape ? movesLandscapePosition : movesPortraitPosition;
            float scale = landscape ? landscapeScale : portraitScale;
            target.localScale = moves.localScale = new Vector3(scale, scale, 0f);
            if (portraitBackground != null)
            {
                portraitBackground.gameObject.SetActive(!landscape);
                // TopUi.SetBackground 0x162f0e8: live camera width and height5.5.
                if (!landscape) portraitBackground.size = new Vector2(cameraSource.orthographicSize * 2f * cameraSource.aspect, 5.5f);
            }
        }
        private void Update()
        {
            if (orientationEnabled && cameraSource != null && (lastWidth != Screen.width || lastHeight != Screen.height)) Arrange();
        }
        private void UpdateRewardTitle(float delta)
        {
            if (!titleAppearing) return;
            titleElapsed += delta;
            float t = Mathf.Min(titleElapsed / rewardAppearDuration, 1f);
            targetTitle.transform.localScale = titleScale * OriginalGoalPanelUi.RewardEase(t, rewardAppearOvershoot);
            if (t >= 1f) titleAppearing = false;
        }
        public void Release()
        { if (moveManager != null) moveManager.OnMoveChanged -= UpdateMoves; moveManager = null; kingVisual?.ReleaseLevel(); goalPanel.OnViewsReplaced -= CacheSorting; goalPanel.Release(); titleAppearing = false; }
        internal void ResetForRestart()
        {
            Release();
            ResetSorting();
            orientationEnabled = true;
            targetTitle.text = initialTitle;
            targetTitle.transform.localPosition = initialTitlePosition;
            targetTitle.transform.localScale = initialTitleScale;
        }
        private void OnDestroy() { Release(); }
    }
}
