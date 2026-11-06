using System;
using System.IO;
using Bizza.FlyMoney.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bizza.FlyMoney.Editor
{
    public static class FlyMoneyLabEditor
    {
        public const string ScenePath = "Assets/FlyMoneyLab/Scenes/FlyMoneyLab.unity";
        private const string SpriteRoot = "Assets/BizzaWZ/Final/Real/GameAssets/WzTexture_Money/";

        [MenuItem("Tools/Fly Money Lab/Open Test Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlaying) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void CreateScene()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject root = new GameObject("Fly Money Lab");
            FlyMoneyLabScene lab = root.AddComponent<FlyMoneyLabScene>();
            lab.paperBR = Load("PieceMoney_BR"); lab.bundleBR = Load("StackMoney_BR");
            lab.paperID = Load("PieceMoney_ID"); lab.bundleID = Load("StackMoney_ID");
            lab.paperUS = Load("PieceMoney_US"); lab.bundleUS = Load("StackMoney_US");
            lab.burstSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BizzaWZ/Final/BizzaGame/Resources/Audios/SFX_DollarExplosion.mp3");
            lab.settings = FlyMoneySettings.Standard;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + name + ".png");

        private static void CheckInterruptionContracts(Action<bool, string> require)
        {
            Sprite sprite = Load("StackMoney_BR");
            var host = new GameObject("Reward Callback Checks", typeof(RectTransform), typeof(Canvas));
            var player = host.AddComponent<FlyMoneyPlayer>();
            player.AutoAdvance = false;
            int arrived = 0, ended = 0;
            try
            {
                player.TryPlay(sprite, sprite, Vector2.zero, Vector2.up * 400, FlyMoneySettings.Standard,
                    onFinished: reason => ended++, onFirstArrival: () => arrived++, playbackId: 17);
                for (int i = 0; i < 13; i++) player.Advance(0.1f);
                require(arrived == 1 && ended == 0, "first arrival is separate from visual completion");
                player.Cancel(17);
                player.Cancel(17);
                require(arrived == 1 && ended == 1 && player.ActiveCount == 0, "arrival then cancel finishes exactly once");
                player.TryPlay(sprite, sprite, Vector2.zero, Vector2.up * 400, FlyMoneySettings.Standard,
                    onFinished: reason => ended++, playbackId: 18);
                UnityEngine.Object.DestroyImmediate(host.transform.GetChild(0).gameObject);
                player.StopAll();
                require(ended == 2, "missing render child cannot prevent completion");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }

            host = new GameObject("Reward Reentrant Destroy", typeof(RectTransform), typeof(Canvas));
            player = host.AddComponent<FlyMoneyPlayer>();
            player.AutoAdvance = false;
            ended = 0;
            player.TryPlay(sprite, sprite, Vector2.zero, Vector2.up * 400, FlyMoneySettings.Standard,
                onFinished: reason => ended++, onFirstArrival: () =>
                {
                    // Edit-mode components do not receive the normal play-mode destruction lifecycle.
                    player.SendMessage("OnDisable");
                    player.SendMessage("OnDestroy");
                    UnityEngine.Object.DestroyImmediate(host);
                });
            player.TryPlay(sprite, sprite, Vector2.zero, Vector2.up * 400, FlyMoneySettings.Standard,
                onFinished: reason => ended++);
            for (int i = 0; i < 13 && player != null; i++) player.Advance(0.1f);
            require(host == null && ended == 2, "arrival callback may destroy the player with two active slots");
        }

        [MenuItem("Tools/Fly Money Lab/Run Self Checks")]
        public static void RunChecks()
        {
            int assertions = 0;
            Action<bool, string> require = (condition, message) =>
            {
                assertions++;
                if (!condition) throw new InvalidOperationException("FlyMoney check: " + message);
            };
            CheckInterruptionContracts(require);
            FlyMoneySettings bad = FlyMoneySettings.Standard;
            bad.rainCount = int.MaxValue; bad.flyCount = -10; bad.duration = float.NaN;
            bad.trailSegments = 1000; bad.trailWidth = float.PositiveInfinity;
            bad.fallSpeed = float.NaN;
            FlyMoneySettings sanitized = bad.Sanitized();
            require(sanitized.rainCount == 64 && sanitized.flyCount == 0 && sanitized.duration == 1.7f &&
                    sanitized.trailSegments == 8 && sanitized.trailWidth == 16f && sanitized.fallSpeed == 48f, "settings bounds");
            bad.fallSpeed = 999f;
            require(bad.Sanitized().fallSpeed == 120f, "fall speed upper bound");
            bad.fallSpeed = -20f;
            require(bad.Sanitized().fallSpeed == 0f, "fall speed lower bound");
            foreach (float size in new[] { 100f, 150f, 500f, 501f, -10f })
            {
                bad.rainSize = bad.flySize = size;
                FlyMoneySettings sizes = bad.Sanitized();
                require(sizes.rainSize == Mathf.Clamp(size, 10f, 500f) && sizes.flySize == sizes.rainSize,
                    "image size range is 10 to 500: " + size);
            }
            bad.rainSize = float.NaN; bad.flySize = float.PositiveInfinity;
            require(bad.Sanitized().rainSize == 50f && bad.Sanitized().flySize == 34f, "invalid image sizes retain defaults");
            foreach (string fieldName in new[] { nameof(FlyMoneySettings.rainSize), nameof(FlyMoneySettings.flySize) })
            {
                var range = (RangeAttribute)Attribute.GetCustomAttribute(typeof(FlyMoneySettings).GetField(fieldName), typeof(RangeAttribute));
                require(range != null && range.min == 10f && range.max == 500f, "Inspector size range matches runtime: " + fieldName);
            }
            FlyMoneySimulation a = new FlyMoneySimulation();
            FlyMoneySimulation b = new FlyMoneySimulation();
            require(!a.SampleBurst(0f).Visible, "uninitialized burst is empty");
            Vector2 origin = new Vector2(0, -75), target = new Vector2(-205, 455);
            foreach (FlyMoneySettings profile in new[] { FlyMoneySettings.Standard, FlyMoneySettings.Low })
            {
                FlyMoneySettings large = profile;
                large.rainSize = large.flySize = 500f;
                a.Reset(large, origin, target, 37);
                b.Reset(profile, origin, target, 37);
                require(a.Settings.rainSize == 500f && a.Settings.flySize == 500f, "both quality profiles accept 500");
                require(a.SampleRain(0, 0.12f).size > b.SampleRain(0, 0.12f).size &&
                    a.SampleFly(0, 0.55f).size > b.SampleFly(0, 0.55f).size, "large settings reach particle samples");
                require(a.SampleRain(0, 0.12f).position == b.SampleRain(0, 0.12f).position &&
                    a.SampleFly(0, 0.95f).position == b.SampleFly(0, 0.95f).position, "size changes preserve trajectories");
            }
            a.Reset(FlyMoneySettings.Standard, origin, target, 37);
            b.Reset(FlyMoneySettings.Standard, origin, target, 37);
            for (int frame = 0; frame < 120; frame++)
            {
                float time = frame / 60f;
                for (int i = 0; i < 28; i++)
                {
                    FlyMoneyFrame sample = a.SampleFly(i, time);
                    require(sample.position == b.SampleFly(i, time).position, "seed reproducibility");
                    require(!float.IsNaN(sample.position.x) && !float.IsInfinity(sample.position.y) &&
                            sample.alpha >= 0 && sample.alpha <= 1f, "finite fly samples");
                }
            }
            require(a.SampleRain(0, 1f).alpha == 0f, "rain ends before collection ends");
            require(!a.SampleBurst(0f).Visible && a.SampleBurst(0.06f).Visible && !a.SampleBurst(0.36f).Visible,
                "opening accent is short and bounded");
            require(!a.SampleBurst(float.NaN).Visible && !a.SampleBurst(-1f).Visible, "invalid burst time");
            require(a.SampleBurst(0.06f).coreAlpha > 0.9f && a.SampleBurst(0.20f).coreAlpha == 0f &&
                a.SampleBurst(0.20f).cloudAlpha > 0f && !a.SampleBurst(0.34f).Visible, "cloud pops then separates and dissolves");
            for (int i = 0; i <= 36; i++)
            {
                FlyMoneyBurstFrame burst = a.SampleBurst(i * 0.01f);
                require(burst.radius <= 102.01f * burst.scale && burst.cloudSize <= 52f * burst.scale &&
                    burst.cloudAlpha >= 0f && burst.cloudAlpha <= 1f && burst.sparkleAlpha >= 0f &&
                    burst.sparkleAlpha <= 1f, "compact cloud and accent bounds");
            }
            int outward = 0;
            for (int i = 0; i < 32; i++)
            {
                FlyMoneyFrame early = a.SampleRain(i, 0.025f), expanded = a.SampleRain(i, 0.12f);
                if ((expanded.position - origin).sqrMagnitude > (early.position - origin).sqrMagnitude) outward++;
                require(expanded.position == b.SampleRain(i, 0.12f).position && expanded.alpha <= 1f,
                    "deterministic opening impulse");
            }
            require(outward >= 28, "paper bursts outward before falling");
            require(a.SampleFly(0, 0.55f).Visible && !a.SampleFly(0, 0.55f).flying, "falling before collection");
            CheckCollectionMotion(require, FlyMoneySettings.Standard, 37);
            CheckCollectionMotion(require, FlyMoneySettings.Low, 7);
            FlyMoneySettings edgeTiming = FlyMoneySettings.Standard;
            edgeTiming.flyCount = 1; edgeTiming.duration = 0.5f; edgeTiming.fallSpeed = 120f;
            CheckCollectionMotion(require, edgeTiming, 0);
            edgeTiming.flyCount = 64; edgeTiming.duration = 5f;
            CheckCollectionMotion(require, edgeTiming, 65535);
            edgeTiming.flyCount = 1; edgeTiming.duration = 1.7f; edgeTiming.fallSpeed = 0f;
            CheckCollectionMotion(require, edgeTiming, 37);
            for (int i = 0; i < 28; i++) require(!a.SampleFly(i, 1.7f).Visible, "all particles end on time");
            for (int i = 0; i < 200; i++) a.SampleFly(i % 28, 0.9f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20000; i++)
            {
                a.SampleFly(i % 28, (i % 100) * 0.017f);
                a.SampleRain(i % 32, (i % 100) * 0.017f);
                a.SampleBurst((i % 100) * 0.017f);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            require(allocated == 0, "simulation sampling must allocate 0 bytes");

            GameObject host = new GameObject("Fly Money Validation", typeof(RectTransform), typeof(Canvas));
            FlyMoneyPlayer player = host.AddComponent<FlyMoneyPlayer>();
            player.AutoAdvance = false;
            int callbacks = 0;
            Action<FlyMoneyEndReason> callback = reason => callbacks++;
            try
            {
                Sprite paper = Load("PieceMoney_BR"), bundle = Load("StackMoney_BR");
                require(paper != null && bundle != null, "scene sprite references");
                require(player.PrepareSprite(paper) && player.PrepareSprite(bundle), "sprite mesh and atlas UV support");
                AudioClip sound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/BizzaWZ/Final/BizzaGame/Resources/Audios/SFX_DollarExplosion.mp3");
                require(sound != null, "opening sound reference");
                player.PrepareSound(sound); player.PrepareSound(sound);
                AudioSource source = host.GetComponent<AudioSource>();
                require(host.GetComponents<AudioSource>().Length == 1 && source.clip == sound && !source.playOnAwake &&
                    !source.loop && source.spatialBlend == 0f, "one reusable 2D sound source");
                player.SoundVolume = float.NaN;
                require(player.SoundVolume == 0.65f, "invalid volume fallback");
                player.SoundVolume = 10f;
                require(player.SoundVolume == 1f && source.volume == 1f, "volume cap");
                player.SoundVolume = 0.65f;
                FlyMoneySettings cloudOnly = FlyMoneySettings.Standard;
                cloudOnly.rainCount = 0; cloudOnly.flyCount = 1; cloudOnly.trails = false;
                require(player.TryPlay(null, bundle, origin, target, cloudOnly), "cloud does not require paper or trails");
                player.SeekNormalized(0.06f / 1.7f);
                Canvas.ForceUpdateCanvases();
                require(player.VertexCount > 0 && player.VertexCount <= 506, "bounded cloud mesh");
                player.StopAll(); player.Paused = false;
                require(player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard, 1, callback), "first play");
                require(player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard, 2, callback), "second play");
                require(player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard, 3, callback), "third play");
                require(player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Low, 4, callback), "fourth play, low profile");
                require(!player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard, 5, callback), "concurrency limit");
                player.Advance(0.1f); player.Paused = true;
                float progress = player.LatestProgress;
                player.Advance(0.1f);
                require(player.LatestProgress == progress, "pause holds time");
                player.Paused = false;
                for (int i = 0; i < 40; i++) player.Advance(0.05f);
                require(player.ActiveCount == 0 && callbacks == 4, "completion once per accepted play");
                player.StopAll(); require(callbacks == 4, "double stop cannot complete twice");
                require(!player.TryPlay(null, bundle, origin, target, FlyMoneySettings.Standard, 1, callback), "missing sprite");
                require(!player.TryPlay(paper, bundle, new Vector2(float.NaN, 0), target, FlyMoneySettings.Standard), "invalid position");
                GameObject disposable = new GameObject("Disposable Target");
                FlyMoneyEndReason result = FlyMoneyEndReason.Completed;
                player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard, 1, reason => result = reason, disposable.transform);
                UnityEngine.Object.DestroyImmediate(disposable);
                player.Advance(0.01f);
                require(player.ActiveCount == 0 && result == FlyMoneyEndReason.TargetLost, "destroyed target cancellation");
                bool restarted = false;
                player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard, 1, reason =>
                    restarted = player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard));
                for (int i = 0; i < 35; i++) player.Advance(0.05f);
                require(restarted && player.ActiveCount == 1, "callback reentrancy");
                player.StopAll();
                player.concurrentLimit = 1;
                for (int i = 0; i < 1000; i++)
                {
                    require(player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Low, (uint)i, callback), "pooled restart");
                    player.SeekNormalized(0.65f); player.StopAll();
                }
                require(callbacks == 1004 && player.ActiveCount == 0 && host.transform.childCount == 3 * FlyMoneyPlayer.HardConcurrencyLimit, "bounded objects and callback count");
                require(player.LatestProgress == 0f, "clear resets progress");
                player.Paused = false;
                for (int i = 0; i < 8; i++)
                {
                    player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard);
                    player.Advance(0.05f); player.StopAll();
                }
                before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++)
                {
                    player.TryPlay(paper, bundle, origin, target, FlyMoneySettings.Standard);
                    player.Advance(0.05f); player.StopAll();
                }
                long playbackAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
                require(playbackAllocated == 0, "warmed playback lifecycle must allocate 0 bytes");
                string json = "{\"passed\":true,\"assertions\":" + assertions + ",\"simulationAllocatedBytes\":" + allocated +
                              ",\"playbackLifecycleAllocatedBytes\":" + playbackAllocated + ",\"stressRuns\":1000}";
                Directory.CreateDirectory("Library/FlyMoneyLab");
                File.WriteAllText("Library/FlyMoneyLab/checks.json", json);
                Debug.Log("FLY_MONEY_CHECKS " + json);
            }
            finally { player.StopAll(); UnityEngine.Object.DestroyImmediate(host); }
        }

        public static void BatchValidate()
        {
            try { RunChecks(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void CheckCollectionMotion(Action<bool, string> require, FlyMoneySettings settings, uint seed)
        {
            FlyMoneySimulation simulation = new FlyMoneySimulation();
            Vector2 origin = new Vector2(0, -75), target = new Vector2(-205, 455);
            simulation.Reset(settings, origin, target, seed);
            float clockScale = settings.duration / 1.7f;
            float spatialScale = Vector2.Distance(origin, target) / 530f;
            float firstLaunch = 0f;
            for (int i = 0; i < settings.flyCount; i++)
            {
                float high = 0.65f;
                while (high < 1.25f && !simulation.SampleFly(i, high * clockScale).flying) high += 0.001f;
                require(high < 1.25f, "launch remains inside original timeline");
                float low = high - 0.001f;
                for (int j = 0; j < 12; j++)
                {
                    float middle = (low + high) * 0.5f;
                    if (simulation.SampleFly(i, middle * clockScale).flying) high = middle;
                    else low = middle;
                }
                float launch = high;
                if (i == 0) firstLaunch = launch;
                FlyMoneyFrame previous = simulation.SampleFly(i, 0.52f * clockScale);
                float previousDrop = -1f;
                float minX = previous.position.x, maxX = minX;
                float minAngle = previous.angle, maxAngle = minAngle;
                for (float time = 0.54f; time < launch; time += 0.02f)
                {
                    FlyMoneyFrame falling = simulation.SampleFly(i, time * clockScale);
                    float drop = previous.position.y - falling.position.y;
                    require(previous.Visible && falling.Visible && !falling.flying &&
                        Mathf.Abs(previous.position.x - falling.position.x) < 1.5f * spatialScale &&
                        Mathf.Abs(previous.angle - falling.angle) < 0.75f, "bounded drifting and rolling during fall");
                    if (settings.fallSpeed > 0f)
                        require(drop > 0f && (previousDrop < 0f || drop > previousDrop + 0.0001f * spatialScale),
                            "fall accelerates gently without stopping");
                    else require(Vector2.Distance(previous.position, falling.position) < 0.002f &&
                        Mathf.Abs(previous.angle - falling.angle) < 0.002f, "zero fall speed disables drift and roll");
                    minX = Mathf.Min(minX, falling.position.x); maxX = Mathf.Max(maxX, falling.position.x);
                    minAngle = Mathf.Min(minAngle, falling.angle); maxAngle = Mathf.Max(maxAngle, falling.angle);
                    previousDrop = drop;
                    previous = falling;
                }
                if (settings.fallSpeed > 0f)
                    require(maxX - minX > 0.001f * spatialScale && maxX - minX < 22f * spatialScale &&
                        maxAngle - minAngle > 0.001f && maxAngle - minAngle < 9f,
                        "fall has subtle horizontal motion and rotation");
                FlyMoneyFrame departure = simulation.SampleFly(i, (launch - 0.0001f) * clockScale);
                FlyMoneyFrame flight = simulation.SampleFly(i, (launch + 0.0001f) * clockScale);
                require(!departure.flying && flight.flying && Vector2.Distance(departure.position, flight.position) < 0.05f * spatialScale &&
                    Mathf.Abs(departure.size - flight.size) < 0.01f && Mathf.Abs(departure.angle - flight.angle) < 0.01f,
                    "fall joins flight without a position size or rotation jump");
                Vector2 atLaunch = simulation.SampleFly(i, launch * clockScale).position;
                Vector2 incoming = (atLaunch - simulation.SampleFly(i, (launch - 0.0005f) * clockScale).position) / 0.0005f;
                Vector2 outgoing = (simulation.SampleFly(i, (launch + 0.0005f) * clockScale).position - atLaunch) / 0.0005f;
                require(Vector2.Distance(incoming, outgoing) < 3f * spatialScale &&
                    (settings.fallSpeed == 0f || outgoing.y < 0f), "fall velocity continues through the turn");
                float launchAngle = simulation.SampleFly(i, launch * clockScale).angle;
                float incomingSpin = (launchAngle - simulation.SampleFly(i, (launch - 0.0005f) * clockScale).angle) / 0.0005f;
                float outgoingSpin = (simulation.SampleFly(i, (launch + 0.0005f) * clockScale).angle - launchAngle) / 0.0005f;
                require(Mathf.Abs(incomingSpin - outgoingSpin) < 1f, "angular velocity continues through the turn");
                FlyMoneyFrame early = simulation.SampleFly(i, (launch + 0.02f) * clockScale);
                FlyMoneyFrame later = simulation.SampleFly(i, (launch + 0.22f) * clockScale);
                require(later.position.y > departure.position.y && Vector2.Distance(later.position, departure.position) >
                    Vector2.Distance(early.position, departure.position) * 4f, "accelerate upward after falling");
                require(!simulation.SampleFly(i, settings.duration).Visible, "fall does not extend total duration");
                if (i == settings.flyCount - 1 && i > 0)
                    require(launch - firstLaunch > 0.45f, "staggered collection span retained");
            }
        }
    }
}
