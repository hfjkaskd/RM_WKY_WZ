#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_PIPELINE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

public class AndroidGraphicsOptimizer : MonoBehaviour
{
    private const string LogPrefix = "[AndroidGraphicsOptimizer]";
    private const string VeryLowQualityLevelName = "VeryLow";
    private const string LowQualityLevelName = "Low";
    private const string MediumQualityLevelName = "Medium";
    private const string HighQualityLevelName = "High";

#if UNITY_PIPELINE_URP
    private static readonly FieldInfo RendererDataListField =
        typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DefaultRendererIndexField =
        typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);
#endif

    [Obfuz.ObfuzIgnore]
    public enum AndroidGraphicsTier
    {
        [LabelText("极低档")]
        VeryLow,

        [LabelText("低档")]
        Low,

        [LabelText("中档")]
        Medium,

        [LabelText("高档")]
        High
    }

    [Obfuz.ObfuzIgnore]
    public enum ApplyMode
    {
        [LabelText("自动检测设备")]
        AutoDetect,

        [LabelText("手动指定档位")]
        Manual
    }

    [Obfuz.ObfuzIgnore]
    public enum LogMode
    {
        [LabelText("关闭")]
        Off,

        [LabelText("简洁模式")]
        Compact,

        [LabelText("详细模式")]
        Verbose
    }

#if UNITY_PIPELINE_URP
    [System.Serializable]
    [InlineProperty]
    [Obfuz.ObfuzIgnore]
    public class AndroidGraphicsTierProfile
    {
        [HorizontalGroup("Row", Width = 0.72f)]
        [LabelText("URP资源")]
        [Required]
        public UniversalRenderPipelineAsset pipelineAsset;

        [HorizontalGroup("Row")]
        [LabelText("目标帧率")]
        [SuffixLabel("FPS", true)]
        [MinValue(1)]
        public int targetFrameRate = 60;

        public void Sanitize()
        {
            targetFrameRate = Mathf.Max(1, targetFrameRate);
        }
    }
#endif

    [BoxGroup("基础设置")]
    [LabelText("仅安卓平台生效")]
    [SerializeField]
    private bool onlyApplyOnAndroid = true;

    [BoxGroup("基础设置")]
    [LabelText("启动时自动应用")]
    [SerializeField]
    private bool applyOnStart = true;

    [BoxGroup("基础设置")]
    [LabelText("应用模式")]
    [SerializeField]
    private ApplyMode applyMode = ApplyMode.AutoDetect;

    [BoxGroup("基础设置")]
    [ShowIf(nameof(IsManualMode))]
    [LabelText("手动指定档位")]
    [SerializeField]
    private AndroidGraphicsTier manualTier = AndroidGraphicsTier.Medium;

#if UNITY_PIPELINE_URP
    [BoxGroup("四档 URP 预设")]
    [LabelText("极低档预设")]
    [SerializeField]
    private AndroidGraphicsTierProfile veryLowTierProfile = new AndroidGraphicsTierProfile
    {
        targetFrameRate = 30
    };

    [BoxGroup("四档 URP 预设")]
    [LabelText("低档预设")]
    [SerializeField]
    private AndroidGraphicsTierProfile lowTierProfile = new AndroidGraphicsTierProfile
    {
        targetFrameRate = 30
    };

    [BoxGroup("四档 URP 预设")]
    [LabelText("中档预设")]
    [SerializeField]
    private AndroidGraphicsTierProfile mediumTierProfile = new AndroidGraphicsTierProfile
    {
        targetFrameRate = 60
    };

    [BoxGroup("四档 URP 预设")]
    [LabelText("高档预设")]
    [SerializeField]
    private AndroidGraphicsTierProfile highTierProfile = new AndroidGraphicsTierProfile
    {
        targetFrameRate = 60
    };
#endif

    [LabelText("编辑器上使用的级别")]
    public AndroidGraphicsTier editorTier = AndroidGraphicsTier.High;

    [BoxGroup("自动检测参数")]
    [LabelText("高分辨率降一档阈值")]
    [SerializeField]
    private int highResolutionThreshold = 2400;

    [BoxGroup("自动检测参数")]
    [LabelText("较高分辨率限制高档阈值")]
    [SerializeField]
    private int mediumResolutionThreshold = 2000;

    [BoxGroup("日志")]
    [LabelText("日志模式")]
    [SerializeField]
    private LogMode logMode = LogMode.Compact;

    [BoxGroup("运行时状态")]
    [ReadOnly]
    [ShowInInspector]
    [LabelText("最终应用档位")]
    private AndroidGraphicsTier lastAppliedTier = AndroidGraphicsTier.Medium;

    [BoxGroup("运行时状态")]
    [ReadOnly]
    [ShowInInspector]
    [LabelText("判定原因")]
    private string lastEvaluateReason = "None";

    [BoxGroup("运行时状态")]
    [ReadOnly]
    [ShowInInspector]
    [LabelText("最近一次检测步骤")]
    private string lastEvaluateStepsPreview = "None";

    private bool IsManualMode => applyMode == ApplyMode.Manual;
    private bool IsLogEnabled => logMode != LogMode.Off;
    private bool IsVerboseLog => logMode == LogMode.Verbose;

    private void Start()
    {
        LogLogger.LOGUserInstall("设置urp:0");
        if (applyOnStart)
        {
            LogLogger.LOGUserInstall("设置urp:1");
            ApplyGraphicsSettings("Start");
        }
    }

    [Button("立即应用设置")]
    [BoxGroup("调试")]
    private void ApplyFromInspector()
    {
        ApplyGraphicsSettings("InspectorButton");
    }

    [Button("仅检测设备档位")]
    [BoxGroup("调试")]
    private void DetectTierOnly()
    {
#if !UNITY_PIPELINE_URP
        LogWarningEx("Detect", "未启用 UNITY_PIPELINE_URP，无法执行完整的URP检测/预览。");
#else
        List<string> stepLogs;
        AndroidGraphicsTier tier = EvaluateAndroidTier(out string reason, out stepLogs);

        lastAppliedTier = tier;
        lastEvaluateReason = reason;
        lastEvaluateStepsPreview = BuildStepsPreview(stepLogs);

        LogInfo("Detect", BuildDetectSummary(tier, reason));

        if (IsVerboseLog)
        {
            LogStepLines("Detect-Step", stepLogs);
        }
#endif
    }

    private void ApplyGraphicsSettings(string trigger)
    {
        LogLogger.LOGUserInstall("设置urp:2");
#if !UNITY_PIPELINE_URP
        LogWarningEx("Apply", "未启用 UNITY_PIPELINE_URP，跳过应用。");
#else
        if (!Application.isPlaying)
        {
            LogWarningEx("Apply", "当前不在运行状态，跳过应用。");
            return;
        }

        LogLogger.LOGUserInstall("设置urp:3");
        bool useEditorTierOverride = Application.isEditor;

        if (!useEditorTierOverride && onlyApplyOnAndroid && Application.platform != RuntimePlatform.Android)
        {
            LogInfo("Skip", $"Trigger={trigger} | Platform={Application.platform} | OnlyApplyOnAndroid=true | {BuildDeviceSummary()}");
            return;
        }

        LogLogger.LOGUserInstall("设置urp:4");
        AndroidGraphicsTier finalTier;
        string reason;
        List<string> stepLogs = null;

        if (useEditorTierOverride)
        {
            LogLogger.LOGUserInstall("设置urp:5");
            finalTier = editorTier;
            reason = $"编辑器模式使用 editorTier={GetTierLabel(editorTier)}";
            stepLogs = new List<string>
            {
                "应用模式=EditorOverride",
                $"编辑器指定结果={GetTierLabel(finalTier)}"
            };
        }
        else if (applyMode == ApplyMode.Manual)
        {
            finalTier = manualTier;
            reason = $"手动指定档位={GetTierLabel(manualTier)}";
            stepLogs = new List<string>
            {
                $"应用模式=Manual",
                $"手动指定结果={GetTierLabel(finalTier)}"
            };
            LogLogger.LOGUserInstall("设置urp:6");
        }
        else
        {
            finalTier = EvaluateAndroidTier(out reason, out stepLogs);
            LogLogger.LOGUserInstall("设置urp:7");
        }

        SetUrpSettingByTier(finalTier);
#endif
    }

    public void SetUrpSettingByTier(AndroidGraphicsTier finalTier)
    {
        #if UNITY_PIPELINE_URP
        ApplyQualityLevelByTier(finalTier);
        #endif
        return;
#if false
        AndroidGraphicsTierProfile tierProfile = GetProfile(finalTier);
        if (tierProfile == null || tierProfile.pipelineAsset == null)
        {
            LogWarningEx("Config", $"Tier={GetTierLabel(finalTier)} | 未配置URP资源，跳过应用。");
            return;
        }

        lastAppliedTier = finalTier;

        RenderPipelineAsset beforeGraphicsAsset = GraphicsSettings.renderPipelineAsset;
        RenderPipelineAsset beforeQualityAsset = QualitySettings.renderPipeline;

        LogLogger.LOGUserInstall("设置urp:8");
        GraphicsSettings.renderPipelineAsset = tierProfile.pipelineAsset;
        QualitySettings.renderPipeline = tierProfile.pipelineAsset;
        Application.targetFrameRate = tierProfile.targetFrameRate;
        QualitySettings.vSyncCount = 0;

        LogInfo("Apply-After",
            $"Tier={GetTierLabel(finalTier)} | " +
            $"AppliedAsset={tierProfile.pipelineAsset.name} | " +
            $"AfterGlobal={GetPipelineSummary(GraphicsSettings.renderPipelineAsset)} | " +
            $"AfterQuality={GetPipelineSummary(QualitySettings.renderPipeline)} | " +
            $"TargetFPS={Application.targetFrameRate} | vSync={QualitySettings.vSyncCount}");
#endif
    }

#if UNITY_PIPELINE_URP

    private void ApplyQualityLevelByTier(AndroidGraphicsTier finalTier)
    {
        AndroidGraphicsTierProfile tierProfile = GetProfile(finalTier);
        RenderPipelineAsset beforeGraphicsAsset = GraphicsSettings.renderPipelineAsset;
        RenderPipelineAsset beforeQualityAsset = QualitySettings.renderPipeline;
        int beforeQualityLevel = QualitySettings.GetQualityLevel();

        string targetQualityLevelName = GetTargetQualityLevelName(finalTier);
        int targetQualityLevelIndex = FindQualityLevelIndex(targetQualityLevelName);
        bool appliedByQualityLevel = false;

        if (targetQualityLevelIndex >= 0)
        {
            QualitySettings.SetQualityLevel(targetQualityLevelIndex, true);
            appliedByQualityLevel = true;
        }
        else if (tierProfile != null && tierProfile.pipelineAsset != null)
        {
            // Fallback for old scenes/prefabs that still rely on direct URP asset switching.
            GraphicsSettings.renderPipelineAsset = tierProfile.pipelineAsset;
            QualitySettings.renderPipeline = tierProfile.pipelineAsset;
        }
        else
        {
            LogWarningEx("Config", $"Tier={GetTierLabel(finalTier)} | Missing Quality Level={targetQualityLevelName} and pipeline asset.");
            return;
        }

        lastAppliedTier = finalTier;

        if (tierProfile != null)
        {
            Application.targetFrameRate = tierProfile.targetFrameRate;
        }

        QualitySettings.vSyncCount = 0;

        LogInfo("Apply-After",
            $"Tier={GetTierLabel(finalTier)} | " +
            $"Mode={(appliedByQualityLevel ? "QualityLevel" : "DirectPipelineAsset")} | " +
            $"BeforeQuality={GetQualityLevelSummary(beforeQualityLevel)} | " +
            $"AfterQuality={GetQualityLevelSummary(QualitySettings.GetQualityLevel())} | " +
            $"ExpectedQuality={targetQualityLevelName} | " +
            $"BeforeGlobal={GetPipelineSummary(beforeGraphicsAsset)} | " +
            $"BeforeQualityAsset={GetPipelineSummary(beforeQualityAsset)} | " +
            $"AfterGlobal={GetPipelineSummary(GraphicsSettings.renderPipelineAsset)} | " +
            $"AfterQuality={GetPipelineSummary(QualitySettings.renderPipeline)} | " +
            $"TargetFPS={Application.targetFrameRate} | vSync={QualitySettings.vSyncCount}");
    }

    private AndroidGraphicsTier EvaluateAndroidTier(out string reason, out List<string> stepLogs)
    {
        stepLogs = new List<string>();

        int memMb = SystemInfo.systemMemorySize;
        string gpuNameRaw = SystemInfo.graphicsDeviceName;
        string gpuNameLower = string.IsNullOrEmpty(gpuNameRaw) ? string.Empty : gpuNameRaw.ToLower();
        int screenMax = Mathf.Max(Screen.width, Screen.height);

        stepLogs.Add($"设备信息 | RAM={memMb}MB | GPU={gpuNameRaw} | Screen={Screen.width}x{Screen.height} | LongSide={screenMax}");

        AndroidGraphicsTier tier;

        // Step1：先按内存粗分
        if (memMb <= 2048)
        {
            tier = AndroidGraphicsTier.VeryLow;
            reason = $"内存={memMb}MB，初判=极低档";
        }
        else if (memMb <= 3072)
        {
            tier = AndroidGraphicsTier.Low;
            reason = $"内存={memMb}MB，初判=低档";
        }
        else if (memMb <= 6144)
        {
            tier = AndroidGraphicsTier.Medium;
            reason = $"内存={memMb}MB，初判=中档";
        }
        else
        {
            tier = AndroidGraphicsTier.High;
            reason = $"内存={memMb}MB，初判=高档";
        }

        stepLogs.Add($"Step1 内存初判 | Tier={GetTierLabel(tier)} | Reason={reason}");

        // Step2：GPU关键词识别，低端GPU降档
        if (IsLowEndGpu(gpuNameLower))
        {
            AndroidGraphicsTier oldTier = tier;
            tier = LowerTier(tier);
            string stepReason = $"识别到低端GPU({gpuNameRaw})，从{GetTierLabel(oldTier)}降到{GetTierLabel(tier)}";
            reason += $" -> {stepReason}";
            stepLogs.Add($"Step2 GPU修正 | HitLowEndGpu=true | {stepReason}");
        }
        else
        {
            stepLogs.Add("Step2 GPU修正 | HitLowEndGpu=false | 保持原档位");
        }

        // Step3：高分辨率进一步降档
        if (screenMax >= highResolutionThreshold)
        {
            AndroidGraphicsTier oldTier = tier;
            tier = LowerTier(tier);
            string stepReason = $"分辨率长边={screenMax} >= {highResolutionThreshold}，从{GetTierLabel(oldTier)}降到{GetTierLabel(tier)}";
            reason += $" -> {stepReason}";
            stepLogs.Add($"Step3 分辨率修正 | HighThresholdHit=true | {stepReason}");
        }
        else if (screenMax >= mediumResolutionThreshold && tier == AndroidGraphicsTier.High)
        {
            AndroidGraphicsTier oldTier = tier;
            tier = AndroidGraphicsTier.Medium;
            string stepReason = $"分辨率长边={screenMax} >= {mediumResolutionThreshold}，高档限制为中档";
            reason += $" -> {stepReason}";
            stepLogs.Add($"Step3 分辨率修正 | MediumThresholdHit=true | 从{GetTierLabel(oldTier)}调整到{GetTierLabel(tier)}");
        }
        else
        {
            stepLogs.Add("Step3 分辨率修正 | 未触发分辨率降档");
        }

        stepLogs.Add($"最终结果 | Tier={GetTierLabel(tier)}");

        return tier;
    }

    private bool IsLowEndGpu(string gpuName)
    {
        // 保守识别，宁可少判，不要乱判
        return
            gpuName.Contains("adreno 5") ||
            gpuName.Contains("adreno (tm) 5") ||
            gpuName.Contains("mali-g31") ||
            gpuName.Contains("mali-g35") ||
            gpuName.Contains("mali-g36") ||
            gpuName.Contains("mali-g52") ||
            gpuName.Contains("mali-t") ||
            gpuName.Contains("powervr");
    }

    private AndroidGraphicsTier LowerTier(AndroidGraphicsTier tier)
    {
        switch (tier)
        {
            case AndroidGraphicsTier.High:
                return AndroidGraphicsTier.Medium;
            case AndroidGraphicsTier.Medium:
                return AndroidGraphicsTier.Low;
            case AndroidGraphicsTier.Low:
                return AndroidGraphicsTier.VeryLow;
            default:
                return AndroidGraphicsTier.VeryLow;
        }
    }

    private AndroidGraphicsTierProfile GetProfile(AndroidGraphicsTier tier)
    {
        switch (tier)
        {
            case AndroidGraphicsTier.VeryLow:
                return veryLowTierProfile;
            case AndroidGraphicsTier.Low:
                return lowTierProfile;
            case AndroidGraphicsTier.Medium:
                return mediumTierProfile;
            case AndroidGraphicsTier.High:
                return highTierProfile;
            default:
                return mediumTierProfile;
        }
    }

    private string GetTierLabel(AndroidGraphicsTier tier)
    {
        switch (tier)
        {
            case AndroidGraphicsTier.VeryLow:
                return "极低档";
            case AndroidGraphicsTier.Low:
                return "低档";
            case AndroidGraphicsTier.Medium:
                return "中档";
            case AndroidGraphicsTier.High:
                return "高档";
            default:
                return tier.ToString();
        }
    }

    private string BuildDeviceSummary()
    {
        string qualityName = "Unknown";
        int qualityLevel = QualitySettings.GetQualityLevel();
        if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length)
        {
            qualityName = QualitySettings.names[qualityLevel];
        }

        return $"Model={SystemInfo.deviceModel} | RAM={SystemInfo.systemMemorySize}MB | GPU={SystemInfo.graphicsDeviceName} | Res={Screen.width}x{Screen.height} | Quality={qualityName}";
    }

    private string BuildDetectSummary(AndroidGraphicsTier tier, string reason)
    {
        return $"Tier={GetTierLabel(tier)} | Reason={reason} | {BuildDeviceSummary()}";
    }

    private string GetTargetQualityLevelName(AndroidGraphicsTier tier)
    {
        switch (tier)
        {
            case AndroidGraphicsTier.VeryLow:
                return VeryLowQualityLevelName;
            case AndroidGraphicsTier.Low:
                return LowQualityLevelName;
            case AndroidGraphicsTier.Medium:
                return MediumQualityLevelName;
            case AndroidGraphicsTier.High:
                return HighQualityLevelName;
            default:
                return MediumQualityLevelName;
        }
    }

    private int FindQualityLevelIndex(string qualityLevelName)
    {
        if (string.IsNullOrEmpty(qualityLevelName))
            return -1;

        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(names[i], qualityLevelName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private string GetQualityLevelSummary(int qualityLevelIndex)
    {
        string[] names = QualitySettings.names;
        if (qualityLevelIndex < 0 || qualityLevelIndex >= names.Length)
            return $"{qualityLevelIndex}(Unknown)";

        return $"{qualityLevelIndex}({names[qualityLevelIndex]})";
    }

    private string GetPipelineSummary(RenderPipelineAsset renderPipelineAsset)
    {
        if (renderPipelineAsset == null)
            return "null";

        UniversalRenderPipelineAsset urpAsset = renderPipelineAsset as UniversalRenderPipelineAsset;
        if (urpAsset == null)
            return renderPipelineAsset.name;

        return
            $"{urpAsset.name}(RenderScale={urpAsset.renderScale:0.##}, " +
            $"MSAA={urpAsset.msaaSampleCount}, HDR={urpAsset.supportsHDR}, Renderer={GetRendererName(urpAsset)})";
    }

    private string GetRendererName(UniversalRenderPipelineAsset urpAsset)
    {
        if (urpAsset == null || RendererDataListField == null || DefaultRendererIndexField == null)
            return "Unknown";

        ScriptableRendererData[] rendererDataList = RendererDataListField.GetValue(urpAsset) as ScriptableRendererData[];
        if (rendererDataList == null || rendererDataList.Length == 0)
            return "None";

        int defaultRendererIndex = (int)DefaultRendererIndexField.GetValue(urpAsset);
        if (defaultRendererIndex < 0 || defaultRendererIndex >= rendererDataList.Length)
        {
            defaultRendererIndex = 0;
        }

        ScriptableRendererData rendererData = rendererDataList[defaultRendererIndex];
        return rendererData != null ? rendererData.name : "Missing";
    }
#endif

    private void LogInfo(string stage, string message)
    {
        if (!IsLogEnabled)
            return;

        Debug.Log($"{LogPrefix}[{stage}] {message}", this);
    }

    private void LogWarningEx(string stage, string message)
    {
        Debug.LogWarning($"{LogPrefix}[{stage}] {message}", this);
    }

    private void LogStepLines(string stage, List<string> stepLogs)
    {
        if (!IsVerboseLog || stepLogs == null || stepLogs.Count == 0)
            return;

        for (int i = 0; i < stepLogs.Count; i++)
        {
            Debug.Log($"{LogPrefix}[{stage}] #{i + 1} {stepLogs[i]}", this);
        }
    }

    private string BuildStepsPreview(List<string> stepLogs)
    {
        if (stepLogs == null || stepLogs.Count == 0)
            return "None";

        return string.Join("\n", stepLogs);
    }

#if UNITY_PIPELINE_URP
    private void OnValidate()
    {
        if (veryLowTierProfile == null) veryLowTierProfile = new AndroidGraphicsTierProfile();
        if (lowTierProfile == null) lowTierProfile = new AndroidGraphicsTierProfile();
        if (mediumTierProfile == null) mediumTierProfile = new AndroidGraphicsTierProfile();
        if (highTierProfile == null) highTierProfile = new AndroidGraphicsTierProfile();

        veryLowTierProfile.Sanitize();
        lowTierProfile.Sanitize();
        mediumTierProfile.Sanitize();
        highTierProfile.Sanitize();

        highResolutionThreshold = Mathf.Max(1600, highResolutionThreshold);
        mediumResolutionThreshold = Mathf.Clamp(mediumResolutionThreshold, 1600, highResolutionThreshold);
    }
#endif
}
#endif