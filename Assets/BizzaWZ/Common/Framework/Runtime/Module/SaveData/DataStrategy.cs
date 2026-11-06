using System;
using System.Collections.Generic;
using Bizza;
using UnityEngine;
using Newtonsoft.Json;

public interface IDataStrategy
{
    public bool Loaded { get; }
    void LoadData();
    void SaveData();
    void OnLoad(Action callback);
}

public class DataStrategy<T> : IDataStrategy where T : ISaveData
{
    private static DataStrategy<T> _instance;
    public static DataStrategy<T> Instance => _instance;

    private static readonly HashSet<string> _saveKeys = new();

    public readonly string key;

    public DataStrategy()
    {
        key = TypeUtil.GetName(typeof(T));
        if (!_saveKeys.Add(key))
        {
            throw new ArgumentException($"Key is already in use. key is {key}");
        }
        _instance ??= this;
    }

    public T Data { get; private set; }
    public bool Loaded { get; private set; }

    private Action onLoad;

    public void OnLoad(Action callback)
    {
        onLoad += callback;
    }

    public void OffLoad(Action callback)
    {
        onLoad -= callback;
    }

    public bool FirstLoad { get; private set; }

    private bool _loading = false;

    public void LoadData()
    {
        if (!_loading)
        {
            _loading = true;
            InternalLoadData();
        }
    }

    protected async void InternalLoadData()
    {
        T data;
        bool hasKey = false;
        string _info = PlayerPrefs.GetString(key, "");
        hasKey = !string.IsNullOrEmpty(_info);

        if (!hasKey)
        {
            LogLogger.LogInfo($"LoadLocalDataStrategy: Data Is Null. Key is {key}");
            data = Activator.CreateInstance<T>();
            data.InitData();
            FirstLoad = true;
        }
        else
        {
            string jsonStr;
            jsonStr = PlayerPrefs.GetString(key);
            try
            {
                data = JsonConvert.DeserializeObject<T>(jsonStr, new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                });
                FirstLoad = false;
            }
            catch (Exception e)
            {
                LogLogger.LogError($"数据转换失败: {e.Message}");
                data = Activator.CreateInstance<T>();
                data.InitData();
                FirstLoad = true;
            }

            LogLogger.LogInfo($"DataStrategy.LoadData key:::{key}\njson:::{jsonStr}");
        }

        Data = data;
        Loaded = true;
        data.AfterLoadData();
        onLoad?.Invoke();
        onLoad = null;
    }

    public async void SaveData()
    {
        SaveDataImmediately();
    }

    // Reward transactions need to observe persistence failures before publishing UI events.
    public void SaveDataImmediately()
    {
        if (Data == null)
        {
            return;
        }

        Data.BeforeSave();

        string jsonStr = JsonConvert.SerializeObject(Data, new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        });

        PlayerPrefs.SetString(key, jsonStr);

        PlayerPrefs.Save();
        // LogUtil.Info($"DataStrategy.SaveData key:::{key}\njson:::{Platform.Instance.SaveSystem.GetString(key)}");
    }
}
