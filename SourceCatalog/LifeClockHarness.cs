using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public interface IOriginalWinInventory { int Coin { get; } }
    public class OriginalActiveLevelData
    { public bool IsSideProgressionLevel, IsLeague, IsLeagueHard, IsLeagueSuperHard, IsLeagueExtremelyHard; public int Difficulty; }
}
public static class LifeClockHarness
{
    static long[] v;
    static readonly List<string> trace = new List<string>();
    static readonly string[] keys = { "LCP", "LST", "OULT", "ELC", "ST", "STD", "LCT" };
    static readonly Dictionary<string, long> rows = new Dictionary<string, long>();
    static OriginalLifeClock clock;
    static OriginalServerTimePublication publication;
    static OriginalLocalUserData user;
    static OriginalBasicLevelProgress basic, second;
    static int reads, ports;
    static void Record(string value) { trace.Add(value); if (++ports == v[23]) throw new InvalidOperationException(); }
    static void Mutate(int change)
    {
        switch (change)
        {
            case 1: clock.ServerClientTimeDiff = 7777; break;
            case 2: clock.IsTimeValidatedByBackend = !clock.IsTimeValidatedByBackend; break;
            case 3: clock.LifeHelper = null; break;
            case 4: clock.IsOfflineClientTimeTakenBack = !clock.IsOfflineClientTimeTakenBack; break;
            case 5: clock.CurrentEarnedLifeCount = 7; break;
            case 6: user.Basic = second; break;
            case 7: user.Basic = null; break;
            case 8: publication.ShouldRead = true; break;
        }
    }
    sealed class Tracker : IOriginalTimeManipulationTracker
    {
        public bool Daily;
        public bool IsTimeManipulationDetected => v[Daily ? 26 : 27] != 0;
        public bool IsThereTimeManipulation(long serverTime) { Record((Daily ? "daily-check," : "weekly-check,") + serverTime); return v[Daily ? 28 : 29] != 0; }
    }
    sealed class Context : IOriginalLifeClockStorage, IOriginalClientClock, IOriginalLifeClockContext, IOriginalLifeClockLives,
        IOriginalWeeklyTimeOwner, IOriginalKeyValueConnection
    {
        readonly Tracker daily = new Tracker { Daily = true }, weekly = new Tracker();
        public int MaxPossibleLivesToHave { get { Record("max," + v[11]); return (int)v[11]; } }
        public float TimeSpentOnPaused => BitConverter.ToSingle(BitConverter.GetBytes(unchecked((int)v[14])), 0);
        public OriginalLocalUserData GetCurrentUser() => (v[22] & 16) != 0 ? null : user;
        public IOriginalTimeManipulationTracker GetDailyRewards() { Record("daily"); return (v[22] & 2) != 0 ? null : daily; }
        public IOriginalWeeklyTimeOwner GetWeeklyMission() { Record("weekly"); return (v[22] & 4) != 0 ? null : this; }
        public IOriginalTimeManipulationTracker DailyLoginTracker => (v[22] & 8) != 0 ? null : weekly;
        public bool IsThereLifeHack(long serverTime) { Record("hack," + serverTime); return v[30] != 0; }
        public void SendPing(int reason) { Record("ping," + reason); }
        public int GetLives() { Record("l," + v[10]); return (int)v[10]; }
        public long RequiredTimeInMsToFillOneLife => 1800000;
        public long PassedTimeMsForNextLife() { Record("passed," + v[12]); return v[12]; }
        public long RemainingMsToEndUnlimitedTimes() { Record("unlimited," + v[13]); return v[13]; }
        public long CurrentTimeInMs()
        {
            long now = unchecked(v[2] + reads * v[3]); reads++; Record("now," + now);
            if (reads == v[24]) Mutate((int)v[25]); return now;
        }
        public int GetInt(string key, int defaultValue = 0) { Record("gi," + key + "," + defaultValue); return rows.TryGetValue(key, out long value) ? (int)value : defaultValue; }
        public long GetLong(string key, long defaultValue = 0) { Record("gl," + key + "," + defaultValue); return rows.TryGetValue(key, out long value) ? value : defaultValue; }
        public bool SetInt(string key, int value) { Record("si," + key + "," + value); rows[key] = value; return true; }
        public bool SetLong(string key, long value) { Record("sl," + key + "," + value); rows[key] = value; return true; }
        public bool DeleteKey(string key) { Record("del," + key); return rows.Remove(key); }
        public int InsertOrReplace(OriginalKeyValue row) { Record("user," + row.Key + "," + row.Value); return 1; }
        public OriginalKeyValue Find(string key) => throw new InvalidOperationException();
        public int Delete(string key) => throw new InvalidOperationException();
        public int UpdateSynced(string key, int value) => throw new InvalidOperationException();
        public int UpdateSyncBlocked(string key, int value) => throw new InvalidOperationException();
        public void BeginTransaction() { } public void Commit() { } public void Dispose() { }
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            v = Array.ConvertAll(line.Split(','), long.Parse); trace.Clear(); rows.Clear(); reads = ports = 0;
            for (int i = 0; i < keys.Length; i++) if (v[15 + i] != long.MinValue) rows[keys[i]] = v[15 + i];
            var context = new Context(); var values = new OriginalUserKeyValue(() => new OriginalDatabaseService(context, 61, null));
            basic = new OriginalBasicLevelProgress(1, 0, null) { FullLivesTimeInMs = v[36], UnlimitedLivesEndTimeInMs = v[37] }; basic.BindLifeStorage(values);
            second = new OriginalBasicLevelProgress(1, 0, null) { FullLivesTimeInMs = unchecked(v[36] + 23), UnlimitedLivesEndTimeInMs = unchecked(v[37] + 29) }; second.BindLifeStorage(values);
            var inventory = new OriginalUserInventoryBalances(0, 0, 0, null) { UnlimitedBoosterInventory = new BitArray(192, true) };
            inventory.BindLifeStorage(values);
            user = new OriginalLocalUserData { Basic = (v[22] & 32) != 0 ? null : basic, Inventory = (v[22] & 64) != 0 ? null : inventory };
            publication = new OriginalServerTimePublication { ShouldRead = v[34] != 0, HasTime = v[35] != 0 };
            clock = new OriginalLifeClock(context, context, context, publication) { LifeHelper = (v[22] & 1) != 0 ? null : context,
                LastClientTime = v[4], ServerClientTimeDiff = v[5], IsTimeValidatedByBackend = v[6] != 0, IsOfflineClientTimeTakenBack = v[7] != 0,
                IsLifeHackDetected = v[8] != 0, AppWillPauseToShowAd = v[9] != 0, CurrentEarnedLifeCount = (int)v[33] };
            if (v[31] != 0) clock.OnTimeValidatedByBackend += () => { Record("validated"); Mutate((int)v[32]); };
            long result = 0; string error = "";
            try
            {
                switch ((int)v[0])
                {
                    case 0: result = clock.CurrentServerTimeInMs(); break;
                    case 1: result = clock.CurrentServerTimeInSeconds(); break;
                    case 2: clock.CheckClientTime(); break;
                    case 3: clock.OnAppPause(); break;
                    case 4: clock.RecalculateAfterPause(); break;
                    case 5: clock.OnAppStart(); break;
                    case 6: clock.OnAppResume(); break;
                    case 7: result = clock.UpdateTime(v[1]) ? 1 : 0; break;
                    case 8: result = publication.HasServerTimeSetAtLeastOnce(context) ? 1 : 0; break;
                    case 9: clock.LoadStoredClockState(); break;
                }
            }
            catch (NullReferenceException) { error = "null"; }
            catch (InvalidOperationException) { error = "port"; }
            var state = new List<string> { result.ToString(), clock.IsTimeValidatedByBackend ? "1" : "0", clock.IsOfflineClientTimeTakenBack ? "1" : "0",
                clock.AppWillPauseToShowAd ? "1" : "0", clock.LastClientTime.ToString(), clock.ServerClientTimeDiff.ToString(), clock.CurrentEarnedLifeCount.ToString(),
                basic.FullLivesTimeInMs.ToString(), basic.UnlimitedLivesEndTimeInMs.ToString(), second.FullLivesTimeInMs.ToString(), second.UnlimitedLivesEndTimeInMs.ToString(),
                publication.ShouldRead ? "1" : "0", publication.HasTime ? "1" : "0" };
            foreach (string key in keys) state.Add(rows.TryGetValue(key, out long value) ? value.ToString() : "-");
            Console.WriteLine(string.Join(",", state) + "," + error + "#" + string.Join(";", trace));
        }
    }
}
