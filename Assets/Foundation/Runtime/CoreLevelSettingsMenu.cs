using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    public sealed class CoreLevelSettingsMenu : MonoBehaviour, IOriginalUiDialogHost
    {
        [SerializeField] private Button gearButton, soundButton, musicButton, quitButton;
        [SerializeField] private Transform gear, gearShadow, options;
        [SerializeField] private GameObject soundMuteLine, musicMuteLine;
        [SerializeField] private Transform[] optionRoots;
        [SerializeField] private Vector3[] closedPositions;
        [SerializeField] private Canvas[] gearCanvases, optionCanvases;
        [SerializeField] private SpriteRenderer[] gearRenderers;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private float openDuration, closeDuration, openInterval, closeInterval, overshoot;
        [SerializeField] private float gearAngle, gearDuration, gearOvershoot;
        [SerializeField] private int closeSound;
        [SerializeField] private float closeSoundOffset;

        private CoreGameFlow owner;
        private OriginalGameAudioHost audio;
        private MenuDialog lifecycle;
        private Coroutine opening, turning;
        private Vector3[] openPositions, closeStartPositions;
        private int gearLayer, dialogLayer;
        private bool quitting, canChoose;
        private float currentAngle;
        public Button GearButton => gearButton;
        public Button SoundButton => soundButton;
        public Button MusicButton => musicButton;
        public Button QuitButton => quitButton;
        public OriginalUiDialog Lifecycle => lifecycle;

        private void Awake()
        {
            openPositions = new Vector3[optionRoots.Length];
            closeStartPositions = new Vector3[optionRoots.Length];
            for (int i = 0; i < optionRoots.Length; i++) openPositions[i] = optionRoots[i].localPosition;
            gearLayer = SortingLayer.NameToID("Ui");
            dialogLayer = SortingLayer.NameToID("Dialog");
        }

        internal void Bind(CoreGameFlow flow, OriginalGameAudioHost audioHost, Camera camera)
        {
            owner = flow;
            audio = audioHost;
            foreach (var canvas in gearCanvases) canvas.worldCamera = camera;
            foreach (var canvas in optionCanvases) canvas.worldCamera = camera;
            gearButton.onClick.AddListener(PressGear);
            soundButton.onClick.AddListener(ToggleSound);
            musicButton.onClick.AddListener(ToggleMusic);
            quitButton.onClick.AddListener(RequestQuit);
            ApplyMuteLines();
        }

        internal void Open(OriginalFlowAction action, IOriginalUiDialogManager manager)
        {
            quitting = false;
            lifecycle = new MenuDialog(this, new OriginalUiDialogAction(action, manager));
            lifecycle.Show();
        }

        private void PressGear()
        {
            if (lifecycle != null && lifecycle.IsShowing) lifecycle.Close();
            else owner.OpenLevelSettings();
        }
        private void ToggleSound() { audio.ToggleSound(); ApplyMuteLines(); }
        private void ToggleMusic() { audio.ToggleMusic(); ApplyMuteLines(); }
        private void ApplyMuteLines()
        {
            soundMuteLine.SetActive(audio.SoundMuted);
            musicMuteLine.SetActive(audio.MusicMuted);
        }

        private void RequestQuit()
        {
            if (canChoose) TriggerQuit();
        }
        private void TriggerQuit()
        {
            if (lifecycle.IsClosing) return;
            quitting = true;
            owner.QueueQuitConfirmation();
            lifecycle.Close();
        }

        private void ShowOptions()
        {
            owner.SetSettingsOrientation(false);
            SetOptionsInteractable(false);
            for (int i = 0; i < optionRoots.Length; i++) optionRoots[i].localPosition = closedPositions[i];
            options.gameObject.SetActive(true);
            SetGearLayer(dialogLayer);
            TurnGear(true);
            opening = StartCoroutine(AnimateOptions(true));
        }

        private IEnumerator CloseOptions()
        {
            SetOptionsInteractable(false);
            if (opening != null) { StopCoroutine(opening); opening = null; }
            for (int i = 0; i < optionRoots.Length; i++) closeStartPositions[i] = optionRoots[i].localPosition;
            audio.Playback.PlaySound(closeSound, closeSoundOffset);
            TurnGear(false);
            yield return AnimateOptions(false);
        }

        // UiSettingsMenuButton.Show/Hide 0x13be4b0/0x13bee90. The option
        // sequence moves from sound to exit on show, and reverses on close.
        private IEnumerator AnimateOptions(bool show)
        {
            float duration = show ? openDuration : closeDuration;
            float interval = show ? openInterval : closeInterval;
            float total = duration + interval * (optionRoots.Length - 1);
            for (float elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                for (int i = 0; i < optionRoots.Length; i++)
                {
                    int sequenceIndex = show ? i : optionRoots.Length - 1 - i;
                    float t = Mathf.Clamp01((elapsed - sequenceIndex * interval) / duration);
                    float eased = show ? OriginalGoalPanelUi.RewardEase(t, overshoot) : Mathf.Sin(t * Mathf.PI * .5f);
                    var from = show ? closedPositions[i] : closeStartPositions[i];
                    var to = show ? openPositions[i] : closedPositions[i];
                    optionRoots[i].localPosition = Vector3.LerpUnclamped(from, to, eased);
                }
                yield return null;
            }
            for (int i = 0; i < optionRoots.Length; i++)
                optionRoots[i].localPosition = show ? openPositions[i] : closedPositions[i];
            if (show) { opening = null; SetOptionsInteractable(true); }
        }

        private void TurnGear(bool show)
        {
            if (turning != null) StopCoroutine(turning);
            turning = StartCoroutine(RotateGear(show ? gearAngle : 0f));
        }
        private IEnumerator RotateGear(float target)
        {
            float from = currentAngle;
            for (float elapsed = 0f; elapsed < gearDuration; elapsed += Time.deltaTime)
            {
                SetGearAngle(Mathf.LerpUnclamped(from, target, OriginalGoalPanelUi.RewardEase(elapsed / gearDuration, gearOvershoot)));
                yield return null;
            }
            SetGearAngle(target);
            turning = null;
        }
        private void SetGearAngle(float angle)
        {
            currentAngle = angle;
            gear.localRotation = gearShadow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void SetOptionsInteractable(bool value)
        {
            canChoose = value;
            soundButton.interactable = musicButton.interactable = quitButton.interactable = value;
        }
        private void SetGearLayer(int layer)
        {
            foreach (var renderer in gearRenderers) renderer.sortingLayerID = layer;
            foreach (var canvas in gearCanvases) canvas.sortingLayerID = layer;
        }
        private void HideOptions()
        {
            options.gameObject.SetActive(false);
            SetGearLayer(gearLayer);
            owner.SetSettingsOrientation(true);
        }

        internal void Release()
        {
            if (owner == null) return;
            gearButton.onClick.RemoveListener(PressGear);
            soundButton.onClick.RemoveListener(ToggleSound);
            musicButton.onClick.RemoveListener(ToggleMusic);
            quitButton.onClick.RemoveListener(RequestQuit);
            StopAllCoroutines();
            opening = turning = null;
            options.gameObject.SetActive(false);
            SetGearLayer(gearLayer);
            SetGearAngle(0f);
            lifecycle = null;
            owner = null;
        }
        private void OnDestroy() => Release();

        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog) { } // The menu stays aligned with its bottom HUD anchor.
        Vector3 IOriginalUiDialogHost.GetCameraCenter() => owner.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active) => options.gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine) => StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay) { } // Reused for this level and its retries.

        private sealed class MenuDialog : OriginalUiDialog
        {
            private readonly CoreLevelSettingsMenu view;
            internal MenuDialog(CoreLevelSettingsMenu view, IOriginalUiDialogAction action) : base(action, view) { this.view = view; }
            public override OriginalDialogConfig GetConfig()
            {
                var config = view.config;
                config.ShouldShowBackgroundOnClose = view.quitting || view.owner.ShouldRetainSettingsShade;
                return config;
            }
            public override void SetSortingOffset(int offset) { } // This flow serializes settings and result dialogs.
            public override void SetTouchSorting(int order) { }
            protected override void OnShow(Vector3 position) => view.ShowOptions();
            protected override IEnumerator CloseAnimation() => view.CloseOptions();
            protected override void OnClose() => view.HideOptions();
            protected override void CloseByEscape() => view.TriggerQuit(); // 0x13be5e0.
        }
    }
}
