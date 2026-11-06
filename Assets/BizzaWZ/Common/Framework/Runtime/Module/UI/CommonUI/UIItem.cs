using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensions;
using UnityExtensions.Tween;

public class ItemData
{
    public string ResId;
    public int count;
}

public class UIItem : MonoBehaviour
{
    public Image ItemBg;
    public Image ItemIcon;
    public TMP_Text LevelText;
    public TMP_Text Count;
    public TweenPlayer Anim;
    public Button ItemBtn;
    public GameObject FragmentObj;

    [HideInInspector]
    public float AmountCount;
    [HideInInspector]
    public E_ItemType ResId;

   // private ItemConfig m_ItemConfig;

    private void Awake()
    {
        ItemBtn.onClick.RemoveAllListeners();
        ItemBtn.onClick.AddListener(OnItemBtnClick);
    }

    public void ShowItem(E_ItemType resId, float count = 0)
    {
        Internal_ShowItem(resId, count);
    }

    public void ShowItem(ItemEntry item)
    {
        ShowItem(item.Type, item.Count);
    }

    // public void ShowItem(ItemEntry itemData)
    // {
        // Internal_ShowItem(itemData.Type, itemData.Count);
    // }

    private void Internal_ShowItem(E_ItemType resId, float count = 0)
    {
        ResetAnim();

        ResId = resId;
        AmountCount = count;

        var item = new ItemEntry()
        {
            Type = resId,
            Count = count,
        };
        Count.text = ItemUtils.GetItemText(item);
        bool isFrag = resId.ToString().StartsWith("Fragment_");
        FragmentObj.SetObjActive(isFrag);
        if (isFrag)
        {
            FragmentObj.GetComponent<Image>().sprite = ItemUtils.GetItemFragIcon(item);
        }

        // FragmentObj.SetObjActive(ItemConfig.ItemType == E_ItemType.TowerFragment);
    }

    public void PlayAnim()
    {
        Anim?.ForwardRestart();
    }

    private void ResetAnim()
    {
        Anim?.ResetToBegin();
    }

    private void OnItemBtnClick()
    {
        // _ = UIModule.Instance.OpenPage(UIPageIds.UI_CommonTipPage, new OpenTipPageArgs()
        // {
        //     pointTrans = transform as RectTransform,
        //     NameText = LanguageUtil.GetText(m_ItemConfig.Name),
        //     DescText = LanguageUtil.GetText(m_ItemConfig.Description)
        // });
    }

    public void HideCountUI()
    {
        this.Count.gameObject.SetActive(false);
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }
    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
