using System;

namespace Whitebox.Foundation
{
    // Recovered rules shared by grid, palette and predefined-item parsing.
    // Numeric ids remain original ids, rather than enum names or presentation labels.
    public static class OriginalLevelRules
    {
        // TiledToTypeConverter.AsCellType, ARM64 RVA 0x1910eb4.
        public static int AsCellType(int tiledId)
        {
            return (unchecked((uint)tiledId) <= 17u && ((1u << (tiledId & 31)) & 0x21b81u) != 0)
                || tiledId == 320 ? 0 : 1;
        }

        // LevelParser.GetIndexFromPoint, ARM64 RVA 0x1c1f7cc.
        public static int GetIndexFromPoint(int x, int y, int width)
        {
            return unchecked(x + width * y);
        }

        // ItemExtensions.ToMatchType (0x2b45044) and ToTiledId (0x2b45054).
        // The original unsigned comparison rejects negative values as well as ids > 6.
        public static int ToMatchType(int tiledId)
        {
            return unchecked((uint)(tiledId - 1)) <= 5u ? tiledId : 0;
        }

        public static int ToTiledId(int matchType)
        {
            return unchecked((uint)(matchType - 1)) <= 5u ? matchType : 0;
        }

        // LevelParser.ParseColors, 0x1c21f80. An independent array is produced;
        // neither the decoded config nor another level's palette is modified.
        public static int[] ParseColors(int[] source, int mustHaveColor, int replacedColor)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new int[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = ReplaceGridColor(source[i], mustHaveColor, replacedColor);
            return result;
        }

        // First stage of TransformToGridWithRandomMatchItems, 0x1c24a78.
        // Random placeholders and sets must still pass through the original randomizer.
        public static int ReplaceGridColor(int tiledId, int mustHaveColor, int replacedColor)
        {
            return replacedColor != 0 && mustHaveColor != 0 && tiledId == replacedColor
                ? mustHaveColor : tiledId;
        }

        // Color replacement within ParsePredefined, 0x1c24788, is intentionally
        // expressed separately: unlike grid replacement, it converts through MatchType.
        public static int ReplacePredefinedColor(int tiledId, int mustHaveColor, int replacedColor)
        {
            return replacedColor != 0 && mustHaveColor != 0 && ToMatchType(tiledId) == replacedColor
                ? ToTiledId(mustHaveColor) : tiledId;
        }
    }
}
