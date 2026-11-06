#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bizza.Sdk
{
    public class OverseaPlatform 
    {
        public static bool IsInit { get; set; }
        public static AggregationAdSdk AdSdk { get; set; }
        public static AdjustAttributionAdapter AttributeSdk { get; protected set; }

        public OverseaPlatform (ChannelConfig channelConfig)
        {
            InitAdAdapter(channelConfig);
            InitAttributeAdapter(channelConfig);
            IsInit = true;
        }

        public void InitAdAdapter(ChannelConfig channelConfig)
        {
            if (AdSdk != null)
            {
                return;
            }

            AdSdk = new AggregationAdSdk();
            AdSdk.InitAdAdapter(channelConfig);
        }

        #region 归因

        public void InitAttributeAdapter(ChannelConfig channelConfig)
        {
            if (AttributeSdk != null)
            {
                return;
            }

            AttributeSdk = new AdjustAttributionAdapter();

            // if (AttributeSdk != null)
            // {
            //     AttributeSdk.Init(channelConfig);
            // }
        }

        #endregion

    }
}
#endif
