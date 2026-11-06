using System;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Clock, pass status, app start/resume and analytics are controlled inputs.
    // LifeHelper, real local account loader/models and SQLite remain runtime code.
    public static class LifeHelperVerification
    {
        private sealed class Context : IOriginalLifeHelperContext, IOriginalLifeTimeHelper, IOriginalLocalAccountLoadContext, IOriginalAreaDataContext
        {
            public OriginalLocalUserData User;
            public OriginalUserKeyValue Values;
            public long Now = 1700000000000;
            public bool Gold, Back, Active, Paused;
            public int Recalculations, Transactions, Subscriptions;
            public Action Started;
            public OriginalLocalUserData GetCurrentUser() => User;
            public bool HasPassGoldStatus(OriginalLocalUserData user) { Require(ReferenceEquals(user, User), "Current life user identity"); return Gold; }
            public IOriginalLifeTimeHelper GetTimeHelper() => this;
            public void AddOnApplicationStart(Action action) { Started += action; Subscriptions++; }
            public void TrackLifeTransaction(int amount) { Transactions += amount; }
            public bool IsUserContextActive => Active;
            public bool GetAfterPaused() => Paused;
            public bool IsOfflineClientTimeTakenBack => Back;
            public long CurrentServerTimeInMs() => Now;
            public void RecalculateAfterPause() { Recalculations++; }
            public long CurrentTimeInMilliseconds => Now;
            public void ParseAndSetAreaFromLocalStorage() => new OriginalAreaDataParser(Values, this).ParseAndSetFromLocalStorage();
            public OriginalUserAreaData GetCurrentAreaData() => User.Area;
            public IOriginalAreaConfigContainer CreateConfigContainer() => new OriginalAreaConfigContainer();
            public IOriginalAreaConfigContainer GetHomeConfigContainer() => throw new InvalidOperationException("Fresh area only.");
            public int PlayableAreaCount => throw new InvalidOperationException("Fresh area only.");
            public IOriginalAreaAssetAvailability GetAssetBundleHelper() => throw new InvalidOperationException("Fresh area only.");
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static OriginalLocalUserData CreateUser(OriginalUserKeyValue values)
        {
            var user = new OriginalLocalUserData { UserId = new OriginalLocalUserIdentity(), Basic = new OriginalBasicLevelProgress(0, 0, null),
                Inventory = new OriginalUserInventoryBalances(0, 0, 0, null), Log = new OriginalLocalLogData(), Story = new OriginalLocalStoryData(), Area = new OriginalUserAreaData(values) };
            user.Basic.BindLifeStorage(values); user.Inventory.BindLifeStorage(values); return user;
        }
        public static void Run()
        {
            var report = new Report();
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "royal-life-" + Guid.NewGuid().ToString("N") + ".sqlite");
                using (var connection = OriginalRuntimeStorage.Open(path))
                {
                    var db = new OriginalDatabaseService(connection, 61, () => { }); db.OnApplicationResume();
                    var values = new OriginalUserKeyValue(() => db);
                    var context = new Context { Values = values, User = CreateUser(values) };
                    OriginalLocalAccountLoad.UpdateDataFromLocalStorage(context, values);
                    var user = context.User; var life = new OriginalLifeHelper(context); life.Bind();
                    Require(context.Subscriptions == 1 && !db.ShouldSave && life.RequiredTimeInMsToFillOneLife == 1800000, "Native construction/bind does not store defaults");
                    context.Started();
                    Require(life.GetLives() == 5 && life.IsFull() && life.HasLives() && !db.ShouldSave, "Fresh native load starts full without writing defaults");
                    report.fresh_load_and_app_start_verified = true;
                    life.DecrementLives();
                    Require(user.Basic.FullLivesTimeInMs == context.Now + 1800000 && values.GetLong("FullLivesTimeInMs") == user.Basic.FullLivesTimeInMs, "Life debit writes native full timestamp");
                    Require(life.GetLives() == 3 && life.HasLives() && life.RemainingSecondsToNextLife() == 0, "Exact native interval boundary is retained");
                    context.Now++;
                    Require(life.GetLives() == 4 && life.RemainingSecondsToNextLife() == 1799 && life.PassedTimeMsForNextLife() == 1000, "First millisecond uses native integer-seconds display");
                    context.Now += 1800000;
                    Require(life.GetLives() == 5 && life.IsFull() && life.PassedTimeMsForNextLife() == 0, "Elapsed server time restores a life without a write");
                    for (int left = 4; left >= 0; left--) { life.DecrementLives(); context.Now++; Require(life.GetLives() == left, "Five normal lives can be consumed"); }
                    long exhausted = user.Basic.FullLivesTimeInMs; int before = context.Transactions;
                    life.DecrementLives(); Require(!life.HasLives() && user.Basic.FullLivesTimeInMs == exhausted && context.Transactions == before, "No duplicate debit at zero lives");
                    life.IncrementLives(2);
                    Require(life.GetLives() == 2 && user.Basic.FullLivesTimeInMs == exhausted - 3600000, "Granted lives subtract native recovery time");
                    report.debit_restore_exhaustion_verified = true;
                    context.Active = context.Paused = true; life.IncrementLives(0);
                    Require(context.Recalculations == 1, "Native active/paused zero grant still recalculates clock");
                    life.AddUnlimitedLivesInSeconds(60);
                    long limited = user.Basic.FullLivesTimeInMs;
                    life.DecrementLives();
                    Require(life.HasUnlimitedLives() && life.IsFull() && user.Basic.FullLivesTimeInMs == limited, "Unlimited lives suppress normal debit");
                    context.Back = true;
                    Require(!life.HasUnlimitedLives() && life.RemainingSecondsToEndUnlimitedTimes() == 60, "Rollback disables entitlement without erasing duration");
                    life.DecrementLives(); Require(user.Basic.FullLivesTimeInMs == limited + 1800000, "Rollback takes the normal life path");
                    context.Back = false;
                    user.Inventory.UpdateChest(unchecked((int)0x5a5a0007));
                    life.AddUnlimitedLivesInMinutes(3);
                    Require(user.Inventory.Chest == unchecked((int)0x5a5a000a) && values.GetInt("Chest") == user.Inventory.Chest && life.RemainingSecondsToEndUnlimitedTimes() == 240,
                        "Unlimited reward extends existing time and preserves native packed paused inventory");
                    long end = user.Basic.UnlimitedLivesEndTimeInMs;
                    life.AddUnlimitedLivesInSeconds(0); Require(user.Basic.UnlimitedLivesEndTimeInMs == end, "Nonpositive seconds do not alter the timer");
                    report.unlimited_and_paused_inventory_verified = true;
                    context.Gold = true; life.RefillLives();
                    Require(life.MaxPossibleLivesToHave == 8 && life.GetLives() == 8, "Pass status selects native eight-life cap");
                    user.Basic.UpdateFullLivesTimeInMs(context.Now + 9 * 1800000L); context.Started();
                    Require(user.Basic.FullLivesTimeInMs == context.Now + 8 * 1800000L, "App start fixes invalid future timestamp at native threshold");
                    report.pass_cap_and_future_timestamp_fix_verified = true;
                    db.Save();
                    foreach (string key in new[] { "FullLivesTimeInMs", "UnlimitedLivesEndTimeInMs", "Chest" })
                    { var row = connection.Find(key); Require(row.Synced == 0 && row.SyncBlocked == 1, "Native local sync flags survive life writes"); }
                    using (var reopened = OriginalRuntimeStorage.Open(path))
                    {
                        var reloadDb = new OriginalDatabaseService(reopened, 61, () => { });
                        var reloadValues = new OriginalUserKeyValue(() => reloadDb);
                        var reload = new Context { Values = reloadValues, User = CreateUser(reloadValues), Now = context.Now };
                        OriginalLocalAccountLoad.UpdateDataFromLocalStorage(reload, reloadValues);
                        Require(reload.User.Basic.FullLivesTimeInMs == user.Basic.FullLivesTimeInMs && reload.User.Basic.UnlimitedLivesEndTimeInMs == end &&
                            reload.User.Inventory.Chest == user.Inventory.Chest && !reloadDb.ShouldSave, "Actual SQLite reopen restores all life state without writing defaults");
                        report.sqlite_reopen_and_sync_flags_verified = true;
                    }
                }
                report.passed = true;
            }
            catch (Exception failure) { report.failure = failure.ToString(); }
            File.WriteAllText(Path.Combine(Application.dataPath, "../live_life_helper_verification.json"), JsonUtility.ToJson(report, true));
        }
        [Serializable] private sealed class Report
        {
            public bool passed, fresh_load_and_app_start_verified, debit_restore_exhaustion_verified, unlimited_and_paused_inventory_verified,
                pass_cap_and_future_timestamp_fix_verified, sqlite_reopen_and_sync_flags_verified;
            public string failure;
            public string scope = "Runtime LifeHelper, Basic/Inventory owners, native local account/area loader and real private Windows SQLite storage. Controlled server time, pass status, app callbacks and analytics are explicit inputs. Not production TimeHelper/backend/app startup, full WinFail/GameScene composition, Home/navigation or Android/iOS proof.";
        }
    }
}
