using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable] public sealed class OriginalGoalUiAsset
    {
        public int goalType;
        public string iconPath, shadowPath;
        public Vector2 shadowPosition, shadowScale, iconOffset;
        public float shadowAlpha, iconScale;
    }
    [Serializable] public struct OriginalGoalCheckStage { public float scale, duration; }
    [Serializable] public sealed class OriginalGoalLayout
    {
        public Vector3[] positions;
        public Vector2 countPosition;
        public Vector3 scale;
    }
    public sealed class OriginalGoalUiAssets : ScriptableObject
    {
        public OriginalGoalUiAsset[] goals;
        public OriginalGoalLayout[] layouts;
        public float singleCoinScale, initialCheckScale, checkDelay, initialCheckMultiplier;
        public int completeSound;
        public float soundOffset;
        public OriginalGoalCheckStage[] checkStages;
        public string prerequisiteText;
        public float rewardAppearDuration, rewardAppearOvershoot;
        public Vector3 flyingHitMultiplier, settledHitMultiplier;
        public float flyingHitDuration, flyingReturnDuration, settledHitDuration;
        public OriginalGoalUiAsset Find(int type)
        {
            for (int i = 0; i < goals.Length; i++) if (goals[i].goalType == type) return goals[i];
            throw new NotSupportedException("Goal view assets not restored for goal type " + type);
        }
    }
}
