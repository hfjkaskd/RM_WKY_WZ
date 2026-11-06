using UnityEngine;

namespace Whitebox.Foundation
{
    // GrassSpreadAnimationView 0x2bc3a68. One pooled view per destination.
    public sealed class OriginalGrassSpreadView : MonoBehaviour, IOriginalPoolable
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer[] sortingOrders;
        [SerializeField] private SpriteRenderer shadow, grassBg, topPatch, rightPatch;
        private OriginalPlayableBoard game;
        private OriginalGridCell target;
        private OriginalGrassViewAssets grass;
        private OriginalBushVisualAssets settings;
        private OriginalBushVisualAssets.SpreadMotion motion;
        private Vector3 source, destination;
        private float delay, elapsed, peak;
        private int sortingLayer;
        private bool moving;

        public int GetPoolId() => 64;
        public void OnSpawn() { }
        public void OnRecycle()
        {
            enabled = false;
            animator.enabled = false;
            target = null;
        }

        internal void Play(OriginalPlayableBoard game, OriginalLevelPresentation presentation, Transform parent,
            Vector3 source, OriginalGridCell target, int xIndex, int yIndex, int layer, Sprite grassSprite)
        {
            this.game = game;
            this.source = source;
            this.target = target;
            sortingLayer = layer;
            grass = presentation.GrassAssets;
            settings = presentation.BushVisuals;
            destination = new Vector3(target.X, target.Y, 0f);
            int group = Mathf.FloorToInt(Vector3.Distance(source, destination));
            motion = settings.GetMotion(group, xIndex, yIndex);
            delay = settings.GetDelay(group, xIndex, yIndex);
            peak = group == 0 ? settings.nearbyScale : settings.edgeScale;
            elapsed = 0f;
            moving = false;
            transform.SetParent(parent, false);
            transform.localPosition = source;
            transform.localScale = Vector3.one;
            animator.enabled = true;
            animator.speed = 0f;
            // PlayAnimationByIndex 0x2bc512c uses the edge clip only for group 2.
            animator.Play(group == 2 ? settings.edgeSpreadState : settings.innerSpreadState, 0, 0f);
            animator.Update(0f);
            grassBg.sprite = grassSprite;
            topPatch.sprite = rightPatch.sprite = grass.GetPatch(2);
            int order = OriginalGrassItemView.GrassOrder(target, 2, presentation.CurrentBoardStartY);
            for (int i = 0; i < sortingOrders.Length; i++)
                OriginalItemSortingRules.Apply(sortingOrders[i], new OriginalSwapSorting(layer, order + i, false));
            OriginalItemSortingRules.Apply(shadow, new OriginalSwapSorting(layer, OriginalGrassItemView.GrassShadowOrder(2), false));
            topPatch.enabled = rightPatch.enabled = false;
            enabled = true;
        }

        private void Update() // 0x2bc51e8.
        {
            elapsed += game.GameplayDeltaTime;
            if (elapsed >= delay) AdvanceMotion();

            float hideDistance = settings.landingHideDistance;
            if ((transform.localPosition - destination).sqrMagnitude <= hideDistance * hideDistance &&
                target.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem existing && existing.HasView && existing.LayerCount != 0)
                existing.ItemView.HideForSpread();
            OriginalGrassItemView.ArrangePatch(target.Neighbors.Get(3), rightPatch, 2, grass, sortingLayer);
            OriginalGrassItemView.ArrangePatch(target.Neighbors.Get(1), topPatch, 2, grass, sortingLayer);
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= settings.completeTime)
                game.FinishBushGrass(target, this, grassBg.sprite);
        }

        private void AdvanceMotion()
        {
            if (!moving)
            {
                animator.speed = motion.animationSpeed;
                moving = true;
            }
            float travel = Mathf.Clamp01((elapsed - delay) / motion.duration);
            transform.localPosition = Vector3.LerpUnclamped(source, destination, motion.curve.Evaluate(travel));
            float t = travel < 1f / 3f ? travel * 3f : (1f - travel) * 1.5f;
            float scale = travel < 1f / 3f
                ? Mathf.Lerp(1f, peak, t * (2f - t))
                : Mathf.Lerp(peak, 1f, 1f - t * t);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
