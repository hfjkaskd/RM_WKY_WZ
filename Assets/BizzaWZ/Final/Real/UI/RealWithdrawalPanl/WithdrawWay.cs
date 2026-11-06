#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

 
public class WithdrawWay : MonoBehaviour
{
    private Action<WithdrawWay> clickAction;

    public Image payIcon;
    
    public GameObject selectedObj;
    
    public PaymentConfig paymentConfig;

    public AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform data;
    
    [Header("按钮")]
    [SerializeField] private BizzaButton clickBtn;

    private void Awake()
    {
        clickBtn.onClick.AddListener(() => { OnClick(); });
    }

    public void Init(Action<WithdrawWay> clickAction, AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform _data)
    {
        this.data = _data;
        this.clickAction = clickAction;

        payIcon.sprite = paymentConfig.GetSpriteByIconKey(_data.Os_Cn);
        
        selectedObj.SetActive(false);
    }

    public void OnClick()
    {
        clickAction(this);
        selectedObj.gameObject.SetActive(true);
    }

    public void OnSelect()
    {
        selectedObj.gameObject.SetActive(true);
    }

    public void OnDeselect()
    {
        selectedObj.gameObject.SetActive(false);
    }
}


[Serializable]
public class WithdrawWayIcon
{
    public string key;
    public Sprite sprite;
}
#endif