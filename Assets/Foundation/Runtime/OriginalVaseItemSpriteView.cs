using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalVaseItemSpriteView : OriginalItemSwapView, IOriginalFallItemView, IOriginalItemFallSortingSurface, IOriginalTutorialHighlightView
    {
        [SerializeField] private OriginalRootTransform rootTransform;
        [SerializeField] private SortingGroup sortingGroup;
        SortingGroup IOriginalTutorialHighlightView.TutorialSortingGroup => sortingGroup;
        [SerializeField] private SpriteRenderer baseView, shadowView;
        [SerializeField] private Transform baseViewPivotRight, baseViewPivotLeft;
        [SerializeField] private Animator vaseAnimator;
        private Transform baseTransform;
        private OriginalItemFallCallbacks fallCallbacks;
        private OriginalLevelPresentation presentation;
        private OriginalDeltaTimeManager clock;
        private OriginalVaseVisualAssets assets;
        private OriginalGameAudioHost audio;
        private IOriginalLevelSeedRandom random;
        private Vector3 initialScale;
        private float shadowInitialAlpha;
        private int itemLayer;

        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public override int GetPoolId() => 38;
        public override void OnSpawn() => vaseAnimator.enabled = false;
        public Sprite CurrentSprite => baseView.sprite;

        private void Awake()
        {
            baseTransform = baseView.transform;
            fallCallbacks = new OriginalItemFallCallbacks(this);
        }

        internal void Initialize(OriginalVaseBoardItem model, OriginalInitialBoardView board, OriginalSwapPoint position,
            int sortingLayer, OriginalGameAudioHost audioHost, IOriginalLevelSeedRandom levelRandom)
        {
            presentation = board.Presentation;
            assets = presentation.VaseVisuals;
            clock = board.Clock;
            audio = audioHost;
            random = levelRandom;
            itemLayer = sortingLayer;
            baseView.sprite = assets.layerSprites[model.LayerCount - 1];
            initialScale = baseTransform.localScale;
            shadowInitialAlpha = shadowView.color.a;
            fallCallbacks.BindModel(model);
            transform.SetParent(board.BoardRoot, false);
            transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            rootTransform.Init();
            rootTransform.BindFallContext(board);
            SwapAnimationEnded();
            SetIsUnderNonFallBlockingAboveStaticItem(false);
        }

        public override void OnRecycle() // 0x15a08f0.
        {
            SetShadowAlpha(shadowInitialAlpha);
            baseTransform.localScale = initialScale;
            baseTransform.localRotation = Quaternion.identity;
            baseViewPivotRight.localRotation = Quaternion.identity;
            baseViewPivotLeft.localRotation = Quaternion.identity;
            fallCallbacks.BindModel(null);
        }

        internal void Damage(int layer, bool tnt) // 0x15a0324.
        {
            audio.GameSfx.PlaySfx(356 + audio.Random.Next(0, 2), .04f);
            baseView.sprite = assets.layerSprites[layer - 1];
            SpawnParticles(layer);
            StartCoroutine(AnimateShake(tnt));
        }

        internal void Explode() // 0x15a0244.
        {
            audio.GameSfx.PlaySfx(354 + audio.Random.Next(0, 2), .04f);
            SpawnParticles(0);
            presentation.Pool.Recycle(this);
        }

        private void SpawnParticles(int layer) =>
            presentation.Particles.GetGlobalParticle<OriginalVaseBurstParticles>(39).Play(layer, transform.position);

        internal void PlayTap() => StartCoroutine(AnimateTap());

        private IEnumerator AnimateTap() // 0x15a156c.
        {
            Vector3 compressedScale = initialScale * assets.tapMinScale;
            for (int segment = 0; segment < assets.tapAngles.Length; segment++)
            {
                Quaternion from = baseTransform.localRotation;
                Quaternion to = Quaternion.Euler(0f, 0f, assets.tapAngles[segment]);
                bool first = segment == 0, last = segment == assets.tapAngles.Length - 1;
                float elapsed = 0f;
                while (elapsed < assets.tapDuration)
                {
                    elapsed = Mathf.Min(elapsed + clock.DeltaTime, assets.tapDuration);
                    float ratio = elapsed / assets.tapDuration;
                    if (first) baseTransform.localScale = Vector3.Lerp(initialScale, compressedScale, ratio);
                    baseTransform.localRotation = Quaternion.Slerp(from, to, ratio);
                    if (last) baseTransform.localScale = Vector3.Lerp(compressedScale, initialScale, ratio);
                    if (first) SetShadowAlpha(Mathf.Lerp(shadowInitialAlpha, assets.tapMinShadowAlpha, ratio));
                    else if (last) SetShadowAlpha(Mathf.Lerp(assets.tapMinShadowAlpha, shadowInitialAlpha, ratio));
                    yield return null;
                }
            }
            yield return null;
        }

        private IEnumerator AnimateShake(bool tnt) // 0x15a0ef4.
        {
            bool rightFirst = random.Next(0, 2) == 1;
            int variation = random.Next(0, 3);
            float angle = (assets.shakeAngleBase + variation * assets.shakeAngleStep) * (rightFirst ? 1f : -1f);
            float time = assets.shakeTimeBase + variation * assets.shakeTimeStep;
            if (tnt)
            {
                angle *= assets.tntAngleMultiplier;
                time *= assets.tntTimeMultiplier;
            }
            Transform first = rightFirst ? baseViewPivotRight : baseViewPivotLeft;
            Transform second = rightFirst ? baseViewPivotLeft : baseViewPivotRight;
            for (int segment = 0; segment < assets.shakeAngles.Length; segment++)
            {
                Transform pivot = segment == 1 ? second : first;
                float duration = assets.shakeDurations[segment] * time;
                for (int phase = 0; phase < 2; phase++)
                {
                    Quaternion from = pivot.localRotation;
                    Quaternion to = phase == 0 ? Quaternion.Euler(0f, 0f, assets.shakeAngles[segment] * angle) : Quaternion.identity;
                    bool easeReturn = phase == 1 && segment == assets.shakeAngles.Length - 1;
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed = Mathf.Min(elapsed + clock.DeltaTime, duration);
                        float ratio = elapsed / duration;
                        // The final return uses ManualEasing.Sine.Out at 0x15a143c.
                        if (easeReturn) ratio = Mathf.Sin(ratio * Mathf.PI * .5f);
                        pivot.localRotation = Quaternion.Slerp(from, to, ratio);
                        yield return null;
                    }
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
