#if BIZZA_REAL_WITHDRAW
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "可以显示教程", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
public class Variable_Bool_CanShowTeach : Variable_Bool
{
    public override object Clone()
    {
        var clone = new Variable_Bool_CanShowTeach();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        var gameplay = World.Current.GetGameMode<GameMode_GamePlay>();
        if (gameplay == null)
        {
            return false;
        }
        bool canPlay = SaveDataUtils.GameData.customTutorialCanPlay;

        return canPlay;
    }
}
#endif
