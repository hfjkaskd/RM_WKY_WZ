#if BIZZA_REAL_WITHDRAW
using System;
using Bizza.FlyMoney;
using Bizza.Sdk;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "RewardItemCollectFxConfig", menuName = "BizzaGame/Reward Item Collect Fx Config")]
[Obfuz.ObfuzIgnore]
public class RewardItemCollectFxConfig : ChannelConfigBase<RewardItemCollectFxConfig>
{
    [FoldoutGroup("新版广告奖励")]
    [LabelText("启用新版动画（关闭回退旧版）")]
    public bool useNativeAdRewardFx = true;
    [FoldoutGroup("新版广告奖励")]
    [LabelText("强制低配")]
    public bool forceLowNativeQuality;
    [FoldoutGroup("新版广告奖励")]
    [LabelText("低内存设备自动低配")]
    public bool autoLowNativeQuality = true;
    [FoldoutGroup("新版广告奖励")]
    [LabelText("标准档参数"), DrawWithUnity]
    public Bizza.FlyMoney.FlyMoneySettings nativeSettings = Bizza.FlyMoney.FlyMoneySettings.Standard;
    [FoldoutGroup("新版广告奖励")]
    [LabelText("低配档参数"), DrawWithUnity]
    public Bizza.FlyMoney.FlyMoneySettings nativeLowSettings = Bizza.FlyMoney.FlyMoneySettings.Low;
    public bool UseLowNativeQuality => forceLowNativeQuality || (autoLowNativeQuality &&
        Application.isMobilePlatform && SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 3072);

    [FoldoutGroup("新版广告奖励/国家拖尾")]
    [LabelText("按国家选择拖尾配色")]
    [Tooltip("只影响新版动画。关闭后，全部使用标准档或低配档里的通用拖尾颜色。")]
    public bool useCountryTrails = true;
    [FoldoutGroup("新版广告奖励/国家拖尾"), ShowIf(nameof(useCountryTrails))]
    [LabelText("美国（US）")]
    public RewardCountryTrailSettings trailsUS = RewardCountryTrailSettings.Create(RewardTrailStyle.UnitedStatesGreen);
    [FoldoutGroup("新版广告奖励/国家拖尾"), ShowIf(nameof(useCountryTrails))]
    [LabelText("印尼（ID）")]
    public RewardCountryTrailSettings trailsID = RewardCountryTrailSettings.Create(RewardTrailStyle.IndonesiaBlue);
    [FoldoutGroup("新版广告奖励/国家拖尾"), ShowIf(nameof(useCountryTrails))]
    [LabelText("巴西（BR）")]
    public RewardCountryTrailSettings trailsBR = RewardCountryTrailSettings.Create(RewardTrailStyle.BrazilPink);

    public FlyMoneySettings GetNativeSettings(AccountModule.E_CountryType country, bool cash)
    {
        FlyMoneySettings settings = UseLowNativeQuality ? nativeLowSettings : nativeSettings;
        if (!useCountryTrails) return settings;
        RewardCountryTrailSettings countryTrails = country switch
        {
            AccountModule.E_CountryType.US => trailsUS,
            AccountModule.E_CountryType.ID => trailsID,
            _ => trailsBR
        };
        countryTrails.ApplyTo(ref settings, cash);
        return settings;
    }

    [FoldoutGroup("并发限制")]
    [LabelText("最大同时播放数")]
    [Range(1, FlyMoneyPlayer.HardConcurrencyLimit)]
    public int maxConcurrentFx = FlyMoneyPlayer.HardConcurrencyLimit;

    [FoldoutGroup("并发限制")]
    [LabelText("每秒最大触发数")]
    [MinValue(1)]
    public int maxStartsPerSecond = 4;

    [FoldoutGroup("并发限制")]
    [LabelText("触发统计窗口")]
    [MinValue(0.05f)]
    public float startWindowSeconds = 1f;

    [FoldoutGroup("免费领取并发限制")]
    [LabelText("免费动画最大同时播放数")]
    [Range(1, RewardItemCollectFlow.FreeConcurrencyLimit)]
    public int maxConcurrentFreeFx = RewardItemCollectFlow.FreeConcurrencyLimit;

    [FoldoutGroup("数量")]
    [LabelText("钞票数量")]
    [Range(1, 32)]
    public int iconCount = 10;

    [FoldoutGroup("数量")]
    [LabelText("主钞票数量")]
    [Range(0, 12)]
    public int heroCount = 4;

    [FoldoutGroup("数量")]
    [LabelText("拖影数量")]
    [Range(0, 4)]
    public int trailCount = 2;

    [FoldoutGroup("尺寸")]
    [LabelText("图标尺寸")]
    [MinValue(1f)]
    public float iconSize = 132f;

    [FoldoutGroup("尺寸")]
    [LabelText("起始缩放")]
    [MinValue(0f)]
    public float startScale = 0.16f;

    [FoldoutGroup("尺寸")]
    [LabelText("弹出基础缩放")]
    [MinValue(0f)]
    public float popScale = 1f;

    [FoldoutGroup("尺寸")]
    [LabelText("结束缩放")]
    [MinValue(0f)]
    public float endScale = 0.08f;

    [FoldoutGroup("透明度")]
    [LabelText("结束透明度")]
    [Range(0f, 1f)]
    public float endAlpha = 0f;

    [FoldoutGroup("透明度")]
    [LabelText("飞行淡出起点")]
    [Range(0f, 1f)]
    public float fadeStart = 0.76f;

    [FoldoutGroup("第一阶段 - 弹出")]
    [LabelText("散开半径最小值")]
    [MinValue(0f)]
    public float scatterRadiusMin = 88f;

    [FoldoutGroup("第一阶段 - 弹出")]
    [LabelText("散开半径最大值")]
    [MinValue(0f)]
    public float scatterRadiusMax = 176f;

    [FoldoutGroup("第一阶段 - 弹出")]
    [LabelText("散开时长")]
    [MinValue(0.01f)]
    public float scatterDuration = 0.18f;

    [FoldoutGroup("第一阶段 - 弹出")]
    [LabelText("弹出冲量")]
    [MinValue(0f)]
    public float scatterKick = 48f;

    [FoldoutGroup("第一阶段 - 弹出")]
    [LabelText("弹出缩放冲量")]
    [MinValue(0f)]
    public float scatterScalePunch = 0.24f;

    [FoldoutGroup("Scatter Layout")]
    [LabelText("Launch Cluster Radius")]
    [MinValue(0f)]
    public float launchClusterRadius = 42f;

    [FoldoutGroup("Scatter Layout")]
    [LabelText("Launch Cluster Ratio")]
    [Range(0.1f, 0.75f)]
    public float launchClusterRatio = 0.42f;

    [FoldoutGroup("Scatter Layout")]
    [LabelText("Start Offset")]
    public Vector2 scatterStartOffset = Vector2.zero;

    [FoldoutGroup("Scatter Layout")]
    [LabelText("Side Scale")]
    [Range(0f, 2f)]
    public float scatterFanSideScale = 1.08f;

    [FoldoutGroup("Scatter Layout")]
    [LabelText("Forward Scale")]
    [Range(-1f, 1.2f)]
    public float scatterFanForwardScale = 0.35f;

    [FoldoutGroup("Scatter Layout")]
    [LabelText("Cluster Jitter")]
    [Range(0f, 0.6f)]
    public float scatterClusterJitter = 0.26f;

    [FoldoutGroup("第二阶段 - 晃动")]
    [LabelText("晃动时长")]
    [MinValue(0.01f)]
    public float hoverDuration = 0.2f;

    [FoldoutGroup("第二阶段 - 晃动")]
    [LabelText("额外错峰时长")]
    [MinValue(0f)]
    public float hoverExtraDelayMax = 0.12f;

    [FoldoutGroup("第二阶段 - 晃动")]
    [LabelText("上下晃动幅度")]
    [MinValue(0f)]
    public float hoverAmplitude = 9f;

    [FoldoutGroup("第二阶段 - 晃动")]
    [LabelText("左右晃动幅度")]
    [MinValue(0f)]
    public float hoverSideAmplitude = 7f;

    [FoldoutGroup("第二阶段 - 晃动")]
    [LabelText("晃动次数")]
    [MinValue(0f)]
    public float hoverShakeCycles = 2f;

    [FoldoutGroup("第二阶段 - 晃动")]
    [LabelText("预吸附比例")]
    [Range(0f, 1f)]
    public float hoverAttract = 0.08f;

    [FoldoutGroup("第三阶段 - 飞行")]
    [LabelText("起手错峰")]
    [MinValue(0f)]
    public float startDelayMax = 0.012f;

    [FoldoutGroup("第三阶段 - 飞行")]
    [LabelText("飞行时长")]
    [MinValue(0.01f)]
    public float flyDuration = 0.24f;

    [FoldoutGroup("第三阶段 - 飞行")]
    [LabelText("飞行弧线高度")]
    [MinValue(0f)]
    public float flyArcHeight = 46f;

    [FoldoutGroup("第三阶段 - 飞行")]
    [LabelText("飞行侧向弧度")]
    [MinValue(0f)]
    public float flyArcSide = 20f;

    [FoldoutGroup("第三阶段 - 飞行")]
    [LabelText("飞行中段缩放")]
    [MinValue(0f)]
    public float flyMidScaleAdd = 0.02f;

    [FoldoutGroup("第三阶段 - 飞行")]
    [LabelText("最大旋转速度")]
    [MinValue(0f)]
    public float rotationMaxSpeed = 260f;

    [FoldoutGroup("拖影")]
    [LabelText("拖影间隔")]
    [MinValue(0.001f)]
    public float trailInterval = 0.026f;

    [FoldoutGroup("拖影")]
    [LabelText("拖影透明度")]
    [Range(0f, 1f)]
    public float trailAlpha = 0.14f;

    [FoldoutGroup("拖影")]
    [LabelText("拖影出现进度")]
    [Range(0f, 1f)]
    public float trailShowStart = 0.08f;

    [FoldoutGroup("爆闪")]
    [LabelText("爆闪时长")]
    [MinValue(0.01f)]
    public float burstDuration = 0.16f;

    [FoldoutGroup("爆闪")]
    [LabelText("爆闪起始缩放")]
    [MinValue(0f)]
    public float burstStartScale = 0.18f;

    [FoldoutGroup("爆闪")]
    [LabelText("爆闪结束缩放")]
    [MinValue(0f)]
    public float burstEndScale = 0.65f;

    [FoldoutGroup("爆闪")]
    [LabelText("爆闪透明度")]
    [Range(0f, 1f)]
    public float burstAlpha = 0.35f;

    [FoldoutGroup("调试")]
    [LabelText("调试物品")]
    public E_ItemType debugItemType = E_ItemType.Gold;

    [FoldoutGroup("调试")]
    [LabelText("调试数量")]
    public float debugAddCount = 100f;

    [FoldoutGroup("调试")]
    [LabelText("播放动画")]
    public bool debugPlayAnim = true;

    [FoldoutGroup("调试")]
    [LabelText("显示货币栏")]
    public bool debugShowCurrencyBar;

    [FoldoutGroup("Debug")]
    [LabelText("Use Reward Tips Flow")]
    public bool debugUseRewardTipsFlow = true;

    [FoldoutGroup("调试")]
    [LabelText("起点")]
    public Transform debugStartTransform;

    [FoldoutGroup("调试")]
    [LabelText("目标")]
    public Transform debugTargetTransform;

    [FoldoutGroup("调试")]
    [LabelText("动画类型（免费领取默认旧版）")]
    public RewardCollectAnimation debugAnimation;

    [FoldoutGroup("调试")]
    [Button("仅预览动画（不加钱）")]
    private void PreviewAnimation()
    {
        if (!Application.isPlaying) return;
        VFXUtils.PlayItemCollectFx(debugItemType, ResolveDebugStartPosition(), null,
            bUiPos: false, target: debugTargetTransform, animation: debugAnimation);
    }

    [FoldoutGroup("调试")]
    [Button("中断全部飞钱（奖励不回退）")]
    private void StopRewardAnimations()
    {
        if (Application.isPlaying) RewardItemCollectFlow.CancelAll();
    }

    public int MaxConcurrentFx => Mathf.Clamp(maxConcurrentFx, 1, FlyMoneyPlayer.HardConcurrencyLimit);
    public int MaxStartsPerSecond => Mathf.Max(1, maxStartsPerSecond);
    public float StartWindowSeconds => Mathf.Max(0.05f, startWindowSeconds);
    public int MaxConcurrentFreeFx => Mathf.Clamp(maxConcurrentFreeFx, 1, RewardItemCollectFlow.FreeConcurrencyLimit);
    public int IconCount => Mathf.Clamp(iconCount, 1, 32);
    public int HeroCount => Mathf.Clamp(heroCount, 0, IconCount);
    public int TrailCount => Mathf.Clamp(trailCount, 0, 4);
    public float IconSize => Mathf.Max(1f, iconSize);
    public float StartScale => Mathf.Max(0f, startScale);
    public float PopScale => Mathf.Max(0f, popScale);
    public float EndScale => Mathf.Max(0f, endScale);
    public float EndAlpha => Mathf.Clamp01(endAlpha);
    public float FadeStart => Mathf.Clamp01(fadeStart);
    public float ScatterRadiusMin => Mathf.Max(0f, scatterRadiusMin);
    public float ScatterRadiusMax => Mathf.Max(ScatterRadiusMin, scatterRadiusMax);
    public float ScatterDuration => Mathf.Max(0.01f, scatterDuration);
    public float ScatterKick => Mathf.Max(0f, scatterKick);
    public float ScatterScalePunch => Mathf.Max(0f, scatterScalePunch);
    public float LaunchClusterRadius => Mathf.Max(0f, launchClusterRadius);
    public float LaunchClusterRatio => Mathf.Clamp(launchClusterRatio, 0.1f, 0.75f);
    public Vector2 ScatterStartOffset => scatterStartOffset;
    public float ScatterFanSideScale => Mathf.Clamp(scatterFanSideScale, 0f, 2f);
    public float ScatterFanForwardScale => Mathf.Clamp(scatterFanForwardScale, -100f, 100f);
    public float ScatterClusterJitter => Mathf.Clamp(scatterClusterJitter, 0f, 0.6f);
    public float HoverDuration => Mathf.Max(0.01f, hoverDuration);
    public float HoverExtraDelayMax => Mathf.Max(0f, hoverExtraDelayMax);
    public float HoverAmplitude => Mathf.Max(0f, hoverAmplitude);
    public float HoverSideAmplitude => Mathf.Max(0f, hoverSideAmplitude);
    public float HoverShakeCycles => Mathf.Max(0f, hoverShakeCycles);
    public float HoverAttract => Mathf.Clamp01(hoverAttract);
    public float StartDelayMax => Mathf.Max(0f, startDelayMax);
    public float FlyDuration => Mathf.Max(0.01f, flyDuration);
    public float FlyArcHeight => Mathf.Max(0f, flyArcHeight);
    public float FlyArcSide => Mathf.Max(0f, flyArcSide);
    public float FlyMidScaleAdd => Mathf.Max(0f, flyMidScaleAdd);
    public float RotationMaxSpeed => Mathf.Max(0f, rotationMaxSpeed);
    public float TrailInterval => Mathf.Max(0.001f, trailInterval);
    public float TrailAlpha => Mathf.Clamp01(trailAlpha);
    public float TrailShowStart => Mathf.Clamp01(trailShowStart);
    public float BurstDuration => Mathf.Max(0.01f, burstDuration);
    public float BurstStartScale => Mathf.Max(0f, burstStartScale);
    public float BurstEndScale => Mathf.Max(0f, burstEndScale);
    public float BurstAlpha => Mathf.Clamp01(burstAlpha);

    [FoldoutGroup("调试")]
    [Button("快速加钱")]
    private void DebugAddMoney()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("RewardItemCollectFxConfig: 请在 Play Mode 下使用快速加钱。");
            return;
        }

        var item = ItemUtils.ToCorrect(new ItemEntry
        {
            Type = debugItemType,
            Count = debugAddCount
        });

        if (debugUseRewardTipsFlow)
        {
            DebugAddMoneyWithRewardTipsFlow(item);
            return;
        }

        DebugAddMoneyFromPosition(item, ResolveDebugStartPosition());
    }

    private void DebugAddMoneyWithRewardTipsFlow(ItemEntry item)
    {
        if (!debugPlayAnim)
        {
            UIUtils.ShowTips(item, default);
            DebugApplyItem(item);
            return;
        }

        UIUtils.ShowTips(item, default, (goldPos, itemPos) =>
        {
            Vector3 startPos = item.Type == E_ItemType.Gold ? goldPos : itemPos;
            if (startPos == Vector3.zero)
            {
                startPos = ResolveDebugStartPosition();
            }

            DebugAddMoneyFromPosition(item, startPos);
        });
    }

    private void DebugAddMoneyFromPosition(ItemEntry item, Vector3 startPos)
    {
        if (IsDirectMoneyType(item.Type))
        {
            if (!debugPlayAnim)
            {
                DebugApplyItem(item);
                return;
            }

            VFXUtils.PlayItemCollectFx(
                item.Type,
                startPos,
                () => DebugApplyItem(item),
                bUiPos: false,
                target: debugTargetTransform,
                animation: debugAnimation);
            return;
        }

        ItemUtils.AddItem(item, new AddItemParam
        {
            playAnim = debugPlayAnim,
            isAd = false,
            startPos = startPos,
            bUiPos = false,
            showCurrencyBar = debugShowCurrencyBar,
            target = debugTargetTransform,
            source = E_AddItemSource.None,
            animation = debugAnimation,
        });
    }

    private Vector3 ResolveDebugStartPosition()
    {
        if (debugStartTransform != null)
        {
            return debugStartTransform.position;
        }

        BroadcastBarController broadcastBar = BroadcastBarController.Instance;
        if (broadcastBar != null && broadcastBar.entryAObj != null)
        {
            return broadcastBar.entryAObj.transform.position;
        }

        return Vector3.zero;
    }

    private static bool IsDirectMoneyType(E_ItemType itemType)
    {
        return itemType == E_ItemType.Dollar || itemType == E_ItemType.WithDrawDanDollar;
    }

    private static void DebugApplyItem(ItemEntry inItem)
    {
        SaveDataUtils.ItemData.itemMap.TryAdd(inItem.Type, new ItemEntry { Type = inItem.Type });

        ItemEntry item = SaveDataUtils.ItemData.itemMap[inItem.Type];
        ItemEntry prevItem = item;
        item.Count += inItem.Count;
        item.Count = Mathf.Max(0f, item.Count);
        SaveDataUtils.ItemData.itemMap[inItem.Type] = item;

        BizzaEventSystem.Emit(EventDefine.Item.ItemChangedWithData, prevItem, item);
        SaveDataUtils.itemStrategy.SaveData();
        BizzaEventSystem.Emit(EventDefine.Item.ItemChanged);
    }
}

public enum RewardTrailStyle
{
    [LabelText("跟随通用颜色")] Common = 0,
    [LabelText("巴西粉色")] BrazilPink = 1,
    [LabelText("印尼蓝色")] IndonesiaBlue = 2,
    [LabelText("美国绿色")] UnitedStatesGreen = 3,
    [LabelText("金币金色")] Gold = 4,
    [LabelText("自定义颜色")] Custom = 5,
    [LabelText("关闭拖尾")] Off = 6
}

[Serializable]
public struct RewardCountryTrailSettings
{
    [LabelText("钞票拖尾")]
    [Tooltip("单货币模式中，显示为钞票的金币也使用此项。")]
    public RewardTrailStyle cashStyle;
    [LabelText("钞票自定义颜色"), ShowIf(nameof(UseCustomCash)), ColorUsage(false)]
    [Tooltip("仅设置 RGB；透明度使用当前画质档的通用拖尾颜色 A 值。")]
    public Color customCashColor;
    [LabelText("金币拖尾")]
    public RewardTrailStyle coinStyle;
    [LabelText("金币自定义颜色"), ShowIf(nameof(UseCustomCoin)), ColorUsage(false)]
    [Tooltip("仅设置 RGB；透明度使用当前画质档的通用拖尾颜色 A 值。")]
    public Color customCoinColor;

    private bool UseCustomCash => cashStyle == RewardTrailStyle.Custom;
    private bool UseCustomCoin => coinStyle == RewardTrailStyle.Custom;

    public static RewardCountryTrailSettings Create(RewardTrailStyle cashStyle) => new RewardCountryTrailSettings
    {
        cashStyle = cashStyle, coinStyle = RewardTrailStyle.Gold,
        customCashColor = Color.white, customCoinColor = Color.white
    };

    public void ApplyTo(ref FlyMoneySettings settings, bool cash)
    {
        RewardTrailStyle style = cash ? cashStyle : coinStyle;
        if (style == RewardTrailStyle.Off) { settings.trails = false; return; }
        Color color = style switch
        {
            RewardTrailStyle.BrazilPink => new Color(0.94f, 0.22f, 0.69f),
            RewardTrailStyle.IndonesiaBlue => new Color(0.2f, 0.7f, 1f),
            RewardTrailStyle.UnitedStatesGreen => new Color(0.35f, 0.85f, 0.38f),
            RewardTrailStyle.Gold => new Color(1f, 0.73f, 0.16f),
            RewardTrailStyle.Custom => cash ? customCashColor : customCoinColor,
            _ => settings.trailColor
        };
        color.a = settings.trailColor.a;
        settings.trailColor = color;
    }
}
#endif
