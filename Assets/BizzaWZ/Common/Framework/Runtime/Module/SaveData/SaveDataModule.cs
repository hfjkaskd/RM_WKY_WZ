using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 游戏数据模块
/// </summary>
public class SaveDataModule : BaseGameModule<SaveDataModule>, IUpdate
{
    public bool Enabled => true;

    private float _saveTimePoint;

    public float autoSaveInterval = 60f;

    public bool LoadingFinish => _localLoadingCount <= 0 && _gameLoadingCount <= 0 && _tempLoadingCount <= 0;
    private int _localLoadingCount;

    private static float Last_GameTimeSeconds = 0;

    [ShowInInspector, ReadOnly]
    public int LocalLoadingCount
    {
        get => _localLoadingCount;
        private set
        {
            int prev = _localLoadingCount;
            _localLoadingCount = value;
            if (prev > 0 && value <= 0)
            {
                CheckLocalLoaded();
            }
        }
    }
    
    [ShowInInspector, ReadOnly]
    private int _gameLoadingCount;
    [ShowInInspector, ReadOnly]
    private int _tempLoadingCount;
    private bool _tableLoaded;

    private bool TableLoaded
    {
        get => _tableLoaded;
        set
        {
            _tableLoaded = value;
            CheckLocalLoaded();
        }
    }
    
    public readonly List<IDataStrategy> allGameData = new();
    public readonly List<IDataStrategy> allLocalData = new();
    public readonly List<IDataStrategy> allTempData = new();

    private void InitReflection()
    {
        foreach (var field in typeof(SaveDataUtils).GetFields())
        {
            if (!field.IsStatic) continue;
            try
            {
                object obj = field.GetValue(null);
                var type = obj.GetType();
                if (type.GetGenericTypeDefinition() != typeof(DataStrategy<>)) continue;
                var strategy = (IDataStrategy)obj;
                var array = type.GenericTypeArguments;
                if (array.Length == 1)
                {
                    var genericType = array[0];
                    if (typeof(ILocalSaveData).IsAssignableFrom(genericType))
                    {
                        allLocalData.Add(strategy);
                    }
                    else if (typeof(ITempSaveData).IsAssignableFrom(genericType))
                    {
                        allTempData.Add(strategy);
                    }
                    else if (typeof(ISaveData).IsAssignableFrom(genericType))
                    {
                        allGameData.Add(strategy);
                    }
                }
            }
            catch (Exception e)
            {
                LogLogger.LogError(e);
            }
        }
    }

    private void CheckLocalLoaded()
    {
        if (LocalLoadingCount <= 0 && TableLoaded)
        {
            LoadAllGameData();
            LoadAllTempData();
            WaitForLoadingFinish().Forget();
        }
    }
    public async UniTask WaitForLoadingFinish()
    {
        while (!LoadingFinish)
        {
            await UniTask.Yield(); 
        }
        BizzaEventSystem.Emit(EventDefine.Frame.GameDataLoaded);
        if (SaveDataUtils.GameData.isNewDay)
        {
            BizzaEventSystem.Emit(EventDefine.Frame.OnNewDayLogin);
        }
    }

    private void LoadAllGameData()
    {
        foreach (var dataStrategy in allGameData)
        {
            if (dataStrategy.Loaded) continue;
            _gameLoadingCount++;
            dataStrategy.OnLoad(OnLoad);
            dataStrategy.LoadData();
        }
        return;
        
        void OnLoad()
        {
            _gameLoadingCount--;
        }
    }

    private void LoadAllLocalData()
    {
        foreach (var dataStrategy in allLocalData)
        {
            if (dataStrategy.Loaded) continue;
            LocalLoadingCount++;
            dataStrategy.OnLoad(OnLoad);
            dataStrategy.LoadData();
        }
        return;
        
        void OnLoad()
        {
            LocalLoadingCount--;
        }
    }

    private void LoadAllTempData()
    {
        foreach (var dataStrategy in allTempData)
        {
            if (dataStrategy.Loaded) continue;
            _tempLoadingCount++;
            dataStrategy.OnLoad(OnLoad);
            dataStrategy.LoadData();
        }
        return;
        
        void OnLoad()
        {
            _tempLoadingCount--;
        }
    }

    public void SaveAllGameData()
    {
        foreach (var dataStrategy in allGameData)
        {
            dataStrategy.SaveData();
        }
    }

    public void SaveAllLocalData()
    {
        foreach (var dataStrategy in allLocalData)
        {
            dataStrategy.SaveData();
        }
    }

    public void SaveAllTempData()
    {
        foreach (var dataStrategy in allTempData)
        {
            dataStrategy.SaveData();
        }
    }

    public void SaveAllData()
    {
        SaveAllGameData();
        SaveAllLocalData();
        SaveAllTempData();
    }

    private void OnTableLoad()
    {
        BizzaEventSystem.Off(EventDefine.Frame.DataTableLoaded, OnTableLoad);
        TableLoaded = true;
    }

    public override void InitGameModule()
    {
        BizzaEventSystem.On(EventDefine.Frame.DataTableLoaded, OnTableLoad);
        InitReflection();
        // LoadAllLocalData();
    }

    public void ManualLoadAllData()
    {
        // CheckLocalLoaded();
        LoadAllGameData();
        LoadAllTempData();
        WaitForLoadingFinish().Forget();
    }

    public override void ReleaseGameModule()
    {
    }

    public override void OnGameQuit()
    {
        base.OnGameQuit();
        SaveAllGameData();
        SaveAllLocalData();
    }

    public static bool dirty = false;

    public void OnUpdate(float delta)
    {
        if (_saveTimePoint < Time.time)
        {
            _saveTimePoint = Time.time + autoSaveInterval;
            dirty = true;
        }

        if (dirty)
        {
            dirty = false;
            SaveAllGameData();
        }

        // if (GameSaveData.resDirty)
        // {
        //     GameSaveData.resDirty = false;
        //     BizzaEventSystem.Emit(EventDefine.Res.ResChangedDelay, GameSaveData.resChangeMask);
        //     GameSaveData.resChangeMask = 0;
        // }

        if (SaveDataUtils.GameData != null && Time.unscaledTime - Last_GameTimeSeconds > 60)
        {
            Last_GameTimeSeconds += 60;
            SaveDataUtils.GameData.totalGameMinutes++;
            SaveDataUtils.gameStrategy.SaveData();
            // BizzaEventSystem.Emit(EventDefine.Res.PlayTimeChange);
#if UNITY_EDITOR
            Debug.Log("[统计] 累计游戏时间(min):" + SaveDataUtils.GameData.totalGameMinutes);
#endif
        }

        // BtnOnlineRewards.BeforeSave();
    }
}