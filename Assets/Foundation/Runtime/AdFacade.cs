using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IRewardedAdProvider { void Show(string requestId, Action<AdOutcome> callback); }

    // New local test adapter. No vendor SDK, network request or real ad impression.
    public sealed class LocalAdProvider : IRewardedAdProvider
    {
        private readonly AdScenario scenario;
        public LocalAdProvider(AdScenario scenario) { this.scenario = scenario ?? throw new ArgumentNullException(nameof(scenario)); }
        public void Show(string requestId, Action<AdOutcome> callback) { callback(scenario.Outcome); }
    }

    public sealed class AdFacade
    {
        private readonly IRewardedAdProvider provider;
        private readonly int reward;
        private readonly HashSet<string> settled = new HashSet<string>();
        private readonly HashSet<string> pending = new HashSet<string>();
        public event Action<AdOutcome> Finished;
        public event Action<int> RewardGranted;
        public int EarnedRewards { get; private set; }

        public AdFacade(IRewardedAdProvider provider, int reward)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (reward < 0) throw new ArgumentOutOfRangeException(nameof(reward));
            this.reward = reward;
        }

        public bool Show(string requestId)
        {
            if (string.IsNullOrEmpty(requestId) || settled.Contains(requestId) || !pending.Add(requestId)) return false;
            provider.Show(requestId, outcome => Complete(requestId, outcome));
            return true;
        }

        private void Complete(string requestId, AdOutcome outcome)
        {
            if (!pending.Remove(requestId) || !settled.Add(requestId)) return;
            if (outcome == AdOutcome.Completed) { EarnedRewards += reward; RewardGranted?.Invoke(reward); }
            Finished?.Invoke(outcome);
        }
    }
}
