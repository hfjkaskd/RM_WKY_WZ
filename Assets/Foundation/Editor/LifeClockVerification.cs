using System;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class LifeClockVerification
    {
        private sealed class Client : IOriginalClientClock
        { public long Now = 1700000000000; public long CurrentTimeInMs() => Now; }
        private sealed class Tracker : IOriginalTimeManipulationTracker
        {
            public bool Manipulates;
            public bool IsTimeManipulationDetected => false;
            public bool IsThereTimeManipulation(long serverTime) => Manipulates;
        }
        private sealed class Context : IOriginalLifeClockContext, IOriginalLifeHelperContext, IOriginalWeeklyTimeOwner, IOriginalLocalAccountLoadContext, IOriginalAreaDataContext
        {
            public OriginalLocalUserData User;
            public OriginalUserKeyValue Values;
            public readonly Client Client = new Client();
            public OriginalLifeClock Clock;
            public OriginalLifeHelper Life;
            public Action Started;
            public readonly Tracker Daily = new Tracker(), Weekly = new Tracker();
            public float PausedSeconds;
            public bool AfterPaused;
            public int Pings, LastPing;
            public int MaxPossibleLivesToHave => Life.MaxPossibleLivesToHave;
            public float TimeSpentOnPaused => PausedSeconds;
            public OriginalLocalUserData GetCurrentUser() => User;
            public IOriginalTimeManipulationTracker GetDailyRewards() => Daily;
            public IOriginalWeeklyTimeOwner GetWeeklyMission() => this;
            public IOriginalTimeManipulationTracker DailyLoginTracker => Weekly;
            public bool IsThereLifeHack(long serverTime) => false; // Explicit controlled detector input.
            public void SendPing(int reason) { Pings++; LastPing = reason; } // Explicit backend boundary.
            public bool HasPassGoldStatus(OriginalLocalUserData user) => false;
            public IOriginalLifeTimeHelper GetTimeHelper() => Clock;
            public void AddOnApplicationStart(Action action) { Started += action; }
            public void TrackLifeTransaction(int amount) { }
            public bool IsUserContextActive => true;
            public bool GetAfterPaused() => AfterPaused;
            public long CurrentTimeInMilliseconds => Client.CurrentTimeInMs();
            public void ParseAndSetAreaFromLocalStorage() => new OriginalAreaDataParser(Values, this).ParseAndSetFromLocalStorage();
            public OriginalUserAreaData GetCurrentAreaData() => User.Area;
            public IOriginalAreaConfigContainer CreateConfigContainer() => new OriginalAreaConfigContainer();
            public IOriginalAreaConfigContainer GetHomeConfigContainer() => throw new NotSupportedException("Fresh area only.");
            public int PlayableAreaCount => throw new NotSupportedException("Fresh area only.");
            public IOriginalAreaAssetAvailability GetAssetBundleHelper() => throw new NotSupportedException("Fresh area only.");
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static OriginalLocalUserData NewUser(OriginalUserKeyValue values)
        {
            var user = new OriginalLocalUserData { UserId = new OriginalLocalUserIdentity(), Basic = new OriginalBasicLevelProgress(0, 0, null),
                Inventory = new OriginalUserInventoryBalances(0, 0, 0, null), Area = new OriginalUserAreaData(values), Log = new OriginalLocalLogData(), Story = new OriginalLocalStoryData() };
            user.Basic.BindLifeStorage(values); user.Inventory.BindLifeStorage(values); return user;
        }
        public static void Run()
        {
            var report = new Report();
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "royal-clock-" + Guid.NewGuid().ToString("N"));
                using (var userSql = OriginalRuntimeStorage.Open(path + ".user"))
                using (var appSql = OriginalRuntimeStorage.Open(path + ".app"))
                {
                    var userDb = new OriginalDatabaseService(userSql, 61, () => { }); userDb.OnApplicationResume();
                    var appDb = new OriginalDatabaseService(appSql, 67, () => { }); appDb.OnApplicationResume();
                    var values = new OriginalUserKeyValue(() => userDb); int appResolves = 0;
                    var app = new OriginalAppKeyValue(() => { appResolves++; return appDb; });
                    var context = new Context { Values = values, User = NewUser(values) };
                    OriginalLocalAccountLoad.UpdateDataFromLocalStorage(context, values);
                    var publication = new OriginalServerTimePublication();
                    var clock = context.Clock = new OriginalLifeClock(app, context.Client, context, publication);
                    var life = context.Life = new OriginalLifeHelper(context); clock.LifeHelper = new OriginalLifeClockAdapter(life);
                    clock.LoadStoredClockState(); life.Bind(); context.Started();
                    Require(clock.GetServerClientTimeDiff() == 0 && !userDb.ShouldSave && !appDb.ShouldSave && life.GetLives() == 5 &&
                        !publication.HasServerTimeSetAtLeastOnce(app) && appResolves == 1, "Fresh user/application reads use separate cached databases without default writes");
                    clock.OnAppStart(); Require(context.LastPing == 1 && context.Pings == 1, "Native start checks client time then requests reason1 ping");
                    life.DecrementLives(); context.Client.Now++;
                    Require(clock.UpdateTime(context.Client.Now) && clock.IsTimeValidatedByBackend && publication.HasServerTimeSetAtLeastOnce(app), "Accepted supplied server time publishes validated state/cache");
                    clock.OnAppPause(); long firstReference = context.Client.Now - 1000 - 60000;
                    Require(app.GetLong("LST") == firstReference && app.GetInt("LCP") == 4 && !app.HasKey("OULT") && !clock.IsTimeValidatedByBackend,
                        "Validated pause stores native recovery reference/count and clears offline unlimited duration");
                    report.validated_pause_and_entry_debit_verified = true;
                    context.Client.Now += 1802000; clock.OnAppResume();
                    Require(context.LastPing == 2 && context.Pings == 2 && life.GetLives() == 5, "Resume earns a real life from elapsed client time");
                    context.PausedSeconds = 1802.75f; clock.OnAppPause();
                    Require(app.GetLong("LST") == firstReference + 1740000 && app.GetInt("ELC") == 1 && app.GetLong("OULT") == 0,
                        "Offline pause truncates seconds before milliseconds, advances native reference and accumulates actual earned lives");
                    report.offline_resume_and_pause_storage_verified = true;
                    context.Client.Now -= 60001; clock.OnAppResume();
                    Require(clock.IsOfflineClientTimeTakenBack && !life.HasUnlimitedLives(), "Native greater-than60000 rollback gate reaches actual LifeHelper");
                    app.SetLong("OULT", 9000);
                    values.SetLong("RET",12345);values.SetLong("TET",23456);values.SetLong("LET",34567);
                    values.SetInt("Chest",0x7f00ffff);values.SetLong("RemainingBoosterTimes",-1);
                    values.SetLong("MultiplierEndTimes",-1);values.SetLong("Feature2",-1);
                    var boosterFields=new string[25];for(int i=0;i<24;i++)boosterFields[i]="-1";boosterFields[24]="-128";
                    values.SetString(OriginalLocalAccountLoad.UnlimitedBoosterKey,string.Join("\u00a7",boosterFields));
                    OriginalLocalAccountLoad.UpdateDataFromLocalStorage(context,values);
                    var timedInventory=context.User.Inventory;var retainedBits=timedInventory.UnlimitedBoosterInventory;
                    long suppliedServer = context.Client.Now + 200000;
                    Require(clock.UpdateTime(suppliedServer) && !clock.IsOfflineClientTimeTakenBack && context.User.Basic.UnlimitedLivesEndTimeInMs == 1,
                        "Server validation performs the actual rollback reset and persists native unlimited end1");
                    Require(ReferenceEquals(timedInventory,context.User.Inventory)&&timedInventory.RocketEndTime==0&&timedInventory.TntEndTime==0&&timedInventory.LightBallEndTime==0&&
                        timedInventory.Chest==0x7f000000&&timedInventory.RemainingBoosterTimes==0&&timedInventory.MultiplierEndTimes==(-1L&~(2147483647L<<31))&&
                        timedInventory.Feature2==(-1L&~(65535L<<26)),"Actual ResetTimes clears only native time ranges while preserving unrelated packed inventory");
                    Require(ReferenceEquals(retainedBits,timedInventory.UnlimitedBoosterInventory)&&retainedBits.Length==200&&retainedBits[199],"Unlimited reset retains identity and bits beyond191");
                    for(int i=0;i<192;i++)Require(!retainedBits[i],"Actual unlimited-booster time bits clear");
                    Require(values.GetString(OriginalLocalAccountLoad.UnlimitedBoosterKey,null)==OriginalUnlimitedBoosterSerialization.Serialize(retainedBits),"Native signed-byte serialized reset is actually stored");
                    report.actual_inventory_time_reset_verified=report.unlimited_booster_tail_preserved_verified=true;
                    Require(clock.GetServerClientTimeDiff() == 200000 && clock.CurrentServerTimeInMs() == suppliedServer &&
                        values.GetLong("FullLivesTimeInMs") == context.User.Basic.FullLivesTimeInMs, "Offset change recalculates actual full-life timestamp through user storage");
                    report.rollback_and_server_offset_verified = true;
                    context.AfterPaused = true; life.IncrementLives(0); context.AfterPaused = false;
                    Require(!clock.IsTimeValidatedByBackend && app.GetInt("LCP") == 5, "Actual LifeHelper grant routes through real RecalculateAfterPause rather than a count-only callback");
                    report.life_grant_recalculates_actual_clock_verified = true;
                    context.Daily.Manipulates = true;
                    Require(!clock.UpdateTime(0) && !clock.IsTimeValidatedByBackend && clock.GetServerClientTimeDiff() == 200000 &&
                        !publication.HasServerTimeSetAtLeastOnce(app), "Rejected time still updates ST/cache but retains unvalidated clock offset");
                    context.Daily.Manipulates = false;
                    int pings = context.Pings; clock.AppWillPauseToShowAd = true; clock.OnAppResume();
                    Require(!clock.AppWillPauseToShowAd && context.Pings == pings, "Ad resume clears its flag after client check without a second ping");
                    report.rejected_time_and_ad_resume_verified = true;
                    foreach (string key in new[] { "ST", "STD", "LCT", "LST", "LCP", "ELC" })
                    { var row = appSql.Find(key); Require(row.Synced == 0 && row.SyncBlocked == 1, "Application writes retain native local sync flags"); }
                    foreach(string key in new[]{"RET","TET","LET","Chest","RemainingBoosterTimes","MultiplierEndTimes","Feature2",OriginalLocalAccountLoad.UnlimitedBoosterKey})
                    {var row=userSql.Find(key);Require(row.Synced==0&&row.SyncBlocked==1,"Actual reset rows retain native local sync flags");}
                    appDb.ManualUpdate(66); Require(appDb.ShouldSave, "Application DB retains native67-frame save interval");
                    appDb.ManualUpdate(67); Require(!appDb.ShouldSave, "Application DB commits at frame67"); userDb.Save();
                    using (var reopenedUser = OriginalRuntimeStorage.Open(path + ".user"))
                    using (var reopenedApp = OriginalRuntimeStorage.Open(path + ".app"))
                    {
                        var reloadUserDb = new OriginalDatabaseService(reopenedUser, 61, () => { });
                        var reloadAppDb = new OriginalDatabaseService(reopenedApp, 67, () => { });
                        var reloadValues = new OriginalUserKeyValue(() => reloadUserDb);
                        var reload = new Context { Values = reloadValues, User = NewUser(reloadValues) }; reload.Client.Now = context.Client.Now;
                        OriginalLocalAccountLoad.UpdateDataFromLocalStorage(reload, reloadValues);
                        var restored = new OriginalLifeClock(new OriginalAppKeyValue(() => reloadAppDb), reload.Client, reload, new OriginalServerTimePublication());
                        restored.LoadStoredClockState();
                        Require(restored.GetServerClientTimeDiff() == 200000 && restored.CurrentServerTimeInMs() == clock.CurrentServerTimeInMs() &&
                            reload.User.Basic.FullLivesTimeInMs == context.User.Basic.FullLivesTimeInMs && reload.User.Basic.UnlimitedLivesEndTimeInMs == 1 &&
                            !reloadUserDb.ShouldSave && !reloadAppDb.ShouldSave, "Actual user/application SQLite reopen restores life/clock state without writes");
                        var restoredInventory=reload.User.Inventory;
                        Require(restoredInventory.RocketEndTime==0&&restoredInventory.TntEndTime==0&&restoredInventory.LightBallEndTime==0&&restoredInventory.Chest==timedInventory.Chest&&
                            restoredInventory.MultiplierEndTimes==timedInventory.MultiplierEndTimes&&restoredInventory.Feature2==timedInventory.Feature2&&restoredInventory.RemainingBoosterTimes==0&&
                            restoredInventory.UnlimitedBoosterInventory.Length==200&&restoredInventory.UnlimitedBoosterInventory[199],"Actual native loader restores reset time fields and retained tail bits");
                    }
                    report.separate_app_user_databases_and_reopen_verified = true;
                }
                report.passed = true;
            }
            catch (Exception failure) { report.failure = failure.ToString(); }
            File.WriteAllText(Path.Combine(Application.dataPath, "../live_life_clock_verification.json"), JsonUtility.ToJson(report, true));
        }
        [Serializable] private sealed class Report
        {
            public bool passed, validated_pause_and_entry_debit_verified, offline_resume_and_pause_storage_verified, rollback_and_server_offset_verified,
                life_grant_recalculates_actual_clock_verified, rejected_time_and_ad_resume_verified, separate_app_user_databases_and_reopen_verified,
                actual_inventory_time_reset_verified,unlimited_booster_tail_preserved_verified;
            public string failure;
            public string scope = "Actual runtime LifeClock/LifeHelper/Basic/Inventory.ResetTimes, signed-byte unlimited-booster serialization and typed AppKeyValue/UserKeyValue with separate private Windows SQLite connections, native local-account/area load,67/61 save intervals and reopen. ResetTimes is called directly by runtime time validation; scalar/packed ranges,192 cleared bits and retained bit199/200-bit length/identity/storage/reload are checked. Supplied client/server times, daily/weekly/life-hack detector results and ping transport remain explicit verification ports. Not full TimeHelper context/HTTP/detector implementations, production scene/account binding, Home or mobile proof.";
        }
    }
}
