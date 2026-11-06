using System;

namespace Whitebox.Foundation
{
    public interface IOriginalProgressTimedEvent { bool IsRemainingTimeFinished { get; } }
    public interface IOriginalProgressEvent { bool ShouldProgress { get; } }
    public interface IOriginalBasicLevelProgressContext
    {
        IOriginalProgressTimedEvent GetKingsCup();
        IOriginalProgressTimedEvent GetTeamBattle();
        IOriginalProgressEvent GetTeamTreasure();
        IOriginalProgressEvent GetWeeklyLeaderboard();
        int MaxLevelCount { get; }
        void MarkCurrentSideProgressionLevelPlayed();
        bool CanMultiplyInLevel(bool checkAfterRemainingMoves);
        int GetActiveAmountMultiplier();
        void ClearWinProgressFlag();
        void ClearOtherLevelProgressFlag();
        void WriteLevel(int level);
        void WriteLeagueLevel(int level);
        void TriggerUpdateLevel(int level);
        void UpdateStoryLevel();
        void IncrementMyLevelWithEvents(int amount, bool kingsCup, bool teamBattle, bool teamTreasure, bool weeklyLeaderboard);
        void IncrementMyLeagueScoreWithEvents(int amount, bool kingsCup, bool teamBattle, bool teamTreasure, bool weeklyLeaderboard);
        void SetMissionProgress(int type, int amount);
        void OnCrownsWon(int amount);
    }
    // BasicUserData level/league progression. Database key resolution and the
    // real feature/leaderboard owners remain explicit required dependencies.
    public sealed partial class OriginalBasicLevelProgress
    {
        private readonly IOriginalBasicLevelProgressContext context;
        public int Level { get; internal set; }
        public int LeagueLevel { get; internal set; }
        public int SyncId { get; internal set; }
        public int UserOfferSegment { get; internal set; }
        public long FullLivesTimeInMs { get; set; } // Native public setter0x2f81bec.
        public long UnlimitedLivesEndTimeInMs { get; internal set; }
        public long UserData { get; internal set; }
        public OriginalBasicLevelProgress(int level, int leagueLevel, IOriginalBasicLevelProgressContext context)
        { Level = level; LeagueLevel = leagueLevel; this.context = context; }
        public void UpdateLevel(int level) // 0x2f825d4; diagnostic logging omitted.
        {
            if (Level != level) { context.ClearWinProgressFlag(); context.ClearOtherLevelProgressFlag(); }
            Level = level; context.WriteLevel(level); context.TriggerUpdateLevel(level);
        }
        public void UpdateLeagueLevel(int level) // 0x2f827f8.
        {
            if (LeagueLevel != level) { context.ClearWinProgressFlag(); context.ClearOtherLevelProgressFlag(); }
            LeagueLevel = level; context.WriteLeagueLevel(level);
        }
        public void IncrementLevel(OriginalActiveLevelData active) // 0x2f82d54.
        {
            var kings = context.GetKingsCup();
            var battle = context.GetTeamBattle();
            var treasure = context.GetTeamTreasure();
            var weekly = context.GetWeeklyLeaderboard();
            if (active == null) throw new NullReferenceException();
            bool league = false;
            int amount;
            if (active.IsSideProgressionLevel)
            {
                context.MarkCurrentSideProgressionLevelPlayed();
                amount = context.CanMultiplyInLevel(false) ? unchecked(context.GetActiveAmountMultiplier() * 2) : 2;
            }
            else if (active.IsLeague)
            {
                league = true;
                UpdateLeagueLevel(unchecked(LeagueLevel + 1));
                amount = active.IsLeagueHard ? 3 : active.IsLeagueSuperHard ? 5 : active.IsLeagueExtremelyHard ? 10 : 1;
                if (context.CanMultiplyInLevel(false)) amount = unchecked(amount * context.GetActiveAmountMultiplier());
            }
            else
            {
                if (Level > context.MaxLevelCount) return;
                UpdateLevel(unchecked(Level + 1));
                amount = active.Difficulty <= 0 ? 1 : active.Difficulty == 1 ? 3 : active.Difficulty == 2 ? 5 : 10;
                if (context.CanMultiplyInLevel(false)) amount = unchecked(amount * context.GetActiveAmountMultiplier());
                context.UpdateStoryLevel();
            }
            bool kingsFinished = (kings ?? throw new NullReferenceException()).IsRemainingTimeFinished;
            bool battleFinished = (battle ?? throw new NullReferenceException()).IsRemainingTimeFinished;
            bool treasureProgress = (treasure ?? throw new NullReferenceException()).ShouldProgress;
            bool weeklyProgress = (weekly ?? throw new NullReferenceException()).ShouldProgress;
            if (league) context.IncrementMyLeagueScoreWithEvents(amount, !kingsFinished, !battleFinished, treasureProgress, weeklyProgress);
            else context.IncrementMyLevelWithEvents(amount, !kingsFinished, !battleFinished, treasureProgress, weeklyProgress);
            if (!kings.IsRemainingTimeFinished) context.SetMissionProgress(34, amount);
            if (league) context.OnCrownsWon(amount);
        }
    }
}
