using Bizza.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PrewarmActionGraphTask : LoadingTaskBase
{
    public override LoadingTaskName TaskName => LoadingTaskName.PrewarmFinalFeatures;
    public override float Weight => 0.1f;
    public override LoadingTaskName[] Dependencies => new[] { LoadingTaskName.LoadTables };

    public override async UniTask Execute()
    {
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] PrewarmActionGraphTask.Execute begin");
#endif
        SetProgress(0.9f);
        await ActionGraphUtils.Prewarm();
        SetProgress(1f);
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] PrewarmActionGraphTask.Execute end");
#endif
    }
}
