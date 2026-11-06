using System;

namespace Whitebox.Foundation
{
    public interface IOriginalClashSessionManager
    {
        IOriginalClashSessionHelper Helper { get; }
        IOriginalClashScoreConfig Config { get; }
        bool IsInEvent { get; }
        bool IsAllBundlesDownloaded();
        bool ShouldProgress { get; }
    }
    public interface IOriginalClashSessionHelper
    {
        bool IsScoreLocked { get; }
        void UpdateLocalScore(int score, int comboType);
    }
    public interface IOriginalClashScoreConfig
    {
        int GetMatchItemScore();
        int GetPropellerItemScore();
        int GetRocketItemScore();
        int GetTntItemScore();
        int GetLightballItemScore();
        int GetPropellerPropellerComboScore();
        int GetPropellerRocketComboScore();
        int GetPropellerTntComboScore();
        int GetRocketRocketComboScore();
        int GetRocketTntComboScore();
        int GetTntTntComboScore();
        int GetLightballLightballComboMultiplier();
        int GetRemainingMoveScore();
        int GetWinBonus();
    }
    public interface IOriginalClashCollectViewPrefab { }
    public interface IOriginalClashGameplayAsset { IOriginalClashCollectViewPrefab GetCollectViewPrefab(); }
    public interface IOriginalClashSessionPools
    {
        void TryCreateClashOfChampionsGameplayCollectViewPool(IOriginalClashCollectViewPrefab prefab);
        void TryClearClashOfChampionsGameplayCollectViewPool();
    }

    // Native 0x338ae24 and 0x338c878..0x338cf9c. Required application,
    // bundle/asset/pool and progress owners are supplied by their real context.
    public partial class OriginalClashOfChampionsLevelData : OriginalBeforeAfterData, IOriginalClashLevelStart
    {
        public const string GameplayBundleName = "clash_of_champions_gameplay_v3";
        public const string DialogBundleName = "clash_of_champions_dialog_v2";
        public const string CollectViewAssetName = "ClashOfChampionsGameplayCollectView";
        private readonly Func<IOriginalClashSessionManager> resolveManager;
        private readonly Func<IOriginalSessionBundleManager> bundleManager;
        private readonly Func<IOriginalClashSessionPools> pools;
        private readonly Func<IOriginalSessionEventBundle, string, IOriginalClashGameplayAsset> loadAsset;
        private readonly Func<int> activeAmountMultiplier;
        private readonly Func<OriginalActiveLevelData> currentLevel;
        protected IOriginalClashSessionManager manager;
        protected IOriginalClashSessionHelper helper;
        protected IOriginalSessionEventBundle gameplayBundle;
        protected int scoreMultiplier;
        protected bool isScoreLockedForLevel;
        protected int matchItemScore, propellerItemScore, rocketItemScore, tntItemScore, lightballItemScore;
        protected int propellerPropellerComboScore, propellerRocketComboScore, propellerTntComboScore;
        protected int rocketRocketComboScore, rocketTntComboScore, tntTntComboScore, lightballLightballComboMultiplier;
        protected int remainingMoveScore, remainingSpecialItemsMultiplier;
        public int TotalLevelScoreForEvt { get; private set; }
        public IOriginalSessionEventBundle GameplayBundle => gameplayBundle;

        public OriginalClashOfChampionsLevelData(Func<IOriginalClashSessionManager> resolveManager,
            Func<IOriginalSessionBundleManager> bundleManager, Func<IOriginalClashSessionPools> pools,
            Func<IOriginalSessionEventBundle, string, IOriginalClashGameplayAsset> loadAsset,
            Func<int> activeAmountMultiplier, Func<OriginalActiveLevelData> currentLevel)
        {
            this.resolveManager = resolveManager ?? throw new ArgumentNullException(nameof(resolveManager));
            this.bundleManager = bundleManager ?? throw new ArgumentNullException(nameof(bundleManager));
            this.pools = pools ?? throw new ArgumentNullException(nameof(pools));
            this.loadAsset = loadAsset ?? throw new ArgumentNullException(nameof(loadAsset));
            this.activeAmountMultiplier = activeAmountMultiplier ?? throw new ArgumentNullException(nameof(activeAmountMultiplier));
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
        }
        private IOriginalClashSessionPools Pools()
        { var value = pools(); if (value == null) throw new NullReferenceException(); return value; }
        private IOriginalSessionEventBundle GetBundle(string name)
        {
            var owner = bundleManager(); if (owner == null) throw new NullReferenceException();
            var value = owner.GetBundle(name); if (value == null) throw new NullReferenceException();
            var typed = value as IOriginalSessionEventBundle; if (typed == null) throw new InvalidCastException();
            return typed;
        }
        public void Init(sbyte levelDifficulty, bool isMultiplied, bool isAfterRestore = false) // 0x338c958.
        {
            if (isAfterRestore) TotalLevelScoreForEvt = After;
            else { ShouldConsume = false; Before = 0; After = 0; TotalLevelScoreForEvt = 0; }
            manager = resolveManager(); if (manager == null) throw new NullReferenceException();
            helper = manager.Helper;
            HandleGameplayBundleAndCollectViewPool();
            int difficultyMultiplier = GetMultiplierValueForDifficulty(levelDifficulty);
            int amountMultiplier = isMultiplied ? activeAmountMultiplier() : 1;
            scoreMultiplier = unchecked(amountMultiplier * difficultyMultiplier);
            if (helper == null) throw new NullReferenceException();
            isScoreLockedForLevel = helper.IsScoreLocked;
            if (manager == null) throw new NullReferenceException();
            var config = manager.Config; if (config == null) throw new NullReferenceException();
            matchItemScore = config.GetMatchItemScore();
            propellerItemScore = config.GetPropellerItemScore();
            rocketItemScore = config.GetRocketItemScore();
            tntItemScore = config.GetTntItemScore();
            lightballItemScore = config.GetLightballItemScore();
            propellerPropellerComboScore = config.GetPropellerPropellerComboScore();
            propellerRocketComboScore = config.GetPropellerRocketComboScore();
            propellerTntComboScore = config.GetPropellerTntComboScore();
            rocketRocketComboScore = config.GetRocketRocketComboScore();
            rocketTntComboScore = config.GetRocketTntComboScore();
            tntTntComboScore = config.GetTntTntComboScore();
            lightballLightballComboMultiplier = config.GetLightballLightballComboMultiplier();
            remainingMoveScore = config.GetRemainingMoveScore();
            remainingSpecialItemsMultiplier = config.GetWinBonus();
        }
        protected void HandleGameplayBundleAndCollectViewPool() // 0x338cba0.
        {
            if (manager == null) throw new NullReferenceException();
            if (manager.IsInEvent)
            {
                if (manager == null) throw new NullReferenceException();
                if (!manager.IsAllBundlesDownloaded()) return;
                var bundle = GetBundle(GameplayBundleName);
                if (!bundle.LoadBundle()) return;
                var asset = loadAsset(bundle, CollectViewAssetName); if (asset == null) throw new NullReferenceException();
                var prefab = asset.GetCollectViewPrefab();
                Pools().TryCreateClashOfChampionsGameplayCollectViewPool(prefab); // Native forwards a null component too.
                gameplayBundle = bundle;
            }
            else if (gameplayBundle != null)
            {
                Pools().TryClearClashOfChampionsGameplayCollectViewPool();
                var previous = gameplayBundle; if (previous == null) throw new NullReferenceException();
                previous.UnloadBundle(true);
                gameplayBundle = null;
                GetBundle(DialogBundleName).UnloadBundle(true);
            }
        }
        public override void LoadSaveData(long value) // 0x338c8d0, inherited unsigned 1/20/20 snapshot.
        {
            base.LoadSaveData(value);
            var difficultyLevel = currentLevel(); if (difficultyLevel == null) throw new NullReferenceException();
            var multiplierLevel = currentLevel(); if (multiplierLevel == null) throw new NullReferenceException();
            Init(difficultyLevel.Difficulty, multiplierLevel.IsMultipliedInLevel, true);
        }
        public void ResetDataOnRoundFinished() // 0x338ce20: total, multiplier and bundle are retained.
        { ShouldConsume = false; Before = 0; After = 0; }
        protected bool CanIncreaseScore // 0x338c878.
        {
            get
            {
                if (manager == null) throw new NullReferenceException();
                return manager.ShouldProgress && !isScoreLockedForLevel;
            }
        }
        protected int CalculateFinalScore(int score) => unchecked(scoreMultiplier * score); // 0x338c8b4.
        protected int IncrementScore(int score, int comboType = 0) // 0x338cea0.
        {
            if (helper == null) throw new NullReferenceException();
            int final = CalculateFinalScore(score);
            helper.UpdateLocalScore(final, comboType);
            AddExtra(final);
            TotalLevelScoreForEvt = unchecked(TotalLevelScoreForEvt + final);
            return final;
        }
        public int IncrementScoreForItem(int itemType) // 0x338ce2c.
        {
            if (!CanIncreaseScore) return 0;
            int score = itemType == 2 ? rocketItemScore : itemType == 3 ? propellerItemScore : itemType == 4 ? tntItemScore : matchItemScore;
            return IncrementScore(score);
        }
        public int IncrementScoreForCombo(int comboType, int normalCellCount) // 0x338cf00; ELF jump table 0x4053e89.
        {
            if (!CanIncreaseScore) return 0;
            int score;
            switch (comboType)
            {
                case 1: score = unchecked(lightballLightballComboMultiplier * normalCellCount); break;
                case 5: score = tntTntComboScore; break;
                case 6: score = rocketTntComboScore; break;
                case 7: score = rocketRocketComboScore; break;
                case 8: score = propellerTntComboScore; break;
                case 9: score = propellerRocketComboScore; break;
                case 10: score = propellerPropellerComboScore; break;
                default: score = 0; break;
            }
            return IncrementScore(score, comboType);
        }
        public void IncrementScoreForRemainingMoves(int remainingMoves) // 0x338d10c.
        { if (CanIncreaseScore) IncrementScore(unchecked(remainingMoveScore * remainingMoves)); }
    }
}
