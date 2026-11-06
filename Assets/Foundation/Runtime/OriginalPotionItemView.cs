using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalPotionItemView : OriginalItemSwapView, IOriginalTutorialHighlightView
    {
        [SerializeField] private OriginalRootTransform rootTransform;
        [SerializeField] private SortingGroup sortingGroup;
        SortingGroup IOriginalTutorialHighlightView.TutorialSortingGroup => sortingGroup;
        [SerializeField] private Animator potionsAnimator;
        [SerializeField] private Transform potionsContainer;
        private static readonly int Idle = Animator.StringToHash("Base Layer.PotionsIdleAnimation");
        private static readonly int Shake = Animator.StringToHash("Base Layer.PotionsShakeAnimation1");
        private static readonly int Tap = Animator.StringToHash("Base Layer.PotionsTapAnimation");
        private readonly OriginalPotionBottleView[] bottles = new OriginalPotionBottleView[4];
        private readonly int[] animationOrder = { 0, 1, 2, 3 };
        private OriginalPlayableBoard game;
        private OriginalLevelPresentation presentation;
        private OriginalGameAudioHost audio;
        private OriginalGeneralLevelRandom random;

        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public override int GetPoolId() => 58;
        public override void OnSpawn() { }

        internal void Initialize(OriginalPotionBoardItem model, OriginalInitialBoardView board, OriginalPlayableBoard owner,
            OriginalGameAudioHost audioHost, OriginalGeneralLevelRandom levelRandom, OriginalPotionVisualAssets assets)
        {
            game = owner;
            presentation = board.Presentation;
            audio = audioHost;
            random = levelRandom;
            var cell = model.Mediator.CurrentCell.Owner;
            transform.SetParent(board.BoardRoot, false);
            transform.localPosition = new Vector3(cell.X, cell.Y, 0f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            rootTransform.Init();
            // PotionItemView.IsReverseSorted 0x2179b98 and ItemView.InitTransform 0x2b41e40.
            bool covered = OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItem(model) ||
                cell.StaticTouch.HasStaticItem(6);
            var sorting = covered ? OriginalItemSortingRules.Default(game.ItemSortingLayer) :
                OriginalItemSortingRules.AnimatedItem(game.ItemSortingLayer, cell.Y - presentation.CurrentBoardStartY);
            sortingGroup.sortingLayerID = sorting.Layer;
            sortingGroup.sortingOrder = sorting.Order;
            model.RestoreBottleColors(assets.bottleColors);
            for (int i = 0; i < bottles.Length; i++)
            {
                var bottle = presentation.Pool.Spawn<OriginalPotionBottleView>(59, true);
                bottle.Initialize(assets.bottleColors[i], assets, potionsContainer, i);
                bottle.gameObject.SetActive(model.HasBottle(bottle.Color));
                bottles[i] = bottle;
            }
            random.ShuffleArray(animationOrder);
            potionsAnimator.Play(Idle, 0, 0f);
        }

        internal short GetActiveBottleMask()
        {
            short mask = 0;
            for (int i = 0; i < bottles.Length; i++)
                if (bottles[i].gameObject.activeSelf) mask |= (short)(1 << i);
            return mask;
        }

        public override void OnRecycle()
        {
            for (int i = 0; i < bottles.Length; i++)
            {
                presentation.Pool.Recycle(bottles[i]);
                bottles[i] = null;
            }
        }

        internal void Damage(int color) // 0x2177b8c.
        {
            potionsAnimator.Play(Shake, 0, 0f);
            game.PlaySfx(286 + audio.Random.Next(0, 2), .04f);
            for (int i = 0; i < bottles.Length; i++)
            {
                var bottle = bottles[i];
                if (bottle.Color != color || !bottle.gameObject.activeSelf) continue;
                presentation.Particles.GetGlobalParticle<OriginalPotionBottleParticles>(60).Play(color, bottle.transform.position);
                bottle.gameObject.SetActive(false);
                break;
            }
            PlayBottles(false);
        }

        internal void Explode()
        {
            game.PlaySfx(288, .04f);
            game.PlayHaptic(18, 3, .7f, 0f, false);
            presentation.Particles.GetGlobalParticle<OriginalPotionRackParticles>(61).Play(transform.position);
            presentation.Pool.Recycle(this);
        }

        internal void PlayTap(int layer)
        {
            game.PlaySfx(layer == 1 ? 290 : 289, .04f);
            game.PlayHaptic(layer == 1 ? 25 : 38, 0, .7f, 0f, false);
            potionsAnimator.Play(Tap, 0, 0f);
            PlayBottles(true);
        }

        private void PlayBottles(bool tap)
        {
            random.ShuffleArray(animationOrder);
            int order = 0;
            for (int i = 0; i < animationOrder.Length; i++)
            {
                var bottle = bottles[animationOrder[i]];
                if (bottle.gameObject.activeSelf) bottle.ScheduleAnimation(order++, tap);
            }
        }
    }
}
