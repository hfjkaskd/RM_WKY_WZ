using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Whitebox.Foundation
{
    public sealed class OriginalCoinItemSpriteView : OriginalItemSwapView, IOriginalFallItemView, IOriginalItemFallSortingSurface
    {
        public OriginalRootTransform rootTransform;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public SortingGroup sortingGroup;
        public Animator animator;
        public SpriteRenderer baseView,shadowView;
        [SerializeField]private string flipAnimation;
        [SerializeField]private Color resetShadowColor;
        [SerializeField]private Vector3 resetShadowPosition;
        [SerializeField] private string idleAnimation, tapAnimation, flipNoLoopAnimation;
        [SerializeField] private float tapSpeed, flightDuration, flightDip, flightSide, flightTilt, flightShadowAlpha;
        [FormerlySerializedAs("flightEndUp")]
        [SerializeField] private float flightEndDrop;
        [SerializeField] private Vector3 flightShadowOffset;
        private int flipHash, idleHash, tapHash, flipNoLoopHash, itemLayer;
        private float flightBatchDelay, flightFlipOffset;
        private float flightFlipSpeed, flightFlipTime, secondFlightFlipAt;
        private bool flightFlipStarted;
        private Transform shadowTransform;
        private OriginalItemFallCallbacks fallCallbacks;
        private void Awake()
        {
            flipHash = Animator.StringToHash(flipAnimation);
            idleHash = Animator.StringToHash(idleAnimation);
            tapHash = Animator.StringToHash(tapAnimation);
            flipNoLoopHash = Animator.StringToHash(flipNoLoopAnimation);
            shadowTransform = shadowView.transform;
            fallCallbacks = new OriginalItemFallCallbacks(this);
        }
        public override int GetPoolId()=>68; // 0x21f3b14.
        public override void OnSpawn(){} // 0x21f3b1c.
        public override void OnRecycle()
        {
            ResetValues();
            fallCallbacks.BindModel(null);
        }
        private void ResetValues() // 0x21f3a3c.
        {
            animator.speed = 1f;
            baseView.color = Color.white;
            shadowView.color = resetShadowColor;
            shadowTransform.localPosition = resetShadowPosition;
        }
        public void Initialize(OriginalCoinBoardItem model, Transform parent, OriginalSwapPoint position, int layer,
            IOriginalFallSquashContext fallContext)
        {
            itemLayer = layer;
            fallCallbacks.BindModel(model);
            transform.SetParent(parent, false);
            transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            rootTransform.Init();
            rootTransform.BindFallContext(fallContext);
            SwapAnimationEnded();
            SetIsUnderNonFallBlockingAboveStaticItem(false);
            animator.Play(idleHash, 0, 0f);
            ResetValues();
        }
        public void PlayFlipAnimation(float normalizedTime)=>animator.Play(flipHash,0,normalizedTime);
        public void SetCollectionSorting(int layer,int order)
        {sortingGroup.sortingLayerID=layer;sortingGroup.sortingOrder=order;}
        public OriginalSwapSorting GetSorting() => new OriginalSwapSorting(sortingGroup.sortingLayerID, sortingGroup.sortingOrder, false);
        public void UpdateSorting(OriginalSwapSorting sorting) => SetCollectionSorting(sorting.Layer, sorting.Order);
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
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) => fallCallbacks.SetIsUnderNonFallBlockingAboveStaticItem(under);
        public void FallAnimationStarted() => fallCallbacks.FallAnimationStarted();
        public void FallStarted() { }
        public void FallEnded() { }
        public void UpdateFallState(int state, float speed, float maxSpeed) => rootTransform.UpdateFallState(state, speed, maxSpeed);
        int IOriginalItemFallSortingSurface.GetAtlasId() => -1;
        bool IOriginalItemFallSortingSurface.ShouldSortByCell() => false;
        bool IOriginalItemFallSortingSurface.IsReverseSorted() => false;
        OriginalSwapSorting IOriginalItemFallSortingSurface.GetItemSorting(int atlas, bool byCell, int x, int y, bool reverse) =>
            OriginalItemSortingRules.Default(itemLayer);

        public void PlayTap(int row) // 0x21f40e0.
        {
            UpdateSorting(OriginalItemSortingRules.AnimatedItem(itemLayer, row));
            animator.speed = tapSpeed;
            animator.Play(tapHash, 0, 0f);
            StartCoroutine(RestoreTapSorting());
        }
        private IEnumerator RestoreTapSorting()
        {
            while (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != tapHash) yield return null;
            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1f) yield return null;
            ArrangeSelfSorting();
        }
        public void PrepareBoardFlight()
        {
            flightBatchDelay = flightFlipOffset = 0f;
            DisableFillMask();
            transform.localScale = Vector3.one;
            rootTransform.SetScale(Vector3.one);
            shadowView.color = new Color(1f, 1f, 1f, flightShadowAlpha);
        }

        internal void SetBoardFlightTiming(float delay, float flipOffset)
        {
            flightBatchDelay = delay;
            flightFlipOffset = flipOffset;
        }

        internal bool TickFlightFlip(float deltaTime)
        {
            flightFlipTime += deltaTime;
            if (!flightFlipStarted)
            {
                PlayFlightFlip();
                flightFlipStarted = true;
            }
            if (flightFlipTime < secondFlightFlipAt) return false;
            PlayFlightFlip();
            return true;
        }

        private void PlayFlightFlip()
        {
            animator.speed = flightFlipSpeed;
            animator.Play(flipNoLoopHash, 0, flightFlipOffset);
        }

        // CoinBezierCollect 0x21f4a40. The original build locks the portrait path.
        internal IEnumerator FlyToGoal(Vector3 goal, int collectCount,
            OriginalDeltaTimeManager clock, int uiLayer, OriginalPlayableBoard game, Action arrived)
        {
            // CoinCollectHelper assigns the batch offsets in the registration
            // frame's LateUpdate. The original iterator reads them on resume.
            yield return null;

            const float slowAtGoalDistance = 3f;
            float duration = flightDuration + flightBatchDelay;
            float minSpeed = UnityEngine.Random.Range(-.05f, .05f) + 1f;
            float startSpeed = UnityEngine.Random.Range(-.05f, .05f) + 1f;
            int order = 13000 - collectCount;
            SetCollectionSorting(itemLayer, order);
            var start = transform.position;
            var end = goal + Vector3.down * flightEndDrop;
            var control = new Vector3(start.x + (goal.x < start.x ? flightSide : 0f), start.y + flightDip, 0f);
            var startShadow = shadowTransform.localPosition;
            var maxShadow = startShadow + flightShadowOffset;
            var startRotation = transform.localRotation;
            var maxRotation = Quaternion.Euler(0f, 0f, flightTilt);
            var minPoint = OriginalCubicBezierCurve.CalculateMin(start, control, end, duration);
            float maxSpeed = Mathf.Lerp(.2f, 1f, 1f - (Vector3.Distance(goal, minPoint) - 8f) / 10f);
            float speed = .2f;
            bool descending = true, uiSorting = false;
            float elapsed = 0f;
            // The native sequence inserts its two callbacks at 0 and 0.2 / speed.
            // The application animation update first sees it on the following frame.
            flightFlipSpeed = UnityEngine.Random.Range(.6f, flightDuration);
            secondFlightFlipAt = .2f / flightFlipSpeed;
            flightFlipTime = 0f;
            flightFlipStarted = false;
            game.QueueCoinFlip(this);
            while (elapsed < duration)
            {
                elapsed += clock.DeltaTime * speed;
                var point = OriginalCubicBezierCurve.GetValue(start, control, end, elapsed / duration);
                if (descending && point.y > transform.position.y) descending = false;
                transform.position = point;
                if (descending)
                {
                    float ratio = (start.y - point.y) / (start.y - minPoint.y);
                    shadowTransform.localPosition = Vector3.Lerp(startShadow, maxShadow, ratio);
                    transform.localRotation = Quaternion.Lerp(startRotation, maxRotation, ratio);
                    speed = Mathf.Lerp(startSpeed, minSpeed, ratio);
                }
                else
                {
                    float distance = end.y - minPoint.y;
                    float ratio = (point.y - minPoint.y) / distance;
                    shadowTransform.localPosition = Vector3.Lerp(maxShadow, startShadow, ratio);
                    transform.localRotation = Quaternion.Lerp(maxRotation, startRotation, 2f * ratio);
                    float threshold = (distance - slowAtGoalDistance) / distance;
                    if (ratio > threshold)
                    {
                        speed = Mathf.Lerp(maxSpeed, maxSpeed / 5f, (ratio - threshold) / (1f - threshold));
                        if (!uiSorting)
                        {
                            SetCollectionSorting(uiLayer, order + 2000);
                            uiSorting = true;
                        }
                    }
                    else if (ratio < .5f) speed = Mathf.Lerp(minSpeed, maxSpeed, 2f * ratio);
                }
                yield return null;
            }
            arrived();
        }
    }
}
