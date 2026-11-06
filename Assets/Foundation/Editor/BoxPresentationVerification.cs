using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // A separate fixture uses the actual model/view/pool/emitter. No services
    // are installed into the production board by this verification command.
    public static class BoxPresentationVerification
    {
        private static OriginalLevelPresentation presentation;
        private static OriginalBoxBoardItem model;
        private static OriginalBoxItemSpriteView view;
        private static OriginalBoxExplodeParticles effect;
        private static OriginalGridCell cell;
        private static OriginalGameOperations operations;
        private static Services services;
        private static bool[] seen;
        private static int phase, hit, frame, selectedLayer;
        private static float started, hitTime;
        [MenuItem("Recovery/Verify Box Presentation (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || presentation != null) throw new InvalidOperationException("Run once in Play mode.");
            try
            {
                presentation = Object.FindObjectOfType<OriginalLevelPresentation>();
                var board = Object.FindObjectOfType<OriginalInitialBoardView>();
                Require(presentation != null && board != null && presentation.Pool.HasPool(36) && presentation.Pool.GetPoolLength(37) == 1, "Native box pool IDs and one global explode prewarm");
                var level = OriginalLevelReader.Load("Levels/level_1");
                Require(level.Name == "00001v02_L" && level.Goals.Length == 1 && level.Goals[0].Goal == 11,
                    "This verification uses the original fixed first-level goal data");
                int target = level.Goals[0].Count;
                Require(board.GoalPreparation != null && board.Goals != null && board.GoalClaims != null
                    && board.GoalPreparation.GetBoardCount() == 1 && board.Goals.GetAllGoals().Count == 1
                    && !board.GoalPreparation.IsBonusLevel && board.GoalPreparation.GoalClaims.Count == 0
                    && board.Goals.GetGoalTargetCount(11) == target && board.Goals.GetGoalLeftCount(11) == target
                    && board.Goals.GetGoalUiLeftCount(11) == target && board.Goals.GetGoalDisplayCount(11) == target
                    && !board.Goals.AreCurrentGoalsReached(), "Formal board initializes native model/UI goals from the original level");
                Require(board.ItemGoals != null, "Formal board composes concrete item goal services");
                for (int x = 0; x < board.Grid.Width; x++)
                for (int y = 0; y < board.Grid.Height; y++)
                {
                    var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                    int tiledId = level.Grid.Items[OriginalLevelRules.GetIndexFromPoint(x, y, board.Grid.Width)];
                    Require(item != null && item.GoalType == OriginalGoalTypes.FromTiledId(tiledId),
                        "Formal match/box goal identities come from native item-settings selection");
                }
                var root = ((OriginalMatchItemSpriteView)board.Grid.GetCell(0, 7).Mediation.CurrentItem.SwapView).transform.parent;
                cell = new OriginalGridCell(4, 10, 1);
                operations = new OriginalGameOperations(() => Time.frameCount);
                cell.BindGameOperations(operations); cell.BindFreezeCountdown(new OriginalCellFreezeCountdown(cell.UnFreezeFall));
                var audio = Object.FindObjectOfType<OriginalGameAudioHost>();
                Require(audio != null && audio.GameSfx != null, "Formal prefab composes actual game audio before the board");
                for (int type = 33; type <= 38; type++)
                {
                    var clip = audio.BoxAssets.GetSfxClip(type);
                    Require(clip != null && clip.samples > 0 && clip.channels == (type == 33 || type == 36 ? 1 : 2),
                        "Recovered original box clip preserves its mono/stereo channels and is decodable: " + type);
                }
                var savedRandom = UnityEngine.Random.state;
                for (int seed = 62021; seed < 62033; seed++)
                {
                    UnityEngine.Random.InitState(seed);
                    int expectedPair = UnityEngine.Random.Range(-5, 3), expectedMax = UnityEngine.Random.Range(0, 9);
                    bool expectedBool = UnityEngine.Random.Range(0, 2) == 0;
                    float expectedFloat = UnityEngine.Random.Range(-1.5f, 6.25f);
                    UnityEngine.Random.InitState(seed);
                    Require(audio.Random.Next(-5, 3) == expectedPair && audio.Random.Next(9) == expectedMax
                        && audio.Random.NextBool() == expectedBool && audio.Random.Next(-1.5f, 6.25f) == expectedFloat,
                        "Application random uses the original shared Unity integer/boolean/float sequence");
                }
                UnityEngine.Random.state = savedRandom;
                services = new Services(presentation, operations, audio);
                model = new OriginalBoxBoardItem(20001, 6, 4) { GoalType = 11 };
                cell.Mediation.SetAllItems(model); model.BindGoalServices(services);
                model.OnExplodeEvent += OnExploded;
                model.CreateView(presentation, root, new Vector3(cell.X, cell.Y, 0f), services, services);
                view = model.ItemView;
                Require(view.baseView.sprite.name == "Items-BoxItem-box_3" && model.HasView && view.Model == model, "Original layer-four sprite and model/view binding");
                Require(view.GetSorting().Layer == services.ItemSortingLayer && view.GetSorting().Order == 1000
                    && view.shadowView.sortingOrder == view.baseView.sortingOrder - 1, "Native group and local shadow sorting");
                view.Hide(); Require(!view.visualRoot.gameObject.activeSelf && !view.shadowView.gameObject.activeSelf, "Box Hide includes shadow");
                view.Show(); Require(view.visualRoot.gameObject.activeSelf && view.shadowView.gameObject.activeSelf, "Box Show includes shadow");
                effect = presentation.Particles.GetGlobalParticle<OriginalBoxExplodeParticles>(37);
                Require(effect.layer1.Length == 7 && effect.layer2.Length == 2 && effect.layer3.Length == 2 && effect.layer4.Length == 2, "Original particle array lengths");
                int clones = 0;
                foreach (var layer in Layers())
                    for (int i = 0; i < layer.Length; i++)
                        if (layer[i].shadow != null)
                        {
                            clones++;
                            Require(layer[i].shadow.system != layer[i].particle
                                && layer[i].shadow.system.randomSeed == layer[i].particle.randomSeed,
                                "Configured shadow source cloned during Awake");
                        }
                Require(clones == 7, "Seven original shadow sources");
                started = Time.realtimeSinceStartup; frame = Time.frameCount; phase = 0; hit = 0;
                EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }
        private static OriginalParticleInfo[][] Layers() => new[] { effect.layer1, effect.layer2, effect.layer3, effect.layer4 };
        private static void Tick() { try { Step(); } catch { Cleanup(); throw; } }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Require(Time.realtimeSinceStartup - started < 12f, "Box presentation verification timeout");
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            cell.FreezeCountdown.Update(OriginalLevelClock.QuantizeDelta(Time.deltaTime));
            if (phase == 0)
            {
                ClearParticles();
                services.SeedCheckData();
                selectedLayer = 3 - hit;
                seen = new bool[Layers()[selectedLayer].Length];
                var data = OriginalExploder.Next(5);
                model.Explode(data);
                Require(model.LayerCount == selectedLayer && services.Sounds == hit + 1 && services.Useful == hit + 1, "Model layer damage and original sound/useful order");
                Require(services.UsefulState.IsUsefulMoveMade && services.King.ReliefCoroutine != null
                    && services.CheckData.IsThereLightballCombo, "Useful flag follows relief request; board reset waits one Unity frame");
                var relief = services.King.ReliefCoroutine;
                services.King.TryKingRelief();
                Require(ReferenceEquals(relief, services.King.ReliefCoroutine), "Repeated same-frame relief request retains its existing coroutine");
                model.Explode(data);
                Require(model.LayerCount == selectedLayer && services.Sounds == hit + 1, "Repeated exploder ID does not damage twice");
                if (selectedLayer > 0)
                {
                    Require(model.HasView && view.gameObject.activeSelf && cell.Mediation.CurrentItem == model
                        && view.baseView.sprite.name == "Items-BoxItem-box_" + (selectedLayer - 1), "Damage updates sprite without model removal");
                    Require(services.GoalState.GetGoalLeftCount(11) == 1 && services.GoalState.GetGoalUiLeftCount(11) == 1
                        && !services.GoalState.IsAllGoalsReached(), "Partial box damage retains actual model/UI goal counts");
                }
                else Require(!model.HasView && !view.gameObject.activeSelf && cell.Mediation.CurrentItem == null
                    && cell.Mediation.NextItem == null && cell.Mediation.TargetItem == null
                    && services.Goals == 1 && services.GoalUi == 1 && services.Story == 1 && services.Exploded == 1
                    && presentation.Pool.GetPoolLength(36) == 1, "Final fracture recycles view before model/goal/event completion");
                ObserveParticles(); hitTime = Time.time; phase = 1; return;
            }
            ObserveParticles();
            if (Time.time - hitTime < .25f) return;
            Require(services.King.ReliefCoroutine == null && !services.CheckData.IsThereLightballCombo
                && !services.CheckData.IsThereSingleLightball && services.CheckData.ItemTypesForTotalLayer.Count == 0
                && services.CheckData.MatchTypesForLightball.Count == 0 && services.CheckData.CheckedComboTypes.Count == 0,
                "Native board-check state reset and relief handle clear after one-frame check");
            for (int i = 0; i < services.CheckData.MatchItemCounts.Length; i++)
                Require(services.CheckData.MatchItemCounts[i] == 0, "King check resets all original match-type counters");
            for (int i = 0; i < seen.Length; i++) Require(seen[i], "Every original selected-layer burst emitted in Unity: " + selectedLayer + "/" + i);
            Require(presentation.Particles.GetGlobalParticle<OriginalBoxExplodeParticles>(37) == effect && presentation.Pool.GetPoolLength(37) == 0,
                "All four hits use one shared global particle");
            hit++;
            if (hit < 4) { phase = 0; return; }
            if (cell.IsFallFrozen) return;
            Require(!operations.Container.HasAny(), "Box final freeze timer releases operation");
            Require(services.GoalState.GetGoalLeftCount(11) == 0 && services.GoalState.GetGoalUiLeftCount(11) == 0
                && services.GoalState.GetGoalDisplayCount(11) == 0 && services.GoalState.IsAllGoalsReached()
                && services.GoalNotifications == 1 && services.UiNotifications == 1,
                "Actual goal manager model/display counts and completion after final box release");
            var replacement = new OriginalBoxBoardItem(20002, 6, 1);
            replacement.CreateView(presentation, view.transform.parent, new Vector3(1, 2, 0), services, services);
            Require(replacement.ItemView == view && view.gameObject.activeSelf && view.baseView.sprite.name == "Items-BoxItem-box_0"
                && view.animationRoot.localScale == Vector3.one && view.animationRoot.localRotation == Quaternion.identity
                && view.Model == replacement, "Same configured view reused and layer-one sprite restored");
            var result = new Result { passed = true, layer_hits = 4, particle_systems = 13, shadow_clones = 7,
                shared_global_particle_checked = true, pooled_view_reuse_checked = true, actual_model_damage_and_removal_checked = true,
                actual_goal_state_checked = true,
                production_goal_initialization_checked = true,
                production_item_goal_services_checked = true,
                production_view_goal_collection_checked = true,
                actual_useful_move_and_king_relief_checked = true,
                actual_audio_playback_checked = true,
                application_random_checked = true,
                scope = "Formal goal initialization/identities and item goal services plus separate pooled four-hit box fixture. Uses real goal/view collection, model/view/particle/recycle/freeze and useful-state/King iterator with actual coroutine player. Now uses actual application Unity Random.Range and formal GameSfx/AudioManager one-shot path on configured original StartScene AudioSource. Checks six decodable original box clips, selected variant/offset and source playback after each hit. Tests API playback state, not speaker output. Recovered Resources paths supply original/bundle clips; native download lifecycle and application audio preferences/music/looping remain pending. Explicit unworried/final-board context and full King analysis/UI remain boundaries. Formal box damage/session/active-level composition, trail/progress visuals, HUD, normal resolution, fall/refill/settlement and rendered-original parity remain pending." };
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "box_presentation_unity_verification.json"), JsonUtility.ToJson(result, true) + "\n");
            Debug.Log("Box presentation Unity verification passed."); Cleanup();
        }
        private static void ObserveParticles()
        {
            var layers = Layers();
            for (int layer = 0; layer < layers.Length; layer++)
                for (int i = 0; i < layers[layer].Length; i++)
                {
                    var info = layers[layer][i];
                    if (layer != selectedLayer) { Require(info.particle.particleCount == 0, "Unselected box layer does not emit"); continue; }
                    if (info.particle.particleCount > 0) seen[i] = true;
                    if (info.shadow != null)
                        Require(info.shadow.system.randomSeed == info.particle.randomSeed
                            && info.shadow.system.particleCount == info.particle.particleCount, "Real burst and cloned shadow share seed/count");
                }
        }
        private static void ClearParticles()
        {
            foreach (var layer in Layers())
                for (int i = 0; i < layer.Length; i++)
                {
                    layer[i].particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (layer[i].shadow != null) layer[i].shadow.system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
        }
        private static void OnExploded()
        {
            Require(!model.HasView && model.Mediator.CurrentCell == null && !view.gameObject.activeSelf && services.Goals == 1 && services.GoalUi == 1,
                "Final event follows native goal/UI and view/model removal"); services.Exploded++;
            Require(!services.UsefulState.IsUsefulMoveMade, "Box explode event precedes its final useful-move request");
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (presentation != null && presentation.Pool != null && presentation.Pool.IsPoolAlive())
            {
                if (effect != null) ClearParticles();
                presentation.Particles.Clear(false);
                if (view != null && view.gameObject.activeSelf) presentation.Pool.Recycle(view);
            }
            presentation = null; model = null; view = null; effect = null; services = null; cell = null;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private sealed class Services : IOriginalBoxViewServices, IOriginalBoxExplosionServices, IOriginalItemGoalServices, IOriginalKingReliefServices
        {
            private readonly OriginalLevelPresentation owner;
            private readonly OriginalGameAudioHost audio;
            private int selectedRandom;
            public int Sounds, Useful, Goals, GoalUi, Story, Exploded;
            public int GoalNotifications, UiNotifications;
            public OriginalGoalManager GoalState { get; }
            private readonly OriginalItemGoalServices itemGoals;
            private readonly OriginalItemViewGoalCollection viewCollection;
            private readonly OriginalItemUsefulMoveServices usefulMoves;
            public OriginalUsefulMoveState UsefulState { get; }
            public OriginalKingRelief King { get; }
            public OriginalKingBoardCheckData CheckData { get; }
            public Services(OriginalLevelPresentation owner, OriginalGameOperations operations, OriginalGameAudioHost audio)
            {
                this.owner = owner;
                this.audio = audio;
                CheckData = new OriginalKingBoardCheckData
                {
                    MatchItemCounts = new int[7], // Native MatchType identities 0..6, without Enum reflection.
                    ItemTypesForTotalLayer = new Dictionary<int, HashSet<int>>(),
                    MatchTypesForLightball = new HashSet<int>(), CheckedComboTypes = new HashSet<int>()
                };
                King = new OriginalKingRelief(CheckData, owner.Coroutines, this);
                UsefulState = new OriginalUsefulMoveState(() => King);
                usefulMoves = new OriginalItemUsefulMoveServices(() => UsefulState);
                var claims = new OriginalGoalClaims();
                GoalState = new OriginalGoalManager(() => 0, operations, claims);
                viewCollection = new OriginalItemViewGoalCollection(claims, new NormalFixtureContext(),
                    () => throw new InvalidOperationException("A normal final board does not request the multiple-board system."), null);
                GoalState.InitWithGoalsAndLimits(new[] { new Dictionary<int, OriginalGoalTuple>
                    { { 11, new OriginalGoalTuple(11, 1, false, GoalState) } } },
                    new[] { new Dictionary<int, OriginalLimitTuple>() }, false);
                itemGoals = new OriginalItemGoalServices(GoalState,
                    () => throw new InvalidOperationException("Box item UI collection must not resolve progress for goal type 11."));
                GoalState.OnGoalUpdated += (type, count) =>
                {
                    Require(type == 11 && count == 0 && Goals == 0 && GoalUi == 0
                        && GoalState.GetGoalUiLeftCount(11) == 1 && !model.HasView && model.Mediator.CurrentCell == null,
                        "Model goal notification precedes UI collection and follows release");
                    GoalNotifications++;
                };
                GoalState.OnGoalUpdatedUi += (type, count, prerequisite, start, complete) =>
                {
                    Require(type == 11 && count == 0 && !prerequisite && !start && complete
                        && Goals == 1 && GoalUi == 0 && GoalNotifications == 1,
                        "UI notification follows model notification with native completion flags");
                    UiNotifications++;
                };
            }
            public OriginalGameObjectPool Pool => owner.Pool;
            public OriginalParticlesManager Particles => owner.Particles;
            public int ItemSortingLayer => SortingLayer.NameToID("Item");
            public float LevelDeltaTime => owner.LevelDeltaTime;
            public int BoardStartY => 0;
            public int NextApplicationRandom(int min, int max)
            { Require(min == 0 && max == 3, "Original application random bounds"); selectedRandom = audio.Random.Next(min, max); return selectedRandom; }
            public void PlaySfx(int type, float delay)
            {
                Require(type == (Sounds < 3 ? 36 + selectedRandom : 33 + selectedRandom) && delay == .04f,
                    "Original damage/final sound variants and throttle offset");
                audio.GameSfx.PlaySfx(type, delay);
                Require(audio.SoundSource.isPlaying, "Actual configured source plays original box one-shot"); Sounds++;
            }
            public void TryCollectForStoryLevel(OriginalBoxItemSpriteView item, Vector3 position)
            {
                Require(item.Model == model && model.HasView && model.Mediator.CurrentCell != null && item.gameObject.activeSelf && position == item.baseView.transform.position,
                    "View collect request precedes view/model recycle");
                Require(viewCollection.TryCollectForStoryLevel(item.Model.ItemType, position.x, position.y, position.z) == null,
                    "Real empty claims and normal-final-board collection gate require no progress effect");
                Story++;
            }
            public void SeedCheckData()
            {
                UsefulState.ResetIsUsefulMoveMade();
                CheckData.IsThereLightballCombo = true; CheckData.IsThereSingleLightball = true;
                for (int i = 0; i < CheckData.MatchItemCounts.Length; i++) CheckData.MatchItemCounts[i] = i + 1;
                CheckData.ItemTypesForTotalLayer.Add(6, new HashSet<int> { 11 });
                CheckData.MatchTypesForLightball.Add(1); CheckData.CheckedComboTypes.Add(2);
            }
            public void UsefulMoveMade() { usefulMoves.UsefulMoveMade(); Useful++; }
            public bool IsWorried => false;
            public int LeftMoves => throw new InvalidOperationException("Unworried relief must not query moves.");
            public int LastMoveLayerCapsCount => throw new InvalidOperationException("Unworried relief must not query caps.");
            public bool IsWinPossible() { throw new InvalidOperationException("Unworried relief must not analyze a win."); }
            public bool IsPlaying() { throw new InvalidOperationException("Unworried relief must not query game state."); }
            public bool ShouldKingBeWorried() { throw new InvalidOperationException("Unworried relief must not analyze worry."); }
            public void TryStopWorry(int reason) { throw new InvalidOperationException("Unworried relief must not stop worry."); }
            public void DecreaseGoal(int type)
            {
                Require(type == 11 && !model.HasView && model.Mediator.CurrentCell == null, "Goal update follows model removal");
                itemGoals.DecreaseGoal(type); Goals++;
            }
            public void CollectAsGoalUi(OriginalBoardItem item)
            {
                Require(item == model && Goals == 1, "Box goal UI follows goal decrement");
                itemGoals.CollectAsGoalUi(item); GoalUi++;
            }
        }
        private sealed class NormalFixtureContext : IOriginalViewCollectionLevelContext
        {
            public bool IsStoryLevel => false;
            public bool IsFinalBoard => true;
            public bool IsPortalLevel => false;
            public bool IsWaterLevel => false;
            public bool IsMultipleBoardLevel => false;
        }
        [Serializable] private sealed class Result
        { public bool passed, shared_global_particle_checked, pooled_view_reuse_checked, actual_model_damage_and_removal_checked, actual_goal_state_checked, production_goal_initialization_checked, production_item_goal_services_checked, production_view_goal_collection_checked, actual_useful_move_and_king_relief_checked, actual_audio_playback_checked, application_random_checked; public int layer_hits, particle_systems, shadow_clones; public string scope; }
    }
}
