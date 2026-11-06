using System;
using System.IO;

[Serializable]
public class RemoteGroupRuntimeConfig
{
    public const string ResourcesAssetName = "RemoteGroupRuntimeConfig";
    public const string ResourcesAssetPath = "Assets/Resources/RemoteGroupRuntimeConfig.bytes";

    public const string StreamingAssetsDirectoryName = "RemoteGroup";
    public const string BundledContentDirectoryName = "RemoteContent";
    public const string LocalDefaultGameDataFileName = "Default.bytes";
    public const string PersistentDataDirectoryName = "RemoteGroup";
    public const string PersistentGameDataDirectoryName = "gamedata";
    public const string PersistentStrategyFileName = "group.bytes";
    public const string AssignmentCompletedPrefsKey = "RemoteGroup.AssignmentCompleted";

    public const string DefaultRemoteRootUrl = "";
    public const string DefaultRemoteGroupDataName = "group.bytes";
    public const string DefaultGameDataDirectoryName = "gamedata";
    public const string DefaultCommercialDirectoryName = "commercial";
    public const string DefaultCommercialConfigName = "ChannelConfig";
    public const string DefaultCommonDirectoryName = "Common";
    public const float DefaultTimeout = 5f;
    public const int DefaultRetryTimes = 2;
    public const string DefaultUserGroupDefaultName = "Default";
    public const string DefaultUserGroupNamePrefsKey = "RemoteGroup.UserGroupName";

    public string RemoteRootUrl = DefaultRemoteRootUrl;
    public string RemoteGroupDataName = DefaultRemoteGroupDataName;
    public string GameDataDirectoryName = DefaultGameDataDirectoryName;
    public string CommercialDirectoryName = DefaultCommercialDirectoryName;
    public string CommercialConfigName = DefaultCommercialConfigName;
    public bool StartCommercialGroupIndexAtOne;
    public string CommonDirectoryName = DefaultCommonDirectoryName;
    public float Timeout = DefaultTimeout;
    public int RetryTimes = DefaultRetryTimes;
    public string UserGroupDefaultName = DefaultUserGroupDefaultName;
    public string UserGroupNamePrefsKey = DefaultUserGroupNamePrefsKey;
    public bool CountryGroupEnabled;
    public bool CountryLevelEnabled;

    public string GroupStrategyUrl => BuildRemoteResourceUrl(RemoteGroupDataName, CountryGroupEnabled);

    public static RemoteGroupRuntimeConfig CreateDefault()
    {
        return new RemoteGroupRuntimeConfig();
    }

    public RemoteGroupRuntimeConfig CloneSanitized()
    {
        var defaultConfig = CreateDefault();
        var remoteGroupDataName = NormalizeText(RemoteGroupDataName, defaultConfig.RemoteGroupDataName);
        if (!string.Equals(Path.GetExtension(remoteGroupDataName), ".bytes", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("远端分组策略必须使用 .bytes 二进制文件。");
        }

        return new RemoteGroupRuntimeConfig
        {
            RemoteRootUrl = NormalizeRootUrl(RemoteRootUrl, defaultConfig.RemoteRootUrl),
            RemoteGroupDataName = remoteGroupDataName,
            GameDataDirectoryName = NormalizePath(GameDataDirectoryName, defaultConfig.GameDataDirectoryName),
            CommercialDirectoryName = NormalizePath(CommercialDirectoryName, defaultConfig.CommercialDirectoryName),
            CommercialConfigName = NormalizeFileNameWithoutExtension(CommercialConfigName, defaultConfig.CommercialConfigName),
            StartCommercialGroupIndexAtOne = StartCommercialGroupIndexAtOne,
            CommonDirectoryName = NormalizePath(CommonDirectoryName, defaultConfig.CommonDirectoryName),
            Timeout = Timeout > 0f ? Timeout : defaultConfig.Timeout,
            RetryTimes = RetryTimes >= 0 ? RetryTimes : defaultConfig.RetryTimes,
            UserGroupDefaultName = NormalizeText(UserGroupDefaultName, defaultConfig.UserGroupDefaultName),
            UserGroupNamePrefsKey = NormalizeText(UserGroupNamePrefsKey, defaultConfig.UserGroupNamePrefsKey),
            CountryGroupEnabled = CountryGroupEnabled,
            CountryLevelEnabled = CountryLevelEnabled
        };
    }

    public string BuildGameDataUrl(string userGroupName)
    {
        var gameDataDirectoryName = NormalizePath(GameDataDirectoryName, DefaultGameDataDirectoryName);
        return BuildRemoteResourceUrl($"{gameDataDirectoryName}/{userGroupName}.bytes", CountryLevelEnabled);
    }

    public string BuildCommercialConfigUrl(int zeroBasedGroupIndex)
    {
        if (zeroBasedGroupIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zeroBasedGroupIndex));
        }

        var directoryName = NormalizePath(CommercialDirectoryName, DefaultCommercialDirectoryName);
        var configName = NormalizeFileNameWithoutExtension(CommercialConfigName, DefaultCommercialConfigName);
        var fileIndex = zeroBasedGroupIndex + (StartCommercialGroupIndexAtOne ? 1 : 0);
        var relativePath = $"{directoryName}/{configName}{fileIndex}.bytes";
        var countryPrefix = GetCountryPrefix(CountryGroupEnabled);
        if (!string.IsNullOrEmpty(countryPrefix))
        {
            relativePath = $"{countryPrefix}/{relativePath}";
        }

        return CombineUrl(RemoteRootUrl, relativePath);
    }

    public string GetCountryPrefixForDisplay()
    {
        return GetResourceDirectoryPrefix(CountryGroupEnabled);
    }

    public string GetLevelCountryPrefixForDisplay()
    {
        return GetResourceDirectoryPrefix(CountryLevelEnabled);
    }

    private static string GetCountryPrefix(bool useCountryPrefix)
    {
        if (!useCountryPrefix || AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            return string.Empty;
        }

        return AccountModule.CountryType.ToString();
    }

    private string BuildRemoteResourceUrl(string relativePath, bool useCountryPrefix)
    {
        var normalizedPath = NormalizePath(relativePath, DefaultRemoteGroupDataName);
        var commonDirectoryName = NormalizePath(CommonDirectoryName, DefaultCommonDirectoryName);
        var countryPrefix = GetCountryPrefix(useCountryPrefix);
        if (!string.IsNullOrEmpty(countryPrefix))
        {
            normalizedPath = $"{countryPrefix}/{normalizedPath}";
        }
        else
        {
            normalizedPath = $"{commonDirectoryName}/{normalizedPath}";
        }

        return CombineUrl(RemoteRootUrl, normalizedPath);
    }

    private string GetResourceDirectoryPrefix(bool useCountryPrefix)
    {
        var countryPrefix = GetCountryPrefix(useCountryPrefix);
        return string.IsNullOrEmpty(countryPrefix)
            ? NormalizePath(CommonDirectoryName, DefaultCommonDirectoryName)
            : countryPrefix;
    }

    private static string NormalizeText(string value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static string NormalizeRootUrl(string value, string defaultValue)
    {
        var rootUrl = NormalizeText(value, defaultValue);
        return rootUrl.EndsWith("/", StringComparison.Ordinal) ? rootUrl : rootUrl + "/";
    }

    private static string NormalizePath(string value, string defaultValue)
    {
        var path = NormalizeText(value, defaultValue).Replace("\\", "/").Trim('/');
        return string.IsNullOrEmpty(path) ? defaultValue.Trim('/') : path;
    }

    private static string NormalizeFileNameWithoutExtension(string value, string defaultValue)
    {
        var fileName = Path.GetFileNameWithoutExtension(NormalizeText(value, defaultValue));
        return string.IsNullOrWhiteSpace(fileName) ? defaultValue : fileName.Trim();
    }

    private static string CombineUrl(string rootUrl, string fileName)
    {
        var root = NormalizeRootUrl(rootUrl, DefaultRemoteRootUrl);
        var file = NormalizeText(fileName, DefaultRemoteGroupDataName).TrimStart('/');
        return root + file;
    }
}
