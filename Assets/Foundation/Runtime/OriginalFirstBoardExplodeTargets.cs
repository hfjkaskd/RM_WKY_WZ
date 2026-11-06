using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Concrete target mediator for match/box/special items and below-cell grass.
    public sealed partial class OriginalFirstBoardExplodeTargets : IOriginalSingleExplodeTargetServices, IOriginalItemTargetScoreContext
    {
        private readonly OriginalGoalManager goals;
        private readonly OriginalGoalClaims claims;
        private readonly Func<int> currentFrame;
        private readonly GoalHitTotals areaGoalHits = new GoalHitTotals();
        private readonly GoalHitTotals singlePropellerGoalHits = new GoalHitTotals();

        private sealed class GoalHitTotals
        {
            public int[] Hits = Array.Empty<int>();
            public readonly Dictionary<int, int> MultiCellHits = new Dictionary<int, int>();
            public void Reset(int count)
            {
                if (Hits.Length == count) Array.Clear(Hits, 0, Hits.Length);
                else Hits = new int[count];
                MultiCellHits.Clear();
            }
        }
        public OriginalFirstBoardExplodeTargets(OriginalGoalManager goals, OriginalGoalClaims claims, Func<int> currentFrame)
        {
            this.goals = goals ?? throw new ArgumentNullException(nameof(goals));
            this.claims = claims ?? throw new ArgumentNullException(nameof(claims));
            this.currentFrame = currentFrame ?? throw new ArgumentNullException(nameof(currentFrame));
        }
        public int GetGoalLeftCount(int goalType) => goals.GetGoalLeftCount(goalType); // ItemModel 0x2b46d88.
        public void RequireSupportedScoring()
        {
            if (claims.HasClaimers) throw new NotSupportedException("Goal claimers require their original completion scorer.");
        }
        private void RequireSupported(OriginalGridCell cell)
        {
            int aboveCount = cell.StaticTouch.AboveItems.Count;
            int belowCount = cell.StaticTouch.BelowItems.Count;
            if (aboveCount > 1 || (aboveCount == 1 && Honey(cell) == null) || belowCount > 1 ||
                (belowCount == 1 && Grass(cell) == null) || claims.HasClaimers)
                throw new NotSupportedException("This target composition requires the original owner for other statics and goal claimers.");
        }
        private static OriginalGrassBoardItem Grass(OriginalGridCell cell) =>
            cell.StaticTouch.GetStaticItem(2) as OriginalGrassBoardItem;

        private static OriginalHoneyBoardItem Honey(OriginalGridCell cell) =>
            cell.StaticTouch.GetStaticItem(OriginalHoneyBoardItem.StaticType) as OriginalHoneyBoardItem;

        public int GetScore(OriginalGridCell cell, int guidedType, bool canSpreadJelly) // 0x343bd78, ordinary numbered boards.
        {
            if (canSpreadJelly) throw new NotSupportedException("Jelly scoring requires its original static mediator.");
            bool single = OriginalExplodeTargetRules.IsSingleExploderStrategy(guidedType);
            if (!single && guidedType != 1 && guidedType != 2 && guidedType != 3)
                throw new NotSupportedException("Multiple-exploder scoring is a separate target path.");
            int frame = currentFrame();
            if (!single && cell.GuidedScoreFrame == frame && cell.GuidedScoreType == guidedType)
                return cell.GuidedScore;
            int score = CalculateCellScore(cell, guidedType);
            cell.GuidedScoreFrame = frame;
            cell.GuidedScoreType = guidedType;
            cell.GuidedScore = score;
            return score;
        }

        private int CalculateCellScore(OriginalGridCell cell, int guidedType) // 0x343bf04 / 0x343c248.
        {
            RequireSupported(cell);
            var honey = Honey(cell);
            if (honey?.RemainingExplodeCount() > 0) return honey.GetExplodeScore(guidedType);
            OriginalBoardItem item = cell.Mediation.CurrentItem;
            if (item?.IsObstacle == true) return item.GetExplodeScore(guidedType);
            var grass = Grass(cell);
            return grass != null ? grass.GetExplodeScore(guidedType) : item?.GetExplodeScore(guidedType) ?? 0;
        }
        public int RemainingExplodeCount(OriginalGridCell cell, OriginalExplodeData data) // 0x343c39c.
        {
            RequireSupported(cell);
            OriginalBoardItem item = cell.Mediation.CurrentItem;
            var grass = Grass(cell);
            int aboveHits = Honey(cell)?.RemainingExplodeCount() ?? 0;
            if (grass == null) return unchecked(aboveHits + (item?.RemainingExplodeCount() ?? 0));
            int obstacleHits = item?.IsObstacle == true ? item.RemainingExplodeCount() : 0;
            return unchecked(aboveHits + obstacleHits + grass.RemainingExplodeCount());
        }
        public OriginalGridCell TryRedirect(OriginalGridCell cell, OriginalExplodeData data, List<OriginalGridCell> remainingOptions)
        { RequireSupported(cell); return cell; } // 0x343caa4, no-above/ice branch.
        public IOriginalExplodeTarget AddIncomingExploder(OriginalGridCell cell, OriginalExplodeData data) // 0x343c80c.
        {
            RequireSupported(cell);
            var item = cell.Mediation.CurrentItem;
            var grass = Grass(cell);
            var honey = Honey(cell);
            IOriginalExplodeTarget target = null;
            if (honey != null && honey.RemainingExplodeCount() > 0) target = honey;
            else if (item?.IsObstacle == true && item.RemainingExplodeCount() > 0) target = item;
            else if (grass != null && grass.RemainingExplodeCount() > 0) target = grass;
            else if (item?.IsObstacle == false && item.RemainingExplodeCount() > 0) target = item;
            target?.AddIncomingExploder(data);
            return target;
        }

        private void CountPropellerGoalHits(OriginalGridCell cell, OriginalGoalTuple[] finalGoals, GoalHitTotals totals)
        {
            RequireSupported(cell);
            var honey = Honey(cell);
            if (honey != null)
            {
                AddGoalHit(honey, finalGoals, totals);
                return;
            }
            var item = cell.Mediation.TargetItem;
            // CellModel 0x319d6cc selects a blocking target first. Otherwise
            // both the top below static and the non-obstacle target can count.
            if (item?.IsObstacle != true) AddGoalHit(Grass(cell), finalGoals, totals);
            AddGoalHit(item, finalGoals, totals);
        }

        private static void AddGoalHit(IOriginalExplodeTarget item, OriginalGoalTuple[] finalGoals, GoalHitTotals totals)
        {
            if (item == null) return;
            if (OriginalMultiCellObstacle.GetMaster(item as OriginalBoardItem) is { } footprintMaster)
                item = footprintMaster;
            int goalHits = 1;
            if (item is OriginalCupboardBoardItem cupboard)
            {
                // Cupboard 0x2bcb03c and WinCalculationData 0x1ca3aa0.
                if (cupboard.RemainingExplodeCount() > 10) return;
                totals.MultiCellHits.TryGetValue(cupboard.Id, out int hits);
                if (hits >= cupboard.LayerCount) return;
                totals.MultiCellHits[cupboard.Id] = hits + 1;
            }
            else if (item is OriginalBoardItem master &&
                (master is OriginalPotionBoardItem or OriginalBushBoardItem or OriginalSafeBoardItem))
            {
                // MultipleCellItemModel 0x2b4bdd4 counts the whole footprint once.
                if (master.RemainingExplodeCount() != 1 || totals.MultiCellHits.ContainsKey(master.Id)) return;
                totals.MultiCellHits.Add(master.Id, 1);
                // Potion 0x2177e58 includes the bottles already targeted in flight.
                if (master is OriginalPotionBoardItem) goalHits = master.LayerCount;
            }
            // ItemModel 0x2b47910 and StaticItemModel 0x2b5286c use unreserved hits.
            // MailBox 0x24adf08 always adds one hit per mailbox cell.
            else if (item is not OriginalMailBoxBoardItem && item.RemainingExplodeCount() != 1) return;

            for (int i = 0; i < finalGoals.Length; i++)
                if (finalGoals[i].GoalType == item.GoalType)
                {
                    totals.Hits[i] = unchecked(totals.Hits[i] + goalHits);
                    break;
                }
        }
    }
}
