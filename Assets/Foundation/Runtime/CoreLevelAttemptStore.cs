using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // Local numbered-level counterpart of LevelSaveManager. Only the main
    // thread reads models. The writer receives detached save data.
    internal sealed class CoreLevelAttemptStore
    {
        internal const int SaveVersionWithoutOpeningRefillCounts = 1;
        private const int CoreSaveVersion = 2;
        private const int MaxSaveBytes = 1024 * 1024;
        private const int MaxDifficultyDraws = 10_000_000;
        private readonly string path;
        private readonly object gate = new object();
        private readonly OriginalGameStateSaveData state = new OriginalGameStateSaveData();
        private FLevelSaveData pendingWrite;
        private Task writer;
        private Exception writerError;
        private bool writePending, writing;
        private int readyFrames, lastMoves;
        private bool needsSave, watching;

        internal CoreLevelAttemptStore(string progressPath) { path = progressPath + ".level"; }

        internal FLevelSaveData Load(FTiledLevel level)
        {
            try
            {
                using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (input.Length < 4 || input.Length > MaxSaveBytes)
                    throw new InvalidDataException("Invalid level save file size.");
                using var reader = new BinaryReader(input);
                var saved = OriginalLevelSaveFlatBuffer.Decode(reader.ReadBytes((int)input.Length));
                Validate(saved, level);
                return saved;
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
            catch (Exception error) when (error is InvalidDataException || error is IOException ||
                error is UnauthorizedAccessException || error is DecoderFallbackException)
            {
                Debug.LogWarning("Ignoring the local level save. " + error.Message);
                Clear();
                return null;
            }
        }

        internal void Begin(OriginalPlayableBoard game, OriginalGameStateSaveData restored)
        {
            watching = !game.Session.IsSideProgressionLevel;
            // GameManager.StartLevel 0x1e3b094 queues this before view initialization.
            SaveImmediate(game, restored?.HasShowedAnyDynamicTutorial() ?? false,
                restored?.FreeBoosterTutorialBoosterUsage() ?? false);
        }

        internal void Tick(OriginalPlayableBoard game, CoreGameFlow flow)
        {
            if (!watching) return;
            var currentState = game.State;
            if (currentState.IsWin() || currentState.IsFail() || currentState.IsQuit() || currentState.IsNoPossibleMove())
            {
                Clear();
                return;
            }
            if (!currentState.IsPlaying() && !currentState.IsEgo()) return;

            // LevelSaveManager.ManualUpdate 0x23dfe5c. Two operation-free
            // frames are required after the last board or tutorial operation.
            bool busy = currentState.HasAnyOperation() || game.Auto.IsThereAWaitingMatch ||
                !game.Fall.IsAllItemsSteady || !flow.CanSaveAttempt;
            needsSave |= busy || game.Moves.MadeMoves != lastMoves;
            if (busy) { readyFrames = 0; return; }
            if (!needsSave || ++readyFrames < 2) return;
            SaveImmediate(game, flow.HasShownDynamicTutorial, flow.UsedTutorialBooster);
        }

        internal void SaveImmediate(OriginalPlayableBoard game, bool shownDynamicTutorial, bool usedTutorialBooster)
        {
            if (!watching) return;
            var data = new FLevelSaveData {
                ValidationSaveData = new FValidationSaveData { LevelName = game.Session.Name, AppVersion = CoreSaveVersion },
                BoardSaveData = game.CaptureBoard(),
                GameStateSaveData = state.PrepareCoreState(game.Moves, game.Session, game.GetLevelRandom(),
                    shownDynamicTutorial, usedTutorialBooster),
                BoosterSaveData = Array.Empty<long>(), LevelStatisticsSaveData = game.CaptureStatistics(),
                EventSaveData = Array.Empty<long>()
            };
            data.ValidationSaveData.Checksum = Checksum(data);
            Enqueue(data);
            lastMoves = game.Moves.MadeMoves;
            needsSave = false;
            readyFrames = 0;
        }

        internal void Clear()
        {
            watching = false;
            Enqueue(null);
            Flush();
        }

        internal void Flush()
        {
            Task current;
            lock (gate) current = writer;
            current?.GetAwaiter().GetResult();
            Exception error;
            lock (gate) { error = writerError; writerError = null; }
            if (error != null) Debug.LogWarning("Could not write the local level save. " + error.Message);
        }

        private void Enqueue(FLevelSaveData data)
        {
            lock (gate)
            {
                pendingWrite = data;
                writePending = true;
                if (writing) return;
                writing = true;
                writer = Task.Run(WritePending);
            }
        }

        private void WritePending()
        {
            while (true)
            {
                FLevelSaveData data;
                lock (gate)
                {
                    if (!writePending) { writing = false; return; }
                    data = pendingWrite;
                    writePending = false;
                    pendingWrite = null;
                }
                try
                {
                    string temporary = path + ".tmp";
                    if (data == null)
                    {
                        File.Delete(path);
                        File.Delete(temporary);
                    }
                    else
                    {
                        File.WriteAllBytes(temporary, OriginalLevelSaveFlatBuffer.Encode(data));
                        if (File.Exists(path)) File.Replace(temporary, path, null);
                        else File.Move(temporary, path);
                    }
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    lock (gate) writerError = error;
                }
            }
        }

        // LevelLoadValidationHelper.GetChecksum 0x23df110. The local core
        // profile has no remote player ID. Schema version and level name are
        // checked separately, as in LevelRestoreManager.ShouldLoadGameScene.
        private static long Checksum(FLevelSaveData data) =>
            (long)data.ValidationSaveData.LevelName.Length + data.BoardSaveData.GridData.Grid.Length +
            data.GameStateSaveData.Length + data.BoosterSaveData.Length + data.LevelStatisticsSaveData.Length;

        private void Validate(FLevelSaveData saved, FTiledLevel level)
        {
            var identity = saved.ValidationSaveData;
            var board = saved.BoardSaveData;
            var grid = board?.GridData;
            var creator = board?.ItemModelCreatorSaveData;
            var queues = creator?.PredefinedItemsSaveData?.Queues;
            if (identity?.LevelName != level.Name || identity.AppVersion is not (SaveVersionWithoutOpeningRefillCounts or CoreSaveVersion) ||
                grid?.Grid == null || grid.Width != level.Grid.Width || grid.Height != level.Grid.Height ||
                grid.Grid.Length != level.Grid.Cells.Length || (grid.ShifterData?.Length ?? 0) != 0 ||
                queues?.Length != 9 || saved.GameStateSaveData?.Length != 2 ||
                saved.BoosterSaveData == null ||
                saved.LevelStatisticsSaveData?.Length is not (0 or OriginalLevelStatisticsSaveData.SavedWordCount) ||
                identity.Checksum != Checksum(saved))
                throw new InvalidDataException("Saved level identity or layout does not match.");

            state.SetData(saved.GameStateSaveData);
            int max = state.GetMaxMoves(), left = state.GetLeftMoves(), made = state.GetMadeMoves();
            if (max != level.Move || left <= 0 || made + left != max ||
                state.GetEgoPackageItemThrowInProgress() != 0 || state.GetDifficultySeedGenerationCount() > MaxDifficultyDraws)
                throw new InvalidDataException("Saved level budget or difficulty cursor is invalid.");
            ValidateGoals(board.Goals, level);
            for (int i = 0; i < grid.Grid.Length; i++)
            {
                var cell = grid.Grid[i];
                ValidateItem(cell.CurrentItem, false);
                int staticMask = 0;
                foreach (var item in cell.StaticItems ?? Array.Empty<FStaticItemSaveData>())
                {
                    bool grass = item.TiledId == 31 || item.TiledId == 32;
                    int bit = grass ? 1 : 2;
                    if ((staticMask & bit) != 0 || (grass ? item.Data < 1 || item.Data > 2 :
                        item.TiledId != OriginalHoneyBoardItem.TiledId || item.Data != 1))
                        throw new InvalidDataException("Invalid saved static item.");
                    staticMask |= bit;
                }
            }
            foreach (var queue in queues)
                foreach (var item in queue.Items ?? Array.Empty<FItemSaveData>()) ValidateItem(item, true);

            var types = new HashSet<int>();
            foreach (var pair in creator.CreatedSetItemCountExceptMatchItem ?? Array.Empty<FIntIntPair>())
                if (pair.Key <= 1 || pair.Value < 0 || !types.Add(pair.Key))
                    throw new InvalidDataException("Invalid saved refill counts.");
        }

        private static void ValidateItem(FItemSaveData item, bool queued)
        {
            int id = item?.TiledId ?? 0;
            if (id == 0 && !queued) return;
            int type = OriginalTiledItemTypes.AsItemType(id);
            if (type == 1 && (item.Data == 1 || item.Data == 9)) return;
            if (type >= 2 && type <= 15 && type != 12 && item.Data > 0 &&
                item.Data <= OriginalTiledItemLayers.AsLayerCount(id)) return;
            if (type == 12 && ((id == 53 && item.Data > 0 && item.Data <= 15) ||
                ((id == 43 || id == 44 || id == 54) && item.Data == 1))) return;
            throw new InvalidDataException("Invalid saved item.");
        }

        private static void ValidateGoals(FBoardGoalSaveData[] boards, FTiledLevel level)
        {
            int boardCount = 0;
            foreach (var goal in level.Goals) boardCount = Math.Max(boardCount, goal.BoardId + 1);
            if (boards?.Length != boardCount) throw new InvalidDataException("Invalid saved goal board count.");
            for (int i = 0; i < boards.Length; i++)
            {
                var expected = new HashSet<int>();
                foreach (var goal in level.Goals)
                {
                    if (goal.BoardId != i) continue;
                    if (goal.Count != 0 || goal.Goal == 21) { expected.Add(goal.Goal); continue; }
                    int prerequisite = OriginalLevelGoalPreparation.GetPrerequisiteGoal(goal.Goal);
                    foreach (var other in level.Goals)
                        if (other.BoardId == i && other.Goal == prerequisite && other.Count != 0) expected.Add(goal.Goal);
                }
                var saved = boards[i];
                var types = saved.GoalType ?? Array.Empty<short>();
                if (types.Length != expected.Count || (saved.LeftCount?.Length ?? 0) != types.Length ||
                    (saved.TargetCount?.Length ?? 0) != types.Length)
                    throw new InvalidDataException("Invalid saved goal vector length.");
                for (int j = 0; j < types.Length; j++)
                    if (!expected.Remove(types[j]) || saved.LeftCount[j] < 0 || saved.TargetCount[j] < 0)
                        throw new InvalidDataException("Invalid saved goal data.");
            }
        }
    }
}
