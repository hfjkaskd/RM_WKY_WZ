using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit diagnostic only; all emission/follow/stop/recycle work is done
    // by the same runtime components and recovered prefabs as a device build.
    public static class SpecialCreationVerification
    {
        private static OriginalLevelPresentation presentation;
        private static OriginalSpecialItemCreationParticles front;
        private static OriginalSpecialItemCreationParticlesBack back;
        private static OriginalMatchItemSpriteView first, second;
        private static List<List<ParticleSystem.Particle>> beforeFirst, beforeSecond;
        private static int state, frame, available, ownedCount, copiedCount;
        private static float started, backStarted;
        private static Vector3 backBefore;
        private static readonly Vector3 move = new Vector3(.75f, -.25f, 0f);

        [MenuItem("Recovery/Verify Special Creation (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || presentation != null)
                throw new InvalidOperationException("Run this verification once in Play mode.");
            presentation = Object.FindObjectOfType<OriginalLevelPresentation>();
            try
            {
                Require(presentation != null && presentation.Pool.HasPool(5) && presentation.Pool.HasPool(6), "Configured front/back pools");
                Require(presentation.Pool.GetPoolLength(5) == 1 && presentation.Pool.GetPoolLength(6) == 0,
                    "Native one-front and zero-back prewarm");
                available = presentation.Pool.GetPoolLength(2);
                front = presentation.Particles.GetGlobalParticle<OriginalSpecialItemCreationParticles>(5);
                Require(front == presentation.Particles.GetGlobalParticle<OriginalSpecialItemCreationParticles>(5), "Shared front");
                Require(front.particleInfoLayers.Length == 1 && front.particleInfoLayers[0].particleInfos.Length == 4,
                    "Recovered four-system layer");
                first = presentation.SpawnMatchView(); second = presentation.SpawnMatchView();
                first.transform.position = new Vector3(-1f, 1f, 0f);
                second.transform.position = new Vector3(1f, 1f, 0f);
                front.PlayOptimized(first.transform.position, first.transform);
                front.PlayOptimized(second.transform.position, second.transform);
                beforeFirst = front.GetParticles(first.transform);
                beforeSecond = front.GetParticles(second.transform);
                ownedCount = Count(beforeFirst);
                Require(ownedCount > 0 && Count(beforeSecond) > 0, "Particles assigned to both masters");
                var seeds = new HashSet<uint>();
                CheckSeeds(beforeFirst, seeds); CheckSeeds(beforeSecond, seeds);
                var infos = front.particleInfoLayers[0].particleInfos;
                for (int i = 0; i < infos.Length; i++)
                {
                    Require(infos[i].particle.particleCount == beforeFirst[i].Count + beforeSecond[i].Count,
                        "All emitted particles attributed exactly once");
                    // Pause engine motion on these temporary effect instances
                    // to isolate the runtime master's movement from velocity.
                    infos[i].particle.Pause(false);
                }
                first.transform.position += move;
                state = 0; frame = Time.frameCount; started = Time.realtimeSinceStartup;
                EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }

        private static void Tick()
        {
            try { TickState(); }
            catch { Cleanup(); throw; }
        }

        private static void TickState()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Require(Time.realtimeSinceStartup - started < 12f, "Special creation verification timeout");
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            if (state == 0)
            {
                var afterFirst = front.GetParticles(first.transform);
                var afterSecond = front.GetParticles(second.transform);
                CheckPositions(beforeFirst, afterFirst, move);
                CheckPositions(beforeSecond, afterSecond, Vector3.zero);
                var transfer = new OriginalSpecialCreationParticleTransfer(first.transform, presentation.Pool);
                transfer.SetCreationParticles(front, false, false);
                Require(transfer.CreationParticles == front && presentation.Pool.GetPoolLength(6) == 0,
                    "Ordinary creation retains front ownership without a back spawn");
                transfer.SetCreationParticles(front, true, false);
                Require(transfer.CreationParticles == null, "Native snapshot transfer clears the source reference last");
                var backs = Object.FindObjectsOfType<OriginalSpecialItemCreationParticlesBack>();
                Require(backs.Length == 1, "One concrete back spawned by native transfer");
                back = backs[0];
                Require(back.particleSystems.Length == 4, "Recovered four back systems");
                transfer.DecreaseCreationParticleSorting();
                Require(presentation.Particles.ActiveCount == 2, "Repeated transfer is a no-op after reference clear");
                backStarted = Time.time;
                copiedCount = 0;
                for (int i = 0; i < 4; i++)
                {
                    Require(back.particleSystems[i].particleCount == afterFirst[i].Count, "Back copies selected master's particle snapshot");
                    copiedCount += back.particleSystems[i].particleCount;
                }
                Require(Count(front.GetParticles(first.transform)) == 0 && Count(front.GetParticles(second.transform)) == Count(beforeSecond),
                    "Selective clear leaves the other master's particles");
                backBefore = back.transform.position;
                first.transform.position += move;
                state = 1;
                return;
            }
            if (state == 1)
            {
                Require((back.transform.position - backBefore - move).sqrMagnitude < .00000001f, "Back follows target displacement");
                state = 2;
            }
            if (back.gameObject.activeSelf)
            {
                Require(Time.time < backStarted + 1.5f, "Typed delayed recycle executes");
                return;
            }
            Require(Time.time >= backStarted + 1f && back.transform.position == Vector3.zero
                && presentation.Pool.GetPoolLength(6) == 1, "One-second scaled-time recycle and reset");
            // Clear must cancel a pending call before the object is reused.
            var reused = presentation.Pool.Spawn<OriginalSpecialItemCreationParticlesBack>(6, true);
            Require(reused == back, "Back is reused from its native pool");
            reused.Init(second.transform);
            reused.PlayParticles(beforeSecond);
            presentation.Particles.Clear(false);
            Require(presentation.Pool.GetPoolLength(5) == 1 && presentation.Pool.GetPoolLength(6) == 1,
                "Manager clear recycles both layers");
            back = presentation.Pool.Spawn<OriginalSpecialItemCreationParticlesBack>(6, true);
            backStarted = Time.time;
            state = 3;
            EditorApplication.update -= Tick;
            EditorApplication.update += CheckCancelled;
        }

        private static void CheckCancelled()
        {
            try
            {
                if (!EditorApplication.isPlaying) { Cleanup(); return; }
                if (Time.time < backStarted + 1.1f) return;
                Require(back.gameObject.activeSelf && presentation.Pool.GetPoolLength(6) == 0,
                    "Cleared timer does not recycle a reused object");
                var result = new Result { passed = true, owned_particles = ownedCount, copied_particles = copiedCount,
                    shared_front = true, isolated_master_follow = true, selective_stop = true, back_follow = true,
                    scaled_recycle = true, cancel_before_reuse = true, creation_transfer = true,
                    scope = "Actual recovered four-system front/back prefabs, seed ownership for two masters, paused-engine follow isolation, SpecialItemView's deferred/above-spreading snapshot transfer and reference cleanup, selective clear, back displacement and one-second typed recycle/cancel/reuse. No native Invoke player-loop phase equivalence, concrete special-item factory/model animation, scene player input, refill or rendered-original parity is asserted." };
                string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "special_creation_unity_verification.json");
                File.WriteAllText(path, JsonUtility.ToJson(result, true) + "\n");
                Debug.Log("Special creation Unity verification passed: " + path);
                Cleanup();
            }
            catch { Cleanup(); throw; }
        }

        private static int Count(List<List<ParticleSystem.Particle>> layers)
        {
            int count = 0;
            for (int i = 0; i < layers.Count; i++) count += layers[i].Count;
            return count;
        }
        private static void CheckSeeds(List<List<ParticleSystem.Particle>> layers, HashSet<uint> seeds)
        {
            for (int i = 0; i < layers.Count; i++)
                for (int j = 0; j < layers[i].Count; j++) Require(seeds.Add(layers[i][j].randomSeed), "Globally unique particle ownership seeds");
        }
        private static void CheckPositions(List<List<ParticleSystem.Particle>> before, List<List<ParticleSystem.Particle>> after, Vector3 delta)
        {
            for (int i = 0; i < before.Count; i++)
            {
                Require(before[i].Count == after[i].Count, "Paused ownership count");
                for (int j = 0; j < before[i].Count; j++)
                    Require(before[i][j].randomSeed == after[i][j].randomSeed &&
                        (after[i][j].position - before[i][j].position - delta).sqrMagnitude < .00000001f, "Master-relative particle displacement");
            }
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick; EditorApplication.update -= CheckCancelled;
            if (presentation != null && presentation.Pool.IsPoolAlive())
            {
                if (front != null)
                {
                    var infos = front.particleInfoLayers[0].particleInfos;
                    for (int i = 0; i < infos.Length; i++) infos[i].particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                presentation.Particles.Clear(false);
                if (first != null && first.gameObject.activeSelf) presentation.RecycleView(first);
                if (second != null && second.gameObject.activeSelf) presentation.RecycleView(second);
                if (first != null && second != null) Require(presentation.Pool.GetPoolLength(2) == available, "Temporary match views returned");
            }
            presentation = null; front = null; back = null; first = null; second = null;
            beforeFirst = null; beforeSecond = null; state = 0;
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        [Serializable]
        private sealed class Result
        {
            public bool passed, shared_front, isolated_master_follow, selective_stop, back_follow, scaled_recycle, cancel_before_reuse, creation_transfer;
            public int owned_particles, copied_particles;
            public string scope;
        }
    }
}
