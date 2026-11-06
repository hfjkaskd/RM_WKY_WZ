using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PropUtils
{
    public static bool UseProp(E_ItemType itemType)
    {
        bool used = false;
        switch (itemType)
        {
            case E_ItemType.GameProp_1:
                used = UseProp_1();
                break;
            case E_ItemType.GameProp_2:
                used = UseProp_2();
                break;
            case E_ItemType.GameProp_3:
                used = UseProp_3();
                break;
            case E_ItemType.GameProp_4:
                used = UseProp_4();
                break;
            case E_ItemType.GameProp_5:
                used = UseProp_5();
                break;
        }
        return used;
    }

    public static bool UseProp_1()
    {
        return FlowModule.UseProp_1();
    }

    public static bool UseProp_2()
    {
        return FlowModule.UseProp_2();
    }

    public static bool UseProp_3()
    {
        return FlowModule.UseProp_3();
    }

    public static bool UseProp_4()
    {
        return FlowModule.UseProp_4();
    }

    public static bool UseProp_5()
    {
        return FlowModule.UseProp_5();
    }

    public static void UsePropOver(E_ItemType itemType, bool breakFlow)
    {
        switch (itemType)
        {
            case E_ItemType.GameProp_1:
                UseProp_1_Over(breakFlow);
                break;
            case E_ItemType.GameProp_2:
                UseProp_2_Over(breakFlow);
                break;
            case E_ItemType.GameProp_3:
                UseProp_3_Over(breakFlow);
                break;
            case E_ItemType.GameProp_4:
                UseProp_4_Over(breakFlow);
                break;
            case E_ItemType.GameProp_5:
                UseProp_5_Over(breakFlow);
                break;
        }
    }

    public static void UseProp_1_Over(bool breakFlow)
    {
        FlowModule.PropUse_1_Over(breakFlow);
    }

    public static void UseProp_2_Over(bool breakFlow)
    {
        FlowModule.PropUse_2_Over(breakFlow);
    }

    public static void UseProp_3_Over(bool breakFlow)
    {
        FlowModule.PropUse_3_Over(breakFlow);
    }

    public static void UseProp_4_Over(bool breakFlow)
    {
        FlowModule.PropUse_4_Over(breakFlow);
    }

    public static void UseProp_5_Over(bool breakFlow)
    {
        FlowModule.PropUse_5_Over(breakFlow);
    }
}
