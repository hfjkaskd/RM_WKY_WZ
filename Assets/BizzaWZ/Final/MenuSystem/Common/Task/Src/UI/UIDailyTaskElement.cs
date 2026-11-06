#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDailyTaskElement : MonoBehaviour
{
    public Image progressBar;
    public TMP_Text descTxt;
    public TMP_Text progressTxt;
    public Button btnReward;
    public Button btnAds;
    public Button btnGoto;
    public GameObject objRewarded;
    public GameObject objRewardedMask;
    public UIItem inventoryPref;
    public Transform rewardRoot;
    public UIRedPoint redPointUI;
    private MenuSys_Task.RuntimeDailyTaskInfo m_dailyRuntimeInfo;
    private List<UIItem> ins = new();

    public GameObject[] stateObjs;

    [Button]
    public void Test()
    {
        var item = ItemUtils.ToCorrect(m_dailyRuntimeInfo.config.RewardsList[0]);
        // ItemUtils.ShowGetItemPanel(item, E_AdPos.DailyTask, (success) =>
        // {
        // if (success)
        {
            m_dailyRuntimeInfo.GetReward(btnReward.transform.position);
            RefreshTaskView();
            MenuSys_Task.Instance.Update_TaskRedpoint();
        }
        // });
    }

    private void OnDestroy()
    {
        if (m_dailyRuntimeInfo != null)
            m_dailyRuntimeInfo.onUpdateProgress -= OnTaskUpdateProgress;
    }

    public void Init(MenuSys_Task.RuntimeDailyTaskInfo runtimeDailyTaskInfo)
    {
        if (m_dailyRuntimeInfo != null)
            m_dailyRuntimeInfo.onUpdateProgress -= OnTaskUpdateProgress;

        m_dailyRuntimeInfo = runtimeDailyTaskInfo;
        m_dailyRuntimeInfo.onUpdateProgress += OnTaskUpdateProgress;
        descTxt.text = LanguageUtils.GetText(m_dailyRuntimeInfo.config.Description);
        btnReward.onClick.RemoveAllListeners();
        btnReward.onClick.AddListener(() =>
        {
            if (m_dailyRuntimeInfo.TaskState == E_TaskState.Complete)
            {
                if (m_dailyRuntimeInfo.CanGetReward() && m_dailyRuntimeInfo.config.RewardsList.Count > 0)
                {

                    SaveDataUtils.GameData.btnDailyTimeClick++;
#if BIZZA_REAL_WITHDRAW
                    Real_GetRewardPanelUtil.OpenGetRewardPanel(DoubleGetRewardPanel.E_UseScene.DailyTask, callback);
                    return;
#else
                    Debug.LogError("todo 假网赚显示获得奖励界面");
                    var item = ItemUtils.ToCorrect(m_dailyRuntimeInfo.config.RewardsList[0]);

                    var money = new ItemEntry();
                    money.Type = E_ItemType.Dollar;
                    money = ItemUtils.ToCorrect(money);

                    var coin = new ItemEntry();
                    coin.Type = E_ItemType.Gold;
                    coin.Count = TableUtils.Global.DefaultDollarNum;
                    coin = ItemUtils.ToCorrect(coin);
#endif
                    void callback(bool isSuccess)
                    {
                        if (!isSuccess) return;
                        m_dailyRuntimeInfo.GetReward(btnReward.transform.position);
                        RefreshTaskView();
                        MenuSys_Task.Instance.Update_TaskRedpoint();
                    }
                }
            }
        });
        btnAds.onClick.RemoveAllListeners();
        btnAds.onClick.AddListener(() =>
        {
            // SdkUtils.PlayVideoAd((success) =>
            // {
            //     if (success)
            //     {
            //         m_dailyRuntimeInfo.CurrentProgress = m_dailyRuntimeInfo.TargetProgress;
            //         m_dailyRuntimeInfo.GetReward();
            //         RefreshTaskView();
            //     }
            // }, "ads_task_" + m_dailyRuntimeInfo.TaskId);
        });
        btnGoto.onClick.RemoveAllListeners();
        btnGoto.onClick.AddListener(() =>
        {
            //TODO
            UIModule.Instance.ClosePage(UIPageIds.UI_DailyTaskPage);
            // EventModule.BroadCast(EventDefine.UI.GotoMainTapPage, m_dailyRuntimeInfo.config.Goto);
        });

        for (int i = 0; i < ins.Count; i++)
        {
            Destroy(ins[i].gameObject);
        }
        ins.Clear();
        for (int i = 0; i < m_dailyRuntimeInfo.config.RewardsList.Count; i++)
        {
            var reward = m_dailyRuntimeInfo.config.RewardsList[i];
            // var inventory = Instantiate(inventoryPref);
            // inventory.ShowItem(reward);
            // inventory.transform.SetParent(rewardRoot);
            // inventory.transform.localScale = Vector3.one;
            // ins.Add(inventory);
        }

        redPointUI.Key = m_dailyRuntimeInfo.RedPointKey;

        RefreshTaskView();
    }


    public void RefreshTaskView()
    {
        //进度
        progressTxt.text = $"{m_dailyRuntimeInfo.CurrentProgress}/{m_dailyRuntimeInfo.TargetProgress}";
        progressBar.fillAmount = Mathf.Clamp01(m_dailyRuntimeInfo.CurrentProgress * 1.0f / m_dailyRuntimeInfo.TargetProgress);
        //按钮
        objRewarded.gameObject.SetActive(m_dailyRuntimeInfo.TaskState == E_TaskState.Rewarded);
        objRewardedMask.gameObject.SetActive(m_dailyRuntimeInfo.TaskState == E_TaskState.Rewarded);
        if (m_dailyRuntimeInfo.config.AdsFinish)
        {
            btnGoto.gameObject.SetActive(false);
            btnAds.gameObject.SetActive(m_dailyRuntimeInfo.TaskState == E_TaskState.Uncomplete);
            btnReward.gameObject.SetActive(m_dailyRuntimeInfo.TaskState == E_TaskState.Complete);
        }
        // else if (m_dailyRuntimeInfo.config.CompleteConditions == E_AllTaskType.DailyPlayTime)
        // {
        //     btnGoto.gameObject.SetActive(false);
        //     btnAds.gameObject.SetActive(false);
        //     btnReward.gameObject.SetActive(m_dailyRuntimeInfo.TaskState == E_TaskState.Complete);
        // }
        else
        {
            btnGoto.gameObject.SetActive(m_dailyRuntimeInfo.TaskState == E_TaskState.Uncomplete);
            btnAds.gameObject.SetActive(false);
            btnReward.gameObject.SetActive(m_dailyRuntimeInfo.TaskState == E_TaskState.Complete);
        }

        foreach (var v in stateObjs)
        {
            v.gameObject.SetObjActive(false);
        }
        stateObjs[(int)m_dailyRuntimeInfo.TaskState].SetObjActive(true);
    }

    private void OnTaskUpdateProgress()
    {
        RefreshTaskView();
    }
}
#endif
