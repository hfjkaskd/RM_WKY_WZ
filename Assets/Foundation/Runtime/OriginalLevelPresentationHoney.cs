using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalLevelPresentation
    {
        [SerializeField] private string honeyAssetsPath, honeyPrefabPath, honeyBurstPrefabPath, honeyTapPrefabPath;
        internal OriginalHoneyViewAssets HoneyAssets { get; private set; }

        internal void PrepareHoneyPools(int amount) // 0x34998c8.
        {
            HoneyAssets = Resources.Load<OriginalHoneyViewAssets>(honeyAssetsPath);
            if (HoneyAssets == null) throw new InvalidOperationException("Original honey assets are missing.");
            HoneyAssets.LoadSprites();
            Pool.CreatePool(LoadPrefab(honeyPrefabPath).GetComponent<OriginalHoneyItemView>(), amount);
            Pool.CreatePool(LoadPrefab(honeyBurstPrefabPath).GetComponent<OriginalHoneyBurstParticles>(), 1);
            Pool.CreatePool(LoadPrefab(honeyTapPrefabPath).GetComponent<OriginalHoneyTapParticles>(), 1);
        }

        internal OriginalHoneyItemView SpawnHoneyView() => Pool.Spawn<OriginalHoneyItemView>(43, true);
    }
}
