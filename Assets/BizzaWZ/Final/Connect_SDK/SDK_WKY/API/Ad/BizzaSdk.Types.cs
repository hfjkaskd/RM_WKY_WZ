#if BIZZA_REAL_WITHDRAW
using System;

namespace Bizza.Sdk
{
    public struct CheckAdReadyArgs
    {
        public float ecpmLimit;
        public bool excludeReplenish;
    }

    public struct ShowAdArgs
    {
        public E_AdsSource adSource;
        public Action<ShowAdResult> onFinish;
        public Action onFailed;
        public float ecpmLimit;
        public string adPos;
        public AdType beAdType;
        public AdType activeAdType;

        public float fakeDollarNum;
    }

    public struct ShowAdResult
    {
        public bool success;

#if BIZZA_REAL_WITHDRAW
        public AccountModule.OceanShineAdRevenueResponse response;

        public ShowAdResult(bool success, AccountModule.OceanShineAdRevenueResponse response)
        {
            this.success = success;
            this.response = response;
        }

        public ShowAdResult Default()
        {
            return new ShowAdResult(false, null);
        }
#endif
    }

    [Obfuz.ObfuzIgnore]
    public enum AdType
    {
        Reward,
        Interstitial,
        Banner,
    }

    [Flags]
    [Obfuz.ObfuzIgnore]
    public enum E_AdsSource
    {
        All = 0,
        Max = 1 << 0,
        TopOn = 1 << 1,
    }
}
#endif
