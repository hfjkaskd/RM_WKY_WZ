using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Command-line test transport only. Opens the unchanged production scene;
    // it keeps runtime services/initialization unchanged. Core startup no longer
    // opens the peripheral account/area database; legacy account tests own temp DBs.
    [InitializeOnLoad]
    public static class BackgroundVerification
    {
        private const string ActiveKey = "Recovery.BackgroundVerification.Active";
        private const string TestKey = "Recovery.BackgroundVerification.Test";
        private const string StartedKey = "Recovery.BackgroundVerification.Started";
        private const string InvokedKey = "Recovery.BackgroundVerification.Invoked";
        private const string TimeoutKey = "Recovery.BackgroundVerification.Timeout";
        private static string runtimeError;

        static BackgroundVerification()
        {
            if (Application.isBatchMode && SessionState.GetBool(ActiveKey, false)) Bind();
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the isolated batch verification project.");
            string[] args = Environment.GetCommandLineArgs();
            string test = null;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-recoveryVerification") test = args[i + 1];
            GetReportName(test); // Reject unsupported names before changing editor state.
            int timeout = 0;
            int timeoutIndex = Array.IndexOf(args, "-recoveryTimeoutSeconds");
            if (timeoutIndex >= 0 && (timeoutIndex + 1 == args.Length ||
                !int.TryParse(args[timeoutIndex + 1], out timeout) || timeout <= 0))
                throw new ArgumentException("The verification timeout must be a positive number of seconds.");
            if (test == "core-directed-tutorial-author")
            {
                CoreDirectedTutorialAuthoring.Run();
                File.WriteAllText("background_verification.json", "{\"passed\":true,\"test\":\"core-directed-tutorial-author\"}\n");
                EditorApplication.Exit(0); return;
            }
            if (test == "core-modal-tutorial-author")
            {
                CoreUiAuthoring.RunModalTutorial();
                File.WriteAllText("background_verification.json", "{\"passed\":true,\"test\":\"core-modal-tutorial-author\"}\n");
                EditorApplication.Exit(0); return;
            }
            if (test == "core-no-moves-author")
            {
                CoreUiAuthoring.RunNoPossibleMoves();
                File.WriteAllText("background_verification.json", "{\"passed\":true,\"test\":\"core-no-moves-author\"}\n");
                EditorApplication.Exit(0); return;
            }
            if (test == "core-ui-author")
            {
                CoreUiAuthoring.Run();
                File.WriteAllText("background_verification.json", "{\"passed\":true,\"test\":\"core-ui-author\",\"scope\":\"Explicit independent-project prefab authoring; production scene is not played.\"}\n");
                EditorApplication.Exit(0); return;
            }
            if (File.Exists(ReportPath(test))) File.Delete(ReportPath(test));
            SessionState.SetString(TestKey, test);
            SessionState.SetFloat(StartedKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetInt(TimeoutKey, timeout);
            SessionState.SetBool(InvokedKey, false);
            SessionState.SetBool(ActiveKey, true);
            Bind();
            bool grass = test == "core-grass" || test == "core-grass-chain";
            bool cupboard = test is "core-cupboard" or "core-early-chapter" or "core-cupboard-footprint" or "core-potion-footprint";
            bool randomChapter = test == "core-random-chapter" || test == "core-random-opening";
            bool coin = test == "core-coin";
            bool mail = test is "core-mail" or "core-mail-batch" or "core-mail-target-score";
            bool mixedChapter = test == "core-mixed-chapter";
            bool eggChapter = test == "core-egg-chapter";
            bool potionChapter = test == "core-potion-chapter";
            bool vaseChapter = test == "core-vase-chapter";
            bool owlChapter = test == "core-owl-chapter";
            bool bushChapter = test is "core-bush-chapter" or "core-bush-coin-opening" or "core-bush-burst";
            bool honeyChapter = test == "core-honey-chapter" || test == "core-honey-opening";
            bool safeChapter = test == "core-safe-chapter" || test == "core-safe-opening" || test == "core-level100";
            bool modalTutorials = test == "core-modal-tutorials";
            bool directedTutorials = test == "core-directed-tutorials";
            bool optionalTutorials = test == "core-optional-tutorials";
            bool hammerTutorial = test == "core-hammer";
            bool arrowTutorial = test == "core-arrow";
            bool cannonTutorial = test == "core-cannon";
            bool jesterTutorial = test == "core-jester";
            bool lightballTapGuide = test == "core-lightball-tap-guide";
            bool dynamicPowerGuide = test == "core-dynamic-power-guides";
            bool recolorShuffle = test == "core-recolor-shuffle";
            bool bonusShuffle = test == CoreBonusShuffleVerification.TestName;
            bool matchHints = test is "core-match-hints" or "core-power-hints" or "core-progress-pause";
            bool kingWorry = test == "core-king-worry";
            bool kingMailWorry = test == "core-king-mail-worry";
            bool kingSafeWorry = test == "core-king-safe-worry";
            bool kingCombo = test == "core-king-combo" || test == "core-king-ballball";
            bool kingMixedLightball = test is "core-king-lightball-tnt" or "core-king-lightball-propeller" or "core-king-lightball-rocket";
            bool lightballTntOverlap = test == LightballTntComboVerification.OverlapTest;
            bool kingPropeller = test.StartsWith("core-king-propeller-", StringComparison.Ordinal);
            bool dialogBackground = test == "core-dialog-background";
            bool authoredOpenings = test is "core-authored-openings" or "core-natural-attempts";
            bool honeyVaseReveal = test == "core-honey-vase-reveal";
            bool portrait = test is "game-scene" or "camera-projection" or "life-helper" ||
                test is "core-catalog" or "core-flow" or "core-flow-back" || test == "core-attempt-resume" || test == "core-opening-checkpoint" || test == "core-level-settings" || authoredOpenings || dialogBackground || test == "core-no-moves" || test == "core-box-chapter" || grass || cupboard || randomChapter || coin || mail ||
                mixedChapter || eggChapter || potionChapter || vaseChapter || owlChapter || bushChapter || honeyChapter || honeyVaseReveal || safeChapter || test == "core-merge-first-frame" ||
                modalTutorials || directedTutorials || optionalTutorials || matchHints || lightballTapGuide || dynamicPowerGuide || recolorShuffle || bonusShuffle ||
                hammerTutorial || arrowTutorial || cannonTutorial || jesterTutorial || kingWorry || kingMailWorry || kingSafeWorry ||
                kingCombo || kingMixedLightball || kingPropeller || lightballTntOverlap;
            EditorSceneManager.OpenScene(portrait || test == "square-background" ? "Assets/Foundation/Scenes/GameScene.unity" :
                "Assets/Foundation/Scenes/RecoveryPreview.unity", OpenSceneMode.Single);
            if (portrait) GameSceneVerification.ConfigurePortraitPreview();
            if (test == "square-background") PlayModeWindow.SetCustomRenderingResolution(480, 480, "Royal Match Square");
            if (test is "game-scene" or "core-flow" or "core-flow-back") GameSceneVerification.VerifyAuthoredBackground();
            if (test == "core-attempt-resume") GrassGameplayVerification.SeedProgress(CoreAttemptResumeVerification.ReadLevel());
            if (test == "core-opening-checkpoint" && !CoreOpeningCheckpointVerification.Reopening &&
                CoreAttemptResumeVerification.ReadLevel() > 1)
                GrassGameplayVerification.SeedProgress(CoreAttemptResumeVerification.ReadLevel());
            if (grass || optionalTutorials) GrassGameplayVerification.SeedProgress();
            if (hammerTutorial) GrassGameplayVerification.SeedProgress(8);
            if (arrowTutorial) GrassGameplayVerification.SeedProgress(14);
            if (cannonTutorial) GrassGameplayVerification.SeedProgress(17);
            if (jesterTutorial) GrassGameplayVerification.SeedProgress(19);
            if (test == "core-box-chapter" || test == "core-level-settings" || matchHints || kingWorry || kingCombo || kingPropeller || dialogBackground)
                GrassGameplayVerification.SeedProgress(test == "core-king-ballball" ? 3 : 2);
            if (kingMixedLightball) GrassGameplayVerification.SeedProgress(83);
            if (lightballTntOverlap) GrassGameplayVerification.SeedProgress(3);
            if (cupboard) GrassGameplayVerification.SeedProgress(test switch
            {
                "core-cupboard" => 11, "core-cupboard-footprint" => 16, "core-potion-footprint" => 59, _ => 7
            });
            if (randomChapter) GrassGameplayVerification.SeedProgress(test == "core-random-opening" ? 15 : 12);
            if (coin) GrassGameplayVerification.SeedProgress(20);
            if (mail || modalTutorials || kingMailWorry) GrassGameplayVerification.SeedProgress(test switch
            {
                "core-mail-batch" => 55, "core-mail-target-score" => 94, _ => 21
            });
            if (mixedChapter) GrassGameplayVerification.SeedProgress(22);
            if (authoredOpenings)
                GrassGameplayVerification.SeedProgress(CoreAuthoredOpeningVerification.ReadRequestedLevels(
                    test == "core-natural-attempts" ? 1 : 22, allowRetries: test == "core-natural-attempts")[0]);
            if (lightballTapGuide || recolorShuffle || test == "core-no-moves") GrassGameplayVerification.SeedProgress(55);
            if (dynamicPowerGuide) GrassGameplayVerification.SeedProgress(5);
            if (bonusShuffle) GrassGameplayVerification.SeedProgress(80);
            if (eggChapter) GrassGameplayVerification.SeedProgress(31);
            if (potionChapter) GrassGameplayVerification.SeedProgress(41);
            if (vaseChapter) GrassGameplayVerification.SeedProgress(51);
            if (owlChapter) GrassGameplayVerification.SeedProgress(61);
            if (bushChapter) GrassGameplayVerification.SeedProgress(test == "core-bush-coin-opening" ? 80 : 71);
            if (honeyChapter) GrassGameplayVerification.SeedProgress(81);
            if (test == "core-merge-first-frame") GrassGameplayVerification.SeedProgress(83);
            if (test == "core-guided-score-cache") GrassGameplayVerification.SeedProgress(99);
            if (honeyVaseReveal) GrassGameplayVerification.SeedProgress(85);
            if (safeChapter || kingSafeWorry) GrassGameplayVerification.SeedProgress(test == "core-level100" ? 100 : 91);
            EditorApplication.EnterPlaymode();
        }

        private static void Bind()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= ObserveLog;
            Application.logMessageReceived += ObserveLog;
        }

        private static void ObserveLog(string message, string stack, LogType type)
        {
            if (runtimeError == null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                runtimeError = message + "\n" + stack;
        }

        private static void Tick()
        {
            string test = SessionState.GetString(TestKey, "");
            bool coreFlow = test is "core-flow" or "core-flow-back";
            try
            {
                if (runtimeError != null) throw new InvalidOperationException(runtimeError);
                int timeout = SessionState.GetInt(TimeoutKey, 0);
                if (timeout == 0)
                    timeout = test == "core-egg-chapter" || test == "core-potion-chapter" ||
                        test == "core-vase-chapter" || test == "core-owl-chapter" || test == "core-bush-chapter" || test == "core-honey-chapter" || test == "core-safe-chapter" || test == "core-authored-openings" || test == "core-natural-attempts" ? 800 :
                        (test == "core-random-chapter" || test == "core-coin" || test == "core-mixed-chapter" ? 540 : 300);
                if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedKey, 0f) > timeout)
                    throw new TimeoutException("Background production-scene verification timed out.");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                if (!SessionState.GetBool(InvokedKey, false))
                {
                    var host = UnityEngine.Object.FindObjectOfType<OriginalContextUpdateHost>();
                    var controller = UnityEngine.Object.FindObjectOfType<FoundationController>();
                    if (host == null || controller == null) return;
                    if (test == "core-opening-checkpoint")
                    {
                        if (!controller.CoreFlow.IsEnteringLevel) return;
                        SessionState.SetBool(InvokedKey, true);
                        CoreOpeningCheckpointVerification.Run(controller);
                        return;
                    }
                    if (coreFlow && controller.CoreFlow.IsEnteringLevel)
                        CoreFlowVerification.ObserveInitialEntrance(controller);
                    if (test == "core-directed-tutorials" && controller.CoreFlow.IsEnteringLevel)
                        CoreDirectedTutorialVerification.ObserveInitialEntrance(controller);
                    if (Time.frameCount < 3 || host.CompletedUpdateCount < 3 || controller.Gameplay == null) return;
                    bool capturesGameScene = coreFlow || test is "game-scene" or "core-catalog" or "camera-projection" or "life-helper";
                    if (capturesGameScene && controller.CoreFlow.IsEnteringLevel) return;
                    SessionState.SetBool(InvokedKey, true);
                    // Legacy per-kernel fixtures explicitly replace the core
                    // flow owner. Core gameplay checks keep the production end chain.
                    if (!capturesGameScene && test != "square-background" && test != "core-attempt-resume" && test != "core-level-settings" && test != "core-authored-openings" && test != "core-natural-attempts" && test != "core-dialog-background" && test != "core-no-moves" && test != "core-box-chapter" && test != "core-grass" && test != "core-grass-chain" &&
                        test is not ("core-cupboard" or "core-early-chapter" or "core-cupboard-footprint" or "core-potion-footprint") &&
                        test != "core-random-chapter" && test != "core-random-opening" && test != "core-coin" &&
                        test is not ("core-mail" or "core-mail-batch" or "core-mail-target-score") && test != "core-mixed-chapter" && test != "core-egg-chapter" &&
                        test != "core-potion-chapter" && test != "core-vase-chapter" && test != "core-owl-chapter" &&
                        test is not ("core-bush-chapter" or "core-bush-coin-opening" or "core-bush-burst") &&
                        test is not ("core-honey-chapter" or "core-honey-opening" or "core-honey-vase-reveal" or "core-merge-first-frame") &&
                        test is not ("core-safe-chapter" or "core-safe-opening" or "core-level100" or "core-guided-score-cache") &&
                        test != "core-modal-tutorials" && test != "core-directed-tutorials" && test != "core-optional-tutorials" &&
                        test is not ("core-match-hints" or "core-power-hints" or "core-progress-pause" or "core-lightball-tap-guide" or "core-dynamic-power-guides") &&
                        test != "core-king-worry" && test != "core-king-mail-worry" && test != "core-king-safe-worry" &&
                        test != "core-king-combo" && test != "core-king-ballball" &&
                        test is not ("core-king-lightball-tnt" or "core-king-lightball-propeller" or "core-king-lightball-rocket" or LightballTntComboVerification.OverlapTest) &&
                        !test.StartsWith("core-king-propeller-", StringComparison.Ordinal) &&
                        test != "core-recolor-shuffle" && test != CoreBonusShuffleVerification.TestName && test != "core-hammer" && test != "core-arrow" && test != "core-cannon" && test != "core-jester")
                        controller.CoreFlow.Release();
                    switch (test)
                    {
                        case "core-no-moves": CoreNoPossibleMovesVerification.Run(controller); break;
                        case "core-level-settings": CoreLevelSettingsVerification.Run(controller); break;
                        case "core-attempt-resume": CoreAttemptResumeVerification.Run(controller); break;
                        case "core-box-chapter": CoreMixedChapterVerification.Run(controller, 3); break;
                        case "core-flow":
                        case "core-flow-back":
                            CoreCatalogVerification.Run();RequireSupplement("live_core_catalog_verification.json");
                            GameSceneVerification.Run();RequireSupplement("live_game_scene_verification.json");
                            CoreFlowVerification.Run(test == "core-flow-back");break;
                        case "core-authored-openings": CoreAuthoredOpeningVerification.Run(controller); break;
                        case "core-natural-attempts": CoreNaturalAttemptVerification.Run(controller); break;
                        case "core-dialog-background": CoreDialogBackgroundVerification.Run(controller); break;
                        case "core-grass": GrassGameplayVerification.Run(controller); break;
                        case "core-grass-chain": CoreGrassChainVerification.Run(controller); break;
                        case "core-coin": CoreCoinVerification.Run(controller); break;
                        case "core-mail": CoreMailVerification.Run(controller); break;
                        case "core-mail-batch": CoreMailVerification.RunFlightBatch(controller); break;
                        case "core-mail-target-score": CoreMailVerification.VerifyTargetScore(controller); break;
                        case "core-guided-score-cache": CoreMixedChapterVerification.VerifyGuidedScoreCache(controller); break;
                        case "core-modal-tutorials": CoreModalTutorialVerification.Run(controller); break;
                        case "core-directed-tutorials": CoreDirectedTutorialVerification.Run(controller); break;
                        case "core-optional-tutorials": CoreOptionalTutorialVerification.Run(controller); break;
                        case "core-hammer": CoreHammerVerification.Run(controller); break;
                        case "core-arrow": CoreArrowVerification.Run(controller); break;
                        case "core-cannon": CoreCannonVerification.Run(controller); break;
                        case "core-jester": CoreJesterVerification.Run(controller); break;
                        case "core-match-hints": CoreMatchHintVerification.Run(controller); break;
                        case "core-power-hints": CorePowerHintVerification.Run(controller); break;
                        case "core-progress-pause": CorePowerHintVerification.Run(controller, pauseAfterTap: true); break;
                        case "core-king-worry": CoreKingWorryVerification.Run(controller); break;
                        case "core-king-mail-worry": CoreKingWorryVerification.Run(controller, 21); break;
                        case "core-king-safe-worry": CoreKingWorryVerification.Run(controller, 91); break;
                        case "core-king-combo":
                        case "core-king-ballball": CoreKingComboVerification.Run(controller, test == "core-king-ballball"); break;
                        case "core-king-lightball-tnt":
                        case "core-king-lightball-propeller":
                        case "core-king-lightball-rocket": CoreKingComboVerification.RunMixedLightball(controller, test); break;
                        case LightballTntComboVerification.OverlapTest: LightballTntComboVerification.RunOverlap(controller); break;
                        case "core-king-propeller-single":
                        case "core-king-propeller-pair":
                        case "core-king-propeller-tnt":
                        case "core-king-propeller-rocket": CoreKingPropellerVerification.Run(controller, test); break;
                        case "core-lightball-tap-guide": CoreLightballTapGuideVerification.Run(controller); break;
                        case "core-dynamic-power-guides": CoreDynamicPowerTutorialVerification.Run(controller); break;
                        case "core-recolor-shuffle": CoreRecolorShuffleVerification.Run(controller); break;
                        case CoreBonusShuffleVerification.TestName: CoreBonusShuffleVerification.Run(controller); break;
                        case "core-mixed-chapter":
                        case "core-egg-chapter":
                        case "core-potion-chapter":
                        case "core-vase-chapter":
                        case "core-owl-chapter":
                        case "core-bush-coin-opening":
                        case "core-bush-chapter": CoreMixedChapterVerification.Run(controller); break;
                        case "core-bush-burst": CoreMixedChapterVerification.VerifyBushBurst(controller); break;
                        case "core-honey-opening": CoreMixedChapterVerification.Run(controller, 81); break;
                        case "core-honey-vase-reveal": CoreMixedChapterVerification.VerifyHoneyVaseReveal(controller); break;
                        case "core-merge-first-frame": CoreMixedChapterVerification.VerifyMergeFirstFrame(controller); break;
                        case "core-honey-chapter": CoreMixedChapterVerification.Run(controller, 90); break;
                        case "core-safe-opening": CoreMixedChapterVerification.Run(controller, 91); break;
                        case "core-level100":
                        case "core-safe-chapter": CoreMixedChapterVerification.Run(controller, 100); break;
                        case "core-cupboard":
                        case "core-random-opening":
                        case "core-random-chapter":
                        case "core-early-chapter": CoreCupboardVerification.Run(controller); break;
                        case "core-cupboard-footprint":
                        case "core-potion-footprint": CoreCupboardVerification.RunFootprint(controller); break;
                        case "game-start": GameStartVerification.Run(); break;
                        case "initial-population": GameStartVerification.Run(); LivePlayerInputVerification.Run(); break;
                        case "possible-moves": GameStartVerification.Run(); PossibleMoveVerification.Run(); LivePlayerInputVerification.Run(); break;
                        case "generated-counts": GameStartVerification.Run(); GeneratedCountVerification.Run(); LivePlayerInputVerification.Run(); break;
                        case "game-scene":
                            GameSceneVerification.Run();
                            controller.CoreFlow.Release();
                            LivePlayerInputVerification.Run();
                            break;
                        case "core-catalog":
                            CoreCatalogVerification.Run(); RequireSupplement("live_core_catalog_verification.json");
                            GameSceneVerification.Run(); RequireSupplement("live_game_scene_verification.json");
                            controller.CoreFlow.Release();
                            LivePlayerInputVerification.Run();
                            break;
                        case "camera-projection":
                            CameraProjectionVerification.Run();
                            GameSceneVerification.Run();
                            controller.CoreFlow.Release();
                            LivePlayerInputVerification.Run();
                            break;
                        case "square-background": CameraProjectionVerification.RunSquareBackground(); break;
                        case "life-helper":
                            LifeHelperVerification.Run(); RequireSupplement("live_life_helper_verification.json");
                            GameStartVerification.Run(); RequireSupplement("live_game_start_verification.json");
                            GameSceneVerification.Run(); RequireSupplement("live_game_scene_verification.json");
                            controller.CoreFlow.Release();
                            LivePlayerInputVerification.Run();
                            break;
                        case "life-clock-win":
                            LifeClockVerification.Run(); RequireSupplement("live_life_clock_verification.json");
                            RemainingCoinsVerification.RunNormal(); break;
                        case "area-data": AreaDataVerification.Run(); break;
                        case "lightball-presentation": LightballUsePresentationVerification.Run(); break;
                        case "player-input":
                            GameSceneVerification.VerifyPreviewBackground();
                            LivePlayerInputVerification.Run();
                            break;
                        case "ego-continuation": EgoContinuationVerification.Run(); break;
                        case "propeller-targets": PropellerTargetVerification.Run(); break;
                        case "propeller-tap": LivePlayerInputVerification.RunPropellerTap(); break;
                        case "propeller-swap": LivePlayerInputVerification.RunPropellerSwap(); break;
                        case "tnt-tap": LivePlayerInputVerification.RunTntTap(); break;
                        case "tnt-swap": LivePlayerInputVerification.RunTntSwap(); break;
                        case "tnt-tnt-combo": TntTntComboVerification.Run(); break;
                        case "rocket-rocket-combo": RocketRocketComboVerification.Run(); break;
                        case "tnt-rocket-combo": TntRocketComboVerification.Run(); break;
                        case "goal-ui": GoalUiVerification.Run(); break;
                        case "win-logo-visual": WinLogoVisualVerification.Run(); break;
                        case "win-logo-skip": RemainingCoinsVerification.RunLogoSkip(); break;
                        case "remaining-moves-exploder": RemainingMovesExploderVerification.Run(); break;
                        case "remaining-moves-replacement": RemainingMovesReplacementVerification.Run(); break;
                        case "remaining-coins-normal": RemainingCoinsVerification.RunNormal(); break;
                        case "remaining-coins-skip": RemainingCoinsVerification.RunSkip(); break;
                        case "remaining-coins-early-skip": RemainingCoinsVerification.RunEarlySkip(); break;
                        case "propeller-pair-combo": PropellerPairComboVerification.Run(); break;
                        case "tnt-propeller-combo": TntPropellerComboVerification.Run(); break;
                        case "ball-ball-combo": BallBallComboVerification.Run(); break;
                        case "propeller-rocket-combo": PropellerRocketComboVerification.Run(); break;
                        case "lightball-tnt-combo": LightballTntComboVerification.Run(); break;
                        case "lightball-rocket-combo": LightballRocketComboVerification.Run(); break;
                        case "lightball-combos": LightballComboRegressionVerification.Run(); break;
                        case "lightball-propeller-combo": LightballPropellerComboVerification.Run(); break;
                        case "lightball-combo-regression": LightballComboRegressionVerification.RunWithPropellers(); break;
                        case "lightball-tap": LivePlayerInputVerification.RunLightballTap(); break;
                        case "lightball-swap": LivePlayerInputVerification.RunLightballSwap(); break;
                        case "rocket-horizontal-tap": LivePlayerInputVerification.RunRocketHorizontalTap(); break;
                        case "rocket-horizontal-swap": LivePlayerInputVerification.RunRocketHorizontalSwap(); break;
                        case "rocket-vertical-tap": LivePlayerInputVerification.RunRocketVerticalTap(); break;
                        case "rocket-vertical-swap": LivePlayerInputVerification.RunRocketVerticalSwap(); break;
                    }
                }
                if (!File.Exists(ReportPath(test))) return;
                var result = JsonUtility.FromJson<TestResult>(File.ReadAllText(ReportPath(test)));
                if (result.status == "running") return;
                if (!result.passed) throw new InvalidOperationException(result.failure ?? "Verification did not pass.");
                Finish(test, true, null);
            }
            catch (Exception error) { Finish(test, false, error.ToString()); }
        }

        private static void Finish(string test, bool passed, string failure)
        {
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= ObserveLog;
            var report = new BatchReport { passed = passed, test = test, failure = failure };
            File.WriteAllText(Path.Combine(Application.dataPath, "../background_verification.json"), JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("Background production-scene verification finished: " + test + ", passed=" + passed);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void RequireSupplement(string name)
        {
            var report = JsonUtility.FromJson<TestResult>(File.ReadAllText(Path.Combine(Application.dataPath, "../" + name)));
            if (!report.passed) throw new InvalidOperationException(name + ": " + report.failure);
        }
        private static string ReportPath(string test) => Path.Combine(Application.dataPath, "../" + GetReportName(test));
        private static string GetReportName(string test)
        {
            switch (test)
            {
                case "game-start": return "live_game_start_verification.json";
                case "initial-population": return "live_player_input_verification.json";
                case "possible-moves": return "live_player_input_verification.json";
                case "generated-counts": return "live_player_input_verification.json";
                case "game-scene": return "live_player_input_verification.json";
                case "camera-projection": return "live_player_input_verification.json";
                case "square-background": return "live_square_background_verification.json";
                case "core-catalog":return "live_player_input_verification.json";
                case "core-ui-author":return "core_ui_authoring.json";
                case "core-modal-tutorial-author": return "core_modal_tutorial_authoring.json";
                case "core-directed-tutorial-author": return "core_directed_tutorial_authoring.json";
                case "core-modal-tutorials": return "live_core_modal_tutorials_verification.json";
                case "core-directed-tutorials": return "live_core_directed_tutorials_verification.json";
                case "core-optional-tutorials": return "live_core_optional_tutorials_verification.json";
                case "core-match-hints": return "live_core_match_hint_verification.json";
                case "core-power-hints":
                case "core-progress-pause": return "live_core_power_hint_verification.json";
                case "core-king-worry": return "live_core_king_worry_verification.json";
                case "core-king-mail-worry": return "live_core_king_mail_worry_verification.json";
                case "core-king-safe-worry": return "live_core_king_safe_worry_verification.json";
                case "core-king-combo": return "live_core_king_combo_verification.json";
                case "core-king-ballball": return "live_core_king_ballball_verification.json";
                case LightballTntComboVerification.OverlapTest: return LightballTntComboVerification.OverlapReportPath;
                case "core-king-lightball-tnt":
                case "core-king-lightball-propeller":
                case "core-king-lightball-rocket":
                case "core-king-propeller-single":
                case "core-king-propeller-pair":
                case "core-king-propeller-tnt":
                case "core-king-propeller-rocket": return "live_" + test.Replace('-', '_') + "_verification.json";
                case "core-hammer": return "live_core_hammer_verification.json";
                case "core-arrow": return "live_core_arrow_verification.json";
                case "core-cannon": return "live_core_cannon_verification.json";
                case "core-jester": return "live_core_jester_verification.json";
                case "core-lightball-tap-guide": return "live_core_lightball_tap_guide_verification.json";
                case "core-dynamic-power-guides": return "live_core_dynamic_power_tutorial_verification.json";
                case "core-recolor-shuffle": return "live_core_recolor_shuffle_verification.json";
                case CoreBonusShuffleVerification.TestName: return CoreBonusShuffleVerification.ReportPath;
                case "core-no-moves-author": return "core_no_moves_authoring.json";
                case "core-no-moves": return "live_core_no_moves_verification.json";
                case "core-level-settings": return CoreLevelSettingsVerification.ReportPath;
                case "core-attempt-resume": return CoreAttemptResumeVerification.ReportPath;
                case "core-opening-checkpoint": return CoreOpeningCheckpointVerification.ReportPath;
                case "core-box-chapter": return "live_core_box_chapter_verification.json";
                case "core-flow":
                case "core-flow-back": return "live_core_flow_verification.json";
                case "core-authored-openings": return CoreAuthoredOpeningVerification.ReportPath;
                case "core-natural-attempts": return CoreNaturalAttemptVerification.ReportPath;
                case "core-dialog-background": return "live_core_dialog_background_verification.json";
                case "core-grass": return "live_grass_gameplay_verification.json";
                case "core-grass-chain": return "live_core_grass_chain_verification.json";
                case "core-cupboard":
                case "core-early-chapter": return "live_core_cupboard_verification.json";
                case "core-cupboard-footprint":
                case "core-potion-footprint": return CoreCupboardVerification.FootprintReportPath;
                case "core-random-opening":
                case "core-random-chapter": return "live_core_random_chapter_verification.json";
                case "core-coin": return "live_core_coin_verification.json";
                case "core-mail": return "live_core_mail_verification.json";
                case "core-mail-batch": return CoreMailVerification.BatchReportPath;
                case "core-mail-target-score": return CoreMailVerification.TargetScoreReportPath;
                case "core-guided-score-cache": return CoreMixedChapterVerification.GuidedScoreCacheReportPath;
                case "core-mixed-chapter": return "live_core_mixed_chapter_verification.json";
                case "core-egg-chapter": return "live_core_egg_chapter_verification.json";
                case "core-potion-chapter": return "live_core_potion_chapter_verification.json";
                case "core-vase-chapter": return "live_core_vase_chapter_verification.json";
                case "core-owl-chapter": return "live_core_owl_chapter_verification.json";
                case "core-bush-chapter": return "live_core_bush_chapter_verification.json";
                case "core-bush-coin-opening": return "live_core_bush_chapter_verification.json";
                case "core-bush-burst": return CoreMixedChapterVerification.BushBurstReportPath;
                case "core-honey-chapter":
                case "core-honey-opening": return "live_core_honey_chapter_verification.json";
                case "core-honey-vase-reveal": return CoreMixedChapterVerification.HoneyVaseRevealReportPath;
                case "core-merge-first-frame": return CoreMixedChapterVerification.MergeFirstFrameReportPath;
                case "core-safe-chapter":
                case "core-safe-opening":
                case "core-level100": return "live_core_safe_chapter_verification.json";
                case "life-helper": return "live_player_input_verification.json";
                case "life-clock-win": return "live_remaining_coins_normal_verification.json";
                case "area-data": return "live_area_data_verification.json";
                case "lightball-presentation": return "lightball_use_presentation_verification.json";
                case "player-input": return "live_player_input_verification.json";
                case "ego-continuation": return "live_ego_continuation_verification.json";
                case "propeller-targets": return "live_propeller_targets_verification.json";
                case "propeller-tap": return "live_propeller_tap_verification.json";
                case "propeller-swap": return "live_propeller_swap_verification.json";
                case "tnt-tap": return "live_tnt_tap_verification.json";
                case "tnt-swap": return "live_tnt_swap_verification.json";
                case "tnt-tnt-combo": return "live_tnt_tnt_combo_verification.json";
                case "rocket-rocket-combo": return "live_rocket_rocket_combo_verification.json";
                case "tnt-rocket-combo": return "live_tnt_rocket_combo_verification.json";
                case "goal-ui": return "live_goal_ui_verification.json";
                case "win-logo-visual": return "live_win_logo_visual_verification.json";
                case "win-logo-skip": return "live_remaining_coins_logo_skip_verification.json";
                case "remaining-moves-exploder": return "live_remaining_moves_exploder_verification.json";
                case "remaining-moves-replacement": return "live_remaining_moves_replacement_verification.json";
                case "remaining-coins-normal": return "live_remaining_coins_normal_verification.json";
                case "remaining-coins-skip": return "live_remaining_coins_skip_verification.json";
                case "remaining-coins-early-skip": return "live_remaining_coins_early_skip_verification.json";
                case "propeller-pair-combo": return "live_propeller_pair_combo_verification.json";
                case "tnt-propeller-combo": return "live_tnt_propeller_combo_verification.json";
                case "ball-ball-combo": return "live_ball_ball_combo_verification.json";
                case "propeller-rocket-combo": return "live_propeller_rocket_combo_verification.json";
                case "lightball-tnt-combo": return "live_lightball_tnt_combo_verification.json";
                case "lightball-rocket-combo": return "live_lightball_rocket_combo_verification.json";
                case "lightball-combos": return "live_lightball_combos_verification.json";
                case "lightball-propeller-combo": return "live_lightball_propeller_combo_verification.json";
                case "lightball-combo-regression": return "live_lightball_combo_regression_verification.json";
                case "lightball-tap": return "live_lightball_tap_verification.json";
                case "lightball-swap": return "live_lightball_swap_verification.json";
                case "rocket-horizontal-tap": return "live_rocket_horizontal_tap_verification.json";
                case "rocket-horizontal-swap": return "live_rocket_horizontal_swap_verification.json";
                case "rocket-vertical-tap": return "live_rocket_vertical_tap_verification.json";
                case "rocket-vertical-swap": return "live_rocket_vertical_swap_verification.json";
                default: throw new ArgumentException("Unknown background verification name.", nameof(test));
            }
        }
        [Serializable] private sealed class TestResult { public bool passed; public string status, failure; }
        [Serializable] private sealed class BatchReport
        {
            public bool passed;
            public string test, failure;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Isolated Unity batch editor opens the unchanged formal scene and runs existing explicit verifiers after real runtime startup. No editor runtime fallback or gameplay substitutes. Does not verify physical pointer input or rendered-original parity.";
        }
    }
}
