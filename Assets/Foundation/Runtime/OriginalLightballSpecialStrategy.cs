using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Native LightballSpecialStrategy's shared normal-lightball branch. Above-static
    // and super-lightball mechanics require their own original owners before admission.
    public abstract class OriginalLightballSpecialStrategy : OriginalDefaultLightballStrategy
    {
        protected readonly List<OriginalSpecialBoardItem> replaced = new List<OriginalSpecialBoardItem>();
        protected OriginalGridCell lightballCell;
        protected OriginalLightballItemBg partnerBackground;
        protected Coroutine partnerShake;
        public IReadOnlyList<OriginalSpecialBoardItem> ReplacedItems => replaced;
        public int ConvertedCount { get; private set; }
        public int SelectedMatchType => matchType;
        protected abstract OriginalSpecialBoardItem Partner { get; }
        protected abstract int ComboItemType { get; }
        protected abstract float ItemExplodeDelay { get; }
        protected abstract void ShowPartner();
        protected abstract void SortPartner();
        protected abstract void CreatePartnerBackground();
        protected abstract OriginalSpecialBoardItem CreateConvertedItem(OriginalGridCell cell, OriginalMatchBoardItem old);
        protected abstract void AnimateConvertedItem(OriginalSpecialBoardItem item, OriginalLightballRay ray);
        public int ExplicitTriggerCount { get; private set; }
        public float LastTriggerWaitElapsed { get; private set; }
        public event Action<OriginalSpecialBoardItem, OriginalCellPoint, float> ItemTriggered;

        protected OriginalLightballSpecialStrategy(OriginalInitialBoardView board, OriginalPlayableBoard game,
            OriginalGeneralLevelRandom random, HashSet<int> selectedColors) : base(board, game, random, selectedColors) { }
        public override void Start(OriginalLightballBoardItem item, OriginalBoardItem other, OriginalExplodeData data) // 0x1f46e48.
        {
            replaced.Clear(); ConvertedCount = ExplicitTriggerCount = 0;
            LastTriggerWaitElapsed = 0f;
            Initialize(item); matchType = targeting.FindMostCommonItem(); targeting.FindItemsByType(matchType);
            model.CurrentCell.FreezeFall(); game.TouchListener.DisableTouch();
            random.Shuffle(targeting.Items); StartShakingLightball(other); StartSendingRays();
        }
        public override void SingleRayReached(OriginalLightballRay ray, OriginalMatchBoardItem item)
        {
            RecordRayReached(); // Native base callback has haptic/jelly work, inactive on this board.
            var cell = item.Mediator.CurrentCell.Owner;
            if (cell.StaticTouch.AboveItems.Count != 0)
                throw new NotSupportedException("Lightball conversion under a chained/static item requires its original owner.");
            item.Mediator.ClearFromAllRegisteredCells(); item.RecycleView(); // 0x1f47a4c.
            var converted = CreateConvertedItem(cell, item);
            cell.Mediation.SetCurrentItem(converted); cell.Mediation.SetTargetItem(converted); // 0x1f47aac; Next stays empty.
            replaced.Add(converted); ConvertedCount++;
            ray.BindConvertedItem(converted); AnimateConvertedItem(converted, ray);
        }
        protected override void StoppedLookingForNewItems() // 0x1f4885c / 0x1f48880.
        {
            lightballCell = model.CurrentCell; model.ExplodeSelf(); Partner.SetVisibilityActiveFlag(true); ShowPartner();
            lightballCell.StaticTouch.ExplodeTopMostAboveItem(OriginalExploder.Next(15));
            lightballCell.Mediation.SetCurrentItem(Partner); lightballCell.Mediation.SetTargetItem(Partner);
            replaced.Add(Partner); SortPartner(); Partner.OnExplodeEvent += PartnerExploded;
            game.SpecialItemCreated(Partner, lightballCell, 10, 1, true);
            if (useParticles != null) { useParticles.StopEmitting(); useParticles = null; }
            CreatePartnerBackground();
        }
        private void PartnerExploded() // 0x1f48c94.
        {
            Partner.OnExplodeEvent -= PartnerExploded;
            if (partnerShake != null) { Coroutines.StopCoroutine(partnerShake); partnerShake = null; }
            if (partnerBackground != null) { partnerBackground.RecycleSelf(); partnerBackground = null; }
            lightballCell.UnFreezeFall();
        }
        protected override void AllRaysComplete() // 0x1f48610, no non-converted chain items in this admitted branch.
        {
            game.IncrementLightballCreatedItem(ComboItemType, targeting.Items.Count + 1);
            for (int i = 0; i < targeting.Items.Count; i++)
                if (targeting.Items[i].HasView && targeting.Items[i].IsReserved) targeting.Items[i].UnReserve();
            Coroutines.ManualStartCoroutine(ExplodeItems(), AnimationCompleted);
        }
        protected virtual IEnumerator ExplodeItems() // 0x1f48ec8.
        {
            var data = OriginalExploder.Next(15);
            replaced.Sort(TopLeftComparer.Instance);
            for (int i = 0; i < replaced.Count; i++)
            {
                var item = replaced[i];
                if (item == null || !item.HasView || item.CurrentCell == null) continue;
                ExplicitTriggerCount++;
                ItemTriggered?.Invoke(item, new OriginalCellPoint(item.CurrentCell.X, item.CurrentCell.Y), board.Clock.Time);
                item.Explode(data);
                var wait = (OriginalGameplayWait)game.Wait(ItemExplodeDelay);
                yield return wait; LastTriggerWaitElapsed = wait.ElapsedSeconds;
            }
            board.State.FinishSpecialOperation(); board.Moves.CompleteMoveByType(9);
        }
        protected override void Reset() { base.Reset(); replaced.Clear(); }
        private sealed class TopLeftComparer : IComparer<OriginalSpecialBoardItem>
        {
            public static readonly TopLeftComparer Instance = new TopLeftComparer();
            public int Compare(OriginalSpecialBoardItem first, OriginalSpecialBoardItem second) // 0x3197e04.
            {
                if (first == null || second == null || first.CurrentCell == null || second.CurrentCell == null) return 0;
                var a = first.CurrentCell; var b = second.CurrentCell;
                if (a.Y > b.Y) return -1; if (a.Y < b.Y) return 1;
                return a.X < b.X ? -1 : a.X > b.X ? 1 : 0;
            }
        }
    }
}
