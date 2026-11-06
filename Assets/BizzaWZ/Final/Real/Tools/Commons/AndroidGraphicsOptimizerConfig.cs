#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Obfuz.ObfuzIgnore]
public enum AndroidGraphicsDeviceTier
{
    [LabelText("超低档")]
    VeryLow,
    [LabelText("低档")]
    Low,
    [LabelText("中档")]
    Medium,
    [LabelText("高档")]
    High
}

[Serializable]
[InlineProperty]
[Obfuz.ObfuzIgnore]
public class AndroidGraphicsTierSettings
{
    [BoxGroup("画质参数")]
    [LabelText("渲染缩放")]
    [SuffixLabel("倍", true)]
    [Range(0.1f, 2f)]
    public float renderScale = 1f;

    [BoxGroup("画质参数")]
    [LabelText("目标帧率")]
    [SuffixLabel("FPS", true)]
    [Min(1)]
    public int targetFrameRate = 30;

    [BoxGroup("画质参数")]
    [LabelText("MSAA 采样")]
    [Tooltip("建议填写 0、2、4 或 8。")]
    [Min(0)]
    public int msaa = 0;

    [BoxGroup("画质参数")]
    [LabelText("开启 HDR")]
    public bool enableHdr = false;

    public void Sanitize()
    {
        renderScale = Mathf.Max(0.1f, renderScale);
        targetFrameRate = Mathf.Max(1, targetFrameRate);
        msaa = Mathf.Max(0, msaa);
    }
}

[Obfuz.ObfuzIgnore]
[CreateAssetMenu(
    fileName = "AndroidGraphicsOptimizerConfig",
    menuName = "Graphics/Android Graphics Optimizer Config"
)]
[Title("安卓画质优化配置")]
public class AndroidGraphicsOptimizerConfig : ScriptableObject
{
    [BoxGroup("设备分档规则")]
    [LabelText("超低档最大内存")]
    [SuffixLabel("MB", true)]
    [Min(0)]
    public int veryLowMaxMemoryMb = 2048;

    [BoxGroup("设备分档规则")]
    [LabelText("低档最大内存")]
    [SuffixLabel("MB", true)]
    [Min(0)]
    public int lowMaxMemoryMb = 3072;

    [BoxGroup("设备分档规则")]
    [LabelText("中档最大内存")]
    [SuffixLabel("MB", true)]
    [Min(0)]
    public int mediumMaxMemoryMb = 6144;

    [BoxGroup("分辨率降档规则")]
    [LabelText("强制降一档分辨率阈值")]
    [SuffixLabel("px", true)]
    [Min(0)]
    public int forceLowerTierScreenMax = 2400;

    [BoxGroup("分辨率降档规则")]
    [LabelText("高档降为中档阈值")]
    [SuffixLabel("px", true)]
    [Min(0)]
    public int capHighTierToMediumScreenMax = 2000;

    [BoxGroup("低端 GPU 识别")]
    [LabelText("低端 GPU 关键字")]
    [ListDrawerSettings(Expanded = true)]
    public List<string> lowEndGpuKeywords = new List<string>
    {
        "adreno 5",
        "adreno (tm) 5",
        "mali-g31",
        "mali-g52",
        "mali-t",
        "powervr"
    };

    [BoxGroup("档位参数")]
    [LabelText("超低档")]
    public AndroidGraphicsTierSettings veryLowSettings = new AndroidGraphicsTierSettings
    {
        renderScale = 0.35f,
        targetFrameRate = 30,
        msaa = 0,
        enableHdr = false
    };

    [BoxGroup("档位参数")]
    [LabelText("低档")]
    public AndroidGraphicsTierSettings lowSettings = new AndroidGraphicsTierSettings
    {
        renderScale = 0.55f,
        targetFrameRate = 30,
        msaa = 0,
        enableHdr = false
    };

    [BoxGroup("档位参数")]
    [LabelText("中档")]
    public AndroidGraphicsTierSettings mediumSettings = new AndroidGraphicsTierSettings
    {
        renderScale = 0.8f,
        targetFrameRate = 60,
        msaa = 2,
        enableHdr = false
    };

    [BoxGroup("档位参数")]
    [LabelText("高档")]
    public AndroidGraphicsTierSettings highSettings = new AndroidGraphicsTierSettings
    {
        renderScale = 0.9f,
        targetFrameRate = 60,
        msaa = 2,
        enableHdr = false
    };

    public AndroidGraphicsTierSettings GetTierSettings(AndroidGraphicsDeviceTier tier)
    {
        switch (tier)
        {
            case AndroidGraphicsDeviceTier.VeryLow:
                return veryLowSettings;
            case AndroidGraphicsDeviceTier.Low:
                return lowSettings;
            case AndroidGraphicsDeviceTier.Medium:
                return mediumSettings;
            case AndroidGraphicsDeviceTier.High:
                return highSettings;
            default:
                return mediumSettings;
        }
    }

    private void OnValidate()
    {
        veryLowMaxMemoryMb = Mathf.Max(0, veryLowMaxMemoryMb);
        lowMaxMemoryMb = Mathf.Max(veryLowMaxMemoryMb, lowMaxMemoryMb);
        mediumMaxMemoryMb = Mathf.Max(lowMaxMemoryMb, mediumMaxMemoryMb);

        forceLowerTierScreenMax = Mathf.Max(0, forceLowerTierScreenMax);
        capHighTierToMediumScreenMax = Mathf.Max(0, capHighTierToMediumScreenMax);

        if (forceLowerTierScreenMax > 0 && capHighTierToMediumScreenMax > forceLowerTierScreenMax)
        {
            capHighTierToMediumScreenMax = forceLowerTierScreenMax;
        }

        if (lowEndGpuKeywords == null)
        {
            lowEndGpuKeywords = new List<string>();
        }

        if (veryLowSettings == null)
        {
            veryLowSettings = new AndroidGraphicsTierSettings();
        }

        if (lowSettings == null)
        {
            lowSettings = new AndroidGraphicsTierSettings();
        }

        if (mediumSettings == null)
        {
            mediumSettings = new AndroidGraphicsTierSettings();
        }

        if (highSettings == null)
        {
            highSettings = new AndroidGraphicsTierSettings();
        }

        veryLowSettings.Sanitize();
        lowSettings.Sanitize();
        mediumSettings.Sanitize();
        highSettings.Sanitize();
    }
}
#endif