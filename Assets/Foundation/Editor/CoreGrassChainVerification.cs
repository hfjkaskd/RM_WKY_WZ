using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreGrassChainVerification
    {
        private const string ReportPath = "live_core_grass_chain_verification.json";
        private enum Step { Level4Win, Reload, FirstSwap, Rocket, FinishGrass }
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalGoalTuple grassGoal;
        private static OriginalGrassBoardItem darkTarget;
        private static OriginalFillingCell interiorFill;
        private static OriginalGridCell from, to;
        private static readonly Samples samples = new Samples();
        private static Step step;
        private static int level, previousSceneOwner, frame, sample, fixtureTaps;
        private static float started, steadySince;
        private static bool wonLevel4;
        private static Report report;

        public static void Run(FoundationController owner)
        {
            controller = owner;
            report = new Report();
            level = 4;
            step = Step.Level4Win;
            frame = -1;
            started = Time.realtimeSinceStartup;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorApplication.update += Tick;
            GrassGameplayVerification.Run(owner, _ => wonLevel4 = true);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            controller = UnityEngine.Object.FindObjectOfType<FoundationController>();
        }

        private static void Tick()
        {
            if (Time.frameCount == frame) return;
            frame = Time.frameCount;
            try
            {
                Require(Time.realtimeSinceStartup - started < 175f, "Timed out at level " + level + ", " + step);
                Advance();
            }
            catch (Exception error)
            {
                report.failure = error.ToString();
                Finish();
            }
        }

        private static void Advance()
        {
            if (step == Step.Level4Win)
            {
                if (wonLevel4) Continue();
                return;
            }
            if (step == Step.Reload)
            {
                if (controller == null || controller.GetInstanceID() == previousSceneOwner || controller.Gameplay == null) return;
                BeginLevel();
                return;
            }

            ObserveRefillAndDamage();
            var guide = controller.CoreFlow.DirectedTutorial;
            if (guide != null && guide.IsGuiding && guide.FocusCell != null)
            {
                if (!guide.DismissButton.interactable) return;
                listener.StopTouch();
                if (sample < 3) sample = 0;
                CoreFlowVerification.Click(guide.DismissButton, controller.CoreFlow.UiCamera);
                report.dynamic_guides_dismissed++;
                return;
            }
            if (sample < 3)
            {
                var position = (sample == 0 ? from : to).GetViewPosition();
                samples.Position = new Vector2(position.x, position.y);
                samples.Down = sample == 0;
                samples.Held = sample < 2;
                samples.Up = sample == 2;
                listener.ManualUpdate();
                sample++;
                return;
            }
            if (step == Step.FinishGrass && grassGoal.IsGoalCompleted())
            {
                var dialog = controller.CoreFlow.WinDialog;
                if (dialog == null || !dialog.Lifecycle.IsShowing || dialog.IsAnimationWorking) return;
                Require(grassGoal.LeftCountUi == 0 && game.Session.State == 3 &&
                    controller.CoreFlow.RemainingMoves.CollectionCount > 0, "Grass goal, win and remaining moves completed.");
                if (level == 5) report.level5_tnt_taps = fixtureTaps;
                else report.level6_tnt_taps = fixtureTaps;
                Continue();
                return;
            }
            if (!IsSettled()) return;
            if (step == Step.FirstSwap)
            {
                VerifyFirstSwap();
                return;
            }
            if (step == Step.Rocket)
            {
                VerifyRocket();
                return;
            }

            Require(fixtureTaps < 16 && game.CanUserMakeMove(),
                $"Grass completion fixture has {fixtureTaps} taps, {game.Moves.LeftMoves} moves and {grassGoal.LeftCount} targets left.");
            var center = FindTntCell();
            Require(center != null, "A movable item can host the next TNT stimulus.");
            PlaceSpecial(center, 40);
            fixtureTaps++;
            Gesture(center, center);
        }

        private static void Continue()
        {
            int next = level + 1;
            Require(controller.Progress.LevelToPlay == next, "Production progress advances before Continue.");
            using (var db = OriginalRuntimeStorage.Open(controller.Progress.DatabasePath))
                Require(db.Find("Level").Value == next.ToString(), "Next level is in the isolated SQLite save.");
            previousSceneOwner = controller.GetInstanceID();
            CoreFlowVerification.Click(controller.CoreFlow.WinDialog.ContinueButton, controller.CoreFlow.UiCamera);
            level = next;
            step = Step.Reload;
        }

        private static void BeginLevel()
        {
            game = controller.Gameplay;
            Require(controller.CurrentLevelNumber == level && game.Session.Number == level &&
                game.WinFail == controller.CoreFlow.WinFail && game.State.IsPlaying(), "Continue loads the production level.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var definition = OriginalLevelReader.Load("Levels/level_" + level);
            Require(game.Session.Name == definition.Name && game.Moves.LeftMoves == definition.Move, "Original identity and move budget.");
            int holes = 0, empty = 0, dark = 0, light = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                int index = OriginalLevelRules.GetIndexFromPoint(x, y, definition.Grid.Width);
                int raw = definition.Grid.Items[index];
                var item = cell.Mediation.CurrentItem;
                var grass = Grass(cell);
                int layer = definition.Grid.Cells[index].Grass;
                if (raw == 0 || raw == 11)
                {
                    Require(!cell.IsNormalCell && item == null && grass == null, "A native gap has no model.");
                    holes++;
                }
                else if (raw == 13)
                {
                    Require(cell.IsNormalCell && item == null, "A normal empty cell starts without a movable item.");
                    empty++;
                }
                else if (raw == 22)
                    Require(item is OriginalBoxBoardItem && item.LayerCount == 2, "Original two-layer box.");
                else
                    Require(item is OriginalMatchBoardItem match && match.MatchType == raw, "Original fixed match item.");
                Require(cell.CellView.front.gameObject.activeSelf == cell.IsNormalCell &&
                    cell.CellView.background.gameObject.activeSelf == cell.IsNormalCell, "Visual cells follow native geometry.");
                for (int side = 1; side < 8; side += 2)
                    Require(cell.CellView.border.borderRenderers[side].gameObject.activeSelf ==
                        (cell.IsNormalCell && cell.Neighbors.Get(side)?.IsNormalCell != true), "Original visible board edge.");
                if (layer == 0)
                {
                    Require(grass == null, "No grass beyond its original positions.");
                    continue;
                }
                Require(grass != null && grass.LayerCount == layer - 30 && grass.CurrentCell == cell &&
                    grass.HasView && Mathf.Approximately(grass.ItemView.shadow.color.a, layer == 32 ? .9f : .6f),
                    "Grass layer and view match the decoded cell.");
                Require(grass.ItemView.baseView.sprite.name.Contains(layer == 32 ? "dark_base" : "light_base"), "Original grass sprite family.");
                if (layer == 32) dark++;
                else light++;
            }
            grassGoal = Array.Find(board.Goals.GetFinalGoals(), goal => goal.GoalType == 13);
            Require(grassGoal != null && grassGoal.LeftCount == dark + light && grassGoal.LeftCountUi == dark + light,
                "The original grass target counts cells.");
            if (level == 5)
            {
                Require(holes == 8 && empty == 0 && light == 73 && dark == 0, "Level 5 original irregular board.");
                interiorFill = board.Grid.GetCell(4, 6) as OriginalFillingCell;
                Require(interiorFill != null && interiorFill.FillType == 1 &&
                    !board.Grid.GetCell(4, 7).IsNormalCell, "Original interior refill source immediately under the gap.");
                report.level5_loaded = true;
                Capture("core_grass_level5.png");
            }
            else if (level == 6)
            {
                Require(holes == 9 && empty == 0 && dark == 36 && light == 36, "Level 6 original split board and grass layers.");
                darkTarget = Grass(board.Grid.GetCell(2, 8));
                report.level6_loaded = true;
                Capture("core_grass_level6.png");
            }
            else
            {
                Require(holes == 0 && empty == 32 && dark == 32 && light == 0 &&
                    board.Goals.GetGoalLeftCount(11) == 17, "Level 7 has its empty lower cells, layered boxes and grass.");
                report.level7_loaded = true;
                Capture("core_grass_level7.png");
            }
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            device.Unbind();
            listener = new OriginalGameTouchListener(samples, game);
            fixtureTaps = 0;
            step = Step.FirstSwap;
            if (level == 5) Gesture(board.Grid.GetCell(3, 7), board.Grid.GetCell(3, 6));
            else if (level == 6) Gesture(board.Grid.GetCell(1, 7), board.Grid.GetCell(1, 8));
            else Gesture(board.Grid.GetCell(2, 6), board.Grid.GetCell(2, 5));
        }

        private static void ObserveRefillAndDamage()
        {
            if (level == 5 && interiorFill != null)
            {
                var item = interiorFill.Mediation.TargetItem;
                if ((item?.FallComponent as OriginalFallComponentState)?.FillingCell == interiorFill)
                    report.level5_interior_refill = true;
            }
            if (level == 6 && !report.level6_first_layer && darkTarget.LayerCount == 1)
            {
                Require(darkTarget.CurrentCell == board.Grid.GetCell(2, 8) && darkTarget.HasView &&
                    darkTarget.ItemView.baseView.sprite.name.Contains("light_base") &&
                    Mathf.Approximately(darkTarget.ItemView.shadow.color.a, .6f), "First hit changes the existing grass to its light view.");
                Require(grassGoal.LeftCount == 72 && grassGoal.LeftCountUi == 72, "A first hit on double-layer grass preserves the target count.");
                report.level6_first_layer = true;
            }
        }

        private static void VerifyFirstSwap()
        {
            int initialMoves = level == 6 ? 35 : level == 5 ? 30 : 25;
            Require(game.AcceptedSwaps == 1 && game.Moves.LeftMoves == initialMoves - 1 && game.CreatedRefills > 0,
                "The original fixed board accepted a recorded natural swap and spent one move.");
            if (level == 5)
            {
                Require(report.level5_interior_refill && Grass(interiorFill) == null && grassGoal.LeftCount <= 70,
                    "The match hits the interior filling cell, clears grass and refills from that cell.");
                CheckFullySettled();
                Capture("core_grass_level5_refill.png");
                step = Step.FinishGrass;
                return;
            }
            if (level == 6)
            {
                Require(report.level6_first_layer, "The natural match hit the dark grass.");
                CheckFullySettled();
                Capture("core_grass_level6_light.png");
                if (!darkTarget.HasView)
                {
                    VerifyFinalGrassHit();
                    return;
                }
                PlaceRocket(2, 0);
            }
            else
            {
                Require(board.Grid.GetCell(0, 4).Mediation.CurrentItem is OriginalBoxBoardItem box && box.LayerCount == 1 &&
                    board.Grid.GetCell(0, 3).Mediation.CurrentItem == null, "A damaged box still blocks the cavity.");
                PlaceRocket(0, 5);
            }
            step = Step.Rocket;
        }

        private static void VerifyRocket()
        {
            if (level == 6)
            {
                Require(game.Moves.LeftMoves == 33, "The rocket tap spends one move.");
                CheckFullySettled();
                VerifyFinalGrassHit();
                return;
            }
            var opened = board.Grid.GetCell(0, 3);
            Require(game.Moves.LeftMoves == 23 && !(board.Grid.GetCell(0, 4).Mediation.CurrentItem is OriginalBoxBoardItem) &&
                opened.Mediation.CurrentItem != null && opened.Mediation.CurrentItem == opened.Mediation.TargetItem &&
                board.Goals.GetGoalLeftCount(11) < 17, "Opening the original box barrier refills a formerly empty cell.");
            report.level7_box_cavity_refilled = true;
            Capture("core_grass_level7_refill.png");
            report.passed = true;
            Finish();
        }

        private static void VerifyFinalGrassHit()
        {
            Require(!darkTarget.HasView && darkTarget.CurrentCell == null && Grass(board.Grid.GetCell(2, 8)) == null &&
                grassGoal.LeftCount < 72 && grassGoal.LeftCount == grassGoal.LeftCountUi, "Final hit removes the grass and updates the HUD.");
            report.level6_final_layer = true;
            step = Step.FinishGrass;
        }

        private static void CheckFullySettled()
        {
            int grassCount = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                Require((item != null) == cell.IsNormalCell && item == cell.Mediation.NextItem && item == cell.Mediation.TargetItem,
                    "Normal cells refill and original gaps remain empty at " + x + "," + y);
                if (Grass(cell) != null) grassCount++;
            }
            Require(grassCount == grassGoal.LeftCount && grassCount == grassGoal.LeftCountUi, "Grass and HUD counts agree after settling.");
        }

        private static void PlaceRocket(int x, int minY)
        {
            for (int y = board.Grid.Height - 1; y >= minY; y--)
            {
                var source = board.Grid.GetCell(x, y);
                if (!(source.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                PlaceSpecial(source, 20);
                Gesture(source, source);
                return;
            }
            throw new InvalidOperationException("The rocket stimulus requires an existing match item in the original column.");
        }

        private static OriginalGridCell FindTntCell()
        {
            OriginalGridCell best = null;
            int highest = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var source = board.Grid.GetCell(x, y);
                if (!(source.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                int count = 0;
                for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (Math.Abs(dx) == 2 && Math.Abs(dy) == 2) continue;
                    var cell = board.Grid.GetCell(x + dx, y + dy);
                    if (cell != null && Grass(cell) != null) count++;
                }
                if (count > highest) { highest = count; best = source; }
            }
            return best;
        }

        private static void PlaceSpecial(OriginalGridCell cell, int tiledId)
        {
            var old = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            old.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)old.SwapView);
            game.CreateItemAtCell(tiledId, cell);
        }

        private static OriginalGrassBoardItem Grass(OriginalGridCell cell) =>
            cell.StaticTouch.GetStaticItem(2) as OriginalGrassBoardItem;

        private static void Gesture(OriginalGridCell source, OriginalGridCell destination)
        {
            from = source;
            to = destination;
            sample = 0;
            steadySince = -1f;
        }

        private static bool IsSettled()
        {
            if (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            {
                steadySince = -1f;
                return false;
            }
            if (steadySince < 0f) steadySince = Time.realtimeSinceStartup;
            return Time.realtimeSinceStartup - steadySince > .6f;
        }

        private static void Capture(string file) => GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, file);

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (device != null && device.Listener == null) device.Bind(game);
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
            public bool passed, level5_loaded, level5_interior_refill, level6_loaded, level6_first_layer, level6_final_layer;
            public bool level7_loaded, level7_box_cavity_refilled;
            public int level5_tnt_taps, level6_tnt_taps, dynamic_guides_dismissed;
            public string failure;
            public string scope = "The actual original core scene starts at level 4 via isolated SQLite. Real result buttons advance into levels 5–7. Recorded natural swaps exercise the fixed original boards, with explicit TNT and rocket placements for follow-up paths. Level 7 checks the originally blocked empty cells and their refill after the box barrier opens. Full natural solves, device input and original rendered parity remain outside this run.";
        }
    }
}
