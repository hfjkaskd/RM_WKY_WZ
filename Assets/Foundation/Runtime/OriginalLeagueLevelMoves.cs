using System;

namespace Whitebox.Foundation
{
    public readonly struct OriginalLeagueLevel
    {
        public readonly int Level, Moves;
        public OriginalLeagueLevel(string token) // 0x1e13040: additional ':' fields are ignored.
        {
            if (token == null) throw new NullReferenceException();
            string[] fields = token.Split(':');
            Level = int.Parse(fields[0]);
            Moves = int.Parse(fields[1]);
        }
    }
    // LeagueManager's levels/moves cache only. AppKeyValue supplies LeagueLevels
    // with the original default; this class does not substitute a different list.
    public sealed class OriginalLeagueLevelMoves
    {
        private readonly Func<string> readStoredLevels;
        private OriginalLeagueLevel[] levels;
        public OriginalLeagueLevelMoves(Func<string> readStoredLevels)
        { this.readStoredLevels = readStoredLevels ?? throw new ArgumentNullException(nameof(readStoredLevels)); }
        public void ParseLevelAndMoves(string text) // 0x1e12130: cache is assigned before all entries are parsed.
        {
            if (text == null) throw new NullReferenceException();
            string[] tokens = text.Split(',');
            levels = new OriginalLeagueLevel[tokens.Length];
            for (int i = 0; i < tokens.Length; i++) levels[i] = new OriginalLeagueLevel(tokens[i]);
        }
        public OriginalLeagueLevel GetLeagueLevel(int index) // 0x1e14688: signed wrapped (index - 1) remainder.
        {
            if (levels == null) ParseLevelAndMoves(readStoredLevels());
            int offset = unchecked(index - 1);
            int selected = levels.Length == 0 ? offset : offset % levels.Length;
            return levels[selected];
        }
    }
    public sealed class OriginalLeagueUserProgress
    {
        public int LeagueLevel;
    }
    public sealed class OriginalLeagueMoveContext
    {
        private readonly Func<OriginalActiveLevelData> currentLevel;
        private readonly Func<OriginalLeagueLevelMoves> currentLeague;
        private readonly Func<OriginalLeagueUserProgress> currentProgress;
        public OriginalLeagueMoveContext(Func<OriginalActiveLevelData> currentLevel,
            Func<OriginalLeagueLevelMoves> currentLeague, Func<OriginalLeagueUserProgress> currentProgress)
        {
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
            this.currentLeague = currentLeague ?? throw new ArgumentNullException(nameof(currentLeague));
            this.currentProgress = currentProgress ?? throw new ArgumentNullException(nameof(currentProgress));
        }
        public int GetLeagueMoveOffset() // 0x283db2c; also SemiDynamicDifficulty.GetUpdatedMoveCount 0x2500c38.
        {
            var active = currentLevel(); if (active == null) throw new NullReferenceException();
            if (active.LeagueIndex <= 0) return 0;
            var league = currentLeague();
            var progress = currentProgress();
            if (progress == null || league == null) throw new NullReferenceException();
            return league.GetLeagueLevel(progress.LeagueLevel).Moves;
        }
    }
    public sealed class OriginalSemiDynamicDifficulty
    {
        private readonly OriginalLeagueMoveContext league;
        public OriginalSemiDynamicDifficulty(OriginalLeagueMoveContext league)
        { this.league = league ?? throw new ArgumentNullException(nameof(league)); }
        public void SetLevelNumber(int level) { } // 0x2500c30 is a native ret in this version.
        public int GetUpdatedMoveCount(int moves) => unchecked(moves + league.GetLeagueMoveOffset()); // 0x2500c38.
    }
    public sealed class OriginalLevelEpisodeHelper
    {
        private readonly OriginalSemiDynamicDifficulty strategy;
        public OriginalLevelEpisodeHelper(OriginalSemiDynamicDifficulty strategy) { this.strategy = strategy; }
        public void SetEpisodeId(int level) // 0x1e186d0 dispatches ILevelDifficultyStrategy.SetLevelNumber.
        { if (strategy == null) throw new NullReferenceException(); strategy.SetLevelNumber(level); }
        public int GetUpdatedMoveCount(int moves) // 0x1e188c0 dispatches ILevelDifficultyStrategy.GetUpdatedMoveCount.
        { if (strategy == null) throw new NullReferenceException(); return strategy.GetUpdatedMoveCount(moves); }
    }
}
