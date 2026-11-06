using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalBallBallComboServices
    {
        OriginalGridCell GetCell(OriginalCellPoint point);
        void RecycleLightballForCombo(OriginalLightballBoardItem item);
        void StartSpecialOperation();
        void FinishSpecialOperation();
        void DisableComboTouch();
        void EnableComboTouch();
        void PlayBallBallView(Vector3 position, int swipe, Action explode);
        void ShakeBallBallGrid();
        void StartBallBallCoroutine(IEnumerator routine);
        object WaitBallBall(float seconds);
        void CollectBallBallMadness(OriginalExplodeData data, OriginalGridCell center);
    }

    // BallBallCombo 0x1e2a400..0x1e2b3c8, normal/no-jelly branch.
    // The visual callback, radial damage, freeze and touch owners retain
    // their independent native lifetimes; no all-at-once board clearing.
    public sealed class OriginalBallBallCombo
    {
        private readonly IOriginalBallBallComboServices services;
        private OriginalGridIterator iterator;
        private OriginalGridCell cellTo, cellFrom;
        // Preserve ARM64 single-precision boundaries on Mono as well.
        private float ringDelayValue;
        private readonly SortedDictionary<int,List<OriginalCellPoint>> byRadius = new SortedDictionary<int,List<OriginalCellPoint>>();
        public bool IsExplosionActive { get; private set; }
        public bool IsComboActive { get; private set; }
        public OriginalExplodeData ExplodeData { get; private set; }
        public event Action<int,OriginalCellPoint> CellTriggered;
        public event Action<int,float> RingWaitStarted;
        public OriginalBallBallCombo(IOriginalGridIteratorStrategy strategy,IOriginalBallBallComboServices services)
        { iterator = new OriginalGridIterator(strategy); this.services = services ?? throw new ArgumentNullException(nameof(services)); }
        public void Prepare(OriginalLightballBoardItem to,OriginalLightballBoardItem from)
        {
            byRadius.Clear(); cellTo = to.CurrentCell; cellFrom = from.CurrentCell;
            to.Mediator.ClearFromAllRegisteredCells(); services.RecycleLightballForCombo(to);
            // Native Prepare clears all current/target/next slots of the
            // surviving from cell, then recycles its view directly as well.
            cellFrom.Mediation.ClearAllItems(); services.RecycleLightballForCombo(from);
        }
        public void Explode() // Incoming trigger2 is not reused by the native normal explosion.
        {
            services.StartSpecialOperation(); services.DisableComboTouch(); IsComboActive = true;
            cellTo.FreezeFall(); cellFrom.FreezeFall();
            int swipe = cellFrom.X == cellTo.X ? (cellFrom.Y < cellTo.Y ? 1 : 0) : (cellFrom.X >= cellTo.X ? 3 : 2);
            services.PlayBallBallView(cellFrom.GetViewPosition(),swipe,ShakeGridAndExplodeCells);
        }
        private void ShakeGridAndExplodeCells()
        { services.ShakeBallBallGrid(); services.StartBallBallCoroutine(ExplodeCells()); }
        private IEnumerator ExplodeCells() // 0x1e2ae24.
        {
            cellTo.UnFreezeFall(); cellFrom.UnFreezeFall(); iterator.Reset(); int max = -1;
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                int dx = unchecked(cellFrom.X-cell.X), dy = unchecked(cellFrom.Y-cell.Y);
                int radius = Mathf.RoundToInt(Mathf.Sqrt((float)(unchecked(dx*dx+dy*dy))) * 2f);
                if (!byRadius.TryGetValue(radius,out var points)) { points = new List<OriginalCellPoint>(); byRadius.Add(radius,points); }
                points.Add(new OriginalCellPoint(cell.X,cell.Y)); cell.FreezeFall(); max = Math.Max(max,radius);
            }
            IsExplosionActive = true;
            ExplodeData = OriginalExploder.Next(16,false,new OriginalCellPoint(cellFrom.X,cellFrom.Y));
            yield return RadialExplode(max);
            services.CollectBallBallMadness(ExplodeData,cellFrom); IsExplosionActive = false;
            yield return null; // Native lets the explosion owner finish before releasing all cells.
            iterator.Reset(); while ((cell = iterator.MoveNext()) != null) cell.UnFreezeFall();
            services.FinishSpecialOperation(); services.EnableComboTouch(); IsComboActive = false;
        }
        private IEnumerator RadialExplode(int max) // 0x1e2bcec, no jelly in this admitted composition.
        {
            foreach (var ring in byRadius)
            {
                for (int i = 0; i < ring.Value.Count; i++)
                {
                    OriginalCellPoint point = ring.Value[i];
                    services.GetCell(point)?.ExplodeCurrentItem(ExplodeData);
                    CellTriggered?.Invoke(ring.Key,point);
                }
                if (ring.Key >= max) continue;
                ringDelayValue = .01f*ring.Key;
                ringDelayValue = ringDelayValue*ring.Key;
                ringDelayValue = 6f-Mathf.Min(ringDelayValue,1.5f);
                ringDelayValue = ringDelayValue/160f;
                RingWaitStarted?.Invoke(ring.Key,ringDelayValue); yield return services.WaitBallBall(ringDelayValue);
            }
        }
    }

}
