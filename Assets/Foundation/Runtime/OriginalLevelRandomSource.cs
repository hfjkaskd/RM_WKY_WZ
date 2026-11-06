using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // LevelRandomManager: SetDifficultySeed 0x1ed9840, GetDifficultyNext 0x1ed98c8.
    // The caller supplies the seed selected by the original attempt-seed lifecycle.
    public sealed partial class OriginalLevelRandomSource : IOriginalMatchRandomSource
    {
        private Random difficultyRandom;
        public int DifficultySeed { get; private set; }
        public int DifficultySeedGenerationCount { get; private set; }

        // The original manager constructor leaves both random streams unset.
        public OriginalLevelRandomSource() { }

        public OriginalLevelRandomSource(int difficultySeed)
        {
            SetDifficultySeed(difficultySeed);
        }

        public void SetDifficultySeed(int seed)
        {
            DifficultySeed = seed;
            DifficultySeedGenerationCount = 0;
            difficultyRandom = new Random(seed);
        }

        public int DifficultyNext(int min, int max)
        {
            DifficultySeedGenerationCount = unchecked(DifficultySeedGenerationCount + 1);
            return difficultyRandom.Next(min, max);
        }

        internal void RestoreDifficultyGenerationCount(int count) // 0x1ed9b24..0x1ed9b58.
        {
            DifficultySeedGenerationCount = count;
            // Native replay consumes Next(1), without incrementing the saved
            // count. This is O(count), allocation-free after source creation.
            for (int i = 0; i < count; i++) difficultyRandom.Next(1);
        }

        // Original byte-list specialization 0x260e410.
        public byte GetRandomItemFromListBasedOnDifficulty(List<byte> candidates)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            return candidates[DifficultyNext(0, candidates.Count)];
        }

        // Original ushort-list specialization 0x260ecc4; forward swaps consume n-1 samples.
        public void ShuffleListBasedOnDifficulty(List<ushort> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            int count = values.Count;
            for (int i = 0; i < count - 1; i++)
            {
                int selected = DifficultyNext(i, count);
                ushort value = values[selected];
                values[selected] = values[i];
                values[i] = value;
            }
        }
    }
}
