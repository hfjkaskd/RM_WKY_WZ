using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Diagnostics only. No alternate initialization, resources or game logic.
    public static class TntCreationVerification
    {
        private static OriginalLevelPresentation owner;
        private static Services services;
        private static OriginalTntItemSpriteView reused;
        private static int test, frame, phase, animationFrames, sortingFixes, resumedFuses;
        private static float started, phaseStarted;
        private static readonly float[] durations = new float[3];
        [MenuItem("Recovery/Verify TNT Creation (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || owner != null) throw new InvalidOperationException("Run once in Play mode.");
            owner = Object.FindObjectOfType<OriginalLevelPresentation>();
            try
            {
                Require(owner != null && owner.Pool.GetPoolLength(14) == 20 && owner.Pool.GetPoolLength(16) == 20, "Configured TNT/fuse pools");
                test = phase = sortingFixes = resumedFuses = 0; reused = null; started = Time.realtimeSinceStartup;
                Begin(); EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }
        private static void Begin()
        {
            services = new Services(owner, test);
            var group = new OriginalMatchGroupSnapshot(1, test == 1 ? 6 : 5, 2);
            for (int i = 0; i < services.Cells.Length; i++) group.Cells.Add(services.Cells[i]);
            owner.CreateSpecialItemCreator(services).CreateSpecialItemForMatchGroup(group, services.Cells[0]);
            var model = services.Model;
            Require(model.ItemType == 4 && model.RemainingMoveCoins == 3 && !model.IsCreatedByLightball && model.HasView,
                "Native TNT constructor/type/view state");
            Require(model.Config.Radius == 2 && model.Config.RingInterval == .02f && model.Config.ExplodeDelay == .1f
                && model.Config.Shake.Delay == .05f && model.Config.Shake.Duration == .15f && model.Config.Shake.MaxVibrate == .2f
                && !model.Config.Shake.ShouldScale && model.Config.Shake.ShouldVisitCenter && model.Config.Shake.Direction == 0,
                "Original TNT explosion/shake config prepared after view");
            Require(!model.IsActive && !model.ItemView.gameObject.activeSelf && owner.Coroutines.Count == 1, "Creator wait reservation");
            Require(model.CurrentCell == services.Cells[0] && model.Mediator.NextCell.Owner == model.CurrentCell
                && model.Mediator.TargetCell.Owner == model.CurrentCell, "Native three-slot registration");
            Require(model.ItemView.transform.localPosition == new Vector3(model.CurrentCell.X, model.CurrentCell.Y, 0f), "CellPoint position");
            Require(owner.Pool.GetPoolLength(14) == 19 && owner.Pool.GetPoolLength(16) == 19, "Concrete view and owned fuse spawned");
            Require(reused == null || reused == model.ItemView, "TNT view reused"); reused = model.ItemView;
            Require(services.RecordCalls == 5, "Creation/session/madness/mission/useful records dispatched once");
            for (int i = 0; i < services.Cells.Length; i++) Require(services.Cells[i].FallFreezeCount == 1, "Copied group frozen");
            frame = Time.frameCount; animationFrames = 0;
        }
        private static void Tick() { try { Step(); } catch { Cleanup(); throw; } }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Require(Time.realtimeSinceStartup - started < 25f, "TNT verification timeout");
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            var view = services.Model.ItemView;
            var fuse = (OriginalTntFuseParticles)view.Fuse;
            if (phase == 1)
            {
                if (view.animator.isActiveAndEnabled) return;
                Require(Time.time - phaseStarted > 1.35f && view.baseView.gameObject.activeSelf && view.GetSorting().Order == 1000,
                    "Recovered booster animation completes naturally");
                view.PlayLightballCreationAnimation(); phaseStarted = Time.time; phase = 2; return;
            }
            if (phase == 2)
            {
                Require(view.animator.isActiveAndEnabled && view.fuseMask.gameObject.activeSelf && view.fuseForLightball.gameObject.activeSelf,
                    "Lightball creation visibility retained");
                Require(view.fuseForLightball.maskInteraction == SpriteMaskInteraction.VisibleOutsideMask
                    && view.fuseMask.backSortingOrder == 1008 && view.fuseMask.frontSortingOrder == 1012,
                    "Original lightball fuse mask sorting range");
                // Isolate transform-copy behavior from Animator's later
                // player-loop evaluation. Natural clip progression is still
                // checked below; phase equivalence is not asserted here.
                view.ManualUpdate();
                Require((fuse.Transform.position - view.movingFuseParent.position).sqrMagnitude < .0001f
                    && (view.fuseMask.transform.position - view.fuseMaskAnimated.position).sqrMagnitude < .0001f,
                    "Native animated fuse and mask follow");
                if (Time.time - phaseStarted < .65f) return;
                Require(view.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f,
                    "Lightball state bypasses normal automatic stop after its half-second clip");
                view.StopCreationAnimation();
                Finish(); return;
            }
            if (services.SoundFrame < 0) return;
            if (view.animator.isActiveAndEnabled)
            {
                animationFrames++;
                Require(!view.baseView.gameObject.activeSelf && services.Model.IsActive, "Native creation visibility/activation");
                Require(view.GetSorting().Order == (view.DidFixCreationSorting || test != 0 && services.AppearFrame >= 0 ? 1000 : 9100),
                    "Native early sorting unlock");
                var normalized = view.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                if (normalized < .38f) Require(!fuse.fuseParticles.isPlaying, "Fuse stopped during first animation portion");
                if (normalized > .53f)
                {
                    Require(fuse.fuseParticles.isPlaying, "Fuse resumes beyond half animation");
                    resumedFuses++;
                }
                if (services.AppearFrame >= 0) CheckAppear();
                return;
            }
            durations[test] = Time.time - services.SoundTime;
            Require(animationFrames > 2 && durations[test] > 1.35f && durations[test] < 1.8f, "Recovered 1.45 second animation completes naturally");
            Require(view.DidFixCreationSorting && view.baseView.gameObject.activeSelf && view.GetSorting().Order == 1000
                && owner.Coroutines.Count == 0, "Creation state settles"); sortingFixes++;
            CheckAppear();
            services.Model.IsPreparedForSpecialUse = true;
            Require(!services.Model.IsSwappable && !new OriginalItemSwapAvailability().IsAvailableForSwap(services.Model), "Prepared TNT rejects swap");
            services.Model.IsPreparedForSpecialUse = false;
            Require(new OriginalItemSwapAvailability().IsAvailableForSwap(services.Model), "Unprepared active steady TNT accepts swap");
            view.Hide(); Require(!fuse.fuseParticles.isPlaying, "Hide stops owned fuse");
            view.Show(); Require(fuse.fuseParticles.isPlaying, "Show resumes owned fuse");
            if (test == 2)
            {
                view.PlayBoosterAnimation(); phaseStarted = Time.time; phase = 1;
                Require(view.GetSorting().Layer == SortingLayer.NameToID("Ui") && view.GetSorting().Order == 7000, "Native booster sorting");
                return;
            }
            Release(); test++; Begin();
        }
        private static void CheckAppear()
        {
            Require(services.AppearFrame - services.SoundFrame == 1, "Creation particle extra frame");
            for (int i = 0; i < services.Cells.Length; i++) Require(services.Cells[i].FallFreezeCount == 0, "Group unfrozen");
            Require(!services.Operations.Container.HasAny(), "All creation operations released");
            if (test == 2 && services.BackCount == 0)
            {
                var backs = Object.FindObjectsOfType<OriginalSpecialItemCreationParticlesBack>();
                for (int i = 0; i < backs.Length; i++) for (int j = 0; j < backs[i].particleSystems.Length; j++) services.BackCount += backs[i].particleSystems[j].particleCount;
            }
            if (test == 2) Require(services.BackCount > 0, "Actual spreading particle transfer");
        }
        private static void Finish()
        {
            int backCount = services.BackCount; Release();
            var result = new Result { passed = true, creation_cases = 3, sorting_fixes = sortingFixes, resumed_fuse_observations = resumedFuses,
                spreading_particles = backCount, normal_seconds = durations[0], static_seconds = durations[1], spreading_seconds = durations[2],
                scope = "Actual recovered TNT/fuse prefabs and unchanged Animator clips with native concrete model/factory, creator/wait/coroutine, mediation, typed records, one-frame appear delay, static/spreading sorting and particle transfer, early sorting fix, late fuse resume, natural 1.45 second completion, owned fuse recycle/reuse, prepared-state swap gate, hide/show, booster state and lightball mask/fuse transform copies isolated by explicit diagnostic ManualUpdate with natural clip progression and external stop. Animator/Update phase equivalence is not asserted. Sound 346/.04 is traced at its required service boundary. Native full pool phases/low-end classification, AudioManager playback, TNT explosion/combo damage, player input/refill/settlement and original rendered parity are pending." };
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "tnt_creation_unity_verification.json"), JsonUtility.ToJson(result, true) + "\n");
            Debug.Log("TNT creation Unity verification passed."); Cleanup();
        }
        private static void Release()
        {
            var view = services.Model.ItemView;
            services.Model.Mediator.ClearFromAllRegisteredCells(); owner.Pool.Recycle(view); services.Model = null;
            // The view owns its fuse: recycle it before clearing global particles.
            owner.Particles.Clear(false);
            Require(owner.Pool.GetPoolLength(14) == 20 && owner.Pool.GetPoolLength(16) == 20 && view.Fuse == null,
                "Owned fuse and TNT return exactly once");
            Require(view.baseView.color == Color.white && !view.fuseMask.gameObject.activeSelf && !view.fuseForLightball.gameObject.activeSelf
                && view.fuseMask.transform.localPosition == new Vector3(-.18f, -.09f, 0f)
                && view.fuseMask.transform.localScale == new Vector3(50f, 50f, 1f), "Native recycle subset and mask pose");
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (owner != null && owner.Pool.IsPoolAlive())
            {
                if (services != null && services.Model != null) Release();
                owner.Particles.Clear(false);
            }
            owner = null; services = null; reused = null;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private sealed class AboveItem : IOriginalStaticTouchItem { public bool IsTouchBlocking => true; public bool IsTouchBlockingExceptChain => true; }
        private sealed class Services : IOriginalSpecialItemCreationServices, IOriginalTntViewServices, IOriginalSpecialItemChainedStaticState
        {
            private readonly OriginalLevelPresentation owner; private readonly int scenario;
            public readonly OriginalGridCell[] Cells = new OriginalGridCell[5];
            public readonly OriginalGameOperations Operations = new OriginalGameOperations(() => Time.frameCount);
            public OriginalTntBoardItem Model;
            public int RecordCalls, SoundFrame = -1, AppearFrame = -1, BackCount;
            public float SoundTime;
            public Services(OriginalLevelPresentation owner, int scenario)
            {
                this.owner = owner; this.scenario = scenario;
                for (int i = 0; i < Cells.Length; i++) { Cells[i] = new OriginalGridCell(scenario - 1 + i, 5, 1); Cells[i].BindGameOperations(Operations); }
                if (scenario == 1) Cells[0].StaticTouch.AboveItems.Add(new AboveItem());
            }
            public OriginalGameObjectPool Pool => owner.Pool;
            public int ItemSortingLayer => SortingLayer.NameToID("Item");
            public int ParticlesSortingLayer => SortingLayer.NameToID("Particles");
            public int UiSortingLayer => SortingLayer.NameToID("Ui");
            public bool IsPlaying => true; public bool IsSessionStarted => true; public sbyte CurrentLevelDifficulty => 0;
            public bool HasIncomingTouchBlockingItem(OriginalGridCell cell) => false;
            public bool IsFallingCross(OriginalBoardItem item)
            {
                var fall = item.FallComponent as OriginalFallComponentState;
                if (fall == null) throw new NullReferenceException(); return fall.IsFallingCross;
            }
            public bool IsAboveStaticSpreading(OriginalGridCell cell) => scenario == 2;
            public bool IsUnderHoney(OriginalBoardItem item) => scenario == 1 && item.Mediator.CurrentCell != null;
            public bool IsUnderRoof(OriginalBoardItem item) => false; public bool IsUnderChain(OriginalBoardItem item) => false;
            public IOriginalSpecialCreationItem CreateItemAtCell(int tiledId, OriginalGridCell cell)
            {
                Require(tiledId == 40, "Native TNT tile");
                Model = OriginalTntItemCreation.CreateAtCell(91000 + scenario, cell, owner, owner.transform, () => Time.time, this, this);
                Model.FallComponent = new OriginalFallComponentState(Model); return Model;
            }
            public void ManualStartCoroutine(IEnumerator routine) { owner.Coroutines.ManualStartCoroutine(routine); }
            public object CreateGameplayWait(float duration) => new OriginalGameplayWait(duration, () => OriginalLevelClock.QuantizeDelta(Time.deltaTime));
            public void IncrementItemCreation(int type, bool auto) { Require(type == 4 && !auto, "Creation record"); RecordCalls++; }
            public void SpecialItemCreated(IOriginalSpecialCreationItem model, OriginalGridCell cell, int delay, int count, bool sticker)
            { Require(model == Model && cell == Model.CurrentCell && delay == 17 && count == 1 && sticker, "Session record"); RecordCalls++; }
            public void TryCollectMadness(OriginalGridCell cell, int type, int count, int delay, IOriginalSpecialCreationItem model, bool remaining)
            { Require(cell == Model.CurrentCell && model == Model && type == 2 && count == 5 && delay == 17 && !remaining, "Madness record"); RecordCalls++; }
            public void AddMissionEvent(int type, sbyte difficulty, int count) { Require(type == 4 && difficulty == 0 && count == 1, "Mission record"); RecordCalls++; }
            public void UsefulMoveMade() { RecordCalls++; }
            public void ReportMissingCreationCell() { throw new InvalidOperationException("TNT lost cell"); }
            public void PlaySfx(int type, float offset) { Require(type == 346 && offset == .04f && SoundFrame == -1, "Native TNT SFX dispatch"); SoundFrame = Time.frameCount; SoundTime = Time.time; }
            public OriginalSpecialItemCreationParticles PlayAppearParticle(IOriginalSpecialCreationView view)
            {
                Require(view == Model.ItemView && AppearFrame == -1, "TNT appear owner"); AppearFrame = Time.frameCount;
                var front = owner.Particles.GetGlobalParticle<OriginalSpecialItemCreationParticles>(5);
                front.PlayOptimized(Model.ItemView.transform.position, Model.ItemView.transform); return front;
            }
        }
        [Serializable] private sealed class Result
        {
            public bool passed; public int creation_cases, sorting_fixes, resumed_fuse_observations, spreading_particles;
            public float normal_seconds, static_seconds, spreading_seconds; public string scope;
        }
    }
}
