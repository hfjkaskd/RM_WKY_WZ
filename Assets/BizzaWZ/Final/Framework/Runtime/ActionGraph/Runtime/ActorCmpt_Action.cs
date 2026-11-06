using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using cfg;
using Sirenix.OdinInspector;
using UnityEngine;

public class ActorCmpt_Action : ActorMonoCmpt<GameActor>
{
#if UNITY_EDITOR
    private StringBuilder _sb = new StringBuilder();
#endif
    [Multiline(10)]
    public string debugInfo;

    [ShowInInspector][LabelText("正在运行的图表数量")]
    public int DebugRunningGraphNum => runningGraphs.Count;

    [CustomValueDrawer("CustomValueDrawer")][ShowInInspector]
    public List<ActionGraphBase> runningGraphs = new();

#if UNITY_EDITOR
    private static ActionGraphBase CustomValueDrawer(ActionGraphBase v, GUIContent label)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(v.graphName))
        {
            GraphDebugUtils.DebugGraph(v, v.context.self);
        }

        GUILayout.EndHorizontal();
        return v;
    }
#endif

    [HideLabel][InlineProperty]
    public GraphBlackBoard blackBoard = new GraphBlackBoard();

    protected override void OnInit()
    {
        // runningGraphs.Clear();

#if UNITY_EDITOR
        _sb.Clear();
        debugInfo = _sb.ToString();
#endif
    }

    protected override void OnReleaseBegin()
    {
        base.OnReleaseBegin();
        for (int i = runningGraphs.Count - 1; i >= 0; i--)
        {
            if (ActionModule.Instance != null)
            {
                ActionModule.Instance.Stop(runningGraphs[i]);
            }
        }
        runningGraphs.Clear();
        blackBoard.Clear();
    }

    public void OnGraphStart(ActionGraphBase graph)
    {
        runningGraphs.Add(graph);
#if UNITY_EDITOR
        _sb.AppendLine($"开始:{graph.graphName} {Time.frameCount}");
        debugInfo = _sb.ToString();
#endif
    }

    public void OnGraphEnd(ActionGraphBase graph)
    {
        runningGraphs.Remove(graph);
#if UNITY_EDITOR
        _sb.AppendLine($"结束:{graph.graphName} {Time.frameCount}");
        debugInfo = _sb.ToString();
#endif
    }
}

