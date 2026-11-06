using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // BushItemModel 0x2bc2f4c, 0x2bc3270. The four cells share one hit register.
    public sealed class OriginalBushBoardItem : OriginalMultiCellObstacle
    {
        private OriginalPlayableBoard game;
        public OriginalBushItemView ItemView { get; private set; }
        public override bool IsFallable => false;
        public override bool IsSwappable => false;
        public override bool CanExplodeWithNearMatch => true;
        public override bool CanReceiveGrass => false;

        internal OriginalBushBoardItem(OriginalItemSettings settings) : base(settings) { }

        internal void BindView(OriginalBushItemView view, OriginalPlayableBoard owner)
        {
            ItemView = view;
            game = owner;
            BindSwapView(view);
            MarkViewCreated();
        }

        protected override void TryExplode(OriginalExplodeData data)
        {
            if (!HasView) return;
            ItemView.Damage(--layer);
            if (layer == 0)
            {
                var origin = Mediator.CurrentCell.Owner;
                FinalExplodeCompleted(.15f);
                ItemView.Explode(origin);
                game.ManualStartCoroutine(game.ReleaseGrassPrerequisite());
            }
            game.UsefulMoveMade();
        }

        public override bool PlayTouchDownAnimation()
        {
            ItemView.PlayTap(layer);
            return true;
        }

        public override Vector3 GetMasterViewCenterPosition() => ItemView.Center;

        public override int GetExplodeScore(int guidedType) => GetFourCellExplodeScore(guidedType);

        internal void AddProxyIncoming(int id) => AddNonExtraIncomingExploder(id);
        internal void RemoveProxyIncoming(int id) => RemoveNonExtraIncomingExploder(id);
    }

    // BushFakeItemModel 0x2bc2e54 / FakeItemModel 0x2b22098.
    public sealed class OriginalBushFakeBoardItem : OriginalBoardItem
    {
        public OriginalBushBoardItem Master { get; }
        public override bool HasView => Master.HasView;
        public override bool IsFallable => false;
        public override bool IsSwappable => false;
        public override bool CanExplodeWithNearMatch => true;
        public override bool CanReceiveGrass => false;

        internal OriginalBushFakeBoardItem(OriginalItemSettings settings) : base(settings, settings.LayerCount)
        {
            int direction = settings.TiledId == 45 ? 5 : settings.TiledId == 46 ? 6 : 7;
            Master = settings.CellModel.Neighbors.Get(direction)?.Mediation.CurrentItem as OriginalBushBoardItem;
            if (Master == null) throw new InvalidOperationException("Bush proxy has no original bottom-left master.");
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
