using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;


public static class ActionDataForEditor
{
    public static ActionGraphBase curGraph;
}
 [Obfuz.ObfuzIgnore]
public abstract class ActionContextBase
{
    public GameActor self;

    //各种输出参数
    public GameActor tmpActor;
    public GameActor foreachActor;
    public GameActor skill;
    public int tmpEnum;
    public bool isCrit;
    public bool isMissing;
    public float bulletDamage;
    public Vector3 bulletTargetPos;

    public string eventName;
    public float eventArgFloat1;
    public string eventArgStr1;
    public GameActor eventArgActor1;

    public abstract GameActor SelfOrOwner { get; }

    // public virtual BattleActor Attacker { get; }
}
 [Obfuz.ObfuzIgnore]
public class ActionContext_Common : ActionContextBase, IOnRelease
{
    public void OnRelease()
    {
        self = null;
    }

    public override GameActor SelfOrOwner => self;
}
 [Obfuz.ObfuzIgnore]
public enum E_GraphEvent_Common
{
    OnStart = 1,
    OnEnd,
    SendEvent,
}

 
[Serializable] [Obfuz.ObfuzIgnore]
public abstract class GraphConfigData : ICloneable
{
    public abstract object Clone();
    public abstract void CloneTo(GraphConfigData data);
}

  [Obfuz.ObfuzIgnore]
public abstract class ActionGraphBase : ICloneable, IPoolObject
{
    [Multiline(5)]
    [Title("图表备注")] [HideLabel]
    public string note;

    [NonSerialized][JsonIgnore]
    internal bool autoReleaseToPool = true;

    [FoldoutGroup("基础配置(不配表时，将使用这里的配置)")]
    [SerializeReference][HideLabel][HideReferenceObjectPicker][InlineProperty]
    internal GraphConfigData configData;

    internal abstract bool UpdateInGamePause { get; }
    internal virtual bool IsValid => CurState != E_ExecuteState.None && CurState != E_ExecuteState.WaitToDestroy;

    internal abstract GraphConfigData CreateConfigData();

    [HideLabel][InlineProperty]
    public GraphBlackBoard blackBoard = new GraphBlackBoard();

    //config
    [NonSerialized] public string graphName;
    [NonSerialized] [HideInInspector] public string graphFullName;
    [SerializeReference][HideInInspector]
    public NodeBase root;
    [NonSerialized][HideInInspector]
    public List<OutputActionBase> callbackRoots = new();
    [SerializeReference][HideInInspector]
    public List<VariableBase> variableList = new();
    [SerializeReference][HideInInspector]
    public List<NodeBase> unconnectedNodes = new();
    [SerializeReference][HideInInspector]
    public List<VariableBase> defaultVariableList = new();

    //runtime data
    public Action<ActionGraphBase> onEnd; //finish or stop

    [JsonIgnore]
    public float RunningTime => World.Current.GameTime - startTime;
    [HideInInspector][JsonIgnore][NonSerialized]
    public float startTime;
    public bool Running => _curState == E_ExecuteState.Running;
    public E_ExecuteState CurState => _curState;
    private E_ExecuteState _curState;
    public ActionContextBase context;

    [NonSerialized][JsonIgnore]
    internal bool interruptedEnd;//是否为被打断结束

    public T GetVariableNode<T>() where T : OutputActionBase
    {
        if (callbackRoots == null)
        {
            return default;
        }

        foreach (var v in callbackRoots)
        {
            if (v is T varType)
            {
                return varType;
            }
        }

        return default;
    }

    public virtual void EditorInit()
    {
        // if (defaultVariableList == null) defaultVariableList = new List<VariableBase>();
        // else defaultVariableList.Clear();
        // InitDefaultVariables();
        if (configData == null)
        {
            configData = CreateConfigData();
        }
    }

    // protected abstract void InitDefaultVariables();

    public virtual void OnReset()
    {
        _curState = E_ExecuteState.None;
        interruptedEnd = false;
        onEnd = null;
        blackBoard.CopyFrom(ActionGraphUtils.GetGraphSO(graphName).graph.blackBoard);
    }

    public ActionGraphBase(string name)
    {
        graphName = name;
        root = new RootNode();
    }

    public void OnEnter(in ExecuteArgs executeArgs)
    {
#if UNITY_EDITOR
        if (graphName == GraphDebugUtils.debugGraphName && context.self == GraphDebugUtils.debugOwner)
        {
            // GraphDebugUtils.debugGraph = this;
            if (GraphDebugUtils.debugGraph != this)
            {
                GraphDebugUtils.DebugGraph(this, context.self);
            }
        }
#endif
        if (_curState != E_ExecuteState.None)
        {
            return;
        }
        _curState = E_ExecuteState.Running;
        startTime = World.Current.GameTime;
        // ActionGraphLog.Verbose($"开始执行Graph:{graphName}\n" + this.ToString());
        ActionCallback.TriggerEvent(this, (int)E_GraphEvent_Common.OnStart, context);
    }

    public void OnExit()
    {
        if (_curState == E_ExecuteState.None)
        {
            return;
        }

        interruptedEnd = _curState == E_ExecuteState.Running;
        ActionCallback.TriggerEvent(this, (int)E_GraphEvent_Common.OnEnd, context);
        _curState = E_ExecuteState.None;

        onEnd?.Invoke(this);

    }

    //提前触发Start事件
    public void TriggerEnterEvent(ExecuteArgs executeArgs)
    {
        if (_curState == E_ExecuteState.None)
        {
            OnEnter(executeArgs);
        }
    }

    public E_ExecuteState Execute(ActionContextBase context, float dt)
    {
        // #if UNITY_EDITOR
        // if (graphName == GraphDebugUtils.debugGraphName && context.self == GraphDebugUtils.debugOwner)
        // {
        //     GraphDebugUtils.debugGraph = this;
        // }
        // #endif

        if (_curState == E_ExecuteState.WaitToDestroy)
        {
            return E_ExecuteState.WaitToDestroy;
        }

        if (root == null)
        {
            return E_ExecuteState.Failed;
        }

        var executeArgs = new ExecuteArgs()
        {
            graph = this,
            context = context,
            deltaTime = dt,
        };

        var ret = root.Execute(executeArgs);

        if (ret == E_ExecuteState.Success || ret == E_ExecuteState.Failed)
        {
            // OnExit();
        }
        else
        {
            _curState = ret;
        }

        return ret;
    }

    //只能内部调用，使用ActionModule.Instance.Stop
    internal void __InternalStop(bool interrupt)
    {
        if (root == null)
        {
            return;
        }

        var executeArgs = new ExecuteArgs()
        {
            graph = this,
            context = context,
        };

        root.Exit(executeArgs);
        OnExit();

        if (interrupt)
        {
            _curState = E_ExecuteState.WaitToDestroy;
        }
    }


    public abstract object Clone();

    protected void CloneBase(ActionGraphBase graphBase)
    {
        graphBase.note = note;
        graphBase.graphName = graphName;
        graphBase.configData = (GraphConfigData)configData?.Clone();
        graphBase.blackBoard = (GraphBlackBoard)blackBoard?.Clone();
        // graphBase.context = context.Clone();
        graphBase.root = (NodeBase)root.Clone();
        RecursiveCloneChildren(root, graphBase.root);
        if (callbackRoots != null)
        {
            graphBase.callbackRoots = new();
            // foreach (var v in callbackRoots)
            // {
            //     graphBase.callbackRoots.Add(v.Key, (NodeBase)v.Value.Clone());
            // }
        }
        else
        {
            graphBase.callbackRoots = null;
        }

        if (variableList != null)
        {
            graphBase.variableList = new List<VariableBase>();
            foreach (var v in variableList)
            {
                if (v == null)
                {
                    graphBase.variableList.Add(null);
                    continue;
                }
                var variable = (VariableBase) v.Clone();
                variable.editorPos = v.editorPos;
                graphBase.variableList.Add(variable);
            }
        }
        else
        {
            graphBase.variableList = null;
        }

        if (defaultVariableList != null)
        {
            graphBase.defaultVariableList = new List<VariableBase>();
            foreach (var v in defaultVariableList)
            {
                if (v == null)
                {
                    graphBase.defaultVariableList.Add(null);
                    continue;
                }
                var variable = (VariableBase) v.Clone();
                variable.editorPos = v.editorPos;
                graphBase.defaultVariableList.Add(variable);
            }
        }
        else
        {
            graphBase.defaultVariableList = null;
        }

        graphBase.unconnectedNodes = new List<NodeBase>();
        if (unconnectedNodes != null)
        {
            foreach (var v in unconnectedNodes)
            {
                var unconnectedRoot = (NodeBase) v.Clone();
                RecursiveCloneChildren(v, unconnectedRoot);
                graphBase.unconnectedNodes.Add(unconnectedRoot);
            }
        }
    }

    private void RecursiveCloneChildren(NodeBase from, NodeBase to)
    {
        to.editorPos = from.editorPos;
        to.titleNote = from.titleNote;
        to.note = from.note;
        if (from.children != null)
        {
            if (to.children == null)
            {
                to.children = new List<NodeBase>();
            }
            else
            {
                to.children.Clear();
            }

            foreach (var child in from.children)
            {
                if (child == null)
                {
                    to.children.Add(null);
                    continue;
                }
                var newChild = (NodeBase) child.Clone();
                RecursiveCloneChildren(child, newChild);
                to.children.Add(newChild);
            }
        }
        else
        {
            to.children = null;
        }
    }

    public List<NodeBase> GetAllNodes()
    {
        List<NodeBase> list = new List<NodeBase>();
        RecursiveGetNodes(root, list);
        foreach (var node in unconnectedNodes)
        {
            RecursiveGetNodes(node, list);
        }

        return list;
    }

    public List<VariableBase> GetAllVariable()
    {
        List<VariableBase> list = new List<VariableBase>();
        list.AddRange(variableList);
        list.AddRange(defaultVariableList);
        return list;
    }

    public virtual void CheckValid()
    {
        StringBuilder sb = new StringBuilder();
        bool hasError = false;
        var nodes = GetAllNodes();
        foreach (var node in nodes)
        {
            hasError |= node.CheckValid(sb);
        }

        var variables = GetAllVariable();
        foreach (var var in variables)
        {
            hasError |= var.CheckValid(sb);
        }

        if (hasError)
        {
            ActionGraphLog.Error($"Graph存在配置错误：{graphName}\n{sb.ToString()}");
        }
    }

    private void RecursiveGetNodes(NodeBase node, List<NodeBase> list)
    {
        if (node == null) return;

        list.Add(node);
        if (node.children != null)
        {
            foreach (var v in node.children)
            {
                RecursiveGetNodes(v, list);
            }
        }
    }

    public override string ToString()
    {
        if (root == null)
        {
            return "null";
        }

        var ret = root.ToString(0);
        foreach (var v in unconnectedNodes)
        {
            if (v is OutputActionBase eventAction)
            {
                ret += "\n";
                ret += v.ToString(0);
            }
        }

        return ret;
    }

    public void GetFromPool()
    {

    }

    public void ReleaseToPool()
    {
        if (context != null)
        {
            PoolUtil.ReleaseClass(context);
            context = null;
        }
    }
}

