using System;

namespace Whitebox.Foundation
{
    public interface IOriginalThreeColorAbValues { int GetThreeColorLevelsAbGroup(); }
    public interface IOriginalThreeColorNameContext { bool IsCurrentLevelThreeColor { get; } }
    public interface IOriginalThreeColorUserProgress { int GameType { get; } }
    public interface IOriginalThreeColorVersionEntries { bool TryGetVersion(int level, out int version); }
    public sealed class OriginalThreeColorLevelNames : IOriginalThreeColorLevelNames
    {
        private readonly Func<OriginalActiveLevelData> currentLevel;
        private readonly Func<IOriginalThreeColorAbValues> currentAb;
        private readonly Func<IOriginalThreeColorNameContext> currentManager;
        private readonly Func<IOriginalThreeColorUserProgress> currentProgress;
        private readonly IOriginalThreeColorVersionEntries versions;
        public OriginalThreeColorLevelNames(Func<OriginalActiveLevelData> currentLevel,
            Func<IOriginalThreeColorAbValues> currentAb, Func<IOriginalThreeColorNameContext> currentManager,
            Func<IOriginalThreeColorUserProgress> currentProgress, IOriginalThreeColorVersionEntries versions)
        {
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
            this.currentAb = currentAb ?? throw new ArgumentNullException(nameof(currentAb));
            this.currentManager = currentManager ?? throw new ArgumentNullException(nameof(currentManager));
            this.currentProgress = currentProgress ?? throw new ArgumentNullException(nameof(currentProgress));
            this.versions = versions ?? throw new ArgumentNullException(nameof(versions));
        }
        private int CurrentAbGroup()
        { var ab = currentAb(); if (ab == null) throw new NullReferenceException(); return ab.GetThreeColorLevelsAbGroup(); }
        public bool IsEnabled() // 0x283cf7c.
        { int group = CurrentAbGroup(); return group >= 2 && group <= 5; }
        public bool IsHardLevelExcludedForGroup() => CurrentAbGroup() == 5; // 0x283ec44.
        public bool ShouldReportThreeColorExtension(int level, int leagueIndex) // 0x283d348.
        {
            var active = currentLevel(); if (active == null) throw new NullReferenceException();
            if (active.IsTableGame) return false;
            var manager = currentManager(); if (manager == null) throw new NullReferenceException();
            if (manager.IsCurrentLevelThreeColor) return true;
            if (!IsEnabled() || !IsHardLevelExcludedForGroup()) return false;
            if (leagueIndex <= 0) return GetLevelDifficulty(level) > 0;
            int remainder = leagueIndex % 10;
            return remainder == 2 || remainder == 5 || remainder == 9;
        }
        public string GetLevelExtension() // 0x283d444: original ELF table 0x512f630.
        {
            switch (CurrentAbGroup())
            {
                case 2: return "U";
                case 3: return "V";
                case 4: return "W";
                case 5: return "X";
                default: return "L";
            }
        }
        public bool TryGetVersionOverride(int level, out int version) // 0x283fe14; config entry lookup remains required.
        {
            bool found = versions.TryGetVersion(level, out version);
            if (!found) version = 0;
            return found;
        }
        public bool IsLevelAvailableForExtreme(int level) // 0x283eda4.
        {
            var progress = currentProgress(); if (progress == null) throw new NullReferenceException();
            return level >= 111 && progress.GameType == 0 && !IsItemTutorialOrSpecialLevel(unchecked(level - 1));
        }
        public int GetLevelDifficulty(int level) // 0x283eca4.
        {
            if (!IsLevelAvailableForExtreme(level))
            {
                if (level < 39) return 0;
                if ((uint)level % 10 == 5) return 1;
                if (level == 39 || level == 49) return 1;
                return (uint)level % 10 == 9 ? 2 : 0;
            }
            switch (level % 10)
            {
                case 2: return 1;
                case 5: return 2;
                case 9: return 3;
                default: return 0;
            }
        }
        public static bool IsItemTutorialOrSpecialLevel(int level) // 0x283ee24.
        {
            if (level < 1002)
            {
                if (level < 202) return level >= 121 && (level - 121) % 20 == 0;
                if (level < 301) return level == 231 || level == 261;
                return (level - 301) % 50 == 0;
            }
            return level >= 1101 && (level - 1101) % 100 == 0;
        }
    }
}
