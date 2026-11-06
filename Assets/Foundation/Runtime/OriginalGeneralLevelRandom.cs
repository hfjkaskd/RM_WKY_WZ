using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // LevelRandomManager's regular System.Random stream, independent of the
    // persisted difficulty stream. ResetSeed 0x1ed9160 / Shuffle 0x2686f0c.
    public sealed class OriginalGeneralLevelRandom : IOriginalLevelSeedRandom
    {
        private readonly Random random;
        public OriginalGeneralLevelRandom(IOriginalLevelSeedRandom application)
        { random = new Random(application.Next(0, 20000000)); }
        public int Next(int min, int max) => random.Next(min, max);
        public void Shuffle<T>(List<T> list)
        {
            int count = list.Count;
            for (int i = 0; i < count - 1; i++)
            { int index = Next(i, count); T value = list[index]; list[index] = list[i]; list[i] = value; }
        }

        // Native LevelRandomManager.ShuffleArray 0x260e4d8.
        internal void ShuffleArray<T>(T[] values)
        {
            for (int last = values.Length - 1; last > 0; last--)
            {
                int selected = random.Next(last + 1);
                T value = values[last];
                values[last] = values[selected];
                values[selected] = value;
            }
        }
    }
}
