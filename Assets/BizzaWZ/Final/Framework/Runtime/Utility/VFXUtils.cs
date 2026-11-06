#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public static class VFXUtils
{

    public static Dictionary<E_ItemType, Transform> itemFlyTarget = new();

    public static GameObject PlayOnce(string name, Vector3 pos, float duration = 2f, Transform parent = null)
    {
        var prefab = AssetUtils.LoadAssetSync<GameObject>("Prefabs/VFX/" + name);
        if (prefab == null)
        {
            return null;
        }

        var inst = PoolUtil.GetGameObject(prefab);
        if (parent != null)
        {
            inst.transform.SetParent(parent);
        }
        inst.SetActive(true);
        inst.transform.position = pos;
        inst.transform.localScale = Vector3.one;
        GameUtils.DelayDo(() =>
        {
            if (inst != null)
            {
                PoolUtil.ReleaseGameObject(inst);
            }
        }, duration);
        return inst;
    }


    public static GameObject PlayFollow(string name, Transform parent, Vector3 localPos = default)
    {
        var prefab = AssetUtils.LoadAssetSync<GameObject>("Prefabs/VFX/" + name);
        if (prefab == null)
        {
            return null;
        }

        var inst = PoolUtil.GetGameObject(prefab);
        inst.transform.SetParent(parent);
        inst.SetActive(true);
        inst.transform.localPosition = localPos;
        inst.transform.localScale = Vector3.one;
        return inst;
    }

    private static RewardItemCollectFxConfig ItemCollectFxConfig => RewardItemCollectFxConfig.Instance;
    private static int MaxConcurrentItemCollectFx => ItemCollectFxConfig.MaxConcurrentFx;
    private static int MaxItemCollectFxStartsPerSecond => ItemCollectFxConfig.MaxStartsPerSecond;
    private static float ItemCollectFxStartWindowSeconds => ItemCollectFxConfig.StartWindowSeconds;
    private static int ItemCollectFxIconCount => ItemCollectFxConfig.IconCount;
    private static int ItemCollectFxHeroCount => ItemCollectFxConfig.HeroCount;
    private static int ItemCollectFxTrailCount => ItemCollectFxConfig.TrailCount;
    private static float ItemCollectFxIconSize => ItemCollectFxConfig.IconSize;
    private static float ItemCollectFxStartScale => ItemCollectFxConfig.StartScale;
    private static float ItemCollectFxPopScale => ItemCollectFxConfig.PopScale;
    private static float ItemCollectFxEndScale => ItemCollectFxConfig.EndScale;
    private static float ItemCollectFxEndAlpha => ItemCollectFxConfig.EndAlpha;
    private static float ItemCollectFxFadeStart => ItemCollectFxConfig.FadeStart;
    private static float ItemCollectFxScatterRadiusMin => ItemCollectFxConfig.ScatterRadiusMin;
    private static float ItemCollectFxScatterRadiusMax => ItemCollectFxConfig.ScatterRadiusMax;
    private static float ItemCollectFxScatterDuration => ItemCollectFxConfig.ScatterDuration;
    private static float ItemCollectFxScatterKick => ItemCollectFxConfig.ScatterKick;
    private static float ItemCollectFxScatterScalePunch => ItemCollectFxConfig.ScatterScalePunch;
    private static float ItemCollectFxLaunchClusterRadius => ItemCollectFxConfig.LaunchClusterRadius;
    private static float ItemCollectFxLaunchClusterRatio => ItemCollectFxConfig.LaunchClusterRatio;
    private static Vector2 ItemCollectFxScatterStartOffset => ItemCollectFxConfig.ScatterStartOffset;
    private static float ItemCollectFxScatterFanSideScale => ItemCollectFxConfig.ScatterFanSideScale;
    private static float ItemCollectFxScatterFanForwardScale => ItemCollectFxConfig.ScatterFanForwardScale;
    private static float ItemCollectFxScatterClusterJitter => ItemCollectFxConfig.ScatterClusterJitter;
    private static float ItemCollectFxHoverDuration => ItemCollectFxConfig.HoverDuration;
    private static float ItemCollectFxHoverExtraDelayMax => ItemCollectFxConfig.HoverExtraDelayMax;
    private static float ItemCollectFxHoverAmplitude => ItemCollectFxConfig.HoverAmplitude;
    private static float ItemCollectFxHoverSideAmplitude => ItemCollectFxConfig.HoverSideAmplitude;
    private static float ItemCollectFxHoverShakeCycles => ItemCollectFxConfig.HoverShakeCycles;
    private static float ItemCollectFxHoverAttract => ItemCollectFxConfig.HoverAttract;
    private static float ItemCollectFxStartDelayMax => ItemCollectFxConfig.StartDelayMax;
    private static float ItemCollectFxFlyDuration => ItemCollectFxConfig.FlyDuration;
    private static float ItemCollectFxFlyArcHeight => ItemCollectFxConfig.FlyArcHeight;
    private static float ItemCollectFxFlyArcSide => ItemCollectFxConfig.FlyArcSide;
    private static float ItemCollectFxFlyMidScaleAdd => ItemCollectFxConfig.FlyMidScaleAdd;
    private static float ItemCollectFxRotationMaxSpeed => ItemCollectFxConfig.RotationMaxSpeed;
    private static float ItemCollectFxTrailInterval => ItemCollectFxConfig.TrailInterval;
    private static float ItemCollectFxTrailAlpha => ItemCollectFxConfig.TrailAlpha;
    private static float ItemCollectFxTrailShowStart => ItemCollectFxConfig.TrailShowStart;
    private static float ItemCollectFxBurstDuration => ItemCollectFxConfig.BurstDuration;
    private static float ItemCollectFxBurstStartScale => ItemCollectFxConfig.BurstStartScale;
    private static float ItemCollectFxBurstEndScale => ItemCollectFxConfig.BurstEndScale;
    private static float ItemCollectFxBurstAlpha => ItemCollectFxConfig.BurstAlpha;
    private static readonly Queue<float> itemCollectFxStartTimes = new();
    private static readonly Vector2[] itemCollectFxScatterClusterAnchors =
    {
        new Vector2(-0.72f, 0.78f),
        new Vector2(0.64f, 0.88f),
        new Vector2(-0.16f, 1.08f),
        new Vector2(0.28f, 0.58f),
        new Vector2(-0.48f, 0.5f),
    };

    private static int activeItemCollectFxCount;
    private static int activeFreeItemCollectFxCount;
    private static GameObject itemCollectFxRootTemplate;
    private static GameObject itemCollectFxIconTemplate;

    private struct ItemCollectFxFrameState
    {
        public bool Visible;
        public bool IsFlying;
        public Vector2 Position;
        public float Scale;
        public float Rotation;
        public float Alpha;
    }

    public static void PlayItemCollectFx(E_ItemType itemType, Vector3 startPos, Action onComplete, bool setIcon = false,
        bool bUiPos = false, Transform target = null, RewardCollectAnimation animation = RewardCollectAnimation.Legacy,
        float delay = 0f)
    {
        RewardItemCollectFlow.Play(itemType, startPos, onComplete, bUiPos, target, animation, delay);
    }

    internal static async UniTask PlayLegacyItemCollectFx(RewardItemCollectFlow.Request request)
    {
        E_ItemType itemType = request.itemType;
        if (ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode && itemType == E_ItemType.Gold)
            itemType = E_ItemType.Dollar;
        string prefabName = "VFX_Get" + itemType;
        Vector3 startPos = request.position;
        bool bUiPos = request.uiPosition;
        Transform target = request.target;
        bool fxStarted = false;
        GameObject root = null;
        Image burstImage = null;
        Image[] icons = null;
        Image[][] trailImages = null;
        RectTransform burstRect = null;
        RectTransform[] iconRects = null;
        RectTransform[][] trailRects = null;
        bool spawnFeedbackPlayed = false;
        bool flyFeedbackPlayed = false;
        Vector2[] scatterPositions = null;
        Vector2[] flyStartPositions = null;
        Vector2[] flyControlPositions = null;
        float[] delays = null;
        float[] scatterDurations = null;
        float[] flyDurations = null;
        float[] startRotations = null;
        float[] rotationSpeeds = null;
        float[] popScales = null;
        float[] hoverPhases = null;
        float[] hoverAmplitudes = null;
        float[] hoverDurations = null;

        void CompleteReward(bool playSound)
        {
            request.Notify(playSound);
        }

        try
        {
            if (target == null)
            {
                TrackItemCollectFx(itemType, prefabName, true, 0);
                CompleteReward(false);
                return;
            }

            RectTransform fxParent = GetItemCollectFxParent();
            Sprite sprite = ResolveItemCollectSprite(itemType, target);
            if (fxParent == null || sprite == null)
            {
                TrackItemCollectFx(itemType, prefabName, true, 0);
                CompleteReward(false);
                return;
            }

            Canvas canvas = fxParent.GetComponentInParent<Canvas>();
            if (!TryGetAnchoredPosition(target.position, fxParent, canvas, out Vector2 targetAnchoredPosition))
            {
                TrackItemCollectFx(itemType, prefabName, true, 0);
                CompleteReward(false);
                return;
            }

            root = CreateItemCollectFxRoot(itemType, fxParent, startPos, bUiPos, canvas);
            request.legacyRoot = root;
            if (root == null)
            {
                TrackItemCollectFx(itemType, prefabName, true, 0);
                CompleteReward(false);
                return;
            }

            RectTransform rootRect = root.transform as RectTransform;
            if (rootRect == null)
            {
                TrackItemCollectFx(itemType, prefabName, true, 0);
                CompleteReward(false);
                return;
            }

            Vector2 targetLocalPosition = targetAnchoredPosition - rootRect.anchoredPosition;
            burstImage = CreateItemCollectIcon(rootRect, sprite, "ItemCollectBurst");
            burstRect = burstImage != null ? burstImage.transform as RectTransform : null;
            if (burstImage != null)
            {
                burstImage.transform.SetAsFirstSibling();
            }

            icons = new Image[ItemCollectFxIconCount];
            trailImages = new Image[ItemCollectFxIconCount][];
            iconRects = new RectTransform[ItemCollectFxIconCount];
            trailRects = new RectTransform[ItemCollectFxIconCount][];
            scatterPositions = new Vector2[ItemCollectFxIconCount];
            flyStartPositions = new Vector2[ItemCollectFxIconCount];
            flyControlPositions = new Vector2[ItemCollectFxIconCount];
            delays = new float[ItemCollectFxIconCount];
            scatterDurations = new float[ItemCollectFxIconCount];
            flyDurations = new float[ItemCollectFxIconCount];
            startRotations = new float[ItemCollectFxIconCount];
            rotationSpeeds = new float[ItemCollectFxIconCount];
            popScales = new float[ItemCollectFxIconCount];
            hoverPhases = new float[ItemCollectFxIconCount];
            hoverAmplitudes = new float[ItemCollectFxIconCount];
            hoverDurations = new float[ItemCollectFxIconCount];

            int createdIconCount = 0;
            float totalDuration = 0f;
            float rewardCompleteTime = float.MaxValue;
            float flyFeedbackTime = float.MaxValue;
            for (int i = 0; i < ItemCollectFxIconCount; i++)
            {
                trailImages[i] = new Image[ItemCollectFxTrailCount];
                trailRects[i] = new RectTransform[ItemCollectFxTrailCount];
                for (int j = 0; j < ItemCollectFxTrailCount; j++)
                {
                    trailImages[i][j] = CreateItemCollectIcon(rootRect, sprite, "ItemCollectTrail");
                    trailRects[i][j] = trailImages[i][j] != null ? trailImages[i][j].transform as RectTransform : null;
                    if (trailImages[i][j] != null)
                    {
                        trailImages[i][j].enabled = false;
                        trailImages[i][j].transform.SetAsFirstSibling();
                    }
                }

                icons[i] = CreateItemCollectIcon(rootRect, sprite);
                iconRects[i] = icons[i] != null ? icons[i].transform as RectTransform : null;
                bool isHero = i < ItemCollectFxHeroCount;
                scatterPositions[i] = GetScatterPosition(i, isHero, targetLocalPosition);
                delays[i] = isHero
                    ? UnityEngine.Random.Range(0f, ItemCollectFxStartDelayMax * 0.35f)
                    : UnityEngine.Random.Range(0f, ItemCollectFxStartDelayMax);
                scatterDurations[i] = ItemCollectFxScatterDuration *
                                      (isHero ? UnityEngine.Random.Range(0.72f, 0.92f) : UnityEngine.Random.Range(0.88f, 1.04f));
                flyDurations[i] = ItemCollectFxFlyDuration *
                                  (isHero ? UnityEngine.Random.Range(0.86f, 1f) : UnityEngine.Random.Range(0.94f, 1.08f));
                hoverDurations[i] = ItemCollectFxHoverDuration + UnityEngine.Random.Range(0f, ItemCollectFxHoverExtraDelayMax) +
                                    (isHero ? UnityEngine.Random.Range(-0.04f, 0.02f) : UnityEngine.Random.Range(0f, 0.025f));
                hoverPhases[i] = UnityEngine.Random.Range(0f, 1f);
                hoverAmplitudes[i] = ItemCollectFxHoverAmplitude *
                                     (isHero ? UnityEngine.Random.Range(0.9f, 1.22f) : UnityEngine.Random.Range(0.7f, 1.05f));
                flyStartPositions[i] = GetHoverFramePosition(
                    scatterPositions[i],
                    targetLocalPosition,
                    hoverDurations[i],
                    hoverDurations[i],
                    hoverPhases[i],
                    hoverAmplitudes[i]);
                flyControlPositions[i] = GetFlyControlPosition(flyStartPositions[i], targetLocalPosition);
                startRotations[i] = UnityEngine.Random.Range(-38f, 38f);
                rotationSpeeds[i] = UnityEngine.Random.Range(
                    -ItemCollectFxRotationMaxSpeed * (isHero ? 1.18f : 0.72f),
                    ItemCollectFxRotationMaxSpeed * (isHero ? 1.18f : 0.72f));
                popScales[i] = ItemCollectFxPopScale *
                               (isHero ? UnityEngine.Random.Range(1.18f, 1.36f) : UnityEngine.Random.Range(0.62f, 0.88f));

                if (icons[i] != null)
                {
                    createdIconCount++;
                    float iconFlyStartTime = delays[i] + scatterDurations[i] + hoverDurations[i];
                    float iconEndTime = delays[i] + scatterDurations[i] + hoverDurations[i] + flyDurations[i];
                    totalDuration = Mathf.Max(totalDuration, iconEndTime);
                    flyFeedbackTime = Mathf.Min(flyFeedbackTime, iconFlyStartTime);
                    rewardCompleteTime = Mathf.Min(rewardCompleteTime, iconEndTime);
                }
            }

            if (createdIconCount == 0)
            {
                TrackItemCollectFx(itemType, prefabName, true, 0);
                CompleteReward(false);
                return;
            }

            fxStarted = true;
            TrackItemCollectFx(itemType, prefabName, false, createdIconCount);
            PlayDollarExplosionSound();
            spawnFeedbackPlayed = true;

            float elapsed = 0f;

            while (elapsed < totalDuration && root != null && request.IsValid)
            {
                UpdateItemCollectBurstFrame(burstImage, burstRect, elapsed);
                UpdateItemCollectIconFrames(
                    icons,
                    trailImages,
                    iconRects,
                    trailRects,
                    scatterPositions,
                    flyStartPositions,
                    flyControlPositions,
                    delays,
                    scatterDurations,
                    flyDurations,
                    startRotations,
                    rotationSpeeds,
                    popScales,
                    hoverPhases,
                    hoverAmplitudes,
                    hoverDurations,
                    targetLocalPosition,
                    elapsed);

                if (!flyFeedbackPlayed && elapsed >= flyFeedbackTime)
                {
                    PlayDollarExplosionSound();
                    flyFeedbackPlayed = true;
                }

                if (elapsed >= rewardCompleteTime)
                {
                    CompleteReward(true);
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed = request.elapsed;
            }

            if (!request.IsValid) return;
            if (root != null)
            {
                if (!spawnFeedbackPlayed)
                {
                    PlayDollarExplosionSound();
                }

                if (!flyFeedbackPlayed)
                {
                    PlayDollarExplosionSound();
                }

                UpdateItemCollectBurstFrame(burstImage, burstRect, totalDuration);
                UpdateItemCollectIconFrames(
                    icons,
                    trailImages,
                    iconRects,
                    trailRects,
                    scatterPositions,
                    flyStartPositions,
                    flyControlPositions,
                    delays,
                    scatterDurations,
                    flyDurations,
                    startRotations,
                    rotationSpeeds,
                    popScales,
                    hoverPhases,
                    hoverAmplitudes,
                    hoverDurations,
                    targetLocalPosition,
                    totalDuration);
            }

            CompleteReward(true);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            CompleteReward(fxStarted);
        }
        finally
        {
            try
            {
                ReleaseItemCollectIcon(burstImage);
                ReleaseItemCollectTrailImages(trailImages);
                ReleaseItemCollectIcons(icons);
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                request.legacyRoot = null;
                try { if (root != null) ReleaseItemCollectFxRoot(root); }
                catch (Exception exception) { Debug.LogException(exception); }
                finally { request.End(false); }
            }
        }
    }

    public static void PlayGetCoinSound(E_ItemType itemType)
    {
        var isGold = itemType == E_ItemType.Gold;
        var sound = isGold ? "SFX_GetGold" : "SFX_GetDollar";
        SoundManager.Instance.PlaySFX(sound);
    }

    private static void PlayDollarExplosionSound()
    {
        SoundManager.Instance.PlaySFX("SFX_DollarExplosion");
    }

    internal static void PlayItemCollectArriveFeedback()
    {
        VibrationUtils.VibrateStableClick(E_VibrateType.Light);
        SoundManager.Instance.PlaySFX("SFX_GetDollar");
    }

    private static RectTransform GetItemCollectFxParent()
    {
        UIModule uiModule = UIModule.Instance;
        if (uiModule == null)
        {
            return null;
        }

        RectTransform parent = uiModule.RewardItemLayer;
        if (parent != null)
        {
            return parent;
        }

        return uiModule.UICanvas.transform as RectTransform;
    }

    internal static Sprite ResolveItemCollectSprite(E_ItemType itemType, Transform target)
    {
        Sprite sprite = ItemUtils.GetItemIcon(itemType);
        if (sprite != null)
        {
            return sprite;
        }

        Image targetImage = target != null ? target.GetComponent<Image>() : null;
        if (targetImage != null && targetImage.sprite != null)
        {
            return targetImage.sprite;
        }

        targetImage = target != null ? target.GetComponentInChildren<Image>() : null;
        return targetImage != null ? targetImage.sprite : null;
    }

    private static GameObject CreateItemCollectFxRoot(
        E_ItemType itemType,
        RectTransform parent,
        Vector3 startPos,
        bool bUiPos,
        Canvas canvas)
    {
        GameObject root = PoolUtil.GetGameObject(GetItemCollectFxRootTemplate());
        if (root == null)
        {
            return null;
        }

        root.name = $"ItemCollectFx_{itemType}";
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();
        root.SetActive(true);

        var rootRect = root.transform as RectTransform;
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = Vector2.zero;
        rootRect.localRotation = Quaternion.identity;
        rootRect.localScale = Vector3.one;

        if (bUiPos)
        {
            rootRect.anchoredPosition = new Vector2(startPos.x, startPos.y);
        }
        else if (TryGetAnchoredPosition(startPos, parent, canvas, out Vector2 startAnchoredPosition))
        {
            rootRect.anchoredPosition = startAnchoredPosition;
        }
        else
        {
            root.transform.position = startPos;
        }

        var canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        return root;
    }

    private static Image CreateItemCollectIcon(RectTransform parent, Sprite sprite, string objectName = "ItemCollectIcon")
    {
        GameObject iconObject = PoolUtil.GetGameObject(GetItemCollectFxIconTemplate());
        if (iconObject == null)
        {
            return null;
        }

        iconObject.name = objectName;
        iconObject.transform.SetParent(parent, false);
        iconObject.SetActive(true);

        var iconRect = iconObject.transform as RectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = Vector2.one * ItemCollectFxIconSize;
        iconRect.localRotation = Quaternion.identity;
        iconRect.localScale = Vector3.one * ItemCollectFxStartScale;

        var icon = iconObject.GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = true;
        icon.color = Color.white;
        return icon;
    }

    private static GameObject GetItemCollectFxRootTemplate()
    {
        if (itemCollectFxRootTemplate != null)
        {
            return itemCollectFxRootTemplate;
        }

        itemCollectFxRootTemplate = new GameObject(
            "__ItemCollectFxRootTemplate",
            typeof(RectTransform),
            typeof(CanvasGroup));
        itemCollectFxRootTemplate.SetActive(false);
        itemCollectFxRootTemplate.transform.SetParent(PrefabPoolModule.Instance.transform, false);
        return itemCollectFxRootTemplate;
    }

    private static GameObject GetItemCollectFxIconTemplate()
    {
        if (itemCollectFxIconTemplate != null)
        {
            return itemCollectFxIconTemplate;
        }

        itemCollectFxIconTemplate = new GameObject(
            "__ItemCollectFxIconTemplate",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        itemCollectFxIconTemplate.SetActive(false);
        itemCollectFxIconTemplate.transform.SetParent(PrefabPoolModule.Instance.transform, false);

        var icon = itemCollectFxIconTemplate.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.color = Color.white;
        return itemCollectFxIconTemplate;
    }

    private static void UpdateItemCollectIconFrames(
        Image[] icons,
        Image[][] trailImages,
        RectTransform[] iconRects,
        RectTransform[][] trailRects,
        Vector2[] scatterPositions,
        Vector2[] flyStartPositions,
        Vector2[] flyControlPositions,
        float[] delays,
        float[] scatterDurations,
        float[] flyDurations,
        float[] startRotations,
        float[] rotationSpeeds,
        float[] popScales,
        float[] hoverPhases,
        float[] hoverAmplitudes,
        float[] hoverDurations,
        Vector2 targetLocalPosition,
        float elapsed)
    {
        if (icons == null ||
            iconRects == null ||
            scatterPositions == null ||
            flyStartPositions == null ||
            flyControlPositions == null ||
            delays == null ||
            scatterDurations == null ||
            flyDurations == null ||
            startRotations == null ||
            rotationSpeeds == null ||
            popScales == null ||
            hoverPhases == null ||
            hoverAmplitudes == null ||
            hoverDurations == null)
        {
            return;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            float localTime = elapsed - delays[i];
            UpdateItemCollectIconFrame(
                icons[i],
                iconRects[i],
                trailImages != null && i < trailImages.Length ? trailImages[i] : null,
                trailRects != null && i < trailRects.Length ? trailRects[i] : null,
                scatterPositions[i],
                flyStartPositions[i],
                flyControlPositions[i],
                targetLocalPosition,
                scatterDurations[i],
                flyDurations[i],
                startRotations[i],
                rotationSpeeds[i],
                popScales[i],
                hoverPhases[i],
                hoverAmplitudes[i],
                hoverDurations[i],
                localTime);
        }
    }

    private static void UpdateItemCollectIconFrame(
        Image icon,
        RectTransform iconRect,
        Image[] trails,
        RectTransform[] trailRects,
        Vector2 scatterPosition,
        Vector2 flyStartPosition,
        Vector2 flyControlPosition,
        Vector2 targetLocalPosition,
        float scatterDuration,
        float flyDuration,
        float startRotation,
        float rotationSpeed,
        float popScale,
        float hoverPhase,
        float hoverAmplitude,
        float hoverDuration,
        float localTime)
    {
        if (icon == null || iconRect == null)
        {
            return;
        }

        ItemCollectFxFrameState frameState = EvaluateItemCollectFrame(
            scatterPosition,
            flyStartPosition,
            flyControlPosition,
            targetLocalPosition,
            scatterDuration,
            flyDuration,
            startRotation,
            rotationSpeed,
            popScale,
            hoverPhase,
            hoverAmplitude,
            hoverDuration,
            localTime);
        ApplyItemCollectFrameState(icon, iconRect, frameState);

        if (trails == null || trailRects == null)
        {
            return;
        }

        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] == null || i >= trailRects.Length || trailRects[i] == null)
            {
                continue;
            }

            float trailTime = localTime - (i + 1) * ItemCollectFxTrailInterval;
            ItemCollectFxFrameState trailState = EvaluateItemCollectFrame(
                scatterPosition,
                flyStartPosition,
                flyControlPosition,
                targetLocalPosition,
                scatterDuration,
                flyDuration,
                startRotation,
                rotationSpeed,
                popScale,
                hoverPhase,
                hoverAmplitude,
                hoverDuration,
                trailTime);

            if (!trailState.IsFlying)
            {
                trailState.Visible = false;
            }
            else
            {
                float flyStartTime = scatterDuration + hoverDuration;
                float flyProgress = Mathf.Clamp01((trailTime - flyStartTime) / Mathf.Max(flyDuration, 0.0001f));
                if (flyProgress < ItemCollectFxTrailShowStart)
                {
                    trailState.Visible = false;
                    ApplyItemCollectFrameState(trails[i], trailRects[i], trailState);
                    continue;
                }

                float trailFadeIn = Mathf.Clamp01((flyProgress - ItemCollectFxTrailShowStart) / 0.1f);
                float trailWeight = 1f - (float)i / Mathf.Max(1, trails.Length);
                trailState.Alpha *= ItemCollectFxTrailAlpha * trailWeight * trailFadeIn;
                trailState.Scale *= Mathf.Lerp(0.92f, 0.68f, (float)i / Mathf.Max(1, trails.Length - 1));
            }

            ApplyItemCollectFrameState(trails[i], trailRects[i], trailState);
        }
    }

    private static ItemCollectFxFrameState EvaluateItemCollectFrame(
        Vector2 scatterPosition,
        Vector2 flyStartPosition,
        Vector2 flyControlPosition,
        Vector2 targetLocalPosition,
        float scatterDuration,
        float flyDuration,
        float startRotation,
        float rotationSpeed,
        float popScale,
        float hoverPhase,
        float hoverAmplitude,
        float hoverDuration,
        float localTime)
    {
        scatterDuration = Mathf.Max(scatterDuration, 0.0001f);
        flyDuration = Mathf.Max(flyDuration, 0.0001f);

        if (localTime < 0f)
        {
            return new ItemCollectFxFrameState
            {
                Visible = false,
                Position = ItemCollectFxScatterStartOffset,
                Scale = ItemCollectFxStartScale,
                Rotation = 0f,
                Alpha = 0f
            };
        }

        if (localTime <= scatterDuration)
        {
            float t = Mathf.Clamp01(localTime / scatterDuration);
            Vector2 startPosition = ItemCollectFxScatterStartOffset;
            Vector2 scatterDirection = scatterPosition - startPosition;
            Vector2 outward = scatterDirection.sqrMagnitude > 0.0001f ? scatterDirection.normalized : Vector2.right;
            Vector2 clusterPosition = GetLaunchClusterPosition(
                startPosition,
                scatterPosition,
                hoverPhase,
                startRotation,
                popScale);
            float clusterRatio = ItemCollectFxLaunchClusterRatio;
            if (t <= clusterRatio)
            {
                float clusterT = Mathf.Clamp01(t / Mathf.Max(clusterRatio, 0.0001f));
                float clusterEase = EaseOutBack(clusterT);
                float launchPulse = Mathf.Sin(clusterT * Mathf.PI);
                return new ItemCollectFxFrameState
                {
                    Visible = true,
                    Position = Vector2.LerpUnclamped(startPosition, clusterPosition, clusterEase) +
                               outward * launchPulse * ItemCollectFxScatterKick * 0.65f +
                               Vector2.up * launchPulse * ItemCollectFxLaunchClusterRadius * 0.22f,
                    Scale = Mathf.LerpUnclamped(ItemCollectFxStartScale, popScale * 1.08f, clusterEase) +
                            launchPulse * ItemCollectFxScatterScalePunch * Mathf.Clamp(popScale, 0.6f, 1.35f),
                    Rotation = Mathf.LerpUnclamped(0f, startRotation * 0.72f, EaseOutCubic(clusterT)),
                    Alpha = Mathf.Clamp01(clusterT * 4f),
                    IsFlying = false
                };
            }

            float spreadT = Mathf.Clamp01((t - clusterRatio) / Mathf.Max(1f - clusterRatio, 0.0001f));
            float spreadEase = EaseOutCubic(spreadT);
            float settlePulse = Mathf.Sin(spreadT * Mathf.PI);
            return new ItemCollectFxFrameState
            {
                Visible = true,
                Position = Vector2.LerpUnclamped(clusterPosition, scatterPosition, spreadEase) +
                           outward * settlePulse * ItemCollectFxScatterKick * 0.24f,
                Scale = Mathf.LerpUnclamped(popScale * 1.08f, popScale, spreadEase) +
                        settlePulse * ItemCollectFxScatterScalePunch * Mathf.Clamp(popScale, 0.6f, 1.35f) * 0.32f,
                Rotation = Mathf.LerpUnclamped(startRotation * 0.72f, startRotation, spreadEase),
                Alpha = 1f,
                IsFlying = false
            };
        }

        float hoverTime = localTime - scatterDuration;
        hoverDuration = Mathf.Max(hoverDuration, 0.0001f);
        if (hoverTime <= hoverDuration)
        {
            float hoverProgress = Mathf.Clamp01(hoverTime / hoverDuration);
            float shakeAngle = hoverProgress * ItemCollectFxHoverShakeCycles * Mathf.PI * 2f;
            return new ItemCollectFxFrameState
            {
                Visible = true,
                Position = GetHoverFramePosition(
                    scatterPosition,
                    targetLocalPosition,
                    hoverTime,
                    hoverDuration,
                    hoverPhase,
                    hoverAmplitude),
                Scale = popScale * (1f + Mathf.Sin(shakeAngle) * 0.03f),
                Rotation = startRotation + Mathf.Sin(shakeAngle + hoverPhase * Mathf.PI * 0.5f) * 12f,
                Alpha = 1f,
                IsFlying = false
            };
        }

        float flyTime = hoverTime - hoverDuration;
        float flyT = Mathf.Clamp01(flyTime / flyDuration);
        float moveT = EaseInQuad(flyT);
        Vector2 position = QuadraticBezier(flyStartPosition, flyControlPosition, targetLocalPosition, moveT);

        float midFlightPulse = Mathf.Sin(flyT * Mathf.PI) * ItemCollectFxFlyMidScaleAdd;
        float scaleT = EaseInQuad(Mathf.Clamp01((flyT - 0.16f) / 0.84f));
        float scale = Mathf.LerpUnclamped(popScale, ItemCollectFxEndScale, scaleT) + midFlightPulse;
        float fadeDuration = Mathf.Max(1f - ItemCollectFxFadeStart, 0.0001f);
        float fadeT = Mathf.Clamp01((flyT - ItemCollectFxFadeStart) / fadeDuration);
        float alpha = Mathf.LerpUnclamped(1f, ItemCollectFxEndAlpha, EaseInQuad(fadeT));

        return new ItemCollectFxFrameState
        {
            Visible = true,
            Position = position,
            Scale = Mathf.Max(0f, scale),
            Rotation = startRotation + rotationSpeed * flyTime * 0.75f,
            Alpha = alpha,
            IsFlying = true
        };
    }

    private static void ApplyItemCollectFrameState(Image icon, RectTransform iconRect, ItemCollectFxFrameState frameState)
    {
        if (icon == null || iconRect == null)
        {
            return;
        }

        bool visible = frameState.Visible && frameState.Alpha > 0.01f;
        icon.enabled = visible;
        if (!visible)
        {
            return;
        }

        iconRect.anchoredPosition = frameState.Position;
        iconRect.localRotation = Quaternion.Euler(0f, 0f, frameState.Rotation);
        iconRect.localScale = Vector3.one * frameState.Scale;
        SetImageAlpha(icon, frameState.Alpha);
    }

    private static void UpdateItemCollectBurstFrame(Image burstImage, RectTransform burstRect, float elapsed)
    {
        if (burstImage == null || burstRect == null)
        {
            return;
        }

        if (elapsed < 0f || elapsed > ItemCollectFxBurstDuration)
        {
            burstImage.enabled = false;
            return;
        }

        float t = Mathf.Clamp01(elapsed / Mathf.Max(ItemCollectFxBurstDuration, 0.0001f));
        float scale = Mathf.LerpUnclamped(ItemCollectFxBurstStartScale, ItemCollectFxBurstEndScale, EaseOutBack(t));
        float alpha = ItemCollectFxBurstAlpha * (1f - EaseInQuad(t));

        burstImage.enabled = alpha > 0.01f;
        if (!burstImage.enabled)
        {
            return;
        }

        burstRect.anchoredPosition = Vector2.zero;
        burstRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-18f, 26f, EaseOutCubic(t)));
        burstRect.localScale = Vector3.one * scale;
        burstImage.color = new Color(1f, 0.95f, 0.48f, alpha);
    }

    private static void ReleaseItemCollectIcons(Image[] icons)
    {
        if (icons == null)
        {
            return;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            ReleaseItemCollectIcon(icons[i]);
        }
    }

    private static void ReleaseItemCollectIcon(Image icon)
    {
        if (icon == null)
        {
            return;
        }

        icon.enabled = true;
        icon.sprite = null;
        icon.color = Color.white;

        if (icon.transform is RectTransform iconRect)
        {
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.localRotation = Quaternion.identity;
            iconRect.localScale = Vector3.one;
        }

        PoolUtil.ReleaseGameObject(icon.gameObject);
    }

    private static void ReleaseItemCollectTrailImages(Image[][] trailImages)
    {
        if (trailImages == null)
        {
            return;
        }

        for (int i = 0; i < trailImages.Length; i++)
        {
            ReleaseItemCollectIcons(trailImages[i]);
        }
    }

    private static void ReleaseItemCollectFxRoot(GameObject root)
    {
        var canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        PoolUtil.ReleaseGameObject(root);
    }

    private static void SetImageAlpha(Image icon, float alpha)
    {
        Color color = icon.color;
        color.a = alpha;
        icon.color = color;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseInQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t;
    }

    private static float EaseInAbsorb(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.42f;
        const float c3 = c1 + 1f;
        float offset = t - 1f;
        float offsetSquared = offset * offset;
        return 1f + c3 * offsetSquared * offset + c1 * offsetSquared;
    }

    private static Vector2 QuadraticBezier(Vector2 from, Vector2 control, Vector2 to, float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return inverse * inverse * from + 2f * inverse * t * control + t * t * to;
    }

    private static Vector2 GetLaunchClusterPosition(
        Vector2 startPosition,
        Vector2 scatterPosition,
        float hoverPhase,
        float startRotation,
        float popScale)
    {
        Vector2 scatterDirection = scatterPosition - startPosition;
        float distance = scatterDirection.magnitude;
        Vector2 outward = distance > 0.0001f ? scatterDirection / distance : Vector2.up;
        Vector2 tangent = new Vector2(-outward.y, outward.x);
        float radius = Mathf.Min(ItemCollectFxLaunchClusterRadius, distance * 0.38f);
        float angle = hoverPhase * Mathf.PI * 2f + startRotation * Mathf.Deg2Rad;
        float layer = Mathf.Sin(angle * 1.7f + 0.6f) * 0.5f + 0.5f;
        float side = Mathf.Sin(angle);
        float scaleWeight = Mathf.Clamp(popScale / Mathf.Max(ItemCollectFxPopScale, 0.0001f), 0.65f, 1.35f);

        return startPosition +
               outward * radius * Mathf.Lerp(0.42f, 0.86f, layer) +
               tangent * side * radius * 0.56f * scaleWeight +
               Vector2.up * radius * Mathf.Lerp(0.18f, 0.42f, layer);
    }

    private static Vector2 GetScatterPosition(int index, bool isHero, Vector2 targetLocalPosition)
    {
        Vector2 forward = targetLocalPosition.sqrMagnitude > 1f ? targetLocalPosition.normalized : Vector2.up;
        if (Vector2.Dot(forward, Vector2.up) < 0.2f)
        {
            forward = Vector2.Lerp(forward, Vector2.up, 0.45f).normalized;
        }

        Vector2 tangent = new Vector2(-forward.y, forward.x);
        int anchorIndex = isHero
            ? index % itemCollectFxScatterClusterAnchors.Length
            : (index * 2 + 1) % itemCollectFxScatterClusterAnchors.Length;
        Vector2 anchor = itemCollectFxScatterClusterAnchors[anchorIndex];

        float radius = UnityEngine.Random.Range(ItemCollectFxScatterRadiusMin, ItemCollectFxScatterRadiusMax) *
                       (isHero ? UnityEngine.Random.Range(0.98f, 1.16f) : UnityEngine.Random.Range(0.72f, 0.98f));
        float jitter = ItemCollectFxScatterClusterJitter * (isHero ? 0.65f : 1f);
        float side = (anchor.x + UnityEngine.Random.Range(-jitter, jitter)) *
                     radius *
                     ItemCollectFxScatterFanSideScale;
        float depth = (anchor.y + UnityEngine.Random.Range(-jitter * 0.55f, jitter * 0.45f)) *
                      radius *
                      ItemCollectFxScatterFanForwardScale;

        return tangent * side + forward * depth;
    }

    private static Vector2 GetHoverPosition(
        Vector2 scatterPosition,
        float hoverTime,
        float hoverDuration,
        float hoverPhase,
        float hoverAmplitude)
    {
        Vector2 outward = scatterPosition.sqrMagnitude > 0.0001f ? scatterPosition.normalized : Vector2.right;
        Vector2 tangent = new Vector2(-outward.y, outward.x);
        float progress = Mathf.Clamp01(hoverTime / Mathf.Max(hoverDuration, 0.0001f));
        float envelope = Mathf.Sin(progress * Mathf.PI);
        float shakeAngle = progress * ItemCollectFxHoverShakeCycles * Mathf.PI * 2f;
        float side = hoverPhase < 0.5f ? -1f : 1f;
        float vertical = Mathf.Sin(shakeAngle) * hoverAmplitude * envelope;
        float horizontal = Mathf.Sin(shakeAngle + Mathf.PI * 0.5f) *
                           ItemCollectFxHoverSideAmplitude *
                           envelope *
                           side;
        float drift = Mathf.Sin((progress + hoverPhase * 0.35f) * Mathf.PI * 2f) * 3f * envelope;
        return scatterPosition + new Vector2(horizontal, vertical) + tangent * drift;
    }

    private static Vector2 GetHoverFramePosition(
        Vector2 scatterPosition,
        Vector2 targetLocalPosition,
        float hoverTime,
        float hoverDuration,
        float hoverPhase,
        float hoverAmplitude)
    {
        Vector2 hoverPosition = GetHoverPosition(scatterPosition, hoverTime, hoverDuration, hoverPhase, hoverAmplitude);
        float attractT = EaseInQuad(Mathf.Clamp01(hoverTime / Mathf.Max(hoverDuration, 0.0001f))) * ItemCollectFxHoverAttract;
        return Vector2.LerpUnclamped(hoverPosition, targetLocalPosition, attractT);
    }

    private static Vector2 GetFlyControlPosition(Vector2 scatterPosition, Vector2 targetLocalPosition)
    {
        Vector2 direction = targetLocalPosition - scatterPosition;
        float distance = direction.magnitude;
        Vector2 normal = distance > 0.0001f
            ? new Vector2(-direction.y, direction.x) / distance
            : Vector2.right;

        float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float sideOffset = UnityEngine.Random.Range(ItemCollectFxFlyArcSide * 0.35f, ItemCollectFxFlyArcSide) * side;
        float height = Mathf.Clamp(distance * 0.08f, ItemCollectFxFlyArcHeight * 0.35f, ItemCollectFxFlyArcHeight);
        return Vector2.Lerp(scatterPosition, targetLocalPosition, 0.52f) +
               normal * sideOffset +
               Vector2.up * height;
    }

    internal static bool TryGetAnchoredPosition(
        Vector3 worldPosition,
        RectTransform parent,
        Canvas canvas,
        out Vector2 anchoredPosition)
    {
        if (parent == null)
        {
            anchoredPosition = Vector2.zero;
            return false;
        }

        Camera camera = GetCanvasCamera(canvas);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out anchoredPosition);
    }

    private static Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        if (canvas.worldCamera != null)
        {
            return canvas.worldCamera;
        }

        UIModule uiModule = UIModule.Instance;
        return uiModule != null && uiModule.UICamera != null ? uiModule.UICamera : Camera.main;
    }

    internal static bool CanBeginItemCollectFx(int count, RewardCollectAnimation animation = RewardCollectAnimation.BurstCollect)
    {
        // Free rewards start immediately whenever a concurrent slot is available.
        if (animation == RewardCollectAnimation.Legacy)
            return count > 0 && activeFreeItemCollectFxCount + count <= ItemCollectFxConfig.MaxConcurrentFreeFx;
        Queue<float> starts = itemCollectFxStartTimes;
        float window = ItemCollectFxStartWindowSeconds;
        float now = Time.unscaledTime;
        while (starts.Count > 0 && now - starts.Peek() >= window)
        {
            starts.Dequeue();
        }

        return count > 0 && activeItemCollectFxCount + count <= MaxConcurrentItemCollectFx &&
            starts.Count + count <= MaxItemCollectFxStartsPerSecond;
    }

    internal static bool TryBeginItemCollectFx(E_ItemType itemType, string prefabName, RewardCollectAnimation animation)
    {
        if (!CanBeginItemCollectFx(1, animation))
        {
            TrackItemCollectFx(itemType, prefabName, true, 0);
            return false;
        }

        if (animation == RewardCollectAnimation.Legacy)
        {
            activeFreeItemCollectFxCount++;
        }
        else
        {
            activeItemCollectFxCount++;
            itemCollectFxStartTimes.Enqueue(Time.unscaledTime);
        }
        return true;
    }

    internal static void EndItemCollectFx(RewardCollectAnimation animation)
    {
        if (animation == RewardCollectAnimation.Legacy)
            activeFreeItemCollectFxCount = Mathf.Max(0, activeFreeItemCollectFxCount - 1);
        else
            activeItemCollectFxCount = Mathf.Max(0, activeItemCollectFxCount - 1);
    }

    private static void TrackItemCollectFx(E_ItemType itemType, string prefabName, bool throttled, int iconCount)
    {
        // SnakeArrow does not include BubbleBlast's Crashlytics wrapper.
    }
}
#endif
