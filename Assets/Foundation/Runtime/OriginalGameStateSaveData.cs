namespace Whitebox.Foundation
{
    public interface IOriginalEgoSaveState { bool ItemThrowInProgress { get; } }
    public interface IOriginalNewUserTutorialSaveState { bool HasShownAnyDynamicTutorial(); }
    public interface IOriginalLevelTutorialSaveState { bool FreeBoosterTutorialBoosterUsage { get; } }
    public interface IOriginalDynamicTutorialSaveState
    {
        bool ItemShown { get; }
        bool SuperDukeShown { get; }
        bool MagicDukeShown { get; }
        bool MagicLanternShown { get; }
    }
    // Required context resolution, in the native order. New-user lookup must
    // enforce UserContext unit2; current-level lookup must enforce user/session.
    // Their contained helper/active-level may be null and fail later on access.
    public interface IOriginalGameStateSaveServices
    {
        OriginalMoveManager GetMoveManager();
        IOriginalEgoSaveState GetEgoState();
        OriginalLevelRandomManager GetLevelRandomManager();
        IOriginalNewUserTutorialSaveState GetNewUserTutorialState();
        IOriginalLevelTutorialSaveState GetLevelTutorialState();
        IOriginalDynamicTutorialSaveState GetDynamicTutorialState();
        OriginalActiveLevelData GetCurrentActiveLevel();
    }
    // Native GameStateSaveDataHelper:86 bits in two longs, not a JSON schema.
    // Database transport and owning user/save manager remain separate services.
    public sealed class OriginalGameStateSaveData : OriginalGenericSaveDataHelper, IOriginalSavedLevelRandom
    {
        // Field initializer SHA256:6E8ABEB607AC371B7F4CA439BAB8A685D5D78ECD4DE301C89828FE32150B31FC.
        private static readonly int[] DataSizes = { 7, 7, 13, 7, 1, 10, 30, 1, 1, 1, 5, 1, 1, 1 };
        public OriginalGameStateSaveData() { SetConfig(new OriginalGenericSaveDataConfig(DataSizes)); } // 0x23e2a7c.
        public long[] PrepareStateSaveData(IOriginalGameStateSaveServices services) // 0x23e0834.
        {
            var moves = services.GetMoveManager(); // LevelContext11,32,0; UserContext2; LevelContext21,51.
            var ego = services.GetEgoState();
            var random = services.GetLevelRandomManager();
            var newUser = services.GetNewUserTutorialState();
            var tutorial = services.GetLevelTutorialState();
            var dynamicTutorial = services.GetDynamicTutorialState();
            var active = services.GetCurrentActiveLevel();
            var values = new long[14];
            values[0] = moves.LeftMoves;
            values[1] = active.MoveCount;
            values[2] = moves.MadeMoves;
            values[3] = active.EgoPurchaseCount;
            values[4] = ego.ItemThrowInProgress ? 1 : 0;
            values[5] = random.DifficultySource.DifficultySeed;
            values[6] = random.DifficultySource.DifficultySeedGenerationCount;
            values[7] = newUser.HasShownAnyDynamicTutorial() ? 1 : 0;
            values[8] = tutorial.FreeBoosterTutorialBoosterUsage ? 1 : 0;
            values[9] = dynamicTutorial.ItemShown ? 1 : 0;
            values[10] = unchecked(OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount + 15);
            values[11] = dynamicTutorial.SuperDukeShown ? 1 : 0;
            values[12] = dynamicTutorial.MagicDukeShown ? 1 : 0;
            values[13] = dynamicTutorial.MagicLanternShown ? 1 : 0;
            return PrepareData(values);
        }
        public int GetLeftMoves() => unchecked((int)GetData(0)); // 0x23e3490.
        public int GetMaxMoves() => unchecked((int)GetData(1)); // 0x23e345c.
        public int GetMadeMoves() => unchecked((int)GetData(2)); // 0x23e3a58.
        public int GetEgoPurchaseCount() => unchecked((int)GetData(3)); // 0x23e34c4.
        public int GetEgoPackageItemThrowInProgress() => unchecked((int)GetData(4)); // 0x23e34f8.
        public int GetDifficultySeed() => unchecked((int)GetData(5)); // 0x23e6304.
        public int GetDifficultySeedGenerationCount() => unchecked((int)GetData(6)); // 0x23e6318.
        public bool HasShowedAnyDynamicTutorial() => unchecked((int)GetData(7)) == 1; // 0x23e632c.
        public bool FreeBoosterTutorialBoosterUsage() => unchecked((int)GetData(8)) == 1; // 0x23e6348.
        public bool GetDynamicItemTutorialShown() => GetData(9) == 1; // 0x23e6364.
        public int GetAddedExtraMoveCount() => unchecked((int)GetData(10) - 15); // 0x23e6380.
        public bool GetDynamicSuperDukeTutorialShown() => GetData(11) == 1; // 0x23e6398.
        public bool GetDynamicMagicDukeTutorialShown() => GetData(12) == 1; // 0x23e63b4.
        public bool GetDynamicMagicLanternItemTutorialShown() => GetData(13) == 1; // 0x23e63d0.

        internal long[] PrepareCoreState(OriginalMoveManager moves, OriginalActiveLevelData active,
            OriginalLevelRandomSource difficulty, bool shownDynamicTutorial, bool usedTutorialBooster)
        {
            var values = new long[DataSizes.Length];
            values[0] = moves.LeftMoves;
            values[1] = active.MoveCount;
            values[2] = moves.MadeMoves;
            values[5] = difficulty.DifficultySeed;
            values[6] = difficulty.DifficultySeedGenerationCount;
            // The early tap and combo guides use NewUserNewUxHelper 0x1f71ab0.
            // The separate dynamic item flag belongs to later obstacle guides.
            values[7] = shownDynamicTutorial ? 1 : 0;
            values[8] = usedTutorialBooster ? 1 : 0;
            values[10] = 15; // No extra move adjustment is active for the scoped local levels.
            return PrepareData(values);
        }
    }
}
