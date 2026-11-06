using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // ILevelDataProvider's eleven native slots. Fresh and saved providers have
    // separate owners; restoring a level must not substitute the fresh parser.
    public interface IOriginalLevelDataProvider : IOriginalGridItemPopulationData
    {
        List<Dictionary<int, OriginalGoalTuple>> GetGoals();
        OriginalItemSettings? TryGetItemSettingsForStaticItem(int x, int y, int staticItemType);
        int GetMaxMoves();
        int GetLeftMoves();
        int GetPredefinedItemQueuesLength();
        int GetPredefinedItemQueueItemIdLength(int i);
        int GetPredefinedItemQueueItemIdTiledId(int i, int j);
        int GetPredefinedItemQueueItemIdPointX(int i);
        int GetMadeMoves();
    }
    public interface IOriginalGameStartParser : IOriginalLevelDataProvider
    {
        void PrepareGrid(IOriginalLevelDataProvider data);
        bool IsLevelRandomized { get; }
        void RandomizeTileGrid();
    }
    public interface IOriginalGameStartLevelManager
    {
        IOriginalGameStartParser Parser { get; }
        List<Dictionary<int, OriginalLimitTuple>> Limits { get; }
        IOriginalGameStartNewUserUx NewUserUx { get; }
    }
    public interface IOriginalGameStartNewUserUx { void OnLevelEnter(); }
    public interface IOriginalGameStartSession
    {
        void Start();
        bool IsSideProgressionLevel { get; }
        bool IsStory { get; }
        bool IsAdmin { get; }
        int Number { get; }
    }
    public interface IOriginalGameStartEnteredFeature { void EnteredLevel(); }
    public interface IOriginalGameStartMagicDuke { void OnLevelStart(); }
    public interface IOriginalGameStartClash { IOriginalGameStartEnteredFeature GameplayHelper { get; } }
    public interface IOriginalGameStartNightmare { void OnNightmareLevelStart(int levelNumber); }
    public interface IOriginalGameStartLives { void DecrementLives(); }
    public interface IOriginalGameStartCloche
    {
        int GetCloche();
        bool ShouldResetClocheProgress(int progress);
        bool ShouldShowCloche();
        void SetCloche(int progress);
    }
    public interface IOriginalGameStartEvents
    {
        void EnteredLevel();
        void EnteredSideProgressionLevel();
    }
    public interface IOriginalGameStartUser { void SendDataToBackend(bool force); }
    public interface IOriginalGameStartKingsCup
    {
        bool ShouldTrySendClaimForNoReward();
        void TrySendClaimForNoReward(bool force);
    }
    public interface IOriginalGameStartTeamBattle { void TrySendClaimForNoReward(); }
    public interface IOriginalGameStartClock { void Restart(); void Stop(); }
    public interface IOriginalGameStartLevelContext { void Activate(bool forNewLevel); void RemoveLateUnits(); }
    public interface IOriginalGameStartItemCreator
    {
        void UpdateCreatedItemsCountExceptPredefined();
        void ClearCreatedItemsCountExceptPredefined();
    }
    public interface IOriginalGameStartItemFactory
    {
        void CreateParentGameObjects();
        IOriginalGameStartItemCreator ItemCreator { get; }
    }
    public interface IOriginalGameStartShuffle { bool ShouldTryAgain(); }
    public interface IOriginalGameStartGrid
    {
        void CreateCells(IOriginalGameStartParser parser);
        void CreateStaticItems(IOriginalLevelDataProvider data, int origin);
        void CreateItems(IOriginalLevelDataProvider data, int origin);
        void InitializeViews();
    }
    public interface IOriginalGameStartRestore
    {
        IOriginalLevelDataProvider Loader { get; }
        void RestoreLevelDataBeforeGrid();
        void RestoreLevelDataAfterGrid();
    }
    public interface IOriginalGameStartTutorial { void PrepareTutorial(int origin); }
    public interface IOriginalGameStartSave { void SaveImmediate(); }
    public interface IOriginalGameStartSorting { void CacheAllAtlasIdsInOrder(); }
    public interface IOriginalGameStartAnalytics { void LevelStart(int origin); }
    public interface IOriginalGameStartContext
    {
        // Each call corresponds to a native context/singleton lookup. Do not
        // cache these across callbacks: providers can change during startup.
        IOriginalGameStartSession GetActiveLevel();
        IOriginalGameStartEnteredFeature GetSuperBooster();
        IOriginalGameStartEnteredFeature GetSuperDuke();
        IOriginalGameStartMagicDuke GetMagicDuke();
        IOriginalGameStartLevelManager GetLevelManager();
        IOriginalGameStartEnteredFeature GetMultiplierToken();
        IOriginalGameStartClash GetClashOfChampions();
        IOriginalGameStartNightmare GetKingNightmare();
        IOriginalGameStartCloche GetCloche();
        IOriginalGameStartEvents GetEvents();
        IOriginalGameStartUser GetUserManager();
        IOriginalGameStartKingsCup GetKingsCup();
        IOriginalGameStartTeamBattle GetTeamBattle();
        void IncrementUserInt(string key, int amount, int defaultValue);
        OriginalMoveManager GetMoveManager();
        OriginalGoalManager GetGoalManager();
        IOriginalGameStartRestore GetRestoreManager();
        IOriginalGameStartSave GetSaveManager();
        void LogLevelStart(int initializationAttempts);
    }

    // GameManager's entry portion. Its exit flag is the same owner consumed by
    // GameContext.ManualUpdate; no second entry-only copy of that flag exists.
    // Application/UserContext composition is required, not supplied by stubs.
    public sealed partial class OriginalGameStartManager
    {
        public static int CurrentLevelOrigin { get; private set; }
        private readonly OriginalGameExitManager exit;
        private readonly IOriginalGameStartContext context;
        private readonly IOriginalGameStartClock stopwatch;
        public IOriginalGameStartLevelContext LevelContext;
        public IOriginalGameStartItemFactory ItemFactory;
        public IOriginalGameStartGrid GridManager;
        public IOriginalGameStartLives LifeHelper;
        public IOriginalGameStartTutorial TutorialManager;
        public IOriginalGameStartSorting SortingManager;
        public IOriginalGameStartAnalytics AnalyticsManager;
        public IOriginalGameStartShuffle ShuffleManager;

        public OriginalGameStartManager(OriginalGameExitManager exit, IOriginalGameStartContext context, IOriginalGameStartClock stopwatch)
        {
            this.exit = exit ?? throw new ArgumentNullException(nameof(exit));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.stopwatch = stopwatch;
        }
        private static T Required<T>(T owner) where T : class => owner ?? throw new NullReferenceException();

        public void StartGame() // 0x1e38b40. Diagnostic-only side-level formatting omitted.
        {
            CurrentLevelOrigin = 1;
            exit.ShouldExitGame = false;
            var session = Required(context.GetActiveLevel());
            session.Start();
            Required(context.GetSuperBooster()).EnteredLevel();
            Required(context.GetSuperDuke()).EnteredLevel();
            Required(context.GetMagicDuke()).OnLevelStart();
            Required(Required(context.GetLevelManager()).NewUserUx).OnLevelEnter();
            Required(context.GetMultiplierToken()).EnteredLevel();
            Required(Required(context.GetClashOfChampions()).GameplayHelper).EnteredLevel();
            if (session.IsSideProgressionLevel)
            {
                if (session.IsStory && !session.IsAdmin)
                    Required(context.GetKingNightmare()).OnNightmareLevelStart(session.Number);
                Required(context.GetEvents()).EnteredSideProgressionLevel();
            }
            else
            {
                Required(LifeHelper).DecrementLives();
                var cloche = Required(context.GetCloche());
                if (cloche.ShouldResetClocheProgress(cloche.GetCloche()) && cloche.ShouldShowCloche()) cloche.SetCloche(0);
                Required(context.GetEvents()).EnteredLevel();
            }
            Required(context.GetUserManager()).SendDataToBackend(false);
            var kingsCup = Required(context.GetKingsCup());
            if (kingsCup.ShouldTrySendClaimForNoReward()) kingsCup.TrySendClaimForNoReward(false);
            Required(context.GetTeamBattle()).TrySendClaimForNoReward();
            context.IncrementUserInt("LevelTryCount", 1, 0);
            Required(stopwatch).Restart();
            Required(LevelContext).Activate(true);
            Required(ItemFactory).CreateParentGameObjects();
            int count = StartLevel();
            Required(AnalyticsManager).LevelStart(1);
            Required(stopwatch).Stop();
            context.LogLevelStart(count);
        }
        public void RestoreGame() // 0x1e38a30. No session.Start, life debit or fresh-start event hooks.
        {
            CurrentLevelOrigin = 3;
            exit.ShouldExitGame = false;
            Required(stopwatch).Restart();
            Required(LevelContext).Activate(true);
            Required(context.GetRestoreManager()).RestoreLevelDataBeforeGrid();
            Required(ItemFactory).CreateParentGameObjects();
            int count = StartLevel();
            Required(context.GetRestoreManager()).RestoreLevelDataAfterGrid();
            Required(stopwatch).Stop();
            context.LogLevelStart(count);
        }
        public int StartLevel() // 0x1e3acd0.
        {
            var moves = context.GetMoveManager();
            var goals = context.GetGoalManager();
            var level = context.GetLevelManager();
            IOriginalLevelDataProvider data = CurrentLevelOrigin == 3
                ? Required(Required(context.GetRestoreManager()).Loader)
                : Required(Required(level).Parser);
            var goalData = data.GetGoals();
            var limits = Required(level).Limits;
            Required(goals).InitWithGoalsAndLimits(goalData, limits, CurrentLevelOrigin != 3);
            Required(level.Parser).PrepareGrid(data);
            int max = data.GetMaxMoves();
            int left = data.GetLeftMoves();
            int made = data.GetMadeMoves();
            Required(moves).SetMovesForStart(max, left, made);
            Required(GridManager).CreateCells(level.Parser);
            int count = CreateItems(data);
            Required(TutorialManager).PrepareTutorial(CurrentLevelOrigin);
            var save = context.GetSaveManager();
            if (save != null) save.SaveImmediate(); // The native save singleton is explicitly optional here.
            Required(SortingManager).CacheAllAtlasIdsInOrder();
            Required(GridManager).InitializeViews();
            return count;
        }
    }
}
