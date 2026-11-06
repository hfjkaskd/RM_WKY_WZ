using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using UnityEngine.Networking;

[Obfuz.ObfuzIgnore]
/// <summary>
/// 网络检测工具类（静态方法，无需实例化）
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// 异步检测是否有网络连接
    /// </summary>
    /// <param name="timeoutSeconds">总超时时间（秒）</param>
    /// <returns>true:有网络, false:无网络</returns>
    public static async Task<bool> HasNetworkConnectionAsync(float timeoutSeconds = 3f)
    {
        try
        {
            // 1. 快速本地检查
            if (!await QuickLocalCheckAsync())
            {
                Debug.Log("[网络检测] 本地检查: 无网络");
                return false;
            }

            // 2. HTTP 探测（保持原函数名）
            return await CheckWithPingAsync(timeoutSeconds);
        }
        catch (Exception e)
        {
            Debug.LogError($"[网络检测] 检测失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 快速本地网络检查
    /// </summary>
    private static async Task<bool> QuickLocalCheckAsync()
    {
        await Task.Yield();

        // Unity 基础网络状态（非常重要）
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return false;
        }

        // 真机上这个 API 有时不可靠，失败就放过
        try
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return false;
            }
        }
        catch
        {
            // 忽略，继续做 HTTP 检测
        }

        return true;
    }
    
    /// <summary>
    /// 并发检测（内部已从 Ping 改为 HTTP）
    /// </summary>
    private static async Task<bool> CheckWithPingAsync(float timeoutSeconds)
    {
        // 稳定、全球可访问的 HTTP 地址
        string[] testUrls =
        {
            "https://www.cloudflare.com",   // 全球 CDN，国内可访问
            "https://www.microsoft.com",    // 全球稳定
            "https://www.apple.com",        // iOS 网络下非常稳
            "https://www.amazon.com"        // 全球访问性强
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var tasks = testUrls
                .Select(url => PingServerAsync(url, (int)(timeoutSeconds * 1000), cts.Token))
                .ToArray();

            var completedTask = await Task.WhenAny(tasks);

            if (cts.Token.IsCancellationRequested)
                return false;

            bool result = await completedTask;
            cts.Cancel(); // 取消其他检测

            return result;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 单个“Ping”检测（实际为 HTTP 探测）
    /// </summary>
    private static async Task<bool> PingServerAsync(
        string server,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            using (var request = UnityWebRequest.Head(server))
            {
                request.timeout = Mathf.CeilToInt(timeoutMs / 1000f);

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        return false;
                    }

                    await Task.Yield();
                }

                // 能连上服务器即可认为“有网络”
                return request.result == UnityWebRequest.Result.Success
                       || request.responseCode >= 200;
            }
        }
        catch
        {
            return false;
        }
    }
}
