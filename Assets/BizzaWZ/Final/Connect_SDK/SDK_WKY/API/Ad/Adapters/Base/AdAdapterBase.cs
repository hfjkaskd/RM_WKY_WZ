#if BIZZA_REAL_WITHDRAW
using System;

namespace Bizza.Sdk
{
    //视频类广告基类：插屏，激励，开屏等
    public abstract class VideoAdAdapterBase : AllAdAdapterBase
    {
        public abstract double ECPM { get; }
        public abstract ShowAdArgs ShowAdArg { get;  set;}
        /// <summary>
        /// args 中的参数不能使用回调
        /// </summary>
        /// <param name="success"></param>
        /// <param name="fail"></param>
        /// <param name="args"></param>
        public abstract void ShowAds(Action onSuccess, Action onFailed, ShowAdArgs args);

        protected virtual double IncomeRate => ChannelConfig.Instance.incomeRate;
    }
}
#endif
