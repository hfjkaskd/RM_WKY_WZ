using System;

namespace Whitebox.Foundation
{
    public sealed class OriginalGridTransform : IOriginalGridItemPopulationData
    {
        private readonly int[] preparedIds;
        private readonly OriginalMatchItemRandomizer randomizer;
        private readonly int width;
        public int[,] Items { get; }
        public int[,] OriginalIds { get; }
        public bool HasAtLeastOnePossibleMatch { get; private set; }

        // Random-fill stage of TransformToGridWithRandomMatchItems 0x1c24a78.
        // Input IDs must already have the original palette and 211..213 selections applied.
        public OriginalGridTransform(int[] preparedIds, int width, int height, int[][] sets,
            IOriginalMatchRandomSource random)
        {
            this.preparedIds = preparedIds ?? throw new ArgumentNullException(nameof(preparedIds));
            this.width = width;
            randomizer = new OriginalMatchItemRandomizer(new int[preparedIds.Length], width, sets, random);
            Items = new int[width, height];
            OriginalIds = new int[width, height];
            Randomize();
        }

        public void Randomize()
        {
            randomizer.Reset(preparedIds);
            for (int i = 0; i < preparedIds.Length; i++)
            {
                int x = i % width;
                int y = (int)((float)i / width); // Preserve original float division.
                OriginalIds[x, y] = randomizer.GetOriginalValue(i);
                int value = randomizer.GetValue(i);
                if (value == -100)
                {
                    int previous = i - 1;
                    int retry = randomizer.GetValue(previous, preparedIds[previous]);
                    if (retry != -100) Items[previous % width, (int)((float)previous / width)] = retry;
                    value = randomizer.GetValue(i);
                    if (value == -100) value = preparedIds[i];
                }
                Items[x, y] = value;
            }
            HasAtLeastOnePossibleMatch = randomizer.SecureAtLeastOnePossibleMatch(Items);
        }

        public int GetItemAtCell(int x, int y) => Items[x, y];
        public int GetItemOriginalIdAtCell(int x, int y) => OriginalIds[x, y];

        public int[] CopyItemsInGridOrder()
        {
            var result = new int[preparedIds.Length];
            for (int y = 0; y < Items.GetLength(1); y++)
            for (int x = 0; x < width; x++)
                result[OriginalLevelRules.GetIndexFromPoint(x, y, width)] = Items[x, y];
            return result;
        }
    }
}
