#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using UnityEngine;

public class SingleCurrencyMode : MonoBehaviour
{
    [SerializeField, Header("如果是在真混模式下不显示则勾选")] private bool isReverse = false;
    private void Awake()
    {
        if (isReverse)
        {
            gameObject.SetActive(ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode);
        }
        else
        {

            gameObject.SetActive(!ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode);
        }
    }
}
#endif