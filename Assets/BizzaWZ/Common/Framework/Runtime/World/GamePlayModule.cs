using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GamePlayModule : BaseGameModule, IUpdate //, ILateUpdate
{
    public bool Enabled => enabled;
    public World World { get; private set; }

    #region Game Module Event
    public override void InitGameModule()
    {
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] GamePlayModule.InitGameModule begin config:{(m_gameModeConfig != null)} default:{m_defaultGameMode}");
#endif
        if (m_gameModeConfig)
        {
            m_gameModeConfig.Init();
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] GamePlayModule.GameModeConfig init count:{m_gameModeConfig.ConfigDic.Count}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("[WhiteBootstrap] GamePlayModule missing GameModeConfig");
#endif
        }
        // GameObject worldGo = new GameObject("World", typeof(World), typeof(RectTransform), typeof(Canvas));
        GameObject worldGo = new GameObject("World", typeof(World));
        World = worldGo.GetComponent<World>();
        DontDestroyOnLoad(worldGo);
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] GamePlayModule.InitGameModule end world:{(World != null)}");
#endif
    }

    public override void ReleaseGameModule()
    {
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] GamePlayModule.ReleaseGameModule world:{(World != null)}");
#endif
        if (World != null)
        {
            World.ClearActor();
        }
        World = null;
    }

    public void OnUpdate(float delta)
    {
        if (World == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[WhiteBootstrap] GamePlayModule.OnUpdate skip because World is null");
#endif
            return;
        }
        World.OnUpdate(delta);
    }

    // public void OnLateUpdate(float delta)
    // {
    //     World.OnLateUpdate(delta);
    // }

    public override void PreReleaseGameModule()
    {
        base.PreReleaseGameModule();
        //TODO 关闭游戏前处理存档
    }

    #endregion

    #region Scene Manage

    public void AddSubScene(string sceneAsset)
    {
        StartCoroutine(AddSubSceneCoroutine(sceneAsset));
    }

    public void RemoveSubScene(string sceneAsset)
    {
        StartCoroutine(RemoveSubSceneCoroutine(sceneAsset));
    }

    private IEnumerator AddSubSceneCoroutine(string sceneAsset)
    {
        var operation = SceneManager.LoadSceneAsync(sceneAsset, LoadSceneMode.Additive);
        yield return operation;
        World.AddSubSceneFinish();
    }

    private IEnumerator RemoveSubSceneCoroutine(string sceneAsset)
    {
        var operation = SceneManager.UnloadSceneAsync(sceneAsset);
        yield return operation;
        World.RemoveSubSceneFinish();
    }

    #endregion

    #region GameMode

    [SerializeField]
    private E_GameModeType m_defaultGameMode;
    [SerializeField]
    private GameModeConfig1 m_gameModeConfig;
    public GameModeConfig1 GameModeConfig => m_gameModeConfig;
    #endregion

    public void GameStart()
    {
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] GamePlayModule.GameStart default:{m_defaultGameMode} world:{(World != null)}");
#endif
        if (World == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[WhiteBootstrap] GamePlayModule.GameStart failed because World is null");
#endif
            return;
        }
        World.ChangeGameMode(m_defaultGameMode);
    }
}
