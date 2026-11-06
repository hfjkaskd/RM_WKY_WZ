using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Bizza.Sdk
{
    /// <summary>
    /// ChannelConfig 跨平台读取器。
    ///
    /// 支持：
    ///     Unity Editor
    ///     Android
    ///     iOS
    ///
    /// 配置文件：
    ///     StreamingAssets/ChannelConfig.bytes
    /// </summary>
    public static class ChannelConfigLoader
    {
        private const string CONFIG_FILE_NAME =
            "ChannelConfig.bytes";


        /// <summary>
        /// 是否正在加载
        /// </summary>
        public static bool IsLoading
        {
            get;
            private set;
        }


        /// <summary>
        /// 加载 ChannelConfig。
        ///
        /// 加载成功后：
        ///
        ///     ChannelConfig.Instance
        ///
        /// 会自动赋值。
        /// </summary>
        public static async UniTask<ChannelConfig> LoadAsync()
        {
            if (IsLoading)
            {
                Debug.LogWarning(
                    "ChannelConfig 正在加载中，请不要重复调用。");

                return ChannelConfig.Instance;
            }


            IsLoading = true;


            try
            {
                // =========================================
                // StreamingAssets 路径
                // =========================================

                string path =
                    GetConfigPath();


                Debug.Log(
                    $"开始加载 ChannelConfig：{path}");


                // =========================================
                // UnityWebRequest
                //
                // Editor：
                //     file://...
                //
                // Android：
                //     jar:file://...
                //
                // iOS：
                //     file://...
                //
                // 统一处理
                // =========================================

                using UnityWebRequest request =
                    UnityWebRequest.Get(path);


                await request.SendWebRequest();


                // =========================================
                // 检查错误
                // =========================================

                if (request.result !=
                    UnityWebRequest.Result.Success)
                {
                    throw new Exception(
                        $"读取 ChannelConfig 失败。\n" +
                        $"Path: {path}\n" +
                        $"Error: {request.error}");
                }


                // =========================================
                // 获取二进制
                // =========================================

                byte[] data =
                    request.downloadHandler.data;


                if (data == null ||
                    data.Length == 0)
                {
                    throw new Exception(
                        "ChannelConfig.bytes 内容为空。");
                }


                Debug.Log(
                    $"ChannelConfig 二进制读取成功，" +
                    $"Size: {data.Length} Bytes");


                // =========================================
                // 自定义反序列化
                // =========================================

                ChannelConfig config =
                    ChannelConfigBinarySerializer.Deserialize(
                        data);


                if (config == null)
                {
                    throw new Exception(
                        "ChannelConfig 反序列化结果为空。");
                }


                // =========================================
                // 设置全局 Instance
                // =========================================

                ChannelConfig.Instance =
                    config;


                Debug.Log(
                    "ChannelConfig 加载成功");


                return config;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"ChannelConfig 加载失败：\n{e}");

                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }


        /// <summary>
        /// 获取 StreamingAssets 中的配置路径。
        /// </summary>
        private static string GetConfigPath()
        {
            return
                $"{Application.streamingAssetsPath}/" +
                CONFIG_FILE_NAME;
        }
    }
}