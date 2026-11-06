using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalGrassItemView : MonoBehaviour, IOriginalPoolable
    {
        public Transform viewParent;
        public SpriteRenderer baseView, topPatch, rightPatch, shadow;
        private OriginalGrassBoardItem model;
        private OriginalGrassViewAssets assets;
        private IOriginalLevelSeedRandom random;
        private OriginalLevelPresentation presentation;
        private OriginalGameAudioHost gameAudio;
        private int boardStartY, itemLayer;
        public int GetPoolId() => 40;

        public void Initialize(OriginalGrassBoardItem item, OriginalGrassViewAssets assets, IOriginalLevelSeedRandom random,
            OriginalLevelPresentation presentation, OriginalGameAudioHost audio, Transform parent, int boardStartY, Sprite spreadSprite = null)
        {
            model = item;
            this.assets = assets;
            this.random = random;
            this.presentation = presentation;
            gameAudio = audio;
            this.boardStartY = boardStartY;
            itemLayer = SortingLayer.NameToID("Item");
            item.BindView(this);
            transform.SetParent(parent, false);
            var cell = item.CurrentCell;
            transform.localPosition = new Vector3(cell.X, cell.Y, 0f);
            ShowForSpread();
            UpdateView(item.LayerCount, spreadSprite);
        }

        internal static int GrassOrder(OriginalGridCell cell, int layer, int boardStartY) =>
            -26000 + layer * 150 + cell.X + (cell.Y - boardStartY) * 10;
        internal static int GrassShadowOrder(int layer) => -26020 + layer * 150;

        private void UpdateView(int layer, Sprite spreadSprite = null)
        {
            var cell = model.CurrentCell;
            baseView.sprite = spreadSprite != null ? spreadSprite : assets.GetBase(layer, random);
            baseView.color = Color.white;
            baseView.sortingLayerID = itemLayer;
            baseView.sortingOrder = GrassOrder(cell, layer, boardStartY);
            shadow.sprite = assets.Shadow;
            shadow.color = new Color(1f, 1f, 1f, assets.GetShadowAlpha(layer));
            shadow.sortingLayerID = itemLayer;
            shadow.sortingOrder = GrassShadowOrder(layer);
            UpdatePatches();
            UpdateNeighbors(); // GrassItemView.UpdateView 0x349d430.
        }

        public void UpdatePatches()
        {
            var neighbors = model.CurrentCell.Neighbors;
            ArrangePatch(neighbors.Get(3), rightPatch, model.LayerCount, assets, itemLayer);
            ArrangePatch(neighbors.Get(1), topPatch, model.LayerCount, assets, itemLayer);
        }

        internal static void ArrangePatch(OriginalGridCell neighbor, SpriteRenderer patch, int layer,
            OriginalGrassViewAssets assets, int itemLayer)
        {
            var grass = neighbor?.StaticTouch.GetStaticItem(2) as OriginalGrassBoardItem;
            int neighborLayer = grass?.LayerCount ?? (neighbor?.IncomingGrassCount > 0 ? 2 : 0);
            if (neighborLayer == 0)
            {
                patch.enabled = false;
                return;
            }
            layer = Math.Min(layer, neighborLayer);
            patch.enabled = true;
            patch.sprite = assets.GetPatch(layer);
            // ArrangePatch 0x349e058 leaves the authored renderer color and animated alpha intact.
            patch.sortingLayerID = itemLayer;
            patch.sortingOrder = -26010 + layer * 150;
        }

        public void Damage(int damagedLayer)
        {
            UpdateView(damagedLayer);
            SpawnParticle(damagedLayer);
            UpdateNeighbors(); // Damage 0x349cfa8 refreshes again after the particle.
        }

        internal void HideForSpread()
        {
            if (viewParent.gameObject.activeSelf) viewParent.gameObject.SetActive(false);
        }

        internal void ShowForSpread()
        {
            if (!viewParent.gameObject.activeSelf) viewParent.gameObject.SetActive(true);
        }

        internal void RefillFromSpread(int layer, Sprite sprite) => UpdateView(layer, sprite);

        public void Explode()
        {
            HideForSpread();
            SpawnParticle(0);
            UpdateNeighbors();
            presentation.Pool.Recycle(this);
        }

        private void SpawnParticle(int layer)
        {
            gameAudio.GameSfx.PlaySfx(164 + gameAudio.Random.Next(0, 2), .04f);
            presentation.Particles.GetGlobalParticle<OriginalGrassExplodeParticles>(41).Play(layer, transform.position);
        }

        private void UpdateNeighbors()
        {
            var neighbors = model.CurrentCell.Neighbors;
            Notify(neighbors.Get(7));
            Notify(neighbors.Get(5));
        }

        private static void Notify(OriginalGridCell cell)
        {
            if (cell?.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem grass) grass.ItemView.UpdatePatches();
        }

        public void PlayTapParticles(int layer)
        {
            gameAudio.GameSfx.PlaySfx(166, .04f);
            var particle = presentation.Pool.Spawn<OriginalGrassTapParticles>(42, true);
            particle.Play(layer / 2);
            particle.transform.position = transform.position;
        }

        public void OnSpawn() { }
        public void OnRecycle() // 0x349e2e0.
        {
            model = null;
            rightPatch.enabled = false;
            topPatch.enabled = false;
            ShowForSpread();
        }
    }
}
