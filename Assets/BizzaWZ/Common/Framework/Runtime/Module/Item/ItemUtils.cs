using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[Obfuz.ObfuzIgnore]
public enum E_AddItemSource
{
    None,
    Match,
    MatchBoost,
    TaskBonus,
    Settlement,
    Flow,
    Dan,
    UsSlot,
}

[Obfuz.ObfuzIgnore]
public partial struct AddItemParam
{
    public bool playAnim;
    public Vector3 startPos;
    public bool bUiPos;
    public E_AddItemSource source;
    public Transform target;
    public Action addFinishAction;
    public RewardCollectAnimation animation;
    public UnityEngine.Object callbackOwner;
    public string rewardId;
}

public enum RewardCollectAnimation { Legacy = 0, BurstCollect = 1 }


[Obfuz.ObfuzIgnore]
public partial class ItemUtils
{
    public static ItemEntry ToCorrect(ItemEntry item)
    {
        return item;
    }

    public static string FormatCount(ItemEntry item)
    {
        if (Mathf.Abs(item.Count - Mathf.Round(item.Count)) < 0.001f)
        {
            return Mathf.RoundToInt(item.Count).ToString();
        }

        return item.Count.ToString("0.##");
    }

    public static Sprite GetSprite(string path)
    {
        return AssetUtils.LoadAssetSync<Sprite>(path);
    }

    public static float FormatCountFloat(ItemEntry item)
    {
        return item.Count;
    }

    public static ItemEntry Get(E_ItemType itemType)
    {
        if (!SaveDataUtils.ItemData.itemMap.TryGetValue(itemType, out var item))
        {
            item = new ItemEntry
            {
                Type = itemType,
            };
        }

        return item;
    }

    public static string GetItemText(E_ItemType itemType)
    {
        return GetItemText(Get(itemType));
    }

    public static string GetItemText(ItemEntry item)
    {
        return FormatCount(item);
    }

    public static Sprite GetItemIcon(ItemEntry item)
    {
        return GetItemIcon(item.Type);
    }

    public static Sprite GetItemFragIcon(ItemEntry item)
    {
        return null;
    }

    public static Sprite GetItemIcon(E_ItemType itemType)
    {
        PropConfigSO propConfig = PropConfigSO.Instance;
        if (propConfig == null)
        {
            return null;
        }

        var itemConfig = propConfig.GetPropConfigInfo(itemType);
        return itemConfig == null ? null : itemConfig.propIcon;
    }

    public static void AddItem(ItemEntry inItem, AddItemParam inParam)
    {
        #if BIZZA_REAL_WITHDRAW
        GrantReward(inItem, inParam)?.Play(inParam.startPos);
        #endif
    }

    public static void AddItem(ItemEntry item, bool playAnim = false, Vector3 startPos = default, float rate = 1.0f)
    {
        AddItem(item.Type, item.Count * rate, playAnim, startPos);
    }

    public static void AddItem(E_ItemType itemType, float amount, bool playAnim = false, Vector3 startPos = default, bool bUiPos = true)
    {
        AddItem(new ItemEntry()
        {
            Type = itemType,
            Count = amount,
        }, new AddItemParam()
        {
            playAnim = playAnim,
            bUiPos = bUiPos,
            startPos = startPos,
        });
    }

    public static bool TryReduceItem(E_ItemType itemType, int count = 1)
    {
        return TryReduceItemFloat(itemType, count);
    }

    public static bool TryReduceItemFloat(E_ItemType itemType, float count = 1)
    {
#if BIZZA_REAL_WITHDRAW
        if (float.IsNaN(count) || float.IsInfinity(count) || count < 0f || SaveDataUtils.ItemData == null)
            return false;
#endif
        if (GetItemCount(itemType) < count)
        {
            return false;
        }

#if BIZZA_REAL_WITHDRAW
        var presentation = GrantReward(new ItemEntry { Type = itemType, Count = -count }, default);
        if (presentation == null) return false;
        presentation.Play(default);
#else
        AddItem(itemType, -count);
#endif
        return true;
    }

    public static float GetOriginItemCount(E_ItemType itemType)
    {
        return Get(itemType).Count;
    }

    public static float GetItemCount(E_ItemType itemType)
    {
        return Get(itemType).Count;
    }

    public static int GetItemCountInt(E_ItemType itemType)
    {
        return Mathf.RoundToInt(GetItemCount(itemType));
    }

    public static void AddItem(ItemEntry a, ItemEntry b)
    {
    }

    private static int _showCurrencyBar;
    private static int _currencyBarVersion;
    private static void OnMoneyFlyStart()
    {
        _showCurrencyBar++;
        _currencyBarVersion++;
        #if BIZZA_REAL_WITHDRAW
        if (_showCurrencyBar > 0)
        {
            BizzaEventSystem.Emit(EventDefine.Frame.ShowCurrencyBar, true);
        }
        #endif
    }

    private static void OnMoneyFlyFinish()
    {
        _showCurrencyBar = Mathf.Max(0, _showCurrencyBar - 1);
        int version = ++_currencyBarVersion;
        #if BIZZA_REAL_WITHDRAW
        if (_showCurrencyBar <= 0)
        {
            GameUtils.DelayDo(() =>
            {
                if (_showCurrencyBar == 0 && version == _currencyBarVersion)
                    BizzaEventSystem.Emit(EventDefine.Frame.ShowCurrencyBar, false);
            }, 1);
        }
        #endif
    }
}
