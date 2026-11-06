#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotEnter : MonoBehaviour
{
    private const int slotLimit = SlotProgressUtil.RequiredPassedLevels;
#if BIZZA_REAL_WITHDRAW
    private int levelIndex => SlotProgressUtil.Current;
    
    private string progressValue => $"{levelIndex}/{slotLimit}";
#endif

    //public TMP_Text titleTxt;

    public Image progressImag;
    public TMP_Text progressTxt;

    public GameObject slotCanHintObj;

    [SerializeField] private BizzaButton bizza;
#if BIZZA_REAL_WITHDRAW
    private void OnEnable()
    {
        bizza.onClick.AddListener(OnClick);
        BizzaEventSystem.Set(EventDefine.CustomGameEvent.SlotProgressChanged, OnWinRefresh, true);
        OnRefresh();
    }

    private void OnDisable()
    {
        bizza.onClick.RemoveListener(OnClick);
        BizzaEventSystem.Set(EventDefine.CustomGameEvent.SlotProgressChanged, OnWinRefresh, false);
    }
#endif
    private void OnWinRefresh()
    {
        OnRefresh();
    }

    public void OnRefresh()
    {
#if BIZZA_REAL_WITHDRAW
        progressTxt.text = progressValue;
        progressImag.fillAmount = (float)levelIndex / slotLimit;
        slotCanHintObj.SetActive(SlotProgressUtil.CanFreeSpin);

        LogLogger.LogInfo("SlotEnter OnRefresh " + "SaveDataUtils.GameData.playerUnlockedLv_" + levelIndex
            + "slotLimit_" + slotLimit + "progressValue_" + levelIndex / slotLimit);
#endif
    }

    public void OnClick()
    {
#if BIZZA_REAL_WITHDRAW
        UIModule.Instance.OpenPage(UIPageIds.SlotPanel);
#endif
    }
}
#endif
