#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public partial class UIPageIds
{
    public static readonly PageId SlotPanel = "SlotPanel";
}

public class SlotPanel : UIPageBase
{
    public SlotMachineManager slotMachineManager;

    public TMP_Text slotHintTxt;
    public GameObject canClickObj;
    public GameObject notCanClickObj;

    public SlotRewardPanel slotRewardPanel;

    private float coinValue;

    private bool isSloting = false; public bool IsSloting => isSloting;

    public BizzaButton closeBtn;
    public BizzaButton faqBtn;
    public BizzaButton slotBtn;

    protected override void OnAwake()
    {
        base.OnAwake();

        closeBtn.onClick.AddListener(OnClosePanel);
        faqBtn.onClick.AddListener(OnClickFQA);
        slotBtn.onClick.AddListener(PlaySlotBtn);
    }

    protected override void OnClose()
    {
        BizzaEventSystem.Set(EventDefine.CustomGameEvent.SlotProgressChanged, OnProgressChanged, false);
        SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlayBGM("SFX_BGM");
    }

    protected override void OnOpen()
    {
        isSloting = false;
        BizzaEventSystem.Set(EventDefine.CustomGameEvent.SlotProgressChanged, OnProgressChanged, true);
        Refresh();
        SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlayBGM("SevenBgm");
    }

    private void OnProgressChanged()
    {
        if (!isSloting) Refresh();
    }

    private void Refresh()
    {
#if BIZZA_REAL_WITHDRAW
        coinValue = 0;
        bool isCanclick = SlotProgressUtil.CanFreeSpin;
        canClickObj.SetActive(isCanclick);
        notCanClickObj.SetActive(!isCanclick);
        slotRewardPanel.gameObject.SetActive(false);
        if (isCanclick)
        {
            slotHintTxt.text = LanguageUtils.GetText("SlotPanel_HaveSpin");
        }
        else
        {
            slotHintTxt.text = LanguageUtils.GetText("SlotPanel_AdSpin");
        }
#endif
    }

    public void OnClosePanel()
    {
        CloseSelf();
    }

    public void OnClickFQA()
    {
        UIModule.Instance.OpenPage(UIPageIds.SlotFAQPanel);
    }

    [Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.MethodName)]
    public void PlaySlotBtn()
    {
#if BIZZA_REAL_WITHDRAW
        if (isSloting) return;
        isSloting = true;
        if (SlotProgressUtil.CanFreeSpin)
        {
            SlotProgressUtil.SetProgress(0);
            OnPlaySlot(false);
        }
        else
        {
            float reward = WithdrawalUtil.GetCustomizedFloatByCountryType(
                WithdrawalUtil.GetDollarCountByReward());
            BizzaSdk.Ad.ShowRewardAd(
                E_AdPos.USSlot.ToString(),
                reward,
                OnAdResult
            );
            UIModule.Instance.m_curadvertistics--;
        }

        Refresh();
        BizzaEventSystem.Emit(EventDefine.CustomGameEvent.SlotProgressChanged);
#endif
    }

    [Button("Test Slot Btn")]
    public void TestSlotBtn()
    {
#if BIZZA_REAL_WITHDRAW
        float reward = WithdrawalUtil.GetCustomizedFloatByCountryType(
            WithdrawalUtil.GetDollarCountByReward());
        BizzaSdk.Ad.ShowRewardAd(
                E_AdPos.USSlot.ToString(),
                reward,
                OnAdResult
            );
#endif
    }

    private void OnAdResult(Bizza.Sdk.ShowAdResult param)
    {
        #if BIZZA_REAL_WITHDRAW
        bool isSuccess = param.success;
        AccountModule.OceanShineAdRevenueResponse response = param.response;
        if (!isSuccess || response == null)
        {
            isSloting = false;
            return;
        }
        coinValue = (float)response.GetBalance();
        //         LogUtil.Error("OnAdResult coinValue: " + coinValue);
        OnPlaySlot(true);
        #endif
    }

    private void OnPlaySlot(bool isAd)
    {
        float _dollar = isAd ? WithdrawalUtil.GetDollarCountByReward() : WithdrawalUtil.GetDollarCountBtFree();
        SoundManager.Instance.PlaySFX("SevenSpin");
        slotMachineManager.PlayAnim(
            isAd,
            (string type) =>
            {
                slotRewardPanel.gameObject.SetActive(true);
                slotRewardPanel.Init(coinValue, _dollar, type, isAd);
                isSloting = false;
            }
        );
    }

}
#endif
