using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public interface IOriginalTntItemViewDelegate
    {
        OriginalGridCell CurrentCell { get; }
        bool IsUnderHoney();
        bool IsUnderRoof();
        bool IsUnderChain();
    }
    public interface IOriginalTntViewServices
    {
        OriginalGameObjectPool Pool { get; }
        int ItemSortingLayer { get; }
        int ParticlesSortingLayer { get; }
        int UiSortingLayer { get; }
        bool HasIncomingTouchBlockingItem(OriginalGridCell cell);
        bool IsFallingCross(OriginalBoardItem item);
        void PlaySfx(int type, float offset);
    }
    // Creation/idle/sorting portion of native TntItemView. Explosion and combo
    // presentation have independent native paths and are restored separately.
    public sealed class OriginalTntItemSpriteView : OriginalItemSwapView, IOriginalPoolable, IOriginalSpecialCreationView
    {
        public Transform resetView, fuseTransform, movingFuseParent, fuseMaskAnimated;
        public OriginalRootTransform rootTransform;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public SpriteMask fuseMask;
        public SpriteRenderer baseView, shadow, fuseForLightball;
        public Animator animator;
        public SortingGroup sortingGroup;
        private IOriginalTntItemViewDelegate viewDelegate;
        private OriginalTntViewAssets assets;
        private IOriginalTntViewServices services;
        private IOriginalTntFuseParticles fuse;
        private readonly List<ParticleSystemRenderer> fuseMaskRenderers = new();
        private OriginalSpecialCreationParticleTransfer transfer;
        private int currentCellYToCheckAboveItem;
        private bool playFuseAnimation, didFixCreationSorting, allowSortingUpdates, isUnderAboveItem, underChain, hidden;
        private float initialShadowAlpha;
        public IOriginalTntFuseParticles Fuse => fuse;
        public bool DidFixCreationSorting => didFixCreationSorting;
        public bool IsUnderChain => underChain;
        public float PropellerComboShadowAlpha => assets.propellerComboShadowAlpha;
        public override int GetPoolId() => 14; // 0x27b87d4.
        public override void OnSpawn() { } // 0x27b87dc.
        private void Awake() { initialShadowAlpha = shadow.color.a; } // SpecialItemView 0x27b4be4.

        public void Initialize(IOriginalTntItemViewDelegate model, Transform parent, Vector3 position,
            OriginalTntViewAssets assets, IOriginalTntViewServices services) // 0x27b56b8.
        {
            viewDelegate = model; this.assets = assets; this.services = services;
            baseView.sprite = assets.GetSprite();
            transform.SetParent(parent); transform.localPosition = position;
            transform.localScale = Vector3.one; transform.rotation = Quaternion.identity;
            SwapAnimationEnded();
            resetView.localPosition = Vector3.zero; resetView.localScale = Vector3.one; resetView.localRotation = Quaternion.identity;
            if (hidden) Show();
            isUnderAboveItem = model.IsUnderHoney() || model.IsUnderRoof();
            underChain = model.IsUnderChain();
            var cell = model.CurrentCell;
            allowSortingUpdates = true; UpdateSorting(DefaultSorting());
            baseView.gameObject.SetActive(true); animator.gameObject.SetActive(false);
            shadow.transform.localPosition = assets.shadowOffset;
            shadow.transform.localScale = Vector3.one; shadow.color = assets.shadowColor;
            if (transfer == null) transfer = new OriginalSpecialCreationParticleTransfer(transform, services.Pool);
            PrepareFuseParticles();
            fuse.Play();
        }
        private void PrepareFuseParticles() // 0x27b7d50.
        {
            fuse = services.Pool.Spawn<OriginalTntFuseParticles>(16, true);
            var target = fuse.Transform;
            target.SetParent(fuseTransform); target.localPosition = Vector3.zero; target.localScale = Vector3.one;
            ResetFuseParticlesSorting();
        }
        public void ResetFuseParticlesSorting() => fuse.UpdateSorting(new OriginalSwapSorting(services.ParticlesSortingLayer, 0, false)); // 0x27b9470.

        public override void EnableFillMask() // 0x27b7ed8.
        {
            base.EnableFillMask();
            SetFuseFillMask(SpriteMaskInteraction.VisibleOutsideMask);
        }

        public override void DisableFillMask() // 0x27b7ffc.
        {
            SetFuseFillMask(SpriteMaskInteraction.None);
            base.DisableFillMask();
        }

        private void SetFuseFillMask(SpriteMaskInteraction interaction)
        {
            fuse.Transform.GetComponentsInChildren(true, fuseMaskRenderers);
            foreach (var renderer in fuseMaskRenderers) renderer.maskInteraction = interaction;
            fuseMaskRenderers.Clear();
        }

        private OriginalSwapSorting DefaultSorting() => OriginalItemSortingRules.Default(services.ItemSortingLayer);
        public OriginalSwapSorting GetCreationSorting() => new OriginalSwapSorting(services.ItemSortingLayer, 9100, false); // 0x12d85ec.
        public OriginalSwapSorting GetSorting() => new OriginalSwapSorting(sortingGroup.sortingLayerID, sortingGroup.sortingOrder, false);
        public void UpdateSorting(OriginalSwapSorting value) // 0x27b8bcc.
        {
            if (allowSortingUpdates)
            {
                sortingGroup.sortingLayerID = value.Layer; sortingGroup.sortingOrder = value.Order;
            }
            if (!value.SortEverything)
            {
                if (viewDelegate == null) throw new NullReferenceException();
                var cell = viewDelegate.CurrentCell;
                if (cell == null) return;
                if (!cell.StaticTouch.HasTouchBlockingItem() && !services.HasIncomingTouchBlockingItem(cell)) return;
            }
            if (fuse != null) fuse.UpdateSorting(OriginalItemSortingRules.WithOffset(value, 1));
        }
        public void UpdateSortingForAnimation(OriginalSwapSorting value, bool allowOtherUpdates)
        {
            allowSortingUpdates = true; UpdateSorting(value); allowSortingUpdates = allowOtherUpdates;
        }
        public void PlayCreationAnimation(float normalizedStartTime, bool playSound) // 0x27b8d9c.
        {
            if (playSound) services.PlaySfx(assets.creationSound, assets.creationSoundOffset);
            baseView.gameObject.SetActive(false); animator.gameObject.SetActive(true);
            fuse.Stop(); didFixCreationSorting = false;
            UpdateSortingForAnimation(GetCreationSorting(), false);
            animator.Play(0, 0, normalizedStartTime);
        }
        public void PlayBoosterAnimation() // 0x27b8f18.
        {
            baseView.gameObject.SetActive(false); animator.gameObject.SetActive(true); fuse.Stop();
            UpdateSortingForAnimation(new OriginalSwapSorting(services.UiSortingLayer, 7000, false), true);
            animator.Play(assets.GetState(false), 0, 0f);
        }
        public void PlayLightballCreationAnimation() // 0x27b925c.
        {
            fuseForLightball.gameObject.SetActive(true);
            fuseForLightball.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            var sorting = OriginalItemSortingRules.WithOffset(GetSorting(), 10);
            OriginalItemSortingRules.Apply(fuseForLightball, sorting);
            var back = OriginalItemSortingRules.WithOffset(sorting, -2);
            var front = OriginalItemSortingRules.WithOffset(sorting, 2);
            fuseMask.gameObject.SetActive(true);
            fuseMask.backSortingLayerID = back.Layer; fuseMask.backSortingOrder = back.Order;
            fuseMask.frontSortingLayerID = front.Layer; fuseMask.frontSortingOrder = front.Order;
            baseView.gameObject.SetActive(false); animator.gameObject.SetActive(true);
            animator.Play(assets.GetState(true), 0, 0f); playFuseAnimation = true;
        }
        private void Update() { ManualUpdate(); }
        public void ManualUpdate() // 0x27b811c.
        {
            if (animator.isActiveAndEnabled)
            {
                if (playFuseAnimation)
                {
                    fuse.Transform.position = movingFuseParent.position;
                    fuseMask.transform.position = fuseMaskAnimated.position;
                    fuseMask.transform.rotation = fuseMaskAnimated.rotation;
                    fuseMask.transform.localScale = fuseMaskAnimated.localScale;
                }
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == assets.GetState(true)) return;
                if (state.normalizedTime >= 1f) StopCreationAnimation();
                else if (state.normalizedTime <= .5f)
                {
                    if (state.normalizedTime > .4f && !didFixCreationSorting && viewDelegate != null)
                    {
                        var cell = viewDelegate.CurrentCell;
                        if (cell != null)
                        {
                            didFixCreationSorting = true;
                            if (viewDelegate.CurrentCell == null) throw new NullReferenceException();
                            UpdateSortingForAnimation(DefaultSorting(), true);
                        }
                    }
                }
                else fuse.Play();
                return;
            }
            if (currentCellYToCheckAboveItem < 0) return;
            if (viewDelegate == null) throw new NullReferenceException();
            if (viewDelegate.CurrentCell != null)
            {
                var cell = viewDelegate.CurrentCell;
                if (cell == null || cell.Mediation.CurrentItem == null) throw new NullReferenceException();
                if (!services.IsFallingCross(cell.Mediation.CurrentItem)) UpdateSorting(DefaultSorting());
            }
            if (currentCellYToCheckAboveItem == 0) currentCellYToCheckAboveItem = -1;
        }
        public void StopCreationAnimation() // 0x27b9098.
        {
            if (!animator.isActiveAndEnabled) return;
            baseView.gameObject.SetActive(true); animator.gameObject.SetActive(false);
            if (viewDelegate == null) throw new NullReferenceException();
            if (viewDelegate.CurrentCell == null) return;
            if (viewDelegate.CurrentCell == null) throw new NullReferenceException();
            UpdateSortingForAnimation(DefaultSorting(), true);
        }
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) // 0x27b985c.
        {
            if (under)
            {
                if (!didFixCreationSorting)
                {
                    if (viewDelegate == null) throw new NullReferenceException();
                    var cell = viewDelegate.CurrentCell;
                    if (cell != null && viewDelegate.CurrentCell == null) throw new NullReferenceException();
                    UpdateSortingForAnimation(DefaultSorting(), true); transfer.DecreaseCreationParticleSorting();
                }
                if (viewDelegate == null) throw new NullReferenceException();
                if (viewDelegate.CurrentCell == null && fuse != null) fuse.UpdateSorting(OriginalItemSortingRules.WithOffset(DefaultSorting(), 1));
            }
            isUnderAboveItem = under;
            var current = viewDelegate == null ? null : viewDelegate.CurrentCell;
            UpdateSorting(DefaultSorting());
        }
        public void SetCreationParticles(OriginalSpecialItemCreationParticles particles, bool above) => transfer.SetCreationParticles(particles, above, isUnderAboveItem);
        public void SetGameObjectActive(bool active) { gameObject.SetActive(active); }
        public void PlayFuseParticles() { fuse.Play(); }
        public override void SwapAnimationStarted(bool valid, int otherItem) // 0x27b9554.
        {
            base.SwapAnimationStarted(valid,otherItem);
            if (otherItem != 3) return;
            if (fuse != null) fuse.Recycle();
            fuse = services.Pool.Spawn<OriginalTntPropellerComboFuseParticles>(17,true); // 0x27b9594.
            fuse.Transform.SetParent(fuseTransform);
            fuse.Transform.localPosition = Vector3.zero; fuse.Transform.localScale = Vector3.one;
        }
        public void Hide() { resetView.gameObject.SetActive(false); hidden = true; fuse.Stop(); }
        public override void Show() { resetView.gameObject.SetActive(true); hidden = false; if (fuse != null) fuse.Play(); }
        public void FallStarted() { if (viewDelegate == null) throw new NullReferenceException(); var cell = viewDelegate.CurrentCell; currentCellYToCheckAboveItem = cell == null ? 11 : cell.Y; }
        public void FallEnded() { currentCellYToCheckAboveItem = -1; }
        public void ChangeAlpha(float alpha) { var color = baseView.color; color.a = alpha; baseView.color = color; color = shadow.color; color.a = alpha; shadow.color = color; }
        public void ResetAlpha() { var color = baseView.color; color.a = 1f; baseView.color = color; color = shadow.color; color.a = initialShadowAlpha; shadow.color = color; }
        public override void OnRecycle() // 0x27b8944, no extra base reset.
        {
            // An attempt reset can return views directly through the pool.
            if (IsFillMaskEnabled) DisableFillMask();
            baseView.transform.localScale = Vector3.one; baseView.transform.localPosition = Vector3.zero;
            fuseMask.transform.localPosition = assets.recycledMaskOffset;
            fuseMask.transform.rotation = Quaternion.identity; fuseMask.transform.localScale = assets.recycledMaskScale;
            if (fuse != null) { fuse.Recycle(); fuse = null; }
            viewDelegate = null; currentCellYToCheckAboveItem = 0;
            animator.gameObject.SetActive(false); playFuseAnimation = false;
            fuseMask.gameObject.SetActive(false); fuseForLightball.gameObject.SetActive(false); baseView.color = Color.white;
        }
    }
}
