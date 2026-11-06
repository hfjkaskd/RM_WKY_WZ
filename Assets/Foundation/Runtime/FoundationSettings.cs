using UnityEngine;

namespace Whitebox.Foundation
{
    [CreateAssetMenu(menuName = "Whitebox/Foundation settings")]
    public sealed class FoundationSettings : ScriptableObject
    {
        [SerializeField] private CoreLevelCatalog coreLevels;
        [SerializeField] private int sessionStartCoin, sessionStartStar;
        [SerializeField] private string levelResourcePath = "Levels/level_1";
        [SerializeField] private string visualPrefabResourcePath = "Recovered/GameObject/BoxItemPrefab";
        [SerializeField] private Vector3 previewPosition = Vector3.zero;
        [SerializeField] private int playableLevelNumber = 1;
        [SerializeField] private string playableLevelName = "00001v02_L";
        [SerializeField] private int localDifficultySeed;
        [SerializeField] private float swapDuration = .135f;
        [SerializeField] private float swapSoundDelay = .04f;
        [SerializeField] private int targetFrameRate, vSyncCount;
        [SerializeField] private int[] winLowMatchRatios;
        [SerializeField] private float winLogoBeforeShowDelay = .1f, winLogoBeforePlayDelay = .2f;
        public string LevelResourcePath => levelResourcePath;
        public CoreLevelCatalog CoreLevels => coreLevels != null ? coreLevels : throw new System.InvalidOperationException("Core level catalog is missing.");
        public OriginalLevelStartInventory CreateCoreInventory() => new OriginalLevelStartInventory { Coin = sessionStartCoin, Star = sessionStartStar };
        public string VisualPrefabResourcePath => visualPrefabResourcePath;
        public Vector3 PreviewPosition => previewPosition;
        public int PlayableLevelNumber => playableLevelNumber;
        public string PlayableLevelName => playableLevelName;
        public int LocalDifficultySeed => localDifficultySeed;
        public float SwapDuration => swapDuration;
        public float SwapSoundDelay => swapSoundDelay;
        public int TargetFrameRate => targetFrameRate;
        public int VSyncCount => vSyncCount;
        public float WinLogoBeforeShowDelay => winLogoBeforeShowDelay;
        public float WinLogoBeforePlayDelay => winLogoBeforePlayDelay;
        public int[] CreateWinLowMatchRatios() => (int[])winLowMatchRatios.Clone();
    }
}
