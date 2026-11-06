using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalLevelPresentation
    {
        [SerializeField] private OriginalGrassViewAssets grassAssets;
        [SerializeField] private string grassPrefabPath, grassExplodePrefabPath, grassTapPrefabPath;
        public OriginalGrassViewAssets GrassAssets => grassAssets;

        public void PrepareGrassPools(int amount)
        {
            if (amount <= 0) return;
            if (grassAssets == null) throw new InvalidOperationException("Grass assets are not configured.");
            Pool.CreatePool(LoadPrefab(grassPrefabPath).GetComponent<OriginalGrassItemView>(), amount);
            Pool.CreatePool(LoadPrefab(grassExplodePrefabPath).GetComponent<OriginalGrassExplodeParticles>(), 1);
            Pool.CreatePool(LoadPrefab(grassTapPrefabPath).GetComponent<OriginalGrassTapParticles>(), 1);
        }

        public OriginalGrassItemView SpawnGrassView() => Pool.Spawn<OriginalGrassItemView>(40, true);
    }
}
