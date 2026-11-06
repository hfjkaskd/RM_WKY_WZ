#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Obfuz;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

 
public class WithdrawDanItem : MonoBehaviour
{
    public Image icon;
    public TMP_Text danText;

    public TMP_Text hintText;
    public TMP_Text moneyText1;
    public TMP_Text moneyText2;
    public TMP_Text moneyText3;

    public List<TMP_Text> maxlevelTexts;

    public GameObject prepareStateObj;
    public GameObject claimStateObj;
    public GameObject claimedStateObj;

    public Image progressImage;
    public TMP_Text progressText;

    private bool isArrive; // 是否达到目标
    private float _dollar;
    private int _index;
    private WithdrawDanPanel _withdrawDanPanel;

    public BizzaButton prepareStateBtn;
    public BizzaButton claimStateBtn;
    public BizzaButton claimedStateBtn;

    private void Awake()
    {
        prepareStateBtn.onClick.AddListener(OnClickClaim);
        claimStateBtn.onClick.AddListener(OnClickClaim);
        claimedStateBtn.onClick.AddListener(OnClickClaim);
    }

    /// <summary>
    /// 初始化 段位Item
    /// </summary>
    /// <param name="icon">图标</param>
    /// <param name="dan">图标Icon内容</param>
    /// <param name="hint">段位提示</param>
    /// <param name="current">段位等级</param>
    /// <param name="dollar">提现金额</param>
    public void Init(int index, float dollar, Sprite icon, string dan, string hint, string current, int curCount, int maxCount, bool isClaimed,
        bool isArrive, WithdrawDanPanel withdrawDanPanel)
    {
        _index = index;
        this.isArrive = isArrive && !isClaimed;
        _dollar = dollar;
        this.icon.sprite = icon;
        danText.text = dan;
        _withdrawDanPanel = withdrawDanPanel;
        hintText.text = hint;
        foreach (var currentText in maxlevelTexts)
        {
            currentText.text = current;
        }
        float money = ItemUtils.FormatCountFloat(new ItemEntry()
        {
            Type = E_ItemType.WithDrawDanDollar,
            Count = dollar,
        });
        string moneyStr = LanguageUtils.GetText("CurrencyToken")+ItemUtils.GetFormatDollar(money);
        moneyText1.text = moneyStr;
        moneyText2.text = moneyStr;
        moneyText3.text = moneyStr;
        progressText.text = curCount + "/" + maxCount;
        progressImage.fillAmount = (float)curCount / maxCount;

        prepareStateObj.SetActive(curCount < maxCount);
        claimStateObj.SetActive(curCount >= maxCount && !isClaimed);
        claimedStateObj.SetActive(isClaimed);
    }

    [ObfuzIgnore(ObfuzScope.MethodName)]
    public void OnClickClaim()
    {
        if (SaveDataUtils.WithDrawDanPanelData.IsClaimed(_index))
        {
            UIUtils.ShowLanguageTips("WithdrawDanPanel_AlreadyObtained");
        }
        else if (isArrive)
        {
            ItemUtils.AddItem(
                new ItemEntry()
                {
                    Type = E_ItemType.WithDrawDanDollar,
                    Count = _dollar,
                },
                new AddItemParam()
                {
                    playAnim = true,
                    isAd = false,
                    startPos = icon.transform.position,
                    bUiPos = false,
                    source = E_AddItemSource.Dan,
                    target = _withdrawDanPanel.balanceTxt.transform,
                });
            SaveDataUtils.WithDrawDanPanelData.SetClaimState(_index, E_RewardStateType.Claimed);
            SaveDataUtils.WithDrawDanPanelStrategy.SaveData();
            claimStateObj.SetActive(false);
            claimedStateObj.SetActive(true);
        }
        else
        {
            // 关闭界面
            UIModule.Instance.ClosePage(UIPageIds.WithdrawDanPanel);
        }
        
        BizzaEventSystem.Emit(EventDefine.Game.RefreshDanItem);
    }
}
#endif