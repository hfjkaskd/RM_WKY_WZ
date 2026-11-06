#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
using Bizza;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static AccountModule;
using Object = UnityEngine.Object;

public partial class UIPageIds
{
    public static readonly PageId WithdrawDanPanel = "WithdrawDanPanel";
}

public class WithdrawDanPanel : UIPageBase
{
    [Button]
    public void ResetData()
    {
        SaveDataUtils.WithDrawDanPanelData.InitData();
    }
    public TMP_Text balanceTxt;

    public Image progressImg;
    public TMP_Text progressTxt;

    public TMP_Text hintTxt;

    public WithdrawDanItem item;
    public Transform root;
    private List<WithdrawDanItem> items = new List<WithdrawDanItem>();

    public WithDrawMissionSO USWithdrawMissionSO;
    public WithDrawMissionSO IDWithdrawMissionSO;
    public WithDrawMissionSO withdrawMissionSO
    {
        get
        {
            if (AccountModule.CountryType == E_CountryType.US || AccountModule.CountryType == E_CountryType.BR)
            {
                return USWithdrawMissionSO;
            }
            else if (AccountModule.CountryType == E_CountryType.ID)
            {
                return IDWithdrawMissionSO;
            }
            else
            {
                Debug.LogError($"未实现提现功能的国家类型：{AccountModule.CountryType}");
                return null;
            }
        }
    }


    public List<Sprite> danSprites = new();
    public List<string> danDescs = new();
    [TitleGroup("印尼")]
    public List<WithDrawMissionSO> danLevelsID = new();
    [TitleGroup("美国")]
    public List<WithDrawMissionSO> danLevelsUS = new();
    private int CurStage
    {
        get => SaveDataUtils.WithDrawDanPanelData.curStageIndex;
        set => SaveDataUtils.WithDrawDanPanelData.curStageIndex = value;
    }

    
   public BizzaButton closeBtn;
    public BizzaButton withdrawBtn;
    public BizzaButton historiyBtn;
    public BizzaButton faqBtn;

    protected override void OnAwake()
    {
        base.OnAwake();
        closeBtn.onClick.AddListener(OnClickCloseBtn);
        withdrawBtn.onClick.AddListener(OnClickWithdrawBtn);
        historiyBtn.onClick.AddListener(OnClickWithdrawHistory);
        faqBtn.onClick.AddListener(OnClickFQABtn);
    }


    /*
     * 玩家观看广告的数量，
     * 玩家提现余额
     * 阶段任务点
     */

    protected override void OnClose()
    {
        BizzaEventSystem.Off(EventDefine.Item.ItemChangedWithData, OnRewardChanged);
        if (progressImg) progressImg.DOKill();
        if (progressTxt) DOTween.Kill(progressTxt);
        if (balanceTxt) DOTween.Kill(balanceTxt);
    }
    protected override void OnOpen()
    {
        items.SetCmptListCount(item, root, 7);
        float curLevel = SaveDataUtils.GameData.playerSelectedLv - 1;
        for (int i = 0; i < 7; i++)
        {
            bool isClaimed = SaveDataUtils.WithDrawDanPanelData.IsClaimed(i);
            List<WithDrawMissionSO> danLevels = new();
            if (AccountModule.CountryType == E_CountryType.ID)
            {
                danLevels = danLevelsID;
            }
            else if (AccountModule.CountryType == E_CountryType.US || AccountModule.CountryType == E_CountryType.BR)
            {
                danLevels = danLevelsUS;
            }
            items[i].Init(i,
                danLevels[i].withdrawMoney,
                danSprites[i],
                LanguageUtils.GetText(danDescs[i]),
                LanguageUtils.GetFormatText("WithdrawDanPanel_TaskHint", danLevels[i].missions[0].conditionData.targetValue),
                curLevel.ToString(),
                SaveDataUtils.GameData.playerSelectedLv - 1,
                (int)danLevels[i].missions[0].conditionData.targetValue,
                isClaimed,
                curLevel >= danLevels[i].missions[0].conditionData.targetValue
                , this
                );
        }
        // ClearChildsForRoot();
        Refresh();
        UpdateProgress(false);
        UpdateMoneyText(false);
        BizzaEventSystem.On(EventDefine.Item.ItemChangedWithData, OnRewardChanged);
    }

    private void OnRewardChanged()
    {
        if (!isActiveAndEnabled) return;
        UpdateProgress();
        UpdateMoneyText();
        Refresh();
    }

    private WithdrawMissionData GetCurMission()
    {
        return withdrawMissionSO.GetMissionByStateSafe(CurStage);
    }

    public void Refresh()
    {        // var list = AccountModule.Instance.
        var curMission = GetCurMission();
        if (curMission == null)
        {
            progressImg.fillAmount = 1;
           LogLogger.LogInfo(BaseConst.LOG_Game, "已经完成所有任务");
            return;
        }
        hintTxt.text = curMission.GetWithdrawDesc(curMission.GetValueOfCondition());
    }

    private float _currentMoney = 0;
    public void UpdateMoneyText(bool isAnim = true)
    {
        float targetMoney = ItemUtils.FormatCountFloat(new ItemEntry
        {
            Type = E_ItemType.WithDrawDanDollar,
            Count = ItemUtils.GetItemCount(E_ItemType.WithDrawDanDollar),
        });

        // 不需要动画（初始化 / 强制刷新）
        if (!isAnim)
        {
            _currentMoney = targetMoney;
            balanceTxt.text = LanguageUtils.GetText("CurrencyToken") + targetMoney;
            return;
        }

        float startValue = _currentMoney;
        float endValue = targetMoney;
        DOTween.Kill(balanceTxt);
        DOTween.To(() => startValue, x =>
        {
            startValue = x;
            _currentMoney = x;
            balanceTxt.text = LanguageUtils.GetText("CurrencyToken") + x;
        }, endValue, 0.8f)
        .SetTarget(balanceTxt)
        .SetEase(Ease.OutSine);
    }


    public void UpdateProgress(bool isAnim = true)
    {
        var curMission = GetCurMission();
        if (curMission == null)
        {
            progressImg.fillAmount = 1f;
            progressTxt.text = "100%";
           LogLogger.LogInfo(BaseConst.LOG_Game, "已经完成所有任务");
            return;
        }

        float targetValue = 0f;
        if (curMission.conditionData.condition == E_WithdrawCondition.WithDrawDanDollar)
        {
            var itemEntry = new ItemEntry
            {
                Type = E_ItemType.WithDrawDanDollar,
                Count = curMission.conditionData.targetValue,
            };
            targetValue = ItemUtils.FormatCountFloat(itemEntry);
        }
        else
        {
            targetValue = curMission.conditionData.targetValue;
        }

        float curValue = curMission.GetValueOfCondition();

        // 防止除0 + clamp，避免超过100%或负数
        float progress = (targetValue <= 0f) ? 0f : Mathf.Clamp01(curValue / targetValue);

        if (isAnim)
        {
            // 先杀掉旧动画（避免叠加）
            progressImg.DOKill();
            DOTween.Kill(progressTxt);

            DOTween.To(
                () => progressImg ? progressImg.fillAmount : 0f,
                x =>
                {
                    if (!progressImg) return;
                    progressImg.fillAmount = x;
                },
                progress,
                0.8f
            )
            .SetEase(Ease.OutSine)
            .SetTarget(progressImg);

            float start = progressImg.fillAmount; // 或者用旧值缓存更准
            DOTween.To(() => start, x =>
            {
                start = x;
                progressTxt.text = $"{Mathf.FloorToInt(start * 100f)}%";
            }, progress, 0.8f)
            .SetEase(Ease.OutSine)
            .SetTarget(progressTxt);
        }
        else
        {
            progressImg.fillAmount = progress;
            progressTxt.text = $"{Mathf.FloorToInt(progress * 100f)}%";
        }
    }


    public void OnClickCloseBtn()
    {
        UIModule.Instance.ClosePage(this);
    }

    public void OnClickWithdrawBtn()
    {
        var curMission = GetCurMission();
        if (curMission == null)
        {
            LogLogger.LogInfo("已经完成所有阶段");
            return;
        }

        if (!curMission.IsCanWithdraw())
        {
            UIUtils.ShowLanguageTips("WithdrawDanPanel_UnableClaim");
            return;
        }
        if (CurStage == 0)
        {
            ItemUtils.TryReduceItemFloat(E_ItemType.WithDrawDanDollar, withdrawMissionSO.withdrawMoney);
        }
        CurStage++;
        UIUtils.ShowLanguageTips("FakeWithdrawPanel_NextStage");
        LogLogger.LogInfo($"WithdrawDan 进入下一阶段：{CurStage}");

        Refresh();
        UpdateProgress(true);
    }


    private bool initForEditor = false;
    private void ClearChildsForRoot()
    {
        if (initForEditor)
        {
            return;
        }
        initForEditor = true;
        int childCount = root.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
    public void OnClickWithdrawHistory()
    {
        UIModule.Instance.OpenPage(UIPageIds.WithdrawHistory).Forget();
    }
    public void OnClickFQABtn()
    {
        UIModule.Instance.OpenPage(UIPageIds.QFA).Forget();
    }

}
#endif
