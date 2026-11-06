using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;


// /// <summary>
// /// 简单节点：只有一个子节点
// /// </summary>
// public abstract class SimpleNode : NodeBase
// {
//
// }

 
[Serializable]
[Obfuz.ObfuzIgnore]
public abstract class NodeBase : GraphElementBase
{
    [NonSerialized][JsonIgnore][HideInInspector]
    public ActionGraphBase belongGraph;

    [NonSerialized][JsonIgnore][HideInInspector]
    public E_DebugExecuteState debugState;

    [NonSerialized] [JsonIgnore] [HideInInspector]
    public int lastHasStateFrame;

    #region config
    [HideInInspector][JsonIgnore]
    public NodeBase parent;
    [SerializeReference]
    public List<NodeBase> children = new();
    #endregion

    #region runtime data

    //是否拥有多个子节点
    [JsonIgnore]
    public virtual bool multChild => false;

    protected NodeBase SingleChild
    {
        get
        {
            if (children == null || children.Count == 0) return null;
            // ActionGraphLog.Assert(children.Count == 1, "SimpleNode SingleChild internal error");
            return children[0];
        }
    }

    [JsonIgnore]
    public E_ExecuteState CurExecuteState => _curExecuteState;

    protected E_ExecuteState _curExecuteState
    {
        get => __curExecuteState;
        set
        {
            if (__curExecuteState != value)
            {
                var oldValue = __curExecuteState;
                __curExecuteState = value;
#if UNITY_EDITOR
                OnStateChange(oldValue, __curExecuteState);
#endif
            }
        }
    }

    [Conditional("UNITY_EDITOR")]
    protected virtual void OnStateChange(E_ExecuteState oldValue, E_ExecuteState newValue)
    {
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        if (this.IsDebugSelected())
        {
            Debug.LogError(oldValue + " ===> " + newValue);
        }
#endif
    }

    private E_ExecuteState __curExecuteState;

    [NonSerialized]
    internal E_ExecuteState _lastFrameRet;

    public virtual void OnReset()
    {
        _curExecuteState = E_ExecuteState.None;
        _lastFrameRet = E_ExecuteState.None;
    }

    #endregion

    /// <summary>
    /// 执行节点
    /// </summary>
    /// <param name="executeArgs"></param>
    /// <returns></returns>
    public E_ExecuteState Execute(in ExecuteArgs executeArgs)
    {
        if (!executeArgs.IsValid)
        {
            return E_ExecuteState.Failed;
        }

#if ENABLE_ACTION_GRAPH_PROFILER
        // if (this is ActionNodeBase)
        {
            Profiler.BeginSample(ProfilerDebugName);
        }
#endif
        _lastFrameRet = _curExecuteState;
        if (_curExecuteState == E_ExecuteState.None)
        {
            Enter(executeArgs);
        }

        if (_curExecuteState == E_ExecuteState.Running)
        {
            var ret = OnExecute(executeArgs);
#if UNITY_EDITOR
            // ActionGraphLog.Assert(ret == E_ExecuteState.Running || ret == E_ExecuteState.Success || ret == E_ExecuteState.Failed, $"错误的返回状态:{GetType().ToString()}");
#endif
            _curExecuteState = ret;

            if (executeArgs.graph.CurState == E_ExecuteState.WaitToDestroy)
            {
#if ENABLE_ACTION_GRAPH_PROFILER
                // if (this is ActionNodeBase)
                {
                    Profiler.EndSample();
                }
#endif
                return E_ExecuteState.Failed;
            }

            if (ret == E_ExecuteState.Success || ret == E_ExecuteState.Failed)
            {
                Exit(executeArgs);
            }
#if UNITY_EDITOR
            GraphDebugUtils.UpdateNodeExecuteState(this, executeArgs, ret);
#endif

#if ENABLE_ACTION_GRAPH_PROFILER
            // if (this is ActionNodeBase)
            {
                Profiler.EndSample();
            }
#endif
            return ret;
        }

        return E_ExecuteState.Failed;
    }

    /// <summary>
    /// 节点进入，在同一帧Execute之前调用
    /// </summary>
    /// <param name="executeArgs"></param>
    public void Enter(in ExecuteArgs executeArgs)
    {
        if (_curExecuteState != E_ExecuteState.None)
        {
            ActionGraphLog.Error($"error:{_curExecuteState}");
            return;
        }

        _curExecuteState = E_ExecuteState.Running;
        OnEnter(executeArgs);
    }

    /// <summary>
    /// 节点离开
    /// </summary>
    /// <param name="executeArgs"></param>
    public void Exit(in ExecuteArgs executeArgs)
    {
        // if (_curExecuteState != E_ExecuteState.Running)
        // {
        //     ActionGraphLog.Error("error");
        //     return;
        // }

        if (_curExecuteState == E_ExecuteState.None)
        {
            return;
        }

        var prevState = _curExecuteState;
        _curExecuteState = E_ExecuteState.None;

        if (children != null)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                // if (child._curExecuteState == E_ExecuteState.Running)
                if (child != null)
                {
                    child.Exit(executeArgs);//Stop
                }
            }
        }

        // if (prevState == E_ExecuteState.Running)
        {
            OnExit(executeArgs, prevState == E_ExecuteState.Running);
        }
    }


    #region 需要实现的函数

    /// <summary>
    /// 检查当前节点的合法性
    /// </summary>
    public virtual bool IsValid => _curExecuteState != E_ExecuteState.None;

    protected abstract E_ExecuteState OnExecute(in ExecuteArgs executeArgs);

    protected virtual void OnEnter(in ExecuteArgs executeArgs)
    {
    }

    protected virtual void OnExit(in ExecuteArgs executeArgs, bool interrupt)
    {

    }

    // public virtual void Stop(in ExecuteArgs executeArgs)
    // {
    //
    // }
    #endregion

    public string ToString(int level)
    {
        string indent = new string(' ', level * 6);
        var variables = GraphDebugUtils.GetAllVariableInClass<VariableWrapperBase>(this);
        string result = indent + "--" + DebugName + $"({string.Join(',', variables.Select(x=>((VariableWrapperBase)x.fieldValue)?.DebugName ?? "null"))})" + "\n";
        if (children != null)
        {
            foreach (var child in children)
            {
                if (child != null)
                {
                    result += child.ToString(level + 1);
                }
                else
                {
                    result += "[null]";
                }
            }
        }

        return result;
    }

    #region editor data
    [LabelText("标题")][PropertyOrder(9998)]
    public string titleNote;
    [LabelText("备注")][Multiline(5)][PropertyOrder(9999)]
    public string note;

    #endregion
}
