using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadGameResTask : LoadingTaskBase
{
    public override LoadingTaskName TaskName => LoadingTaskName.LoadGameRes;
    public override float Weight => 0.1f;
    public override LoadingTaskName[] Dependencies => Array.Empty<LoadingTaskName>(); // 没有依赖
    public override async UniTask Execute()
    {
        SetProgress(0.9f);
        BizzaEventSystem.Emit(EventDefine.Frame.DataTableLoaded);
    }
}
