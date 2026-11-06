using System;
using Bizza.UserInformationVerification;

namespace Bizza.UserInformationVerification
{
    /// <summary>
    /// IP 检测模块的 SCB 生命周期门面。SCB 作为 IP 模块必需能力，
    /// 由 AuthFlow 统一调用，业务模块不直接依赖 ScreenCaptureBlocker.Capture。
    /// </summary>
    public static class ScreenCaptureProtection
    {
        private static bool initialized;

        public static bool IsInitialized => initialized;

        public static bool TryProtect()
        {
            if (initialized)
            {
                return true;
            }

            try
            {
                ScreenCaptureBlocker.Capture.ProtectWindowContent();
                initialized = true;
                return true;
            }
            catch (Exception exception)
            {
                initialized = false;
                UserInformationVerificationRuntime.Logger.Error(
                    "[屏幕采集保护] 初始化失败：" + exception.Message);
                return false;
            }
        }

        public static void Release()
        {
            if (!initialized)
            {
                return;
            }

            try
            {
                ScreenCaptureBlocker.Capture.UnprotectWindowContent();
            }
            catch (Exception exception)
            {
                UserInformationVerificationRuntime.Logger.Warning(
                    "[屏幕采集保护] 释放失败：" + exception.Message);
            }
            finally
            {
                initialized = false;
            }
        }
    }
}
