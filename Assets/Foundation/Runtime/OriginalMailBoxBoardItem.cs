using UnityEngine;

namespace Whitebox.Foundation
{
    // MailBoxItemModel 0x24ad26c. Collecting a letter leaves the obstacle in its cell.
    public sealed class OriginalMailBoxBoardItem : OriginalBoardItem
    {
        public override bool CanReceiveGrass => false; // 0x24adc7c.
        private OriginalPlayableBoard game;
        private OriginalGoalManager goals;
        public OriginalMailBoxItemView ItemView { get; private set; }
        public bool IsActive { get; private set; } = true;
        public override bool CanExplodeWithNearMatch => true;
        public override bool IsFallable => false;
        public override bool IsSwappable => false;

        internal OriginalMailBoxBoardItem(OriginalItemSettings settings) : base(settings, 1) { }

        internal void BindView(OriginalMailBoxItemView view, OriginalPlayableBoard owner, OriginalGoalManager goalManager)
        {
            ItemView = view;
            game = owner;
            goals = goalManager;
            IsActive = goals.GetGoalLeftCount(GoalType) > 0;
            BindSwapView(view);
            MarkViewCreated();
            if (IsActive) goals.OnGoalUpdated += OnGoalUpdated;
        }

        internal void Unbind() => goals.OnGoalUpdated -= OnGoalUpdated;

        public override bool IsValidExplodeTarget(int guidedType) => IsActive;
        public override int RemainingExplodeCount() => IsActive ? goals.GetGoalLeftCount(GoalType) - IncomingExploderCount : 0;
        public override int GetExplodeScore(int guidedType) => IsActive ? (base.GetExplodeScore(guidedType) / 100) * 75 : 0;
        public override Vector3 GetViewPosition() => ItemView.transform.position;

        protected override void TryExplode(OriginalExplodeData data) // 0x24ad7c0.
        {
            if (goals.GetGoalUiLeftCount(GoalType) < 1 || !ItemView.CanCollect) return;
            ItemView.PlayCollect();
            game.StartMailCollection(this);
            game.UsefulMoveMade();
            CollectAsGoal(false);
        }

        private void OnGoalUpdated(int goal, int left) // 0x24ada94.
        {
            if (goal != GoalType || left > 0) return;
            IsActive = false;
            ItemView.Close();
            Unbind();
        }

        internal void LetterArrived() => GoalServices.CollectAsGoalUi(this);

        public override bool PlayTouchDownAnimation()
        {
            ItemView.PlayTap(IsActive);
            return true;
        }
    }
}
