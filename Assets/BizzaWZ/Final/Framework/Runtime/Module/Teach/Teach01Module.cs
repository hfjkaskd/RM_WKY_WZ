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
    public bool IsTeaching => TeachEnabled && !released &&
        (Flow != null ? Flow.IsRunning : !attempted && SaveDataUtils.TeachData != null &&
         !SaveDataUtils.TeachData.IsCompleted(Teach01Flow.SaveKey));

    public override void InitGameModule()
    {
        TeachEnabled = GameExtensionRegistry.ShouldEnableTeach();
#if UNITY_EDITOR
        TeachEnabled &= UnityEditor.EditorPrefs.GetBool("Editor_TeachMode", true);
#endif
        if (TeachEnabled) BizzaEventSystem.On(EventDefine.Frame.LoadingStageComplete, OnLoadingFinished);
    }

    void OnLoadingFinished() { loadingFinished = true; TryStart(); }
    void Update() { if (loadingFinished && !attempted && !released) TryStart(); }

    void TryStart()
    {
        if (!TeachEnabled || attempted || released || SaveDataUtils.GameData == null ||
            AccountModule.Instance?.Os_Current_Uso == null || UIModule.Instance == null || ChannelConfig.Instance == null) return;
        attempted = true;
        if (SaveDataUtils.TeachData.IsCompleted(Teach01Flow.SaveKey) || !IsNewUser()) return;
        Run().Forget();
    }

    static bool IsNewUser()
    {
#if !BIZZA_HTTP_AD
        return false;
#elif !BIZZA_ENABLE_MAX
        return !SaveDataUtils.GameData.customTutorialEnd;
#elif UNITY_EDITOR
        return AccountModule.Instance.Os_Current_Uso.Os_Ncm || ChannelConfig.Instance.real_CustomConfig.enterNewbieGuide;
#else
        return AccountModule.Instance.Os_Current_Uso.Os_Ncm;
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
