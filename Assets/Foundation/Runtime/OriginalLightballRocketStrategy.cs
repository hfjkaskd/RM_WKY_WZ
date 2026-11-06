using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalLightballRocketStrategy : OriginalLightballSpecialStrategy
    {
        private OriginalRocketBoardItem partner;
        private readonly Dictionary<int, int> orientations = new Dictionary<int, int>();
        private int lastOrientation;
        protected override OriginalSpecialBoardItem Partner => partner;
        protected override int ComboItemType => 2;
        protected override float ItemExplodeDelay => Assets.rocketExplodeDelay;
        public override float PositionOffset => Assets.rocketPositionOffset;
        public override float MaxShakeScale => Assets.rocketMaxShakeScale;
        public int AssignedCount { get; private set; }
        public OriginalLightballRocketStrategy(OriginalInitialBoardView board, OriginalPlayableBoard game,
            OriginalGeneralLevelRandom random, HashSet<int> selectedColors) : base(board, game, random, selectedColors) { }
        public override void Start(OriginalLightballBoardItem item, OriginalBoardItem other, OriginalExplodeData data) // 0x1f47d44.
        {
            partner = other as OriginalRocketBoardItem ?? throw new ArgumentException("Lightball rocket strategy requires a rocket partner.");
            lastOrientation = partner.Orientation; AssignedCount = 0; base.Start(item, other, data);
        }
        protected override void MatchItemAdded(OriginalMatchBoardItem item) // 0x1f47ea0; before native shuffle.
        {
            orientations.Add(item.Id, lastOrientation); AssignedCount++;
            if (lastOrientation == 0) lastOrientation = 1; else if (lastOrientation == 1) lastOrientation = 0;
        }
        protected override void MatchItemRemoved(OriginalMatchBoardItem item) => orientations.Remove(item.Id); // 0x1f48078.
        public int GetAssignedOrientation(int id) => orientations[id];
        protected override OriginalSpecialBoardItem CreateConvertedItem(OriginalGridCell cell, OriginalMatchBoardItem old) =>
            game.CreateLightballRocket(orientations[old.Id] == 0 ? 30 : 20, cell); // 0x1f4850c.
        protected override void AnimateConvertedItem(OriginalSpecialBoardItem item, OriginalLightballRay ray) // 0x1f480e4.
        {
            var view = ((OriginalRocketBoardItem)item).ItemView;
            view.UpdateSortingForAnimation(new OriginalSwapSorting(game.ItemSortingLayer, 11000, false), false);
            view.PlayCreationAnimation(Assets.rocketConversionStartTime, false);
            ray.CreateItemBackground(item.ItemType, view.transform.position, view.rootTransform); ray.ShakeItem(view.rootTransform);
        }
        protected override void ShowPartner() => partner.ItemView.Show();
        protected override void SortPartner() => partner.ItemView.UpdateSortingForAnimation(new OriginalSwapSorting(game.ItemSortingLayer, 11000, false), false);
        protected override void CreatePartnerBackground() => // Native 0x1f47ff8 creates the background without shaking the partner.
            partnerBackground = CreateBackground(partner.ItemType, partner.ItemView.transform.position, partner.ItemView.rootTransform);
        protected override void Reset() { base.Reset(); orientations.Clear(); partner = null; }
    }

    public sealed partial class OriginalPlayableBoard
    {
        private void ExplodeLightballRocketCombo(OriginalSpecialBoardItem to, OriginalSpecialBoardItem from, OriginalExplodeData data)
        {
            var center = from.CurrentCell;
            var lightball = (to as OriginalLightballBoardItem) ?? (OriginalLightballBoardItem)from;
            var rocket = (to as OriginalRocketBoardItem) ?? (OriginalRocketBoardItem)from;
            if (!ReferenceEquals(lightball.CurrentCell, center))
            {
                lightball.Mediator.ClearFromAllRegisteredCells(); center.Mediation.ClearAllItems(); center.Mediation.SetAllItems(lightball);
            }
            rocket.Mediator.ClearFromAllRegisteredCells(); rocket.SetVisibilityActiveFlag(false); rocket.ItemView.Hide();
            lightball.ComboWith(rocket, new OriginalLightballRocketStrategy(board, this, generalRandom, specialCreator.LightballSelectedColors), data);
        }
        internal OriginalRocketBoardItem CreateLightballRocket(int tiledId, OriginalGridCell cell)
        {
            int id = OriginalBoardItem.StaticId; OriginalBoardItem.StaticId = unchecked(id + 1);
            var item = new OriginalRocketBoardItem(id, tiledId == 30 ? 0 : 1, true, tiledId, () => board.Clock.Time, this);
            item.TiledId = tiledId;
            item.CreateView(board.Presentation, board.BoardRoot, new Vector3(cell.X, cell.Y, 0f), this);
            item.GoalType = OriginalGoalTypes.FromItemType(item.ItemType);
            item.BindExplosion(this); item.BindTargetScoreContext(guidedTargetServices); item.BindGoalServices(board.ItemGoals);
            item.CreateFallComponent(); ((OriginalFallComponentState)item.FallComponent).BindMotionServices(board.FallMotion);
            item.FallView = new OriginalSpecialItemFallView(item, ItemSortingLayer);
            board.LocalMoves.RegisterTarget(item.ItemView.transform);
            return item;
        }
    }
}
