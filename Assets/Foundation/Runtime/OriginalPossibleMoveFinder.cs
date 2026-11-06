using System;

namespace Whitebox.Foundation
{
    // Shuffle.PossibleMatch, distinct from the match-group candidate struct.
    public struct OriginalSwappableMatch
    {
        public OriginalGridCell RequiredCell, Top, Right, Bottom, Left;
        public int MinNeighborCount;
        public bool ContainsSpecialItem;
        public void SetSwappableNeighbor(OriginalGridCell cell, int direction) // 0x1ee5b88.
        {
            switch (direction) { case 1: Top = cell; break; case 3: Right = cell; break; case 5: Bottom = cell; break; case 7: Left = cell; break; }
        }
        public int GetSwappableNeighborsCount() => (Top == null ? 0 : 1) + (Right == null ? 0 : 1) + (Bottom == null ? 0 : 1) + (Left == null ? 0 : 1);
        public bool IsValid(bool acceptSpecialItem = false) // 0x1ee5bd4.
        {
            int count = GetSwappableNeighborsCount();
            return count != 0 && count >= MinNeighborCount && (!ContainsSpecialItem || acceptSpecialItem);
        }
    }
    public interface IOriginalPossibleMoveServices
    {
        bool CanEnterPossibleMatch(OriginalGridCell cell);
        bool CanEnterPossibleMatchAcceptingChain(OriginalGridCell cell);
        bool HasSpecialItem(OriginalGridCell cell);
        bool HasTouchBlockingItem(OriginalGridCell cell);
        bool HasChain(OriginalGridCell cell);
        bool IsMatchItem(OriginalBoardItem item);
        bool IsSpecialItem(OriginalBoardItem item);
        bool IsSwappable(OriginalBoardItem item);
        int GetMatchType(OriginalBoardItem item);
    }
    public sealed class OriginalPossibleMoveFinder
    {
        private readonly IOriginalPossibleMoveServices services;
        public OriginalPossibleMoveFinder(IOriginalPossibleMoveServices services) { this.services = services; }
        private static OriginalBoardItem Required(OriginalBoardItem item) => item ?? throw new NullReferenceException();
        public int CheckBoard(OriginalGridIterator iterator) // 0x1ee5c64; iterator intentionally copied by value.
        {
            iterator.Reset(); int matchItems = 0; bool lightball = false;
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                var mediation = cell.Mediation;
                if (!mediation.HasCurrentItem()) continue;
                if (services.IsSpecialItem(Required(mediation.CurrentItem)) && services.CanEnterPossibleMatch(cell))
                {
                    if (Required(mediation.CurrentItem).ItemType != 5) return 2;
                    bool adjacentSpecial = HasAnySpecialItemAround(cell);
                    lightball = true;
                    if (adjacentSpecial) return 2;
                }
                else if (services.IsMatchItem(Required(mediation.CurrentItem)) && services.CanEnterPossibleMatchAcceptingChain(cell))
                {
                    int color = services.GetMatchType(Required(mediation.CurrentItem));
                    var directions = OriginalCellNeighbors.TouchingNeighborTypes;
                    matchItems++;
                    for (int i = 0; i < directions.Length; i++)
                        if (CheckNeighborForPossibleMatches(cell, color, directions[i]).IsValid()) return 2;
                }
            }
            return lightball && matchItems > 0 ? 1 : 0;
        }
        public bool HasAnySpecialItemAround(OriginalGridCell cell) // 0x1ee5e98.
        {
            var directions = OriginalCellNeighbors.TouchingNeighborTypes;
            for (int i = 0; i < directions.Length; i++)
            {
                var neighbor = cell.Neighbors.Get(directions[i]);
                if (neighbor != null && services.CanEnterPossibleMatch(neighbor) &&
                    services.IsSpecialItem(Required(neighbor.Mediation.CurrentItem))) return true;
            }
            return false;
        }
        public OriginalSwappableMatch CheckNeighborForPossibleMatches(OriginalGridCell cell, int color, int direction) // 0x1ee5f94.
        {
            var neighbor = cell.Neighbors.Get(direction);
            if (neighbor != null && services.CanEnterPossibleMatchAcceptingChain(neighbor) &&
                services.GetMatchType(Required(neighbor.Mediation.CurrentItem)) == color)
                return CheckNeighborsForMatchType(neighbor.Neighbors.Get(direction), color, 2, direction);
            return CheckNeighborsForMatchType(neighbor, color, 3, direction);
        }
        public OriginalSwappableMatch CheckNeighborsForMatchType(OriginalGridCell cell, int color, int targetCount, int direction) // 0x1ee6068.
        {
            var result = new OriginalSwappableMatch();
            if (cell == null) return result;
            result.ContainsSpecialItem = services.HasSpecialItem(cell);
            result.MinNeighborCount = targetCount;
            if (!cell.IsNormalCell) return result;
            if (cell.Mediation.HasCurrentItem() || services.HasTouchBlockingItem(cell))
            {
                if (!services.CanEnterPossibleMatch(cell) || !services.IsSwappable(Required(cell.Mediation.CurrentItem))) return result;
            }
            var directions = OriginalCellNeighbors.TouchingNeighborTypes;
            for (int i = 0; i < directions.Length; i++)
            {
                int side = directions[i];
                var neighbor = cell.Neighbors.Get(side);
                if (neighbor == null || !services.CanEnterPossibleMatchAcceptingChain(neighbor) ||
                    services.GetMatchType(Required(neighbor.Mediation.CurrentItem)) != color) continue;
                bool checkChain = targetCount == 2 ? side != Opposite(direction)
                    : targetCount == 3 && side != direction && side != Opposite(direction);
                if (checkChain && services.HasChain(neighbor)) continue;
                result.RequiredCell = cell;
                result.SetSwappableNeighbor(neighbor, side);
            }
            return result;
        }
        private static int Opposite(int direction) => unchecked(direction + 4) % 8; // 0x319571c, signed remainder.
    }
    public interface IOriginalInitialMatchSearch { bool IsThereAnyMatch(bool excludeFallingItems); }
    // Initialization-only part of ShuffleManager. Runtime shuffle animation,
    // no-move event handling and HasOnlyLightballMove publication remain separate.
    public sealed class OriginalInitialShuffleCheck : IOriginalGameStartShuffle
    {
        private readonly OriginalPossibleMoveFinder finder;
        private readonly OriginalGridIterator iterator;
        public IOriginalInitialMatchSearch MatchManager;
        public bool OnlyLightballExists { get; private set; }
        public OriginalInitialShuffleCheck(OriginalPossibleMoveFinder finder, OriginalGridIterator iterator)
        { this.finder = finder; this.iterator = iterator; }
        public bool ShouldTryAgain() // 0x1ee6c20.
        {
            OnlyLightballExists = false;
            int result = finder.CheckBoard(iterator);
            if (result == 0) return true;
            if (result == 1) { OnlyLightballExists = true; return true; }
            if (result == 2) return (MatchManager ?? throw new NullReferenceException()).IsThereAnyMatch(false);
            return false;
        }
    }
}
