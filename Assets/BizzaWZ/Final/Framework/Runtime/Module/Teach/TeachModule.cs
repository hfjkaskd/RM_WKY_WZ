using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg;
using System;

#if UNITY_EDITOR
using UnityEditor;
public static class TeachEditorTool
{

    [MenuItem("工具/开关/教程模式/打开")]
    static void TeachMode_On()
    {
        EditorPrefs.SetBool("Editor_TeachMode", true);
    }

    [MenuItem("工具/开关/教程模式/关闭")]
    static void TeachMode_Off()
    {
        EditorPrefs.SetBool("Editor_TeachMode", false);
    }

    [MenuItem("工具/开关/教程模式/清除")]
    static void TeachMode_Clear()
    {
        EditorPrefs.DeleteKey("Editor_TeachMode");
    }
}
#endif

public class TeachModule : BaseGameModule<TeachModule>
{
    private readonly List<ActionGraph_Teach> GraphBases = new List<ActionGraph_Teach>();
    private readonly List<ActionGraph_Teach> RunningGraphBases = new();
    public int RunningGraphCount => RunningGraphBases.Count;

    public bool IsTeaching => isTeaching;
    private bool isTeaching = false;
    public bool teachEnable;

    public override void InitGameModule()
    {
        bool enable = GameExtensionRegistry.ShouldEnableTeach();
#if UNITY_EDITOR
        enable = enable && UnityEditor.EditorPrefs.GetBool("Editor_TeachMode", true);
#endif
        teachEnable = enable;
        if (!enable)
        {
            return;
        }

        BizzaEventSystem.On(EventDefine.Frame.LoadingStageComplete, AddGraphBase);
    }

    private void AddGraphBase()
    {
        ActionGraph_Teach GraphBase;
        bool _ing = false;
        var _teachConfigId = "Teach_01";
        if (SaveDataUtils.TeachData.IsCompleted(_teachConfigId))
        {
            LogLogger.LogInfo("teachConfig.Id" + _teachConfigId);
            return;
        }
        _ing = true;

        GraphBase = ActionGraphUtils.Get(_teachConfigId, false) as ActionGraph_Teach;
        GraphBases.Add(GraphBase);

        LogLogger.LogInfo("_ing.Id" + _ing);
        isTeaching = _ing;
    }

    void Update()
    {
        if (!isTeaching)
            return;

        for (int i = RunningGraphBases.Count - 1; i >= 0; i--)
        {
            if (!RunningGraphBases[i].Running)
            {
                RunningGraphBases.RemoveAt(i);
            }
        }

        for (int i = GraphBases.Count - 1; i >= 0; i--)
        {
            ActionGraph_Teach GraphBase = GraphBases[i];
            if (GraphBase?.conditionOverride != null)
            {
                var context = PoolUtil.GetClass<ActionContext_Teach>();
                var args = ActionGraphUtils.GetEventActionArgs(GraphBase, context);
                bool isRun = GraphBase.conditionOverride.GetValueWithDefault(args, false);
                if (isRun)
                {
                    ActionModule.Instance.Run(GraphBase, context);
                    GraphBases.Remove(GraphBase);
                    RunningGraphBases.Add(GraphBase);
                }
            }
        }
    }

    public override void ReleaseGameModule()
    {
        BizzaEventSystem.Off(EventDefine.Frame.LoadingStageComplete, AddGraphBase);
    }
}
