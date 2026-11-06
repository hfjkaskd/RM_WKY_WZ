using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Bizza.TokenClientSystem
{
    /// <summary>
    /// AuthConfig 跨平台读取器。
    /// 配置文件位置：Assets/StreamingAssets/AuthConfig.bytes
    /// </summary>
    public static class AuthConfigLoader
    {
        private const string ConfigFileName = "AuthConfig.bytes";
        private static bool loaded;
        private static bool loading;

        public static bool IsLoaded => loaded;
        public static bool IsLoading => loading;

        public static async UniTask<AuthConfigData> LoadAsync(bool forceReload = false)
        {
            if (loaded && !forceReload)
            {
                return AuthConfig.Current;
            }

            if (loading)
            {
                await UniTask.WaitUntil(() => !loading);
                return AuthConfig.Current;
            }

            loading = true;
            try
            {
                string path = GetConfigPath();
                Debug.Log($"开始加载 AuthConfig：{path}");

                using (UnityWebRequest request = UnityWebRequest.Get(path))
                {
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        throw new InvalidOperationException(
                            $"读取 AuthConfig 失败。Path: {path}, Error: {request.error}");
                    }

                    byte[] data = request.downloadHandler?.data;
                    AuthConfigData config = AuthConfigBinarySerializer.Deserialize(data);
                    AuthConfig.Apply(config);
                    loaded = true;

                    Debug.Log(
                        $"AuthConfig 加载成功：WorkerUrl={AuthConfig.WorkerUrl}，" +
                        $"Timeout={AuthConfig.RequestTimeoutSeconds}秒");
                    return AuthConfig.Current;
                }
            }
            catch (Exception exception)
            {
                // 保留原有硬编码默认值，避免配置文件缺失时改变当前行为。
                AuthConfig.ResetToDefaults();
                loaded = true;
                Debug.LogWarning(
                    $"AuthConfig 加载失败，将使用代码默认值：{exception.Message}");
                return AuthConfig.Current;
            }
            finally
            {
                loading = false;
            }
        }

        private static string GetConfigPath()
        {
            return $"{Application.streamingAssetsPath}/{ConfigFileName}";
        }
    }
}
