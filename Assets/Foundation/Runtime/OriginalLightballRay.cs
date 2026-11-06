using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalLightballRay : OriginalItemParticles
    {
        public LineRenderer colorLine, whiteLine;
        public Color[] colors;
        private OriginalDefaultLightballStrategy strategy;
        private OriginalMatchBoardItem target;
        private OriginalBoardItem explosionTarget;
        private OriginalGridCell frozenCell;
        private OriginalLightballItemBg background;
        private Coroutine rayRoutine, shakeRoutine;
        private bool ended, collected;
        public override int GetPoolId() => 26;

        public void Initialize(OriginalDefaultLightballStrategy owner, OriginalMatchBoardItem item)
        {
            strategy = owner; target = item;
            colorLine.positionCount = whiteLine.positionCount = 2;
            whiteLine.startColor = whiteLine.endColor = colors[0];
            colorLine.startColor = colorLine.endColor = colors[ColorIndex(item.MatchType)];
            var sorting = owner.GetLightballSorting();
            colorLine.sortingLayerID = whiteLine.sortingLayerID = sorting.Layer;
            colorLine.sortingOrder = sorting.Order - 3; whiteLine.sortingOrder = sorting.Order - 2;
            var config = owner.Assets;
            if (item.MatchType == 1 || item.MatchType == 5)
            { whiteLine.widthMultiplier = config.rayWhiteWidth15; colorLine.widthMultiplier = config.rayColorWidth15; }
            else if (item.MatchType == 2 || item.MatchType == 4)
            { whiteLine.widthMultiplier = config.rayWhiteWidth24; colorLine.widthMultiplier = config.rayColorWidth24; }
            else if (item.MatchType == 3)
            { whiteLine.widthMultiplier = config.rayWhiteWidth3; colorLine.widthMultiplier = config.rayColorWidth3; }
            frozenCell = item.Mediator.CurrentCell?.Owner;
            if (frozenCell != null) frozenCell.FreezeFall();
            explosionTarget = target; explosionTarget.OnExplodeEvent += EndRayRecycleSelf;
            rayRoutine = owner.Coroutines.ManualStartCoroutine(MoveAndDestroy());
        }
        private static int ColorIndex(int type)
        {
            switch (type)
            { case 0: return 0; case 1: return 1; case 2: return 2; case 3: return 4; case 4: return 6; case 5: return 5; case 6: return 3; default: throw new ArgumentOutOfRangeException(nameof(type)); }
        }
        public void CreateItemBackground(OriginalMatchItemSpriteView view)
        {
            CreateItemBackground(target.ItemType, view.BaseSpritePosition, view.RootTransform);
        }
        public void CreateItemBackground(int itemType, Vector3 position, OriginalRootTransform root)
        {
            background = strategy.CreateBackground(itemType, position, root);
            background.rayTipParticle.Play(); // Native CreateItemBg plays it after Init.
        }
        public void BindConvertedItem(OriginalBoardItem item)
        {
            explosionTarget.OnExplodeEvent -= EndRayRecycleSelf;
            explosionTarget = item; explosionTarget.OnExplodeEvent += EndRayRecycleSelf;
        }
        public void ShakeItem(OriginalRootTransform root) => shakeRoutine = strategy.Coroutines.ManualStartCoroutine(strategy.ShakeItem(root));
        private IEnumerator MoveAndDestroy() // Native 0x1f41618.
        {
            var config = strategy.Assets;
            Vector3 origin = transform.position;
            colorLine.SetPosition(0, origin); whiteLine.SetPosition(0, origin);
            Vector3 point = strategy.GetView(target).transform.position;
            float elapsed = 0f, duration = config.rayReachFrames / 60f;
            float colorWidth = colorLine.widthMultiplier, whiteWidth = whiteLine.widthMultiplier;
            bool reached = false;
            while (elapsed < duration)
            {
                elapsed += strategy.DeltaTime;
                if (!target.HasView) break;
                point = strategy.GetView(target).transform.position;
                float ratio = Mathf.Clamp01(elapsed / duration);
                float angle = ratio * 3.1415927410125732f * .5f;
                Vector3 tip = Vector3.Lerp(origin, point, (float)Math.Sin(angle));
                colorLine.SetPosition(1, tip); whiteLine.SetPosition(1, tip);
                float easing = 1f - (float)Math.Cos(angle);
                colorLine.widthMultiplier = Mathf.Lerp(colorWidth * config.rayInitialWidthScale, colorWidth, easing);
                whiteLine.widthMultiplier = Mathf.Lerp(whiteWidth * config.rayInitialWidthScale, whiteWidth, easing);
                if (!reached && Vector3.Distance(tip, point) <= config.rayReachDistance)
                {
                    reached = true;
                    if (target.HasView && target.Mediator.CurrentCell != null) strategy.SingleRayReached(this, target);
                }
                if (background != null) background.rayTipParticle.transform.position = colorLine.GetPosition(1);
                yield return null;
            }
            collected = true; strategy.RayStartedToCollect();
            float alpha = whiteLine.startColor.a;
            elapsed = 0f;
            float finish = (strategy.SpeedIncrease ? config.fastRayFinishFrames : config.rayFinishFrames) / 60f;
            float fadeStart = (strategy.SpeedIncrease ? config.fastRayFadeStartFrames : config.rayFadeStartFrames) / 60f;
            float fadeEnd = (strategy.SpeedIncrease ? config.fastRayFadeEndFrames : config.rayFadeEndFrames) / 60f;
            while (elapsed < finish)
            {
                elapsed += strategy.DeltaTime;
                float ratio = elapsed / finish;
                colorLine.SetPosition(0, Vector3.Lerp(origin, point, -.5f * ((float)Math.Cos(3.1415927410125732f * ratio) - 1f)));
                var color = whiteLine.startColor;
                color.a = Mathf.Lerp(alpha, 0f, Mathf.Clamp01((elapsed - fadeStart) / (fadeEnd - fadeStart)));
                whiteLine.startColor = whiteLine.endColor = color;
                yield return null;
            }
            colorLine.enabled = whiteLine.enabled = false;
            rayRoutine = null; strategy.SingleRayComplete();
        }
        public void EndRayRecycleSelf() => EndRay(true);
        private void EndRay(bool recycle)
        {
            if (ended) return;
            if (explosionTarget != null) explosionTarget.OnExplodeEvent -= EndRayRecycleSelf;
            ended = true;
            if (!collected && recycle) strategy.RayStartedToCollect();
            if (rayRoutine != null)
            { strategy.Coroutines.StopCoroutine(rayRoutine); if (recycle) strategy.SingleRayComplete(); }
            if (shakeRoutine != null) { strategy.Coroutines.StopCoroutine(shakeRoutine); shakeRoutine = null; }
            if (background != null) { background.RecycleSelf(); background = null; }
            if (frozenCell != null) frozenCell.UnFreezeFall();
            if (recycle) base.RecycleSelf();
        }
        public override void OnRecycle()
        {
            EndRay(false);
            colorLine.enabled = whiteLine.enabled = true;
            strategy = null; target = null; explosionTarget = null; shakeRoutine = null; background = null; frozenCell = null;
            ended = collected = false;
            base.OnRecycle();
        }
    }
}
