using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard : IOriginalTntRocketComboServices
    {
        private readonly List<OriginalTntRocketAnimation> tntRocketAnimations = new List<OriginalTntRocketAnimation>(4);
        public event Action<OriginalRocketBoardItem> OnComboRocketCreated;
        public void StartComboOperation() => board.State.StartSpecialOperation();
        public void FinishComboOperation() => board.State.FinishSpecialOperation();
        public void AnimateTntRocket(OriginalGridCell center, OriginalGridCell empty, OriginalSpecialBoardItem to,
            OriginalSpecialBoardItem from, int swipe, Action completed) =>
            tntRocketAnimations.Add(new OriginalTntRocketAnimation(this, board.Presentation, center, empty, to, from, swipe, completed));
        private void TickBoardAnimations()
        {
            float deltaTime = board.Clock.DeltaTime;
            audio.Playback.TickFadingSounds(deltaTime);
            BoosterTweenUpdate?.Invoke();
            TickBallBallAnimations();
            if (goalPanel != null) goalPanel.ManualUpdate(deltaTime);
            TickCoinFlips(deltaTime);
            TickMailAnimations(deltaTime);
            for (int i = 0; i < tntRocketAnimations.Count;)
            {
                var animation = tntRocketAnimations[i]; animation.Tick(deltaTime);
                if (animation.IsComplete) tntRocketAnimations.RemoveAt(i); else i++;
            }
            board.LocalMoves.TickLevelMoves(deltaTime);
        }
        public void MarkTntRocketCells(OriginalGridCell center, OriginalExplodeData horizontal, OriginalExplodeData vertical)
        {
            // The supported handlers (e.g. Box at 0x2bc5ddc) record these IDs
            // only when the level contains Jelly (static type 3).
        }
        public OriginalRocketBoardItem CreateComboRocketAt(int tiledId, OriginalCellPoint point)
        {
            // Native ItemCreator.CreateItemAt: detached view/model, including
            // outside-grid coordinates. Never replace a current cell occupant.
            int id = OriginalBoardItem.StaticId; OriginalBoardItem.StaticId = unchecked(id + 1);
            var item = new OriginalRocketBoardItem(id, tiledId == 30 ? 0 : 1, false, tiledId, () => board.Clock.Time, this);
            item.CreateView(board.Presentation, board.BoardRoot, new Vector3(point.X, point.Y, 0f), this);
            item.GoalType = OriginalGoalTypes.FromItemType(item.ItemType);
            item.BindExplosion(this); item.BindTargetScoreContext(guidedTargetServices); item.BindGoalServices(board.ItemGoals);
            item.CreateFallComponent();
            ((OriginalFallComponentState)item.FallComponent).BindMotionServices(board.FallMotion);
            item.FallView = new OriginalSpecialItemFallView(item, ItemSortingLayer);
            board.LocalMoves.RegisterTarget(item.ItemView.transform);
            OnComboRocketCreated?.Invoke(item);
            return item;
        }
        public void RecycleComboViews(OriginalTntBoardItem tnt, OriginalRocketBoardItem rocket)
        { board.Presentation.Pool.Recycle(tnt.ItemView); board.Presentation.Pool.Recycle(rocket.ItemView); }
    }
}
