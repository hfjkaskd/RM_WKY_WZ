using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable]
    public struct OriginalDialogConfig
    {
        public bool ShouldCloseOnEscape, ShouldCloseOnTouch, ShouldCloseOnSwipe;
        public float BackgroundFadeInDelay, BackgroundFadeInDuration, BackgroundFadeOutDuration, BackgroundFadeOutDelay;
        public float BackgroundFadeAlpha, BackgroundStartAlphaForAnimation;
        public bool ShouldInheritUiScale, ShouldShowBackgroundOnClose, ShouldPlayCloseAnimation, ShouldNotDestroyOnClose;
        public float DestroyOnCloseDelay;
    }

    // TweenManager.GetDeltaTime 0x38ffc34 selects the live level clock
    // while the game context is active. Every dialog tween shares that clock.
    public interface IOriginalTweenClock
    {
        float DeltaTime { get; }
    }

    [Serializable]
    internal struct OriginalDialogEntrance
    {
        [SerializeField] private float startScale, peakScale, dipScale;
        [SerializeField] private float riseDuration, dipDuration, settleDuration;

        // UiDialog.OnShow 0x25cb6cc appends three OutQuad scale tweens.
        internal IEnumerator Play(Transform target, Vector3 position, Vector3 scale, IOriginalTweenClock clock, Action onComplete = null)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            target.position = position;
            target.localScale = scale * startScale;
            float elapsed = 0f;
            float settleStart = riseDuration + dipDuration;
            float end = settleStart + settleDuration;
            while (elapsed < end)
            {
                yield return null;
                elapsed += clock.DeltaTime;
                float factor;
                if (elapsed < riseDuration) factor = Ease(startScale, peakScale, elapsed / riseDuration);
                else if (elapsed < settleStart) factor = Ease(peakScale, dipScale, (elapsed - riseDuration) / dipDuration);
                else factor = Ease(dipScale, 1f, Mathf.Clamp01((elapsed - settleStart) / settleDuration));
                target.localScale = scale * factor;
            }
            target.localScale = scale;
            onComplete?.Invoke();
        }

        private static float Ease(float from, float to, float t) => Mathf.LerpUnclamped(from, to, t * (2f - t));
    }

    public interface IOriginalUiDialogAction
    {
        IOriginalUiDialogManager DialogManager { get; }
        void Complete();
    }
    public interface IOriginalUiDialogManager
    {
        void ShowDialog(OriginalUiDialog dialog);
        bool CloseDialog(OriginalUiDialog dialog);
        void DialogClosed(OriginalUiDialog dialog);
    }
    public interface IOriginalUiDialogHost
    {
        void UpdateParent(OriginalUiDialog dialog);
        Vector3 GetCameraCenter();
        void SetActive(bool active);
        void StartDialogCoroutine(IEnumerator routine);
        void DestroyDialog(float delay);
    }
    // Typed replacement for UiDialog's DialogAction reference. The manager is
    // read again at each native access, including after user callbacks.
    public sealed class OriginalUiDialogAction : IOriginalUiDialogAction
    {
        private readonly OriginalFlowAction action;
        public IOriginalUiDialogManager DialogManager { get; set; }
        public OriginalUiDialogAction(OriginalFlowAction action, IOriginalUiDialogManager manager)
        { this.action = action; DialogManager = manager; }
        public void Complete() => (action ?? throw new NullReferenceException()).Complete();
    }

    // UiDialog 0x25cb31c/0x25cb9f8/0x25cbf44. Scene, camera and coroutine
    // transport are explicit owners, shared by Editor and player builds.
    public abstract class OriginalUiDialog : IOriginalFlowDialog
    {
        private readonly IOriginalUiDialogHost host;
        public IOriginalUiDialogAction Action { get; set; }
        public int DialogId { get; set; }
        public bool IsShowing { get; private set; }
        public bool IsClosing { get; private set; }
        protected OriginalUiDialog(IOriginalUiDialogAction action, IOriginalUiDialogHost host)
        { Action = action; this.host = host; }
        public void Show()
        {
            if (IsShowing) return; // Native duplicate-show diagnostic is nonthrowing.
            IsShowing = true;
            RequiredManager().ShowDialog(this);
            (host ?? throw new NullReferenceException()).UpdateParent(this);
            var center = host.GetCameraCenter(); center.z = 0f;
            OnShow(center);
        }
        public void Close() => Close(false);
        public void ForceClose() => Close(true);
        private void Close(bool force)
        {
            if (!IsShowing || IsClosing) return;
            if (!force && !CanClose()) return;
            IsClosing = true;
            // A manager rejection leaves IsClosing set in the original.
            if (!RequiredManager().CloseDialog(this)) return;
            (host ?? throw new NullReferenceException()).SetActive(true);
            host.StartDialogCoroutine(CloseCoroutine());
        }
        private IOriginalUiDialogManager RequiredManager() =>
            ((Action ?? throw new NullReferenceException()).DialogManager ?? throw new NullReferenceException());
        private IEnumerator CloseCoroutine()
        {
            if (GetConfig().ShouldPlayCloseAnimation) yield return CloseAnimation();
            RequiredManager().DialogClosed(this);
            OnClose();
            (Action ?? throw new NullReferenceException()).Complete();
            var config = GetConfig(); // Native rereads after action completion.
            if (!config.ShouldNotDestroyOnClose)
                (host ?? throw new NullReferenceException()).DestroyDialog(config.DestroyOnCloseDelay);
            IsShowing = IsClosing = false;
        }
        public void PressBackground(bool isSwipe)
        {
            var config = GetConfig();
            if (isSwipe ? config.ShouldCloseOnSwipe : config.ShouldCloseOnTouch) CloseByBackground();
        }
        public void PressEscape() { if (GetConfig().ShouldCloseOnEscape) CloseByEscape(); }
        protected virtual bool CanClose() => true;
        protected virtual IEnumerator CloseAnimation() { yield break; }
        protected virtual void CloseByBackground() => Close();
        protected virtual void CloseByEscape() => Close();
        protected abstract void OnShow(Vector3 position);
        protected virtual void OnClose() { }
        public abstract OriginalDialogConfig GetConfig();
        public abstract void SetSortingOffset(int offset);
        public abstract void SetTouchSorting(int order);
    }
}
