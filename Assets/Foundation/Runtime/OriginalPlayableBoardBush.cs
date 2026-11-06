using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard
    {
        private const int GrassGoal = 13;
        private const int GrassSpreadOperation = 12;
        internal float GameplayDeltaTime => board.Clock.DeltaTime;

        internal void CreateBushView(OriginalBushBoardItem item, OriginalSwapPoint position)
        {
            var view = Pool.Spawn<OriginalBushItemView>(62, true);
            item.BindView(view, this);
            view.Initialize(item, board, this, audio, position);
            if (board.Goals.GetGoalIndex(GrassGoal) >= 0) board.Goals.AddToPrerequisite(GrassGoal);
        }

        internal void SpreadBushGrass(OriginalGridCell origin) // 0x2bc3608, 0x2bc3810.
        {
            var center = new Vector3(origin.X + .5f, origin.Y + .5f, 0f);
            for (int x = -1; x <= 2; x++)
            {
                int rowIndex = 0;
                for (int y = -1; y <= 2; y++)
                {
                    var cell = board.Grid.GetCell(origin.X + x, origin.Y + y);
                    if (cell == null) continue;
                    int yIndex = rowIndex++;
                    if (!cell.CanReceiveGrass() ||
                        cell.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem existing && existing.LayerCount == 2) continue;
                    var spread = Pool.Spawn<OriginalGrassSpreadView>(64, true);
                    var sprite = board.Presentation.GrassAssets.GetBase(2, generalRandom);
                    cell.IncomingGrassCount++;
                    State.Operations.OperationStart(GrassSpreadOperation);
                    spread.Play(this, board.Presentation, board.BoardRoot, center, cell, x + 1, yIndex, ItemSortingLayer, sprite);
                }
            }
        }

        internal IEnumerator ReleaseGrassPrerequisite() // 0x2bc40e4.
        {
            yield return CreateGameplayWait(.2f);
            board.Goals.RemoveFromPrerequisite(GrassGoal);
        }

        internal void FinishBushGrass(OriginalGridCell target, OriginalGrassSpreadView spread, Sprite sprite) // 0x2bc5520.
        {
            target.IncomingGrassCount--;
            if (target.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem existing)
            {
                existing.RefillLayers(sprite);
                existing.ItemView.ShowForSpread();
            }
            else
            {
                var grass = new OriginalGrassBoardItem(32, board.Goals, board.InitialItemAmounts, usefulItems);
                target.AddStaticItem(grass);
                board.Presentation.SpawnGrassView().Initialize(grass, board.Presentation.GrassAssets, generalRandom,
                    board.Presentation, audio, board.BoardRoot, board.Layout.CurrentBoard.Bottom, sprite);
                board.Goals.IncreaseGoal(GrassGoal);
                board.Goals.IncreaseGoalUi(GrassGoal);
            }
            State.Operations.OperationFinish(GrassSpreadOperation);
            Pool.Recycle(spread);
        }
    }

    public partial class OriginalBoardItem
    {
        public virtual bool CanReceiveGrass => true; // 0x2b46f08.
    }

    public partial class OriginalGridCell
    {
        internal int IncomingGrassCount { get; set; } // Native CellModel +0x34.
        internal bool CanReceiveGrass() => IsNormalCell && !StaticTouch.HasStaticItem(17) &&
            (Mediation.CurrentItem == null || Mediation.CurrentItem.CanReceiveGrass); // 0x319dd10.
    }

    public sealed partial class OriginalLevelPresentation
    {
        [SerializeField] private string bushAssetsPath, bushPrefabPath, bushBurstPrefabPath, grassSpreadPrefabPath;
        [SerializeField] private string bushRigPath, bushTexturePath;
        [SerializeField] private Material bushMaterialTemplate;
        internal OriginalBushVisualAssets BushVisuals { get; private set; }
        internal OriginalKingRigData BushRig { get; private set; }
        internal Material BushMaterial { get; private set; }

        internal void PrepareBushPools(int amount) // 0x2bc4488.
        {
            if (amount == 0) return;
            if (BushMaterial == null)
            {
                BushVisuals = Resources.Load<OriginalBushVisualAssets>(bushAssetsPath);
                var asset = Resources.Load<TextAsset>(bushRigPath);
                var atlas = Resources.Load<Texture2D>(bushTexturePath);
                if (BushVisuals == null || asset == null || atlas == null)
                    throw new InvalidOperationException("Original bush assets are missing.");
                using (var stream = new MemoryStream(asset.bytes, false)) BushRig = new OriginalKingRigData(stream, "3.8.99");
                Resources.UnloadAsset(asset);
                BushMaterial = new Material(bushMaterialTemplate) { mainTexture = atlas };
            }
            Pool.CreatePool(LoadPrefab(bushPrefabPath).GetComponent<OriginalBushItemView>(), amount);
            Pool.CreatePool(LoadPrefab(bushBurstPrefabPath).GetComponent<OriginalBushBurstParticles>(), 1);
            Pool.CreatePool(LoadPrefab(grassSpreadPrefabPath).GetComponent<OriginalGrassSpreadView>(), amount * 10);
        }

        private void ReleaseBushMaterial()
        {
            if (BushMaterial != null) Destroy(BushMaterial);
        }
    }
}
