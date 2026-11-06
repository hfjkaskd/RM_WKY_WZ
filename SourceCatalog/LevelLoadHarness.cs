using System;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;

static class LevelLoadHarness
{
    sealed class Fixture : IOriginalSlbAbValues, IOriginalOneMoveRemovalAb, IOriginalThreeColorAbValues,
        IOriginalThreeColorNameContext, IOriginalThreeColorUserProgress, IOriginalThreeColorVersionEntries,
        IOriginalThreeColorMovePreprocessor, IOriginalLevelLoadParser, IOriginalWelcomeBackGiftProgress
    {
        public int Flags, LoadFlags, UserLevel, RawMoves, ThreeMode, ThreeResult, ResourceNumber, GiftFail, TierValue, StartValue, Date = 42;
        public bool Reward = true;
        public int SlbGroup;
        public string LeagueText;
        public OriginalActiveLevelData Active;
        public OriginalSlbMoveConfigData SlbData;
        public readonly List<string> Trace = new List<string>();
        public bool IsCurrentLevelThreeColor => ThreeMode != 0;
        public int GameType => 0;
        public int AbGroup => SlbGroup;
        public int Tier => TierValue;
        public int StartLevel => StartValue;
        public void SetAbValues() { Trace.Add("abvalues"); if ((Flags & 64) != 0) throw new Exception(); }
        public bool IsOneMoveRemoveAbEnabled() { Trace.Add("oneab"); return false; }
        public int GetThreeColorLevelsAbGroup()
        { Trace.Add("ab," + Active.Number + "," + Active.MoveCount + "," + (Active.IsAdmin ? 1 : 0) + "," + Active.State); if ((Flags & 1024) != 0) throw new Exception(); return 0; }
        public bool TryGetVersion(int level, out int version)
        { Trace.Add("version," + level); if ((Flags & 2048) != 0) throw new Exception(); version = 0; return false; }
        public OriginalSlbMoveConfigData ReadSlb()
        { Trace.Add("storage"); if ((Flags & 128) != 0) throw new Exception(); return SlbData; }
        public int GetAdjustedMoveCount(int level, int moves, bool random, bool negative, bool bonus)
        { Trace.Add("three," + level + "," + moves + "," + B(random) + "," + B(negative) + "," + B(bonus)); if ((LoadFlags & 16384) != 0) throw new Exception(); return ThreeResult; }
        public int MoveCount => RawMoves;
        public string LevelName { get { Trace.Add("name"); if ((LoadFlags & 8192) != 0) throw new Exception(); return "fixturev99_L"; } }
        public bool ShouldForceReloadLevel()
        { Trace.Add("force"); if ((LoadFlags & 2048) != 0) throw new Exception(); return (LoadFlags & 256) != 0; }
        public void Resources(OriginalLevelLoad owner)
        { Trace.Add("resources," + owner.LevelNumber + "," + B(owner.WasAdminLoad)); if ((LoadFlags & 1024) != 0) throw new Exception(); owner.Parser = (LoadFlags & 2) != 0 ? this : null; owner.LevelNumber = ResourceNumber; }
        public void SetStartLevel(int value) { StartValue = value; Trace.Add("start," + value); if (GiftFail == 1) throw new Exception(); }
        public void SetTier(int value) { TierValue = value; Trace.Add("tier," + value); if (GiftFail == 2) throw new Exception(); }
        public void SetOfflineGiftDeservedDate(int value) { Date = value; Trace.Add("date," + value); if (GiftFail == 3) throw new Exception(); }
        public void SetShouldGiveReward(bool value) { Reward = value; Trace.Add("reward," + B(value)); if (GiftFail == 4) throw new Exception(); }
        public OriginalActiveLevelData Current() => (Flags & 14) == 14 ? Active : null;
    }
    static string B(bool b) => b ? "1" : "0";
    static string D(string s) => s == "~" ? null : Encoding.UTF8.GetString(Convert.FromBase64String(s));
    static string E(string s) => s == null ? "~" : Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    static string[] A(string s) => s == "~" ? null : s == "" ? new string[0] : System.Array.ConvertAll(s.Split(','), D);
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var p = line.Split('#'); var h = System.Array.ConvertAll(p[0].Split(','), int.Parse);
            var f = new Fixture { Flags = h[0], LoadFlags = h[1], UserLevel = h[3], RawMoves = h[4], ThreeMode = h[5], ThreeResult = h[6], TierValue = h[9], StartValue = h[10], SlbGroup = h[14], ResourceNumber = h[17], GiftFail = h[18], LeagueText = D(p[1]) };
            var a = p[3].Split(';'); f.SlbData = new OriginalSlbMoveConfigData { c = A(a[0]), d = A(a[1]), f = A(a[2]), g = A(a[3]), h = A(a[4]) };
            var slb = new OriginalSlbMoveConfig(() => (f.Flags & 32) != 0 ? f : null, f.ReadSlb);
            var names = new OriginalThreeColorLevelNames(f.Current, () => (f.Flags & 18) == 18 ? f : null,
                () => (f.Flags & 256) != 0 ? f : null, () => (f.Flags & 514) == 514 ? f : null, f);
            f.Active = new OriginalActiveLevelData(slb, names) { Number = 77, MoveCount = -7, OriginalMoveCount = -8, Name = "prior", State = 5, LeagueIndex = h[7], Difficulty = (sbyte)h[11], IsBonus = h[12] != 0, IsReactivation = h[13] != 0, IsAdmin = h[15] != 0, IsOnline = h[16] != 0 };
            var league = new OriginalLeagueLevelMoves(() => { f.Trace.Add("league_storage"); return f.LeagueText; });
            var leagueProgress = new OriginalLeagueUserProgress { LeagueLevel = h[8] };
            var leagueContext = new OriginalLeagueMoveContext(f.Current, () => { f.Trace.Add("league"); return (f.LoadFlags & 128) != 0 ? league : null; }, () => (f.Flags & 514) == 514 ? leagueProgress : null);
            var strategy = (f.LoadFlags & 8) != 0 ? new OriginalSemiDynamicDifficulty(leagueContext) : null;
            var episode = new OriginalLevelEpisodeHelper(strategy);
            var tierArrays = p[2].Split(';');
            Func<string, int[]> ints = s => s == "~" ? null : s == "" ? new int[0] : System.Array.ConvertAll(s.Split(','), int.Parse);
            var tiers = new OriginalWelcomeBackMoveTiers { tier1 = ints(tierArrays[0]), tier2 = ints(tierArrays[1]), tier3 = ints(tierArrays[2]) };
            var user = new OriginalWelcomeBackUserProgress { Level = f.UserLevel };
            var gift = new OriginalWelcomeBackMoves(() => { user.Level = f.UserLevel; return (f.Flags & 514) == 514 ? user : null; }, f.Current,
                days => { f.Trace.Add("churn," + days); if (f.GiftFail == 5) throw new Exception(); }, tiers)
            { Progress = (f.LoadFlags & 64) != 0 ? f : null, Rewards = new object(), LastChurnPeriod = h[19] };
            var duke = new OriginalSuperDukeMoveRemoval(() => (f.Flags & 18) == 18 ? f : null);
            var adjust = new OriginalMoveBudgetAdjustment(() => { f.Trace.Add("getduke"); return (f.Flags & 1) != 0 ? duke : null; }, f.Current, slb);
            var session = new OriginalLevelLoadSession { ActiveLevel = f.Current() };
            var load = new OriginalLevelLoad(f.Resources, () => { if ((f.Flags & 514) != 514) throw new NullReferenceException(); return f.UserLevel; },
                () => { session.ActiveLevel = f.Current(); return (f.Flags & 6) == 6 ? session : null; }, f, adjust)
            { Parser = (f.LoadFlags & 1) != 0 ? f : null, LevelNumber = h[2], WasAdminLoad = (f.LoadFlags & 512) != 0, IsRandomized = h[20] != 0, Episode = (f.LoadFlags & 4) != 0 ? episode : null, ThreeColorContext = (f.LoadFlags & 16) != 0 ? f : null, WelcomeBack = (f.LoadFlags & 32) != 0 ? gift : null };
            OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount = 77;
            var output = new List<string>();
            foreach (string step in p[4].Split(';'))
            {
                var x = step.Split(','); f.Trace.Clear(); string result = "-"; int v = x.Length > 1 && x[0] != "p" ? int.Parse(x[1]) : 0;
                try
                {
                    switch (x[0])
                    {
                        case "l": load.LevelLoad(); break;
                        case "w": result = gift.GetExtraMovesOfWelcomeBackGift().ToString(); break;
                        case "a": result = B(gift.IsMoveGiftActive()); break;
                        case "r": gift.ResetGiftData(v != 0); break;
                        case "c": result = leagueContext.GetLeagueMoveOffset().ToString(); break;
                        case "g": var value = league.GetLeagueLevel(v); result = value.Level + "," + value.Moves; break;
                        case "p": league.ParseLevelAndMoves(D(x[1])); break;
                        case "e": if (strategy == null) throw new Exception(); result = strategy.GetUpdatedMoveCount(v).ToString(); break;
                        case "u": f.UserLevel = v; break;
                        case "i": f.Active.LeagueIndex = v; break;
                        case "b": f.Active.IsBonus = v != 0; f.Active.Difficulty = (sbyte)int.Parse(x[2]); break;
                        case "n": load.LevelNumber = v; break;
                        case "f": f.Flags = v; break;
                    }
                }
                catch { result = "error"; }
                // The native strategy methods themselves are verified separately;
                // their empty SetLevelNumber has no externally visible call trace.
                output.Add(result + ":" + load.LevelNumber + ":" + B(load.WasAdminLoad) + ":" + B(load.Parser != null) + ":" + f.Active.Number + ":" + f.Active.MoveCount + ":" + f.Active.OriginalMoveCount + ":" + E(f.Active.Name) + ":" + B(f.Active.IsAdmin) + ":" + B(f.Active.IsOnline) + ":" + f.Active.State + ":" + OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount + ":" + f.TierValue + ":" + f.StartValue + ":" + f.Date + ":" + B(f.Reward) + ":" + gift.LastChurnPeriod + ":" + B(gift.Rewards != null) + ":" + string.Join(";", f.Trace));
            }
            Console.WriteLine(string.Join("|", output));
        }
    }
}
