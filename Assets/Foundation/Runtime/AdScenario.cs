using UnityEngine;

namespace Whitebox.Foundation
{
    public enum AdOutcome { Completed = 0, Cancelled = 1, Unavailable = 2 }

    [CreateAssetMenu(menuName = "Whitebox/Ad scenario")]
    public sealed class AdScenario : ScriptableObject
    {
        [SerializeField] private AdOutcome outcome = AdOutcome.Completed;
        [SerializeField, Min(0)] private int rewardAmount = 1;
        public AdOutcome Outcome => outcome;
        public int RewardAmount => rewardAmount;
        public void Configure(AdOutcome result, int reward) { outcome = result; rewardAmount = Mathf.Max(0, reward); }
    }
}
