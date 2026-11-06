using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Bizza.UserInformationVerification;
using LogLogger = Bizza.UserInformationVerification.AuthLog;

namespace Bizza.TokenClientSystem
{
    [Serializable]
    public sealed class AuthRequest
    {
        public string requestId;
        public string appId;
        public string packageName;
        public string authToken;
        public string deviceInfoJson;
        public string playIntegrityToken;
    }
    [Serializable]
    public sealed class AuthResponse
    {
        public bool allow;
        public string token;
        public string publicReason;
        public string debugInfo;
        public JToken extension;
    }
    [Serializable]
    public sealed class AuthResult
    {
        public bool Allow { get; private set; }
        public bool IsNetworkError { get; private set; }
        public bool HasServerResponse { get; private set; }
        public string RequestId { get; private set; }
        public string PublicReason { get; private set; }
        public string DebugInfo { get; private set; }
        public string Error { get; private set; }

        public static AuthResult Allowed(string requestId, string token)
        {
            return new AuthResult
            {
                Allow = true,
                RequestId = requestId ?? string.Empty,
                PublicReason = string.Empty,
                DebugInfo = string.Empty,
                Error = string.Empty,
            };
        }

        public static AuthResult Denied(string requestId, string publicReason, string debugInfo)
        {
            return new AuthResult
            {
                Allow = false,
                HasServerResponse = true,
                RequestId = requestId ?? string.Empty,
                PublicReason = publicReason ?? string.Empty,
                DebugInfo = debugInfo ?? string.Empty,
                Error = string.Empty,
            };
        }

        public static AuthResult NetworkFailure(string requestId, string error)
        {
            return new AuthResult
            {
                Allow = false,
                IsNetworkError = true,
                RequestId = requestId ?? string.Empty,
                PublicReason = "SERVER_ERROR",
                DebugInfo = string.Empty,
                Error = error ?? string.Empty,
            };
        }
    }

    public sealed class AuthService
    {
        private readonly DeviceInfoCollector deviceInfoCollector;

        public AuthService(DeviceInfoCollector deviceInfoCollector = null)
        {
            this.deviceInfoCollector = deviceInfoCollector ?? new DeviceInfoCollector();
        }

        public async UniTask<AuthResult> AuthorizeAsync(CancellationToken cancellationToken = default)
        {
            string requestId = Guid.NewGuid().ToString();
            string oldToken = PlayerPrefs.GetString(AuthConfig.AuthTokenKey, string.Empty);
            LogLogger.LogIP($"[授权服务] 授权流程开始：requestId={requestId}，已有Token={!string.IsNullOrWhiteSpace(oldToken)}，包名={UserInformationVerificationRuntime.GetPackageName()}");

            AuthDeviceInfo deviceInfo;
            try
            {
                LogLogger.LogIP($"[授权服务] 开始采集设备信息：requestId={requestId}");
                deviceInfo = await deviceInfoCollector.CollectAsync(cancellationToken);
                LogLogger.LogIP($"[授权服务] 设备信息采集完成：requestId={requestId}，Google ID存在={!string.IsNullOrWhiteSpace(deviceInfo?.googleId)}，Android ID存在={!string.IsNullOrWhiteSpace(deviceInfo?.androidId)}，VPN连接={deviceInfo?.IsVpnConnected}");
                LogLogger.LogIP($"[授权服务] 地区信息：requestId={requestId}，Unity地区={deviceInfo?.UnityRegionCode}，原生语言区域={deviceInfo?.NativeLocale}，原生国家={deviceInfo?.NativeLocaleCountry}，原生国家代码={deviceInfo?.NativeCountryCode}，Unity时区={deviceInfo?.UnityTimeZoneId}，原生时区={deviceInfo?.NativeTimeZoneId}");
                LogLogger.LogIP($"[授权服务] 网络地区信息：requestId={requestId}，SIM国家={deviceInfo?.SimCountryIso}，网络国家={deviceInfo?.NetworkCountryIso}，SIM运营商={deviceInfo?.SimOperator}，网络运营商={deviceInfo?.NetworkOperator}，运营商名称={deviceInfo?.NetworkOperatorName}，VPN连接={deviceInfo?.IsVpnConnected}，系统语言={Application.systemLanguage}，运行平台={Application.platform}");
            }
            catch (OperationCanceledException)
            {
                LogLogger.LogIP($"[授权服务] 设备信息采集被取消：requestId={requestId}");
                return AuthResult.NetworkFailure(requestId, "request canceled");
            }
            catch (Exception exception)
            {
                LogLogger.LogIP($"[授权服务] 设备信息采集失败：requestId={requestId}，错误={exception.Message}");
                return AuthResult.NetworkFailure(requestId, exception.Message);
            }

            string deviceInfoJson = DeviceInfoCollector.Serialize(deviceInfo);
            string playIntegrityToken = await GeneratePlayIntegrityToken(cancellationToken);
            LogLogger.LogIP($"[授权服务] Play Integrity Token 生成完成：requestId={requestId}，长度={playIntegrityToken?.Length ?? 0}");

            AuthRequest requestBody = new AuthRequest
            {
                requestId = requestId,
                appId = AuthConfig.AppId,
                packageName = UserInformationVerificationRuntime.GetPackageName(),
                authToken = oldToken ?? string.Empty,
                deviceInfoJson = deviceInfoJson,
                playIntegrityToken = playIntegrityToken,
            };

            string json = JsonConvert.SerializeObject(requestBody);
            LogLogger.LogIP($"[授权服务] 授权请求已组装：requestId={requestId}，appId={requestBody.appId}，包名={requestBody.packageName}，请求长度={json.Length}，设备信息长度={deviceInfoJson.Length}，包含旧Token={!string.IsNullOrWhiteSpace(requestBody.authToken)}，包含PlayIntegrityToken={!string.IsNullOrWhiteSpace(playIntegrityToken)}");
            LogLogger.LogIP($"[授权服务] 上报请求内容（设备标识和Token已脱敏，deviceInfoJson为结构化展示）：{BuildRequestLogContent(requestBody)}");
            AuthResponse response;

            try
            {
                LogLogger.LogIP($"[授权服务] 开始发送授权请求：requestId={requestId}，超时时间={AuthConfig.RequestTimeoutSeconds}秒");
                response = await PostAsync(json, requestId, cancellationToken);
                LogLogger.LogIP(response == null
                    ? $"[授权服务] 服务端响应为空：requestId={requestId}"
                    : $"[授权服务] 已收到服务端响应：requestId={requestId}，allow={response.allow}，包含Token={!string.IsNullOrWhiteSpace(response.token)}，原因={NormalizeReason(response.publicReason)}");
                if (response != null)
                {
                    bool hasDebugInfo = !string.IsNullOrWhiteSpace(response.debugInfo);
                    LogLogger.LogIP($"[授权服务] 服务端诊断信息状态：requestId={requestId}，包含debugInfo={hasDebugInfo}，debugInfo开关={AuthConfig.EnableDebugResponse}，debugInfo长度={response.debugInfo?.Length ?? 0}");
                    if (AuthConfig.EnableDebugResponse && hasDebugInfo)
                    {
                        LogLogger.LogIP($"[授权服务] 服务端debugInfo：requestId={requestId}，内容={response.debugInfo}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogLogger.LogIP($"[授权服务] 授权请求被取消：requestId={requestId}");
                return AuthResult.NetworkFailure(requestId, "request canceled");
            }
            catch (Exception exception)
            {
                LogLogger.LogIP($"[授权服务] 授权请求失败：requestId={requestId}，错误={exception.Message}");
                return AuthResult.NetworkFailure(requestId, exception.Message);
            }

            if (response == null)
            {
                LogLogger.LogIP($"[授权服务] 服务端响应无效：requestId={requestId}");
                return AuthResult.NetworkFailure(requestId, "invalid server response");
            }

            if (!response.allow)
            {
                PlayerPrefs.DeleteKey(AuthConfig.AuthTokenKey);
                PlayerPrefs.Save();

                string publicReason = NormalizeReason(response.publicReason);
                string debugInfo = AuthConfig.EnableDebugResponse ? response.debugInfo : string.Empty;
                LogLogger.LogIP($"[授权服务] 服务端拒绝授权：requestId={requestId}，原因={publicReason}");
                LogLogger.LogIP($"[授权服务] 拒绝授权后已清理本地Token：requestId={requestId}");
                return AuthResult.Denied(requestId, publicReason, debugInfo);
            }

            if (!string.IsNullOrWhiteSpace(response.token))
            {
                PlayerPrefs.SetString(AuthConfig.AuthTokenKey, response.token);
                PlayerPrefs.Save();
                LogLogger.LogIP($"[授权服务] 新Token已保存：requestId={requestId}");
            }
            else
            {
                LogLogger.LogIP($"[授权服务] 服务端允许授权，但没有返回新Token：requestId={requestId}");
            }

            LogLogger.LogIP($"[授权服务] 服务端允许授权：requestId={requestId}");
            return AuthResult.Allowed(requestId, response.token);
        }

        public async UniTask<string> GeneratePlayIntegrityToken(
            CancellationToken cancellationToken = default)
        {
            string token = await UserInformationVerificationRuntime
                .PlayIntegrityTokenProvider
                .GetTokenAsync(cancellationToken);
            LogLogger.LogIP($"[授权服务] Play Integrity Token 生成完成，长度={token?.Length ?? 0}");
            return token ?? string.Empty;
        }

        private static async UniTask<AuthResponse> PostAsync(
            string json,
            string requestId,
            CancellationToken cancellationToken)
        {
            using (UnityWebRequest request = new UnityWebRequest(AuthConfig.WorkerUrl, UnityWebRequest.kHttpVerbPOST))
            {
                LogLogger.LogIP($"[授权服务] HTTP请求已创建：requestId={requestId}，地址={AuthConfig.WorkerUrl}");
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Max(1, AuthConfig.RequestTimeoutSeconds);
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                try
                {
                    LogLogger.LogIP($"[授权服务] HTTP请求发送中：requestId={requestId}");
                    await request.SendWebRequest().WithCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    LogLogger.LogIP($"[授权服务] HTTP请求被取消：requestId={requestId}");
                    request.Abort();
                    throw;
                }

                string responseText = request.downloadHandler?.text ?? string.Empty;
                LogLogger.LogIP($"[授权服务] HTTP响应已收到：requestId={requestId}，结果={request.result}，状态码={request.responseCode}，响应长度={responseText.Length}");
                LogLogger.LogIP($"[授权服务] 服务端返回内容（Token已脱敏）：requestId={requestId}，内容={BuildResponseLogContent(responseText)}");
                if (request.result != UnityWebRequest.Result.Success &&
                    request.result != UnityWebRequest.Result.ProtocolError)
                {
                    LogLogger.LogIP($"[授权服务] HTTP请求失败：requestId={requestId}，错误={request.error}");
                    throw new InvalidOperationException(
                        string.IsNullOrEmpty(request.error)
                            ? $"HTTP request failed ({request.responseCode})"
                            : request.error);
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    LogLogger.LogIP($"[授权服务] HTTP响应内容为空：requestId={requestId}");
                    throw new InvalidOperationException("empty server response");
                }

                try
                {
                    AuthResponse response = JsonConvert.DeserializeObject<AuthResponse>(NormalizeJson(responseText));
                    LogLogger.LogIP(response == null
                        ? $"[授权服务] HTTP响应解析结果为空：requestId={requestId}"
                        : $"[授权服务] HTTP响应解析完成：requestId={requestId}，允许授权={response.allow}，包含Token={!string.IsNullOrWhiteSpace(response.token)}，原因={NormalizeReason(response.publicReason)}");
                    return response;
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"server response parse failed, requestId={requestId}: {exception.Message}",
                        exception);
                }
            }
        }

        private static string NormalizeReason(string publicReason)
        {
            string reason = (publicReason ?? string.Empty).Trim().ToUpperInvariant();
            switch (reason)
            {
                case "VPN":
                case "REGION":
                case "APP_INTEGRITY":
                case "TOKEN_INVALID":
                case "SERVER_ERROR":
                    return reason;
                default:
                    return "SERVER_ERROR";
            }
        }

        private static string BuildRequestLogContent(AuthRequest request)
        {
            JObject payload = JObject.FromObject(request ?? new AuthRequest());
            MaskSensitiveProperty(payload, "authToken");
            MaskSensitiveProperty(payload, "playIntegrityToken");

            JToken deviceInfoToken = payload["deviceInfoJson"];
            if (deviceInfoToken != null && deviceInfoToken.Type == JTokenType.String)
            {
                string deviceInfoJson = deviceInfoToken.Value<string>();
                if (!string.IsNullOrWhiteSpace(deviceInfoJson))
                {
                    try
                    {
                        JObject deviceInfo = JObject.Parse(NormalizeJson(deviceInfoJson));
                        MaskSensitiveProperty(deviceInfo, "googleId");
                        MaskSensitiveProperty(deviceInfo, "androidId");
                        MaskSensitiveProperty(deviceInfo, "deviceUniqueIdentifier");
                        payload["deviceInfoJson"] = deviceInfo;
                    }
                    catch (Exception exception)
                    {
                        payload["deviceInfoJson"] = $"<设备信息解析失败：{exception.Message}>";
                    }
                }
            }

            return payload.ToString(Formatting.None);
        }

        private static string BuildResponseLogContent(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return "<空响应>";
            }

            try
            {
                JToken response = JToken.Parse(NormalizeJson(responseText));
                if (response is JObject responseObject)
                {
                    MaskSensitiveProperty(responseObject, "token");
                }
                return response.ToString(Formatting.None);
            }
            catch (Exception exception)
            {
                return $"<响应解析失败，长度={responseText.Length}，错误={exception.Message}>";
            }
        }

        private static void MaskSensitiveProperty(JObject target, string propertyName)
        {
            if (target == null || target[propertyName] == null)
            {
                return;
            }

            string value = target[propertyName].Type == JTokenType.String
                ? target[propertyName].Value<string>()
                : target[propertyName].ToString(Formatting.None);
            target[propertyName] = string.IsNullOrEmpty(value)
                ? "<空>"
                : $"<已脱敏，长度={value.Length}>";
        }

        private static string NormalizeJson(string text)
        {
            return string.IsNullOrEmpty(text)
                ? string.Empty
                : text.TrimStart('\uFEFF', '\u200B', '\0', ' ', '\r', '\n', '\t');
        }
    }
}
