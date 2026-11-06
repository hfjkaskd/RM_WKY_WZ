#if BIZZA_REAL_WITHDRAW
using System;
using Bizza.Sdk;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting;


[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "是否纯真提现", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class Action_Bool_IsSingleCurrency : Variable_Bool
{
    public override object Clone()
    {
        var clone = new Action_Bool_IsSingleCurrency();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
#if BIZZA_REAL_WITHDRAW
        bool isSingleCurrency = ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode;
        return isSingleCurrency;
#else
        return false;
#endif
    }
}

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "是否为正式包", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class Action_Bool_IsWhitePage : Variable_Bool
{
    public override object Clone()
    {
        var clone = new Action_Bool_IsWhitePage();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        bool isOfficePage;
        #if BIZZA_REAL_WITHDRAW
        isOfficePage = true;
        #else
        isOfficePage = false;
        #endif
        return isOfficePage;
    }
}

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "是否非美国", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class Action_Bool_IsNotUS : Variable_Bool
{
    public override object Clone() => new Action_Bool_IsNotUS();

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        return AccountModule.CountryType != AccountModule.E_CountryType.US;
    }
}
#endif
