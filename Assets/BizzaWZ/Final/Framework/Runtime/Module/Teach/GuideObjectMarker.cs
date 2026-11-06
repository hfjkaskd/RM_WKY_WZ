using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideObjectMarker : MonoBehaviour
{
    [LabelText("标识ID")]
    public string GUID;

    [ShowInInspector, ReadOnly]
    public static Dictionary<string, GuideObjectMarker> Global = new Dictionary<string, GuideObjectMarker>();

    private void OnEnable()
    {
        RefreshData();
    }

    public void RefreshData()
    {
        if (string.IsNullOrEmpty(GUID))
            return;

        if (!Global.ContainsKey(GUID))
        {
            Global.Add(GUID, this);
        }
        else
        {
            Global[GUID] = this;
        }
    }

}
