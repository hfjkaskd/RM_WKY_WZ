using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public interface IOriginalLightballViewServices
    {
        OriginalLightballViewAssets LightballAssets { get; }
        OriginalSwapSorting GetLightballUseSorting();
        long StartLightballChargeSound();
        void ReleaseLightballChargeSound(long key, bool exploded);
    }
    // LightballItemView's original Init/creation/update branch. Activation,
    // rays and combos are independent native paths and remain separate.
    public sealed class OriginalLightballItemSpriteView : OriginalItemSwapView, IOriginalPoolable, IOriginalSpecialCreationView
    {
        public OriginalRootTransform rootTransform;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public SortingGroup sortingGroup;
        public SpriteRenderer baseView, shadow;
        public Animator animator;
        public string creationState, chargerLandState, defaultState;
        public int creationSound;
        public float creationSoundDelay;
        private int creationHash, chargerHash, defaultHash;
        private long chargeSoundKey;
        private bool creationFinished, allowSortingUpdates, underAbove, didStartUse;
        private OriginalSpecialCreationParticleTransfer transfer;
        private IOriginalRocketViewServices services;
        public bool CreationFinished => creationFinished;
        private void Awake()
        {
            creationHash = Animator.StringToHash(creationState);
            chargerHash = Animator.StringToHash(chargerLandState);
            defaultHash = Animator.StringToHash(defaultState);
        }
        public override int GetPoolId() => 23; // 0x1f41ee4.
        public override void OnSpawn() { } // 0x1f41eec.
        public override void OnRecycle() => ReleaseChargeSound(false);
        public void Initialize(Transform parent, Vector3 position, IOriginalRocketViewServices services)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            transfer = new OriginalSpecialCreationParticleTransfer(transform, services.Pool);
            transform.SetParent(parent); transform.localPosition = position;
            transform.localScale = Vector3.one; transform.rotation = Quaternion.identity;
            SwapAnimationEnded(); rootTransform.Init();
            allowSortingUpdates = true;
            UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer));
            animator.Play(defaultHash, 0, 0f); creationFinished = true; // 0x1f3dd54.
            didStartUse = false;
        }
        public void SetGameObjectActive(bool active) => gameObject.SetActive(active);
        public OriginalSwapSorting GetSorting() => new OriginalSwapSorting(sortingGroup.sortingLayerID, sortingGroup.sortingOrder, false);
        public void UpdateSorting(OriginalSwapSorting value)
        { if (allowSortingUpdates) { sortingGroup.sortingLayerID = value.Layer; sortingGroup.sortingOrder = value.Order; } }
        private void UpdateSortingForAnimation(OriginalSwapSorting value)
        { allowSortingUpdates = true; UpdateSorting(value); allowSortingUpdates = false; } // 0x2b4ac00, allow=false.
        public void PlayCreationAnimation(float start, bool playSound)
        {
            if (playSound) services.PlaySfx(creationSound, creationSoundDelay);
            creationFinished = false;
            UpdateSortingForAnimation(new OriginalSwapSorting(services.ItemSortingLayer, 7100, false)); // 0x12d90e4.
            animator.Play(creationHash, 0, start); // 0x1f41f70.
        }
        private void Update()
        {
            if (creationFinished) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if ((state.fullPathHash == creationHash || state.fullPathHash == chargerHash) && state.normalizedTime <= 1f) return;
            creationFinished = true;
            UpdateSortingForAnimation(OriginalItemSortingRules.Default(services.ItemSortingLayer)); // 0x1f41cdc.
        }
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under)
        {
            if (under && !creationFinished)
            { UpdateSortingForAnimation(OriginalItemSortingRules.Default(services.ItemSortingLayer)); transfer.DecreaseCreationParticleSorting(); }
            underAbove = under; UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer));
        }
        public void SetCreationParticles(OriginalSpecialItemCreationParticles particles, bool spreading) => transfer.SetCreationParticles(particles, spreading, underAbove);
        public override void SwapAnimationStarted(bool valid, int otherItemType)
        {
            base.SwapAnimationStarted(valid, 0);
            if (valid && otherItemType != 31) PlayUseAnimation(otherItemType); // 0x1f423c4.
        }
        public void PlayUseAnimation(int otherItemType)
        {
            if (didStartUse) return;
            var owner = services as IOriginalLightballViewServices ?? throw new InvalidOperationException("Lightball view owner is not bound.");
            if (otherItemType != 5) chargeSoundKey = owner.StartLightballChargeSound(); // 0x1f42250..0x1f422e0.
            UpdateSortingForAnimation(owner.GetLightballUseSorting());
            animator.Play(Animator.StringToHash(owner.LightballAssets.useState), 0, 0f);
            didStartUse = true; creationFinished = true;
        }
        internal void ReleaseChargeSound(bool exploded)
        {
            long key = chargeSoundKey;
            chargeSoundKey = 0;
            if (key != 0) ((IOriginalLightballViewServices)services).ReleaseLightballChargeSound(key, exploded);
        }
    }
}
