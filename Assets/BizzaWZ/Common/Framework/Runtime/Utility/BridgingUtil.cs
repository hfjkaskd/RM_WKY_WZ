using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class BridgingUtil
{
    public static int GameLevel
    {
        get => SaveDataUtils.GameData != null ? SaveDataUtils.GameData.playerSelectedLv : 1;
        set
        {
            if (SaveDataUtils.GameData != null)
            {
                SaveDataUtils.GameData.playerSelectedLv = value;
            }
        }
    }

    public const int MAX_REVIVE_COUNT = 1;

    public static void GameStart()
    {
        
    }

    public static async UniTask LoadGamePlayAsync()
    {
        await UniTask.CompletedTask;
    }

    public static void NewPlayerEnter()
    {
        
    }

    public static bool PropUse_1()
    {
        return WkyGameplayBridge.Instance == null;
    }

    public static bool PropUse_2()
    {
        return WkyGameplayBridge.Instance == null;
    }

    public static bool PropUse_3()
    {

        return WkyGameplayBridge.Instance == null;
    }

    public static bool PropUse_4()
    {
        return WkyGameplayBridge.Instance == null;
    }

    public static bool PropUse_5()
    {
        return WkyGameplayBridge.Instance == null;
    }

    public static void PropUseOver(E_ItemType itemType, bool breakFlow)
    {
        switch (itemType)
        {
            case E_ItemType.GameProp_1:
                PropUse_1_Over(breakFlow);
                break;
            case E_ItemType.GameProp_2:
                PropUse_2_Over(breakFlow);
                break;
            case E_ItemType.GameProp_3:
                PropUse_3_Over(breakFlow);
                break;
            case E_ItemType.GameProp_4:
                PropUse_4_Over(breakFlow);
                break;
            case E_ItemType.GameProp_5:
                PropUse_5_Over(breakFlow);
                break;
        }
    }

    public static void PropUse_1_Over(bool breakFlow)
    {
    }

    public static void PropUse_2_Over(bool breakFlow)
    {
    }

    public static void PropUse_3_Over(bool breakFlow)
    {
    }

    public static void PropUse_4_Over(bool breakFlow)
    {
    }

    public static void PropUse_5_Over(bool breakFlow)
    {
    }

    public static void CanShowGuide()
    {
    }

    public static void NewPlayerGuideEnd()
    {
        
    }

    public static void LoadGameLevel()
    {
        WkyGameplayBridge.Instance.ReloadAttempt().Forget();
    }

    public static void OnOpenGameWinPanel()
    {

    }

    public static void OnOpenGameLosePanel()
    {
        
    }

    public static void OnOpenGameRevivePanel()
    {
        
    }

    public static void OnReviveResult(bool isRevive)
    {
        WkyGameplayBridge.Instance.ResolveReviveOffer(isRevive);
    }
}
