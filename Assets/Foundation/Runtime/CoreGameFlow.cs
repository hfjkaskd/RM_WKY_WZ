using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation
{
    // Actual core-only scene composition. Native WinFail16, session Win,
    // FlowManager, remaining moves, dialog actions and UiDialog lifetimes run
    // here. Home/account/economy/lives/paid continuation/events are excluded
    // explicitly by the requested product scope, not supplied by test fixtures.
    public sealed class CoreGameFlow : MonoBehaviour, IOriginalWinFailServices, IOriginalWinFlowContext,
        IOriginalRemainingMovesManagerServices, IOriginalFlowExecutorContext, IOriginalWinLogoActionContext,
        IOriginalRemainingMovesActionContext, IOriginalLevelWinActionContext, IOriginalDialogManagerContext,
        IOriginalWinLogoDialogContext, IOriginalRemainingMovesDialogContext, IOriginalLevelWinDialogContext,
        IOriginalLevelWinAds, IOriginalLevelWinLavaFlow, IOriginalLevelWinAncientFlow, IOriginalLevelWinGameManager, IOriginalTweenClock
    {
        [SerializeField] private CoreFlowSettings config;
        [SerializeField] private Transform dialogScaler;
        private Action restartLevel;
        private OriginalPlayableBoard game;
        private OriginalInitialBoardView board;
        private OriginalGameplayHud hud;
        private OriginalGameAudioHost audio;
        private OriginalBottomUi bottomUi;
        private Camera cameraSource;
        private OriginalGameSceneProjection sceneProjection;
        private CoreLevelProgress progress;
        private CoreExcludedPeripheralSystems peripherals;
        private OriginalFlowManager flow;
        private OriginalWinFlowCoordinator win;
        private OriginalDialogManager dialogs;
        private CoreDialogBackgroundPool backdrops;
        private OriginalWinLogoDialog logo;
        private OriginalRemainingMovesDialog remaining;
        private OriginalLevelWinDialog result;
        private CoreLevelFailDialog failure;
        private Coroutine fade;
        private bool transitioning, initialized, failureStarted, outOfMovesStarted;
        private bool uiDisabled, tutorialPending;
        private bool waitingDirectedTutorial, usedTutorialBooster;
        private bool openingHammer, openingBoosterReady;
        private string openingBoosterPath;
        private OriginalDirectedTutorialProfile openingDirected;
        private OriginalModalTutorialProfile openingModal;
        private CoreLevelAttemptStore attemptStore;
        private Coroutine entrance;
        private int dialogInputLocks;
        private OriginalModalTutorialView modalTutorial;
        private OriginalDirectedTutorialView directedTutorial;
        private OriginalHammerTutorial hammerTutorial;
        private OriginalOptionalBoosterTutorial optionalBoosterTutorial;
        private OriginalMatchHintManager matchHints;
        private OriginalDynamicPowerTutorials dynamicPowerTutorials;
        private OriginalGridIterator lightballGuideCells;
        private OriginalBoardPossibleMoveServices tutorialMoveEligibility;
        private float onlyLightballSince = -1f;
        private bool lightballGuideShown;
        public OriginalWinFailManager WinFail { get; private set; }
        public OriginalRemainingMovesManager RemainingMoves { get; private set; }
        public OriginalFlowManager Flow => flow;
        public OriginalLevelWinDialog WinDialog => result;
        public CoreLevelFailDialog FailDialog => failure;
        public CoreOutOfMovesBanner OutOfMovesBanner { get; private set; }
        public CoreNoPossibleMovesDialog NoPossibleMovesDialog { get; private set; }
        public CoreLevelQuitDialog LevelQuitDialog { get; private set; }
        public bool IsTransitioning => transitioning;
        public bool IsEnteringLevel => entrance != null;
        public bool IsInitialized => initialized && !IsEnteringLevel;
        public bool BlocksBoardInput => initialized &&
            (transitioning || IsEnteringLevel || uiDisabled || dialogInputLocks > 0 || dialogs.HasActiveDialog || tutorialPending ||
             (modalTutorial != null && modalTutorial.BlocksBoardInput));
        private bool BlocksSettingsInput => BlocksBoardInput ||
            (directedTutorial != null && directedTutorial.IsGuiding) ||
            (hammerTutorial != null && hammerTutorial.IsGuiding) ||
            (optionalBoosterTutorial != null && (optionalBoosterTutorial.IsGuiding || optionalBoosterTutorial.IsSelected));
        public OriginalModalTutorialView ModalTutorial => modalTutorial;
        public OriginalDirectedTutorialView DirectedTutorial => directedTutorial;
        public OriginalHammerTutorial HammerTutorial => hammerTutorial;
        public OriginalOptionalBoosterTutorial OptionalBoosterTutorial => optionalBoosterTutorial;
        public OriginalMatchHintManager MatchHints => matchHints;
        public CoreFlowSettings Configuration => config;
        internal bool CanSaveAttempt => IsInitialized && !tutorialPending && !waitingDirectedTutorial &&
            (directedTutorial == null || !directedTutorial.IsGuiding) &&
            (modalTutorial == null || !modalTutorial.BlocksBoardInput) &&
            (hammerTutorial == null || (!hammerTutorial.IsGuiding && !hammerTutorial.IsSelected)) &&
            (optionalBoosterTutorial == null || (!optionalBoosterTutorial.IsGuiding && !optionalBoosterTutorial.IsSelected));
        internal bool HasShownDynamicTutorial => dynamicPowerTutorials.HasShown;
        internal bool UsedTutorialBooster => usedTutorialBooster ||
            (hammerTutorial != null && !hammerTutorial.IsGuiding && !hammerTutorial.IsSelected) ||
            (optionalBoosterTutorial != null && optionalBoosterTutorial.IsSpent);

        internal void PrepareOpeningTutorial(int level, bool hasBottomUi, OriginalGameStateSaveData restored)
        {
            // PrepareTutorial 0x218c74c, RestoreFromSaveData 0x218f43c.
            // On a restored level only an unused free-booster tutorial is prepared.
            usedTutorialBooster = restored != null && restored.FreeBoosterTutorialBoosterUsage();
            openingHammer = openingBoosterReady = waitingDirectedTutorial = false;
            openingBoosterPath = null;
            openingDirected = null;
            openingModal = null;
            if (hasBottomUi && !usedTutorialBooster)
            {
                openingHammer = level == OriginalBoosterUnlock.GetUnlockLevel(OriginalBoosterType.Hammer);
                if (level == OriginalBoosterUnlock.GetUnlockLevel(OriginalBoosterType.Arrow))
                    openingBoosterPath = config.arrowTutorialPrefabPath;
                else if (level == OriginalBoosterUnlock.GetUnlockLevel(OriginalBoosterType.Cannon))
                    openingBoosterPath = config.cannonTutorialPrefabPath;
                else if (level == OriginalBoosterUnlock.GetUnlockLevel(OriginalBoosterType.JesterHat))
                    openingBoosterPath = config.jesterTutorialPrefabPath;
                // A fresh restored board still gets the prompt. Once a move
                // was made, the native booster tutorial skips its guided steps.
                openingBoosterReady = restored != null && restored.GetMadeMoves() > 0;
            }
            if (restored != null || openingHammer || openingBoosterPath != null) return;
            if (config.directedTutorials != null)
                foreach (var profile in config.directedTutorials)
                    if (profile.level == level) { openingDirected = profile; return; }
            if (config.modalTutorials != null)
                foreach (var profile in config.modalTutorials)
                    if (profile.level == level) { openingModal = profile; return; }
        }

        internal void Initialize(OriginalPlayableBoard game, OriginalInitialBoardView board, OriginalGameplayHud hud,
            OriginalGameAudioHost audio, Camera camera, CoreLevelProgress progress, OriginalBottomUi bottomUi,
            Action restartLevel, bool animateEntrance, bool restarted, OriginalGameStateSaveData restoredState,
            CoreLevelAttemptStore attemptStore, OriginalGameSceneProjection projection)
        {
            InitializeDialogFlow(audio, camera, progress);
            sceneProjection = projection;
            this.game = game; this.board = board; this.hud = hud;
            this.bottomUi = bottomUi;
            this.attemptStore = attemptStore;
            this.restartLevel = restartLevel ?? throw new ArgumentNullException(nameof(restartLevel));
            peripherals = new CoreExcludedPeripheralSystems(game.Session);
            win = new OriginalWinFlowCoordinator(this);
            RemainingMoves = new OriginalRemainingMovesManager(this); game.BindRemainingMovesManager(RemainingMoves);
            WinFail = game.BindWinFail(this); initialized = true;
            bottomUi?.LevelSettings?.Bind(this, audio, camera);
            matchHints = new OriginalMatchHintManager(board, game, progress, config.matchHintParticlesPrefabPath, config.matchHintTiming);
            dynamicPowerTutorials = new OriginalDynamicPowerTutorials(game, board, progress,
                restoredState != null && restoredState.HasShowedAnyDynamicTutorial());
            tutorialMoveEligibility = new OriginalBoardPossibleMoveServices(board.MatchBlocking);
            lightballGuideCells = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            game.BindLevelFlow(this, attemptStore);
            if (animateEntrance) audio.PlayGameMusic(game.Session.Number);
            if (restoredState != null && board.Layout.IsFinalBoard)
                hud.goalPanel.RestoreCompletedChecks();
            if (animateEntrance && restoredState == null)
                entrance = StartCoroutine(EnterLevel(bottomUi, !restarted));
            else FinishLevelEntrance(bottomUi);
        }

        internal void ShowCompletion(OriginalGameAudioHost audio, Camera camera, CoreLevelProgress progress)
        {
            InitializeDialogFlow(audio, camera, progress);
            initialized = true;
            flow.Push(new SavedCompletionAction(this));
        }

        private void InitializeDialogFlow(OriginalGameAudioHost audio, Camera camera, CoreLevelProgress progress)
        {
            if (initialized || config == null) throw new InvalidOperationException("Core flow prefab configuration.");
            this.audio = audio;
            cameraSource = camera ?? throw new ArgumentNullException(nameof(camera));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            flow = new OriginalFlowManager(this);
            dialogs = new OriginalDialogManager(this);
            backdrops = new CoreDialogBackgroundPool(camera, () => Create<CoreDialogBackground>(config.backdropPrefabPath), this);
        }

        // GameUi.StartDefaultLevel 0x162b784 and CellGridManager.ShowGridWithAnimation 0x320d044.
        private IEnumerator EnterLevel(OriginalBottomUi bottomUi, bool animateHud)
        {
            var timing = config.levelEntrance;
            if (timing.boardDuration <= 0f || timing.hudDuration <= 0f || timing.boardStart < 0f)
                throw new InvalidOperationException("Core entrance timing is missing.");
            var grid = board.BoardRoot;
            float gridX = grid.position.x;
            float aspect = Mathf.Min(cameraSource.aspect, 1f / cameraSource.aspect);
            float uiScale = aspect >= .8f ? 1.1904762f : 1f;
            float startX = gridX + timing.boardOffset * uiScale;
            float height = cameraSource.orthographicSize;
            float topStart = height + timing.topOutsideOffset;
            float bottomStart = -height + timing.bottomOutsideOffset;
            float topEnd = hud.transform.position.y;
            float bottomEnd = bottomUi != null ? bottomUi.transform.position.y : 0f;
            hud.SetOrientationEnabled(false);
            if (bottomUi != null) bottomUi.SetOrientationEnabled(false);
            if (animateHud) audio.GameSfx.PlaySfx(209, .04f);

            bool boardSoundPlayed = false;
            float elapsed = 0f;
            float duration = timing.boardStart + timing.boardDuration;
            while (elapsed < duration)
            {
                var position = grid.position;
                float boardT = Mathf.Clamp01((elapsed - timing.boardStart) / timing.boardDuration);
                position.x = Mathf.LerpUnclamped(startX, gridX, OriginalGoalPanelUi.RewardEase(boardT, timing.boardOvershoot));
                grid.position = position;
                if (animateHud && elapsed <= timing.hudDuration)
                {
                    float hudT = OriginalGoalPanelUi.RewardEase(Mathf.Clamp01(elapsed / timing.hudDuration), timing.hudOvershoot);
                    SetWorldY(hud.transform, Mathf.LerpUnclamped(topStart, topEnd, hudT));
                    if (bottomUi != null) SetWorldY(bottomUi.transform, Mathf.LerpUnclamped(bottomStart, bottomEnd, hudT));
                }
                else if (animateHud)
                {
                    SetWorldY(hud.transform, topEnd);
                    if (bottomUi != null) SetWorldY(bottomUi.transform, bottomEnd);
                    animateHud = false;
                }
                if (!boardSoundPlayed && elapsed >= timing.boardSoundTime)
                {
                    audio.GameSfx.PlaySfx(208, .04f);
                    boardSoundPlayed = true;
                }
                yield return null;
                elapsed += Time.deltaTime;
            }
            var settled = grid.position;
            settled.x = gridX;
            grid.position = settled;
            hud.SetOrientationEnabled(true);
            hud.RefreshLayout();
            if (bottomUi != null)
            {
                bottomUi.SetOrientationEnabled(true);
                bottomUi.RefreshLayout();
            }
            entrance = null;
            FinishLevelEntrance(bottomUi);
        }

        private static void SetWorldY(Transform root, float y)
        {
            var position = root.position;
            position.y = y;
            root.position = position;
        }

        private void FinishLevelEntrance(OriginalBottomUi bottomUi)
        {
            // GameManager.GridAnimationCompleted 0x1e3be84 refreshes the grid before input opens.
            board.RefreshTouchOrigin();
            game.State.StartLevel();
            if (openingHammer || openingBoosterPath != null)
            {
                tutorialPending = true;
                StartCoroutine(ShowBoosterTutorial(bottomUi, openingBoosterPath));
            }
            else if (openingDirected != null)
            {
                tutorialPending = openingDirected.delay <= 0f;
                waitingDirectedTutorial = true;
                StartCoroutine(ShowDirectedTutorial(openingDirected));
            }
            else if (openingModal != null)
            {
                tutorialPending = true;
                StartCoroutine(ShowModalTutorial(openingModal));
            }
        }

        private IEnumerator ShowBoosterTutorial(OriginalBottomUi bottomUi, string optionalPrefabPath)
        {
            while (IsInitialized && (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation()))
                yield return null;
            if (IsInitialized && game.State.IsPlaying())
            {
                if (optionalPrefabPath == null)
                {
                    hammerTutorial = Create<OriginalHammerTutorial>(config.hammerTutorialPrefabPath);
                    hammerTutorial.Bind(game, board, bottomUi, cameraSource);
                    game.BindHammerTutorial(hammerTutorial);
                }
                else
                {
                    optionalBoosterTutorial = Create<OriginalOptionalBoosterTutorial>(optionalPrefabPath);
                    optionalBoosterTutorial.Bind(game, board, hud, bottomUi, cameraSource, sceneProjection, openingBoosterReady);
                    game.BindOptionalBoosterTutorial(optionalBoosterTutorial);
                }
            }
            tutorialPending = false;
        }

        private IEnumerator ShowDirectedTutorial(OriginalDirectedTutorialProfile profile)
        {
            float elapsed = 0f;
            while (IsInitialized && game.AcceptedSwaps == 0 && elapsed < profile.delay &&
                game.Session.SpecialItemCounts.TappedLightballCount < profile.lightballTaps)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (profile.stableBoardSeconds > 0f)
            {
                // RunTutorial 0x2190950 polls CanShowNextStep 0x276bb24 every .05s.
                // The first check starts with a zero clock origin; a busy check resets it.
                var poll = new WaitForSeconds(.05f);
                float stableSince = 0f;
                while (IsInitialized && game.AcceptedSwaps == 0)
                {
                    float now = board.Clock.Time;
                    if (now > .2f)
                    {
                        if (board.State.HasAnyOperation()) stableSince = -1f;
                        else
                        {
                            if (stableSince < 0f) stableSince = now;
                            if (now - stableSince > profile.stableBoardSeconds) break;
                        }
                    }
                    yield return poll;
                }
            }
            else
                while (IsInitialized && !profile.showWhileBoardBusy && game.AcceptedSwaps == 0 &&
                    (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation()))
                    yield return null;
            if (IsInitialized && game.State.IsPlaying() && game.AcceptedSwaps == 0)
            {
                directedTutorial = Create<OriginalDirectedTutorialView>(profile.prefabPath);
                directedTutorial.Bind(profile, game, board, hud, cameraSource);
            }
            waitingDirectedTutorial = false;
            tutorialPending = false;
        }

        private IEnumerator ShowModalTutorial(OriginalModalTutorialProfile profile)
        {
            while (IsInitialized && (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation()))
                yield return null;
            if (IsInitialized)
            {
                modalTutorial = Create<OriginalModalTutorialView>(config.modalTutorialPrefabPath);
                modalTutorial.Bind(profile, board, hud, cameraSource);
            }
            tutorialPending = false;
        }
        private T Create<T>(string path) where T : Component
        {
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing configured core dialog: " + path);
            var instance = Instantiate(prefab, cameraSource.transform);
            return instance.GetComponent<T>() ?? throw new InvalidOperationException("Core dialog component is missing: " + path);
        }
        private void Update()
        {
            if (!IsInitialized || transitioning) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PressBack();
                if (transitioning) return;
            }
            if (game != null) flow.ManualUpdate();
        }

        private bool BoosterGuideActive => (hammerTutorial != null && hammerTutorial.IsGuiding) ||
            (optionalBoosterTutorial != null && (optionalBoosterTutorial.IsGuiding || optionalBoosterTutorial.IsSelected));

        internal void UpdateMatchHints()
        {
            if (!IsInitialized || transitioning) return;
            matchHints.ManualUpdate(BlocksBoardInput || BoosterGuideActive ||
                (directedTutorial != null && directedTutorial.IsGuiding));
        }

        internal void UpdateDynamicTutorials()
        {
            if (!IsInitialized || transitioning) return;
            bool guideBusy = BlocksBoardInput || directedTutorial != null || BoosterGuideActive ||
                flow.Main.GetRunningAction() != null;
            UpdateLightballTapTutorial(guideBusy);
            if (dynamicPowerTutorials.TrySelect(guideBusy || directedTutorial != null, out var focus, out var other))
            {
                bool isCombo = other != null;
                directedTutorial = Create<OriginalDirectedTutorialView>(isCombo
                    ? config.dynamicComboTutorialPrefabPath : config.lightballTapTutorialPrefabPath);
                directedTutorial.Bind(null, game, board, hud, cameraSource, focus, other,
                    isCombo ? dynamicPowerTutorials.CompleteCombo : dynamicPowerTutorials.CompleteTap);
                // NewUserNewUxHelper.SetHasShownTutorialInLevel 0x1f71adc.
                attemptStore?.SaveImmediate(game, HasShownDynamicTutorial, UsedTutorialBooster);
            }
        }

        private void UpdateLightballTapTutorial(bool guideBusy)
        {
            if (lightballGuideShown) return;
            // DynamicLevelTutorialManager 0x218c084 resets the timer even between sparse ticks.
            var operations = board.State.Operations.Container;
            if (operations.HasAny())
            {
                if (!operations.HasMask(OriginalGameOperationContainer.FallAndSwapMask))
                    onlyLightballSince = -1f;
                return;
            }
            // DynamicLightballTapTutorialCreator 0x2246210 and 0x2246350.
            if (guideBusy || !game.CanUserMakeMove() || board.Clock.FrameCount % 10 != 0) return;
            if (!game.HasOnlyLightballMove)
            {
                onlyLightballSince = -1f;
                return;
            }
            float now = board.Clock.Time;
            if (onlyLightballSince < 0f) onlyLightballSince = now;
            if (now - onlyLightballSince < config.lightballTapTutorialDelay || !board.State.IsGameStable()) return;

            lightballGuideCells.Reset();
            OriginalGridCell cell;
            while ((cell = lightballGuideCells.MoveNext()) != null)
            {
                if (!(cell.Mediation.CurrentItem is OriginalLightballBoardItem) ||
                    !tutorialMoveEligibility.CanEnterPossibleMatch(cell) || HasSwappableMatchNeighbor(cell)) continue;
                directedTutorial = Create<OriginalDirectedTutorialView>(config.lightballTapTutorialPrefabPath);
                directedTutorial.Bind(null, game, board, hud, cameraSource, cell);
                lightballGuideShown = true;
                return;
            }
            onlyLightballSince = -1f;
        }

        private bool HasSwappableMatchNeighbor(OriginalGridCell cell)
        {
            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
            {
                var neighbor = cell.Neighbors.Get(direction);
                if (neighbor?.Mediation.CurrentItem is OriginalMatchBoardItem &&
                    OriginalCellTouchRules.CanSwapTo(neighbor, board.SwapAvailability)) return true;
            }
            return false;
        }

        public void Release()
        {
            if (!initialized) return;
            initialized = false;
            StopFadeOutSoundSourceCoroutine();
            if (audio != null) audio.StopGameMusic();
            StopAllCoroutines();
            entrance = null;
            matchHints?.Dispose();
            dynamicPowerTutorials?.Dispose();
            game?.ReleaseLevelFlow();
            game?.ReleaseWinFail(); game?.BindRemainingMovesManager(null);
            // Scene disposal can run after the camera has destroyed its children.
            // Retire the registry without scheduling close animations on those views.
            dialogs.HardReset();
            flow.Clear(); RemainingMoves?.Clear();
            DestroyView(logo);
            DestroyView(remaining);
            DestroyView(result);
            DestroyView(failure);
            DestroyView(OutOfMovesBanner);
            DestroyView(NoPossibleMovesDialog);
            DestroyView(LevelQuitDialog);
            if (bottomUi != null) bottomUi.LevelSettings?.Release();
            backdrops.Release();
            DestroyView(modalTutorial);
            if (directedTutorial != null) directedTutorial.Release();
            if (hammerTutorial != null) hammerTutorial.Release();
            if (optionalBoosterTutorial != null) optionalBoosterTutorial.Release();
            StopSoundInLoop(359);
            logo = null; remaining = null; result = null; failure = null;
            OutOfMovesBanner = null; NoPossibleMovesDialog = null;
            LevelQuitDialog = null;
            modalTutorial = null; directedTutorial = null; hammerTutorial = null; optionalBoosterTutorial = null;
            attemptStore = null;
            transitioning = failureStarted = outOfMovesStarted = uiDisabled = tutorialPending = lightballGuideShown = false;
            dialogInputLocks = 0;
            onlyLightballSince = -1f;
        }
        private void DestroyView(Component view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
        private void OnDestroy() => Release();
        public bool IsBonus => game.Session.IsBonus;
        public bool IsStory => false;
        public bool ShouldSpeedUpLevelEnd => !IsBonus;
        public bool CanUseMoveTokenInLevel => false;
        public float LevelTime => board.Clock.Time;
        public void OnWin()
        {
            if (!IsBonus) KingBeHappy(); // Native bonus boards celebrate in StartWinFlow instead.
            game.UpdateLowMatchRatios(game.CreateWinLowMatchRatios());
        }
        public void StartWinFlow() => win.StartWinFlow();
        public void TrySendRemainingMoves() => RemainingMoves.TrySendMoves();
        public int GetGoalLeftCount(int type, bool finalBoard) => board.Goals.GetGoalLeftCount(type, finalBoard);
        public void UpdateReactivationProgressAfterWin() { } // Reactivation is excluded.
        public void SessionWin(int coins) => game.Session.Win(game.Moves, coins, peripherals, peripherals);
        public void LevelEnd() => progress.CompleteLevel(game.Session.Number);
        public void MultiplierTokenOnStoryOrBonusLevelWin() { } // Core levels exclude the token inventory.
        public void KingBeHappy() => hud.KingVisual?.BeHappy(!IsBonus);
        public IEnumerator FadeOutSoundSource()
        {
            float start = audio.SoundSource.volume, elapsed = 0;
            while (elapsed < config.soundFadeDuration)
            {
                elapsed += Time.deltaTime;
                audio.SoundSource.volume = Mathf.Lerp(start, 0, Mathf.Clamp01(elapsed / config.soundFadeDuration));
                yield return null;
            }
            audio.SoundSource.volume = 0;
        }
        public Coroutine ManualStartCoroutine(IEnumerator routine) { fade = StartCoroutine(routine); return fade; }
        public void StopFadeOutSoundSourceCoroutine() { if (fade != null) StopCoroutine(fade); fade = null; }
        public void ActivateSoundSourceAfterFadeOut() { audio.SoundSource.volume = 1; }
        public OriginalFlowManager GetFlowManager() => flow;
        public OriginalFlowAction CreateWinLogoAction() => new OriginalShowWinLogoDialogAction(this);
        public OriginalFlowAction CreateRemainingMovesAction() => new OriginalRemainingMovesDialogAction(this);
        public OriginalFlowAction CreateLevelWinAction() => new OriginalShowLevelWinDialogAction(this);
        public bool ShouldDelayAutoActions() => false;
        public bool IsInDelayedActions(OriginalFlowAction action) => false;
        public void DelayAutoAction(OriginalFlowAction action) => throw new InvalidOperationException("Core flow has no peripheral auto actions.");
        public float LogoBeforeShowDelay => game.WinLogoBeforeShowDelay;
        public float LogoBeforePlayDelay => game.WinLogoBeforePlayDelay;
        public void DisableUiTouch() { uiDisabled = true; }
        public void EnableUiTouch() { uiDisabled = false; }
        public Coroutine StartApplicationCoroutine(IEnumerator routine) => StartCoroutine(routine);
        public IOriginalWinLogoDialog InstantiateWinLogoDialog(OriginalFlowAction action)
        {
            audio.StopGameMusic(); // WinLogoAnimationDialog.Awake 0x18d108c.
            logo = Create<OriginalWinLogoDialog>(config.winLogoPrefabPath);
            logo.Bind(action, dialogs, this);
            return logo;
        }
        public OriginalRemainingMovesManager GetRemainingMovesManager() => RemainingMoves;
        public IOriginalFlowDialog InstantiateRemainingMovesDialog(OriginalFlowAction action)
        {
            remaining = Create<OriginalRemainingMovesDialog>(config.remainingMovesPrefabPath);
            remaining.Bind(action, dialogs, this); return remaining;
        }
        public IOriginalFlowDialog InstantiateDefaultWinDialog(OriginalFlowAction action)
        {
            result = Create<OriginalLevelWinDialog>(config.winDialogPrefabPath);
            if (game == null) result.BindCompletion(action, dialogs, this, config.completedTitle, progress.LevelToPlay);
            else result.Bind(action, dialogs, this);
            result.SetContinueText(progress.IsComplete ? config.completedContinueText : config.continueText);
            return result;
        }
        public IOriginalLevelWinDialog InstantiateLeagueWinDialog(OriginalFlowAction action) => throw new InvalidOperationException("League is outside the first100 ordinary levels.");
        public bool IsHardOrExtremeLevelBundleAvailable => false;
        public bool IsLeagueBundleDownloaded => false;
        public AssetBundle TryLoadLeaguePopupBundle() => throw new InvalidOperationException("League is out of scope.");
        public void ResetTopUiSorting() => hud.ResetSorting();
        public OriginalActiveLevelData GetCurrentSession() => game.Session;
        public OriginalGoalManager Goals => board.Goals;
        public OriginalAutoExplodeManager Auto => game.Auto;
        public bool RewardReductionEnabled => false;
        public int Difficulty => game.Session.Difficulty;
        public int LeftMoves => game.Session.LeftMoves;
        public int CollectedCoins => game.Session.AnimationValues.Coin.GetCollectedAmount();
        public bool HasAnySpecialItemOnBoard()
        {
            for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
                if (board.Grid.GetCell(x,y)?.Mediation.CurrentItem is OriginalSpecialBoardItem) return true;
            return false;
        }
        public int CountItemOnBoard(int type)
        {
            int count = 0;
            for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
                if (board.Grid.GetCell(x,y)?.Mediation.CurrentItem?.ItemType == type) count++;
            return count;
        }
        public void RemainingItemCreationStarted() => game.RemainingItemCreationStarted();
        public OriginalRemainingMovesItemReplacer CreateMoveReplacer() => new OriginalRemainingMovesItemReplacer(
            new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid,board.Layout)),board.Presentation.Coroutines,
            new OriginalRemainingMovesReplacementServices(game,board,hud,audio));
        public OriginalRemainingMovesItemExploder CreateItemExploder() => new OriginalRemainingMovesItemExploder(
            new OriginalTopLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid,board.Layout)),game.State,board.Presentation.Coroutines,
            () => board.Clock.Time, () => board.Clock.DeltaTime, hud.ResetSorting);
        public OriginalRemainingMovesCoinCollectAnimation CreateCoinCollection() => new OriginalRemainingMovesCoinCollectAnimation(
            new OriginalRemainingMovesCoinServices(game,board,hud,audio.Playback));
        public void SessionRemainingMovesEnd(int notReplaced,int coins,Dictionary<int,int> specials)
            => game.Session.RemainingMovesEnd(notReplaced, coins, specials, peripherals);
        public void LevelRemainingMovesEnd() { } // No account/event rewards are persisted.
        public void StartExtraMoveFlow() => throw new InvalidOperationException("Paid/feature extra moves are excluded.");
        public void StartEgoFlow()
        {
            if (outOfMovesStarted || failureStarted) return;
            outOfMovesStarted = true;
            audio.StopGameMusic(true);
            hud.KingVisual?.BeSad();
            flow.Push(new OutOfMovesBannerAction(this));
        }

        internal void CompleteOutOfMovesBanner()
        {
            if (!initialized || transitioning) return;
            OutOfMovesBanner = null;
            EnableOrientation(35);
            flow.Clear(false);
            StartFailFlow();
        }
        public void StartNoPossibleMoveFlow() // 0x1ede668 and native session state 5 at 0x32b9eec.
        {
            if (failureStarted) return;
            failureStarted = true;
            game.Auto.Disable();
            game.Session.LeftMoves = game.Moves.LeftMoves;
            game.Session.MadeMoves = game.Moves.MadeMoves;
            game.Session.State = 5;
            audio.StopGameMusic();
            StopAllSounds();
            PlaySound(212, .04f);
            flow.Push(new NoPossibleMovesAction(this));
        }
        public void StartFailFlow()
        {
            if (failureStarted) return;
            failureStarted = true;
            game.State.SetStateToFail(); game.Auto.Disable();
            game.Session.LeftMoves = game.Moves.LeftMoves; game.Session.MadeMoves = game.Moves.MadeMoves; game.Session.State = 4;
            audio.StopGameMusic();
            if (!outOfMovesStarted)
            {
                StopAllSounds();
                hud.KingVisual?.BeSad();
            }
            PlaySound(212, .04f);
            flow.Push(new FailureAction(this));
        }
        // BackButtonListener.PressBack 0x2083d50: dialog, selected booster, then level quit.
        public void PressBack()
        {
            if (!IsInitialized || transitioning || dialogs.PressBack() || game == null) return;
            if (optionalBoosterTutorial != null && optionalBoosterTutorial.IsSelected)
            {
                optionalBoosterTutorial.Deselect();
                return;
            }
            if (!BlocksSettingsInput) QueueQuitConfirmation();
        }
        internal void OpenLevelSettings()
        {
            if (!IsInitialized || !game.State.IsPlaying() || BlocksSettingsInput) return;
            flow.Push(new LevelSettingsAction(this));
        }
        internal bool ShouldRetainSettingsShade => game.State.IsWin();
        internal void SetSettingsOrientation(bool enabled)
        {
            hud.SetOrientationEnabled(enabled);
            bottomUi.SetOrientationEnabled(enabled);
            if (enabled)
            {
                hud.RefreshLayout();
                bottomUi.RefreshLayout();
            }
        }
        internal void QueueQuitConfirmation()
        {
            if (game.State.IsPlaying()) flow.Push(new LevelQuitAction(this));
        }
        internal bool TryBeginLevelQuit()
        {
            if (!game.State.IsPlaying() || failureStarted) return false;
            // LevelQuitDialog.QuitAfterWarnings 0x1340ad8 publishes Quit before close.
            failureStarted = true;
            game.State.SetStateToQuit();
            game.Auto.Disable();
            return true;
        }
        internal void CompleteLevelQuit()
        {
            // StartQuitFlow 0x13411f0 and UserActiveLevelData.Quit 0x32ba50c.
            audio.StopGameMusic();
            game.Session.LeftMoves = game.Moves.LeftMoves;
            game.Session.MadeMoves = game.Moves.MadeMoves;
            game.Session.State = 6;
            if (game.Session.IsBonus)
            {
                // Core entry returns to the same unfinished bonus level.
                RetryLevel();
                return;
            }
            hud.KingVisual?.BeSad();
            PlaySound(212, .04f);
            flow.Push(new FailureAction(this));
        }
        internal void ClearQuitDialog() => LevelQuitDialog = null;
        public void SpendMovesOnLevelQuit() => throw new InvalidOperationException("Move tokens are excluded.");
        public void ReportOperationTimeout(int count,int state) => Debug.LogError("Core WinFail operation timeout: " + count + ", state " + state);
        public void DisableTouchForGame() => dialogInputLocks++;
        public void CloseTooltip(bool animate) { } // No shop/inventory tooltips.
        public void ShowBackground(int id, OriginalDialogConfig config, int offset, bool first) =>
            backdrops.Show(id, config, offset, first, config.ShouldCloseOnTouch ? dialogs.GetDialog(id) : null);
        public void HideBackground(int id, OriginalDialogConfig config, Action hidden) =>
            backdrops.Hide(id, config, hidden);
        public void EnableTouchForGame()
        {
            if (dialogInputLocks > 0) dialogInputLocks--;
        }
        internal void RefreshDialogBackgroundLayout()
        {
            backdrops?.Resize();
            if (logo != null) logo.RefreshTrumpetLayout();
        }
        // UiDialog.UpdateParent 0x25cb464 selects the scene's GameDialogScaler for inherited dialogs.
        public void UpdateDialogParent(Transform view, OriginalDialogConfig config)
        {
            if (config.ShouldInheritUiScale && dialogScaler == null)
                throw new InvalidOperationException("The scene must bind the original dialog scaler.");
            view.SetParent(config.ShouldInheritUiScale ? dialogScaler : cameraSource.transform, false);
        }
        public Vector3 GetCameraCenter() => cameraSource.transform.position;
        public bool IsDeviceSupportLandscape => false; // CameraManager.IsDeviceSupportLandscape 0x2002340.
        public float GameplayDeltaTime => board != null ? board.Clock.DeltaTime : Time.deltaTime;
        float IOriginalTweenClock.DeltaTime => GameplayDeltaTime;
        public void EnableOrientation(int reason) => hud?.SetOrientationEnabled(true);
        public void DisableOrientation(int reason) => hud?.SetOrientationEnabled(false);
        public void DisableHaptic() { } // Existing haptic transport remains absent; no SDK changes.
        public void EnableHaptic() { }
        public void SetDialogTouchSorting(int order) { } // Standard Canvas/GraphicRaycaster owns input ordering.
        public Camera UiCamera => cameraSource;
        internal Vector3 SafeCenter => sceneProjection != null
            ? sceneProjection.SafeCenter
            : OriginalGameSceneProjection.GetSafeCenter(cameraSource, Screen.safeArea);
        public Vector3 UiScale => Vector3.one;
        public bool IsLandscape => OriginalCameraProjection.IsLandscape;
        public long PlayWinLogoSound(int clipType) => audio.Playback.PlayStoppableSound(clipType, audio);
        public void StopWinLogoSound(long key) => audio.Playback.StopSound(key);
        public void RaiseWinLogoHud(int layer, int order) => hud.SetWinLogoSorting(layer, order);
        public bool HasRemainingMovesManager() => RemainingMoves != null;
        public void TriggerTapToSkipClick() => RemainingMoves.TriggerTapToSkipClick();
        public string GetTapToSkipText() => config.skipText;
        public Vector3 GetTapTextPosition()
        {
            // TapToSkip.ArrangeTapTextPosition 0x18d0b64 uses the midpoint
            // between the grid's bottom edge and BottomUi.GetTopCenterPosition.
            var left = board.Grid.GetCell(0, 0).GetViewPosition();
            var right = board.Grid.GetCell(board.Grid.Width - 1, 0).GetViewPosition();
            var gridBottom = (left + right) * .5f;
            gridBottom.y -= .5f;
            return (gridBottom + bottomUi.transform.position) * .5f;
        }
        public string GetLevelTitle(OriginalActiveLevelData session) => progress.IsComplete ? config.completedTitle : string.Format(config.levelTitleFormat,session.Number);
        public bool ShouldShowRewardedAd => false;
        public bool ShouldShowCloche => false;
        public bool ShouldShowWeeklyPanel => false;
        public void StopAllSounds() => audio.Playback.StopAllSounds();
        public void MarkLevelWinDialogShown() => audio.GameSfx.SetNoGameSound(true);
        public void PrepareBottomPanel(bool cloche,bool weekly) { if (cloche || weekly) throw new InvalidOperationException("Peripheral footer is excluded."); }
        public void ShowBottomPanel() => throw new InvalidOperationException("Peripheral footer is excluded.");
        public void ShowLeagueCrown() => throw new InvalidOperationException("League is excluded.");
        public void PlaySound(int id,float offset) => audio.Playback.PlaySound(id,offset);
        public void PlaySoundInLoop(int id) => audio.Playback.PlaySoundInLoop(id);
        public void StopSoundInLoop(int id) { if (audio != null && audio.Playback != null && audio.Playback.GetLoopCount(id)>0) audio.Playback.StopSoundInLoop(id); }
        public void EnableBottomPanelOrientation() { } // No peripheral footer.
        public IOriginalLevelWinAds GetAds() => this;
        public IOriginalLevelWinGameManager GetGameManager() => this;
        public IOriginalLevelWinLavaFlow GetLavaFlow() => this;
        public IOriginalLevelWinAncientFlow GetAncientFlow() => this;
        public void TryShowForCoin(Action<bool> done) => throw new InvalidOperationException("Rewarded economy UI is excluded.");
        public void TryShowAfterLevelWin(Action done) => done(); // Existing no-vendor SDK processing is retained.
        public bool TryPushLevelEndFlow(int state,Action done) => false;
        public bool TryPushLevelEndFlow(Action done,bool fail,bool won) => false;
        public void ExitGameWhenPossible()
        {
            if (transitioning) return;
            if (progress.IsComplete) progress.StartOver();
            BeginTransition();
            StartCoroutine(Reload());
        }
        public void RetryLevel() => RestartCurrentLevel(null);
        internal void RestartCurrentLevel(OriginalUiDialog dialog)
        {
            if (!initialized || transitioning || game == null) return;
            BeginTransition(dialog);
            // Native retry 0x1350338 and no-moves restart 0x15893d8 schedule the next attempt before closing.
            restartLevel();
            dialog?.Close();
        }
        private void BeginTransition(OriginalUiDialog restartingDialog = null)
        {
            transitioning = true;
            game?.State.SetStateToNoGame();
            if (restartingDialog == null) dialogs.ClearDialogs();
        }
        private IEnumerator Reload()
        {
            yield return null;
            SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
        }
        private sealed class SavedCompletionAction : OriginalFlowAction
        {
            private readonly CoreGameFlow owner;
            public SavedCompletionAction(CoreGameFlow owner) { this.owner = owner; }
            public override void Execute() => owner.InstantiateDefaultWinDialog(this).Show();
            public override void Cancel(OriginalFlowAction action) { }
        }
        private sealed class LevelSettingsAction : OriginalFlowAction
        {
            private readonly CoreGameFlow owner;
            public LevelSettingsAction(CoreGameFlow owner) { this.owner = owner; }
            public override int Type => 1;
            public override void Execute() => owner.bottomUi.LevelSettings.Open(this, owner.dialogs);
            public override void Cancel(OriginalFlowAction action) { }
        }
        private sealed class LevelQuitAction : OriginalFlowAction
        {
            private readonly CoreGameFlow owner;
            public LevelQuitAction(CoreGameFlow owner) { this.owner = owner; }
            public override int Type => 1;
            public override void Execute()
            {
                if (!owner.game.State.IsPlaying()) { Complete(); return; }
                owner.LevelQuitDialog = owner.Create<CoreLevelQuitDialog>(owner.config.levelQuitPrefabPath);
                owner.LevelQuitDialog.Open(this, owner.dialogs, owner);
            }
            public override void Cancel(OriginalFlowAction action) { }
        }
        private sealed class NoPossibleMovesAction : OriginalFlowAction
        {
            private readonly CoreGameFlow owner;
            public NoPossibleMovesAction(CoreGameFlow owner) { this.owner = owner; }
            public override void Execute()
            {
                owner.NoPossibleMovesDialog = owner.Create<CoreNoPossibleMovesDialog>(owner.config.noPossibleMovesPrefabPath);
                owner.NoPossibleMovesDialog.Bind(this, owner.dialogs, owner);
            }
            public override void Cancel(OriginalFlowAction action) { }
        }
        private sealed class OutOfMovesBannerAction : OriginalFlowAction
        {
            private readonly CoreGameFlow owner;
            public OutOfMovesBannerAction(CoreGameFlow owner) { this.owner = owner; }
            public override int Type => 2;
            public override void Execute()
            {
                var banner = owner.Create<CoreOutOfMovesBanner>(owner.config.outOfMovesBannerPrefabPath);
                owner.OutOfMovesBanner = banner;
                banner.Bind(this, owner.dialogs, owner);
                banner.Show();
            }
            public override void Cancel(OriginalFlowAction action) { }
        }
        private sealed class FailureAction : OriginalFlowAction
        {
            private readonly CoreGameFlow owner;
            public FailureAction(CoreGameFlow owner) { this.owner = owner; }
            public override void Execute()
            {
                owner.failure = owner.Create<CoreLevelFailDialog>(owner.config.failDialogPrefabPath);
                owner.failure.Bind(this,owner.dialogs,owner); owner.failure.Show();
            }
            public override void Cancel(OriginalFlowAction action) { }
        }
    }
}
