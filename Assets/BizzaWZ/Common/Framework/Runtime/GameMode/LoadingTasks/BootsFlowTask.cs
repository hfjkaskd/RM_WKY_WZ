#if WKY_SDK || USER_VERIFICATION_SDK
using System;
using System.Collections.Generic;
using Bizza.GameAnalytics;
using Bizza.Loading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 框架内的统一启动任务。具体 SDK/授权阶段由 Boots_Flow 编排，
/// 本任务只负责接入 LoadingProcedure 的依赖、进度和加载埋点。
/// </summary>
public sealed class BootsFlowTask : LoadingTaskBase
{
    public override LoadingTaskName TaskName => LoadingTaskName.Boots;

    public override float Weight
    {
        get
        {
#if WKY_SDK && USER_VERIFICATION_SDK
            return 0.4f;
#elif USER_VERIFICATION_SDK
            return 0.3f;
#else
            return 0.1f;
#endif
        }
    }

    public override LoadingTaskName[] Dependencies => new[]
    {
        LoadingTaskName.LoadTables,
        LoadingTaskName.LoadGameData,
    };

    public override async UniTask Execute()
    {
        var observer = new LoadingObserver(SetProgress);
        await Boots_Flow.Init(observer);
        SetProgress(1f);
    }

    private sealed class LoadingObserver : IBootsFlowObserver
    {
        private readonly Action<float> setProgress;
        private readonly Dictionary<BootsFlowStage, string> stageIds = new();

        public LoadingObserver(Action<float> setProgress)
        {
            this.setProgress = setProgress;
        }

        public void OnBootsFlowEvent(BootsFlowEvent flowEvent)
        {
            setProgress(flowEvent.OverallProgress);

            switch (flowEvent.EventType)
            {
                case BootsFlowEventType.Started:
                    stageIds[flowEvent.Stage] =
                        BizzaGameAnalytics.TrackGameLoadStageStart(
                            GetAnalyticsStageName(flowEvent.Stage));
                    break;

                case BootsFlowEventType.Succeeded:
                    CompleteStage(flowEvent.Stage, true, null);
                    break;

                case BootsFlowEventType.Failed:
                    CompleteStage(
                        flowEvent.Stage,
                        false,
                        flowEvent.FailureReason ?? "BootsStageFailed");
                    break;
            }
        }

        private void CompleteStage(
            BootsFlowStage stage,
            bool success,
            string failureReason)
        {
            if (!stageIds.TryGetValue(stage, out string stageId) ||
                string.IsNullOrEmpty(stageId))
            {
                return;
            }

            BizzaGameAnalytics.TrackGameLoadStageComplete(
                stageId,
                success,
                failureReason);
            stageIds.Remove(stage);
        }

        private static string GetAnalyticsStageName(BootsFlowStage stage)
        {
            switch (stage)
            {
                case BootsFlowStage.WKYSDK:
                    return "Boots.WKY_SDK";
                case BootsFlowStage.UserVerification:
                    return "Boots.UserVerification";
                default:
                    return "Boots.Unknown";
            }
        }
    }
}
#endif
