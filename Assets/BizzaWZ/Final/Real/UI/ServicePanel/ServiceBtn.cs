#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServiceBtn : MonoBehaviour
{
    public BizzaButton bizzaButton;
    
    public Image redDot;

    private void Awake()
    {
        if (bizzaButton == null)
        {
            bizzaButton = GetComponent<BizzaButton>();
        }
        bizzaButton.onClick.AddListener(() =>
        {
            UIModule.Instance.OpenPage(UIPageIds.ServicePanel);
        });
       
    }

    private void OnEnable()
    {
         BizzaEventSystem.On(EventDefine.Item.ServiceDataAlter, Refresh);
         Refresh();
    }

    private void OnDisable()
    {
         BizzaEventSystem.Off(EventDefine.Item.ServiceDataAlter, Refresh);
    }

    private void Refresh()
    {
        if (redDot != null)
        {
            redDot.gameObject.SetActive(SaveDataUtils.GameData.serviceAlter);
        }
    }
}
#endif