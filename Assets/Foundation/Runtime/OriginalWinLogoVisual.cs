using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    // Original WinLogoAnimationDialog visual timeline. The owning dialog/flow
    // controls close/skip and sound; this component never fabricates a result.
    public sealed class OriginalWinLogoVisual : MonoBehaviour
    {
        [SerializeField] private OriginalWinLogoRigView animationView;
        [SerializeField] private Transform fireworks;
        [SerializeField] private Renderer otherPartsRenderer;
        [SerializeField] private SortingGroup fireworkSorting;
        [SerializeField] private int skipSortingOrder;
        [SerializeField] private ParticleSystem[] fireworkSystems;
        [SerializeField] private float particlesTime = 1f, closeTime = 3f, skipTime = .75f;
        private float elapsed;
        [SerializeField] private int logoPartIndex = 1;
        [SerializeField] private Vector2 shortWideTrumpetOffsets, wideTrumpetOffsets, squareTrumpetOffsets;
        private bool started, particlesStarted, closeRequested, skipEnabled, fireworksOwnershipTransferred;
        public bool CanSkip => skipEnabled;
        public float AnimationTime => elapsed;
        internal float TrackTime => animationView.TrackTime;
        public OriginalWinLogoRigView AnimationView => animationView;
        public event Action OnSkipEnabled;
        public event Action OnCloseRequested;
        public void Initialize()
        {
            animationView.Initialize();
            animationView.Advance(Time.deltaTime); // Awake advances the hidden track once at 0x18d112c.
            animationView.gameObject.SetActive(false);
            for (int i = 0; i < fireworkSystems.Length; i++) fireworkSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        internal void RefreshTrumpetLayout(Camera camera)
        {
            // WinLogoAnimationDialog.SetTrumpetsForScreen 0x18d1334, portrait branch.
            float aspect = camera.aspect > 1f ? 1f / camera.aspect : camera.aspect;
            var offsets = aspect < .57f ? Vector2.zero : aspect < .6f ? shortWideTrumpetOffsets :
                aspect < .8f ? wideTrumpetOffsets : squareTrumpetOffsets;
            animationView.SetHorizontalOffsets(offsets);
        }
        public void Play() // 0x18d0730: original typed replacements for Invoke.
        {
            elapsed = 0;
            particlesStarted = closeRequested = skipEnabled = false; started = true;
            animationView.SetPartVisible(logoPartIndex, true);
            animationView.gameObject.SetActive(true); animationView.RefreshMesh();
        }
        public void ManualUpdate(float unityDelta)
        {
            if (!started) return;
            elapsed += unityDelta; // Original SkeletonAnimation uses Unity.deltaTime.
            if (animationView.gameObject.activeSelf)
            {
                animationView.Advance(unityDelta);
            }
            if (!skipEnabled && elapsed >= skipTime) { skipEnabled = true; OnSkipEnabled?.Invoke(); }
            if (!particlesStarted && elapsed >= particlesTime)
            {
                particlesStarted = true;
                for (int i = 0; i < fireworkSystems.Length; i++) fireworkSystems[i].Play(false);
                fireworks.SetParent(null, true);
            }
            // Invoke literal at GOT0x53c1050 -> ScriptString0x5606e50 is
            // "Close", not "StopAnimation". The dialog owner must advance its
            // FlowAction and manage background/lifetime on this actual close.
            if (!closeRequested && elapsed >= closeTime) { closeRequested = true; OnCloseRequested?.Invoke(); }
        }
        public void CloseVisual()
        { started = false; animationView.gameObject.SetActive(false); StopEmitting(); }
        public void ReverseForSkip() { animationView.SetSpeed(-2f); }
        internal void SetSkipSorting() // OnTapToSkip 0x18d1a90/0x18d1acc.
        {
            otherPartsRenderer.sortingOrder = skipSortingOrder;
            fireworkSorting.sortingOrder = skipSortingOrder;
        }
        public void StopAnimation() { animationView.gameObject.SetActive(false); }
        public void CloseDialogVisual()
        {
            animationView.SetPartVisible(logoPartIndex, false);
            animationView.ClearClosingSlots();
            StopEmitting();
        }
        public void StopEmitting()
        {
            for (int i = 0; i < fireworkSystems.Length; i++)
            { var emission = fireworkSystems[i].emission; emission.enabled = false; }
        }
        public GameObject TransferFireworksOwnership()
        { fireworksOwnershipTransferred = true; return fireworks.gameObject; }
        private void OnDestroy()
        { if (!fireworksOwnershipTransferred && fireworks != null && fireworks.parent == null) Destroy(fireworks.gameObject); }
    }
}
