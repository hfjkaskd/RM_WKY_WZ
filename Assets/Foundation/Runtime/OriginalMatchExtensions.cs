using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalMatchGroups
    {
        private struct ExtensionCandidate
        {
            public OriginalGridCell Cell;
            public int Direction;
        }
        private readonly List<ExtensionCandidate> extensions = new List<ExtensionCandidate>(56);
        // CellNeighbors.TouchingNeighborTypes: recovered private field SHA256
        // 49C16B6F...236F8F855, referenced by native cctor 0x31955ec.
        private static readonly int[] touchingDirections = { 1, 3, 5, 7 };

        // ProcessExtensionsForAllMatches / FindAndProcessExtensionCandidates
        // 0x12e1110 / 0x12e2ba0. Gather once from the original group, then
        // process candidates in order; newly added cells do not seed a flood fill.
        public void ExtendGroups(IOriginalMatchBlockingState blocking, bool excludeFallingItems)
        {
            if (blocking == null) throw new ArgumentNullException(nameof(blocking));
            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                extensions.Clear();
                for (int i = 0; i < group.CellCount; i++)
                for (int d = 0; d < touchingDirections.Length; d++)
                {
                    int direction = touchingDirections[d];
                    var cell = group.GetCell(i).Neighbors.Get(direction);
                    if (CanExtend(cell, group, blocking, excludeFallingItems, true))
                        extensions.Add(new ExtensionCandidate { Cell = cell, Direction = direction });
                }
                for (int i = 0; i < extensions.Count; i++)
                {
                    if (group.IsFull) break;
                    var candidate = extensions[i];
                    int opposite = (candidate.Direction + 4) % 8;
                    var back = candidate.Cell.Neighbors.Get(opposite);
                    back = back == null ? null : back.Neighbors.Get(opposite);
                    bool accepted = false;
                    if (CanExtend(back, group, blocking, excludeFallingItems, false))
                    {
                        if (!group.ContainsCell(back))
                        {
                            Claim(group, back);
                            if (group.IsFull) break;
                        }
                        accepted = true;
                    }
                    var forward = candidate.Cell.Neighbors.Get(candidate.Direction);
                    if (CanExtend(forward, group, blocking, excludeFallingItems, false))
                    {
                        if (!group.ContainsCell(forward))
                        {
                            Claim(group, forward);
                            if (group.IsFull) break;
                        }
                        accepted = true;
                    }
                    if (accepted && !group.ContainsCell(candidate.Cell)) Claim(group, candidate.Cell);
                }
                var snapshot = group.CaptureSnapshot();
                for (int i = 0; i < group.CellCount; i++)
                {
                    var item = group.GetCell(i).Mediation.CurrentItem as OriginalMatchBoardItem;
                    if (item == null) throw new InvalidOperationException("Extended group has no match item.");
                    item.MatchGroup.Init(snapshot.Id, snapshot.PatternType, snapshot.MatchType, snapshot.Cells);
                }
            }
        }

        private bool CanExtend(OriginalGridCell cell, OriginalResolvedMatchGroup group,
            IOriginalMatchBlockingState blocking, bool excludeFallingItems, bool candidateOnly)
        {
            if (cell == null) return false;
            if (blocking.HasTouchBlockingItemExceptChain(cell) || blocking.HasIncomingTouchBlockingItem(cell)) return false;
            var item = cell.Mediation.CurrentItem;
            if (item == null) return false;
            if (excludeFallingItems && item.Mediator.CurrentCell != item.Mediator.NextCell) return false;
            if (!(item is OriginalMatchBoardItem match) || match.MatchType != group.MatchType) return false;
            if (!IsVisited(cell)) return true;
            return !candidateOnly && InBounds(cell) && visited[cell.X, cell.Y] == group.Id;
        }
        private void Claim(OriginalResolvedMatchGroup group, OriginalGridCell cell)
        {
            if (InBounds(cell)) visited[cell.X, cell.Y] = group.Id;
            group.AddCell(cell);
        }
    }
}
