using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    public sealed class CoreDialogBackground : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Image dismissImage;
        [SerializeField] private Canvas dismissCanvas;
        private OriginalUiDialog dismissDialog;
        private IOriginalTweenClock clock;
        private float elapsed, delay, duration, startAlpha, endAlpha;
        private Action onHidden;

        internal bool IsAllocated { get; private set; }

        internal void Initialize(Camera camera, IOriginalTweenClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            dismissCanvas.worldCamera = camera;
            dismissButton.onClick.AddListener(PressBackground);
            Resize(camera);
            enabled = false;
            gameObject.SetActive(false);
        }

        internal void Resize(Camera camera)
        {
            var center = camera.transform.position;
            center.z = 0f;
            transform.position = center;
            float height = camera.orthographicSize * 2f;
            background.size = new Vector2(height * camera.aspect, height);
            dismissImage.rectTransform.sizeDelta = background.size;
        }

        // DialogBackgroundDark.Show 0x25c7bf8. Reuse completes a pending hide.
        internal void Show(OriginalDialogConfig config, int sortingOffset, OriginalUiDialog dismissible)
        {
            if (onHidden != null) CompleteHide(false);
            IsAllocated = true;
            gameObject.SetActive(true);
            // The core prefabs flatten their renderer orders starting at zero.
            background.sortingOrder = sortingOffset - 1;
            dismissDialog = dismissible;
            background.enabled = dismissible == null;
            dismissCanvas.sortingOrder = background.sortingOrder;
            dismissCanvas.gameObject.SetActive(dismissible != null);
            dismissButton.interactable = dismissible != null;
            if (config.BackgroundFadeInDuration > 0f)
            {
                SetAlpha(config.BackgroundStartAlphaForAnimation);
                StartFade(config.BackgroundFadeAlpha, config.BackgroundFadeInDelay, config.BackgroundFadeInDuration);
            }
            else
            {
                enabled = false;
                SetAlpha(config.BackgroundFadeAlpha);
            }
        }

        // DialogBackgroundDark.Hide 0x25c8170. Keep the current alpha when
        // cancelling a show, and keep the renderer visible for a retained close.
        internal void Hide(OriginalDialogConfig config, Action completed)
        {
            dismissDialog = null;
            dismissButton.interactable = false;
            onHidden = completed;
            if (config.ShouldShowBackgroundOnClose || config.BackgroundFadeOutDuration <= 0f)
                CompleteHide(config.ShouldShowBackgroundOnClose);
            else
                StartFade(0f, config.BackgroundFadeOutDelay, config.BackgroundFadeOutDuration);
        }

        private void StartFade(float alpha, float wait, float time)
        {
            startAlpha = background.color.a;
            endAlpha = alpha;
            delay = wait;
            duration = time;
            elapsed = 0f;
            enabled = true;
        }

        private void Update()
        {
            elapsed += clock.DeltaTime;
            if (elapsed < delay) return;
            float t = Mathf.Clamp01((elapsed - delay) / duration);
            float inverse = 1f - t;
            SetAlpha(Mathf.LerpUnclamped(startAlpha, endAlpha, 1f - inverse * inverse * inverse)); // Ease.OutCubic
            if (t < 1f) return;
            if (onHidden != null) CompleteHide(false);
            else enabled = false;
        }

        private void CompleteHide(bool keepVisible)
        {
            var completed = onHidden;
            onHidden = null;
            enabled = false;
            IsAllocated = false;
            if (!keepVisible)
            {
                SetAlpha(0f);
                gameObject.SetActive(false);
            }
            completed?.Invoke();
        }

        private void SetAlpha(float alpha)
        {
            var color = background.color;
            color.a = alpha;
            background.color = color;
            dismissImage.color = color;
        }
        private void PressBackground() => dismissDialog?.PressBackground(false);

        internal void Release()
        {
            dismissDialog = null;
            dismissButton.onClick.RemoveListener(PressBackground);
            onHidden = null;
            enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    // DialogBackground 0x25c7790/0x25c7a08/0x25c7e40.
    internal sealed class CoreDialogBackgroundPool
    {
        private readonly Camera camera;
        private readonly IOriginalTweenClock clock;
        private readonly Func<CoreDialogBackground> create;
        private readonly List<CoreDialogBackground> layers = new List<CoreDialogBackground>();
        private readonly Dictionary<int, CoreDialogBackground> byDialog = new Dictionary<int, CoreDialogBackground>();

        public CoreDialogBackgroundPool(Camera camera, Func<CoreDialogBackground> create, IOriginalTweenClock clock)
        {
            this.camera = camera;
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.create = create;
        }

        public void Show(int id, OriginalDialogConfig config, int offset, bool first, OriginalUiDialog dismissible)
        {
            if (!byDialog.TryGetValue(id, out var layer))
            {
                layer = GetLayer(first);
                byDialog.Add(id, layer);
            }
            layer.Resize(camera);
            layer.Show(config, offset, dismissible);
        }

        private CoreDialogBackground GetLayer(bool first)
        {
            if (first && layers.Count > 0) return layers[0];
            foreach (var layer in layers)
                if (!layer.IsAllocated) return layer;
            var created = create();
            created.Initialize(camera, clock);
            layers.Add(created);
            return created;
        }

        public void Hide(int id, OriginalDialogConfig config, Action hidden)
        {
            if (!byDialog.TryGetValue(id, out var layer))
            {
                hidden?.Invoke();
                return;
            }
            layer.Hide(config, () =>
            {
                hidden?.Invoke();
                byDialog.Remove(id);
            });
        }

        public void Resize()
        {
            foreach (var layer in layers) layer.Resize(camera);
        }

        public void Release()
        {
            foreach (var layer in layers)
                if (layer != null) layer.Release();
            byDialog.Clear();
            layers.Clear();
        }
    }
}
