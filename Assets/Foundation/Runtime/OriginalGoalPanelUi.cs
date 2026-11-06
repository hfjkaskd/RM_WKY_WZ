using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalGoalPanelUi : MonoBehaviour, IOriginalGoalCheckPanel
    {
        [SerializeField] private string goalPrefabPath;
        [SerializeField] private OriginalGoalUiAssets assets;
        private OriginalGoalManager goals;
        private OriginalSfxPlayback audio;
        private Action animateRewardTitle;
        private Action<float> updateRewardTitle;
        private bool appearing;
        private float appearElapsed;
        private Vector3 appearScale;
        public event Action OnViewsReplaced;
        private readonly List<OriginalGoalUi> views = new List<OriginalGoalUi>(4);
        public int LeftGoalCheckCount { get; private set; }
        public bool ShouldSpeedUpLevelEnd { get; private set; }
        public IReadOnlyList<OriginalGoalUi> Views => views;
        public Vector3 GetGoalPosition(int type) // 0x18cb97c: the goal view root, rather than its icon child.
        {
            int index=goals.GetGoalIndex(type);
            if(index>=0&&index<views.Count)return views[index].transform.position;
            Debug.LogError("Goal view position is missing: "+type);return transform.position;
        }
        public void Initialize(OriginalGoalManager goals, OriginalSfxPlayback audio, bool speedUp, Action animateRewardTitle, Action<float> updateRewardTitle)
        {
            this.goals = goals; this.audio = audio; this.animateRewardTitle = animateRewardTitle;
            this.updateRewardTitle = updateRewardTitle ?? throw new ArgumentNullException(nameof(updateRewardTitle));
            ShouldSpeedUpLevelEnd = speedUp;
            CreateViews(false);
            if (speedUp) LeftGoalCheckCount = views.Count;
            goals.BindGoalChecks(this);
            goals.OnGoalUpdatedUi += UpdateGoals;
        }
        private void CreateViews(bool reward)
        {
            var entries = goals.GetFinalGoals();
            if (entries.Length < 1 || entries.Length > assets.layouts.Length) throw new NotSupportedException("Goal layout not restored.");
            var layout = assets.layouts[entries.Length - 1];
            GameObject prefab = null;
            for (int i = 0; i < entries.Length; i++)
            {
                OriginalGoalUi view;
                if (i < views.Count) view = views[i];
                else
                {
                    if (prefab == null) prefab = Resources.Load<GameObject>(goalPrefabPath);
                    if (prefab == null) throw new InvalidOperationException("Original goal prefab missing.");
                    view = Instantiate(prefab, transform).GetComponent<OriginalGoalUi>();
                    views.Add(view);
                }
                view.Initialize(entries[i], assets.Find(entries[i].GoalType), goals, this, assets, audio);
                view.SetCountPosition(layout.countPosition);
                view.transform.localPosition = layout.positions[i];
                view.transform.localScale = reward ? Vector3.zero : layout.scale;
            }
            for (int i = views.Count - 1; i >= entries.Length; i--)
            {
                views[i].gameObject.SetActive(false);
                Destroy(views[i].gameObject);
                views.RemoveAt(i);
            }
            if (reward) { appearing = true; appearElapsed = 0f; appearScale = layout.scale; }
        }
        public void ReplaceGoalsWithCoin() // 0x18cbad4.
        {
            animateRewardTitle();
            for (int i = 0; i < views.Count; i++) Destroy(views[i].gameObject);
            views.Clear();
            CreateViews(true);
            // Native replacement deliberately preserves the completed check
            // count; reward coins never become another victory condition.
            goals.BindGoalChecks(this);
            OnViewsReplaced?.Invoke();
        }
        // GameUi.StartLevelWithoutAnimation 0x162c9c4 and GoalPanelUi 0x18cc064.
        internal void RestoreCompletedChecks()
        {
            var entries = goals.GetFinalGoals();
            for (int i = 0; i < entries.Length; i++)
            {
                var goal = entries[i];
                var view = views[i];
                if (!goal.IsGoalCompleted() || goals.HasPrerequisite(goal.GoalType) ||
                    view.checkImage.gameObject.activeSelf) continue;
                view.checkImage.gameObject.SetActive(true);
                view.countText.gameObject.SetActive(false);
                if (ShouldSpeedUpLevelEnd) DecreaseLeftGoalCheck();
            }
        }
        private void UpdateGoals(int type, int count, bool prerequisite, bool start, bool complete)
        { for (int i = 0; i < views.Count; i++) views[i].OnGoalUpdated(type, count, prerequisite, start, complete); }
        public void ManualUpdate(float delta)
        {
            updateRewardTitle?.Invoke(delta); // Created before the replacement goal tween in 0x18cbad4.
            if (appearing)
            {
                appearElapsed += delta; // TweenManager advances the current level clock while the game is active.
                float t = Mathf.Min(appearElapsed / assets.rewardAppearDuration, 1f);
                float s = RewardEase(t, assets.rewardAppearOvershoot);
                for (int i = 0; i < views.Count; i++) views[i].transform.localScale = appearScale * s;
                if (t >= 1f) appearing = false;
            }
            for (int i = 0; i < views.Count; i++) views[i].ManualUpdate(delta);
        }
        // DOTween OutBack(27), default overshoot. Parameters belong to the
        // recovered serialized assets, shared by the goal and title animation.
        public static float RewardEase(float t, float overshoot)
        { t -= 1f; return t * t * ((overshoot + 1f) * t + overshoot) + 1f; }
        public void DecreaseLeftGoalCheck() { LeftGoalCheckCount = unchecked(LeftGoalCheckCount - 1); } // 0x18cc160.
        public bool AreAllGoalChecksShown() => LeftGoalCheckCount == 0; // 0x18cc170.
        public void Release()
        { if (goals != null) { goals.OnGoalUpdatedUi -= UpdateGoals; goals.ReleaseGoalChecks(this); } goals = null; animateRewardTitle = null; updateRewardTitle = null; appearing = false; }
        private void OnDestroy() { Release(); }
    }
}
