using System;
using System.Collections.Generic;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // Native initial grid population precedes view creation and player input.
    public sealed partial class OriginalInitialBoardView : MonoBehaviour, IOriginalFallSquashContext, IOriginalMatchViewCreationServices, IOriginalItemViewCreationContext, IOriginalGridItemPopulationContext, IOriginalRocketItemStaticState
    {
        [Serializable]
        public sealed class ItemVisual
        {
            public int TiledId;
            public string PrefabPath;
            public string SpritePath;
            public string ShadowPath;
            public OriginalMatchViewPose MatchPose;
        }

        [SerializeField] private Transform boardRoot;
        [SerializeField] private OriginalGridShakeAnimation gridShake;
        public void ShakeGrid(OriginalTntShakeConfig config)
        { gridShake.Bind(Clock); gridShake.Shake(config); }
        [SerializeField] private ItemVisual[] visuals;
        [SerializeField] private OriginalLocalMovePlayer localMoves;
        [SerializeField] private OriginalLevelPresentation presentation;
        [SerializeField] private OriginalContextUpdateHost contextUpdates;
        private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private IOriginalGoalProgress goalProgress;
        private IOriginalGameStateServices gameStateServices;
        private bool isScroll;
        private OriginalGameSfx fallSfx;
        private IOriginalMatchItemExplosionServices matchExplosionServices;
        private IOriginalItemTargetScoreContext targetScoreContext;
        private OriginalPlayableBoard playable;
        private Func<float> levelTime;
        private int[] potionColors;
        public Transform BoardRoot => boardRoot;
        internal Vector2 TouchOrigin { get; private set; }
        internal void RefreshTouchOrigin() => TouchOrigin = gridShake.RestWorldPosition;
        public void SetBoardPortraitOffset(float offset)
        {
            // The original uses the midpoint of the outer cells, including
            // ordinary boards with nine or ten rows.
            var region = Layout.CurrentBoard;
            gridShake.SetRestHeight(offset - (region.Bottom + region.Top) * .5f);
            RefreshTouchOrigin();
        }
        public bool IsScroll => isScroll;
        bool IOriginalFallSquashContext.IsScroll => isScroll;
        float IOriginalFallSquashContext.DeltaTime => Clock.DeltaTime;
        public OriginalCellGrid Grid { get; private set; }
        public OriginalBoardLayout Layout { get; private set; }
        public int CreatedViewCount { get; private set; }
        public int CreatedCellViewCount { get; private set; }
        public int CreatedModelCount { get; private set; }
        public int InitialPopulationAttempts { get; private set; }
        internal OriginalLevelRandomSource DifficultyRandom { get; private set; }
        internal OriginalParsedLevelSet[] ParsedSets { get; private set; }
        internal OriginalMatchTileColors TileColors { get; private set; }
        public event Action OnItemModelsInitialized;
        private OriginalGridItemPopulation initialPopulation;
        public OriginalDeltaTimeManager Clock { get; private set; }
        public OriginalGameOperations GameOperations { get; private set; }
        public OriginalGameStateManager State { get; private set; }
        public OriginalMoveManager Moves { get; private set; }
        public OriginalGoalClaims GoalClaims { get; private set; }
        public OriginalGoalManager Goals { get; private set; }
        public OriginalLevelGoalPreparation GoalPreparation { get; private set; }
        public OriginalItemGoalServices ItemGoals { get; private set; }
        public OriginalInitialItemAmounts InitialItemAmounts { get; private set; }
        public OriginalBoardFallMotionServices FallMotion { get; private set; }
        public OriginalMatchManager Matches { get; private set; }
        public OriginalPossibleMoveFinder PossibleMoves { get; private set; }
        public OriginalInitialShuffleCheck InitialShuffle { get; private set; }
        public OriginalMatchExplosionReadiness MatchReadiness { get; private set; }
        public OriginalMatchBlockingState MatchBlocking { get; private set; }
        public OriginalItemSwapAvailability SwapAvailability { get; private set; }
        public OriginalSwapDecision SwapDecision { get; private set; }
        public OriginalSwapActionState FirstSwapAction { get; private set; }
        public OriginalSwapActionState SecondSwapAction { get; private set; }
        public OriginalLocalMovePlayer LocalMoves => localMoves;
        public OriginalLevelPresentation Presentation => presentation;
        public event Action<int> OnSwapEnded;
        public void BindGoalProgress(IOriginalGoalProgress value)
        {
            goalProgress = value ?? throw new ArgumentNullException(nameof(value));
        }
        public void BindGameStateServices(IOriginalGameStateServices value)
        { gameStateServices = value ?? throw new ArgumentNullException(nameof(value)); }
        public void BindFallSfx(OriginalGameSfx value) { fallSfx = value ?? throw new ArgumentNullException(nameof(value)); }

        public void Initialize(FTiledLevel level, int difficultySeed, FBoardSaveData restored = null)
        {
            if (Grid != null) throw new InvalidOperationException("Board is already initialized.");
            if (boardRoot == null || gridShake == null || visuals == null || localMoves == null ||
                presentation == null || contextUpdates == null)
                throw new InvalidOperationException("Board prefab configuration is missing.");
            bool water = level.WaterData != null && level.WaterData.WaterReceivableCellCount > 0;
            bool shifter = level.ShifterGrids != null && level.ShifterGrids.Length > 0;
            if (water || shifter || level.Scroll)
                throw new NotSupportedException("Water, shifter and scroll grids require their original board composition.");
            for (int i = 0; i < level.Grid.Cells.Length; i++)
            {
                var c = level.Grid.Cells[i];
                if ((c.Grass != 0 && c.Grass != 31 && c.Grass != 32) ||
                    (c.Honey != 0 && c.Honey != OriginalHoneyBoardItem.TiledId) || c.Curtain != 0 || c.Chain != 0 || c.Roof != 0 ||
                    c.Jelly != 0 || c.Treasuremap != 0 || c.Ceramic != 0 || c.MushroomSoil != 0 ||
                    c.Blinds != 0 || c.WaterSoil != 0 || c.ForceField != 0 || c.Cryptex != 0 ||
                    c.IceBorder != 0 || c.MetalPlates != 0 || c.ItemCurtain != 0)
                    throw new NotSupportedException("Static item creation must be restored before loading this level.");
            }
            DifficultyRandom = new OriginalLevelRandomSource(difficultySeed);
            ParsedSets = OriginalParsedLevelSet.ParseSets(level, 0, 0);
            TileColors = new OriginalMatchTileColors(DifficultyRandom, 0, 0);
            potionColors = level.Colors;
            var preparedIds = OriginalLevelRules.ParseColors(level.Grid.Items, 0, 0);
            var palettes = new int[ParsedSets.Length][];
            for (int i = 0; i < palettes.Length; i++) palettes[i] = ParsedSets[i].ElementIds;
            if (palettes.Length != 0)
                for (int i = 0; i < preparedIds.Length; i++)
                    if (unchecked((uint)(preparedIds[i] - 211)) < 3u)
                        preparedIds[i] = TileColors.SelectMatchTileColor(preparedIds[i], palettes[0]);
            var transformed = restored == null
                ? new OriginalGridTransform(preparedIds, level.Grid.Width, level.Grid.Height, palettes, DifficultyRandom) : null;
            var itemIds = transformed == null ? preparedIds : transformed.CopyItemsInGridOrder();
            restoredGrid = restored == null ? null : new RestoredGrid(restored.GridData);
            for (int i = 0; i < itemIds.Length; i++)
            {
                // The authored placeholder still defines the cell shape on a
                // restore. Only the saved current item needs a model/view.
                int id = restored == null ? itemIds[i] : restored.GridData.Grid[i].CurrentItem?.TiledId ?? 0;
                if (!OriginalGridItemPopulation.ShouldCreateItem(OriginalLevelRules.AsCellType(itemIds[i]), id,
                    restored == null ? 1 : 3)) continue;
                int itemType = OriginalTiledItemTypes.AsItemType(id);
                if ((itemType >= 7 && itemType <= 11) || itemType == 15 || IsSpecialId(id) ||
                    id == 43 || id == 44 || id == 53 || id == 54 || itemType == 13 || itemType == 14) continue;
                if (OriginalLevelRules.ToMatchType(id) == 0 && (id < 21 || id > 24))
                    throw new NotSupportedException("Initial item model is not restored: " + id);
                FindVisual(id);
            }
            if (presentation.Pool == null) presentation.Initialize();
            Grid = OriginalRegularGridCreation.Create(level.Grid, itemIds, false, false, false, out var layout);
            Layout = layout;
            // GridPositioner 0x1e33b68 / 0x1e33c50 places the origin to the
            // left of the board center by half the distance between the outer cells.
            var boardPosition = boardRoot.localPosition;
            boardPosition.x = -(layout.CurrentBoard.Left + layout.CurrentBoard.Right) * .5f;
            boardRoot.localPosition = boardPosition;
            RefreshTouchOrigin();
            isScroll = level.Scroll;
            Clock = contextUpdates.CreateDeltaTimeManager();
            levelTime = () => Clock.Time;
            presentation.BindFallContext(this);
            presentation.BindCellContext(Layout, water, shifter);
            MatchReadiness = new OriginalMatchExplosionReadiness(() => Clock.Time);
            State = new OriginalGameStateManager(() => Clock.FrameCount, () => gameStateServices);
            GameOperations = State.Operations;
            // No budget or play state is fabricated before the active-level
            // provider and actual LevelTouch context are composed.
            Moves = new OriginalMoveManager();
            GoalClaims = new OriginalGoalClaims();
            Goals = new OriginalGoalManager(() => Layout.CurrentBoardIndex, GameOperations, GoalClaims);
            GoalPreparation = new OriginalLevelGoalPreparation(level, Goals);
            var goals = new List<Dictionary<int, OriginalGoalTuple>>();
            var limits = new List<Dictionary<int, OriginalLimitTuple>>();
            var counts = new List<Dictionary<int, int>>();
            GoalPreparation.ParseGoals(goals);
            GoalPreparation.ParseLimits(limits);
            GoalPreparation.ParseCounts(counts);
            InitialItemAmounts = new OriginalInitialItemAmounts(presentation.GetParticleAmount, RequireFillingHelmetPiggyContext);
            OriginalLevelItemAmountPreparation.Create(level, InitialItemAmounts, goals, counts, limits);
            FallMotion = new OriginalBoardFallMotionServices(this, fallSfx);
            // RestoreLoader.GetGoals 0x23e2f60 applies saved counters to the authored goal identities.
            if (restored != null) RestoreGoals(goals, restored.Goals);
            Goals.InitWithGoalsAndLimits(goals, limits, restored == null);
            ItemGoals = new OriginalItemGoalServices(Goals, () => goalProgress);
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                var cell = Grid.GetCell(x, y);
                cell.BindGameOperations(GameOperations);
                var countdown = new OriginalCellFreezeCountdown(cell.UnFreezeFall);
                cell.BindFreezeCountdown(countdown);
            }
            CreateHoneyModels(level.Grid);
            initialPopulation = new OriginalGridItemPopulation(
                new OriginalTotalBottomLeftVerticalStrategy(new OriginalBoardGridAccess(Grid, Layout)), this);
            initialPopulation.OnItemModelsInitialized += NotifyItemModelsInitialized;
            MatchBlocking = new OriginalMatchBlockingState(false, null);
            Matches = new OriginalMatchManager(Grid, Layout, MatchBlocking, new OriginalMatchFreezeOperations());
            PossibleMoves = new OriginalPossibleMoveFinder(new OriginalBoardPossibleMoveServices(MatchBlocking));
            InitialShuffle = new OriginalInitialShuffleCheck(PossibleMoves,
                new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(Grid, Layout))))
                { MatchManager = new OriginalInitialMatchSearch(Matches) };
            if (restoredGrid == null) PopulateInitialItems(transformed);
            else
            {
                InitialPopulationAttempts = 0;
                initialPopulation.CreateItems(restoredGrid, 3);
            }
            SwapAvailability = new OriginalItemSwapAvailability();
            var swapContext = new OriginalSwapMatchContext(Matches);
            SwapDecision = new OriginalSwapDecision(swapContext);
            FirstSwapAction = CreateSwapAction(SwapDecision);
            SecondSwapAction = CreateSwapAction(new OriginalSwapDecision(swapContext));
        }

        // GameManager.StartLevel requests its checkpoint before InitializeViews.
        internal void InitializeCellAndMatchViews()
        {
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                presentation.CreateCellView(Grid.GetCell(x, y));
                CreatedCellViewCount++;
            }
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                var item = Grid.GetCell(x, y).Mediation.CurrentItem;
                if (item is OriginalMatchBoardItem match)
                {
                    match.CreateView(new OriginalSwapPoint(x, y, 0f));
                    localMoves.RegisterTarget(((OriginalMatchItemSpriteView)match.SwapView).transform);
                    CreatedViewCount++;
                }
            }
            Debug.Log("Original initial board created: " + Grid.Width + "x" + Grid.Height + ", views=" + CreatedViewCount);
        }

        internal void ResetForRestart()
        {
            initialPopulation.OnItemModelsInitialized -= NotifyItemModelsInitialized;
            localMoves.CancelAll();
            if (gridShake.IsShaking) gridShake.StopShaking();
            contextUpdates.LateUpdates.UnregisterFromLateUpdate(Clock.ManualLateUpdate);
            presentation.RecycleAttemptViews();
            Grid = null;
            CreatedCellViewCount = CreatedModelCount = CreatedViewCount = 0;
            goalProgress = null;
            gameStateServices = null;
            matchExplosionServices = null;
            targetScoreContext = null;
            playable = null;
        }

        private void NotifyItemModelsInitialized() => OnItemModelsInitialized?.Invoke();

        // GameManager.CreateItems 0x1e3bb98. Only the final population gets views.
        private void PopulateInitialItems(OriginalGridTransform data)
        {
            const int maxRetries = 500;
            initialPopulation.CreateItems(data, 1);
            InitialPopulationAttempts = 1;
            for (int retry = 0; retry < maxRetries && data.HasAtLeastOnePossibleMatch && InitialShuffle.ShouldTryAgain(); retry++)
            {
                data.Randomize();
                CreatedModelCount = 0;
                initialPopulation.CreateItems(data, 1);
                InitialPopulationAttempts = Math.Min(InitialPopulationAttempts + 1, maxRetries);
            }
        }
        OriginalBoardItem IOriginalGridItemPopulationContext.CreateItemModelForStart(int tiled, int original, OriginalGridCell cell)
        {
            if (OriginalInitialItemCreation.IsASet(tiled)) throw new NotSupportedException("Unprepared initial set ID.");
            var settings = OriginalItemSettings.ForTiledId(tiled);
            settings.CellModel = cell;
            settings.IsHandGiven = !OriginalInitialItemCreation.IsASet(original);
            var item = ((IOriginalItemViewCreationContext)this).CreateItem(settings);
            CreatedModelCount++;
            return item;
        }
        short IOriginalGridItemPopulationContext.GetRestoredItemData(int x, int y) => restoredGrid.Cell(x, y).CurrentItem.Data;
        void IOriginalGridItemPopulationContext.LoadItemSaveData(OriginalBoardItem item, short data) => item.RestoreItemData(data);

        internal void BindPlayableItemServices(OriginalPlayableBoard owner, IOriginalMatchItemExplosionServices matches,
            IOriginalItemTargetScoreContext targetScores)
        {
            playable = owner;
            matchExplosionServices = matches ?? throw new ArgumentNullException(nameof(matches));
            targetScoreContext = targetScores ?? throw new ArgumentNullException(nameof(targetScores));
        }

        internal void BindPlayableItems(IOriginalBoxViewServices boxes, IOriginalBoxExplosionServices usefulMoves)
        {
            for (int x = 0; x < Grid.Width; x++) for (int y = 0; y < Grid.Height; y++)
            {
                var item = Grid.GetCell(x, y).Mediation.CurrentItem;
                if (item != null) item.BindTargetScoreContext(targetScoreContext);
                if (item is OriginalMatchBoardItem match) match.BindExplosionServices(matchExplosionServices);
                else if (item is OriginalBoxBoardItem box)
                {
                    box.CreateView(presentation, boardRoot, new Vector3(x, y, 0f), boxes, usefulMoves);
                    CreatedViewCount++;
                }
                else if (item is OriginalCoinBoardItem || item is OriginalEggBoardItem || item is OriginalVaseBoardItem ||
                    item is OriginalOwlStatueBoardItem || item is OriginalSpecialBoardItem ||
                    item is OriginalMailBoxBoardItem || item is OriginalPotionBoardItem ||
                    item is OriginalBushBoardItem || item is OriginalSafeBoardItem)
                {
                    ((IOriginalItemViewCreationContext)this).CreateView(item, new OriginalSwapPoint(x, y, 0f));
                    CreatedViewCount++;
                }
            }
        }

        internal void CreateGrassModels(FTiledGrid data, IOriginalBoxExplosionServices usefulMoves)
        {
            int amount = InitialItemAmounts.GetStaticItemAmount(2);
            if (amount == 0 && restoredGrid == null) return;
            // CellGridManager.CreateStaticItems 0x320f87c.
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                var saved = restoredGrid?.GetStaticItem(x, y, 31, 32);
                int tiled = restoredGrid == null ? data.Cells[OriginalLevelRules.GetIndexFromPoint(x, y, data.Width)].Grass
                    : saved?.TiledId ?? 0;
                if (tiled == 0) continue;
                Grid.GetCell(x, y).AddStaticItem(new OriginalGrassBoardItem(tiled, Goals, InitialItemAmounts, usefulMoves, saved?.Data ?? 0));
            }
        }

        internal void BindGrassViews(OriginalGameAudioHost audio, IOriginalLevelSeedRandom random)
        {
            int amount = InitialItemAmounts.GetStaticItemAmount(2);
            // Bush levels can create grass even when no grass is on the opening board.
            bool poolsReady = amount > 0;
            if (poolsReady) presentation.PrepareGrassPools(amount);
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                if (!(Grid.GetCell(x, y).StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem grass)) continue;
                if (!poolsReady)
                {
                    presentation.PrepareGrassPools(1);
                    poolsReady = true;
                }
                presentation.SpawnGrassView().Initialize(grass, presentation.GrassAssets, random,
                    presentation, audio, boardRoot, Layout.CurrentBoard.Bottom);
            }
        }

        internal OriginalCupboardItemRegistry CreateCupboards(OriginalGameAudioHost audio, IOriginalLevelSeedRandom random,
            IOriginalBoxExplosionServices usefulMoves)
        {
            int amount = InitialItemAmounts.GetItemAmount(8);
            if (amount == 0) return null;
            presentation.PrepareCupboardPools(amount);
            var cupboardItems = new OriginalCupboardItemRegistry(Clock);
            for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
            {
                if (!(Grid.GetCell(x, y).Mediation.CurrentItem is OriginalCupboardBoardItem cupboard)) continue;
                cupboard.CreateView(presentation, boardRoot, audio, random, Clock, usefulMoves, cupboardItems);
                CreatedViewCount++;
            }
            return cupboardItems;
        }

        private static bool RequireFillingHelmetPiggyContext() => throw new NotSupportedException("Unique filling-item set/session ownership is required for helmet piggy pool preparation.");

        IOriginalMatchCreatedItemView IOriginalMatchViewCreationServices.SpawnMatchView() => presentation.SpawnMatchView();
        void IOriginalMatchViewCreationServices.InitializeMatchView(IOriginalMatchCreatedItemView view,
            OriginalMatchBoardItem model, OriginalSwapPoint position)
        {
            var match = (OriginalMatchItemSpriteView)view;
            var visual = FindVisual(model.MatchType);
            match.BindFallContext(this);
            match.Initialize(model, boardRoot, position, LoadSprite(visual.SpritePath), LoadSprite(visual.ShadowPath), visual.MatchPose);
        }
        OriginalBoardItem IOriginalItemViewCreationContext.CreateItem(OriginalItemSettings settings)
        {
            var item = IsSpecialId(settings.TiledId) ? CreateSpecialModel(settings) : OriginalCoreItemModels.Create(settings, potionColors);
            item.BindGoalServices(ItemGoals);
            if (targetScoreContext != null) item.BindTargetScoreContext(targetScoreContext);
            ((OriginalFallComponentState)item.FallComponent).BindMotionServices(FallMotion);
            if (item is OriginalMatchBoardItem match)
            {
                match.BindViewCreationServices(this);
                if (matchExplosionServices != null) match.BindExplosionServices(matchExplosionServices);
            }
            return item;
        }
        void IOriginalItemViewCreationContext.CreateView(OriginalBoardItem item, OriginalSwapPoint position)
        {
            if (item is OriginalMatchBoardItem match) match.CreateView(position);
            else if (item is OriginalCoinBoardItem coin) playable.CreateBoardCoinView(coin, position);
            else if (item is OriginalEggBoardItem egg) playable.CreateEggView(egg, position);
            else if (item is OriginalVaseBoardItem vase) playable.CreateVaseView(vase, position);
            else if (item is OriginalOwlStatueBoardItem owl) playable.CreateOwlStatueView(owl, position);
            else if (item is OriginalPotionBoardItem potion) playable.CreatePotionView(potion);
            else if (item is OriginalBushBoardItem bush) playable.CreateBushView(bush, position);
            else if (item is OriginalSafeBoardItem safe) playable.CreateSafeView(safe, position);
            else if (item is OriginalMailBoxBoardItem mailbox) playable.CreateMailBoxView(mailbox, position);
            else if (item is OriginalSpecialBoardItem special) playable.CreateSpecialView(special, position);
            else throw new NotSupportedException("Production filling view is not composed for " + item.ItemType);
        }
        IOriginalCreatedItemView IOriginalItemViewCreationContext.GetItemView(OriginalBoardItem item) => item.SwapView as IOriginalCreatedItemView;

        private static bool IsSpecialId(int id) => id == 10 || id == 20 || id == 30 || id == 40 || id == 50;
        private OriginalSpecialBoardItem CreateSpecialModel(OriginalItemSettings settings)
        {
            int id = OriginalBoardItem.StaticId;
            OriginalBoardItem.StaticId = unchecked(id + 1);
            int tiled = settings.TiledId;
            OriginalSpecialBoardItem item;
            switch (tiled)
            {
                case 10: item = new OriginalPropellerBoardItem(id, levelTime, this, settings.IsCreatedByLightball); break;
                case 20:
                case 30: item = new OriginalRocketBoardItem(id, tiled == 30 ? 0 : 1, settings.IsCreatedByLightball, tiled, levelTime, this); break;
                case 40: item = new OriginalTntBoardItem(id, settings.IsCreatedByLightball, tiled, levelTime, this); break;
                case 50: item = new OriginalLightballBoardItem(id, levelTime, this); break;
                default: throw new ArgumentOutOfRangeException(nameof(settings));
            }
            item.TiledId = tiled;
            item.GoalType = settings.GoalType;
            item.CreateFallComponent();
            return item;
        }
        bool IOriginalSpecialItemStaticState.IsUnderHoney(OriginalBoardItem item) => item.IsUnderHoney();
        bool IOriginalSpecialItemStaticState.IsUnderRoof(OriginalBoardItem item) => item.IsUnderRoof();
        bool IOriginalSpecialItemStaticState.IsAboveStaticSpreading(OriginalGridCell cell) => cell.IsAboveStaticSpreading;
        bool IOriginalSpecialItemChainedStaticState.IsUnderChain(OriginalBoardItem item) =>
            item.Mediator.CurrentCell?.Owner.StaticTouch.HasStaticItem(6) == true;

        private OriginalSwapActionState CreateSwapAction(OriginalSwapDecision decision)
        {
            var action = new OriginalSwapActionState(decision);
            action.OnSwapAnimationStart += HandleSwapStart;
            action.OnSwapAnimationEnd += HandleSwapEnd;
            return action;
        }
        private void HandleSwapStart() { GameOperations.OperationStart(2); } // 0x1ee1c80.
        private void HandleSwapEnd(int moveType)
        {
            GameOperations.OperationFinish(2); // 0x1ee1cc0 precedes its move event.
            OnSwapEnded?.Invoke(moveType);
        }

        private ItemVisual FindVisual(int id)
        {
            if (id >= 22 && id <= 24) id = 21; // Original box family uses its layer-specific BoxItemAssets sprite.
            for (int i = 0; i < visuals.Length; i++) if (visuals[i].TiledId == id) return visuals[i];
            throw new NotSupportedException("Initial visual is not configured: " + id);
        }
        private GameObject LoadPrefab(string path)
        {
            if (!prefabs.TryGetValue(path, out var value))
            {
                value = Resources.Load<GameObject>(path);
                if (value == null) throw new InvalidOperationException("Missing board prefab: " + path);
                prefabs.Add(path, value);
            }
            return value;
        }
        private Sprite LoadSprite(string path)
        {
            if (!sprites.TryGetValue(path, out var value))
            {
                value = Resources.Load<Sprite>(path);
                if (value == null) throw new InvalidOperationException("Missing board sprite: " + path);
                sprites.Add(path, value);
            }
            return value;
        }
    }
}
