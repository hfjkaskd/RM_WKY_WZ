using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelWinDialogContext
    {
        Camera UiCamera { get; }
        Vector3 UiScale { get; }
        float GameplayDeltaTime { get; }
        Vector3 GetCameraCenter();
        void UpdateDialogParent(Transform view, OriginalDialogConfig config);
        void SetDialogTouchSorting(int order);
        OriginalActiveLevelData GetCurrentSession();
        string GetLevelTitle(OriginalActiveLevelData session);
        bool ShouldShowRewardedAd { get; }
        bool ShouldShowCloche { get; }
        bool ShouldShowWeeklyPanel { get; }
        void StopAllSounds();
        void MarkLevelWinDialogShown();
        void PrepareBottomPanel(bool cloche, bool weekly);
        void ShowBottomPanel();
        void ShowLeagueCrown();
        void DisableOrientation(int reason);
        void EnableOrientation(int reason);
        void PlaySound(int id, float offset);
        void PlaySoundInLoop(int id);
        void StopSoundInLoop(int id);
        void EnableBottomPanelOrientation();
        IOriginalLevelWinAds GetAds();
        IOriginalLevelWinGameManager GetGameManager();
        IOriginalLevelWinLavaFlow GetLavaFlow();
        IOriginalLevelWinAncientFlow GetAncientFlow();
    }
    // The actual recovered result prefab owns its sprite/text hierarchy and
    // standard buttons. Activity/camera/localization/SDK owners are required;
    // theme bundles and full orientation layout remain separate.
    public sealed class OriginalLevelWinDialog : MonoBehaviour, IOriginalFlowDialog, IOriginalUiDialogHost, IOriginalLevelWinCloseContext
    {
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private Button[] continueButtons;
        [SerializeField] private TMP_Text[] continueLabels;
        [SerializeField] private Button rewardedButton, closeButton;
        [SerializeField] private Canvas[] buttonCanvases;
        [SerializeField] private OriginalCurvedDialogTitle title;
        [SerializeField] private TextMeshPro defaultCoinText, rewardedCoinText;
        [SerializeField] private GameObject bottomWithAd, bottomWithoutAd, centerWithAd, centerWithoutAd, bottomPanel;
        [SerializeField] private Transform star;
        [SerializeField] private Animator pillowAnimator;
        [SerializeField] private string pillowState;
        [SerializeField] private Vector3 starStartPosition, starStartScale;
        [SerializeField] private float starStartAngle, starMoveDuration, pillowCue, textCue, starSoundOffset;
        [SerializeField] private OriginalGoalCheckStage[] starBounce;
        [SerializeField] private OriginalWinTextAnimation wellDoneText;
        private IOriginalLevelWinDialogContext context;
        private WinLifecycle lifecycle;
        private OriginalLevelWinActions actions;
        private Vector3 starEndPosition, starEndScale;
        private float elapsed;
        private bool animating, starSoundPending, pillowStarted, textStarted, shouldEnableOrientation, shouldShowRewardedAd, isLeague, destroyed;
        private int pillowStateHash;
        public OriginalUiDialog Lifecycle => lifecycle;
        public Button ContinueButton => continueButtons[0];
        public bool IsAnimationWorking => animating;
        public OriginalLevelWinActions Actions => actions;
        public void SetContinueText(string text)
        {
            if (continueLabels == null || continueLabels.Length == 0) throw new InvalidOperationException("Core Continue labels are not configured.");
            foreach (var label in continueLabels) label.text = text;
        }
        public void Bind(OriginalFlowAction action, IOriginalUiDialogManager manager, IOriginalLevelWinDialogContext context)
        {
            var session = context.GetCurrentSession() ?? throw new NullReferenceException();
            Bind(action, manager, context, context.GetLevelTitle(session), session.Number, session);
        }
        internal void BindCompletion(OriginalFlowAction action, IOriginalUiDialogManager manager,
            IOriginalLevelWinDialogContext context, string completionTitle, int levelNumber) =>
            Bind(action, manager, context, completionTitle, levelNumber, null);

        private void Bind(OriginalFlowAction action, IOriginalUiDialogManager manager,
            IOriginalLevelWinDialogContext context, string levelTitle, int levelNumber, OriginalActiveLevelData session)
        {
            this.context = context;
            lifecycle = new WinLifecycle(this, new OriginalUiDialogAction(action, manager)); actions = new OriginalLevelWinActions(this);
            title.SetLevelTitle(levelTitle, levelNumber);
            isLeague = session != null && session.IsLeague;
            bool rewarded = shouldShowRewardedAd = session != null && RequiredContext.ShouldShowRewardedAd;
            centerWithAd.SetActive(rewarded); centerWithoutAd.SetActive(!rewarded);
            bottomWithAd.SetActive(rewarded); bottomWithoutAd.SetActive(!rewarded);
            if (rewarded)
            {
                int amount = (session.AnimationValues?.Coin ?? throw new NullReferenceException()).GetCollectedAmount();
                defaultCoinText.text = amount.ToString(); rewardedCoinText.text = unchecked(amount * 5).ToString();
            }
            for (int i = 0; i < buttonCanvases.Length; i++) buttonCanvases[i].worldCamera = RequiredContext.UiCamera;
            for (int i = 0; i < continueButtons.Length; i++) continueButtons[i].onClick.AddListener(ContinueClicked);
            rewardedButton.onClick.AddListener(RewardedClicked); closeButton.onClick.AddListener(CloseClicked);
            pillowStateHash = Animator.StringToHash(pillowState);
        }
        public void Show() => (lifecycle ?? throw new NullReferenceException()).Show();
        private void ContinueClicked() => actions.OnContinueButton();
        private void RewardedClicked() => actions.OnCoinRewardedAdButtonClicked();
        private void CloseClicked() => actions.OnClosePressed();
        private void ShowStar()
        {
            starEndPosition = star.localPosition; starEndScale = star.localScale;
            star.localPosition = starStartPosition; star.localScale = starStartScale;
            star.localRotation = Quaternion.Euler(0, 0, starStartAngle);
            RequiredContext.DisableOrientation(37); shouldEnableOrientation = true;
            elapsed = 0; pillowStarted = textStarted = false; animating = true;
            starSoundPending = true;
            wellDoneText.Prepare();
        }
        private void Update()
        {
            if (!animating) return;
            // The original star sequence uses TweenManager's gameplay delta.
            float delta = RequiredContext.GameplayDeltaTime;
            if (delta <= 0f) return;
            if (starSoundPending)
            {
                starSoundPending = false;
                RequiredContext.PlaySound(360, starSoundOffset);
            }
            elapsed += delta;
            if (!pillowStarted && elapsed >= pillowCue) { pillowStarted = true; pillowAnimator.Play(pillowStateHash, 0, 0f); }
            if (!textStarted && elapsed >= textCue) { textStarted = true; wellDoneText.Play(); }
            float t = Mathf.Clamp01(elapsed / starMoveDuration);
            star.localPosition = Vector3.LerpUnclamped(starStartPosition, starEndPosition, t);
            star.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(starStartAngle, 0, t));
            if (elapsed < starMoveDuration) { star.localScale = Vector3.LerpUnclamped(starStartScale, starEndScale, t); return; }
            float time = elapsed - starMoveDuration, previous = 1f;
            for (int i = 0; i < starBounce.Length; i++)
            {
                var stage = starBounce[i];
                if (time < stage.duration) { star.localScale = starEndScale * Mathf.LerpUnclamped(previous, stage.scale, time / stage.duration); return; }
                time -= stage.duration; previous = stage.scale;
            }
            star.localScale = starEndScale; animating = false; AfterShowAnimation();
        }
        private void AfterShowAnimation()
        {
            if (!destroyed) RequiredContext.PlaySoundInLoop(359);
            EnableStarOrientation();
        }
        private void EnableStarOrientation()
        {
            if (!shouldEnableOrientation) return;
            shouldEnableOrientation = false; RequiredContext.EnableOrientation(37);
        }
        private void OnDestroy()
        {
            destroyed = true; animating = false;
            if (context != null) { EnableStarOrientation(); context.StopSoundInLoop(359); }
            for (int i = 0; i < continueButtons.Length; i++) if (continueButtons[i] != null) continueButtons[i].onClick.RemoveListener(ContinueClicked);
            if (rewardedButton != null) rewardedButton.onClick.RemoveListener(RewardedClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseClicked);
        }
        private IOriginalLevelWinDialogContext RequiredContext => context ?? throw new NullReferenceException();
        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog) => RequiredContext.UpdateDialogParent(transform, dialog.GetConfig());
        Vector3 IOriginalUiDialogHost.GetCameraCenter() => RequiredContext.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active) => gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine) => StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay) => Destroy(gameObject, delay);
        IOriginalLevelWinAds IOriginalLevelWinCloseContext.GetAds() => RequiredContext.GetAds();
        void IOriginalLevelWinCloseContext.EnableOrientationForStarAnimation() => EnableStarOrientation();
        void IOriginalLevelWinCloseContext.EnableBottomPanelOrientation() => RequiredContext.EnableBottomPanelOrientation();
        IOriginalLevelWinGameManager IOriginalLevelWinCloseContext.GetGameManager() => RequiredContext.GetGameManager();
        IOriginalLevelWinLavaFlow IOriginalLevelWinCloseContext.GetLavaFlow() => RequiredContext.GetLavaFlow();
        IOriginalLevelWinAncientFlow IOriginalLevelWinCloseContext.GetAncientFlow() => RequiredContext.GetAncientFlow();
        void IOriginalLevelWinCloseContext.CloseDialog() => lifecycle.Close();
        private sealed class WinLifecycle : OriginalUiDialog
        {
            private readonly OriginalLevelWinDialog owner;
            public WinLifecycle(OriginalLevelWinDialog owner, IOriginalUiDialogAction action) : base(action, owner) { this.owner = owner; }
            public override OriginalDialogConfig GetConfig() => owner.config;
            public override void SetSortingOffset(int offset) => owner.sortingGroup.sortingOrder = offset;
            public override void SetTouchSorting(int order) => owner.RequiredContext.SetDialogTouchSorting(order);
            protected override void CloseByEscape() => owner.actions.OnClosePressed();
            protected override void OnShow(Vector3 position)
            {
                owner.RequiredContext.StopAllSounds(); owner.RequiredContext.MarkLevelWinDialogShown();
                owner.sortingGroup.sortingOrder = 1;
                bool weekly = owner.RequiredContext.ShouldShowWeeklyPanel, cloche = owner.RequiredContext.ShouldShowCloche;
                if (!cloche && !weekly) { owner.transform.position = Vector3.zero; owner.bottomPanel.SetActive(false); }
                owner.transform.localScale = owner.RequiredContext.UiScale;
                owner.RequiredContext.PrepareBottomPanel(cloche, weekly);
                if (owner.shouldShowRewardedAd) owner.AfterShowAnimation();
                else if (owner.isLeague) owner.RequiredContext.ShowLeagueCrown();
                else owner.ShowStar();
                if (cloche || weekly) owner.RequiredContext.ShowBottomPanel();
            }
        }
    }
}
