#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class SaveDataUtils
{
    // public static TaskSaveData taskSaveData = new();
    public static readonly DataStrategy<TaskSaveData> taskStrategy = new();
    public static TaskSaveData TaskData => taskStrategy.Data;
}

[System.Serializable]
public class TaskRecordData
{
    public TaskRecordData()
    {
    }

    public string TaskId;
    public int CurrentProgress;
    public bool rewarded;
}

 
public class TaskSaveData : ISaveData
{
    public void AfterLoadData()
    {
        taskRecordDataDic.Clear();
        for (int i = 0; i < taskRecordDatas.Count; i++)
        {
            taskRecordDataDic.Add(taskRecordDatas[i].TaskId, taskRecordDatas[i]);
        }
    }

    public void InitData()
    {
    }

    // 主线任务
    public TaskRecordData mainTaskRecord;
    public int taskStartDay = -1;
    // 任务数据
    public Dictionary<string, TaskRecordData> taskRecordDataDic = new();
    public List<TaskRecordData> taskRecordDatas = new();

    public void BeforeSave()
    {
        taskRecordDatas.Clear();
        foreach (var taskRecordData in taskRecordDataDic.Values)
        {
            taskRecordDatas.Add(taskRecordData);
        }
    }
}
#endif
