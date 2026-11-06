#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WathAdProgress : MonoBehaviour
{
    public TMP_Text startText;
    public TMP_Text endText;
    public TMP_Text hintText;
    public TMP_Text progressText;

    public Image progressBar;

    public int WatchAdCount => SaveDataUtils.GameData.todayAdTimes;

    private void OnEnable()
    {
        UpdateProgress();
    }

    public void UpdateProgress()
    {

        string info = AccountModule.Instance.OceanShineAppOtherConfigResponse;
        if (string.IsNullOrEmpty(info))
        {
            gameObject.SetActive(false);
            AccountModule.Instance.Request_AppOtherConfigRequest((success, response) =>
            {
                if (!success || this == null || string.IsNullOrEmpty(response)) return;
                gameObject.SetActive(true);
            }, false, true);
            return;
        }
        gameObject.SetActive(true);

        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(info);
#if DEBUG_MODE
        Debug.LogError("恭喜获得界面配置信息: " + info);
#endif
        int threeLook = Convert.ToInt32(dict[AccountModuleCfg.three_Count]);
        int threeRatio = Convert.ToInt32(dict[AccountModuleCfg.three_Ratio]);
        int twoLook = Convert.ToInt32(dict[AccountModuleCfg.two_Count]);
        int twoRatio = Convert.ToInt32(dict[AccountModuleCfg.two_Ratio]);
        int oneLook = Convert.ToInt32(dict[AccountModuleCfg.one_Count]);
        int oneRatio = Convert.ToInt32(dict[AccountModuleCfg.one_Ratio]);

        if (WatchAdCount >= threeLook)
        {
            hintText.text = LanguageUtils.GetText("GetReward_WatchAd_Hint_Full");
            RefreshInfo(threeRatio, threeRatio, WatchAdCount, threeLook, false);
        }
        else if (WatchAdCount >= twoLook)
        {
            RefreshInfo(twoRatio, threeRatio, WatchAdCount, threeLook);
        }
        else if (WatchAdCount >= oneLook)
        {
            RefreshInfo(oneRatio, twoRatio, WatchAdCount, twoLook);
        }
        else
        {
            RefreshInfo(0, oneRatio, WatchAdCount, oneLook);
        }
    }

    private void RefreshInfo(float startRatio, float endRatio, int watchAdCount, int totalWatchAdCount, bool isNotFull = true)
    {
        startText.text = $"+{startRatio}%";
        endText.text = $"+{endRatio}%";
        progressBar.fillAmount = (float)watchAdCount / totalWatchAdCount;
        progressText.text = $"{watchAdCount}/{totalWatchAdCount}";

        if (isNotFull)
        {
            hintText.text = LanguageUtils.GetFormatText("GetReward_WatchAd_Hint_NotFull", totalWatchAdCount);
        }
    }
}
#endif
