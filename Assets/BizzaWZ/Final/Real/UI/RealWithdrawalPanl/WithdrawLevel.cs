#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bizza;


public class WithdrawLevel : MonoBehaviour
{
    public WithdrawLevelItem withdrawLevelItem;
    public Transform root;
    
    public List<WithdrawLevelItem> items = new List<WithdrawLevelItem>();
    
    private bool isInitForEditor = false;

    public void Refresh()
    {
        DestroyForEditor();
        // List<>
        int count = 3;
        items.SetCmptListCount(withdrawLevelItem, root, count);
        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                // items[i].Init();
            }
        }
    }

    public void DestroyForEditor()
    {
        if (isInitForEditor)
        {
            return;
        }

        isInitForEditor = true;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }
}
#endif