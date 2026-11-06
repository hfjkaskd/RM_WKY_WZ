using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public interface IOriginalWinLogoDialogContext : IOriginalTapToSkipViewContext
    {
        void UpdateDialogParent(Transform view, OriginalDialogConfig config);
        Vector3 GetCameraCenter();
        bool IsDeviceSupportLandscape { get; }
        void DisableHaptic();
        bool ShouldSpeedUpLevelEnd { get; }
        void StopFadeOutSoundSourceCoroutine();
        void ActivateSoundSourceAfterFadeOut();
        void EnableHaptic();
        void SetDialogTouchSorting(int order);
        OriginalRemainingMovesManager GetRemainingMovesManager();
        float GameplayDeltaTime { get; }
        Coroutine StartApplicationCoroutine(IEnumerator routine);
        void EnableOrientation(int reason);
        bool IsLandscape { get; }
        long PlayWinLogoSound(int clipType);
        void StopWinLogoSound(long key);
        void RaiseWinLogoHud(int layer, int order);
    }
    // Prefab-owned lifetime for the recovered logo, sound and TapToSkip.
    public sealed class OriginalWinLogoDialog : MonoBehaviour, IOriginalWinLogoDialog, IOriginalUiDialogHost
    {
        [SerializeField] private OriginalWinLogoVisual visual;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private float fireworkTailDuration = 1f;
        [SerializeField] private OriginalTapToSkipView tapToSkip;
        [SerializeField] private float skipReverseCutoff, skipLandscapeEndTime, skipPortraitEndTime;
        [SerializeField] private int completionSoundType, portraitHudSortingOrder;
        private IOriginalWinLogoDialogContext context;
        private long completionSoundKey;
        private LogoLifecycle lifecycle;
        private int initialSortingOrder;
        private bool stopAnimationPending;
        private float stopAnimationElapsed, stopAnimationDelay, destroyDuration;
        private OriginalRemainingMovesManager remaining;
        public OriginalTapToSkipView TapToSkip => tapToSkip;
        public float AnimationTime => visual.AnimationTime;
        public OriginalUiDialog Lifecycle => lifecycle;
        public void Bind(OriginalFlowAction action, IOriginalUiDialogManager manager, IOriginalWinLogoDialogContext context)
        {
            this.context = context;
            initialSortingOrder = (sortingGroup != null ? sortingGroup : throw new NullReferenceException()).sortingOrder;
            lifecycle = new LogoLifecycle(this, new OriginalUiDialogAction(action, manager));
            (visual != null ? visual : throw new NullReferenceException()).Initialize();
            RefreshTrumpetLayout();
            visual.OnCloseRequested += Close;
            remaining = RequiredContext.GetRemainingMovesManager() ?? throw new NullReferenceException();
            tapToSkip.Bind(RequiredContext);
            visual.OnSkipEnabled += AttachToTapToSkip;
            completionSoundKey = RequiredContext.PlayWinLogoSound(completionSoundType); // Awake 0x18d11b8.
            if (!RequiredContext.IsLandscape) // UpdateGameUiSorting 0x18d15e8.
                RequiredContext.RaiseWinLogoHud(sortingGroup.sortingLayerID, portraitHudSortingOrder);
        }
        internal void RefreshTrumpetLayout() => visual.RefreshTrumpetLayout(RequiredContext.UiCamera);
        public void Show() => (lifecycle ?? throw new NullReferenceException()).Show();
        public void Close() => (lifecycle ?? throw new NullReferenceException()).Close();
        public void PlayWinLogoAnimation() => visual.Play();
        private void Update()
        {
            if (lifecycle == null) return;
            visual.ManualUpdate(Time.deltaTime);
            if (!stopAnimationPending) return;
            stopAnimationElapsed += Time.deltaTime;
            if (stopAnimationElapsed < stopAnimationDelay) return;
            stopAnimationPending = false; visual.StopAnimation();
        }
        private void AttachToTapToSkip() //0x18d17e0.
        {
            tapToSkip.transform.SetParent(null, true);
            remaining.SkipView = tapToSkip;
            remaining.OnTapToSkip += OnTapToSkip;
            tapToSkip.EnableSkipWithoutText(); tapToSkip.ShowSkipText();
        }
        private void OnTapToSkip() // 0x18d18b8.
        {
            RequiredContext.StopWinLogoSound(completionSoundKey);
            float time = visual.TrackTime;
            destroyDuration = 0f;
            if (time < skipReverseCutoff)
            {
                visual.ReverseForSkip();
                stopAnimationElapsed = 0;
                stopAnimationDelay = (time - 43f / 60f) * .5f;
                stopAnimationPending = true;
                destroyDuration = (RequiredContext.IsLandscape ? skipLandscapeEndTime : skipPortraitEndTime) - time;
            }
            visual.SetSkipSorting();
            Close();
        }
        private void OnDestroy()
        {
            if (visual != null) { visual.OnCloseRequested -= Close; visual.OnSkipEnabled -= AttachToTapToSkip; }
            if (remaining != null) remaining.OnTapToSkip -= OnTapToSkip;
        }
        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog) => RequiredContext.UpdateDialogParent(transform, dialog.GetConfig());
        Vector3 IOriginalUiDialogHost.GetCameraCenter() => RequiredContext.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active) => gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine) => StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay) => Destroy(gameObject, delay);
        private IOriginalWinLogoDialogContext RequiredContext => context ?? throw new NullReferenceException();
        private IEnumerator DestroyFireworkParticles(GameObject particles) // 0x18d23c8.
        {
            float delay = destroyDuration;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float duration = 0f;
            while (duration < fireworkTailDuration)
            {
                duration += RequiredContext.GameplayDeltaTime;
                var remaining = RequiredContext.GetRemainingMovesManager() ?? throw new NullReferenceException();
                if (remaining.IsTapToSkipClicked) break;
                yield return null;
            }
            Destroy(particles);
        }
        private sealed class LogoLifecycle : OriginalUiDialog
        {
            private readonly OriginalWinLogoDialog owner;
            public LogoLifecycle(OriginalWinLogoDialog owner, IOriginalUiDialogAction action) : base(action, owner) { this.owner = owner; }
            public override OriginalDialogConfig GetConfig()
            {
                var value = owner.config;
                value.ShouldInheritUiScale = !owner.RequiredContext.IsDeviceSupportLandscape; // 0x18d2240.
                value.DestroyOnCloseDelay = owner.destroyDuration;
                return value;
            }
            public override void SetSortingOffset(int offset) => owner.sortingGroup.sortingOrder = unchecked(owner.initialSortingOrder + offset);
            public override void SetTouchSorting(int order) => owner.RequiredContext.SetDialogTouchSorting(order);
            protected override void OnShow(Vector3 position) => owner.RequiredContext.DisableHaptic(); // 0x18d1768 does not call base.OnShow.
            protected override void OnClose()
            {
                if (owner.RequiredContext.ShouldSpeedUpLevelEnd)
                {
                    owner.RequiredContext.StopFadeOutSoundSourceCoroutine();
                    owner.RequiredContext.ActivateSoundSourceAfterFadeOut();
                    owner.RequiredContext.EnableHaptic();
                }
                owner.remaining.OnTapToSkip -= owner.OnTapToSkip;
                owner.visual.CloseDialogVisual();
                var particles = owner.visual.TransferFireworksOwnership();
                owner.RequiredContext.StartApplicationCoroutine(owner.DestroyFireworkParticles(particles));
                owner.RequiredContext.EnableOrientation(38);
            }
        }
    }
}
