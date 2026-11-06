using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreCupboardVerification
    {
        private const string PathName = "live_core_cupboard_verification.json";
        internal const string FootprintReportPath = "live_core_cupboard_footprint_verification.json";
        private const string RandomChapterPath = "live_core_random_chapter_verification.json";
        private enum Phase { Reload, BoxTap, BoxDrag, RandomSwap, ClosedTap, Door, OpenTap, Plates, Guided, Complete }
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalCupboardBoardItem target;
        private static OriginalCupboardBoardItem guidedTarget;
        private static OriginalPropellerBoardItem propeller;
        private static OriginalGameAudioHost gameAudio;
        private static OriginalGridCell from, to;
        private static OriginalGoalTuple[] originalGoals;
        private static OriginalGridCell boxCell, boxNeighbor;
        private static OriginalBoxBoardItem pressedBox;
        private static readonly Samples input = new Samples();
        private static int level, frame, sample, taps, previousOwner, platesBeforeRocket, layersBeforeFlight;
        private static float started, steadySince;
        private static Phase phase;
        private static Report report;
        private static bool randomChapter, openingOnly;

        internal static void RunFootprint(FoundationController owner)
        {
            var levelBoard = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            int levelNumber = owner.CurrentLevelNumber;
            bool potion = levelNumber == 59;
            Require(potion || levelNumber == 16, "Open the original cupboard or potion footprint board.");
            int moves = owner.Gameplay.Moves.LeftMoves;
            int collected = VerifyFootprintHit(levelBoard, potion ? levelBoard.Grid.GetCell(0, 0) : levelBoard.Grid.GetCell(3, 4),
                potion, hitProxy: false);
            int proxyCollected = VerifyFootprintHit(levelBoard, potion ? levelBoard.Grid.GetCell(2, 0) : levelBoard.Grid.GetCell(4, 2),
                potion, hitProxy: true);
            Require(owner.Gameplay.Moves.LeftMoves == moves, "The direct explosion probes spend no move.");
            File.WriteAllText(FootprintReportPath, $"{{\"passed\":true,\"level\":{levelNumber},\"footprint_cells\":4," +
                $"\"obstacle_goal_decrement\":{collected},\"proxy_obstacle_goal_decrement\":{proxyCollected}," +
                $"\"grass_layers_lost_in_same_wave\":0,\"grass_layers_lost_in_distinct_waves\":2}}\n");
        }

        private static int VerifyFootprintHit(OriginalInitialBoardView levelBoard, OriginalGridCell masterCell,
            bool potion, bool hitProxy)
        {
            var master = masterCell.Mediation.CurrentItem;
            Require(potion ? master is OriginalPotionBoardItem && master.LayerCount == 4 :
                master is OriginalCupboardBoardItem cupboard && cupboard.HasDoor && cupboard.PlateCount == 10,
                "The formal level opens with its original multi-cell obstacle.");
            int x = masterCell.X, y = masterCell.Y;
            var footprint = new[]
            {
                masterCell, levelBoard.Grid.GetCell(x + 1, y), levelBoard.Grid.GetCell(x, y + 1),
                levelBoard.Grid.GetCell(x + 1, y + 1)
            };
            foreach (var cell in footprint)
                Require(cell.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem grass && grass.LayerCount == 2,
                    "Each obstacle cell covers the original two grass layers.");

            int goal = master.GoalType;
            int total = levelBoard.Goals.GetGoalLeftCount(goal);
            int hits = master.LayerCount;
            int grassLeft = levelBoard.Goals.GetGoalLeftCount(13);
            for (int i = 1; i < hits; i++) master.Explode(OriginalExploder.Next(6));
            Require(master.LayerCount == 1, "The fixture leaves the final obstacle layer in place.");

            var wave = OriginalExploder.Next(6);
            var impactCell = hitProxy ? footprint[3] : masterCell;
            impactCell.ExplodeCurrentItem(wave);
            foreach (var cell in footprint)
            {
                Require(cell.Mediation.CurrentItem == null, "The final layer releases the entire obstacle.");
                cell.ExplodeCurrentItem(wave);
                Require(cell.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem grass && grass.LayerCount == 2,
                    $"The rocket wave from ({impactCell.X}, {impactCell.Y}) cannot damage grass revealed at ({cell.X}, {cell.Y}).");
            }
            int collected = potion ? hits : hits - 1;
            Require(levelBoard.Goals.GetGoalLeftCount(goal) == total - collected &&
                levelBoard.Goals.GetGoalLeftCount(13) == grassLeft,
                "The obstacle consumes its own goals and leaves all the grass goals in place.");
            var exposed = footprint[1];
            exposed.ExplodeCurrentItem(OriginalExploder.Next(6));
            Require(exposed.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem remaining && remaining.LayerCount == 1 &&
                levelBoard.Goals.GetGoalLeftCount(13) == grassLeft, "The next wave removes one exposed grass layer.");
            exposed.ExplodeCurrentItem(OriginalExploder.Next(6));
            Require(levelBoard.Goals.GetGoalLeftCount(13) == grassLeft - 1 &&
                levelBoard.Goals.GetGoalUiLeftCount(13) == grassLeft - 1,
                "A second distinct wave clears the grass and updates the model and HUD once.");
            return collected;
        }

        public static void Run(FoundationController owner)
        {
            controller = owner;
            report = new Report();
            level = owner.CurrentLevelNumber;
            openingOnly = level == 15;
            randomChapter = level == 12 || openingOnly;
            Require(level == 7 || level == 11 || randomChapter, "Start from the isolated chapter or cupboard checkpoint.");
            if (openingOnly)
                report.scope = "Actual GameScene at level 15. A natural swap verifies the shared difficulty stream, refill and original temporary box-blocked cavities.";
            else if (randomChapter)
                report.scope = "Actual GameScene and production Continue buttons from levels 12 through 19. Tests replay a box tap and drag, a natural swap on the generated level 15 opening, and controlled TNT taps to exercise all goals, refill and result owners.";
            started = Time.realtimeSinceStartup;
            frame = -1;
            File.WriteAllText(randomChapter ? RandomChapterPath : PathName, "{\"passed\":false,\"status\":\"running\"}\n");
            SceneManager.sceneLoaded += SceneLoaded;
            EditorApplication.update += Tick;
            BeginLevel();
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode) =>
            controller = UnityEngine.Object.FindObjectOfType<FoundationController>();

        private static void Tick()
        {
            if (Time.frameCount == frame) return;
            frame = Time.frameCount;
            try
            {
                Require(Time.realtimeSinceStartup - started < (randomChapter ? 480f : 240f), "Timed out at level " + level + ", " + phase);
                Advance();
            }
            catch (Exception error)
            {
                report.failure = error.ToString();
                Finish();
            }
        }

        private static void BeginLevel()
        {
            game = controller.Gameplay;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            var definition = OriginalLevelReader.Load("Levels/level_" + level);
            Require(game.Session.Number == level && game.Session.Name == definition.Name && game.Moves.LeftMoves == definition.Move &&
                game.WinFail == controller.CoreFlow.WinFail && game.State.IsPlaying(), "Real original chapter level is playing.");
            originalGoals = board.Goals.GetFinalGoals();
            device.Unbind();
            listener = new OriginalGameTouchListener(input, game);
            taps = 0;
            sample = 3;
            steadySince = -1;
            phase = Phase.Complete;
            if (randomChapter)
            {
                VerifyChapterBoard(definition);
                return;
            }
            if (level == 8 || level == 9 || level == 10) Capture("core_chapter_level" + level + ".png");
            if (level != 11) return;

            int masters = 0, proxies = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                int tiled = definition.Grid.Items[OriginalLevelRules.GetIndexFromPoint(x, y, definition.Grid.Width)];
                var item = cell.Mediation.CurrentItem;
                if (tiled == 51)
                {
                    var master = item as OriginalCupboardBoardItem;
                    Require(master != null && master.HasDoor && master.PlateCount == 10 &&
                        master.HasView && !master.IsFallable && !master.IsSwappable, "Master owns the original door, ten plates and one view.");
                    var sorting = master.ItemView.GetComponentInChildren<SortingGroup>(true);
                    int nativeOrder = 7000 - 200 * (y - board.Layout.CurrentBoard.Bottom);
                    Require(sorting.sortingLayerID == game.ItemSortingLayer && sorting.sortingOrder == nativeOrder,
                        $"The cupboard at ({x}, {y}) uses native row sorting {nativeOrder}, observed {sorting.sortingOrder}.");
                    masters++;
                }
                else if (tiled == 41 || tiled == 42 || tiled == 52)
                {
                    var proxy = item as OriginalCupboardFakeBoardItem;
                    int direction = tiled == 41 ? 5 : tiled == 42 ? 6 : 7;
                    Require(proxy != null && proxy.Master == cell.Neighbors.Get(direction).Mediation.CurrentItem &&
                        proxy.HasView && !proxy.IsFallable && !proxy.IsSwappable, "Proxy occupies the original tile and delegates to its master.");
                    proxies++;
                }
                else Require(item is OriginalMatchBoardItem match && match.MatchType == tiled, "Original match color.");
            }
            Require(masters == 4 && proxies == 12 && board.Presentation.Pool.GetSpawnedCount(47) == 4 &&
                originalGoals.Length == 1 && originalGoals[0].GoalType == 15 && originalGoals[0].LeftCountUi == 40,
                "Four original cupboards expose forty plate goals.");
            report.cupboards = masters;
            report.proxy_cells = proxies;
            report.cupboard_row_sorting_verified = true;
            gameAudio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            target = (OriginalCupboardBoardItem)board.Grid.GetCell(2, 5).Mediation.CurrentItem;
            Capture("core_cupboard_initial.png");
            phase = Phase.ClosedTap;
            var proxyCell = board.Grid.GetCell(2, 6);
            Gesture(proxyCell, proxyCell);
        }

        private static void VerifyChapterBoard(ConfigData.FTiledLevel definition)
        {
            var opening = new ChapterOpening { level = level, population_attempts = board.InitialPopulationAttempts };
            Require(originalGoals.Length == definition.Goals.Length, "All original goal types appear in the HUD.");
            foreach (var goal in definition.Goals)
                Require(board.Goals.GetGoalLeftCount(goal.Goal) == goal.Count &&
                    board.Goals.GetGoalUiLeftCount(goal.Goal) == goal.Count, "Original starting goal counts.");
            boxCell = null;
            boxNeighbor = null;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                int index = OriginalLevelRules.GetIndexFromPoint(x, y, board.Grid.Width);
                int id = definition.Grid.Items[index];
                var cell = board.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                var grass = cell.StaticTouch.GetStaticItem(2);
                Require((grass != null) == (definition.Grid.Cells[index].Grass != 0), "Grass occupies the original cells.");
                if (grass != null)
                {
                    opening.grass++;
                    if (item?.ItemType == 8) opening.grass_under_cupboards++;
                }
                if (OriginalInitialItemCreation.IsASet(id))
                {
                    Require(cell.IsNormalCell && item is OriginalMatchBoardItem generated && generated.HasView &&
                        !generated.IsHandGiven && Array.Exists(definition.Sets[id - 14].Elements, element => element.Id == generated.MatchType),
                        "An original set cell contains a generated match model and view from its own palette.");
                    opening.generated_colors++;
                }
                else if (OriginalMatchItemRandomizer.IsMatchItem(id))
                    Require(item is OriginalMatchBoardItem fixedMatch && fixedMatch.IsHandGiven && fixedMatch.MatchType == id && fixedMatch.HasView,
                        "The original fixed match color and hand-given flag are retained.");
                else if (id == 21)
                {
                    Require(item is OriginalBoxBoardItem box && box.HasView && !box.IsSwappable, "The original wooden box is immovable.");
                    opening.boxes++;
                    if (level != 12 || boxNeighbor != null) continue;
                    foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                    {
                        var neighbor = cell.Neighbors.Get(direction);
                        if (!(neighbor?.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                        boxCell = cell;
                        boxNeighbor = neighbor;
                        break;
                    }
                }
                else if (id == 51)
                {
                    Require(item is OriginalCupboardBoardItem cupboard && cupboard.HasDoor && cupboard.PlateCount == 10 && cupboard.HasView,
                        "The original cupboard master and view are ready.");
                    opening.cupboards++;
                }
                else if (id == 41 || id == 42 || id == 52)
                    Require(item is OriginalCupboardFakeBoardItem proxy && proxy.Master != null && proxy.HasView,
                        "The original cupboard proxy delegates to its master.");
            }
            report.chapter_openings.Add(opening);
            if (level == 12)
            {
                Require(boxCell != null && boxNeighbor != null, "An original box borders a movable match item.");
                pressedBox = (OriginalBoxBoardItem)boxCell.Mediation.CurrentItem;
                gameAudio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
                phase = Phase.BoxTap;
                Gesture(boxCell, boxCell);
            }
            else if (level == 15)
            {
                var random = game.GetLevelRandom();
                Require(opening.generated_colors == 38 && opening.boxes == 36 && opening.grass == 36 &&
                    opening.population_attempts > 0 && !board.InitialShuffle.ShouldTryAgain() && !board.Matches.IsThereAnyMatch(false),
                    "The generated original board has all 38 set cells, no automatic opening match and a legal move.");
                report.difficulty_samples_before_swap = random.DifficultySeedGenerationCount;
                Require(report.difficulty_samples_before_swap >= opening.generated_colors, "Refill starts with the difficulty stream already consumed by opening colors.");
                // This opening swipe reaches the filling columns and breaks the boxes at both ends of column 2.
                var source = board.Grid.GetCell(1, 5);
                var destination = board.Grid.GetCell(2, 5);
                Require(source.Mediation.CurrentItem is OriginalMatchBoardItem first &&
                    destination.Mediation.CurrentItem is OriginalMatchBoardItem second &&
                    HasLineAfterSwap(destination, source, first.MatchType, second.MatchType),
                    "The level 15 refill verification uses the reference opening swipe.");
                Capture("core_random_level15_opening.png");
                phase = Phase.RandomSwap;
                Gesture(source, destination);
            }
            else if (level == 16)
                Require(opening.grass_under_cupboards == 24, "All original grass is under the cupboard footprints.");
        }

        internal static bool FindNaturalSwap(OriginalInitialBoardView board, out OriginalGridCell source, out OriginalGridCell destination,
            bool allowCoins = false, Func<OriginalGridCell, bool> matchCenterFilter = null, Func<OriginalBoardItem, bool> requiredItem = null)
        {
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                var match = cell.Mediation.CurrentItem as OriginalMatchBoardItem;
                bool firstRequired = requiredItem?.Invoke(cell.Mediation.CurrentItem) == true;
                if (match == null && !firstRequired && !(allowCoins && cell.Mediation.CurrentItem is OriginalCoinBoardItem)) continue;
                for (int direction = 1; direction <= 3; direction += 2)
                {
                    var neighbor = cell.Neighbors.Get(direction);
                    var other = neighbor?.Mediation.CurrentItem as OriginalMatchBoardItem;
                    bool secondRequired = requiredItem?.Invoke(neighbor?.Mediation.CurrentItem) == true;
                    if (other == null && !secondRequired && !(allowCoins && neighbor?.Mediation.CurrentItem is OriginalCoinBoardItem)) continue;
                    if (requiredItem != null && !firstRequired && !secondRequired) continue;
                    int first = match?.MatchType ?? 0;
                    int second = other?.MatchType ?? 0;
                    if (first == second) continue;
                    bool sourceMatch = second != 0 && HasLineAfterSwap(cell, neighbor, second, first) &&
                        (matchCenterFilter == null || matchCenterFilter(cell));
                    bool destinationMatch = first != 0 && HasLineAfterSwap(neighbor, cell, first, second) &&
                        (matchCenterFilter == null || matchCenterFilter(neighbor));
                    if (!sourceMatch && !destinationMatch &&
                        (!(allowCoins || requiredItem != null) || (!HasSquareAfterSwap(cell, neighbor, second, first) &&
                            !HasSquareAfterSwap(neighbor, cell, first, second)))) continue;
                    source = cell;
                    destination = neighbor;
                    return true;
                }
            }
            source = destination = null;
            return false;
        }

        private static bool HasSquareAfterSwap(OriginalGridCell center, OriginalGridCell other, int color, int otherColor)
        {
            if (color == 0) return false;
            for (int diagonal = 0; diagonal < 8; diagonal += 2)
            {
                int first = (diagonal + 7) % 8, second = (diagonal + 1) % 8;
                if (ColorAfterSwap(center.Neighbors.Get(first), other, otherColor) == color &&
                    ColorAfterSwap(center.Neighbors.Get(second), other, otherColor) == color &&
                    ColorAfterSwap(center.Neighbors.Get(diagonal), other, otherColor) == color) return true;
            }
            return false;
        }

        private static int ColorAfterSwap(OriginalGridCell cell, OriginalGridCell other, int otherColor) =>
            cell == other ? otherColor : (cell?.Mediation.CurrentItem as OriginalMatchBoardItem)?.MatchType ?? 0;

        private static bool HasLineAfterSwap(OriginalGridCell center, OriginalGridCell other, int color, int otherColor)
        {
            return CountMatching(center, other, color, otherColor, 1) + CountMatching(center, other, color, otherColor, 5) >= 2 ||
                CountMatching(center, other, color, otherColor, 3) + CountMatching(center, other, color, otherColor, 7) >= 2;
        }

        private static int CountMatching(OriginalGridCell center, OriginalGridCell other, int color, int otherColor, int direction)
        {
            int count = 0;
            for (var cell = center.Neighbors.Get(direction); cell != null && count < 2; cell = cell.Neighbors.Get(direction))
            {
                int adjacent = ColorAfterSwap(cell, other, otherColor);
                if (adjacent != color) break;
                count++;
            }
            return count;
        }

        private static void Advance()
        {
            if (phase == Phase.Reload)
            {
                if (controller == null || controller.GetInstanceID() == previousOwner || controller.Gameplay == null) return;
                BeginLevel();
                return;
            }
            if (phase == Phase.Guided && propeller.IsInFlight && guidedTarget == null)
            {
                var hit = propeller.TargetItem;
                guidedTarget = hit as OriginalCupboardBoardItem ?? (hit as OriginalCupboardFakeBoardItem)?.Master;
                Require(guidedTarget != null && guidedTarget.IncomingExploderCount > 0, "The live propeller reserves the cupboard master.");
                Require(hit.GetMasterViewCenterPosition() == guidedTarget.GetViewPosition() + Vector3.one * .5f,
                    "The propeller aims at the original cupboard center including its depth.");
                report.guided_cupboard_center_verified = true;
                layersBeforeFlight = guidedTarget.LayerCount;
            }
            if (sample < 3)
            {
                var point = (sample == 0 ? from : to).GetViewPosition();
                input.Position = new Vector2(point.x, point.y);
                input.Down = sample == 0;
                input.Held = sample < 2;
                input.Up = sample == 2;
                listener.ManualUpdate();
                if (phase == Phase.BoxTap && sample == 0)
                {
                    var view = pressedBox.ItemView;
                    Require(view.GetSorting().Order == 7000 - 200 * boxCell.Y &&
                        Quaternion.Angle(Quaternion.identity, view.animationRoot.localRotation) > 0f &&
                        view.animationRoot.localScale.x < 1f && gameAudio.Playback.GetClipLastPlayedTime(39) > 0f,
                        "The actual box press rotates, shrinks, sorts by row and plays the recovered clip.");
                }
                sample++;
                return;
            }
            var hammerGuide = controller.CoreFlow.HammerTutorial;
            if (hammerGuide != null && hammerGuide.IsGuiding)
            {
                if (hammerGuide.IsSelected)
                {
                    var hammerTarget = board.Grid.GetCell(4, 6);
                    Gesture(hammerTarget, hammerTarget);
                }
                else
                {
                    var hammerButton = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>().Boosters[0].Button;
                    CoreFlowVerification.Click(hammerButton, controller.CoreFlow.UiCamera);
                }
                return;
            }
            if (controller.CoreFlow.BlocksBoardInput && level == 8 && hammerGuide == null) return;
            var optionalGuide = controller.CoreFlow.OptionalBoosterTutorial;
            bool cannon = level == 17;
            bool jester = level == 19;
            if (optionalGuide != null && optionalGuide.IsGuiding)
            {
                if (optionalGuide.IsSelected)
                {
                    if (jester) CoreFlowVerification.Click(optionalGuide.ConfirmationButton, controller.CoreFlow.UiCamera, false);
                    else
                    {
                        var target = board.Grid.GetCell(cannon ? 6 : 0, 6);
                        Gesture(target, target);
                    }
                }
                else
                {
                    var button = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>().Boosters[jester ? 3 : cannon ? 2 : 1].Button;
                    CoreFlowVerification.Click(button, controller.CoreFlow.UiCamera, false);
                }
                return;
            }
            if (controller.CoreFlow.BlocksBoardInput && (level == 14 || cannon || jester) && optionalGuide == null) return;
            if (phase == Phase.Complete && game.Session.State == 3)
            {
                VerifyResult();
                return;
            }
            if (!Settled()) return;
            if (phase == Phase.BoxTap)
            {
                var view = pressedBox.ItemView;
                Require(game.Moves.LeftMoves == 30 && view.GetSorting().Order == 1000 &&
                    view.animationRoot.localRotation == Quaternion.identity && view.animationRoot.localScale == Vector3.one &&
                    pressedBox.LayerCount == 1 && originalGoals.Length == 2,
                    "Box feedback restores the original pose and order with no goal or move spent.");
                report.box_tap_verified = true;
                phase = Phase.BoxDrag;
                Gesture(boxCell, boxNeighbor);
                return;
            }
            if (phase == Phase.BoxDrag)
            {
                Require(game.Moves.LeftMoves == 30 && game.AcceptedSwaps == 0 && boxCell.Mediation.CurrentItem == pressedBox &&
                    boxNeighbor.Mediation.CurrentItem is OriginalMatchBoardItem, "The box cannot be dragged into the adjacent match.");
                report.box_drag_rejected = true;
                phase = Phase.Complete;
            }
            if (phase == Phase.RandomSwap)
            {
                report.difficulty_samples_after_swap = game.GetLevelRandom().DifficultySeedGenerationCount;
                Require(game.AcceptedSwaps == 1 && game.Moves.LeftMoves == 22 &&
                    report.difficulty_samples_after_swap > report.difficulty_samples_before_swap, "Natural swap spends one move and refills using the same difficulty stream.");
                report.swap_coordinates = new[] { from.X, from.Y, to.X, to.Y };
                report.rows_after_swap = new string[board.Grid.Height];
                int gaps = 0;
                for (int y = board.Grid.Height - 1; y >= 0; y--)
                {
                    var row = new string[board.Grid.Width];
                    for (int x = 0; x < board.Grid.Width; x++)
                    {
                        var cell = board.Grid.GetCell(x, y);
                        var item = cell.Mediation.CurrentItem;
                        if (item == null)
                        {
                            Require(IsBoxBlockedCavity(cell), "A settled empty cell must be below an original box barrier.");
                            gaps++;
                        }
                        row[x] = item == null ? (cell.ShouldFindCross ? "cross" : "empty") : item.TiledId.ToString();
                    }
                    report.rows_after_swap[board.Grid.Height - 1 - y] = string.Join(" ", row);
                }
                Capture("core_random_level15_refill.png");
                Require(gaps == 4 && board.Goals.GetGoalLeftCount(11) < 36,
                    "The default opening swap refills reachable cells and leaves the four original box-blocked cavities.");
                report.temporarily_box_blocked_cells = gaps;
                report.random_opening_swap_verified = true;
                if (openingOnly)
                {
                    report.passed = true;
                    Finish();
                    return;
                }
                phase = Phase.Complete;
            }
            if (phase == Phase.ClosedTap)
            {
                Require(game.Moves.LeftMoves == 27 && target.HasDoor && target.PlateCount == 10 &&
                    gameAudio.Playback.GetClipLastPlayedTime(103) > 0f,
                    "Touching a proxy only plays the closed cupboard feedback.");
                phase = Phase.Door;
                Gesture(board.Grid.GetCell(4, 8), board.Grid.GetCell(4, 7));
                return;
            }
            if (phase == Phase.Door)
            {
                Require(game.AcceptedSwaps == 1 && game.Moves.LeftMoves == 26 && !target.HasDoor && target.PlateCount == 10 &&
                    originalGoals[0].LeftCount == 40 && originalGoals[0].LeftCountUi == 40,
                    "Natural match reaches proxy tiles, breaks the door once, and leaves all plate goals.");
                report.door_only_hit = true;
                Capture("core_cupboard_door.png");
                phase = Phase.OpenTap;
                var masterCell = board.Grid.GetCell(2, 5);
                Gesture(masterCell, masterCell);
                return;
            }
            if (phase == Phase.OpenTap)
            {
                Require(game.Moves.LeftMoves == 26 && !target.HasDoor && target.PlateCount == 10 &&
                    gameAudio.Playback.GetClipLastPlayedTime(104) > 0f,
                    "Touching the open cupboard plays plate feedback without spending a move.");
                foreach (var animator in target.ItemView.GetComponentsInChildren<Animator>())
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (animator.name == "PlatesUpAnimator")
                        report.open_tap_upper_state = state.shortNameHash == Animator.StringToHash("PlateTapAnimation5");
                    else if (animator.name == "PlatesDownAnimator")
                        report.open_tap_lower_state = state.shortNameHash == Animator.StringToHash("PlateTapAnimation5_2");
                }
                Require(report.open_tap_upper_state && report.open_tap_lower_state,
                    "The open cupboard tap plays the original opposing plate animation variants.");
                report.closed_and_open_taps_verified = true;
                platesBeforeRocket = target.PlateCount;
                var source = board.Grid.GetCell(2, 8);
                Require(source.Mediation.CurrentItem is OriginalMatchBoardItem, "The explicit rocket replaces an ordinary match.");
                PlaceSpecial(source, 20);
                Gesture(source, source);
                phase = Phase.Plates;
                return;
            }
            if (phase == Phase.Plates)
            {
                Require(game.Moves.LeftMoves == 25 && target.PlateCount <= platesBeforeRocket - 2 &&
                    originalGoals[0].LeftCount <= 38 && originalGoals[0].LeftCount == originalGoals[0].LeftCountUi,
                    "One vertical rocket hits the opened master and proxy and counts plates.");
                report.rocket_shared_plate_hits = true;
                Capture("core_cupboard_plates.png");
                var source = board.Grid.GetCell(8, 8);
                Require(source.Mediation.CurrentItem is OriginalMatchBoardItem, "The explicit propeller replaces an ordinary match.");
                propeller = (OriginalPropellerBoardItem)PlaceSpecial(source, 10);
                Gesture(source, source);
                phase = Phase.Guided;
                return;
            }
            if (phase == Phase.Guided)
            {
                Require(game.Moves.LeftMoves == 24 && guidedTarget != null && guidedTarget.LayerCount < layersBeforeFlight &&
                    guidedTarget.IncomingExploderCount == 0 && !propeller.IsInFlight,
                    "The live propeller spends one move, hits the cabinet and releases its reservation.");
                report.guided_cupboard_hit = true;
                phase = Phase.Complete;
            }
            if (!game.CanUserMakeMove()) return;
            Require(taps < 22, "Finish the original goals within the test stimulus budget.");
            var center = ChooseTnt();
            Require(center != null, "A match item can host a TNT that reaches a remaining target.");
            PlaceSpecial(center, 40);
            taps++;
            Gesture(center, center);
        }

        // Native cross fall 0x3211d28 rejects a normal upper cell with
        // no target, or one whose target remains at the blocking box row.
        private static bool IsBoxBlockedCavity(OriginalGridCell cell)
        {
            if (!cell.ShouldFindCross) return false;
            var above = cell.Neighbors.Get(1);
            if (above == null || !above.IsNormalCell || above.IsFallBlocked()) return false;
            var upperTarget = above.Mediation.TargetItem;
            if (upperTarget != null && (!(upperTarget is OriginalBoxBoardItem) || upperTarget.Mediator.NextCell.Owner != above))
                return false;
            while (above != null && !above.Mediation.HasCurrentItem()) above = above.Neighbors.Get(1);
            return above?.Mediation.CurrentItem is OriginalBoxBoardItem;
        }

        private static void VerifyResult()
        {
            var dialog = controller.CoreFlow.WinDialog;
            if (dialog == null || !dialog.Lifecycle.IsShowing || dialog.IsAnimationWorking) return;
            CoreFlowVerification.RequireCurvedTitle(dialog, "Level " + level, level >= 10 ? 2.1f : 1.9f);
            report.two_digit_result_title_verified |= level >= 10;
            foreach (var goal in originalGoals)
                Require(goal.LeftCount == 0 && goal.LeftCountUi == 0, "Original model and HUD goals complete.");
            Require(controller.Progress.LevelToPlay == level + 1 && controller.CoreFlow.RemainingMoves.CollectionCount > 0,
                "Remaining moves and progression complete.");
            using (var db = OriginalRuntimeStorage.Open(controller.Progress.DatabasePath))
                Require(db.Find("Level").Value == (level + 1).ToString(), "Actual SQLite progression.");
            if (randomChapter && level == 15)
            {
                for (int x = 0; x < board.Grid.Width; x++)
                for (int y = 0; y < board.Grid.Height; y++)
                    Require(board.Grid.GetCell(x, y).Mediation.HasCurrentItem(),
                        "After the original box barriers clear, every level 15 cell has refilled.");
                report.blocked_cells_refilled_after_clear = true;
            }
            if (level == 11)
            {
                Require(board.Presentation.Pool.GetSpawnedCount(47) == 0 && target.Mediator.CurrentCell == null,
                    "All master views recycle and the footprint releases.");
                for (int x = 0; x < board.Grid.Width; x++)
                for (int y = 0; y < board.Grid.Height; y++)
                {
                    var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                    Require(item != null && item.ItemType != 8, "Released cupboard footprints refill with ordinary items.");
                }
                report.cupboard_win = true;
                report.cupboard_tnt_taps = taps;
                Capture("core_cupboard_win.png");
                report.passed = true;
                Finish();
                return;
            }

            report.chapter_wins++;
            if (randomChapter && level == 19)
            {
                Require(report.chapter_wins == 8 && report.box_tap_verified && report.box_drag_rejected &&
                    report.random_opening_swap_verified && report.blocked_cells_refilled_after_clear,
                    "The actual chapter wins levels 12–19 and verifies box and random opening input.");
                Capture("core_random_level19_win.png");
                report.passed = true;
                Finish();
                return;
            }
            previousOwner = controller.GetInstanceID();
            CoreFlowVerification.Click(dialog.ContinueButton, controller.CoreFlow.UiCamera);
            level++;
            phase = Phase.Reload;
        }

        private static OriginalGridCell ChooseTnt()
        {
            OriginalGridCell best = null;
            int highest = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var source = board.Grid.GetCell(x, y);
                if (!(source.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                int hits = 0;
                for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    if (Math.Abs(dx) == 2 && Math.Abs(dy) == 2) continue;
                    var cell = board.Grid.GetCell(x + dx, y + dy);
                    if (cell == null) continue;
                    var item = cell.Mediation.CurrentItem;
                    if (item is OriginalBoxBoardItem || item?.ItemType == 8 || cell.StaticTouch.HasStaticItem(2)) hits++;
                }
                if (hits > highest) { highest = hits; best = source; }
            }
            return best;
        }

        private static IOriginalSpecialCreationItem PlaceSpecial(OriginalGridCell cell, int tiledId)
        {
            var item = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            item.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)item.SwapView);
            return game.CreateItemAtCell(tiledId, cell);
        }

        private static void Gesture(OriginalGridCell source, OriginalGridCell destination)
        {
            from = source;
            to = destination;
            sample = 0;
            steadySince = -1;
        }

        private static bool Settled()
        {
            if (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            {
                steadySince = -1;
                return false;
            }
            if (steadySince < 0f) steadySince = Time.realtimeSinceStartup;
            return Time.realtimeSinceStartup - steadySince > .55f;
        }

        private static void Capture(string file) => GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, file);

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SceneManager.sceneLoaded -= SceneLoaded;
            if (device != null && device.Listener == null) device.Bind(game);
            File.WriteAllText(randomChapter ? RandomChapterPath : PathName, JsonUtility.ToJson(report, true) + "\n");
        }

        private static void Require(bool value, string detail)
        {
            if (!value) throw new InvalidOperationException(detail);
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
            public bool passed, closed_and_open_taps_verified, door_only_hit, rocket_shared_plate_hits, guided_cupboard_hit, cupboard_win;
            public bool guided_cupboard_center_verified, cupboard_row_sorting_verified;
            public bool box_tap_verified, box_drag_rejected, random_opening_swap_verified, blocked_cells_refilled_after_clear;
            public bool two_digit_result_title_verified;
            public bool open_tap_upper_state, open_tap_lower_state;
            public int cupboards, proxy_cells, chapter_wins, cupboard_tnt_taps;
            public int difficulty_samples_before_swap, difficulty_samples_after_swap, temporarily_box_blocked_cells;
            public int[] swap_coordinates;
            public string[] rows_after_swap;
            public List<ChapterOpening> chapter_openings = new List<ChapterOpening>();
            public string failure;
            public string scope = "Formal core scene with isolated progress. Level 11 uses one natural swap and explicit rocket, propeller and TNT placement to exercise the original door, plates, footprint refill and win. Chapter mode reaches level 11 from level 7 through the visible Continue buttons, using controlled TNT placements for the earlier wins.";
        }

        [Serializable]
        private sealed class ChapterOpening
        {
            public int level, population_attempts, generated_colors, boxes, grass, grass_under_cupboards, cupboards;
        }
    }
}
