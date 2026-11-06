using UnityEngine;

namespace Whitebox.Foundation
{
    // PropellerPropellerCombo 0x1e2d62c / 0x1e30088 / 0x1e30334 / 0x1e2d7bc.
    // The original three pilots share a target finder but reserve separate hits.
    public sealed class OriginalPropellerPropellerCombo
    {
        private readonly OriginalPlayableBoard game;
        public OriginalPropellerPropellerCombo(OriginalPlayableBoard game) { this.game = game; }
        public void Explode(OriginalPropellerBoardItem to, OriginalPropellerBoardItem from, OriginalExplodeData data)
        {
            var empty = to.CurrentCell;
            var extra = game.CreateDetachedComboPropeller(from.ItemView.transform.localPosition);
            game.SpecialItemCreated(extra, null, 0, 0, true);
            float toAngle, fromAngle, extraAngle;
            float x = to.ItemView.transform.localPosition.x;
            if (x < 1f) { toAngle = 0f; fromAngle = 270f; extraAngle = 90f; }
            else if (x > 7f) { toAngle = 180f; fromAngle = 90f; extraAngle = 270f; }
            else
            {
                fromAngle = (Random.value * 360f) % 360f;
                toAngle = (fromAngle + 120f) % 360f;
                extraAngle = (toAngle + 120f) % 360f;
            }
            empty.FreezeFallForDuration(.15f);
            to.SetStrategy(new OriginalPropellerPairStrategy(game, toAngle, 0));
            to.Explode(data);
            from.SetStrategy(new OriginalPropellerPairStrategy(game, fromAngle, 1));
            from.Explode(data);
            extra.SetStrategy(new OriginalPropellerPairStrategy(game, extraAngle, 2));
            extra.Explode(data);
        }
    }

    // Native NoExplode / PropellerPropellerCombo / ExtraOne strategies. Only
    // the from model damages takeoff neighbors. The detached extra has no cell.
    internal sealed class OriginalPropellerPairStrategy : IOriginalGuidedExploderStrategy
    {
        private readonly IOriginalPropellerRuntimeServices services;
        private readonly OriginalDefaultPropellerStrategy normal;
        private readonly float startAngle;
        private readonly int role;
        public OriginalPropellerPairStrategy(IOriginalPropellerRuntimeServices services, float startAngle, int role)
        { this.services = services; this.startAngle = startAngle; this.role = role; normal = new OriginalDefaultPropellerStrategy(services, false); }
        public bool ExplodeNeighbors(OriginalGridCell cell, int trigger, OriginalGuidedExploderBoardItem model) =>
            role == 1 && normal.ExplodeNeighbors(cell, trigger, model);
        public OriginalExplodeData NextExploder(OriginalGridCell cell) => role == 2 ? OriginalExploder.Next(8) : normal.NextExploder(cell);
        public void OnTargetFound() { }
        public void OnTargetCancelled() { }
        public void OnTargetReached(OriginalGridCell cell, OriginalExplodeData data) => normal.OnTargetReached(cell, data);
        public void ForceExplode() { }
        public bool IsTargetAmountReached() => true;
        public bool IsTntOrRocketCombo => false;
        public IOriginalGuidedExploderPilot GetNewPilot() => new OriginalPropellerPairPilot(services, startAngle);
    }

    // PropellerPropellerComboPilot 0x1f34284..0x1f348b0: radial expansion
    // precedes the unchanged default homing flight; no substitute teleport.
    internal sealed class OriginalPropellerPairPilot : OriginalDefaultPropellerPilot
    {
        private readonly float startAngle;
        private float expandCompletedAt;
        private Vector3 expandTarget;
        public OriginalPropellerPairPilot(IOriginalPropellerPilotContext context, float startAngle) : base(context) { this.startAngle = startAngle; }
        protected override OriginalGuidedExploderFlyState GetStartState() => OriginalGuidedExploderFlyState.ComboExpand;
        public override void TargetChanged() { if (flyState != OriginalGuidedExploderFlyState.ComboExpand) base.TargetChanged(); }
        protected override void InitializeCurveVariables(Vector3 targetNormalized)
        {
            base.InitializeCurveVariables(targetNormalized);
            expandTarget = transform.position + Quaternion.AngleAxis(startAngle, Vector3.forward) * Vector3.right;
        }
        protected override void MoveToTarget()
        {
            if (flyState != OriginalGuidedExploderFlyState.ComboExpand) { base.MoveToTarget(); return; }
            Vector3 vector = expandTarget - transform.position;
            float distance = vector.magnitude;
            outSpeed = Lerp(startOutSpeed, maxOutSpeed, Mathf.Min(elapsed / .75f, 1f) / .7f) * 7f;
            combinedVelocity = vector.normalized * (outSpeed * context.LevelDeltaTime);
            transform.position += combinedVelocity;
            if (combinedVelocity.magnitude > distance || Approximately(combinedVelocity.magnitude, distance))
            {
                expandCompletedAt = context.LevelTime;
                flyState = OriginalGuidedExploderFlyState.Flying;
                combinedVelocity *= .6f;
            }
            ArrangeScale(distance);
        }
        protected override float CalculateAngleSmoothCoefficient() =>
            1f - Mathf.Clamp01(Mathf.Min(context.LevelTime - expandCompletedAt, context.LevelTime - moveCanceledAtTime) / .4f);
    }

    public sealed partial class OriginalPlayableBoard
    {
        public event System.Action<OriginalPropellerBoardItem> OnComboPropellerCreated;
        public OriginalPropellerBoardItem CreateDetachedComboPropeller(Vector3 position)
        {
            int id = OriginalBoardItem.StaticId; OriginalBoardItem.StaticId = unchecked(id + 1);
            var item = new OriginalPropellerBoardItem(id, () => board.Clock.Time, this);
            item.CreateView(board.Presentation, board.BoardRoot, position, this);
            item.GoalType = OriginalGoalTypes.FromItemType(item.ItemType);
            item.BindExplosion(this); item.BindTargetScoreContext(guidedTargetServices); item.BindGoalServices(board.ItemGoals);
            item.CreateFallComponent();
            ((OriginalFallComponentState)item.FallComponent).BindMotionServices(board.FallMotion);
            item.FallView = new OriginalSpecialItemFallView(item, ItemSortingLayer);
            board.LocalMoves.RegisterTarget(item.ItemView.transform);
            OnComboPropellerCreated?.Invoke(item);
            return item;
        }
    }
}
