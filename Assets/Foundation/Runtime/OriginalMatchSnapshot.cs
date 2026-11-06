using System;

namespace Whitebox.Foundation
{
    public interface IOriginalSwapMatchGroup
    {
        bool IsValid { get; }
        bool ContainsItem(OriginalBoardItem item);
        void FreezeCells();
        void CopySnapshotTo(OriginalSavedSwapGroup destination);
    }
    // Native Cell14 is inline value storage. An array here would alias later
    // searches when the MatchGroup value is copied.
    public struct OriginalMatchCells
    {
        private OriginalGridCell c0, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13;
        public int Count { get; private set; }
        public OriginalGridCell Get(int index)
        {
            // Cell14 indexers address capacity, independently of Count. Native
            // logs invalid indexes and returns null, without throwing.
            if (unchecked((uint)index) >= 14u) return null;
            switch (index)
            {
                case 0: return c0; case 1: return c1; case 2: return c2; case 3: return c3;
                case 4: return c4; case 5: return c5; case 6: return c6; case 7: return c7;
                case 8: return c8; case 9: return c9; case 10: return c10; case 11: return c11;
                case 12: return c12; default: return c13;
            }
        }
        public void Set(int index, OriginalGridCell cell) // Cell14.set_Item 0x12de7cc.
        {
            if (unchecked((uint)index) >= 14u) return; // Native diagnostic log omitted.
            switch (index)
            {
                case 0: c0 = cell; break; case 1: c1 = cell; break; case 2: c2 = cell; break;
                case 3: c3 = cell; break; case 4: c4 = cell; break; case 5: c5 = cell; break;
                case 6: c6 = cell; break; case 7: c7 = cell; break; case 8: c8 = cell; break;
                case 9: c9 = cell; break; case 10: c10 = cell; break; case 11: c11 = cell; break;
                case 12: c12 = cell; break; case 13: c13 = cell; break;
            }
        }
        public void Add(OriginalGridCell cell) // 0x12de9a0: increment follows setter.
        { Set(Count, cell); Count = unchecked(Count + 1); }
    }

    // MatchGroup ctor/Init/Reset 0x12def34/0x12defe4/0x12df088.
    public struct OriginalMatchGroupSnapshot
    {
        public int PatternType;
        public int MatchType;
        public OriginalMatchCells Cells;
        public byte Id;
        public OriginalGridCell SpecialItemCreationCell;
        public bool ExplosionCalculated;
        public bool CanExplode;
        public bool Exists;

        public OriginalMatchGroupSnapshot(byte id, int pattern, int color) : this()
        {
            Id = id;
            PatternType = pattern;
            MatchType = color;
            Exists = true;
        }
        public void Init(byte id, int pattern, int color, OriginalMatchCells cells)
        {
            Id = id;
            PatternType = pattern;
            MatchType = color;
            Cells = cells;
            SpecialItemCreationCell = null;
            ExplosionCalculated = false;
            CanExplode = false;
            // Native Init leaves exists unchanged.
        }
        public void Reset()
        {
            PatternType = 0;
            MatchType = 0;
            Id = 0;
            SpecialItemCreationCell = null;
            ExplosionCalculated = false;
            CanExplode = false;
            Exists = false;
            // Native Reset preserves inline cell storage and its count.
        }
    }

    public interface IOriginalMatchFreezeOperations
    {
        void FreezeFall(OriginalGridCell cell);
    }
    // Two owned instances are retained by a swap action. Copies do not allocate
    // arrays, box structs or retain the mutable pooled group container.
    public sealed class OriginalSavedSwapGroup : IOriginalSwapMatchGroup
    {
        private IOriginalMatchFreezeOperations freeze;
        public OriginalMatchGroupSnapshot Snapshot { get; private set; }
        public bool IsValid => Snapshot.Exists;
        public void CopyFrom(OriginalMatchGroupSnapshot snapshot, IOriginalMatchFreezeOperations freeze)
        {
            Snapshot = snapshot;
            this.freeze = freeze;
        }
        public void CopySnapshotTo(OriginalSavedSwapGroup destination) => destination.CopyFrom(Snapshot, freeze);
        internal void ReplaceSnapshot(OriginalMatchGroupSnapshot snapshot) { Snapshot = snapshot; }
        internal void SetSpecialItemCreationCell(OriginalGridCell cell)
        {
            var value = Snapshot;
            value.SpecialItemCreationCell = cell;
            Snapshot = value;
        }
        public bool ContainsItem(OriginalBoardItem item)
        {
            if (!Snapshot.Exists) return false;
            if (item == null) throw new NullReferenceException();
            if (!(item is OriginalMatchBoardItem match)) throw new InvalidOperationException("Expected a match item.");
            return Snapshot.Id == match.NativeMatchGroupId;
        }
        public void FreezeCells()
        {
            if (freeze == null) throw new InvalidOperationException("Freeze operations are not bound.");
            var cells = Snapshot.Cells;
            for (int i = 0; i < cells.Count; i++) freeze.FreezeFall(cells.Get(i));
        }
    }
}
