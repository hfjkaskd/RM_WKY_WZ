#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Bizza;

 
public class BonusBonus : MonoBehaviour
{
    public BonusRate bonusRate;
    
    public TMP_Text refreshTimeTxt;
    
    public DailyBonusItem bonusItem;
    public Transform root;
    
    public List<DailyBonusItem> items = new List<DailyBonusItem>();
    
    private bool isInitForEditor = false;

    public void Refresh()
    {
        DestroyForEditor();
        
        bonusRate.Init("5.0%", true, false);
        
        // List<>
        items.SetCmptListCount<DailyBonusItem>(bonusItem, root, 3);
        
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