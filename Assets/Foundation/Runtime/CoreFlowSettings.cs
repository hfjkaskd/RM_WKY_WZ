using UnityEngine;

namespace Whitebox.Foundation
{
    [System.Serializable]
    public struct OriginalLevelEntranceTiming
    {
        public float boardOffset, boardStart, boardDuration, boardOvershoot, boardSoundTime;
        public float hudDuration, hudOvershoot, topOutsideOffset, bottomOutsideOffset;
    }

    [CreateAssetMenu(menuName = "Whitebox/Core flow settings")]
    public sealed class CoreFlowSettings : ScriptableObject
    {
        public string progressFileName;
        public OriginalLevelEntranceTiming levelEntrance;
        public string modalTutorialPrefabPath;
        public string hammerTutorialPrefabPath;
        public string arrowTutorialPrefabPath;
        public string cannonTutorialPrefabPath;
        public string jesterTutorialPrefabPath;
        public OriginalModalTutorialProfile[] modalTutorials;
        public OriginalDirectedTutorialProfile[] directedTutorials;
        public string lightballTapTutorialPrefabPath;
        public string dynamicComboTutorialPrefabPath;
        public float lightballTapTutorialDelay;
        public string matchHintParticlesPrefabPath;
        public OriginalMatchHintTiming matchHintTiming;
        public string winLogoPrefabPath, remainingMovesPrefabPath, winDialogPrefabPath, failDialogPrefabPath, noPossibleMovesPrefabPath, backdropPrefabPath;
        public string outOfMovesBannerPrefabPath;
        [Min(0)] public int firstReviveMoves;
        public string levelQuitPrefabPath;
        public string levelTitleFormat, completedTitle, continueText, completedContinueText, retryText, failText, skipText;
        public float soundFadeDuration;
    }
}
