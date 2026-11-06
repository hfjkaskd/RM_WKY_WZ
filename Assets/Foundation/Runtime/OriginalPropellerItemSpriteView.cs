using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    // GuidedExploderItemView Init/creation plus PropellerItemView's fall states.
    public sealed partial class OriginalPropellerItemSpriteView : OriginalItemSwapView, IOriginalPoolable, IOriginalSpecialCreationView
    {
        public OriginalRootTransform rootTransform;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public SortingGroup sortingGroup;
        public SpriteRenderer shadow;
        public Animator animator;
        public Transform animatorTransform, fanTransform;
        public Vector3 initialShadowPosition;
        public string creationState, startFallState, endFallState, endFallAltState;
        public int creationSound;
        public float creationSoundDelay;
        private int creationHash, startHash, endHash, endAltHash;
        private OriginalPropellerBoardItem model;
        private IOriginalRocketViewServices services;
        private OriginalSpecialCreationParticleTransfer transfer;
        private bool underAbove;
        public event Action<OriginalPropellerBoardItem> OnFallCreated;
        private void Awake()
        {
            creationHash = Animator.StringToHash(creationState); startHash = Animator.StringToHash(startFallState);
            endHash = Animator.StringToHash(endFallState); endAltHash = Animator.StringToHash(endFallAltState);
        }
        public override int GetPoolId() => 10; // 0x27c0a28.
        public override void OnSpawn() { } // 0x27b1c98.
        public override void OnRecycle() // 0x27c0f34.
        {
            fanTransform.localRotation = Quaternion.identity;
            OnFallCreated = null;
            onTargetReached = null;
            pilot = null;
            model = null;
            services = null;
        }
        public void Initialize(OriginalPropellerBoardItem model, Transform parent, Vector3 position, IOriginalRocketViewServices services)
        {
            this.model = model; this.services = services;
            pilot = null; didStartFlyAnimation = false;
            transfer = new OriginalSpecialCreationParticleTransfer(transform, services.Pool);
            shadow.transform.localPosition = initialShadowPosition; shadow.gameObject.SetActive(true);
            animatorTransform.localRotation = Quaternion.identity;
            transform.SetParent(parent); transform.localPosition = position;
            transform.localScale = Vector3.one; transform.rotation = Quaternion.identity;
            SwapAnimationEnded(); rootTransform.Init();
            UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer)); // 0x27b1abc.
        }
        public void SetGameObjectActive(bool active) => gameObject.SetActive(active);
        public OriginalSwapSorting GetSorting() => new OriginalSwapSorting(sortingGroup.sortingLayerID, sortingGroup.sortingOrder, false);
        public void UpdateSorting(OriginalSwapSorting value) { sortingGroup.sortingLayerID = value.Layer; sortingGroup.sortingOrder = value.Order; }
        public void PlayCreationAnimation(float start, bool playSound)
        { animator.Play(creationHash, 0, start); if (playSound) services.PlaySfx(creationSound, creationSoundDelay); } // 0x27b1d8c.
        public void StopCreationAnimation() => animator.Play(creationHash, 0, 1f); // 0x27b1c5c.
        public void FallStarted() // 0x27c0a8c.
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == creationHash && state.normalizedTime >= 1f || state.fullPathHash == endHash || state.fullPathHash == endAltHash)
                animator.Play(startHash, 0, 0f);
        }
        public void FallEnded() // 0x27c0bc0, static-free/current-cell branch.
        {
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == startHash) PlayEndFallAnimation();
            var cell = model.CurrentCell;
            if (cell != null && !OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItem(cell.Mediation.CurrentItem)) OnFallCreated?.Invoke(model);
        }
        // Called directly by the concrete fall lifecycle, without clip events
        // or serialized UnityEvent binding.
        public void PlayEndFallAnimation() => animator.Play(endHash, 0, 0f);
        public void PlayEndFallAltAnimation() => animator.Play(endAltHash, 0, 0f);
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under)
        { underAbove = under; if (under) transfer.DecreaseCreationParticleSorting(); UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer)); }
        public void SetCreationParticles(OriginalSpecialItemCreationParticles particles, bool spreading) => transfer.SetCreationParticles(particles, spreading, underAbove);
    }
}
