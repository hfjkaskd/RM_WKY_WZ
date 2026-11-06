#if BIZZA_REAL_WITHDRAW
#if BIZZA_ENABLE_SHUSHU
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using ThinkingData.Analytics;

namespace Bizza.Sdk
{
    public class ThinkingDataAdapter : AnalysisSdkBase
    {
        private bool needInit = true;
        private static readonly Dictionary<string, object> userProperties = new();

        public static ThinkingDataAdapter Instance { get; private set; }

        public ThinkingDataAdapter()
        {
            Instance = this;
        }

        public override void Init(ChannelConfig channelConfig)
        {
            var appCode = channelConfig.tkConfig.appCode;
            string udid = SystemInfo.deviceUniqueIdentifier;
            string openUdid = appCode + "@" + udid;
            var appId = channelConfig.tkConfig.appId;
            var serverUrl = channelConfig.tkConfig.serverUrl;
            Debug.Log("数数登录 " + openUdid);
            TDAnalytics.Init(appId, serverUrl);
            TDAnalytics.SetDistinctId(openUdid);
            TDAnalytics.Login(openUdid);
            TDAnalytics.EnableAutoTrack(TDAutoTrackEventType.All);
            Dictionary<string,string> dict = new Dictionary<string, string>();
            // var appCode = ChannelConfig.Instance.tkConfig.appCode;
            dict.Add("appcode", appCode);
            var t=JsonConvert.SerializeObject(dict);
            TDAnalytics.UserSet(t);
            TDAnalytics.EnableLog(channelConfig.tkConfig.debug);
            needInit = false;
            Debug.Log("数数初始化成功，初始化的appcode为" + appCode );
        }

        public override void SendCustomEvent(string eventName, Dictionary<string, object> eventParams)
        {
            if (needInit) { return; }
            AddCommonParams(eventParams);
            TDAnalytics.Track(eventName, eventParams);
        }

        /// <summary>
        /// 添加通用参数
        /// </summary>
        private static void AddCommonParams(Dictionary<string, object> eventParams)
        {
            string openUdid = ChannelConfig.Instance.tkConfig.appCode;
            eventParams["appcode"] = openUdid;
        }

        public override void SetUserProperty(string key, object value)
        {
            userProperties[key] = value;
        }

        public override void UploadUserProperty(Dictionary<string, object> properties)
        {
            if (needInit) { return; }
            TDAnalytics.UserSet(properties);
        }
    }
}
#endif
#endif
