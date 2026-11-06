#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
using cfg;

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDailyActivityElement : MonoBehaviour
{
    public GameObject m_imgNormal, m_imgOpen, m_imgFinished, m_rewardPreview, m_rewardContent;
    public UIItem inventoryPref;
    public TMP_Text m_activityAmount;
    public Button btnGetReward;
    private MenuSys_Task.RuntimeActivityTaskInfo m_runtimeTaskInfo;

    private List<UIItem> rewardUIs = new();

    void Start()
    {
        m_rewardPreview.SetActive(false);
    }


    public void Init(MenuSys_Task.RuntimeActivityTaskInfo runtimeActivityTaskInfo)
    {
        if (m_runtimeTaskInfo != null)
            m_runtimeTaskInfo.onUpdateProgress -= OnTaskUpdateProgress;
        m_runtimeTaskInfo = runtimeActivityTaskInfo;
        runtimeActivityTaskInfo.onUpdateProgress += OnTaskUpdateProgress;
        m_activityAmount.text = runtimeActivityTaskInfo.config.Number.ToString();
        btnGetReward.onClick.RemoveAllListeners();
        btnGetReward.onClick.AddListener(OnActivityClick);
        for (int i = 0; i < rewardUIs.Count; i++)
        {
            PoolUtil.ReleaseGameObject(rewardUIs[i].gameObject);
        }
        rewardUIs.Clear();
        for (int i = 0; i < runtimeActivityTaskInfo.config.RewardsList.Count; i++)
        {
            var item = runtimeActivityTaskInfo.config.RewardsList[i];
            var inventory = PoolUtil.GetGameObject(inventoryPref);
            inventory.transform.SetParent(m_rewardContent.transform);
            inventory.gameObject.SetActive(true);
            // inventory.ShowItem(item.Id, item.IntValue);
            inventory.transform.localScale = Vector3.one * 0.55f;
            // rewardUIs.Add(inventory);
        }

        RefreshView();
    }

    private void Update()
    {
        if (!m_rewardPreview.activeSelf) return;
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
#else
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended)
#endif
        {
            if (EventSystem.current.currentSelectedGameObject != m_rewardPreview)
            {
                m_rewardPreview.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        m_runtimeTaskInfo.onUpdateProgress -= OnTaskUpdateProgress;
    }

    public void OnActivityClick()
    {
        if (m_runtimeTaskInfo.TaskState == E_TaskState.Uncomplete)
        {
            m_rewardPreview.SetActive(true);
        }
        else if (m_runtimeTaskInfo.TaskState == E_TaskState.Complete)
        {
            m_runtimeTaskInfo.GetReward(transform.position);
            BizzaEventSystem.Emit(EventDefine.Task.TaskRefresh);
            RefreshView();
        }
    }

    public void RefreshView()
    {
        m_imgNormal.SetActive(m_runtimeTaskInfo.TaskState != E_TaskState.Complete &&
                              m_runtimeTaskInfo.TaskState != E_TaskState.Rewarded);
        m_imgOpen.SetActive(m_runtimeTaskInfo.TaskState == E_TaskState.Rewarded);
        m_imgFinished.SetActive(m_runtimeTaskInfo.TaskState == E_TaskState.Complete &&
                                m_runtimeTaskInfo.TaskState != E_TaskState.Rewarded);
    }

    private void OnTaskUpdateProgress()
    {
        RefreshView();
    }
}
#endif
