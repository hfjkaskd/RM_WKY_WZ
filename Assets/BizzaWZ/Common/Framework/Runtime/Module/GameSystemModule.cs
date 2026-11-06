using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using UnityEngine;

public class GameSystemModule : BaseGameModule<GameSystemModule>
{
    public override void InitGameModule()
    {
        base.InitGameModule();
        InitSubSystem();
    }

    public override void PreReleaseGameModule()
    {
        base.PreReleaseGameModule();
    }

    public override void ReleaseGameModule()
    {
        base.ReleaseGameModule();
        if (m_subSystems != null)
        {
            for (int i = 0; i < m_subSystems.Count; i++)
            {
                if (m_subSystems[i] == null) continue;
                m_subSystems[i].ReleaseSystem();
            }
        }
    }

    protected override void SetListener(bool addOrRemove)
    {

    }

    #region Sub System

    private readonly List<BaseSubSystem> m_subSystems = new List<BaseSubSystem>();

    public void InitSubSystem()
    {
        m_subSystems.Clear();

        // --- Add SubSystem ---
        //AddSubSystem<GameSystem_Inventory>();
        //AddSubSystem<GameSystem_Lock>();
        //AddSubSystem<RedPointMgrSystem>();

        var menuSystems = CollectAndCreateMenuSystems();
        foreach (var v in menuSystems)
        {
            v.InitSystem();
            m_subSystems.Add(v);
        }
    }

    private List<BaseSubSystem> CollectAndCreateMenuSystems()
    {
        List<BaseSubSystem> menuSystems = new List<BaseSubSystem>();

        // 获取当前程序集
        Assembly assembly = Assembly.GetExecutingAssembly();

        // 获取程序集中的所有类型
        Type[] types = assembly.GetTypes();

        // 筛选出继承自 MenuSystem 的子类
        foreach (Type type in types)
        {
            if (type.IsSubclassOf(typeof(BaseSubSystem)) && !type.IsAbstract)
            {
                // 创建子类的实例
                BaseSubSystem instance = (BaseSubSystem)Activator.CreateInstance(type);
                menuSystems.Add(instance);
                LogLogger.LogVerbose($"自动创建系统:{type.ToString()}");
            }
        }

        return menuSystems;
    }

    void Update()
    {
        for (int i = 0; i < m_subSystems.Count; i++)
        {
            if (m_subSystems[i] == null) continue;
            if (m_subSystems[i] is IUpdate updateSys)
            {
                if (updateSys.Enabled)
                {
                    updateSys.OnUpdate(Time.deltaTime);
                }
            }
        }
    }

    void LateUpdate()
    {
        for (int i = 0; i < m_subSystems.Count; i++)
        {
            if (m_subSystems[i] == null) continue;
            if (m_subSystems[i] is ILateUpdate updateSys)
            {
                if (updateSys.Enabled)
                {
                    updateSys.OnLateUpdate(Time.deltaTime);
                }
            }
        }
    }

    #endregion
}

/// <summary>
/// 游戏子系统，生命周期同Game
/// </summary>
public abstract class BaseSubSystem
{
    /// <summary>
    /// 初始化系统
    /// </summary>
    public abstract void InitSystem();

    /// <summary>
    /// 销毁系统
    /// </summary>
    public abstract void ReleaseSystem();

}

/// <summary>
/// 游戏子系统，单例模式
/// </summary>
public abstract class MenuSystemBase<T> : BaseSubSystem where T : BaseSubSystem
{
    private static T m_instance;

    public static T Instance => m_instance;

    public override void InitSystem()
    {
        m_instance = this as T;
        SetListener(true);
        InternalSetListener(true);
    }

    public override void ReleaseSystem()
    {
        SetListener(false);
        InternalSetListener(false);
    }

    private void InternalSetListener(bool addOrRemove)
    {
        BizzaEventSystem.Set(EventDefine.Frame.DataTableLoaded, OnDataTableLoaded, addOrRemove);
        BizzaEventSystem.Set(EventDefine.Frame.GameDataLoaded, OnPlayerDataLoaded, addOrRemove);
        BizzaEventSystem.Set(EventDefine.Frame.OnNewDayLogin, OnNewDay, addOrRemove);
    }


    /// <summary>
    /// 当玩家数据加载完成
    /// 这个阶段会在数值表加载完成之后
    /// </summary>
    protected virtual void OnPlayerDataLoaded() {}

    /// <summary>
    /// 当数据表加载完成
    /// </summary>
    protected virtual void OnDataTableLoaded() {}

    protected virtual void OnNewDay() {}

    protected virtual void SetListener(bool addOrRemove) {}
}
