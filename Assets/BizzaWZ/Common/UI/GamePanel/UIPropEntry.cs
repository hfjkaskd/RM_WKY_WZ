using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPropEntry : MonoBehaviour
{
    private const string LockedPropIconResource = "Recovered/UI/hud_booster_button";
    private static Sprite lockedPropIconSprite;

    public BizzaButton btn;
    public E_ItemType itemType;
    public TMP_Text itemNumTxt;

    public Image propIcon;
    public GameObject lockIcon;
    public TMP_Text unlockLevelTxt;

    public GameObject haveTips;
    public GameObject addTips;
    public GameObject cancelTips;
    public static E_ItemType usingPropType = E_ItemType.None;

    private PropConfigInfo propConfigInfo; public PropConfigInfo PropConfigInfo => propConfigInfo;
    private bool isUnLock = false; public bool IsUnLock => isUnLock;
    void Awake()
    {
        btn.onClick.AddListener(() =>
        {
            OnClickProp();
        });
    }

    void OnEnable()
    {
        UIItemUtils.Bind(itemType, itemNumTxt);
        BizzaEventSystem.Set(EventDefine.Item.ItemChanged, Action, true);
        BizzaEventSystem.Set(EventDefine.Item.GameWin, Refresh, true);
        BizzaEventSystem.Set(EventDefine.Item.PropUseOver, PropUseOver, true);
        if (propConfigInfo != null)
        {
            Refresh();
            return;
        }

        Action();
    }

    private void Action()
    {
        UIItemUtils.Bind(itemType, itemNumTxt);
        if (propConfigInfo != null && !isUnLock)
        {
            SetLockState();
            return;
        }
        addTips.SetActive(ItemUtils.GetItemCount(itemType) == 0);
    }

    void OnDisable()
    {
        UIItemUtils.Unbind(itemNumTxt);
        BizzaEventSystem.Set(EventDefine.Item.ItemChanged, Action, false);
        BizzaEventSystem.Set(EventDefine.Item.GameWin, Refresh, false);
        BizzaEventSystem.Set(EventDefine.Item.PropUseOver, PropUseOver, false);
    }

    public void Init(PropConfigInfo propConfigInfo)
    {
        this.propConfigInfo = propConfigInfo;
        propIcon.sprite = propConfigInfo.propIcon;
        itemType = propConfigInfo.propType;
        Refresh();
    }

    private void Refresh()
    {
        isUnLock = true;
        if (propConfigInfo.unlockFunction)
        {
            isUnLock = propConfigInfo.unlockCondition.IsUnlock(SaveDataUtils.GameData.playerSelectedLv);
        }

        if (isUnLock)
        {
            SetNormalState();
        }
        else
        {
            SetLockState();
        }
    }

    public void SetLockState()
    {
        haveTips.SetActive(false);
        addTips.SetActive(false);
        cancelTips.SetActive(false);
        propIcon.gameObject.SetActive(false);
        RefreshUnlockLevelTxt();
        if (unlockLevelTxt != null)
        {
            unlockLevelTxt.gameObject.SetActive(true);
        }
        ApplyLockedPropIconSprite();
        lockIcon.SetActive(true);
    }

    private void ApplyLockedPropIconSprite()
    {
        Image lockImage = lockIcon != null ? lockIcon.GetComponent<Image>() : null;
        if (lockImage == null)
        {
            return;
        }

        Sprite sprite = GetLockedPropIconSprite();
        if (sprite == null)
        {
            return;
        }

        lockImage.sprite = sprite;
        lockImage.preserveAspect = true;
    }

    private static Sprite GetLockedPropIconSprite()
    {
        if (lockedPropIconSprite != null)
        {
            return lockedPropIconSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(LockedPropIconResource);
        if (texture == null)
        {
            Debug.LogWarning($"Missing locked prop icon resource: {LockedPropIconResource}");
            return null;
        }

        lockedPropIconSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        lockedPropIconSprite.name = "hud_booster_button_locked_prop";
        return lockedPropIconSprite;
    }

    public void SetAddState()
    {
        lockIcon.SetActive(false);
        if (unlockLevelTxt != null)
        {
            unlockLevelTxt.gameObject.SetActive(false);
        }
        propIcon.gameObject.SetActive(true);
        haveTips.SetActive(false);
        addTips.SetActive(true);
        cancelTips.SetActive(false);
    }

    public void SetHaveState()
    {
        lockIcon.SetActive(false);
        if (unlockLevelTxt != null)
        {
            unlockLevelTxt.gameObject.SetActive(false);
        }
        propIcon.gameObject.SetActive(true);
        haveTips.SetActive(true);
        addTips.SetActive(false);
        cancelTips.SetActive(false);
    }

    public void SetCancelState()
    {
        lockIcon.SetActive(false);
        if (unlockLevelTxt != null)
        {
            unlockLevelTxt.gameObject.SetActive(false);
        }
        propIcon.gameObject.SetActive(true);
        haveTips.SetActive(false);
        addTips.SetActive(false);
        cancelTips.SetActive(true);
    }

    public void SetNormalState()
    {
        bool isHave = ItemUtils.GetItemCount(itemType) > 0;
        if (isHave)
        {
            SetHaveState();
        }
        else
        {
            SetAddState();
        }
    }

    private void RefreshUnlockLevelTxt()
    {
        if (unlockLevelTxt == null)
        {
            return;
        }

        if (propConfigInfo != null && propConfigInfo.unlockFunction)
        {
            unlockLevelTxt.text = $"Lv.{propConfigInfo.unlockCondition.unlockLevel}";
            return;
        }

        unlockLevelTxt.text = string.Empty;
    }

    private void OnClickProp()
    {
        if (!IsUnLock)
        {
            UIUtils.ShowLanguageTips("Tips_PropInLockState");
            return;
        }
        if (usingPropType != itemType && usingPropType != E_ItemType.None)
        {
            LogLogger.LogInfo("其它道具正在使用中 " + usingPropType);
            return;
        }

        if (usingPropType == itemType)
        {
            LogLogger.LogInfo("道具取消使用 " + itemType);
            SetNormalState();
            PropUtils.UsePropOver(itemType, true);
            usingPropType = E_ItemType.None;
            return;
        }

        var _propUseTimes = NumbericalStatistics._propUseTimes;
        if (_propUseTimes.TryGetValue(itemType, out var curTimes))
        {
            if (curTimes >= PropConfigInfo.preLimitNum)
            {
                UIUtils.ShowLanguageTips("Tips_PropReachMaxTimes");
                return;
            }
        }

        if (ItemUtils.GetItemCount(itemType) > 0)
        {
            bool usesuccess = PropUtils.UseProp(itemType);
            if (!usesuccess)
            {
                return;
            }
            usingPropType = itemType;
            if (PropConfigInfo.cancelFunction)
            {
                SetCancelState();
            }
            else
            {
                PropUseOver(itemType, usesuccess);
            }
        }
        else
        {
            LogLogger.LogInfo($"道具数量不足，无法使用:{itemType}");
#if BIZZA_REAL_WITHDRAW
            UIModule.Instance.OpenPage(UIPageIds.AddPropPanel, itemType).Forget();
#else
            ItemUtils.AddItem(new ItemEntry
            {
                Type = itemType,
                Count = 1,
            }, new AddItemParam
            {
                playAnim = true,
                startPos = propIcon != null ? propIcon.transform.position : transform.position,
                bUiPos = false,
                addFinishAction = SetNormalState,
            });
#endif
        }
    }

    private void PropUseOver(E_ItemType _itemType, bool usesuccess)
    {
        if (_itemType != itemType)
        {
            LogLogger.LogInfo("不是预先使用的道具，无法结束使用");
            return;
        }
        if (usingPropType != itemType)
        {
            Debug.LogError("道具使用结束，道具类型不匹配, 需要检查代码");
            return;
        }

        usingPropType = E_ItemType.None;

        if (!usesuccess)
        {
            LogLogger.LogInfo("道具使用失败, 不扣除数量");
            SetNormalState();
            return;
        }

        var _propUseTimes = NumbericalStatistics._propUseTimes;
        if (ItemUtils.TryReduceItem(itemType))
        {
            LogLogger.LogInfo($"使用了道具:{itemType}");

            if (!_propUseTimes.ContainsKey(itemType))
            {
                _propUseTimes.Add(itemType, 0);
            }

            _propUseTimes[itemType]++;
            StatisticModule.Instance.UseItem(itemType, SaveDataUtils.GameData.playerSelectedLv, _propUseTimes[itemType]);
        }
        SetNormalState();
    }
}
