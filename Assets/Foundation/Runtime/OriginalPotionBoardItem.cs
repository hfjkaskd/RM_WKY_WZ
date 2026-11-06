using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // PotionItemModel 0x2177318 / 0x2177a90. The model stores palette order
    // independently of the order used for the four original bottle positions.
    public sealed class OriginalPotionBoardItem : OriginalMultiCellObstacle
    {
        private const int AllBottles = 15;
        private readonly List<int> remainingColors;
        private readonly Dictionary<int, int> fakeComboHits = new Dictionary<int, int>();
        private OriginalPlayableBoard game;
        private int restoredBottleMask = AllBottles;
        public OriginalPotionItemView ItemView { get; private set; }
        public override bool IsFallable => false;
        public override bool IsSwappable => false;
        public override bool CanExplodeWithNearMatch => true;

        internal OriginalPotionBoardItem(OriginalItemSettings settings, int[] colors) : base(settings)
        {
            if (colors == null || colors.Length != layer)
                throw new InvalidOperationException("The original potion rack requires its level palette.");
            remainingColors = new List<int>(colors);
        }

        internal void BindView(OriginalPotionItemView view, OriginalPlayableBoard owner)
        {
            game = owner;
            ItemView = view;
            BindSwapView(view);
            MarkViewCreated();
        }

        public bool HasBottle(int color) => remainingColors.Contains(color);

        // PotionItemModel.GetSaveData 0x2177f08 stores the original view-slot mask.
        internal override short SaveRestorableData() =>
            ItemView != null ? ItemView.GetActiveBottleMask() : (short)restoredBottleMask;

        internal override void RestoreItemData(short data) => restoredBottleMask = data & AllBottles;

        internal void RestoreBottleColors(int[] viewColors)
        {
            if (restoredBottleMask == AllBottles) return;
            for (int i = 0; i < viewColors.Length; i++)
                if ((restoredBottleMask & (1 << i)) == 0) remainingColors.Remove(viewColors[i]);
            layer = remainingColors.Count;
        }
        public override int GetExplodeScore(int guidedType) => GetFourCellExplodeScore(guidedType);
        public override Vector3 GetMasterViewCenterPosition() => GetViewPosition() + Vector3.one * .5f;

        protected override void TryExplode(OriginalExplodeData data)
        {
            if (!HasView || remainingColors.Count == 0) return;
            int color = data.MatchType != 0 ? data.MatchType : remainingColors[game.NextRegularRandom(0, remainingColors.Count)];
            if (!remainingColors.Remove(color)) return;
            layer = remainingColors.Count;
            ItemView.Damage(color);
            game.UsefulMoveMade();
            if (layer != 0) CollectAsGoal();
            else
            {
                ItemView.Explode();
                FinalExplodeCompleted(.15f);
            }
        }

        internal void HitFromThirdComboProxy(OriginalExplodeData data) // 0x2176bac.
        {
            fakeComboHits.TryGetValue(data.Id, out int previous);
            int hits = previous + 1;
            if (hits == 3) TryExplode(data);
            fakeComboHits[data.Id] = hits;
        }

        public override void ExplodeAll(OriginalExplodeData data) // 0x2177cac reads the live count each iteration.
        {
            for (int i = 0; i < remainingColors.Count; i++) TryExplode(data);
        }

        public override bool PlayTouchDownAnimation()
        {
            ItemView.PlayTap(layer);
            return true;
        }

        internal void AddProxyIncoming(int id) => AddNonExtraIncomingExploder(id);
        internal void RemoveProxyIncoming(int id) => RemoveNonExtraIncomingExploder(id);
    }

    // PotionFakeItemModel 0x21769e4 / 0x2176aa8. Each footprint cell has
    // independent mediation and TNT-rocket hit registration.
    public sealed class OriginalPotionFakeBoardItem : OriginalBoardItem
    {
        public OriginalPotionBoardItem Master { get; }
        public override bool HasView => Master.HasView;
        public override bool IsFallable => false;
        public override bool IsSwappable => false;
        public override bool CanExplodeWithNearMatch => true;

        internal OriginalPotionFakeBoardItem(OriginalItemSettings settings) : base(settings, settings.LayerCount)
        {
            int direction = settings.TiledId == 43 ? 5 : settings.TiledId == 44 ? 6 : 7;
            Master = settings.CellModel.Neighbors.Get(direction)?.Mediation.CurrentItem as OriginalPotionBoardItem;
            if (Master == null) throw new InvalidOperationException("Potion proxy has no original bottom-left master.");
            Master.AddFootprintCell(settings.CellModel);
            Master.OnExplodeEvent += ReleaseCell;
        }

        public override void Explode(OriginalExplodeData data)
        {
            Master.Explode(data);
            if (data.Trigger == 12 && RegisterExploder(data.Id)) Master.HitFromThirdComboProxy(data);
        }
        public override void ExplodeAll(OriginalExplodeData data) => Master.ExplodeAll(data);
        public override bool PlayTouchDownAnimation() => Master.PlayTouchDownAnimation();
        public override int RemainingExplodeCount() => Master.RemainingExplodeCount();
        public override int GetExplodeScore(int guidedType) => Master.GetExplodeScore(guidedType);
        public override Vector3 GetViewPosition() => Master.GetViewPosition();
        public override Vector3 GetMasterViewCenterPosition() => Master.GetMasterViewCenterPosition();

        public override void AddIncomingExploder(OriginalExplodeData data)
        {
            if (data.Trigger == 10 || data.Trigger == 11 || data.Trigger == 13)
            {
                Master.AddProxyIncoming(data.Id);
                AddExtraIncomingExploder(data);
            }
            else Master.AddIncomingExploder(data);
        }

        public override void RemoveIncomingExploder(OriginalExplodeData data)
        {
            if (data.Trigger == 10 || data.Trigger == 11 || data.Trigger == 13)
            {
                Master.RemoveProxyIncoming(data.Id);
                RemoveExtraIncomingExploder(data);
            }
            else Master.RemoveIncomingExploder(data);
        }

        private void ReleaseCell()
        {
            Master.OnExplodeEvent -= ReleaseCell;
            Mediator.CurrentCell.Owner.FreezeFallForDuration(.15f);
            Mediator.ClearFromAllRegisteredCells();
        }
    }
}
