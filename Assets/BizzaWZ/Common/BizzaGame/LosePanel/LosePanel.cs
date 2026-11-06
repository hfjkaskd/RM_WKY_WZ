using System;
using Unity.VisualScripting;
using UnityEngine;

public partial class UIPageIds
{
    public static readonly PageId LosePanel = "LosePanel";
}

public partial class GameSaveData : ISaveData
{
    public int currentReviveCount = 0;
    public int levelFailCount;
}

public enum LoseReason
{
    Health,
    Timeout,
}

public struct LevelInfo
{
    public double levelProgress;
    public int levelTotalTarget;
    public int levelAchieveTarget;
    public int levelRemainingTarget; 
    public string levelPropUsedCount;
}

public class LosePanel : UIPageBase<LoseReason, LevelInfo>
{
    public override bool PreserveRectTransformOnOpen => true;

    public BizzaButton reviveButton;

    public BizzaButton bizzaLoseButton1;
    public BizzaButton bizzaLoseButton2;

    public GameObject[] reviveObjs;
    public GameObject[] loseObjs;

    private LoseReason loseReason;
    private LevelInfo levelInfo;
    private int pageGeneration;
    private bool offersRevive;
    private bool adPending;

    protected override void OnClose()
    {
        pageGeneration++;
        adPending = false;
        if (offersRevive)
            WkyGameplayBridge.Instance?.ResolveReviveOffer(false, this);
        offersRevive = false;
        if (reviveButton != null)
        {
            reviveButton.onClick.RemoveListener(OnClickReviveButton);
        }

        if (bizzaLoseButton1 != null)
        {
            bizzaLoseButton1.onClick.RemoveListener(RestartLevel);
        }

        if (bizzaLoseButton2 != null)
        {
            bizzaLoseButton2.onClick.RemoveListener(RestartLevel);
        }
    }

    protected override void OnOpen(LoseReason loseReason, LevelInfo levelInfo)
    {
        this.loseReason = loseReason;
        this.levelInfo = levelInfo;
        if (reviveButton != null)
        {
            reviveButton.onClick.AddListener(OnClickReviveButton);
        }

        if (bizzaLoseButton1 != null)
        {
            bizzaLoseButton1.onClick.AddListener(RestartLevel);
        }

        if (bizzaLoseButton2 != null)
        {
            bizzaLoseButton2.onClick.AddListener(RestartLevel);
        }
        pageGeneration++;
        adPending = false;
        offersRevive = WkyGameplayBridge.Instance?.CanOfferRevive == true;
        if (reviveButton != null)
            reviveButton.interactable = offersRevive;
        SetObjsActive(reviveObjs, offersRevive);
        SetObjsActive(loseObjs, !offersRevive);
    }

    private void OnClickReviveButton()
    {
        var bridge = WkyGameplayBridge.Instance;
        if (!offersRevive || adPending || bridge?.CanOfferRevive != true)
            return;

        adPending = true;
        reviveButton.interactable = false;
        int expectedGeneration = pageGeneration;
#if BIZZA_REAL_WITHDRAW
        if (!BizzaSdk.Ad.Inited)
        {
            FinishReviveAd(expectedGeneration, false);
            return;
        }

        try
        {
            float reward = WithdrawalUtil.GetCustomizedFloatByCountryType(
                WithdrawalUtil.GetDollarCountBtFree());
            BizzaSdk.Ad.ShowRewardAd("Revive", reward,
                result => FinishReviveAd(expectedGeneration, result.success));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            FinishReviveAd(expectedGeneration, false);
        }
#else
        FinishReviveAd(expectedGeneration, false);
#endif
    }

    private void FinishReviveAd(int expectedGeneration, bool succeeded)
    {
        if (this == null || expectedGeneration != pageGeneration || !adPending || IsClosing ||
            WkyGameplayBridge.Instance?.CanOfferRevive != true)
            return;

        adPending = false;
        FlowModule.OnReviveResult(succeeded, loseReason, levelInfo);
    }

    private void RestartLevel()
    {
        if (offersRevive)
        {
            WkyGameplayBridge.Instance?.ResolveReviveOffer(false, this);
            return;
        }

        FlowModule.LoadGameLevel();
    }

    private static void SetObjsActive(GameObject[] objs, bool active)
    {
        if (objs == null)
        {
            return;
        }

        foreach (var obj in objs)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }
}
