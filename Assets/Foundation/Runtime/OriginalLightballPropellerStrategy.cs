using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalLightballPropellerStrategy : OriginalLightballSpecialStrategy
    {
        private OriginalPropellerBoardItem partner;
        protected override OriginalSpecialBoardItem Partner => partner;
        protected override int ComboItemType => 3;
        protected override float ItemExplodeDelay => Assets.propellerExplodeDelay;
        public OriginalLightballPropellerStrategy(OriginalInitialBoardView board, OriginalPlayableBoard game,
            OriginalGeneralLevelRandom random, HashSet<int> selectedColors) : base(board, game, random, selectedColors) { }
        public override void Start(OriginalLightballBoardItem item, OriginalBoardItem other, OriginalExplodeData data) // 0x1f46d6c.
        {
            partner = other as OriginalPropellerBoardItem ?? throw new ArgumentException("Lightball propeller strategy requires a propeller partner.");
            base.Start(item, other, data);
        }
        protected override OriginalSpecialBoardItem CreateConvertedItem(OriginalGridCell cell, OriginalMatchBoardItem old)
        {
            var item = game.CreateLightballPropeller(cell);
            game.SpecialItemCreated(item, cell, 0, 1, true); // 0x1f4763c precedes current/target registration.
            return item;
        }
        protected override void AnimateConvertedItem(OriginalSpecialBoardItem item, OriginalLightballRay ray) // 0x1f473dc.
        {
            var view = ((OriginalPropellerBoardItem)item).ItemView;
            view.UpdateSorting(new OriginalSwapSorting(game.ItemSortingLayer, 11000, false));
            // Native propeller conversion has no PlayCreationAnimation call here.
            ray.CreateItemBackground(item.ItemType, view.transform.position, view.rootTransform); ray.ShakeItem(view.rootTransform);
        }
        protected override void ShowPartner() => partner.ItemView.rootTransform.ShowView();
        protected override void SortPartner() => partner.ItemView.UpdateSorting(new OriginalSwapSorting(game.ItemSortingLayer, 11000, false));
        protected override void CreatePartnerBackground() // 0x1f472cc.
        {
            partnerBackground = CreateBackground(partner.ItemType, partner.ItemView.transform.position, partner.ItemView.rootTransform);
            partnerShake = Coroutines.ManualStartCoroutine(ShakeItem(partner.ItemView.rootTransform));
        }
        protected override void Reset() { base.Reset(); partner = null; }
    }

    public sealed partial class OriginalPlayableBoard
    {
        private void ExplodeLightballPropellerCombo(OriginalSpecialBoardItem to, OriginalSpecialBoardItem from, OriginalExplodeData data)
        {
            var center = from.CurrentCell;
            var lightball = (to as OriginalLightballBoardItem) ?? (OriginalLightballBoardItem)from;
            var propeller = (to as OriginalPropellerBoardItem) ?? (OriginalPropellerBoardItem)from;
            if (!ReferenceEquals(lightball.CurrentCell, center))
            {
                lightball.Mediator.ClearFromAllRegisteredCells(); center.Mediation.ClearAllItems(); center.Mediation.SetAllItems(lightball);
            }
            propeller.Mediator.ClearFromAllRegisteredCells(); propeller.SetVisibilityActiveFlag(false); propeller.ItemView.rootTransform.HideView();
            lightball.ComboWith(propeller, new OriginalLightballPropellerStrategy(board, this, generalRandom, specialCreator.LightballSelectedColors), data);
        }
        internal OriginalPropellerBoardItem CreateLightballPropeller(OriginalGridCell cell)
        {
            int id = OriginalBoardItem.StaticId; OriginalBoardItem.StaticId = unchecked(id + 1);
            var item = new OriginalPropellerBoardItem(id, () => board.Clock.Time, this, true);
            item.TiledId = 10;
            item.CreateView(board.Presentation, board.BoardRoot, new Vector3(cell.X, cell.Y, 0f), this);
            item.ItemView.OnFallCreated += NotifyPropellerFall;
            item.GoalType = OriginalGoalTypes.FromItemType(item.ItemType);
            item.BindExplosion(this); item.BindTargetScoreContext(guidedTargetServices); item.BindGoalServices(board.ItemGoals);
            item.CreateFallComponent(); ((OriginalFallComponentState)item.FallComponent).BindMotionServices(board.FallMotion);
            item.FallView = new OriginalSpecialItemFallView(item, ItemSortingLayer);
            board.LocalMoves.RegisterTarget(item.ItemView.transform);
            return item;
        }
    }
}
