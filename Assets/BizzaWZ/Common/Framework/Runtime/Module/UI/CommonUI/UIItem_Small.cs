using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItem_Small : MonoBehaviour
{
    public Image ItemBg;
    public Image ItemIcon;
    public TMP_Text Count;
    public Button ItemBtn;

    [HideInInspector]
    public int AmountCount;
    [HideInInspector]
    public string ResId;
    public GameObject FragmentObj;

    //private ItemConfig m_ItemConfig;

    private void Awake()
    {
        ItemBtn.onClick.RemoveAllListeners();
        ItemBtn.onClick.AddListener(OnItemBtnClick);
    }

    public void ShowItem(string resId, int count = 0)
    {
        Internal_ShowItem(resId, count);
    }

    public void ShowItem(ItemData itemData)
    {
        Internal_ShowItem(itemData.ResId, itemData.count);
    }

    private void Internal_ShowItem(string resId, int count = 0)
    {
        // ResId = resId;
        // AmountCount = count;
        //
        // Count.text = count.ToString();
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

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
