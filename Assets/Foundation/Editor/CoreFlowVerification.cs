using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Whitebox.Foundation.Editor
{
    public static class CoreFlowVerification
    {
        private static FoundationController controller;
        private static int phase,frame,initialId,secondId;
        private static float started,failureShownAt,lastKingWinTime;
        private static float bannerClickedAt;
        private static OriginalDeltaTimeManager entranceClock, failedClock;
        private static OriginalPlayableBoard failedAttempt;
        private static OriginalGameObjectPool secondPool;
        private static OriginalGoalUi secondGoalView;
        private static AudioSource secondMusic;
        private static int oldFrame;
        private static float continueHudCue, automaticExitCue;
        private static float entranceRestX;
        private static bool entranceCrossedCenter, entranceAnimatedHud;
        private static float entranceTopY, entranceBottomY;
        private static OriginalGameplayHud entranceHud;
        private static OriginalBottomUi entranceBottom;
        private static bool entranceTopMoved, entranceBottomMoved;
        private static bool goalsFinished;
        private static AudioSource openingMusic;
        private static OriginalApplicationRandom openingApplicationRandom;
        private static OriginalInitialBoardView openingBoard;
        private static OriginalKingVisual openingKing, secondKing;
        private static SpriteRenderer winBackdrop;
        private static TextMeshPro[] winTextLayers;
        private static Transform winStar;
        private static Animator winPillow;
        private static OriginalSfxPlayback winAudio;
        private static float maxResultFrameDelta;
        private static CoreModalTutorialVerification.GestureSamples openingSamples;
        private static OriginalGameTouchListener openingListener;
        private static int openingSample;
        private static string failure;
        private static readonly Report report=new Report();
        private static bool closeWinWithBack;
        internal static void ObserveInitialEntrance(FoundationController owner)
        {
            ObserveEntrance(owner, true, true);
            report.cold_entry_verified = true;
        }

        private static void ObserveEntrance(FoundationController owner, bool animateHud, bool capture = false)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var flow = owner.CoreFlow;
            var camera = flow.UiCamera;
            var audio = owner.GetComponentInChildren<OriginalGameAudioHost>().Playback;
            Require(board.State.IsNoGame() && !board.Moves.CanUserMakeMove() && flow.BlocksBoardInput &&
                board.Moves.MadeMoves == 0 && flow.DirectedTutorial == null && flow.ModalTutorial == null,
                "The opening owns board input until the sequence finishes.");
            if (!ReferenceEquals(entranceClock, board.Clock))
            {
                entranceClock = board.Clock;
                var region = board.Layout.CurrentBoard;
                entranceRestX = board.BoardRoot.parent.TransformPoint(
                    new Vector3(-(region.Left + region.Right) * .5f, 0f, 0f)).x;
                entranceCrossedCenter = false;
                Require(camera.WorldToViewportPoint(board.BoardRoot.position).x > 1f,
                    "The authored board starts to the right of the viewport.");
                entranceHud = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();
                entranceBottom = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>();
                Require(entranceHud != null && entranceBottom != null, "The normal opening owns both HUDs.");
                entranceAnimatedHud = animateHud;
                entranceTopY = entranceHud.transform.position.y;
                entranceBottomY = entranceBottom.transform.position.y;
                entranceTopMoved = entranceBottomMoved = false;
                float hudCue = audio.GetClipLastPlayedTime(209);
                Require(animateHud ? !audio.CanPlayAudio(209, 1000f) : hudCue == continueHudCue,
                    "The HUD entrance cue is exclusive to fresh entries.");
            }
            entranceTopMoved |= Mathf.Abs(entranceHud.transform.position.y - entranceTopY) > .005f;
            entranceBottomMoved |= Mathf.Abs(entranceBottom.transform.position.y - entranceBottomY) > .005f;
            if (board.BoardRoot.position.x < entranceRestX - .025f) entranceCrossedCenter = true;
            if (capture && !report.cold_entry_board_captured &&
                board.BoardRoot.position.x < entranceRestX + 4f && board.BoardRoot.position.x > entranceRestX + .3f)
            {
                Require(!audio.CanPlayAudio(208, 1000f), "The native board cue precedes the visible slide.");
                Capture(camera, "core_flow_level_entrance.png");
                report.cold_entry_board_captured = true;
            }
        }

        private static void RequireEntranceSettled(FoundationController owner)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            Require(ReferenceEquals(entranceClock, board.Clock) && entranceCrossedCenter &&
                Mathf.Abs(board.BoardRoot.position.x - entranceRestX) < .001f &&
                !owner.GetComponentInChildren<OriginalGameAudioHost>().Playback.CanPlayAudio(208, 1000f),
                "The recovered board animation overshoots, settles exactly and plays its cue.");
            Require(entranceTopMoved == entranceAnimatedHud && entranceBottomMoved == entranceAnimatedHud,
                "Both HUDs slide on fresh entry and hold their position on restart.");
        }

        public static void Run(bool useBack)
        {
            closeWinWithBack = useBack;
            if (useBack) report.scope += " This run uses the production Back handler to leave the result.";
            controller=UnityEngine.Object.FindObjectOfType<FoundationController>();
            Require(controller!=null&&controller.CoreFlow.IsInitialized&&controller.CurrentLevelNumber==1,"Real core entry starts level1");
            Require(report.cold_entry_verified && report.cold_entry_board_captured, "Observed the initial production animation.");
            RequireEntranceSettled(controller);
            report.level1_attempt_seed = ObserveAttemptSeed(1, 111);
            openingMusic = RequirePlayingMusic(controller);
            openingApplicationRandom = controller.GetComponentInChildren<OriginalGameAudioHost>().Random;
            Require(openingMusic.clip.name == "Gameplay_1", "Level 1 uses the original built-in track.");
            report.opening_music = openingMusic.clip.name;
            var preview=controller.GetComponent<UnityEngine.UIElements.UIDocument>();
            Require(preview!=null&&!preview.enabled&&preview.panelSettings==null&&preview.visualTreeAsset==null,
                "The formal scene has no active recovery preview, panel or rewarded-ad UI asset");
            report.formal_mock_ad_preview_disconnected=true;
            Require(controller.Gameplay.WinFail==controller.CoreFlow.WinFail,"Actual permanent WinFail16 is the production core owner");
            Require(controller.CoreFlow.Configuration.progressFileName.StartsWith("CoreLevels_verify_",StringComparison.Ordinal),"Use the isolated progress-file transport");
            initialId=controller.GetInstanceID();started=Time.realtimeSinceStartup;
            SceneManager.sceneLoaded+=SceneLoaded;Application.logMessageReceived+=ObserveLog;
            EditorApplication.update+=Tick;GoalUiVerification.OnFinished+=GoalsFinished;
            File.WriteAllText("live_core_flow_verification.json","{\"passed\":false,\"status\":\"running\"}\n");
            phase=-1;
            openingBoard=UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            openingKing=UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual;
            Require(IsKingIdle(openingKing),
                "The formal scene starts with an authored ordinary King idle clip.");
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            openingSamples=new CoreModalTutorialVerification.GestureSamples();
            openingListener=new OriginalGameTouchListener(openingSamples,controller.Gameplay);
        }
        private static void SceneLoaded(Scene scene,LoadSceneMode mode){controller=UnityEngine.Object.FindObjectOfType<FoundationController>();}
        private static void GoalsFinished(){goalsFinished=true;}
        private static void ObserveLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=message+"\n"+stack;}
        private static void Tick()
        {
            try{Step();}catch(Exception error){report.failure=error.ToString();Finish();Debug.LogException(error);}
        }
        private static void Step()
        {
            if(!EditorApplication.isPlaying||frame==Time.frameCount)return;frame=Time.frameCount;
            Require(failure==null,failure);Require(Time.realtimeSinceStartup-started<180,"Core win/continue/fail/retry scenario timeout, phase="+phase);
            if (controller == null) return;
            if (controller.CoreFlow.IsEnteringLevel)
            {
                if (phase == 1 || phase == 3) ObserveEntrance(controller, phase == 1);
                return;
            }
            if (controller.Gameplay == null || !controller.CoreFlow.IsInitialized) return;
            var game=controller.Gameplay;var owner=controller.CoreFlow;
            if(phase==-1)
            {
                if(openingSample<3)
                {
                    if(openingSample==0&&(owner.DirectedTutorial==null||!owner.DirectedTutorial.IsGuiding))return;
                    openingSamples.Set(openingBoard.Grid.GetCell(5,openingSample==0?5:4),openingSample);
                    openingListener.ManualUpdate();openingSample++;return;
                }
                if(!game.Fall.IsAllItemsSteady||openingBoard.State.HasAnyOperation()||game.Auto.IsThereAWaitingMatch)return;
                if(game.CreatedRefills==0||game.GoalPanel.Views[0].countText.text!=openingBoard.Goals.GetGoalLeftCount(11).ToString())return;
                Require(game.Moves.MadeMoves==1&&!game.IsUiTouching,"The original opening drag spends a move and releases input");
                GoalUiVerification.Run(true);phase=0;return;
            }
            if(phase==0)
            {
                ObserveKingWin();
                if (!report.music_stopped_at_win_logo)
                {
                    var logo = owner.UiCamera.GetComponentInChildren<OriginalWinLogoDialog>();
                    if (logo != null && logo.Lifecycle.IsShowing)
                    {
                        Require(!openingMusic.isPlaying && openingMusic.timeSamples == 0,
                            "The win logo stops and resets the dedicated music source.");
                        report.music_stopped_at_win_logo = true;
                    }
                }
                ObserveWinBackdrop(owner);
                ObserveWinPresentation(owner);
                if(!goalsFinished||owner.WinDialog==null||!owner.WinDialog.Lifecycle.IsShowing||owner.WinDialog.IsAnimationWorking)return;
                if (string.IsNullOrEmpty(report.king_win_loop) || !report.win_curved_text_settled) return;
                Require(report.king_win_mixed && report.king_connector_mixed,
                    "The formal King blends both into the celebration and into its connector queue.");
                Require(game.IsUiTouching,"The core win dialog owns board input blocking");
                Require(report.music_stopped_at_win_logo && !openingMusic.isPlaying,
                    "The level music stays stopped during the result and its separate loop.");
                RequireCurvedTitle(owner.WinDialog, "Level 1", 1.9f);
                report.win_title_curve_verified = true;
                Require(report.win_text_stagger && report.win_text_overshoot && report.win_pillow_started &&
                    report.win_star_hit_time > 0f && report.win_idle_loop_time > report.win_star_hit_time,
                    "The real result shows staggered glyphs and plays both original audio cues.");
                float cueTolerance = Mathf.Max(.05f, maxResultFrameDelta * 3f);
                Require(Mathf.Abs(report.win_pillow_delay - .1f) < cueTolerance &&
                    Mathf.Abs(report.win_first_glyph_delay - .2f) < cueTolerance &&
                    Mathf.Abs(report.win_idle_loop_time - report.win_star_hit_time - .5355f) < cueTolerance,
                    "The actual pillow, text and idle audio follow the star sequence cues.");
                Require(report.win_logo_backdrop_fade && report.remaining_moves_backdrop_reused &&
                    RequireSingleBackdrop(owner.UiCamera) == winBackdrop && Mathf.Approximately(winBackdrop.color.a, .8f),
                    "The logo fade, transparent remaining-moves shade and immediate result reuse one real backdrop.");
                Require(game.Session.State==3&&game.Session.WonAnyLevel&&game.Session.ExtraCoins>0,"Native session Win and remaining moves reached the real result");
                Require(!owner.RemainingMoves.IsRemainingMovesActive&&owner.RemainingMoves.CollectionCount>0,"Real remaining-move replacement, special explosions and coin flights completed");
                var rewardTitle = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().TargetTitle;
                Require(rewardTitle.text == "Reward", "The actual remaining-move sequence changed the HUD title.");
                GameSceneVerification.RequireHudTitleCurve(rewardTitle);
                Require(controller.Progress.LevelToPlay==2,"Actual core progress advances before Continue");
                Require(!File.Exists(controller.Progress.DatabasePath + ".level"), "Victory clears the finished attempt snapshot.");
                using(var db=OriginalRuntimeStorage.Open(controller.Progress.DatabasePath))
                {
                    Require(db.Find("Level").Value=="2","Actual SQLite commit contains level2");
                    Require(db.Find("Coins").Key==null&&db.Find("Stars").Key==null&&db.Find("Area").Key==null,"Core progress writes no peripheral balances/area");
                }
                report.win_remaining_reward=game.Session.ExtraCoins;report.win_left_moves=game.Session.LeftMoves;
                report.production_win_remaining_result_bound=true;Capture("core_flow_win.png");
                if (closeWinWithBack)
                {
                    owner.PressBack();
                    Require(owner.IsTransitioning, "Back on the top win result starts the actual exit.");
                    report.back_closed_win = true;
                }
                else Click(owner.WinDialog.ContinueButton,owner.UiCamera);
                phase=1;return;
            }
            if(phase==1)
            {
                if(controller.GetInstanceID()==initialId||controller.CurrentLevelNumber!=2)return;
                RequireEntranceSettled(controller);
                report.continue_entry_verified = true;
                report.level2_attempt_seed = ObserveAttemptSeed(1, 404);
                Require(ReferenceEquals(openingApplicationRandom, controller.GetComponentInChildren<OriginalGameAudioHost>().Random),
                    "Continue preserves the application random source across the scene change.");
                report.application_random_survives_continue = true;
                secondMusic = RequirePlayingMusic(controller);
                report.continue_music = secondMusic.clip.name;
                continueHudCue = controller.GetComponentInChildren<OriginalGameAudioHost>().Playback.GetClipLastPlayedTime(209);
                Require(game.Session.Number==2&&game.Session.Name=="00002v02_L"&&game.Moves.LeftMoves==27&&game.State.IsPlaying(),"Continue reloads the actual original second level with27 moves");
                RequireNoBackdrop(owner.UiCamera);
                GameSceneVerification.RequireGameBackground(GameSceneVerification.CastleGardenBackgroundGuid);
                Require(winAudio.GetLoopCount(359) == 0, "Continue releases the result loop audio source.");
                var bottom=UnityEngine.Object.FindObjectOfType<OriginalBottomUi>();
                Require(bottom!=null&&bottom.IsInitialized&&bottom.Boosters.Length==4,"Continue restores all four original tool slots");
                foreach(var slot in bottom.Boosters)
                    Require(!slot.Button.interactable&&!slot.Presentation.ShowAdd,"The level 2 tool slots have no purchase action");
                secondKing=UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual;
                secondPool = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>().Presentation.Pool;
                secondGoalView = game.GoalPanel.Views[0];
                secondId=controller.GetInstanceID();report.actual_next_level_loaded=true;Capture("core_flow_level2.png");
                // Explicit branch stimulus, not a simulated27-move playthrough.
                // The real MoveManager event drives the real WinFail/failed UI.
                game.Moves.IncreaseMove(-game.Moves.LeftMoves,0);phase=2;return;
            }
            if(phase==2)
            {
                if (secondKing.CurrentAnimation=="k-1_uzulme" && secondKing.IsAnimationMixing && secondKing.AnimationTime>0f)
                    report.king_fail_mixed=true;
                var lossAudio = controller.GetComponentInChildren<OriginalGameAudioHost>();
                float bannerStartedAt = lossAudio.Playback.GetClipLastPlayedTime(258);
                if (owner.OutOfMovesBanner != null)
                {
                    Require(game.State.IsEgo() && owner.BlocksBoardInput && !game.CanUserMakeMove() && owner.FailDialog == null,
                        "The ordinary banner owns the out-of-moves transition before failure.");
                    if (!report.out_of_moves_banner_center_verified && Time.time - bannerStartedAt >= 1.1f)
                    {
                        Require(owner.OutOfMovesBanner.SkipButton.interactable &&
                            owner.OutOfMovesBanner.GetComponentInChildren<TextMeshPro>().text == "Out of Moves!",
                            "The recovered title and its visible skip Button are active during the hold.");
                        Require(Mathf.Approximately(RequireSingleBackdrop(owner.UiCamera).color.a, 0f),
                            "The native banner leaves the game view undimmed.");
                        Require(!lossAudio.MusicSource.isPlaying && lossAudio.MusicSource.timeSamples > 0,
                            "The native out-of-moves banner pauses music and retains its position.");
                        report.paused_music_samples = lossAudio.MusicSource.timeSamples;
                        Capture("core_flow_out_of_moves.png");
                        report.out_of_moves_banner_center_verified = true;
                    }
                    if (report.paused_music_samples > 0)
                        Require(!lossAudio.MusicSource.isPlaying && lossAudio.MusicSource.timeSamples == report.paused_music_samples,
                            "The paused track does not advance while the banner holds.");
                    return;
                }
                if(owner.FailDialog==null||!owner.FailDialog.Lifecycle.IsShowing)return;
                if (failureShownAt == 0)
                {
                    failureShownAt = Time.time;
                    Require(report.paused_music_samples > 0 && !lossAudio.MusicSource.isPlaying && lossAudio.MusicSource.timeSamples == 0,
                        "Final failure stops and resets the previously paused source.");
                    report.music_stopped_at_failure = true;
                    float exitStartedAt = lossAudio.Playback.GetClipLastPlayedTime(259);
                    Require(report.out_of_moves_banner_center_verified && bannerStartedAt > 0f &&
                        Mathf.Abs(exitStartedAt - bannerStartedAt - 1.5166667f) < .2f &&
                        Mathf.Abs(failureShownAt - bannerStartedAt - 1.95f) < .2f &&
                        lossAudio.GameAssets.GetClip(258) != null && lossAudio.GameAssets.GetClip(259) != null,
                        "The recovered entrance and exit audio accompany the automatic banner-to-failure timeline.");
                    report.out_of_moves_auto_duration = failureShownAt - bannerStartedAt;
                    return;
                }
                if(Time.time-failureShownAt<.25f)return;
                Require(game.State.IsFail()&&game.Session.State==4&&!game.CanUserMakeMove(),"Actual zero-budget branch produces a blocked failure state");
                Require(game.IsUiTouching,"The core fail dialog owns board input blocking");
                if (closeWinWithBack)
                {
                    owner.PressBack();
                    Require(!owner.IsTransitioning && owner.FailDialog.Lifecycle.IsShowing &&
                        !owner.FailDialog.Lifecycle.IsClosing && owner.LevelQuitDialog == null,
                        "Back respects the failure dialog's disabled escape action.");
                    report.back_respected_failure_dialog = true;
                }
                Require(Mathf.Approximately(RequireSingleBackdrop(owner.UiCamera).color.a, .8f),
                    "The real failure fades to the authored dark shade.");
                Require(controller.Progress.LevelToPlay==2,"Failure does not advance progress or spend lives");
                Require(!File.Exists(controller.Progress.DatabasePath + ".level"), "Defeat clears the finished attempt snapshot.");
                owner.FailDialog.FailureText.ForceMeshUpdate();
                Require(owner.FailDialog.FailureText.textInfo.characterCount>0&&owner.FailDialog.FailureText.fontSharedMaterial.shader.name=="TextMeshPro/Mobile/Distance Field","Fail text uses its restored original SDF shader and generated glyphs");
                Require(owner.FailDialog.FailureText.text == "Level Failed!", "The failure message matches the original prefab.");
                RequireCurvedTitle(owner.FailDialog, "Level 2", 1.9f);
                report.fail_title_curve_verified = true;
                Require(owner.FailDialog.VisibleGoalCount==1,"Failure shows actual original goal result slots");
                Require(secondKing.CurrentAnimation=="k-1_uzulme"&&!secondKing.IsAnimationLooping && report.king_fail_mixed,
                    "The actual failure blends into the authored non-looping sad reaction.");
                report.king_fail_reaction=secondKing.CurrentAnimation;
                report.production_zero_move_failure_bound=true;Capture("core_flow_fail.png");
                failedAttempt = game;
                failedClock = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>().Clock;
                oldFrame = failedClock.FrameCount;
                automaticExitCue = lossAudio.Playback.GetClipLastPlayedTime(259);
                Click(owner.FailDialog.RetryButton,owner.UiCamera);phase=3;return;
            }
            if(phase==3)
            {
                if (ReferenceEquals(failedAttempt, game)) return;
                Require(controller.GetInstanceID() == secondId && controller.CurrentLevelNumber == 2,
                    "Retry keeps the current Foundation and level.");
                RequireEntranceSettled(controller);
                report.retry_entry_verified = true;
                report.level2_retry_seed = ObserveAttemptSeed(2, 167);
                report.retry_music = RequirePlayingMusic(controller).clip.name;
                // Native ResetValues advances Load(1) to the active state2.
                Require(game.Moves.LeftMoves==27&&game.State.IsPlaying()&&game.Session.State==2,"Retry creates a fresh, playable level2 session");
                RequireNoBackdrop(owner.UiCamera);
                report.result_and_failure_backdrops_cleared_on_reload = true;
                GameSceneVerification.RequireGameBackground(GameSceneVerification.CastleGardenBackgroundGuid);
                report.castle_garden_background_survives_continue_and_retry = true;
                var board=UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
                Require(ReferenceEquals(board.Presentation.Pool, secondPool) && game.GoalPanel.Views[0] == secondGoalView &&
                    RequirePlayingMusic(controller) == secondMusic && secondKing == UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual &&
                    failedClock.FrameCount - oldFrame <= 2 && !ReferenceEquals(board.Clock, failedClock),
                    "Retry reuses the item pool, goal view, music source and King while retiring the old level clock.");
                report.retry_reused_scene_pool_and_hud = true;
                Require(board.Goals.GetGoalLeftCount(11)==36,"Retry restores the original36-box goal");
                Require(IsKingIdle(UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual),
                    "A fresh level restores the authored idle sequence.");
                VerifyCap(owner.Configuration.progressFileName+"_boundary");
                Capture("core_flow_retry.png");report.actual_retry_reloaded_same_level=true;report.cap100_and_restart_verified=true;
                report.core_booster_inventory_purchase_ui_absent=true;
                failedAttempt = game;
                var cell = board.Grid.GetCell(6, 4);
                var tile = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
                tile.Mediator.ClearFromAllRegisteredCells();
                tile.RecycleView();
                game.CreateItemAtCell(40, cell);
                Require(secondPool.GetSpawnedCount(14) == 1 && secondPool.GetSpawnedCount(16) == 1,
                    "The second retry fixture has one TNT with a borrowed fuse.");
                game.Moves.IncreaseMove(-game.Moves.LeftMoves, 0);
                phase = 4;
                return;
            }
            if (phase == 4)
            {
                var banner = owner.OutOfMovesBanner;
                var lossAudio = controller.GetComponentInChildren<OriginalGameAudioHost>();
                if (banner == null || Time.time - lossAudio.Playback.GetClipLastPlayedTime(258) < 1.1f) return;
                bannerClickedAt = Time.time;
                Click(banner.SkipButton, owner.UiCamera, false);
                Require(banner.Lifecycle.IsClosing && !banner.SkipButton.interactable,
                    "The visible standard Button starts the native short exit once.");
                phase = 5;
                return;
            }
            if (phase == 5)
            {
                if (owner.FailDialog == null || !owner.FailDialog.Lifecycle.IsShowing) return;
                var lossAudio = controller.GetComponentInChildren<OriginalGameAudioHost>();
                report.out_of_moves_skip_duration = Time.time - bannerClickedAt;
                Require(Mathf.Abs(report.out_of_moves_skip_duration - .4f) < .15f &&
                    owner.OutOfMovesBanner == null && lossAudio.Playback.GetClipLastPlayedTime(259) == automaticExitCue &&
                    game.State.IsFail() && !game.CanUserMakeMove(),
                    "The skipped banner exits without the automatic exit sound and hands off to failure.");
                Click(owner.FailDialog.RetryButton, owner.UiCamera);
                report.out_of_moves_standard_button_skip_verified = true;
                phase = 6;
                return;
            }
            if (phase == 6)
            {
                if (ReferenceEquals(failedAttempt, game)) return;
                Require(controller.GetInstanceID() == secondId && controller.CurrentLevelNumber == 2,
                    "Repeated retries stay in the same scene.");
                Require(game.State.IsPlaying() && game.Moves.LeftMoves == 27 && owner.OutOfMovesBanner == null &&
                    secondPool.GetSpawnedCount(14) == 0 && secondPool.GetSpawnedCount(16) == 0,
                    "Retry after the short exit clears both the TNT and its nested fuse from the reused pool.");
                report.retry_recycled_tnt_and_fuse = true;
                report.level2_second_retry_seed = ObserveAttemptSeed(3, 985);
                // The level 3 smoke check separately exercises its layered boxes.
                controller.Progress.CompleteLevel(2);
                SceneManager.LoadSceneAsync(controller.gameObject.scene.path, LoadSceneMode.Single);
                phase = 7;
                return;
            }
            if(phase==7)
            {
                if(controller.CurrentLevelNumber!=3)return;
                report.level3_attempt_seed = ObserveAttemptSeed(1, 697);
                int expected=0;var definition=OriginalLevelReader.Load("Levels/level_3");
                foreach(int id in definition.Grid.Items)if(id==22)expected++;
                Require(expected>0,"Native level3 contains its two-layer boxes");
                var sprite=Resources.Load<Sprite>("Recovered/Sprite/Items-BoxItem-box_1");int observed=0;
                foreach(var view in UnityEngine.Object.FindObjectsOfType<OriginalBoxItemSpriteView>())
                    if(view.Model!=null&&view.Model.TiledId==22)
                    {Require(view.Model.LayerCount==2&&view.baseView.sprite==sprite,"Real layered-box model and native sprite bind in level3");observed++;}
                Require(observed==expected&&game.State.IsPlaying(),"All original two-layer boxes populate actual level3");
                report.original_level3_two_layer_boxes_bound=true;Capture("core_flow_level3.png");report.passed=true;Finish();
            }
        }
        private static int ObserveAttemptSeed(int expectedAttempt, int expectedSeed)
        {
            // Original ARM64 ResetDifficultySeed with neutral identity inputs.
            int attempt = controller.Progress.LevelTryCount;
            int seed = controller.Gameplay.GetLevelRandom().DifficultySeed;
            Require(attempt == expectedAttempt && seed == expectedSeed,
                $"Level {controller.CurrentLevelNumber} starts at attempt {expectedAttempt}, seed {expectedSeed}; observed {attempt}, {seed}.");
            return seed;
        }

        internal static AudioSource RequirePlayingMusic(FoundationController owner)
        {
            var audio = owner.GetComponentInChildren<OriginalGameAudioHost>();
            var music = audio.MusicSource;
            Require(music != audio.SoundSource && music.loop && Mathf.Approximately(music.volume, 1f) &&
                music.clip != null && music.clip.loadType == AudioClipLoadType.Streaming,
                "The formal scene uses a separate looping source and the streamed original asset.");
            Require(music.isPlaying && music.timeSamples > 0, "The native level track is playing and has advanced.");
            return music;
        }

        private static bool IsKingIdle(OriginalKingVisual king) =>
            (king.CurrentAnimation=="idle_loop_2" || king.CurrentAnimation=="idle_loop_3") && !king.IsAnimationLooping;

        private static void ObserveWinBackdrop(CoreGameFlow owner)
        {
            if (report.remaining_moves_backdrop_reused) return;
            if (winBackdrop == null)
            {
                var view = owner.UiCamera.GetComponentInChildren<CoreDialogBackground>();
                if (view == null) return;
                winBackdrop = view.GetComponent<SpriteRenderer>();
            }
            if (winBackdrop.color.a > 0f && winBackdrop.color.a < .8f)
                report.win_logo_backdrop_fade = true;
            if (owner.RemainingMoves.IsRemainingMovesActive)
            {
                Require(RequireSingleBackdrop(owner.UiCamera) == winBackdrop &&
                    Mathf.Approximately(winBackdrop.color.a, 0f),
                    "Remaining moves reuse the logo backdrop with the original transparent start and end alpha.");
                if (closeWinWithBack)
                {
                    owner.PressBack();
                    Require(!owner.IsTransitioning && owner.RemainingMoves.IsRemainingMovesActive &&
                        owner.WinDialog == null && owner.LevelQuitDialog == null,
                        "Back leaves the authored remaining-moves sequence in progress.");
                    report.back_respected_remaining_moves = true;
                }
                report.remaining_moves_backdrop_reused = true;
            }
        }

        private static void ObserveWinPresentation(CoreGameFlow owner)
        {
            var result = owner.WinDialog;
            if (result == null || !result.Lifecycle.IsShowing || report.win_curved_text_settled) return;
            if (winTextLayers == null)
            {
                var text = result.GetComponentInChildren<OriginalWinTextAnimation>();
                Require(text != null, "The formal result prefab binds its curved text animator.");
                winTextLayers = text.GetComponentsInChildren<TextMeshPro>();
                Require(winTextLayers.Length == 4, "The original foreground and three outline layers participate.");
                winStar = text.transform.parent.Find("Star");
                var authored = new SerializedObject(result);
                winPillow = (Animator)authored.FindProperty("pillowAnimator").objectReferenceValue;
                var audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
                Require(winStar != null && winPillow != null && audio.GameAssets.GetClip(360) != null &&
                    audio.GameAssets.GetClip(359) != null, "The original star, pillow and both audio clips are present.");
                winAudio = audio.Playback;
            }
            maxResultFrameDelta = Mathf.Max(maxResultFrameDelta, Time.deltaTime);
            report.win_star_hit_time = winAudio.GetClipLastPlayedTime(360);
            report.win_idle_loop_time = winAudio.GetClipLastPlayedTime(359);
            if (!report.win_pillow_started && winPillow.GetCurrentAnimatorStateInfo(0).IsName("LevelWinPillowAnimation"))
            {
                report.win_pillow_started = true;
                report.win_pillow_delay = Time.time - report.win_star_hit_time;
            }
            Require(winAudio.GetLoopCount(359) == (result.IsAnimationWorking ? 0 : 1),
                "Only the finished star sequence owns the idle loop.");

            bool partial = true, overshoot = true, settled = true;
            foreach (var layer in winTextLayers)
            {
                var info = layer.textInfo;
                float first = -1f, last = 0f;
                bool curved = false;
                for (int i = 0; i < info.characterCount; i++)
                {
                    var c = info.characterInfo[i];
                    if (!c.isVisible) continue;
                    var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                    var edge = vertices[c.vertexIndex + 1] - vertices[c.vertexIndex];
                    var unwarped = c.topLeft - c.bottomLeft;
                    last = edge.magnitude / unwarped.magnitude;
                    if (first < 0f) first = last;
                    settled &= Mathf.Abs(last - 1f) < .02f;
                    curved |= Mathf.Abs(edge.x - unwarped.x) > .01f;
                }
                partial &= first > .1f && last < .01f;
                overshoot &= first > 1.1f;
                settled &= first >= 0f && curved;
            }
            if (partial && !report.win_text_stagger)
            {
                report.win_text_stagger = true;
                report.win_first_glyph_delay = Time.time - report.win_star_hit_time;
                Capture("core_flow_win_text_enter.png");
            }
            report.win_text_overshoot |= overshoot;
            report.win_curved_text_settled = settled;
            if (settled)
                Require(!result.IsAnimationWorking && Quaternion.Angle(winStar.localRotation, Quaternion.identity) < .01f &&
                    Mathf.Approximately(winStar.localScale.x, 1.66f), "The star is back at the authored settled pose.");
        }

        private static SpriteRenderer RequireSingleBackdrop(Camera camera)
        {
            var layers = camera.GetComponentsInChildren<CoreDialogBackground>(true);
            Require(layers.Length == 1 && layers[0].gameObject.activeSelf, "The normal sequence has one visible backdrop slot.");
            return layers[0].GetComponent<SpriteRenderer>();
        }

        private static void RequireNoBackdrop(Camera camera) =>
            Require(camera.GetComponentInChildren<CoreDialogBackground>(true) == null,
                "The previous scene's backdrop was released during reload.");

        private static void ObserveKingWin()
        {
            var king=openingKing;
            string clip=king.CurrentAnimation;
            if (clip=="k-1sevinme_6")
            {
                Require(!king.IsAnimationLooping && king.AnimationTime>=lastKingWinTime,
                    "The original celebration advances once without restarting.");
                lastKingWinTime=king.AnimationTime;
                report.king_win_reaction=clip;
                if (king.IsAnimationMixing && king.AnimationTime>0f) report.king_win_mixed=true;
            }
            else if (!string.IsNullOrEmpty(report.king_win_reaction))
            {
                Require(clip=="idle_connect_1" || clip=="idle_connect_2",
                    "The celebration is followed by its two original idle connectors.");
                if (king.IsAnimationMixing && king.AnimationTime>0f) report.king_connector_mixed=true;
                if (!king.IsAnimationLooping) report.king_win_connector=clip;
                else
                {
                    Require(!string.IsNullOrEmpty(report.king_win_connector) && report.king_win_connector!=clip,
                        "The first connector finishes once and the other holds as a loop.");
                    report.king_win_loop=clip;
                }
            }
        }
        private static void VerifyCap(string file)
        {
            using(var progress=new CoreLevelProgress(file,100))
            {
                progress.CompleteLevel(100);Require(progress.IsComplete&&progress.LevelToPlay==100,"Completed core catalog never stores101");
                using(var db=OriginalRuntimeStorage.Open(progress.DatabasePath))Require(db.Find("Level").Value=="100","Final-level cap is persisted");
                bool rejected=false;try{progress.CompleteLevel(101);}catch(InvalidDataException){rejected=true;}Require(rejected,"Out-of-scope completion is rejected");
                progress.StartOver();Require(!progress.IsComplete&&progress.LevelToPlay==1,"Explicit Play Again resets to1");
            }
        }
        internal static void RequireCurvedTitle(Component dialog, string expected, float scale)
        {
            var curve = dialog.GetComponentInChildren<OriginalCurvedDialogTitle>();
            Require(curve != null, "The active dialog uses its authored title curve.");
            var title = curve.GetComponent<TextMeshPro>();
            title.ForceMeshUpdate();
            var info = title.textInfo;
            Require(title.text == expected && info.lineCount == 1, "The full title fits on one line: " + expected);
            float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;
                minimum = Mathf.Min(minimum, c.vertex_BL.position.x);
                maximum = Mathf.Max(maximum, c.vertex_TR.position.x);
            }
            float highestOffset = 0f;
            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;
                var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                int vertex = c.vertexIndex;
                Vector3 source = c.vertex_BL.position;
                Vector3 edge = c.vertex_TL.position - source;
                Vector3 renderedEdge = vertices[vertex + 1] - vertices[vertex];
                Require(Mathf.Abs(edge.magnitude - renderedEdge.magnitude) < .001f, "The curve preserves glyph scale.");
                var pivot = new Vector3((source.x + c.vertex_TR.position.x) * .5f, c.baseLine, 0f);
                var rotation = Quaternion.AngleAxis(Vector2.SignedAngle(edge, renderedEdge), Vector3.forward);
                float offset = (vertices[vertex] - pivot - rotation * (source - pivot)).y;
                // Cubic Hermite interpolation of the two original keys with zero endpoint values.
                float t = Mathf.InverseLerp(minimum, maximum, pivot.x);
                float complement = 1f - t;
                float expectedOffset = scale * t * complement * (.18817352f * complement + .20729297f * t);
                Require(Mathf.Abs(offset - expectedOffset) < .001f, "The rendered title uses its original curve strength: " + expected);
                highestOffset = Mathf.Max(highestOffset, offset);
            }
            Require(highestOffset > .05f, "The title mesh visibly arches.");
        }

        internal static void Click(Button button,Camera camera,bool repeat=true)
        {
            Require(button!=null&&button.interactable&&button.gameObject.activeInHierarchy,"Actual visible standard Button is ready");
            var graphic=button.targetGraphic;Require(graphic!=null&&graphic.isActiveAndEnabled&&graphic.color.a>0,"Button targets its actual visible Graphic");
            var point=camera.WorldToScreenPoint(graphic.rectTransform.TransformPoint(graphic.rectTransform.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Require(hits.Count>0,"Standard EventSystem finds the visible Button surface");
            var handler=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Require(handler==button.gameObject,"Top raycast reaches the same visual Button: "+(hits.Count>0?hits[0].gameObject.name:"none"));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
            if (repeat) ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        private static void Capture(string name) => Capture(controller.CoreFlow.UiCamera, name);
        private static void Capture(Camera camera, string name)
        {
            var target=new RenderTexture(720,1280,24);var old=camera.targetTexture;var active=RenderTexture.active;
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(720,1280,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,720,1280),0,0);image.Apply();File.WriteAllBytes(name,image.EncodeToPNG());
            camera.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private static void Finish()
        {
            EditorApplication.update-=Tick;SceneManager.sceneLoaded-=SceneLoaded;Application.logMessageReceived-=ObserveLog;GoalUiVerification.OnFinished-=GoalsFinished;
            File.WriteAllText("live_core_flow_verification.json",JsonUtility.ToJson(report,true)+"\n");
        }
        [Serializable]private sealed class Report
        {
            public bool passed,production_win_remaining_result_bound,actual_next_level_loaded,production_zero_move_failure_bound,actual_retry_reloaded_same_level,cap100_and_restart_verified,core_booster_inventory_purchase_ui_absent;
            public string king_win_reaction, king_win_connector, king_win_loop, king_fail_reaction;
            public bool king_win_mixed, king_connector_mixed, king_fail_mixed;
            public bool win_logo_backdrop_fade, remaining_moves_backdrop_reused, result_and_failure_backdrops_cleared_on_reload;
            public bool back_respected_remaining_moves, back_closed_win, back_respected_failure_dialog;
            public bool castle_garden_background_survives_continue_and_retry, retry_reused_scene_pool_and_hud, retry_recycled_tnt_and_fuse;
            public bool cold_entry_verified, cold_entry_board_captured, continue_entry_verified, retry_entry_verified;
            public bool application_random_survives_continue;
            public bool music_stopped_at_win_logo, music_stopped_at_failure;
            public string opening_music, continue_music, retry_music;
            public int paused_music_samples;
            public bool out_of_moves_banner_center_verified, out_of_moves_standard_button_skip_verified;
            public float out_of_moves_auto_duration, out_of_moves_skip_duration;
            public bool win_text_stagger, win_text_overshoot, win_curved_text_settled, win_pillow_started;
            public bool win_title_curve_verified, fail_title_curve_verified;
            public float win_star_hit_time, win_idle_loop_time, win_pillow_delay, win_first_glyph_delay;
            public bool original_level3_two_layer_boxes_bound,formal_mock_ad_preview_disconnected;
            public int level1_attempt_seed, level2_attempt_seed, level2_retry_seed, level2_second_retry_seed, level3_attempt_seed;
            public int win_left_moves,win_remaining_reward;public string failure;
            public string scope="The formal scene uses controlled TNT and rocket placements plus recorded gestures to complete level 1. The production win and remaining-moves flow persist progress before the result exit loads level 2. Two explicit zero-budget MoveManager stimuli exercise automatic and Button-skipped banner exits, actual defeat and retry. The King plays the authored celebration and its two connectors on victory, then a single sad reaction on defeat. A separate private progress file exercises the level 100 boundary.";
        }
    }
}
