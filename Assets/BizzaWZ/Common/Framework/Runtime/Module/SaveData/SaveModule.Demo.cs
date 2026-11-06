using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveModule_Demo : MonoBehaviour
{
    public void Test()
    {
        // //获取金币数量
        // var coinNum = SaveDataUtil.GameData.GetRes(E_ResType.Currency, "Coin");
        //
        // //添加100金币
        // SaveDataUtil.GameData.AddRes(new ResSave
        // {
        //     type = E_ResType.Currency,
        //     key = "Coin",//这里的key需要根据货币表的id填写,自行配表填写，对应即可
        //     count = 100,
        // });
        //
        // //尝试花费100金币
        // bool success = SaveDataUtil.GameData.TryReduceRes(new ResSave
        // {
        //     type = E_ResType.Currency,
        //     key = "Coin", //这里的key需要根据货币表的id填写,自行配表填写，对应即可
        //     count = 100,
        // });
        // Debug.Log($"花费金币100:{success}");
        //
        // //升级卡牌
        // SaveDataUtil.GameData.UpgradeCard(E_ResType.Card, "SK_Gun", 1);//将SK_Gun的卡牌升级1级，SK_Gun为具体的卡牌id，自行配表填写，对应即可
        //
        // //增加体力超出上限
        // SaveDataUtil.GameData.AddRes(new ResSave(){key = "Energy", count = 30, type = E_ResType.Currency}, param: new AddResParam()
        // {
        //     overLimit = true,
        // });
    }
}
