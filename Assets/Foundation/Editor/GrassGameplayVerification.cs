using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class GrassGameplayVerification
    {
        private const string ReportPath = "live_grass_gameplay_verification.json";
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static Samples input;
        private static OriginalGridCell from, to;
        private static OriginalPropellerBoardItem propeller;
        private static OriginalGrassBoardItem flightTarget;
        private static OriginalGrassItemView[] openingGrassViews;
        private static OriginalGoalTuple grassGoal;
        private static int sample, frame, phase;
        private static float started, steadySince;
        private static Report report;
        private static Action<FoundationController> onWin;

        public static void SeedProgress(int level = 4)
        {
            var settings = AssetDatabase.LoadAssetAtPath<CoreFlowSettings>("Assets/Foundation/Settings/CoreFlowSettings.asset");
            Require(settings != null && settings.progressFileName.StartsWith("CoreLevels_verify_", StringComparison.Ordinal),
                "Use the isolated core-progress transport before selecting a level.");
            using var progress = new CoreLevelProgress(settings.progressFileName, 1);
            if (level == 1) progress.StartOver();
            else progress.CompleteLevel(level - 1);
        }

        public static void Run(FoundationController owner, Action<FoundationController> verifiedWin = null)
        {
            onWin = verifiedWin;
            controller = owner;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            game = controller.Gameplay;
            report = new Report();
            Require(controller.CurrentLevelNumber == 4 && game.Session.Name == "00004v02_L" &&
                game.WinFail == controller.CoreFlow.WinFail && game.Moves.LeftMoves == 30, "Actual core level 4 is running.");
            var level = OriginalLevelReader.Load("Levels/level_4");
            Require(board.Grid.Width == 9 && board.Grid.Height == 9 && board.Goals.GetGoalUiLeftCount(13) == 49 &&
                board.InitialItemAmounts.GetStaticItemAmount(2) == 49, "Original dimensions, grass count and HUD.");
            var goals = board.Goals.GetFinalGoals();
            Require(goals.Length == 1 && goals[0].GoalType == 13, "Original level 4 grass goal.");
            grassGoal = goals[0];
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                int i = OriginalLevelRules.GetIndexFromPoint(x, y, level.Grid.Width);
                var item = cell.Mediation.CurrentItem as OriginalMatchBoardItem;
                Require(item != null && item.MatchType == level.Grid.Items[i], "Original fixed match item at " + x + "," + y);
                if (item.MatchType == 5) report.gems++;
                var grass = Grass(cell);
                if (level.Grid.Cells[i].Grass == 0)
                {
                    Require(grass == null, "No grass outside the original target region.");
                    continue;
                }
                Require(grass != null && grass.LayerCount == 1 && grass.HasView && grass.CurrentCell == cell &&
                    !cell.IsFallBlocked() && !grass.IsTouchBlocking, "Original single-layer grass stays below movable tiles.");
                var view = grass.ItemView;
                Require(view.baseView.sprite != null && view.shadow.sprite != null &&
                    view.baseView.sortingOrder == -25850 + x + (y - board.Layout.CurrentBoard.Bottom) * 10 &&
                    view.baseView.sortingLayerID == SortingLayer.NameToID("Item") &&
                    Vector3.Distance(view.transform.position, cell.GetViewPosition()) < .001f &&
                    Mathf.Approximately(view.shadow.color.a, .6f), "Grass view matches its world position and original sorting.");
                report.initial_grass++;
            }
            Require(report.initial_grass == 49 && report.gems == 17, "All 49 original grass views and 17 gems populated.");
            openingGrassViews = new OriginalGrassItemView[3];
            for (int i = 0; i < openingGrassViews.Length; i++)
            {
                var view = Grass(board.Grid.GetCell(i + 2, 1)).ItemView;
                Require(view.topPatch.enabled && view.rightPatch.enabled, "The opening grass views have visible neighbor patches.");
                openingGrassViews[i] = view;
            }
            Capture(controller.CoreFlow.UiCamera, "grass_initial.png");
            device.Unbind();
            input = new Samples();
            listener = new OriginalGameTouchListener(input, game);
            started = Time.realtimeSinceStartup;
            frame = -1;
            phase = 0;
            StartGesture(board.Grid.GetCell(5, 1), board.Grid.GetCell(4, 1));
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static OriginalGrassBoardItem Grass(OriginalGridCell cell) =>
            cell.StaticTouch.GetStaticItem(2) as OriginalGrassBoardItem;

        private static void Tick()
        {
            if (Time.frameCount == frame) return;
            frame = Time.frameCount;
            try { Step(); }
            catch (Exception error) { report.failure = error.ToString(); Finish(); }
        }

        private static void Step()
        {
            Require(Time.realtimeSinceStartup - started < 130f, "Timed out waiting for level 4 phase " + phase);
            if (phase == 0 && sample == 0)
            {
                var tutorial = controller.CoreFlow.DirectedTutorial;
                if (tutorial == null || !tutorial.IsGuiding || !tutorial.DismissButton.interactable) return;
                Require(from.IsTouchEnabled && to.IsTouchEnabled && !board.Grid.GetCell(0, 1).IsTouchEnabled,
                    "The original optional guide permits the grass-region opening gesture.");
            }
            if (propeller?.IsInFlight == true && propeller.TargetItem is OriginalGrassBoardItem target)
            {
                Require(target.IncomingExploderCount > 0, "In-flight propeller owns a real grass reservation.");
                flightTarget = target;
            }
            if (sample < 3)
            {
                var point = (sample == 0 ? from : to).GetViewPosition();
                input.Position = new Vector2(point.x, point.y);
                input.Down = sample == 0;
                input.Held = sample < 2;
                input.Up = sample == 2;
                listener.ManualUpdate();
                sample++;
                return;
            }

            if (phase == 3)
            {
                var dialog = controller.CoreFlow.WinDialog;
                if (dialog == null || !dialog.Lifecycle.IsShowing || dialog.IsAnimationWorking) return;
                Require(grassGoal.LeftCount == 0 && grassGoal.LeftCountUi == 0 && game.Session.State == 3 &&
                    controller.Progress.LevelToPlay == 5, "The completed grass goal advances through the real win flow.");
                var goals = board.Goals.GetCurrentGoals();
                Require(goals.Length == 1 && goals[0].GoalType == 21, "The production remaining flow replaces completed goals with its coin display.");
                Require(game.Session.ExtraCoins > 0 && game.Session.LeftMoves > 0 &&
                    controller.CoreFlow.RemainingMoves.CollectionCount > 0 &&
                    !controller.CoreFlow.RemainingMoves.IsRemainingMovesActive &&
                    dialog.ContinueButton.interactable, "Remaining moves were converted before the result button unlocks.");
                using (var db = OriginalRuntimeStorage.Open(controller.Progress.DatabasePath))
                    Require(db.Find("Level").Value == "5", "Core progress actually persists level 5.");
                report.remaining_moves = game.Session.LeftMoves;
                report.remaining_move_reward = game.Session.ExtraCoins;
                report.result_goal_replacement_verified = true;
                Capture(controller.CoreFlow.UiCamera, "grass_win.png");
                report.passed = true;
                Finish();
                onWin?.Invoke(controller);
                return;
            }

            if (phase == 2 && board.Goals.IsAllGoalsReached())
            {
                phase = 3;
                return;
            }
            if (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            {
                steadySince = -1;
                return;
            }
            if (steadySince < 0) steadySince = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - steadySince < .55f) return;
            CheckSettledBoard();

            if (phase == 0)
            {
                var tutorial = controller.CoreFlow.DirectedTutorial;
                Require((tutorial == null || !tutorial.IsGuiding) && board.Grid.GetCell(0, 1).IsTouchEnabled,
                    "The actual opening match ends the optional guide and releases the outer cells.");
                Require(game.AcceptedSwaps == 1 && game.Moves.LeftMoves == 29 && game.CreatedRefills > 0,
                    "A real adjacent swap clears pieces, consumes one move and spawns refills.");
                for (int i = 0; i < openingGrassViews.Length; i++)
                {
                    Require(Grass(board.Grid.GetCell(i + 2, 1)) == null, "Direct match cleared the original grass below it.");
                    var view = openingGrassViews[i];
                    Require(!view.gameObject.activeSelf && view.viewParent.gameObject.activeSelf &&
                        !view.rightPatch.enabled && !view.topPatch.enabled,
                        "Recycled grass has an inactive root, a reset child and disabled patches.");
                    report.recycled_grass_views++;
                }
                report.grass_after_swap = board.Goals.GetGoalLeftCount(13);
                report.refills_after_swap = game.CreatedRefills;
                Require(report.grass_after_swap <= 46 && report.grass_after_swap > 0, "Direct grass hits counted.");
                Capture(controller.CoreFlow.UiCamera, "grass_after_match.png");
                var source = ChooseSpecialCell();
                propeller = (OriginalPropellerBoardItem)PlaceSpecial(source, 10);
                phase = 1;
                StartGesture(source, source);
                return;
            }
            if (phase == 1)
            {
                Require(game.Moves.LeftMoves == 28 && propeller.IsExploded && !propeller.IsInFlight &&
                    flightTarget != null && !flightTarget.HasView && flightTarget.IncomingExploderCount == 0,
                    "A recorded propeller tap spends one move and resolves its reserved grass target.");
                report.propeller_hit = true;
                phase = 2;
            }

            Require(report.fixture_tnt_taps < 16 && game.CanUserMakeMove(), "Remaining grass is reachable within the move budget.");
            var center = ChooseSpecialCell();
            PlaceSpecial(center, 40);
            report.fixture_tnt_taps++;
            StartGesture(center, center);
        }

        private static void CheckSettledBoard()
        {
            int grassCount = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                Require(item != null && item == cell.Mediation.NextItem && item == cell.Mediation.TargetItem &&
                    !cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "Refills settle every level 4 cell.");
                var grass = Grass(cell);
                if (grass == null) continue;
                Require(grass.CurrentCell == cell && grass.LayerCount == 1 && grass.IncomingExploderCount == 0,
                    "Uncleared grass stays fixed and reservations are released.");
                grassCount++;
            }
            Require(grassCount == board.Goals.GetGoalLeftCount(13) && grassCount == board.Goals.GetGoalUiLeftCount(13),
                "Remaining grass matches model and HUD goal counts.");
        }

        private static OriginalGridCell ChooseSpecialCell()
        {
            OriginalGridCell best = null;
            int highest = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!(cell.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                int hits = 0;
                for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (Math.Abs(dx) == 2 && Math.Abs(dy) == 2) continue;
                    var candidate = board.Grid.GetCell(x + dx, y + dy);
                    if (candidate != null && Grass(candidate) != null) hits++;
                }
                if (hits > highest) { highest = hits; best = cell; }
            }
            Require(best != null, "A match item can host the explicit special-item stimulus.");
            return best;
        }

        private static IOriginalSpecialCreationItem PlaceSpecial(OriginalGridCell cell, int tiledId)
        {
            var old = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            old.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)old.SwapView);
            return game.CreateItemAtCell(tiledId, cell);
        }

        private static void StartGesture(OriginalGridCell source, OriginalGridCell destination)
        {
            from = source;
            to = destination;
            sample = 0;
            steadySince = -1;
        }

        internal static void Capture(Camera camera, string file)
        {
            var target = RenderCapture(camera);
            try { SaveCapture(target, file); }
            finally { UnityEngine.Object.DestroyImmediate(target); }
        }

        internal static RenderTexture RenderCapture(Camera camera)
        {
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var target = new RenderTexture(720, 1280, 24);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                return target;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(target);
                throw;
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
            }
        }

        internal static void SaveCapture(RenderTexture target, string file)
        {
            var active = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(file, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            openingGrassViews = null;
            device.Bind(game);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class Samples : IOriginalGameTouchInput
        {
            public Vector2 Position;
            public bool Down, Held, Up;
            public Vector2 GetMouseWorldPosition() => Position;
            public bool GetMouseButtonDown() => Down;
            public bool GetMouseButton() => Held;
            public bool GetMouseButtonUp() => Up;
        }

        [Serializable]
        private sealed class Report
        {
            public bool passed, propeller_hit, result_goal_replacement_verified;
            public int initial_grass, gems, grass_after_swap, refills_after_swap, recycled_grass_views;
            public int fixture_tnt_taps, remaining_moves, remaining_move_reward;
            public string failure;
            public string scope = "Actual original level 4, one swap on the unmodified original board, followed by an explicitly placed propeller and TNT items activated through recorded player input. Production grass model, HUD, fall, moves, WinFail, remaining conversion, result and SQLite progress. Level 4 has only single-layer grass. Physical input, natural full-level solves, original render parity and level 5 are outside this verification.";
        }
    }
}
