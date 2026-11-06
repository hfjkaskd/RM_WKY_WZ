using System;
using System.Collections.Generic;
using System.Text;


/// <summary>
/// 继承自MonoBehaviour的状态基类
/// </summary>
/// <typeparam name="T">通常是各种状态类型的枚举值</typeparam>
public class FsmStateMonoBase<T> : UnityEngine.MonoBehaviour, IFsmState<T>
{
    /// <summary>
    /// 内部字段：当前的状态枚举
    /// </summary>
    protected readonly T stateType;

    /// <summary>
    /// 内部字段：状态机控制器(相关联的实体对象)
    /// </summary>
    protected readonly FsmControllerBaseMono<T> controller;

    /// <summary>
    /// 获取当前状态对应的状态枚举
    /// </summary>
    /// <returns></returns>
    public T GetStateType()
    {
        return stateType;
    }

    public FsmStateMonoBase(T stateType, FsmControllerBaseMono<T> controller)
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
    /// 离开状态
    /// </summary>
    public virtual void Leave()
    {
    }

    /// <summary>
    /// 更新状态(注意该函数和unity的Update同名)
    /// </summary>
    public virtual void Update()
    {
    }
}