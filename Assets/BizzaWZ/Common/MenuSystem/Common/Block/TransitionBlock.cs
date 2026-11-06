using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionBlock : BaseSingleton<TransitionBlock>
{
    public Animation animation;
    public string openAnim = "open";
    public string closeAnim = "close";
    public bool _playingAnim;
    public bool _isCloseAnim;


    public static void ToGamePlay(bool showTransition = false)
    {
        if (showTransition)
        {
            TransitionBlock.Instance.ShowTransition(() =>
            {
                World.Current.ChangeGameModeWithSameBattle(E_GameModeType.GamePlay);
            });
        }
        else
        {
            World.Current.ChangeGameModeWithSameBattle(E_GameModeType.GamePlay);
        }
    }

    public static void ToMainMenu(float progress, string reason)
    {
        LogLogger.LogInfo("ToMainMenu");
        SaveDataUtils.GameData.gameBoardData = "";
        // SaveDataUtils.gameStrategy.SaveData();

        StatisticModule.Instance.OnLevelFinish(SaveDataUtils.GameData.playerSelectedLv, progress, reason);

        TransitionBlock.Instance.ShowTransition(() =>
        {
            SaveDataUtils.GameData.gameBoardData = "";
            SaveDataUtils.gameStrategy.SaveData();
            World.Current.ChangeGameModeWithSameBattle(E_GameModeType.GamePlay);
        });
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.SetActive(false);
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        _loadFinish = true;
    }

    private bool _loadFinish = false;
    private bool _handleFinish = false;

    [Button]
    void Test()
    {
        ShowTransition(null);
    }

    public async void ShowTransition(Action action)
    {
        _loadFinish = false;
        gameObject.SetActive(true);
        await animation.PlayWithCallback(openAnim, null);
        action?.Invoke();
        // await UniTask.WaitUntil(() => _loadFinish);
        // await UniTask.DelayFrame(3);
        await animation.PlayWithCallback(closeAnim, null);
        gameObject.SetActive(false);
    }


    public async void Open(Action action)
    {
        _handleFinish = false;
        gameObject.SetActive(true);
        await animation.PlayWithCallback(openAnim, null);
        action?.Invoke();
        _handleFinish = true;
    }

    public async void Close()
    {
        await UniTask.WaitUntil(() => _handleFinish);
        await UniTask.DelayFrame(3);
        await animation.PlayWithCallback(closeAnim, null);
        gameObject.SetActive(false);
    }

}
