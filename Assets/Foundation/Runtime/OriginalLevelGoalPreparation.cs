using System;
using System.Collections.Generic;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    public struct OriginalLevelGoalClaim
    {
        public readonly int GoalType, Count, BoardId;
        public OriginalLevelGoalClaim(int goalType, int count, int boardId)
        { GoalType = goalType; Count = count; BoardId = boardId; } // 0x190f03c.
    }

    // Decoded-data counterpart of LevelParser's goal/limit preparation.
    // Dictionary order, zero-target pruning and claim collection order matter.
    public sealed class OriginalLevelGoalPreparation
    {
        private readonly FTiledLevel level;
        private readonly IOriginalGoalConditionServices conditions;
        private readonly List<OriginalLevelGoalClaim> claims = new List<OriginalLevelGoalClaim>();
        private OriginalBoardLayout rawBoardRows;
        public bool IsBonusLevel { get; private set; }
        public IReadOnlyList<OriginalLevelGoalClaim> GoalClaims => claims;
        public OriginalLevelGoalPreparation(FTiledLevel level, IOriginalGoalConditionServices conditions)
        {
            this.level = level ?? throw new ArgumentNullException(nameof(level));
            this.conditions = conditions;
        }
        public int GetBoardCount() // 0x1c2545c; goals, rather than geometry, own this count.
        {
            if (level.WaterData != null && level.WaterData.WaterReceivableCellCount > 0)
                return level.WaterData.BoardCount;
            if (level.Goals == null || level.Goals.Length == 0) return 0;
            int maximum = -1;
            for (int i = 0; i < level.Goals.Length; i++)
                if (maximum <= level.Goals[i].BoardId) maximum = level.Goals[i].BoardId;
            return unchecked(maximum + 1);
        }
        public int GetBoardIdForRow(int y) // 0x1c25b68 / 0x1c25c28.
        {
            if (rawBoardRows == null)
            {
                var grid = level.Grid;
                bool water = level.WaterData != null && level.WaterData.WaterReceivableCellCount > 0;
                // Same separator/upper-bound algorithm as BoardManager, here
                // explicitly using raw config IDs instead of transformed IDs.
                rawBoardRows = OriginalBoardLayout.Create(grid.Width, grid.Height, grid.Items, level.Scroll, water);
            }
            return rawBoardRows.GetBoardId(0, y);
        }
        public static int GetPrerequisiteGoal(int type) // 0x2b45534.
        {
            switch (type)
            {
                case 13: return 22;
                case 14: return 54;
                case 30: return 29;
                default: return 0;
            }
        }
        public void ParseGoals(List<Dictionary<int, OriginalGoalTuple>> goalDicts) // 0x1c206b0.
        {
            IsBonusLevel = false;
            for (int count = GetBoardCount(); count > 0; count--) goalDicts.Add(new Dictionary<int, OriginalGoalTuple>());
            int length = level.Goals == null ? 0 : level.Goals.Length;
            for (int i = 0; i < length; i++)
            {
                var goal = level.Goals[i];
                if (goal.Goal == 21) IsBonusLevel = true;
                var board = goalDicts[goal.BoardId];
                board.Add(goal.Goal, new OriginalGoalTuple(goal.Goal, goal.Count, goal.IsFromSettings, conditions));
            }
            for (int i = 0; i < length; i++)
            {
                var goal = level.Goals[i];
                if (goal.Goal == 21) continue;
                int prerequisite = GetPrerequisiteGoal(goal.Goal);
                if (goal.Count != 0) continue;
                var board = goalDicts[goal.BoardId];
                if (prerequisite != 0 && board.ContainsKey(prerequisite) && board[prerequisite].TargetCount != 0) continue;
                board.Remove(goal.Goal);
            }
            ApplyGoalClaims(goalDicts);
        }
        public void ParseLimits(List<Dictionary<int, OriginalLimitTuple>> limitDicts) // 0x1c20adc.
        {
            for (int count = GetBoardCount(); count > 0; count--) limitDicts.Add(new Dictionary<int, OriginalLimitTuple>());
            int length = level.Limits == null ? 0 : level.Limits.Length;
            for (int i = 0; i < length; i++)
            {
                var limit = level.Limits[i];
                var board = limitDicts[limit.BoardId];
                board.Add(limit.Limit, new OriginalLimitTuple(limit.Limit, limit.Count));
            }
        }
        public void ParseCounts(List<Dictionary<int, int>> countDicts) // 0x1c215bc.
        {
            for (int count = GetBoardCount(); count > 0; count--) countDicts.Add(new Dictionary<int, int>());
            int length = level.Counts == null ? 0 : level.Counts.Length;
            for (int i = 0; i < length; i++)
            {
                var count = level.Counts[i];
                countDicts[count.BoardId].Add(count.Type, count.Count);
            }
        }
        private void ApplyGoalClaims(List<Dictionary<int, OriginalGoalTuple>> goalDicts) // 0x1c25560.
        {
            claims.Clear();
            CollectGoalClaims(goalDicts);
            for (int i = 0; i < claims.Count; i++)
            {
                var claim = claims[i];
                if (claim.Count <= 0) continue;
                if (claim.BoardId < 0 || claim.BoardId >= goalDicts.Count)
                {
                    Debug.LogError("Goal claim has an invalid board: " + claim.GoalType + ", " + claim.BoardId);
                    continue;
                }
                var board = goalDicts[claim.BoardId];
                if (!board.TryGetValue(claim.GoalType, out var tuple))
                {
                    tuple = new OriginalGoalTuple(claim.GoalType, 0, false, conditions);
                    board.Add(claim.GoalType, tuple);
                }
                tuple.UpdateTargetCount(claim.Count);
                tuple.AddClaim(claim.Count);
            }
        }
        private void CollectGoalClaims(List<Dictionary<int, OriginalGoalTuple>> goalDicts) // 0x1c258a4.
        {
            int length = level.ItemCurtains == null ? 0 : level.ItemCurtains.Length;
            for (int i = 0; i < length; i++)
            {
                var curtain = level.ItemCurtains[i];
                int boardId = GetBoardIdForRow(curtain.MinY);
                int goalLength = curtain.Goals == null ? 0 : curtain.Goals.Length;
                for (int goal = 0; goal < goalLength; goal++)
                {
                    int type = curtain.Goals[goal];
                    // An absent FlatBuffers Targets field reads as zero.
                    int count = curtain.Targets == null ? 0 : curtain.Targets[goal];
                    claims.Add(new OriginalLevelGoalClaim(type, count, boardId));
                    var board = goalDicts[boardId];
                    if (board.ContainsKey(type) && board[type].TargetCount > 0) board[type].ResetTargetCount();
                }
            }
        }
    }
}
