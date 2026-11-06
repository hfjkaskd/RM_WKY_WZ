using System;
using System.Collections.Generic;
using Whitebox.Foundation;

// The native snapshot reads these owner fields directly. Explicit supplied
// values isolate the codec; the Unity check uses the actual runtime owners.
namespace Whitebox.Foundation
{
    public interface IOriginalSavedLevelRandom { int GetDifficultySeed(); int GetDifficultySeedGenerationCount(); }
    public sealed class OriginalMoveManager { public int LeftMoves, MadeMoves; }
    public sealed class OriginalActiveLevelData { public int MoveCount, EgoPurchaseCount; }
    public sealed class OriginalLevelRandomSource { public int DifficultySeed, DifficultySeedGenerationCount; }
    public sealed class OriginalLevelRandomManager { public readonly OriginalLevelRandomSource DifficultySource = new OriginalLevelRandomSource(); }
    public static class OriginalMoveBudgetAdjustment { public static int LastAddedExtraMoveCount; }
}
class Probe : OriginalGenericSaveDataHelper { public long Read(int i) => GetData(i); }
class Services : IOriginalGameStateSaveServices, IOriginalEgoSaveState, IOriginalNewUserTutorialSaveState,
    IOriginalLevelTutorialSaveState, IOriginalDynamicTutorialSaveState
{
    public int Flags = 1023;
    public readonly OriginalMoveManager Moves = new OriginalMoveManager();
    public readonly OriginalActiveLevelData Active = new OriginalActiveLevelData();
    public readonly OriginalLevelRandomManager Random = new OriginalLevelRandomManager();
    public long[] Values;
    public void Refresh()
    {
        Moves.LeftMoves = (int)Values[0]; Active.MoveCount = (int)Values[1]; Moves.MadeMoves = (int)Values[2];
        Active.EgoPurchaseCount = (int)Values[3]; Random.DifficultySource.DifficultySeed = (int)Values[5];
        Random.DifficultySource.DifficultySeedGenerationCount = (int)Values[6]; OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount = (int)Values[10];
    }
    public OriginalMoveManager GetMoveManager() { Verify.Record("move"); return (Flags & 1) != 0 ? Moves : null; }
    public IOriginalEgoSaveState GetEgoState() { Verify.Record("ego"); return (Flags & 2) != 0 ? this : null; }
    public OriginalLevelRandomManager GetLevelRandomManager() { Verify.Record("random"); return (Flags & 4) != 0 ? Random : null; }
    public IOriginalNewUserTutorialSaveState GetNewUserTutorialState()
    { Verify.Record("newuser"); if ((Flags & 8) == 0) throw new NullReferenceException(); return (Flags & 16) != 0 ? this : null; }
    public IOriginalLevelTutorialSaveState GetLevelTutorialState() { Verify.Record("tutorial"); return (Flags & 32) != 0 ? this : null; }
    public IOriginalDynamicTutorialSaveState GetDynamicTutorialState() { Verify.Record("dynamic"); return (Flags & 64) != 0 ? this : null; }
    public OriginalActiveLevelData GetCurrentActiveLevel()
    { Verify.Record("active"); if ((Flags & 384) != 384) throw new NullReferenceException(); return (Flags & 512) != 0 ? Active : null; }
    public bool ItemThrowInProgress => Values[4] != 0;
    public bool HasShownAnyDynamicTutorial() { Verify.Record("shown"); return Values[7] != 0; }
    public bool FreeBoosterTutorialBoosterUsage => Values[8] != 0;
    public bool ItemShown => Values[9] != 0;
    public bool SuperDukeShown => Values[11] != 0;
    public bool MagicDukeShown => Values[12] != 0;
    public bool MagicLanternShown => Values[13] != 0;
}
class Verify
{
    static int calls, failure;
    static readonly List<string> trace = new List<string>();
    public static void Record(string value) { trace.Add(value); if (++calls == failure) throw new InvalidOperationException(); }
    static long[] Longs(string text) => text == "~" ? null : text == "" ? new long[0] : Array.ConvertAll(text.Split('/'), long.Parse);
    static string Show(long[] a) => a == null ? "~" : string.Join("/", a);
    static OriginalGenericSaveDataConfig Config(string text, out int[] widths)
    {
        widths = text == "~" || text == "!" ? null : text == "" ? new int[0] : Array.ConvertAll(text.Split('/'), int.Parse);
        return text == "~" ? null : new OriginalGenericSaveDataConfig(widths);
    }
    static long ReadState(OriginalGameStateSaveData s, int i)
    {
        switch (i)
        {
            case 0:return s.GetLeftMoves(); case 1:return s.GetMaxMoves(); case 2:return s.GetMadeMoves();
            case 3:return s.GetEgoPurchaseCount(); case 4:return s.GetEgoPackageItemThrowInProgress();
            case 5:return s.GetDifficultySeed(); case 6:return s.GetDifficultySeedGenerationCount();
            case 7:return s.HasShowedAnyDynamicTutorial()?1:0; case 8:return s.FreeBoosterTutorialBoosterUsage()?1:0;
            case 9:return s.GetDynamicItemTutorialShown()?1:0; case 10:return s.GetAddedExtraMoveCount();
            case 11:return s.GetDynamicSuperDukeTutorialShown()?1:0; case 12:return s.GetDynamicMagicDukeTutorialShown()?1:0;
            default:return s.GetDynamicMagicLanternItemTutorialShown()?1:0;
        }
    }
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var p = line.Split('#'); bool state = p[0] == "s";
            var probe = new Probe(); var saved = new OriginalGameStateSaveData();
            OriginalGenericSaveDataHelper helper = state ? (OriginalGenericSaveDataHelper)saved : probe;
            int[] widths; var config = Config(p[1], out widths); if (!state) helper.SetConfig(config);
            long[] loaded = Longs(p[2]); helper.SetData(loaded);
            var services = new Services { Values = state ? Longs(p[1]) : new long[14] }; if (state) services.Refresh();
            var output = new List<string>();
            foreach (var command in p[3].Split(';'))
            {
                var f = command.Split(','); string result = "-"; calls = 0; trace.Clear();
                try
                {
                    int i = f.Length > 1 && f[0] != "p" && f[0] != "q" && f[0] != "d" && f[0] != "c" && f[0] != "b" && f[0] != "w" ? int.Parse(f[1]) : 0;
                    switch (f[0])
                    {
                        case "g":result = (state ? ReadState(saved, i) : probe.Read(i)).ToString(); break;
                        case "r":result = helper.IsDataReadable()?"1":"0"; break;
                        case "p":case "q":
                            var packed = state ? saved.PrepareStateSaveData(services) : helper.PrepareData(Longs(f[1]));
                            result = Show(packed); if(f[0] == "q") { loaded = packed; helper.SetData(loaded); } break;
                        case "c":config = Config(f[1], out widths); helper.SetConfig(config); break;
                        case "d":loaded = Longs(f[1]); helper.SetData(loaded); break;
                        case "m":widths[i] = int.Parse(f[2]); break;
                        case "v":loaded[i] = long.Parse(f[2]); break;
                        case "o":result = config.GetOffset(i).ToString(); break;
                        case "z":result = config.GetSize(i).ToString(); break;
                        case "n":result = config.GetDataCount().ToString(); break;
                        case "t":result = config.GetTotalDataLength().ToString(); break;
                        case "f":services.Flags = i; break;
                        case "x":failure = i; break;
                        case "e":services.Values[i] = long.Parse(f[2]); services.Refresh(); break;
                        case "b":result = OriginalGenericSaveDataConfig.GetValueFromIndexes(long.Parse(f[1]), int.Parse(f[2]), int.Parse(f[3])).ToString();break;
                        case "w":result = OriginalGenericSaveDataConfig.SetToIndexes(long.Parse(f[1]), long.Parse(f[2]), int.Parse(f[3]), int.Parse(f[4])).ToString();break;
                    }
                }
                catch (Exception) { result = "error"; }
                output.Add(result + ":" + Show(loaded) + ":" + string.Join("/", trace));
            }
            Console.WriteLine(string.Join("|", output)); failure = 0;
        }
    }
}
