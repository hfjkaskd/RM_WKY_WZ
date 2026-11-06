using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public struct OriginalMatchCollectData
    {
        public OriginalMatchCells OrderedCells;
        public int CollectType, PatternOrientation, MinYIndex, MinXIndex;
        public OriginalGridCell SpecialItemCreationCell;

        public OriginalMatchCollectData(int collectType) : this() { CollectType = collectType; } // 0x21a4fac.
        public void Initialize() // 0x21ac470.
        {
            int minY = 1000, maxY = -1000, minX = 1000, maxX = -1000;
            for (int i = 0; i < OrderedCells.Count; i++)
            {
                var cell = OrderedCells.Get(i);
                if (cell == null) throw new NullReferenceException();
                if (maxX <= cell.X) maxX = cell.X;
                if (cell.X <= minX) minX = cell.X;
                if (maxY <= cell.Y) maxY = cell.Y;
                if (cell.Y < minY) { MinYIndex = i; minY = cell.Y; }
            }
            PatternOrientation = minX == maxX ? 1 : minY == maxY ? 2 : 0;
            // Native Initialize leaves MinXIndex and the other fields intact.
        }
    }

    public struct OriginalSpecialItemCreationData
    {
        public OriginalMatchCells OrderedCells;
        public int CellCount => OrderedCells.Count;
        public bool ShouldCreateSpecialItem => OrderedCells.Count > 0; // 0x21a63a4.
        public OriginalGridCell GetCreationCell() => OrderedCells.Count == 0 ? null : OrderedCells.Get(0); // 0x21a6678.
        public int GetIndex(OriginalGridCell cell) // 0x21a65d4; missing cells return zero.
        {
            for (int i = 0; i < OrderedCells.Count; i++) if (OrderedCells.Get(i) == cell) return i;
            return 0;
        }
    }

    public sealed partial class OriginalMatchBoardItem
    {
        public OriginalMatchCollectData CollectData; // Native +0x150.
        public OriginalSpecialItemCreationData SpecialItemCreationData; // Native +0x1e0.
    }

    public interface IOriginalMatchDataServices
    {
        bool HasStaticItem(OriginalGridCell cell, int staticItemType);
        // Native ShuffleCells receives an inline value copy. It consumes the
        // original random-manager operation without rewriting the saved cells.
        void ShuffleCellsBasedOnDifficulty(OriginalMatchCells cells);
    }

    // MatchManager.CreateCollectData / CreateSpecialItemData, with original
    // BottomLeftHorizontalSorter and MatchGroup creation-cell selection.
    public sealed class OriginalMatchDataPreparation
    {
        private sealed class BottomLeftComparer : IComparer<OriginalBoardItem>
        {
            public int Compare(OriginalBoardItem first, OriginalBoardItem second) // 0x319751c.
            {
                var a = CurrentCell(first);
                var b = CurrentCell(second);
                if (b.Y < a.Y) return 1;
                if (a.Y < b.Y) return -1;
                return a.X < b.X ? -1 : b.X < a.X ? 1 : 0;
            }
        }
        private static readonly IComparer<OriginalBoardItem> bottomLeft = new BottomLeftComparer();
        private readonly IOriginalMatchDataServices services;
        private readonly List<OriginalBoardItem> items = new List<OriginalBoardItem>(14);
        private readonly List<OriginalGridCell> specialCells = new List<OriginalGridCell>(14);
        private float firstDistance, comparedDistance;

        public OriginalMatchDataPreparation(IOriginalMatchDataServices services)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public OriginalMatchCollectData CreateCollectData(OriginalMatchGroupSnapshot group) // 0x12e3a98.
        {
            items.Clear();
            var data = new OriginalMatchCollectData(group.PatternType == 1 ? 1 : 2);
            for (int i = 0; i < group.Cells.Count; i++) items.Add(group.Cells.Get(i).Mediation.CurrentItem);
            items.Sort(bottomLeft);
            for (int i = 0; i < items.Count; i++) data.OrderedCells.Add(CurrentCell(items[i]));
            data.SpecialItemCreationCell = GetSpecialItemCell(ref group);
            data.Initialize();
            return data;
        }
        public OriginalSpecialItemCreationData CreateSpecialItemData(OriginalMatchGroupSnapshot group) // 0x12e3d88.
        {
            var data = default(OriginalSpecialItemCreationData);
            if (group.PatternType == 1) return data;
            var creation = GetSpecialItemCell(ref group);
            specialCells.Clear();
            specialCells.Add(creation);
            for (int i = 0; i < group.Cells.Count; i++)
            {
                var cell = group.Cells.Get(i);
                if (cell == creation || services.HasStaticItem(cell, 6)) continue;
                if (creation == null) throw new NullReferenceException();
                firstDistance = Distance(creation, cell);
                int index = 1;
                for (; index < specialCells.Count; index++)
                {
                    comparedDistance = Distance(creation, specialCells[index]);
                    if (firstDistance < comparedDistance) break;
                }
                specialCells.Insert(index, cell);
            }
            for (int i = 0; i < specialCells.Count; i++) data.OrderedCells.Add(specialCells[i]);
            return data;
        }
        public OriginalGridCell GetSpecialItemCell(ref OriginalMatchGroupSnapshot group) // 0x12df234 / 0x12df298.
        {
            if (group.SpecialItemCreationCell != null) return group.SpecialItemCreationCell;
            services.ShuffleCellsBasedOnDifficulty(group.Cells); // 0x12df454.
            float lastStateChange = 0f;
            for (int i = 0; i < group.Cells.Count; i++)
            {
                var cell = group.Cells.Get(i);
                var item = cell.Mediation.CurrentItem;
                if (item == null) throw new NullReferenceException();
                var fall = item.FallComponent as IOriginalItemFallTiming;
                if (fall == null) throw new NullReferenceException();
                float changed = fall.StateChangedTime;
                if (!services.HasStaticItem(cell, 6) &&
                    (lastStateChange < changed || group.SpecialItemCreationCell == null || group.SpecialItemCreationCell.Y < cell.Y))
                {
                    group.SpecialItemCreationCell = cell;
                    lastStateChange = changed;
                }
            }
            return group.SpecialItemCreationCell;
        }
        private static OriginalGridCell CurrentCell(OriginalBoardItem item)
        {
            if (item == null) throw new NullReferenceException();
            var cell = item.Mediator.CurrentCell;
            if (cell == null || cell.Owner == null) throw new NullReferenceException();
            return cell.Owner;
        }
        private static float Distance(OriginalGridCell first, OriginalGridCell second) // 0x343ba6c.
        {
            if (first == null || second == null) throw new NullReferenceException();
            int dx = unchecked(first.X - second.X), dy = unchecked(first.Y - second.Y);
            return (float)Math.Sqrt((float)unchecked(dx * dx + dy * dy));
        }
    }
}
