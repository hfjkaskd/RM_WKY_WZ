using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 状态机控制器基类，使用状态机的实体需要继承该类
/// </summary>
/// <typeparam name="T">通常是各种状态的枚举值</typeparam>
public abstract class FsmControllerBase<T, TOwner> : IFsmController<T>
{
    /// <summary>
    /// 内部字段：当前状态
    /// </summary>
    protected FsmStateBase<T, TOwner> currentState;

    /// <summary>
    /// 内部字段：包含全部状态的字典
    /// </summary>
    protected Dictionary<T, FsmStateBase<T, TOwner>> allStates;

    /// <summary>
    /// 获取当前状态
    /// </summary>
    /// <returns></returns>
    public IFsmState<T> GetCurrentState()
    {
        return currentState;
    }

    public abstract void OnInit(TOwner owner);
    public abstract void OnRelease(TOwner owner);

    /// <summary>
    /// 初始化状态机
    /// </summary>
    public virtual void InitState()
    {
        allStates = new Dictionary<T, FsmStateBase<T, TOwner>>();
    }

    /// <summary>
    /// 添加状态
    /// </summary>
    /// <param name="stateType">状态类型</param>
    /// <param name="state">状态实例</param>
    public void AddState(T stateType, FsmStateBase<T, TOwner> state)
    {
        if (allStates.ContainsKey(stateType))
        {
            return;
        }

        allStates[stateType] = state;
    }

    /// <summary>
    /// 获取状态
    /// </summary>
    /// <param name="stateType">状态类型</param>
    /// <returns></returns>
    public IFsmState<T> GetState(T stateType)
    {
        if (allStates.ContainsKey(stateType))
            return allStates[stateType];

        return null;
    }

    /// <summary>
    /// 更新当前状态机(需要从外部调用)
    /// </summary>
    public virtual void UpdateState()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    /// <summary>
    /// 获取当前的状态类型，如果当前状态为空，返回默认值
    /// </summary>
    /// <returns></returns>
    public T GetCurrentStateType()
    {
        if (currentState != null)
        {
            return currentState.GetStateType();
        }
        else
        {
            return default(T);
        }
    }

    public bool HasState(T stateType)
    {
        return allStates.ContainsKey(stateType);
    }

    public T prevStateType;

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="stateType">新的状态类型</param>
    public virtual void SwitchState(T stateType)
    {
        if (!HasState(stateType))
        {
            LogLogger.LogVerbose(LogTag.LOG_Game,$"无目标状态:{stateType.ToString()}");
            return;
        }

        if (currentState != null)
        {
            prevStateType = currentState.GetStateType();
            currentState.Leave();
        }

        currentState = allStates[stateType];
        currentState.Enter();
    }

    /// <summary>
    /// 停止状态机
    /// </summary>
    public void StopStateManager()
    {
        if (currentState != null)
        {
            currentState.Leave();
        }

        currentState = null;
    }
}