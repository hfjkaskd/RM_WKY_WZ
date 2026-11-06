#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinMachineBackArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponent<CoinMachineItem>();
        if (item != null)
        {
            Destroy(item.gameObject);
            ItemUtils.AddItem(E_ItemType.Gold, 1);
        }
    }
}
#endif
