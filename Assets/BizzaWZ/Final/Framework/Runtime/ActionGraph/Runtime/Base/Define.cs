using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


/// <summary>
/// 执行状态，只能返回Running/Success/Failed三个状态
/// </summary>
/// 
  [Obfuz.ObfuzIgnore]
public enum E_ExecuteState
{
    //内部使用，未开始/已经结束被销毁
    None,

    ////返回状态用下面这三种//////
    Running,
    Success,
    Failed,

    //内部使用，等待销毁
    WaitToDestroy,
}
  [Obfuz.ObfuzIgnore]
public enum E_DebugExecuteState
{
    None,

    Running,
    Success,
    Failed,

    Start,
}

/// <summary>
/// 图表执行参数
/// </summary>
public struct ExecuteArgs
{
    public bool IsValid => graph != null && graph.IsValid;
    public GameActor Self => context?.self;//图表的执行者：如单位buff就是单位，子弹行为树/子弹buff就是子弹
    public GameActor SelfOrOwner => context?.SelfOrOwner;
    // public BattleActor BattleActor => SelfOrOwner as BattleActor;//根据图表执行者找到单位，如单位就是本身，子弹/技能就是发射单位
    // public BattleActor Attacker => context?.Attacker;//攻击者，如果是技能，则返回释放者，如果是子弹，则返回子弹所有者，如果是buff，则返回buff添加者

    public Transform Transform => Self != null ? Self.transform : null;

    public ActionContextBase context;//上下文，用于存放和图表类型相关的数据，如技能上下文会保存技能本身，技能朝向，技能目标等数据
    public ActionGraphBase graph;//当前节点所在的图表
    public float deltaTime;
}

public static class ActionGraphExt
{
    public static bool IsReturnState(this E_ExecuteState executeState)
    {
        return executeState == E_ExecuteState.Success || executeState == E_ExecuteState.Failed || executeState == E_ExecuteState.None;
    }
}