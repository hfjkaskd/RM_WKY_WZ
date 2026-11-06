#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza.Sdk
{
    public abstract class AttributionAdapterBase
{
    public abstract void Init(ChannelConfig channelConfig);

    public abstract void AttributeStart(string eventName);

    public abstract void AttributeAdShow(object param);
    }
}
#endif
