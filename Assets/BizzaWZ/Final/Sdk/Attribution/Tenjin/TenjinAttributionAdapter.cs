#if BIZZA_REAL_WITHDRAW
#if BIZZA_ENABLE_TENJIN
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza.Sdk
{
    public class TenjinAttributionAdapter : AttributionAdapterBase
    {
    public override async void Init(ChannelConfig channelConfig)
    {
        // string key = ChannelConfig.Instance.tenjinKey;
        // Debug.Log("初始化Tenjin  记得修改KEY " + key);
        var key = channelConfig.tenjinKey;
        BaseTenjin instance = Tenjin.getInstance(key);
#if UNITY_IOS
        // instance.RegisterAppForAdNetworkAttribution();
        string sysVer = UnityEngine.iOS.Device.systemVersion;
        string[] verArr = sysVer.Split('.');
        if (verArr.Length < 1)
        {
            verArr = new[] { "18" };//补充默认值
        }
        int mainV = int.Parse(verArr[0]);
        if (mainV > 14)
        {
            Debug.Log("Tenjin 请求AT");

            // Tenjin wrapper for requestTrackingAuthorization
            instance.RequestTrackingAuthorizationWithCompletionHandler((status) =>
            {
                Debug.Log("===> App Tracking Transparency Authorization Status: " + status);
                switch (status)
                {
                    case 0:
                        Debug.Log("ATTrackingManagerAuthorizationStatusNotDetermined case");
                        Debug.Log("Not Determined");
                        Debug.Log("Unknown consent");
                        break;
                    case 1:
                        Debug.Log("ATTrackingManagerAuthorizationStatusRestricted case");
                        Debug.Log(@"Restricted");
                        Debug.Log(@"Device has an MDM solution applied");
                        break;
                    case 2:
                        Debug.Log("ATTrackingManagerAuthorizationStatusDenied case");
                        Debug.Log("Denied");
                        Debug.Log("Denied consent");
                        break;
                    case 3:
                        Debug.Log("ATTrackingManagerAuthorizationStatusAuthorized case");
                        Debug.Log("Authorized");
                        Debug.Log("Granted consent");
                        break;
                    default:
                        Debug.Log("Unknown");
                        break;
                }

                Debug.Log("Tenjin ATT初始化");

                AttributionUtil.DoTenjinConnect(instance);

            });
        }
        else
        {
            Debug.Log("Tenjin 直接初始化");
            AttributionUtil.DoTenjinConnect(instance);
        }
#elif UNITY_ANDROID
        instance.SetAppStoreType(AppStoreType.googleplay);
        // Sends install/open event to Tenjin
        instance.Connect();
#endif

    }

    public override void AttributeAdShow(object param)
    {

    }
    }
}
#endif
#endif
