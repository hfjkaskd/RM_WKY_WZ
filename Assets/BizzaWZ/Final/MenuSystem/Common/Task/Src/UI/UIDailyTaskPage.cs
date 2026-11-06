#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
using cfg;

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIDailyTaskPage : UIPageBase
{
    public PageId LegacyPageType => UIPageIds.UI_DailyTaskPage;

    public GameObject DailyRootObj;
    public GameObject PlayTimeRootObj;

    public RectTransform dailyTaskRoot;
    public RectTransform dailyPlaytimeRoot;

    public RectTransform activityTaskRoot;
    public BizzaButton tabDaily;
    public BizzaButton tabWeekly;
    public BizzaButton tabPlaytime;

    public UIDailyActivityElement activityElement;
    public UIDailyTaskElement dailyTaskElement;
    public Image activityBar;
    public TMP_Text titleText;
    public TMP_Text txtActiveCount;
    public TMP_Text RefreshTimeText;
    public string[] titleTextArray;
    private int maxActivity;

    private List<UIDailyActivityElement> activityEles = new();
    private List<UIDailyTaskElement> dailyEles = new();
    private List<MenuSys_Task.RuntimeDailyTaskInfo> dailyTaskInfos = new();

    // protected override void SetListener(bool addOrRemove)
    // {
    //     // BizzaEventSystem.Set(EventDefine.Item.ItemUpdate, OnActivityRefresh, addOrRemove);
    //     // BizzaEventSystem.Set(EventDefine.Task.TaskRefresh, OnTaskRefresh, addOrRemove);
    //     // BizzaEventSystem.Set<string>(EventDefine.Task.DailyTaskRewarded, OnDailyTaskStateChange, addOrRemove);
    //     // BizzaEventSystem.Set(EventDefine.MenuShop.UpdateRewardNextTime, OnUpdateRewardNextTimeCallback, addOrRemove);
    // }

    private bool IsPlayTimeTask;
    private int refreshDays = 1;
    private E_ItemType activityResType;
    private int timePass = -1;
    public Button closeBtn;

    void Awake()
    {
        tabDaily.onClick.AddListener(() => { OnTabClick(0); });
        tabWeekly.onClick.AddListener(() => { OnTabClick(1); });
        tabPlaytime.onClick.AddListener(() => { OnTabClick(2); });
        closeBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });
    }

    protected override void OnOpen()
    {
        int taskStartDay = SaveDataUtils.TaskData.taskStartDay;
        int currentDay = (int)TimeUtil.GetCurrentTime(ETimeUnit.DAYS);
        timePass = currentDay - taskStartDay;
        OnTabClick(0);
        // UiManager.Instance.OpenMask();

        BizzaEventSystem.Set(EventDefine.Task.TaskRefresh, OnTaskRefresh, true);
    }

    protected override void OnClose()
    {
        // UiManager.Instance.CloseMask();

        BizzaEventSystem.Set(EventDefine.Task.TaskRefresh, OnTaskRefresh, false);
    }

    private void RefreshDailyTask()
    {
        if (IsPlayTimeTask)
        {
            DailyRootObj.SetActive(false);
            PlayTimeRootObj.SetActive(true);

            for (int i = 0; i < dailyEles.Count; i++)
            {
                PoolUtil.ReleaseGameObject(dailyEles[i].gameObject);
            }
            dailyEles.Clear();
            dailyTaskInfos.Clear();
            var dailyTaskCfgs = TableUtils.Tables.TblDailyTaskConfig.DataList;
            for (int i = 0; i < dailyTaskCfgs.Count; i++)
            {
                // if (dailyTaskCfgs[i].CompleteConditions != E_AllTaskType.OnlineTime) continue;

                var dailyTaskInfo = MenuSys_Task.Instance.dailyTaskInfos[dailyTaskCfgs[i].Id];
                dailyTaskInfos.Add(dailyTaskInfo);

                var ele = PoolUtil.GetComponent(dailyTaskElement);
                ele.gameObject.SetActive(true);
                ele.transform.SetParent(dailyPlaytimeRoot);
                ele.transform.localScale = Vector3.one;
                dailyEles.Add(ele);
            }
        }
        else
        {
            DailyRootObj.SetActive(true);
            PlayTimeRootObj.SetActive(false);
            //1. 展示活跃值任务
            maxActivity = 0;
            var activityCfgs = TableUtils.Tables.TblActivityTaskConfig.DataList;
            for (int i = 0; i < activityCfgs.Count; i++)
            {
                if (activityCfgs[i].RefreshDays != refreshDays) continue;
                if (maxActivity < activityCfgs[i].Number)
                {
                    maxActivity = activityCfgs[i].Number;
                }
            }

            for (int i = 0; i < activityEles.Count; i++)
            {
                PoolUtil.ReleaseGameObject(activityEles[i].gameObject);
            }

            activityEles.Clear();

            for (int i = 0; i < activityCfgs.Count; i++)
            {
                var activityCfg = activityCfgs[i];
                if (activityCfg.RefreshDays != refreshDays) continue;

                var activityTaskInfo = MenuSys_Task.Instance.dailyActivityTaskInfos[activityCfg.Id];
                var ele = PoolUtil.GetComponent(activityElement);
                ele.gameObject.SetActive(true);
                ele.transform.SetParent(activityTaskRoot);
                activityEles.Add(ele);

                ele.Init(activityTaskInfo);
                //计算位置
                //全长的比例
                float n = Mathf.Clamp01(activityTaskInfo.config.Number * 1.0f / maxActivity);
                float width = activityTaskRoot.rect.width;
                ele.transform.localPosition = new Vector3(-width * 0.5f + n * width, 0, 0);
                ele.transform.localScale = Vector3.one;
            }

            //2. 展示每日任务
            for (int i = 0; i < dailyEles.Count; i++)
            {
                PoolUtil.ReleaseGameObject(dailyEles[i].gameObject);
            }
            dailyEles.Clear();
            dailyTaskInfos.Clear();
            var dailyTaskCfgs = TableUtils.Tables.TblDailyTaskConfig.DataList;
            for (int i = 0; i < dailyTaskCfgs.Count; i++)
            {
                if (dailyTaskCfgs[i].RefreshDays != refreshDays) continue;
                //Runtime
                var dailyTaskInfo = MenuSys_Task.Instance.dailyTaskInfos[dailyTaskCfgs[i].Id];
                dailyTaskInfos.Add(dailyTaskInfo);
                //TaskUI
                var ele = PoolUtil.GetComponent(dailyTaskElement);
                ele.gameObject.SetActive(true);
                ele.transform.SetParent(dailyTaskRoot);
                ele.transform.localScale = Vector3.one;
                dailyEles.Add(ele);
            }

            //2.1 排序
            OnActivityRefresh();
        }

        SortDailyTask();
    }

    private void OnUpdateRewardNextTimeCallback()
    {
        // var sceonds = TimeUtil.GetSecondsToTargetTime(10);
        // var lastDay = refreshDays - timePass % refreshDays - 1;
        // RefreshTimeText.text = LanguageUtils.GetFormatText("1125", TimeUtil.SecondToStringV2(sceonds + lastDay * 3600 * 24));
    }

    private void SortDailyTask()
    {
        dailyTaskInfos.Sort((a, b) =>
        {
            if (a.TaskState == b.TaskState)
            {
                return a.TaskType - b.TaskType;
            }

            return a.TaskState - b.TaskState;
        });
        for (int i = 0; i < dailyEles.Count; i++)
        {
            if (i < dailyTaskInfos.Count)
            {
                dailyEles[i].Init(dailyTaskInfos[i]);
                dailyEles[i].gameObject.SetActive(true);
            }
            else
            {
                dailyEles[i].gameObject.SetActive(false);
            }
        }
    }

    public bool enableTab;
    private void OnTabClick(int tabIdx)
    {

    }

    private void OnTaskRefresh()
    {
        for (int i = 0; i < activityEles.Count; i++)
        {
            activityEles[i].RefreshView();
        }

        SortDailyTask();
        OnActivityRefresh();
    }

    private void OnActivityRefresh()
    {
        int n = ItemUtils.GetItemCountInt(activityResType);
        activityBar.fillAmount = Mathf.Clamp01(n * 1.0f / maxActivity);
        txtActiveCount.text = n.ToString();
    }

    private void OnDailyTaskStateChange(string taskId)
    {
        SortDailyTask();
    }
}

public static partial class UIPageIds
{
    public static readonly PageId UI_DailyTaskPage = "UI_DailyTaskPage";
}
#endif
