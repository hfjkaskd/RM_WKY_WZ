
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
        #if BIZZA_REAL_WITHDRAW
        var config = ChannelConfig.Instance;
        if (config == null || string.IsNullOrWhiteSpace(config.real_CustomConfig.versionLog)) return;

        string version = config.real_CustomConfig.versionLog;
        #if BIZZA_REMOTEGROUPDATA
        tMP_Text.text = $"{version}_{(ChannelConfig.IsRelese_Mode ? 0 : 1)}_{RemoteGroupDataSystem.current.GetUserGroupName()}";
        #else
        tMP_Text.text = $"{version}_{(ChannelConfig.IsRelese_Mode ? 0 : 1)}";
        #endif
        #endif

        
    }
}

