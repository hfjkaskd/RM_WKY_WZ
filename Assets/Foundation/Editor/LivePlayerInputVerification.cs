using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Only the device samples are recorded inputs. All services are the formal
    // scene's production owners; no diagnostic gameplay context is installed.
    public static class LivePlayerInputVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameAudioHost audioHost;
        private static OriginalFallTargetPlanner nativeFallQueries;
        private static OriginalGameTouchListener listener;
        private static Samples samples;
        private static int stage, sampleFrame, previousFrame, leftBefore, acceptedBefore;
        private static float commandTime, steadySince;
        private static OriginalGridCell from, to;
        private static bool sawRocket, sawTnt, sawLightball, sawPropeller;
        private static string failure;
        private static int variant, refillsBefore;
        private static bool activatingSpecial;
        private static OriginalTntBoardItem activatedTnt;
        private static OriginalGridShakeAnimation gridShake;
        private static Vector3 tntRestPosition;
        private static float peakTntShake;
        private static OriginalLightballBoardItem activatedLightball;
        private static OriginalRocketBoardItem activatedRocket;
        private static OriginalPropellerBoardItem activatedPropeller;
        private static Vector3 propellerStart;
        private static float propellerFlightDistance;
        private static int maxPropellerOperations;
        private static readonly System.Collections.Generic.List<string> propellerTrace = new System.Collections.Generic.List<string>();
        private static int maxPropellerLoop, maxPropellerTakeoff, maxPropellerHit;
        private static float maxPropellerScale;
        private static bool[,] originalBoxSlots;
        private static int blockedPropellerHoles;
        private static Vector3 rocketFirstStart, rocketSecondStart;
        private static float rocketFirstDistance, rocketSecondDistance;
        private static int maxRocketOperations;
        private static bool IsLightballMode => variant == 3 || variant == 4;
        private static bool IsRocketMode => variant >= 5 && variant <= 8;
        private static bool IsPropellerMode => variant == 9 || variant == 10;
        private static bool IsSwapMode => variant == 2 || variant == 4 || variant == 6 || variant == 8 || variant == 10;
        private static int RocketOrientation => variant >= 7 ? 1 : 0;
        private static readonly System.Collections.Generic.List<OriginalLightballBoardItem> lightballs = new System.Collections.Generic.List<OriginalLightballBoardItem>();
        private static string ReportPath => Path.Combine(Application.dataPath, "../" + ReportName);
        private static string ReportName
        {
            get
            {
                switch (variant)
                {
                    case 1: return "live_tnt_tap_verification.json";
                    case 2: return "live_tnt_swap_verification.json";
                    case 3: return "live_lightball_tap_verification.json";
                    case 4: return "live_lightball_swap_verification.json";
                    case 5: return "live_rocket_horizontal_tap_verification.json";
                    case 6: return "live_rocket_horizontal_swap_verification.json";
                    case 7: return "live_rocket_vertical_tap_verification.json";
                    case 8: return "live_rocket_vertical_swap_verification.json";
                    case 9: return "live_propeller_tap_verification.json";
                    case 10: return "live_propeller_swap_verification.json";
                    default: return "live_player_input_verification.json";
                }
            }
        }
        [MenuItem("Recovery/Verify Live Player Input")]
        public static void Run() => RunMode(0);
        [MenuItem("Recovery/Verify Live TNT Tap")]
        public static void RunTntTap() => RunMode(1);
        [MenuItem("Recovery/Verify Live TNT Swap")]
        public static void RunTntSwap() => RunMode(2);
        [MenuItem("Recovery/Verify Live Lightball Tap")]
        public static void RunLightballTap() => RunMode(3);
        [MenuItem("Recovery/Verify Live Lightball Swap")]
        public static void RunLightballSwap() => RunMode(4);
        [MenuItem("Recovery/Verify Live Horizontal Rocket Tap")]
        public static void RunRocketHorizontalTap() => RunMode(5);
        [MenuItem("Recovery/Verify Live Horizontal Rocket Swap")]
        public static void RunRocketHorizontalSwap() => RunMode(6);
        [MenuItem("Recovery/Verify Live Vertical Rocket Tap")]
        public static void RunRocketVerticalTap() => RunMode(7);
        [MenuItem("Recovery/Verify Live Vertical Rocket Swap")]
        public static void RunRocketVerticalSwap() => RunMode(8);
        public static void RunPropellerTap() => RunMode(9);
        public static void RunPropellerSwap() => RunMode(10);
        private static void RunMode(int mode)
        {
            if (!EditorApplication.isPlaying || board != null) throw new InvalidOperationException("Run once in a fresh Play session.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            audioHost = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            nativeFallQueries = new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, game, null), new OriginalFallRefillColumns());
            if (game == null || device.Listener == null || board.Moves.MadeMoves != 0)
            { Cleanup(); throw new InvalidOperationException("A fresh formal player scene is required."); }
            device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            lightballs.Clear(); game.OnSpecialCreated += ObserveSpecial;
            variant = mode; activatingSpecial = false; activatedTnt = null; activatedLightball = null; activatedRocket = null;
            gridShake = board.BoardRoot.GetComponent<OriginalGridShakeAnimation>();
            peakTntShake = 0f;
            activatedPropeller = null; propellerFlightDistance = 0f; maxPropellerOperations = 0; propellerTrace.Clear();
            maxPropellerLoop = maxPropellerTakeoff = maxPropellerHit = blockedPropellerHoles = 0; maxPropellerScale = 1f;
            originalBoxSlots = new bool[board.Grid.Width, board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                originalBoxSlots[x, y] = board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBoxBoardItem;
            rocketFirstDistance = rocketSecondDistance = 0f; maxRocketOperations = 0;
            stage = 0; sawRocket = sawTnt = sawLightball = sawPropeller = false; failure = null; previousFrame = Time.frameCount;
            Application.logMessageReceived += ObserveLog;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(board.Grid.GetCell(0, 7), board.Grid.GetCell(1, 7));
            EditorApplication.update += Tick;
        }
        private static void Begin(OriginalGridCell a, OriginalGridCell b)
        {
            from = a; to = b; sampleFrame = 0; commandTime = Time.realtimeSinceStartup; steadySince = -1;
            leftBefore = board.Moves.LeftMoves; acceptedBefore = game.AcceptedSwaps;
        }
        private static void Tick()
        {
            try { Step(); }
            catch (Exception error)
            {
                File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { passed = false, failure = error.Message, accepted_swaps = game == null ? 0 : game.AcceptedSwaps, rejected_swaps = game == null ? 0 : game.RejectedSwaps, left_moves = board == null ? 0 : board.Moves.LeftMoves, created_refills = game == null ? 0 : game.CreatedRefills, created_specials = game == null ? 0 : game.CreatedSpecials, rocket_created = sawRocket, tnt_created = sawTnt, lightball_created = sawLightball, propeller_created = sawPropeller, propeller_flight = propellerFlightDistance, propeller_max_operations = maxPropellerOperations, propeller_trace = propellerTrace.ToArray(), propeller_max_scale = maxPropellerScale, blocked_original_box_holes = blockedPropellerHoles, propeller_max_loop = maxPropellerLoop, propeller_takeoff_instances = maxPropellerTakeoff, propeller_hit_instances = maxPropellerHit }, true) + "\n");
                Cleanup(); throw;
            }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            if (failure != null) throw new InvalidOperationException("Live runtime reported: " + failure);
            if (Time.frameCount == previousFrame) return; previousFrame = Time.frameCount;
            if (Time.realtimeSinceStartup - commandTime > 15f) throw new InvalidOperationException("Live player command did not settle.");
            if (activatedTnt != null)
                peakTntShake = Mathf.Max(peakTntShake, Vector3.Distance(tntRestPosition, board.BoardRoot.localPosition));
            if (activatingSpecial && activatedRocket != null)
            {
                rocketFirstDistance = Mathf.Max(rocketFirstDistance, Vector3.Distance(rocketFirstStart, activatedRocket.ItemView.FirstPart.transform.position));
                rocketSecondDistance = Mathf.Max(rocketSecondDistance, Vector3.Distance(rocketSecondStart, activatedRocket.ItemView.SecondPart.transform.position));
                maxRocketOperations = Math.Max(maxRocketOperations, board.State.Operations.Container.GetCount(4));
            }
            if (activatingSpecial && activatedPropeller != null)
            {
                propellerFlightDistance = Mathf.Max(propellerFlightDistance, Vector3.Distance(propellerStart, activatedPropeller.ItemView.transform.position));
                maxPropellerOperations = Math.Max(maxPropellerOperations, board.State.Operations.Container.GetCount(3));
                maxPropellerScale = Mathf.Max(maxPropellerScale, activatedPropeller.ItemView.transform.localScale.x);
                maxPropellerLoop = Math.Max(maxPropellerLoop, audioHost.Playback.GetLoopCount(293));
                maxPropellerTakeoff = Math.Max(maxPropellerTakeoff, board.Presentation.GetParticleAmount(20) - board.Presentation.Pool.GetPoolLength(11));
                maxPropellerHit = Math.Max(maxPropellerHit, board.Presentation.GetParticleAmount(20) - board.Presentation.Pool.GetPoolLength(12));
                if (propellerTrace.Count < 5 || Time.frameCount % 5 == 0 && propellerTrace.Count < 30 && activatedPropeller.IsInFlight)
                    propellerTrace.Add("frame=" + Time.frameCount + " dt=" + Time.deltaTime + " pos=" + activatedPropeller.ItemView.transform.position + " target=" + (activatedPropeller.TargetItem == null ? "none" : activatedPropeller.TargetItem.GetMasterViewCenterPosition().ToString()) + " fly=" + (int)activatedPropeller.ItemView.FlyState + " op3=" + board.State.Operations.Container.GetCount(3));
            }
            if (sampleFrame < 3)
            {
                var p = (sampleFrame == 0 ? from : to).GetViewPosition();
                samples.Position = new Vector2(p.x, p.y);
                samples.Down = sampleFrame == 0; samples.Held = sampleFrame < 2; samples.Up = sampleFrame == 2;
                listener.ManualUpdate(); sampleFrame++; return;
            }
            if (board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping || !game.Fall.IsAllItemsSteady
                || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            { steadySince = -1; return; }
            if (steadySince < 0) { steadySince = Time.realtimeSinceStartup; return; }
            if (Time.realtimeSinceStartup - steadySince < 1.6f) return;
            // Native rocket parts finish operation4 before the outer flight
            // coroutine's distance/speed+1 recycle tail completes. Wait for that
            // actual owner; command timeout still fails a missing recycle.
            if (activatingSpecial && IsRocketMode && board.Presentation.Pool.GetSpawnedCount(9) != 0) return;
            blockedPropellerHoles = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y); if (!cell.IsNormalCell) continue;
                var item = cell.Mediation.CurrentItem;
                if (item == null)
                {
                    // Straight search stops at an immovable box above. Query
                    // the ACTUAL planner's read-only cross-source methods for
                    // both sides; neither call assigns a live target or fills
                    // a cell. Ordinary match/source holes are never exempted.
                    Require(originalBoxSlots[x, y] && cell.Mediation.NextItem == null && cell.Mediation.TargetItem == null
                        && !cell.IsFallFrozen && cell.CrossFallFreezeCount == 0
                        && HasNoNativeBoxRefillRoute(cell),
                        "Only destroyed original-box cavities with no native straight/cross route may remain empty; cell=" + x + "," + y);
                    blockedPropellerHoles++;
                    continue;
                }
                Require(item != null && ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem),
                    "All slots refill and settle; cell=" + x + "," + y + " current=" + (item == null ? -1 : item.ItemType)
                    + " next=" + (cell.Mediation.NextItem == null ? -1 : cell.Mediation.NextItem.ItemType)
                    + " target=" + (cell.Mediation.TargetItem == null ? -1 : cell.Mediation.TargetItem.ItemType));
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No creation/swap/rocket cross-fall freeze remains");
                sawRocket |= item is OriginalRocketBoardItem; sawTnt |= item is OriginalTntBoardItem;
                sawLightball |= item is OriginalLightballBoardItem; sawPropeller |= item is OriginalPropellerBoardItem;
                var view = board.FallMotion.GetView(item);
                if (view != null)
                {
                    var p = view.Position;
                    Require(Vector3.Distance(new Vector3(p.X, p.Y, p.Z), cell.GetViewPosition()) < .0011f, "View lands at its model cell");
                }
            }
            if (activatingSpecial)
            {
                bool lightball = IsLightballMode;
                if (lightball)
                {
                    Require(!activatedLightball.HasView && activatedLightball.CurrentCell == null, "Lightball releases its view/model cell");
                    var strategy = activatedLightball.Strategy;
                    Require(strategy.SentRayCount > 0 && strategy.ReachedRayCount > 0 && strategy.CompletedRayCount == strategy.SentRayCount, "Real native ray flight/return completes");
                    Require(board.Presentation.Pool.GetPoolLength(26) == 50 && board.Presentation.Pool.GetPoolLength(25) == 50, "Ray and target-background pools fully recycled");
                }
                else if (IsPropellerMode)
                {
                    Require(activatedPropeller.IsExploded && !activatedPropeller.HasView && activatedPropeller.CurrentCell == null && !activatedPropeller.IsInFlight, "Propeller releases its source cell and flight operation");
                    Require(propellerFlightDistance > 1f && maxPropellerOperations >= 1, "Source propeller view actually flies under operation3; distance=" + propellerFlightDistance + ", maxOperation=" + maxPropellerOperations);
                    Require(activatedPropeller.TargetItem != null && !activatedPropeller.TargetItem.HasView, "Reached target actually receives original damage");
                    Require(maxPropellerLoop > 0 && maxPropellerTakeoff > 0 && maxPropellerHit > 0 && maxPropellerScale > 1.1f, "Original flight loop, takeoff/hit particle instances and scale-up actually occur");
                    Require(Application.targetFrameRate == 60 && QualitySettings.vSyncCount == 0, "Original application frame-rate startup is applied");
                    Require(!activatedPropeller.ItemView.gameObject.activeSelf && activatedPropeller.ItemView.FlyState == OriginalGuidedExploderFlyState.Stable, "Propeller view lands and recycles");
                    Require(audioHost.Playback.GetLoopCount(293) == 0, "Propeller looping flight audio released");
                    for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                        Require((board.Grid.GetCell(x, y).Mediation.CurrentItem == null || board.Grid.GetCell(x, y).Mediation.CurrentItem.IncomingExploderCount == 0), "Propeller pending hits all released");
                    Require(board.Presentation.Pool.GetPoolLength(11) == board.Presentation.GetParticleAmount(20) && board.Presentation.Pool.GetPoolLength(12) == board.Presentation.GetParticleAmount(20), "Propeller takeoff and target particles return to source pools");
                }
                else if (IsRocketMode)
                {
                    Require(activatedRocket.IsExploded && !activatedRocket.HasView && activatedRocket.CurrentCell == null, "Rocket releases its view/model cell");
                    Require(rocketFirstDistance > 1.5f && rocketSecondDistance > 1.5f && maxRocketOperations >= 2, "Both source projectile transforms fly and own their operations");
                    Require(board.Presentation.Pool.GetPoolLength(9) == board.Presentation.GetParticleAmount(40), "Both source rocket particle instances return to pool9");
                }
                else
                {
                    Require(activatedTnt.IsExploded && activatedTnt.CurrentCell == null, "TNT activates and releases its model cell");
                    // Frame one can add MidVibrate to the initial MaxVibrate on both axes.
                    Require(peakTntShake > .05f && peakTntShake <= .425f &&
                        !gridShake.IsShaking && board.BoardRoot.localPosition == tntRestPosition,
                        $"TNT shakes and returns to rest. Peak {peakTntShake}, active {gridShake.IsShaking}, " +
                        $"rest {tntRestPosition}, final {board.BoardRoot.localPosition}");
                }
                Require(board.Moves.LeftMoves == leftBefore - 1, "Exactly one move spent on special interaction");
                bool swapped = IsSwapMode;
                Require(game.AcceptedSwaps == acceptedBefore + (swapped ? 1 : 0), "Tap/swap events keep their native ownership");
                Require(game.CreatedRefills > refillsBefore && board.State.GetSpecialOperationCount() == 0, "Special damage refills and finishes its operation");
                Require(listener.TouchDisableCount == 0 && game.TouchListener == listener && game.RunningLightballColors == 0, "Actual input owner unlocked and shared colors released before device rebind");
                int chains = 0;
                for (int i = 0; i < lightballs.Count; i++)
                {
                    var item = lightballs[i]; var strategy = item.Strategy;
                    if (strategy == null || item.HasView || strategy.SentRayCount == 0) continue;
                    Require(strategy.CompletedRayCount == strategy.SentRayCount, "Chained lightball rays all complete"); chains++;
                }
                Require(game.CanUserMakeMove(), "Player move availability restored after special completion");
                device.Bind(game); PlayableBoardObservation.Capture();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { passed = true,
                    tnt_activated = variant <= 2, tnt_peak_shake = peakTntShake,
                    lightball_activated = lightball, rocket_activated = IsRocketMode, propeller_activated = IsPropellerMode, propeller_flight = propellerFlightDistance, propeller_max_operations = maxPropellerOperations, propeller_trace = propellerTrace.ToArray(), propeller_max_scale = maxPropellerScale, blocked_original_box_holes = blockedPropellerHoles, propeller_max_loop = maxPropellerLoop, propeller_takeoff_instances = maxPropellerTakeoff, propeller_hit_instances = maxPropellerHit, completed_lightball_activations = chains,
                    rocket_orientation = IsRocketMode ? activatedRocket.Orientation : -1, rocket_first_flight = rocketFirstDistance, rocket_second_flight = rocketSecondDistance, rocket_max_operations = maxRocketOperations,
                    sent_rays = lightball ? activatedLightball.Strategy.SentRayCount : 0,
                    reached_rays = lightball ? activatedLightball.Strategy.ReachedRayCount : 0,
                    completed_rays = lightball ? activatedLightball.Strategy.CompletedRayCount : 0,
                    accepted_swaps = game.AcceptedSwaps, rejected_swaps = game.RejectedSwaps,
                    left_moves = board.Moves.LeftMoves, goals_left = board.Goals.GetGoalLeftCount(11),
                    created_refills = game.CreatedRefills, created_specials = game.CreatedSpecials,
                    scope = "Frame-separated native listener input into formal production scene. Naturally creates the special through legal swaps, then taps or swaps it with a normal item. Checks exactly one move, real activation, source pooled ray/rocket/explosion presentation, damage/refill, input/color release and settled/unfrozen model slots/landing tolerance. Ordinary source holes must refill. Throughout the replay, only a destroyed original-box cavity blocked above by an immovable box with both read-only native cross-source queries returning no target may remain empty. The verifier never assigns targets or fills cells. Actual production target60/vSync0 startup drives real Unity deltas; no captureDeltaTime override. Runtime errors fail verification. Does not verify physical input, all special combos/events, full session, looping lightball audio219, settlement or rendered-original parity."
                }, true) + "\n");
                Debug.Log("Formal special interaction passed: mode=" + variant + ", refills=" + game.CreatedRefills);
                Cleanup(); return;
            }
            if (stage == 0)
            {
                Require(board.Moves.LeftMoves == leftBefore && game.AcceptedSwaps == acceptedBefore && game.RejectedSwaps == 1, "Invalid drag restores budget and positions");
                stage++; Begin(board.Grid.GetCell(3, 5), board.Grid.GetCell(3, 4)); return;
            }
            Require(game.AcceptedSwaps == acceptedBefore + 1 && board.Moves.LeftMoves == leftBefore - 1, "Exactly one move spent on accepted drag");
            if (stage == 1) Require(board.Goals.GetGoalLeftCount(11) == 33 && game.CreatedRefills >= 6, "Original first triple hits three boxes and refills");
            if (variant != 0 && HasRequestedSpecial())
            {
                for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    var special = cell.Mediation.CurrentItem as OriginalSpecialBoardItem;
                    if (!IsRequestedSpecial(special)) continue;
                    var destination = cell;
                    if (IsSwapMode)
                    {
                        destination = null;
                        for (int direction = 0; direction < 4; direction++)
                        {
                            var candidate = board.Grid.GetCell(x + (direction == 0 ? 1 : direction == 1 ? -1 : 0), y + (direction == 2 ? 1 : direction == 3 ? -1 : 0));
                            if (candidate?.Mediation.CurrentItem is OriginalMatchBoardItem) { destination = candidate; break; }
                        }
                        if (destination == null) continue;
                    }
                    activatedTnt = special as OriginalTntBoardItem; activatedLightball = special as OriginalLightballBoardItem;
                    if (activatedTnt != null) tntRestPosition = board.BoardRoot.localPosition;
                    activatedRocket = special as OriginalRocketBoardItem;
                    activatedPropeller = special as OriginalPropellerBoardItem;
                    if (activatedPropeller != null) propellerStart = activatedPropeller.ItemView.transform.position;
                    if (activatedRocket != null)
                    {
                        rocketFirstStart = activatedRocket.ItemView.FirstPart.transform.position;
                        rocketSecondStart = activatedRocket.ItemView.SecondPart.transform.position;
                    }
                    activatingSpecial = true; refillsBefore = game.CreatedRefills;
                    Begin(cell, destination); return;
                }
                throw new InvalidOperationException("No requested special interaction available.");
            }
            if (variant == 0 && game.AcceptedSwaps >= 10)
            {
                device.Bind(game);
                PlayableBoardObservation.Capture();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report {
                    passed = true, accepted_swaps = game.AcceptedSwaps, rejected_swaps = game.RejectedSwaps,
                    left_moves = board.Moves.LeftMoves, goals_left = board.Goals.GetGoalLeftCount(11),
                    created_refills = game.CreatedRefills, created_specials = game.CreatedSpecials,
                    blocked_original_box_holes = blockedPropellerHoles,
                    rocket_created = sawRocket, tnt_created = sawTnt, lightball_created = sawLightball, propeller_created = sawPropeller
                }, true) + "\n");
                Debug.Log("Formal live player input verification passed: invalid/valid drag, box goals, refill, 10accepted drags and all72 cells stable; naturally created special patterns recorded.");
                Cleanup(); return;
            }
            Require(stage < 24, "Requested special must be naturally reached within the remaining player move budget");
            stage++;
            int wanted = IsPropellerMode ? 2 : IsRocketMode ? (RocketOrientation == 0 ? 40 : 30) : IsLightballMode ? 7 : !sawTnt ? 5 : !sawRocket ? 3 : !sawLightball ? 7 : 2;
            if (ChooseMove(wanted, out var a, out var b) || ChooseMove(1, out a, out b))
            { Begin(a, b); return; }
            throw new InvalidOperationException("No supported move found in current live board.");
        }
        private static bool HasNoNativeBoxRefillRoute(OriginalGridCell cell)
        {
            OriginalGridCell above = cell.Neighbors.Get(1);
            if (!(above?.Mediation.CurrentItem is OriginalBoxBoardItem)) return false;
            return !nativeFallQueries.IsFallPossibleFromCellToCell(above, cell)
                && nativeFallQueries.GetNextItemOfCell(above, cell) == null
                && nativeFallQueries.FindTargetItemFromCrossCell(cell, cell, 0) == null
                && nativeFallQueries.FindTargetItemFromCrossCell(cell, cell, 2) == null;
        }
        private static bool IsRequestedSpecial(OriginalSpecialBoardItem item)
        {
            if (IsPropellerMode) return item is OriginalPropellerBoardItem;
            if (IsRocketMode) return item is OriginalRocketBoardItem rocket && rocket.Orientation == RocketOrientation;
            return variant <= 2 ? item is OriginalTntBoardItem : item is OriginalLightballBoardItem;
        }
        private static bool HasRequestedSpecial()
        {
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                if (IsRequestedSpecial(board.Grid.GetCell(x, y).Mediation.CurrentItem as OriginalSpecialBoardItem)) return true;
            return false;
        }
        private static bool ChooseMove(int requested, out OriginalGridCell a, out OriginalGridCell b)
            => ChooseMove(board, requested, out a, out b);
        internal static bool ChooseMove(OriginalInitialBoardView board, int requested, out OriginalGridCell a, out OriginalGridCell b)
        {
            var data = OriginalLevelReader.Load("Levels/level_1");
            int[] ids = new int[board.Grid.Width * board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                ids[OriginalLevelRules.GetIndexFromPoint(x, y, board.Grid.Width)] = board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalMatchBoardItem m ? m.MatchType : 21;
            var shadow = OriginalRegularGridCreation.Create(data.Grid, ids, false, false, false, out var layout);
            for (int x = 0; x < shadow.Width; x++) for (int y = 0; y < shadow.Height; y++)
            {
                int id = ids[OriginalLevelRules.GetIndexFromPoint(x, y, shadow.Width)];
                if (id != 21) shadow.GetCell(x, y).Mediation.SetAllItems(new OriginalMatchBoardItem(x * shadow.Height + y, 1, id));
            }
            var matches = new OriginalMatchManager(shadow, layout, new OriginalMatchBlockingState(false, null), new OriginalMatchFreezeOperations());
            for (int x = 0; x < shadow.Width; x++) for (int y = 0; y < shadow.Height; y++) for (int d = 0; d < 2; d++)
            {
                var c = shadow.GetCell(x, y); var n = shadow.GetCell(x + (d == 0 ? 1 : 0), y + (d == 1 ? 1 : 0));
                var first = c.Mediation.CurrentItem; var second = n?.Mediation.CurrentItem;
                if (first == null || second == null) continue;
                OriginalSwapDecision.SetAllItems(c, second); OriginalSwapDecision.SetAllItems(n, first);
                bool target = false;
                var groups = matches.FindExtensiveMatchesInArea(n.X, n.Y, 3);
                for (int i = 0; i < groups.Count; i++)
                {
                    var g = groups[i]; if (!g.ContainsItem(first) && !g.ContainsItem(second)) continue;
                    target |= requested == 30 ? g.PatternType == 3 : requested == 40 ? g.PatternType == 4 : requested == 3 ? g.PatternType == 3 || g.PatternType == 4 : requested == 5 ? g.PatternType == 5 || g.PatternType == 6 : g.PatternType == requested;
                }
                OriginalSwapDecision.SetAllItems(c, first); OriginalSwapDecision.SetAllItems(n, second);
                if (!target) continue;
                a = board.Grid.GetCell(c.X, c.Y); b = board.Grid.GetCell(n.X, n.Y); return true;
            }
            a = b = null; return false;
        }
        private static void ObserveSpecial(OriginalSpecialBoardItem item)
        {
            sawRocket |= item is OriginalRocketBoardItem; sawTnt |= item is OriginalTntBoardItem;
            sawLightball |= item is OriginalLightballBoardItem; sawPropeller |= item is OriginalPropellerBoardItem;
            if (item is OriginalLightballBoardItem lightball) lightballs.Add(lightball);
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (game != null) game.OnSpecialCreated -= ObserveSpecial;
            Application.logMessageReceived -= ObserveLog;
            if (device != null && game != null && EditorApplication.isPlaying && device.Listener == null) device.Bind(game);
            board = null; game = null; device = null; audioHost = null; nativeFallQueries = null; listener = null; samples = null;
        }
        private static void ObserveLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failure = message; }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        private sealed class Samples : IOriginalGameTouchInput
        {
            public Vector2 Position; public bool Down, Held, Up;
            public Vector2 GetMouseWorldPosition() => Position;
            public bool GetMouseButtonDown() => Down;
            public bool GetMouseButton() => Held;
            public bool GetMouseButtonUp() => Up;
        }
        [Serializable] private sealed class Report
        {
            public bool passed, physical_drag_verified, tnt_activated, lightball_activated, rocket_activated;
            public bool propeller_activated;
            public float tnt_peak_shake;
            public float propeller_flight;
            public string[] propeller_trace;
            public float propeller_max_scale;
            public int blocked_original_box_holes;
            public int propeller_max_loop, propeller_takeoff_instances, propeller_hit_instances;
            public int propeller_max_operations;
            public int rocket_orientation, rocket_max_operations;
            public float rocket_first_flight, rocket_second_flight;
            public int sent_rays, reached_rays, completed_rays, completed_lightball_activations;
            public bool rocket_created, tnt_created, lightball_created, propeller_created;
            public string failure;
            public int accepted_swaps, rejected_swaps, left_moves, goals_left, created_refills, created_specials;
            public string scope = "Frame-separated recorded device samples through native GameTouchListener into formal production scene services. Checks invalid swap spends0, accepted swaps spend1, first triple/three box goals/six refills, 10accepted swaps and naturally observed special creation patterns (each requested type also has its own activation mode), automatic cascades, original creation coroutines and all72 model slots/no holes/no freeze/landing tolerance. Fails on runtime errors/exceptions. Shadow native match search only chooses gestures and never mutates live cells. Does not verify physical drag timing, special activation/combos, complete session startup, settlement or original rendered parity.";
        }
    }
}
