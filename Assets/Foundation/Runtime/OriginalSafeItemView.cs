using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalSafeItemView : OriginalItemSwapView, IOriginalTutorialHighlightView
    {
        private static readonly int idle = Animator.StringToHash("Base Layer.SafeIdleAnimation");
        private static readonly int shake = Animator.StringToHash("Base Layer.SafeShakeAnimation1");
        private static readonly int firstTap = Animator.StringToHash("Base Layer.SafeFirstTapAnimation");
        private static readonly int tap = Animator.StringToHash("Base Layer.SafeCommonTapAnimation");
        private static readonly int coins = Animator.StringToHash("Base Layer.CoinsAnimation1");
        private static readonly int coinsTap = Animator.StringToHash("Base Layer.CoinsTapAnimation");
        [SerializeField] private OriginalRootTransform rootTransform;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private Animator safeAnimator, coinsAnimator;
        [SerializeField] private SpriteRenderer[] spriteRenderers;
        private OriginalLevelPresentation presentation;
        private OriginalPlayableBoard game;
        private OriginalSortedParticleDepth particleDepth;

        SortingGroup IOriginalTutorialHighlightView.TutorialSortingGroup => sortingGroup;
        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public override int GetPoolId() => 65;
        public override void OnSpawn() { }
        public override void OnRecycle() { }

        internal void Initialize(OriginalInitialBoardView board, OriginalPlayableBoard game, OriginalSwapPoint point,
            int layer, OriginalSortedParticleDepth particleDepth) // 0x2f116dc.
        {
            this.game = game;
            this.particleDepth = particleDepth;
            presentation = board.Presentation;
            transform.SetParent(board.BoardRoot, false);
            transform.localPosition = new Vector3(point.X, point.Y, point.Z);
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            rootTransform.Init();
            sortingGroup.sortingLayerID = game.ItemSortingLayer;
            sortingGroup.sortingOrder = OriginalItemSortingRules.AnimatedItem(game.ItemSortingLayer,
                (int)point.Y - presentation.CurrentBoardStartY).Order;

            coinsAnimator.gameObject.SetActive(false);
            spriteRenderers[2].gameObject.SetActive(true);
            for (int i = 0; i < spriteRenderers.Length; i++) spriteRenderers[i].enabled = i < 2 || i <= layer;
            if (layer <= 1) PlayCoins(1f);
            safeAnimator.Play(idle, 0, 0f);
        }

        internal void Damage(int layer) // 0x2f119f4.
        {
            safeAnimator.Play(shake, 0, 0f);
            game.PlaySfx(layer == 4 ? 333 : 332, .04f);
            var debris = presentation.Pool.Spawn<OriginalSafeDamageParticles>(66, true);
            debris.transform.position = transform.position;
            debris.Play(layer, particleDepth);
            spriteRenderers[layer + 1].enabled = false;
            if (layer == 1) PlayCoins(0f);
        }

        internal void Explode() // 0x2f118d8.
        {
            game.PlaySfx(331, .04f);
            game.PlayHaptic(18, 3, .7f, 0f, false);
            var burst = presentation.Pool.Spawn<OriginalSafeBreakParticles>(67, true);
            burst.transform.position = transform.position;
            burst.Play();
            presentation.Pool.Recycle(this);
        }

        internal void PlayTap(int layer)
        {
            if (layer == 5)
            {
                game.PlaySfx(334, .04f);
                game.PlayHaptic(38, 0, .7f, 0f, false);
                safeAnimator.Play(firstTap, 0, 0f);
                return;
            }
            game.PlaySfx(layer == 1 ? 336 : 335, .04f);
            game.PlayHaptic(layer == 1 ? 18 : 25, 0, .7f, 0f, false);
            if (layer == 1) coinsAnimator.Play(coinsTap, 0, 0f);
            safeAnimator.Play(tap, 0, 0f);
        }

        private void PlayCoins(float normalizedTime)
        {
            coinsAnimator.gameObject.SetActive(true);
            coinsAnimator.Play(coins, 0, normalizedTime);
        }
    }
}
