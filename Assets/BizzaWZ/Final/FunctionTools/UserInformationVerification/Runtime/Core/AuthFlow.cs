using System;
using Cysharp.Threading.Tasks;
using Bizza.UserInformationVerification;
using LogLogger = Bizza.UserInformationVerification.AuthLog;

namespace Bizza.TokenClientSystem
{
    /// <summary>
    /// IP 检测统一入口：配置加载 → 公共设备采集 → SCB 保护 → 授权。
    /// SDK 已初始化设备信息时直接复用；独立运行时由本流程完成首次注入。
    /// </summary>
    public sealed class AuthFlow
    {
        private readonly AuthService authService;

        public AuthFlow(AuthService authService = null)
        {
            this.authService = authService ?? new AuthService();
        }

        /// <summary>
        /// 由 Boots_Flow 调用的授权初始化阶段。
        /// AppId 按“公共 DeviceInfoUtil → AuthConfig.bytes”顺序读取。
        /// </summary>
        public async UniTask Init(Action<float> onProgress)
        {
            onProgress?.Invoke(0f);
            await AuthConfigLoader.LoadAsync();
            onProgress?.Invoke(0.15f);

            if (!DeviceInfoUtil.IsInitialized)
            {
                DeviceInfoUtil.CustomDeviceInfo deviceInfoConfig =
                    CreateDeviceInfoConfig();
                await DeviceInfoUtil.InitializeAsync(deviceInfoConfig);
            }
            onProgress?.Invoke(0.45f);

            string effectiveAppId = DeviceInfoUtil.Current?.Os_Apid;
            if (string.IsNullOrWhiteSpace(effectiveAppId))
            {
                effectiveAppId = AuthConfig.AppId;
            }

            AuthConfig.SetAppIdOverride(effectiveAppId);
            onProgress?.Invoke(0.55f);
            try
            {
                LogLogger.LogIP("[AuthFlow] 配置加载完成，AppId=" + AuthConfig.AppId);
                LogLogger.LogIP("[AuthFlow] 已复用公共 DeviceInfoUtil 设备快照");
#if !DEBUG_MODE && !UNITY_EDITOR
                if (!ScreenCaptureProtection.TryProtect())
                {
                    const string message = "屏幕采集保护初始化失败，已阻止授权流程。";
                    LogLogger.LogIP("[AuthFlow] " + message);
                    AuthResult protectionFailure = AuthResult.NetworkFailure(
                        string.Empty,
                        "screen capture protection initialization failed");
                    await UserInformationVerificationRuntime.FailureHandler
                        .HandleAsync(protectionFailure);
                    throw new TokenClientBlockedException("SCREEN_CAPTURE_PROTECTION");
                }
#endif
                onProgress?.Invoke(0.65f);
                AuthResult result = await authService.AuthorizeAsync();
                onProgress?.Invoke(0.9f);
                LogLogger.LogIP(result == null
                    ? "[令牌客户端] 授权流程返回空结果"
                    : $"[令牌客户端] 授权流程返回：允许授权={result.Allow}，有服务端响应={result.HasServerResponse}，网络错误={result.IsNetworkError}，原因={result.PublicReason}，requestId={result.RequestId}");

                if (result != null && result.Allow)
                {
                    LogLogger.LogIP($"[令牌客户端] 流程完成：允许授权=true，requestId={result.RequestId}");
                    onProgress?.Invoke(1f);
                    return;
                }

                LogLogger.LogIP(result == null
                    ? "[令牌客户端] 授权被阻断：结果为空"
                    : $"[令牌客户端] 授权被阻断：原因={result.PublicReason}，错误={result.Error}，requestId={result.RequestId}");
                await UserInformationVerificationRuntime.FailureHandler
                    .HandleAsync(result);
                throw new TokenClientBlockedException(result?.PublicReason ?? "SERVER_ERROR");
            }
            finally
            {
                AuthConfig.ClearAppIdOverride();
            }
        }

        private static DeviceInfoUtil.CustomDeviceInfo CreateDeviceInfoConfig()
        {
            return new DeviceInfoUtil.CustomDeviceInfo
            {
                appId = AuthConfig.AppId
            };
        }
    }

    public sealed class TokenClientBlockedException : System.Exception
    {
        public TokenClientBlockedException(string reason)
            : base($"TokenClient 阻止继续加载：{reason}")
        {
        }
    }
}
