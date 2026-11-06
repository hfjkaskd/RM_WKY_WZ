#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using UnityEngine;

public class ShowBySingleMode : MonoBehaviour
{
    // 

    private void Awake()
    {
        Show();
    }

    private void Show()
    {
        gameObject.SetActive(!ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode);
    }
}
#endif