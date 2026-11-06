using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // An explicit verification command. It calls the same runtime components
    // as a device build, and never changes the board or supplies game fallbacks.
    public static class MatchPresentationVerification
    {
        private static OriginalMatchExplodeParticles effect;
        private static OriginalMatchItemSpriteView match;
        private static OriginalLevelPresentation presentation;
        private static int matchAvailableBefore;
        private static bool checkingExplosion;
        private static bool checkingMerge;
        private static int mergeCallbackCount;
        private static Vector3 mergeCompletedPosition;
        private static int frames;
        private static int particlesAtEmission;
        private static bool running;
        private static float startedAt;
        private static readonly ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[16];

        [MenuItem("Recovery/Verify Match Presentation (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || running)
                throw new InvalidOperationException("Run this verification once in Play mode.");
            presentation = Object.FindObjectOfType<OriginalLevelPresentation>();
            Require(presentation != null && presentation.Pool != null, "Running scene pool");
            Require(presentation.Pool.GetPoolLength(2) == 74 && presentation.Pool.GetPoolLength(4) == 1,
                "Native 110 match views minus 36 board views and one explode prewarm");
            matchAvailableBefore = presentation.Pool.GetPoolLength(2);
            effect = presentation.Particles.GetGlobalParticle<OriginalMatchExplodeParticles>(4);
            Require(effect == presentation.Particles.GetGlobalParticle<OriginalMatchExplodeParticles>(4)
                && presentation.Particles.ActiveCount == 1 && presentation.Pool.GetPoolLength(4) == 0,
                "Global particle cache and spawn callback");
            match = presentation.SpawnMatchView();
            running = true;
            try
            {
                VerifyRecycle();
                Require(effect.blastParts.Length == 5 && effect.shadowParticles.Length == 5, "Recovered particle array lengths");
                for (int i = 0; i < 5; i++)
                {
                    var part = effect.blastParts[i];
                    var shadow = effect.shadowParticles[i].system;
                    Require(shadow != part && shadow.transform.parent == part.transform.parent, "Shadow clone/parent");
                    Require(shadow.GetComponent<ParticleSystemRenderer>().sortingOrder ==
                        part.GetComponent<ParticleSystemRenderer>().sortingOrder - 1, "Shadow sorting");
                }
                for (int type = 1; type <= 5; type++)
                    effect.Play(new Vector3(type - 3, 0f, 0f), type % 2 == 0, type);
                Require(effect.lightRing.particleCount == 5 && effect.blastDots.particleCount == 50, "Ring/dot emission count");
                for (int type = 1; type <= 5; type++)
                {
                    int index = OriginalMatchExplodeParticles.GetIndexByType(type);
                    var part = effect.blastParts[index];
                    var shadow = effect.shadowParticles[index].system;
                    int expected = type % 2 == 0 ? 8 : 4;
                    Require(part.particleCount == expected && shadow.particleCount == expected, "Fragment/shadow emission count");
                    Require(part.randomSeed == shadow.randomSeed, "Paired fragment/shadow seed");
                    particlesAtEmission += part.particleCount + shadow.particleCount;
                }
                frames = Time.frameCount;
                startedAt = Time.realtimeSinceStartup;
                EditorApplication.update += Tick;
            }
            catch
            {
                Cleanup();
                throw;
            }
        }
        private static void VerifyRecycle()
        {
            BindTestMatch();
            match.RootScale = new OriginalSwapPoint(.25f, .5f, .75f);
            var renderers = match.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer baseView = null, overlay = null, shadow = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name == "Base") baseView = renderers[i];
                else if (renderers[i].name == "Overlay") overlay = renderers[i];
                else if (renderers[i].name == "Shadow") shadow = renderers[i];
            }
            Require(baseView != null && overlay != null && shadow != null, "Recovered renderer bindings");
            baseView.transform.localPosition = new Vector3(1, 2, 3);
            shadow.transform.localPosition = new Vector3(3, 2, 1);
            baseView.color = new Color(.2f, .4f, .6f, .1f);
            overlay.gameObject.SetActive(true);
            match.SwapAnimationStarted(false, 0);
            match.UpdateSortingForAnimation(new OriginalSwapSorting(231352099, 2222, false), false);
            var original = match;
            presentation.RecycleView(match);
            Require(!match.gameObject.activeSelf && match.transform.parent == presentation.Pool.GetPoolTransform()
                && presentation.Pool.GetPoolLength(2) == matchAvailableBefore, "View returned to pool");
            var scale = match.RootScale;
            Require(scale.X == 1 && scale.Y == 1 && scale.Z == 1, "Recycle scale");
            Require(baseView.transform.localPosition == Vector3.zero && shadow.transform.localPosition == Vector3.zero,
                "Recycle positions");
            Require(baseView.transform.parent.localScale == Vector3.one &&
                baseView.transform.parent.localPosition == Vector3.zero, "Recycle view parent");
            Require(baseView.color == new Color(.2f, .4f, .6f, 1f) && !overlay.gameObject.activeSelf,
                "Recycle alpha/highlight");
            Require(match.IsSwapping, "Native recycle preserves swapping flag");
            match.UpdateSorting(new OriginalSwapSorting(231352099, 4444, false));
            Require(baseView.sortingOrder == 4444 && shadow.sortingOrder == 4443 && overlay.sortingOrder == 9000,
                "Recycle enables sorting updates");
            match = presentation.SpawnMatchView();
            Require(match == original && match.gameObject.activeSelf, "Same view reused");
            match.HideView();
            baseView.sortingLayerID = 0;
            BindTestMatch();
            Require(!match.IsSwapping && baseView.gameObject.activeInHierarchy && baseView.sortingLayerID == 231352099,
                "Init restores swap/hidden state and original Item layer");
            Require(baseView.transform.localPosition == new Vector3(.007f, -.01f, 0f)
                && shadow.transform.parent.localPosition == new Vector3(-.017f, -.015f, 0f)
                && shadow.color == new Color(1f, 1f, 1f, .37f), "Native blue item pose");
        }
        private static void BindTestMatch()
        {
            match.BindModel(new OriginalMatchBoardItem(10001, OriginalTiledItemTypes.AsItemType(1), 1));
            match.Bind(Resources.Load<Sprite>("Recovered/Sprite/Items-MatchItem-shield"),
                Resources.Load<Sprite>("Recovered/Sprite/Items-MatchItem-shadow_shield"),
                new OriginalMatchViewPose { BaseOffset = new Vector3(.007f, -.01f, 0f),
                    ShadowParentOffset = new Vector3(-.017f, -.015f, 0f), ShadowColor = new Color(1f, 1f, 1f, .37f) });
        }
        private static void Tick()
        {
            try { TickState(); }
            catch { Cleanup(); throw; }
        }
        private static void TickState()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            if (Time.realtimeSinceStartup - startedAt > 10f)
            {
                Cleanup();
                throw new InvalidOperationException("Shadow particles did not finish within ten seconds.");
            }
            // Wait for actual player frames, not editor update frequency.
            if (Time.frameCount == frames) return;
            frames = Time.frameCount;
            if (effect == null) { Cleanup(); throw new InvalidOperationException("Particle verification instance disappeared."); }
            if (checkingMerge)
            {
                if (mergeCallbackCount == 0) return;
                Require(mergeCallbackCount == 1 && !match.gameObject.activeSelf && !match.HasMergeCoroutine
                    && presentation.Coroutines.Count == 0 && presentation.Pool.GetPoolLength(2) == matchAvailableBefore,
                    "Merge callback recycle and manual host stop");
                Require(match.transform.position == mergeCompletedPosition, "Stopped merge does not snap pooled view to target");
                Finish();
                return;
            }
            if (checkingExplosion)
            {
                if (match.gameObject.activeSelf) return;
                Require(presentation.Pool.GetPoolLength(2) == matchAvailableBefore
                    && presentation.Particles.ActiveCount == 1 && effect.lightRing.particleCount > 0,
                    "Concrete animation-to-global-particle-to-view-pool chain");
                // Start on an ordinary frame so this checks an asynchronous
                // callback while the native coroutine handle is registered.
                if (Time.deltaTime >= .1f) return;
                checkingExplosion = false;
                checkingMerge = true;
                match = presentation.SpawnMatchView();
                BindTestMatch();
                match.transform.position = new Vector3(-2f, 1f, 0f);
                match.UpdateSorting(new OriginalSwapSorting(0, 2222, false));
                match.ExplodeAndMoveToPosition(new Vector3(2f, 1f, 0f), 3, CompleteMerge);
                Require(presentation.Coroutines.Count == 1 && match.HasMergeCoroutine
                    && match.GetSorting().Layer == 231352099 && match.GetSorting().Order == 8997,
                    "Merge registered and indexed Item sorting applied after temporary layer override");
                return;
            }
            bool alive = false;
            for (int i = 0; i < effect.shadowParticles.Length; i++)
            {
                var shadow = effect.shadowParticles[i].system;
                int count = shadow.GetParticles(particleBuffer);
                alive |= count != 0;
                for (int j = 0; j < count; j++)
                {
                    var color = particleBuffer[j].startColor;
                    if (color.r != 0 || color.g != 0 || color.b != 0)
                    {
                        Cleanup();
                        throw new InvalidOperationException("Shadow color was not updated by the runtime component.");
                    }
                }
            }
            if (alive) return;
            presentation.Particles.Clear(false);
            Require(presentation.Particles.ActiveCount == 0 && presentation.Pool.GetPoolLength(4) == 1
                && !effect.gameObject.activeSelf, "Manager clear and recycle callback");
            var reused = presentation.Particles.GetGlobalParticle<OriginalMatchExplodeParticles>(4);
            Require(reused == effect && presentation.Particles.ActiveCount == 1, "Global particle reused after clear");
            match.StartExplodeAnimation(2);
            checkingExplosion = true;
        }
        private static void CompleteMerge()
        {
            Require(match.gameObject.activeSelf && match.HasMergeCoroutine && presentation.Coroutines.Count == 1,
                "Merge callback precedes host stop and view recycle");
            mergeCallbackCount++;
            mergeCompletedPosition = match.transform.position;
            presentation.RecycleView(match);
        }
        private static void Finish()
        {
            var result = new Result { passed = true, unity_version = Application.unityVersion,
                shadow_clone_count = 5, ring_particles = 5, dot_particles = 50,
                fragment_and_shadow_particles = particlesAtEmission, view_recycle_checked = true,
                prewarmed_match_views = 110, initial_board_match_views = 36, pool_reuse_checked = true,
                global_particle_cache_checked = true, concrete_explosion_services_checked = true,
                manual_merge_recycle_checked = true,
                additive_shader = effect.lightRing.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name,
                scope = "Runtime recovered-prefab clone/seed/sorting/emission, shadow lifetime and view reset. Scene match pool/reuse, global cache/clear callbacks, concrete shrink/particle/recycle chain, and indexed merge movement on the restored manual host with callback-before-stop, pooled view deactivation and no later target snap. No player input, full model-resolution adapter, special creation, refill or rendered-original parity is asserted." };
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "match_presentation_unity_verification.json");
            File.WriteAllText(path, JsonUtility.ToJson(result, true) + "\n");
            Debug.Log("Match presentation Unity verification passed: " + path);
            Cleanup();
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (presentation != null && presentation.Pool.IsPoolAlive())
            {
                presentation.Particles.Clear(false);
                if (match != null) match.StopMergeCoroutine();
                if (match != null && match.gameObject.activeSelf) presentation.RecycleView(match);
            }
            effect = null; match = null; running = false; frames = 0; particlesAtEmission = 0;
            presentation = null; checkingExplosion = false;
            checkingMerge = false; mergeCallbackCount = 0;
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        [Serializable]
        private sealed class Result
        {
            public bool passed;
            public string unity_version;
            public int shadow_clone_count, ring_particles, dot_particles, fragment_and_shadow_particles;
            public bool view_recycle_checked;
            public int prewarmed_match_views, initial_board_match_views;
            public bool pool_reuse_checked, global_particle_cache_checked, concrete_explosion_services_checked;
            public bool manual_merge_recycle_checked;
            public string additive_shader;
            public string scope;
        }
    }
}
