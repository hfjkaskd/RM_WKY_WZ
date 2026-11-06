using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit diagnostics, using production prefabs, pools, matching, backend,
    // coroutine tweens and adapter. Resolution/audio are traced boundaries.
    public static class SwapPresentationVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalLevelPresentation owner;
        private static OriginalMatchItemSpriteView[] views;
        private static OriginalBoardItem[] items;
        private static OriginalGridCell[] cells;
        private static OriginalSwapActionState action;
        private static OriginalNormalSwapPresentation adapter;
        private static TraceGameplay gameplay;
        private static OriginalSwapParticles flash;
        private static OriginalMatchItemSwapParticles[] particles;
        private static int phase, frame, starts, ends, matchBefore, intermediateFrames, directionCases;
        private static float started, phaseTime, swapSeconds, wrongSeconds;
        private static Transform root;
        private static readonly ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[16];
        [MenuItem("Recovery/Verify Swap Presentation (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || owner != null) throw new InvalidOperationException("Run once in Play mode.");
            board = Object.FindObjectOfType<OriginalInitialBoardView>(); owner = board.Presentation;
            try
            {
                Require(owner.Pool.GetPoolLength(3) == 10 && owner.Pool.GetPoolLength(1) == 1, "Native default-profile prewarm");
                root = ((OriginalMatchItemSpriteView)board.Grid.GetCell(0, 7).Mediation.CurrentItem.SwapView).transform.parent;
                matchBefore = owner.Pool.GetPoolLength(2); started = Time.realtimeSinceStartup;
                starts = ends = intermediateFrames = directionCases = 0; phase = -1; frame = Time.frameCount;
                EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }
        private static void Prepare()
        {
            var grid = new OriginalCellGrid(3, 2);
            cells = new OriginalGridCell[6]; views = new OriginalMatchItemSpriteView[6]; items = new OriginalBoardItem[6];
            int[] types = { 1, 2, 3, 2, 1, 2 };
            var operations = new OriginalGameOperations(() => Time.frameCount);
            for (int i = 0; i < 6; i++)
            {
                int x = i % 3, y = i / 3;
                cells[i] = new OriginalGridCell(x, y, 1); cells[i].BindGameOperations(operations); grid.SetCell(x, y, cells[i]);
                var model = new OriginalMatchBoardItem(98000 + i, OriginalTiledItemTypes.AsItemType(types[i]), types[i]);
                items[i] = model; cells[i].Mediation.SetAllItems(model);
                var view = views[i] = owner.SpawnMatchView();
                view.transform.SetParent(root); view.transform.localPosition = new Vector3(x, y, 0f);
                view.BindModel(model);
                string suffix = types[i] == 1 ? "shield" : types[i] == 2 ? "leaf" : "book";
                view.Bind(Resources.Load<Sprite>("Recovered/Sprite/Items-MatchItem-" + suffix),
                    Resources.Load<Sprite>("Recovered/Sprite/Items-MatchItem-shadow_" + suffix),
                    new OriginalMatchViewPose { ShadowColor = new Color(1f, 1f, 1f, .37f) });
                model.BindSwapView(view); model.FallComponent = new OriginalFallComponentState(model);
                board.LocalMoves.RegisterTarget(view.transform);
            }
            grid.CacheNeighbors();
            var layout = OriginalBoardLayout.Create(3, 2, types, false, false);
            var matches = new OriginalMatchManager(grid, layout, new OriginalMatchBlockingState(false, null), new OriginalMatchFreezeOperations());
            action = new OriginalSwapActionState(new OriginalSwapDecision(new OriginalSwapMatchContext(matches)));
            var capturedAction = action;
            action.OnSwapAnimationStart += () => { starts++; operations.OperationStart(2); };
            action.OnSwapAnimationEnd += move => { Require(capturedAction.IsSwapping, "Native end event precedes idle"); ends++; operations.OperationFinish(2); };
            gameplay = new TraceGameplay(); adapter = new OriginalNormalSwapPresentation(root, owner, board.LocalMoves, gameplay, SortingLayer.NameToID("Item"));
            frame = Time.frameCount; phaseTime = Time.time;
        }
        private static void BeginWrong()
        {
            Prepare();
            var backend = new OriginalNormalSwapAnimationBackend(action, adapter, .135f, .04f);
            Require(!action.StartSwapAnimation(cells[0], cells[1], backend), "Isolated wrong swap rejected by actual match search");
            Require(action.IsSwapping && views[0].IsSwapping && views[1].IsSwapping && starts == 1, "Actual wrong swap starts");
            Require(views[0].GetSorting().Order == 9005 && views[1].GetSorting().Order == 1000, "Native outward asymmetric sorting");
            particles = Object.FindObjectsOfType<OriginalMatchItemSwapParticles>();
            Require(particles.Length == 2 && owner.Pool.GetPoolLength(3) == 8, "Two pooled directional trails");
            flash = owner.Particles.GetGlobalParticle<OriginalSwapParticles>(1);
            Require(flash == owner.Particles.GetGlobalParticle<OriginalSwapParticles>(1) && flash.circleParticle.particleCount == 2,
                "Two cell flashes share the global optimized emitter");
            var renderer = flash.circleParticle.GetComponent<ParticleSystemRenderer>();
            Require(renderer.sortingLayerID == SortingLayer.NameToID("Item") && renderer.sortingOrder == -1, "Native flash sorting");
            int count = flash.circleParticle.GetParticles(buffer); Require(count == 2, "Exact cell-flash burst count");
            var a = root.TransformPoint(Vector3.zero); var b = root.TransformPoint(Vector3.right);
            var first = buffer[0].position; var second = buffer[1].position;
            if (flash.circleParticle.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            { first = flash.circleParticle.transform.TransformPoint(first); second = flash.circleParticle.transform.TransformPoint(second); }
            Require(Near(first, a) && Near(second, b), "Flash uses cell world position under translated board root: " + first + "/" + second + " expected " + a + "/" + b);
            CheckTrails();
        }
        private static void Tick() { try { Step(); } catch { Cleanup(); throw; } }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Require(Time.realtimeSinceStartup - started < 18f, "Swap verification timeout");
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            if (phase == -1) { phase = 0; BeginWrong(); return; }
            if (phase < 2)
            {
                if (action.IsSwapping)
                {
                    int index = phase == 0 ? 0 : 1;
                    var pos = views[index].transform.localPosition;
                    var origin = new Vector3(index % 3, index / 3, 0f);
                    if ((pos - origin).sqrMagnitude > .00001f && (pos - origin).sqrMagnitude < .999f) intermediateFrames++;
                    CheckTrails(); return;
                }
                Require(!views[0].IsSwapping && !views[1].IsSwapping && ends == phase + 1, "View/event completion");
                for (int i = 0; i < cells.Length; i++) Require(cells[i].FallFreezeCount == 0, "Native swap and saved-group freezes released");
                if (phase == 0)
                {
                    wrongSeconds = Time.time - phaseTime;
                    Require(wrongSeconds >= .25f && views[0].transform.localPosition == Vector3.zero
                        && views[1].transform.localPosition == Vector3.right, "Wrong swap returns through actual .135 second tweens: " + wrongSeconds + " positions " + views[0].transform.localPosition + "/" + views[1].transform.localPosition);
                    Require(gameplay.Sounds == 2 && gameplay.Haptics == 1 && gameplay.MatchCallbacks == 0, "Wrong completion audio/haptic boundaries");
                    phase = 2; return;
                }
                swapSeconds = Time.time - phaseTime;
                Require(swapSeconds >= .12f && views[1].transform.localPosition == new Vector3(1, 1, 0)
                    && views[4].transform.localPosition == new Vector3(1, 0, 0), "Valid actual .135 second exchange");
                Require(gameplay.Sounds == 1 && gameplay.MatchCallbacks == 1 && gameplay.SpecialCallbacks == 1 && gameplay.Clears == 1,
                    "Required resolution callbacks dispatched in native order");
                Require(gameplay.Snapshot != null && gameplay.Snapshot.HasMatchGroup, "Actual saved match group reaches required resolution boundary");
                phase = 3; return;
            }
            if (phase == 2 || phase == 3)
            {
                if (Time.time - phaseTime < 1.04f) return;
                Require(owner.Pool.GetPoolLength(3) == 10, "Every one-second trail returned once");
                ReleaseViews(); owner.Particles.Clear(false);
                Require(owner.Pool.GetPoolLength(1) == 1, "Global flash returned by manager");
                if (phase == 2)
                {
                    phase = 1; Prepare(); var backend = new OriginalNormalSwapAnimationBackend(action, adapter, .135f, .04f);
                    Require(action.StartSwapAnimation(cells[1], cells[4], backend), "Actual three-green valid swap");
                    particles = Object.FindObjectsOfType<OriginalMatchItemSwapParticles>(); Require(particles.Length == 2, "Valid trails spawned");
                    Require(views[1].GetSorting().Order == 9005 && views[4].GetSorting().Order == 8905, "Valid swap sorting"); return;
                }
                phase = 4;
                var view = (OriginalMatchItemSpriteView)board.Grid.GetCell(0, 7).Mediation.CurrentItem.SwapView;
                particles = new OriginalMatchItemSwapParticles[4];
                for (int i = 0; i < 4; i++)
                {
                    int direction = 1 + i * 2; var particle = particles[i] = owner.PlaySwapParticle(view, direction);
                    Require(particle.GetParticleForDirection(direction).isPlaying, "Configured directional system plays with children");
                    Require(particle.transform.parent == view.transform && particle.transform.localPosition == Vector3.zero
                        && particle.transform.localScale == Vector3.one && particle.transform.localRotation == Quaternion.identity, "Native Init pose");
                    directionCases++;
                }
                phaseTime = Time.time; owner.Particles.Clear(false);
                Require(owner.Pool.GetPoolLength(3) == 10, "Manager cancels scheduled directional effects");
                return;
            }
            if (phase == 4)
            {
                if (Time.time - phaseTime < .55f) return;
                var view = (OriginalMatchItemSpriteView)board.Grid.GetCell(0, 7).Mediation.CurrentItem.SwapView;
                particles[0] = owner.PlaySwapParticle(view, 1); phaseTime = Time.time; phase = 5; return;
            }
            if (phase == 5)
            {
                if (Time.time - phaseTime < .55f) return;
                Require(particles[0].gameObject.activeSelf && owner.Pool.GetPoolLength(3) == 9,
                    "Canceled old deadline cannot recycle the reused effect early");
                phase = 6; return;
            }
            if (phase == 6)
            {
                if (Time.time - phaseTime < 1.06f) return;
                Require(owner.Pool.GetPoolLength(3) == 10 && !particles[0].gameObject.activeSelf, "Reused effect gets only its new deadline");
                Require(intermediateFrames > 0 && board.LocalMoves.GrowthCount == 0, "Actual intermediate motion without tween-pool growth");
                var result = new Result { passed = true, direction_cases = directionCases, intermediate_frames = intermediateFrames,
                    wrong_seconds = wrongSeconds, valid_seconds = swapSeconds,
                    scope = "Actual recovered swap/flash prefabs, production pools/adapter/backend/matching and coroutine tweens. Tests wrong out-and-back and valid match exchange, sorting/flags/events/freeze release, translated cell-world flash coordinates, one shared flash, four directional children, scaled one-second return, manager cancellation and pooled reuse without local tween growth. Audio/haptic and valid resolution are explicitly traced service boundaries; no placeholder services are installed in the scene. Full input/resolution/fall/refill/moves/goals/settlement and original rendered parity remain pending." };
                File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "swap_presentation_unity_verification.json"), JsonUtility.ToJson(result, true) + "\n");
                Debug.Log("Swap presentation Unity verification passed."); Cleanup();
            }
        }
        private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < .0001f;
        private static void CheckTrails()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = particles[i]; if (!particle.gameObject.activeSelf) continue;
                Require(Near(particle.transform.position, particle.transform.parent.position), "Trail follows moving view root");
                var sorting = ((OriginalMatchItemSpriteView)particle.transform.parent.GetComponent<OriginalMatchItemSpriteView>()).GetSorting();
                var renderers = particle.GetComponentsInChildren<Renderer>();
                for (int j = 0; j < renderers.Length; j++) Require(renderers[j].sortingLayerID == sorting.Layer
                    && renderers[j].sortingOrder == sorting.Order + 1, "Trail and child sorting tracks item + 1");
            }
        }
        private static void ReleaseViews()
        {
            if (views == null) return;
            for (int i = 0; i < views.Length; i++) { items[i].Mediator.ClearFromAllRegisteredCells(); owner.RecycleView(views[i]); }
            views = null; items = null;
            Require(owner.Pool.GetPoolLength(2) == matchBefore, "Diagnostic views returned");
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (owner != null && owner.Pool.IsPoolAlive()) { owner.Particles.Clear(false); ReleaseViews(); }
            board = null; owner = null; particles = null; flash = null; action = null; adapter = null;
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private sealed class TraceGameplay : IOriginalNormalSwapGameplay
        {
            public int Sounds, Haptics, MatchCallbacks, SpecialCallbacks, Clears;
            public OriginalSwapDecision Snapshot;
            public void ClearSinglePropellerWinCalculationData() { Clears++; }
            public void PlaySfx(int id, float delay) { Require((id == 254 || id == 370) && delay == .04f, "SFX boundary"); Sounds++; }
            public void PlayHaptic(int id, int type, float strength, float duration, bool replay)
            { Require(id == 38 && type == 1 && strength == .7f && duration == 0f && !replay, "Haptic boundary"); Haptics++; }
            public void ExplodeMatchGroups(OriginalSwapDecision decision, OriginalBoardItem other, OriginalBoardItem moving)
            {
                Require(Clears == 1 && MatchCallbacks == 0 && SpecialCallbacks == 0, "Required match resolution callback order");
                Snapshot = decision; MatchCallbacks++;
                // This isolated presentation check does not execute damage. Its
                // copied match-group reservations are released at this boundary.
                ReleaseGroup(decision.FromMatch);
                ReleaseGroup(decision.ToMatch);
            }
            private static void ReleaseGroup(IOriginalSwapMatchGroup group)
            {
                if (group == null) return;
                var copied = new OriginalSavedSwapGroup(); group.CopySnapshotTo(copied);
                for (int i = 0; i < copied.Snapshot.Cells.Count; i++) copied.Snapshot.Cells.Get(i).UnFreezeFall();
            }
            public void ExplodeSpecialItems(OriginalBoardItem other, OriginalBoardItem moving)
            { Require(MatchCallbacks == 1, "Required special callback follows matches"); SpecialCallbacks++; }
        }
        [Serializable] private sealed class Result
        { public bool passed; public int direction_cases, intermediate_frames; public float wrong_seconds, valid_seconds; public string scope; }
    }
}
