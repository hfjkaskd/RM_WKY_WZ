using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Native LightballTntStrategy's normal-lightball branch.
    public sealed class OriginalLightballTntStrategy : OriginalLightballSpecialStrategy
    {
        private OriginalTntBoardItem partner;
        protected override OriginalSpecialBoardItem Partner => partner;
        protected override int ComboItemType => 4;
        protected override float ItemExplodeDelay => 0f;
        public float CreationAnimationStartedAt { get; private set; } = -1f;
        public float ExplosionStartedAt { get; private set; } = -1f;
        public float ExplosionWaitElapsed { get; private set; }
        public OriginalLightballTntStrategy(OriginalInitialBoardView board, OriginalPlayableBoard game,
            OriginalGeneralLevelRandom random, HashSet<int> selectedColors) : base(board, game, random, selectedColors) { }
        public override void Start(OriginalLightballBoardItem item, OriginalBoardItem other, OriginalExplodeData data) // 0x212f560.
        {
            partner = other as OriginalTntBoardItem ?? throw new ArgumentException("Lightball TNT strategy requires a TNT partner.");
            CreationAnimationStartedAt = ExplosionStartedAt = -1f; ExplosionWaitElapsed = 0f;
            base.Start(item, other, data);
        }
        protected override OriginalSpecialBoardItem CreateConvertedItem(OriginalGridCell cell, OriginalMatchBoardItem old) => game.CreateLightballTnt(cell);
        protected override void AnimateConvertedItem(OriginalSpecialBoardItem item, OriginalLightballRay ray) // 0x212f690.
        {
            var view = ((OriginalTntBoardItem)item).ItemView;
            view.UpdateSortingForAnimation(new OriginalSwapSorting(game.ItemSortingLayer, 11000, false), false);
            view.PlayCreationAnimation(.3103448152542114f, false);
            ray.CreateItemBackground(item.ItemType, view.transform.position, view.rootTransform); ray.ShakeItem(view.rootTransform);
        }
        protected override void ShowPartner() => partner.ItemView.Show();
        protected override void SortPartner() => partner.ItemView.UpdateSortingForAnimation(new OriginalSwapSorting(game.ItemSortingLayer, 11000, false), false);
        protected override void CreatePartnerBackground() // 0x212fac8.
        {
            partnerBackground = CreateBackground(partner.ItemType, partner.ItemView.transform.position, partner.ItemView.rootTransform);
            partner.ItemView.ResetFuseParticlesSorting(); partner.ItemView.PlayFuseParticles();
            partnerShake = Coroutines.ManualStartCoroutine(ShakeItem(partner.ItemView.rootTransform));
        }
        protected override void AllRaysComplete() // 0x212fe98.
        {
            CreationAnimationStartedAt = board.Clock.Time;
            var data = OriginalExploder.Next(14);
            for (int i = 0; i < replaced.Count; i++)
            {
                var item = (OriginalTntBoardItem)replaced[i];
                if (!item.HasView || item.CurrentCell == null) continue;
                item.LightballExplodeData = data; item.ItemView.PlayLightballCreationAnimation();
            }
            Coroutines.ManualStartCoroutine(CollectMadness(data, lightballCell)); base.AllRaysComplete();
        }
        private IEnumerator CollectMadness(OriginalExplodeData data, OriginalGridCell cell) // 0x2130430.
        {
            yield return new WaitForSeconds(.7f);
            Vector3 p = cell.GetViewPosition(); game.Session.TryCollectMadnessForExploder(data, new OriginalSwapPoint(p.x, p.y, p.z), 0);
        }
        protected override IEnumerator ExplodeItems() // 0x21301f8; TNT overrides the base sorted staggered sequence.
        {
            var wait = (OriginalGameplayWait)game.Wait(.5f); yield return wait;
            ExplosionWaitElapsed = wait.ElapsedSeconds; ExplosionStartedAt = board.Clock.Time;
            var data = OriginalExploder.Next(15);
            for (int i = 0; i < replaced.Count; i++)
                if (replaced[i].HasView && replaced[i].CurrentCell != null) replaced[i].Explode(data);
            board.State.FinishSpecialOperation(); board.Moves.CompleteMoveByType(9);
        }
    }

    public sealed partial class OriginalPlayableBoard
    {
        private void ExplodeLightballTntCombo(OriginalSpecialBoardItem to, OriginalSpecialBoardItem from, OriginalExplodeData data)
        {
            var center = from.CurrentCell;
            var lightball = (to as OriginalLightballBoardItem) ?? (OriginalLightballBoardItem)from;
            var tnt = (to as OriginalTntBoardItem) ?? (OriginalTntBoardItem)from;
            if (!ReferenceEquals(lightball.CurrentCell, center))
            {
                lightball.Mediator.ClearFromAllRegisteredCells(); center.Mediation.ClearAllItems(); center.Mediation.SetAllItems(lightball);
            }
            tnt.Mediator.ClearFromAllRegisteredCells(); tnt.SetVisibilityActiveFlag(false); tnt.ItemView.Hide();
            lightball.ComboWith(tnt, new OriginalLightballTntStrategy(board, this, generalRandom, specialCreator.LightballSelectedColors), data);
        }
        internal OriginalTntBoardItem CreateLightballTnt(OriginalGridCell cell)
        {
            int id = OriginalBoardItem.StaticId; OriginalBoardItem.StaticId = unchecked(id + 1);
            var item = new OriginalTntBoardItem(id, true, 40, () => board.Clock.Time, this);
            item.TiledId = 40;
            item.CreateView(board.Presentation, board.BoardRoot, new Vector3(cell.X, cell.Y, 0f), this);
            item.GoalType = OriginalGoalTypes.FromItemType(item.ItemType);
            item.BindExplosion(this); item.BindTargetScoreContext(guidedTargetServices); item.BindGoalServices(board.ItemGoals);
            item.CreateFallComponent(); ((OriginalFallComponentState)item.FallComponent).BindMotionServices(board.FallMotion);
            item.FallView = new OriginalSpecialItemFallView(item, ItemSortingLayer);
            board.LocalMoves.RegisterTarget(item.ItemView.transform);
            return item;
        }
        public void IncrementLightballCreatedItem(int type, int amount) // 0x27b2e48.
        {
            if (!Session.SpecialItemCounts.Items.TryGetValue(type, out var value))
            { value = new OriginalSpecialItemCount(); Session.SpecialItemCounts.Items.Add(type, value); }
            value.LightballCreationCount = unchecked(value.LightballCreationCount + amount);
        }
    }
}
