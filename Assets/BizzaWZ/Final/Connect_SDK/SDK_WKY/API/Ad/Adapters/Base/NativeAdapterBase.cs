#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using UnityEngine;

namespace Bizza.Sdk
{
    public class NativeAdapterBase : FixedAdAdapterBase
    {
        public override E_AdType adType => E_AdType.Native;
        public override string AdUnitId { get; }
        public override bool IsValid { get; }
        public override bool IsReady { get; }
        public override void Init(string adUnitId)
        {

        }

        public override void Load()
        {
        }

        public override bool IsShowing { get; }
        public override void Show()
        {
        }

        public override void Hide()
        {
        }
    }
}
#endif
