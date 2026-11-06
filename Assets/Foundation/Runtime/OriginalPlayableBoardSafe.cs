using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard
    {
        internal void CreateSafeView(OriginalSafeBoardItem item, OriginalSwapPoint position)
        {
            var view = board.Presentation.Pool.Spawn<OriginalSafeItemView>(65, true);
            view.Initialize(board, this, position, item.LayerCount, board.Presentation.SortedParticleDepth);
            item.BindView(view, this);
        }
    }

    public sealed partial class OriginalLevelPresentation
    {
        [SerializeField] private string safePrefabPath, safeDamagePrefabPath, safeBreakPrefabPath;
        private OriginalSortedParticleDepth sortedParticleDepth;
        internal OriginalSortedParticleDepth SortedParticleDepth => sortedParticleDepth ??= new OriginalSortedParticleDepth();

        internal void PrepareSafePools(int amount) // 0x2f11ee4.
        {
            if (amount == 0) return;
            int particles = GetParticleAmount(amount);
            Pool.CreatePool(LoadPrefab(safePrefabPath).GetComponent<OriginalSafeItemView>(), amount);
            Pool.CreatePool(LoadPrefab(safeDamagePrefabPath).GetComponent<OriginalSafeDamageParticles>(), particles);
            Pool.CreatePool(LoadPrefab(safeBreakPrefabPath).GetComponent<OriginalSafeBreakParticles>(), particles);
        }
    }
}
