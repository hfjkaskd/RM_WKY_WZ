using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    // Ordinary portrait OutOfMovesBanner, 0x1589e80 and 0x158a9a4.
    public sealed class CoreOutOfMovesBanner : MonoBehaviour, IOriginalFlowDialog, IOriginalUiDialogHost
    {
        [SerializeField] private Canvas buttonCanvas;
        [SerializeField] private Button skipButton;
        [SerializeField] private MeshRenderer titleRenderer;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private float enterDuration, overshootDuration, settleDuration, holdDuration, exitDuration, exitPause;
        [SerializeField] private float enterOffset, overshootOffset, exitOvershoot, skipDuration;
        private CoreGameFlow owner;
        private BannerLifecycle lifecycle;
        private Coroutine playback;
        private Vector3 top, center, bottom, skipBottom;
        private bool leaving;
        private int bannerOrder, titleOrder;
        public OriginalUiDialog Lifecycle => lifecycle;
        public Button SkipButton => skipButton;

        public void Bind(OriginalFlowAction action, IOriginalUiDialogManager manager, CoreGameFlow flow)
        {
            owner = flow;
            lifecycle = new BannerLifecycle(this, new OriginalUiDialogAction(action, manager));
            buttonCanvas.worldCamera = owner.UiCamera;
            bannerOrder = buttonCanvas.sortingOrder;
            titleOrder = titleRenderer.sortingOrder;
            skipButton.onClick.AddListener(lifecycle.Close);
        }

        public void Show() => lifecycle.Show();

        private void StartAnimation()
        {
            owner.DisableOrientation(35);
            var camera = owner.UiCamera;
            var cameraCenter = camera.transform.position;
            float halfHeight = skipButton.targetGraphic.rectTransform.rect.height * .5f;
            float edge = camera.orthographicSize + halfHeight;
            top = cameraCenter + Vector3.up * edge;
            bottom = cameraCenter + Vector3.down * edge;
            skipBottom = cameraCenter + Vector3.down * (camera.orthographicSize + 1f);
            center = owner.SafeCenter;
            transform.position = top;
            playback = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            owner.PlaySound(258, .04f);
            var entry = center - Vector3.up * enterOffset;
            var overshoot = center + Vector3.up * overshootOffset;
            float elapsed = 0f;
            float settleStart = enterDuration + overshootDuration;
            float holdStart = settleStart + settleDuration;
            float exitStart = holdStart + holdDuration;
            while (elapsed < exitStart)
            {
                if (elapsed < enterDuration)
                    transform.position = SineMove(top, entry, elapsed / enterDuration);
                else if (elapsed < settleStart)
                    transform.position = SineMove(entry, overshoot, (elapsed - enterDuration) / overshootDuration);
                else if (elapsed < holdStart)
                    transform.position = SineMove(overshoot, center, (elapsed - settleStart) / settleDuration);
                else transform.position = center;
                yield return null;
                elapsed += owner.GameplayDeltaTime;
            }

            DisableSkip();
            owner.PlaySound(259, .04f);
            float end = exitStart + exitDuration + exitPause;
            while (elapsed < end)
            {
                float t = Mathf.Clamp01((elapsed - exitStart) / exitDuration);
                float ease = t * t * ((exitOvershoot + 1f) * t - exitOvershoot);
                transform.position = Vector3.LerpUnclamped(center, bottom, ease);
                yield return null;
                elapsed += owner.GameplayDeltaTime;
            }
            transform.position = bottom;
            playback = null;
            lifecycle.Close();
        }

        private IEnumerator SkipExit()
        {
            StopCoroutine(playback);
            playback = null;
            DisableSkip();
            var start = transform.position;
            float elapsed = 0f;
            while (elapsed < skipDuration + exitPause)
            {
                float t = Mathf.Clamp01(elapsed / skipDuration);
                transform.position = Vector3.LerpUnclamped(start, skipBottom, t * t * t);
                yield return null;
                elapsed += owner.GameplayDeltaTime;
            }
            transform.position = skipBottom;
        }

        private static Vector3 SineMove(Vector3 from, Vector3 to, float t) =>
            Vector3.LerpUnclamped(from, to, Mathf.Sin(t * Mathf.PI * .5f));

        private void DisableSkip()
        {
            leaving = true;
            skipButton.interactable = false;
            skipButton.targetGraphic.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (lifecycle != null && skipButton != null) skipButton.onClick.RemoveListener(lifecycle.Close);
        }

        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog) => owner.UpdateDialogParent(transform, dialog.GetConfig());
        Vector3 IOriginalUiDialogHost.GetCameraCenter() => owner.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active) => gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine) => StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay) => Destroy(gameObject, delay);

        private sealed class BannerLifecycle : OriginalUiDialog
        {
            private readonly CoreOutOfMovesBanner view;
            public BannerLifecycle(CoreOutOfMovesBanner view, IOriginalUiDialogAction action) : base(action, view) { this.view = view; }
            public override OriginalDialogConfig GetConfig()
            {
                var value = view.config;
                value.ShouldCloseOnTouch = view.playback != null && !view.leaving;
                value.ShouldPlayCloseAnimation = value.ShouldCloseOnTouch;
                return value;
            }

            public override void SetSortingOffset(int offset)
            {
                view.buttonCanvas.sortingOrder = view.bannerOrder + offset;
                view.titleRenderer.sortingOrder = view.titleOrder + offset;
            }

            public override void SetTouchSorting(int order) { }
            protected override void OnShow(Vector3 position) => view.StartAnimation();
            protected override IEnumerator CloseAnimation() => view.SkipExit();
            protected override void OnClose() => view.owner.CompleteOutOfMovesBanner();
        }
    }
}
