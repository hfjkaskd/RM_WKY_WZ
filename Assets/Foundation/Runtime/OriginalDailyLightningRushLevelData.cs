using System;

namespace Whitebox.Foundation
{
    public interface IOriginalSessionBundle
    {
        bool LoadBundle();
        void UnloadBundle(bool unloadAllLoadedObjects);
    }
    public interface IOriginalSessionEventBundle : IOriginalSessionBundle { }
    public interface IOriginalSessionBundleManager { IOriginalSessionBundle GetBundle(string name); }
    public interface IOriginalDailyLightningRushProgress { int LastEnteredStep { get; } }
    public interface IOriginalDailyLightningRushSessionManager
    {
        bool ShouldProgress { get; }
        bool IsAllBundlesDownloaded();
        IOriginalDailyLightningRushProgress Progress { get; }
    }
    public interface IOriginalDailyLightningRushPools
    {
        void RecycleAllSpawnedDailyLightningRushCollectViews();
        void ClearDailyLightningRushPool();
        void CreateDailyLightningRushPool();
    }
    public interface IOriginalDailyLightningRushCollectView { }

    // Native 0x338ae10, 0x338d3dc..0x338da30 and 0x338e1a4..0x338e334.
    // Required application/bundle/pool owners are supplied explicitly. This data
    // object neither fabricates inactive managers nor preloads unavailable art.
    public partial class OriginalDailyLightningRushLevelData : OriginalBeforeAfterData
    {
        public const string GameplayBundleName = "daily_lightning_rush_gameplay_v2";
        private readonly Func<IOriginalDailyLightningRushSessionManager> manager;
        private readonly Func<IOriginalSessionBundleManager> bundleManager;
        private readonly Func<IOriginalDailyLightningRushPools> pools;
        private readonly Func<OriginalActiveLevelData> currentLevel;
        private readonly Func<bool> isTableGameLevel;
        protected IOriginalSessionEventBundle gameplayBundle;
        protected int step = -1, lightningRushIconCount, difficulty;
        protected IOriginalDailyLightningRushCollectView collectView;
        protected bool isCollectViewReady;
        public IOriginalSessionEventBundle GameplayBundle => gameplayBundle;
        public int Step => step;
        public int LightningRushIconCount => lightningRushIconCount;
        public int Difficulty => difficulty;
        public IOriginalDailyLightningRushCollectView CollectView => collectView;
        public bool IsCollectViewReady => isCollectViewReady;

        public OriginalDailyLightningRushLevelData(Func<IOriginalDailyLightningRushSessionManager> manager,
            Func<IOriginalSessionBundleManager> bundleManager, Func<IOriginalDailyLightningRushPools> pools,
            Func<OriginalActiveLevelData> currentLevel, Func<bool> isTableGameLevel)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
            this.pools = pools ?? throw new ArgumentNullException(nameof(pools));
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
            this.isTableGameLevel = isTableGameLevel ?? throw new ArgumentNullException(nameof(isTableGameLevel));
        }
        private IOriginalDailyLightningRushSessionManager Manager()
        { var value = manager(); if (value == null) throw new NullReferenceException(); return value; }
        private IOriginalDailyLightningRushPools Pools()
        { var value = pools(); if (value == null) throw new NullReferenceException(); return value; }
        private IOriginalSessionEventBundle GetGameplayBundle()
        {
            var owner = bundleManager(); if (owner == null) throw new NullReferenceException();
            var value = owner.GetBundle(GameplayBundleName); if (value == null) throw new NullReferenceException();
            var typed = value as IOriginalSessionEventBundle;
            if (typed == null) throw new InvalidCastException();
            return typed;
        }
        private int LastEnteredStep()
        {
            var progress = Manager().Progress; if (progress == null) throw new NullReferenceException();
            return progress.LastEnteredStep;
        }
        public override void Init(int levelDifficulty) // 0x338d3dc; no base Init call.
        {
            difficulty = levelDifficulty;
            int start = -1;
            if (Manager().ShouldProgress)
            {
                // Native logs an error then returns here, retaining old state.
                // Only that diagnostic Log.Error argument building is omitted.
                if (!Manager().IsAllBundlesDownloaded()) return;
                var bundle = GetGameplayBundle();
                if (bundle.LoadBundle())
                {
                    int previousStep = step;
                    if (previousStep != LastEnteredStep()) Pools().ClearDailyLightningRushPool();
                    step = LastEnteredStep(); // A separate manager/progress resolve and read.
                    Pools().CreateDailyLightningRushPool();
                    gameplayBundle = bundle;
                    start = 0;
                }
            }
            else if (gameplayBundle != null)
            {
                Pools().RecycleAllSpawnedDailyLightningRushCollectViews();
                Pools().ClearDailyLightningRushPool();
                var previous = gameplayBundle; if (previous == null) throw new NullReferenceException();
                previous.UnloadBundle(false);
                gameplayBundle = null;
            }
            collectView = null; lightningRushIconCount = 0; isCollectViewReady = false;
            ShouldConsume = false; Before = start; After = start;
            if (start == 0) IncrementCount(10);
        }
        public void InitForRestore(int levelDifficulty) // 0x338d7b8: deliberately preserves snapshot and presentation state.
        {
            difficulty = levelDifficulty;
            if (!Manager().ShouldProgress) return;
            if (!Manager().IsAllBundlesDownloaded()) return; // Diagnostic-only Log.Error omitted.
            var bundle = GetGameplayBundle();
            if (!bundle.LoadBundle()) return;
            Pools().CreateDailyLightningRushPool();
            gameplayBundle = bundle;
        }
        public bool IsEventValid() // 0x338da00.
        { return Before != -1 && Manager().ShouldProgress; }
        protected bool IncrementCount(int amount) // 0x338d704.
        {
            if (!IsEventValid() || isTableGameLevel()) return false;
            ShouldConsume = true; // Native writes this even when amount is zero.
            AddExtra(unchecked(GetMultiplierValueForDifficulty(difficulty) * amount));
            return true;
        }
        public override void LoadSaveData(long value) // 0x338e238: recover active-level difficulty after snapshot writes.
        {
            base.LoadSaveData(value);
            var level = currentLevel(); if (level == null) throw new NullReferenceException();
            InitForRestore(level.Difficulty);
        }
        public override short GetDataLength(int index) // 0x338e2f0; native save has three fields.
        { return index == 0 ? (short)1 : index == 1 || index == 2 ? (short)24 : (short)0; }
        public override bool GetIsDataSigned(int index) => index == 1 || index == 2; // 0x338e31c.
    }
}
