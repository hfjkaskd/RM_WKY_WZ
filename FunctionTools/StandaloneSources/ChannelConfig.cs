using System;

namespace Bizza.Sdk
{
    // 独立 IP 工具的兼容配置。SDK 共存时不应导出此文件，改用 SDK 自己的 ChannelConfig。
    public class ChannelConfig
    {
        public static ChannelConfig instance;
        public static ChannelConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ChannelConfig();
                }

                return instance;
            }
        }

        public string AppId = "wxa9b7d0c6c9e5b7d0";
        public Real_CustomConfig real_CustomConfig;
        public HttpDataConfig httpConfig;
    }

    public class Real_CustomConfig
    {
        public string CountryName;
        public string DefaultCountryName;
        public string GenerateFakeAndroidId;
    }

    [Serializable]
    public class HttpDataConfig
    {
        public string domain = string.Empty;
        public string aes_key = string.Empty;
    }
}

public class AccountModule
{
    public static E_CountryType CountryType;

    public enum E_CountryType
    {
        None,
        US,
        BR,
        ID,
#if !BIZZA_HTTP_AD
        JP,
        KR,
        MX,
        SA,
        DE
#endif
    }
}
