#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

 
public class WithdrawAmountItem : MonoBehaviour
{
    public TMP_Text amountTxt;

    public GameObject getObj;
    public GameObject getedObj;

    public GameObject selectObj;
    public BizzaButton btn;
    private int _index;
    private bool _isStarterItem;
    private FakeWithdrawPanel _panel;

    public void Init(FakeWithdrawPanel panel, int index, string amount, bool canGet, bool isStarterItem)
    {
        _panel = panel;
        _index = index;
        amountTxt.text = amount;
        Refresh(canGet, isStarterItem);
        btn.onClick.RemoveListener(OnClick);
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (_isStarterItem && _panel.isReward) return;
        _panel.SetSelectIndex(_index);
        _panel.OnRefresh();
    }
    
    public void Refresh(bool canGet, bool isStarterItem)
    {
        _isStarterItem = isStarterItem;
        getObj.SetActive(isStarterItem && canGet);
        getedObj.SetActive(isStarterItem && !canGet);
    }

    public void OnSelectState(bool isSelect)
    {
        selectObj.SetActive(isSelect);
    }

    public void SetSelectState(bool isSelect)
    {
        selectObj.SetActive(isSelect);
    }
}
#endif