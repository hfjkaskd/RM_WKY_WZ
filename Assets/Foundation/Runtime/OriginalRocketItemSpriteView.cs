using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public interface IOriginalRocketItemViewDelegate
    {
        int Orientation { get; }
        OriginalGridCell CurrentCell { get; }
        bool IsUnderHoney();
        bool IsUnderRoof();
        bool IsUnderChain();
    }
    public interface IOriginalRocketViewServices
    {
        OriginalGameObjectPool Pool { get; }
        int ItemSortingLayer { get; }
        bool IsWaterMode { get; }
        bool IsDeviceWide { get; }
        void PlaySfx(int type, float offset);
    }

    public sealed partial class OriginalRocketItemSpriteView : OriginalItemSwapView, IOriginalPoolable, IOriginalSpecialCreationView
    {
        public Transform resetView;
        public OriginalRootTransform rootTransform;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public SortingGroup sortingGroup;
        public SpriteRenderer baseView, shadow, part1, part2;
        public Animator animator;
        public Transform verticalAnimation, horizontalAnimation;
        private IOriginalRocketItemViewDelegate viewDelegate;
        private IOriginalRocketViewServices services;
        private OriginalRocketViewAssets assets;
        private OriginalRocketCreationStrategy strategy;
        private OriginalSpecialCreationParticleTransfer transfer;
        private bool creationAnimationFinished, allowSortingUpdates, isUnderAboveItem, underChain, hidden;
        private float initialShadowAlpha;
        public SpriteRenderer BaseRenderer => baseView;
        public SpriteRenderer ShadowRenderer => shadow;
        public SpriteRenderer FirstPart => part1;
        public SpriteRenderer SecondPart => part2;
        public Animator Animator => animator;
        public Transform VerticalAnimation => verticalAnimation;
        public Transform HorizontalAnimation => horizontalAnimation;
        public bool CreationAnimationFinished => creationAnimationFinished;
        public bool IsUnderChain => underChain;
        public float PropellerComboShadowAlpha=>assets.propellerComboShadowAlpha;
        public override int GetPoolId() => 8; // 0x27bbae8.
        public override void OnSpawn() { } // 0x27bbaf0.
        private void Awake() { initialShadowAlpha = shadow.color.a; } // 0x27b4be4.

        public void Initialize(IOriginalRocketItemViewDelegate model, Transform parent, Vector3 position,
            OriginalRocketViewAssets assets, IOriginalRocketViewServices services) // 0x27ba608.
        {
            viewDelegate = model;
            this.services = services;
            this.assets = assets;
            // ItemView.InitTransform 0x2b41e40 / RootTransform.Reset 0x2b51d30.
            transform.SetParent(parent);
            transform.localPosition = position;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            SwapAnimationEnded();
            resetView.localPosition = Vector3.zero;
            resetView.localScale = Vector3.one;
            resetView.localRotation = Quaternion.identity;
            if (hidden) Show();
            isUnderAboveItem = model.IsUnderHoney() || model.IsUnderRoof();
            underChain = model.IsUnderChain();
            // GetAtlasId=-1, IsReverseSorted=false, ShouldSortByCell=false.
            // The native current-cell query still precedes its sorting lookup.
            var cell = model.CurrentCell;
            allowSortingUpdates = true;
            UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer));
            if (transfer == null) transfer = new OriginalSpecialCreationParticleTransfer(transform, services.Pool);
            if (model.Orientation == 0) strategy = new OriginalHorizontalRocketCreationStrategy(this, assets, services);
            else if (services.IsWaterMode) strategy = new OriginalWaterVerticalRocketCreationStrategy(this, assets, services);
            else strategy = new OriginalVerticalRocketCreationStrategy(this, assets, services);
            strategy.SetSprites();
            creationAnimationFinished = true;
            baseView.gameObject.SetActive(true);
            horizontalAnimation.gameObject.SetActive(false);
            verticalAnimation.gameObject.SetActive(false);
        }
        private void Update() { ManualUpdate(); }
        public void ReinitializeOrientation(IOriginalRocketItemViewDelegate model)
        {
            // Native model GetViewPosition reads the world-space view center,
            // then calls the same Init path (whose transform input is local).
            var position = transform.position;
            Initialize(model, transform.parent, position, assets, services);
        }
        public void ManualUpdate() // 0x27bb9a8.
        {
            if (creationAnimationFinished) return;
            if (viewDelegate == null) throw new NullReferenceException();
            if (viewDelegate.CurrentCell != null && !strategy.IsCreationAnimationPlaying()) StopCreationAnimation();
        }
        public OriginalSwapSorting GetCreationSorting() =>
            new OriginalSwapSorting(services.ItemSortingLayer, 9100, false); // 0x27bba90 / 0x12d84c4.
        public void PlayCreationAnimation(float normalizedStartTime, bool playSound) // 0x27bbb54.
        {
            creationAnimationFinished = false;
            baseView.gameObject.SetActive(false);
            UpdateSortingForAnimation(GetCreationSorting(), false);
            strategy.PlayCreationAnimation(normalizedStartTime, playSound);
        }
        public void StopCreationAnimation() // 0x27bbcd0.
        {
            if (creationAnimationFinished) return;
            creationAnimationFinished = true;
            if (viewDelegate != null && viewDelegate.CurrentCell != null)
            {
                if (viewDelegate.CurrentCell == null) throw new NullReferenceException();
                UpdateSortingForAnimation(OriginalItemSortingRules.Default(services.ItemSortingLayer), true);
            }
            baseView.gameObject.SetActive(true);
            horizontalAnimation.gameObject.SetActive(false);
            verticalAnimation.gameObject.SetActive(false);
        }
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) // 0x27bbf70 / 0x2b4b288.
        {
            if (under && !creationAnimationFinished)
            {
                if (viewDelegate == null || viewDelegate.CurrentCell == null) throw new NullReferenceException();
                UpdateSortingForAnimation(OriginalItemSortingRules.Default(services.ItemSortingLayer), true);
                transfer.DecreaseCreationParticleSorting();
            }
            isUnderAboveItem = under;
            var cell = viewDelegate == null ? null : viewDelegate.CurrentCell;
            UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer));
        }
        public void UpdateSorting(OriginalSwapSorting value) // ItemView 0x2b4ac40.
        {
            if (!allowSortingUpdates) return;
            sortingGroup.sortingLayerID = value.Layer;
            sortingGroup.sortingOrder = value.Order;
        }
        public OriginalSwapSorting GetSorting() => new OriginalSwapSorting(sortingGroup.sortingLayerID, sortingGroup.sortingOrder, false);
        public void UpdateSortingForAnimation(OriginalSwapSorting value, bool allowOtherUpdates) // 0x2b4ac00.
        {
            allowSortingUpdates = true;
            UpdateSorting(value);
            allowSortingUpdates = allowOtherUpdates;
        }
        public void SetGameObjectActive(bool active) { gameObject.SetActive(active); }
        public void SetCreationParticles(OriginalSpecialItemCreationParticles particles, bool isAboveStaticSpreading)
        {
            transfer.SetCreationParticles(particles, isAboveStaticSpreading, isUnderAboveItem);
        }
        public void Hide() { resetView.gameObject.SetActive(false); hidden = true; }
        public override void Show() { resetView.gameObject.SetActive(true); hidden = false; }
        public void ChangeAlpha(float alpha)
        {
            var color = baseView.color; color.a = alpha; baseView.color = color;
            color = shadow.color; color.a = alpha; shadow.color = color;
        }
        public void ResetAlpha()
        {
            var color = baseView.color; color.a = 1f; baseView.color = color;
            color = shadow.color; color.a = initialShadowAlpha; shadow.color = color;
        }
        public override void OnRecycle() // 0x27bbaf4 deliberately does not call a base reset.
        {
            viewDelegate = null;
            strategy.SetSprites();
            baseView.color = Color.white;
        }
    }
}
