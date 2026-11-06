using System;
using System.Collections.Generic;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // PredefinedItemContainer 0x1c91298..0x1c915d8. The original owns nine
    // column queues independent of board width; dequeuing does not change
    // initial counts.
    public sealed class OriginalPredefinedItemContainer
    {
        private readonly Queue<OriginalBoardItem>[] queues = new Queue<OriginalBoardItem>[9];
        public readonly Dictionary<int, int> InitialItemCounts = new Dictionary<int, int>();

        public OriginalPredefinedItemContainer()
        {
            for (int i = 0; i < queues.Length; i++) queues[i] = new Queue<OriginalBoardItem>();
        }

        public void AddPredefinedItem(int column, OriginalBoardItem item)
        {
            // Enqueue precedes the original null dereference: preserve mutation.
            queues[column].Enqueue(item);
            if (item == null) throw new NullReferenceException();
            InitialItemCounts.TryGetValue(item.ItemType, out int count);
            InitialItemCounts[item.ItemType] = unchecked(count + 1);
        }

        public OriginalBoardItem GetPredefinedItem(int column)
        {
            var queue = queues[column];
            return queue.Count == 0 ? null : queue.Dequeue();
        }

        public int HitRequiredForLevelProgression(Func<OriginalBoardItem, int> getRequiredHits)
        {
            if (getRequiredHits == null) throw new ArgumentNullException(nameof(getRequiredHits));
            int total = 0;
            for (int i = 0; i < queues.Length; i++)
                foreach (var item in queues[i])
                {
                    if (item == null) throw new NullReferenceException();
                    total = unchecked(total + getRequiredHits(item));
                }
            return total;
        }

        internal FPredefinedItemsSaveData Capture()
        {
            var saved = new FItemModelQueueSaveData[queues.Length];
            for (int i = 0; i < queues.Length; i++)
            {
                var items = new FItemSaveData[queues[i].Count];
                int index = 0;
                foreach (var item in queues[i]) items[index++] = OriginalInitialBoardView.CaptureItem(item);
                saved[i] = new FItemModelQueueSaveData { Items = items };
            }
            return new FPredefinedItemsSaveData { Queues = saved };
        }

        internal void Restore(FPredefinedItemsSaveData data, Func<OriginalItemSettings, OriginalBoardItem> create)
        {
            for (int i = 0; i < queues.Length; i++)
                foreach (var entry in data.Queues[i].Items ?? Array.Empty<FItemSaveData>())
                {
                    var item = create(OriginalItemSettings.ForTiledId(entry.TiledId, entry.Data));
                    item.RestoreItemData(entry.Data);
                    AddPredefinedItem(i, item);
                }
        }

        public void Clear()
        {
            for (int i = 0; i < queues.Length; i++) queues[i].Clear();
            InitialItemCounts.Clear();
        }
    }
}
