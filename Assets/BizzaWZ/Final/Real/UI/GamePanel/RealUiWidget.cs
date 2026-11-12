#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RealUiWidget : MonoBehaviour
{
    public BizzaButton taskBtn;
    public TMP_Text levelTxt;
    [SerializeField] private bool levelTextProvidedByCurrencyBar;


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
        if (levelTextProvidedByCurrencyBar) return;
        levelTxt.text = $"{SaveDataUtils.GameData.playerSelectedLv}"; //LanguageUtils.GetFormatText("Menu_LevelBtn", SaveDataUtils.GameData.playerUnlockedLv);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
#endif
