using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Owned data reset by KingAnimationStateManager.ResetBoardCheckData.
    public sealed class OriginalKingBoardCheckData
    {
        public bool IsThereLightballCombo, IsThereSingleLightball;
        public int[] MatchItemCounts;
        public Dictionary<int, HashSet<int>> ItemTypesForTotalLayer;
        public HashSet<int> MatchTypesForLightball, CheckedComboTypes;
        public void Reset() // 0x23e7540.
        {
            IsThereLightballCombo = false; IsThereSingleLightball = false;
            if (MatchItemCounts == null) throw new NullReferenceException();
            for (int i = 0; i < MatchItemCounts.Length; i++) MatchItemCounts[i] = 0;
            if (ItemTypesForTotalLayer == null) throw new NullReferenceException();
            ItemTypesForTotalLayer.Clear();
            if (MatchTypesForLightball == null) throw new NullReferenceException();
            MatchTypesForLightball.Clear();
            if (CheckedComboTypes == null) throw new NullReferenceException();
            CheckedComboTypes.Clear();
        }
    }
    public interface IOriginalKingReliefServices
    {
        bool IsWorried { get; }
        int LeftMoves { get; }
        int LastMoveLayerCapsCount { get; }
        bool IsWinPossible();
        bool IsPlaying();
        bool ShouldKingBeWorried();
        void TryStopWorry(int changeReason);
    }

    // Relief request and <CheckBoardDelayed>d__63 only. Complete King state,
    // board analysis, move subscriptions and King UI are required services.
    public sealed class OriginalKingRelief : IOriginalKingRelief
    {
        private readonly OriginalKingBoardCheckData data;
        private readonly IOriginalManualCoroutinePlayer player;
        private readonly IOriginalKingReliefServices services;
        public Coroutine ReliefCoroutine { get; private set; }
        public OriginalKingRelief(OriginalKingBoardCheckData data, IOriginalManualCoroutinePlayer player,
            IOriginalKingReliefServices services)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public void TryKingRelief() // 0x23e9220 / CheckBoardDelayed 0x23e92b4.
        {
            if (!ReferenceEquals(ReliefCoroutine, null)) return;
            ReliefCoroutine = player.ManualStartCoroutine(new CheckBoardIterator(this));
        }
        private sealed class CheckBoardIterator : IEnumerator, IDisposable // MoveNext 0x23e993c.
        {
            private readonly OriginalKingRelief owner;
            private int state;
            public object Current { get; private set; }
            public CheckBoardIterator(OriginalKingRelief owner) { this.owner = owner; }
            public bool MoveNext()
            {
                int previous = state;
                if (previous == 0) { state = 1; Current = null; return true; }
                if (previous != 1) return false;
                state = -1;
                owner.data.Reset();
                var context = owner.services;
                if (context.IsWorried)
                {
                    int moves = context.LeftMoves;
                    if (moves > 0 && moves <= context.LastMoveLayerCapsCount && context.IsWinPossible()
                        && context.IsPlaying() && !context.ShouldKingBeWorried())
                        context.TryStopWorry(5);
                }
                // Native does not clear the handle after a failed required call.
                owner.ReliefCoroutine = null;
                return false;
            }
            public void Reset() { throw new NotSupportedException(); }
            public void Dispose() { }
        }
    }
}
