using System;
using Unity.Profiling;
using UnityEngine;

namespace Bizza.FlyMoney
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public sealed class FlyMoneyPlayer : MonoBehaviour
    {
        public const int HardConcurrencyLimit = 4;
        [Range(1, HardConcurrencyLimit)] public int concurrentLimit = HardConcurrencyLimit;
        public bool AutoAdvance { get; set; } = true;
        public bool Paused
        {
            get => paused;
            set { paused = value; SyncSoundPause(); }
        }
        public bool SoundEnabled
        {
            get => soundEnabled;
            set { soundEnabled = value; if (!value) StopSound(); }
        }
        public float SoundVolume
        {
            get => soundVolume;
            set
            {
                soundVolume = FlyMoneySettings.Finite(value) ? Mathf.Clamp01(value) : 0.65f;
                if (soundSource != null) soundSource.volume = soundVolume;
            }
        }
        public int ActiveCount { get; private set; }
        public int VisibleParticles { get; private set; }
        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public int FinishedCount { get; private set; }
        public float LatestProgress { get; private set; }
        public bool ApplicationPaused => applicationPaused;
        public int VertexCount
        {
            get
            {
                int count = 0;
                if (slots != null)
                    for (int i = 0; i < slots.Length; i++)
                        if (slots[i].active)
                            for (int j = 0; j < 3; j++) count += slots[i].graphics[j].VertexCount;
                return count;
            }
        }

        internal sealed class Slot
        {
            internal readonly FlyMoneySimulation simulation = new FlyMoneySimulation();
            internal readonly FlyMoneyFrame[] rainFrames = new FlyMoneyFrame[FlyMoneySettings.MaxParticlesPerLayer];
            internal readonly FlyMoneyFrame[] flyFrames = new FlyMoneyFrame[FlyMoneySettings.MaxParticlesPerLayer];
            internal readonly FlyMoneyGraphic[] graphics = new FlyMoneyGraphic[3];
            internal bool active;
            internal float elapsed;
            internal Transform target;
            internal bool watchTarget;
            internal Action<FlyMoneyEndReason> callback;
            internal Action onFirstArrival;
            internal int playbackId;
            internal int version;
        }

        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("FlyMoney.Update");
        private Slot[] slots;
        private readonly FlyMoneySprite[] spriteCache = new FlyMoneySprite[8];
        private int cacheCursor;
        private bool stopping;
        private bool applicationPaused;
        private bool skipResumeFrame;
        private bool paused;
        private bool soundEnabled = true;
        private bool soundPaused;
        private float soundVolume = 0.65f;
        private float nextSoundTime;
        private AudioSource soundSource;

        private void Awake() => Initialize();

        private void OnEnable()
        {
            applicationPaused = false;
            skipResumeFrame = true;
        }

        private void Initialize()
        {
            if (slots != null) return;
            slots = new Slot[HardConcurrencyLimit];
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i] = new Slot();
                for (int j = 0; j < 3; j++)
                {
                    GameObject child = new GameObject("Batch " + i + " - " + j, typeof(RectTransform), typeof(CanvasRenderer));
                    child.SetActive(false);
                    child.layer = gameObject.layer;
                    RectTransform rect = (RectTransform)child.transform;
                    rect.SetParent(transform, false);
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                    FlyMoneyGraphic graphic = child.AddComponent<FlyMoneyGraphic>();
                    graphic.slot = slot;
                    graphic.layer = j;
                    graphic.raycastTarget = false;
                    graphic.maskable = false;
                    slot.graphics[j] = graphic;
                }
            }
        }

        public bool PrepareSprite(Sprite sprite)
        {
            Initialize();
            return GetSprite(sprite) != null;
        }

        // Call during screen setup, not at the moment of reward. The shared clip is never unloaded here.
        public void PrepareSound(AudioClip clip)
        {
            StopSound();
            if (clip == null)
            {
                if (soundSource != null) soundSource.clip = null;
                return;
            }
            if (soundSource == null)
            {
                soundSource = gameObject.AddComponent<AudioSource>();
                soundSource.playOnAwake = false;
                soundSource.loop = false;
                soundSource.spatialBlend = 0f;
                soundSource.dopplerLevel = 0f;
                soundSource.volume = soundVolume;
            }
            soundSource.clip = clip;
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        }

        private void PlaySound()
        {
            if (!Application.isPlaying || !soundEnabled || Paused || applicationPaused || soundSource == null ||
                !soundSource.isActiveAndEnabled || soundSource.clip == null ||
                soundSource.clip.loadState != AudioDataLoadState.Loaded || soundSource.isPlaying || soundPaused ||
                Time.unscaledTime < nextSoundTime) return;
            nextSoundTime = Time.unscaledTime + 0.12f;
            soundSource.Play();
        }

        private void SyncSoundPause()
        {
            if (soundSource == null) return;
            if (Paused || applicationPaused)
            {
                if (!soundPaused && soundSource.isPlaying) { soundSource.Pause(); soundPaused = true; }
            }
            else if (soundPaused)
            {
                soundSource.UnPause();
                soundPaused = false;
            }
        }

        private void StopSound()
        {
            if (soundSource != null) soundSource.Stop();
            soundPaused = false;
        }

        private FlyMoneySprite GetSprite(Sprite sprite)
        {
            if (sprite == null) return null;
            for (int i = 0; i < spriteCache.Length; i++)
                if (spriteCache[i] != null && spriteCache[i].sprite == sprite) return spriteCache[i];
            FlyMoneySprite prepared = FlyMoneySprite.Prepare(sprite);
            if (prepared == null) return null;
            spriteCache[cacheCursor] = prepared;
            cacheCursor = (cacheCursor + 1) % spriteCache.Length;
            return prepared;
        }

        // Only accepted requests own a callback. A rejected request returns false without calling it.
        public bool TryPlay(Sprite rainSprite, Sprite flySprite, Vector2 origin, Vector2 destination,
            FlyMoneySettings settings, uint seed = 1, Action<FlyMoneyEndReason> onFinished = null,
            Transform targetToWatch = null, Action onFirstArrival = null, int playbackId = 0)
        {
            Initialize();
            if (!isActiveAndEnabled || stopping || applicationPaused || !FlyMoneySettings.Finite(origin) ||
                !FlyMoneySettings.Finite(destination) || origin.sqrMagnitude > 1e12f || destination.sqrMagnitude > 1e12f ||
                ActiveCount >= Mathf.Clamp(concurrentLimit, 1, HardConcurrencyLimit)) return Reject();
            settings = settings.Sanitized();
            if (settings.rainCount + settings.flyCount == 0) return Reject();
            FlyMoneySprite rain = settings.rainCount > 0 ? GetSprite(rainSprite) : null;
            FlyMoneySprite fly = settings.flyCount > 0 ? GetSprite(flySprite) : null;
            if ((settings.rainCount > 0 && rain == null) || (settings.flyCount > 0 && fly == null)) return Reject();
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                if (slot.active) continue;
                slot.simulation.Reset(settings, origin, destination, seed);
                slot.elapsed = 0f;
                slot.callback = onFinished;
                slot.onFirstArrival = onFirstArrival;
                slot.playbackId = playbackId;
                slot.version++;
                slot.target = targetToWatch;
                slot.watchTarget = targetToWatch != null;
                slot.graphics[0].geometry = rain;
                slot.graphics[1].geometry = null;
                slot.graphics[2].geometry = fly;
                slot.active = true;
                ActiveCount++;
                AcceptedCount++;
                LatestProgress = 0f;
                for (int j = 0; j < 3; j++)
                {
                    slot.graphics[j].gameObject.SetActive(true);
                    slot.graphics[j].SetMaterialDirty();
                }
                Sample(slot);
                PlaySound();
                return true;
            }
            return Reject();
        }

        public bool TryPlayFromTransforms(Sprite rain, Sprite fly, Transform origin, Transform target,
            FlyMoneySettings settings, uint seed = 1, Action<FlyMoneyEndReason> onFinished = null)
        {
            if (origin == null || target == null) return Reject();
            RectTransform rect = (RectTransform)transform;
            if (!TryConvert(origin, rect, out Vector2 from) || !TryConvert(target, rect, out Vector2 to)) return Reject();
            return TryPlay(rain, fly, from, to, settings, seed, onFinished, target);
        }

        private static bool TryConvert(Transform source, RectTransform destination, out Vector2 local)
        {
            Canvas fromCanvas = source.GetComponentInParent<Canvas>();
            Canvas toCanvas = destination.GetComponentInParent<Canvas>();
            Camera fromCamera = fromCanvas != null && fromCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? fromCanvas.worldCamera : null;
            Camera toCamera = toCanvas != null && toCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? toCanvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(fromCamera, source.position);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(destination, screen, toCamera, out local);
        }

        private bool Reject() { RejectedCount++; return false; }

        private void Update()
        {
            if (skipResumeFrame) { skipResumeFrame = false; return; }
            if (AutoAdvance) Advance(Time.unscaledDeltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (slots == null || !isActiveAndEnabled || ActiveCount == 0) return;
            Slot[] advancingSlots = slots;
            using (UpdateMarker.Auto())
            {
                VisibleParticles = 0;
                for (int i = 0; i < advancingSlots.Length; i++)
                {
                    Slot slot = advancingSlots[i];
                    if (!slot.active) continue;
                    if (slot.watchTarget && (slot.target == null || !slot.target.gameObject.activeInHierarchy))
                    { Finish(slot, FlyMoneyEndReason.TargetLost); continue; }
                    if (Paused || applicationPaused)
                    {
                        for (int j = 0; j < slot.simulation.Settings.rainCount; j++)
                            if (slot.rainFrames[j].Visible) VisibleParticles++;
                        for (int j = 0; j < slot.simulation.Settings.flyCount; j++)
                            if (slot.flyFrames[j].Visible) VisibleParticles++;
                        continue;
                    }
                    if (!FlyMoneySettings.Finite(deltaTime) || deltaTime < 0f) continue;
                    slot.elapsed += Mathf.Min(deltaTime, 0.1f);
                    LatestProgress = Mathf.Clamp01(slot.elapsed / slot.simulation.Settings.duration);
                    if (slot.elapsed >= slot.simulation.FirstArrivalTime && slot.onFirstArrival != null)
                    {
                        int version = slot.version;
                        Action arrival = slot.onFirstArrival;
                        slot.onFirstArrival = null;
                        try { arrival(); }
                        catch (Exception exception) { Debug.LogException(exception, this); }
                        if (!slot.active || slot.version != version) continue;
                    }
                    if (slot.elapsed >= slot.simulation.Settings.duration) { Finish(slot, FlyMoneyEndReason.Completed); continue; }
                    VisibleParticles += Sample(slot);
                }
            }
        }

        public void SeekNormalized(float progress)
        {
            if (slots == null || !FlyMoneySettings.Finite(progress)) return;
            Paused = true;
            StopSound();
            VisibleParticles = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].active) continue;
                slots[i].elapsed = Mathf.Clamp(progress, 0f, 0.9999f) * slots[i].simulation.Settings.duration;
                VisibleParticles += Sample(slots[i]);
            }
            LatestProgress = Mathf.Clamp01(progress);
        }

        private static int Sample(Slot slot)
        {
            int visible = 0;
            for (int i = 0; i < slot.simulation.Settings.rainCount; i++)
            {
                slot.rainFrames[i] = slot.simulation.SampleRain(i, slot.elapsed);
                if (slot.rainFrames[i].Visible) visible++;
            }
            for (int i = 0; i < slot.simulation.Settings.flyCount; i++)
            {
                slot.flyFrames[i] = slot.simulation.SampleFly(i, slot.elapsed);
                if (slot.flyFrames[i].Visible) visible++;
            }
            for (int i = 0; i < 3; i++)
                if (slot.graphics[i] != null) slot.graphics[i].SetVerticesDirty();
            return visible;
        }

        public void StopAll() => StopAll(FlyMoneyEndReason.Stopped);

        public void Cancel(int playbackId)
        {
            if (slots == null || playbackId == 0) return;
            Slot[] cancellingSlots = slots;
            for (int i = 0; i < cancellingSlots.Length; i++)
                if (cancellingSlots[i].active && cancellingSlots[i].playbackId == playbackId)
                    Finish(cancellingSlots[i], FlyMoneyEndReason.Stopped);
        }

        private void StopAll(FlyMoneyEndReason reason)
        {
            if (slots == null || stopping) return;
            Slot[] stoppingSlots = slots;
            stopping = true;
            try
            {
                StopSound();
                for (int i = 0; i < stoppingSlots.Length; i++) if (stoppingSlots[i].active) Finish(stoppingSlots[i], reason);
                VisibleParticles = 0;
                LatestProgress = 0f;
            }
            finally { stopping = false; }
        }

        private void Finish(Slot slot, FlyMoneyEndReason reason)
        {
            if (!slot.active) return;
            Action<FlyMoneyEndReason> callback = slot.callback;
            slot.callback = null;
            slot.onFirstArrival = null;
            slot.playbackId = 0;
            slot.target = null;
            slot.watchTarget = false;
            slot.active = false;
            ActiveCount--;
            FinishedCount++;
            if (ActiveCount == 0) StopSound();
            for (int j = 0; j < 3; j++)
            {
                try
                {
                    FlyMoneyGraphic graphic = slot.graphics[j];
                    if (graphic == null) continue;
                    graphic.geometry = null;
                    graphic.gameObject.SetActive(false);
                }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
            // Release the slot before user code runs; callbacks may start another animation.
            try { callback?.Invoke(reason); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            SyncSoundPause();
            if (!paused) skipResumeFrame = true;
        }

        private void OnDisable() => StopAll(FlyMoneyEndReason.Disabled);

        private void OnDestroy()
        {
            StopAll(FlyMoneyEndReason.Disabled);
            if (soundSource != null)
            {
                if (Application.isPlaying) Destroy(soundSource);
                else DestroyImmediate(soundSource);
                soundSource = null;
            }
            for (int i = 0; i < spriteCache.Length; i++) spriteCache[i] = null;
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++)
                for (int j = 0; j < 3; j++)
                {
                    FlyMoneyGraphic graphic = slots[i].graphics[j];
                    if (graphic == null) continue;
                    graphic.slot = null;
                    if (Application.isPlaying) Destroy(graphic.gameObject);
                    else DestroyImmediate(graphic.gameObject);
                }
            slots = null;
        }
    }
}
