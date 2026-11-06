using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalLevelPresentation
    {
        [SerializeField] private string cupboardPrefabPath, cupboardDoorPath, cupboardPlatePath, cupboardBreakPath;
        public OriginalSortedParticleDepth CupboardParticleDepth => SortedParticleDepth;

        public void PrepareCupboardPools(int amount)
        {
            Pool.CreatePool(LoadPrefab(cupboardPrefabPath).GetComponent<OriginalCupboardItemView>(), amount);
            Pool.CreatePool(LoadPrefab(cupboardDoorPath).GetComponent<OriginalCupboardDoorParticles>(), GetParticleAmount(amount));
            Pool.CreatePool(LoadPrefab(cupboardPlatePath).GetComponent<OriginalCupboardBurstParticles>(), 1);
            Pool.CreatePool(LoadPrefab(cupboardBreakPath).GetComponent<OriginalCupboardBurstParticles>(), 1);
        }
    }
}
