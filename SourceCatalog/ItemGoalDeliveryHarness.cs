using System;
using System.Collections.Generic;
using Whitebox.Foundation;

// Storage/goal-notification boundary; actual GoalManager integration is checked
// separately in Unity and goal_state_native_verification.json.
namespace Whitebox.Foundation
{
    public sealed class OriginalBoardItem { public int GoalType; }
    public interface IOriginalItemGoalServices
    { void DecreaseGoal(int type); void CollectAsGoalUi(OriginalBoardItem item); }
    public sealed class OriginalGoalManager
    {
        public void DecreaseGoal(int type) { Verify.Trace.Add("m," + type); }
        public void DecreaseGoalUi(int type)
        {
            Verify.Trace.Add("u," + type);
            if (Verify.Values[1] != -999) Verify.Item.GoalType = Verify.Values[1];
        }
    }
}
sealed class Context : IOriginalProgressLevelContext
{
    public bool IsStoryLevel { get { Verify.Trace.Add("S"); return Verify.Values[3] != 0; } }
    public bool IsFinalBoard { get { Verify.Trace.Add("F"); return Verify.Values[4] != 0; } }
    public bool IsPortalLevel { get { Verify.Trace.Add("P"); return Verify.Values[5] != 0; } }
}
sealed class Story : IOriginalStoryGoalProgress
{ public void IncrementProgress(int type) { Verify.Trace.Add("s," + type); } }
sealed class Board : IOriginalBoardGoalProgress
{
    public void IncrementProgress(int type, IOriginalMultipleBoardProgressTarget target)
    {
        if (!ReferenceEquals(target, Verify.Target)) throw new Exception("Original target object must be forwarded unchanged.");
        Verify.Trace.Add("b," + type + "," + (target == null ? 0 : 1));
    }
}
sealed class Target : IOriginalMultipleBoardProgressTarget { }
static class Verify
{
    public static readonly List<string> Trace = new List<string>();
    public static int[] Values;
    public static OriginalBoardItem Item;
    public static IOriginalMultipleBoardProgressTarget Target;
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split(';'); int type = parts[0] == "C" || parts[0] == "G" ? 0 : int.Parse(parts[1]);
            if (parts[0] == "I") { Console.WriteLine(OriginalGoalTypes.FromItemType(type)); continue; }
            if (parts[0] == "M") { Console.WriteLine(OriginalGoalTypes.FromMatchType(type)); continue; }
            if (parts[0] == "S") { Console.WriteLine(OriginalGoalTypes.FromStaticItemType(type)); continue; }
            if (parts[0] == "T") { Console.WriteLine(OriginalGoalTypes.FromTiledId(type)); continue; }
            if (parts[0] == "P") { Console.WriteLine(OriginalGoalTypes.ShouldPlayParticleOnCollect(type) ? 1 : 0); continue; }
            Values = Array.ConvertAll(parts[1].Split(','), int.Parse); Trace.Clear();
            Item = new OriginalBoardItem { GoalType = Values[0] };
            var progress = new OriginalGameUiProgress(new Context(), Values[6] == 0 ? null : new Story(), Values[7] == 0 ? null : new Board());
            var goals = new OriginalItemGoalServices(new OriginalGoalManager(), () =>
            {
                Trace.Add("R");
                if (Values[2] != -999) Item.GoalType = Values[2];
                return Values[8] == 0 || Values[9] == 0 ? null : progress;
            });
            Target = Values[10] == 0 ? null : new Target();
            try
            {
                if (parts[0] == "C") goals.CollectAsGoalUi(Item, Target);
                else progress.IncrementProgress(Item.GoalType, Target);
                Trace.Add("ok");
            }
            catch (NullReferenceException) { Trace.Add("error"); }
            Console.WriteLine(string.Join(";", Trace));
        }
    }
}
