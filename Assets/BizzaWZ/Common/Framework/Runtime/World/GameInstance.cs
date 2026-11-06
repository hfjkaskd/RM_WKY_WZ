#define PUBLISH_FOR_DEBUG
using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

public class GameInstance : BaseGameInstance
{
    // public GMTool gmPrefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSceneGameInstanceStarted()
    {
        var gameInstance = FindObjectOfType<GameInstance>();
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] AfterSceneLoad found GameInstance: {gameInstance != null}");
#endif
        if (gameInstance != null)
        {
            gameInstance.EnsureGameModulesInitialized();
        }
    }

    protected override void Awake()
    {
        UniTaskScheduler.UnobservedTaskException += (e) =>
        {
            // 全局捕获未被处理的异常
            Debug.LogError($"未捕获异常: {e}");
        };
        base.Awake();
        // QualitySettings.vSyncCount = 0; 
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(gameObject);
        Input.multiTouchEnabled = true;
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void InitGameModule() 
    {
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] InitGameModule begin");
#endif
        GraphicsSettings.lightsUseLinearIntensity = true;
        GraphicsSettings.lightsUseColorTemperature = true;

        TryAddGameModule<UIModule>(true);
        TryAddGameModule<SaveDataModule>(true);
        TryAddGameModule<SoundManager>(false);
        TryAddGameModule<StatisticModule>(false);
        TryAddGameModule<GameSystemModule>(false);
        TryAddGameModule<PrefabPoolModule>(false);
        TryAddGameModule<TimeModule>(false);

        GameExtensionRegistry.RunGameModuleHooks(this);

        var gamePlayModule = TryAddGameModule<GamePlayModule>(true);
        if (gamePlayModule == null)
        {
            return;
        }

#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] GamePlayModule.GameStart");
#endif
        gamePlayModule.GameStart();

        GameExtensionRegistry.RunStartupHooks();
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] InitGameModule end");
#endif
    }

    private T TryAddGameModule<T>(bool required) where T : BaseGameModule
    {
        try
        {
            T module = InternalAddGameModule<T>();
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] Init module ok: {typeof(T).Name}");
#endif
            return module;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameInstance] Init module failed: {typeof(T).Name}\n{e}");
            if (required)
            {
                throw;
            }

            return null;
        }
    }
}
