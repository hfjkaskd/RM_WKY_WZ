using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalAttemptSeedContext
    {
        int GetLevelNumber();
        long GetUserId();
        int GetDeviceIdKey();
        int GetAttemptIndex();
    }
    // Difficulty subset of LevelRandomManager, 0x1ed92b4 and 0x1ed95bc.
    public sealed class OriginalAttemptSeeds
    {
        private const int MaxSeed = 1000; // Original protocol constant.
        private readonly List<ushort> seedList = new List<ushort>(MaxSeed);
        private readonly OriginalLevelRandomSource random;
        private bool isSeedListGenerated;
        private int deviceIdKey;
        internal int SelectedSeed => random.DifficultySeed;
        internal bool IsGenerated => isSeedListGenerated;
        internal int CachedDeviceIdKey => deviceIdKey;
        internal IReadOnlyList<ushort> Seeds => seedList;

        public OriginalAttemptSeeds(OriginalLevelRandomSource random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        // Device ID parsing and persisted attempt-index reads remain caller responsibilities.
        public void ResetDifficultySeed(int levelNumber, long userId, int resolvedDeviceIdKey,
            int persistedAttemptIndex)
        {
            if (!isSeedListGenerated)
            {
                seedList.Clear();
                for (ushort i = 0; i < MaxSeed; i++) seedList.Add(i);
                if (deviceIdKey == 0) deviceIdKey = resolvedDeviceIdKey;
                int seed = unchecked(levelNumber + (int)userId + deviceIdKey);
                random.SetDifficultySeed(seed);
                random.ShuffleListBasedOnDifficulty(seedList);
                isSeedListGenerated = true;
            }
            random.SetDifficultySeed(seedList[persistedAttemptIndex % seedList.Count]);
        }

        // Live lifecycle path: avoid eager user/device/storage queries. The
        // native generated flag is set before reading the persisted attempt.
        public void ResetDifficultySeed(IOriginalAttemptSeedContext context)
        {
            if (!isSeedListGenerated)
            {
                int level = context.GetLevelNumber();
                seedList.Clear();
                for (ushort i = 0; i < MaxSeed; i++) seedList.Add(i);
                long user = context.GetUserId();
                if (deviceIdKey == 0) deviceIdKey = context.GetDeviceIdKey();
                int seed = unchecked(level + (int)user + deviceIdKey);
                random.SetDifficultySeed(seed);
                random.ShuffleListBasedOnDifficulty(seedList);
                isSeedListGenerated = true;
            }
            int attempt = context.GetAttemptIndex();
            random.SetDifficultySeed(seedList[attempt % seedList.Count]);
        }
        public int GetDeviceIdKey(Func<int> resolve)
        {
            if (deviceIdKey == 0) deviceIdKey = resolve();
            return deviceIdKey;
        }

        // ClearUserCaches 0x1ed9a00 clears the generated flag but keeps the device key.
        public void ClearUserCaches()
        {
            isSeedListGenerated = false;
        }
    }
}
