using System;
using System.Collections.Generic;
using Whitebox.Foundation;

// Unrelated progression types are compile-only boundaries. All life models,
// helper arithmetic, key/value writes and paused-time packing use runtime code.
namespace Whitebox.Foundation
{
    public interface IOriginalWinInventory { int Coin { get; } }
    public class OriginalActiveLevelData
    {
        public bool IsSideProgressionLevel, IsLeague, IsLeagueHard, IsLeagueSuperHard, IsLeagueExtremelyHard;
        public int Difficulty;
    }
}
public static class LifeHelperHarness
{
    static long[] v;
    static readonly List<string> trace = new List<string>();
    static OriginalLocalUserData user, other;
    static OriginalBasicLevelProgress basic, second;
    static OriginalUserInventoryBalances inventory, secondInventory;
    static int clockReads, writes;
    sealed class Context : IOriginalLifeHelperContext, IOriginalLifeTimeHelper, IOriginalKeyValueConnection
    {
        public OriginalLocalUserData Current;
        public Action Started;
        public bool Pass, Back;
        public OriginalLocalUserData GetCurrentUser() => Current;
        public bool HasPassGoldStatus(OriginalLocalUserData value) { trace.Add("p," + (Pass ? 1 : 0)); return Pass; }
        public IOriginalLifeTimeHelper GetTimeHelper() { trace.Add("time-owner"); return (v[6] & 8) != 0 ? null : this; }
        public void AddOnApplicationStart(Action action) { trace.Add("subscribe"); Started += action; }
        public bool IsOfflineClientTimeTakenBack { get { trace.Add("back," + (Back ? 1 : 0)); return Back; } }
        public bool IsUserContextActive { get { trace.Add("active," + v[4]); return v[4] != 0; } }
        public bool GetAfterPaused() { trace.Add("paused," + v[5]); return v[5] != 0; }
        public void RecalculateAfterPause() { trace.Add("recalculate"); }
        public void TrackLifeTransaction(int amount) { trace.Add("track," + amount); }
        public long CurrentServerTimeInMs()
        {
            long now = unchecked(v[12] + clockReads * v[13]); clockReads++;
            trace.Add("t," + now);
            if (clockReads == v[8]) switch ((int)v[9])
            {
                case 1: Current = other; break;
                case 2: user.Basic = second; break;
                case 3: user.Basic = null; break;
                case 4: basic.FullLivesTimeInMs = unchecked(now + 3600000); break;
                case 5: basic.UnlimitedLivesEndTimeInMs = unchecked(now + 10000); break;
                case 6: Pass = !Pass; break;
                case 7: Back = !Back; break;
                case 8: Current = null; break;
                case 9: user.Inventory = secondInventory; break;
            }
            return now;
        }
        public int InsertOrReplace(OriginalKeyValue row)
        {
            trace.Add((row.Key == "FullLivesTimeInMs" ? "F," : row.Key == "UnlimitedLivesEndTimeInMs" ? "U," : "C,") + row.Value);
            if (++writes == v[7]) throw new InvalidOperationException();
            return 1;
        }
        public OriginalKeyValue Find(string key) => throw new InvalidOperationException("Unexpected read");
        public int Delete(string key) => throw new InvalidOperationException();
        public int UpdateSynced(string key, int value) => throw new InvalidOperationException();
        public int UpdateSyncBlocked(string key, int value) => throw new InvalidOperationException();
        public void BeginTransaction() { }
        public void Commit() { }
        public void Dispose() { }
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            v = Array.ConvertAll(line.Split(','), long.Parse); trace.Clear(); clockReads = writes = 0;
            var context = new Context { Pass = v[2] != 0, Back = v[3] != 0 };
            var db = new OriginalDatabaseService(context, 61, null);
            var values = new OriginalUserKeyValue(() => db);
            basic = new OriginalBasicLevelProgress(1, 0, null) { FullLivesTimeInMs = v[10], UnlimitedLivesEndTimeInMs = v[11] };
            second = new OriginalBasicLevelProgress(1, 0, null) { FullLivesTimeInMs = unchecked(v[10] + 12345), UnlimitedLivesEndTimeInMs = unchecked(v[11] + 54321) };
            inventory = new OriginalUserInventoryBalances(2000, 0, 0, null) { Chest = unchecked((int)v[14]) };
            secondInventory = new OriginalUserInventoryBalances(2000, 0, 0, null) { Chest = unchecked((int)(v[14] + 70000)) };
            basic.BindLifeStorage(values); second.BindLifeStorage(values); inventory.BindLifeStorage(values); secondInventory.BindLifeStorage(values);
            user = new OriginalLocalUserData { Basic = (v[6] & 2) != 0 ? null : basic, Inventory = (v[6] & 4) != 0 ? null : inventory };
            other = new OriginalLocalUserData { Basic = second, Inventory = secondInventory };
            context.Current = (v[6] & 1) != 0 ? null : user;
            var life = new OriginalLifeHelper(context); long result = 0; string error = "";
            try
            {
                life.Bind();
                switch ((int)v[0])
                {
                    case 0: result = life.MaxPossibleLivesToHave; break;
                    case 1: context.Started(); break;
                    case 2: result = life.GetLives(); break;
                    case 3: result = life.IsFull() ? 1 : 0; break;
                    case 4: result = life.HasLives() ? 1 : 0; break;
                    case 5: result = life.HasUnlimitedLives() ? 1 : 0; break;
                    case 6: life.RefillLives(); break;
                    case 7: life.IncrementLives(unchecked((int)v[1])); break;
                    case 8: life.DecrementLives(); break;
                    case 9: result = life.RemainingSecondsToNextLife(); break;
                    case 10: result = life.PassedTimeMsForNextLife(); break;
                    case 11: result = life.RemainingSecondsToEndUnlimitedTimes(); break;
                    case 12: result = life.RemainingMsToEndUnlimitedTimes(); break;
                    case 13: life.AddUnlimitedLivesInMinutes(unchecked((int)v[1])); break;
                    case 14: life.AddUnlimitedLivesInSeconds(unchecked((int)v[1])); break;
                    case 15: basic.UpdateFullLivesTimeInMs(v[1]); break;
                    case 16: basic.UpdateUnlimitedLivesEndTimeInMs(v[1]); break;
                    case 17: basic.SubstractTimeFromFullLivesTimeInMs(v[1]); break;
                    case 18: result = inventory.GetPausedUnlimitedLivesTimeInMinutes(); break;
                    case 19: inventory.UpdatePausedUnlimitedLivesTimeInMinutes(unchecked((int)v[1])); break;
                    case 20: result = life.RequiredTimeInMsToFillOneLife; break;
                }
            }
            catch (NullReferenceException) { error = "null"; }
            catch (InvalidOperationException) { error = "write"; }
            Console.WriteLine(result + "," + basic.FullLivesTimeInMs + "," + basic.UnlimitedLivesEndTimeInMs + "," + inventory.Chest + "," + second.FullLivesTimeInMs + "," + second.UnlimitedLivesEndTimeInMs + "," + secondInventory.Chest + "," + error + "#" + string.Join(";", trace));
        }
    }
}
