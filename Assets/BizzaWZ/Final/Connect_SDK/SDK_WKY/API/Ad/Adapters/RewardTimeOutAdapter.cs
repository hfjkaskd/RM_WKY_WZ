#if BIZZA_REAL_WITHDRAW
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bizza.Sdk
{
    public interface ITimer
    {
        void TimeOut(float timeOut, Action callback);
        void Stop();
    }
}

public class UniTaskTimer : Bizza.Sdk.ITimer
{
    private CancellationTokenSource cancellationTokenSource;
    private Action callback;

    public void TimeOut(float timeOut, Action callback)
    {
        if (cancellationTokenSource != null)
        {
            return;
        }

        this.callback = callback;
        cancellationTokenSource = new CancellationTokenSource();
        RunTimerAsync(timeOut, cancellationTokenSource).Forget();
    }

    public void Stop()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource = null;
            callback = null;
        }
    }

    private async UniTask RunTimerAsync(float timeOut, CancellationTokenSource source)
    {
        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(Mathf.Max(0f, timeOut)),
                ignoreTimeScale: true,
                cancellationToken: source.Token,
                cancelImmediately: true);

            if (!source.IsCancellationRequested)
            {
                callback?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            // Stop() 主动取消计时，不执行超时回调。
        }
        finally
        {
            if (ReferenceEquals(cancellationTokenSource, source))
            {
                cancellationTokenSource = null;
                callback = null;
            }

            source.Dispose();
        }
    }
}

namespace Bizza.Sdk
{
    public abstract class RewardTimeOutAdapter : RewardAdAdapter
    {
        protected ShowAdArgs showAdArgs;
        protected ITimer timer = new UniTaskTimer();
        public float maxWaitTime = 10;
        protected override void ShowRewardAdFinish()
        {
            Debug.Log("ShowRewardAdFinish");
            // onReward = null;
            // onShowFail = null;
            onShowFinish?.Invoke();
            timer.Stop();
            ProtectedDestroyRewardAd();
        }

        protected abstract void ProtectedShowRewardAds();
        protected abstract void ProtectedDestroyRewardAd();

        public override void ShowAds(Action onSuccess, Action onFailed, ShowAdArgs args)
        {
            LogLogger.LogAdInfo($"进入到MAX 的激励播放器中： ");
            onReward = onSuccess;
            onShowFail = onFailed;
            showAdArgs = args;
#if BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_MAX && BIZZA_HTTP_AD
            var id = _cachedBatchId;
            if (args.fakeDollarNum > 0)
            {
                LogLogger.LogAdInfo($"入队钞票： {args.fakeDollarNum}");
                CurrencyBar.OnEnQueue(id, args.fakeDollarNum);
            }
#endif

            //请求展示广告 
            bool isShowSuccess = IsReady;
            LogLogger.LogAdInfo($"Max激励广告 {isShowSuccess}");
            if (!isShowSuccess)
            {
                timer.TimeOut(maxWaitTime, () => { LoadRewardAdFail(); });
            }
            else
            {
                ProtectedShowRewardAds();
            }
        }
    }

    public class DebugRewardAdAdapter : RewardTimeOutAdapter
    {
        public override double ECPM => 0;

        public override E_AdType adType => E_AdType.RewardAd;
        public override string AdUnitId => "DebugRewardAdAdapter";
        public override bool IsValid { get; }
        public override bool IsReady => true;
        protected ShowAdArgs showAdArgs;
        public override ShowAdArgs ShowAdArg { get => showAdArgs; set { showAdArgs = value; } }

        public override void Init(string adUnitId)
        {

        }

        public override void Load()
        {
        }

        protected override void ProtectedShowRewardAds()
        {
            const float rate = 100;
            const float delay = 2f;
            bool success = UnityEngine.Random.Range(0, 100) < rate;
            RunFakeRewardAsync(delay, success).Forget();

            async UniTask RunFakeRewardAsync(float waitTime, bool rewardSuccess)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(waitTime),
                    ignoreTimeScale: true);

                if (rewardSuccess)
                {
                    Success();
                }
                else
                {
                    Fail();
                }
            }

            void Success()
            {
                OnReward();
                ShowRewardAdFinish();
            }

            void Fail()
            {
                ShowRewardAdFail();
                ShowRewardAdFinish();
            }
        }

        protected override void ProtectedDestroyRewardAd()
        {
            // not need
        }
    }
}
#endif
