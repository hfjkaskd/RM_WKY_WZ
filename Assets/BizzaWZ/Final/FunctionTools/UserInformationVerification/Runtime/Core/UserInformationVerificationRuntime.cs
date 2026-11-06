using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Bizza.TokenClientSystem;

namespace Bizza.UserInformationVerification
{
    internal static class AuthLog
    {
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void LogIP(string message)
        {
            UserInformationVerificationRuntime.Logger.Info(message);
        }

        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void LogVerbose(string message)
        {
            UserInformationVerificationRuntime.Logger.Info(message);
        }
    }

    public interface IAuthFailureHandler
    {
        UniTask HandleAsync(AuthResult result, CancellationToken cancellationToken = default);
    }

    public interface IPlayIntegrityTokenProvider
    {
        UniTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// IP 模块的宿主适配入口。设备信息统一由公共 DeviceInfoUtil 提供。
    /// </summary>
    public static class UserInformationVerificationRuntime
    {
        private static IAppIdentityProvider appIdentityProvider;
        private static IAuthLogger logger = new UnityAuthLogger();
        private static IAuthFailureHandler failureHandler = CreateDefaultFailureHandler();
        private static IPlayIntegrityTokenProvider playIntegrityTokenProvider =
            new EmptyPlayIntegrityTokenProvider();

        public static IAppIdentityProvider AppIdentityProvider
        {
            get => appIdentityProvider;
            set => appIdentityProvider = value;
        }

        public static IAuthLogger Logger
        {
            get => logger ?? (logger = new UnityAuthLogger());
            set => logger = value ?? new UnityAuthLogger();
        }

        public static IAuthFailureHandler FailureHandler
        {
            get => failureHandler ?? (failureHandler = CreateDefaultFailureHandler());
            set => failureHandler = value ?? CreateDefaultFailureHandler();
        }

        public static IPlayIntegrityTokenProvider PlayIntegrityTokenProvider
        {
            get => playIntegrityTokenProvider ?? (playIntegrityTokenProvider =
                new EmptyPlayIntegrityTokenProvider());
            set => playIntegrityTokenProvider = value ?? new EmptyPlayIntegrityTokenProvider();
        }

        public static void Configure(
            IAppIdentityProvider identityProvider = null,
            IAuthLogger authLogger = null,
            IAuthFailureHandler authFailureHandler = null,
            IPlayIntegrityTokenProvider integrityTokenProvider = null)
        {
            if (identityProvider != null)
            {
                AppIdentityProvider = identityProvider;
            }

            if (authLogger != null)
            {
                Logger = authLogger;
            }

            if (authFailureHandler != null)
            {
                FailureHandler = authFailureHandler;
            }

            if (integrityTokenProvider != null)
            {
                PlayIntegrityTokenProvider = integrityTokenProvider;
            }
        }

        public static string GetAppId()
        {
            return AppIdentityProvider?.AppId ?? string.Empty;
        }

        public static string GetPackageName()
        {
            string packageName = AppIdentityProvider?.PackageName;
            return string.IsNullOrWhiteSpace(packageName)
                ? Application.identifier ?? string.Empty
                : packageName;
        }

        public static string GetUnityRegionCode()
        {
            return AppIdentityProvider?.UnityRegionCode ?? string.Empty;
        }

        public static void ResetToDefaults()
        {
            appIdentityProvider = null;
            logger = new UnityAuthLogger();
            failureHandler = CreateDefaultFailureHandler();
            playIntegrityTokenProvider = new EmptyPlayIntegrityTokenProvider();
        }

        private sealed class UnityAuthLogger : IAuthLogger
        {
            public void Info(string message) => Debug.Log(message ?? string.Empty);
            public void Warning(string message) => Debug.LogWarning(message ?? string.Empty);
            public void Error(string message) => Debug.LogError(message ?? string.Empty);
        }

        private sealed class DefaultAuthFailureHandler : IAuthFailureHandler
        {
            public UniTask HandleAsync(
                AuthResult result,
                CancellationToken cancellationToken = default)
            {
                var args = new CommonConfirmTipsPanel.Args
                {
                    isLanguage = true,
                    des = CommonConfirmTipsPanel.GetTokenClientMessageKey(
                        result?.PublicReason),
                };
                return SDKAssetHandler.OpenCommonConfirmTipsPanel(args);
            }
        }

        private static IAuthFailureHandler CreateDefaultFailureHandler()
        {
            return new DefaultAuthFailureHandler();
        }

        private sealed class EmptyPlayIntegrityTokenProvider : IPlayIntegrityTokenProvider
        {
            public UniTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(string.Empty);
            }
        }
    }

    /// <summary>
    /// 将宿主现有事件/弹窗接入 IP 模块的轻量适配器，避免 Runtime 直接引用 SDK UI。
    /// </summary>
    public sealed class DelegateAuthFailureHandler : IAuthFailureHandler
    {
        private readonly Func<AuthResult, CancellationToken, UniTask> handler;

        public DelegateAuthFailureHandler(Func<AuthResult, CancellationToken, UniTask> handler)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public UniTask HandleAsync(
            AuthResult result,
            CancellationToken cancellationToken = default)
        {
            return handler(result, cancellationToken);
        }
    }

    public sealed class DelegateAppIdentityProvider : IAppIdentityProvider
    {
        private readonly Func<string> appId;
        private readonly Func<string> packageName;
        private readonly Func<string> unityRegionCode;

        public DelegateAppIdentityProvider(
            Func<string> appId,
            Func<string> packageName = null,
            Func<string> unityRegionCode = null)
        {
            this.appId = appId ?? throw new ArgumentNullException(nameof(appId));
            this.packageName = packageName;
            this.unityRegionCode = unityRegionCode;
        }

        public string AppId => appId() ?? string.Empty;
        public string PackageName => packageName?.Invoke() ?? string.Empty;
        public string UnityRegionCode => unityRegionCode?.Invoke() ?? string.Empty;
    }

    public sealed class DelegatePlayIntegrityTokenProvider : IPlayIntegrityTokenProvider
    {
        private readonly Func<CancellationToken, UniTask<string>> generator;

        public DelegatePlayIntegrityTokenProvider(
            Func<CancellationToken, UniTask<string>> generator)
        {
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        }

        public UniTask<string> GetTokenAsync(
            CancellationToken cancellationToken = default)
        {
            return generator(cancellationToken);
        }
    }
}
