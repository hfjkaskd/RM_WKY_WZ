using System;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelLoadParser
    {
        int MoveCount { get; }
        string LevelName { get; }
        bool ShouldForceReloadLevel();
    }
    public interface IOriginalLevelLoadSession { OriginalActiveLevelData ActiveLevel { get; } }
    // Explicit session input retained for isolated routing diagnostics. The
    // application-owned OriginalUserSessionData implements the same contract.
    public sealed class OriginalLevelLoadSession : IOriginalLevelLoadSession { public OriginalActiveLevelData ActiveLevel { get; set; } }
    // LevelManager.LevelLoad routing, including resource-reload decisions and
    // the original writes-before/after active Load. Resource selection/download,
    // application context and session Start are required separate composition.
    public sealed class OriginalLevelLoad
    {
        private readonly Action<OriginalLevelLoad> resourcesLoad;
        private readonly Func<int> currentUserLevelNumber;
        private readonly Func<IOriginalLevelLoadSession> currentSession;
        private readonly IOriginalThreeColorMovePreprocessor threeColorMoves;
        private readonly OriginalMoveBudgetAdjustment moveAdjustment;
        public IOriginalLevelLoadParser Parser;
        public OriginalLevelEpisodeHelper Episode;
        public IOriginalThreeColorNameContext ThreeColorContext;
        public IOriginalWelcomeBackMoveProvider WelcomeBack;
        public int LevelNumber;
        public bool WasAdminLoad, IsRandomized;
        public OriginalLevelLoad(Action<OriginalLevelLoad> resourcesLoad, Func<int> currentUserLevelNumber,
            Func<IOriginalLevelLoadSession> currentSession, IOriginalThreeColorMovePreprocessor threeColorMoves,
            OriginalMoveBudgetAdjustment moveAdjustment)
        {
            this.resourcesLoad = resourcesLoad ?? throw new ArgumentNullException(nameof(resourcesLoad));
            this.currentUserLevelNumber = currentUserLevelNumber ?? throw new ArgumentNullException(nameof(currentUserLevelNumber));
            this.currentSession = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
            this.threeColorMoves = threeColorMoves ?? throw new ArgumentNullException(nameof(threeColorMoves));
            this.moveAdjustment = moveAdjustment ?? throw new ArgumentNullException(nameof(moveAdjustment));
        }
        public bool ShouldForceReloadLevel() // 0x1e1b4d0: parser query precedes the admin-load flag.
        {
            if (Parser == null) throw new NullReferenceException();
            return Parser.ShouldForceReloadLevel() || WasAdminLoad;
        }
        public void LevelLoad() // 0x1e1a8fc.
        {
            if (Parser == null) resourcesLoad(this);
            else if (ShouldForceReloadLevel()) { LevelNumber = 0; resourcesLoad(this); }
            else if (Parser == null || LevelNumber != currentUserLevelNumber()) resourcesLoad(this);
            WasAdminLoad = false;
            if (Parser == null) return; // Original reports a diagnostic and returns; no synthetic parser is installed.
            var session = currentSession();
            if (session == null || Episode == null) throw new NullReferenceException();
            var active = session.ActiveLevel;
            Episode.SetEpisodeId(LevelNumber);
            if (ThreeColorContext == null) throw new NullReferenceException();
            int moves;
            if (!ThreeColorContext.IsCurrentLevelThreeColor)
            {
                if (Parser == null || Episode == null) throw new NullReferenceException();
                moves = Episode.GetUpdatedMoveCount(Parser.MoveCount);
            }
            else
            {
                if (Parser == null || active == null) throw new NullReferenceException();
                moves = threeColorMoves.GetAdjustedMoveCount(LevelNumber, Parser.MoveCount, IsRandomized,
                    LevelNumber < 0, active.IsBonus || active.IsReactivation);
            }
            if (WelcomeBack == null) throw new NullReferenceException();
            int extra = WelcomeBack.GetExtraMovesOfWelcomeBackGift();
            if (Parser == null) throw new NullReferenceException();
            int number = LevelNumber;
            string name = Parser.LevelName;
            if (active == null) throw new NullReferenceException();
            int total = unchecked(moves + extra);
            active.Load(number, name, total, false, false);
            int adjustment = moveAdjustment.GetMoveRemoveAmount(total);
            active.UpdateMoveCount(unchecked(total + adjustment));
        }
    }
}
