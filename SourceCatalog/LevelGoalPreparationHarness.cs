using System;
using System.Collections.Generic;
using Whitebox.Foundation;
using Whitebox.Foundation.ConfigData;
namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
sealed class Conditions : IOriginalGoalConditionServices
{
    public bool HasOperation(int operation) => false;
    public bool IsGoalReached(int type) => false;
}
static class Verify
{
    static int[] Numbers(string value) => value.Length == 0 ? new int[0] : Array.ConvertAll(value.Split(','), int.Parse);
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#'); var head = Numbers(parts[0]);
            var level = new FTiledLevel { Grid = new FTiledGrid { Width = head[0], Height = head[1], Items = Numbers(parts[1]) }, Scroll = head[2] != 0,
                WaterData = head[3] == 0 ? null : new FWaterData { WaterReceivableCellCount = head[3], BoardCount = head[4] } };
            var goals = new List<FGoalData>(); var limits = new List<FLimitData>(); var curtains = new List<FItemCurtainData>();
            if (parts[2].Length > 0) foreach (var record in parts[2].Split(';'))
            {
                var data = Numbers(record); goals.Add(new FGoalData { Goal = data[0], Count = data[1], IsFromSettings = data[2] != 0, BoardId = data[3] });
            }
            if (parts[3].Length > 0) foreach (var record in parts[3].Split(';'))
            {
                var data = Numbers(record); limits.Add(new FLimitData { Limit = data[0], Count = data[1], BoardId = data[2] });
            }
            if (parts[4].Length > 0) foreach (var record in parts[4].Split(';'))
            {
                var data = record.Split('/'); curtains.Add(new FItemCurtainData { MinY = int.Parse(data[0]), Goals = Numbers(data[1]),
                    Targets = data[2] == "-" ? null : Numbers(data[2]) });
            }
            level.Goals = goals.ToArray(); level.Limits = limits.ToArray(); level.ItemCurtains = curtains.ToArray();
            var parser = new OriginalLevelGoalPreparation(level, new Conditions());
            var parsedGoals = new List<Dictionary<int, OriginalGoalTuple>>(); var parsedLimits = new List<Dictionary<int, OriginalLimitTuple>>();
            string status = "1";
            try { parser.ParseGoals(parsedGoals); } catch (Exception) { status = "E"; }
            try { parser.ParseLimits(parsedLimits); } catch (Exception) { status += "E"; }
            var goalOutput = new List<string>();
            foreach (var board in parsedGoals)
            {
                var output = new List<string>();
                foreach (var entry in board)
                {
                    var t = entry.Value; output.Add(string.Join(",", new[] { entry.Key, t.GoalType, t.TargetCount, t.LeftCount, t.LeftCountUi, t.ClaimedCount, t.IsFromSettings ? 1 : 0 }));
                }
                goalOutput.Add(string.Join(";", output));
            }
            var limitOutput = new List<string>();
            foreach (var board in parsedLimits)
            {
                var output = new List<string>(); foreach (var entry in board) output.Add(entry.Key + "," + entry.Value.GoalType + "," + entry.Value.Limit);
                limitOutput.Add(string.Join(";", output));
            }
            var claimOutput = new List<string>();
            foreach (var claim in parser.GoalClaims) claimOutput.Add(claim.GoalType + "," + claim.Count + "," + claim.BoardId);
            var rows = new List<string>();
            for (int y = -1; y <= head[1]; y++) try { rows.Add(parser.GetBoardIdForRow(y).ToString()); } catch (Exception) { rows.Add("E"); }
            var initialized = new List<string>(); var notifications = new List<string>();
            if (status == "1")
            {
                var manager = new OriginalGoalManager(() => 0, new OriginalGameOperations(() => 42), new OriginalGoalClaims());
                manager.OnGoalUpdated += (type, count) => notifications.Add("m," + type + "," + count);
                manager.OnGoalUpdatedUi += (type, count, prerequisite, start, complete) => notifications.Add("u," + type + "," + count + ","
                    + (prerequisite ? 1 : 0) + (start ? 1 : 0) + (complete ? 1 : 0));
                manager.InitWithGoalsAndLimits(parsedGoals, parsedLimits, head[5] != 0);
                foreach (var board in manager.GetAllGoals()) foreach (var tuple in board)
                    initialized.Add(string.Join(",", new[] { tuple.GoalType, tuple.TargetCount, tuple.LeftCount, tuple.LeftCountUi, tuple.ClaimedCount }));
            }
            Console.WriteLine(status + "#" + (parser.IsBonusLevel ? "1" : "0") + "#" + parser.GetBoardCount() + "#" + string.Join("|", goalOutput)
                + "#" + string.Join("|", limitOutput) + "#" + string.Join(";", claimOutput) + "#" + string.Join(",", rows)
                + "#" + string.Join(";", initialized) + "#" + string.Join(";", notifications));
        }
    }
}
