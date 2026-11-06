using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable]
    public struct OriginalMatchViewPose
    {
        public Vector3 BaseOffset;
        public Vector3 ShadowParentOffset;
        public Color ShadowColor;
    }
    public interface IOriginalMatchExplosionViewServices
    {
        void PlayExplodeParticle(OriginalMatchItemSpriteView view, bool extraEmit);
        void RecycleView(OriginalMatchItemSpriteView view);
    }

    public sealed class OriginalMatchItemSpriteView : OriginalItemSwapView, IOriginalMatchExplodeAnimationView, IOriginalMatchMergeAnimationView, IOriginalPoolable, IOriginalMatchHighlightView, IOriginalMatchHighlightSurface, IOriginalFallItemView, IOriginalItemFallSortingSurface, IOriginalMatchCreatedItemView
    {
        [SerializeField] private Transform animationRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private OriginalRootTransform rootTransform;
        private readonly List<SpriteRenderer> fillMaskRenderers = new List<SpriteRenderer>(3);
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer overlayRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private Transform shadowParent;
        [SerializeField] private int itemSortingLayer;
        private OriginalMatchItemSorting sorting;
        private IOriginalMatchExplosionViewServices explosionServices;
        private OriginalMatchBoardItem model;
        private OriginalCoroutinePlayer coroutinePlayer;
        private OriginalMatchMergeSettings mergeSettings;
        private OriginalRoyalCoroutine mergeCoroutine;
        private OriginalMatchHighlight highlight;
        private OriginalItemFallCallbacks fallCallbacks;
        public bool HasMergeCoroutine => mergeCoroutine != null;
        public bool IsHighlighted => highlight.IsHighlighted;
        public bool IsHighlightActive => overlayRenderer.gameObject.activeSelf;
        private void Awake() { highlight = new OriginalMatchHighlight(this); fallCallbacks=new OriginalItemFallCallbacks(this); }
        public int MatchType => model.MatchType;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public OriginalRootTransform RootTransform => rootTransform;
        public Vector3 BaseSpritePosition => baseRenderer.transform.position;
        public override int GetPoolId() => 2; // 0x21a9914.
        public override void OnSpawn() { } // 0x21a991c is empty in the original.
        public void BindModel(OriginalMatchBoardItem item) { model = item; highlight.BindModel(item); fallCallbacks.BindModel(item); }
        public void Highlight(bool enable) => highlight.Highlight(enable);
        public void SetHighlightRatio(float ratio) => highlight.SetHighlightRatio(ratio);
        public float GetHighlightRatio() => highlight.GetHighlightRatio();
        public float GetMaxHighlightRatio() => highlight.GetMaxHighlightRatio();
        void IOriginalMatchHighlightSurface.SetHighlightActive(bool active) => overlayRenderer.gameObject.SetActive(active);
        float IOriginalMatchHighlightSurface.HighlightAlpha
        {
            get => overlayRenderer.color.a;
            set { var color = overlayRenderer.color; color.a = value; overlayRenderer.color = color; }
        }
        public void Bind(Sprite sprite, Sprite shadow, OriginalMatchViewPose pose)
        {
            BindVisuals(sprite, shadow, pose);
            InitializeTransform();
        }
        public void Initialize(OriginalMatchBoardItem item, Transform parent, OriginalSwapPoint position,
            Sprite sprite, Sprite shadow, OriginalMatchViewPose pose)
        {
            BindModel(item);
            BindVisuals(sprite, shadow, pose);
            transform.SetParent(parent);
            transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            InitializeTransform();
        }
        private void BindVisuals(Sprite sprite, Sprite shadow, OriginalMatchViewPose pose)
        {
            if (animationRoot == null || visualRoot == null || baseRenderer == null || overlayRenderer == null || shadowRenderer == null || shadowParent == null)
                throw new InvalidOperationException("Match sprite renderers are not configured.");
            baseRenderer.sprite = sprite;
            overlayRenderer.sprite = sprite;
            shadowRenderer.sprite = shadow;
            // MatchItemView.Init 0x21a5094 offsets are prefab configuration.
            baseRenderer.transform.localPosition = pose.BaseOffset;
            shadowParent.localPosition = pose.ShadowParentOffset;
            shadowRenderer.color = pose.ShadowColor;
            overlayRenderer.color = new Color(1f, 1f, 1f, 0f);
            shadowRenderer.enabled = true;
            transform.localRotation = Quaternion.identity;
        }
        private void InitializeTransform()
        {
            // ItemView.InitTransform 0x2b41e40 and RootTransform.Reset 0x2b51d30.
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            SwapAnimationEnded();
            rootTransform.Init();
            // The recovered prefab owns the Item sorting-layer reference.
            // InitialBoardView rejects static items, so its initial pieces are
            // not under a chain. Static-item initialization is restored separately.
            sorting = new OriginalMatchItemSorting(baseRenderer, overlayRenderer, shadowRenderer,
                itemSortingLayer, false);
            sorting.Initialize();
        }
        public void BindExplosionServices(IOriginalMatchExplosionViewServices services)
        {
            explosionServices = services ?? throw new ArgumentNullException(nameof(services));
        }
        public void StartExplodeAnimation(int trigger)
        {
            if (explosionServices == null) throw new InvalidOperationException("Match explosion presentation is not bound.");
            StartCoroutine(new OriginalMatchExplodeAnimation(this, GetLevelDeltaTime, CreateWait, trigger));
        }
        public void BindMergeServices(OriginalCoroutinePlayer player, OriginalMatchMergeSettings settings)
        {
            coroutinePlayer = player ?? throw new ArgumentNullException(nameof(player));
            mergeSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        public OriginalSwapPoint Position
        {
            get { var p = transform.position; return new OriginalSwapPoint(p.x, p.y, p.z); }
            set { transform.position = new Vector3(value.X, value.Y, value.Z); }
        }
        public void ExplodeAndMoveToPosition(Vector3 target, int index, Action onComplete) // 0x21a66d4.
        {
            if (coroutinePlayer == null || mergeSettings == null) throw new InvalidOperationException("Match merge services are not bound.");
            UpdateSorting(OriginalItemSortingRules.SpecialCreation(itemSortingLayer, index));
            var routine = OriginalRoyalCoroutine.Create(new OriginalMatchMergeAnimation(this, GetLevelDeltaTime,
                mergeSettings, new OriginalSwapPoint(target.x, target.y, target.z), true, onComplete));
            // Unity can run the completion before StartCoroutine returns its handle.
            mergeCoroutine = routine;
            coroutinePlayer.StartCoroutine(routine);
        }
        public void StopMergeCoroutine()
        {
            var routine = mergeCoroutine;
            mergeCoroutine = null;
            routine?.Kill();
        }
        public OriginalSwapPoint RootScale
        {
            get
            {
                var scale = animationRoot.localScale;
                return new OriginalSwapPoint(scale.x, scale.y, scale.z);
            }
            set { animationRoot.localScale = new Vector3(value.X, value.Y, value.Z); }
        }
        public void PlayExplodeParticle(bool extraEmit) { explosionServices.PlayExplodeParticle(this, extraEmit); }
        public void HideView() { rootTransform.HideView(); }
        public void BindFallContext(IOriginalFallSquashContext context) => rootTransform.BindFallContext(context);
        public void UpdateFallState(int state, float previousSpeed, float maxSpeed) => rootTransform.UpdateFallState(state,previousSpeed,maxSpeed);
        public OriginalSwapPoint LocalPosition
        { get { var p=transform.localPosition; return new OriginalSwapPoint(p.x,p.y,p.z); } }
        public bool IsUnderNonFallBlockingAboveStaticItem => fallCallbacks.IsUnderNonFallBlockingAboveStaticItem;
        public void FallStarted() { } // Native ItemView 0x2b4b02c is empty.
        public void FallEnded() { } // Native ItemView 0x2b4b030 is empty.
        public void FallAnimationStarted() => fallCallbacks.FallAnimationStarted();
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) // MatchItemView 0x21aa1cc.
        {
            if(under)Highlight(false);
            // The shared flag is assigned before the native sorting callbacks.
            highlight.IsUnderNonFallBlockingAboveStaticItem=under;
            fallCallbacks.SetIsUnderNonFallBlockingAboveStaticItem(under);
        }
        void IOriginalFallItemView.ArrangeSelfSorting() { ArrangeSelfSorting(); }
        int IOriginalItemFallSortingSurface.GetAtlasId() => -1; // Normal MatchItemView inherits all three getters.
        bool IOriginalItemFallSortingSurface.ShouldSortByCell() => false;
        bool IOriginalItemFallSortingSurface.IsReverseSorted() => false;
        OriginalSwapSorting IOriginalItemFallSortingSurface.GetItemSorting(int atlas,bool byCell,int x,int y,bool reverse)
        {
            if(atlas!=-1 || byCell || reverse)throw new NotSupportedException("Optimized/reverse sorting is not the normal match view path.");
            return OriginalItemSortingRules.Default(itemSortingLayer); // SortingOptimizationManager 0x12dd208.
        }
        // Native ItemView queries all current children, including inactive and
        // runtime attachments. Reuse list storage instead of allocating its
        // original array. This runs only when the mask changes, not each frame.
        public override void EnableFillMask()
        {
            GetComponentsInChildren(true,fillMaskRenderers);
            for (int i=0;i<fillMaskRenderers.Count;i++) fillMaskRenderers[i].maskInteraction=SpriteMaskInteraction.VisibleOutsideMask;
            IsFillMaskEnabled=true;
        }
        public override void DisableFillMask()
        {
            GetComponentsInChildren(true,fillMaskRenderers);
            for (int i=0;i<fillMaskRenderers.Count;i++) fillMaskRenderers[i].maskInteraction=SpriteMaskInteraction.None;
            IsFillMaskEnabled=false;
        }
        public void RecycleView() { explosionServices.RecycleView(this); }
        public override void OnRecycle() // MatchItemView.OnRecycle 0x21a9920.
        {
            sorting.EnableSortingUpdates();
            overlayRenderer.gameObject.SetActive(false);
            animationRoot.localScale = Vector3.one;
            baseRenderer.transform.localPosition = Vector3.zero;
            visualRoot.localPosition = Vector3.zero;
            model = null; // Native itemViewDelegate is cleared at this point.
            highlight.BindModel(null);
            fallCallbacks.BindModel(null);
            shadowRenderer.transform.localPosition = Vector3.zero;
            var color = baseRenderer.color;
            color.a = 1f;
            baseRenderer.color = color;
            var parent = baseRenderer.transform.parent;
            parent.localScale = Vector3.one;
            parent.localPosition = baseRenderer.transform.localPosition;
        }
        private static float GetLevelDeltaTime() => OriginalLevelClock.QuantizeDelta(Time.deltaTime);
        private static object CreateWait(float seconds) => new WaitForSeconds(seconds);
        public void UpdateSorting(OriginalSwapSorting value) { sorting.UpdateSorting(value); }
        internal void SetShuffleSorting(bool recolored) // 0x21a97b4.
        {
            baseRenderer.sortingLayerID = shadowRenderer.sortingLayerID = itemSortingLayer;
            baseRenderer.sortingOrder = recolored ? 11999 : 12000;
            shadowRenderer.sortingOrder = 11900;
        }
        public OriginalSwapSorting GetSorting() => sorting.GetSorting();
        public OriginalSwapSorting ArrangeSelfSorting() => sorting.ArrangeSelfSorting();
        public void EnableSortingUpdates() { sorting.EnableSortingUpdates(); }
        public void UpdateSortingForAnimation(OriginalSwapSorting value, bool allowOtherSortingUpdates)
        {
            sorting.UpdateSortingForAnimation(value, allowOtherSortingUpdates);
        }
    }
}
