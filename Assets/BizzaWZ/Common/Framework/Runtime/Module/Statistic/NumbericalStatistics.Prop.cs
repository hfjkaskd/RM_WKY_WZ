using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class NumbericalStatistics
{
    public static Dictionary<E_ItemType, int> _propUseTimes = new();
    #region 道具

    public static void InitProp()
    {
        _propUseTimes[E_ItemType.GameProp_1] = 0;
        _propUseTimes[E_ItemType.GameProp_2] = 0;
        _propUseTimes[E_ItemType.GameProp_3] = 0;
        _propUseTimes[E_ItemType.GameProp_4] = 0;
        _propUseTimes[E_ItemType.GameProp_5] = 0;
    }


    #endregion

}
