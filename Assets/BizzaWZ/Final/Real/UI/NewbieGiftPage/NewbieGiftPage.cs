#if BIZZA_REAL_WITHDRAW
using Bizza.Sdk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId NewbieGiftPage = "NewbieGiftPage";
}

public class NewbieGiftPage : UIPageBase
{
    public BizzaButton continueBtn;

    public static string pageKey = "NewbieGiftPage";

    public TMP_Text Os_ComText; // commonMerge

    private AccountModule.OceanShineUserInfoResponse userInfoResponse;

    void Awake()
    {
        BizzaEventSystem.On(EventDefine.RealWithdraw.Response_UserReachReportResponse, GetNewUserReward);
        continueBtn.onClick.AddListener(() =>
        {
            OnButtonClick();
            if (!ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
            {
                 UIModule.Instance.OpenPage(UIPageIds.FakeWithdrawPanel);
            }
        });

        void GetNewUserReward()
        {
            UIModule.Instance.ClosePage(UIPageIds.NewbieGiftPage);
            PlayerPrefs.SetInt(pageKey, 1);
            PlayerPrefs.Save();

            World.Current.GetGameMode<GameMode_Loading>().StartGame();
            BizzaEventSystem.Off(EventDefine.RealWithdraw.Response_UserReachReportResponse, GetNewUserReward);
        }
    }

    private float _nuwbieGift;

    protected override void OnOpen()
    {
        _nuwbieGift = WithdrawalUtil.GetNewbieGift();
        Os_ComText.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(_nuwbieGift)}";
        // AccountModule.Instance.Request_UserReachReportRequest(OnRefresh);
    }

    public void OnButtonClick()
    {
        if (_nuwbieGift > 0)
        {
            ItemEntry itemEntry = new()
            {
                Count = _nuwbieGift,
                Type = E_ItemType.Dollar
            };

            if (ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
            {
                itemEntry.Type = E_ItemType.Gold;
            }
            ItemUtils.AddItem(itemEntry, new AddItemParam()
            {
                playAnim = true,
                animation = RewardCollectAnimation.BurstCollect,
                bUiPos = false,
                startPos = continueBtn.GetComponent<RectTransform>().position,
                showCurrencyBar = true,
            });

            CloseSelf();
        }
        else
        {
            LogLogger.LogError($"新手引导 - _nuwbieGift = {_nuwbieGift},是没有获得还是给的就是0 ??");
        }
    }

    protected override void OnClose()
    {
    }
}
#endif
