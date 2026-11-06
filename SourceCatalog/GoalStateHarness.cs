using System;
using System.Collections.Generic;
using Whitebox.Foundation;

namespace UnityEngine { public static class Debug { public static void LogError(object value) { throw new Exception("Operation error"); } } }

sealed class Conditions : IOriginalGoalConditionServices
{
    public int Mask, Goals;
    public bool HasOperation(int operation) => (Mask & (1 << operation)) != 0;
    public bool IsGoalReached(int type) => (Goals & (type == 54 ? 1 : 2)) != 0;
}
sealed class Claimer : IOriginalGoalClaimer
{
    public int Id, Active, Want, Await, Outstanding;
    public bool IsInCurrentBoard() => Active != 0;
    public bool WantsGoal(int type) => Want == type;
    public bool HasIncomingGoal(int type) => Await == type;
    public bool CanRestoreGoal(int type) => Want == type;
    public int GetOutstandingTargetForGoal(int type) => type == Want ? Outstanding : 0;
    public void DecrementTarget(int type) { Verify.Trace.Add("claim:" + Id + ":" + type); }
    public void IncrementTarget(int type) { Verify.Trace.Add("restore:" + Id + ":" + type); }
    public void ItemReached(int type) { Verify.Trace.Add("collect:" + Id + ":" + type); }
}
static class Verify
{
    public static readonly List<string> Trace = new List<string>();
    static readonly int[] Types = { 11, 1, 13, 14, 21, 30, 54, 29 };
    static int I(string text) => int.Parse(text);
    static string State(OriginalGoalTuple tuple) => string.Join(",", new[] { tuple.TargetCount, tuple.LeftCount, tuple.LeftCountUi, tuple.ClaimedCount });
    static string Flags(OriginalGoalTuple tuple) => (tuple.IsFullyClaimed() ? "1" : "0") + (tuple.IsGoalCompleted() ? "1" : "0") +
        (tuple.IsBottomCollect() ? "1" : "0") + (tuple.IsSpecificWinConditionalGoal() ? "1" : "0");
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            Trace.Clear();
            var parts = line.Split('#');
            var head = Array.ConvertAll(parts[1].Split(','), I);
            var result = new List<string>();
            if (parts[0] == "T")
            {
                var tuple = new OriginalGoalTuple(head[0], head[1], head[2] != 0, new Conditions { Mask = head[3], Goals = head[4] });
                foreach (var command in parts[2].Split(';'))
                {
                    var data = command.Split(','); int arg = I(data[1]); string value = "-";
                    switch (data[0])
                    {
                        case "d": value = tuple.DecreaseGoal().ToString(); break;
                        case "u": value = tuple.DecreaseGoalUi().ToString(); break;
                        case "i": value = tuple.IncreaseGoal().ToString(); break;
                        case "j": value = tuple.IncreaseGoalUi().ToString(); break;
                        case "r": tuple.Reset(); break;
                        case "s": tuple.SetGoal(arg); break;
                        case "a": tuple.AddClaim(arg); break;
                        case "t": value = tuple.UpdateTargetCount(arg).ToString(); break;
                        case "z": tuple.ResetTargetCount(); break;
                    }
                    result.Add(value + ":" + State(tuple) + ":" + Flags(tuple));
                }
                Console.WriteLine(string.Join("|", result)); continue;
            }
            int boards = head[0], current = head[1], events = head[2];
            var operations = new OriginalGameOperations(() => 42);
            for (int op = 0; op < 28; op++) if ((head[3] & (1 << op)) != 0) operations.OperationStart(op);
            var claims = new OriginalGoalClaims();
            var manager = new OriginalGoalManager(() => current == -1 ? (int?)null : current, operations, claims);
            var goals = new List<Dictionary<int, OriginalGoalTuple>>();
            var limits = new List<Dictionary<int, OriginalLimitTuple>>();
            int seed = head[4];
            for (int board = 0; board < boards; board++)
            {
                var values = new Dictionary<int, OriginalGoalTuple>();
                for (int i = 0; i < Types.Length; i++)
                {
                    var tuple = new OriginalGoalTuple(Types[i], (seed + board * 3 + i) % 5, false, manager);
                    tuple.AddClaim((seed + i) % 3);
                    values.Add(Types[i], tuple);
                }
                goals.Add(values);
                limits.Add(new Dictionary<int, OriginalLimitTuple> { { 11, new OriginalLimitTuple(11, board + seed) } });
            }
            manager.InitWithGoalsAndLimits(goals, limits, false);
            var claimObjects = new Claimer[3];
            for (int i = 0; i < claimObjects.Length; i++)
            {
                claimObjects[i] = new Claimer { Id = i, Active = (seed + i) % 2, Want = Types[(seed + i) % Types.Length],
                    Await = Types[(seed + i * 2) % Types.Length], Outstanding = (seed + i) % 4 };
                if ((seed & (1 << i)) != 0) { claims.Register(claimObjects[i]); claims.Register(claimObjects[i]); }
            }
            if ((events & 1) != 0) manager.OnGoalUpdated += (type, count) => Trace.Add("model:" + type + ":" + count);
            if ((events & 2) != 0) manager.OnGoalUpdatedUi += (type, count, prerequisite, start, complete) =>
                Trace.Add("ui:" + type + ":" + count + ":" + (prerequisite ? 1 : 0) + (start ? 1 : 0) + (complete ? 1 : 0));
            foreach (var command in parts[2].Split(';'))
            {
                var data = command.Split(','); int arg = I(data[1]); string value = "-";
                try
                {
                    switch (data[0])
                    {
                        case "d": manager.DecreaseGoal(arg); break;
                        case "u": manager.DecreaseGoalUi(arg); break;
                        case "i": manager.IncreaseGoal(arg); break;
                        case "j": manager.IncreaseGoalUi(arg); break;
                        case "f": manager.FlyingToBeCollected(arg); break;
                        case "a": manager.AddToPrerequisite(arg); break;
                        case "r": manager.RemoveFromPrerequisite(arg); break;
                        case "c": manager.ClearPrerequisiteItems(); break;
                        case "v": manager.ForceUpdateGoal(arg); break;
                        case "s": manager.SyncClaimedGoalUiCounts(); break;
                        case "t": manager.UpdateGoal(arg, I(data[2])); break;
                        case "b": current = arg; break;
                        case "x": claims.Unregister(claimObjects[arg]); break;
                        case "k": claims.Register(claimObjects[arg]); break;
                        case "q": value = string.Join(",", new[] { manager.GetGoalIndex(arg), manager.GetGoalIndex(arg, true),
                            manager.GetGoalTargetCount(arg), manager.GetGoalTargetCountForBoard(0, arg), manager.GetGoalLeftCount(arg, true),
                            manager.GetLimitCount(arg), manager.IsGoalClaimedForBoard(0, arg) ? 1 : 0 }); break;
                        case "g": value = string.Join(",", new[] { manager.GetGoalLeftCount(arg), manager.GetGoalUiLeftCount(arg),
                            manager.GetGoalDisplayCount(arg), manager.IsGoalReached(arg) ? 1 : 0, manager.AreCurrentGoalsReached() ? 1 : 0,
                            manager.IsAllGoalsReached() ? 1 : 0, manager.HasPrerequisite(arg) ? 1 : 0,
                            manager.GetFlyingGoalCount(arg), manager.HaveAnyFlyingMatchItemsToCollect(arg) ? 1 : 0 }); break;
                    }
                }
                catch (Exception) { value = "E"; }
                var state = new List<string>();
                foreach (var board in manager.GetAllGoals()) foreach (var tuple in board) state.Add(State(tuple));
                result.Add(value + ":" + string.Join("/", state) + ":" + string.Join("/", Trace)); Trace.Clear();
            }
            Console.WriteLine(string.Join("|", result));
        }
    }
}
