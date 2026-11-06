using System;
using System.Collections.Generic;
using Whitebox.Foundation;

// Explicit service boundaries for differential orchestration verification.
// These are not application services or a production LevelContext factory.
namespace Whitebox.Foundation
{
    public sealed class OriginalGoalTuple { }
    public sealed class OriginalLimitTuple { }
    public struct OriginalItemSettings { }
    public interface IOriginalLevelWinGameManager { void ExitGameWhenPossible(); }
    public sealed class OriginalGameStateManager { public void SetStateToNoGame() { throw new NotSupportedException(); } }
    public sealed class OriginalMoveManager
    {
        public void SetMovesForStart(int max, int left, int made)
        { GameStartHarness.Step("moves," + max + "," + left + "," + made); GameStartHarness.Left = left; GameStartHarness.Made = made; }
    }
    public sealed class OriginalGoalManager
    {
        public void InitWithGoalsAndLimits(IReadOnlyList<Dictionary<int, OriginalGoalTuple>> goals,
            IReadOnlyList<Dictionary<int, OriginalLimitTuple>> limits, bool reset)
        { GameStartHarness.Step("init," + GameStartHarness.GoalId(goals) + "," + GameStartHarness.LimitId(limits) + "," + (reset ? 1 : 0)); GameStartHarness.Reset = reset ? 1 : 0; }
    }
}
public static class GameStartHarness
{
    public static readonly List<string> Trace = new List<string>();
    public static int Left, Made, Reset;
    private static int[] v;
    private static int calls, tries, lives, cloche, activeId, restoreId, retries;
    private static bool running, seed, randomized;
    private static OriginalGameStartManager manager;
    private static Context context;
    private static Provider parser, parser2, restored;
    private static Grid grid, grid2;
    private static Level level;
    private static Restore restore, restore2;
    private static Session session, session2;
    private static readonly List<Dictionary<int, OriginalGoalTuple>>[] goalLists = {
        new List<Dictionary<int, OriginalGoalTuple>>(), new List<Dictionary<int, OriginalGoalTuple>>(), new List<Dictionary<int, OriginalGoalTuple>>() };
    private static readonly List<Dictionary<int, OriginalLimitTuple>>[] limitLists = {
        new List<Dictionary<int, OriginalLimitTuple>>(), new List<Dictionary<int, OriginalLimitTuple>>() };
    public static int GoalId(IReadOnlyList<Dictionary<int, OriginalGoalTuple>> list) => ReferenceEquals(list, goalLists[0]) ? 0 : ReferenceEquals(list, goalLists[1]) ? 1 : 2;
    public static int LimitId(IReadOnlyList<Dictionary<int, OriginalLimitTuple>> list) => ReferenceEquals(list, limitLists[0]) ? 0 : 1;
    public static void Step(string label)
    {
        Trace.Add(label); calls++;
        if (calls == v[2]) throw new InvalidOperationException();
        if (calls != v[3]) return;
        switch (v[4])
        {
            case 1: level.Parser = parser2; break;
            case 2: level.Parser = null; break;
            case 3: manager.GridManager = grid2; break;
            case 4: manager.GridManager = null; break;
            case 5: restoreId = 1; break;
            case 6: activeId = 1; break;
            case 7: level.Limits = limitLists[1]; break;
            case 8: manager.TutorialManager = null; break;
            case 9: manager.SortingManager = null; break;
            case 10: manager.AnalyticsManager = null; break;
            case 11: randomized = false; break;
            case 12: manager.ItemFactory = null; break;
            case 13: manager.LevelContext = null; break;
            case 14: manager.ShuffleManager = null; break;
        }
    }
    private static T Missing<T>(int id, T item) where T : class => v[1] == id ? null : item;
    private sealed class Session : IOriginalGameStartSession
    {
        private readonly int id;
        public Session(int id) { this.id = id; }
        public void Start() => Step("session," + id);
        public bool IsSideProgressionLevel => id == 0 ? v[5] < 0 || v[6] != 0 : true;
        public bool IsStory => id == 0 ? v[5] < 0 : true;
        public bool IsAdmin => id == 0 ? v[7] != 0 : false;
        public int Number => id == 0 ? v[13] : 88;
    }
    private sealed class Feature : IOriginalGameStartEnteredFeature, IOriginalGameStartMagicDuke, IOriginalGameStartNewUserUx,
        IOriginalGameStartNightmare, IOriginalGameStartLives, IOriginalGameStartCloche, IOriginalGameStartEvents,
        IOriginalGameStartUser, IOriginalGameStartKingsCup, IOriginalGameStartTeamBattle, IOriginalGameStartClash,
        IOriginalGameStartClock, IOriginalGameStartLevelContext, IOriginalGameStartItemFactory, IOriginalGameStartTutorial,
        IOriginalGameStartSave, IOriginalGameStartSorting, IOriginalGameStartAnalytics,
        IOriginalGameStartItemCreator, IOriginalGameStartShuffle
    {
        private readonly string label;
        public Feature(string label) { this.label = label; }
        public void EnteredLevel() => Step(label);
        public void OnLevelStart() => Step("magic");
        public void OnLevelEnter() => Step("ux");
        public void OnNightmareLevelStart(int number) => Step("nightmare," + number);
        public void DecrementLives() { Step("life"); lives++; }
        public int GetCloche() { Step("cloche"); return 17; }
        public bool ShouldResetClocheProgress(int value) { Step("reset?," + value); return v[8] != 0; }
        public bool ShouldShowCloche() { Step("show?"); return v[9] != 0; }
        public void SetCloche(int value) { Step("set," + value); cloche = value; }
        public void EnteredSideProgressionLevel() => Step("side");
        public void SendDataToBackend(bool force) => Step("backend," + (force ? 1 : 0));
        public bool ShouldTrySendClaimForNoReward() { Step("claim?"); return v[10] != 0; }
        public void TrySendClaimForNoReward(bool force) => Step("claim," + (force ? 1 : 0));
        public void TrySendClaimForNoReward() => Step("team");
        public IOriginalGameStartEnteredFeature GameplayHelper => Missing(11, new Feature("clash"));
        public void Restart() { Step("restart"); running = true; }
        public void Stop() { Step("stop"); running = false; }
        public void Activate(bool fresh) => Step("activate," + (fresh ? 1 : 0));
        public void RemoveLateUnits() => Step("late-clear");
        public void CreateParentGameObjects() => Step("parents");
        public IOriginalGameStartItemCreator ItemCreator => Missing(32, this);
        public void UpdateCreatedItemsCountExceptPredefined() => Step("counts-update");
        public void ClearCreatedItemsCountExceptPredefined() => Step("counts-clear");
        public bool ShouldTryAgain() { Step("retry?"); return retries++ < v[12]; }
        public void PrepareTutorial(int origin) => Step("tutorial," + origin);
        public void SaveImmediate() => Step("save");
        public void CacheAllAtlasIdsInOrder() => Step("sorting");
        public void LevelStart(int origin) => Step("analytics," + origin);
    }
    private sealed class Provider : IOriginalGameStartParser
    {
        public readonly int Id;
        public Provider(int id) { Id = id; }
        public List<Dictionary<int, OriginalGoalTuple>> GetGoals() { Step("goals," + Id); return goalLists[Id]; }
        public void PrepareGrid(IOriginalLevelDataProvider data) => Step("prepare," + Id + "," + ((Provider)data).Id);
        public bool IsLevelRandomized => randomized;
        public void RandomizeTileGrid() => Step("randomize," + Id);
        public int GetMaxMoves() { Step("max," + Id); return v[14] + Id; }
        public int GetLeftMoves() { Step("left," + Id); return v[15] + Id; }
        public int GetMadeMoves() { Step("made," + Id); return v[16] + Id; }
        public int GetItemAtCell(int x, int y) { throw new NotSupportedException(); }
        public int GetItemOriginalIdAtCell(int x, int y) { throw new NotSupportedException(); }
        public OriginalItemSettings? TryGetItemSettingsForStaticItem(int x, int y, int type) { throw new NotSupportedException(); }
        public int GetPredefinedItemQueuesLength() { throw new NotSupportedException(); }
        public int GetPredefinedItemQueueItemIdLength(int i) { throw new NotSupportedException(); }
        public int GetPredefinedItemQueueItemIdTiledId(int i, int j) { throw new NotSupportedException(); }
        public int GetPredefinedItemQueueItemIdPointX(int i) { throw new NotSupportedException(); }
    }
    private sealed class Level : IOriginalGameStartLevelManager
    {
        public IOriginalGameStartParser Parser { get; set; }
        public List<Dictionary<int, OriginalLimitTuple>> Limits { get; set; }
        public IOriginalGameStartNewUserUx NewUserUx => Missing(8, new Feature("ux"));
    }
    private sealed class Grid : IOriginalGameStartGrid
    {
        private readonly int id;
        public Grid(int id) { this.id = id; }
        public void CreateCells(IOriginalGameStartParser parser) => Step("cells," + id + "," + (parser == null ? -1 : ((Provider)parser).Id));
        public void InitializeViews() => Step("views," + id);
        public void CreateStaticItems(IOriginalLevelDataProvider data, int origin) => Step("static," + id + "," + ((Provider)data).Id + "," + origin);
        public void CreateItems(IOriginalLevelDataProvider data, int origin) => Step("items," + id + "," + ((Provider)data).Id + "," + origin);
    }
    private sealed class Restore : IOriginalGameStartRestore
    {
        private readonly int id;
        public Restore(int id) { this.id = id; }
        public IOriginalLevelDataProvider Loader => Missing(24, id == 0 ? restored : parser2);
        public void RestoreLevelDataBeforeGrid() => Step("before," + id);
        public void RestoreLevelDataAfterGrid() => Step("after," + id);
    }
    private sealed class Context : IOriginalGameStartContext
    {
        private Feature Unit(int unit, int missing, string label) { Step("unit," + unit); return Missing(missing, new Feature(label)); }
        public IOriginalGameStartSession GetActiveLevel() => seed || v[1] <= 3 && v[1] > 0 ? null : activeId == 0 ? session : session2;
        public IOriginalGameStartEnteredFeature GetSuperBooster() => Unit(29, 4, "booster");
        public IOriginalGameStartEnteredFeature GetSuperDuke() => Unit(50, 5, "duke");
        public IOriginalGameStartMagicDuke GetMagicDuke() => Unit(63, 6, "magic");
        public IOriginalGameStartLevelManager GetLevelManager() { Step("unit,2"); return Missing(7, level); }
        public IOriginalGameStartEnteredFeature GetMultiplierToken() => Unit(69, 9, "multiplier");
        public IOriginalGameStartClash GetClashOfChampions() => Unit(74, 10, "clash");
        public IOriginalGameStartNightmare GetKingNightmare() => Unit(23, 30, "nightmare");
        public IOriginalGameStartCloche GetCloche() => Unit(31, 13, "cloche");
        public IOriginalGameStartEvents GetEvents() => Unit(20, 14, "event");
        public IOriginalGameStartUser GetUserManager() => Unit(1, 15, "user");
        public IOriginalGameStartKingsCup GetKingsCup() => Unit(7, 16, "cup");
        public IOriginalGameStartTeamBattle GetTeamBattle() => Unit(8, 17, "team");
        public void IncrementUserInt(string key, int amount, int defaultValue) { Step("increment," + key + "," + amount + "," + defaultValue); tries = unchecked(tries + amount); }
        public OriginalMoveManager GetMoveManager() { Step("level,11"); return Missing(21, new OriginalMoveManager()); }
        public OriginalGoalManager GetGoalManager() { Step("level,12"); return Missing(22, new OriginalGoalManager()); }
        public IOriginalGameStartRestore GetRestoreManager() { Step("restore"); return Missing(23, restoreId == 0 ? restore : restore2); }
        public IOriginalGameStartSave GetSaveManager() { Step("save?"); return v[11] != 0 ? new Feature("save") : null; }
        public void LogLevelStart(int count) => Step("log," + count);
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            v = Array.ConvertAll(line.Split(','), int.Parse);
            context = new Context(); var exit = new OriginalGameExitManager(null, null, null);
            seed = true; var originSeed = new OriginalGameStartManager(exit, context, null);
            try { if (v[0] == 1 || v[0] == 3) originSeed.RestoreGame(); else originSeed.StartGame(); } catch (NullReferenceException) { }
            seed = false; Trace.Clear(); calls = 0; tries = 100; lives = 0; cloche = 17; Left = -111; Made = -222; Reset = -1; running = false; activeId = restoreId = retries = 0; randomized = v[12] != 0;
            parser = new Provider(0); parser2 = new Provider(1); restored = new Provider(2);
            level = new Level { Parser = Missing(25, parser), Limits = limitLists[0] };
            grid = new Grid(0); grid2 = new Grid(1); restore = new Restore(0); restore2 = new Restore(1); session = new Session(0); session2 = new Session(1);
            var feature = new Feature("field"); manager = new OriginalGameStartManager(exit, context, Missing(18, feature)) {
                LevelContext = Missing(19, feature), ItemFactory = Missing(20, feature), GridManager = Missing(26, grid),
                LifeHelper = Missing(12, feature), TutorialManager = Missing(27, feature), SortingManager = Missing(28, feature), AnalyticsManager = Missing(29, feature), ShuffleManager = Missing(31, feature) };
            exit.ShouldExitGame = true; bool failed = false;
            try { if (v[0] == 0) manager.StartGame(); else if (v[0] == 1) manager.RestoreGame(); else Trace.Add("result," + manager.StartLevel()); }
            catch (Exception) { failed = true; }
            Console.WriteLine((failed ? 1 : 0) + "#" + OriginalGameStartManager.CurrentLevelOrigin + "," + (exit.ShouldExitGame ? 1 : 0) + "," + (running ? 1 : 0) + "," + tries + "," + lives + "," + cloche + "," + Left + "," + Made + "," + Reset + "#" + string.Join(";", Trace));
        }
    }
}
