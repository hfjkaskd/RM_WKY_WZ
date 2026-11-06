using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class ShowAllText : MonoBehaviour
{
    public TMP_Text text;

    [Button]
    public void Show()
    {
        HashSet<char> hs = new();
        foreach (var v in TableUtils.Tables.TblLanguage.DataList)
        {
            foreach (var v2 in v.Dict)
            {
                foreach (var v3 in v2.Value)
                {
                    hs.Add(v3);
                }
            }
        }

        text.text = string.Concat(hs);
    }
}
