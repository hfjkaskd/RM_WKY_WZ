using System;
using System.Collections.Generic;

[Obfuz.ObfuzIgnore]
public enum E_AllTaskType
{
    None = 0,
    Activity = 1,
    DailyLogin = 2,
    FinishLevel = 3,
    OnlineTime = 4,
    WatchAd = 5,
    FinishBingo = 6,
    MatchItem = 7,
    TalentLevelUp = 8,
    KillMonster = 9,
    OpenEquipTreature = 10,
    OpenActorTreature = 11,
    WipeAmount = 12,
    BuyGold = 13,
    BuyShop = 14,
    EnterBattle = 15,
    DailyPlayTime = 16,
    RoleStarUp = 17,
    EquipMerge = 18,
    ActivityAmount = 19,
    CollectionReward = 20,
    EquipAmount = 21,
    EquipLevelUp = 22,
    LevelFinish = 23,
    OfflineReward = 24,
    OpenTowerBox = 25,
    RefreshDailyShop = 26,
    TalentLevelTo = 27,
    UnlockPlane = 28,
    WaveFinish = 29,
    BuyShop2 = 30,
}

[Serializable]
public struct vector2
{
    public float X;
    public float Y;

    public vector2(float x, float y)
    {
        X = x;
        Y = y;
    }
}

[Serializable]
public struct StringFloat
{
    public string Key;
    public float FloatVal;

    public StringFloat(string key, float floatVal)
    {
        Key = key;
        FloatVal = floatVal;
    }
}

[Serializable]
public struct IntIntString
{
    public int X;
    public int Y;
    public string Id;

    public IntIntString(int x, int y, string id)
    {
        X = x;
        Y = y;
        Id = id;
    }
}

public partial struct ItemEntry
{
    internal static ItemEntry Read(SimpleConfigBinaryReader reader)
    {
        return new ItemEntry
        {
            Type = (E_ItemType)reader.ReadInt32(),
            Count = reader.ReadSingle(),
        };
    }
}

[Serializable]
public sealed class GlobalConfig
{
    public List<StringFloat> LanguageInfos;
    public List<IntIntString> ComboTips;
    public float ComboCd;
    public float FlyToAreaSpeed;
    public int MoneyItemId;
    public float DefaultDollarNum;
    public float AdDollarNum;
    public int AdDollarPoss;
    public float AdDollarMinCd;
    public int AdDollarMaxTimes;
    public int MultDollarPoss;
    public vector2 MultDollarRange;
    public int InterAdPoss;
    public int InterAdMaxTimes;
    public float FlowDollarNum;
    public vector2 DollarRange;
    public int CombineCoinNum;
    public int AdCoinNum;
    public float FragmentDestroyDuration;
    public int PropUseMaxTimes;
    public int ReviveMaxTimes;

    internal static GlobalConfig Read(SimpleConfigBinaryReader reader)
    {
        var config = new GlobalConfig
        {
            LanguageInfos = new List<StringFloat>(reader.ReadCount()),
        };

        for (var i = 0; i < config.LanguageInfos.Capacity; i++)
        {
            config.LanguageInfos.Add(new StringFloat(reader.ReadString(), reader.ReadSingle()));
        }

        config.ComboTips = new List<IntIntString>(reader.ReadCount());
        for (var i = 0; i < config.ComboTips.Capacity; i++)
        {
            config.ComboTips.Add(new IntIntString(reader.ReadInt32(), reader.ReadInt32(), reader.ReadString()));
        }

        config.ComboCd = reader.ReadSingle();
        config.FlyToAreaSpeed = reader.ReadSingle();
        config.MoneyItemId = reader.ReadInt32();
        config.DefaultDollarNum = reader.ReadSingle();
        config.AdDollarNum = reader.ReadSingle();
        config.AdDollarPoss = reader.ReadInt32();
        config.AdDollarMinCd = reader.ReadSingle();
        config.AdDollarMaxTimes = reader.ReadInt32();
        config.MultDollarPoss = reader.ReadInt32();
        config.MultDollarRange = reader.ReadVector2();
        config.InterAdPoss = reader.ReadInt32();
        config.InterAdMaxTimes = reader.ReadInt32();
        config.FlowDollarNum = reader.ReadSingle();
        config.DollarRange = reader.ReadVector2();
        config.CombineCoinNum = reader.ReadInt32();
        config.AdCoinNum = reader.ReadInt32();
        config.FragmentDestroyDuration = reader.ReadSingle();
        config.PropUseMaxTimes = reader.ReadInt32();
        config.ReviveMaxTimes = reader.ReadInt32();
        return config;
    }
}

[Serializable]
public sealed class LanguageConfig
{
    public string Id;
    public Dictionary<string, string> Dict;

    internal static LanguageConfig Read(SimpleConfigBinaryReader reader)
    {
        var id = reader.ReadString();
        var dictCount = reader.ReadCount();
        var config = new LanguageConfig
        {
            Id = id,
            Dict = new Dictionary<string, string>(dictCount),
        };

        for (var i = 0; i < dictCount; i++)
        {
            config.Dict.Add(reader.ReadString(), reader.ReadString());
        }

        return config;
    }
}

[Serializable]
public sealed class LangNumConfig
{
    public string Lang;
    public List<int> NumList;
    public List<string> TextList;

    internal static LangNumConfig Read(SimpleConfigBinaryReader reader)
    {
        var config = new LangNumConfig
        {
            Lang = reader.ReadString(),
            NumList = new List<int>(reader.ReadCount()),
        };

        for (var i = 0; i < config.NumList.Capacity; i++)
        {
            config.NumList.Add(reader.ReadInt32());
        }

        config.TextList = new List<string>(reader.ReadCount());
        for (var i = 0; i < config.TextList.Capacity; i++)
        {
            config.TextList.Add(reader.ReadString());
        }

        return config;
    }
}

[Serializable]
public sealed class TblCommonWzTextureConfig
{
    public string Name;
    public string Path;

    internal static TblCommonWzTextureConfig Read(SimpleConfigBinaryReader reader)
    {
        return new TblCommonWzTextureConfig
        {
            Name = reader.ReadString(),
            Path = reader.ReadString(),
        };
    }
}

[Serializable]
public sealed class TblWzCountryTextureConfig
{
    public string Name;
    public string BRPath;
    public string IDPath;
    public string USPath;

    internal static TblWzCountryTextureConfig Read(SimpleConfigBinaryReader reader)
    {
        return new TblWzCountryTextureConfig
        {
            Name = reader.ReadString(),
            BRPath = reader.ReadString(),
            IDPath = reader.ReadString(),
            USPath = reader.ReadString(),
        };
    }
}

[Serializable]
public sealed class DailyTaskConfig
{
    public string Id;
    public int RefreshDays;
    public string Description;
    public E_AllTaskType CompleteConditions;
    public int ConditionValues;
    public List<ItemEntry> RewardsList;
    public bool AdsFinish;

    internal static DailyTaskConfig Read(SimpleConfigBinaryReader reader)
    {
        var config = new DailyTaskConfig
        {
            Id = reader.ReadString(),
            RefreshDays = reader.ReadInt32(),
            Description = reader.ReadString(),
            CompleteConditions = (E_AllTaskType)reader.ReadInt32(),
            ConditionValues = reader.ReadInt32(),
            RewardsList = new List<ItemEntry>(reader.ReadCount()),
        };

        for (var i = 0; i < config.RewardsList.Capacity; i++)
        {
            config.RewardsList.Add(ItemEntry.Read(reader));
        }

        config.AdsFinish = reader.ReadBoolean();
        return config;
    }
}

[Serializable]
public sealed class ActivityTaskConfig
{
    public string Id;
    public int RefreshDays;
    public E_ItemType Item;
    public int Number;
    public List<ItemEntry> RewardsList;

    internal static ActivityTaskConfig Read(SimpleConfigBinaryReader reader)
    {
        var config = new ActivityTaskConfig
        {
            Id = reader.ReadString(),
            RefreshDays = reader.ReadInt32(),
            Item = (E_ItemType)reader.ReadInt32(),
            Number = reader.ReadInt32(),
            RewardsList = new List<ItemEntry>(reader.ReadCount()),
        };

        for (var i = 0; i < config.RewardsList.Capacity; i++)
        {
            config.RewardsList.Add(ItemEntry.Read(reader));
        }

        return config;
    }
}

// 教程表数据已从当前 Excel 配置中移除。保留空类型只是为了兼容现有教程模块的容器访问，
// 不参与导表，也不会生成资源文件。
[Serializable]
public sealed class TeachConfig
{
    public string Id;
}
