using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    public interface IOriginalTapToSkipViewContext : IOriginalTapToSkipContext
    {
        Camera UiCamera { get; }
        Vector3 GetTapTextPosition();
        string GetTapToSkipText();
    }
    public sealed class OriginalTapToSkipView : MonoBehaviour, IOriginalTapToSkipPresentation, IOriginalRemainingMovesSkipView
    {
        [SerializeField] private Button button;
        [SerializeField] private Graphic hitGraphic;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Transform root;
        [SerializeField] private TMP_Text text;
        [SerializeField] private bool alwaysShowInteractiveLabel;
        private bool interactionEnabled;
        [SerializeField] private float fadeDuration, pulseDuration, pulseScale;
        private IOriginalTapToSkipViewContext context;
        private OriginalTapToSkip model;
        private bool animate;
        private float elapsed;
        public Button Button => button;
        public OriginalTapToSkip Model => model;
        public event Action DestroyRequested;
        public void Bind(IOriginalTapToSkipViewContext context)
        {
            this.context = context;
            canvas.worldCamera = (context ?? throw new NullReferenceException()).UiCamera;
            model = new OriginalTapToSkip(context, this);
            button.onClick.AddListener(Clicked);
            model.Hide();
        }
        public void Hide() => (model ?? throw new NullReferenceException()).Hide();
        public void EnableSkipWithoutText() => (model ?? throw new NullReferenceException()).EnableSkipWithoutText();
        public void ShowSkipText() => (model ?? throw new NullReferenceException()).ShowSkipText();
        private void Clicked() => (model ?? throw new NullReferenceException()).Clicked();
        void IOriginalTapToSkipPresentation.SetInteractionEnabled(bool enabled)
        {
            interactionEnabled=enabled;button.interactable=enabled;hitGraphic.raycastTarget=enabled;
            if(enabled&&alwaysShowInteractiveLabel)ShowVisibleText();
        }
        void IOriginalTapToSkipPresentation.SetTextEnabled(bool enabled) => text.enabled = enabled || (alwaysShowInteractiveLabel&&interactionEnabled);
        void IOriginalTapToSkipPresentation.ArrangeTextPosition() => transform.position = RequiredContext.GetTapTextPosition();
        void IOriginalTapToSkipPresentation.ShowText() => ShowVisibleText();
        private void ShowVisibleText()
        {
            if (root == null) throw new NullReferenceException();
            text.transform.localScale = Vector3.one;
            text.enabled = true; text.text = RequiredContext.GetTapToSkipText();
            var color = text.color; color.a = 0; text.color = color;
            elapsed = 0; animate = true;
        }
        private void Update()
        {
            if (!animate) return;
            elapsed += Time.deltaTime;
            float phase = Mathf.Repeat(elapsed, pulseDuration * 2f);
            float t = phase < pulseDuration ? phase / pulseDuration : 2f - phase / pulseDuration;
            float ease = .5f - .5f * Mathf.Cos(t * Mathf.PI); // Native Ease.InOutSine4, restart loop.
            text.transform.localScale = Vector3.one * Mathf.LerpUnclamped(1f, pulseScale, ease);
            var color = text.color; color.a = Mathf.Clamp01(elapsed / fadeDuration); text.color = color;
        }
        public void DestroySkipView() { DestroyRequested?.Invoke(); Destroy(gameObject); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Clicked); }
        private IOriginalTapToSkipViewContext RequiredContext => context ?? throw new NullReferenceException();
    }
}
