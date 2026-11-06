using System;

namespace Whitebox.Foundation
{
    public interface IOriginalLifeClockStorage
    {
        int GetInt(string key, int defaultValue = 0);
        long GetLong(string key, long defaultValue = 0);
        bool SetInt(string key, int value);
        bool SetLong(string key, long value);
        bool DeleteKey(string key);
    }
    public interface IOriginalClientClock { long CurrentTimeInMs(); }
    public sealed class OriginalSystemClientClock : IOriginalClientClock
    {
        public long CurrentTimeInMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // TimeUtil0x29d6ef8.
    }
    public interface IOriginalLifeClockLives
    {
        int GetLives();
        long RequiredTimeInMsToFillOneLife { get; }
        long PassedTimeMsForNextLife();
        long RemainingMsToEndUnlimitedTimes();
    }
    public sealed class OriginalLifeClockAdapter : IOriginalLifeClockLives
    {
        private readonly OriginalLifeHelper owner;
        public OriginalLifeClockAdapter(OriginalLifeHelper owner) { this.owner = owner; }
        public int GetLives() => owner.GetLives();
        public long RequiredTimeInMsToFillOneLife => owner.RequiredTimeInMsToFillOneLife;
        public long PassedTimeMsForNextLife() => owner.PassedTimeMsForNextLife();
        public long RemainingMsToEndUnlimitedTimes() => owner.RemainingMsToEndUnlimitedTimes();
    }
    public interface IOriginalTimeManipulationTracker
    {
        bool IsTimeManipulationDetected { get; }
        bool IsThereTimeManipulation(long serverTime);
    }
    public interface IOriginalWeeklyTimeOwner { IOriginalTimeManipulationTracker DailyLoginTracker { get; } }
    public interface IOriginalLifeClockContext
    {
        int MaxPossibleLivesToHave { get; }
        float TimeSpentOnPaused { get; }
        OriginalLocalUserData GetCurrentUser();
        IOriginalTimeManipulationTracker GetDailyRewards();
        IOriginalWeeklyTimeOwner GetWeeklyMission();
        bool IsThereLifeHack(long serverTime);
        void SendPing(int reason);
    }
    // One application-owned publication preserves TimeHelper's static cache.
    public sealed class OriginalServerTimePublication
    {
        internal bool ShouldRead = true, HasTime;
        public bool HasServerTimeSetAtLeastOnce(IOriginalLifeClockStorage app) // 0x239766c/0x2397748.
        {
            if (ShouldRead) { ShouldRead = false; HasTime = app.GetLong("ST") > 0; }
            return HasTime;
        }
    }
    // TimeHelper's life/clock state and lifecycle methods, not its HTTP/ping,
    // time-hack detector, notification, timezone or complete context binding.
    // Required feature/backend ports must be supplied by their real owners.
    public sealed class OriginalLifeClock : IOriginalLifeTimeHelper
    {
        private readonly IOriginalLifeClockStorage app;
        private readonly IOriginalClientClock client;
        private readonly IOriginalLifeClockContext context;
        private readonly OriginalServerTimePublication publication;
        public IOriginalLifeClockLives LifeHelper { get; set; }
        public bool IsTimeValidatedByBackend { get; internal set; }
        public bool IsOfflineClientTimeTakenBack { get; internal set; }
        public bool IsLifeHackDetected { get; internal set; }
        public bool AppWillPauseToShowAd { get; set; }
        internal long LastClientTime, ServerClientTimeDiff;
        internal int CurrentEarnedLifeCount;
        public event Action OnTimeValidatedByBackend;
        public OriginalLifeClock(IOriginalLifeClockStorage app, IOriginalClientClock client,
            IOriginalLifeClockContext context, OriginalServerTimePublication publication)
        { this.app = app; this.client = client; this.context = context; this.publication = publication; }
        public void LoadStoredClockState() // Only clock reads/assignments from Bind0x2395804.
        { ServerClientTimeDiff = app.GetLong("STD"); LastClientTime = app.GetLong("LCT"); }
        public long CurrentServerTimeInMs() // 0x239649c: read offset after client callback.
        { long now = client.CurrentTimeInMs(); return unchecked(now + ServerClientTimeDiff); }
        public long GetServerClientTimeDiff() => ServerClientTimeDiff; // 0x2396a80.
        public long CurrentServerTimeInSeconds() => CurrentServerTimeInMs() / 1000; // 0x2396a88.
        public void OnAppStart() { CheckClientTime(); context.SendPing(1); } // 0x2395bf8.
        public void OnAppResume() // 0x2395e34; diagnostic logging omitted.
        {
            CheckClientTime();
            if (AppWillPauseToShowAd) AppWillPauseToShowAd = false;
            else context.SendPing(2);
        }
        public void CheckClientTime() // 0x2395c14; diagnostic logging omitted.
        {
            IsOfflineClientTimeTakenBack = false;
            long last = LastClientTime;
            long now = client.CurrentTimeInMs();
            if (last > unchecked(now + 60000)) { IsOfflineClientTimeTakenBack = true; return; }
            int max = context.MaxPossibleLivesToHave;
            int previous = app.GetInt("LCP", max);
            int earned = unchecked((LifeHelper ?? throw new NullReferenceException()).GetLives() - previous);
            CurrentEarnedLifeCount = earned < 0 ? 0 : earned;
        }
        public void RecalculateAfterPause() => OnAppPause(); // 0x239773c.
        public void OnAppPause() // 0x2395f04; diagnostic logging omitted.
        {
            int lives = (LifeHelper ?? throw new NullReferenceException()).GetLives();
            if (IsTimeValidatedByBackend)
            {
                long now = CurrentServerTimeInMs();
                int max = context.MaxPossibleLivesToHave;
                long passed = lives < max ? (LifeHelper ?? throw new NullReferenceException()).PassedTimeMsForNextLife() : 0;
                app.SetLong("LST", unchecked(now - passed - 60000));
                app.DeleteKey("OULT");
            }
            else
            {
                long elapsed = unchecked(ToNativeInt64(context.TimeSpentOnPaused) * 1000);
                if (elapsed >= 1)
                {
                    long interval = (LifeHelper ?? throw new NullReferenceException()).RequiredTimeInMsToFillOneLife;
                    long reference = app.GetLong("LST");
                    long earned = elapsed / interval;
                    if (earned >= 1) app.SetLong("LST", unchecked(reference + earned * (interval - 60000)));
                    app.SetLong("OULT", (LifeHelper ?? throw new NullReferenceException()).RemainingMsToEndUnlimitedTimes());
                }
            }
            app.SetInt("LCP", lives);
            int current = CurrentEarnedLifeCount;
            int previous = app.GetInt("ELC");
            app.SetInt("ELC", unchecked(previous + current));
            IsTimeValidatedByBackend = false;
            if (!IsOfflineClientTimeTakenBack)
            {
                LastClientTime = client.CurrentTimeInMs();
                app.SetLong("LCT", LastClientTime);
            }
        }
        public bool UpdateTime(long serverTime) // 0x2393a9c; diagnostic logging omitted.
        {
            app.SetLong("ST", serverTime);
            publication.ShouldRead = false; publication.HasTime = serverTime > 0;
            if (!IsLifeHackDetected && !IsTimeValidatedByBackend)
            {
                var daily = context.GetDailyRewards();
                var weekly = context.GetWeeklyMission();
                if (weekly == null || daily == null) throw new NullReferenceException();
                if (!daily.IsTimeManipulationDetected)
                {
                    var tracker = weekly.DailyLoginTracker ?? throw new NullReferenceException();
                    if (!tracker.IsTimeManipulationDetected &&
                        (daily.IsThereTimeManipulation(serverTime) || tracker.IsThereTimeManipulation(serverTime) || context.IsThereLifeHack(serverTime))) return false;
                }
            }
            if (IsOfflineClientTimeTakenBack)
            {
                if (app.GetLong("OULT") >= 1)
                    ((context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException()).UpdateUnlimitedLivesEndTimeInMs(1);
                var inventory = (context.GetCurrentUser() ?? throw new NullReferenceException()).Inventory ?? throw new NullReferenceException();
                inventory.ResetTimes();
            }
            var callback = OnTimeValidatedByBackend;
            IsTimeValidatedByBackend = true; IsOfflineClientTimeTakenBack = false;
            callback?.Invoke();
            long now = client.CurrentTimeInMs();
            long difference = unchecked(serverTime - now);
            if (Math.Abs((float)unchecked(ServerClientTimeDiff - difference)) < 60000f) return true;
            ServerClientTimeDiff = difference;
            app.SetLong("STD", difference);
            int max = context.MaxPossibleLivesToHave;
            int lives = (LifeHelper ?? throw new NullReferenceException()).GetLives();
            now = CurrentServerTimeInMs();
            var helper = LifeHelper ?? throw new NullReferenceException();
            long interval = helper.RequiredTimeInMsToFillOneLife;
            long passed = helper.PassedTimeMsForNextLife();
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            basic.UpdateFullLivesTimeInMs(unchecked(now + interval * unchecked(max - lives) - passed));
            return true;
        }
        private static long ToNativeInt64(float value)
        {
            // ARM64 FCVTZS saturates; +Infinity is explicitly mapped to0 by IL2CPP.
            if (float.IsNaN(value) || float.IsPositiveInfinity(value)) return 0;
            if (value >= 9223372036854775808f) return long.MaxValue;
            if (value <= -9223372036854775808f) return long.MinValue;
            return (long)value;
        }
    }
}
