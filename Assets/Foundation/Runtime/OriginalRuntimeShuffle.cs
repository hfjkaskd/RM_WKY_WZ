using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // ShuffleManager 0x1ee690c. Candidate placement changes mediation only.
    // A successful board animates its existing views once, outside the search.
    internal sealed class OriginalRuntimeShuffle : IOriginalContextBehaviour
    {
        private const int Operation = 6;
        private const int MaxTries = 500;
        private const int AllowStationaryAfter = 99;
        private const int AllowStationaryAfterRecolor = 249;
        private readonly OriginalInitialBoardView board;
        private readonly OriginalPlayableBoard game;
        private readonly OriginalBoardPossibleMoveServices services;
        private readonly OriginalGridIterator iterator;
        private readonly OriginalGridCell[] cells, unfilled;
        private readonly OriginalMatchBoardItem[] items, originalItems;
        private readonly Dictionary<OriginalMatchBoardItem, OriginalGridCell> originalCells;
        private readonly Vector3[] fromPositions, toPositions;
        private readonly OriginalMatchItemSpriteView[] movingViews;
        private readonly int[] colorCounts = new int[5], colorChoices = new int[5];
        private int count, recolorOriginalIndex = -1, previousColor;
        private bool dirty, detectOnlyLightballAtStart = true;
        public int Id => 6;
        public bool HasOnlyLightballMove { get; private set; }

        internal OriginalRuntimeShuffle(OriginalInitialBoardView board, OriginalPlayableBoard game)
        {
            this.board = board;
            this.game = game;
            services = new OriginalBoardPossibleMoveServices(board.MatchBlocking);
            iterator = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            int capacity = board.Grid.Width * board.Grid.Height;
            cells = new OriginalGridCell[capacity];
            unfilled = new OriginalGridCell[capacity];
            items = new OriginalMatchBoardItem[capacity];
            originalItems = new OriginalMatchBoardItem[capacity];
            originalCells = new Dictionary<OriginalMatchBoardItem, OriginalGridCell>(capacity);
            fromPositions = new Vector3[capacity];
            toPositions = new Vector3[capacity];
            movingViews = new OriginalMatchItemSpriteView[capacity];
        }

        public void Bind() { }

        public void ManualUpdate()
        {
            if (board.Clock.FrameCount % 10 != 0) return;
            // ShuffleManager.ManualUpdate 0x1ee690c reads the pending shift flag.
            var operations = board.State.Operations.Container;
            if (operations.HasAny() || board.State.ShouldTryToShiftCells())
            {
                dirty = true;
                if (!operations.HasMask(OriginalGameOperationContainer.FallAndSwapMask))
                    HasOnlyLightballMove = false;
                return;
            }
            if (!game.Moves.CanUserMakeMove() || game.Auto.IsThereAWaitingMatch) return;
            if (!dirty)
            {
                if (!detectOnlyLightballAtStart) return;
                detectOnlyLightballAtStart = false;
                HasOnlyLightballMove = board.PossibleMoves.CheckBoard(iterator) == 1;
                return;
            }

            int result = board.PossibleMoves.CheckBoard(iterator);
            if (result == 2)
            {
                dirty = false;
                return;
            }
            Shuffle(result == 1);
        }

        private void Shuffle(bool onlyLightball)
        {
            board.State.Operations.OperationStart(Operation);
            Prepare(true);
            int recolorTarget = ChooseRecolorTarget();
            if (recolorTarget != 0) RecolorOneItem(recolorTarget);
            bool recolored = recolorOriginalIndex >= 0;
            bool succeeded = count != 0 && (recolored ? TryWithRecolor() : TryWithoutRecolor());
            if (succeeded || !onlyLightball)
            {
                if (recolored) game.Session.NoPossibleMoveColoredShuffleCount++;
                else game.Session.NoPossibleMoveShuffleCount++;
            }
            if (succeeded)
            {
                game.ManualStartCoroutine(AnimateShuffle());
                return;
            }
            // RevertItems 0x1ee7a1c uses the last candidate result to notify WinFail.
            // OnShuffleUnsuccessful 0x1ee7290 separately publishes the starting result.
            bool lastCandidateHadOnlyLightball = count == 0 ? onlyLightball : board.InitialShuffle.OnlyLightballExists;
            if (recolored) originalItems[recolorOriginalIndex].SetShuffleMatchType(previousColor);
            ClearCells();
            for (int i = 0; i < count; i++) originalCells[originalItems[i]].Mediation.SetAllItems(originalItems[i]);
            ClearSearch();
            board.State.Operations.OperationReset(Operation);
            if (!lastCandidateHadOnlyLightball) game.WinFail.OnNoPossibleMove();
            HasOnlyLightballMove = onlyLightball;
        }

        private void Prepare(bool first) // 0x1ee6e64.
        {
            if (first) originalCells.Clear();
            var scan = iterator;
            scan.Reset();
            count = 0;
            OriginalGridCell cell;
            while ((cell = scan.MoveNext()) != null)
            {
                if (!services.CanEnterPossibleMatch(cell) || !(cell.Mediation.CurrentItem is OriginalMatchBoardItem item)) continue;
                cells[count] = cell;
                if (first)
                {
                    originalItems[count] = item;
                    originalCells.Add(item, cell);
                }
                items[count] = originalItems[count];
                count++;
                item.Mediator.ClearFromAllRegisteredCells();
                cell.Mediation.ClearAllItems();
            }
        }

        private bool TryWithoutRecolor() // 0x1ee6ad0.
        {
            Place(false, -1);
            for (int attempt = 0; attempt < MaxTries; attempt++)
            {
                if (!board.InitialShuffle.ShouldTryAgain() &&
                    (attempt >= AllowStationaryAfter || !HasItemInOriginalCell())) return true;
                Prepare(false);
                Place(false, -1);
            }
            return false;
        }

        private bool TryWithRecolor() // 0x1ee70b8.
        {
            int start = board.DifficultyRandom.DifficultyNext(0, count);
            // Validate the previous placement before rebuilding the candidates.
            // Keep the forced pair for each full round, then try without it.
            for (int round = 0; round <= count; round++)
            {
                int fixedIndex = round == count ? -1 : (start + round) % count;
                if (fixedIndex == recolorOriginalIndex) fixedIndex = (fixedIndex + 1) % count;
                for (int attempt = 0; attempt < MaxTries; attempt++)
                {
                    bool allowStationary = attempt >= AllowStationaryAfterRecolor;
                    if (!board.InitialShuffle.ShouldTryAgain() &&
                        (allowStationary || !HasItemInOriginalCell())) return true;
                    if (round != 0 || attempt != 0) Prepare(false);
                    Place(allowStationary, fixedIndex);
                }
            }
            return false;
        }

        private void Place(bool allowStable, int fixedIndex) // 0x1ee72d8.
        {
            int remaining = count, waiting = 0;
            OriginalGridCell fixedFrom = null, fixedTo = null;
            if (fixedIndex >= 0)
            {
                var from = originalItems[recolorOriginalIndex];
                var to = originalItems[fixedIndex];
                fixedFrom = originalCells[from];
                fixedTo = originalCells[to];
                RemoveCandidate(from, ref remaining);
                RemoveCandidate(to, ref remaining);
                fixedFrom.Mediation.SetAllItems(to);
                fixedTo.Mediation.SetAllItems(from);
            }
            for (int i = 0; i < count; i++)
            {
                var cell = cells[i];
                if (cell == fixedFrom || cell == fixedTo) continue;
                var item = FindItemForCell(cell, ref remaining, allowStable);
                if (item == null) unfilled[waiting++] = cell;
                else cell.Mediation.SetAllItems(item);
            }
            for (int i = 0; i < waiting; i++) unfilled[i].Mediation.SetAllItems(items[i]);
        }

        private OriginalMatchBoardItem FindItemForCell(OriginalGridCell cell, ref int remaining, bool allowStable)
        {
            int left = MatchType(cell.Neighbors.Get(7)), below = MatchType(cell.Neighbors.Get(5));
            int first = left == MatchType(cell.Neighbors.Get(7)?.Neighbors.Get(7)) ? left : 0;
            int second = below == MatchType(cell.Neighbors.Get(5)?.Neighbors.Get(5)) ? below : 0;
            if (left == below && left != 0 && MatchType(cell.Neighbors.Get(6)) == left) second = left;
            int index = board.DifficultyRandom.DifficultyNext(0, remaining);
            for (int offset = 0; offset < remaining; offset++)
            {
                index = (index + offset) % remaining;
                var item = items[index];
                if (item.MatchType == first || item.MatchType == second || (!allowStable && originalCells[item] == cell)) continue;
                items[index] = items[--remaining];
                return item;
            }
            return null;
        }

        private static int MatchType(OriginalGridCell cell) =>
            cell?.IsNormalCell == true && cell.Mediation.CurrentItem is OriginalMatchBoardItem item ? item.MatchType : 0;

        private bool HasItemInOriginalCell()
        {
            for (int i = 0; i < count; i++)
                if (originalItems[i].Mediator.CurrentCell?.Owner == originalCells[originalItems[i]]) return true;
            return false;
        }

        private void RemoveCandidate(OriginalMatchBoardItem item, ref int remaining)
        {
            for (int i = 0; i < remaining; i++)
            {
                if (items[i] != item) continue;
                items[i] = items[--remaining];
                return;
            }
            throw new InvalidOperationException("A recolored shuffle piece is missing from the live board.");
        }

        private int ChooseRecolorTarget() // 0x1ee82c4.
        {
            Array.Clear(colorCounts, 0, colorCounts.Length);
            for (int i = 0; i < count; i++)
            {
                int index = originalItems[i].MatchType - 1;
                if ((uint)index >= (uint)colorCounts.Length || ++colorCounts[index] > 2) return 0;
            }
            int choices = 0;
            for (int i = 0; i < colorCounts.Length; i++)
                if (colorCounts[i] == 2 && HasRecolorSource(i + 1)) colorChoices[choices++] = i + 1;
            for (int remaining = choices; remaining > 1;)
            {
                int selected = board.DifficultyRandom.DifficultyNext(0, remaining);
                remaining--;
                (colorChoices[remaining], colorChoices[selected]) = (colorChoices[selected], colorChoices[remaining]);
            }
            return choices == 0 ? 0 : colorChoices[0];
        }

        private bool HasRecolorSource(int target)
        {
            for (int i = 0; i < count; i++) if (IsRecolorSource(originalItems[i], target)) return true;
            return false;
        }

        private bool IsRecolorSource(OriginalMatchBoardItem item, int target) => item.MatchType != target &&
            (item.MatchType != 5 || !board.InitialItemAmounts.IsThereItemInLevel(22));

        private void RecolorOneItem(int target) // 0x1ee813c.
        {
            int candidates = 0;
            for (int i = 0; i < count; i++) if (IsRecolorSource(originalItems[i], target)) candidates++;
            int index = board.DifficultyRandom.DifficultyNext(0, candidates);
            for (int i = 0; i < count; i++)
            {
                var item = originalItems[i];
                if (!IsRecolorSource(item, target) || index-- != 0) continue;
                previousColor = item.MatchType;
                recolorOriginalIndex = i;
                item.SetShuffleMatchType(target);
                return;
            }
        }

        private IEnumerator AnimateShuffle() // 0x1ee8844.
        {
            yield return game.CreateGameplayWait(.35f);
            var recolored = recolorOriginalIndex >= 0 ? originalItems[recolorOriginalIndex] : null;
            int recoloredSlot = -1;
            int itemLayer = game.ItemSortingLayer;
            for (int i = 0; i < count; i++)
            {
                var cell = cells[i];
                var item = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
                var view = (OriginalMatchItemSpriteView)item.SwapView;
                movingViews[i] = view;
                fromPositions[i] = view.transform.position;
                toPositions[i] = cell.GetViewPosition();
                view.SetShuffleSorting(item == recolored);
                if (item == recolored) recoloredSlot = i;
            }
            game.SetShuffleOrientation(false);
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                // Native MoveAllItems tests duration before adding this frame. Its last
                // eased sample may exceed one and must remain unclamped.
                elapsed += board.Clock.DeltaTime;
                float ratio = BackInOut(elapsed);
                for (int i = 0; i < count; i++)
                {
                    Vector3 position = Vector3.LerpUnclamped(fromPositions[i], toPositions[i], ratio);
                    if (i == recoloredSlot && ratio >= .5f)
                    {
                        movingViews[i] = game.ReplaceShuffleMatch(recolored, position);
                        recoloredSlot = -1;
                    }
                    movingViews[i].transform.position = position;
                }
                yield return null;
            }
            int startY = board.Layout.CurrentBoard.Bottom;
            for (int i = 0; i < count; i++)
                movingViews[i].UpdateSortingForAnimation(OriginalItemSortingRules.AnimatedItem(itemLayer, cells[i].Y - startY), true);
            game.SetShuffleOrientation(true);
            ClearSearch();
            board.State.Operations.OperationReset(Operation);
        }

        internal static float BackInOut(float value)
        {
            // ManualEasing.Back.InOut (0x2c962dc), with strength 0.5.
            const float overshoot = .5f * 2.5949094f;
            float t = value + value;
            if (t < 1f) return t * t * (t * (1f + overshoot) - overshoot) * .5f;
            t -= 2f;
            return (t * t * (overshoot + t * (1f + overshoot)) + 2f) * .5f;
        }

        private void ClearCells()
        {
            for (int i = 0; i < count; i++)
            {
                cells[i].Mediation.CurrentItem?.Mediator.ClearFromAllRegisteredCells();
                cells[i].Mediation.ClearAllItems();
            }
        }

        private void ClearSearch()
        {
            Array.Clear(movingViews, 0, count);
            Array.Clear(originalItems, 0, count);
            Array.Clear(items, 0, count);
            Array.Clear(cells, 0, count);
            Array.Clear(unfilled, 0, count);
            originalCells.Clear();
            count = 0;
            recolorOriginalIndex = -1;
            dirty = false;
        }
    }
}
