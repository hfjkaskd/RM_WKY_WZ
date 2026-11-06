#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RealUiWidget : MonoBehaviour
{
    public BizzaButton taskBtn;
    public TMP_Text levelTxt;


    public ItemForCountry itemForCountry;


    void Awake()
    {
        taskBtn.onClick.AddListener(() =>
        {
            _ = UIModule.Instance.OpenPage(UIPageIds.UI_DailyTaskPage);
        });
    }

    void OnEnable()
    {
        RefreshLevelText();
        itemForCountry.OnRefresh();
    }

    public void RefreshLevelText()
    {
        levelTxt.text = $"{SaveDataUtils.GameData.playerSelectedLv}"; //LanguageUtils.GetFormatText("Menu_LevelBtn", SaveDataUtils.GameData.playerUnlockedLv);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
#endif