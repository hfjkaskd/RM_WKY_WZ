using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalBushItemView : OriginalItemSwapView, IOriginalTutorialHighlightView
    {
        [SerializeField] private OriginalRootTransform rootTransform;
        [SerializeField] private SortingGroup sortingGroup;
        SortingGroup IOriginalTutorialHighlightView.TutorialSortingGroup => sortingGroup;
        [SerializeField] private OriginalMailRigView rig;
        [SerializeField] private Transform view;
        private OriginalPlayableBoard game;
        private OriginalLevelPresentation presentation;
        private OriginalGameAudioHost audioHost;
        public Vector3 Center => view.position;
        public OriginalMailRigView Rig => rig;

        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public override int GetPoolId() => 62;
        public override void OnSpawn() { }
        public override void OnRecycle() { }

        internal void Initialize(OriginalBushBoardItem item, OriginalInitialBoardView board,
            OriginalPlayableBoard owner, OriginalGameAudioHost audio, OriginalSwapPoint position)
        {
            game = owner;
            presentation = board.Presentation;
            audioHost = audio;
            transform.SetParent(board.BoardRoot, false);
            transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            rootTransform.Init();
            sortingGroup.sortingLayerID = game.ItemSortingLayer;
            sortingGroup.sortingOrder = OriginalItemSortingRules.AnimatedItem(game.ItemSortingLayer,
                (int)position.Y - presentation.CurrentBoardStartY).Order;
            rig.Bind(presentation.BushRig, presentation.BushMaterial);
            rig.Play(presentation.BushVisuals.DamageState(item.LayerCount), normalizedTime: 1f, initialDelta: Time.deltaTime);
        }

        internal void Damage(int layer) // 0x2bc334c.
        {
            if (layer == 0)
            {
                game.PlaySfx(40, .04f);
                game.PlaySfx(44, .04f);
            }
            else
            {
                game.PlaySfx(layer == 4 ? 41 : audioHost.Random.Next(42, 44), .04f);
                rig.Play(presentation.BushVisuals.DamageState(layer), initialDelta: Time.deltaTime);
            }
            presentation.Particles.GetGlobalParticle<OriginalBushBurstParticles>(63).Play(layer, Center);
        }

        internal void PlayTap(int layer) // 0x2bc3fc0.
        {
            game.PlaySfx(50 - layer, .04f);
            game.PlayHaptic(25, 0, .7f, 0f, false);
            rig.Play(presentation.BushVisuals.TapState(layer), initialDelta: Time.deltaTime);
        }

        internal void Explode(OriginalGridCell origin)
        {
            // Sorting.GetBushExplode 0x12dae54.
            sortingGroup.sortingLayerID = SortingLayer.NameToID("Front");
            sortingGroup.sortingOrder = 100;
            game.PlayHaptic(18, 3, .7f, 0f, false);
            game.SpreadBushGrass(origin);
            presentation.Pool.Recycle(this);
        }
    }
}
