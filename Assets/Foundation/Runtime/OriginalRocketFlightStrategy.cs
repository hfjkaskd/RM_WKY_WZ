using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public abstract partial class OriginalRocketCreationStrategy
    {
        protected IOriginalRocketFlightServices Flight => (IOriginalRocketFlightServices)services;
        public abstract void Play(OriginalExplodeData data, OriginalGridCell cell,
            List<OriginalCellPoint> first, List<OriginalCellPoint> second);
        protected OriginalRocketItemParticles GetParticles(Transform parent, Vector3 direction) // 0x27bcc4c.
        {
            var particles = Flight.SpawnRocketParticles();
            particles.SetParticlePosition(direction); particles.Play();
            var root = particles.transform;
            root.SetParent(parent); root.localPosition = Vector3.zero;
            root.localScale = new Vector3(1f, 1f, 0f); // Native Vector2.one is promoted with z=0.
            return particles;
        }
        protected OriginalGridCell GetCell(OriginalCellPoint point) => Flight.Grid.GetCell(point.X, point.Y);
        protected static bool IsDefault(OriginalCellPoint point) => point.X == -1 && point.Y == -1;
        protected static bool SamePoint(OriginalCellPoint a, OriginalCellPoint b) => a.X == b.X && a.Y == b.Y;
    }

    public sealed partial class OriginalHorizontalRocketCreationStrategy
    {
        private int finishedPartCount;
        private readonly List<OriginalGridCell> explodedCells = new List<OriginalGridCell>(10);
        public override void Play(OriginalExplodeData data, OriginalGridCell cell,
            List<OriginalCellPoint> first, List<OriginalCellPoint> second) // 0x27bc6a8.
        {
            services.PlaySfx(assets.useSound, assets.useSoundOffset);
            Flight.RocketCoroutines.ManualStartCoroutine(PlayHorizontalRocket(data, cell, first, second));
        }
        private IEnumerator PlayHorizontalRocket(OriginalExplodeData data, OriginalGridCell cell,
            List<OriginalCellPoint> left, List<OriginalCellPoint> right) // 0x27bd7cc.
        {
            view.BaseRenderer.gameObject.SetActive(false); view.HorizontalAnimation.gameObject.SetActive(false);
            OriginalRocketItemParticles firstParticles, secondParticles;
            float leftDuration = StartPart(data, cell, left, view.FirstPart.transform, false, out firstParticles);
            float rightDuration = StartPart(data, cell, right, view.SecondPart.transform, true, out secondParticles);
            finishedPartCount = 0; explodedCells.Clear();
            float elapsed = 0f, wait = Mathf.Max(leftDuration, rightDuration) + assets.recycleTail;
            while (elapsed < wait && !Flight.State.IsNoGame())
            { elapsed += Flight.RocketDeltaTime; yield return null; }
            view.Recycle(); services.Pool.Recycle(firstParticles); services.Pool.Recycle(secondParticles);
        }
        private float StartPart(OriginalExplodeData data, OriginalGridCell center, List<OriginalCellPoint> cells,
            Transform part, bool right, out OriginalRocketItemParticles particles) // 0x27bc8b4 / 0x27bcd9c.
        {
            part.gameObject.SetActive(true);
            Vector3 start = part.position;
            float sign = right ? 1f : -1f;
            var target = new Vector3(sign * (Flight.CameraWidth * .5f + assets.horizontalScreenMargin), start.y, 0f);
            float unfreeze = isWideOrLandscapeDevice ? sign * assets.wideDeviceUnfreezeX : target.x;
            Flight.RocketCoroutines.ManualStartCoroutine(MoveToGlobalAndDestroy(data, part, target, unfreeze, center, cells));
            particles = GetParticles(part, right ? Vector3.left : Vector3.right);
            return Vector3.Distance(start, target) / assets.speed;
        }
        private IEnumerator MoveToGlobalAndDestroy(OriginalExplodeData data, Transform part, Vector3 target,
            float unfreezeX, OriginalGridCell center, List<OriginalCellPoint> cells) // 0x27bd1bc.
        {
            view.RocketPartStart();
            bool hasUnfrozenCells = false;
            yield return null;
            bool allCellsExploded = false;
            var lastCell = cells.Count == 0 ? OriginalCellPoint.Default : cells[cells.Count - 1];
            OriginalGridCell lastExplodedCell = null;
            float direction = target.x < part.position.x ? -1f : 1f, speed = assets.speed;
            center?.FreezeFall();
            var checkedJelly = new bool[9];
            while (true)
            {
                if (isWideOrLandscapeDevice && !hasUnfrozenCells && direction * (unfreezeX - part.position.x) < 0f)
                {
                    hasUnfrozenCells = true; center?.UnFreezeFall(); TryUnfreezeAll();
                }
                if (direction * (target.x - part.position.x) < 0f)
                {
                    if (!isWideOrLandscapeDevice || !hasUnfrozenCells) { center?.UnFreezeFall(); TryUnfreezeAll(); }
                    if (!allCellsExploded) view.RocketPartEnd();
                    yield break;
                }
                var position = part.position;
                // Match the native float32 multiply/add boundaries so Mono
                // and IL2CPP cross a cell's hit threshold on the same frame.
                float distance = (float)(Flight.RocketDeltaTime * speed);
                float offset = (float)(distance * direction);
                position.x = (float)(position.x + offset); part.position = position;
                speed += assets.accelerationPerFrame;
                if (!allCellsExploded)
                {
                    if (cells.Count == 0) { allCellsExploded = true; view.RocketPartEnd(); }
                    else for (int i = 0; i < cells.Count; i++)
                    {
                        var cell = GetCell(cells[i]);
                        if (Mathf.Abs(cell.GetViewPosition().x - part.position.x) < assets.hitDistance)
                        {
                            if (!checkedJelly[i]) checkedJelly[i] = true;
                            // Native repeats hits while a cell is inside the
                            // projectile window. Per-item exploder IDs own damage
                            // deduplication; every freeze has a matching release.
                            cell.FreezeFall(); explodedCells.Add(cell);
                            cell.ExplodeCurrentItem(data); lastExplodedCell = cell;
                        }
                        else if (!IsDefault(lastCell) && ReferenceEquals(lastExplodedCell, GetCell(lastCell)))
                        { allCellsExploded = true; view.RocketPartEnd(); break; }
                    }
                }
                yield return null;
            }
        }
        private void TryUnfreezeAll() // 0x27bd040.
        {
            finishedPartCount++;
            if (finishedPartCount < 2) return;
            for (int i = 0; i < explodedCells.Count; i++) explodedCells[i].UnFreezeFall();
        }
    }

    public partial class OriginalVerticalRocketCreationStrategy
    {
        public override void Play(OriginalExplodeData data, OriginalGridCell cell,
            List<OriginalCellPoint> first, List<OriginalCellPoint> second) // 0x27be02c.
        {
            services.PlaySfx(assets.useSound, assets.useSoundOffset);
            Flight.RocketCoroutines.ManualStartCoroutine(PlayVerticalRocket(data, first, second, cell));
        }
        private IEnumerator PlayVerticalRocket(OriginalExplodeData data, List<OriginalCellPoint> top,
            List<OriginalCellPoint> bottom, OriginalGridCell center) // 0x27bec14.
        {
            for (int i = 0; i < top.Count; i++) GetCell(top[i])?.CrossFallFreeze();
            for (int i = 0; i < bottom.Count; i++) GetCell(bottom[i])?.CrossFallFreeze();
            center?.CrossFallFreeze();
            view.BaseRenderer.gameObject.SetActive(false); view.VerticalAnimation.gameObject.SetActive(false);
            Action complete = () =>
            {
                for (int i = 0; i < top.Count; i++) GetCell(top[i])?.CrossFallUnFreeze();
                for (int i = 0; i < bottom.Count; i++) GetCell(bottom[i])?.CrossFallUnFreeze();
                center?.CrossFallUnFreeze();
            };
            var first = view.FirstPart.transform; first.gameObject.SetActive(true);
            Vector3 firstStart = first.position;
            var firstTarget = new Vector3(firstStart.x, Flight.CameraHeight * .5f + assets.verticalScreenMargin, 0f);
            Flight.RocketCoroutines.ManualStartCoroutine(MoveToGlobalAndDestroy(data, first, firstTarget, top, center,
                bottom.Count <= top.Count ? complete : null));
            var firstParticles = GetParticles(first, Vector3.down);
            var second = view.SecondPart.transform; second.gameObject.SetActive(true);
            Vector3 secondStart = second.position;
            float targetY = -Flight.CameraHeight * .5f - assets.verticalScreenMargin;
            if (Flight.RocketLayout.Count >= 2) targetY *= Flight.RocketLayout.CurrentBoardIndex + 1;
            var secondTarget = new Vector3(secondStart.x, targetY, 0f);
            Flight.RocketCoroutines.ManualStartCoroutine(MoveToGlobalAndDestroy(data, second, secondTarget, bottom, center,
                bottom.Count > top.Count ? complete : null));
            var secondParticles = GetParticles(second, Vector3.up);
            float wait = Mathf.Max(Vector3.Distance(firstStart, firstTarget) / assets.speed,
                Vector3.Distance(secondStart, secondTarget) / assets.speed) + assets.recycleTail;
            float elapsed = 0f;
            while (elapsed < wait && !Flight.State.IsNoGame())
            { elapsed += Flight.RocketDeltaTime; yield return null; }
            view.Recycle(); services.Pool.Recycle(firstParticles); services.Pool.Recycle(secondParticles);
        }
        private IEnumerator MoveToGlobalAndDestroy(OriginalExplodeData data, Transform part, Vector3 target,
            List<OriginalCellPoint> cells, OriginalGridCell center, Action onComplete) // 0x27be674.
        {
            view.RocketPartStart(); yield return null;
            bool allCellsExploded = false;
            var lastCell = cells.Count == 0 ? OriginalCellPoint.Default : cells[cells.Count - 1];
            OriginalGridCell lastFrozenCell = null;
            float direction = target.y < part.position.y ? -1f : 1f, speed = assets.speed;
            var checkedJelly = new bool[11];
            while (true)
            {
                if (direction * (target.y - part.position.y) < 0f)
                {
                    if (!allCellsExploded) { lastFrozenCell?.UnFreezeFall(); view.RocketPartEnd(); }
                    onComplete?.Invoke(); yield break;
                }
                float oldSpeed = speed; speed += assets.accelerationPerFrame;
                var position = part.position;
                float distance = (float)(Flight.RocketDeltaTime * oldSpeed);
                float offset = (float)(distance * direction);
                position.y = (float)(position.y + offset); part.position = position;
                if (!allCellsExploded)
                {
                    if (cells.Count == 0) { allCellsExploded = true; view.RocketPartEnd(); }
                    else for (int i = 0; i < cells.Count; i++)
                    {
                        var cell = GetCell(cells[i]);
                        if (Mathf.Abs(cell.GetViewPosition().y - part.position.y) < assets.hitDistance)
                        {
                            if (!checkedJelly[i]) checkedJelly[i] = true;
                            if (!ReferenceEquals(lastFrozenCell, cell))
                            { lastFrozenCell?.UnFreezeFall(); lastFrozenCell = cell; cell.FreezeFall(); }
                            cell.ExplodeCurrentItem(data);
                        }
                        else if (lastFrozenCell != null && SamePoint(new OriginalCellPoint(lastFrozenCell.X, lastFrozenCell.Y), lastCell))
                        {
                            lastFrozenCell.UnFreezeFall(); lastFrozenCell = null;
                            allCellsExploded = true; view.RocketPartEnd(); break;
                        }
                    }
                }
                yield return null;
            }
        }
    }
    public sealed partial class OriginalWaterVerticalRocketCreationStrategy
    {
        public override void Play(OriginalExplodeData data, OriginalGridCell cell,
            List<OriginalCellPoint> first, List<OriginalCellPoint> second) =>
            throw new NotSupportedException("Water rocket flight ownership is not composed. The formal loader rejects water levels.");
    }
}
