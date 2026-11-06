using System;

namespace Whitebox.Foundation
{
    // The real TimeHelper and application/user owners must supply these ports.
    // No wall-clock substitution, feature defaults or production startup shim.
    public interface IOriginalLifeTimeHelper
    {
        bool IsOfflineClientTimeTakenBack { get; }
        long CurrentServerTimeInMs();
        void RecalculateAfterPause();
    }
    public interface IOriginalLifeHelperContext
    {
        OriginalLocalUserData GetCurrentUser();
        bool HasPassGoldStatus(OriginalLocalUserData user);
        IOriginalLifeTimeHelper GetTimeHelper();
        void AddOnApplicationStart(Action action);
        void TrackLifeTransaction(int amount);
        bool IsUserContextActive { get; }
        bool GetAfterPaused();
    }

    public sealed class OriginalLifeHelper
    {
        public const int StandardMaxPossibleLivesToHave = 5, RoyalPassMaxPossibleLivesToHave = 8;
        public readonly long RequiredTimeInMsToFillOneLife;
        private readonly IOriginalLifeHelperContext context;
        private IOriginalLifeTimeHelper timeHelper;
        public int Id => 3; // 0x1e1dde4.
        public OriginalLifeHelper(IOriginalLifeHelperContext context) // 0x1e1e908.
        {
            this.context = context;
            RequiredTimeInMsToFillOneLife = (long)TimeSpan.FromMinutes(30).TotalMilliseconds;
        }
        public int MaxPossibleLivesToHave // 0x1e1dd84.
        {
            get
            {
                var user = context.GetCurrentUser() ?? throw new NullReferenceException();
                return context.HasPassGoldStatus(user) ? RoyalPassMaxPossibleLivesToHave : StandardMaxPossibleLivesToHave;
            }
        }
        public void Bind() // 0x1e1ddec.
        {
            timeHelper = context.GetTimeHelper();
            context.AddOnApplicationStart(FixFullLivesTimeInMs);
        }
        private void FixFullLivesTimeInMs() // 0x1e1dee4; diagnostic logging omitted.
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            long full = basic.FullLivesTimeInMs;
            long now = timeHelper.CurrentServerTimeInMs();
            int max = MaxPossibleLivesToHave;
            if (unchecked(full - now) < unchecked(RequiredTimeInMsToFillOneLife * unchecked(max + 1))) return;
            now = timeHelper.CurrentServerTimeInMs();
            max = MaxPossibleLivesToHave;
            basic.UpdateFullLivesTimeInMs(unchecked(now + RequiredTimeInMsToFillOneLife * max));
        }
        public int GetLives() // 0x1e1e07c.
        {
            int max = MaxPossibleLivesToHave;
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            long full = basic.FullLivesTimeInMs;
            long remaining = unchecked(full - timeHelper.CurrentServerTimeInMs());
            if (remaining < 1) return max;
            int missing = unchecked((int)(remaining / RequiredTimeInMsToFillOneLife) + 1);
            return unchecked(max - (missing > max ? max : missing));
        }
        public bool IsFull() // 0x1e1e11c.
            => HasUnlimitedLives() || GetLives() == MaxPossibleLivesToHave;
        public bool HasLives() // 0x1e1bd0c: the equality boundary differs from GetLives().
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            long full = basic.FullLivesTimeInMs;
            long now = timeHelper.CurrentServerTimeInMs();
            int max = MaxPossibleLivesToHave;
            return unchecked(full - now) <= unchecked(RequiredTimeInMsToFillOneLife * (max - 1)) || HasUnlimitedLives();
        }
        public bool HasUnlimitedLives() // 0x1e1e154.
        {
            if (timeHelper.IsOfflineClientTimeTakenBack) return false;
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic;
            long now = timeHelper.CurrentServerTimeInMs();
            return now < (basic ?? throw new NullReferenceException()).UnlimitedLivesEndTimeInMs;
        }
        public void RefillLives() // 0x1e1e1e4.
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic;
            long now = timeHelper.CurrentServerTimeInMs();
            (basic ?? throw new NullReferenceException()).UpdateFullLivesTimeInMs(now);
        }
        public void IncrementLives(int amount = 1) // 0x1e1e25c.
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            basic.SubstractTimeFromFullLivesTimeInMs(unchecked(RequiredTimeInMsToFillOneLife * amount));
            context.TrackLifeTransaction(amount);
            if (context.IsUserContextActive && context.GetAfterPaused()) timeHelper.RecalculateAfterPause();
        }
        public void DecrementLives() // 0x1e1e394.
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic;
            if (HasUnlimitedLives() || !HasLives()) return;
            if (basic == null) throw new NullReferenceException();
            long full = basic.FullLivesTimeInMs;
            long now = timeHelper.CurrentServerTimeInMs();
            long updated = full > now ? unchecked(basic.FullLivesTimeInMs + RequiredTimeInMsToFillOneLife) :
                unchecked(timeHelper.CurrentServerTimeInMs() + RequiredTimeInMsToFillOneLife);
            basic.FullLivesTimeInMs = updated; // Native assignment precedes UpdateFullLivesTimeInMs.
            basic.UpdateFullLivesTimeInMs(updated);
            context.TrackLifeTransaction(-1);
        }
        public int RemainingSecondsToNextLife() // 0x1e1e470.
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            long full = basic.FullLivesTimeInMs;
            long now = timeHelper.CurrentServerTimeInMs();
            return Math.Max(0, unchecked((int)(unchecked(full - now) % RequiredTimeInMsToFillOneLife / 1000)));
        }
        public long PassedTimeMsForNextLife() // 0x1e1e558.
        {
            int remaining = RemainingSecondsToNextLife();
            return remaining < 1 ? 0 : unchecked(RequiredTimeInMsToFillOneLife - unchecked(remaining * 1000));
        }
        public int RemainingSecondsToEndUnlimitedTimes() // 0x1e1e58c.
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            long end = basic.UnlimitedLivesEndTimeInMs;
            return Math.Max(0, unchecked((int)(unchecked(end - timeHelper.CurrentServerTimeInMs()) / 1000)));
        }
        public long RemainingMsToEndUnlimitedTimes() // 0x1e1e668.
        {
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            long end = basic.UnlimitedLivesEndTimeInMs;
            return Math.Max(0L, unchecked(end - timeHelper.CurrentServerTimeInMs()));
        }
        public void AddUnlimitedLivesInMinutes(int deltaMinutes) // 0x1e1e724.
        {
            AddUnlimitedLivesInSeconds(unchecked(deltaMinutes * 60));
            var inventory = (context.GetCurrentUser() ?? throw new NullReferenceException()).Inventory ?? throw new NullReferenceException();
            int paused = inventory.GetPausedUnlimitedLivesTimeInMinutes();
            if (paused >= 1) inventory.UpdatePausedUnlimitedLivesTimeInMinutes(unchecked(paused + deltaMinutes));
        }
        public void AddUnlimitedLivesInSeconds(int deltaSeconds) // 0x1e1e7b8.
        {
            if (deltaSeconds < 1) return;
            var basic = (context.GetCurrentUser() ?? throw new NullReferenceException()).Basic ?? throw new NullReferenceException();
            long end = basic.UnlimitedLivesEndTimeInMs;
            long now = timeHelper.CurrentServerTimeInMs();
            basic.UpdateUnlimitedLivesEndTimeInMs(unchecked(Math.Max(end, now) + (long)TimeSpan.FromSeconds(deltaSeconds).TotalMilliseconds));
        }
    }
}
