#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bizza.Sdk;
using Bizza.Unity.Android;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

[Obfuz.ObfuzIgnore]
public static class HttpUtil
{
    public static long serverTime = 0;
    public static string accountToken;

    private static readonly List<float> HTTP_TIMEOUT_SECONDS = new List<float> { 3f, 5f, 8f };
    private const int MAX_RETRY_COUNT = 2;
    private const int RETRY_DELAY_MS = 300;

    // Android 原生桥兜底超时，防止 Java 层没回调导致 C# 请求永久悬空
    private const float ANDROID_BRIDGE_TIMEOUT_SECONDS = 
    #if BIZZA_HTTP_AD
    600f;
    #else
    8f;
    #endif

    // 图片下载兜底超时
    private const float IMAGE_DOWNLOAD_TIMEOUT_SECONDS = 15f;

    const string ClassName = "com.bangsawan.oceanshine.HttpUtil";
    const string SendHttpMethodName = "SendHTTPInfo";

    /// <summary>
    /// Android Java 回调结果缓存。
    /// 注意：
    /// null 表示已注册但还没有回调；
    /// string.Empty 表示 Java 已回调，但结果为空/失败；
    /// 非空字符串表示 Java 返回了正常内容。
    /// </summary>
    private static Dictionary<int, string> HTTPReply = new();
    private static readonly HashSet<string> LoggedDeviceEnvironments = new();

    private static void LogDeviceEnvironmentOnce(string methodName, string environment)
    {
        string key = methodName + "|" + environment;
        if (LoggedDeviceEnvironments.Add(key))
        {
            LogLogger.LogDeviceEnvironment(
                $"功能=HTTP请求(HttpUtil); 方法={methodName}; 环境={environment}");
        }
    }

    private static int HttpId;
    private static DateTime CurrentTime => DateTime.Now;

    private static readonly JsonSerializerSettings JsonSetting = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static HttpDataConfig HttpDataConfig
    {
        get
        {
            if (ChannelConfig.Instance == null)
            {
                LogLogger.LogVerbose(LogTag.LOG_HTTP, "ChannelConfig.Instance is null");
                return null;
            }

            return ChannelConfig.Instance.httpConfig;
        }
    }

    #region Http Request Params

    private static readonly Dictionary<string, object> requestParams = new();

    public static void AddParam(string key, object data) => requestParams[key] = data;

    private static string SerializeRequestParams() => JsonConvert.SerializeObject(requestParams);

    #endregion

    #region Public APIs

    public static void RequestToServer<T>(
        string path,
        string requestJson,
        Action<FailHttpResponse<T>> response,
        bool block = false,
        float delay = 0,
        string method = "POST")
    {
        LogLogger.LogHttpInfo("发送", path, requestJson);
        LogLogger.LogInfo(LogTag.LOG_HTTP, $"向 {path}； 数据为：{requestJson}； 时间为 {CurrentTime}");

        byte[] bodyRaw = BuildEncryptedBody(requestJson);

        RequestToServerCoroutine(path, bodyRaw, response, delay, block, method)
            .Forget(ex => LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Forget exception: {ex}"));
    }

    public static void RequestToServer(
        string path,
        string requestJson,
        Action<bool, string> response,
        bool block = false,
        float delay = 0,
        string method = "POST")
    {
        LogLogger.LogHttpInfo("发送", path, requestJson);
        LogLogger.LogInfo(LogTag.LOG_HTTP, $"向 {path}； 数据为：{requestJson}； 时间为 {CurrentTime}");

        byte[] bodyRaw = BuildEncryptedBody(requestJson);

        RequestToServerCoroutine(path, bodyRaw, response, delay, block, method)
            .Forget(ex => LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Forget exception: {ex}"));
    }

    #endregion

    #region Coroutine - Generic (T)

    private static async UniTask RequestToServerCoroutine<T>(
        string path,
        byte[] bodyRaw,
        Action<FailHttpResponse<T>> response,
        float delay,
        bool block,
        string method)
    {
        string url = BuildUrl(path);
        LogLogger.LogVerbose(LogTag.LOG_HTTP, $"URL ::: {url}");

        void Reply(bool success, T data, string msg, string code)
        {
            response?.Invoke(new FailHttpResponse<T>
            {
                success = success,
                data = data,
                message = msg,
                errorCode = code
            });
        }

        void ReplyDefault(string msg = "Network Error", string code = "-1")
        {
            Reply(false, default, msg, code);
        }
#if !COMMONGAME
        if (block) LoadingBlock.AddBlock(nameof(HttpUtil));
#endif

        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            LogDeviceEnvironmentOnce("RequestToServerCoroutine<T>", "安卓真机");
            int id = HttpId++;

            // null 表示等待 Java 回调中
            HTTPReply[id] = null;

            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"URL ::: {url} + ; id :{id} ; ");

            JavaBridgeUtils.CallStaticMethodCacheClass(
                ClassName,
                SendHttpMethodName,
                url,
                ToSByteArray(bodyRaw),
                id,
                delay
            );

            string responseJson = await WaitAndroidBridgeReplyOrTimeout(id, ANDROID_BRIDGE_TIMEOUT_SECONDS);

            if (responseJson == null)
            {
                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Android HTTP timeout: url={url}, id={id}");
                ReplyDefault("Network Timeout", "NETWORK_TIMEOUT");
                return;
            }

            if (string.IsNullOrEmpty(responseJson))
            {
                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Android HTTP empty response: url={url}, id={id}");
                ReplyDefault("Network Error", "NETWORK_ERROR");
                return;
            }

            string respOuterJson = responseJson;
#endif

#if UNITY_EDITOR
            LogDeviceEnvironmentOnce("RequestToServerCoroutine<T>", "编辑器");
            if (delay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(delay),
                    ignoreTimeScale: true);
            }
            var request = await SendWithRetry(() => CreateRequest(url, method, bodyRaw), url, MAX_RETRY_COUNT);

            if (request == null)
            {
                ReplyDefault("Network Error", "NETWORK_ERROR");
                return;
            }

            using (request)
            {
                if (!IsNetworkSuccess(request))
                {
                    LogLogger.LogVerbose(LogTag.LOG_HTTP, $"HTTP Network Error: {request.error}");
                    ReplyDefault(request.error, "NETWORK_ERROR");
                    return;
                }

                string respOuterJson = request.downloadHandler.text;
#endif

                // ===== 解析 outer =====
                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"HTTP 返回 raw: url={respOuterJson}");

                if (!TryParseOuter(respOuterJson, out HttpResponse<string> outer))
                {
                    ReplyDefault("Invalid Response", "PARSE_ERROR");
                    return;
                }

                serverTime = outer.st;

                // 保持原语义：业务失败也当 success=true，但 data=default
                if (outer.code != "200")
                {
                    LogLogger.LOGHTTPInfo($"HTTP!=200: {outer.Message} - {outer.code}");
                    Reply(true, default, outer.Message, outer.code);
                    return;
                }

                // ===== 解密 =====
                if (!TryDecryptOuterData(outer.data, out string plainJson))
                {
                    ReplyDefault("Decrypt Error", "DECRYPT_ERROR");
                    return;
                }

                LogLogger.LogHttpInfo("接收", path, plainJson);
                LogLogger.LogInfo(LogTag.LOG_HTTP, "服务器最后结果 " + url + " --- " + plainJson);

                try
                {
                    // ===== 反序列化 =====
                    T dataObj = JsonConvert.DeserializeObject<T>(plainJson, JsonSetting);
                    LogLogger.LogInfo(LogTag.LOG_HTTP, $"反序列化成功: {JsonConvert.SerializeObject(dataObj)}");
                    Reply(true, dataObj, outer.Message, outer.code);
                }
                catch (Exception e)
                {
                    LogLogger.LOGHTTPInfo(
                        $"HTTP回调异常: {typeof(T).FullName}; 异常类型: {e.GetType().FullName}; 异常信息: {e.Message}; JSON 内容: {plainJson}"
                    );
                    LogLogger.LOGHTTPInfo($"堆栈: {e.StackTrace}");

                    ReplyDefault("Deserialize Error", "DESERIALIZE_ERROR");
                }

#if UNITY_EDITOR
            }
#endif
        }
        finally
        {
#if !COMMONGAME
            if (block) LoadingBlock.RemoveBlock(nameof(HttpUtil));
#endif
        }
    }

    #endregion

    #region Coroutine - Raw JSON

    private static async UniTask RequestToServerCoroutine(
        string path,
        byte[] bodyRaw,
        Action<bool, string> response,
        float delay,
        bool block,
        string method)
    {
        string url = BuildUrl(path);
        LogLogger.LogVerbose(LogTag.LOG_HTTP, $"URL ::: {url}");

#if !COMMONGAME
        if (block) LoadingBlock.AddBlock(nameof(HttpUtil));
#endif

        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            LogDeviceEnvironmentOnce("RequestToServerCoroutine(raw)", "安卓真机");
            int id = HttpId++;

            // null 表示等待 Java 回调中
            HTTPReply[id] = null;

            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"URL ::: {url} + ; id :{id} ; ");

            JavaBridgeUtils.CallStaticMethodCacheClass(
                ClassName,
                SendHttpMethodName,
                url,
                ToSByteArray(bodyRaw),
                id,
                delay
            );

            string responseJson = await WaitAndroidBridgeReplyOrTimeout(id, ANDROID_BRIDGE_TIMEOUT_SECONDS);

            if (responseJson == null)
            {
                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Android HTTP timeout: url={url}, id={id}");
                response?.Invoke(false, null);
                return;
            }

            if (string.IsNullOrEmpty(responseJson))
            {
                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Android HTTP empty response: url={url}, id={id}");
                response?.Invoke(false, null);
                return;
            }

            string respOuterJson = responseJson;
#endif

#if UNITY_EDITOR
            LogDeviceEnvironmentOnce("RequestToServerCoroutine(raw)", "编辑器");
            if (delay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(delay),
                    ignoreTimeScale: true);
            }
            var request = await SendWithRetry(() => CreateRequest(url, method, bodyRaw), url, MAX_RETRY_COUNT);

            if (request == null)
            {
                response?.Invoke(false, null);
                return;
            }

            using (request)
            {
                if (!IsNetworkSuccess(request))
                {
                    LogLogger.LogVerbose(LogTag.LOG_HTTP, $"HTTP Network Error: {request.error}");
                    response?.Invoke(false, null);
                    return;
                }

                string respOuterJson = request.downloadHandler.text;
#endif

                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"HTTP raw: {respOuterJson}");

                if (!TryParseOuter(respOuterJson, out HttpResponse<string> outer))
                {
                    response?.Invoke(false, null);
                    return;
                }

                serverTime = outer.st;

                // 保持原语义：业务失败 => false,null
                if (outer.code != "200")
                {
                    response?.Invoke(false, null);
                    return;
                }

                if (!TryDecryptOuterData(outer.data, out string plainJson))
                {
                    response?.Invoke(false, null);
                    return;
                }

                response?.Invoke(true, plainJson);

#if UNITY_EDITOR
            }
#endif
        }
        finally
        {
#if !COMMONGAME
            if (block) LoadingBlock.RemoveBlock(nameof(HttpUtil));
#endif
        }
    }

    #endregion

    #region Android Bridge Timeout

    /// <summary>
    /// 等待 Android Java 层回调。
    /// 返回 null 表示超时；
    /// 返回 string.Empty 表示 Java 回调了空内容；
    /// 返回非空字符串表示收到响应。
    /// </summary>
    private static async UniTask<string> WaitAndroidBridgeReplyOrTimeout(int id, float timeoutSeconds)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        bool cancelled = await UniTask.WaitUntil(
            () => HTTPReply.TryGetValue(id, out var value) && value != null,
            cancellationToken: cts.Token
        ).SuppressCancellationThrow();

        if (cancelled)
        {
            HTTPReply.Remove(id);
            return null;
        }

        if (!HTTPReply.TryGetValue(id, out string json))
        {
            return null;
        }

        HTTPReply.Remove(id);
        return json;
    }

    #endregion

    #region Retry + Timeout Core

    /// <summary>
    /// 发送请求：每次尝试都带超时控制，最多重试 maxRetry 次。
    /// 成功则返回那一次成功的 request，失败返回 null。
    /// </summary>
    private static async UniTask<UnityWebRequest> SendWithRetry(
        Func<UnityWebRequest> requestFactory,
        string url,
        int maxRetry)
    {
        for (int attempt = 1; attempt <= maxRetry; attempt++)
        {
            var request = requestFactory();

            int timeoutIndex = Mathf.Clamp(attempt - 1, 0, HTTP_TIMEOUT_SECONDS.Count - 1);
            float time = HTTP_TIMEOUT_SECONDS[timeoutIndex];

            bool sentOk = await SendOnceWithTimeout(request, url, time);

            // sentOk=false 表示：超时/异常
            // sentOk=true 但 result!=Success 表示：网络层失败
            if (sentOk && request.result == UnityWebRequest.Result.Success)
            {
                return request;
            }

            LogLogger.LogVerbose(
                LogTag.LOG_HTTP,
                $"HTTP attempt {attempt}/{maxRetry} failed: url={url}, result={request.result}, error={request.error}"
            );

            request.Dispose();

            if (attempt < maxRetry)
            {
                await UniTask.Delay(RETRY_DELAY_MS);
            }
            else
            {
#if !COMMONGAME
                UIUtils.ShowTips(LanguageUtils.GetText("Tips_NetworkError"));
#endif
                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"重试超出次数 - 失败 failed: url={url}");
            }
        }

        return null;
    }

    /// <summary>
    /// 只发送一次请求，并带超时控制。
    /// </summary>
    private static async UniTask<bool> SendOnceWithTimeout(UnityWebRequest request, string url, float time)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(time));

        try
        {
            await request.SendWebRequest().ToUniTask(cancellationToken: cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"HTTP Timeout ({time}s): {url}");
            request.Abort();
            return false;
        }
        catch (UnityWebRequestException e)
        {
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"UnityWebRequestException: {e.Message} url={url}");
            return false;
        }
        catch (Exception e)
        {
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Request Exception: {e} url={url}");
            return false;
        }
    }

    #endregion

    #region Helpers

    private static string BuildUrl(string path)
    {
        var domain = ChannelConfig.Instance.httpConfig.domain.TrimEnd('/');
        var p = path?.TrimStart('/') ?? string.Empty;
        return $"{domain}/{p}";
    }

    private static UnityWebRequest CreateRequest(string url, string method, byte[] bodyRaw)
    {
        var request = new UnityWebRequest(url, method)
        {
            downloadHandler = new DownloadHandlerBuffer(),
            uploadHandler = new UploadHandlerRaw(bodyRaw ?? Array.Empty<byte>())
        };

        request.SetRequestHeader("X-Bundle", Application.identifier);

        if (!string.IsNullOrEmpty(accountToken))
        {
            request.SetRequestHeader("access_token", accountToken);
        }

        return request;
    }

    private static bool IsNetworkSuccess(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.Success;
    }

    private static bool TryParseOuter(string json, out HttpResponse<string> outer)
    {
        try
        {
            outer = JsonConvert.DeserializeObject<HttpResponse<string>>(json, JsonSetting);
            return true;
        }
        catch (Exception e)
        {
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"Outer JSON 解析失败: {e}; json={json}");
            outer = default;
            return false;
        }
    }

    private static byte[] BuildEncryptedBody(string plainJson)
    {
        byte[] cipherBytes = EncryptionUtil.EncryptByXXTeA(
            plainJson,
            ChannelConfig.Instance.httpConfig.aes_key
        );

        string signBase64 = Convert.ToBase64String(cipherBytes);

        var wrapper = new BaseRequestDto
        {
            sign = signBase64
        };

        string wrapperJson = JsonConvert.SerializeObject(wrapper);
        return Encoding.UTF8.GetBytes(wrapperJson);
    }

    private static bool TryDecryptOuterData(string base64Cipher, out string plainJson)
    {
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(base64Cipher);
            plainJson = EncryptionUtil.DecryptByXXTeA(
                cipherBytes,
                ChannelConfig.Instance.httpConfig.aes_key
            );

            return true;
        }
        catch (Exception e)
        {
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"解密失败: {e}");
            plainJson = null;
            return false;
        }
    }

    #endregion

    #region DTOs

    [Serializable]
    public struct HttpResponse<T>
    {
        public string code;
        public string msg;
        public string message;
        public string uid;
        public long st;
        public T data;

        public string Message => !string.IsNullOrEmpty(message) ? message : msg;
    }

    [Serializable]
    public class BaseRequestDto
    {
        public string sign;
    }

    #endregion

    #region 图片下载

    /// <summary>
    /// 下载图片
    /// </summary>
    public static void RequestDownloadRawImage(string imgPath, Action<Texture> onDownloadFinish)
    {
        DownloadingRawImage(imgPath, onDownloadFinish).Forget();
    }

    public static async UniTask DownloadingRawImage(string imgPath, Action<Texture> onDownloadFinish)
    {
        using UnityWebRequest request = new UnityWebRequest(imgPath);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(IMAGE_DOWNLOAD_TIMEOUT_SECONDS));

        DownloadHandlerTexture textDownload = new DownloadHandlerTexture(true);
        request.downloadHandler = textDownload;

        try
        {
            await request.SendWebRequest().ToUniTask(cancellationToken: cts.Token);

            if (request.result == UnityWebRequest.Result.Success)
            {
                onDownloadFinish?.Invoke(textDownload.texture);
            }
            else
            {
                LogLogger.LogVerbose(LogTag.LOG_HTTP, $"下载图片失败: {request.error}");
                onDownloadFinish?.Invoke(null);
            }
        }
        catch (OperationCanceledException)
        {
            request.Abort();
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"下载图片超时: {imgPath}");
            onDownloadFinish?.Invoke(null);
        }
        catch (Exception ex)
        {
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"下载图片时发生异常: {ex.Message}");
            onDownloadFinish?.Invoke(null);
        }
    }

    #endregion

    #region Android Callback

    public static void LoginHTTPDict(int id, string json)
    {
        if (HTTPReply.ContainsKey(id))
        {
            // json 为 null 时转成 string.Empty，表示 Java 已回调但内容为空
            HTTPReply[id] = json ?? string.Empty;
        }
        else
        {
            // 超时后 Java 才回调属于正常迟到回调，不应该打 Error
            LogLogger.LogVerbose(LogTag.LOG_HTTP, $"HTTP late reply ignored, id={id}");
        }
    }

    static sbyte[] ToSByteArray(byte[] bytes)
    {
        var sbytes = new sbyte[bytes.Length];
        Buffer.BlockCopy(bytes, 0, sbytes, 0, bytes.Length);
        return sbytes;
    }

    #endregion
}

[Serializable]
public struct FailHttpResponse<T>
{
    public bool success;
    public T data;
    public string message;
    public string errorCode;
}
#endif
