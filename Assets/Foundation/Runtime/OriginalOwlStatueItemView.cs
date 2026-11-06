using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalOwlStatueItemView : OriginalItemSwapView
    {
        [SerializeField] private OriginalRootTransform rootTransform;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private SpriteRenderer baseView;
        private Transform pivot;
        private OriginalInitialBoardView board;
        private OriginalOwlStatueVisualAssets assets;
        private OriginalGameAudioHost audio;
        private OriginalPlayableBoard game;

        protected override OriginalRootTransform RecycleRoot => rootTransform;
        public override int GetPoolId() => 53;
        public override void OnSpawn() { }
        public override void OnRecycle() { }
        public Sprite CurrentSprite => baseView.sprite;

        private void Awake() => pivot = rootTransform.transform;

        internal void Initialize(OriginalOwlStatueBoardItem item, OriginalInitialBoardView board,
            OriginalPlayableBoard game, OriginalGameAudioHost audio, OriginalSwapPoint position)
        {
            this.board = board;
            this.game = game;
            this.audio = audio;
            assets = board.Presentation.OwlStatueVisuals;
            baseView.sprite = assets.layerSprites[item.LayerCount - 1];
            transform.SetParent(board.BoardRoot, false);
            transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            rootTransform.Init();
            SetSorting(OriginalItemSortingRules.Default(game.ItemSortingLayer));
        }

        internal void Explode() // 0x24734b0.
        {
            game.PlaySfx(260, .04f);
            SpawnParticles(0);
            board.Presentation.Pool.Recycle(this);
        }

        internal void Damage(int layer) // 0x2473540.
        {
            int firstClip = layer == 1 ? 261 : 263;
            game.PlaySfx(audio.Random.Next(firstClip, firstClip + 2), .04f);
            baseView.sprite = assets.layerSprites[layer - 1];
            SpawnParticles(layer);
        }

        private void SpawnParticles(int layer) =>
            board.Presentation.Particles.GetGlobalParticle<OriginalOwlStatueBurstParticles>(54).Play(layer, transform.position);

        internal void PlayTap(int row) // 0x24735f8.
        {
            game.PlaySfx(265, .04f);
            game.PlayHaptic(25, 0, .7f, 0f, false);
            StartCoroutine(AnimateTap(row));
        }

        private IEnumerator AnimateTap(int row) // 0x2473bbc.
        {
            int itemLayer = game.ItemSortingLayer;
            SetSorting(OriginalItemSortingRules.AnimatedItem(itemLayer, row - board.Presentation.CurrentBoardStartY));
            Vector3 compressedScale = Vector3.one * assets.tapMinScale;
            for (int segment = 0; segment < assets.tapAngles.Length; segment++)
            {
                Quaternion from = pivot.localRotation;
                Quaternion to = Quaternion.Euler(0f, 0f, assets.tapAngles[segment]);
                bool first = segment == 0, last = segment == assets.tapAngles.Length - 1;
                float elapsed = 0f;
                while (elapsed < assets.tapDuration)
                {
                    elapsed = Mathf.Min(elapsed + board.Clock.DeltaTime, assets.tapDuration);
                    float ratio = elapsed / assets.tapDuration;
                    if (first) pivot.localScale = Vector3.Lerp(Vector3.one, compressedScale, ratio);
                    pivot.localRotation = Quaternion.Slerp(from, to, ratio);
                    if (last) pivot.localScale = Vector3.Lerp(compressedScale, Vector3.one, ratio);
                    yield return null;
                }
            }
            SetSorting(OriginalItemSortingRules.Default(itemLayer));
            yield return null;
        }

        private void SetSorting(OriginalSwapSorting sorting)
        {
            sortingGroup.sortingLayerID = sorting.Layer;
            sortingGroup.sortingOrder = sorting.Order;
        }
    }
}
