using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalColumnRefill
    {
        bool CanRefillFromColumn(int x);
    }

    // FillingCellModel state and incoming-item queues. FallManager, item/view
    // lifecycle and refill execution are restored separately.
    public sealed partial class OriginalFillingCell : OriginalGridCell
    {
        private static event Action<int> OnFirstItemEnterNewBoard;
        private readonly HashSet<OriginalBoardItem> comingFromTopBoard = new HashSet<OriginalBoardItem>();
        private Queue<OriginalBoardItem> topBoardItemQueue;
        public bool IsPredefined { get; }
        public bool CanRefill { get; set; }
        public OriginalBoardItem LastCreatedItem { get; set; }
        public bool HasCreatedItem => LastCreatedItem != null;
        public int IncomingItemCountFromTopBoard => comingFromTopBoard.Count;

        // 0x343d004. The initial board is BoardManager's initial index (+0x40).
        public OriginalFillingCell(int x, int y, int cellType, int boardId, int fillType,
            bool isPredefined, bool wallLevel, int initialBoardIndex)
            : base(x, y, cellType, boardId, fillType)
        {
            IsPredefined = isPredefined;
            CanRefill = !wallLevel || initialBoardIndex == boardId;
            if (wallLevel)
            {
                OnFirstItemEnterNewBoard -= AllowRefillForBoard;
                OnFirstItemEnterNewBoard += AllowRefillForBoard;
            }
        }

        // 0x343d1c0: matching board opens permission, never closes it.
        public void AllowRefillForBoard(int boardId)
        {
            if (BoardId == boardId && !CanRefill) CanRefill = true;
        }
        // 0x343d270 invokes the shared event; bindings are entirely code-based.
        public static void AllowRefillOnNewBoard(int boardId) => OnFirstItemEnterNewBoard?.Invoke(boardId);
        public void AddCreatedItem(OriginalBoardItem item) { LastCreatedItem = item; } // 0x343d1e4
        public void RemoveCreatedItem() { LastCreatedItem = null; } // 0x343d1ec

        // 0x343d1f8: query the fall manager before checking the local blockers.
        public bool IsReadyToRefill(IOriginalColumnRefill fallManager)
        {
            if (fallManager == null) throw new ArgumentNullException(nameof(fallManager));
            return fallManager.CanRefillFromColumn(X) && comingFromTopBoard.Count == 0 && CanRefill;
        }

        // 0x343d2dc / callback 0x343d8d0. Even duplicate registration binds a
        // new callback, as in the original; the callback only removes the item.
        public void AddItemToComingFromTopBoard(OriginalBoardItem item)
        {
            comingFromTopBoard.Add(item);
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.OnExplodeEvent += () => comingFromTopBoard.Remove(item);
        }

        // 0x343d3f4: an explicit removal opens refill permission via the shared
        // event when permission is closed, even if the item was already absent.
        public void RemoveItemComingFromTopBoard(OriginalBoardItem item)
        {
            comingFromTopBoard.Remove(item);
            if (!CanRefill) AllowRefillOnNewBoard(BoardId);
        }

        // 0x343d498: allocate the queue before both duplicate checks.
        public void AddToTopBoardItemQueue(OriginalBoardItem item)
        {
            if (topBoardItemQueue == null) topBoardItemQueue = new Queue<OriginalBoardItem>();
            if (comingFromTopBoard.Contains(item) || topBoardItemQueue.Contains(item)) return;
            topBoardItemQueue.Enqueue(item);
        }

        // 0x343d5bc: discard items without views. A null head is consumed and
        // returned immediately; it does not skip ahead to another queued item.
        public OriginalBoardItem GetQueuedItemFromTopBoard()
        {
            if (topBoardItemQueue == null) return null;
            while (topBoardItemQueue.Count > 0)
            {
                OriginalBoardItem item = topBoardItemQueue.Peek();
                if (item == null || item.HasView) return topBoardItemQueue.Dequeue();
                topBoardItemQueue.Dequeue();
            }
            return null;
        }

        // 0x343d69c.
        public bool ContainsInTopQueue(OriginalBoardItem item) => topBoardItemQueue != null && topBoardItemQueue.Contains(item);
    }
}
