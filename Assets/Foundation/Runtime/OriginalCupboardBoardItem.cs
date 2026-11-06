using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // CupboardItemModel owns the door, ten plates and one view. Fake cells
    // retain individual mediation and exploder identities.
    public sealed class OriginalCupboardBoardItem : OriginalMultiCellObstacle
    {
        private bool hasDoor = true;
        private int doorExploderId;
        private IOriginalBoxExplosionServices usefulMoves;
        private OriginalCupboardItemRegistry cupboardItems;
        public OriginalCupboardItemView ItemView { get; private set; }
        public bool HasDoor => hasDoor;
        public int PlateCount => layer - (hasDoor ? 1 : 0);
        public override bool CanExplodeWithNearMatch => true;
        public override bool IsFallable => false;
        public override bool IsSwappable => false;

        internal OriginalCupboardBoardItem(OriginalItemSettings settings) : base(settings) { }

        internal override void RestoreItemData(short data)
        {
            base.RestoreItemData(data);
            hasDoor = layer == OriginalTiledItemLayers.AsLayerCount(TiledId);
        }

        internal void CreateView(OriginalLevelPresentation presentation, Transform parent, OriginalGameAudioHost audio,
            IOriginalLevelSeedRandom random, OriginalDeltaTimeManager clock, IOriginalBoxExplosionServices usefulMoves,
            OriginalCupboardItemRegistry cupboardItems)
        {
            this.usefulMoves = usefulMoves;
            this.cupboardItems = cupboardItems;
            var cell = Mediator.CurrentCell.Owner;
            ItemView = presentation.Pool.Spawn<OriginalCupboardItemView>(47, true);
            ItemView.Initialize(this, parent, new Vector3(cell.X, cell.Y, 0f), presentation, audio, random, clock);
            BindSwapView(ItemView);
            HasView = true;
            cupboardItems.Add(this);
        }

        private bool TryExplodeDoor(OriginalExplodeData data) // 0x2bcac1c.
        {
            if (!hasDoor) return doorExploderId == data.Id;
            hasDoor = false;
            layer--;
            doorExploderId = data.Id;
            ItemView.RemoveDoor(cupboardItems.FindPreviousShake(this));
            return true;
        }

        public override void Explode(OriginalExplodeData data) // 0x2bcac7c.
        {
            if (TryExplodeDoor(data)) return;
            if (data.Trigger == 5) TryExplode(data);
            else base.Explode(data);
        }

        internal void ExplodeFromFake(OriginalExplodeData data) // 0x2bca088.
        {
            if (TryExplodeDoor(data)) usefulMoves.UsefulMoveMade();
            else TryExplode(data);
        }

        protected override void TryExplode(OriginalExplodeData data) // 0x2bca7f0.
        {
            layer--;
            ItemView.Damage(layer, cupboardItems.FindPreviousShake(this), data);
            usefulMoves.UsefulMoveMade();
            if (layer > 0)
            {
                CollectAsGoal();
                return;
            }
            cupboardItems.Remove(this);
            ItemView.Explode();
            FinalExplodeCompleted(.15f);
        }

        public override void ExplodeAll(OriginalExplodeData data) // 0x2bcad38.
        {
            if (!RegisterExploder(data.Id)) return;
            if (layer == 1)
            {
                TryExplode(data);
                return;
            }
            if (hasDoor) TryExplodeDoor(data);
            else TryExplode(data);
            int unshownPlates = layer - 1;
            layer = 1;
            TryExplode(data);
            for (int i = 0; i < unshownPlates; i++) CollectAsGoal();
        }

        public override bool PlayTouchDownAnimation() // 0x2bcb188.
        {
            ItemView.PlayTap(hasDoor, layer);
            return true;
        }

        internal void AddProxyIncoming(OriginalExplodeData data) => AddNonExtraIncomingExploder(data.Id);
        internal void RemoveProxyIncoming(OriginalExplodeData data) => RemoveNonExtraIncomingExploder(data.Id);
        public override Vector3 GetMasterViewCenterPosition() => GetViewPosition() + Vector3.one * .5f; // 0x2bcc73c.

        public override int GetExplodeScore(int guidedType) // 0x2bca77c, 0x2b4c428.
        {
            if (guidedType is 3 or 6)
            {
                int remaining = RemainingExplodeCount();
                return remaining >= 2 && remaining <= 10 ? 0x6f910 : 0x37c88;
            }
            return GetFourCellExplodeScore(guidedType);
        }
    }

    // CupboardItemRegistry 0x2bcb9c4. Each attempt registers models in view creation order.
    internal sealed class OriginalCupboardItemRegistry
    {
        private const int MaxItems = 16;
        private readonly List<OriginalCupboardBoardItem> items = new List<OriginalCupboardBoardItem>(MaxItems);
        private readonly OriginalDeltaTimeManager clock;

        public OriginalCupboardItemRegistry(OriginalDeltaTimeManager clock) => this.clock = clock;

        public void Add(OriginalCupboardBoardItem item) // 0x2bca684.
        {
            if (items.Count < MaxItems) items.Add(item);
        }

        public void Remove(OriginalCupboardBoardItem item) => items.Remove(item);

        public void SortByCellDistance() => items.Sort(CompareByCellDistance); // 0x2bcb934.

        private static int CompareByCellDistance(OriginalCupboardBoardItem first, OriginalCupboardBoardItem second)
        {
            return DistanceFromOrigin(first).CompareTo(DistanceFromOrigin(second)); // 0x2bcba50.
        }

        private static float DistanceFromOrigin(OriginalCupboardBoardItem item)
        {
            var cell = item.Mediator.CurrentCell.Owner;
            return Vector2.Distance(Vector2.zero, new Vector2(cell.X, cell.Y));
        }

        public int FindPreviousShake(OriginalCupboardBoardItem item) // 0x2bcb0bc.
        {
            for (int i = items.IndexOf(item) - 1; i >= 0; i--)
            {
                var view = items[i].ItemView;
                if (clock.FrameCount - view.ShakeFrame < 5) return view.SelectedShake;
            }
            return -1;
        }
    }

    // CupboardFakeItemModel. The constructor resolves its native neighbor
    // direction during bottom-left vertical model population.
    public sealed class OriginalCupboardFakeBoardItem : OriginalBoardItem
    {
        public OriginalCupboardBoardItem Master { get; }
        public override bool HasView => Master.HasView;
        public override bool CanExplodeWithNearMatch => true;
        public override bool IsFallable => false;
        public override bool IsSwappable => false;

        internal OriginalCupboardFakeBoardItem(OriginalItemSettings settings) : base(settings, settings.LayerCount)
        {
            int direction = settings.TiledId == 41 ? 5 : settings.TiledId == 42 ? 6 : 7;
            Master = settings.CellModel.Neighbors.Get(direction)?.Mediation.CurrentItem as OriginalCupboardBoardItem;
            if (Master == null) throw new InvalidOperationException("Cupboard proxy has no original bottom-left master.");
            Master.AddFootprintCell(settings.CellModel);
            Master.OnExplodeEvent += ReleaseCell;
        }

        public override void Explode(OriginalExplodeData data) // 0x2bca144.
        {
            if (data.Trigger == 5) TryExplode(data);
            else base.Explode(data);
        }

        protected override void TryExplode(OriginalExplodeData data) => Master.ExplodeFromFake(data);
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
                Master.AddProxyIncoming(data);
                AddExtraIncomingExploder(data);
            }
            else Master.AddIncomingExploder(data);
        }

        public override void RemoveIncomingExploder(OriginalExplodeData data)
        {
            if (data.Trigger == 10 || data.Trigger == 11 || data.Trigger == 13)
            {
                Master.RemoveProxyIncoming(data);
                RemoveExtraIncomingExploder(data);
            }
            else Master.RemoveIncomingExploder(data);
        }

        private void ReleaseCell() // FakeItemModel.FinalExplodeCompleted 0x2b221b8.
        {
            Master.OnExplodeEvent -= ReleaseCell;
            Mediator.CurrentCell.Owner.FreezeFallForDuration(.15f);
            Mediator.ClearFromAllRegisteredCells();
        }
    }
}
