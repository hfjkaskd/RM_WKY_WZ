using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // KingAnimationStateManager's board reactions in ordinary core levels.
    internal sealed class OriginalKingBoardReaction : IOriginalKingRelief, IOriginalKingReliefServices
    {
        private static readonly int[] LayerCaps = { 1, 3, 5 }; // 0x23e9884.
        private const int LightballComboRayThreshold = 8;
        private readonly OriginalInitialBoardView board;
        private readonly OriginalPredefinedItemContainer predefined;
        private readonly OriginalFillSetGeneration generation;
        private readonly OriginalFirstBoardExplodeTargets winTargets;
        private readonly OriginalTopLeftHorizontalStrategy strategy;
        private readonly OriginalCoroutinePlayer player;
        private readonly OriginalKingRelief relief;
        private readonly OriginalKingBoardCheckData checkData = new OriginalKingBoardCheckData
        {
            MatchItemCounts = new int[7],
            ItemTypesForTotalLayer = new Dictionary<int, HashSet<int>>(),
            MatchTypesForLightball = new HashSet<int>(),
            CheckedComboTypes = new HashSet<int>()
        };
        private readonly Func<OriginalBoardItem, int> countItemHits;
        private readonly HashSet<OriginalBoardItem> countedBoardItems = new();
        private OriginalKingVisual visual;
        private Coroutine stateCheck;
        private float lastExcitedTime;
        private bool stoppedWorry;
        public bool IsWorried { get; private set; }
        internal int WorryStartMove { get; set; }
        internal int TotalHappyCount { get; set; }
        internal int TotalExcitedCount { get; set; }
        internal int ReliefMove { get; set; }
        public int LeftMoves => board.Moves.LeftMoves;
        public int LastMoveLayerCapsCount => LayerCaps.Length;

        internal OriginalKingBoardReaction(OriginalInitialBoardView board, OriginalPredefinedItemContainer predefined,
            OriginalFillSetGeneration generation, OriginalFirstBoardExplodeTargets winTargets)
        {
            this.board = board;
            this.predefined = predefined;
            this.generation = generation;
            this.winTargets = winTargets;
            strategy = new OriginalTopLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout));
            player = board.Presentation.Coroutines;
            relief = new OriginalKingRelief(checkData, player, this);
            countItemHits = CountItemHits;
        }

        internal void Bind(OriginalKingVisual kingVisual)
        {
            visual = kingVisual;
            visual.HappyAnimationStarted += RecordHappyAnimation;
            board.Moves.OnMoveChanged += OnMoveChanged;
            board.Moves.OnMoveCompleted += OnMoveCompleted;
            board.Goals.OnGoalUpdated += OnGoalUpdated;
        }

        internal void Release()
        {
            if (visual != null) visual.HappyAnimationStarted -= RecordHappyAnimation;
            board.Moves.OnMoveChanged -= OnMoveChanged;
            board.Moves.OnMoveCompleted -= OnMoveCompleted;
            board.Goals.OnGoalUpdated -= OnGoalUpdated;
            if (stateCheck != null) player.StopCoroutine(stateCheck);
            if (relief.ReliefCoroutine != null) player.StopCoroutine(relief.ReliefCoroutine);
            stateCheck = null;
            visual = null;
        }

        private void OnMoveChanged(int left, int changeType) // 0x23e8190.
        {
            if (changeType != 1 && changeType != 3) return;
            IsWorried = false;
            stoppedWorry = false;
        }

        private void OnMoveCompleted(int moveType) // 0x23e8208, IsDefaultMove 0x1ee2b50.
        {
            if (moveType is not (1 or 5 or 8 or 10 or 11 or 12 or 13 or 14)) return;
            if (stateCheck != null) player.StopCoroutine(stateCheck);
            stateCheck = player.ManualStartCoroutine(CheckStateDelayed());
        }

        private IEnumerator CheckStateDelayed() // 0x23e9ab4.
        {
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
                if (LeftMoves <= LayerCaps.Length && !board.State.IsWin() && !stoppedWorry) continue;
                stateCheck = null;
                yield break;
            }
            stateCheck = null;
            checkData.Reset();
            if (!IsWinPossible() || LeftMoves <= 0 || ShouldKingBeWorried())
            {
                if (IsWorried) yield break;
                WorryStartMove = LeftMoves;
                visual.BeWorried();
                IsWorried = true;
            }
            else TryStopWorry(5);
        }

        public void TryKingRelief()
        {
            if (visual != null) relief.TryKingRelief();
        }

        public bool IsPlaying() => board.State.IsPlaying();

        public bool IsWinPossible() // 0x23e7728.
        {
            // The original flying-throw generators are outside levels 1–100.
            return board.Layout.IsFinalBoard && !board.State.HasOperation(12) && !board.State.HasOperation(16);
        }

        private bool CanPredictWin => visual != null && IsWinPossible() && IsPlaying() && visual.CanBeHappy; // 0x23e77a8, first-100 items.

        private void OnGoalUpdated(int goalType, int remaining) => ClearSinglePropellerWinCalculationData(); // 0x23e6e28.
        internal void ClearSinglePropellerWinCalculationData() => winTargets.ClearSinglePropellerGoalHits(); // 0x23e6e44.

        internal void OnSinglePropellerTarget(OriginalGridCell cell) // 0x23e7f08.
        {
            if (CanPredictWin && winTargets.IsSinglePropellerWinning(cell)) PlayPredictedWin();
        }

        internal void OnWinningPropellerCombo(bool isUserAction) // 0x23e7c7c.
        {
            if (!CanPredictWin) return;
            if (isUserAction) PlayPredictedWin();
            else TryStopWorry(6);
        }

        private void RecordHappyAnimation() => TotalHappyCount++; // KingUi.BeHappy, 0x18ca01c.

        private void PlayPredictedWin() // 0x23e6e60.
        {
            stoppedWorry = true;
            IsWorried = false;
            visual.BeHappy(true);
        }

        internal void OnUserCombo(int comboType, OriginalGridCell center) // 0x23e7148.
        {
            if (visual == null || comboType < 1 || comboType > 7) return;
            checkData.Reset();
            bool mixedLightball = comboType is 2 or 3 or 4;
            if (!mixedLightball && CanPredictWin)
            {
                // The native simulation advances the shared Exploder id before
                // scoring. The first 100 levels contain no jelly spread rules.
                OriginalExploder.Next(comboType is 6 or 7 ? 7 : 14);
                if (winTargets.IsComboWinning(comboType, new OriginalCellPoint(center.X, center.Y)))
                {
                    PlayPredictedWin();
                    return;
                }
            }
            if (comboType > 4 || LeftMoves <= LayerCaps.Length) return;
            float time = board.Clock.Time;
            if (lastExcitedTime != 0f && time - lastExcitedTime <= 5f) return;
            if (mixedLightball && !CanMixedLightballExciteKing()) return;
            visual.BeExcited();
            TotalExcitedCount++;
            lastExcitedTime = time;
            if (IsWorried)
            {
                stoppedWorry = true;
                IsWorried = false;
            }
        }

        private bool CanMixedLightballExciteKing() // 0x23e761c / 0x23e7918 / 0x23e7a00.
        {
            int largestColorCount = 0;
            var counts = checkData.MatchItemCounts;
            var iterator = new OriginalGridIterator(strategy);
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                if (cell.StaticTouch.HasTouchBlockingItemExceptChain() ||
                    cell.Mediation.CurrentItem is not OriginalMatchBoardItem match) continue;
                largestColorCount = Math.Max(largestColorCount, ++counts[match.MatchType]);
            }
            checkData.IsThereLightballCombo = true;
            // Ordinary combos use the largest color plus the combo's own ray.
            return largestColorCount + 1 >= LightballComboRayThreshold;
        }

        public void TryStopWorry(int changeReason) // 0x23e7d24.
        {
            if (!IsWorried) return;
            if (visual.StopWorry()) ReliefMove = LeftMoves;
            IsWorried = false;
            stoppedWorry = true;
        }

        public bool ShouldKingBeWorried() // 0x23e84c8.
        {
            int cap = LayerCaps[LeftMoves - 1];
            int hits = predefined.HitRequiredForLevelProgression(countItemHits) +
                generation.HitRequiredForLevelProgression(board.Layout.CurrentBoard.Height);
            bool countedMailbox = false;
            countedBoardItems.Clear();
            var iterator = new OriginalGridIterator(strategy);
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                hits += CountStaticHits(cell.StaticTouch.BelowItems);
                // CheckItem 0x23e8ca4 groups every footprint under its master's identifier.
                var item = cell.Mediation.CurrentItem switch
                {
                    OriginalCupboardFakeBoardItem proxy => proxy.Master,
                    OriginalPotionFakeBoardItem proxy => proxy.Master,
                    OriginalSafeFakeBoardItem proxy => proxy.Master,
                    OriginalBushFakeBoardItem proxy => proxy.Master,
                    var current => current
                };
                // Mailbox is the only first-100 type in ShouldCheckItemTypeOnce (0x23e8fc0).
                if (item != null && countedBoardItems.Add(item) &&
                    (item is not OriginalMailBoxBoardItem || !countedMailbox))
                {
                    hits += CountItemHits(item);
                    countedMailbox |= item is OriginalMailBoxBoardItem;
                }
                hits += CountStaticHits(cell.StaticTouch.AboveItems);
                if (hits > cap) return true;
            }
            return false;
        }

        private static int CountStaticHits(List<IOriginalStaticTouchItem> items)
        {
            int hits = 0;
            // StaticItemModel 0x2b52984 uses the remaining count after reservations.
            for (int i = 0; i < items.Count; i++)
                if (items[i] is IOriginalExplodeTarget target) hits += target.RemainingExplodeCount();
            return hits;
        }

        private int CountItemHits(OriginalBoardItem item)
        {
            // ItemModel 0x2b47c98 dispatches to RemainingExplodeCount.
            // The color-goal eligibility rule comes from MatchItemModel 0x21a7bf4.
            return item switch
            {
                null or OriginalSpecialBoardItem => 0,
                OriginalMatchBoardItem match => match.Mediator.CurrentCell?.Owner?.StaticTouch.BelowItems.Count == 0 &&
                    board.Goals.GetGoalLeftCount(match.GoalType) > 0 ? match.RemainingExplodeCount() : 0,
                OriginalBushBoardItem bush => bush.RemainingExplodeCount() + 32, // 0x2bc40c8, grass to uncover.
                _ => item.RemainingExplodeCount()
            };
        }
    }
}
