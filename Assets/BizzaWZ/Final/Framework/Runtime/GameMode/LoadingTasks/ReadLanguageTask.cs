#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 如果没有存储语言，则通过 IP 自动识别
/// </summary>
public class ReadLanguageTask : LoadingTaskBase
{
    public override LoadingTaskName TaskName => LoadingTaskName.ReadLanguage;
    public override float Weight => 1.0f;
    public override LoadingTaskName[] Dependencies => Array.Empty<LoadingTaskName>();
    public override async UniTask Execute()
    {
        SetProgress(0.9f);
        GameInstance.Instance.StartCoroutine(LanguageUtils.InitLanguageFromIP());
        await UniTask.WaitUntil(() => !string.IsNullOrEmpty(LanguageUtils.SelectedLanguage));
        SetProgress(1f);
    }
}
#endif
