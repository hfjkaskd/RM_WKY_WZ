#if BIZZA_REAL_WITHDRAW && COMMONGAME
using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 本地游戏数据保存工具。
///
/// 该类提供 AccountModule 和 BizzaSdk.Ad.cs 当前使用的兼容接口：
/// SaveDataUtils.GameData、SaveDataUtils.Save() 以及
/// SaveDataUtils.gameStrategy.SaveData()。
/// </summary>
public static class SaveDataUtils
{
    private const string SaveFileName = "wky_game_data.json";

    private static GameDataInfo _gameData;

    /// <summary>
    /// 当前游戏数据。首次使用时会自动从本地读取，没有存档则使用默认值。
    /// </summary>
    public static GameDataInfo GameData
    {
        get
        {
            EnsureInitialized();
            return _gameData;
        }
    }

    /// <summary>
    /// 保留现有调用方式：SaveDataUtils.gameStrategy.SaveData()。
    /// </summary>
    public static readonly SaveStrategy gameStrategy = new SaveStrategy();

    /// <summary>
    /// 从本地存档读取数据。
    /// </summary>
    public static void Load()
    {
        string savePath = GetSavePath();

        try
        {
            if (!File.Exists(savePath))
            {
                _gameData = new GameDataInfo();
                return;
            }

            string json = File.ReadAllText(savePath);
            _gameData = string.IsNullOrWhiteSpace(json)
                ? new GameDataInfo()
                : JsonUtility.FromJson<GameDataInfo>(json);

            if (_gameData == null)
            {
                _gameData = new GameDataInfo();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取本地存档失败，将使用默认数据。路径: {savePath}\n{exception.Message}");
            _gameData = new GameDataInfo();
        }
    }

    /// <summary>
    /// 将当前游戏数据保存到本地。
    /// </summary>
    public static void Save()
    {
        EnsureInitialized();

        string savePath = GetSavePath();

        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(savePath, JsonUtility.ToJson(_gameData, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"保存本地存档失败。路径: {savePath}\n{exception.Message}");
        }
    }

    private static void EnsureInitialized()
    {
        if (_gameData == null)
        {
            Load();
        }
    }

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    /// <summary>
    /// 与现有代码兼容的存档策略对象。
    /// </summary>
    public sealed class SaveStrategy
    {
        public void SaveData()
        {
            SaveDataUtils.Save();
        }
    }

    /// <summary>
    /// 当前项目已有调用所需的本地数据字段。
    /// </summary>
    [Serializable]
    public sealed class GameDataInfo
    {
        public int serviceInfoCount;
        public bool serviceAlter;
        public int todayAdTimes;
        public bool fakeWithdrawPanelFirstWithdraw;
        public int totalFinishAdTimes;
        public int totalbeLookAdCount;
        public int userLastLoginLookAdCount;
        public int userLastLoginLookRewardAdCount;
        public int userLastLoginLookInsertAdCount;
        public int userTodayLookAdCount;
        public int userLookDailyAdCountMax;
        public int userLookDailyAdCount;
        public double totalAdEcpmValue;

        public int playerSelectedLv;
        public int tutorialStep;
        public int btnDailyTaskClick;
        public int btnWithdrawClick;
        public int btnDailyTimeClick;
        public int levelAttemptCount;
        public int levelReviveCount;
        public int OperaStep;
        public long levelStartTime;
        public long lastWithdrawTime;

        // JsonUtility 不直接序列化 DateTime，因此用 ticks 持久化。
        public long lastGetDailyRewardTimeTicks;

        public DateTime lastGetDailyRewardTime
        {
            get
            {
                if (lastGetDailyRewardTimeTicks <= 0)
                {
                    return DateTime.MinValue;
                }

                try
                {
                    return new DateTime(lastGetDailyRewardTimeTicks, DateTimeKind.Local);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return DateTime.MinValue;
                }
            }
            set
            {
                lastGetDailyRewardTimeTicks = value == DateTime.MinValue ? 0 : value.Ticks;
            }
        }
    }
}
#endif
