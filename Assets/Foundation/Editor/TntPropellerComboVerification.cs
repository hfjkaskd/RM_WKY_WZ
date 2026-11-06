using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class TntPropellerComboVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameAudioHost audio;
        private static OriginalGameTouchListener listener;
        private static OriginalFallTargetPlanner fallQueries;
        private static Samples samples;
        private static OriginalGridCell source, destination;
        private static OriginalPropellerBoardItem propeller;
        private static OriginalTntBoardItem tnt;
        private static IOriginalExplodeTarget reservedTarget;
        private static bool[,] originalBoxes;
        private static int test, frame, sample, movesBefore, swapsBefore, refillsBefore, goalsBefore;
        private static float started, steadyAt, launchTime, arrivalTime, distance;
        private static Vector3 startPosition;
        private static bool carried, wobbled, delayedFinish, captured;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_tnt_propeller_combo_verification.json");
        public static void Run()
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>(); device.Unbind();
            audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            fallQueries = new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, game, null), new OriginalFallRefillColumns());
            originalBoxes = new bool[board.Grid.Width, board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                originalBoxes[x,y] = board.Grid.GetCell(x,y).Mediation.CurrentItem is OriginalBoxBoardItem;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(); EditorApplication.update += Tick;
        }
        private static void Begin()
        {
            bool vertical = test >= 4, reverse = test % 4 >= 2;
            bool found = false;
            for (int y = 7; y >= 5 && !found; y--) for (int x = 0; x < (vertical ? 9 : 8); x++)
            {
                var a = board.Grid.GetCell(x, y); var b = board.Grid.GetCell(vertical ? x : x + 1, vertical ? y - 1 : y);
                if (!(a.Mediation.CurrentItem is OriginalMatchBoardItem) || !(b.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                source = reverse ? b : a; destination = reverse ? a : b; found = true; break;
            }
            Require(found, "Adjacent regular cells for the next real swap fixture");
            Clear(source); Clear(destination);
            tnt = (OriginalTntBoardItem)game.CreateItemAtCell(40, test % 2 == 0 ? source : destination);
            propeller = (OriginalPropellerBoardItem)game.CreateItemAtCell(10, test % 2 == 0 ? destination : source);
            movesBefore = game.Moves.LeftMoves; swapsBefore = game.AcceptedSwaps; refillsBefore = game.CreatedRefills;
            goalsBefore = board.Goals.GetGoalLeftCount(11); startPosition = destination.GetViewPosition();
            carried = wobbled = delayedFinish = captured = false; sample = 0; distance = 0f; reservedTarget = null;
            launchTime = arrivalTime = -1f; started = Time.realtimeSinceStartup; steadyAt = -1f;
        }
        private static void Clear(OriginalGridCell cell)
        { var old = (OriginalMatchBoardItem)cell.Mediation.CurrentItem; old.Mediator.ClearFromAllRegisteredCells(); old.RecycleView(); }
        private static void Tick()
        {
            try { Step(); }
            catch (Exception e) { report.failure = e.ToString(); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); Cleanup(); throw; }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
            Require(Time.realtimeSinceStartup - started < 30f, "Carrier/area damage/refill completes without resetting native operations: case=" + test);
            if (sample < 3)
            {
                Vector3 p = (sample == 0 ? source : destination).GetViewPosition(); samples.Position = new Vector2(p.x,p.y);
                samples.Down = sample == 0; samples.Held = sample < 2; samples.Up = sample == 2; listener.ManualUpdate(); sample++; return;
            }
            if (propeller.IsInFlight)
            {
                if (launchTime < 0f) launchTime = Time.time;
                Require(propeller.TargetExplodeData.Trigger == 13 && tnt.CurrentCell == null, "Native trigger13 selects an area for a detached original TNT");
                Require(tnt.ItemView.transform.parent == propeller.ItemView.transform, "Actual TNT prefab is carried by the flying propeller");
                Require(tnt.ItemView.Fuse is OriginalTntPropellerComboFuseParticles && board.Presentation.Pool.GetSpawnedCount(17) >= 1,
                    "The native swap-start branch replaces the regular fuse with the real combo17 prefab");
                Require(((OriginalTntPropellerComboFuseParticles)tnt.ItemView.Fuse).fuseParticles.isPlaying
                    && audio.Playback.GetLoopCount(296) > 0, "Actual combo fuse particles and original loop296 are playing");
                Require(game.State.Operations.Container.GetCount(3) >= 1 && game.State.Operations.Container.GetCount(9) >= 1, "Flight and TNT-combo operation owners are active");
                int offset = tnt.ItemView.GetSorting().Order - propeller.ItemView.GetSorting().Order;
                Require(offset == 10 || offset == -10, "Native alternating front/back sorting is applied to both views");
                carried = true; distance = Mathf.Max(distance, Vector3.Distance(startPosition, propeller.ItemView.transform.position));
                wobbled |= Mathf.Abs(propeller.ItemView.rootTransform.GetPosition().x) > .01f;
                reservedTarget = propeller.TargetItem;
                Require(reservedTarget != null && reservedTarget.GetExtraIncomingExplodeCount(3) >= 1, "Area target owns the incoming TNT reservation");
                if (!captured && test == 0 && distance > .5f) { Capture("tnt_propeller_carrying.png"); captured = true; }
            }
            if (carried && !propeller.IsInFlight && tnt.IsExploded)
            {
                if (arrivalTime < 0f) arrivalTime = Time.time;
                delayedFinish |= game.State.Operations.Container.GetCount(9) > 0;
            }
            if (!carried || !tnt.IsExploded || propeller.IsInFlight || game.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady ||
                game.Auto.IsThereAWaitingMatch || board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping) { steadyAt = -1f; return; }
            if (steadyAt < 0f) { steadyAt = Time.time; return; }
            if (Time.time - steadyAt < .4f) return;
            Require(wobbled && distance > .25f && delayedFinish, "Real root motion, homing and delayed operation9 release ran");
            // Native detached TNT ExplodeViews 0x27b5e00..0x27b5e28 recycles
            // its view directly and skips FinalExplodeCompleted/HasView=false.
            // Pool ownership below proves release, independently of that flag.
            Require(tnt.IsExploded && !propeller.HasView && tnt.CurrentCell == null && propeller.CurrentCell == null, "Original detached TNT and propeller completion paths run");
            Require(reservedTarget.GetExtraIncomingExplodeCount(3) == 0, "Incoming area reservation is removed at arrival");
            Require(game.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == swapsBefore + 1, "One player gesture consumes exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(8, out int count) && count == test + 1, "Native combo8 usage increments once");
            Require(game.CreatedRefills > refillsBefore && game.CanUserMakeMove(), "Actual range damage/refill completes and player input is restored");
            Require(audio.Playback.GetLoopCount(296) == 0, "Carrier arrival stops the native combination loop");
            if (test == 0) Require(board.Goals.GetGoalLeftCount(11) < goalsBefore, "The TNT's actual target-area damage advances box goals");
            int cavities = 0, props = 0, tnts = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x,y); var item = cell.Mediation.CurrentItem;
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No takeoff or TNT-ring freeze remains");
                if (item == null)
                {
                    var above = cell.Neighbors.Get(1); while (above != null && above.Mediation.CurrentItem == null) above = above.Neighbors.Get(1);
                    Require(originalBoxes[x,y] && above?.Mediation.CurrentItem is OriginalBoxBoardItem && !fallQueries.IsFallPossibleFromCellToCell(above,cell)
                        && fallQueries.GetNextItemOfCell(above,cell) == null && fallQueries.FindTargetItemFromCrossCell(cell,cell,0) == null
                        && fallQueries.FindTargetItemFromCrossCell(cell,cell,2) == null, "Only native unreachable original-box cavities may be empty"); cavities++;
                }
                else
                {
                    Require(ReferenceEquals(item,cell.Mediation.TargetItem) && ReferenceEquals(item,cell.Mediation.NextItem), "Refilled mediation slots settle together");
                    if (item is OriginalPropellerBoardItem) props++; if (item is OriginalTntBoardItem) tnts++;
                }
            }
            Require(board.Presentation.Pool.GetSpawnedCount(10) == props && board.Presentation.Pool.GetSpawnedCount(14) == tnts
                && board.Presentation.Pool.GetSpawnedCount(16) == tnts && board.Presentation.Pool.GetSpawnedCount(17) == 0,
                "All borrowed carrier/TNT/regular-fuse views belong to remaining board models; combo fuse17 returns");
            report.flight_distance[test] = distance; report.native_box_cavities[test] = cavities; report.completed_combos++;
            if (++test < 8) { Begin(); return; }
            report.passed = true; report.left_moves = game.Moves.LeftMoves; report.goals_left = board.Goals.GetGoalLeftCount(11);
            Capture("tnt_propeller_refilled.png"); File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true)+"\n"); Cleanup();
        }
        private static void Capture(string name)
        {
            Camera camera = device.PointerCamera; var target = new RenderTexture(Mathf.RoundToInt(768 * camera.aspect),768,24);
            RenderTexture previous = camera.targetTexture, active = RenderTexture.active; Texture2D pixels = null;
            try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target; pixels = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(Path.Combine(Application.dataPath,"../"+name),pixels.EncodeToPNG()); }
            finally { camera.targetTexture = previous; RenderTexture.active = active; if (pixels != null) UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(target); }
        }
        private static void Cleanup()
        { EditorApplication.update -= Tick; if (device != null && game != null && device.Listener == null) device.Bind(game); }
        private static void Require(bool value,string message) { if (!value) throw new InvalidOperationException(message); }
        private sealed class Samples : IOriginalGameTouchInput
        { public Vector2 Position; public bool Down,Held,Up; public Vector2 GetMouseWorldPosition()=>Position;
            public bool GetMouseButtonDown()=>Down; public bool GetMouseButton()=>Held; public bool GetMouseButtonUp()=>Up; }
        [Serializable] private sealed class Report
        {
            public bool passed; public string failure; public int completed_combos,left_moves,goals_left;
            public float[] flight_distance = new float[8]; public int[] native_box_cavities = new int[8];
            public string scope = "Eight explicit production-factory TNT/propeller fixtures exercise both item orders, horizontal/vertical swipes and both directions in one formal player scene. Checks actual carrying/root motion/sorting, trigger13 reservations, operations3/9, detached TNT damage/goals/refill, one move, pool/fuse/freeze release and subsequent input. No natural creation, physical input, original rendered parity, extra-combo/private coroutine, conditional goal/helper/static/jelly/event/charger or full lifecycle parity claim.";
        }
    }
}
