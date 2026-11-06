using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;


  [Obfuz.ObfuzIgnore]
public enum E_GameModeType : int
{
    BattleTest = -1,
    None,
    Loading,
    MainMenu,
    GamePlay,
    BonusGame,
    Teach,
    Max,
}

[CreateAssetMenu(menuName = "BizzaGame/GameModeConfig")]
public class GameModeConfig1 : ScriptableObject
{
    [SerializeField]
    private List<GameModeConfigData> m_configs = new List<GameModeConfigData>();

    public List<GameModeConfigData> Configs => m_configs;
    public Dictionary<E_GameModeType, GameModeConfigData> ConfigDic { get; private set; }

    public static GameModeConfig1 Instance { get; private set; }

    public void Init()
    {
        InitConfigDic();
        Instance = this;
    }

    private void InitConfigDic()
    {
        if (ConfigDic == null)
            ConfigDic = new Dictionary<E_GameModeType, GameModeConfigData>();
        ConfigDic.Clear();
        int infoLength = Configs.Count;
        for (int i = 0; i < infoLength; i++)
        {
            ConfigDic.Add(Configs[i].gameModeType, Configs[i]);
        }
    }
}

[System.Serializable]
public class GameModeConfigData
{
    [ReadOnly]
    public E_GameModeType gameModeType; 
    // [ReadOnly]
    // public AssetReferenceGameObject gameModeReference;
    public GameModeBase gameModePrefab;
}
