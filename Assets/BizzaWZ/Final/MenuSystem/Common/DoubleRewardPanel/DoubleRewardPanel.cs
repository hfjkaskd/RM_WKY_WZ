#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DoubleGetRewardPanel : UIPageBase<ItemEntry, DoubleGetRewardPanel.E_UseScene>
{
     [Obfuz.ObfuzIgnore]
     
    public enum E_UseScene
    {
        MatchReward,
        WinPanel,
        DailyTask,
        Bubble,
        Ad,
        CloseGetReward,
        Test,
    }

    public static float LastAdTime;

    public PageId LegacyPageType => UIPageIds.DoubleGetRewardPanel;

    public BizzaButton adBtn;
    public BizzaButton noThanksBtn;

    public ItemEntry item;
    public TMP_Text itemTxt;
    public TMP_Text doubleItemTxt;
    public TMP_Text levelTxt;
    public TMP_Text getOnlyTxt;
    public Transform startPos;
    public Transform doubleStartPos;

    public GameObject levelTips;
    private DoubleGetRewardPanel.E_UseScene _useScene;
    public GameObject gameProgress;

    void Awake()
    {
        adBtn.onClick.AddListener(() =>
        {
            // AdModule.OpenRewardAds(GetAdPos(_useScene), OnAdFinish);
        });

        noThanksBtn.onClick.AddListener(() =>
        {
            ItemUtils.AddItem(item, new AddItemParam()
            {
                playAnim = true,
                isAd = false,
                startPos = startPos.position,
                bUiPos = false,
                source = GetItemSource(_useScene),
                showCurrencyBar = _useScene == E_UseScene.DailyTask,
            });
            if (_useScene == E_UseScene.WinPanel)
            {
                TransparentBlock.AddBlock(this);
                GameUtils.DelayDo(() =>
                {
                    TransparentBlock.RemoveBlock(this);
                    CloseSelf();
                    TransitionBlock.ToMainMenu(1f, "胜利");
                }, 1f);
            }
            else
            {
                CloseSelf(0.5f);
                // AdUtils.CheckShowInterAdPoss(Vector3.zero);
            }
        });
    }

    protected override void OnClose()
    {
        LastAdTime = Time.realtimeSinceStartup;
        // AdUtils.CheckShowInterAdTimes(Vector3.zero);
    }

    private void OnAdFinish(bool success)
    {
        if (success)
        {
            // ItemUtils.AddItem(item.Type, item.Count * 2, true, false, doubleStartPos.position, false);
            ItemUtils.AddItem(item * 2, new AddItemParam()
            {
                playAnim = true,
                isAd = true,
                animation = RewardCollectAnimation.BurstCollect,
                startPos = doubleStartPos.position,
                bUiPos = false,
                source = GetItemSource(_useScene),
                showCurrencyBar = _useScene == E_UseScene.DailyTask,
            });
            if (_useScene == E_UseScene.WinPanel)
            {
                TransparentBlock.AddBlock(this);
                GameUtils.DelayDo(() =>
                {
                    TransparentBlock.RemoveBlock(this);
                    CloseSelf();
                    TransitionBlock.ToMainMenu(1f, "胜利");
                }, 1f);
            }
            else
            {
                CloseSelf(0.5f);
            }
        }
    }

    protected override void OnOpen(ItemEntry a, DoubleGetRewardPanel.E_UseScene useScene)
    {
        item = a;
        _useScene = useScene;
        itemTxt.text = ItemUtils.GetItemText(a);
        doubleItemTxt.text = ItemUtils.GetItemText(a * 2);
        levelTxt.text = LanguageUtils.GetFormatText("Menu_LevelBtn", SaveDataUtils.GameData.playerSelectedLv);
        levelTips.SetObjActive(useScene == E_UseScene.WinPanel);

        gameProgress.SetObjActive(World.Current.GetGameMode<GameMode_GamePlay>() != null);
        getOnlyTxt.text = LanguageUtils.GetFormatText("Btn_GetOnly", ItemUtils.GetItemText(a));
    }

    public static E_AddItemSource GetItemSource(DoubleGetRewardPanel.E_UseScene inUseScene)
    {
        var ret = E_AddItemSource.None;
        if (inUseScene == E_UseScene.WinPanel)
        {
            ret = E_AddItemSource.Settlement;
        }
        else if (inUseScene == E_UseScene.MatchReward)
        {
            ret = E_AddItemSource.MatchBoost;
        }
        else if (inUseScene == E_UseScene.DailyTask)
        {
            ret = E_AddItemSource.TaskBonus;
        }

        return ret;
    }
}

public static partial class UIPageIds
{
    public static readonly PageId DoubleGetRewardPanel = nameof(DoubleGetRewardPanel);
}
#endif
