using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum BootsFlowStage
{
    WKYSDK,
    UserVerification,
}

public enum BootsFlowEventType
{
    Started,
    Progress,
    Succeeded,
    Failed,
}

public enum BootsFlowState
{
    NotStarted,
    Running,
    Succeeded,
    Failed,
}

/// <summary>
/// Boots_Flow 的通用阶段通知。外部项目可以忽略，框架项目可将其接到加载进度和埋点。
/// </summary>
public sealed class BootsFlowEvent
{
    public BootsFlowStage Stage { get; }
    public BootsFlowEventType EventType { get; }
    public float StageProgress { get; }
    public float OverallProgress { get; }
    public string FailureReason { get; }

    public BootsFlowEvent(
        BootsFlowStage stage,
        BootsFlowEventType eventType,
        float stageProgress,
        float overallProgress,
        string failureReason = null)
    {
        Stage = stage;
        EventType = eventType;
        StageProgress = Mathf.Clamp01(stageProgress);
        OverallProgress = Mathf.Clamp01(overallProgress);
        FailureReason = failureReason;
    }
}

public interface IBootsFlowObserver
{
    void OnBootsFlowEvent(BootsFlowEvent flowEvent);
}

/// <summary>
/// SDK 与用户授权的统一启动入口，可供当前框架和外部项目共同使用。
/// </summary>
public static class Boots_Flow
{
    private const float WKYSDKWeight = 1f;
    private const float UserVerificationWeight = 3f;

    private static readonly List<IBootsFlowObserver> Observers = new();
    private static UniTaskCompletionSource initializationCompletionSource;
    private static BootsFlowState state = BootsFlowState.NotStarted;
    private static Exception lastFailure;

    public static BootsFlowState State => state;
    public static Exception LastFailure => lastFailure;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Observers.Clear();
        initializationCompletionSource = null;
        state = BootsFlowState.NotStarted;
        lastFailure = null;
    }

    /// <summary>
    /// 初始化只会真正执行一次。并发调用会等待同一个任务，成功后的调用会直接完成，
    /// 失败后的调用会得到第一次初始化的原始异常。
    /// </summary>
    public static UniTask Init(
        IBootsFlowObserver observer = null,
        CancellationToken cancellationToken = default)
    {
        if (state == BootsFlowState.NotStarted)
        {
            AddObserver(observer);
            state = BootsFlowState.Running;
            initializationCompletionSource = new UniTaskCompletionSource();
            RunInitializationAsync().Forget();
        }
        else if (state == BootsFlowState.Running)
        {
            AddObserver(observer);
        }

        UniTask task = initializationCompletionSource == null
            ? UniTask.CompletedTask
            : initializationCompletionSource.Task;

        return cancellationToken.CanBeCanceled
            ? task.AttachExternalCancellation(cancellationToken)
            : task;
    }

    private static void AddObserver(IBootsFlowObserver observer)
    {
        if (observer != null && !Observers.Contains(observer))
        {
            Observers.Add(observer);
        }
    }

    private static async UniTask RunInitializationAsync()
    {
        try
        {
            Bizza.Unity.Android.AndroidJavaMessageDispatcher.EnsureCreated();

#if WKY_SDK
            GameObject dispatcherObject = GameObject.Find(
                nameof(Bizza.Unity.Android.AndroidJavaMessageDispatcher));
            if (dispatcherObject != null &&
                dispatcherObject.GetComponent<TipNativeBridge>() == null)
            {
                dispatcherObject.AddComponent<TipNativeBridge>();
            }
#endif

            float totalWeight = GetTotalWeight();
            float completedWeight = 0f;

#if WKY_SDK
            await ExecuteStage(
                BootsFlowStage.WKYSDK,
                WKYSDKWeight,
                totalWeight,
                completedWeight,
                progress => new WKY_Flow().Start_Init_Flow(progress));
            completedWeight += WKYSDKWeight;
#endif

#if USER_VERIFICATION_SDK
            await ExecuteStage(
                BootsFlowStage.UserVerification,
                UserVerificationWeight,
                totalWeight,
                completedWeight,
                progress => new Bizza.TokenClientSystem.AuthFlow().Init(progress));
#endif

            state = BootsFlowState.Succeeded;
            initializationCompletionSource.TrySetResult();
        }
        catch (Exception exception)
        {
            lastFailure = exception;
            state = BootsFlowState.Failed;
            initializationCompletionSource.TrySetException(exception);
        }
        finally
        {
            Observers.Clear();
        }
    }

    private static async UniTask ExecuteStage(
        BootsFlowStage stage,
        float stageWeight,
        float totalWeight,
        float completedWeight,
        Func<Action<float>, UniTask> execute)
    {
        float lastStageProgress = 0f;
        Notify(stage, BootsFlowEventType.Started, 0f,
            CalculateOverallProgress(completedWeight, stageWeight, totalWeight, 0f));

        try
        {
            await execute(progress =>
            {
                lastStageProgress = Mathf.Clamp01(progress);
                Notify(stage, BootsFlowEventType.Progress, lastStageProgress,
                    CalculateOverallProgress(
                        completedWeight,
                        stageWeight,
                        totalWeight,
                        lastStageProgress));
            });

            Notify(stage, BootsFlowEventType.Succeeded, 1f,
                CalculateOverallProgress(completedWeight, stageWeight, totalWeight, 1f));
        }
        catch (Exception exception)
        {
            Notify(stage, BootsFlowEventType.Failed, lastStageProgress,
                CalculateOverallProgress(
                    completedWeight,
                    stageWeight,
                    totalWeight,
                    lastStageProgress),
                GetFailureReason(stage, exception));
            throw;
        }
    }

    private static float GetTotalWeight()
    {
        float totalWeight = 0f;
#if WKY_SDK
        totalWeight += WKYSDKWeight;
#endif
#if USER_VERIFICATION_SDK
        totalWeight += UserVerificationWeight;
#endif
        return Mathf.Max(1f, totalWeight);
    }

    private static float CalculateOverallProgress(
        float completedWeight,
        float stageWeight,
        float totalWeight,
        float stageProgress)
    {
        return (completedWeight + stageWeight * Mathf.Clamp01(stageProgress)) /
               Mathf.Max(1f, totalWeight);
    }

    private static string GetFailureReason(BootsFlowStage stage, Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return "BootsCancelled";
        }

#if USER_VERIFICATION_SDK
        if (exception is Bizza.TokenClientSystem.TokenClientBlockedException)
        {
            return "UserVerificationBlocked";
        }
#endif

#if WKY_SDK
        if (exception is WKYInitializationException wkyException)
        {
            return "WKYSDK." + wkyException.ErrorCode;
        }
#endif

        return stage + "." + exception.GetType().Name;
    }

    private static void Notify(
        BootsFlowStage stage,
        BootsFlowEventType eventType,
        float stageProgress,
        float overallProgress,
        string failureReason = null)
    {
        if (Observers.Count == 0)
        {
            return;
        }

        var flowEvent = new BootsFlowEvent(
            stage,
            eventType,
            stageProgress,
            overallProgress,
            failureReason);

        for (int i = 0; i < Observers.Count; i++)
        {
            try
            {
                Observers[i].OnBootsFlowEvent(flowEvent);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Boots_Flow observer failed: {exception.Message}");
            }
        }
    }
}
