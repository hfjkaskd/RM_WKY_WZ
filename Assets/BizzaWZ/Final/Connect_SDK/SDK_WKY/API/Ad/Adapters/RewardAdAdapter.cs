#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Bizza.Sdk
{
    public abstract class RewardAdAdapter :
#if BIZZA_HTTP_AD && BIZZA_ENABLE_MAX
        HttpAdAdapterBase
#elif !BIZZA_HTTP_AD && BIZZA_ENABLE_MAX
        LocalAdAdapterBase
#elif BIZZA_REAL_WITHDRAW
        VideoAdAdapterBase
#endif
    {
        protected Action onReward = null;
        protected Action onShowFail = null;
        public Action onLoadFail = null;
        public Action onShowFinish = null;

        // public abstract double ECPM { get; }
        //
        // public abstract string Name { get; }
        //
        // public abstract string AdUnitId { get; }
        //
        // public abstract E_AdPos pos { get; }

        protected virtual void ShowRewardAdFinish()
        {
            // onReward = null;
            // onShowFail = null;
            onShowFinish?.Invoke();
        }

        protected virtual void ShowRewardAdFail()
        {
            onShowFail?.Invoke();
            onReward = null;
            onShowFail = null;
        }

        protected virtual void LoadRewardAdFail()
        {
            Debug.Log("LoadRewardAdFail 加载失败");
            onLoadFail?.Invoke();
            ShowRewardAdFail();
            ShowRewardAdFinish();
        }

        protected virtual void OnReward()
        {
            onReward?.Invoke();
            
            onReward = null;
            onShowFail = null;
        }
    }
}
#endif
