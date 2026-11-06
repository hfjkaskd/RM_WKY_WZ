using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Concrete normal-motion boundaries backed by the same formal board,
    // initial level counters, level clock, cell Transforms and GameSfx owner.
    public sealed class OriginalBoardFallMotionServices : IOriginalFallMotionServices
    {
        private readonly OriginalInitialBoardView board;
        private readonly OriginalGameSfx sfx;
        public OriginalBoardFallMotionServices(OriginalInitialBoardView board, OriginalGameSfx sfx)
        { this.board = board != null ? board : throw new ArgumentNullException(nameof(board)); this.sfx = sfx ?? throw new ArgumentNullException(nameof(sfx)); }
        public float LevelTime => board.Clock.Time;
        public float LevelDeltaTime => board.Clock.DeltaTime;
        public float MaxFallSpeed => board.IsScroll ? 28f : 20f;
        public IOriginalFallItemView GetView(OriginalBoardItem item) => item is OriginalSpecialBoardItem special ? special.FallView : item.SwapView as IOriginalFallItemView;
        public OriginalSwapPoint GetItemPosition(OriginalBoardItem item) => GetView(item).Position;
        public OriginalSwapPoint GetCellPosition(OriginalGridCell cell)
        {
            Vector3 position = cell.GetViewPosition();
            return new OriginalSwapPoint(position.x, position.y, position.z);
        }
        public bool IsInCurrentBoard(OriginalGridCell cell) => cell.BoardId == board.Layout.CurrentBoardIndex; // CellModel 0x3196c8c.
        public OriginalSwapSorting GetCrossSorting(OriginalBoardItem item) =>
            OriginalCrossFallSorting.Get(item, item is OriginalSpecialBoardItem special ? special.FallView.GetSorting().Layer :
                item is OriginalCoinBoardItem coin ? coin.ItemView.GetSorting().Layer : ((OriginalMatchItemSpriteView)item.SwapView).GetSorting().Layer);
        public bool HasNonFallBlockingAboveStaticItemInLevel() => board.InitialItemAmounts.IsThereNonFallBlockingAboveStaticItemInLevel();
        public bool IsUnderNonFallBlockingAboveStaticItem(OriginalBoardItem item) => OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItem(item);
        public bool HasStaticItem(OriginalGridCell cell, int type) => cell.StaticTouch.HasStaticItem(type);
        public void PlayFallHit() => sfx.PlayFallHit();
    }
}
