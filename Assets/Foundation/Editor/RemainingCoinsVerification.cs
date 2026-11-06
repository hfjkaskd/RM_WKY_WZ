using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Whitebox.Foundation.Editor
{
    public static class RemainingCoinsVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameplayHud hud;
        private static OriginalGameAudioHost audio;
        private static OriginalRemainingMovesManager manager;
        private static Services services;
        private static bool goalsDone,skipped,captured;
        private static int variant,frame,phase,startMoves,peakPilots,peakCoins,peakOperations,resultClickFrame;
        private static float started,completedAt,goalsFinishedAt;
        private static string VariantName=>variant==0?"normal":variant==1?"skip":variant==2?"early_skip":"logo_skip";
        private static string PathName=>Path.Combine(Application.dataPath,"../live_remaining_coins_"+VariantName+"_verification.json");
        public static void RunNormal()=>Run(0);
        public static void RunSkip()=>Run(1);
        public static void RunEarlySkip()=>Run(2);
        public static void RunLogoSkip()=>Run(3);
        private static void Run(int useVariant)
        {
            variant=useVariant;board=UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();game=UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            hud=UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();audio=UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            services=new Services();manager=new OriginalRemainingMovesManager(services);services.Manager=manager;
            game.BindRemainingMovesManager(manager);game.BindWinFail(services);
            File.WriteAllText(PathName,"{\"passed\":false,\"status\":\"running\"}\n");
            GoalUiVerification.OnFinished+=GoalsFinished;GoalUiVerification.Run();EditorApplication.update+=Tick;
        }
        private static void GoalsFinished(){goalsDone=true;goalsFinishedAt=Time.realtimeSinceStartup;}
        private static void Tick()
        {try{Step();}catch(Exception e){File.WriteAllText(PathName,JsonUtility.ToJson(new Report{failure=e.ToString()},true));Cleanup();throw;}}
        private static void Step()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            services.ManualUpdate();
            if(!goalsDone)return;
            if(phase==3){FinishAfterContinue();return;}
            if(phase==0)
            {
                Require(Time.realtimeSinceStartup-goalsFinishedAt<30f,"Automatic win gate timeout: flow="+services.WinFlowCalls+",started="+game.WinFail.DidStartWinFlow+",state="+game.State.GetGameState()+",ops="+game.State.HasAnyOperation()+",ready="+manager.IsRemainingMovesReady+",active="+manager.IsRemainingMovesActive);
                if(services.WinFlowCalls==0)return;
                // Actual goal events and permanent unit16 already set Win,
                // dispatched OnWin and invoked Session.Win at the check gate.
                Require(game.Session.State==3&&game.Session.WonAnyLevel&&game.Session.LeftMoves==game.Moves.LeftMoves&&
                    game.Session.MadeMoves==game.Moves.MadeMoves&&game.Session.Star.GetCollectedAmount()==1&&services.WinContext.ClocheCalls==1,
                    "Actual Session.Win captures moves/state, awards first-board star and invokes the cloche boundary once");
                Require(game.State.IsWin()&&services.WinTriggerCalls==1&&services.WinFlowCalls==1&&
                    services.KingStopCalls==1&&services.IncrementedLives==1&&services.EgoResetCalls==1&&
                    game.IsLowMatchEnabled&&game.LowMatchTotalRatio==100,
                    "Actual WinFail goal subscription, OnWin side-effect order, low-match owner and stable/check gates execute once");
                startMoves=game.Session.LeftMoves;
                Require(startMoves>2,"Formal goal scenario retains an actual move budget for conversion and skip");
                // Native fast-end can show the logo while nonblocking board
                // operations still run. Remaining readiness may arrive before
                // OR after the dialog; do not force either scheduling order.
                Require((services.LogoClosed==0&&services.Flow.Main.GetRunningAction() is OriginalShowWinLogoDialogAction&&services.Flow.Main.PendingCount==2)||
                    (services.LogoClosed==1&&services.RemainingShown==1),
                    "Native queue waits on actual logo close before handing off remaining moves");
                started=Time.realtimeSinceStartup;phase=1;return;
            }
            Require(Time.realtimeSinceStartup-started<120f,"Actual move replacement, special damage, coin flights and native tail recycling complete");
            if(variant==0&&!services.FirstLevelSkipRejected&&services.IsSkipAttached)
            {ClickSkipButton();Require(!manager.IsTapToSkipClicked,"Original first level rejects Button clicks");services.FirstLevelSkipRejected=true;}
            if(variant==3){StepLogoSkip();return;}
            if(services.RemainingShown==0)return;
            Require(manager.Replacer.StartMoves==startMoves,"Replacer captures the actual completed level budget after the logo action");
            int pilots=board.Presentation.Pool.GetSpawnedCount(76),coins=board.Presentation.Pool.GetSpawnedCount(68),operations=game.State.Operations.Container.GetCount(5);
            peakPilots=Math.Max(peakPilots,pilots);peakCoins=Math.Max(peakCoins,coins);peakOperations=Math.Max(peakOperations,operations);
            if(!captured&&(coins>0||(variant==2&&board.Presentation.Pool.GetSpawnedCount(77)>0)))
            {Capture("remaining_coins_"+VariantName+"_flying.png");captured=true;}
            if(!skipped&&((variant==1&&operations>0)||(variant==2&&manager.Replacer.ReplacedMoves>=2&&!manager.Replacer.IsComplete)))
            {ClickSkipButton();Require(manager.IsTapToSkipClicked,"Actual standard Button dispatches native skip entry");skipped=true;}
            if(services.EndCalls==0)return;
            if(phase==1){phase=2;completedAt=Time.time;}
            if(Time.time-completedAt<2.2f||pilots!=0||coins!=0||game.State.HasAnyOperation()||!game.Fall.IsAllItemsSteady||board.Presentation.Coroutines.Count!=0)return;
            Require(services.EndCalls==1&&services.SessionCalls==1&&services.CreationStartedCalls==1&&services.Closed==1&&services.SkipDestroyed==1,"Native completion/account/item-creator boundaries/dialog/skip teardown occur once");
            Require(services.LogoClosed==1&&services.ResultShown==1&&services.RemainingShown==1&&services.ReactivationCalls==1&&
                services.LevelEndCalls==1&&services.DownloadCalls==1&&services.TutorialResetCalls==1&&services.MultiplierWinCalls==1&&
                services.AnalyticsCalls==1&&services.UserEndCalls==1&&services.TouchDisableCalls==1&&services.TouchEnableCalls==1&&
                services.Flow.Main.GetRunningAction() is OriginalShowLevelWinDialogAction&&services.Flow.Main.PendingCount==0,
                "Actual native win coordinator, level-end dispatch and queued dialog actions reach the explicit final dialog owner once");
            Require(!manager.IsRemainingMovesActive&&!manager.IsRemainingMovesReady&&manager.SkipView==null,"Remaining phase and readiness release");
            Require(captured,"Actual native remaining-move or coin flights are visible");
            if(variant!=2)Require(manager.CollectionCount>0&&peakOperations>0&&peakCoins>0&&peakPilots>0,"Actual production special hooks create pooled coin pilots and operation5 ownership");
            Require(game.State.GetSpecialOperationCount()==0,"All remaining special operations release");
            Require(game.Session.AnimationValues.Coin.GetCollectedAmount()==manager.ExtraCoinAmount&&board.Goals.GetGoalLeftCount(21)==manager.ExtraCoinAmount,
                "Native manager accounting reconciles real coin model and explicit account boundary");
            Require(game.Session.ExtraCoins==manager.ExtraCoinAmount&&services.SuperLookups==3,
                "Actual Session.RemainingMovesEnd stores extraCoins and resolves all three native super-lightball lookups");
            // Native IncreaseGoalUi0x3214158 does not decrement the flying
            // dictionary. Coin21 retains its departure count; operation5 and
            // the pilot pool, rather than that dictionary, own pending flights.
            Require(board.Goals.GetGoalUiLeftCount(21)==manager.ExtraCoinAmount&&hud.goalPanel.Views[0].countText.text==manager.ExtraCoinAmount.ToString(),
                "Every actual pilot arrival reaches reward UI exactly once: model="+manager.ExtraCoinAmount+",ui="+board.Goals.GetGoalUiLeftCount(21)+",text="+hud.goalPanel.Views[0].countText.text);
            Require(board.Goals.GetFlyingGoalCount(21)==services.GoalCountAtEnd-10,"Native reward flying counter retains exactly the emitted coin count");
            Require(manager.ExtraCoinAmount==10+board.Goals.GetFlyingGoalCount(21)+Mathf.FloorToInt(services.NotReplaced*2.5f)+services.BoardCoinsAtEnd,
                "Final reward includes native base, every emitted coin, unsent-move compensation and active unexploded board specials");
            Require(game.Session.LeftMoves==startMoves&&game.Moves.LeftMoves==startMoves&&!game.CanUserMakeMove(),"Conversion preserves the actual model budget and Win state blocks further player moves");
            Require(!game.WinFail.IsEnabled,"Stable-board gate eventually releases the native win manager");
            Require(manager.ExtraCoinAmount>=10&&manager.GetInitialCoinAmount()==10,"Normal first-board native base reward10");
            var account=services.Account;
            Require(account.User.Basic.Level==account.InitialLevel+1&&account.StoredLevel==account.User.Basic.Level&&account.LevelChangedCalls==1&&
                account.User.Inventory.Star==game.Session.Star.After&&account.StoredStar==game.Session.Star.After&&account.StarsUpdatedCalls==1&&
                account.User.Inventory.Coin==account.InitialCoin+game.Session.AnimationValues.Coin.GetCollectedAmount()&&account.StoredCoin==account.User.Inventory.Coin&&
                account.CoinUpdatedCalls==1&&account.SaveClearCalls==1&&account.FeatureEndCalls==2&&account.EpisodeWinCalls==1&&account.NewUxWinCalls==1&&
                account.StatsWinCalls==1&&account.EventInfoCalls==1&&account.EventWinCalls==1&&account.RemainingEventCalls==1&&account.RemainingMissionCalls==1&&
                account.BackendCalls==1&&account.RewardAnalyticsCalls==1,
                "Actual native user-end phases increment account level and absolute stars, grant remaining coins once, and preserve storage/event/backend fixture dispatch order");
            if(variant==2)Require(skipped&&manager.IsTapToSkipClicked&&manager.Replacer.ReplacedMoves<startMoves&&services.NotReplaced==startMoves-manager.Replacer.ReplacedMoves,"Early skip preserves partial conversion and accounts every unsent move");
            else Require(manager.Replacer.ReplacedMoves==startMoves&&services.NotReplaced==0&&hud.moveText.text=="0","Normal replacement converts the entire actual move budget before coin collection");
            if(variant==1)Require(skipped&&manager.IsTapToSkipClicked,"Late skip completes actual in-flight coin callbacks");
            account.VerifyPersisted();
            Capture("remaining_coins_"+VariantName+"_reward.png");
            services.VerifyResultButton();
            phase=3;resultClickFrame=Time.frameCount;
        }
        private static void WriteNormalReport()
        {
            var account=services.Account;
            File.WriteAllText(PathName,JsonUtility.ToJson(new Report{passed=true,skipped=variant!=0,initial_remaining_moves=startMoves,converted_moves=manager.Replacer.ReplacedMoves,
                unconverted_moves=services.NotReplaced,reward_coins=manager.ExtraCoinAmount,collections=manager.CollectionCount,
                peak_coin_views=peakCoins,peak_pilots=peakPilots,peak_operation5=peakOperations,native_departure_count=board.Goals.GetFlyingGoalCount(21),
                active_board_coin_value_at_end=services.BoardCoinsAtEnd,unconverted_move_coin_value=Mathf.FloorToInt(services.NotReplaced*2.5f),
                session_star_reward=game.Session.Star.GetCollectedAmount(),session_win_state=game.Session.State,native_win_cloche_calls=services.WinContext.ClocheCalls,
                win_trigger_calls=services.WinTriggerCalls,win_flow_calls=services.WinFlowCalls,low_match_total=game.LowMatchTotalRatio,
                logo_closed=services.LogoClosed,logo_runtime_dialog_lifecycle_verified=true,remaining_runtime_dialog_lifecycle_verified=true,skip_button_entry_verified=variant!=0,first_level_skip_rejected=services.FirstLevelSkipRejected,skip_ui_level=variant==0?1:2,remaining_dialog_shown=services.RemainingShown,result_dialog_owner_shown=services.ResultShown,
                result_prefab_registration_verified=true,result_continue_button_verified=true,result_exit_requests=services.ResultExitRequests,
                actual_game_context_deactivation_verified=true,actual_level_pool_recycled=true,inactive_level_updates_stopped=true,home_scene_load_requests=1,actual_game_exit_manager_verified=true,result_dialog_cleared=true,result_view_destroyed=true,result_flow_completed=true,game_state_after_continue=game.State.GetGameState(),
                level_end_calls=services.LevelEndCalls,reactivation_calls=services.ReactivationCalls,
                account_level=account.User.Basic.Level,account_star=account.User.Inventory.Star,account_coin=account.User.Inventory.Coin,
                account_coin_update_calls=account.CoinUpdatedCalls,account_star_update_calls=account.StarsUpdatedCalls,
                remaining_dialog_started_ready=services.RemainingStartedReady,account_database_reopen_verified=account.DatabaseReopenVerified,account_database_path=account.DatabasePath,
                account_native_reload_verified=account.NativeLocalReloadVerified,account_initial_coin=account.InitialCoin,account_area_load_calls=account.AreaLoadCalls,account_area_data_restored=account.AreaDataRestored,
                runtime_storage_factory_verified=account.RuntimeStorageFactoryVerified,directory_rules_verified=account.DirectoryRulesVerified,
                actual_life_entry_debit_verified=account.LifeEntryDebitVerified,actual_win_life_refund_verified=account.WinLifeRefundVerified,
                account_life_reopen_verified=account.LifeReopenVerified,entry_life_timestamp=account.EntryLifeTimestamp,win_life_timestamp=account.LoadedUser.Basic.FullLivesTimeInMs,
                elapsed=Time.realtimeSinceStartup-started},true)+"\n");Cleanup();
        }
        private sealed class Services:IOriginalRemainingMovesManagerServices,
            IOriginalRemainingSessionContext,IOriginalDailyLightningRushSessionManager,IOriginalClashSessionManager,IOriginalClashSessionHelper,IOriginalClashScoreConfig,
            IOriginalWinFailServices,IOriginalWinTransitionContext,IOriginalWinFlowContext,IOriginalLevelEndContext,
            IOriginalFlowExecutorContext,IOriginalWinLogoActionContext,IOriginalRemainingMovesActionContext,IOriginalLevelWinActionContext,IOriginalLevelRemainingEndContext, IOriginalDialogManagerContext, IOriginalWinLogoDialogContext, IOriginalRemainingMovesDialogContext,
            IOriginalLevelWinDialogContext,IOriginalLevelWinAds,IOriginalLevelWinLavaFlow,IOriginalLevelWinAncientFlow,IOriginalGameExitUiContext,
            IOriginalGameContextDeactivationServices,IOriginalGameContextItemCleanup,IOriginalGameContextTouchEnabler,IOriginalGameContextSceneLoader
        {
            public OriginalRemainingMovesManager Manager;public int SessionCalls,EndCalls,CreationStartedCalls,Closed,SkipDestroyed,NotReplaced,GoalCountAtEnd,BoardCoinsAtEnd;
            public int SuperLookups;
            public int WinTriggerCalls,WinFlowCalls,KingStopCalls,IncrementedLives,EgoResetCalls;
            public int LogoClosed,RemainingShown,ResultShown,ReactivationCalls,LevelEndCalls,DownloadCalls,TutorialResetCalls,MultiplierWinCalls,AnalyticsCalls,UserEndCalls,TouchDisableCalls,TouchEnableCalls,KingHappyCalls;
            public readonly OriginalFlowManager Flow;
            private readonly OriginalWinFlowCoordinator coordinator;
            private OriginalWinLogoDialog logoDialog;
            private readonly OriginalDialogManager dialogManager;
            public int LogoShown, LogoCloseAudio, LogoShowHaptic, LogoHideHaptic, DialogBackgroundShows, DialogBackgroundHides, LogoStopSound;
            public float LogoTime=>logoDialog!=null?logoDialog.AnimationTime:0;
            private OriginalRemainingMovesDialog remainingDialog;
            private OriginalLevelWinDialog resultDialog;
            private readonly OriginalGameExitManager exitManager;
            private readonly OriginalGameExitUi exitUi;
            private readonly OriginalContextUpdateHost updates;
            private readonly OriginalGameTouchInput input;
            private readonly OriginalContextContainer levelUnits;
            private readonly OriginalGameContextRuntimeState publication;
            private readonly OriginalGameContextDeactivation contextExit;
            private readonly IOriginalGameContextUnit[] gameUnits;
            private int deactivations,clockFrameAtDeactivate,homeLoadRequests,systemMessageRequests,unitUpdates,unitUpdatesAtDeactivate,poolRecycleCalls;
            public int ResultExitRequests,ResultClosed,ResultAdRequests,ResultLoopStarts,ResultLoopStops,ResultOrientationLocks,ResultOrientationUnlocks;
            private int landscapeFlagsCalls,homeBackgroundCalls,slidingTextHides;
            private readonly OriginalActiveLevelData tapSession;
            public bool FirstLevelSkipRejected;
            public bool IsSkipAttached=>Manager.SkipView is OriginalTapToSkipView;
            public OriginalTapToSkipView SkipView=>Manager.SkipView as OriginalTapToSkipView;
            public readonly SessionWinFixtureContext WinContext;
            public readonly UserEndFixtureContext Account;
            public bool RemainingStartedReady;
            private readonly OriginalRemainingSessionEvents sessionEvents;
            public Services()
            {
                // Explicit locked-event owners for this first-board fixture.
                // Actual production snapshot classes and reward gates run;
                // these owners do not claim account/event unlock composition.
                Account=new UserEndFixtureContext(game);
                WinContext=new SessionWinFixtureContext(game){Inventory=Account.User.Inventory};
                Flow=new OriginalFlowManager(this);coordinator=new OriginalWinFlowCoordinator(this);
                tapSession=variant==0?game.Session:new OriginalActiveLevelData(new OriginalSlbMoveConfig(
                    ()=>throw new NotSupportedException("Tap-only session does not read SLB."),
                    ()=>throw new NotSupportedException("Tap-only session does not read storage.")),game){Number=2};
                dialogManager=new OriginalDialogManager(this);
                dialogManager.OnDialogShown+=dialog=>{Require(dialog.IsShowing&&!dialog.IsClosing,"Native show flag precedes manager event");if(logoDialog!=null&&dialog==logoDialog.Lifecycle)LogoShown++;else if(resultDialog!=null&&dialog==resultDialog.Lifecycle)ResultShown++;else RemainingShown++;};
                dialogManager.OnDialogClosed+=dialog=>{Require(dialog.IsShowing&&dialog.IsClosing,"Native manager close event precedes final flag reset");if(logoDialog!=null&&dialog==logoDialog.Lifecycle)LogoClosed++;else if(resultDialog!=null&&dialog==resultDialog.Lifecycle)ResultClosed++;else Closed++;};
                exitUi=new OriginalGameExitUi(this);
                var loading=new OriginalSceneHomeLoadingDecision(()=>IsStory,()=>game.Session.IsTableGame,
                    ()=>false,()=>throw new NotSupportedException("Ordinary first-board fixture has no Lava Home-area lookup."));
                exitManager=new OriginalGameExitManager(game.State,loading,()=>exitUi);
                exitManager.OnGameExit+=()=>{Require(game.State.IsNoGame()&&!exitManager.ShouldExitGame&&ResultClosed==1&&!dialogManager.HasActiveDialog,"Native exit event follows actual state/UI/dialog clear and precedes shouldExitGame write");ResultExitRequests++;};
                updates=UnityEngine.Object.FindObjectOfType<OriginalContextUpdateHost>();input=UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();levelUnits=updates.Context;
                Require(levelUnits!=null&&input.Listener!=null,"Bind actual live units/input before the exit dispatcher");
                publication=new OriginalGameContextRuntimeState{Controller=board,IsActive=true};
                gameUnits=new IOriginalGameContextUnit[]{new BoundGameUnit(0,flag=>game.TouchListener.Clear(flag)),new BoundGameUnit(1,exitManager.Clear),new BoundGameUnit(2,ClearBoundLevelOwners)};
                contextExit=new OriginalGameContextDeactivation(publication,exitManager,updates.LateUpdates,this);
                contextExit.OnDeactivate+=()=>{Require(!publication.IsActive&&publication.Controller!=null&&exitManager.ShouldExitGame,"Native inactive publication precedes deactivation event/reset");deactivations++;clockFrameAtDeactivate=board.Clock.FrameCount;unitUpdatesAtDeactivate=unitUpdates;};
                updates.BindGameContextExit(contextExit);
                var data=game.Session.AnimationValues.StartSnapshots;
                var lightning=new OriginalDailyLightningRushLevelData(()=>this,
                    ()=>throw new NotSupportedException("No active event bundle in this fixture."),
                    ()=>throw new NotSupportedException("No active event pool in this fixture."),()=>game.Session,()=>false);
                lightning.Init(game.Session.Difficulty);data.DailyLightningRush=lightning;
                data.PropellerRush=new OriginalPropellerRushLevelData(()=>game,()=>false,()=>game.Session);
                data.PropellerRush.Init(0,game.Session.Difficulty);
                var clash=new OriginalClashOfChampionsLevelData(()=>this,
                    ()=>throw new NotSupportedException("No active event bundle in this fixture."),
                    ()=>throw new NotSupportedException("No active event pool in this fixture."),
                    (bundle,name)=>throw new NotSupportedException("No active event art in this fixture."),()=>1,()=>game.Session);
                clash.Init(game.Session.Difficulty,false);data.ClashOfChampions=clash;
                sessionEvents=new OriginalRemainingSessionEvents(game.Session,this);
            }
            public OriginalGoalManager Goals=>board.Goals;public OriginalAutoExplodeManager Auto=>game.Auto;
            public bool IsBonus=>game.Session.IsBonus;public bool IsStory=>game.Session.StoryLevel<0;public bool ShouldSpeedUpLevelEnd=>!IsBonus;public bool RewardReductionEnabled=>false;
            public int Difficulty=>game.Session.Difficulty;public int LeftMoves=>game.Session.LeftMoves;public int CollectedCoins=>game.Session.AnimationValues.Coin.GetCollectedAmount();
            public bool HasAnySpecialItemOnBoard(){for(int y=0;y<board.Grid.Height;y++)for(int x=0;x<board.Grid.Width;x++)if(board.Grid.GetCell(x,y).Mediation.CurrentItem is OriginalSpecialBoardItem)return true;return false;}
            public int CountItemOnBoard(int type){int count=0;for(int y=0;y<board.Grid.Height;y++)for(int x=0;x<board.Grid.Width;x++)if(board.Grid.GetCell(x,y).Mediation.CurrentItem?.ItemType==type)count++;return count;}
            // Item-creator/LevelManager/account/dialog owners remain recorded
            // boundaries. Session data and locked-event reward gates are real.
            public void RemainingItemCreationStarted(){CreationStartedCalls++;game.RemainingItemCreationStarted();}
            public bool CanUseMoveTokenInLevel=>false;public float LevelTime=>board.Clock.Time;
            public void OnWin(){WinTriggerCalls++;OriginalWinTransition.OnWin(this);}
            public void StartWinFlow()
            {
                WinFlowCalls++;coordinator.StartWinFlow();
            }
            public int GetGoalLeftCount(int type,bool finalBoard)=>board.Goals.GetGoalLeftCount(type,finalBoard);
            public void UpdateReactivationProgressAfterWin(){ReactivationCalls++;}
            public void SessionWin(int coins)=>game.Session.Win(game.Moves,coins,WinContext,new OriginalSessionWinRewards(game.Session.AnimationValues,WinContext));
            public void LevelEnd(){LevelEndCalls++;OriginalLevelEnd.End(this);}
            public OriginalActiveLevelData GetCurrentSession()=>game.Session;
            public void IncreaseUselessMoveCount(OriginalActiveLevelData session){Require(session==game.Session,"Current session identity");game.IncreaseUselessMoveCount();}
            public void TryDownloadEpisodeLevelBundle(){DownloadCalls++;}
            public void ResetPrelevelTutorial(){TutorialResetCalls++;}
            public void MultiplierSelectionOnLevelWin(){MultiplierWinCalls++;}
            public void SendLevelEndAnalytics(){AnalyticsCalls++;}
            public void UserLevelEnd(){UserEndCalls++;OriginalUserLevelEnd.LevelEnd(Account);}
            public void UserRemainingMovesEnd()=>OriginalUserLevelEnd.RemainingMovesEnd(Account);
            public void SendLevelEndRewardAnalytics(object data){Require(data==null,"Remaining manager passes the native null multiplier payload");Account.RewardAnalyticsCalls++;}
            public void MultiplierTokenOnStoryOrBonusLevelWin()=>throw new InvalidOperationException("Ordinary fixture has no bonus token dispatch.");
            public IEnumerator FadeOutSoundSource(){yield break;} // Explicit audio owner boundary, not a production fade implementation.
            public Coroutine ManualStartCoroutine(IEnumerator routine)=>board.StartCoroutine(routine);
            public OriginalFlowManager GetFlowManager()=>Flow;
            public OriginalFlowAction CreateWinLogoAction()=>new OriginalShowWinLogoDialogAction(this);
            public OriginalFlowAction CreateRemainingMovesAction()=>new OriginalRemainingMovesDialogAction(this);
            public OriginalFlowAction CreateLevelWinAction()=>new OriginalShowLevelWinDialogAction(this);
            public bool ShouldDelayAutoActions()=>false;
            public bool IsInDelayedActions(OriginalFlowAction action)=>false;
            public void DelayAutoAction(OriginalFlowAction action)=>throw new InvalidOperationException("Ordinary fixture has no auto action.");
            public float LogoBeforeShowDelay=>game.WinLogoBeforeShowDelay;
            public float LogoBeforePlayDelay=>game.WinLogoBeforePlayDelay;
            public void DisableUiTouch(){TouchDisableCalls++;}
            public void EnableUiTouch(){TouchEnableCalls++;}
            public Coroutine StartApplicationCoroutine(IEnumerator routine)=>board.StartCoroutine(routine);
            public IOriginalWinLogoDialog InstantiateWinLogoDialog(OriginalFlowAction action)
            {
                var prefab=Resources.Load<GameObject>("Foundation/Prefabs/OriginalWinLogoVisual");
                Require(prefab!=null,"Original configured logo prefab exists");
                logoDialog=UnityEngine.Object.Instantiate(prefab).GetComponent<OriginalWinLogoDialog>();
                Require(logoDialog!=null,"Original prefab carries a runtime dialog owner");
                logoDialog.Bind(action,dialogManager,this);
                logoDialog.TapToSkip.DestroyRequested+=()=>SkipDestroyed++;
                return logoDialog;
            }
            public OriginalRemainingMovesManager GetRemainingMovesManager()=>Manager;
            public IOriginalFlowDialog InstantiateRemainingMovesDialog(OriginalFlowAction action)
            {
                Require(LogoClosed==1&&LogoShown==1&&!dialogManager.HasActiveDialog&&LogoCloseAudio==1&&LogoShowHaptic==1&&LogoHideHaptic==1,"Remaining view follows the native logo close protocol");
                var prefab=Resources.Load<GameObject>("Foundation/Prefabs/OriginalRemainingMovesDialog");
                remainingDialog=UnityEngine.Object.Instantiate(prefab).GetComponent<OriginalRemainingMovesDialog>();
                Require(remainingDialog!=null,"Configured runtime remaining dialog");
                RemainingStartedReady=manager.IsRemainingMovesReady;board.Presentation.PrepareRemainingMovesParticles();
                remainingDialog.Bind(action,dialogManager,this);return remainingDialog;
            }
            public void ResetTopUiSorting()=>hud.ResetSorting();
            public IOriginalFlowDialog InstantiateDefaultWinDialog(OriginalFlowAction action)
            {
                Require(Closed==1||(Manager.IsTapToSkipClicked&&RemainingShown==0),"Result follows remaining close or native logo shortcut");
                var prefab=Resources.Load<GameObject>("Foundation/Prefabs/OriginalLevelWinDialog");
                Require(prefab!=null,"Recovered result prefab exists");
                resultDialog=UnityEngine.Object.Instantiate(prefab).GetComponent<OriginalLevelWinDialog>();
                Require(resultDialog!=null,"Result prefab owns its runtime lifecycle and code-bound buttons");
                resultDialog.Bind(action,dialogManager,this);return resultDialog;
            }
            public IOriginalLevelWinDialog InstantiateLeagueWinDialog(OriginalFlowAction action)=>throw new InvalidOperationException("First board is not league.");
            public bool IsHardOrExtremeLevelBundleAvailable=>throw new InvalidOperationException("First board does not query league bundles.");
            public bool IsLeagueBundleDownloaded=>throw new InvalidOperationException("First board does not query league bundles.");
            public AssetBundle TryLoadLeaguePopupBundle()=>throw new InvalidOperationException("First board does not query league bundles.");
            public void ManualUpdate(){Account.Database.ManualUpdate(Time.frameCount);Account.AppDatabase.ManualUpdate(Time.frameCount);Flow.ManualUpdate();}
            public void Dispose(){updates.ReleaseContext(levelUnits);Account.Dispose();if(logoDialog!=null)UnityEngine.Object.Destroy(logoDialog.gameObject);if(remainingDialog!=null)UnityEngine.Object.Destroy(remainingDialog.gameObject);if(resultDialog!=null)UnityEngine.Object.Destroy(resultDialog.gameObject);}
            // Explicit scene/backdrop/audio owners; the runtime dialog lifecycle,
            // registration, native timer and FlowAction completion are real.
            public void DisableTouchForGame(){}
            public void CloseTooltip(bool animate){Require(!animate,"Native tooltip close argument");}
            public void ShowBackground(int id,OriginalDialogConfig config,int offset,bool first)
            {Require(id>=1&&id<=3&&offset==0&&first&&config.ShouldShowBackgroundOnClose==(id==1),"Native logo/remaining/result registration/config");DialogBackgroundShows++;}
            public void HideBackground(int id,OriginalDialogConfig config,Action hidden)
            {Require(id>=1&&id<=3&&dialogManager.HasActiveDialog,"Close keeps registration until DialogClosed");DialogBackgroundHides++;hidden();}
            public void EnableTouchForGame(){}
            public void UpdateDialogParent(Transform view,OriginalDialogConfig config)
            {view.SetParent(UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().PointerCamera.transform);}
            public Vector3 GetCameraCenter()=>UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().PointerCamera.transform.position;
            public bool IsDeviceSupportLandscape=>true; // Explicit landscape-capable camera owner in this fixture.
            public float GameplayDeltaTime=>board.Clock.DeltaTime;
            public void EnableOrientation(int reason){Require(reason==38||reason==37,"Native dialog orientation reason");if(reason==37)ResultOrientationUnlocks++;}
            public void DisableHaptic(){LogoShowHaptic++;}
            public void ActivateSoundSourceAfterFadeOut(){LogoCloseAudio++;}
            public void EnableHaptic(){LogoHideHaptic++;}
            public void SetDialogTouchSorting(int order){Require(order==0||order==-11,"Native dialog touch ordering");}
            public Camera UiCamera=>UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().PointerCamera;
            public Vector3 UiScale=>Vector3.one;
            public bool IsLandscape=>true;
            public long PlayWinLogoSound(int clipType) => 0; // Explicit audio boundary.
            public void StopWinLogoSound(long key) { LogoStopSound++; }
            public void RaiseWinLogoHud(int layer, int order) { } // This fixture uses landscape.
            OriginalActiveLevelData IOriginalTapToSkipContext.GetCurrentSession()=>tapSession;
            public bool HasRemainingMovesManager()=>Manager!=null;
            public void TriggerTapToSkipClick()=>Manager.TriggerTapToSkipClick();
            public string GetTapToSkipText()=>"Tap to Skip"; // Explicit English localization owner.
            public Vector3 GetTapTextPosition()=>(board.Grid.GetCell(0,0).GetViewPosition()+board.Grid.GetCell(board.Grid.Width-1,0).GetViewPosition())*.5f;
            public string GetLevelTitle(OriginalActiveLevelData session)=>"Level "+session.Number; // Explicit English localization boundary.
            public bool ShouldShowRewardedAd=>false; // Explicit first-board ad eligibility boundary; no SDK changes.
            public bool ShouldShowCloche=>false;
            public bool ShouldShowWeeklyPanel=>false;
            public void StopAllSounds()=>audio.SoundSource.Stop(); // Remaining AudioManager pool/loop shutdown is still a boundary.
            public void MarkLevelWinDialogShown()=>audio.GameSfx.SetNoGameSound(true); // Native LevelContext unit22 flag.
            public void PrepareBottomPanel(bool cloche,bool weekly)=>Require(!cloche&&!weekly,"Explicit locked first-board footer owners");
            public void ShowBottomPanel()=>throw new NotSupportedException("Locked fixture has no active footer.");
            public void ShowLeagueCrown()=>throw new NotSupportedException("First board is not league.");
            public void DisableOrientation(int reason){Require(reason==37,"Native star orientation reason");ResultOrientationLocks++;}
            public void PlaySound(int id,float offset)=>Require(id==360&&offset==.04f,"Native star sound boundary");
            public void PlaySoundInLoop(int id){Require(id==359,"Native result loop boundary");ResultLoopStarts++;}
            public void StopSoundInLoop(int id){Require(id==359,"Native result loop teardown boundary");ResultLoopStops++;}
            public void EnableBottomPanelOrientation(){} // Explicit footer owner.
            public IOriginalLevelWinAds GetAds()=>this;
            public IOriginalLevelWinGameManager GetGameManager()=>exitManager;
            public IOriginalLevelWinLavaFlow GetLavaFlow()=>this;
            public IOriginalLevelWinAncientFlow GetAncientFlow()=>this;
            public void TryShowForCoin(Action<bool> completed)=>throw new NotSupportedException("First-board rewarded ad is not eligible in this fixture.");
            public void TryShowAfterLevelWin(Action completed){ResultAdRequests++;completed();} // Existing no-vendor ad boundary.
            public bool TryPushLevelEndFlow(int state,Action completed){Require(state==2,"Native Lava win state");return false;}
            public bool TryPushLevelEndFlow(Action completed,bool didFail,bool didWin){Require(!didFail&&didWin,"Native Ancient win arguments");return false;}
            public void SetGameUiActive(bool active){Require(!active,"Native exit hides game UI");hud.gameObject.SetActive(active);}
            public void TryShowLandscapeFlags(bool show,bool animate){Require(show&&!animate,"Native exit flag arguments");landscapeFlagsCalls++;} // Explicit Home landscape-flags owner.
            public void PrepareHomeBackground(){homeBackgroundCalls++;} // Explicit AreaManager/background owner; no fabricated Home view.
            public IOriginalDialogClearer GetDialogManager()=>dialogManager;
            public void HideSlidingText(){slidingTextHides++;} // Explicit sliding-text owner.
            public void VerifyResultButton()
            {
                Require(resultDialog!=null&&resultDialog.Lifecycle.IsShowing&&!resultDialog.IsAnimationWorking&&ResultLoopStarts==1&&ResultOrientationLocks==1&&ResultOrientationUnlocks==1,"Actual result registered and native star/pillow timeline completes");
                var labels=resultDialog.GetComponentsInChildren<TMPro.TextMeshPro>(false);bool hasContinue=false,hasWellDone=false;
                for(int i=0;i<labels.Length;i++)
                {
                    var label=labels[i];
                    Require(label.fontSharedMaterial.shader.name=="TextMeshPro/Mobile/Distance Field"&&label.textInfo.characterCount>0,"Result text uses its SDF shader and generated glyphs: "+label.name);
                    if(label.text=="Continue")hasContinue=true;if(label.text=="Well Done!")hasWellDone=true;
                }
                Require(hasContinue&&hasWellDone,"Recovered Continue and Well Done labels participate in rendering");
                var button=resultDialog.ContinueButton;Require(button.gameObject.activeInHierarchy&&button.onClick.GetPersistentEventCount()==0,"Actual visible result Button is code-bound");
                var input=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);
                Require(ResultAdRequests==1&&ResultExitRequests==1&&ResultClosed==1&&!resultDialog.Lifecycle.IsShowing&&Flow.Main.GetRunningAction()==null&&exitManager.ShouldExitGame&&game.State.IsNoGame()&&!hud.gameObject.activeSelf,
                    "Continue reaches actual native exit state/UI preparation, manager force-close lifecycle and flow completion once");
            }
            public void VerifyExitUiDestroyed()
            {
                Require(resultDialog==null&&ResultLoopStops==1&&!dialogManager.HasActiveDialog&&dialogManager.SortingOrder==0&&Flow.Main.GetRunningAction()==null&&Flow.Main.PendingCount==0,
                    "Deferred Unity destruction releases actual result view after its native close/flow protocol");
                Require(game.State.IsNoGame()&&!game.CanUserMakeMove()&&!exitManager.ShouldExitGame&&landscapeFlagsCalls==1&&homeBackgroundCalls==1&&slidingTextHides==1,"Native reset consumes exit flag and NoGame blocks player input");
                Require(deactivations==1&&!publication.IsActive&&publication.Controller==null&&homeLoadRequests==1&&systemMessageRequests==1&&poolRecycleCalls==1,"Native context consumes exit on its actual Unity update and reaches required scene/message ports once");
                Require(unitUpdates==unitUpdatesAtDeactivate&&board.Clock.FrameCount==clockFrameAtDeactivate,"Inactive context stops actual game-unit and level-clock dispatch");
                for(int id=0;id<board.Presentation.Pool.PoolCount;id++)Require(board.Presentation.Pool.GetSpawnedCount(id)==0,"Actual level pool retains no spawned item/cell/particle views: "+id);
                for(int x=0;x<board.Grid.Width;x++)for(int y=0;y<board.Grid.Height;y++)
                {
                    var cell=board.Grid.GetCell(x,y);
                    Require(cell.CellView==null&&!cell.Mediation.HasCurrentItem()&&!cell.Mediation.HasNextItem()&&!cell.Mediation.HasTargetItem(),"Native cell reset clears its view and all bidirectional item slots before the pool sweep");
                }
            }
            public void ManualUpdateGameUnits(){unitUpdates++;levelUnits.ManualUpdate();} // Input sample/replay transport remains explicitly verifier-owned; no double dispatch.
            public void RemoveOrientationChangeListener(OriginalGameContextDeactivation owner){} // Required camera subscription boundary.
            public IOriginalGameContextUnit[] GetGameUnits()=>gameUnits;
            public void KillAllTweens(bool complete)=>Require(!complete,"Native DOTween KillAll(false) boundary; custom global tween ownership remains separate.");
            public void StopAllManualTweenCoroutines()=>board.LocalMoves.StopAllCoroutines();
            public void KillGameCoroutines()=>board.Presentation.Coroutines.KillAll();
            public IOriginalGameContextItemCleanup GetItemFactory()=>this;
            public void RecycleAllSpawned(){board.Presentation.Pool.RecycleAllSpawned();poolRecycleCalls++;}
            public void ClearPools()=>board.Presentation.Pool.ClearAllPools();
            public void ResetCurrentCameraOrientationLock(){} // Required CameraManager owner boundary.
            public void KillAllAnimatorUpdates(){} // Required application AnimatorManager boundary.
            public IOriginalGameContextTouchEnabler GetUiTouchEnabler()=>this;
            public void EnablePinch(object owner)=>Require(ReferenceEquals(owner,contextExit),"Native pinch release owner");
            public void EnableVerticalScroll(object owner)=>Require(ReferenceEquals(owner,contextExit),"Native vertical-scroll release owner");
            public void EnableHorizontalScroll(object owner)=>Require(ReferenceEquals(owner,contextExit),"Native horizontal-scroll release owner");
            public bool IsLowEnd=>false; // Explicit non-low-end fixture device profile.
            public bool IsEarlySideProgressionLevel=>false; // Explicit ordinary first-board fixture; production level-kind owner remains required.
            public bool IsSideProgressionLevelPlayed()=>throw new NotSupportedException("Ordinary fixture does not query side-progression inventory.");
            public void ReloadLevel()=>throw new NotSupportedException("Ordinary fixture does not reload a side-progression level.");
            public IOriginalGameContextSceneLoader GetSceneManager()=>this;
            public void LoadHomeScene(){homeLoadRequests++;} // Scene loading/actual Home resources remain a required boundary.
            public void LoadGameScene()=>throw new NotSupportedException("Ordinary fixture has no early side-progression restart.");
            public void SendGetMessages(object request){Require(request==null,"Native system-message request payload");systemMessageRequests++;}
            private void ClearBoundLevelOwners(bool gameExit)
            {
                // Only the actual managers currently composed by this first-
                // board fixture are cleared, in native LevelContextId order.
                // This is not the complete112-slot production factory.
                OriginalExploder.Reset();game.ResetBoundGridViews(); // CellGrid2 must precede Particles18.
                game.Auto.Clear(gameExit);game.Fall.Clear(gameExit);game.Moves.Clear(gameExit);
                game.State.Clear(gameExit);game.WinFail.Clear(gameExit);board.Presentation.Particles.Clear(gameExit);
            }
            private sealed class BoundGameUnit:IOriginalGameContextUnit
            {
                public int Id{get;}private readonly Action<bool> clear;
                public BoundGameUnit(int id,Action<bool> clear){Id=id;this.clear=clear;}
                public void Bind()=>throw new NotSupportedException("Fixture adapter wraps an already-bound functional owner; production binding is not implemented by it.");
                public void Clear(bool gameExit)=>clear(gameExit);
            }
            public void TrySendRemainingMoves()=>Manager.TrySendMoves();
            public void StartExtraMoveFlow()=>throw new NotSupportedException("First-board fixture has no extra-token owner.");
            public void StartEgoFlow()=>throw new NotSupportedException("This fixture verifies actual goal victory only.");
            public void StartNoPossibleMoveFlow()=>throw new NotSupportedException("This fixture verifies actual goal victory only.");
            public void StartFailFlow()=>throw new NotSupportedException("This fixture verifies actual goal victory only.");
            public void SpendMovesOnLevelQuit()=>throw new NotSupportedException("No quit is injected by this fixture.");
            public void ReportOperationTimeout(int count,int state)=>throw new InvalidOperationException("Unexpected WinFail operation timeout.");
            public void StopFadeOutSoundSourceCoroutine(){}
            // Explicit presentation/account owner fixture. The actual low-match
            // state and configured native ratio array are live runtime owners.
            public bool AnimationImprovementEnabled=>false;
            public void KingBeHappy(){KingHappyCalls++;}
            public void KingStopWorry(){KingStopCalls++;}
            public int[] CreateWinLowMatchRatios()=>game.CreateWinLowMatchRatios();
            public void UpdateLowMatchRatios(int[] ratios)=>game.UpdateLowMatchRatios(ratios);
            public void IncrementLives(int amount){Account.RefundWinLife(amount);IncrementedLives+=amount;}
            public void ResetEgoOnWin(){EgoResetCalls++;}
            public OriginalRemainingMovesItemReplacer CreateMoveReplacer()=>new OriginalRemainingMovesItemReplacer(
                new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid,board.Layout)),board.Presentation.Coroutines,
                new OriginalRemainingMovesReplacementServices(game,board,hud,audio));
            public OriginalRemainingMovesItemExploder CreateItemExploder()=>new OriginalRemainingMovesItemExploder(
                new OriginalTopLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid,board.Layout)),game.State,board.Presentation.Coroutines,()=>board.Clock.Time,()=>board.Clock.DeltaTime,hud.ResetSorting);
            public OriginalRemainingMovesCoinCollectAnimation CreateCoinCollection()=>new OriginalRemainingMovesCoinCollectAnimation(new OriginalRemainingMovesCoinServices(game,board,hud,audio.Playback));
            public void SessionRemainingMovesEnd(int notReplaced,int coins,Dictionary<int,int> remaining)
            {
                SessionCalls++;NotReplaced=notReplaced;GoalCountAtEnd=board.Goals.GetGoalLeftCount(21);
                // Capture actual cell models at the account boundary, before
                // pending explosion coroutines change the post-skip board.
                for(int y=0;y<board.Grid.Height;y++)for(int x=0;x<board.Grid.Width;x++)
                {
                    if(!(board.Grid.GetCell(x,y).Mediation.CurrentItem is OriginalSpecialBoardItem item)||!item.IsActive||item.IsExploded)continue;
                    BoardCoinsAtEnd+=OriginalRemainingMovesManager.GetCoinValue(item);
                }
                var mission=game.Session.AnimationValues.StartSnapshots.MissionEvents;
                int rockets=mission.CreatedRocketCount,tnt=mission.CreatedTntCount;
                game.Session.RemainingMovesEnd(notReplaced,coins,remaining,sessionEvents);
                int half=notReplaced/2*OriginalBeforeAfterData.GetMultiplierValueForDifficulty(game.Session.Difficulty);
                Require(mission.CreatedRocketCount==rockets+half&&mission.CreatedTntCount==tnt+half,
                    "Actual Session end adds the native two mission counters for unsent moves");
                var data=game.Session.AnimationValues.StartSnapshots;
                Require(data.DailyLightningRush.Before==-1&&data.DailyLightningRush.After==-1&&data.PropellerRush.GetCollectedAmount()==0&&
                    ((OriginalClashOfChampionsLevelData)data.ClashOfChampions).GetCollectedAmount()==0,
                    "Actual locked-event snapshots do not receive progression rewards");
            }
            public void LevelRemainingMovesEnd(){EndCalls++;OriginalLevelEnd.RemainingMovesEnd(this,null);}
            public IOriginalMadnessSessionManager Madness=>game;
            public bool IsPlayingWithSuperLightball(){SuperLookups++;return ((IOriginalMissionSuperBooster)game).IsPlayingWithSuperBooster();}
            public bool ShouldProgress=>false;public bool IsInEvent=>false;public bool IsScoreLocked=>false;
            public IOriginalClashSessionHelper Helper=>this;public IOriginalClashScoreConfig Config=>this;
            public IOriginalDailyLightningRushProgress Progress=>throw new NotSupportedException("Locked fixture event has no progress owner.");
            public bool IsAllBundlesDownloaded()=>throw new NotSupportedException("Locked fixture does not query event downloads.");
            public void UpdateLocalScore(int score,int combo)=>throw new InvalidOperationException("Locked fixture must not progress Clash score.");
            public int GetMatchItemScore()=>0;public int GetPropellerItemScore()=>0;public int GetRocketItemScore()=>0;
            public int GetTntItemScore()=>0;public int GetLightballItemScore()=>0;public int GetPropellerPropellerComboScore()=>0;
            public int GetPropellerRocketComboScore()=>0;public int GetPropellerTntComboScore()=>0;public int GetRocketRocketComboScore()=>0;
            public int GetRocketTntComboScore()=>0;public int GetTntTntComboScore()=>0;public int GetLightballLightballComboMultiplier()=>0;
            public int GetRemainingMoveScore()=>0;public int GetWinBonus()=>0;
        }
        private static void StepLogoSkip()
        {
            if(!skipped)
            {
                if(!services.IsSkipAttached||services.LogoTime<.95f)return;
                Require(services.LogoClosed==0&&services.RemainingShown==0,"Button click precedes actual logo close");
                Capture("win_logo_skip_before.png");ClickSkipButton();skipped=true;
                Require(manager.IsTapToSkipClicked&&services.LogoStopSound==1,"Button dispatch reaches the original logo skip callback");
            }
            if(services.EndCalls==0)return;
            if(phase==1){phase=2;completedAt=Time.time;}
            if(Time.time-completedAt<2.2f||game.State.HasAnyOperation()||!game.Fall.IsAllItemsSteady||board.Presentation.Coroutines.Count!=0)return;
            Require(services.LogoClosed==1&&services.RemainingShown==0&&services.Closed==0&&services.ResultShown==1&&services.SkipDestroyed==1,"Native early-logo skip completes once without instantiating remaining dialog");
            Require(services.SessionCalls==1&&services.EndCalls==1&&services.CreationStartedCalls==0&&manager.Replacer.ReplacedMoves==0&&services.NotReplaced==startMoves,"Immediate skip accounts the original entire move budget");
            services.Account.VerifyPersisted();Capture("win_logo_skip_reward.png");services.VerifyResultButton();
            phase=3;resultClickFrame=Time.frameCount;
        }
        private static void WriteLogoSkipReport()
        {
            File.WriteAllText(PathName,JsonUtility.ToJson(new Report{passed=true,skipped=true,skip_ui_level=2,logo_runtime_dialog_lifecycle_verified=true,
                skip_button_entry_verified=true,initial_remaining_moves=startMoves,converted_moves=0,unconverted_moves=services.NotReplaced,
                reward_coins=manager.ExtraCoinAmount,logo_closed=services.LogoClosed,remaining_dialog_shown=0,result_dialog_owner_shown=services.ResultShown,
                result_prefab_registration_verified=true,result_continue_button_verified=true,result_exit_requests=services.ResultExitRequests,
                actual_game_context_deactivation_verified=true,actual_level_pool_recycled=true,inactive_level_updates_stopped=true,home_scene_load_requests=1,actual_game_exit_manager_verified=true,result_dialog_cleared=true,result_view_destroyed=true,result_flow_completed=true,game_state_after_continue=game.State.GetGameState(),
                account_level=services.Account.User.Basic.Level,account_star=services.Account.User.Inventory.Star,account_coin=services.Account.User.Inventory.Coin,
                account_initial_coin=services.Account.InitialCoin,account_database_reopen_verified=services.Account.DatabaseReopenVerified,
                account_native_reload_verified=services.Account.NativeLocalReloadVerified,runtime_storage_factory_verified=services.Account.RuntimeStorageFactoryVerified,
                elapsed=Time.realtimeSinceStartup-started},true)+"\n");Cleanup();
        }
        private static void FinishAfterContinue()
        {
            if(Time.frameCount<=resultClickFrame+1)return;
            services.VerifyExitUiDestroyed();
            Capture(variant==3?"win_logo_skip_after_continue.png":"remaining_coins_"+VariantName+"_after_continue.png");
            if(variant==3)WriteLogoSkipReport();else WriteNormalReport();
        }
        private static void ClickSkipButton()
        {
            var view=services.SkipView;Require(view!=null&&EventSystem.current!=null,"Configured standard Button and EventSystem exist");
            Require(view.Button.onClick.GetPersistentEventCount()==0,"Button events are code-bound");
            ExecuteEvents.Execute(view.Button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        private static void Capture(string name)
        {
            var camera=UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().PointerCamera;var target=new RenderTexture(Mathf.RoundToInt(768*camera.aspect),768,24);
            var previous=camera.targetTexture;var active=RenderTexture.active;Texture2D pixels=null;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../"+name),pixels.EncodeToPNG());}
            finally{camera.targetTexture=previous;RenderTexture.active=active;if(pixels!=null)UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(target);}
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private static void Cleanup(){EditorApplication.update-=Tick;GoalUiVerification.OnFinished-=GoalsFinished;services.Dispose();game.ReleaseWinFail();game.BindRemainingMovesManager(null);}
        [Serializable]private sealed class Report
        {
            public bool actual_life_entry_debit_verified,actual_win_life_refund_verified,account_life_reopen_verified;
            public long entry_life_timestamp,win_life_timestamp;
            public bool account_area_data_restored;
            public bool actual_game_context_deactivation_verified,actual_level_pool_recycled,inactive_level_updates_stopped;public int home_scene_load_requests;
            public bool actual_game_exit_manager_verified,result_dialog_cleared,result_view_destroyed,result_flow_completed;public int game_state_after_continue;
            public bool passed,skipped,logo_runtime_dialog_lifecycle_verified,remaining_runtime_dialog_lifecycle_verified,skip_button_entry_verified,first_level_skip_rejected,remaining_dialog_started_ready,account_database_reopen_verified,account_native_reload_verified,runtime_storage_factory_verified,directory_rules_verified,result_prefab_registration_verified,result_continue_button_verified;public string failure,account_database_path;public int result_exit_requests,skip_ui_level,initial_remaining_moves,converted_moves,unconverted_moves,reward_coins,collections,peak_coin_views,peak_pilots,peak_operation5,native_departure_count,active_board_coin_value_at_end,unconverted_move_coin_value,session_star_reward,session_win_state,native_win_cloche_calls,win_trigger_calls,win_flow_calls,low_match_total,logo_closed,remaining_dialog_shown,result_dialog_owner_shown,level_end_calls,reactivation_calls,account_level,account_star,account_coin,account_coin_update_calls,account_star_update_calls,account_initial_coin,account_area_load_calls;public float elapsed;
            public string scope="Formal36box goal scenario. Actual native local-account loader reads an empty private Windows SQLite database (level1/coins2000/stars0 without default writes), then actual win/remaining/user-end/storage kernels commit rewards. Actual TimeUtil client clock, LifeClock offset storage reads and LifeHelper now pre-debit entry and return that life through WinTransition/Basic/UserKeyValue; database reopen verifies the life timestamp and sync flags. No backend time response or paused app is injected in this account fixture; weekly-pass eligibility is explicitly false. The original logo/remaining/result prefabs own runtime UiDialog lifecycle, real DialogManager registration, remaining-move/special/coin flights and standard Continue/Skip buttons. Continue reaches actual GameExit/NoGame, dialog clear, flow completion and real context exit dispatch, late/local-move/manual coroutine cleanup and pool recycling. Home-load request is checked, not actual Home navigation. Feature/event eligibility, full112-slot factory, global camera/tween/touch/Home/localization/haptic/audio/King/Ego/reactivation/download/tutorial/save-file/backend and production application composition remain fixture boundaries. Original area parser/six-task config and reopened account loader run without default writes. Full production settlement composition, themes/footer/orientation layout, physical input, Android/iOS and original rendered parity remain incomplete.";
        }
    }
}
