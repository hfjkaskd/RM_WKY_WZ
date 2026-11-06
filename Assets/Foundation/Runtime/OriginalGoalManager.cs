using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalGoalCheckPanel
    {
        void DecreaseLeftGoalCheck();
        bool AreAllGoalChecksShown();
        void ReplaceGoalsWithCoin();
    }
    // State/counting portion of GoalManager. Board zero is the final board;
    // initial/current boards count down toward it in the original layout.
    public sealed class OriginalGoalManager : IOriginalGoalConditionServices
    {
        public delegate void GoalUpdatedUi(int type, int count, bool hasPrerequisite,
            bool isUpdatedForStartValue, bool isGoalCompleted);
        private readonly Func<int?> currentBoard;
        private readonly OriginalGameOperations operations;
        private readonly OriginalGoalClaims claims;
        private IOriginalGoalCheckPanel checkPanel;
        public void BindGoalChecks(IOriginalGoalCheckPanel panel) { checkPanel = panel; }
        public void ReleaseGoalChecks(IOriginalGoalCheckPanel panel) { if (ReferenceEquals(checkPanel, panel)) checkPanel = null; }
        public void DecreaseLeftGoalCheck() // 0x3215554 forwards to the actual GoalPanelUi.
        { if (checkPanel == null) throw new NullReferenceException(); checkPanel.DecreaseLeftGoalCheck(); }
        public bool AreAllGoalChecksShown() // 0x32155a8.
        { if (checkPanel == null) throw new NullReferenceException(); return checkPanel.AreAllGoalChecksShown(); }
        private List<OriginalGoalTuple[]> goals;
        private List<OriginalLimitTuple[]> limits;
        private readonly Dictionary<int, int> prerequisites = new Dictionary<int, int>();
        private readonly Dictionary<int, int> flying = new Dictionary<int, int>();
        public event Action<int, int> OnGoalUpdated;
        public event GoalUpdatedUi OnGoalUpdatedUi;

        public OriginalGoalManager(Func<int?> currentBoard, OriginalGameOperations operations, OriginalGoalClaims claims)
        {
            this.currentBoard = currentBoard ?? throw new ArgumentNullException(nameof(currentBoard));
            this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
            this.claims = claims ?? throw new ArgumentNullException(nameof(claims));
        }
        public bool HasOperation(int operation) => operations.Container.Has(operation);
        public void InitWithGoalsAndLimits(IReadOnlyList<Dictionary<int, OriginalGoalTuple>> allGoals,
            IReadOnlyList<Dictionary<int, OriginalLimitTuple>> allLimits, bool shouldReset) // 0x3212444.
        {
            ClearPrerequisiteItems();
            goals = new List<OriginalGoalTuple[]>(allGoals.Count);
            limits = new List<OriginalLimitTuple[]>(allLimits.Count);
            for (int board = 0; board < allGoals.Count; board++)
            {
                var entries = allGoals[board];
                var values = new OriginalGoalTuple[entries.Count];
                goals.Add(values);
                int index = 0;
                foreach (var entry in entries)
                {
                    var tuple = entry.Value;
                    if (shouldReset) tuple.Reset();
                    values[index++] = tuple;
                    OnGoalUpdated?.Invoke(entry.Key, tuple.LeftCount);
                    if (board == 0 && OnGoalUpdatedUi != null)
                        OnGoalUpdatedUi(entry.Key, GetGoalDisplayCount(tuple), true, true, false);
                    // Init does not clear unrelated old flying entries.
                    flying[entry.Key] = 0;
                }
            }
            for (int board = 0; board < allLimits.Count; board++)
            {
                var entries = allLimits[board];
                var values = new OriginalLimitTuple[entries.Count];
                limits.Add(values);
                int index = 0;
                foreach (var entry in entries) values[index++] = entry.Value;
            }
        }
        public List<OriginalGoalTuple[]> GetAllGoals() => goals; // 0x3213240.
        public OriginalGoalTuple[] GetCurrentGoals() => goals.Count == 0 ? Array.Empty<OriginalGoalTuple>() : goals[RequireBoard()];
        public OriginalGoalTuple[] GetFinalGoals() => goals.Count == 0 ? Array.Empty<OriginalGoalTuple>() : goals[0];
        public void ReplaceGoalsWithCoin(int initialCoins) // 0x3214efc.
        {
            ClearPrerequisiteItems();
            int boardCount = goals.Count;
            goals = new List<OriginalGoalTuple[]>(boardCount);
            for (int board = 0; board < boardCount; board++)
                goals.Add(new[] { new OriginalGoalTuple(21, initialCoins, false, this) });
            // Native retains unrelated flying entries and does not emit model
            // or UI update events while replacing the goal collection.
            flying[21] = 0;
            if (checkPanel == null) throw new NullReferenceException();
            checkPanel.ReplaceGoalsWithCoin();
        }
        private int RequireBoard()
        {
            int? board = currentBoard();
            if (!board.HasValue) throw new NullReferenceException();
            return board.Value;
        }
        private int BoardOrLast() => currentBoard() ?? goals.Count - 1;
        public int GetGoalIndex(int goalType, bool checkFinalBoard = false) // 0x3213494.
        {
            if (goals.Count == 0) return -1;
            return Find(goals[checkFinalBoard ? 0 : BoardOrLast()], goalType);
        }
        private static int Find(OriginalGoalTuple[] values, int goalType)
        {
            for (int i = 0; i < values.Length; i++) if (values[i].GoalType == goalType) return i;
            return -1;
        }
        private OriginalGoalTuple GetCurrentTuple(int index) => goals[RequireBoard()][index];
        public int GetGoalDisplayCount(int goalType) // 0x32133e4.
        {
            int index = GetGoalIndex(goalType);
            return index < 0 ? 0 : GetGoalDisplayCount(GetCurrentTuple(index));
        }
        public int GetGoalDisplayCount(OriginalGoalTuple tuple) // 0x3213038.
        {
            int claimed = claims.HasClaimers ? claims.GetTotalOutstandingForGoal(tuple.GoalType) : tuple.ClaimedCount;
            int remaining = unchecked(tuple.LeftCountUi - claimed);
            return remaining < 0 ? 0 : remaining;
        }
        public void DecreaseGoal(int goalType) // 0x32136cc; debug-only native log omitted.
        {
            claims.GetClaimerWantingGoal(goalType)?.DecrementTarget(goalType);
            int index = GetGoalIndex(goalType);
            if (index < 0) return;
            int count = GetCurrentTuple(index).DecreaseGoal();
            OnGoalUpdated?.Invoke(goalType, count);
        }
        public void DecreaseGoalUi(int goalType) // 0x3213954.
        {
            if (flying.TryGetValue(goalType, out int count)) flying[goalType] = unchecked(count - 1);
            var claimer = claims.GetClaimerAwaitingGoal(goalType);
            int index = GetGoalIndex(goalType);
            if (claimer != null)
            {
                if (index >= 0) GetCurrentTuple(index).DecreaseGoalUi();
                claimer.ItemReached(goalType);
                RefreshCompletedGoalUi(goalType);
                return;
            }
            if (index < 0) return;
            var tuple = GetCurrentTuple(index);
            tuple.DecreaseGoalUi();
            if (RequireBoard() == 0) NotifyUi(goalType, tuple, false);
        }
        private void RefreshCompletedGoalUi(int goalType) // 0x3213c08.
        {
            if (RequireBoard() != 0) return;
            int index = GetGoalIndex(goalType);
            if (index < 0) return;
            var tuple = GetCurrentTuple(index);
            if (!tuple.IsGoalCompleted() || HaveAnyFlyingMatchItemsToCollect(goalType)) return;
            bool prerequisite = prerequisites.ContainsKey(goalType);
            var handler = OnGoalUpdatedUi;
            if (handler != null) handler(goalType, GetGoalDisplayCount(tuple), prerequisite, false, true);
        }
        private void NotifyUi(int goalType, OriginalGoalTuple tuple, bool start)
        {
            bool prerequisite = prerequisites.ContainsKey(goalType);
            var handler = OnGoalUpdatedUi;
            if (handler != null) handler(goalType, GetGoalDisplayCount(tuple), prerequisite, start, tuple.IsGoalCompleted());
        }
        public void ForceUpdateGoal(int goalType) // 0x3213db8.
        {
            int index = GetGoalIndex(goalType);
            if (index < 0) return;
            var tuple = GetCurrentTuple(index);
            OnGoalUpdated?.Invoke(goalType, tuple.LeftCount);
            NotifyUi(goalType, tuple, false);
        }
        public void UpdateGoal(int goalType, int amount) // 0x3213f08.
        {
            int index = GetGoalIndex(goalType);
            if (index < 0) return;
            int count = GetCurrentTuple(index).UpdateTargetCount(amount);
            OnGoalUpdated?.Invoke(goalType, count);
        }
        public void IncreaseGoal(int goalType) // 0x3213fec.
        {
            claims.GetClaimerToRestoreGoal(goalType)?.IncrementTarget(goalType);
            int index = GetGoalIndex(goalType);
            if (index < 0) return;
            int count = GetCurrentTuple(index).IncreaseGoal();
            OnGoalUpdated?.Invoke(goalType, count);
        }
        public void IncreaseGoalUi(int goalType) // 0x3214158.
        {
            int index = GetGoalIndex(goalType);
            if (index < 0) return;
            var tuple = GetCurrentTuple(index);
            tuple.IncreaseGoalUi();
            if (RequireBoard() == 0) NotifyUi(goalType, tuple, false);
        }
        public int GetGoalTargetCount(int goalType) // 0x32142a0.
        {
            if (goals == null) return 0;
            int index = GetGoalIndex(goalType);
            return index < 0 ? 0 : goals[BoardOrLast()][index].TargetCount;
        }
        public int GetGoalTargetCountForBoard(int board, int goalType) // 0x3214378 / 0x3214444.
        {
            if (goals == null || board >= goals.Count) return 0;
            int index = Find(goals[board], goalType);
            return index < 0 ? 0 : goals[board][index].TargetCount;
        }
        public bool IsGoalClaimedForBoard(int board, int goalType) // 0x3214504.
        {
            if (goals == null || board >= goals.Count) return false;
            int index = Find(goals[board], goalType);
            return index >= 0 && goals[board][index].ClaimedCount > 0;
        }
        public int GetGoalLeftCount(int goalType, bool checkFinalBoard = false) // 0x32146cc.
        {
            int index = GetGoalIndex(goalType, checkFinalBoard);
            return index < 0 ? 0 : goals[checkFinalBoard ? 0 : RequireBoard()][index].LeftCount;
        }
        public int GetGoalUiLeftCount(int goalType) // 0x3214794.
        {
            int index = GetGoalIndex(goalType);
            return index < 0 ? 0 : GetCurrentTuple(index).LeftCountUi;
        }
        public int GetLimitCount(int goalType) // 0x32145d8.
        {
            if (limits == null || limits.Count == 0) return 0;
            int board = currentBoard() ?? limits.Count - 1;
            if (board < 0 || board >= limits.Count) return 0;
            var entries = limits[board];
            for (int i = 0; i < entries.Length; i++) if (entries[i].GoalType == goalType) return entries[i].Limit;
            return 0;
        }
        public bool IsAllGoalsReached() // 0x3214840.
        {
            if (HasAnyPrerequisite()) return false;
            for (int board = 0; board < goals.Count; board++)
                for (int i = 0; i < goals[board].Length; i++) if (!goals[board][i].IsGoalCompleted()) return false;
            return true;
        }
        public bool AreCurrentGoalsReached() // 0x3214988.
        {
            var entries = GetCurrentGoals();
            for (int i = 0; i < entries.Length; i++) if (!entries[i].IsGoalCompleted()) return false;
            return true;
        }
        public bool IsGoalReached(int goalType) // 0x32149fc.
        {
            int index = GetGoalIndex(goalType);
            return index < 0 || GetCurrentTuple(index).IsGoalCompleted();
        }
        public void SyncClaimedGoalUiCounts() // 0x321357c.
        {
            if (!claims.HasClaimers) return;
            var entries = GetCurrentGoals();
            for (int i = 0; i < entries.Length; i++)
            {
                var tuple = entries[i];
                var handler = OnGoalUpdatedUi;
                if (!tuple.IsFullyClaimed() && handler != null)
                {
                    int goalType = tuple.GoalType;
                    int count = GetGoalDisplayCount(tuple);
                    bool prerequisite = prerequisites.ContainsKey(tuple.GoalType);
                    handler(goalType, count, prerequisite, true, tuple.IsGoalCompleted());
                }
            }
        }
        public void ClearPrerequisiteItems() => prerequisites.Clear(); // 0x3212fe8.
        public void AddToPrerequisite(int goalType) // 0x321309c.
        {
            prerequisites.TryGetValue(goalType, out int count);
            prerequisites[goalType] = unchecked(count + 1);
        }
        public void RemoveFromPrerequisite(int goalType) // 0x3213170.
        {
            if (!prerequisites.TryGetValue(goalType, out int count)) return;
            count = unchecked(count - 1);
            if (count == 0) prerequisites.Remove(goalType);
            else prerequisites[goalType] = count;
        }
        public bool HasPrerequisite(int goalType) => prerequisites.ContainsKey(goalType); // 0x32154fc.
        public bool HasAnyPrerequisite() => prerequisites.Count > 0; // 0x321492c.
        public void FlyingToBeCollected(int goalType) // 0x3214de8.
        {
            if (flying.TryGetValue(goalType, out int count)) flying[goalType] = unchecked(count + 1);
        }
        public int GetFlyingGoalCount(int goalType) => flying[goalType]; // 0x3214ea4.
        public bool HaveAnyFlyingMatchItemsToCollect(int goalType) => flying[goalType] > 0; // 0x3213d54.
    }
}
