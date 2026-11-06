using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // Live scene composition for the recovered, static-free first board. This
    // owns no Editor callbacks or scripted test moves. Account/event startup
    // and the remaining special-item implementations are separate work.
    public sealed partial class OriginalPlayableBoard : IOriginalGameTouchServices, IOriginalMoveTouchEvents,
        IOriginalLevelSwapServices, IOriginalLevelTapServices, IOriginalNormalSwapGameplay,
        IOriginalImpossibleSwapGameplay, IOriginalMatchExplosionServices, IOriginalMatchGroupSystems,
        IOriginalFallLevelStatus, IOriginalPredefinedPreparationContext, IOriginalInitialItemCreationContext,
        IOriginalFillSetGenerationContext, IOriginalFillingPlacementSpace, IOriginalFillingItemCreation,
        IOriginalViewCollectionLevelContext, IOriginalProgressLevelContext, IOriginalGameStateServices,
        IOriginalThreeColorLevelNames, IOriginalMadnessSessionManager, IOriginalPropellerRushProgress,
        IOriginalActiveLevelResetContext, IOriginalSessionProgress, IOriginalSlbAbValues,
        IOriginalSpecialItemCreationServices, IOriginalRocketViewServices, IOriginalTntViewServices,
        IOriginalRocketItemStaticState, IOriginalMissionSuperBooster, IOriginalTntExplosionServices,
        IOriginalMatchItemExplosionSystems, IOriginalLightballExplosionServices, IOriginalGameTouchListenerOwner, IOriginalLightballViewServices
    {
        private readonly OriginalInitialBoardView board;
        private readonly OriginalGameAudioHost audio;
        private readonly OriginalContextUpdateHost host;
        private readonly Func<bool> uiTouching;
        private readonly OriginalLevelPressRouter presses;
        private readonly OriginalLevelTapRouter taps;
        private readonly OriginalLevelSwapRouter swaps;
        private readonly OriginalImpossibleSwapPresentation impossible;
        private readonly OriginalSwapMatchResolution firstResolution, secondResolution;
        private readonly OriginalMatchGroupExplosion explosion;
        private readonly OriginalUsefulMoveState useful;
        private readonly OriginalItemUsefulMoveServices usefulItems;
        private readonly OriginalLevelRandomSource random;
        private readonly OriginalGeneralLevelRandom generalRandom;
        internal OriginalGeneralLevelRandom GeneralRandom => generalRandom;
        internal OriginalCoroutinePlayer GameCoroutines => board.Presentation.Coroutines;
        public OriginalGameTouchListener TouchListener { get; private set; }
        public void BindTouchListener(OriginalGameTouchListener listener) { TouchListener = listener; }
        private readonly OriginalMatchTileColors colors;
        private readonly int[] firstIds;
        private readonly OriginalInitialItemCreation initial;
        private readonly OriginalPredefinedItemContainer predefined;
        private readonly OriginalFillSetGeneration generation;
        private readonly OriginalLowMatchTuning lowMatch;
        private readonly FoundationSettings settings;
        private OriginalWinFailManager winFail;
        private readonly OriginalRuntimeShuffle shuffle;
        private OriginalGridCell tutorialTapCell;
        public bool HasOnlyLightballMove => shuffle.HasOnlyLightballMove;
        public OriginalWinFailManager WinFail => winFail;
        public int LowMatchTotalRatio => lowMatch.TotalRatio;
        public bool IsLowMatchEnabled => lowMatch.IsEnabled;
        public int[] CreateWinLowMatchRatios() => settings.CreateWinLowMatchRatios();
        public float WinLogoBeforeShowDelay => settings.WinLogoBeforeShowDelay;
        public float WinLogoBeforePlayDelay => settings.WinLogoBeforePlayDelay;
        public void UpdateLowMatchRatios(int[] ratios) => lowMatch.UpdateLowMatchRatios(ratios);
        public void RemainingItemCreationStarted() => generation.RemainingMovesStarted();
        public OriginalWinFailManager BindWinFail(IOriginalWinFailServices services)
        {
            if (winFail != null) throw new InvalidOperationException("WinFail is already bound.");
            winFail = new OriginalWinFailManager(State, board.Goals, Moves, Auto, services);
            // Native permanent order is Auto7, Fall8, WinFail16, Shuffle6.
            context.Add(new FrameUnit(16, winFail.Bind, winFail.ManualUpdate));
            winFail.Bind();
            context.Add(shuffle);
            return winFail;
        }
        public void ReleaseWinFail()
        { if (winFail == null) return; winFail.Release(); context.Remove(16); context.Remove(6); winFail = null; }
        internal void BindLevelFlow(CoreGameFlow flow, CoreLevelAttemptStore store)
        {
            // LevelContext registers Hint17, DynamicTutorial51 and Save74 after Shuffle6.
            context.Add(new FrameUnit(17, null, flow.UpdateMatchHints));
            context.Add(new FrameUnit(51, null, flow.UpdateDynamicTutorials));
            if (store != null) context.Add(new FrameUnit(74, null, () => store.Tick(this, flow)));
        }
        internal void ReleaseLevelFlow()
        {
            context.Remove(17);
            context.Remove(51);
            context.Remove(74);
        }
        private readonly OriginalFillingItemFactory creation;
        private readonly OriginalItemViewCreation itemViews;
        private readonly OriginalContextContainer context;
        private readonly OriginalKingBoardReaction king;
        private readonly OriginalLevelStatisticsSaveData statistics = new OriginalLevelStatisticsSaveData();
        internal void BindKingVisual(OriginalKingVisual visual)
        {
            // The captured ordinary game has KingRobertAnimationEnabled=1.
            if (IsStoryLevel || Session.IsBonus) return;
            king.Bind(visual);
            GuidedTargets.SingleTargetSelected += king.OnSinglePropellerTarget;
            AreaTargets.WinningComboSelected += king.OnWinningPropellerCombo;
            LineTargets.WinningComboSelected += king.OnWinningPropellerCombo;
        }
        private readonly OriginalSpecialItemCreator specialCreator;
        private readonly OriginalTntTntCombo tntTntCombo;
        private readonly OriginalRocketRocketCombo rocketRocketCombo = new OriginalRocketRocketCombo();
        private readonly OriginalTntRocketCombo tntRocketCombo;
        public OriginalTntRocketCombo TntRocketCombo => tntRocketCombo;
        private OriginalGoalPanelUi goalPanel;
        public OriginalGoalPanelUi GoalPanel => goalPanel;
        public void BindGoalPanel(OriginalGoalPanelUi panel, Action<bool> orientation)
        { goalPanel = panel; setCoinOrientation = orientation; }
        public OriginalTntTntCombo TntTntCombo => tntTntCombo;
        public OriginalBallBallCombo BallBallCombo { get; }
        private readonly FTiledLevel level;
        private readonly OriginalLevelStartInventory inventory;
        private readonly OriginalSessionPlayerPreferences preferences = new OriginalSessionPlayerPreferences();
        private Action<int> completionHandlers;
        internal event Action BoosterTweenUpdate;
        public OriginalActiveLevelData Session { get; }
        public OriginalGeneratedItemCountState InitialCounts { get; }
        private IOriginalSavedCreatedItemCounts savedCreatedItemCounts;
        public void BindSavedCreatedItemCounts(IOriginalSavedCreatedItemCounts value)
        { savedCreatedItemCounts = value ?? throw new ArgumentNullException(nameof(value)); }
        public OriginalFallManager Fall { get; }
        public OriginalAutoExplodeManager Auto { get; }
        public OriginalSingleExplodeTargetFinder GuidedTargets { get; }
        public OriginalAreaExplodeTargetFinder AreaTargets { get; }
        public OriginalLineExplodeTargetFinder LineTargets { get; }
        private readonly OriginalFirstBoardExplodeTargets guidedTargetServices;
        public OriginalMoveManager Moves => board.Moves;
        public int AcceptedSwaps { get; private set; }
        public int RejectedSwaps { get; private set; }
        public int CreatedRefills { get; private set; }
        public int CreatedSpecials { get; private set; }
        public event Action<OriginalSpecialBoardItem> OnSpecialCreated;
        public event Action OnSpecialTapped, OnTntOrRocketTapped;
        internal event Action OnSpecialComboUsed;
        public OriginalGameStateManager State => board.State;
        public OriginalCellGrid Grid => board.Grid;
        bool IOriginalSpecialItemExplosionServices.IsPlaying => board.State.IsPlaying();
        public float StartedAt { get; private set; }
        public event Action<OriginalGridCell> OnTap { add { taps.OnTap += value; } remove { taps.OnTap -= value; } }
        public event Action<OriginalGridCell, OriginalGridCell, bool> OnSwap { add { swaps.OnSwap += value; } remove { swaps.OnSwap -= value; } }
        public event Action<int> OnMoveCompleted
        {
            add { taps.OnMoveCompleted += value; board.OnSwapEnded += value; completionHandlers += value; }
            remove { taps.OnMoveCompleted -= value; board.OnSwapEnded -= value; completionHandlers -= value; }
        }

        public OriginalPlayableBoard(OriginalInitialBoardView board, OriginalGameAudioHost audio,
            OriginalContextUpdateHost host, FTiledLevel level, FoundationSettings settings, Func<bool> uiTouching, Camera gameCamera,
            OriginalGameSceneProjection gameSceneProjection, OriginalLevelStartInventory levelInventory, int levelNumber, FLevelSaveData restored = null)
        {
            inventory=levelInventory??throw new ArgumentNullException(nameof(levelInventory));
            this.board = board; this.audio = audio; this.host = host; this.level = level; this.settings = settings;
            tntTntCombo = new OriginalTntTntCombo(board.Presentation, board.State);
            tntRocketCombo = new OriginalTntRocketCombo(this);
            BallBallCombo = new OriginalBallBallCombo(new OriginalBottomLeftVerticalStrategy(new OriginalBoardGridAccess(board.Grid,board.Layout)),this);
            this.gameCamera = gameCamera == null ? throw new ArgumentNullException(nameof(gameCamera)) : gameCamera;
            this.gameSceneProjection = gameSceneProjection == null ? throw new ArgumentNullException(nameof(gameSceneProjection)) : gameSceneProjection;
            this.uiTouching = uiTouching ?? throw new ArgumentNullException(nameof(uiTouching));
            if (level.Name != settings.CoreLevels.GetEntry(levelNumber).originalName)
                throw new InvalidOperationException("Playable board identity does not match its scoped catalog entry.");
            // Continue the configured local difficulty stream after initial population.
            random = board.DifficultyRandom;
            generalRandom = new OriginalGeneralLevelRandom(audio.Random);
            guidedTargetServices = new OriginalFirstBoardExplodeTargets(board.Goals, board.GoalClaims, () => board.Clock.FrameCount);
            GuidedTargets = new OriginalSingleExplodeTargetFinder(
                new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)),
                generalRandom, guidedTargetServices);
            var madness = new OriginalMadnessSessionData(() => this, () => this,
                () => throw new NotSupportedException("Madness pools are not installed."), () => false);
            var animation = new OriginalActiveLevelAnimationValues(new OriginalBeforeAfterData(), new OriginalBeforeAfterData(),
                new OriginalLevelStartSnapshots { Madness = madness, MissionEvents = new OriginalMissionEventsStartData(() => this) });
            var slb = new OriginalSlbMoveConfig(() => this, () => null);
            Session = new OriginalActiveLevelData(slb, this, animation);
            Session.Load(levelNumber, level.Name, level.Move);
            Session.IsBonus = board.GoalPreparation.IsBonusLevel;
            Session.ResetValues(this);
            Session.LevelStartTime = Time.realtimeSinceStartup;
            guidedTargetServices.BindAreaContext(board.Grid, () => Session.IsBonus);
            guidedTargetServices.BindLineContext(board.Layout);
            AreaTargets = new OriginalAreaExplodeTargetFinder(
                new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)),
                generalRandom, guidedTargetServices);
            LineTargets=new OriginalLineExplodeTargetFinder(guidedTargetServices,generalRandom);
            madness.Init(false, Session.Difficulty);

            useful = new OriginalUsefulMoveState(() => king, Session);
            usefulItems = new OriginalItemUsefulMoveServices(() => useful);
            var matchItems = new OriginalBoardMatchItemExplosionServices(board, () => Session.MatchItemCounts, usefulItems, this);
            board.BindPlayableItemServices(this, matchItems, guidedTargetServices);
            explosion = new OriginalMatchGroupExplosion(board.MatchReadiness,
                new OriginalBoardMatchGroupExplosionServices(board, null, () => Session, this));
            specialCreator = board.Presentation.CreateSpecialItemCreator(this);
            board.CreateGrassModels(level.Grid, usefulItems);
            board.ReleaseRestoreGrid();
            board.BindGoalProgress(new OriginalGameUiProgress(this, null, null));
            board.BindGameStateServices(this);

            var parsed = board.ParsedSets;
            var sets = new OriginalLevelSetSelection[parsed.Length];
            var targetColumns = new OriginalTargetColumnSetContainer();
            for (int i = 0; i < sets.Length; i++)
            {
                sets[i] = new OriginalLevelSetSelection(parsed[i], random);
                targetColumns.AddConfiguredSet(sets[i]);
            }
            firstIds = parsed[0].ElementIds;
            colors = board.TileColors;
            generation = new OriginalFillSetGeneration(random, targetColumns, this);
            generation.AddAvailableSets(sets);
            lowMatch = new OriginalLowMatchTuning(random);
            lowMatch.UpdateLowMatchRatios(level.LowMatch);
            predefined = new OriginalPredefinedItemContainer();
            king = new OriginalKingBoardReaction(board, predefined, generation, guidedTargetServices);
            initial = new OriginalInitialItemCreation(sets, random, predefined, this);
            if (restored == null)
            {
                OriginalPredefinedPreparation.Parse(new OriginalDecodedPredefinedData(level), sets, 0, 0, this);
                // CreateItemModelForStart 0x23ebf0c reports every initial item
                // to ItemCreator before the first refill can use the sets.
                for (int x = 0; x < Grid.Width; x++)
                for (int y = 0; y < Grid.Height; y++)
                {
                    var item = Grid.GetCell(x, y).Mediation.CurrentItem;
                    if (item != null) generation.OnNewItemCreated(item.ItemType);
                }
            }
            else
            {
                var creator = restored.BoardSaveData.ItemModelCreatorSaveData;
                predefined.Restore(creator.PredefinedItemsSaveData, CreateItem);
                if (restored.ValidationSaveData.AppVersion == CoreLevelAttemptStore.SaveVersionWithoutOpeningRefillCounts)
                    AddLegacyOpeningRefillCounts(level, creator);
                BindSavedCreatedItemCounts(new OriginalDecodedCreatedItemCounts(() => restored));
            }
            InitialCounts = generation.CreateInitialCountState(board.Goals, predefined, () => savedCreatedItemCounts);
            if (restored != null)
            {
                InitialCounts.UpdateCreatedItemsCountExceptPredefined();
                savedCreatedItemCounts = null;
            }
            var columns = new OriginalFallRefillColumns();
            itemViews = new OriginalItemViewCreation(board);
            creation = new OriginalFillingItemFactory(columns,
                new OriginalFillingCreationBackend(predefined, generation, itemViews), this);
            var fallContext = new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, this, null);
            Fall = new OriginalFallManager(columns, new OriginalFallTargetPlanner(fallContext, columns), this, board.GameOperations);
            Auto = new OriginalAutoExplodeManager(() => board.Clock.FrameCount);
            var autoServices = new OriginalBoardAutoExplodeMatchServices(board.Matches, explosion);
            // The original permanent registration dispatches auto7 before fall8.
            context = new OriginalContextContainer(OriginalLevelUnitRegistration.UnitCount);
            context.Add(new FrameUnit(7, () => Auto.Bind(board.State, autoServices), Auto.ManualUpdate));
            context.Add(new FrameUnit(8, () => Fall.InitializeGrid(fallContext), Fall.ManualUpdate));
            context.BindAll();
            shuffle = new OriginalRuntimeShuffle(board, this);

            presses = new OriginalLevelPressRouter(this);
            taps = new OriginalLevelTapRouter(this);
            firstResolution = new OriginalSwapMatchResolution(board.FirstSwapAction, this);
            secondResolution = new OriginalSwapMatchResolution(board.SecondSwapAction, this);
            var presentation = new OriginalNormalSwapPresentation(board.BoardRoot, board.Presentation, board.LocalMoves, this, SortingLayer.NameToID("Item"));
            impossible = new OriginalImpossibleSwapPresentation(board.LocalMoves, this, SortingLayer.NameToID("Item"));
            swaps = new OriginalLevelSwapRouter(board.Grid, board.FirstSwapAction, board.SecondSwapAction,
                new OriginalNormalSwapAnimationBackend(board.FirstSwapAction, presentation, settings.SwapDuration, settings.SwapSoundDelay),
                new OriginalNormalSwapAnimationBackend(board.SecondSwapAction, presentation, settings.SwapDuration, settings.SwapSoundDelay), board.SwapAvailability, this);
            Moves.Bind(board.State, this);
            if (restored == null)
            {
                var fresh = new OriginalFreshLevelMoves(() => Session);
                Moves.SetMovesForStart(fresh.GetMaxMoves(), fresh.GetLeftMoves(), fresh.GetMadeMoves());
            }
            else
            {
                var savedState = new OriginalGameStateSaveData();
                savedState.SetData(restored.GameStateSaveData);
                Session.UpdateMoveCount(savedState.GetMaxMoves());
                Moves.SetMovesForStart(savedState.GetMaxMoves(), savedState.GetLeftMoves(), savedState.GetMadeMoves());
                random.SetDifficultySeed(savedState.GetDifficultySeed());
                random.RestoreDifficultyGenerationCount(savedState.GetDifficultySeedGenerationCount());
                statistics.Restore(restored.LevelStatisticsSaveData, Session, king);
            }
            swaps.OnSwap += CountSwap;
        }

        internal void InitializeViews()
        {
            board.InitializeCellAndMatchViews();
            var collection = new OriginalItemViewGoalCollection(board.GoalClaims, this,
                () => throw new NotSupportedException("This level has one board."), null);
            var boxes = new OriginalBoardBoxViewServices(board.Presentation, audio, collection, SortingLayer.NameToID("Item"));
            if (Session.IsBonus) PrepareBoardCoinCollection();
            PrepareMailBoxes();
            board.Presentation.PrepareVasePools(board.InitialItemAmounts.GetItemAmount(7));
            board.Presentation.PrepareEggPools(board.InitialItemAmounts.GetItemAmount(9));
            board.Presentation.PrepareOwlStatuePools(board.InitialItemAmounts.GetItemAmount(10));
            board.Presentation.PreparePotionPools(board.InitialItemAmounts.GetItemAmount(12));
            board.Presentation.PrepareBushPools(board.InitialItemAmounts.GetItemAmount(13));
            board.Presentation.PrepareSafePools(board.InitialItemAmounts.GetItemAmount(14));
            board.BindPlayableItems(boxes, usefulItems);
            var cupboardItems = board.CreateCupboards(audio, generalRandom, usefulItems);
            board.BindGrassViews(audio, generalRandom);
            board.BindHoneyViews(audio, generalRandom, usefulItems);
            // The original CellGridManager notifies the registry after all board views exist.
            cupboardItems?.SortByCellDistance();
            host.BindContext(context, TickBoardAnimations);
        }

        internal long[] CaptureStatistics() => statistics.Capture(Session, king);

        private void AddLegacyOpeningRefillCounts(FTiledLevel level, FItemModelCreatorSaveData creator)
        {
            var counts = new Dictionary<int, FIntIntPair>();
            foreach (var pair in creator.CreatedSetItemCountExceptMatchItem ?? Array.Empty<FIntIntPair>())
                counts.Add(pair.Key, pair);

            // In the scoped catalog, opening refill obstacles are literal IDs.
            foreach (int tiledId in level.Grid.Items)
            {
                int type = OriginalTiledItemTypes.AsItemType(tiledId);
                if (!OriginalFillSetGeneration.IsFillingObstacle(type)) continue;
                if (!counts.TryGetValue(type, out var pair))
                {
                    pair = new FIntIntPair { Key = type };
                    counts.Add(type, pair);
                }
                int goal = OriginalGoalTypes.FromItemType(type);
                int target = GetGoalTargetCount(goal), limit = GetLimitCount(goal);
                int cap = Math.Min(target > 0 ? target : int.MaxValue, limit > 0 ? limit : int.MaxValue);
                // Older attempts can already contain excess obstacles. Keep
                // their board and restore the exhausted quota at its cap.
                pair.Value = (int)Math.Min((long)pair.Value + 1, cap);
            }
            var result = new FIntIntPair[counts.Count];
            counts.Values.CopyTo(result, 0);
            creator.CreatedSetItemCountExceptMatchItem = result;
        }

        internal FBoardSaveData CaptureBoard()
        {
            var counts = new FIntIntPair[generation.CreatedCounts.Count];
            int index = 0;
            foreach (var entry in generation.CreatedCounts)
            {
                if (entry.Key == 1) continue; // ItemCreator.GetSaveData 0x23e18d4 skips Match.
                counts[index++] = new FIntIntPair { Key = entry.Key, Value = entry.Value };
            }
            if (index != counts.Length) Array.Resize(ref counts, index);
            return new FBoardSaveData {
                GridData = board.CaptureCellGrid(), Goals = board.CaptureGoals(),
                ItemModelCreatorSaveData = new FItemModelCreatorSaveData {
                    CreatedSetItemCountExceptMatchItem = counts, PredefinedItemsSaveData = predefined.Capture()
                }
            };
        }

        private void CountSwap(OriginalGridCell from, OriginalGridCell to, bool valid)
        { if (valid) AcceptedSwaps++; else RejectedSwaps++; }
        public void Dispose()
        {
            boosterSession?.Dispose();
            boosterSession = null;
            GuidedTargets.SingleTargetSelected -= king.OnSinglePropellerTarget;
            AreaTargets.WinningComboSelected -= king.OnWinningPropellerCombo;
            LineTargets.WinningComboSelected -= king.OnWinningPropellerCombo;
            king.Release();
            ReleaseWinFail();
            ReleaseBoardCoinCollection();
            ReleaseMailBoxes();
            host.ReleaseContext(context);
            if (goalPanel != null) goalPanel.Release(); goalPanel = null;
            tntRocketAnimations.Clear();
            ballBallAnimations.Clear();
            context.Clear();
            GuidedTargets.Clear();
            board.OnSwapEnded -= completionHandlers;
            completionHandlers = null;
            Moves.Clear(true);
            swaps.OnSwap -= CountSwap;
            presses.CancelTouchedItemAnimation();
        }
        public bool IsUiTouching => uiTouching();
        private OriginalHammerTutorial hammerTutorial;
        private OriginalOptionalBoosterTutorial optionalBoosterTutorial;
        private RoyalMatchBoosterSession boosterSession;

        internal void BindBoosterInventory(IRoyalMatchBoosterInventory inventory, OriginalBottomUi bottom,
            OriginalGameplayHud hud, Camera camera, OriginalGameSceneProjection projection,
            CoreFlowSettings configuration, Func<bool> inputBlocked)
        {
            boosterSession = new RoyalMatchBoosterSession(inventory, this, board, bottom, hud, camera,
                projection, configuration, inputBlocked);
        }

        internal bool IsPaidBoosterInteractionActive => boosterSession != null && boosterSession.IsActive;
        internal void CancelPaidBoosterSelection() => boosterSession?.CancelSelection();

        internal bool HasActiveBoosterGuide =>
            (hammerTutorial != null && hammerTutorial.IsGuiding) ||
            (optionalBoosterTutorial != null && (optionalBoosterTutorial.IsGuiding || optionalBoosterTutorial.IsSelected));

        internal void BindHammerTutorial(OriginalHammerTutorial tutorial) => hammerTutorial = tutorial;
        internal void BindOptionalBoosterTutorial(OriginalOptionalBoosterTutorial tutorial) => optionalBoosterTutorial = tutorial;
        public bool IsBoosterSelected => (hammerTutorial != null && hammerTutorial.IsSelected) ||
            (optionalBoosterTutorial != null && optionalBoosterTutorial.IsSelected) ||
            (boosterSession != null && boosterSession.IsSelected);
        public bool CanUserMakeMove() => Moves.CanUserMakeMove();
        public OriginalGridCell GetCellFromWorldPosition(Vector2 position)
        {
            // 0x320cfa0 subtracts the board origin recorded by OnBoardUpdated 0x320adac.
            var point = OriginalCellPointInput.FromMousePosition(position - board.TouchOrigin,
                board.Grid.Width == 9, Application.platform == RuntimePlatform.IPhonePlayer);
            return board.Grid.GetCell(point.X, point.Y);
        }
        public bool ShouldDeselectBoosterOnTouch(OriginalGridCell cell) =>
            (optionalBoosterTutorial != null && optionalBoosterTutorial.ShouldDeselect(cell)) ||
            (boosterSession != null && boosterSession.ShouldDeselect(cell));

        public void DeselectBooster()
        {
            if (optionalBoosterTutorial != null) optionalBoosterTutorial.Deselect();
            boosterSession?.CancelSelection();
        }
        public void CancelTouchedItemAnimation() => presses.CancelTouchedItemAnimation();
        public void TouchCell(OriginalGridCell cell) => presses.TouchCell(cell);
        public bool TapCell(OriginalGridCell cell) => taps.TapCell(cell);
        internal int GetTutorialExplodeScore(OriginalGridCell cell, int guidedType) =>
            guidedTargetServices.GetScore(cell, guidedType, false);
        public bool SwapCells(OriginalGridCell from, OriginalCellPoint to) => swaps.SwapCells(from, to);
        internal void SetTutorialTapCell(OriginalGridCell cell) => tutorialTapCell = cell;
        public bool IsImpossibleMoveDisabledForCell(OriginalCellPoint point) =>
            tutorialTapCell != null && tutorialTapCell.X == point.X && tutorialTapCell.Y == point.Y;
        public void StartImpossibleSwapAnimation(OriginalSwapActionState action, OriginalGridCell from, OriginalCellPoint to) =>
            new OriginalImpossibleSwapAnimation(action, from, to, impossible).Play();
        public void IncreaseFalseSwipeCount() { Session.FalseSwipeCount = unchecked(Session.FalseSwipeCount + 1); }
        public void CheckForUsefulMove()
        {
            if (useful.IsUsefulMoveMade) useful.ResetUselessMoveStreak();
            else useful.IncreaseUselessMoveCount();
            useful.ResetIsUsefulMoveMade();
        }
        // The propeller target/pilot calculation cache is not composed yet.
        public void ClearSinglePropellerWinCalculationData() => king.ClearSinglePropellerWinCalculationData();
        public void PlaySfx(int id, float delay) => audio.GameSfx.PlaySfx(id, delay);
        internal void PlayApplicationSfx(int id, float replayInterval) => audio.Playback.PlaySound(id, replayInterval);
        public void PlayHaptic(int id, int type, float strength, float duration, bool replay) { } // SDK boundary remains disabled.
        public void TryPlayStaticInvalidMoveAnimation(OriginalCellPoint point)
        {
            var cell = board.Grid.GetCell(point.X, point.Y);
            if (cell != null) OriginalCellPress.TryPlayInvalidMoveAnimationForSwap(cell);
        }
        public void ExplodeMatchGroups(OriginalSwapDecision decision, OriginalBoardItem other, OriginalBoardItem moving) =>
            (ReferenceEquals(decision, board.FirstSwapAction.Decision) ? firstResolution : secondResolution).Resolve(other, moving);
        // ComboManager.IsCombo (0x1e2d558). The hint picker and the swap
        // dispatcher use the same combination IDs.
        internal static int GetSpecialComboType(OriginalSpecialBoardItem first, OriginalSpecialBoardItem second) =>
            (first.ItemType, second.ItemType) switch
            {
                (5, 5) => 1,
                (5, 4) or (4, 5) => 2,
                (5, 3) or (3, 5) => 3,
                (5, 2) or (2, 5) => 4,
                (4, 4) => 5,
                (4, 2) or (2, 4) => 6,
                (2, 2) => 7,
                (4, 3) or (3, 4) => 8,
                (3, 2) or (2, 3) => 9,
                (3, 3) => 10,
                _ => 0
            };

        public void ExplodeSpecialItems(OriginalBoardItem other, OriginalBoardItem moving)
        {
            var first = other as OriginalSpecialBoardItem;
            var second = moving as OriginalSpecialBoardItem;
            var data = OriginalExploder.Next(2); // 0x1ee42c0 creates even for a normal swap.
            if (first != null && second != null)
            {
                int comboType = GetSpecialComboType(first, second);
                if (comboType == 0) throw new NotSupportedException("This special combo implementation is not restored yet.");
                first.IsPreparedForSpecialUse = true;
                second.IsPreparedForSpecialUse = true;
                king.OnUserCombo(comboType, second.CurrentCell);
                switch (comboType)
                {
                    case 1:
                        BallBallCombo.Prepare((OriginalLightballBoardItem)first, (OriginalLightballBoardItem)second);
                        BallBallCombo.Explode();
                        break;
                    case 2: ExplodeLightballTntCombo(first, second, data); break;
                    case 3: ExplodeLightballPropellerCombo(first, second, data); break;
                    case 4: ExplodeLightballRocketCombo(first, second, data); break;
                    case 5:
                        tntTntCombo.Prepare((OriginalTntBoardItem)first, (OriginalTntBoardItem)second);
                        tntTntCombo.Explode(data);
                        break;
                    case 6:
                        tntRocketCombo.Prepare(first, second);
                        tntRocketCombo.Explode(data);
                        break;
                    case 7:
                        rocketRocketCombo.Prepare((OriginalRocketBoardItem)first, (OriginalRocketBoardItem)second);
                        rocketRocketCombo.Explode(data);
                        break;
                    case 8: new OriginalTntPropellerCombo(this).Explode(first, second, data); break;
                    case 9: new OriginalPropellerRocketCombo(this).Explode(first, second, data); break;
                    case 10:
                        new OriginalPropellerPropellerCombo(this).Explode(
                            (OriginalPropellerBoardItem)first, (OriginalPropellerBoardItem)second, data);
                        break;
                }
                Session.SpecialItemCounts.Combos.TryGetValue(comboType, out int count);
                Session.SpecialItemCounts.Combos[comboType] = unchecked(count + 1); // IncrementComboUse 0x27b2f34.
                OnSpecialComboUsed?.Invoke();
                UsefulMoveMade();
                // Native dispatches MoveManager's completion directly at
                // 0x1ee30a0. There is no swap operation to finish for combo 6.
                if (comboType == 6) Moves.CompleteMoveByType(12);
                return;
            }
            var special = first ?? second;
            if (special == null) return;
            special.SwapWith(first != null ? moving : other, data);
            IncrementItemUse(special.ItemType, true);
        }
        internal void BindSynthesisObserver(Func<bool> canRecord, Action<Vector3> record) =>
            explosion.BindSynthesisObserver(canRecord, record);

        public bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group, OriginalExplodeData data, bool automatic) => explosion.ExplodeMatchGroup(group, data, automatic);
        public void ReportPatternChanged(int previous, int current) => Debug.LogWarning("Match pattern changed during swap.");
        public void ReportMatchExplosionFailed() => Debug.LogWarning("Swap group was not ready to explode.");
        public bool IsAutoMatchDisabledForWaterLevel => false;
        public void ShuffleCellsBasedOnDifficulty(OriginalMatchCells cells) => random.ShuffleCellsBasedOnDifficulty(cells);
        public void CreateSpecialItemForMatchGroup(OriginalMatchGroupSnapshot group, OriginalGridCell cell, bool automatic) =>
            specialCreator.CreateSpecialItemForMatchGroup(group, cell, automatic);
        public void PlayMatchExplode(bool swap) => audio.GameSfx.PlayMatchExplode(swap);
        public OriginalBoardItem CreateItemForFillingCellAt(float x, float y, OriginalFillingCell cell)
        {
            var item = creation.CreateItemForFillingCellAt(x, y, cell);
            if (item != null) CreatedRefills++;
            return item;
        }
        public bool IsPortalLevel => false;
        public bool IsMultipleBoardLevel => board.Layout.Count > 1;
        public bool IsLevelWon => Session.State == 3;
        public bool IsStoryLevel => Session.StoryLevel < 0;
        public bool IsFinalBoard => board.Layout.CurrentBoardIndex == 0;
        public bool IsWaterLevel => board.Presentation.IsWaterLevel;
        public int[][] PotionColors => null;
        public bool ContainsDrillMasterPoint(OriginalGridCell cell) => false;
        public OriginalBoardItem CreateItem(OriginalItemSettings settings) => ((IOriginalItemViewCreationContext)board).CreateItem(settings);
        public void OnNewItemCreated(int type) => generation.OnNewItemCreated(type);
        public OriginalLevelRandomSource GetLevelRandom() => random;
        public int SelectMatchTileColor(int id, OriginalLevelSetSelection set, OriginalLevelRandomSource source) => colors.SelectMatchTileColor(id, firstIds);
        public IOriginalPredefinedItemCreator ItemCreator => initial;
        public bool IsTuningEnabled => lowMatch.IsEnabled;
        public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell) => lowMatch.GetTunedItem(set, cell);
        public int GetLimitCount(int goal) => board.Goals.GetLimitCount(goal);
        public int GetGoalTargetCount(int goal) => board.Goals.GetGoalTargetCount(goal);
        public void ReportRandomColorFallback(int total) => Debug.LogError("Fill set has no weighted color: " + total);
        public float OffsetX => board.BoardRoot.position.x;
        public float OffsetY => board.BoardRoot.position.y;
        public void GetCellViewPosition(OriginalFillingCell cell, out float x, out float y) { var p = cell.GetViewPosition(); x = p.x; y = p.y; }
        public void StartProfilingLevel() { StartedAt = Time.realtimeSinceStartup; }
        public void ClearFlow(bool gameExit) { }
        public int LeftMoves => Moves.LeftMoves;
        public bool IsPlaying() => board.State.IsPlaying();
        public bool ShouldReportThreeColorExtension(int number, int league) => false;
        public bool TryGetVersionOverride(int number, out int version) { version = 0; return false; }
        public string GetLevelExtension() => "L";
        public int EventId => -1;
        public int EventType => 0;
        public bool ShouldShowIcon() => false;
        public int GetCount() => 0;
        public bool ShouldProgress => false;
        public OriginalLevelStartInventory GetInventory() => inventory;
        public IOriginalSessionProgress GetSuperBooster() => this;
        public IOriginalSessionProgress GetSuperDuke() => this;
        public IOriginalSessionIntPreferences Preferences => preferences;
        public int CurrentProgress => 0;
        public int AbGroup => 0;
        public void SetAbValues() { }
        public void UseBooster(OriginalGridCell cell)
        {
            if (hammerTutorial != null && hammerTutorial.IsSelected) hammerTutorial.Use(cell);
            else if (optionalBoosterTutorial != null && optionalBoosterTutorial.IsSelected) optionalBoosterTutorial.Use(cell);
            else boosterSession?.Use(cell);
        }
        public void IncrementTappedLightball() { Session.SpecialItemCounts.TappedLightballCount++; }
        // New-user UX persistence belongs to the pending account owner. These
        // notifications keep that boundary separate from native tap completion.
        public void OnSpecialItemTapped() => OnSpecialTapped?.Invoke();
        public void IncrementItemUse(int type, bool combo)
        {
            var count = GetSpecialCount(type);
            count.UseCount = unchecked(count.UseCount + 1);
            if (combo) Session.SpecialItemCounts.SwappedItemCount = unchecked(Session.SpecialItemCounts.SwappedItemCount + 1);
        }
        public void IncrementAutoTrigger(int type)
        {
            Session.SpecialItemCounts.AutoTriggeredItemCount = unchecked(Session.SpecialItemCounts.AutoTriggeredItemCount + 1);
            var count = GetSpecialCount(type); count.AutoTriggerCount = unchecked(count.AutoTriggerCount + 1);
        }
        private OriginalSpecialItemCount GetSpecialCount(int type)
        {
            if (!Session.SpecialItemCounts.Items.TryGetValue(type, out var count))
            { count = new OriginalSpecialItemCount(); Session.SpecialItemCounts.Items.Add(type, count); }
            return count;
        }
        public void OnTntOrRocketItemTapped() => OnTntOrRocketTapped?.Invoke();
        public void StartGameplayCoroutine(IEnumerator routine) => board.Presentation.Coroutines.ManualStartCoroutine(routine);
        public object Wait(float seconds) => CreateGameplayWait(seconds);
        public void PlayTntExplosion(OriginalTntItemSpriteView view) => board.Presentation.PlayTntExplosion(view, audio.GameSfx);
        public void ShakeGrid(OriginalTntShakeConfig config) => board.ShakeGrid(config);
        public void RecycleTntView(OriginalTntItemSpriteView view) => Pool.Recycle(view);
        public OriginalDefaultLightballStrategy CreateDefaultLightballStrategy() =>
            new OriginalDefaultLightballStrategy(board, this, generalRandom, specialCreator.LightballSelectedColors);
        public OriginalLightballViewAssets LightballAssets => board.Presentation.LightballAssets;
        public int RunningLightballColors => specialCreator.LightballSelectedColors.Count;
        public OriginalSwapSorting GetLightballUseSorting() => new OriginalSwapSorting(ItemSortingLayer, 11010 + specialCreator.LightballSelectedColors.Count * 10, false);
        private const int LightballChargeSound = 219;
        private const long SharedLightballCharge = -1;
        long IOriginalLightballViewServices.StartLightballChargeSound() =>
            LightballAssets.speedIncrease
                ? audio.GameSfx.PlayStoppableSfx(LightballChargeSound, audio, loop: true)
                : audio.GameSfx.TryPlaySfxInLoop(LightballChargeSound) ? SharedLightballCharge : 0;
        void IOriginalLightballViewServices.ReleaseLightballChargeSound(long key, bool exploded)
        {
            if (key == SharedLightballCharge) audio.GameSfx.StopSoundInLoop(LightballChargeSound);
            else if (exploded) audio.Playback.FadeOutSound(key, LightballAssets.chargeFadeSeconds);
            else audio.Playback.StopSound(key);
        }
        public void ExplodeLightballView(OriginalLightballItemSpriteView view)
        {
            PlaySfx(214, .04f);
            view.ReleaseChargeSound(true);
            board.Presentation.PlayLightballExplosion(view.transform.position);
            Pool.Recycle(view);
        }
        public void CollectTntMadness(OriginalExplodeData data, OriginalGridCell center)
        {
            var position = center.GetViewPosition();
            Session.TryCollectMadnessForExploder(data, new OriginalSwapPoint(position.x, position.y, position.z), 0);
        }

        public OriginalGameObjectPool Pool => board.Presentation.Pool;
        internal void CreateOwlStatueView(OriginalOwlStatueBoardItem item, OriginalSwapPoint position)
        {
            var view = Pool.Spawn<OriginalOwlStatueItemView>(53, true);
            view.Initialize(item, board, this, audio, position);
            item.BindView(view, this);
        }
        internal void CreateVaseView(OriginalVaseBoardItem item, OriginalSwapPoint position)
        {
            var view = Pool.Spawn<OriginalVaseItemSpriteView>(38, true);
            view.Initialize(item, board, position, ItemSortingLayer, audio, generalRandom);
            item.BindView(view, this);
            board.LocalMoves.RegisterTarget(view.transform);
        }
        internal void CreatePotionView(OriginalPotionBoardItem item)
        {
            var view = Pool.Spawn<OriginalPotionItemView>(58, true);
            view.Initialize(item, board, this, audio, generalRandom, board.Presentation.PotionVisuals);
            item.BindView(view, this);
        }
        internal void CreateEggView(OriginalEggBoardItem item, OriginalSwapPoint position)
        {
            var view = Pool.Spawn<OriginalEggItemSpriteView>(51, true);
            view.Initialize(item, board, position, ItemSortingLayer);
            item.BindView(view, this);
            board.LocalMoves.RegisterTarget(view.transform);
        }
        internal void ExplodeEggView(OriginalEggItemSpriteView view)
        {
            PlaySfx(140 + audio.Random.Next(0, 3), .04f);
            board.Presentation.Particles.GetGlobalParticle<OriginalEggBurstParticles>(52).Play(view.transform.position);
            Pool.Recycle(view);
        }
        public int ItemSortingLayer => SortingLayer.NameToID("Item");
        public int ParticlesSortingLayer => SortingLayer.NameToID("Particles");
        public int UiSortingLayer => SortingLayer.NameToID("Ui");
        public bool IsWaterMode => IsWaterLevel;
        bool IOriginalSpecialItemCreationServices.IsPlaying => board.State.IsPlaying();
        public bool IsSessionStarted => Session.State == 2;
        public sbyte CurrentLevelDifficulty => Session.Difficulty;
        public bool IsAboveStaticSpreading(OriginalGridCell cell) => cell.IsAboveStaticSpreading;
        public bool IsUnderHoney(OriginalBoardItem item) => item.IsUnderHoney();
        public bool IsUnderRoof(OriginalBoardItem item) => item.IsUnderRoof();
        public bool IsUnderChain(OriginalBoardItem item) => item.Mediator.CurrentCell != null && item.Mediator.CurrentCell.Owner.StaticTouch.HasStaticItem(6);
        public bool HasIncomingTouchBlockingItem(OriginalGridCell cell) => board.MatchBlocking.HasIncomingTouchBlockingItem(cell);
        public bool IsFallingCross(OriginalBoardItem item) => ((OriginalFallComponentState)item.FallComponent).IsFallingCross;
        public IOriginalSpecialCreationItem CreateItemAtCell(int tiledId, OriginalGridCell cell)
        {
            var item = (OriginalSpecialBoardItem)CreateItem(OriginalItemSettings.ForTiledId(tiledId));
            CreateSpecialView(item, new OriginalSwapPoint(cell.X, cell.Y, 0f));
            cell.Mediation.ClearAllItems();
            cell.Mediation.SetAllItems(item);
            CreatedSpecials++;
            return item;
        }
        internal void CreateSpecialView(OriginalSpecialBoardItem item, OriginalSwapPoint point)
        {
            var position = new Vector3(point.X, point.Y, point.Z);
            var parent = board.BoardRoot;
            if (item is OriginalRocketBoardItem rocket)
            {
                rocket.CreateView(board.Presentation, parent, position, this);
                rocket.BindExplosion(this);
            }
            else if (item is OriginalTntBoardItem tnt)
            {
                tnt.CreateView(board.Presentation, parent, position, this);
                tnt.BindExplosion(this);
            }
            else if (item is OriginalLightballBoardItem lightball)
            {
                lightball.CreateView(board.Presentation, parent, position, this);
                lightball.BindExplosion(this);
            }
            else if (item is OriginalPropellerBoardItem propeller)
            {
                propeller.CreateView(board.Presentation, parent, position, this);
                propeller.BindExplosion(this);
                propeller.ItemView.OnFallCreated += NotifyPropellerFall;
            }
            else throw new NotSupportedException("Special view is not composed for " + item.TiledId);
            item.BindSpecialExplosion(this);
            item.FallView = new OriginalSpecialItemFallView(item, ItemSortingLayer);
            board.LocalMoves.RegisterTarget(((Component)item.View).transform);
        }
        public void ManualStartCoroutine(IEnumerator routine) { board.Presentation.Coroutines.ManualStartCoroutine(routine); }
        public object CreateGameplayWait(float duration) => new OriginalGameplayWait(duration, () => board.Clock.DeltaTime);
        internal void SetShuffleOrientation(bool enabled) => setCoinOrientation(enabled);
        internal OriginalMatchItemSpriteView ReplaceShuffleMatch(OriginalMatchBoardItem previous, Vector3 world) // 0x21a77dc.
        {
            var current = previous.Mediator.CurrentCell;
            var next = previous.Mediator.NextCell;
            var target = previous.Mediator.TargetCell;
            var sorting = ((OriginalMatchItemSpriteView)previous.SwapView).GetSorting();
            Vector3 local = board.BoardRoot.InverseTransformPoint(world);
            int tiledId = OriginalLevelRules.ToTiledId(previous.MatchType);
            var replacement = itemViews.CreateItemAt(tiledId, new OriginalSwapPoint(local.x, local.y, local.z));
            var view = (OriginalMatchItemSpriteView)replacement.SwapView;
            view.UpdateSorting(sorting);
            previous.Mediator.ClearFromAllRegisteredCells();
            previous.RecycleView();
            current?.SetCurrentItem(replacement);
            next?.SetNextItem(replacement);
            target?.SetTargetItem(replacement);
            board.LocalMoves.RegisterTarget(view.transform);
            return view;
        }
        public void IncrementItemCreation(int type, bool automatic)
        {
            if (!Session.SpecialItemCounts.Items.TryGetValue(type, out var count))
            { count = new OriginalSpecialItemCount(); Session.SpecialItemCounts.Items.Add(type, count); }
            count.CreationCount = unchecked(count.CreationCount + 1);
            if (automatic) count.AutoCreationCount = unchecked(count.AutoCreationCount + 1);
        }
        public void SpecialItemCreated(IOriginalSpecialCreationItem model, OriginalGridCell cell, int delay, int count, bool sticker) => OnSpecialCreated?.Invoke((OriginalSpecialBoardItem)model);
        public void TryCollectMadness(OriginalGridCell cell, int type, int count, int delay, IOriginalSpecialCreationItem model, bool remaining)
        {
            var p = cell.GetViewPosition();
            Session.TryCollectMadness(type, new OriginalSwapPoint(p.x, p.y, p.z), count, delay, (OriginalBoardItem)model, remaining);
        }
        public void AddMissionEvent(int type, sbyte difficulty, int count) => Session.AnimationValues.StartSnapshots.MissionEvents.AddExtraForItem(type, count);
        public void UsefulMoveMade() => useful.UsefulMoveMade();
        public void IncreaseUselessMoveCount() => useful.IncreaseUselessMoveCount();
        public bool IsPlayingWithSuperBooster() => false;
        public void ReportMissingCreationCell() => Debug.LogError("Created special item lost its cell.");
        public OriginalSpecialItemCreationParticles PlayAppearParticle(IOriginalSpecialCreationView view)
        {
            var t = ((Component)view).transform;
            var front = board.Presentation.Particles.GetGlobalParticle<OriginalSpecialItemCreationParticles>(5);
            front.PlayOptimized(t.position, t);
            return front;
        }
        private void NotifyPropellerFall(OriginalPropellerBoardItem model) => OnSpecialCreated?.Invoke(model);

        private sealed class FrameUnit : IOriginalContextBehaviour
        {
            public int Id { get; }
            private readonly Action bind, update;
            public FrameUnit(int id, Action bind, Action update) { Id = id; this.bind = bind; this.update = update; }
            public void Bind() => bind?.Invoke();
            public void ManualUpdate() => update();
        }
    }
}
