using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/*  道具名字
ItemName_BackProp
ItemName_ClearProp
ItemName_RefreshProp
*/

[CreateAssetMenu(
    fileName = "PropConfig",
    menuName = "Config/PropConfig"
)]
public class PropConfigSO : ScriptableObject
{
    private const string DefaultResourcesPath = "Configs/PropConfig";
    private const string WhiteResourcesPath = "WhiteConfigs/PropConfig";
#if UNITY_EDITOR
    private const string EditorAssetPath = "Assets/BizzaWZ/Common/Resources/Configs/PropConfig.asset";
#endif

    [SerializeField]
    [LabelText("道具配置列表")]
    [ListDrawerSettings(
        Expanded = true,
        ShowIndexLabels = true,
        ListElementLabelName = nameof(PropConfigInfo.InspectorName),
        NumberOfItemsPerPage = 8)]
    private List<PropConfigInfo> propCfgInfos = new List<PropConfigInfo>();
    public List<PropConfigInfo> PropCfgInfos => propCfgInfos;

    public PropConfigInfo GetPropConfigInfo(E_ItemType propType)
    {
        return propCfgInfos.Find(x => x.propType == propType);
    }

    private static PropConfigSO instance = null;

    public static PropConfigSO Instance
    {
        get
        {
            if (instance == null)
            {
                instance = LoadInstance();
            }
            return instance;
        }
    }

    private static PropConfigSO LoadInstance()
    {
        var so = Resources.Load<PropConfigSO>(DefaultResourcesPath);
        if (so != null)
        {
            return so;
        }

        so = Resources.Load<PropConfigSO>(WhiteResourcesPath);
        if (so != null)
        {
            return so;
        }

#if UNITY_EDITOR
        so = UnityEditor.AssetDatabase.LoadAssetAtPath<PropConfigSO>(EditorAssetPath);
        if (so != null)
        {
            return so;
        }
#endif

        Debug.LogError($"道具配置为空，检查资源路径：PropConfigSO is null, paths:{DefaultResourcesPath},{WhiteResourcesPath}");
        return null;
    }
}

[Serializable]
public class PropConfigInfo
{
    [HideInInspector]
    public string InspectorName => propType.ToString();

    [BoxGroup("基础配置")]
    [LabelText("道具类型")]
    public E_ItemType propType;

    [BoxGroup("基础配置")]
    [LabelText("道具图标")]
    [PreviewField(64, ObjectFieldAlignment.Left)]
    public Sprite propIcon;

    [BoxGroup("功能配置")]
    [LabelText("支持解锁")]
    public bool unlockFunction;

    [BoxGroup("功能配置")]
    [ShowIf(nameof(unlockFunction))]
    [Indent]
    [LabelText("解锁条件")]
    public PropUnlockCondition unlockCondition;

    [BoxGroup("功能配置")]
    [LabelText("支持取消")]
    public bool cancelFunction;

    [BoxGroup("次数限制")]
    [LabelText("每局使用次数限制")]
    public int preLimitNum;

    [BoxGroup("次数限制")]
    [LabelText("初始时赠送道具数量")]
    public int newPlayerPropCount = 1;
}

[Serializable]
public struct PropUnlockCondition
{
    [LabelText("解锁关卡")]
    public int unlockLevel;

    public bool IsUnlock(int level)
    {
        return level >= unlockLevel;
    }
}
