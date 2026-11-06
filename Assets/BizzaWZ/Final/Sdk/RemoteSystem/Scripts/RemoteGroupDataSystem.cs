using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public enum RemoteGroupDataSource
{
    None,
    LocalDefault,
    Remote
}

public class RemoteGroupDataSystem
{
    private const string LogTag = "[RemoteGroupDataSystem]";
    private const string StrategyAssignmentPrefsPrefix = "RemoteGroup.StrategyAssignment.";

    private static readonly RemoteGroupDataSystem system = new RemoteGroupDataSystem();

    private RemoteGroupRuntimeConfig runtimeConfig = RemoteGroupRuntimeConfig.CreateDefault();
    private bool runtimeConfigLoaded;
    private bool sessionInitialized;
    private string sessionUserGroupName;
    private RemoteGroupDataSource sessionDataSource;
    private byte[] sessionGameData;
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
    private bool sessionHasAdStatistics;
    private GameAB_CustomData sessionAdStatistics;
    private int sessionCommercialFileIndex;
    private string sessionCommercialUserGroupName;
#endif

    private RemoteGroupDataSystem() { }

    public static RemoteGroupDataSystem current => system;

    public bool UsesCountryConfig
    {
        get
        {
            LoadRuntimeConfig();
            return runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled;
        }
    }

    public async Task InitGameData()
    {
        GameConfigLog("开始初始化远端分组配置。");
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
        LoadRuntimeConfig();
        await AdStatisticsConfigMiddleware.PreloadBundledConfigAsync(
            runtimeConfig.StartCommercialGroupIndexAtOne ? 1 : 0,
            runtimeConfig.CommercialConfigName);
#endif
        await Init();
        GameConfigLog($"分组配置初始化完成：当前分组={GetUserGroupName()}，数据来源={GetCurrentDataSource()}，是否已正式完成分组={HasCompletedAssignment()}。");
    }

    private async Task Init()
    {
        LoadRuntimeConfig();
        ResetSessionState();
        var countryPrefixDescription = runtimeConfig.CountryGroupEnabled
            ? $"已开启，当前国家={AccountModule.CountryType}，实际前缀={GetCountryPrefixDescription(false)}"
            : "未开启";
        var levelCountryPrefixDescription = runtimeConfig.CountryLevelEnabled
            ? $"已开启，当前国家={AccountModule.CountryType}，实际前缀={GetCountryPrefixDescription(true)}"
            : "未开启";
        GameConfigLog($"运行时配置已准备：分组策略地址={runtimeConfig.GroupStrategyUrl}，远端关卡目录={runtimeConfig.GameDataDirectoryName}，分组/商业化国家路径={countryPrefixDescription}，关卡国家路径={levelCountryPrefixDescription}，本地默认配置路径={BuildLocalDefaultGameDataPath()}。");

        var usesCountryConfig = runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled;
        if (usesCountryConfig && AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            ClearCommittedAssignment();
            GameConfigLog("当前国家为 None，直接使用本地 Default 配置，不请求远端，也不写入正式分组完成记录。");
            try
            {
                await ActivateLocalDefaultFallback(false);
            }
            catch (Exception exception)
            {
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
                if (AdStatisticsConfigMiddleware.HasPreloadedBundledConfig())
                {
                    ActivateSession(runtimeConfig.UserGroupDefaultName, RemoteGroupDataSource.None, Array.Empty<byte>());
                    GameConfigLog($"本地 Default 关卡数据不可用，None 国家仍使用随包广告配置：{exception.Message}。");
                    return;
                }
#endif
                throw;
            }
            return;
        }

        if (!usesCountryConfig && AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            GameConfigLog("未开启国家相关配置，当前国家为 None 仅表示不使用国家前缀；继续请求公共分组策略并进行正常随机。");
        }

#if UNITY_EDITOR
        if (RemoteGroupEditorOverride.Enabled)
        {
            GameConfigLog("编辑器分组覆盖已开启，使用编辑器指定的分组。");
            await InitWithEditorUserGroupOverride();
            return;
        }

        ClearEditorOverrideSavedGroupIfNeeded();
#endif

        if (await TryLoadCommittedAssignment())
        {
            GameConfigLog($"检测到已完成的分组记录，跳过本次远端分组策略请求和远端关卡下载：分组={GetUserGroupName()}。");
            return;
        }

        string selectedUserGroupName = null;
        try
        {
            RemoteGroupStrategy strategy;
            var usingBundledStrategy = false;
            try
            {
                GameConfigLog("没有可复用的已完成分组记录，开始请求远端二进制分组策略。");
                var strategyPayload = await RequestBytesWithRetry(runtimeConfig.GroupStrategyUrl);
                strategy = ParseStrategyPayload(strategyPayload);
                if (!TrySelectUserGroupName(strategy, out selectedUserGroupName))
                {
                    throw new InvalidDataException("远端分组策略中没有有效的用户分组。");
                }
                SavePersistentStrategy(strategyPayload);
                GameConfigLog($"远端分组策略请求并解析成功：字节数={strategyPayload.Length}。");
            }
            catch (Exception remoteException)
            {
                GameConfigLog($"远端分组策略不可用，尝试随包本地策略：{remoteException.Message}。");
                strategy = await LoadBundledGroupStrategy();
                usingBundledStrategy = true;
                if (!TrySelectUserGroupName(strategy, out selectedUserGroupName))
                {
                    throw new InvalidDataException("随包本地分组策略中没有有效的用户分组。");
                }
            }

            GameConfigLog($"当前使用{(usingBundledStrategy ? "随包本地" : "远端")}分组策略，选中的分组={selectedUserGroupName}。");

#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
            // 商业化配置独立于关卡数据；即使本地 Default.bytes 缺失，也可使用远端或包内配置。
            await TryActivateRemoteAdStatistics(strategy, selectedUserGroupName);
#endif

            if (usingBundledStrategy)
            {
                // 远端策略不可用时，仍保留本地策略选中的广告分组；关卡数据不阻塞广告配置。
                if (string.Equals(selectedUserGroupName, runtimeConfig.UserGroupDefaultName, StringComparison.Ordinal))
                {
                    try
                    {
                        var localDefaultData = await LoadLocalDefaultGameData();
                        ActivateSession(selectedUserGroupName, RemoteGroupDataSource.LocalDefault, localDefaultData);
                        return;
                    }
                    catch (Exception localDataException)
                    {
                        GameConfigLog($"本地 Default 关卡数据不可用，仅使用随包广告分组：{localDataException.Message}。");
                    }
                }
                ActivateSession(selectedUserGroupName, RemoteGroupDataSource.None, Array.Empty<byte>());
                return;
            }

            if (string.Equals(selectedUserGroupName, runtimeConfig.UserGroupDefaultName, StringComparison.Ordinal))
            {
                GameConfigLog("远端策略选中 Default，开始读取 StreamingAssets 下的本地默认二进制配置。");
                var localDefaultData = await LoadLocalDefaultGameData();
                CommitAssignment(runtimeConfig.UserGroupDefaultName, RemoteGroupDataSource.LocalDefault, localDefaultData);
                GameConfigLog("Default 本地二进制配置读取成功，已正式记录 Default；后续启动不再请求远端。");
                return;
            }

            var gameDataUrl = runtimeConfig.BuildGameDataUrl(selectedUserGroupName);
            GameConfigLog($"开始下载远端分组二进制关卡配置：分组={selectedUserGroupName}，地址={gameDataUrl}。");
            var gameData = await RequestBytesWithRetry(gameDataUrl);
            ValidateGameData(gameData, selectedUserGroupName);
            CommitRemoteAssignment(selectedUserGroupName, gameData);
            GameConfigLog($"远端分组二进制关卡配置保存并校验成功：分组={selectedUserGroupName}，字节数={gameData.Length}；已正式记录该分组，后续启动不再请求远端。");
        }
        catch (Exception exception)
        {
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
            if (!string.IsNullOrWhiteSpace(selectedUserGroupName)
                && (sessionHasAdStatistics || AdStatisticsConfigMiddleware.HasPreloadedBundledConfig()))
            {
                ActivateSession(selectedUserGroupName, RemoteGroupDataSource.None, Array.Empty<byte>());
                GameConfigLog($"关卡数据初始化失败，但广告配置已按分组 {selectedUserGroupName} 启用：{exception.Message}。");
                return;
            }
#endif
            GameConfigLog($"远端分组流程失败，本次临时使用本地 Default，不写入远端分组完成记录。失败原因={exception.Message}");
            try
            {
                await ActivateLocalDefaultFallback();
            }
            catch (Exception fallbackException)
            {
                GameConfigLog($"本地 Default 二进制配置也无法读取，本次分组初始化失败：{fallbackException.Message}");
                throw;
            }
        }
    }

#if UNITY_EDITOR
    private async Task InitWithEditorUserGroupOverride()
    {
        var userGroupName = RemoteGroupEditorOverride.GetUserGroupNameOrDefault(runtimeConfig.UserGroupDefaultName);
        GameConfigLog($"编辑器覆盖分组={userGroupName}。");
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
        await TryActivateRemoteAdStatistics(userGroupName);
#endif
        try
        {
            if (string.Equals(userGroupName, runtimeConfig.UserGroupDefaultName, StringComparison.Ordinal))
            {
                GameConfigLog("编辑器覆盖选择 Default，读取本地默认二进制配置。");
                var localDefaultData = await LoadLocalDefaultGameData();
                CommitAssignment(userGroupName, RemoteGroupDataSource.LocalDefault, localDefaultData);
            }
            else
            {
                var gameDataUrl = runtimeConfig.BuildGameDataUrl(userGroupName);
                GameConfigLog($"编辑器覆盖选择远端分组，下载二进制关卡配置：分组={userGroupName}，地址={gameDataUrl}。");
                var gameData = await RequestBytesWithRetry(gameDataUrl);
                ValidateGameData(gameData, userGroupName);
                CommitRemoteAssignment(userGroupName, gameData);
                GameConfigLog($"编辑器覆盖分组下载并保存成功：分组={userGroupName}，字节数={gameData.Length}。");
            }
        }
        catch (Exception exception)
        {
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
            if (sessionHasAdStatistics || AdStatisticsConfigMiddleware.HasPreloadedBundledConfig())
            {
                ActivateSession(userGroupName, RemoteGroupDataSource.None, Array.Empty<byte>());
                GameConfigLog($"编辑器覆盖分组已启用广告配置，关卡数据不可用：{exception.Message}。");
            }
            else
#endif
            {
                throw;
            }
        }
        RemoteGroupEditorOverride.MarkSavedGroup();
    }

    private void ClearEditorOverrideSavedGroupIfNeeded()
    {
        if (!RemoteGroupEditorOverride.HasSavedGroupMarker())
        {
            return;
        }

        ClearCommittedAssignment();
        RemoteGroupEditorOverride.ClearSavedGroupMarker();
    }
#endif

    private async Task<bool> TryLoadCommittedAssignment()
    {
        if (PlayerPrefs.GetInt(RemoteGroupRuntimeConfig.AssignmentCompletedPrefsKey, 0) == 0)
        {
            GameConfigLog("本地没有远端分组完成标记，需要重新尝试远端分组。");
            return false;
        }

        var userGroupName = PlayerPrefs.GetString(runtimeConfig.UserGroupNamePrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(userGroupName))
        {
            GameConfigLog("发现远端分组完成标记，但分组名称为空，清除无效记录并重新请求远端。");
            ClearCommittedAssignment();
            return false;
        }

        try
        {
            if (string.Equals(userGroupName, runtimeConfig.UserGroupDefaultName, StringComparison.Ordinal))
            {
                GameConfigLog("已正式记录 Default，读取本地 StreamingAssets 默认二进制配置，不请求远端。");
                var localDefaultData = await LoadLocalDefaultGameData();
                ValidateGameData(localDefaultData, runtimeConfig.UserGroupDefaultName);
                ActivateSession(userGroupName, RemoteGroupDataSource.LocalDefault, localDefaultData);
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
                await TryActivateRemoteAdStatistics(userGroupName);
#endif
                GameConfigLog($"已记录的 Default 配置读取成功：字节数={localDefaultData.Length}。");
                return true;
            }

            var path = BuildPersistentGameDataPath(userGroupName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("已记录的远端二进制关卡配置不存在。", path);
            }

            var gameData = File.ReadAllBytes(path);
            ValidateGameData(gameData, userGroupName);
            ActivateSession(userGroupName, RemoteGroupDataSource.Remote, gameData);
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
            await TryActivateRemoteAdStatistics(userGroupName);
#endif
            GameConfigLog($"已正式记录远端分组，读取沙盒缓存成功：分组={userGroupName}，路径={path}，字节数={gameData.Length}。");
            return true;
        }
        catch (Exception exception)
        {
            GameConfigLog($"已记录的分组无法使用，将清除记录并重新请求远端：分组={userGroupName}，原因={exception.Message}。");
            ClearCommittedAssignment();
            return false;
        }
    }

    private async Task ActivateLocalDefaultFallback(bool retryRemoteNextLaunch = true)
    {
        var localDefaultData = await LoadLocalDefaultGameData();
        ValidateGameData(localDefaultData, runtimeConfig.UserGroupDefaultName);
        ActivateSession(runtimeConfig.UserGroupDefaultName, RemoteGroupDataSource.LocalDefault, localDefaultData);
        var nextLaunchDescription = retryRemoteNextLaunch
            ? "下次启动仍会重试远端"
            : "当前国家相关配置仍为 None 时，下次启动仍直接使用本地 Default";
        GameConfigLog($"本次已临时启用本地 Default：字节数={localDefaultData.Length}；未写入远端分组完成记录；{nextLaunchDescription}。");
    }

    private void CommitRemoteAssignment(string userGroupName, byte[] gameData)
    {
        SaveRemoteGameData(userGroupName, gameData);
        CommitAssignment(userGroupName, RemoteGroupDataSource.Remote, gameData);
    }

    private void CommitAssignment(string userGroupName, RemoteGroupDataSource dataSource, byte[] gameData)
    {
        if (string.IsNullOrWhiteSpace(userGroupName))
        {
            throw new InvalidDataException("不能正式记录空的用户分组。");
        }

        ValidateGameData(gameData, userGroupName);
        PlayerPrefs.SetString(runtimeConfig.UserGroupNamePrefsKey, userGroupName);
        PlayerPrefs.SetInt(RemoteGroupRuntimeConfig.AssignmentCompletedPrefsKey, 1);
        PlayerPrefs.Save();
        ActivateSession(userGroupName, dataSource, gameData);
        GameConfigLog($"已写入正式分组记录：分组={userGroupName}，数据来源={dataSource}。");
    }

    private void ActivateSession(string userGroupName, RemoteGroupDataSource dataSource, byte[] gameData)
    {
        sessionInitialized = true;
        sessionUserGroupName = userGroupName;
        sessionDataSource = dataSource;
        sessionGameData = gameData ?? Array.Empty<byte>();
    }

    private void ResetSessionState()
    {
        sessionInitialized = false;
        sessionUserGroupName = runtimeConfig.UserGroupDefaultName;
        sessionDataSource = RemoteGroupDataSource.None;
        sessionGameData = null;
#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
        sessionHasAdStatistics = false;
        sessionAdStatistics = default;
        sessionCommercialFileIndex = runtimeConfig.StartCommercialGroupIndexAtOne ? 1 : 0;
        sessionCommercialUserGroupName = null;
#endif
    }

#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
    /// <summary>
    /// 获取当前分组的远端广告数值。currentLevel 预留给后续按关卡分段扩展。
    /// </summary>
    public bool TryGetActiveAdStatistics(int currentLevel, out GameAB_CustomData statistics)
    {
        _ = currentLevel;
        statistics = sessionAdStatistics;
        return sessionHasAdStatistics;
    }

    public int GetCommercialGroupIndex() => sessionCommercialFileIndex;

    public string GetCommercialConfigName()
    {
        LoadRuntimeConfig();
        return runtimeConfig.CommercialConfigName;
    }
#endif

    public string GetUserGroupName()
    {
        LoadRuntimeConfig();

        if (sessionInitialized && !string.IsNullOrWhiteSpace(sessionUserGroupName))
        {
            return sessionUserGroupName;
        }

        if ((runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled)
            && AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            return runtimeConfig.UserGroupDefaultName;
        }

        if (PlayerPrefs.GetInt(RemoteGroupRuntimeConfig.AssignmentCompletedPrefsKey, 0) != 0)
        {
            var userGroupName = PlayerPrefs.GetString(runtimeConfig.UserGroupNamePrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(userGroupName))
            {
                return userGroupName;
            }
        }

        return runtimeConfig.UserGroupDefaultName;
    }

    public bool IsDefault() => GetUserGroupName() == runtimeConfig.UserGroupDefaultName;

    public bool HasCompletedAssignment()
    {
        LoadRuntimeConfig();
        if ((runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled)
            && AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            return false;
        }

        return PlayerPrefs.GetInt(RemoteGroupRuntimeConfig.AssignmentCompletedPrefsKey, 0) != 0;
    }

    public RemoteGroupDataSource GetCurrentDataSource()
    {
        if (sessionInitialized)
        {
            return sessionDataSource;
        }

        if (!HasCompletedAssignment())
        {
            return RemoteGroupDataSource.None;
        }

        return IsDefault() ? RemoteGroupDataSource.LocalDefault : RemoteGroupDataSource.Remote;
    }

    public string GetCurrentGameDataPath()
    {
        LoadRuntimeConfig();
        var userGroupName = GetUserGroupName();
        return string.Equals(userGroupName, runtimeConfig.UserGroupDefaultName, StringComparison.Ordinal)
            ? BuildLocalDefaultGameDataPath()
            : BuildPersistentGameDataPath(userGroupName);
    }

    public bool TryGetCurrentGameDataBytes(out byte[] gameData)
    {
        gameData = null;

        if (sessionInitialized && sessionGameData != null && sessionGameData.Length > 0)
        {
            gameData = (byte[])sessionGameData.Clone();
            return true;
        }

        try
        {
            var path = GetCurrentGameDataPath();
            if (!File.Exists(path))
            {
                return false;
            }

            gameData = File.ReadAllBytes(path);
            return gameData.Length > 0;
        }
        catch
        {
            gameData = null;
            return false;
        }
    }

    public bool HasGameData()
    {
        return TryGetCurrentGameDataBytes(out var gameData) && gameData.Length > 0;
    }

    public void ClearCachedRemoteData()
    {
        LoadRuntimeConfig();
        GameConfigLog("开始清理已记录的远端分组和沙盒二进制缓存。");
        ClearCommittedAssignment();
        foreach (AccountModule.E_CountryType country in Enum.GetValues(typeof(AccountModule.E_CountryType)))
        {
            PlayerPrefs.DeleteKey(StrategyAssignmentPrefsPrefix + country + ".Name");
            PlayerPrefs.DeleteKey(StrategyAssignmentPrefsPrefix + country + ".Fingerprint");
        }
        PlayerPrefs.Save();
        ResetSessionState();

        try
        {
            var persistentRootPath = GetPersistentRootPath();
            if (Directory.Exists(persistentRootPath))
            {
                Directory.Delete(persistentRootPath, true);
            }

            GameConfigLog("远端分组记录和沙盒二进制缓存清理完成。");
        }
        catch (Exception exception)
        {
            GameConfigLog($"清理沙盒远端数据失败：{exception.Message}。");
        }
    }

#if BIZZA_REAL_WITHDRAW && BIZZA_REMOTEGROUPDATA
    private async Task TryActivateRemoteAdStatistics(string userGroupName)
    {
        sessionHasAdStatistics = false;
        sessionAdStatistics = default;

        try
        {
            var strategyPath = BuildPersistentStrategyPath();
            RemoteGroupStrategy strategy = null;
            if (File.Exists(strategyPath))
            {
                strategy = ParseStrategyPayload(File.ReadAllBytes(strategyPath));
            }

            if (strategy == null)
            {
                try
                {
                    var strategyPayload = await RequestBytesWithRetry(runtimeConfig.GroupStrategyUrl);
                    strategy = ParseStrategyPayload(strategyPayload);
                    SavePersistentStrategy(strategyPayload);
                }
                catch (Exception remoteException)
                {
                    GameConfigLog($"已记录分组的远端策略不可用，读取随包本地策略：{remoteException.Message}。");
                    strategy = await LoadBundledGroupStrategy();
                }
            }

            await TryActivateRemoteAdStatistics(strategy, userGroupName);
        }
        catch (Exception exception)
        {
            GameConfigLog($"远端商业化配置准备失败，将使用广告数值默认值：分组={userGroupName}，原因={exception.Message}。");
        }
    }

    private async Task TryActivateRemoteAdStatistics(RemoteGroupStrategy strategy, string userGroupName)
    {
        sessionHasAdStatistics = false;
        sessionAdStatistics = default;
        sessionCommercialUserGroupName = userGroupName;

        try
        {
            if (!TryGetZeroBasedGroupIndex(strategy, userGroupName, out var zeroBasedGroupIndex))
            {
                throw new InvalidDataException($"分组策略中找不到当前分组：{userGroupName}。");
            }

            var fileIndex = zeroBasedGroupIndex + (runtimeConfig.StartCommercialGroupIndexAtOne ? 1 : 0);
            sessionCommercialFileIndex = fileIndex;
            await AdStatisticsConfigMiddleware.PreloadBundledConfigAsync(
                fileIndex, runtimeConfig.CommercialConfigName);
            var cachePath = BuildPersistentCommercialConfigPath(fileIndex);
            if (TryReadAdStatisticsFile(cachePath, fileIndex, out var cachedConfig, out var cacheError))
            {
                sessionAdStatistics = cachedConfig;
                sessionHasAdStatistics = true;
                GameConfigLog($"远端商业化配置从沙盒缓存读取成功：分组={userGroupName}，序号={fileIndex}，路径={cachePath}。");
                // 不阻塞已有玩家启动；后台更新成功后本次会话立即使用新参数。
                _ = RefreshRemoteAdStatistics(zeroBasedGroupIndex, fileIndex, cachePath, userGroupName);
                return;
            }

            if (!string.IsNullOrEmpty(cacheError))
            {
                GameConfigLog($"沙盒商业化配置不可用，将重新下载：分组={userGroupName}，原因={cacheError}。");
            }

            await RefreshRemoteAdStatistics(zeroBasedGroupIndex, fileIndex, cachePath, userGroupName);
        }
        catch (Exception exception)
        {
            GameConfigLog($"远端商业化配置不可用，将使用广告数值默认值：分组={userGroupName}，原因={exception.Message}。");
        }
    }

    private async Task RefreshRemoteAdStatistics(int zeroBasedGroupIndex, int fileIndex,
        string cachePath, string userGroupName)
    {
        try
        {
            var url = runtimeConfig.BuildCommercialConfigUrl(zeroBasedGroupIndex);
            GameConfigLog($"开始下载远端商业化配置：分组={userGroupName}，序号={fileIndex}，地址={url}。");
            var bytes = await RequestBytesWithRetry(url);
            if (!RemoteAdStatisticsConfigBinarySerializer.TryDeserialize(
                    bytes, fileIndex, out var remoteConfig, out var parseError))
            {
                throw new InvalidDataException(parseError);
            }

            if (!string.Equals(sessionCommercialUserGroupName, userGroupName, StringComparison.Ordinal)) return;
            sessionAdStatistics = remoteConfig;
            sessionHasAdStatistics = true;
            try { SaveCommercialConfig(cachePath, bytes); }
            catch (Exception cacheException)
            {
                GameConfigLog($"远端商业化配置已激活，但写入缓存失败：{cacheException.Message}。");
            }
            GameConfigLog($"远端商业化配置下载并激活成功：分组={userGroupName}，序号={fileIndex}，路径={cachePath}。");
        }
        catch (Exception exception)
        {
            GameConfigLog($"远端商业化配置刷新失败：分组={userGroupName}，原因={exception.Message}。");
        }
    }

    private static bool TryGetZeroBasedGroupIndex(
        RemoteGroupStrategy strategy,
        string userGroupName,
        out int groupIndex)
    {
        groupIndex = -1;
        if (strategy?.Datas == null || string.IsNullOrWhiteSpace(userGroupName))
        {
            return false;
        }

        for (var i = 0; i < strategy.Datas.Count; i++)
        {
            var group = strategy.Datas[i];
            if (group != null && string.Equals(group.UserGroupName, userGroupName, StringComparison.Ordinal))
            {
                groupIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadAdStatisticsFile(
        string path,
        int expectedGroupIndex,
        out GameAB_CustomData config,
        out string errorMessage)
    {
        config = default;
        errorMessage = string.Empty;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            return RemoteAdStatisticsConfigBinarySerializer.TryDeserialize(
                File.ReadAllBytes(path),
                expectedGroupIndex,
                out config,
                out errorMessage);
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private static void SaveCommercialConfig(string path, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = path + ".tmp";
        File.WriteAllBytes(temporaryPath, bytes);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(temporaryPath, path);
    }

    private string BuildPersistentCommercialConfigPath(int fileIndex)
    {
        var scopeName = runtimeConfig.CountryGroupEnabled &&
                        AccountModule.CountryType != AccountModule.E_CountryType.None
            ? AccountModule.CountryType.ToString()
            : "Common";
        return Path.Combine(
            GetPersistentRootPath(),
            runtimeConfig.CommercialDirectoryName,
            scopeName,
            $"{runtimeConfig.CommercialConfigName}{fileIndex}.bytes");
    }
#endif

    private void LoadRuntimeConfig()
    {
        if (runtimeConfigLoaded)
        {
            return;
        }

        runtimeConfig = RemoteGroupRuntimeConfig.CreateDefault();

        try
        {
            var asset = Resources.Load<TextAsset>(RemoteGroupRuntimeConfig.ResourcesAssetName);
            if (asset == null)
            {
                GameConfigLog($"找不到运行时配置资源：Resources/{RemoteGroupRuntimeConfig.ResourcesAssetName}。");
                return;
            }

            if (!RemoteGroupConfigCrypto.TryDecryptToText(asset.text, out var configJson))
            {
                GameConfigLog($"运行时配置资源解密失败：Resources/{RemoteGroupRuntimeConfig.ResourcesAssetName}。");
                return;
            }

            var config = RemoteJsonUtility.FromJson<RemoteGroupRuntimeConfig>(configJson);
            if (config == null)
            {
                GameConfigLog($"运行时配置资源解析失败：Resources/{RemoteGroupRuntimeConfig.ResourcesAssetName}。");
                return;
            }

            runtimeConfig = config.CloneSanitized();
            var countryPrefixDescription = runtimeConfig.CountryGroupEnabled
                ? $"已开启，当前国家={AccountModule.CountryType}，实际前缀={GetCountryPrefixDescription(false)}"
                : "未开启";
            var levelCountryPrefixDescription = runtimeConfig.CountryLevelEnabled
                ? $"已开启，当前国家={AccountModule.CountryType}，实际前缀={GetCountryPrefixDescription(true)}"
                : "未开启";
            GameConfigLog($"运行时配置加载成功：分组策略地址={runtimeConfig.GroupStrategyUrl}，分组/商业化国家路径={countryPrefixDescription}，关卡国家路径={levelCountryPrefixDescription}。");
        }
        catch (Exception exception)
        {
            GameConfigLog($"运行时配置加载失败，使用默认运行时配置：{exception.Message}。");
            runtimeConfig = RemoteGroupRuntimeConfig.CreateDefault();
        }
        finally
        {
            runtimeConfigLoaded = true;
        }
    }

    private string GetCountryPrefixDescription(bool forLevelData)
    {
        var prefix = forLevelData
            ? runtimeConfig.GetLevelCountryPrefixForDisplay()
            : runtimeConfig.GetCountryPrefixForDisplay();
        return string.IsNullOrEmpty(prefix) ? "无（None 或未选择国家）" : $"/{prefix}/";
    }

    private RemoteGroupStrategy ParseStrategyPayload(byte[] payload)
    {
        if (payload == null || payload.Length == 0)
        {
            throw new InvalidDataException("远端分组策略二进制内容为空。");
        }

        var extension = Path.GetExtension(runtimeConfig.RemoteGroupDataName);
        if (!string.Equals(extension, ".bytes", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("远端分组策略只支持 .bytes 二进制文件。");
        }

        var encryptedText = Encoding.UTF8.GetString(payload);
        if (!RemoteGroupConfigCrypto.TryDecryptToText(encryptedText, out var base64Binary))
        {
            throw new InvalidDataException("远端二进制分组策略解密失败。");
        }

        byte[] strategyBytes;
        try
        {
            strategyBytes = Convert.FromBase64String(base64Binary.Trim());
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("远端二进制分组策略不是有效的 Base64 内容。", exception);
        }

        return RemoteGroupBinaryUtility.FromStrategyBytes(strategyBytes);
    }

    private async Task<RemoteGroupStrategy> LoadBundledGroupStrategy()
    {
        var countryDirectory = runtimeConfig.GetCountryPrefixForDisplay();
        var directories = string.Equals(countryDirectory, runtimeConfig.CommonDirectoryName, StringComparison.Ordinal)
            ? new[] { countryDirectory }
            : new[] { countryDirectory, runtimeConfig.CommonDirectoryName };
        foreach (var rootName in new[]
                 {
                     RemoteGroupRuntimeConfig.StreamingAssetsDirectoryName,
                     RemoteGroupRuntimeConfig.BundledContentDirectoryName
                 })
        {
            foreach (var directory in directories)
            {
                var path = Path.Combine(Application.streamingAssetsPath,
                    rootName, directory, runtimeConfig.RemoteGroupDataName);
                try
                {
                    var payload = await RequestBytes(ToStreamingAssetsUrl(path), runtimeConfig.Timeout);
                    var strategy = ParseStrategyPayload(payload);
                    GameConfigLog($"随包本地分组策略读取成功：路径={path}，字节数={payload.Length}。");
                    return strategy;
                }
                catch (Exception exception)
                {
                    GameConfigLog($"随包分组策略不可用：路径={path}，原因={exception.Message}。");
                }
            }
        }

        throw new FileNotFoundException("随包本地分组策略不存在或无法解析。");
    }

    private static bool TrySelectUserGroupName(RemoteGroupStrategy strategy, out string userGroupName)
    {
        userGroupName = string.Empty;
        if (strategy?.Datas == null) return false;

        var country = AccountModule.CountryType.ToString();
        var nameKey = StrategyAssignmentPrefsPrefix + country + ".Name";
        var fingerprintKey = StrategyAssignmentPrefsPrefix + country + ".Fingerprint";
        string fingerprint;
        using (var sha = SHA256.Create())
        {
            fingerprint = Convert.ToBase64String(sha.ComputeHash(
                RemoteGroupBinaryUtility.ToStrategyBytes(strategy)));
        }

        if (string.Equals(PlayerPrefs.GetString(fingerprintKey, string.Empty), fingerprint, StringComparison.Ordinal))
        {
            var savedName = PlayerPrefs.GetString(nameKey, string.Empty);
            foreach (var group in strategy.Datas)
            {
                if (group == null || group.Weight <= 0f
                    || !string.Equals(group.UserGroupName, savedName, StringComparison.Ordinal)) continue;
                userGroupName = savedName;
                return true;
            }
        }

        if (!strategy.TryGetUserGroupName(out userGroupName)) return false;
        PlayerPrefs.SetString(nameKey, userGroupName);
        PlayerPrefs.SetString(fingerprintKey, fingerprint);
        PlayerPrefs.Save();
        return true;
    }

    private async Task<byte[]> LoadLocalDefaultGameData()
    {
        var path = BuildLocalDefaultGameDataPath();
        var url = ToStreamingAssetsUrl(path);
        GameConfigLog($"开始读取本地 Default 二进制配置：路径={path}。");
        var gameData = await RequestBytes(url, runtimeConfig.Timeout);
        ValidateGameData(gameData, runtimeConfig.UserGroupDefaultName);
        GameConfigLog($"本地 Default 二进制配置读取成功：字节数={gameData.Length}。");
        return gameData;
    }

    private async Task<byte[]> RequestBytesWithRetry(string url)
    {
        var requestTimeout = runtimeConfig.Timeout;
        var requestRetryTimes = Mathf.Max(0, runtimeConfig.RetryTimes);
        GameConfigLog($"开始请求二进制数据：地址={url}，超时={requestTimeout}秒，重试次数={requestRetryTimes}。");

        for (var i = 0; i <= requestRetryTimes; i++)
        {
            GameConfigLog($"第 {i + 1}/{requestRetryTimes + 1} 次请求：{url}。");
            var result = await RequestBytes(url, requestTimeout);
            if (result != null && result.Length > 0)
            {
                GameConfigLog($"二进制数据请求成功：第 {i + 1} 次，字节数={result.Length}。");
                return result;
            }

            GameConfigLog($"二进制数据请求未拿到有效内容：第 {i + 1} 次。");
        }

        throw new IOException($"二进制数据重试后仍请求失败：{url}");
    }

    private static async Task<byte[]> RequestBytes(string url, float timeout)
    {
        try
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = Mathf.Max(1, Mathf.CeilToInt(timeout));
            var operation = request.SendWebRequest();
            var startTime = Time.realtimeSinceStartup;

            while (!operation.isDone)
            {
                if (Time.realtimeSinceStartup - startTime >= timeout)
                {
                    request.Abort();
                    GameConfigLog($"二进制数据请求超时：{url}。");
                    return Array.Empty<byte>();
                }

                await Task.Yield();
            }

            var isLocalUrl = url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                             || url.StartsWith("jar:", StringComparison.OrdinalIgnoreCase);
            if (request.result != UnityWebRequest.Result.Success
                || (!isLocalUrl && (request.responseCode < 200 || request.responseCode >= 300)))
            {
                GameConfigLog($"二进制数据请求失败：地址={url}，结果={request.result}，状态码={request.responseCode}，错误={request.error}。");
                return Array.Empty<byte>();
            }

            return request.downloadHandler?.data ?? Array.Empty<byte>();
        }
        catch (Exception exception)
        {
            GameConfigLog($"二进制数据请求异常：地址={url}，原因={exception.Message}。");
            return Array.Empty<byte>();
        }
    }

    private void SavePersistentStrategy(byte[] payload)
    {
        if (payload == null || payload.Length == 0)
        {
            throw new InvalidDataException("不能保存空的远端二进制分组策略。");
        }

        var directory = GetPersistentRootPath();
        Directory.CreateDirectory(directory);
        var path = BuildPersistentStrategyPath();
        File.WriteAllBytes(path, payload);
        GameConfigLog($"远端二进制分组策略已保存到沙盒：路径={path}，字节数={payload.Length}。");
    }

    private static string BuildPersistentStrategyPath()
    {
        return Path.Combine(GetPersistentRootPath(), RemoteGroupRuntimeConfig.PersistentStrategyFileName);
    }

    private void SaveRemoteGameData(string userGroupName, byte[] gameData)
    {
        ValidateGameData(gameData, userGroupName);

        var directory = Path.Combine(GetPersistentRootPath(), RemoteGroupRuntimeConfig.PersistentGameDataDirectoryName);
        Directory.CreateDirectory(directory);
        var path = BuildPersistentGameDataPath(userGroupName);
        var temporaryPath = path + ".tmp";

        File.WriteAllBytes(temporaryPath, gameData);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(temporaryPath, path);
        var savedData = File.ReadAllBytes(path);
        ValidateGameData(savedData, userGroupName);
        GameConfigLog($"远端二进制关卡配置已保存并回读校验：分组={userGroupName}，路径={path}，字节数={savedData.Length}。");
    }

    private void ClearCommittedAssignment()
    {
        try
        {
            PlayerPrefs.DeleteKey(runtimeConfig.UserGroupNamePrefsKey);
            PlayerPrefs.DeleteKey(RemoteGroupRuntimeConfig.AssignmentCompletedPrefsKey);
            PlayerPrefs.Save();
        }
        catch
        {
        }
    }

    private void ValidateGameData(byte[] gameData, string userGroupName)
    {
        if (gameData == null || gameData.Length == 0)
        {
            throw new InvalidDataException($"分组 {userGroupName} 的二进制关卡配置为空。");
        }
    }

    private static void GameConfigLog(string message)
    {
        LogLogger.LogGameConfigLoad($"{LogTag} {message}");
    }

    private string BuildLocalDefaultGameDataPath()
    {
        return Path.Combine(
            Application.streamingAssetsPath,
            RemoteGroupRuntimeConfig.StreamingAssetsDirectoryName,
            RemoteGroupRuntimeConfig.LocalDefaultGameDataFileName);
    }

    private string BuildPersistentGameDataPath(string userGroupName)
    {
        if (string.IsNullOrWhiteSpace(userGroupName))
        {
            throw new ArgumentException("用户分组名称为空。", nameof(userGroupName));
        }

        return Path.Combine(
            GetPersistentRootPath(),
            RemoteGroupRuntimeConfig.PersistentGameDataDirectoryName,
            userGroupName + ".bytes");
    }

    private static string GetPersistentRootPath()
    {
        return Path.Combine(Application.persistentDataPath, RemoteGroupRuntimeConfig.PersistentDataDirectoryName);
    }

    private static string ToStreamingAssetsUrl(string path)
    {
        if (path.Contains("://"))
        {
            return path.Replace("\\", "/");
        }

        return new Uri(path).AbsoluteUri;
    }
}

[Serializable]
public class RemoteGroupStrategy
{
    public System.Collections.Generic.List<GroupData> Datas = new System.Collections.Generic.List<GroupData>();

    public bool TryGetUserGroupName(out string userGroupName)
    {
        userGroupName = string.Empty;
        if (Datas == null || Datas.Count <= 0)
        {
            return false;
        }

        var totalWeight = 0f;
        for (var i = 0; i < Datas.Count; i++)
        {
            var data = Datas[i];
            if (data == null || string.IsNullOrWhiteSpace(data.UserGroupName) || data.Weight <= 0f)
            {
                continue;
            }

            totalWeight += data.Weight;
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        var randomWeight = UnityEngine.Random.Range(0f, totalWeight);
        var accumulatedWeight = 0f;
        for (var i = 0; i < Datas.Count; i++)
        {
            var data = Datas[i];
            if (data == null || string.IsNullOrWhiteSpace(data.UserGroupName) || data.Weight <= 0f)
            {
                continue;
            }

            accumulatedWeight += data.Weight;
            if (randomWeight <= accumulatedWeight)
            {
                userGroupName = data.UserGroupName;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public class GroupData
{
    public string UserGroupName;

    public float Weight;
}
