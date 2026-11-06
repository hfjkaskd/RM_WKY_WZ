using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation.Editor
{
    // Real session/goal/move/storage owners with explicit application, feature,
    // grid/item/tutorial/save/restore boundaries. Not installed in the scene.
    public static class GameStartVerification
    {
        public static void Run()
        {
            var report = new Report();
            try
            {
                VerifyFormalPopulation(report);
                string path = Path.Combine(Path.GetTempPath(), "royal-entry-" + Guid.NewGuid().ToString("N") + ".sqlite");
                using (var connection = OriginalRuntimeStorage.Open(path))
                {
                    var database = new OriginalDatabaseService(connection, 61, () => { });
                    database.OnApplicationResume();
                    var values = new OriginalUserKeyValue(() => database);
                    var formalGame = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
                    var countData = new FLevelSaveData { BoardSaveData = new FBoardSaveData {
                        ItemModelCreatorSaveData = new FItemModelCreatorSaveData() } };
                    formalGame.BindSavedCreatedItemCounts(new OriginalDecodedCreatedItemCounts(() => countData));
                    var counts = new CountProbe(formalGame.InitialCounts);
                    var context = new Context(values, counts);
                    var exit = new OriginalGameExitManager(null, null, null) { ShouldExitGame = true };
                    var clock = new OriginalGameStartClock();
                    var actualBoard = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
                    var shuffle = new ShuffleProbe(actualBoard.InitialShuffle);
                    var manager = new OriginalGameStartManager(exit, context, clock) {
                        LevelContext = context, ItemFactory = context, GridManager = context,
                        LifeHelper = context, TutorialManager = context, SortingManager = context, AnalyticsManager = context, ShuffleManager = shuffle };
                    context.Active.WonAnyLevel = true; context.Active.FalseSwipeCount = 9;
                    context.Fresh.Goals[0][11].SetGoal(4);
                    manager.StartGame();
                    Require(OriginalGameStartManager.CurrentLevelOrigin == 1 && !exit.ShouldExitGame && !clock.IsRunning, "Fresh origin, shared exit flag and real stopwatch");
                    Require(context.Active.State == 2 && !context.Active.WonAnyLevel && context.Active.FalseSwipeCount == 0 && context.Active.EventMultiplier == 3, "Real UserActiveLevelData.Start executes through the captured adapter");
                    Require(context.Moves.LeftMoves == 30 && context.Moves.MadeMoves == 0 && context.Goals.GetGoalDisplayCount(11) == context.Fresh.Goals[0][11].TargetCount, "Fresh data resets the real goal tuple and uses adjusted active move budget");
                    Require(context.LifeDebits == 1 && context.FeatureEntries == 5 && context.UserEntries == 1 && context.EventEntries == 1 && context.BackendCalls == 1, "Required fresh-start service boundaries are called once");
                    Require(context.LifeUser.Basic.FullLivesTimeInMs == context.LifeNow + 1800000 && values.GetLong("FullLivesTimeInMs") == context.LifeUser.Basic.FullLivesTimeInMs && context.LifeTransactions == 1,
                        "GameStart calls the actual LifeHelper and persists the real first-entry debit");
                    report.real_life_debit_verified = true;
                    Require(context.TryCount == 1 && database.ShouldSave && context.AnalyticsCalls == 1 && context.LastLog == 1, "Actual SQLite-backed LevelTryCount, analytic boundary and initialization-attempt count");
                    Require(shuffle.Calls == 1 && !actualBoard.InitialShuffle.OnlyLightballExists, "Explicit randomized entry fixture consults the real formal-board finder and real match manager once");
                    report.real_initial_shuffle_verified = true;
                    report.actual_session_start_verified = report.fresh_move_goal_models_verified = true;
                    database.Save();
                    context.Active.State = 3; context.Active.WonAnyLevel = true; context.Active.FalseSwipeCount = 12;
                    context.Saved.Goals[0][11].SetGoal(7);
                    context.UseRestored = true;
                    exit.ShouldExitGame = true;
                    manager.RestoreGame();
                    Require(OriginalGameStartManager.CurrentLevelOrigin == 3 && !exit.ShouldExitGame && !clock.IsRunning, "Restore origin, shared exit flag and real stopwatch");
                    Require(context.Active.State == 3 && context.Active.WonAnyLevel && context.Active.FalseSwipeCount == 12, "Restore does not call fresh session.Start");
                    Require(context.Moves.LeftMoves == 19 && context.Moves.MadeMoves == 11 && context.Goals.GetGoalDisplayCount(11) == 7, "Saved provider retains real goal progress and native packed move values");
                    Require(context.BeforeGrid == 1 && context.AfterGrid == 1 && context.RestoreReads == 3 && context.TryCount == 1 && !database.ShouldSave && context.LifeDebits == 1 && context.AnalyticsCalls == 1, "Restore resolves owner three times without fresh entry debit, attempt write or analytics");
                    Require(counts.UpdateCalls == 1 && counts.ClearCalls == 0, "Restore reaches the actual formal generation-count owner exactly once");
                    report.real_generated_count_restore_verified = true;
                    Require(context.Prepares == 2 && context.Creates == 2 && context.Initializes == 2 && context.Tutorials == 2 && context.Sorts == 2 && context.Activations == 2 && context.Parents == 2, "Both branches traverse all required level-construction boundaries");
                    Require(context.Trace.IndexOf("before") < context.Trace.IndexOf("prepare-saved") && context.Trace.IndexOf("after") > context.Trace.IndexOf("views-saved"), "Restore data brackets the actual StartLevel orchestration");
                    report.restore_move_goal_models_verified = report.restore_preserves_session_verified = true;
                    report.fresh_left_moves = 30; report.restored_left_moves = context.Moves.LeftMoves; report.restored_made_moves = context.Moves.MadeMoves;
                    report.restored_box_goal = context.Goals.GetGoalDisplayCount(11); report.level_try_count = context.TryCount; report.trace = context.Trace.ToArray();
                }
                using (var reopened = OriginalRuntimeStorage.Open(path))
                {
                    var database = new OriginalDatabaseService(reopened, 61, () => { }); database.OnApplicationResume();
                    var values = new OriginalUserKeyValue(() => database);
                    Require(values.GetInt("LevelTryCount", -1) == 1, "Fresh-start attempt survives actual SQLite reopen");
                    report.sqlite_reopen_verified = true;
                }
                report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally { File.WriteAllText(Path.Combine(Application.dataPath, "../live_game_start_verification.json"), JsonUtility.ToJson(report, true) + "\n"); }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void VerifyFormalPopulation(Report report)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            Require(board != null && board.CreatedModelCount == 72 && board.CreatedCellViewCount == 72 && board.CreatedViewCount == 72, "Formal first-board population creates72 models/cells/views through the runtime kernel");
            var level = OriginalLevelReader.Load("Levels/level_1");
            int first = board.Grid.GetCell(0, 0).Mediation.CurrentItem.Id, index = 0, boxes = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y); var item = cell.Mediation.CurrentItem;
                Require(item != null && ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem), "Every created model occupies all three real mediation slots");
                var expected = OriginalItemSettings.ForTiledId(level.Grid.Items[x + y * board.Grid.Width]);
                Require(item.Id == unchecked(first + index++) && item.TiledId == expected.TiledId && item.ItemType == expected.ItemType && item.LayerCount == expected.LayerCount, "Total-bottom-left-vertical model order and original fixed item settings");
                Require(item.FallComponent != null && item.HasView, "Native models own real fall components and prefab views");
                if (item is OriginalBoxBoardItem) boxes++;
                if (item is OriginalMatchBoardItem match) Require(match.IsHandGiven, "Fixed initial colors retain hand-given flags");
            }
            Require(index == 72 && boxes == 36, "Original first-level quantities");
            report.formal_initial_models_verified = true; report.formal_models = index; report.formal_boxes = boxes;
        }
        private sealed class Provider : IOriginalGameStartParser
        {
            public readonly List<Dictionary<int, OriginalGoalTuple>> Goals = new List<Dictionary<int, OriginalGoalTuple>>();
            public readonly List<Dictionary<int, OriginalLimitTuple>> Limits = new List<Dictionary<int, OriginalLimitTuple>>();
            private readonly Context context;
            private readonly bool saved;
            private readonly OriginalFreshLevelMoves fresh;
            private readonly OriginalGameStateSaveData state;
            public Provider(Context context, FTiledLevel level, bool saved)
            {
                this.context = context; this.saved = saved;
                var preparation = new OriginalLevelGoalPreparation(level, context.Goals);
                preparation.ParseGoals(Goals); preparation.ParseLimits(Limits);
                fresh = new OriginalFreshLevelMoves(() => context.Active);
                if (saved)
                {
                    state = new OriginalGameStateSaveData();
                    long[] packed = state.PrepareData(new long[] { 19, 30, 11, 0, 0, 7, 0, 0, 0, 0, 15, 0, 0, 0 });
                    state.SetData(packed);
                }
            }
            public List<Dictionary<int, OriginalGoalTuple>> GetGoals() => Goals;
            public int GetMaxMoves() => saved ? state.GetMaxMoves() : fresh.GetMaxMoves();
            public int GetLeftMoves() => saved ? state.GetLeftMoves() : fresh.GetLeftMoves();
            public int GetMadeMoves() => saved ? state.GetMadeMoves() : fresh.GetMadeMoves();
            public void PrepareGrid(IOriginalLevelDataProvider data)
            {
                Require(ReferenceEquals(this, context.Fresh) && ReferenceEquals(data, context.UseRestored ? context.Saved : context.Fresh), "Fresh parser receives the selected fresh/saved provider");
                context.Trace.Add(context.UseRestored ? "prepare-saved" : "prepare-fresh"); context.Prepares++;
            }
            public bool IsLevelRandomized => !saved; // Explicit entry-fixture input exercises the real board checker.
            public void RandomizeTileGrid() => throw new InvalidOperationException("Fixed first-level fixture.");
            public int GetItemAtCell(int x, int y) => throw new NotSupportedException("Grid/items are explicit ports in this check.");
            public int GetItemOriginalIdAtCell(int x, int y) => throw new NotSupportedException();
            public OriginalItemSettings? TryGetItemSettingsForStaticItem(int x, int y, int type) => throw new NotSupportedException();
            public int GetPredefinedItemQueuesLength() => throw new NotSupportedException();
            public int GetPredefinedItemQueueItemIdLength(int i) => throw new NotSupportedException();
            public int GetPredefinedItemQueueItemIdTiledId(int i, int j) => throw new NotSupportedException();
            public int GetPredefinedItemQueueItemIdPointX(int i) => throw new NotSupportedException();
        }
        private sealed class ShuffleProbe : IOriginalGameStartShuffle
        {
            private readonly OriginalInitialShuffleCheck owner;
            public int Calls;
            public ShuffleProbe(OriginalInitialShuffleCheck owner) { this.owner = owner; }
            public bool ShouldTryAgain() { Calls++; return owner.ShouldTryAgain(); }
        }
        private sealed class CountProbe : IOriginalGameStartItemCreator
        {
            private readonly OriginalGeneratedItemCountState owner;
            public int UpdateCalls, ClearCalls;
            public CountProbe(OriginalGeneratedItemCountState owner) { this.owner = owner; }
            public void UpdateCreatedItemsCountExceptPredefined() { UpdateCalls++; owner.UpdateCreatedItemsCountExceptPredefined(); }
            public void ClearCreatedItemsCountExceptPredefined() { ClearCalls++; owner.ClearCreatedItemsCountExceptPredefined(); }
        }
        private sealed class Context : IOriginalGameStartContext, IOriginalGameStartLevelManager,
            IOriginalGameStartEnteredFeature, IOriginalGameStartMagicDuke, IOriginalGameStartNewUserUx,
            IOriginalGameStartClash, IOriginalGameStartCloche, IOriginalGameStartEvents, IOriginalGameStartUser,
            IOriginalGameStartKingsCup, IOriginalGameStartTeamBattle, IOriginalGameStartLives,
            IOriginalGameStartLevelContext, IOriginalGameStartItemFactory, IOriginalGameStartGrid,
            IOriginalGameStartRestore, IOriginalGameStartTutorial, IOriginalGameStartSorting, IOriginalGameStartAnalytics,
            IOriginalLifeHelperContext, IOriginalLifeTimeHelper
        {
            public readonly OriginalActiveLevelData Active;
            public readonly OriginalMoveManager Moves = new OriginalMoveManager();
            public readonly OriginalGoalManager Goals;
            public readonly Provider Fresh, Saved;
            private readonly IOriginalGameStartSession activeAdapter;
            private readonly OriginalUserKeyValue values;
            private readonly IOriginalGameStartItemCreator countOwner;
            public readonly List<string> Trace = new List<string>();
            public readonly OriginalLocalUserData LifeUser;
            private readonly OriginalLifeHelper life;
            private Action lifeStart;
            public readonly long LifeNow = 1700000000000;
            public int LifeTransactions;
            public bool UseRestored;
            public int LifeDebits, FeatureEntries, UserEntries, EventEntries, BackendCalls, AnalyticsCalls, LastLog, BeforeGrid, AfterGrid, RestoreReads, Prepares, Creates, Initializes, Tutorials, Sorts, Activations, Parents;
            public Context(OriginalUserKeyValue values, IOriginalGameStartItemCreator countOwner)
            {
                this.values = values; this.countOwner = countOwner;
                LifeUser = new OriginalLocalUserData { Basic = new OriginalBasicLevelProgress(1, 0, null) { FullLivesTimeInMs = LifeNow } };
                LifeUser.Basic.BindLifeStorage(values);
                life = new OriginalLifeHelper(this); life.Bind(); lifeStart();
                activeAdapter = ActiveLevelStartVerification.CreateGameEntrySession(out Active);
                Goals = new OriginalGoalManager(() => 0, new OriginalGameOperations(() => 0), new OriginalGoalClaims());
                var level = OriginalLevelReader.Load("Levels/level_1");
                Fresh = new Provider(this, level, false); Saved = new Provider(this, level, true);
            }
            public int TryCount => values.GetInt("LevelTryCount");
            public IOriginalGameStartSession GetActiveLevel() => activeAdapter;
            public IOriginalGameStartEnteredFeature GetSuperBooster() => this;
            public IOriginalGameStartEnteredFeature GetSuperDuke() => this;
            public IOriginalGameStartMagicDuke GetMagicDuke() => this;
            public IOriginalGameStartLevelManager GetLevelManager() => this;
            public IOriginalGameStartEnteredFeature GetMultiplierToken() => this;
            public IOriginalGameStartClash GetClashOfChampions() => this;
            public IOriginalGameStartNightmare GetKingNightmare() => throw new InvalidOperationException("Normal level.");
            public IOriginalGameStartCloche GetCloche() => this;
            public IOriginalGameStartEvents GetEvents() => this;
            public IOriginalGameStartUser GetUserManager() => this;
            public IOriginalGameStartKingsCup GetKingsCup() => this;
            public IOriginalGameStartTeamBattle GetTeamBattle() => this;
            public void IncrementUserInt(string key, int amount, int defaultValue) => values.IncrementInt(key, amount, defaultValue);
            public OriginalMoveManager GetMoveManager() => Moves;
            public OriginalGoalManager GetGoalManager() => Goals;
            public IOriginalGameStartRestore GetRestoreManager() { RestoreReads++; return this; }
            public IOriginalGameStartSave GetSaveManager() => null; // Explicitly permitted native branch; save format is not tested here.
            public void LogLevelStart(int count) { LastLog = count; }
            public IOriginalGameStartParser Parser => Fresh;
            public List<Dictionary<int, OriginalLimitTuple>> Limits => Fresh.Limits;
            public IOriginalGameStartNewUserUx NewUserUx => this;
            public void EnteredLevel() { FeatureEntries++; }
            void IOriginalGameStartEvents.EnteredLevel() { EventEntries++; }
            public void OnLevelStart() { FeatureEntries++; }
            public void OnLevelEnter() { UserEntries++; }
            public IOriginalGameStartEnteredFeature GameplayHelper => this;
            int IOriginalGameStartCloche.GetCloche() => 0;
            public bool ShouldResetClocheProgress(int progress) => false;
            public bool ShouldShowCloche() => throw new InvalidOperationException("Short-circuited cloche gate.");
            public void SetCloche(int progress) => throw new InvalidOperationException("Short-circuited cloche gate.");
            public void EnteredSideProgressionLevel() => throw new InvalidOperationException("Normal level.");
            public void SendDataToBackend(bool force) { Require(!force, "Native force=false"); BackendCalls++; }
            public bool ShouldTrySendClaimForNoReward() => false;
            public void TrySendClaimForNoReward(bool force) => throw new InvalidOperationException("Short-circuited claim gate.");
            public void TrySendClaimForNoReward() { Trace.Add("team"); }
            public void DecrementLives() { LifeDebits++; life.DecrementLives(); }
            public OriginalLocalUserData GetCurrentUser() => LifeUser;
            public bool HasPassGoldStatus(OriginalLocalUserData user) => false; // Explicit non-pass entry fixture.
            public IOriginalLifeTimeHelper GetTimeHelper() => this;
            public void AddOnApplicationStart(Action action) { lifeStart += action; }
            public bool IsOfflineClientTimeTakenBack => false;
            public long CurrentServerTimeInMs() => LifeNow;
            public void RecalculateAfterPause() => throw new InvalidOperationException("No life grant or resume in this entry fixture.");
            public bool IsUserContextActive => false;
            public bool GetAfterPaused() => throw new InvalidOperationException("Native inactive context short-circuits this query.");
            public void TrackLifeTransaction(int amount) { Require(amount == -1, "Entry consumes exactly one life"); LifeTransactions++; }
            public void Activate(bool fresh) { Require(fresh, "Both entry branches activate forNewLevel=true"); Activations++; }
            public void CreateParentGameObjects() { Parents++; }
            public IOriginalGameStartItemCreator ItemCreator => countOwner;
            public void RemoveLateUnits() => throw new InvalidOperationException("Fixed first-level fixture.");
            public void CreateCells(IOriginalGameStartParser parser) { Require(ReferenceEquals(parser, Fresh), "Cell grid uses fresh geometry even while restoring"); Trace.Add("cells"); }
            public void CreateStaticItems(IOriginalLevelDataProvider data, int origin) { Require(ReferenceEquals(data, UseRestored ? Saved : Fresh), "Selected static provider"); Trace.Add("static"); }
            public void CreateItems(IOriginalLevelDataProvider data, int origin) { Require(ReferenceEquals(data, UseRestored ? Saved : Fresh), "Selected item provider"); Creates++; }
            public void InitializeViews() { Initializes++; Trace.Add(UseRestored ? "views-saved" : "views-fresh"); }
            public IOriginalLevelDataProvider Loader => Saved;
            public void RestoreLevelDataBeforeGrid() { BeforeGrid++; Trace.Add("before"); }
            public void RestoreLevelDataAfterGrid() { AfterGrid++; Trace.Add("after"); }
            public void PrepareTutorial(int origin) { Require(origin == (UseRestored ? 3 : 1), "Actual entry origin reaches tutorial"); Tutorials++; }
            public void CacheAllAtlasIdsInOrder() { Sorts++; }
            public void LevelStart(int origin) { Require(origin == 1, "Fresh-start analytic origin"); AnalyticsCalls++; }
        }
        [Serializable] private sealed class Report
        {
            public bool passed, actual_session_start_verified, fresh_move_goal_models_verified, restore_move_goal_models_verified, restore_preserves_session_verified, sqlite_reopen_verified, formal_initial_models_verified, real_initial_shuffle_verified, real_generated_count_restore_verified, real_life_debit_verified;
            public int fresh_left_moves, restored_left_moves, restored_made_moves, restored_box_goal, level_try_count, formal_models, formal_boxes;
            public string failure;
            public string[] trace;
            public string scope = "Formal72 initial models/cells/prefab views use the runtime population kernel and real mediation. Separate entry fixture uses real active-session Start, goal/move models, shared exit flag, Stopwatch, actual LifeHelper with real Basic timestamp/SQLite debit and attempt/reopen. Time/pass status are explicit inputs; application/feature/context/grid/tutorial/save/restore/sorting/analytics remain fixture boundaries. Not full production startup, all-item/static/restore implementations, original-rendered parity or mobile proof.";
        }
    }
}
