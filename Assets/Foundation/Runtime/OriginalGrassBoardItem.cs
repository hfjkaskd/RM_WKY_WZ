using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Native GrassItemModel/StaticItemModel. This is a cell static target,
    // never a falling ItemModel and never a UI object.
    public sealed class OriginalGrassBoardItem : IOriginalStaticExplosionItem, IOriginalExplodeTarget, IOriginalStaticPressItem
    {
        private readonly OriginalGoalManager goals;
        private readonly OriginalInitialItemAmounts amounts;
        private readonly IOriginalBoxExplosionServices useful;
        private readonly OriginalIncomingExploderContainer incoming = new OriginalIncomingExploderContainer();
        private readonly OriginalExtraIncomingExploderContainer extra = new OriginalExtraIncomingExploderContainer();
        private int layer;
        public int Id { get; }
        public int TiledId { get; }
        public int StaticItemType => 2;
        public int GoalType => 13;
        public int LayerCount => layer;
        public int IncomingExploderCount => incoming.Count;
        public OriginalGridCell CurrentCell { get; set; }
        public OriginalGrassItemView ItemView { get; private set; }
        public bool HasView { get; private set; }
        public bool IsTouchBlocking => false;
        public bool IsTouchBlockingExceptChain => false;
        public bool DoesBlockFall => false;
        public bool IsObstacle => false; // StaticItemModel 0x2b5290c.
        public bool CanExplodeWithMatch => true;
        public bool CanExplodeWithNearMatch => true;

        public OriginalGrassBoardItem(int tiledId, OriginalGoalManager goals, OriginalInitialItemAmounts amounts,
            IOriginalBoxExplosionServices useful, int restoredLayer = 0)
        {
            if (tiledId != 31 && tiledId != 32) throw new ArgumentOutOfRangeException(nameof(tiledId));
            this.goals = goals;
            this.amounts = amounts;
            this.useful = useful;
            TiledId = tiledId;
            layer = restoredLayer == 0 ? tiledId - 30 : restoredLayer;
            Id = OriginalBoardItem.StaticId;
            OriginalBoardItem.StaticId = unchecked(OriginalBoardItem.StaticId + 1);
        }

        public void BindView(OriginalGrassItemView view)
        {
            HasView = true;
            ItemView = view;
        }

        internal void RefillLayers(Sprite sprite) // 0x349d3cc.
        {
            if (layer == 2) return;
            layer++;
            ItemView.RefillFromSpread(layer, sprite);
        }

        public int RemainingExplodeCount() => unchecked(layer - incoming.Count);
        public int GetExtraIncomingExplodeCount(int guidedType) => extra.GetCount(guidedType);
        public void AddIncomingExploder(OriginalExplodeData data)
        {
            incoming.Add(data.Id);
            extra.Add(data.Id, data.Trigger);
        }

        public void RemoveIncomingExploder(OriginalExplodeData data)
        {
            incoming.Remove(data.Id);
            extra.Remove(data.Id, data.Trigger);
        }

        public bool IsValidExplodeTarget(int guidedType) => HasView;
        public bool CanAddPropellerAroundExploder() => true;
        public bool HasExplodeScore(OriginalExplodeData data) => GetExplodeScore(OriginalExplodeTargetRules.AsGuidedType(data.Trigger)) > 0;
        public Vector3 GetViewPosition() => ItemView.transform.position;
        public Vector3 GetMasterViewCenterPosition() => GetViewPosition();

        // The static mediator deduplicates direct hits per exploder.
        public void Explode(OriginalExplodeData data) => TryExplode();

        public void ExplodeAll(OriginalExplodeData data)
        {
            if (layer > 1)
            {
                TryExplode();
                layer = 1;
            }
            TryExplode();
        }

        private void TryExplode() // 0x349ce68.
        {
            int previous = layer;
            layer = unchecked(layer - 1);
            if (previous == 1)
            {
                ItemView.Explode();
                CurrentCell.StaticTouch.ClearStaticItem(StaticItemType);
                HasView = false;
                goals.DecreaseGoal(GoalType);
                goals.DecreaseGoalUi(GoalType);
                if (amounts.IsThereItemInLevel(40))
                    throw new NotSupportedException("Seed-box grass counter must be composed for this level.");
            }
            else if (previous == 2) ItemView.Damage(layer);
            useful.UsefulMoveMade();
        }

        public bool PlayTouchDownAnimation()
        {
            ItemView.PlayTapParticles(layer);
            return true;
        }

        public void PlayInvalidMoveAnimation() { } // StaticItemModel default false/no animation.

        public int GetExplodeScore(int guidedType) // 0x349d040, ordinary board/item branches.
        {
            var item = CurrentCell?.Mediation.CurrentItem;
            int score = 342675;
            if (item == null) return score;
            if (item is OriginalSpecialBoardItem)
            {
                if (item is OriginalLightballBoardItem)
                    return OriginalExplodeTargetRules.IsSingleExploderStrategy(guidedType) ? 155346 : unchecked(score - 1599395);
                return 159915;
            }
            bool useUnderlyingScore = amounts.IsThereItemInLevel(51);
            if (item is OriginalMatchBoardItem match)
            {
                int left = goals.GetGoalLeftCount(match.GoalType);
                if (match.MatchType == 5 && left > 0 && left <= 2) score = 479745;
                else if (left > 0) score = 347244;
                if (useUnderlyingScore || amounts.IsThereItemInLevel(16) || amounts.IsThereItemInLevel(82) || amounts.IsThereItemInLevel(96))
                    return Math.Max(score, item.GetExplodeScore(guidedType));
            }
            if (useUnderlyingScore) score = Math.Max(score, item.GetExplodeScore(guidedType));
            return score;
        }
    }
}
