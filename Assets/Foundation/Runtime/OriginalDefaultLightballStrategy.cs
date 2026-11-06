using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Native DefaultLightballStrategy, normal lightball branch. Formal startup
    // currently admits only the static-free first level and inactive events.
    public partial class OriginalDefaultLightballStrategy
    {
        protected readonly OriginalInitialBoardView board;
        protected readonly OriginalPlayableBoard game;
        protected readonly OriginalGeneralLevelRandom random;
        protected readonly HashSet<int> selectedColors;
        protected readonly OriginalLightballTargeting targeting;
        private readonly List<OriginalMatchBoardItem> remove = new List<OriginalMatchBoardItem>();
        private readonly Dictionary<int, OriginalLightballRay> rays = new Dictionary<int, OriginalLightballRay>();
        private readonly OriginalCellStaticExplosionServices statics = new OriginalCellStaticExplosionServices();
        protected OriginalLightballBoardItem model;
        protected OriginalLightballUseParticles useParticles;
        private Coroutine sender;
        protected bool looking;
        private bool userAction, skipSound;
        protected int matchType, oldMatchType;
        private int incomplete, uncollected, soundIndex;
        protected float startTime;
        private float lastCollect;
        public bool DidStart { get; protected set; }
        public OriginalLightballViewAssets Assets => board.Presentation.LightballAssets;
        public OriginalCoroutinePlayer Coroutines => board.Presentation.Coroutines;
        public float DeltaTime => board.Presentation.LevelDeltaTime;
        public bool SpeedIncrease => Assets.speedIncrease;
        public virtual float PositionOffset => Assets.positionOffset;
        public virtual float MaxShakeScale => Assets.maxShakeScale;
        private float RayDelay => SpeedIncrease ? Assets.fastRayDelay : Assets.rayDelay;
        private float MinimumTime => SpeedIncrease ? .5f : 1f;
        public int SentRayCount { get; private set; }
        public int ReachedRayCount { get; private set; }
        public int CompletedRayCount { get; private set; }
        public OriginalDefaultLightballStrategy(OriginalInitialBoardView board, OriginalPlayableBoard game,
            OriginalGeneralLevelRandom random, HashSet<int> selectedColors)
        {
            this.board = board; this.game = game; this.random = random; this.selectedColors = selectedColors;
            targeting = new OriginalLightballTargeting(board.Grid, board.Layout, selectedColors, MatchItemAdded, MatchItemRemoved);
        }
        protected virtual void MatchItemAdded(OriginalMatchBoardItem item) { }
        protected virtual void MatchItemRemoved(OriginalMatchBoardItem item) { }
        protected void Initialize(OriginalLightballBoardItem item)
        {
            model = item; looking = true; DidStart = true; startTime = board.Clock.Time;
        }
        public virtual void Start(OriginalLightballBoardItem item, OriginalBoardItem other, OriginalExplodeData data)
        {
            Initialize(item);
            int color = other is OriginalMatchBoardItem match ? match.MatchType : 0;
            userAction = data.Trigger == 2 || data.Trigger == 3;
            if (userAction)
            {
                if (!SelectMatchTypeAndStart(color, data.Trigger == 3)) { Reset(); return; }
            }
            else
            {
                board.Moves.CompleteMoveByType(6);
                Coroutines.ManualStartCoroutine(SelectAfterDelay(color));
            }
            StartShakingLightball(other);
            model.CurrentCell.FreezeFall();
            game.TouchListener.DisableTouch();
            model.IsPreparedForSpecialUse = true;
        }
        protected void StartShakingLightball(OriginalBoardItem other)
        {
            useParticles = board.Presentation.SpawnLightballUseParticles();
            useParticles.Play(game.ItemSortingLayer);
            useParticles.transform.SetParent(model.ItemView.rootTransform.transform);
            useParticles.transform.localPosition = Vector3.zero;
            model.ItemView.PlayUseAnimation(other == null ? 0 : other.ItemType);
        }
        private IEnumerator SelectAfterDelay(int color)
        {
            float delay = SpeedIncrease ? .1f : .5f;
            yield return game.Wait(delay);
            if (SpeedIncrease && .5f - delay > 0f && color == 0 && targeting.FindMostCommonItem() == 0)
                yield return game.Wait(.5f - delay);
            SelectMatchTypeAndStart(color, false);
        }
        private bool SelectMatchTypeAndStart(int color, bool tap)
        {
            matchType = color == 0 ? targeting.FindMostCommonItem() : color;
            targeting.FindItemsByType(matchType);
            if (targeting.Items.Count == 0)
            {
                if (tap) return false;
                AllRaysComplete(); return true;
            }
            random.Shuffle(targeting.Items);
            StartSendingRays();
            return true;
        }
        protected void StartSendingRays()
        {
            selectedColors.Add(matchType);
            sender = Coroutines.ManualStartCoroutine(SendAllRays());
        }
        private bool CanSend(OriginalMatchBoardItem item)
        {
            if (rays.ContainsKey(item.Id)) return false;
            var cell = item.Mediator.CurrentCell?.Owner;
            if (cell != null && !OriginalLightballTargeting.IsReadyForLightball(cell)) { remove.Add(item); return false; }
            if (item.HasView && cell != null) return true;
            rays.Add(item.Id, null); return false;
        }
        private IEnumerator SendAllRays() // Native 0x212e1e4.
        {
            float lastSend = board.Clock.Time;
            while (true)
            {
                float elapsed = board.Clock.Time - lastSend;
                if (looking && elapsed > RayDelay) targeting.FindItemsByType(matchType);
                remove.Clear();
                OriginalMatchBoardItem steady = null, above = null, below = null;
                for (int i = 0; i < targeting.Items.Count; i++)
                {
                    var item = targeting.Items[i]; if (!CanSend(item)) continue;
                    var cell = item.Mediator.CurrentCell.Owner;
                    if (((OriginalFallComponentState)item.FallComponent).IsItemSteady)
                    {
                        if (steady == null || !steady.Mediator.CurrentCell.Owner.IsNormalCell && cell.IsNormalCell) steady = item;
                    }
                    else if (steady != null) continue;
                    else if (GetView(item).transform.position.y - cell.GetViewPosition().y > -.001f)
                    {
                        if (above == null || !above.Mediator.CurrentCell.Owner.IsNormalCell && cell.IsNormalCell) above = item;
                    }
                    else if (below == null && cell.IsNormalCell) below = item;
                }
                for (int i = 0; i < remove.Count; i++) targeting.RemoveFromItems(remove[i]);
                remove.Clear();
                if (steady != null && elapsed >= RayDelay)
                { MoveRay(steady); lastSend = board.Clock.Time; }
                else if (above != null && elapsed >= RayDelay * 2f && (below == null || above.Mediator.CurrentCell.Owner.IsNormalCell))
                { MoveRay(above); lastSend = board.Clock.Time; }
                else if (elapsed > .2f && uncollected == 0 && board.Clock.Time - lastCollect > .1f
                    && (!SpeedIncrease || board.Clock.Time - startTime >= MinimumTime))
                {
                    if (looking) { StoppedLookingForNewItems(); looking = false; }
                    if (incomplete == 0)
                    {
                        Coroutines.StopCoroutine(sender);
                        AllRaysComplete();
                    }
                }
                yield return null;
            }
        }
        protected virtual void StoppedLookingForNewItems() { } // Default native strategy has no completion work here.
        private void MoveRay(OriginalMatchBoardItem item)
        {
            item.SelectByLightball();
            if (!skipSound) // 0x212d794: skipped rays keep the next sound index.
            {
                game.PlaySfx(216 + soundIndex, .04f);
                soundIndex = (soundIndex + 1) % 3;
            }
            skipSound = SpeedIncrease && !skipSound;
            incomplete++; uncollected++;
            var ray = board.Presentation.SpawnLightballRay();
            ray.transform.position = model.ItemView.transform.position;
            ray.Initialize(this, item); rays.Add(item.Id, ray); SentRayCount++;
        }
        public OriginalMatchItemSpriteView GetView(OriginalMatchBoardItem item) => (OriginalMatchItemSpriteView)item.SwapView;
        public OriginalSwapSorting GetLightballSorting() => model.ItemView.GetSorting();
        public OriginalSwapSorting GetUseSorting() => new OriginalSwapSorting(game.ItemSortingLayer, 11010 + selectedColors.Count * 10, false);
        public void RayStartedToCollect() { uncollected--; lastCollect = board.Clock.Time; }
        public void SingleRayComplete() { incomplete--; CompletedRayCount++; }
        protected void RecordRayReached() { ReachedRayCount++; }
        public virtual void SingleRayReached(OriginalLightballRay ray, OriginalMatchBoardItem item)
        {
            var view = GetView(item);
            view.UpdateSortingForAnimation(new OriginalSwapSorting(game.ItemSortingLayer, 11000, false), false); // 0x12d91b4.
            ray.CreateItemBackground(view); ray.ShakeItem(view.RootTransform); ReachedRayCount++;
        }
        public OriginalLightballItemBg CreateBackground(int itemType, Vector3 position, OriginalRootTransform parent)
        {
            var bg = board.Presentation.SpawnLightballBackground(itemType, matchType, new OriginalSwapSorting(game.ItemSortingLayer, 4999, false)); // 0x12d9218.
            parent.AddChild(bg.transform); bg.transform.position = position;
            return bg;
        }
        protected virtual void AllRaysComplete() => Coroutines.ManualStartCoroutine(CompleteDelayed(), AnimationCompleted);
        private IEnumerator CompleteDelayed() // 0x1f45c74, source first-level branch.
        {
            float wait = MinimumTime - (board.Clock.Time - startTime);
            if (wait > 0f) yield return game.Wait(wait);
            var data = OriginalExploder.NextWithMatchType(15, matchType);
            var near = OriginalExploder.NextWithMatchType(5, matchType);
            for (int i = 0; i < targeting.Items.Count; i++) targeting.Items[i].UnselectByLightball();
            for (int i = 0; i < targeting.Items.Count; i++)
            {
                var item = targeting.Items[i]; var cell = item.Mediator.CurrentCell?.Owner;
                if (cell == null) continue;
                // Chained and other above-static branches are rejected by the
                // formal first-level loader; their full ownership is separate.
                if (cell.StaticTouch.AboveItems.Count != 0) throw new NotSupportedException("Lightball static-item completion is not composed.");
                board.Presentation.PlayLightballEffectedExplosion(GetView(item).transform.position, item.MatchType);
                ExplodeTouching(cell, near); cell.FreezeFallForDuration(.3f); cell.ExplodeCurrentItem(data);
            }
            var center = model.CurrentCell;
            center.FreezeFallForDuration(.3f); center.UnFreezeFall();
            ExplodeTouching(center, near); center.StaticTouch.ExplodeTopMostAboveItem(data);
            Vector3 position = center.GetViewPosition();
            model.ExplodeSelf();
            game.Session.TryCollectMadness(matchType, new OriginalSwapPoint(position.x, position.y, position.z), targeting.Items.Count, 5, null, false);
            board.State.FinishSpecialOperation();
            board.Moves.CompleteMoveByType(userAction ? 5 : 7);
        }
        protected void AnimationCompleted()
        {
            selectedColors.Remove(matchType); selectedColors.Remove(oldMatchType);
            game.TouchListener.EnableTouch(); Reset();
        }
        protected virtual void Reset()
        {
            targeting.Items.Clear(); rays.Clear(); model = null; DidStart = false; matchType = 0; lastCollect = 0;
            startTime = 0; skipSound = false; incomplete = uncollected = 0; looking = false;
            if (sender != null) { Coroutines.StopCoroutine(sender); sender = null; }
            if (useParticles != null) { useParticles.StopEmitting(); useParticles = null; }
        }
        public IEnumerator ShakeItem(OriginalRootTransform root) // 0x212f150.
        {
            float speed = Assets.scaleSpeed;
            Vector3 initial = root.GetPosition(), start = initial;
            Vector3 target = RandomPosition(initial); float elapsed = 0f;
            while (true)
            {
                elapsed += DeltaTime;
                var scale = root.GetScale();
                if (scale.x > MaxShakeScale || Approximately(scale.x, MaxShakeScale)) speed = -speed;
                else if (scale.x < Assets.minShakeScale || Approximately(scale.x, Assets.minShakeScale)) speed = Assets.scaleSpeed;
                float value = Mathf.Clamp(scale.x + speed * DeltaTime, Assets.minShakeScale, MaxShakeScale);
                root.SetScale(new Vector3(value, value, scale.z));
                var position = root.GetPosition();
                // Native Vector3 Approximately 0x2d7bd64 checks x/y only,
                // with strict bounds; z remains the authored initial value.
                if (Approximately(position.x, target.x) && Approximately(position.y, target.y))
                { start = target; target = RandomPosition(initial); elapsed = 0f; }
                root.SetPosition(Vector3.Lerp(start, target, elapsed / Assets.positionOffsetDuration));
                yield return null;
            }
        }
        private Vector3 RandomPosition(Vector3 initial) => new Vector3(initial.x + (UnityEngine.Random.value * 2f - 1f) * PositionOffset,
            initial.y + (UnityEngine.Random.value * 2f - 1f) * PositionOffset, initial.z);
        private bool Approximately(float value, float target) => value > target - Assets.shakeTolerance && value < target + Assets.shakeTolerance;
    }
}
