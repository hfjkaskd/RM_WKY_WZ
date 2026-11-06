using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard
    {
        private const int CoinGoal = 21;
        private const int CoinFlightOperation = 5;
        private const float MaxCoinBatchSpread = .2f;
        private List<CoinFlight> queuedCoins;
        private List<OriginalCoinItemSpriteView> coinFlips;
        private Action<bool> setCoinOrientation;
        private int activeCoinFlights, coinUiLayer;

        private void PrepareBoardCoinCollection()
        {
            int count = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
                if (board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalCoinBoardItem) count++;
            board.Presentation.PrepareBoardCoinPools(count);
            coinUiLayer = UiSortingLayer;
            queuedCoins = new List<CoinFlight>(board.Grid.Width * board.Grid.Height);
            coinFlips = new List<OriginalCoinItemSpriteView>(board.Grid.Width * board.Grid.Height);
            host.LateUpdates.RegisterToLateUpdate(AssignCoinFlightTiming);
        }

        private void ReleaseBoardCoinCollection()
        {
            if (queuedCoins == null) return;
            host.LateUpdates.UnregisterFromLateUpdate(AssignCoinFlightTiming);
            queuedCoins.Clear();
            coinFlips.Clear();
            setCoinOrientation = null;
        }

        internal void CreateBoardCoinView(OriginalCoinBoardItem item, OriginalSwapPoint position)
        {
            var view = board.Presentation.Pool.Spawn<OriginalCoinItemSpriteView>(68, true);
            view.Initialize(item, board.BoardRoot, position, ItemSortingLayer, board);
            item.BindView(view, this);
            board.LocalMoves.RegisterTarget(view.transform);
        }

        internal void PlayBoardCoinTap(OriginalCoinBoardItem item)
        {
            PlaySfx(88, .04f);
            item.ItemView.PlayTap(item.Mediator.CurrentCell.Owner.Y - board.Layout.CurrentBoard.Bottom);
        }

        internal void StartBoardCoinCollect(OriginalCoinItemSpriteView view) // 0x242d140 and 0x21f3c5c.
        {
            if (activeCoinFlights++ == 0) setCoinOrientation(false);
            State.Operations.OperationStart(CoinFlightOperation);
            PlaySfx(84, .04f);
            board.Presentation.Particles.GetGlobalParticle<OriginalCoinBurstParticles>(69).Play(view.transform.position);
            view.PrepareBoardFlight();
            queuedCoins.Add(new CoinFlight(view, view.transform.position));
            ManualStartCoroutine(view.FlyToGoal(goalPanel.GetGoalPosition(CoinGoal), activeCoinFlights,
                board.Clock, coinUiLayer, this, () => FinishBoardCoinCollect(view)));
            board.Goals.FlyingToBeCollected(CoinGoal);
        }

        internal void IncreaseBoardCoinGoal() => board.Goals.IncreaseGoal(CoinGoal);

        internal void QueueCoinFlip(OriginalCoinItemSpriteView view) => coinFlips.Add(view);

        private void TickCoinFlips(float deltaTime)
        {
            if (coinFlips == null || coinFlips.Count == 0 || deltaTime <= 0f) return;
            int count = coinFlips.Count;
            int pending = 0;
            for (int i = 0; i < count; i++)
            {
                var view = coinFlips[i];
                if (!view.TickFlightFlip(deltaTime)) coinFlips[pending++] = view;
            }
            if (pending < count) coinFlips.RemoveRange(pending, count - pending);
        }

        // CoinCollectHelper 0x21f6a4c. Simultaneous collections are sorted and
        // assigned their duration and animator offsets together in LateUpdate.
        private void AssignCoinFlightTiming()
        {
            int count = queuedCoins.Count;
            if (count == 0) return;
            queuedCoins.Sort();
            float top = queuedCoins[0].Start.y;
            float bottom = queuedCoins[count - 1].Start.y;
            float range = top == bottom ? 1f : top - bottom;
            for (int i = 0; i < count; i++)
            {
                var coin = queuedCoins[i];
                float delay = (MaxCoinBatchSpread / count) * i;
                float time = Mathf.Clamp01((coin.Start.y - bottom) / range) * MaxCoinBatchSpread;
                coin.View.SetBoardFlightTiming(delay, time);
            }
            queuedCoins.Clear();
        }

        private void FinishBoardCoinCollect(OriginalCoinItemSpriteView view) // 0x242d20c.
        {
            coinFlips.Remove(view);
            if (--activeCoinFlights == 0) setCoinOrientation(true);
            board.Goals.IncreaseGoalUi(CoinGoal);
            State.Operations.OperationFinish(CoinFlightOperation);
            Pool.Recycle(view);
        }

        private readonly struct CoinFlight : IComparable<CoinFlight>
        {
            public readonly OriginalCoinItemSpriteView View;
            public readonly Vector3 Start;
            public CoinFlight(OriginalCoinItemSpriteView view, Vector3 start)
            { View = view; Start = start; }
            public int CompareTo(CoinFlight other)
            {
                int y = other.Start.y.CompareTo(Start.y);
                return y == 0 ? Start.x.CompareTo(other.Start.x) : y;
            }
        }
    }
}
