using System.ComponentModel;
using Obfuz;
using UnityEngine;

/// <summary>
/// 游戏模块
/// 游戏模块提供非业务层的功能
/// 例如：资源管理，声音控制，数据表，玩家数据，网络链接，作弊等
/// 理论上游戏模块可以跨项目通用
/// </summary>
[ObfuzIgnore]
public abstract class BaseGameModule : MonoBehaviour
{
    /// <summary>
    /// 初始化游戏模块
    /// </summary>
    public virtual void InitGameModule()
    {
        SetListener(true);
    }

    public virtual void ReleaseGameModule()
    {
        SetListener(false);
    }

    protected virtual void SetListener(bool addOrRemove) {}

    /// <summary>
    /// 延迟初始化
    /// </summary>
    public virtual void LateInitGameModule()
    {
    }

    /// <summary>
    /// 当游戏焦点变化时，用于处理，游戏切换后台的情况
    /// </summary>
    public virtual void OnGameFocus(bool focus)
    {
    }

    /// <summary>
    /// 游戏暂停
    /// </summary>
    public virtual void OnGamePause(bool pause)
    {
    }

    /// <summary>
    /// 游戏退出，他的时机会比Release早
    /// </summary>
    public virtual void OnGameQuit()
    {
    }

    /// <summary>
    /// 预卸载游戏
    /// </summary>
    public virtual void PreReleaseGameModule()
    {
    }

}

/// <summary>
/// 游戏模块的单例模式
/// </summary>
[ObfuzIgnore]
public abstract class BaseGameModule<T> : BaseGameModule where T : BaseGameModule
{
    public static T Instance { get; private set; }
    public bool autoInit = false;

    private void Awake()
    {
        Instance = this as T;
    }

    private void Start()
    {
        Debug.Log($"[{GetType().Name}]模块初始化");
        /*if (autoInit && Instance)
        {
            GameInstance.Instance.AddGameModule(this);
        }*/
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}