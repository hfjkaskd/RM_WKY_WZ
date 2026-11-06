#if UNITY_EDITOR
using NUnit.Framework;
using Bizza.TokenClientSystem;

namespace Bizza.UserInformationVerification.Tests
{
    public sealed class UserInformationVerificationSmokeTests
    {
        [SetUp]
        public void SetUp()
        {
            AuthConfig.ClearAppIdOverride();
            AuthConfig.ResetToDefaults();
            UserInformationVerificationRuntime.ResetToDefaults();
        }

        [Test]
        public void AuthConfig_RoundTrip_PreservesStandaloneAppId()
        {
            AuthConfigData source = AuthConfigData.CreateDefault();
            source.AppId = "test-app-id";
            source.WorkerUrl = "https://example.invalid/decision";
            source.RequestTimeoutSeconds = 9;

            byte[] bytes = AuthConfigBinarySerializer.Serialize(source);
            AuthConfigData result = AuthConfigBinarySerializer.Deserialize(bytes);

            Assert.That(result.AppId, Is.EqualTo(source.AppId));
            Assert.That(result.WorkerUrl, Is.EqualTo(source.WorkerUrl));
            Assert.That(result.RequestTimeoutSeconds, Is.EqualTo(source.RequestTimeoutSeconds));
        }

        [Test]
        public void AuthFlowAppIdOverride_TakesPriorityOverConfig()
        {
            AuthConfigData source = AuthConfigData.CreateDefault();
            source.AppId = "config-app-id";
            AuthConfig.Apply(source);

            AuthConfig.SetAppIdOverride("request-app-id");

            Assert.That(AuthConfig.AppId, Is.EqualTo("request-app-id"));
        }
    }
}
#endif
