using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalBushVisualAssets : ScriptableObject
    {
        [Serializable]
        public sealed class SpreadMotion
        {
            public AnimationCurve curve;
            public float duration, animationSpeed;
        }

        [SerializeField] private string[] damageStates, tapStates;
        [SerializeField] private SpreadMotion[] grassSpreadAnimations;
        [SerializeField] private float[] nearDelays, edgeDelays;
        [SerializeField] internal float initialDelay, landingHideDistance, nearbyScale, edgeScale, completeTime;
        [SerializeField] internal string innerSpreadState, edgeSpreadState;

        internal string DamageState(int layer) => damageStates[layer - 1];
        internal string TapState(int layer) => tapStates[layer - 1];

        internal SpreadMotion GetMotion(int group, int x, int y) // 0x2bc4f90.
        {
            if (group == 0) return grassSpreadAnimations[0];
            if (group == 1 && ((x == 2 && y == 0) || (x == 3 && y == 2))) return grassSpreadAnimations[2];
            return grassSpreadAnimations[1];
        }

        internal float GetDelay(int group, int x, int y) // 0x2bc5004.
        {
            int index = 4 * x + y;
            return initialDelay + (group == 0 ? 0f : group == 1 ? nearDelays[index] : edgeDelays[index]);
        }
    }
}
