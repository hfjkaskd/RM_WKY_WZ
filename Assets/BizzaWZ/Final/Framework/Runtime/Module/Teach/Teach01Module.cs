#if BIZZA_REAL_WITHDRAW
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>在原加载完成事件接入独立教学；同一游戏会话只启动一次。</summary>
public sealed class Teach01Module : BaseGameModule<Teach01Module>
{
    public bool TeachEnabled { get; private set; }
    public Teach01Flow Flow { get; private set; }
    bool loadingFinished, attempted, released;
    public bool IsTeaching => TeachEnabled && !released && AccountModule.Instance?.IsSuspended != true &&
        (Flow != null ? Flow.IsRunning && Flow.CurrentStage != Teach01Flow.Stage.RetryingReward : !attempted && SaveDataUtils.TeachData != null &&
         !SaveDataUtils.TeachData.IsCompleted(Teach01Flow.SaveKey));

    public override void InitGameModule()
    {
        TeachEnabled = GameExtensionRegistry.ShouldEnableTeach();
        if (TeachEnabled) BizzaEventSystem.On(EventDefine.Frame.LoadingStageComplete, OnLoadingFinished);
    }

    void OnLoadingFinished() { loadingFinished = true; TryStart(); }
    void Update() { if (loadingFinished && !attempted && !released) TryStart(); }

    public void OnFirstMatch()
    {
        if (Flow == null || !Flow.IsRunning || AccountModule.Instance?.IsSuspended == true) return;
        var data = SaveDataUtils.TeachData;
        if (data.newbieGiftRequest != NewbieGiftRequestState.NotTriggered) return;
        data.newbieGiftRequest = Teach01UnityServices.AccountReady
            ? NewbieGiftRequestState.NextLogin : NewbieGiftRequestState.Background;
        SaveDataUtils.Save();
    }

    void TryStart()
    {
        if (!TeachEnabled || attempted || released || SaveDataUtils.GameData == null ||
            SaveDataUtils.TeachData == null || AccountModule.Instance == null || UIModule.Instance == null || ChannelConfig.Instance == null) return;
        attempted = true;
        if (AccountModule.Instance.IsSuspended) return;
        var data = SaveDataUtils.TeachData;
        // A persisted pending claim must survive Os_Ncm becoming false and teaching being completed.
        if (data.newbieGiftRequest == NewbieGiftRequestState.NotTriggered &&
            (data.IsCompleted(Teach01Flow.SaveKey) || !IsNewUser())) return;
        if (data.newbieGiftRequest == NewbieGiftRequestState.Succeeded && data.newbieGiftShown) return;
        Run().Forget();
    }

    static bool IsNewUser()
    {
#if !BIZZA_HTTP_AD
        return false;
#else
        return AccountModule.Instance.Os_Current_Uso?.Os_Ncm ?? !SaveDataUtils.GameData.customTutorialEnd;
#endif
    }

    async UniTask Run()
    {
        try
        {
            Flow = new Teach01Flow(new Teach01UnityServices());
            await Flow.RunAsync(this.GetCancellationTokenOnDestroy());
            if (Flow.LastError != null) Debug.LogWarning($"Teach01 stopped at {Flow.CurrentStage}: {Flow.LastError}");
        }
        catch (System.Exception error) { Debug.LogException(error); }
    }

    public override void PreReleaseGameModule() => ReleaseGameModule();
    public override void OnGameQuit() => ReleaseGameModule();
    public override void ReleaseGameModule()
    {
        if (released) return;
        released = true;
        BizzaEventSystem.Off(EventDefine.Frame.LoadingStageComplete, OnLoadingFinished);
        Flow?.Dispose();
    }
}
#endif
