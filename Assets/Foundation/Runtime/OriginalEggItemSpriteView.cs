using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalEggItemSpriteView : OriginalItemSwapView, IOriginalFallItemView, IOriginalItemFallSortingSurface, IOriginalTutorialHighlightView
    {
        [SerializeField] private OriginalRootTransform rootTransform;
        [SerializeField] private SortingGroup sortingGroup;
        SortingGroup IOriginalTutorialHighlightView.TutorialSortingGroup => sortingGroup;
        [SerializeField] private SpriteRenderer shadowView, baseView;
        [SerializeField] private float[] tapAngles;
        [SerializeField] private float tapSegmentDuration, tapMinScale, tapShadowAlpha;
        private Transform baseTransform;
        private OriginalItemFallCallbacks fallCallbacks;
        private OriginalDeltaTimeManager clock;
        private float shadowInitialAlpha;
        private int itemLayer;

        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public override int GetPoolId() => 51;
        public override void OnSpawn() { }
        private void Awake()
        {
            baseTransform = baseView.transform;
            fallCallbacks = new OriginalItemFallCallbacks(this);
        }

        internal void Initialize(OriginalEggBoardItem model, OriginalInitialBoardView board, OriginalSwapPoint position, int layer)
        {
            itemLayer = layer;
            clock = board.Clock;
            shadowInitialAlpha = shadowView.color.a;
            fallCallbacks.BindModel(model);
            transform.SetParent(board.BoardRoot, false);
            transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            rootTransform.Init();
            rootTransform.BindFallContext(board);
            SwapAnimationEnded();
            SetIsUnderNonFallBlockingAboveStaticItem(false);
        }

        public override void OnRecycle() // 0x2e4c7b0.
        {
            baseTransform.localScale = Vector3.one;
            baseTransform.localRotation = Quaternion.identity;
            SetShadowAlpha(shadowInitialAlpha);
            fallCallbacks.BindModel(null);
        }

        internal void PlayTap() => StartCoroutine(AnimateTap());

        private IEnumerator AnimateTap() // 0x2e4c9e8.
        {
            for (int segment = 0; segment < tapAngles.Length; segment++)
            {
                Quaternion from = baseTransform.localRotation;
                Quaternion to = Quaternion.Euler(0f, 0f, tapAngles[segment]);
                bool first = segment == 0, last = segment == tapAngles.Length - 1;
                float elapsed = 0f;
                while (elapsed < tapSegmentDuration)
                {
                    elapsed = Mathf.Min(elapsed + clock.DeltaTime, tapSegmentDuration);
                    float ratio = elapsed / tapSegmentDuration;
                    baseTransform.localRotation = Quaternion.Slerp(from, to, ratio);
                    if (first || last)
                    {
                        float squeeze = first ? ratio : 1f - ratio;
                        baseTransform.localScale = Vector3.one * Mathf.Lerp(1f, tapMinScale, squeeze);
                        SetShadowAlpha(Mathf.Lerp(shadowInitialAlpha, tapShadowAlpha, squeeze));
                    }
                    yield return null;
                }
            }
        }

        private void SetShadowAlpha(float value) => shadowView.color = new Color(1f, 1f, 1f, value);
        public OriginalSwapSorting GetSorting() => new OriginalSwapSorting(sortingGroup.sortingLayerID, sortingGroup.sortingOrder, false);
        public void UpdateSorting(OriginalSwapSorting sorting)
        {
            sortingGroup.sortingLayerID = sorting.Layer;
            sortingGroup.sortingOrder = sorting.Order;
        }
        public void ArrangeSelfSorting() => UpdateSorting(OriginalItemSortingRules.Default(itemLayer));
        public OriginalSwapPoint Position
        {
            get { var p = transform.position; return new OriginalSwapPoint(p.x, p.y, p.z); }
            set => SetPosition(value.X, value.Y, value.Z);
        }
        public OriginalSwapPoint LocalPosition
        {
            get { var p = transform.localPosition; return new OriginalSwapPoint(p.x, p.y, p.z); }
        }
        public bool IsUnderNonFallBlockingAboveStaticItem => fallCallbacks.IsUnderNonFallBlockingAboveStaticItem;
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool value) => fallCallbacks.SetIsUnderNonFallBlockingAboveStaticItem(value);
        public void FallAnimationStarted() => fallCallbacks.FallAnimationStarted();
        public void FallStarted() { }
        public void FallEnded() { }
        public void UpdateFallState(int state, float speed, float maxSpeed) => rootTransform.UpdateFallState(state, speed, maxSpeed);
        int IOriginalItemFallSortingSurface.GetAtlasId() => -1;
        bool IOriginalItemFallSortingSurface.ShouldSortByCell() => false;
        bool IOriginalItemFallSortingSurface.IsReverseSorted() => false;
        OriginalSwapSorting IOriginalItemFallSortingSurface.GetItemSorting(int atlas, bool byCell, int x, int y, bool reverse) =>
            OriginalItemSortingRules.Default(itemLayer);
    }
}
