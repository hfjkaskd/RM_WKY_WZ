using System;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelWinDataContext
    {
        int GetGoalLeftCount(int type, bool finalBoard);
        void UpdateReactivationProgressAfterWin();
        void SessionWin(int coinGoal);
        void LevelEnd();
    }
    public interface IOriginalLevelEndContext
    {
        OriginalActiveLevelData GetCurrentSession();
        void IncreaseUselessMoveCount(OriginalActiveLevelData session);
        void TryDownloadEpisodeLevelBundle();
        void ResetPrelevelTutorial();
        void MultiplierSelectionOnLevelWin();
        void SendLevelEndAnalytics();
        void UserLevelEnd();
    }
    public interface IOriginalLevelRemainingEndContext
    {
        OriginalActiveLevelData GetCurrentSession();
        void UserRemainingMovesEnd();
        // Preserve the caller's native MultiplierDto payload without inventing
        // its still-unbound analytics/DTO implementation.
        void SendLevelEndRewardAnalytics(object multiplierData);
    }
    public static class OriginalLevelEnd
    {
        public static void UpdateLevelWinData(IOriginalLevelWinDataContext context) // 0x1edea24.
        {
            if (context == null) throw new NullReferenceException();
            int coins = context.GetGoalLeftCount(21, false);
            context.UpdateReactivationProgressAfterWin();
            context.SessionWin(coins);
            context.LevelEnd();
        }
        public static void End(IOriginalLevelEndContext context) // 0x1e1bf08.
        {
            var session = (context ?? throw new NullReferenceException()).GetCurrentSession() ?? throw new NullReferenceException();
            if (session.IsAdmin) return;
            if (!session.IsUsefulMoveMade) context.IncreaseUselessMoveCount(session);
            if (session.State == 3)
            {
                context.TryDownloadEpisodeLevelBundle();
                context.ResetPrelevelTutorial();
                context.MultiplierSelectionOnLevelWin();
            }
            context.SendLevelEndAnalytics();
            context.UserLevelEnd();
        }
        public static void RemainingMovesEnd(IOriginalLevelRemainingEndContext context, object multiplierData) // 0x1e1c1f8.
        {
            var session = (context ?? throw new NullReferenceException()).GetCurrentSession() ?? throw new NullReferenceException();
            if (session.IsAdmin) return;
            context.UserRemainingMovesEnd();
            context.SendLevelEndRewardAnalytics(multiplierData);
        }
    }
}
