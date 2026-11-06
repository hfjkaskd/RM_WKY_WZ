using UnityEngine;

namespace Whitebox.Foundation
{
    public static class OriginalItemSortingRules
    {
        // Sorting / SortingOptimizationManager: 0x12dd2f4, 0x12d83fc,
        // 0x12d5db4, 0x12d98a0, 0x12d9904. These are native sorting constants.
        public static OriginalSwapSorting Default(int itemLayer) => new OriginalSwapSorting(itemLayer, 1000, false);
        public static OriginalSwapSorting AnimatedItem(int itemLayer, int row) =>
            new OriginalSwapSorting(itemLayer, unchecked(7000 - 200 * row), false); // 0x12d7c10.
        public static OriginalSwapSorting Hint(int itemLayer) => new OriginalSwapSorting(itemLayer, 7010, true); // 0x12d832c.
        public static OriginalSwapSorting SwapStart(int itemLayer) => new OriginalSwapSorting(itemLayer, 9005, true);
        public static OriginalSwapSorting SpecialCreation(int itemLayer, int index) =>
            new OriginalSwapSorting(itemLayer, unchecked(9000 - index), false); // 0x12d97d0.
        public static OriginalSwapSorting WithOffset(OriginalSwapSorting sorting, int offset)
        {
            return new OriginalSwapSorting(sorting.Layer, unchecked(sorting.Order + offset), sorting.SortEverything);
        }
        public static OriginalSwapSorting MatchOverlay(int itemLayer, bool underChain)
        {
            return new OriginalSwapSorting(itemLayer, underChain ? 1010 : 9000, false);
        }

        // MatchItemSwapParticles.UpdateSorting 0x21a95d8. Renderers are cached
        // once by the particle component, not searched during animation.
        public static void UpdateParticleRenderers(Renderer[] renderers, OriginalSwapSorting sorting)
        {
            for (int i = 0; i < renderers.Length; i++) Apply(renderers[i], sorting);
        }
        internal static void Apply(Renderer renderer, OriginalSwapSorting sorting)
        {
            // SpriteExtensions.SetSorting 0x2d6e314 writes layer before order.
            renderer.sortingLayerID = sorting.Layer;
            renderer.sortingOrder = sorting.Order;
        }
    }

    // Normal MatchItemView inherits GetAtlasId=-1 and IsReverseSorted=false.
    // Its ArrangeSelfSorting therefore selects the native default item sorting.
    // Keep this state inline in the view: no allocation on sorting updates.
    public struct OriginalMatchItemSorting
    {
        private readonly SpriteRenderer baseRenderer, overlayRenderer, shadowRenderer;
        private readonly int itemLayer;
        private bool allowSortingUpdates, underChain;

        public OriginalMatchItemSorting(SpriteRenderer baseRenderer, SpriteRenderer overlayRenderer,
            SpriteRenderer shadowRenderer, int itemLayer, bool underChain)
        {
            this.baseRenderer = baseRenderer;
            this.overlayRenderer = overlayRenderer;
            this.shadowRenderer = shadowRenderer;
            this.itemLayer = itemLayer;
            this.underChain = underChain;
            allowSortingUpdates = false;
        }
        // ItemView.UpdateSortingOnInit 0x2b4abc8.
        public void Initialize()
        {
            allowSortingUpdates = true;
            UpdateSorting(OriginalItemSortingRules.Default(itemLayer));
        }
        public void EnableSortingUpdates() { allowSortingUpdates = true; } // 0x2b4abf4.
        // ItemView.UpdateSortingForAnimation 0x2b4ac00 applies once even while
        // ordinary updates are locked, then installs the requested lock state.
        public void UpdateSortingForAnimation(OriginalSwapSorting sorting, bool allowOtherSortingUpdates)
        {
            allowSortingUpdates = true;
            UpdateSorting(sorting);
            allowSortingUpdates = allowOtherSortingUpdates;
        }
        public void UpdateSorting(OriginalSwapSorting sorting) // 0x21a965c.
        {
            if (!allowSortingUpdates) return;
            OriginalItemSortingRules.Apply(baseRenderer, sorting);
            OriginalItemSortingRules.Apply(shadowRenderer,
                new OriginalSwapSorting(sorting.Layer, unchecked(sorting.Order - 1), false));
            OriginalItemSortingRules.Apply(overlayRenderer, OriginalItemSortingRules.MatchOverlay(itemLayer, underChain));
        }
        public OriginalSwapSorting GetSorting() // 0x21a9748.
        {
            return new OriginalSwapSorting(baseRenderer.sortingLayerID, baseRenderer.sortingOrder, false);
        }
        public OriginalSwapSorting ArrangeSelfSorting() // 0x2b470f4, normal match view.
        {
            var sorting = OriginalItemSortingRules.Default(itemLayer);
            UpdateSorting(sorting);
            return sorting;
        }
        public void RevealFromChain() { underChain = false; } // 0x2b4b444.
    }
}
