using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


/// <summary>
/// 继承自MonoBehaviour的状态机控制器，使用状态机的实体需要继承该类
/// </summary>
/// <typeparam name="T">通常是各种状态的枚举值</typeparam>
public abstract class FsmControllerBaseMono<T> : MonoBehaviour, IFsmController<T>
{
    /// <summary>
    /// 内部字段：当前的状态
    /// </summary>
    protected FsmStateMonoBase<T> currentState;

    /// <summary>
    /// 内部字段：包含全部状态的字典
    /// </summary>
    protected Dictionary<T, FsmStateMonoBase<T>> allStates;

    /// <summary>
    /// 获取当前状态
    /// </summary>
    /// <returns></returns>
    public IFsmState<T> GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 初始化状态机
    /// </summary>
    public virtual void InitState()
    {
        allStates = new Dictionary<T, FsmStateMonoBase<T>>();
        //add state into the dictionary
        //allStates[T] = new FsmStateBase<T>(T,this);
    }

    /// <summary>
    /// 获取状态
    /// </summary>
    /// <param name="stateType">状态类型</param>
    public IFsmState<T> GetState(T stateType)
    {
        return allStates[stateType];
    }

    /// <summary>
    /// 获取当前状态类型，如果当前状态为空，返回默认值
    /// </summary>
    public T GetCurrrentStateType()
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

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="stateType">新状态类型</param>
    public virtual void SwitchState(T stateType)
    {
        if (currentState != null)
        {
            currentState.Leave();
        }

        currentState = allStates[stateType];
        currentState.Enter();
    }

    /// <summary>
    /// 更新当前状态(需要从外部调用)
    /// </summary>
    public virtual void UpdateState()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }
}