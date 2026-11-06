using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // SelectMatchTileColor 0x1c24f70. One instance belongs to one prepared level.
    public sealed class OriginalMatchTileColors
    {
        private readonly Dictionary<int, int> colors = new Dictionary<int, int>();
        private readonly OriginalLevelRandomSource random;
        private readonly int mustHaveColor;
        private readonly int replacedColor;

        public OriginalMatchTileColors(OriginalLevelRandomSource random, int mustHaveColor,
            int replacedColor)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.mustHaveColor = mustHaveColor;
            this.replacedColor = replacedColor;
        }

        public int SelectMatchTileColor(int item, int[] firstSetIds)
        {
            if (colors.TryGetValue(item, out int cached)) return cached;
            if (firstSetIds == null) throw new ArgumentNullException(nameof(firstSetIds));
            var candidates = (int[])firstSetIds.Clone();
            for (int i = 0; i < candidates.Length - 1; i++)
            {
                int selected = random.DifficultyNext(i, candidates.Length);
                int value = candidates[selected];
                candidates[selected] = candidates[i];
                candidates[i] = value;
            }
            int result = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                int id = candidates[i];
                bool alreadySelected = false;
                foreach (var pair in colors)
                    if (pair.Value == id) { alreadySelected = true; break; }
                if (alreadySelected || !OriginalMatchItemRandomizer.IsMatchItem(id)) continue;
                if (replacedColor != 0 && mustHaveColor != 0 &&
                    OriginalLevelRules.ToMatchType(id) == replacedColor)
                {
                    result = mustHaveColor;
                    continue; // Original branch returns without caching if no later color succeeds.
                }
                result = id;
                colors.Add(item, result);
                break;
            }
            if (result != 0) return result;
            result = candidates[random.DifficultyNext(0, candidates.Length)];
            colors.Add(item, result);
            return result;
        }
    }
}
