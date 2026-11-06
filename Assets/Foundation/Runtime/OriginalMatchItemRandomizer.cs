using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Supplied by the recovered LevelRandomManager. No Unity/System.Random fallback.
    public interface IOriginalMatchRandomSource
    {
        byte GetRandomItemFromListBasedOnDifficulty(List<byte> candidates);
    }

    // LevelMatchItemRandomizer, original ARM64 0x1c1dddc..0x1c1ea54.
    public sealed class OriginalMatchItemRandomizer
    {
        private readonly int[] data;
        private readonly int width;
        private readonly int[] setMasks;
        private readonly IOriginalMatchRandomSource random;
        private readonly int[] counts = new int[6];
        private readonly List<byte> candidates = new List<byte>(6);

        public OriginalMatchItemRandomizer(int[] data, int width, int[][] setTiledIds,
            IOriginalMatchRandomSource random)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.width = width;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            if (setTiledIds == null) throw new ArgumentNullException(nameof(setTiledIds));
            setMasks = new int[setTiledIds.Length];
            for (int i = 0; i < setMasks.Length; i++) setMasks[i] = GetMask(setTiledIds[i]);
        }

        public static bool IsMatchItem(int value) => unchecked((uint)(value - 1)) < 6u;
        internal void Reset(int[] source)
        {
            Array.Copy(source, data, data.Length);
            Array.Clear(counts, 0, counts.Length);
        }

        public static int TiledIdToMask(int id) => 1 << unchecked(id - 1);
        public static int ExcludeId(int id, int mask) => mask & ~TiledIdToMask(id);
        public static int IncludeId(int id, int mask) => mask | TiledIdToMask(id);

        public static int GetMask(int[] tiledIds)
        {
            if (tiledIds == null) throw new ArgumentNullException(nameof(tiledIds));
            int mask = 0;
            for (int i = 0; i < tiledIds.Length; i++)
            {
                if (!IsMatchItem(tiledIds[i])) return 0;
                mask = IncludeId(tiledIds[i], mask);
            }
            return mask;
        }

        private int InitialMask(int value)
        {
            switch (value)
            {
                case 14: return setMasks[0];
                case 15: return setMasks[1];
                case 16: return setMasks[2];
                default: return 0;
            }
        }

        public int GetValue(int index)
        {
            int value = data[index];
            return ReturnRandomValue(index, value, InitialMask(value));
        }

        public int GetValue(int index, int initialValue)
        {
            int previous = data[index];
            data[index] = initialValue;
            return ReturnRandomValue(index, initialValue, ExcludeId(previous, InitialMask(initialValue)));
        }

        public int GetOriginalValue(int index) => data[index];

        private int ReturnRandomValue(int index, int value, int mask)
        {
            if (mask == 0)
            {
                if (IsMatchItem(value)) IncreaseCount(value);
                return value;
            }
            int chosen = GetRandomMatchItem(index, mask);
            if (chosen == -100) return -100;
            if (IsMatchItem(chosen))
            {
                data[index] = chosen;
                return chosen;
            }
            return value;
        }

        // Preserve the original flat-array boundary checks, including across row edges.
        // Adding conventional x-coordinate guards would change the original algorithm.
        public static int GetAllowedMask(int[] data, int width, int index, int mask)
        {
            int left = index - 1;
            int up = index - width;
            if (index - 2 >= 0 && IsMatchItem(data[left]) && data[left] == data[index - 2])
                mask = ExcludeId(data[left], mask);
            if (index - 2 * width >= 0 && IsMatchItem(data[up]) && data[up] == data[index - 2 * width])
                mask = ExcludeId(data[up], mask);
            if (left >= 1 && up >= 1 && data[up - 1] == data[up] &&
                IsMatchItem(data[up - 1]) && data[up - 1] == data[left])
                mask = ExcludeId(data[up - 1], mask);
            int right = index + 1;
            int down = index + width;
            if (index + 2 < data.Length && IsMatchItem(data[right]) && data[right] == data[index + 2])
                mask = ExcludeId(data[right], mask);
            if (index + 2 * width < data.Length && IsMatchItem(data[down]) && data[down] == data[index + 2 * width])
                mask = ExcludeId(data[down], mask);
            if (down + 1 < data.Length && data[down + 1] == data[down] &&
                IsMatchItem(data[down + 1]) && data[down + 1] == data[right])
                mask = ExcludeId(data[down + 1], mask);
            return mask;
        }

        private int GetRandomMatchItem(int index, int mask)
        {
            mask = GetAllowedMask(data, width, index, mask);
            candidates.Clear();
            for (int i = 0; i < 6; i++)
                if (((1 << i) & ~mask) == 0) candidates.Add((byte)(i + 1));
            if (candidates.Count == 0) return -100;
            byte chosen = random.GetRandomItemFromListBasedOnDifficulty(candidates);
            IncreaseCount(chosen);
            return chosen;
        }

        private void IncreaseCount(int matchItem)
        {
            counts[matchItem - 1] = unchecked(counts[matchItem - 1] + 1);
        }

        // This original method guarantees a color count >= 3, not an actual swap.
        // It traverses the transformed [x,y] grid with x as the outer dimension.
        public bool SecureAtLeastOnePossibleMatch(int[,] transformed)
        {
            int bestColor = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] > 2) return true;
                if (counts[i] != 0 && (bestColor == 0 || counts[bestColor - 1] < counts[i]))
                    bestColor = i + 1;
            }
            if (bestColor == 0) return false;
            if (transformed == null) throw new ArgumentNullException(nameof(transformed));
            int remaining = 3 - counts[bestColor - 1];
            for (int x = 0; x < transformed.GetLength(0); x++)
                for (int y = 0; y < transformed.GetLength(1); y++)
                    if (transformed[x, y] != bestColor && IsMatchItem(transformed[x, y]))
                    {
                        transformed[x, y] = bestColor;
                        if (--remaining == 0) return true;
                    }
            return false;
        }
    }
}
