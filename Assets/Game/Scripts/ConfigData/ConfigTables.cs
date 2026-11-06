using System;
using System.Collections.Generic;

namespace cfg
{
    public sealed class TblGlobal
    {
        public bool Inited { get; private set; }
        public GlobalConfig Data { get; private set; }

        public List<StringFloat> LanguageInfos => Data?.LanguageInfos;
        public List<IntIntString> ComboTips => Data?.ComboTips;
        public float ComboCd => Data == null ? 0f : Data.ComboCd;
        public float FlyToAreaSpeed => Data == null ? 0f : Data.FlyToAreaSpeed;
        public int MoneyItemId => Data == null ? 0 : Data.MoneyItemId;
        public float DefaultDollarNum => Data == null ? 0f : Data.DefaultDollarNum;
        public float AdDollarNum => Data == null ? 0f : Data.AdDollarNum;
        public int AdDollarPoss => Data == null ? 0 : Data.AdDollarPoss;
        public float AdDollarMinCd => Data == null ? 0f : Data.AdDollarMinCd;
        public int AdDollarMaxTimes => Data == null ? 0 : Data.AdDollarMaxTimes;
        public int MultDollarPoss => Data == null ? 0 : Data.MultDollarPoss;
        public vector2 MultDollarRange => Data == null ? default : Data.MultDollarRange;
        public int InterAdPoss => Data == null ? 0 : Data.InterAdPoss;
        public int InterAdMaxTimes => Data == null ? 0 : Data.InterAdMaxTimes;
        public float FlowDollarNum => Data == null ? 0f : Data.FlowDollarNum;
        public vector2 DollarRange => Data == null ? default : Data.DollarRange;
        public int CombineCoinNum => Data == null ? 0 : Data.CombineCoinNum;
        public int AdCoinNum => Data == null ? 0 : Data.AdCoinNum;
        public float FragmentDestroyDuration => Data == null ? 0f : Data.FragmentDestroyDuration;
        public int PropUseMaxTimes => Data == null ? 0 : Data.PropUseMaxTimes;
        public int ReviveMaxTimes => Data == null ? 0 : Data.ReviveMaxTimes;

        public void Init(byte[] bytes)
        {
            using (var reader = new SimpleConfigBinaryReader(bytes, "tblglobal"))
            {
                var count = reader.ReadCount();
                if (count != 1)
                {
                    throw new InvalidOperationException($"tblglobal expects one row, got {count}.");
                }

                Data = GlobalConfig.Read(reader);
                Inited = true;
            }
        }

        public void Clear()
        {
            Data = null;
            Inited = false;
        }
    }

    public sealed class TblLanguage
    {
        public bool Inited { get; private set; }
        public Dictionary<string, LanguageConfig> DataMap { get; private set; }
        public List<LanguageConfig> DataList { get; private set; }

        public void Init(byte[] bytes)
        {
            using (var reader = new SimpleConfigBinaryReader(bytes, "tbllanguage"))
            {
                DataMap = new Dictionary<string, LanguageConfig>();
                DataList = new List<LanguageConfig>(reader.ReadCount());
                for (var i = 0; i < DataList.Capacity; i++)
                {
                    var value = LanguageConfig.Read(reader);
                    DataList.Add(value);
                    DataMap.Add(value.Id, value);
                }

                Inited = true;
            }
        }

        public void Clear()
        {
            DataMap?.Clear();
            DataList?.Clear();
            DataMap = null;
            DataList = null;
            Inited = false;
        }

        public LanguageConfig GetOrDefault(string key) => DataMap != null && DataMap.TryGetValue(key, out var value) ? value : null;
        public LanguageConfig Get(string key) => DataMap[key];
        public LanguageConfig this[string key] => DataMap[key];
    }

    public sealed class TblLangNum
    {
        public bool Inited { get; private set; }
        public Dictionary<string, LangNumConfig> DataMap { get; private set; }
        public List<LangNumConfig> DataList { get; private set; }

        public void Init(byte[] bytes)
        {
            using (var reader = new SimpleConfigBinaryReader(bytes, "tbllangnum"))
            {
                DataMap = new Dictionary<string, LangNumConfig>();
                DataList = new List<LangNumConfig>(reader.ReadCount());
                for (var i = 0; i < DataList.Capacity; i++)
                {
                    var value = LangNumConfig.Read(reader);
                    DataList.Add(value);
                    DataMap.Add(value.Lang, value);
                }

                Inited = true;
            }
        }

        public void Clear()
        {
            DataMap?.Clear();
            DataList?.Clear();
            DataMap = null;
            DataList = null;
            Inited = false;
        }

        public LangNumConfig GetOrDefault(string key) => DataMap != null && DataMap.TryGetValue(key, out var value) ? value : null;
        public LangNumConfig Get(string key) => DataMap[key];
        public LangNumConfig this[string key] => DataMap[key];
    }

    public sealed class TblWzCountryTexture
    {
        public bool Inited { get; private set; }
        public Dictionary<string, TblWzCountryTextureConfig> DataMap { get; private set; }
        public List<TblWzCountryTextureConfig> DataList { get; private set; }

        public void Init(byte[] bytes)
        {
            using (var reader = new SimpleConfigBinaryReader(bytes, "tblwzcountrytexture"))
            {
                DataMap = new Dictionary<string, TblWzCountryTextureConfig>();
                DataList = new List<TblWzCountryTextureConfig>(reader.ReadCount());
                for (var i = 0; i < DataList.Capacity; i++)
                {
                    var value = TblWzCountryTextureConfig.Read(reader);
                    DataList.Add(value);
                    DataMap.Add(value.Name, value);
                }

                Inited = true;
            }
        }

        public void Clear()
        {
            DataMap?.Clear();
            DataList?.Clear();
            DataMap = null;
            DataList = null;
            Inited = false;
        }

        public TblWzCountryTextureConfig GetOrDefault(string key) => DataMap != null && DataMap.TryGetValue(key, out var value) ? value : null;
        public TblWzCountryTextureConfig Get(string key) => DataMap[key];
        public TblWzCountryTextureConfig this[string key] => DataMap[key];
    }

    public sealed class TblCommonWzTexture
    {
        public bool Inited { get; private set; }
        public Dictionary<string, TblCommonWzTextureConfig> DataMap { get; private set; }
        public List<TblCommonWzTextureConfig> DataList { get; private set; }

        public void Init(byte[] bytes)
        {
            using (var reader = new SimpleConfigBinaryReader(bytes, "tblcommonwztexture"))
            {
                DataMap = new Dictionary<string, TblCommonWzTextureConfig>();
                DataList = new List<TblCommonWzTextureConfig>(reader.ReadCount());
                for (var i = 0; i < DataList.Capacity; i++)
                {
                    var value = TblCommonWzTextureConfig.Read(reader);
                    DataList.Add(value);
                    DataMap.Add(value.Name, value);
                }

                Inited = true;
            }
        }

        public void Clear()
        {
            DataMap?.Clear();
            DataList?.Clear();
            DataMap = null;
            DataList = null;
            Inited = false;
        }

        public TblCommonWzTextureConfig GetOrDefault(string key) => DataMap != null && DataMap.TryGetValue(key, out var value) ? value : null;
        public TblCommonWzTextureConfig Get(string key) => DataMap[key];
        public TblCommonWzTextureConfig this[string key] => DataMap[key];
    }

    public sealed class TblDailyTaskConfig
    {
        public bool Inited { get; private set; }
        public Dictionary<string, DailyTaskConfig> DataMap { get; private set; }
        public List<DailyTaskConfig> DataList { get; private set; }

        public void Init(byte[] bytes)
        {
            using (var reader = new SimpleConfigBinaryReader(bytes, "tbldailytaskconfig"))
            {
                DataMap = new Dictionary<string, DailyTaskConfig>();
                DataList = new List<DailyTaskConfig>(reader.ReadCount());
                for (var i = 0; i < DataList.Capacity; i++)
                {
                    var value = DailyTaskConfig.Read(reader);
                    DataList.Add(value);
                    DataMap.Add(value.Id, value);
                }

                Inited = true;
            }
        }

        public void Clear()
        {
            DataMap?.Clear();
            DataList?.Clear();
            DataMap = null;
            DataList = null;
            Inited = false;
        }

        public DailyTaskConfig GetOrDefault(string key) => DataMap != null && DataMap.TryGetValue(key, out var value) ? value : null;
        public DailyTaskConfig Get(string key) => DataMap[key];
        public DailyTaskConfig this[string key] => DataMap[key];
    }

    public sealed class TblActivityTaskConfig
    {
        public bool Inited { get; private set; }
        public Dictionary<string, ActivityTaskConfig> DataMap { get; private set; }
        public List<ActivityTaskConfig> DataList { get; private set; }

        public void Init(byte[] bytes)
        {
            using (var reader = new SimpleConfigBinaryReader(bytes, "tblactivitytaskconfig"))
            {
                DataMap = new Dictionary<string, ActivityTaskConfig>();
                DataList = new List<ActivityTaskConfig>(reader.ReadCount());
                for (var i = 0; i < DataList.Capacity; i++)
                {
                    var value = ActivityTaskConfig.Read(reader);
                    DataList.Add(value);
                    DataMap.Add(value.Id, value);
                }

                Inited = true;
            }
        }

        public void Clear()
        {
            DataMap?.Clear();
            DataList?.Clear();
            DataMap = null;
            DataList = null;
            Inited = false;
        }

        public ActivityTaskConfig GetOrDefault(string key) => DataMap != null && DataMap.TryGetValue(key, out var value) ? value : null;
        public ActivityTaskConfig Get(string key) => DataMap[key];
        public ActivityTaskConfig this[string key] => DataMap[key];
    }

    public sealed class TblTeach
    {
        public bool Inited { get; private set; } = true;
        public Dictionary<string, TeachConfig> DataMap { get; } = new Dictionary<string, TeachConfig>();
        public List<TeachConfig> DataList { get; } = new List<TeachConfig>();

        public void Clear()
        {
            DataMap.Clear();
            DataList.Clear();
            Inited = false;
        }
    }

    public sealed class Tables
    {
        public static Tables Instance { get; } = new Tables();

        public TblGlobal TblGlobal { get; } = new TblGlobal();
        public TblLanguage TblLanguage { get; } = new TblLanguage();
        public TblLangNum TblLangNum { get; } = new TblLangNum();
        public TblWzCountryTexture TblWzCountryTexture { get; } = new TblWzCountryTexture();
        public TblCommonWzTexture TblCommonWzTexture { get; } = new TblCommonWzTexture();
        public TblDailyTaskConfig TblDailyTaskConfig { get; } = new TblDailyTaskConfig();
        public TblActivityTaskConfig TblActivityTaskConfig { get; } = new TblActivityTaskConfig();
        public TblTeach TblTeach { get; } = new TblTeach();

        private Tables()
        {
        }

        public void InitTable(string tableName, byte[] bytes)
        {
            switch (tableName.ToLowerInvariant())
            {
                case "tblglobal": TblGlobal.Init(bytes); break;
                case "tbllanguage": TblLanguage.Init(bytes); break;
                case "tbllangnum": TblLangNum.Init(bytes); break;
                case "tblwzcountrytexture": TblWzCountryTexture.Init(bytes); break;
                case "tblcommonwztexture": TblCommonWzTexture.Init(bytes); break;
                case "tbldailytaskconfig": TblDailyTaskConfig.Init(bytes); break;
                case "tblactivitytaskconfig": TblActivityTaskConfig.Init(bytes); break;
                default: throw new ArgumentException($"Unknown config table: {tableName}");
            }
        }

        public void ClearTable(string tableName)
        {
            switch (tableName.ToLowerInvariant())
            {
                case "tblglobal": TblGlobal.Clear(); break;
                case "tbllanguage": TblLanguage.Clear(); break;
                case "tbllangnum": TblLangNum.Clear(); break;
                case "tblwzcountrytexture": TblWzCountryTexture.Clear(); break;
                case "tblcommonwztexture": TblCommonWzTexture.Clear(); break;
                case "tbldailytaskconfig": TblDailyTaskConfig.Clear(); break;
                case "tblactivitytaskconfig": TblActivityTaskConfig.Clear(); break;
                default: throw new ArgumentException($"Unknown config table: {tableName}");
            }
        }

        public void ClearTables()
        {
            TblGlobal.Clear();
            TblLanguage.Clear();
            TblLangNum.Clear();
            TblWzCountryTexture.Clear();
            TblCommonWzTexture.Clear();
            TblDailyTaskConfig.Clear();
            TblActivityTaskConfig.Clear();
            TblTeach.Clear();
        }
    }
}
