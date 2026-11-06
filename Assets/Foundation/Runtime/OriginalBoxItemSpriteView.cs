using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public interface IOriginalBoxViewServices
    {
        OriginalGameObjectPool Pool { get; }
        OriginalParticlesManager Particles { get; }
        int ItemSortingLayer { get; }
        float LevelDeltaTime { get; }
        int BoardStartY { get; }
        int NextApplicationRandom(int min, int max);
        void PlaySfx(int type, float delay);
        void TryCollectForStoryLevel(OriginalBoxItemSpriteView view, Vector3 position);
    }
    // BoxItemView's damage, tap, pooling and static-free initialization.
    public sealed class OriginalBoxItemSpriteView : OriginalItemSwapView, IOriginalPoolable,
        IOriginalBoxExplosionView, IOriginalBoxViewExplosionServices
    {
        public OriginalRootTransform rootTransform;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public Transform animationRoot, visualRoot;
        public SortingGroup sortingGroup;
        public SpriteRenderer baseView, shadowView;
        private OriginalBoxViewAssets assets;
        private IOriginalBoxViewServices services;
        private OriginalBoxViewExplosion explosion;
        private Transform tapPivot;
        private bool allowSortingUpdates, hidden;
        public OriginalBoxBoardItem Model { get; private set; }
        public override int GetPoolId() => 36; // 0x2bc6208.
        public override void OnSpawn() { } // 0x2bc6210.
        public override void OnRecycle() { } // 0x2bc6214, no state reset in this override.
        private void Awake()
        {
            explosion = new OriginalBoxViewExplosion(this);
            tapPivot = rootTransform.transform;
        }
        public void Initialize(OriginalBoxBoardItem model, Transform parent, Vector3 position,
            OriginalBoxViewAssets assets, IOriginalBoxViewServices services)
        {
            if (model == null || services == null || assets == null) throw new NullReferenceException();
            var cell = model.Mediator.CurrentCell;
            var above = cell?.Owner?.StaticTouch.AboveItems;
            if (above != null && above.Count != 0 && !(above.Count == 1 && above[0] is OriginalHoneyBoardItem))
                throw new NotSupportedException("Box initialization under this static item is not restored.");
            this.assets = assets; this.services = services;
            baseView.sprite = assets.GetSpriteForLayer(model.LayerCount);
            Model = model;
            transform.SetParent(parent); transform.localPosition = position;
            transform.localScale = Vector3.one; transform.rotation = Quaternion.identity;
            SwapAnimationEnded();
            animationRoot.localPosition = Vector3.zero; animationRoot.localScale = Vector3.one;
            animationRoot.localRotation = Quaternion.identity;
            if (hidden) Show();
            allowSortingUpdates = true;
            UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer));
        }
        public void Explode() => explosion.Explode();
        public void Damage(int damagedLayer) => explosion.Damage(damagedLayer);
        public void PlayTapAnimation() // 0x2bc5d70.
        {
            services.PlaySfx(39, assets.tapSoundDelay);
            StartCoroutine(TapCoroutine());
        }
        private IEnumerator TapCoroutine() // 0x2bc64ac.
        {
            int row = Model.Mediator.CurrentCell.Owner.Y - services.BoardStartY;
            UpdateSorting(OriginalItemSortingRules.AnimatedItem(services.ItemSortingLayer, row));
            var angles = assets.tapRotationDegrees;
            float duration = assets.tapPhaseDuration;
            var compressed = Vector3.one * assets.tapMinScale;
            for (int phase = 0; phase < angles.Length; phase++)
            {
                var from = tapPivot.localRotation;
                var to = Quaternion.Euler(0f, 0f, angles[phase]);
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed = Mathf.Min(elapsed + services.LevelDeltaTime, duration);
                    float fraction = elapsed / duration;
                    if (phase == 0) tapPivot.localScale = Vector3.Lerp(Vector3.one, compressed, fraction);
                    tapPivot.localRotation = Quaternion.Slerp(from, to, fraction);
                    if (phase == angles.Length - 1) tapPivot.localScale = Vector3.Lerp(compressed, Vector3.one, fraction);
                    yield return null;
                }
            }
            UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer));
            yield return null; // Native TapCoroutine state 9 ends on the following frame.
        }
        int IOriginalBoxViewExplosionServices.NextApplicationRandom(int min, int max) => services.NextApplicationRandom(min, max);
        void IOriginalBoxViewExplosionServices.PlaySfx(int type, float delay) => services.PlaySfx(type, delay);
        void IOriginalBoxViewExplosionServices.SetSpriteForLayer(int layer) => baseView.sprite = assets.GetSpriteForLayer(layer);
        void IOriginalBoxViewExplosionServices.PlayExplodeParticle(int layer) // 0x2bc6220.
        {
            var particle = services.Particles.GetGlobalParticle<OriginalBoxExplodeParticles>(37);
            particle.Play(layer, transform.position);
            if (layer == 0) services.TryCollectForStoryLevel(this, baseView.transform.position);
        }
        void IOriginalBoxViewExplosionServices.RecycleView() => services.Pool.Recycle(this);
        public void Hide() // 0x2bc6380.
        {
            visualRoot.gameObject.SetActive(false); hidden = true;
            shadowView.gameObject.SetActive(false);
        }
        public override void Show() // 0x2bc63b8.
        {
            visualRoot.gameObject.SetActive(true); hidden = false;
            shadowView.gameObject.SetActive(true);
        }
        public OriginalSwapSorting GetSorting() => new OriginalSwapSorting(sortingGroup.sortingLayerID, sortingGroup.sortingOrder, false);
        public void UpdateSorting(OriginalSwapSorting sorting) // 0x2bc63f0.
        {
            if (allowSortingUpdates)
            {
                sortingGroup.sortingLayerID = sorting.Layer;
                sortingGroup.sortingOrder = sorting.Order;
            }
            OriginalItemSortingRules.Apply(shadowView,
                new OriginalSwapSorting(baseView.sortingLayerID, unchecked(baseView.sortingOrder - 1), false));
        }
        public void UpdateSortingForAnimation(OriginalSwapSorting sorting, bool allowOtherUpdates)
        {
            allowSortingUpdates = true; UpdateSorting(sorting); allowSortingUpdates = allowOtherUpdates;
        }
    }
    public sealed partial class OriginalBoxBoardItem
    {
        public OriginalBoxItemSpriteView ItemView { get; private set; }
        public void CreateView(OriginalLevelPresentation presentation, Transform parent, Vector3 position,
            IOriginalBoxViewServices viewServices, IOriginalBoxExplosionServices explosionServices)
        {
            ItemView = presentation.SpawnBoxView();
            presentation.InitializeBoxView(ItemView, this, parent, position, viewServices);
            BindSwapView(ItemView);
            BindExplosionServices(ItemView, explosionServices);
            MarkViewCreated();
        }
    }
}
