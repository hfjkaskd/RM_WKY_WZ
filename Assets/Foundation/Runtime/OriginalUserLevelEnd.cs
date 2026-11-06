using System;

namespace Whitebox.Foundation
{
    public interface IOriginalStoryLevelProgress { void AddLevelWin(int level); }
    public interface IOriginalClocheWriter { void SetCloche(int value); }
    public interface IOriginalLevelEndFeature { void OnLevelEnd(); }
    public interface IOriginalLevelMilestonePackage { }
    public sealed class OriginalUserEndState
    {
        public OriginalBasicLevelProgress Basic;
        public OriginalUserInventoryBalances Inventory;
        public OriginalActiveLevelData ActiveLevel;
        public IOriginalStoryLevelProgress StoryProgress;
    }
    public interface IOriginalUserLevelEndContext
    {
        void GetEventsInfoOnLevelEnd();
        OriginalUserEndState GetCurrentUser();
        void ClearLevelSaveFile();
        IOriginalClocheWriter GetClocheWriter();
        IOriginalLevelEndFeature GetSuperDuke();
        IOriginalLevelEndFeature GetMultiplierToken();
        void EpisodeOnLevelSuccess();
        void NewUserUxOnLevelSuccess(OriginalActiveLevelData active);
        int GetWinMissionProgressFlag();
        void SetMissionProgress(int type, int amount);
        bool IsMagicLanternTutorialCompleted { get; }
        void SetMagicLanternTutorialAsSeen(bool value);
        void OnNightmareLevelWin();
        void OnSideProgressionLevelWin();
        void EpisodeOnLevelFail();
        void FailedSideProgressionLevel();
        void QuitDetectionLevelEnded();
        void FailedLevel();
        void OnStatsLevelFail();
        void OnStatsLevelWin();
        IOriginalLevelMilestonePackage GetNThousandLevelPackage(int level);
        void AddInventoryPackage(IOriginalLevelMilestonePackage package, bool isPaid);
        void NThousandRewardClaim(IOriginalLevelMilestonePackage package);
        void MultiplierTokenPurchaseOnLevelWin();
        void UpdateEventsOnLevelWin();
        void SendDataToBackend(bool force);
        void TryMultiplyValues(bool isAfterRemainingMoves);
        void EventsRemainingMovesEnd(OriginalActiveLevelData active);
        void MissionEventsRemainingMovesEnd();
    }
    // UserManager's two end-of-level phases. Event aggregation, restore-file,
    // backend, feature and storage composition remain explicit typed owners.
    public static class OriginalUserLevelEnd
    {
        public static void LevelEnd(IOriginalUserLevelEndContext context) // 0x2aec860.
        {
            context.GetEventsInfoOnLevelEnd();
            var user = context.GetCurrentUser() ?? throw new NullReferenceException();
            var basic = user.Basic;
            var inventory = user.Inventory;
            var active = user.ActiveLevel;
            context.ClearLevelSaveFile();
            if (basic == null) throw new NullReferenceException();
            if (unchecked(basic.Level + 1) >= 32)
            {
                var cloche = context.GetClocheWriter();
                var snapshot = (active ?? throw new NullReferenceException()).Cloche ?? throw new NullReferenceException();
                (cloche ?? throw new NullReferenceException()).SetCloche(snapshot.After);
            }
            (context.GetSuperDuke() ?? throw new NullReferenceException()).OnLevelEnd();
            (context.GetMultiplierToken() ?? throw new NullReferenceException()).OnLevelEnd();
            if (active == null) throw new NullReferenceException();
            if (active.State != 3)
            {
                if (active.State != 6 && active.State != 5) context.EpisodeOnLevelFail();
                if (active.IsSideProgressionLevel) { context.FailedSideProgressionLevel(); context.QuitDetectionLevelEnded(); }
                else context.FailedLevel();
                context.OnStatsLevelFail();
                context.SendDataToBackend(false);
                return;
            }
            context.EpisodeOnLevelSuccess();
            context.NewUserUxOnLevelSuccess(active);
            if (context.GetWinMissionProgressFlag() == 1) context.SetMissionProgress(12, 1);
            if (context.IsMagicLanternTutorialCompleted) context.SetMagicLanternTutorialAsSeen(true);
            basic.IncrementLevel(active);
            var animation = active.AnimationValues ?? throw new NullReferenceException();
            var coin = animation.Coin ?? throw new NullReferenceException();
            if (coin.After > coin.Before)
            {
                if (inventory == null) throw new NullReferenceException();
                inventory.AddCoins(unchecked(coin.After - inventory.Coin), true);
            }
            if (active.IsSideProgressionLevel)
            {
                if (active.StoryLevel < 0)
                {
                    context.OnNightmareLevelWin();
                    var current = context.GetCurrentUser() ?? throw new NullReferenceException();
                    (current.StoryProgress ?? throw new NullReferenceException()).AddLevelWin(basic.Level);
                }
                context.OnSideProgressionLevelWin();
            }
            else
            {
                var star = active.Star ?? throw new NullReferenceException();
                (inventory ?? throw new NullReferenceException()).UpdateStars(star.After);
            }
            context.OnStatsLevelWin();
            animation = active.AnimationValues ?? throw new NullReferenceException();
            var milestone = animation.CompletedNThousandLevelCoin ?? throw new NullReferenceException();
            if (milestone.After > milestone.Before)
            {
                var package = context.GetNThousandLevelPackage(active.Number);
                context.AddInventoryPackage(package, false);
                context.NThousandRewardClaim(package);
            }
            context.MultiplierTokenPurchaseOnLevelWin();
            context.UpdateEventsOnLevelWin();
        }
        public static void RemainingMovesEnd(IOriginalUserLevelEndContext context) // 0x2aeeb3c.
        {
            var user = context.GetCurrentUser() ?? throw new NullReferenceException();
            var active = user.ActiveLevel ?? throw new NullReferenceException();
            var inventory = user.Inventory;
            if (active.ExtraCoins > 0) (inventory ?? throw new NullReferenceException()).AddCoins(active.ExtraCoins, true);
            var animation = active.AnimationValues ?? throw new NullReferenceException();
            if (inventory == null) throw new NullReferenceException();
            var madness = animation.StartSnapshots.Madness as OriginalMadnessSessionData;
            var progress = inventory.GetMadness();
            if (madness == null) throw new NullReferenceException();
            if (madness.IsEventValid() && madness.After > progress.Count)
            {
                animation = active.AnimationValues ?? throw new NullReferenceException();
                madness = animation.StartSnapshots.Madness as OriginalMadnessSessionData ?? throw new NullReferenceException();
                progress.Count = madness.After;
                inventory.UpdateMadness(progress);
            }
            context.TryMultiplyValues(false);
            context.EventsRemainingMovesEnd(active);
            context.MissionEventsRemainingMovesEnd();
            // Native tail formatting is diagnostic-only. Keep the actual sync.
            context.SendDataToBackend(false);
        }
    }
}
