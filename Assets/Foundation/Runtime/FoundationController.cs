using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Whitebox.Foundation
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class FoundationController : MonoBehaviour
    {
        [SerializeField] private FoundationSettings settings;
        [SerializeField] private AdScenario adScenario;
        [SerializeField] private Transform previewRoot;
        [SerializeField] private OriginalInitialBoardView initialBoard;
        [SerializeField] private OriginalGameAudioHost gameAudio;
        [SerializeField] private OriginalGameTouchInput gameTouchInput;
        [SerializeField] private OriginalContextUpdateHost contextUpdates;
        [SerializeField] private string gameplayHudPrefabPath;
        [SerializeField] private string bottomUiPrefabPath;
        [SerializeField] private OriginalGameSceneProjection gameSceneProjection;
        [SerializeField] private CoreGameFlow coreFlow;
        [SerializeField] private CoreFlowSettings coreFlowSettings;
        private CoreLevelProgress progress;
        private IRoyalMatchSessionBridge sessionBridge;
        private CoreLevelAttemptStore attemptStore;
        private OriginalAttemptSeeds attemptSeeds;
        private int currentLevelNumber;
        private OriginalGameplayHud gameplayHud;
        private OriginalBottomUi bottomUi;
        private OriginalPlayableBoard gameplay;
        private VisualElement uiRoot;
        private Label moves, goals;
        // Consumers get the playable board once its entrance finishes.
        public OriginalPlayableBoard Gameplay => coreFlow != null && coreFlow.IsEnteringLevel ? null : gameplay;
        public CoreGameFlow CoreFlow => coreFlow;
        public CoreLevelProgress Progress => progress;
        public int CurrentLevelNumber => currentLevelNumber;
        private ConfigData.FTiledLevel level;
        private AdFacade ads;
        private Button adButton;
        private Label status;
        private Label rewards;
        private int sequence;

        private void Awake()
        {
            if (settings == null) throw new InvalidOperationException("Application timing settings are missing.");
            // ApplicationContextController.ConfigureApplication 0x38fbde4..0x38fbe04.
            // The same startup calls run in Editor and Android/iOS players.
            Application.targetFrameRate = settings.TargetFrameRate;
            QualitySettings.vSyncCount = settings.VSyncCount;
            Input.multiTouchEnabled = false;
        }

        private void OnEnable()
        {
            if (coreFlow == null || coreFlowSettings == null) throw new InvalidOperationException("Core flow prefab settings are missing.");
            if (adScenario == null) sessionBridge = RoyalMatchSessionBridge.Current;
            if (sessionBridge != null) return;
            LoadInitialLevel(settings.PlayableLevelNumber);
            if (!progress.IsComplete && adScenario != null) BindRecoveryPreview();
        }

        private void LoadInitialLevel(int levelNumber)
        {
            progress = new CoreLevelProgress(coreFlowSettings.progressFileName, levelNumber, sessionBridge != null);
            if (adScenario == null)
            {
                int reviveMoves = sessionBridge is IRoyalMatchReviveBridge ? coreFlowSettings.firstReviveMoves : 0;
                attemptStore = new CoreLevelAttemptStore(progress.DatabasePath, reviveMoves);
            }
            currentLevelNumber = progress.LevelToPlay;
            if (progress.IsComplete) return;
            level = settings.CoreLevels.Load(currentLevelNumber);
        }

        private void BindRecoveryPreview()
        {
            var document = GetComponent<UIDocument>();
            if (!document.isActiveAndEnabled || previewRoot == null)
                throw new InvalidOperationException("Recovery preview is not configured.");
            var root = uiRoot = document.rootVisualElement;
            moves = root.Q<Label>("moves");
            goals = root.Q<Label>("goals");
            adButton = root.Q<Button>("reward-ad");
            status = root.Q<Label>("status");
            rewards = root.Q<Label>("reward-count");
            root.Q<Label>("level-summary").text = level.Name + " | " + level.Grid.Width + " x " + level.Grid.Height + " | moves: " + level.Move;
            ads = new AdFacade(new LocalAdProvider(adScenario), adScenario.RewardAmount);
            ads.Finished += OnFinished;
            adButton.clicked += ShowLocalAd;
        }

        private void Start()
        {
            if (sessionBridge == null)
                InitializeGame();
            else
                StartCoroutine(StartWhenServerLevelIsAvailable());
        }

        private IEnumerator StartWhenServerLevelIsAvailable()
        {
            int levelNumber;
            // Additive reload activates the new scene before the previous controller flushes its save.
            while (RoyalMatchSessionBridge.ActiveController != null ||
                   !sessionBridge.TryGetCurrentLevel(out levelNumber)) yield return null;
            RoyalMatchSessionBridge.ActiveController = this;
            LoadInitialLevel(levelNumber);
            InitializeGame();
        }

        private void InitializeGame()
        {
            if (initialBoard == null) throw new InvalidOperationException("Initial board is not configured.");
            if (gameAudio == null) throw new InvalidOperationException("Game audio is not configured.");
            gameAudio.Initialize();
            if (gameTouchInput == null || contextUpdates == null)
                throw new InvalidOperationException("The scene must bind its game camera input and prefab context host.");
            contextUpdates.BindGameTouchInput(gameTouchInput);
            if (progress.IsComplete)
            {
                ShowSavedCompletion();
                return;
            }
            initialBoard.BindFallSfx(gameAudio.GameSfx);
            var hudPrefab = Resources.Load<GameObject>(gameplayHudPrefabPath);
            if (hudPrefab == null) throw new InvalidOperationException("Original gameplay HUD prefab is missing.");
            gameplayHud = Instantiate(hudPrefab, transform).GetComponent<OriginalGameplayHud>();
            CreateBottomUi();
            StartAttempt(false);
        }

        private void CreateBottomUi()
        {
            if (string.IsNullOrEmpty(bottomUiPrefabPath)) return;
            var bottomPrefab = Resources.Load<GameObject>(bottomUiPrefabPath);
            if (bottomPrefab == null) throw new InvalidOperationException("Original bottom UI prefab is missing.");
            bottomUi = Instantiate(bottomPrefab, transform).GetComponent<OriginalBottomUi>();
        }

        private void ShowSavedCompletion()
        {
            attemptStore?.Clear();
            var camera = gameTouchInput.PointerCamera;
            if (gameSceneProjection.isActiveAndEnabled)
            {
                gameSceneProjection.InitializeCompletion(camera);
                gameSceneProjection.OnProjectionChanged += RefreshHudLayout;
            }
            coreFlow.ShowCompletion(gameAudio, camera, progress);
        }

        private void StartAttempt(bool restarted)
        {
            var camera = gameTouchInput.PointerCamera;
            var restored = !restarted ? attemptStore?.Load(level) : null;
            OriginalGameStateSaveData restoredState = null;
            if (restored != null)
            {
                restoredState = new OriginalGameStateSaveData();
                restoredState.SetData(restored.GameStateSaveData);
            }
            initialBoard.Initialize(level, PrepareDifficultySeed(restoredState), restored?.BoardSaveData);
            gameplay = new OriginalPlayableBoard(initialBoard, gameAudio, contextUpdates, level, settings, IsUiTouching, camera,
                gameSceneProjection, settings.CreateCoreInventory(), currentLevelNumber, restored);
            if (restoredState != null) gameplay.Session.EgoPurchaseCount = restoredState.GetEgoPurchaseCount();
            coreFlow.PrepareOpeningTutorial(currentLevelNumber, bottomUi != null, restoredState);
            attemptStore?.Begin(gameplay, restoredState);
            gameplay.InitializeViews();
            bool useGameProjection = gameSceneProjection.isActiveAndEnabled;
            if (useGameProjection)
            {
                if (restarted) gameSceneProjection.RebindSession(gameplay.Session);
                else gameSceneProjection.Initialize(camera, gameplay.Session, initialBoard, level.HasHoleInGrid);
            }
            var projection = useGameProjection ? gameSceneProjection : null;
            gameplayHud.Initialize(gameplay, initialBoard.Goals, gameAudio, camera, projection);
            if (bottomUi != null)
            {
                if (bottomUi.IsInitialized)
                {
                    bottomUi.SetOrientationEnabled(true);
                    bottomUi.RefreshLayout();
                }
                else bottomUi.InitializeCoreDisplay(currentLevelNumber, camera, projection);
            }
            if (useGameProjection) gameSceneProjection.OnProjectionChanged += RefreshHudLayout;
            gameTouchInput.Bind(gameplay);
            coreFlow.Initialize(gameplay, initialBoard, gameplayHud, gameAudio, camera, progress, bottomUi,
                QueueRestart, useGameProjection, restarted, restoredState, attemptStore, projection, sessionBridge);
            if (bottomUi != null && sessionBridge is IRoyalMatchBoosterInventory boosterInventory)
                gameplay.BindBoosterInventory(boosterInventory, bottomUi, gameplayHud, camera, projection,
                    coreFlow.Configuration, () => coreFlow.BlocksBoardInput);
            if (uiRoot != null)
            {
                gameplay.Moves.OnMoveChanged += UpdateMoves;
                initialBoard.Goals.OnGoalUpdatedUi += UpdateGoals;
                UpdateMoves(gameplay.Moves.LeftMoves, 1);
                UpdateGoals(11, initialBoard.Goals.GetGoalDisplayCount(11), false, true, false);
                status.text = "Drag adjacent colored pieces. Boxes cannot be swapped.";
            }
        }

        private int PrepareDifficultySeed(OriginalGameStateSaveData restored)
        {
            if (attemptStore == null) return settings.LocalDifficultySeed;

            int attempt = restored == null ? progress.RecordLevelStart() : progress.LevelTryCount;
            attemptSeeds ??= new OriginalAttemptSeeds(new OriginalLevelRandomSource());
            // This core-only profile has no account or SDK device identifier.
            attemptSeeds.ResetDifficultySeed(currentLevelNumber, 0L, 0, attempt);
            return restored?.GetDifficultySeed() ?? attemptSeeds.SelectedSeed;
        }

        private void QueueRestart() => StartCoroutine(RestartAttempt());

        public void ApplyServerLevel()
        {
            if (sessionBridge == null || gameplay == null || coreFlow.IsTransitioning ||
                (!gameplay.State.IsPlaying() && !coreFlow.IsEnteringLevel)) return;
            if (sessionBridge.TryGetCurrentLevel(out int serverLevel) && serverLevel != currentLevelNumber)
                coreFlow.RetryLevel();
        }

        // GameManager.RestartGame 0x1e3b6f8 resets the game context inside the
        // current scene before activating the new level and sliding in the grid.
        private IEnumerator RestartAttempt()
        {
            yield return null;
            int nextLevel = currentLevelNumber;
            if (sessionBridge != null)
            {
                while (!sessionBridge.TryGetCurrentLevel(out nextLevel)) yield return null;
                if (nextLevel != currentLevelNumber) settings.CoreLevels.GetEntry(nextLevel);
            }
            attemptStore?.Clear();
            coreFlow.Release();
            gameSceneProjection.OnProjectionChanged -= RefreshHudLayout;
            gameplayHud.ResetForRestart();
            gameTouchInput.Unbind();
            contextUpdates.ApplicationCoroutines.KillAll();
            initialBoard.Presentation.Coroutines.KillAll();
            DisposeAttempt();
            initialBoard.ResetForRestart();
            gameAudio.ResetAttemptSounds();
            if (nextLevel != currentLevelNumber)
            {
                level = settings.CoreLevels.Load(nextLevel);
                progress.SetAuthoritativeLevel(nextLevel);
                currentLevelNumber = nextLevel;
                if (bottomUi != null)
                {
                    bottomUi.gameObject.SetActive(false);
                    Destroy(bottomUi.gameObject);
                }
                CreateBottomUi();
            }
            StartAttempt(true);
        }

        private bool IsUiTouching()
        {
            if (coreFlow != null && coreFlow.BlocksBoardInput)
            {
                var listener = gameTouchInput.Listener;
                if (coreFlow.IsBridgeInputBlocked && listener != null && listener.IsTouching)
                    listener.StopTouch();
                return true;
            }
            if (gameSceneProjection.IsOverSquareSideBand(Input.mousePosition)) return true;
            var optionalGuide = coreFlow != null ? coreFlow.OptionalBoosterTutorial : null;
            if (optionalGuide != null && (optionalGuide.IsGuiding || optionalGuide.IsSelected) && EventSystem.current != null)
            {
                int pointerId = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
                if (EventSystem.current.IsPointerOverGameObject(pointerId)) return true;
            }
            var panel = uiRoot?.panel;
            if (panel == null) return false;
            var p = Input.mousePosition;
            var hit = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, new Vector2(p.x, Screen.height - p.y)));
            return hit != null && hit != uiRoot && hit != panel.visualTree;
        }
        private void UpdateMoves(int left, int reason) { moves.text = "Moves: " + left; }
        private void UpdateGoals(int type, int count, bool prerequisite, bool start, bool complete)
        { if (type == 11) goals.text = "Boxes: " + count; }

        private void ShowLocalAd() { ads.Show("local-preview-" + (++sequence)); }
        private void OnFinished(AdOutcome result)
        {
            switch (result)
            {
                case AdOutcome.Completed: status.text = "Local callback: completed"; break;
                case AdOutcome.Cancelled: status.text = "Local callback: cancelled"; break;
                default: status.text = "Local callback: unavailable"; break;
            }
            rewards.text = "Mock rewards: " + ads.EarnedRewards;
        }

        private void Update() => progress?.ManualUpdate(Time.frameCount);
        private void OnApplicationPause(bool paused)
        {
            if (paused) attemptStore?.Flush();
            progress?.SetApplicationPaused(paused);
        }
        private void OnApplicationQuit()
        {
            attemptStore?.Flush();
            progress?.OnApplicationQuit();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (RoyalMatchSessionBridge.ActiveController == this)
                RoyalMatchSessionBridge.ActiveController = null;
            attemptStore?.Flush();
            attemptStore = null;
            if (coreFlow != null) coreFlow.Release();
            if (gameSceneProjection != null) gameSceneProjection.OnProjectionChanged -= RefreshHudLayout;
            if (gameplayHud != null) gameplayHud.Release();
            if (gameTouchInput != null) gameTouchInput.Unbind();
            DisposeAttempt();
            if (adButton != null) adButton.clicked -= ShowLocalAd;
            if (ads != null) ads.Finished -= OnFinished;
            adButton = null;
            ads = null;
            sessionBridge = null;
            uiRoot = null;
            if (progress != null) { progress.Dispose(); progress = null; }
        }
        private void DisposeAttempt()
        {
            if (gameplay == null) return;
            if (uiRoot != null)
            {
                gameplay.Moves.OnMoveChanged -= UpdateMoves;
                initialBoard.Goals.OnGoalUpdatedUi -= UpdateGoals;
            }
            gameplay.Dispose();
            gameplay = null;
        }
        private void RefreshHudLayout()
        {
            if (!coreFlow.IsEnteringLevel)
            {
                gameplayHud?.RefreshLayout();
                if (bottomUi != null) bottomUi.RefreshLayout();
            }
            coreFlow.RefreshDialogBackgroundLayout();
        }
    }
}
