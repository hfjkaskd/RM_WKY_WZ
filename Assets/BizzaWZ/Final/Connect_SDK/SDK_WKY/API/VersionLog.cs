
using System.Collections;
using System.Collections.Generic;
#if BIZZA_REAL_WITHDRAW
using Bizza.Sdk;
#endif
using TMPro;
using UnityEngine;

public class VersionLog : MonoBehaviour
{
    public TMP_Text tMP_Text;
    private void Start()
    {
        if (tMP_Text == null)
        {
            tMP_Text = GetComponent<TMP_Text>();
        }
        if (tMP_Text== null )return;
        #if !BIZZA_REAL_WITHDRAW
        return;
        #endif

        #if BIZZA_REMOTEGROUPDATA
        tMP_Text.text = $"{ChannelConfig.Instance.real_CustomConfig.versionLog}_{(ChannelConfig.IsRelese_Mode ? 0 : 1)}_{RemoteGroupDataSystem.current.GetUserGroupName()}";
        #else
        tMP_Text.text = $"{ChannelConfig.Instance.real_CustomConfig.versionLog}_{(ChannelConfig.IsRelese_Mode ? 0 : 1)}";
        #endif

        
    }
}

