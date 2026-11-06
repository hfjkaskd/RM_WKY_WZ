#if BIZZA_REAL_WITHDRAW
using cfg;
using System;
using System.Collections.Generic;
using BizzaCommon;
using UnityEngine;

 
public enum E_TaskState
{
    Complete,
    Uncomplete,
    Rewarded,
}

public abstract class BaseRuntimeTaskInfo : IPoolObject
{
    //public const int TYPE_ID_SCALE = 1000000;
    public string TaskId;
    public bool hideDetail = false;
    protected int m_currentProgress;
    protected int m_targetProgress;
    public bool rewarded = false;

    public event Action onUpdateProgress;

    public E_TaskState TaskState
    {
        get
        {
            if (rewarded) return E_TaskState.Rewarded;
            if (CurrentProgress < TargetProgress) return E_TaskState.Uncomplete;
            return E_TaskState.Complete;
        }
    }

    /// <summary>
    /// 当前进度
    /// </summary>
    public int CurrentProgress
    {
        get
        {
            if (!hideDetail)
            {
                return m_currentProgress;
            }
            else
            {
                return m_currentProgress < m_targetProgress ? 0 : 1;
            }
        }
        set { m_currentProgress = value; }
    }

    /// <summary>
    /// 目标进度
    /// </summary>
    public int TargetProgress
    {
        get
        {
            if (!hideDetail)
            {
                return m_targetProgress;
            }
            else
            {
                return 1;
            }
        }
        set { m_targetProgress = value; }
    }

    /// <summary>
    /// 任务类型
    /// </summary>
    public E_AllTaskType TaskType;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool Active = true;

    /// <summary>
    /// 任务更新器
    /// </summary>
    public MenuSys_Task.BaseRuntimeTaskUpdater updater;

    public void TaskStart()
    {
        OnTaskInit();
        UpdateProgress(0);
    }

    public void TaskFinish()
    {
        OnTaskFinish();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="num">增加的值</param>
    public void UpdateProgress(int modifiedNum = 1)
    {
        if (!Active) return;
        int lastCurrentProgresss = CurrentProgress;
        updater.UpdateProgress(this, modifiedNum);

        if (CurrentProgress == lastCurrentProgresss)
            return;

        if (CurrentProgress > TargetProgress)
        {
            CurrentProgress = TargetProgress;
        }

        if (modifiedNum > 0)
        {
            OnUpdateProgress();
            onUpdateProgress?.Invoke();
        }
    }

    public bool CanGetReward()
    {
        return !rewarded;
    }

    public void GetReward(Vector3 pos)
    {
        if (!CanGetReward()) return;

        rewarded = true;
        OnTaskRewarded(pos);
        Record();
    }

    /// <summary>
    /// 写存档
    /// </summary>
    public virtual void Record()
    {
        var data = SaveDataUtils.TaskData;
        if (data.taskRecordDataDic.TryGetValue(TaskId, out TaskRecordData recordData))
        {
            recordData.CurrentProgress = CurrentProgress;
            recordData.rewarded = rewarded;
        }
        else
        {
            TaskRecordData taskRecordData = GetTaskRecord();
            taskRecordData.TaskId = TaskId;
            taskRecordData.CurrentProgress = CurrentProgress;
            taskRecordData.rewarded = rewarded;
            data.taskRecordDataDic.Add(TaskId, taskRecordData);
        }

        SaveDataModule.Instance.SaveAllData();
    }

    /// <summary>
    /// 读存档
    /// </summary>
    public void LoadRecord()
    {
        var data = SaveDataUtils.TaskData;
        if (data.taskRecordDataDic.TryGetValue(TaskId, out TaskRecordData value))
        {
            CurrentProgress = value.CurrentProgress;
            rewarded = value.rewarded;
        }
    }

    /// <summary>
    /// 删存档
    /// </summary>
    public void RemoveRecord()
    {
        //int dataKey = (int)RuntimeTypeId * TYPE_ID_SCALE + TaskId;
        var data = SaveDataUtils.TaskData;
        data.taskRecordDataDic.Remove(TaskId);
    }

    /// <summary>
    /// 任务初始化时
    /// </summary>
    protected abstract void OnTaskInit();

    /// <summary>
    /// 任务完成时（切换到Complete状态）
    /// </summary>
    protected abstract void OnTaskFinish();

    /// <summary>
    /// 任务领奖时（切换到Rewarded状态）
    /// </summary>
    protected abstract void OnTaskRewarded(Vector3 pos);

    /// <summary>
    /// 进度更新时（每次进度发生变化时）
    /// </summary>
    protected abstract void OnUpdateProgress();

    protected abstract TaskRecordData GetTaskRecord();

    public virtual void GetFromPool()
    {
        // CurrentProgress = default;
        // rewarded = default;
        // TaskId = default;
    }

    public virtual void ReleaseToPool()
    {
    }

    public void CreateByPool()
    {
    }

    public void DisposeByPool()
    {
    }
}

public partial class MenuSys_Task : MenuSystemBase<MenuSys_Task>
{
    /// <summary>
    /// 创建一个RuntimeTaskInfo， 运行时逻辑不会保存全部数据，只有需要的任务才会独立保存，否则统一计算进度
    /// 进度计算规则由 任务类型 对应的 Updater 控制。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="taskId">任务Id</param>
    /// <param name="taskType">任务类型</param>
    /// <param name="targetProgress">目标进度</param>
    /// <param name="autoLoadRecord">是否自动加载进度目标进度</param>
    /// <param name="recordTogether">统一计数，进度会统一计算。否则会独立计算任务进度</param>
    /// <returns></returns>
    public T CreateTaskInfo<T>(string taskId, E_AllTaskType taskType, int targetProgress, bool autoLoadRecord,
        bool recordTogether = false)
        where T : BaseRuntimeTaskInfo, new()
    {
        T taskInfo = ObjectPoolUtility.GetFromPool<T>();
        taskInfo.TaskId = taskId;
        taskInfo.TaskType = taskType;
        taskInfo.CurrentProgress = 0;
        taskInfo.TargetProgress = targetProgress;
        taskInfo.updater = GetRuntimeUpdaterByType(taskType, recordTogether);
        taskInfo.rewarded = false;

        if (autoLoadRecord)
            taskInfo.LoadRecord();
        if (allTaskDic.TryGetValue(taskType, out var taskList))
        {
            taskList.Add(taskInfo);
        }
        else
        {
            allTaskDic.Add(taskType, new List<BaseRuntimeTaskInfo> { taskInfo });
        }

        return taskInfo;
    }

    /// <summary>
    /// 清除一个RuntimeTaskInfo
    /// 需要一个更便利的快速删除方法
    /// </summary>
    /// <param name="runtimeTaskInfo"></param>
    /// <returns></returns>
    public bool RemoveTaskInfo<T>(T runtimeTaskInfo)
        where T : BaseRuntimeTaskInfo
    {
        runtimeTaskInfo.RemoveRecord();
        if (allTaskDic.TryGetValue(runtimeTaskInfo.TaskType, out var taskList))
        {
            ObjectPoolUtility.ReleaseToPool(runtimeTaskInfo);
            return taskList.Remove(runtimeTaskInfo);
        }

        ObjectPoolUtility.ReleaseToPool(runtimeTaskInfo);
        return false;
    }

    public bool RemoveTaskInfoById(string taskId, E_AllTaskType allTaskType)
    {
        if (allTaskDic.TryGetValue(allTaskType, out var taskList))
        {
            for (int i = 0; i < taskList.Count; i++)
            {
                if (taskList[i].TaskId == taskId)
                {
                    ObjectPoolUtility.ReleaseToPool(taskList[i]);
                    taskList.RemoveAt(i);
                    break;
                }
            }
        }

        _Data.taskRecordDataDic.Remove(taskId);

        return true;
    }
}
#endif
