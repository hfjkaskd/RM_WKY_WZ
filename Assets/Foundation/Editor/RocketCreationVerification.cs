using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Diagnostic entry point only. Model, view, coroutine, pool and animation
    // use production runtime code and the configured recovered assets.
    public static class RocketCreationVerification
    {
        private static OriginalLevelPresentation presentation;
        private static Services services;
        private static OriginalRocketItemSpriteView reused;
        private static int test, frame, animatedFrames;
        private static float started;
        private static readonly float[] durations = new float[3];

        [MenuItem("Recovery/Verify Rocket Creation (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || presentation != null) throw new InvalidOperationException("Run once in Play mode.");
            presentation = Object.FindObjectOfType<OriginalLevelPresentation>();
            try
            {
                Require(presentation != null && presentation.Pool.GetPoolLength(8) == 20, "Native twenty-view rocket pool");
                test = 0; reused = null; started = Time.realtimeSinceStartup;
                Begin();
                EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }
        private static void Begin()
        {
            services = new Services(presentation, test);
            var group = new OriginalMatchGroupSnapshot(1, test == 0 ? 4 : 3, 2);
            for (int i = 0; i < services.Cells.Length; i++) group.Cells.Add(services.Cells[i]);
            presentation.CreateSpecialItemCreator(services).CreateSpecialItemForMatchGroup(group, services.Cells[0]);
            var model = services.Model;
            Require(model.HasView && model.ItemType == 2 && model.RemainingMoveCoins == 2 && !model.IsCreatedByLightball,
                "Concrete native rocket model identity and constructor state");
            Require(!model.IsActive && !model.ItemView.gameObject.activeSelf && presentation.Coroutines.Count == 1,
                "Hidden/inactive special waits on independent coroutine host");
            Require(model.Orientation == (test == 0 ? 0 : 1), "Native tiled-id orientation");
            Require(model.CurrentCell == services.Cells[0] && model.Mediator.NextCell.Owner == model.CurrentCell
                && model.Mediator.TargetCell.Owner == model.CurrentCell, "Three-way cell registration");
            Require(model.ItemView.transform.localPosition == new Vector3(model.CurrentCell.X, model.CurrentCell.Y, 0f),
                "Native CellPoint.ToVector sets creation position");
            for (int i = 0; i < services.Cells.Length; i++) Require(services.Cells[i].FallFreezeCount == 1, "Creation freezes each group cell");
            Require(presentation.Pool.GetPoolLength(8) == 19, "Pool supplies concrete view");
            Require(reused == null || reused == model.ItemView, "Same native view reused across orientations");
            reused = model.ItemView;
            Require(services.Increments == 1 && services.Records == 1 && services.Madness == 1 && services.Missions == 1
                && services.Useful == 1, "Required session and creation records dispatched once");
            frame = Time.frameCount; animatedFrames = 0;
        }
        private static void Tick()
        {
            try { Step(); } catch { Cleanup(); throw; }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Require(Time.realtimeSinceStartup - started < 20f, "Rocket creation verification timeout");
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            var view = services.Model.ItemView;
            if (services.SoundFrame < 0) return;
            var selected = test == 0 ? view.HorizontalAnimation : view.VerticalAnimation;
            var other = test == 0 ? view.VerticalAnimation : view.HorizontalAnimation;
            if (!view.CreationAnimationFinished)
            {
                Require(services.Model.IsActive && view.gameObject.activeSelf && !view.BaseRenderer.gameObject.activeSelf
                    && selected.gameObject.activeSelf && !other.gameObject.activeSelf, "Native animation visibility");
                Require(view.GetSorting().Order == (test == 2 && services.AppearFrame >= 0 ? 1000 : 9100), "Creation sorting lock / spreading unlock");
                animatedFrames++;
                if (services.AppearFrame >= 0) VerifyAppeared();
                return;
            }
            durations[test] = Time.time - services.SoundTime;
            Require(animatedFrames > 1 && durations[test] > .55f && durations[test] < 1.2f, "Recovered Animator completes naturally, without premature stop");
            Require(view.BaseRenderer.gameObject.activeSelf && !selected.gameObject.activeSelf && !other.gameObject.activeSelf
                && view.GetSorting().Order == 1000, "Completed animation restores static body and sorting");
            VerifyAppeared();
            Require(presentation.Coroutines.Count == 0, "Creator routine completes independently of view update");
            var original = Resources.Load<GameObject>("Recovered/GameObject/RocketItemPrefab").GetComponent<OriginalRocketItemSpriteView>();
            view.ChangeAlpha(.2f); view.ResetAlpha();
            Require(view.BaseRenderer.color.a == 1f && view.ShadowRenderer.color.a == original.ShadowRenderer.color.a,
                "Original shadow alpha survives pooled initialization");
            view.BaseRenderer.color = new Color(.2f, .3f, .4f, .5f);
            ReleaseCase();
            Require(reused.BaseRenderer.color == Color.white && !reused.BaseRenderer.gameObject.activeSelf,
                "Native recycle sets sprites and white body without extra base reset");
            test++;
            if (test < 3) { Begin(); return; }
            var result = new Result { passed = true, cases = 3, view_prewarm = 20, spreading_particles = services.BackParticles, horizontal_seconds = durations[0],
                vertical_seconds = durations[1], water_vertical_seconds = durations[2],
                scope = "Real recovered rocket prefab/Animator with natural player-loop progression, configured sprite paths, horizontal/vertical/water strategies, native model CreateView-before-mediation order, production special creator/wait/coroutine, freeze/activation/session sinks, one-frame appear delay, spreading particle transfer, sorting unlock, alpha and pooled reuse. Audio dispatch 314/.04 is traced at the required service boundary; AudioManager playback, rocket explosion/combo travel, full scene input/refill/settlement and original rendered parity remain pending." };
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "rocket_creation_unity_verification.json");
            File.WriteAllText(path, JsonUtility.ToJson(result, true) + "\n");
            Debug.Log("Rocket creation Unity verification passed: " + path);
            Cleanup();
        }
        private static void VerifyAppeared()
        {
            Require(services.AppearFrame - services.SoundFrame == 1, "Native extra frame before creation particles");
            for (int i = 0; i < services.Cells.Length; i++) Require(services.Cells[i].FallFreezeCount == 0, "Creation unfreezes each copied group cell");
            Require(!services.Operations.Container.HasAny(), "All creation operations released");
            if (test == 2) services.CountBack();
            if (test == 2) Require(services.BackParticles > 0, "Spreading branch moves actual emitted particles behind item");
        }
        private static void ReleaseCase()
        {
            presentation.Particles.Clear(false);
            services.Model.Mediator.ClearFromAllRegisteredCells();
            presentation.Pool.Recycle(services.Model.ItemView);
            services.Model = null;
            Require(presentation.Pool.GetPoolLength(8) == 20, "Rocket returned to pool");
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (presentation != null && presentation.Pool.IsPoolAlive())
            {
                if (services != null && services.Model != null) ReleaseCase();
                presentation.Particles.Clear(false);
            }
            presentation = null; services = null; reused = null;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private sealed class Services : IOriginalSpecialItemCreationServices, IOriginalRocketViewServices, IOriginalRocketItemStaticState
        {
            private readonly OriginalLevelPresentation owner;
            private readonly int scenario;
            public OriginalRocketBoardItem Model;
            public readonly OriginalGridCell[] Cells = new OriginalGridCell[4];
            public readonly OriginalGameOperations Operations = new OriginalGameOperations(() => Time.frameCount);
            public int SoundFrame = -1, AppearFrame = -1, Increments, Records, Madness, Missions, Useful;
            public int BackParticles;
            public float SoundTime;
            public Services(OriginalLevelPresentation owner, int scenario)
            {
                this.owner = owner; this.scenario = scenario;
                for (int i = 0; i < Cells.Length; i++) { Cells[i] = new OriginalGridCell(scenario - 1 + i, 5, 1); Cells[i].BindGameOperations(Operations); }
            }
            public OriginalGameObjectPool Pool => owner.Pool;
            public int ItemSortingLayer => SortingLayer.NameToID("Item");
            public bool IsWaterMode => scenario == 2;
            public bool IsDeviceWide => false; // Explicit camera input; this verifier exercises creation only.
            public bool IsPlaying => true;
            public bool IsSessionStarted => true;
            public sbyte CurrentLevelDifficulty => 0;
            public bool IsAboveStaticSpreading(OriginalGridCell cell) => scenario == 2;
            public bool IsUnderHoney(OriginalBoardItem item) { Require(!item.HasView || item.Mediator.CurrentCell != null, "View init precedes HasView/mediation"); return false; }
            public bool IsUnderRoof(OriginalBoardItem item) => false;
            public bool IsUnderChain(OriginalBoardItem item) => false;
            public IOriginalSpecialCreationItem CreateItemAtCell(int tiledId, OriginalGridCell cell)
            {
                Model = OriginalRocketItemCreation.CreateAtCell(90000 + scenario, tiledId, cell, owner, owner.transform,
                    () => Time.time, this, this);
                return Model;
            }
            public void ManualStartCoroutine(IEnumerator routine) { owner.Coroutines.ManualStartCoroutine(routine); }
            public object CreateGameplayWait(float duration) => new OriginalGameplayWait(duration, () => OriginalLevelClock.QuantizeDelta(Time.deltaTime));
            public void IncrementItemCreation(int type, bool auto) { Require(type == 2 && !auto, "Creation statistic arguments"); Increments++; }
            public void SpecialItemCreated(IOriginalSpecialCreationItem model, OriginalGridCell cell, int delay, int count, bool sticker)
            { Require(model == Model && cell == Model.CurrentCell && delay == 17 && count == 1 && sticker, "Session record arguments"); Records++; }
            public void TryCollectMadness(OriginalGridCell cell, int type, int count, int delay, IOriginalSpecialCreationItem model, bool remaining)
            { Require(cell == Model.CurrentCell && model == Model && type == 2 && count == 4 && delay == 17 && !remaining, "Madness record arguments"); Madness++; }
            public void AddMissionEvent(int type, sbyte difficulty, int count) { Require(type == 2 && difficulty == 0 && count == 1, "Mission arguments"); Missions++; }
            public void UsefulMoveMade() { Useful++; }
            public void ReportMissingCreationCell() { throw new InvalidOperationException("Created rocket lost cell"); }
            public void PlaySfx(int type, float offset)
            {
                Require(type == 314 && offset == .04f && SoundFrame == -1, "Original creation sound dispatch");
                SoundFrame = Time.frameCount; SoundTime = Time.time;
            }
            public OriginalSpecialItemCreationParticles PlayAppearParticle(IOriginalSpecialCreationView view)
            {
                Require(view == Model.ItemView && AppearFrame == -1, "Actual rocket appear owner");
                AppearFrame = Time.frameCount;
                var front = owner.Particles.GetGlobalParticle<OriginalSpecialItemCreationParticles>(5);
                front.PlayOptimized(Model.ItemView.transform.position, Model.ItemView.transform);
                return front;
            }
            public void CountBack()
            {
                if (!EditorApplication.isPlaying || Model == null || scenario != 2) return;
                if (BackParticles > 0) return;
                var backs = Object.FindObjectsOfType<OriginalSpecialItemCreationParticlesBack>();
                for (int i = 0; i < backs.Length; i++)
                    for (int j = 0; j < backs[i].particleSystems.Length; j++) BackParticles += backs[i].particleSystems[j].particleCount;
            }
        }
        [Serializable] private sealed class Result
        {
            public bool passed;
            public int cases, view_prewarm, spreading_particles;
            public float horizontal_seconds, vertical_seconds, water_vertical_seconds;
            public string scope;
        }
    }
}
