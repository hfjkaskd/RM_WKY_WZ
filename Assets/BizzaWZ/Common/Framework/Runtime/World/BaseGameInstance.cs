#define PUBLISH_FOR_DEBUG

using System.Collections.Generic;
using UnityEngine;

public abstract class BaseGameInstance : MonoBehaviour
{
    private UpdateProcesser m_updateProcesser = new();
    private bool _gameModulesInitialized;
    public static BaseGameInstance Instance;

    protected virtual void Awake()
    {
        Instance = this;
    }

    protected virtual void Start()
    {
        _InitGameModule();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        m_updateProcesser.OnUpdate(deltaTime);
        m_updateProcesser.OnLateUpdate(deltaTime);
    }

    #region Game Module

    private readonly List<BaseGameModule> baseGameModules = new();

    public void EnsureGameModulesInitialized()
    {
        _InitGameModule();
    }

    private void _InitGameModule()
    {
        if (_gameModulesInitialized)
        {
            return;
        }

        _gameModulesInitialized = true;
        InitGameModule();
    }

    protected abstract void InitGameModule();

    protected T InternalAddGameModule<T>() where T : BaseGameModule
    {
        T gameModule = GetComponentInChildren<T>();
        if (!gameModule)
        {
            GameObject module = new GameObject(typeof(T).Name + "_DynamicInstance");
            module.transform.SetParent(transform);
            module.transform.localPosition = Vector3.zero;
            gameModule = module.AddComponent<T>();
        }
        else if (baseGameModules.Contains(gameModule))
        {
            return gameModule;
        }

        baseGameModules.Add(gameModule);
        gameModule.InitGameModule();
        m_updateProcesser.AddUpdater(gameModule);
        return gameModule;
    }


    protected void InternalAddGameModule<T>(T gameModule) where T : BaseGameModule
    {
        foreach (var v in baseGameModules)
        {
            if (v == null) continue;

            if (v.GetType() == typeof(T))
            {
                //  // LogUtil.Warning($"Module already exist:{v.GetType()}");
                return;
            }
        }

        baseGameModules.Add(gameModule);
        gameModule.InitGameModule();
        m_updateProcesser.AddUpdater(gameModule);
    }


    public T AddGameModule<T>() where T : BaseGameModule
    {
        T gameModule = GetComponentInChildren<T>();
        if (!gameModule)
        {
            GameObject module = new GameObject(typeof(T).Name + "_DynamicInstance");
            module.transform.SetParent(transform);
            module.transform.localPosition = Vector3.zero;
            gameModule = module.AddComponent<T>();
            baseGameModules.Add(gameModule);
            gameModule.InitGameModule();
            m_updateProcesser.AddUpdater(gameModule);
        }

        return gameModule;
    }

    public void AddGameModule<T>(T gameModule) where T : BaseGameModule
    {
        if (!baseGameModules.Contains(gameModule))
        {
            baseGameModules.Add(gameModule);
        }

        gameModule.InitGameModule();
        m_updateProcesser.AddUpdater(gameModule);
    }

    #endregion

    private void OnApplicationFocus(bool focus)
    {
#if UNITY_EDITOR
        LogLogger.LogInfo("GameInstance OnApplicationFocus");
#endif
        if (baseGameModules == null)
        {
            return;
        }

        for (int i = 0; i < baseGameModules.Count; i++)
        {
            baseGameModules[i].OnGameFocus(focus);
        }
    }

    private void OnApplicationPause(bool pause)
    {
#if UNITY_EDITOR
        LogLogger.LogInfo("GameInstance OnApplicationPause");
#endif
        if (baseGameModules == null)
        {
            return;
        }

        for (int i = 0; i < baseGameModules.Count; i++)
        {
            baseGameModules[i].OnGamePause(pause);
        }
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        LogLogger.LogInfo("GameInstance OnApplicationQuit");
#endif
        if (World.Current != null)
        {
            World.Current.OnGameQuit();
        }

        if (baseGameModules == null)
        {
            return;
        }

        for (int i = 0; i < baseGameModules.Count; i++)
        {
            baseGameModules[i].OnGameQuit();
        }
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        LogLogger.LogInfo("GameInstance OnDestroy");
#endif
        if (baseGameModules == null)
        {
            return;
        }

        for (int i = 0; i < baseGameModules.Count; i++)
        {
            baseGameModules[i].PreReleaseGameModule();
        }

        for (int i = 0; i < baseGameModules.Count; i++)
        {
            baseGameModules[i].ReleaseGameModule();
        }
    }
}
