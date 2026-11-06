using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchGroupExplosionServices : IOriginalMatchDataServices
    {
        bool IsAutoMatchDisabledForWaterLevel { get; }
        bool InitJellySpreadData(ref OriginalMatchGroupSnapshot group, OriginalExplodeData data);
        void ExplodeTopMostAboveItem(OriginalGridCell cell, OriginalExplodeData data);
        void ExplodeMatchItem(OriginalMatchBoardItem item, OriginalExplodeData data);
        OriginalGridCell GetCell(OriginalCellPoint point);
        void TryCollectMadness(OriginalGridCell origin, int matchType, int count, int trigger);
        void CreateSpecialItemForMatchGroup(OriginalMatchGroupSnapshot group, OriginalGridCell creationCell, bool isFromAutoMatch);
        void ExplodeCurrentItemByNearMatch(OriginalGridCell cell, OriginalExplodeData data);
        void PlayMatchExplode(bool isSwap);
        void PlayHaptic(int id, int type, float strength, float duration, bool replay);
    }

    // MatchManager.ExplodeMatchGroup 0x12e33dc. Concrete item/static-item
    // explosion, jelly propagation and presentation are required services.
    public sealed class OriginalMatchGroupExplosion
    {
        // Native TouchingNeighborTypes private data SHA256 49C16B6F...236F8F855.
        private static readonly int[] touchingDirections = { 1, 3, 5, 7 };
        private readonly OriginalMatchExplosionReadiness readiness;
        private readonly OriginalMatchDataPreparation preparation;
        private readonly IOriginalMatchGroupExplosionServices services;
        private Func<bool> canRecordSynthesis;
        private Action<Vector3> recordSynthesis;

        public OriginalMatchGroupExplosion(OriginalMatchExplosionReadiness readiness, IOriginalMatchGroupExplosionServices services)
        {
            this.readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            preparation = new OriginalMatchDataPreparation(services);
        }
        internal void BindSynthesisObserver(Func<bool> canRecord, Action<Vector3> record)
        {
            canRecordSynthesis = canRecord;
            recordSynthesis = record;
        }

        public bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group, OriginalExplodeData data, bool isFromAutoMatch = false)
        {
            if (services.IsAutoMatchDisabledForWaterLevel || !readiness.CanAutoExplode(ref group)) return false;
            bool observeSynthesis = canRecordSynthesis?.Invoke() == true;
            OriginalGridCell removedCell = null;
            data.CanSpreadJelly = services.InitJellySpreadData(ref group, data);
            var collect = preparation.CreateCollectData(group);
            var special = preparation.CreateSpecialItemData(group);
            // Native owns a new list for each invocation, including nested
            // explosions; a shared buffer would corrupt its pending neighbors.
            var destroyed = new List<OriginalGridCell>();
            int minX = 100, maxX = -1, minY = 100, maxY = -1;
            for (int i = 0; i < group.Cells.Count; i++)
            {
                var cell = group.Cells.Get(i);
                if (cell == null) throw new NullReferenceException();
                if (services.HasStaticItem(cell, 6))
                {
                    services.ExplodeTopMostAboveItem(cell, data);
                    continue;
                }
                var item = cell.Mediation.CurrentItem as OriginalMatchBoardItem;
                if (item == null) continue;
                item.SpecialItemCreationData = special;
                item.CollectData = collect;
                services.ExplodeMatchItem(item, data);
                if (observeSynthesis && removedCell == null && !ReferenceEquals(cell.Mediation.CurrentItem, item))
                    removedCell = cell;
                minX = Math.Min(minX, cell.X); maxX = Math.Max(maxX, cell.X);
                minY = Math.Min(minY, cell.Y); maxY = Math.Max(maxY, cell.Y);
                destroyed.Add(cell);
            }
            var creationCell = special.GetCreationCell();
            if (creationCell == null)
            {
                OriginalCellPoint origin;
                if (data.Trigger == 2) origin = data.Point;
                else
                {
                    // Native chooses one axis based on whether the destroyed
                    // cells share a row. Int32 sums wrap; division truncates.
                    if (minY == maxY) minX = unchecked(minX + maxX) / 2;
                    else maxY = unchecked(minY + maxY) / 2;
                    origin = new OriginalCellPoint(minX, maxY);
                }
                var cell = services.GetCell(origin);
                if (cell != null) services.TryCollectMadness(cell, group.MatchType, group.Cells.Count, 5);
                DestroyTouchingItems(destroyed, group.MatchType);
                services.PlayMatchExplode(data.Trigger == 2);
            }
            else
            {
                services.CreateSpecialItemForMatchGroup(group, creationCell, isFromAutoMatch);
                DestroyTouchingItems(destroyed, group.MatchType);
            }
            services.PlayHaptic(25, 1, 1f, 0f, false);
            if (removedCell != null) recordSynthesis?.Invoke((creationCell ?? removedCell).GetViewPosition());
            return true;
        }
        public void DestroyTouchingItems(IReadOnlyList<OriginalGridCell> destroyed, int matchType) // 0x12e416c.
        {
            var data = OriginalExploder.NextWithMatchType(5, matchType);
            if (destroyed == null) throw new NullReferenceException();
            for (int i = 0; i < destroyed.Count; i++)
            {
                var cell = destroyed[i];
                if (cell == null) throw new NullReferenceException();
                data.Point = new OriginalCellPoint(cell.X, cell.Y);
                for (int d = 0; d < touchingDirections.Length; d++)
                {
                    var neighbor = cell.Neighbors.Get(touchingDirections[d]);
                    if (neighbor != null) services.ExplodeCurrentItemByNearMatch(neighbor, data);
                }
            }
        }
    }
}
