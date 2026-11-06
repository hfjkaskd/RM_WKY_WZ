#if BIZZA_REAL_WITHDRAW
using Bizza.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class FinalRuntimeInstaller
{
    private const string RuntimeObjectPath = "BizzaFinal/[RealWithdraw]";
    private const string RuntimeObjectName = "[RealWithdraw]";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        GameExtensionRegistry.RegisterGameModuleHook(InstallGameModules);
        GameExtensionRegistry.RegisterLoadingProcedureHook(AddFinalLoadingTasks);
        GameExtensionRegistry.RegisterPreloadHook(PreloadFinalAssets);
        GameExtensionRegistry.RegisterTeachEnableProvider(EnableTeach);
        GameExtensionRegistry.RegisterUiBackBlockProvider(IsTeachBlockingUiBack);
        GameExtensionRegistry.RegisterStartupHook(LoadRuntimeObject);
    }

    private static bool EnableTeach()
    {
        return true;
    }

    private static bool IsTeachBlockingUiBack()
    {
        return TeachUtil.IsTeach;
    }

    private static void PreloadFinalAssets()
    {
        AssetUtils.LoadAssetAsync<GameObject>("UIPanel/UI_TeachFinterMove").Forget();
        AssetUtils.LoadAssetAsync<GameObject>("UIPanel/UI_TeachMask").Forget();
        AssetUtils.LoadAssetAsync<GameObject>("UIPanel/UI_TeachTip").Forget();
    }

    private static void AddFinalLoadingTasks(LoadingProcedure procedure)
    {
        procedure.AddTask(new LoadTablesTask());
        procedure.AddTask(new PrewarmActionGraphTask());
    }

    private static void InstallGameModules(GameInstance gameInstance)
    {
        gameInstance.AddGameModule<ActionModule>();
        gameInstance.AddGameModule<Teach01Module>();
    }

    private static void LoadRuntimeObject()
    {
        GameObject prefab = Resources.Load<GameObject>(RuntimeObjectPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Missing final runtime object: {RuntimeObjectPath}");
            return;
        }

        GameInstance gameInstance = BaseGameInstance.Instance as GameInstance;
        if (gameInstance == null)
        {
            Debug.LogWarning($"Missing GameInstance for final runtime object: {RuntimeObjectPath}");
            return;
        }

        GameObject runtimeObject = Object.Instantiate(prefab, gameInstance.transform, false);
        runtimeObject.name = RuntimeObjectName;
    }
}
#endif
