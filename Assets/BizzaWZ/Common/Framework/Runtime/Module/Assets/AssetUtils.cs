using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if BIZZA_REAL_WITHDRAW
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class AssetUtils
{
    [Obfuz.ObfuzIgnore]
    public enum E_LoadAssetType
    {
        Resources,
        Addressable,
    }

#if BIZZA_REAL_WITHDRAW
    public static E_LoadAssetType LoadAssetType = E_LoadAssetType.Addressable;
#else
    public static E_LoadAssetType LoadAssetType = E_LoadAssetType.Resources;
#endif

    #region 通用接口
    public static void LoadAsset<T>(string resPath, Action<string, T> callback) where T : Object
    {
#if BIZZA_REAL_WITHDRAW
        if (LoadAssetType == E_LoadAssetType.Resources)
        {
            var res = LoadAssetFromResources<T>(resPath);
            callback?.Invoke(resPath, res);
        }
        else
        {
            var res = LoadAssetFromAddressableSync<T>(resPath);
            callback?.Invoke(resPath, res);
        }
#else
        var res = LoadAssetFromResources<T>(resPath);
        callback?.Invoke(resPath, res);
#endif
    }

    public static T LoadAssetSync<T>(string resPath) where T : Object
    {
#if BIZZA_REAL_WITHDRAW
        if (LoadAssetType == E_LoadAssetType.Resources)
        {
            var res = LoadAssetFromResources<T>(resPath);
            return res;
        }
        else
        {
            var res = LoadAssetFromAddressableSync<T>(resPath);
            return res;
        }
#else
        return LoadAssetFromResources<T>(resPath);
#endif
    }

    public static async UniTask<T> LoadAssetAsync<T>(string resPath) where T : Object
    {
#if BIZZA_REAL_WITHDRAW
        if (LoadAssetType == E_LoadAssetType.Resources)
        {
            var res = LoadAssetFromResources<T>(resPath);
            var asset = await UniTask.FromResult(res);
            return asset;
        }
        else
        {
            // LoadAssetAsync();
            return await LoadAssetFromAddressable<T>(resPath);
        }
#else
        var res = LoadAssetFromResources<T>(resPath);
        return await UniTask.FromResult(res);
#endif
    }

    public static async UniTask<IList<T>> LoadAssetsAsync<T>(string resPath) where T : Object
    {
#if BIZZA_REAL_WITHDRAW
        if (LoadAssetType == E_LoadAssetType.Resources)
        {
            var res = LoadAssetsFromResources<T>(resPath);
            return await UniTask.FromResult(res);
        }
        else
        {
            // LoadAssetAsync();
            return await LoadAssetsFromAddressable<T>(resPath);
        }
#else
        var res = LoadAssetsFromResources<T>(resPath);
        return await UniTask.FromResult(res);
#endif
    }

    #endregion

    #region 加载场景

    public static async void LoadSceneWithBlock(string resPath, Action onLoadCompleted = null,
        bool waitUnloadFinish = false)
    {
        await LoadSceneWithBlockAsync(
            resPath,
            onLoadCompleted == null
                ? null
                : new Func<UniTask>(() =>
                {
                    onLoadCompleted();
                    return UniTask.CompletedTask;
                }),
            waitUnloadFinish);
    }

    public static async UniTask LoadSceneWithBlockAsync(string resPath, Func<UniTask> onLoadCompletedAsync = null,
        bool waitUnloadFinish = false)
    {
        var blockReason = "loadScene:::" + resPath;
        TransparentBlock.AddBlock(blockReason);
        try
        {
            if (waitUnloadFinish)
            {
                await LoadScene("Empty");
            }

            await LoadScene(resPath);

            if (onLoadCompletedAsync != null)
            {
                await onLoadCompletedAsync();
            }
        }
        finally
        {
            TransparentBlock.RemoveBlock(blockReason);
        }
    }

    private static string _prevSceneName = "InitWZ";
#if BIZZA_REAL_WITHDRAW
    public static async UniTask LoadScene(string resPath, LoadSceneMode loadSceneMode = LoadSceneMode.Additive,
        Action<string, SceneInstance> onLoaded = null)
#else
    public static async UniTask LoadScene(string resPath, LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
#endif
    {
#if BIZZA_REAL_WITHDRAW
        RewardItemCollectFlow.CancelAll();
#endif
        await SceneManager.LoadSceneAsync(resPath, loadSceneMode);
        await UnloadCurScene();
        _prevSceneName = resPath;
    }

    public static async UniTask UnloadCurScene()
    {
        if (!string.IsNullOrEmpty(_prevSceneName))
        {
            var scene = SceneManager.GetSceneByName(_prevSceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                LogLogger.LogVerbose(LogTag.LOG_Asset, $"unload scene error:{_prevSceneName}");
                return;
            }
            var handle = SceneManager.UnloadSceneAsync(scene);
            if (handle != null)
            {
                await handle;
            }
        }
        _prevSceneName = "";
    }
    #endregion

    private static T LoadAssetFromResources<T>(string resPath) where T : Object
    {
        var res = Resources.Load<T>(resPath);
#if !BIZZA_REAL_WITHDRAW
        if (res == null)
        {
            string whitePath = ToWhiteResourcesPath(resPath);
            if (!string.Equals(whitePath, resPath, StringComparison.Ordinal))
            {
                res = Resources.Load<T>(whitePath);
            }
        }
#endif
        if (res == null)
        {
            LogLogger.LogVerbose(LogTag.LOG_Asset, $"res load failed:{resPath}");
        }

        return res;
    }

    private static IList<T> LoadAssetsFromResources<T>(string resPath) where T : Object
    {
        var res = Resources.LoadAll<T>(resPath);
#if !BIZZA_REAL_WITHDRAW
        if ((res == null || res.Length == 0))
        {
            string whitePath = ToWhiteResourcesPath(resPath);
            if (!string.Equals(whitePath, resPath, StringComparison.Ordinal))
            {
                res = Resources.LoadAll<T>(whitePath);
            }
        }
#endif
        if (res == null)
        {
            LogLogger.LogVerbose(LogTag.LOG_Asset, $"res load failed:{resPath}");
        }

        return res;
    }

#if !BIZZA_REAL_WITHDRAW
    private static string ToWhiteResourcesPath(string resPath)
    {
        const string uiPanelPrefix = "UIPanel/";
        if (string.IsNullOrEmpty(resPath) || !resPath.StartsWith(uiPanelPrefix, StringComparison.Ordinal))
        {
            return resPath;
        }

        return "Prefabs/UI/" + resPath.Substring(uiPanelPrefix.Length);
    }
#endif

#if BIZZA_REAL_WITHDRAW
    private static T LoadAssetFromAddressableSync<T>(string resPath) where T : Object
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<T>(resPath);
            T res = handle.WaitForCompletion();
            if (res == null)
            {
                LogLogger.LogVerbose(LogTag.LOG_Asset, $"res load failed:{resPath}");
            }
            return res;
        }
        catch (Exception e)
        {
            LogLogger.LogVerbose(LogTag.LOG_Asset, $"[AssetModule] LoadAsset Exception:::{e}");
        }

        return null;
    }

    private static async UniTask<T> LoadAssetFromAddressable<T>(string resPath) where T : Object
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<T>(resPath);
            await handle;
            await UniTask.SwitchToMainThread();
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
        }
        catch (Exception e)
        {
            LogLogger.LogVerbose(LogTag.LOG_Asset, $"[AssetModule] LoadAsset Exception:::{e}");
        }

        return null;
    }

    private static async UniTask<IList<T>> LoadAssetsFromAddressable<T>(string resPath) where T : Object
    {
        try
        {
            var handle = Addressables.LoadAssetsAsync<T>(resPath, null);
            await handle;
            await UniTask.SwitchToMainThread();
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
        }
        catch (Exception e)
        {
            LogLogger.LogVerbose(LogTag.LOG_Asset, $"[AssetModule] LoadAsset Exception:::{e}");
        }

        return null;
    }
#endif
}
