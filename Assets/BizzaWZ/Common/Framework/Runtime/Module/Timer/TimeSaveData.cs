using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityExtensions;

public partial class SaveDataUtils
{
    public static readonly DataStrategy<TimeSaveData> TimeSaveStrategy = new();
    public static TimeSaveData TimeSaveData => TimeSaveStrategy.Data;
}

[Obfuz.ObfuzIgnore]
public class TimeSaveData : ISaveData
{
    public List<TimerData> list = new();
    public long lastTriggerNewDayTime;

    public void InitData()
    {

    }

    public void AfterLoadData()
    {
        TimeModule.Instance.AllTimers.Clear();
        foreach (var v in list)
        {
            TimeModule.Instance.Create(v, v.timerId, immedinateAdd:true);
        }
    }

    public void BeforeSave()
    {
        list.Clear();
        foreach (var v in TimeModule.Instance.AllTimers)
        {
            if (v.Value != null && v.Value.NeedToSave())
            {
                list.Add(v.Value._timerData);
            }
        }
    }
}
