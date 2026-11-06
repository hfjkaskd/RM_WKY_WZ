using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Destructive Play-session diagnostic: restart Play afterward. The swap,
    // group, item/static/neighbor/goal and fall implementations are runtime code.
    public static class SwapGroupRefillVerification
    {
        private static OriginalInitialBoardView board;
        private static Fixture fixture;
        private static OriginalFallManager fall;
        private static OriginalSwapActionState action;
        private static OriginalAutoExplodeManager auto;
        private static OriginalContextContainer frameContext;
        private static OriginalContextUpdateHost updateHost;
        private static FrameUnit autoUnit, fallUnit;
        private static int hostStartCount, lateStartCount, clockStartCount;
        private static bool cascade;
        private static OriginalBoardItem[] before;
        private static OriginalBoxBoardItem[] boxes;
        private static Report report;
        private static int frame;
        private static float started, settled;
        [MenuItem("Recovery/Verify Swap Group Refill (Play Mode)")]
        public static void Run() => Run(false);
        [MenuItem("Recovery/Verify Automatic Cascade (Play Mode)")]
        public static void RunCascade() => Run(true);
        private static void Run(bool includeCascade)
        {
            Require(EditorApplication.isPlaying && board == null, "Run once in a fresh Play session");
            try
            {
                board = Object.FindObjectOfType<OriginalInitialBoardView>();
                Require(board != null && board.Goals.GetGoalLeftCount(11) == 36, "Fresh formal first level");
                var audio = Object.FindObjectOfType<OriginalGameAudioHost>();
                cascade = includeCascade; fixture = new Fixture(board, audio); report = new Report();
                if (cascade) report.scope = "Actual first-level swap/group/box-goal/refill path followed by native AutoExplodeManager and actual FindAllMatches/readiness/group explosion of a blue triple created by nine diagnostic predefined items (column1:3,1,3;column2:2,1,2;column3:3,1,4). Actual per-frame automatic scan/retry/idle gates, original difficulty-shuffle consumption, trigger4 delayed freeze, original match/swipe/fall/box SFX/particles/recycle, active-level counts, goals36to33 and nine automatic refill views. Predefined queues are explicit diagnostic inputs, not original first-level content. Frame dispatch uses native auto7 before fall8 through the actual runtime context host; complete original context-container composition remains pending. Also retains original fixture seed371/state2/inactive-Madness/normal-final-board/not-won/unworried contexts, traced haptic/cache-clear and normal-only special boundary. No manual item removal or direct automatic-group trigger. Full player input/session startup/moves, arbitrary cascades/special combos/collection/King/HUD/settlement and rendered-original parity remain pending. Restart Play to restore initial board; no production no-op installed; SDK unchanged.";
                before = new OriginalBoardItem[72];
                var root = ((OriginalMatchItemSpriteView)board.Grid.GetCell(0, 7).Mediation.CurrentItem.SwapView).transform.parent;
                for (int x = 0; x < 9; x++) for (int y = 0; y < 8; y++)
                {
                    var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                    before[x * 8 + y] = item;
                    if (item is OriginalMatchBoardItem match) match.BindExplosionServices(fixture.MatchItems);
                }
                var collection = new OriginalItemViewGoalCollection(board.GoalClaims, fixture,
                    () => throw new InvalidOperationException("Single-board fixture"), null);
                var viewServices = new OriginalBoardBoxViewServices(board.Presentation, audio, collection, SortingLayer.NameToID("Item"));
                boxes = new OriginalBoxBoardItem[3];
                for (int x = 1; x <= 3; x++)
                {
                    var box = boxes[x - 1] = (OriginalBoxBoardItem)board.Grid.GetCell(x, 3).Mediation.CurrentItem;
                    Require(box.LayerCount == 1 && box.SwapView == null, "Original one-layer box model");
                    Transform preview = null;
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root.GetChild(i);
                        if (child.gameObject.activeSelf && child.localPosition == new Vector3(x, 3, 0)
                            && child.GetComponent<OriginalMatchItemSpriteView>() == null)
                        { Require(preview == null, "Unique preview at box cell"); preview = child; }
                    }
                    Require(preview != null, "Configured box preview"); preview.gameObject.SetActive(false);
                    box.OnExplodeEvent += () => report.box_events++;
                    box.CreateView(board.Presentation, root, new Vector3(x, 3, 0), viewServices, fixture.UsefulMoves);
                }
                fixture.PrepareFilling();
                if (cascade) fixture.PrepareCascadeItems();
                var columns = new OriginalFallRefillColumns();
                var creation = new OriginalFillingItemFactory(columns,
                    new OriginalFillingCreationBackend(fixture.Predefined, fixture.Generation, new OriginalItemViewCreation(board)), fixture);
                fixture.Creation = creation;
                var context = new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, fixture, null);
                fall = new OriginalFallManager(columns, new OriginalFallTargetPlanner(context, columns), fixture, board.GameOperations);
                updateHost = Object.FindObjectOfType<OriginalContextUpdateHost>();
                Require(updateHost != null && updateHost.Context == null, "Configured unbound context host");
                frameContext = new OriginalContextContainer(OriginalLevelUnitRegistration.UnitCount);
                frameContext.Add(fixture.RandomManager);
                if (cascade)
                {
                    auto = new OriginalAutoExplodeManager(() => board.Clock.FrameCount);
                    autoUnit = new FrameUnit(7, () => auto.Bind(board.State, fixture), () =>
                    {
                        Require(fallUnit.LastFrame != Time.frameCount, "Native auto-before-fall registration order");
                        auto.ManualUpdate();
                    });
                    frameContext.Add(autoUnit);
                }
                fallUnit = new FrameUnit(8, () => fall.InitializeGrid(context), () =>
                {
                    if (cascade) Require(autoUnit.LastFrame == Time.frameCount, "Automatic matching precedes fall in the same Unity frame");
                    fall.ManualUpdate();
                });
                frameContext.Add(fallUnit); frameContext.BindAll();
                Require(fixture.RandomBindings == 1 && fixture.RandomManager.Id == 0, "Actual random context unit binds once before auto/fall");
                hostStartCount = updateHost.CompletedUpdateCount;
                lateStartCount = updateHost.CompletedLateUpdateCount;
                clockStartCount = board.Clock.FrameCount;
                updateHost.BindContext(frameContext);
                if (cascade)
                    report.scope = report.scope.Replace("Frame dispatch uses native auto7 before fall8 through the actual runtime context host; complete original context-container composition remains pending.",
                        "Actual prefab context host Update dispatches restored ContextContainer in native auto7-before-fall8 registration order; Editor update only observes. Binding/providers are explicit diagnostic bridges. Full27permanent-unit factories, late units, original game-context activation/session ownership remain pending.");
                else report.scope += " Actual prefab context host drives restored ContextContainer/FallManager in Unity Update; Editor only observes. Full original unit factories, late lifecycle and application activation remain pending.";
                report.scope += " Formal board uses native DeltaTimeManager construction and GameContext-style multicast late dispatcher through the configured prefab host. Clock progression must equal completed host LateUpdate callbacks; full parent GameContext activation/deactivation is separate.";
                report.scope += " Random seed371 is now selected through actual LevelRandomManager.ResetSeed before context BindAll, using real application Unity random and explicit diagnostic level1/user1/device12345/attempt362. User/storage providers are diagnostic inputs, not a production player context.";
                report.scope += " Packed-state preflight uses actual GameStateSaveData preparation and two-long readback, actual MoveManager restoration and LevelRandomManager save-provider replay. Sixteen future draws must match after restoring cursor5; the initial packed cursor0 is then restored before the board command. User/database transport and inactive tutorial/Ego contexts remain explicit diagnostic boundaries.";
                report.scope += " Core diagnostic now creates actual UserSessionData owning the shared concrete32-snapshot AnimationData and ActiveLevel, with one required pause subscription. State2/level1, inactive features and application transport remain explicit fixture inputs; formal production startup is still uncomposed.";
                action = board.FirstSwapAction;
                fixture.Resolution = new OriginalSwapMatchResolution(action, fixture);
                var adapter = new OriginalNormalSwapPresentation(root, board.Presentation, board.LocalMoves, fixture, SortingLayer.NameToID("Item"));
                // Native animation constants are explicit diagnostic inputs.
                var backend = new OriginalNormalSwapAnimationBackend(action, adapter, .135f, .04f);
                Require(((OriginalMatchBoardItem)board.Grid.GetCell(3, 5).Mediation.CurrentItem).MatchType == 2
                    && ((OriginalMatchBoardItem)board.Grid.GetCell(3, 4).Mediation.CurrentItem).MatchType == 3, "Original first-level swap colors");
                started = Time.realtimeSinceStartup; settled = -1f; frame = Time.frameCount;
                Require(action.StartSwapAnimation(board.Grid.GetCell(3, 5), board.Grid.GetCell(3, 4), backend), "Actual match search accepts leaf triple swap");
                Require(action.IsSwapping && board.GameOperations.Container.GetCount(2) == 1, "Actual swap operation starts");
                EditorApplication.update += Tick;
            }
            catch { Detach(); throw; }
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying) { Detach(); return; }
                if (frame == Time.frameCount) return; frame = Time.frameCount; report.frames++;
                Require(Time.realtimeSinceStartup - started < 15f, "Group/refill settles within fifteen seconds");
                if (fallUnit.Failure != null) throw new InvalidOperationException("Actual frame-dispatch failure", fallUnit.Failure);
                if (autoUnit != null && autoUnit.Failure != null) throw new InvalidOperationException("Actual automatic-dispatch failure", autoUnit.Failure);
                if (action.IsSwapping || fixture.Groups != 1 || !fall.IsAllItemsSteady
                    || fixture.Created.Count != (cascade ? 9 : 6) || cascade && fixture.AutoGroups != 1)
                { settled = -1f; return; }
                if (settled < 0f) { settled = Time.realtimeSinceStartup; return; }
                if (Time.realtimeSinceStartup - settled < 1.1f) return;
                Require(fixture.Session.MatchItemCounts.GetCount(2) == 3, "Three real leaf explosions counted on actual active-level data");
                Require(board.Goals.GetGoalLeftCount(11) == 33 && board.Goals.GetGoalUiLeftCount(11) == 33
                    && board.Goals.GetGoalDisplayCount(11) == 33, "Native box model/UI/display goals decrease36to33");
                Require(report.box_events == 3 && fixture.GoalUpdates == 3 && fixture.UiUpdates == 3, "Three box and goal notifications");
                foreach (var box in boxes) Require(box.LayerCount == 0 && !box.HasView && box.Mediator.CurrentCell == null, "Actual adjacent box destruction");
                Require(fixture.Session.IsUsefulMoveMade && fixture.King.ReliefCoroutine == null, "Actual useful-state and delayed King relief");
                int groups = cascade ? 2 : 1;
                Require(fixture.Haptics == groups && fixture.MatchSounds == groups && fixture.SpecialChecks == 1 && fixture.ClearRequests == 1,
                    "Single native completion/group feedback dispatch");
                if (cascade)
                {
                    Require(fixture.Session.MatchItemCounts.GetCount(1) == 3 && !auto.IsThereAWaitingMatch,
                        "Three real automatic blue matches counted and waiting flag cleared");
                    Require(board.Matches.FindAllMatches().Count == 0, "No remaining matches after automatic cascade");
                    Require(fixture.Shuffles == 1 && fixture.ShuffleDraws == 2, "Native value-copy shuffle consumes exactly two difficulty draws");
                    report.auto_groups = fixture.AutoGroups; report.auto_attempts = fixture.AutoAttempts;
                    report.shuffle_draws = fixture.ShuffleDraws;
                }
                for (int x = 0; x < 9; x++) for (int y = 0; y < 8; y++)
                {
                    var cell = board.Grid.GetCell(x, y); var item = cell.Mediation.CurrentItem;
                    Require(item != null && ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem), "All72 slots steady");
                    Require(!cell.IsFallFrozen, "All actual swap/group/timed freezes released");
                    if (item.SwapView is OriginalMatchItemSpriteView view)
                    {
                        float error = (view.transform.position - cell.GetViewPosition()).magnitude;
                        report.max_position_error = Mathf.Max(report.max_position_error, error);
                        Require(error <= .0011f && !view.IsFillMaskEnabled && !view.IsSwapping, "Actual landing view pose/mask/swap flags");
                    }
                    if (x < 1 || x > 3 || y < 3) Require(ReferenceEquals(item, before[x * 8 + y]), "Unaffected cells retain model identity");
                }
                Require(!board.GameOperations.Container.HasAny(), "Swap, freeze and fall operations all released");
                Require(board.Presentation.Pool.GetPoolLength(3) == 10, "Swap trails return to native pool");
                Require(fallUnit.Updates > 0 && fallUnit.Updates == unchecked(updateHost.CompletedUpdateCount - hostStartCount),
                    "Actual host, context and fall callback counts agree");
                if (cascade) Require(autoUnit.Updates == fallUnit.Updates, "Both native-ordered callbacks run once per host update");
                int lateUpdates = unchecked(updateHost.CompletedLateUpdateCount - lateStartCount);
                Require(lateUpdates > 0 && unchecked(board.Clock.FrameCount - clockStartCount) == lateUpdates,
                    "Native registered clock advances exactly once per actual host LateUpdate");
                Require(board.Clock.DeltaTime == OriginalLevelClock.QuantizeDelta(Time.deltaTime), "Native clock reads current Unity delta");
                report.host_updates = fallUnit.Updates;
                report.clock_late_updates = lateUpdates;
                report.random_context_id = fixture.RandomManager.Id;
                report.difficulty_seed = fixture.RandomManager.DifficultySource.DifficultySeed;
                report.random_user_bindings = fixture.RandomBindings;
                report.packed_state_longs = fixture.PackedStateLongs;
                report.restored_random_cursor = fixture.RestoredRandomCursor;
                report.restored_future_draws = fixture.RestoredFutureDraws;
                report.restored_left_moves = fixture.RestoredLeftMoves;
                report.shared_user_session = ReferenceEquals(fixture.UserSession.ActiveLevel, fixture.Session)
                    && ReferenceEquals(fixture.Session.AnimationValues, fixture.UserSession.AnimationData);
                report.session_pause_subscribers = fixture.PauseSubscribers;
                Require(report.shared_user_session && report.session_pause_subscribers == 1, "Core services use actual shared user-session owners");
                report.native_auto_before_fall = cascade;
                report.created_views = fixture.Created.Count; report.match_count = cascade ? 6 : 3; report.goal_left = 33;
                report.match_sounds = fixture.MatchSounds; report.haptic_requests = fixture.Haptics;
                File.WriteAllText(Path.Combine(Application.dataPath, cascade ? "../automatic_cascade_unity_verification.json" : "../swap_group_refill_unity_verification.json"), JsonUtility.ToJson(report, true) + "\n");
                Debug.Log(cascade ? "Automatic cascade Unity verification passed. Restart Play to restore fresh level." : "Swap/group/box-goal/refill Unity verification passed. Restart Play to restore fresh level.");
                Detach();
            }
            catch { Detach(); throw; }
        }
        private static void Detach()
        {
            EditorApplication.update -= Tick;
            if (updateHost != null) updateHost.ReleaseContext(frameContext);
            if (frameContext != null) frameContext.Clear();
            frameContext = null; updateHost = null; autoUnit = null; fallUnit = null;
            board = null; fixture = null; fall = null; auto = null; action = null; before = null; boxes = null;
        }
        private sealed class FrameUnit : IOriginalContextBehaviour
        {
            public int Id { get; }
            private readonly Action bind, update;
            public int LastFrame = -1, Updates;
            public Exception Failure;
            public FrameUnit(int id, Action bind, Action update) { Id = id; this.bind = bind; this.update = update; }
            public void Bind() => bind();
            public void ManualUpdate()
            {
                LastFrame = Time.frameCount;
                try { update(); Updates++; } catch (Exception error) { Failure = error; throw; }
            }
        }
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
        [Serializable] private sealed class Report
        {
            public bool passed = true;
            public int frames, box_events, match_count, created_views, goal_left, match_sounds, haptic_requests;
            public int auto_groups, auto_attempts, shuffle_draws;
            public int host_updates, clock_late_updates;
            public int random_context_id, difficulty_seed, random_user_bindings;
            public int packed_state_longs, restored_random_cursor, restored_future_draws, restored_left_moves;
            public bool shared_user_session;
            public int session_pause_subscribers;
            public bool native_auto_before_fall;
            public float max_position_error;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Actual formal first-level models: valid3,5to3,4 swap, coroutine tween/particles, saved-group resolution and native match-group orchestration, real match/static/neighbor/box model and configured box view particles/recycle, actual36to33 model/UI/display goal counts, active-level match statistics/useful counters and delayed King relief, automatic planner/fill/native pooled-view creation and frame motion for six holes. Explicit diagnostic command, seed371, state2 inactive-Madness session, normal-final-board/not-won/unworried contexts and animation constants are inputs. Haptic and propeller-cache-clear requests are traced boundaries; normal-item special dispatch is asserted only. Full input/application startup, move consumption, auto-match cascades, special combos, King analysis/UI, HUD, later lifecycle and original rendered parity remain pending. No production no-op providers installed. Destructive Play-only diagnostic is reset by restarting Play; scene/prefab assets are unchanged. SDK unchanged.";
        }
        private sealed class Fixture : IOriginalMatchGroupSystems, IOriginalMatchExplosionServices, IOriginalNormalSwapGameplay,
            IOriginalFallLevelStatus, IOriginalPredefinedPreparationContext, IOriginalInitialItemCreationContext,
            IOriginalFillSetGenerationContext, IOriginalFillingPlacementSpace, IOriginalFillingItemCreation,
            IOriginalViewCollectionLevelContext, IOriginalKingReliefServices, IOriginalThreeColorLevelNames,
            IOriginalMadnessSessionManager, IOriginalPropellerRushProgress, IOriginalAutoExplodeMatchServices
        {
            private readonly OriginalInitialBoardView board;
            private readonly OriginalGameAudioHost audio;
            public readonly OriginalActiveLevelData Session;
            public readonly OriginalUserSessionData UserSession;
            private readonly SessionEnvironment sessionEnvironment;
            public int PauseSubscribers => sessionEnvironment.Subscribers;
            public readonly OriginalKingRelief King;
            public readonly OriginalItemUsefulMoveServices UsefulMoves;
            public readonly OriginalBoardMatchItemExplosionServices MatchItems;
            private readonly OriginalMatchGroupExplosion groupExplosion;
            private readonly OriginalBoardAutoExplodeMatchServices autoServices;
            public OriginalSwapMatchResolution Resolution;
            public int Groups, Haptics, MatchSounds, SpecialChecks, ClearRequests, GoalUpdates, UiUpdates;
            public int AutoGroups, AutoAttempts, Shuffles, ShuffleDraws;
            public readonly List<OriginalBoardItem> Created = new List<OriginalBoardItem>();
            public IOriginalFillingItemCreation Creation;
            private OriginalLevelRandomSource random;
            public OriginalLevelRandomManager RandomManager { get; private set; }
            private RandomLifecycle randomLifecycle;
            public int RandomBindings => randomLifecycle.UserBindings;
            public int PackedStateLongs, RestoredRandomCursor, RestoredFutureDraws, RestoredLeftMoves;
            private OriginalInitialItemCreation initial;
            private OriginalMatchTileColors colors;
            private OriginalLevelSetSelection[] sets;
            private int[] firstIds;
            public OriginalFillSetGeneration Generation;
            public OriginalPredefinedItemContainer Predefined;
            public Fixture(OriginalInitialBoardView board, OriginalGameAudioHost audio)
            {
                this.board = board; this.audio = audio;
                sessionEnvironment = new SessionEnvironment(this);
                UserSession = new OriginalUserSessionData(new AnimationContext(this), new OriginalSlbMoveConfig(
                    () => throw new InvalidOperationException(), () => throw new InvalidOperationException()), this, sessionEnvironment);
                Session = UserSession.ActiveLevel; Session.State = 2; Session.Number = 1;
                ((OriginalMadnessSessionData)UserSession.AnimationData.Madness).Init(false, 0);
                var check = new OriginalKingBoardCheckData { MatchItemCounts = new int[7],
                    ItemTypesForTotalLayer = new Dictionary<int, HashSet<int>>(), MatchTypesForLightball = new HashSet<int>(), CheckedComboTypes = new HashSet<int>() };
                King = new OriginalKingRelief(check, board.Presentation.Coroutines, this);
                var useful = new OriginalUsefulMoveState(() => King, Session); useful.ResetIsUsefulMoveMade();
                UsefulMoves = new OriginalItemUsefulMoveServices(() => useful);
                MatchItems = new OriginalBoardMatchItemExplosionServices(board, () => Session.MatchItemCounts, UsefulMoves, null);
                groupExplosion = new OriginalMatchGroupExplosion(board.MatchReadiness,
                    new OriginalBoardMatchGroupExplosionServices(board, null, () => Session, this));
                autoServices = new OriginalBoardAutoExplodeMatchServices(board.Matches, groupExplosion);
                board.Goals.OnGoalUpdated += (type, count) => { Require(type == 11, "Actual box goal"); GoalUpdates++; };
                board.Goals.OnGoalUpdatedUi += (type, count, a, b, c) => { Require(type == 11, "Actual box UI goal"); UiUpdates++; };
            }
            private sealed class SessionEnvironment : IOriginalUserSessionEnvironment
            {
                private readonly Fixture owner;
                private Action pause;
                public int Subscribers;
                public SessionEnvironment(Fixture owner) { this.owner = owner; }
                public event Action OnApplicationPause { add { pause += value; Subscribers++; } remove { pause -= value; Subscribers--; } }
                public OriginalActiveLevelData GetCurrentActiveLevel() => owner.Session;
                public int GetCurrentScene() => throw new InvalidOperationException("Application scene transport is not installed in this diagnostic");
            }
            private sealed class AnimationContext : IOriginalAnimationDataContext
            {
                private readonly Fixture owner;
                public AnimationContext(Fixture owner) { this.owner = owner; }
                public IOriginalMadnessSessionManager GetMadness() => owner;
                public IOriginalPropellerRushProgress GetPropellerRush() => owner;
                public OriginalActiveLevelData GetActiveLevel() => owner.Session;
                public bool IsTableGameLevel() => false;
                // Full construction resolves none of these services. An unexpected
                // feature activation must fail, never install a production no-op.
                public IOriginalMadnessSessionPools GetMadnessPools() => throw new InvalidOperationException();
                public IOriginalRoyalPassSessionManager GetRoyalPass() => throw new InvalidOperationException();
                public IOriginalPinataSessionManager GetPinataParty() => throw new InvalidOperationException();
                public IOriginalSessionAbValues GetAbValues() => throw new InvalidOperationException();
                public IOriginalDailyLightningRushSessionManager GetDailyLightningRush() => throw new InvalidOperationException();
                public IOriginalDailyLightningRushPools GetDailyLightningRushPools() => throw new InvalidOperationException();
                public IOriginalSessionBundleManager GetBundleManager() => throw new InvalidOperationException();
                public IOriginalClashSessionManager GetClashOfChampions() => throw new InvalidOperationException();
                public IOriginalClashSessionPools GetClashPools() => throw new InvalidOperationException();
                public IOriginalClashGameplayAsset LoadClashAsset(IOriginalSessionEventBundle bundle, string assetName) => throw new InvalidOperationException();
                public IOriginalLevelCollectState GetWeeklyPassCollectState() => throw new InvalidOperationException();
                public IOriginalLevelCollectState GetMagicCauldronCollectState() => throw new InvalidOperationException();
                public IOriginalLevelCollectState GetDukesFortuneCollectState() => throw new InvalidOperationException();
                public IOriginalLevelCollectState GetOceanOdysseyCollectState() => throw new InvalidOperationException();
                public IOriginalLevelCollectState GetPuzzleBreakCollectState() => throw new InvalidOperationException();
                public IOriginalLevelCollectState GetMergeSmithCollectState() => throw new InvalidOperationException();
                public IOriginalLevelCollectState GetShortExpeditionCollectState() => throw new InvalidOperationException();
                public IOriginalMissionSuperBooster GetMissionSuperBooster() => throw new InvalidOperationException();
                public int GetActiveAmountMultiplier() => owner.Session.EventMultiplier;
            }
            public void PrepareFilling()
            {
                var level = OriginalLevelReader.Load("Levels/level_1"); var parsed = OriginalParsedLevelSet.ParseSets(level, 0, 0);
                randomLifecycle = new RandomLifecycle(this, audio);
                RandomManager = new OriginalLevelRandomManager(randomLifecycle);
                RandomManager.ResetSeed(); // Native activation precedes BindAll.
                random = RandomManager.DifficultySource;
                Require(random.DifficultySeed == 371 && random.DifficultySeedGenerationCount == 0, "Native attempt bank selects diagnostic seed371");
                VerifyPackedStateRestore(level.Move);
                firstIds = parsed[0].ElementIds;
                sets = new[] { new OriginalLevelSetSelection(parsed[0], random) }; colors = new OriginalMatchTileColors(random, 0, 0);
                Generation = new OriginalFillSetGeneration(random, new OriginalTargetColumnSetContainer(), this); Generation.AddAvailableSets(sets);
                Predefined = new OriginalPredefinedItemContainer(); initial = new OriginalInitialItemCreation(sets, random, Predefined, this);
                OriginalPredefinedPreparation.Parse(new OriginalDecodedPredefinedData(level), sets, 0, 0, this);
            }
            private void VerifyPackedStateRestore(int maxMoves)
            {
                Session.MoveCount = maxMoves;
                var snapshot = new SaveSnapshot(this);
                // Explicit diagnostic move state, not a production active-session override.
                snapshot.Moves.SetMovesForStart(maxMoves, maxMoves - 4, 4);
                var saved = new OriginalGameStateSaveData();
                long[] initial = saved.PrepareStateSaveData(snapshot);
                for (int i = 0; i < 5; i++) RandomManager.DifficultyNext(0, 1000);
                long[] packed = saved.PrepareStateSaveData(snapshot);
                Require(packed.Length == 2, "Native86-bit packed state"); PackedStateLongs = packed.Length;
                saved.SetData(packed); Require(saved.IsDataReadable(), "Complete packed state is readable");
                Require(saved.GetDifficultySeed() == 371 && saved.GetDifficultySeedGenerationCount() == 5,
                    "Seed and cross-long cursor read from native layout");
                Require(saved.GetEgoPurchaseCount() == Session.EgoPurchaseCount && saved.GetEgoPackageItemThrowInProgress() == 0
                    && !saved.HasShowedAnyDynamicTutorial() && !saved.FreeBoosterTutorialBoosterUsage()
                    && !saved.GetDynamicItemTutorialShown() && !saved.GetDynamicSuperDukeTutorialShown()
                    && !saved.GetDynamicMagicDukeTutorialShown() && !saved.GetDynamicMagicLanternItemTutorialShown()
                    && saved.GetAddedExtraMoveCount() == OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount,
                    "All remaining packed state fields read back");
                var moves = new OriginalMoveManager();
                moves.SetMovesForStart(saved.GetMaxMoves(), saved.GetLeftMoves(), saved.GetMadeMoves());
                Require(moves.LeftMoves == maxMoves - 4 && moves.MadeMoves == 4, "Actual move manager restores saved budget");
                RestoredLeftMoves = moves.LeftMoves;
                randomLifecycle.SavedState = saved;
                RandomManager.DifficultySource.SetDifficultySeed(999);
                RandomManager.RestoreFromSaveData(); RestoredRandomCursor = random.DifficultySeedGenerationCount;
                var expected = new OriginalLevelRandomSource(371);
                for (int i = 0; i < 5; i++) expected.DifficultyNext(0, 1000);
                for (int i = 0; i < 16; i++)
                {
                    Require(RandomManager.DifficultyNext(0, 1000) == expected.DifficultyNext(0, 1000), "Saved cursor retains future random sequence");
                    RestoredFutureDraws++;
                }
                saved.SetData(new long[1]); Require(!saved.IsDataReadable(), "Truncated packed state is rejected");
                saved.SetData(initial); RandomManager.RestoreFromSaveData();
                Require(random.DifficultySeed == 371 && random.DifficultySeedGenerationCount == 0, "Restore initial packed state before board command");
            }
            private sealed class SaveSnapshot : IOriginalGameStateSaveServices, IOriginalEgoSaveState,
                IOriginalNewUserTutorialSaveState, IOriginalLevelTutorialSaveState, IOriginalDynamicTutorialSaveState
            {
                private readonly Fixture owner;
                public readonly OriginalMoveManager Moves = new OriginalMoveManager();
                public SaveSnapshot(Fixture owner) { this.owner = owner; }
                public OriginalMoveManager GetMoveManager() => Moves;
                public IOriginalEgoSaveState GetEgoState() => this;
                public OriginalLevelRandomManager GetLevelRandomManager() => owner.RandomManager;
                public IOriginalNewUserTutorialSaveState GetNewUserTutorialState() => this;
                public IOriginalLevelTutorialSaveState GetLevelTutorialState() => this;
                public IOriginalDynamicTutorialSaveState GetDynamicTutorialState() => this;
                public OriginalActiveLevelData GetCurrentActiveLevel() => owner.Session;
                // These inactive states are explicit diagnostic inputs until the actual contexts are composed.
                public bool ItemThrowInProgress => false;
                public bool HasShownAnyDynamicTutorial() => false;
                public bool FreeBoosterTutorialBoosterUsage => false;
                public bool ItemShown => false;
                public bool SuperDukeShown => false;
                public bool MagicDukeShown => false;
                public bool MagicLanternShown => false;
            }
            public void PrepareCascadeItems()
            {
                Predefined.Clear();
                int[][] ids = { new[] { 3, 1, 3 }, new[] { 2, 1, 2 }, new[] { 3, 1, 4 } };
                for (int x = 1; x <= 3; x++) for (int i = 0; i < ids[x - 1].Length; i++)
                    Predefined.AddPredefinedItem(x, CreateItem(OriginalItemSettings.ForTiledId(ids[x - 1][i])));
            }
            private sealed class RandomLifecycle : IOriginalLevelRandomLifecycleServices, IOriginalRandomUserEvents
            {
                private readonly Fixture owner;
                private readonly OriginalGameAudioHost audio;
                private Action userChanged;
                public int UserBindings { get; private set; }
                public OriginalGameStateSaveData SavedState;
                public RandomLifecycle(Fixture owner, OriginalGameAudioHost audio) { this.owner = owner; this.audio = audio; }
                public IOriginalLevelSeedRandom GetApplicationRandom() => audio.Random;
                public void RequireCurrentUserSession() { Require(owner.Session != null, "Actual diagnostic session"); }
                public int GetLevelNumber() => owner.Session.Number;
                public long GetUserId() => 1;
                public int GetDeviceIdKey() => OriginalLevelRandomManager.ParseDeviceIdKey("fixture-12345");
                // Native bank for level1/user1/device12345 has seed371 at362.
                public int GetAttemptIndex() => 362;
                public bool IsCurrentLevelWon => owner.Session.State == 3;
                public IOriginalRandomUserEvents GetUserEvents() => this;
                public IOriginalSavedLevelRandom GetSavedRandomData() => SavedState ?? throw new InvalidOperationException("No diagnostic packed state loaded");
                public event Action OnUserChanged
                {
                    add { userChanged += value; UserBindings++; }
                    remove { var before = userChanged; userChanged -= value; if (!ReferenceEquals(before, userChanged)) UserBindings--; }
                }
            }
            public OriginalBoardItem CreateItemForFillingCellAt(float x, float y, OriginalFillingCell cell)
            {
                var item = Creation.CreateItemForFillingCellAt(x, y, cell); Require(item != null && item.HasView, "Actual refill view");
                ((OriginalMatchBoardItem)item).BindExplosionServices(MatchItems); Created.Add(item); return item;
            }
            public IReadOnlyList<OriginalResolvedMatchGroup> FindAllMatches() => autoServices.FindAllMatches();
            public bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group, OriginalExplodeData data, bool automatic)
            {
                if (automatic)
                {
                    AutoAttempts++; Require(data.Trigger == 4, "Automatic native trigger");
                    bool result = autoServices.ExplodeMatchGroup(group, data, true);
                    if (result) { Require(group.PatternType == 1 && group.MatchType == 1 && group.Cells.Count == 3, "Actual automatic blue triple"); AutoGroups++; }
                    return result;
                }
                Require(group.PatternType == 1 && group.MatchType == 2 && group.Cells.Count == 3 && data.Trigger == 2, "Actual normal triple");
                Groups++; return groupExplosion.ExplodeMatchGroup(group, data, false);
            }
            public void ReportPatternChanged(int a, int b) => throw new InvalidOperationException("Unexpected pattern change");
            public void ReportMatchExplosionFailed() => throw new InvalidOperationException("Group rejected");
            public bool IsAutoMatchDisabledForWaterLevel => false;
            public void ShuffleCellsBasedOnDifficulty(OriginalMatchCells cells)
            {
                Require(cascade, "Saved swap creation cell must be retained");
                int previous = random.DifficultySeedGenerationCount;
                random.ShuffleCellsBasedOnDifficulty(cells); ShuffleDraws += unchecked(random.DifficultySeedGenerationCount - previous); Shuffles++;
            }
            public void CreateSpecialItemForMatchGroup(OriginalMatchGroupSnapshot group, OriginalGridCell cell, bool automatic) => throw new InvalidOperationException("Normal triple creates no special");
            public void PlayMatchExplode(bool swap) { Require(swap || cascade, "Swap or actual automatic explode sound"); audio.GameSfx.PlayMatchExplode(swap); Require(audio.SoundSource.isPlaying, "Real original one-shot source"); MatchSounds++; }
            public void PlayHaptic(int id, int type, float strength, float duration, bool replay)
            { Require(id == 25 && type == 1 && strength == 1f && duration == 0f && !replay, "Native group haptic request"); Haptics++; }
            public void ClearSinglePropellerWinCalculationData() { ClearRequests++; }
            public void PlaySfx(int id, float delay) { Require(id == 254 && delay == .04f, "Native swipe feedback"); audio.GameSfx.PlaySfx(id, delay); }
            public void ExplodeMatchGroups(OriginalSwapDecision decision, OriginalBoardItem other, OriginalBoardItem moving) => Resolution.Resolve(other, moving);
            public void ExplodeSpecialItems(OriginalBoardItem other, OriginalBoardItem moving)
            { Require(other is OriginalMatchBoardItem && moving is OriginalMatchBoardItem, "Explicit normal-item special boundary"); SpecialChecks++; }
            public bool IsPortalLevel => false;
            public bool IsMultipleBoardLevel => false;
            public bool IsLevelWon => false;
            public bool IsStoryLevel => false;
            public bool IsFinalBoard => true;
            public bool IsWaterLevel => false;
            public int[][] PotionColors => null;
            public bool ContainsDrillMasterPoint(OriginalGridCell cell) => throw new InvalidOperationException();
            public OriginalBoardItem CreateItem(OriginalItemSettings settings) => ((IOriginalItemViewCreationContext)board).CreateItem(settings);
            public void OnNewItemCreated(int type) => Generation.OnNewItemCreated(type);
            public OriginalLevelRandomSource GetLevelRandom() => random;
            public int SelectMatchTileColor(int id, OriginalLevelSetSelection first, OriginalLevelRandomSource source) => colors.SelectMatchTileColor(id, firstIds);
            public IOriginalPredefinedItemCreator ItemCreator => initial;
            public bool IsTuningEnabled => false;
            public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell) => throw new InvalidOperationException();
            public int GetLimitCount(int goal) => throw new InvalidOperationException();
            public int GetGoalTargetCount(int goal) => throw new InvalidOperationException();
            public void ReportRandomColorFallback(int total) => throw new InvalidOperationException();
            public float OffsetX => throw new InvalidOperationException();
            public float OffsetY => throw new InvalidOperationException();
            public void GetCellViewPosition(OriginalFillingCell cell, out float x, out float y) => throw new InvalidOperationException();
            public bool IsWorried => false;
            public int LeftMoves => throw new InvalidOperationException();
            public int LastMoveLayerCapsCount => throw new InvalidOperationException();
            public bool IsWinPossible() => throw new InvalidOperationException();
            public bool IsPlaying() => throw new InvalidOperationException();
            public bool ShouldKingBeWorried() => throw new InvalidOperationException();
            public void TryStopWorry(int reason) => throw new InvalidOperationException();
            public bool ShouldReportThreeColorExtension(int level, int league) => throw new InvalidOperationException();
            public bool TryGetVersionOverride(int level, out int version) => throw new InvalidOperationException();
            public string GetLevelExtension() => throw new InvalidOperationException();
            public int EventId => throw new InvalidOperationException();
            public int EventType => throw new InvalidOperationException();
            public bool ShouldShowIcon() => false;
            public int GetCount() => throw new InvalidOperationException();
            public bool ShouldProgress => false;
        }
    }
}
