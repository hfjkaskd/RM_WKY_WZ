using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalSafeBoardItem : OriginalMultiCellObstacle
    {
        private OriginalPlayableBoard game;
        public OriginalSafeItemView ItemView { get; private set; }
        public override bool IsFallable => false;
        public override bool IsSwappable => false;
        public override bool CanExplodeWithNearMatch => false;

        internal OriginalSafeBoardItem(OriginalItemSettings settings) : base(settings) { }

        internal void BindView(OriginalSafeItemView view, OriginalPlayableBoard owner)
        {
            ItemView = view;
            game = owner;
            BindSwapView(view);
            MarkViewCreated();
        }

        protected override void TryExplode(OriginalExplodeData data) // 0x2f1187c.
        {
            if (!HasView) return;
            if (--layer == 0)
            {
                ItemView.Explode();
                FinalExplodeCompleted();
            }
            else ItemView.Damage(layer);
            game.UsefulMoveMade();
        }

        public override bool PlayTouchDownAnimation()
        {
            ItemView.PlayTap(layer);
            return true;
        }

        public override int GetExplodeScore(int guidedType) => GetFourCellExplodeScore(guidedType);

        public override Vector3 GetMasterViewCenterPosition() => ItemView.transform.position + .5f * Vector3.one;
        internal void AddProxyIncoming(int id) => AddNonExtraIncomingExploder(id);
        internal void RemoveProxyIncoming(int id) => RemoveNonExtraIncomingExploder(id);
    }

    public sealed class OriginalSafeFakeBoardItem : OriginalBoardItem
    {
        public OriginalSafeBoardItem Master { get; }
        public override bool HasView => Master.HasView;
        public override bool IsFallable => false;
        public override bool IsSwappable => false;
        public override bool CanExplodeWithNearMatch => false;

        internal OriginalSafeFakeBoardItem(OriginalItemSettings settings) : base(settings, settings.LayerCount)
        {
            int direction = settings.TiledId == 47 ? 5 : settings.TiledId == 48 ? 6 : 7; // 0x2f11540.
            Master = settings.CellModel.Neighbors.Get(direction)?.Mediation.CurrentItem as OriginalSafeBoardItem;
            if (Master == null) throw new InvalidOperationException("Safe proxy has no original bottom-left master.");
            Master.AddFootprintCell(settings.CellModel);
            Master.OnExplodeEvent += ReleaseCell;
        }

        public override void Explode(OriginalExplodeData data) => Master.Explode(data);
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
