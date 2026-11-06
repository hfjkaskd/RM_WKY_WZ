using System;

namespace Whitebox.Foundation
{
    public interface IOriginalThreeColorLevelNames
    {
        bool ShouldReportThreeColorExtension(int level, int leagueIndex);
        bool TryGetVersionOverride(int level, out int version);
        string GetLevelExtension();
    }
    // UserActiveLevelData.Load/Reset/name portion. Constructor and ResetValues
    // are in OriginalActiveLevelInitialization; SetStartValues/save remain separate.
    public sealed partial class OriginalActiveLevelData : OriginalActiveLevelMoveData
    {
        private readonly OriginalSlbMoveConfig slb;
        private readonly IOriginalThreeColorLevelNames threeColor;
        public bool IsAdmin, IsOnline, IsReactivation, IsTableGame, IsBonus;
        public sbyte Difficulty;
        public int StoryLevel, LeagueIndex, State;
        // UserActiveLevelData.Win captures these before remaining-move conversion.
        public int LeftMoves, MadeMoves;
        public string Name;
        public bool IsLeague => LeagueIndex > 0; // 0x32b65b8.
        public bool IsLeagueHard => LeagueIndex % 10 == 2; // 0x32b66d0.
        public bool IsLeagueSuperHard => LeagueIndex % 10 == 5; // 0x32b6700.
        public bool IsLeagueExtremelyHard => LeagueIndex % 10 == 9; // 0x32b6730.
        public bool IsSideProgressionLevel => StoryLevel < 0 || IsTableGame; // 0x32b6644.
        public OriginalActiveLevelData(OriginalSlbMoveConfig slb, IOriginalThreeColorLevelNames threeColor)
            : this(slb, threeColor, null) { }
        public void Load(int level, string levelName, int moves, bool admin = false, bool online = false) // 0x32b6768.
        {
            Number = level; MoveCount = moves; OriginalMoveCount = moves;
            // GetFixedLevelName deliberately reads the OLD admin flag. Exceptions
            // retain the new number/moves and the previous name/admin/online/state.
            Name = GetFixedLevelName(levelName);
            IsAdmin = admin; IsOnline = online; State = 1;
        }
        public void Reset() { State = 0; } // 0x32b6760: other data is retained.
        public string GetFixedLevelName(string levelName) // 0x32b6948.
        {
            if (IsAdmin || IsReactivation || StoryLevel < 0 || string.IsNullOrEmpty(levelName)) return levelName;
            int index = levelName.LastIndexOf('_');
            if (index < 0 || index >= levelName.Length - 1) return levelName;
            string prefix = levelName.Substring(0, index);
            if (!threeColor.ShouldReportThreeColorExtension(Number, LeagueIndex))
            {
                if (!slb.TryGetLevelNameOverride(Number, out int slbVersion, out string extension)) return prefix + "_L";
                return StripVersionSegment(prefix) + "v" + slbVersion.ToString("D2") + "_" + extension;
            }
            if (!threeColor.TryGetVersionOverride(Number, out int version)) return prefix + "_" + threeColor.GetLevelExtension();
            return StripVersionSegment(prefix) + "v" + version.ToString("D2") + "_" + threeColor.GetLevelExtension();
        }
        public static string StripVersionSegment(string prefix) // 0x32b6ddc.
        {
            int index = prefix.LastIndexOf('v');
            if (index <= 0 || index >= prefix.Length - 1) return prefix;
            for (int i = index + 1; i < prefix.Length; i++) if (!char.IsDigit(prefix[i])) return prefix;
            return prefix.Substring(0, index);
        }
    }
}
