using System;

namespace Whitebox.Foundation
{
    public partial class OriginalBoardItem { public virtual bool IsMatchItem => false; } // 0x2b46bb8.
    public sealed partial class OriginalMatchBoardItem { public override bool IsMatchItem => true; }
    public sealed class OriginalBoardPossibleMoveServices : IOriginalPossibleMoveServices
    {
        private readonly IOriginalMatchBlockingState blocking;
        public OriginalBoardPossibleMoveServices(IOriginalMatchBlockingState blocking) { this.blocking = blocking; }
        public bool CanEnterPossibleMatch(OriginalGridCell cell) => cell.IsNormalCell && cell.Mediation.HasCurrentItem() &&
            !cell.StaticTouch.HasTouchBlockingItem() && !blocking.HasIncomingTouchBlockingItem(cell); // 0x318ec68.
        public bool CanEnterPossibleMatchAcceptingChain(OriginalGridCell cell) => cell.IsNormalCell && cell.Mediation.HasCurrentItem() &&
            !cell.StaticTouch.HasTouchBlockingItemExceptChain() && !blocking.HasIncomingTouchBlockingItem(cell); // 0x319dcb0.
        public bool HasSpecialItem(OriginalGridCell cell) => cell.Mediation.HasCurrentItem() &&
            (cell.Mediation.CurrentItem ?? throw new NullReferenceException()).IsSpecialItem; // 0x319cd50.
        public bool HasTouchBlockingItem(OriginalGridCell cell) => cell.StaticTouch.HasTouchBlockingItem();
        public bool HasChain(OriginalGridCell cell) => cell.StaticTouch.HasStaticItem(6);
        public bool IsMatchItem(OriginalBoardItem item) => item.IsMatchItem;
        public bool IsSpecialItem(OriginalBoardItem item) => item.IsSpecialItem;
        public bool IsSwappable(OriginalBoardItem item) => item.IsSwappable;
        public int GetMatchType(OriginalBoardItem item) => item is IOriginalMatchTypeProvider match ? match.MatchType : 0;
    }
    public sealed class OriginalInitialMatchSearch : IOriginalInitialMatchSearch
    {
        private readonly OriginalMatchManager manager;
        public OriginalInitialMatchSearch(OriginalMatchManager manager) { this.manager = manager; }
        public bool IsThereAnyMatch(bool excludeFallingItems) =>
            (manager ?? throw new NullReferenceException()).IsThereAnyMatch(excludeFallingItems);
    }
}
