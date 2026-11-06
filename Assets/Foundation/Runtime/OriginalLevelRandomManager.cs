using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalRandomUserEvents { event Action OnUserChanged; }
    public interface IOriginalSavedLevelRandom
    {
        int GetDifficultySeed();
        int GetDifficultySeedGenerationCount();
    }
    public interface IOriginalLevelRandomLifecycleServices : IOriginalAttemptSeedContext
    {
        IOriginalLevelSeedRandom GetApplicationRandom();
        void RequireCurrentUserSession();
        IOriginalRandomUserEvents GetUserEvents();
        bool IsCurrentLevelWon { get; }
        IOriginalSavedLevelRandom GetSavedRandomData();
    }
    // Native LevelRandomManager lifecycle. Application random, user/session,
    // device and save/storage providers are required, never fallback seeds.
    public sealed class OriginalLevelRandomManager : IOriginalLevelRandomContextUnit, IOriginalMatchRandomSource
    {
        private readonly IOriginalLevelRandomLifecycleServices services;
        private readonly Func<int> deviceKeyResolver;
        private readonly OriginalAttemptSeeds attempts;
        private Random random;
        public OriginalLevelRandomSource DifficultySource { get; }
        public bool IsRemoteLevel, IsSetFromAdminPanel, HasSwitchedLevel;
        public int AdminSeed;
        public int Id => 0; // 0x1ed904c.
        public long DeviceIdKey => attempts.GetDeviceIdKey(deviceKeyResolver); // 0x1ed8f04.
        public static int ParseDeviceIdKey(string value) // 0x1ed8f28, SDK string transport supplied by caller.
        {
            string digits = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsDigit(c)) digits += c;
                if (digits.Length > 4) break;
            }
            return int.Parse(digits);
        }
        internal OriginalAttemptSeeds AttemptState => attempts;
        public OriginalLevelRandomManager(IOriginalLevelRandomLifecycleServices services) // 0x1ed9b6c.
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            deviceKeyResolver = services.GetDeviceIdKey;
            DifficultySource = new OriginalLevelRandomSource();
            attempts = new OriginalAttemptSeeds(DifficultySource);
        }
        public void Bind() // 0x1ed9054: remove then add the same target/method.
        {
            var user = services.GetUserEvents();
            if (user == null) throw new NullReferenceException();
            user.OnUserChanged -= ClearUserCaches;
            user.OnUserChanged += ClearUserCaches;
        }
        public void ResetSeed() // 0x1ed9160.
        {
            var application = services.GetApplicationRandom();
            if (application == null) throw new NullReferenceException();
            random = new Random(application.Next(0, 20000000)); // Native range protocol.
            services.RequireCurrentUserSession();
            attempts.ResetDifficultySeed(services);
        }
        public int Next(int min, int max) => random.Next(min, max); // 0x1ed9438.
        public bool NextBool() => random.Next(0, 2) == 0; // 0x1ed9458.
        public int DifficultyNext(int min, int max) => DifficultySource.DifficultyNext(min, max);
        public byte GetRandomItemFromListBasedOnDifficulty(List<byte> values) => DifficultySource.GetRandomItemFromListBasedOnDifficulty(values);
        private void ClearUserCaches() // 0x1ed9a00: retains streams/list/device/admin data.
        {
            IsRemoteLevel = false;
            attempts.ClearUserCaches();
            HasSwitchedLevel = false;
        }
        public void Clear(bool gameExit) // 0x1ed9a10 deliberately ignores gameExit.
        { if (services.IsCurrentLevelWon) ClearUserCaches(); }
        public void RestoreFromSaveData() // 0x1ed9a8c, two current-save lookups.
        {
            var saved = services.GetSavedRandomData();
            if (saved == null) throw new NullReferenceException();
            DifficultySource.SetDifficultySeed(saved.GetDifficultySeed());
            saved = services.GetSavedRandomData();
            if (saved == null) throw new NullReferenceException();
            DifficultySource.RestoreDifficultyGenerationCount(saved.GetDifficultySeedGenerationCount());
        }
    }
}
