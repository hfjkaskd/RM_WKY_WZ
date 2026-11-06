using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Native MatchGroup membership is a byte group-id comparison against the
    // copied group on the match item, rather than a search through its cells.
    public sealed class OriginalResolvedMatchGroup : IOriginalSwapMatchGroup
    {
        private readonly OriginalGridCell[] cells = new OriginalGridCell[14];
        private readonly IOriginalMatchFreezeOperations freeze;
        public byte Id { get; private set; }
        public int PatternType { get; private set; }
        public int MatchType { get; private set; }
        public int CellCount { get; private set; }
        public bool IsValid { get; private set; }
        public bool IsFull => CellCount >= 14;
        public bool ContainsCell(OriginalGridCell cell)
        {
            for (int i = 0; i < CellCount; i++) if (cells[i] == cell) return true;
            return false;
        }
        public OriginalGridCell GetCell(int index)
        {
            if (unchecked((uint)index) >= (uint)CellCount) throw new IndexOutOfRangeException();
            return cells[index];
        }
        internal OriginalResolvedMatchGroup(IOriginalMatchFreezeOperations freeze) { this.freeze = freeze; }
        internal void Initialize(byte id, int pattern, int matchType)
        {
            Array.Clear(cells, 0, cells.Length);
            CellCount = 0;
            Id = id;
            PatternType = pattern;
            MatchType = matchType;
            IsValid = true;
        }
        internal void AddCell(OriginalGridCell cell) { cells[CellCount++] = cell; }
        public OriginalMatchGroupSnapshot CaptureSnapshot()
        {
            var snapshot = new OriginalMatchGroupSnapshot(Id, PatternType, MatchType);
            snapshot.Exists = IsValid;
            for (int i = 0; i < CellCount; i++) snapshot.Cells.Add(cells[i]);
            return snapshot;
        }
        public void CopySnapshotTo(OriginalSavedSwapGroup destination) => destination.CopyFrom(CaptureSnapshot(), freeze);
        public bool ContainsItem(OriginalBoardItem item)
        {
            if (!IsValid) return false;
            if (item == null) throw new NullReferenceException();
            if (!(item is OriginalMatchBoardItem match)) throw new InvalidOperationException("Expected a match item.");
            return Id == match.NativeMatchGroupId;
        }
        public void FreezeCells()
        {
            for (int i = 0; i < CellCount; i++) freeze.FreezeFall(cells[i]);
        }
    }

    // Visited arbitration and CreateAndAddMatchGroup 0x12e1bc8/0x12e2890.
    // Reuses group storage between searches. Results remain valid only until
    // the next Build; animation-owned snapshots are a separate lifecycle stage.
    public sealed partial class OriginalMatchGroups
    {
        private readonly byte[,] visited;
        private readonly List<OriginalResolvedMatchGroup> pool = new List<OriginalResolvedMatchGroup>();
        private readonly List<OriginalResolvedMatchGroup> groups = new List<OriginalResolvedMatchGroup>();
        private readonly IOriginalMatchFreezeOperations freeze;
        public OriginalMatchGroups(int width, int height, IOriginalMatchFreezeOperations freeze)
        {
            visited = new byte[width, height];
            this.freeze = freeze ?? throw new ArgumentNullException(nameof(freeze));
        }

        public IReadOnlyList<OriginalResolvedMatchGroup> Build(IReadOnlyList<OriginalTempMatch> threes,
            IReadOnlyList<OriginalPossibleMatch> sortedCandidates)
        {
            Array.Clear(visited, 0, visited.Length);
            groups.Clear();
            for (int i = 0; i < sortedCandidates.Count; i++)
            {
                var candidate = sortedCandidates[i];
                var first = threes[candidate.FirstTempIndex];
                if (AnyVisited(first)) continue;
                bool hasSecond = candidate.SecondTempIndex >= 0;
                var second = hasSecond ? threes[candidate.SecondTempIndex] : default(OriginalTempMatch);
                if (hasSecond && AnyVisited(second)) continue;
                int index = groups.Count;
                if (index == pool.Count) pool.Add(new OriginalResolvedMatchGroup(freeze));
                var group = pool[index];
                group.Initialize(unchecked((byte)(index + 1)), candidate.PatternType, candidate.MatchType);
                Add(group, first.First); Add(group, first.Middle); Add(group, first.Last); Add(group, first.Fourth);
                if (hasSecond)
                {
                    AddExcept(group, second.First, candidate);
                    AddExcept(group, second.Middle, candidate);
                    AddExcept(group, second.Last, candidate);
                    AddExcept(group, second.Fourth, candidate);
                }
                for (int j = 0; j < group.CellCount; j++)
                {
                    var cell = group.GetCell(j);
                    if (InBounds(cell)) visited[cell.X, cell.Y] = group.Id;
                }
                groups.Add(group);
            }
            // Original writes item copies after all candidates have been resolved.
            for (int i = 0; i < groups.Count; i++)
            {
                var snapshot = groups[i].CaptureSnapshot();
                for (int j = 0; j < groups[i].CellCount; j++)
                {
                    var item = groups[i].GetCell(j).Mediation.CurrentItem as OriginalMatchBoardItem;
                    if (item == null) throw new InvalidOperationException("Match group has no match item.");
                    item.MatchGroup.Init(snapshot.Id, snapshot.PatternType, snapshot.MatchType, snapshot.Cells);
                }
            }
            return groups;
        }
        private bool InBounds(OriginalGridCell cell) => cell.X >= 0 && cell.Y >= 0 &&
            cell.X < visited.GetLength(0) && cell.Y < visited.GetLength(1);
        private bool IsVisited(OriginalGridCell cell) => cell != null &&
            (!InBounds(cell) || visited[cell.X, cell.Y] != 0);
        private bool AnyVisited(OriginalTempMatch match) => IsVisited(match.First) ||
            IsVisited(match.Middle) || IsVisited(match.Last) || IsVisited(match.Fourth);
        private static void Add(OriginalResolvedMatchGroup group, OriginalGridCell cell)
        {
            if (cell != null) group.AddCell(cell);
        }
        private static void AddExcept(OriginalResolvedMatchGroup group, OriginalGridCell cell, OriginalPossibleMatch candidate)
        {
            if (cell != candidate.FirstIntersect && cell != candidate.SecondIntersect) Add(group, cell);
        }
    }
}
