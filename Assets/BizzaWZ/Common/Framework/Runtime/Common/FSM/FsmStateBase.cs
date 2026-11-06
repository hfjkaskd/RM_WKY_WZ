using UnityEngine;
using System.Collections;


/// <summary>
/// 状态机中状态的基类
/// </summary>
/// <typeparam name="T">通常是各种状态类型的枚举值</typeparam>
public abstract class FsmStateBase<T, TOwner> : IFsmState<T>
{
    /// <summary>
    /// 内部字段：当前的状态枚举
    /// </summary>
    protected readonly T stateType;

    /// <summary>
    /// 内部字段：状态机控制器(相关联的实体对象)
    /// </summary>
    protected readonly FsmControllerBase<T, TOwner> controller;

    /// <summary>
    /// 获取当前状态对应的状态枚举
    /// </summary>
    /// <returns></returns>
    public T GetStateType()
    {
        return stateType;
    }

    /// <summary>
    /// 状态机控制器(相关联的实体对象)
    /// </summary>
    public FsmControllerBase<T, TOwner> Controller
    {
        get { return controller; }
    }

    public FsmStateBase(T stateType, FsmControllerBase<T, TOwner> controller)
    {
        this.stateType = stateType;
        this.controller = controller;
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public virtual void Enter()
    {
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// 离开状态
    /// </summary>
    public virtual void Leave()
    {
    }
}