using System;
using System.Collections.Generic;

namespace Whitebox.Foundation.Editor
{
    // Chooses inputs for a test player. Candidate matches run on detached color models.
    internal sealed class CoreNaturalMoveSelection : IOriginalMatchBlockingState
    {
        private readonly OriginalInitialBoardView board;
        private readonly OriginalCellGrid colors;
        private readonly OriginalMatchManager matches;
        private readonly OriginalLightballTargeting lightballTargets;
        private readonly bool[,] conversionHits;
        private readonly HashSet<OriginalGridCell> scored = new();
        private readonly Dictionary<OriginalCupboardBoardItem, int> plateHits = new();
        private readonly HashSet<OriginalGridCell> mailboxAccess = new();
        private OriginalGridCell bestFrom, bestTo;
        private float bestScore;
        private int grassLeft, honeyLeft, mailLeft, platesLeft, safesLeft, dominantColor;
        private bool onlyGrassLeft, planPowerCombos, canUseNewPower;

        public CoreNaturalMoveSelection(OriginalInitialBoardView board)
        {
            this.board = board;
            colors = new OriginalCellGrid(board.Grid.Width, board.Grid.Height);
            lightballTargets = new OriginalLightballTargeting(board.Grid, board.Layout, new HashSet<int>());
            conversionHits = new bool[colors.Width, colors.Height];
            for (int x = 0; x < colors.Width; x++)
            for (int y = 0; y < colors.Height; y++)
            {
                var live = board.Grid.GetCell(x, y);
                colors.SetCell(x, y, new OriginalGridCell(x, y, live.CellType, live.BoardId));
                if (live.Mediation.CurrentItem is OriginalMailBoxBoardItem)
                    for (var above = live.Neighbors.Get(1); above != null && above.IsNormalCell; above = above.Neighbors.Get(1))
                        mailboxAccess.Add(above);
            }
            colors.CacheNeighbors();
            matches = new OriginalMatchManager(colors, board.Layout, this, new OriginalMatchFreezeOperations());
        }

        public bool TryChoose(out OriginalGridCell from, out OriginalGridCell to)
        {
            bestFrom = bestTo = null;
            bestScore = 0f;
            dominantColor = -1;
            grassLeft = board.Goals.GetGoalLeftCount(13);
            honeyLeft = board.Goals.GetGoalLeftCount(OriginalHoneyBoardItem.HoneyGoal);
            mailLeft = board.Goals.GetGoalLeftCount(18);
            platesLeft = board.Goals.GetGoalLeftCount(15);
            safesLeft = board.Goals.GetGoalLeftCount(20);
            planPowerCombos = board.Moves.LeftMoves > 3 && (platesLeft > 0 || safesLeft > 0);
            canUseNewPower = board.Moves.LeftMoves > 1;
            onlyGrassLeft = grassLeft > 0;
            foreach (var goal in board.Goals.GetFinalGoals())
                if (goal.GoalType != 13 && goal.LeftCount > 0) onlyGrassLeft = false;
            for (int x = 0; x < colors.Width; x++)
            for (int y = 0; y < colors.Height; y++)
            {
                var clone = colors.GetCell(x, y);
                clone.Mediation.ClearAllItems();
                if (Live(clone).Mediation.CurrentItem is OriginalMatchBoardItem color)
                    clone.Mediation.SetAllItems(new OriginalMatchBoardItem(x + y * colors.Width, color.ItemType, color.MatchType));
            }
            for (int x = 0; x < colors.Width; x++)
            for (int y = 0; y < colors.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                if (item is OriginalSpecialBoardItem power && OriginalCellTap.CanTapCurrentItem(cell, out _))
                    Consider(cell, null, SinglePowerScore(power, cell, PowerScore(power, cell)));
                if (!OriginalCellTouchRules.CanSwapFrom(cell, board.SwapAvailability)) continue;
                foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                {
                    var neighbor = cell.Neighbors.Get(direction);
                    if (neighbor == null || !OriginalCellTouchRules.CanSwapTo(neighbor, board.SwapAvailability)) continue;
                    var other = neighbor.Mediation.CurrentItem;
                    if (item is OriginalSpecialBoardItem first && other is OriginalSpecialBoardItem second)
                        Consider(cell, neighbor, ComboScore(first, second, neighbor));
                    else if (item is OriginalSpecialBoardItem movingPower && movingPower.CanComboWith(other))
                        Consider(cell, neighbor, PowerSwapScore(movingPower, neighbor, other) + MatchScore(cell, neighbor));
                    else if (other is OriginalSpecialBoardItem receivingPower && receivingPower.CanComboWith(item))
                        Consider(cell, neighbor, PowerSwapScore(receivingPower, cell, item) + MatchScore(cell, neighbor));
                    else if (item is OriginalMatchBoardItem || other is OriginalMatchBoardItem)
                        Consider(cell, neighbor, MatchScore(cell, neighbor));
                }
            }
            from = bestFrom;
            to = bestTo;
            return from != null;
        }

        private float MatchScore(OriginalGridCell from, OriginalGridCell to)
        {
            var a = colors.GetCell(from.X, from.Y);
            var b = colors.GetCell(to.X, to.Y);
            var first = a.Mediation.CurrentItem as OriginalMatchBoardItem;
            var second = b.Mediation.CurrentItem as OriginalMatchBoardItem;
            if (first?.MatchType == second?.MatchType) return 0f;
            AssignPair(a, b, second, first);
            try
            {
                float score = 0f;
                ClearScoring();
                foreach (var group in matches.FindExtensiveMatchesInArea(b.X, b.Y, 3))
                {
                    if (!group.ContainsCell(a) && !group.ContainsCell(b)) continue;
                    float patternScore = !canUseNewPower ? 1f : group.PatternType switch { 1 => 1f, 2 => 4f, 3 or 4 => 6f, 5 or 6 => 9f, 7 => 15f, _ => 0f };
                    // Safes require direct power hits, so leave time to use a newly made power.
                    score += planPowerCombos && safesLeft > 0 && group.PatternType > 1 ? 3f * patternScore : patternScore;
                    if (canUseNewPower && group.PatternType > 1)
                        score += ComboSetupScore(group.ContainsCell(b) ? to : from, group.PatternType);
                    for (int i = 0; i < group.CellCount; i++)
                    {
                        var live = Live(group.GetCell(i));
                        score += ObstacleScore(live, true, group.MatchType);
                        foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                            score += ObstacleScore(live.Neighbors.Get(direction), false, group.MatchType);
                    }
                }
                return score;
            }
            finally { AssignPair(a, b, first, second); }
        }

        private static void AssignPair(OriginalGridCell a, OriginalGridCell b, OriginalBoardItem first, OriginalBoardItem second)
        {
            a.Mediation.ClearAllItems();
            b.Mediation.ClearAllItems();
            if (first != null) a.Mediation.SetAllItems(first);
            if (second != null) b.Mediation.SetAllItems(second);
        }

        private float PowerScore(OriginalSpecialBoardItem power, OriginalGridCell center)
        {
            if (power is OriginalLightballBoardItem) return LightballUseScore(DominantColor);
            if (power is OriginalPropellerBoardItem) return PropellerFlightScore + PropellerTakeoffScore(center);
            return BlastScore(center, power as OriginalRocketBoardItem, (power as OriginalTntBoardItem)?.Config.Radius ?? 0, false);
        }

        private float PowerSwapScore(OriginalSpecialBoardItem power, OriginalGridCell center, OriginalBoardItem other)
        {
            if (power is OriginalLightballBoardItem && other is OriginalMatchBoardItem color)
                return LightballUseScore(color.MatchType);
            return SinglePowerScore(power, power.CurrentCell, PowerScore(power, center));
        }

        private float ComboScore(OriginalSpecialBoardItem moving, OriginalSpecialBoardItem other, OriginalGridCell center)
        {
            // The moved power occupies the destination when the combo begins.
            if (moving is OriginalLightballBoardItem || other is OriginalLightballBoardItem)
                return LightballComboScore(center, moving is OriginalLightballBoardItem ? other : moving);
            if (moving is OriginalTntBoardItem && other is OriginalTntBoardItem)
                return 12f + BlastScore(center, null, 4, false);
            if (moving is OriginalRocketBoardItem && other is OriginalRocketBoardItem)
                return 12f + BlastScore(center, null, 0, true);
            if (moving is OriginalTntBoardItem && other is OriginalRocketBoardItem ||
                moving is OriginalRocketBoardItem && other is OriginalTntBoardItem)
                return 12f + BlastScore(center, null, 1, true);
            if (moving is OriginalPropellerBoardItem && other is OriginalPropellerBoardItem)
                return 12f + 2f * PropellerFlightScore + PropellerTakeoffScore(center);
            if (moving is OriginalPropellerBoardItem || other is OriginalPropellerBoardItem)
            {
                var carried = moving is OriginalPropellerBoardItem ? other : moving;
                float takeoff = carried is OriginalLightballBoardItem ? 0f : PropellerTakeoffScore(center);
                return 12f + PropellerFlightScore + PowerScore(carried, center) + takeoff;
            }
            return 12f + PowerScore(moving, center) + PowerScore(other, center);
        }

        private float ComboSetupScore(OriginalGridCell creation, int pattern)
        {
            float score = 0f;
            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
            {
                var neighbor = creation.Neighbors.Get(direction);
                if (neighbor?.Mediation.CurrentItem is OriginalSpecialBoardItem power)
                    score = Math.Max(score, pattern == 7 || power is OriginalLightballBoardItem ? 30f : 14f);
                else if (planPowerCombos && neighbor?.Mediation.CurrentItem is OriginalMatchBoardItem &&
                    neighbor.Neighbors.Get(direction)?.Mediation.CurrentItem is OriginalSpecialBoardItem)
                    score = Math.Max(score, 6f);
            }
            return score;
        }

        private float SinglePowerScore(OriginalSpecialBoardItem power, OriginalGridCell cell, float score)
        {
            if (!planPowerCombos || power is OriginalLightballBoardItem) return score;
            // Save powers that could meet once a chip between them is cleared.
            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
            {
                var neighbor = cell.Neighbors.Get(direction);
                if (neighbor?.Mediation.CurrentItem is OriginalMatchBoardItem &&
                    neighbor.Neighbors.Get(direction)?.Mediation.CurrentItem is OriginalSpecialBoardItem)
                    return .5f * score;
            }
            return score;
        }

        private int DominantColor => dominantColor < 0 ? dominantColor = lightballTargets.FindMostCommonItem() : dominantColor;

        private float LightballUseScore(int color)
        {
            float score = 5f + ColorScore(color);
            return planPowerCombos ? score * .5f : score;
        }

        private float LightballComboScore(OriginalGridCell center, OriginalSpecialBoardItem partner)
        {
            if (partner is OriginalLightballBoardItem) return 12f + BlastScore(center, null, colors.Width + colors.Height, false);
            int color = DominantColor;
            int orientation = partner is OriginalRocketBoardItem rocket ? rocket.Orientation : -1;
            int radius = (partner as OriginalTntBoardItem)?.Config.Radius ?? 0;
            int converted = 0;
            Array.Clear(conversionHits, 0, conversionHits.Length);
            var region = board.Layout.CurrentBoard;
            // OriginalLightballRocketStrategy alternates orientations in top-left order.
            for (int y = region.Top; y >= region.Bottom; y--)
            for (int x = region.Left; x <= region.Right; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.Mediation.CurrentItem is not OriginalMatchBoardItem item || item.MatchType != color ||
                    item.IsReserved || !OriginalLightballTargeting.IsReadyForLightball(cell)) continue;
                converted++;
                MarkConversionBlast(x, y, radius, orientation, region);
                if (orientation >= 0) orientation = 1 - orientation;
            }
            int partnerOrientation = partner is OriginalRocketBoardItem existing ? existing.Orientation : -1;
            MarkConversionBlast(center.X, center.Y, radius, partnerOrientation, region);
            ClearScoring();
            float score = 12f + converted * (partner is OriginalPropellerBoardItem ? PropellerFlightScore : 1f);
            for (int y = region.Bottom; y <= region.Top; y++)
            for (int x = region.Left; x <= region.Right; x++)
                if (conversionHits[x, y]) score += ObstacleScore(board.Grid.GetCell(x, y), true);
            return score;
        }

        private void MarkConversionBlast(int x, int y, int radius, int orientation, OriginalBoardRegion region)
        {
            int left = orientation == 0 ? region.Left : Math.Max(region.Left, x - radius);
            int right = orientation == 0 ? region.Right : Math.Min(region.Right, x + radius);
            int bottom = orientation == 1 ? region.Bottom : Math.Max(region.Bottom, y - radius);
            int top = orientation == 1 ? region.Top : Math.Min(region.Top, y + radius);
            for (int row = bottom; row <= top; row++)
            for (int column = left; column <= right; column++)
                conversionHits[column, row] = true;
        }

        private float PropellerFlightScore => onlyGrassLeft ? 32f : safesLeft > 0 && platesLeft <= 5 ? 24f : 8f;

        private float PropellerTakeoffScore(OriginalGridCell center)
        {
            // Default takeoff hits the static item under it and the four touching cells.
            ClearScoring();
            float score = ObstacleScore(center, true);
            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                score += ObstacleScore(center.Neighbors.Get(direction), true);
            return score;
        }

        private float BlastScore(OriginalGridCell center, OriginalRocketBoardItem rocket, int radius, bool cross)
        {
            ClearScoring();
            float score = 2f;
            for (int x = 0; x < colors.Width; x++)
            for (int y = 0; y < colors.Height; y++)
            {
                bool outsideColumns = System.Math.Abs(x - center.X) > radius;
                bool outsideRows = System.Math.Abs(y - center.Y) > radius;
                if (rocket != null ? (rocket.Orientation != 0 ? outsideColumns : outsideRows)
                    : cross ? outsideColumns && outsideRows : outsideColumns || outsideRows) continue;
                score += ObstacleScore(board.Grid.GetCell(x, y), true);
            }
            return score;
        }

        private float ColorScore(int color)
        {
            float score = 0f;
            ClearScoring();
            for (int x = 0; x < colors.Width; x++)
            for (int y = 0; y < colors.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!(cell.Mediation.CurrentItem is OriginalMatchBoardItem item) || item.MatchType != color ||
                    item.IsReserved || !OriginalLightballTargeting.IsReadyForLightball(cell)) continue;
                score += .2f + ObstacleScore(cell, true, color);
                foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                    score += ObstacleScore(cell.Neighbors.Get(direction), false, color);
            }
            return score;
        }

        private float ObstacleScore(OriginalGridCell cell, bool direct, int color = 0)
        {
            if (cell == null) return 0f;
            if (cell.StaticTouch.HasStaticItem(OriginalHoneyBoardItem.StaticType))
                return honeyLeft > 0 && scored.Add(cell) ? 9f : 0f;
            var item = cell.Mediation.CurrentItem;
            var potion = item as OriginalPotionBoardItem ?? (item as OriginalPotionFakeBoardItem)?.Master;
            if (potion != null && board.Goals.GetGoalLeftCount(potion.GoalType) > 0 &&
                (color == 0 || potion.HasBottle(color)))
                return scored.Add(potion.Mediator.CurrentCell.Owner) ? 10f : 0f;
            var cupboard = item as OriginalCupboardBoardItem ?? (item as OriginalCupboardFakeBoardItem)?.Master;
            if (cupboard != null && platesLeft > 0) return CupboardScore(cupboard, cell, direct);
            var safe = item as OriginalSafeBoardItem ?? (item as OriginalSafeFakeBoardItem)?.Master;
            if (safe != null && direct && safesLeft > 0)
                return scored.Add(safe.Mediator.CurrentCell.Owner) ? 14f : 0f;
            var bush = item as OriginalBushBoardItem ?? (item as OriginalBushFakeBoardItem)?.Master;
            if (bush != null && board.Goals.GetGoalLeftCount(bush.GoalType) > 0)
                return scored.Add(bush.Mediator.CurrentCell.Owner) ? 9f : 0f;
            float value = item switch
            {
                OriginalBoxBoardItem when board.Goals.GetGoalLeftCount(11) > 0 => 6f,
                OriginalVaseBoardItem when board.Goals.GetGoalLeftCount(12) > 0 => 5f,
                OriginalEggBoardItem when board.Goals.GetGoalLeftCount(16) > 0 => 4f,
                OriginalOwlStatueBoardItem owl when direct && board.Goals.GetGoalLeftCount(owl.GoalType) > 0 =>
                    mailLeft > 0 && mailboxAccess.Contains(cell) ? 14f : 6f,
                OriginalMailBoxBoardItem mailbox when mailbox.IsActive && mailbox.ItemView.CanCollect => 10f,
                _ => 0f,
            };
            if (direct && grassLeft > 0 && item?.IsObstacle != true && cell.StaticTouch.AboveItems.Count == 0 &&
                cell.StaticTouch.HasStaticItem(2)) value += onlyGrassLeft ? 24f : 5f;
            if (value <= 0f || !scored.Add(cell)) return 0f;
            if (item is OriginalBoxBoardItem)
                for (var below = cell.Neighbors.Get(5); below != null; below = below.Neighbors.Get(5))
                    if (below.IsNormalCell && below.Mediation.CurrentItem == null) return value + 4f;
            return value;
        }

        private void ClearScoring()
        {
            scored.Clear();
            plateHits.Clear();
        }

        private float CupboardScore(OriginalCupboardBoardItem cupboard, OriginalGridCell cell, bool direct)
        {
            float hitValue = direct ? 12f : 7f;
            if (cupboard.HasDoor)
                return scored.Add(cupboard.Mediator.CurrentCell.Owner) ? hitValue : 0f;
            plateHits.TryGetValue(cupboard, out int hits);
            if (hits >= cupboard.PlateCount || !scored.Add(cell)) return 0f;
            plateHits[cupboard] = ++hits;
            // Clearing the last plate opens four cells for refill and subsequent matches.
            return hits == cupboard.PlateCount ? hitValue + 12f : hitValue;
        }

        private void Consider(OriginalGridCell from, OriginalGridCell to, float score)
        {
            if (score <= bestScore) return;
            bestFrom = from;
            bestTo = to;
            bestScore = score;
        }

        private OriginalGridCell Live(OriginalGridCell clone) => board.Grid.GetCell(clone.X, clone.Y);
        public bool HasTouchBlockingItemExceptChain(OriginalGridCell cell) =>
            board.MatchBlocking.HasTouchBlockingItemExceptChain(Live(cell));
        public bool HasIncomingTouchBlockingItem(OriginalGridCell cell) =>
            board.MatchBlocking.HasIncomingTouchBlockingItem(Live(cell));
    }
}
