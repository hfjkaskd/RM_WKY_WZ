using System;

namespace Whitebox.Foundation
{
    public interface IOriginalThreeColorMovePreprocessor
    {
        int GetAdjustedMoveCount(int level, int moves, bool randomized, bool negativeLevel, bool bonusOrReactivation);
    }
    // Native move preprocessing. LeagueManager's current offset and the separate
    // ThreeColorLeagueLevelsConfigHelper remain required application services.
    public sealed class OriginalThreeColorMoves : IOriginalThreeColorMovePreprocessor
    {
        private readonly OriginalThreeColorMoveConfig config;
        private readonly Func<IOriginalThreeColorAbValues> currentAb;
        private readonly Func<OriginalActiveLevelData> currentLevel;
        private readonly Func<int> getLeagueMoveOffset;
        private readonly Func<int, int> getConfiguredLeagueMoveOffset, getLeagueMinMoveCount;
        public OriginalThreeColorMoves(OriginalThreeColorMoveConfig config,
            Func<IOriginalThreeColorAbValues> currentAb, Func<OriginalActiveLevelData> currentLevel,
            Func<int> getLeagueMoveOffset, Func<int, int> getConfiguredLeagueMoveOffset,
            Func<int, int> getLeagueMinMoveCount)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.currentAb = currentAb ?? throw new ArgumentNullException(nameof(currentAb));
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
            this.getLeagueMoveOffset = getLeagueMoveOffset ?? throw new ArgumentNullException(nameof(getLeagueMoveOffset));
            this.getConfiguredLeagueMoveOffset = getConfiguredLeagueMoveOffset ?? throw new ArgumentNullException(nameof(getConfiguredLeagueMoveOffset));
            this.getLeagueMinMoveCount = getLeagueMinMoveCount ?? throw new ArgumentNullException(nameof(getLeagueMinMoveCount));
        }
        public int GetGroupMoveMultiplier() // 0x283dc48: original ELF table 0x4044360.
        {
            var ab = currentAb(); if (ab == null) throw new NullReferenceException();
            switch (ab.GetThreeColorLevelsAbGroup())
            {
                case 2: return 5;
                case 3: return 6;
                case 4: return 7;
                case 5: return 6;
                default: return 10;
            }
        }
        public static int ScaleMoveCount(int total, int multiplier) // 0x283dcc0: mul w, scvtf, fdiv, fcvtps (ceiling).
        { return (int)Math.Ceiling((float)((float)unchecked(total * multiplier) / 10f)); }
        public int GetReducedMoveCount(int moves, bool randomized, bool negativeLevel, bool bonusOrReactivation) // 0x283d9cc.
        {
            if (!negativeLevel && !bonusOrReactivation)
            {
                moves = unchecked(moves + getLeagueMoveOffset());
                if (!randomized)
                {
                    moves = ScaleMoveCount(moves, GetGroupMoveMultiplier());
                    if (moves < 11) moves = 10;
                }
            }
            return moves;
        }
        public int ApplyMoveConfigAdjustment(int level, int moves) // 0x283da1c.
        {
            int adjustment = config.GetMoveAdjustment(level);
            if (adjustment >= 0) return unchecked(moves + adjustment);
            int minimum = config.GetMinMoveCount(10);
            if (minimum >= moves) return moves;
            int adjusted = unchecked(moves + adjustment);
            return adjusted >= minimum ? adjusted : minimum;
        }
        public int GetAdjustedMoveCount(int level, int moves, bool randomized, bool negativeLevel, bool bonusOrReactivation) // 0x283d900.
        {
            int adjusted = ApplyMoveConfigAdjustment(level, GetReducedMoveCount(moves, randomized, negativeLevel, bonusOrReactivation));
            var active = currentLevel(); if (active == null) throw new NullReferenceException();
            if (active.LeagueIndex <= 0) return adjusted;
            int offset = getConfiguredLeagueMoveOffset(level);
            int minimum = config.GetMinMoveCount(10);
            int leagueMinimum = getLeagueMinMoveCount(10);
            if (leagueMinimum <= minimum) minimum = leagueMinimum;
            int sum = unchecked(offset + adjusted);
            return minimum <= sum ? sum : minimum;
        }
    }
}
