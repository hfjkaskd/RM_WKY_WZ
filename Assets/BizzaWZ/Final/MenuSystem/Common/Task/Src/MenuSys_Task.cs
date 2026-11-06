#if BIZZA_REAL_WITHDRAW
using cfg;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public partial class MenuSys_Task : MenuSystemBase<MenuSys_Task>, IUpdate
{
    private TaskSaveData _Data => SaveDataUtils.TaskData;

    #region Daily 每日任务

    public class RuntimeDailyTaskInfo : BaseRuntimeTaskInfo
    {
        public DailyTaskConfig config;


        public string RedPointKey
        {
            get
            {
                var tmp = config.RefreshDays == 1 ? "daily" : "weekly";
                return $"root.task.{tmp}.{TaskId}";
            }
        }

        protected override TaskRecordData GetTaskRecord()
        {
            TaskRecordData recordData = new TaskRecordData();
            return recordData;
        }

        protected override void OnTaskFinish()
        {
        }

        protected override void OnTaskInit()
        {
        }

        protected override void OnUpdateProgress()
        {
            Instance.Update_TaskRedpoint();
            BizzaEventSystem.Emit(EventDefine.Task.DailyTaskUpdateProgress, config.Id);
        }

        protected override void OnTaskRewarded(Vector3 pos)
        {
            
        }
    }

    public Dictionary<string, RuntimeDailyTaskInfo> dailyTaskInfos = new();

    #endregion

    #region 活跃任务

    public class RuntimeActivityTaskInfo : BaseRuntimeTaskInfo
    {
        public ActivityTaskConfig config;

        protected override TaskRecordData GetTaskRecord()
        {
            TaskRecordData recordData = new TaskRecordData();
            return recordData;
        }

        protected override void OnTaskFinish()
        {
        }

        protected override void OnTaskInit()
        {
        }

        protected override void OnUpdateProgress()
        {
            Instance.Update_TaskRedpoint();
        }

        protected override void OnTaskRewarded(Vector3 pos)
        {
            // // //处理领奖
            // for (int i = 0; i < config.RewardsList.Count; i++)
            // {
            //     ItemUtils.AddItem(config.RewardsList[i], true, false, pos);
            // }
            //
            // MenuSys_GetReward.Instance.ShowGetRewardPanel();
            // //广播消息
            // Instance.Update_TaskRedpoint();
            // EventModule.BroadCast(EventDefine.Task.DailyActivityTaskReward, config.Id);

            // Bizza.Analytics.Manager.AddCommonParams();
            // Bizza.Analytics.Manager.AddParam("task_id", config.Id);
            // Bizza.Analytics.Manager.AddParam("task_type", config.RefreshDays);
            // Bizza.Analytics.Manager.SendCustomEvent(Bizza.Analytics.EventName.TaskReward_Activity);
        }
    }

    public Dictionary<string, RuntimeActivityTaskInfo> dailyActivityTaskInfos = new();

    #endregion

    #region 周常任务

    #endregion


    private Dictionary<E_AllTaskType, List<BaseRuntimeTaskInfo>> allTaskDic =
        new(new TaskComparer());

    private class TaskComparer : IEqualityComparer<E_AllTaskType>
    {
        public bool Equals(E_AllTaskType x, E_AllTaskType y)
        {
            return GetHashCode(x).Equals(GetHashCode(y));
        }

        public int GetHashCode(E_AllTaskType obj)
        {
            return (int)obj;
        }
    }

    protected override void SetListener(bool addOrRemove)
    {
        BizzaEventSystem.Set(EventDefine.Time.SystemNewDay, OnNewDayReset, addOrRemove);
        //
        // //监听可能涉及到任务状态更新的事件
        //
        // EventModule.SetListener(EventDefine.Item.ItemUpdate, OnItemUpdate, addOrRemove);
        // EventModule.SetListener<E_ShopBoxType, int>(EventDefine.MenuShop.OpenChest, OnOpenShopBox, addOrRemove);
        // EventModule.SetListener(EventDefine.Sdk.OnAdShowSuccess, OnAdShowSuccess, addOrRemove);
        // EventModule.SetListener<ShopItemData>(EventDefine.MenuShop.BuyShopItem, OnShopBuy, addOrRemove);
        // EventModule.SetListener<BattleActor>(EventDefine.Battle.UnitDead, OnBattleActorDead, addOrRemove);
        // // EventModule.SetListener(E_GameEvent.AdsRequestUpdateNextShopRequest, OnRefreshDailyShop, addOrRemove);
        BizzaEventSystem.Set(EventDefine.Frame.LoadingStageComplete, OnLoadingFinish, addOrRemove);
        // EventModule.SetListener<int, bool>(EventDefine.Game.LevelEnd, OnLevelEnd, addOrRemove);
        BizzaEventSystem.Set(EventDefine.Frame.LevelEnd, OnChapterEnd, addOrRemove);
        BizzaEventSystem.Set(EventDefine.Frame.FinishAd, OnFinishAd, addOrRemove);
    }

    private void OnLoadingFinish()
    {
        if (TableUtils.Tables == null || TableUtils.Tables.TblDailyTaskConfig == null)
        {
            return;
        }
        RegistTaskInfoPool();
        CreateTasks();
    }

    public bool Enabled => true;

    #region Events

    /// <summary>
    /// 根据任务类型更新任务进度
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="num">进度变化值</param>
    public void UpdateTaskProgressByTaskType(E_AllTaskType taskType, int modifiedNum = 1,
        System.Func<BaseRuntimeTaskInfo, bool> condition = null)
    {
        // if (mainTaskInfo != null && mainTaskInfo.config != null && mainTaskInfo.config.CompleteConditions == taskType)
        // {
        //     mainTaskInfo.UpdateProgress(modifiedNum);
        // }

        if (allTaskDic.TryGetValue(taskType, out List<BaseRuntimeTaskInfo> taskInfoList))
        {
            int length = taskInfoList.Count - 1;
            for (int i = length; i > -1; i--)
            {
                var taskInfo = taskInfoList[i];
                bool result = condition == null ? true : condition.Invoke(taskInfo);
                if (result)
                    taskInfo.UpdateProgress(modifiedNum);
            }
        }
    }

    /// <summary>
    /// 更改任务进度记录
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="modifiedAmount"></param>
    private void ModifiedTaskRecordData(string taskId, int modifiedAmount)
    {
        if (SaveDataUtils.TaskData.taskRecordDataDic
            .TryGetValue(taskId, out TaskRecordData currProcess))
        {
            currProcess.CurrentProgress += modifiedAmount;
        }
        else
        {
            _Data.taskRecordDataDic.Add(taskId,
                new TaskRecordData { TaskId = taskId, CurrentProgress = modifiedAmount });
        }
    }

    /// <summary>
    /// 设置任务进度记录
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="amount"></param>
    private void SetTaskRecordData(string taskId, int amount)
    {
        if (_Data.taskRecordDataDic
            .TryGetValue(taskId, out TaskRecordData currProcess))
        {
            currProcess.CurrentProgress = amount;
        }
        else
        {
            _Data.taskRecordDataDic.Add(taskId,
                new TaskRecordData { TaskId = taskId, CurrentProgress = amount });
        }
    }

    private void OnFinishAd(bool success)
    {
        ModifiedTaskRecordData("DailyAdCount", 1);
        UpdateTaskProgressByTaskType(E_AllTaskType.WatchAd);
    }

    private void OnChapterEnd(int level, bool success)
    {
        ModifiedTaskRecordData("EnterBattleCount", 1);
        UpdateTaskProgressByTaskType(E_AllTaskType.FinishLevel);
    }

    /// <summary>
    /// 修改活跃进度
    /// </summary>
    private void OnItemUpdate()
    {
        // UpdateTaskProgressByTaskType(E_AllTaskType.ActivityAmount);
    }

    /// <summary>
    /// 观看广告
    /// </summary>
    private void OnAdShowSuccess()
    {
        UpdateTaskProgressByTaskType(E_AllTaskType.WatchAd);
    }

    public float timeSecond = 0;

    /// <summary>
    /// 在线时长
    /// </summary>
    /// <param name="deltaTime"></param>
    private void OnDailyPlaytimeUpdate(float deltaTime)
    {
        timeSecond += deltaTime;
        if (timeSecond > 60.0f) // 一分钟计数一次
        {
            ModifiedTaskRecordData("DailyPlaytime", 1);
            timeSecond -= 60.0f;
            UpdateTaskProgressByTaskType(E_AllTaskType.OnlineTime, 1);
        }
    }

    //
    // private void OnLevelEnd(int level, bool success)
    // {
    //     if (success)
    //     {
    //         UpdateTaskProgressByTaskType(E_AllTaskType.LevelFinish);
    //     }
    // }

    /// <summary>
    /// 关卡扫荡
    /// </summary>
    // private void OnBattleWipe(int chapter)
    // {
    //     //暂时走通用加法
    //     UpdateTaskProgressByTaskType(E_AllTaskType.WipeAmount, 1);
    // }

    // private void OnCollectionGetReward()
    // {
    //     UpdateTaskProgressByTaskType(E_AllTaskType.CollectionReward, 1);
    // }

    // private void OnGetOfflineReward()
    // {
    //     UpdateTaskProgressByTaskType(E_AllTaskType.OfflineReward, 1);
    // }

    // private void OnPlaneUnlock()
    // {
    //     UpdateTaskProgressByTaskType(E_AllTaskType.UnlockPlane);
    // }
    #endregion

    /// <summary>
    /// 新的一天，清空每日任务进度
    /// 早于 CreateTasks 执行
    /// </summary>
    public void OnNewDayReset()
    {
        //TODO:这样改7天的记录就没了，每日和多日需要分开统计
        RemoveTaskInfoById("DailyPlaytime", E_AllTaskType.OnlineTime);

        //重置活跃度
        // ItemUtils.ClearAccountItem(E_ItemType.Activity);
        int taskStartDay = _Data.taskStartDay;
        int currentDay = (int)TimeUtil.GetCurrentTime(ETimeUnit.DAYS);
        // if (taskStartDay < 0)
        // {
            // SaveDataModule.Data.taskSaveData.taskStartDay = currentDay;
            // taskStartDay = currentDay;
        // }

        int timePass = currentDay - taskStartDay;

        //清空每日
        foreach (var dailyConfig in TableUtils.Tables.TblDailyTaskConfig.DataList)
        {
            int refreshSecond = dailyConfig.RefreshDays;
            if (timePass >= refreshSecond && timePass % dailyConfig.RefreshDays == 0) //到时间了 需要刷新
            {
                RemoveTaskInfoById(dailyConfig.Id, dailyConfig.CompleteConditions);
                dailyTaskInfos.Remove(dailyConfig.Id);
            }
        }

        // //清除活跃任务进度
        // foreach (var activityConfig in TableUtils.Tables.TblActivityTaskConfig.DataList)
        // {
        //     int refreshSecond = activityConfig.RefreshDays;
        //     if (timePass > refreshSecond && timePass % activityConfig.RefreshDays == 0) //到时间了 需要刷新
        //     {
        //         //清空活跃值
        //         ItemUtils.ClearAccountItem(activityConfig.Item); //TODO 优化一下算法
        //         //清空任务进度
        //         RemoveTaskInfoById(activityConfig.Id, E_AllTaskType.ActivityAmount);
        //         dailyActivityTaskInfos.Remove(activityConfig.Id);
        //     }
        // }

        CreateTasks();
        BizzaEventSystem.Emit(EventDefine.Task.TaskRefresh);
    }

    public void OnUpdate(float deltaTime)
    {
        OnDailyPlaytimeUpdate(deltaTime);
    }

    /// <summary>
    /// 注册对象池
    /// </summary>
    private void RegistTaskInfoPool()
    {
        GlobalObjectPoolRegistry.Register(typeof(RuntimeDailyTaskInfo), new ObjectPoolBase<RuntimeDailyTaskInfo>());
        GlobalObjectPoolRegistry.Register(typeof(RuntimeActivityTaskInfo),
            new ObjectPoolBase<RuntimeActivityTaskInfo>());
    }

    private void CreateTasks()
    {
        LogLogger.LogVerbose("CreateTasks");
        //根据存档和表数据，创建运行时任务数据
        //创建每日任务
        foreach (var dailyConfig in TableUtils.Tables.TblDailyTaskConfig.DataList)
        {
            if (dailyTaskInfos.ContainsKey(dailyConfig.Id))
            {
                continue;
            }

            var runtimeDailyTaskInfo = CreateTaskInfo<RuntimeDailyTaskInfo>(dailyConfig.Id,
                dailyConfig.CompleteConditions, dailyConfig.ConditionValues, false, false);
            runtimeDailyTaskInfo.config = dailyConfig;
            runtimeDailyTaskInfo.TaskStart();

            dailyTaskInfos.Add(dailyConfig.Id, runtimeDailyTaskInfo);
        }

        // //创建活跃任务
        // foreach (var activityConfig in TableUtils.Tables.TblActivityTaskConfig.DataList)
        // {
        //     if (dailyActivityTaskInfos.ContainsKey(activityConfig.Id))
        //     {
        //         continue;
        //     }
        //
        //     var runtimeActivityTaskInfo = new RuntimeActivityTaskInfo();
        //     runtimeActivityTaskInfo.TaskId = activityConfig.Id;
        //     runtimeActivityTaskInfo.config = activityConfig;
        //     runtimeActivityTaskInfo.TaskType = E_AllTaskType.Activity;
        //     runtimeActivityTaskInfo.CurrentProgress = 0;
        //     runtimeActivityTaskInfo.TargetProgress = activityConfig.Number;
        //     runtimeActivityTaskInfo.updater = ItemAmountUpdater.GetInstance(activityConfig.Item);
        //     runtimeActivityTaskInfo.TaskStart();
        //     runtimeActivityTaskInfo.LoadRecord();
        //     if (allTaskDic.TryGetValue(E_AllTaskType.Activity, out var taskList))
        //     {
        //         taskList.Add(runtimeActivityTaskInfo);
        //     }
        //     else
        //     {
        //         allTaskDic.Add(E_AllTaskType.Activity, new List<BaseRuntimeTaskInfo> { runtimeActivityTaskInfo });
        //     }
        //
        //     dailyActivityTaskInfos.Add(activityConfig.Id, runtimeActivityTaskInfo);
        // }

        //更新红点
        Update_TaskRedpoint();
    }

    public void Update_TaskRedpoint()
    {
        foreach (var taskInfo in dailyTaskInfos.Values)
        {
            RedPointSystem.Instance.SetRed(taskInfo.RedPointKey, taskInfo.TaskState == E_TaskState.Complete);
            // RedPointMgrSystem.Instance.SetState(E_RedPoint.Task_Root, E_RedPoint.Task_Daily_List,
            // taskInfo.TaskId.GetHashCode(),
            // taskInfo.TaskState == E_TaskState.Complete ? ERedPointState.Show : ERedPointState.Hide);
        }

        foreach (var taskInfo in dailyActivityTaskInfos.Values)
        {
            // RedPointMgrSystem.Instance.SetState(E_RedPoint.Task_Root, E_RedPoint.Task_Activity_Reward_List,
            //     taskInfo.TaskId.GetHashCode(),
            //     taskInfo.TaskState == E_TaskState.Complete
            //         ? ERedPointState.Show
            //         : ERedPointState.Hide);
        }
    }




}
#endif
