using System;
using System.IO;

namespace Whitebox.Foundation
{
    // Core-only product data. Reuses the original native SQLite transport,
    // KeyValue schema/typed writes and commit-before-reopen transaction flow.
    // No SDK identity, account/area loader, balances, lives or event owners.
    public sealed class CoreLevelProgress : IDisposable
    {
        private const string LevelKey = "Level", CompletedKey = "CoreLevelsCompleted";
        private const string BoosterCoinsKey = "CoreBoosterCoins";
        private const string AttemptLevelKey = "CoreAttemptLevel";
        private const string PowerHintTapCountKey = "CorePowerHintTapCount";
        private const string SpecialTapCountKey = "CoreSpecialTapCount", ComboUseCountKey = "CoreComboUseCount";
        private const string TapGuideSeenKey = "CoreTapGuideSeen", ComboGuidePendingKey = "CoreComboGuidePending";
        // NewUserDefaultUxConfig constructor 0x284188c.
        internal const int TapGuideFirstLevel = 4, TapGuideLastLevel = 19, TapGuideTapLimit = 3;
        internal const int ComboGuideFirstLevel = 5, ComboGuideLastLevel = 27, ComboGuideUseLimit = 2;
        private readonly IOriginalKeyValueConnection connection;
        private readonly OriginalDatabaseService database;
        private readonly OriginalUserKeyValue values;
        private readonly bool usesAuthoritativeLevel;
        private bool paused;
        public string DatabasePath { get; }
        public int LevelToPlay { get; private set; }
        public bool IsComplete { get; private set; }
        public int BoosterCoins => Math.Max(0, values.GetInt(BoosterCoinsKey));
        public int PowerHintTapCount { get; private set; }
        public int LevelTryCount => values.GetInt(OriginalAccountStorage.LevelTryCountKey);
        internal int SpecialTapCount { get; private set; }
        internal int ComboUseCount { get; private set; }
        internal bool TapGuideSeen { get; private set; }
        internal bool ComboGuidePending { get; private set; }
        public CoreLevelProgress(string fileName, int initialLevel) : this(fileName, initialLevel, false) { }

        internal CoreLevelProgress(string fileName, int initialLevel, bool usesAuthoritativeLevel)
        {
            this.usesAuthoritativeLevel = usesAuthoritativeLevel;
            if (usesAuthoritativeLevel) CheckLevel(initialLevel);
            if (string.IsNullOrEmpty(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("Core progress requires a configured file name.", nameof(fileName));
            DatabasePath = Path.Combine(OriginalRuntimeStorage.Paths.ApplicationDataPath, fileName);
            connection = OriginalRuntimeStorage.Open(DatabasePath);
            database = new OriginalDatabaseService(connection, OriginalDatabaseService.UserSaveInterval, null);
            database.OnApplicationResume();
            values = new OriginalUserKeyValue(() => database);
            if (usesAuthoritativeLevel)
                SetAuthoritativeLevel(initialLevel);
            else
            {
                LevelToPlay = values.GetInt(LevelKey, initialLevel);
                // Migrate the old standalone end-of-catalog save to the next real level.
                if (values.GetInt(CompletedKey) == 1)
                {
                    if (LevelToPlay == CoreLevelCatalog.SupportedLevelCount)
                    {
                        LevelToPlay++;
                        values.SetInt(LevelKey, LevelToPlay);
                        values.SetInt(OriginalAccountStorage.LevelTryCountKey, 0);
                        values.SetInt(OriginalAccountStorage.HardLevelBackgroundIndexKey, 0);
                    }
                    values.SetInt(CompletedKey, 0);
                    database.Save();
                }
            }
            PowerHintTapCount = values.GetInt(PowerHintTapCountKey);
            // Existing core saves only counted TNT/rocket taps.
            SpecialTapCount = values.GetInt(SpecialTapCountKey, PowerHintTapCount);
            ComboUseCount = values.GetInt(ComboUseCountKey);
            TapGuideSeen = values.GetInt(TapGuideSeenKey) == 1;
            bool reachedComboGuide = LevelToPlay >= ComboGuideFirstLevel && LevelToPlay <= ComboGuideLastLevel;
            ComboGuidePending = values.GetInt(ComboGuidePendingKey, reachedComboGuide ? 1 : 0) == 1;
            CheckLevel(LevelToPlay);
        }
        private static void CheckLevel(int number)
        {
            if (number < 1)
                throw new InvalidDataException("Core progress requires a positive level number.");
        }
        internal void SetAuthoritativeLevel(int number)
        {
            CheckLevel(number);
            LevelToPlay = number;

            // Old saves associate their attempt counter with the local level.
            // Keep that association for matching attempts without reading it for selection.
            int previousAttemptLevel = values.GetInt(AttemptLevelKey, 0);
            if (previousAttemptLevel == number) return;
            if (previousAttemptLevel == 0) previousAttemptLevel = values.GetInt(LevelKey, number);
            if (previousAttemptLevel != number)
            {
                values.SetInt(OriginalAccountStorage.LevelTryCountKey, 0);
                values.SetInt(OriginalAccountStorage.HardLevelBackgroundIndexKey, 0);
            }
            values.SetInt(AttemptLevelKey, number);
            database.Save();
        }

        internal int RecordLevelStart() // StartGame 0x1e38b40 and RestartGame 0x1e3b6f8.
        {
            int count = values.IncrementInt(OriginalAccountStorage.LevelTryCountKey);
            database.Save();
            return count;
        }
        public void CompleteLevel(int number)
        {
            CheckLevel(number);
            if (usesAuthoritativeLevel)
            {
                UpdateTutorialProgressAfterWin(number);
                database.Save();
                return;
            }
            LevelToPlay = checked(number + 1);
            UpdateTutorialProgressAfterWin(number);
            values.SetInt(LevelKey, LevelToPlay);
            values.SetInt(OriginalAccountStorage.LevelTryCountKey, 0);
            values.SetInt(OriginalAccountStorage.HardLevelBackgroundIndexKey, 0);
            database.Save();
        }

        public int AddBoosterCoins(int amount)
        {
            if (amount <= 0) return 0;
            int current = BoosterCoins;
            int added = Math.Min(amount, int.MaxValue - current);
            if (added <= 0) return 0;
            values.SetInt(BoosterCoinsKey, current + added);
            database.Save();
            return added;
        }

        public bool TrySpendBoosterCoins(int amount)
        {
            if (amount <= 0) return false;
            int current = BoosterCoins;
            if (current < amount) return false;
            values.SetInt(BoosterCoinsKey, current - amount);
            database.Save();
            return true;
        }
        private void UpdateTutorialProgressAfterWin(int number)
        {
            if (number == ComboGuideFirstLevel - 1 && ComboUseCount < ComboGuideUseLimit)
                SetComboGuidePending(true); // 0x2841cd8.
        }

        public void StartOver()
        {
            if (usesAuthoritativeLevel) return;
            LevelToPlay = 1; IsComplete = false;
            values.SetInt(LevelKey, 1);
            values.SetInt(CompletedKey, 0);
            values.SetInt(OriginalAccountStorage.LevelTryCountKey, 0);
            database.Save();
        }
        internal void RecordPowerHintTap()
        {
            values.SetInt(PowerHintTapCountKey, ++PowerHintTapCount);
        }
        internal void RecordTutorialTap() => values.SetInt(SpecialTapCountKey, ++SpecialTapCount);
        internal void RecordTutorialCombo() => values.SetInt(ComboUseCountKey, ++ComboUseCount);
        internal void MarkTapGuideSeen()
        {
            TapGuideSeen = true;
            values.SetInt(TapGuideSeenKey, 1);
        }
        internal void SetComboGuidePending(bool pending)
        {
            ComboGuidePending = pending;
            values.SetInt(ComboGuidePendingKey, pending ? 1 : 0);
        }
        internal void ManualUpdate(int frameCount)
        {
            if (!paused) database.ManualUpdate(frameCount);
        }
        public void SetApplicationPaused(bool isPaused)
        {
            if (paused == isPaused) return;
            if (isPaused) database.OnApplicationPause();
            else database.OnApplicationResume();
            paused = isPaused;
        }
        internal void OnApplicationQuit() => database.OnApplicationQuit();
        public void Save() => database.Save();
        public void Dispose()
        {
            try { database.OnApplicationQuit(); }
            finally { connection.Dispose(); }
        }
    }
}
