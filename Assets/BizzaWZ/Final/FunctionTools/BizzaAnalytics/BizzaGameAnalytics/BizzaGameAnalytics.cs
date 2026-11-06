using System;
using UnityEngine;

namespace Bizza.GameAnalytics
{
  public enum BizzaAdType
  {
    Rewarded = 0,
    Interstitial = 1,
    Banner = 2,
    AppOpen = 3,
    Other = 4,
  }

  public enum BizzaAdLoadResult
  {
    Success = 0,
    Failed = 1,
  }

  internal sealed class BizzaGameAnalyticsOptions
  {
    public bool EnablePerformanceTracking = true;
    public float PerformanceSampleIntervalSeconds = 60f;
    public float SlowFrameThresholdSeconds = 0.05f;
    public bool EnableAutomaticExceptionTracking = true;
    public int MaxAutomaticExceptionsPerSession = 3;
  }

  /// <summary>
  /// 网赚游戏业务打点入口。所有数据最终只通过 BizzaAnalyticsAgent 上报。
  /// 请在 Unity 主线程调用。
  /// </summary>
  public static class BizzaGameAnalytics
  {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStartTracking()
    {
      BizzaGameAnalyticsRuntime.EnsureInstance().BeginTracking();
    }

    /// <summary>
    /// 在一个真实加载阶段开始时调用，返回用于完成事件配对的 stageId。
    /// 支持并行阶段和同名阶段重复执行。
    /// </summary>
    public static string TrackGameLoadStageStart(string stageName)
    {
      EnsureTrackingRuntime();
      return global::BizzaAnalyticsAgent.MarkGameLoadStageStart(stageName);
    }

    /// <summary>
    /// 在对应加载阶段结束时调用。失败时传稳定错误码或可聚合原因。
    /// </summary>
    public static bool TrackGameLoadStageComplete(
      string stageId,
      bool success = true,
      string failureReason = null)
    {
      EnsureTrackingRuntime();
      return global::BizzaAnalyticsAgent.MarkGameLoadStageComplete(
        stageId,
        success,
        failureReason);
    }

    /// <summary>
    /// 在所有游戏加载流程结束、玩家可以进入游戏时调用一次。
    /// SDK 会自动关联启动最早期产生的 game_start。
    /// </summary>
    public static bool TrackGameLoadComplete()
    {
      EnsureTrackingRuntime();
      return global::BizzaAnalyticsAgent.MarkGameLoadComplete();
    }

    /// <summary>
    /// 更新公共经济参数。balanceUnit 是余额和提现阈值的单位，例如 USD、CENT 或 COIN。
    /// </summary>
    public static bool SetEconomyContext(
      double currentBalance,
      double firstWithdrawalThreshold,
      int withdrawalStage,
      string balanceUnit)
    {
      return EnsureTrackingRuntime().SetEconomyContext(
        currentBalance,
        firstWithdrawalThreshold,
        withdrawalStage,
        balanceUnit);
    }

    public static void SetAttribution(string network, string campaign, string country = null)
    {
      EnsureTrackingRuntime().SetAttribution(network, campaign, country);
    }

    public static void SetUserGroup(string userGroup)
    {
      EnsureTrackingRuntime().SetUserGroup(userGroup);
    }

    public static bool TrackActivation()
    {
      return EnsureTrackingRuntime().TrackActivation();
    }

    public static void TrackTutorialComplete(string tutorialId = "main")
    {
      EnsureTrackingRuntime().TrackTutorialComplete(tutorialId);
    }

    public static void TrackPageOpen(string pageName)
    {
      EnsureTrackingRuntime().TrackPageOpen(pageName);
    }

    public static void TrackPageClose(string pageName)
    {
      EnsureTrackingRuntime().TrackPageClose(pageName);
    }

    public static void TrackItemUse(
      string itemId,
      string itemType,
      int quantity = 1,
      string source = null)
    {
      EnsureTrackingRuntime().TrackItemUse(
        itemId,
        itemType,
        quantity,
        source);
    }

    /// <summary>
    /// 开始一次关卡尝试，成功时返回唯一 attemptId；已有未结束关卡时返回空字符串。
    /// </summary>
    public static string TrackLevelStart(string levelId, int attempt = 1)
    {
      return EnsureTrackingRuntime().TrackLevelStart(levelId, attempt);
    }

    public static bool TrackLevelWin(string levelId, string attemptId = null)
    {
      return EnsureTrackingRuntime().TrackLevelEnd(
        levelId,
        attemptId,
        "win",
        null);
    }

    public static bool TrackLevelFail(
      string levelId,
      string attemptId = null,
      string reason = null)
    {
      return EnsureTrackingRuntime().TrackLevelEnd(
        levelId,
        attemptId,
        "fail",
        reason);
    }

    public static bool TrackLevelQuit(
      string levelId,
      string attemptId = null,
      string reason = null)
    {
      return EnsureTrackingRuntime().TrackLevelEnd(
        levelId,
        attemptId,
        "quit",
        reason);
    }

    public static void TrackAdLoad(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      BizzaAdLoadResult result,
      long latencyMilliseconds = 0,
      string errorCode = null)
    {
      EnsureTrackingRuntime().TrackAdLoad(
        placement,
        adType,
        adNetwork,
        result,
        latencyMilliseconds,
        errorCode);
    }

    /// <summary>
    /// 每次业务尝试播放广告时调用，无论广告是否 Ready。
    /// </summary>
    public static void TrackAdShowRequest(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      bool isReady)
    {
      EnsureTrackingRuntime().TrackAdShowRequest(
        placement,
        adType,
        adNetwork,
        isReady);
    }

    /// <summary>
    /// 广告 SDK 确认开始播放时调用。
    /// </summary>
    public static void TrackAdShow(string placement, BizzaAdType adType, string adNetwork)
    {
      EnsureTrackingRuntime().TrackAdShow(placement, adType, adNetwork);
    }

    /// <summary>
    /// 广告 SDK 的 impression/display 回调触发时调用。impressionId 必须稳定且唯一。
    /// </summary>
    public static bool TrackAdImpression(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      string impressionId)
    {
      return EnsureTrackingRuntime().TrackAdImpression(
        placement,
        adType,
        adNetwork,
        impressionId);
    }

    /// <summary>
    /// 广告 SDK 的收益回调触发时调用；currency 必填，impressionId 在广告 SDK
    /// 能提供稳定唯一值时传入，否则可以为空。revenue 必须使用 currency 对应的主货币单位。
    /// </summary>
    public static bool TrackAdRevenue(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      double revenue,
      string currency,
      string impressionId = null)
    {
      return EnsureTrackingRuntime().TrackAdRevenue(
        placement,
        adType,
        adNetwork,
        revenue,
        currency,
        impressionId);
    }

    public static bool TrackWithdrawalStageEnter(int stage, double stageThresholdAmount)
    {
      return EnsureTrackingRuntime().TrackWithdrawalStageEnter(
        stage,
        stageThresholdAmount);
    }

    public static bool TrackWithdrawalRequest(
      string requestId,
      double amount,
      string currency)
    {
      return EnsureTrackingRuntime().TrackWithdrawalRequest(
        requestId,
        amount,
        currency);
    }

    public static bool TrackWithdrawalResult(
      string requestId,
      double amount,
      string currency,
      bool success,
      string failureReason = null)
    {
      return EnsureTrackingRuntime().TrackWithdrawalResult(
        requestId,
        amount,
        currency,
        success,
        failureReason);
    }

    /// <summary>
    /// 主动上报已捕获的异常。原生崩溃需要平台原生崩溃 SDK，无法由 Unity 退出回调保证送达。
    /// </summary>
    public static void TrackException(Exception exception, bool isFatal = false)
    {
      if (exception == null)
      {
        return;
      }

      EnsureTrackingRuntime().TrackException(
        exception.Message,
        exception.StackTrace,
        exception.GetType().FullName,
        isFatal);
    }

    private static BizzaGameAnalyticsRuntime EnsureTrackingRuntime()
    {
      BizzaGameAnalyticsRuntime runtime = BizzaGameAnalyticsRuntime.EnsureInstance();
      runtime.BeginTracking();
      return runtime;
    }
  }
}
