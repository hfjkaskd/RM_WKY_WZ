using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SaveDataUtils
{
    public static readonly DataStrategy<ItemSaveData> itemStrategy = new();
    public static ItemSaveData ItemData => itemStrategy.Data;
}

[Obfuz.ObfuzIgnore]
public class ItemSaveData : ISaveData
{
    public Dictionary<E_ItemType, ItemEntry> itemMap = new();
    public List<string> rewardReceipts = new();

    public void InitData()
    {
    
    }

    public void AfterLoadData()
    {
    }

    public void BeforeSave()
    {
        
    }
}
