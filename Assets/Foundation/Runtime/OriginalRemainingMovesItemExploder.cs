using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    // RemainingMovesItemExploder 0x1118be4..0x1119588. Traversal and clocks
    // are supplied by the live level; no synthetic board stabilization.
    public sealed class OriginalRemainingMovesItemExploder
    {
        private readonly IOriginalGridIteratorStrategy traversal;
        private readonly OriginalGameStateManager state;
        private readonly OriginalCoroutinePlayer coroutines;
        private readonly Func<float> time, delta;
        private readonly Action resetTopSorting;
        private Coroutine routine;
        private Action onComplete;
        private bool skipClicked, completeCalled;
        private float lastOperationTime;
        public bool IsComplete => completeCalled;
        public OriginalRemainingMovesItemExploder(IOriginalGridIteratorStrategy traversal,
            OriginalGameStateManager state, OriginalCoroutinePlayer coroutines, Func<float> time, Func<float> delta, Action resetTopSorting)
        {
            this.traversal = traversal ?? throw new ArgumentNullException(nameof(traversal));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.coroutines = coroutines ?? throw new ArgumentNullException(nameof(coroutines));
            this.time = time ?? throw new ArgumentNullException(nameof(time));
            this.delta = delta ?? throw new ArgumentNullException(nameof(delta));
            this.resetTopSorting = resetTopSorting ?? throw new ArgumentNullException(nameof(resetTopSorting));
        }
        public void ExplodeItemsOnBoard(Action completed) // 0x1118d40.
        {
            if (skipClicked) { completed(); return; }
            lastOperationTime = time(); completeCalled = false; onComplete = completed;
            routine = coroutines.ManualStartCoroutine(ExplodeSpecialItems());
        }
        public void SkipClicked() // 0x1118c80: completion flag written after callback, including reentrancy.
        {
            skipClicked = true;
            if (!completeCalled)
            {
                if (routine != null) { coroutines.StopCoroutine(routine); routine = null; }
                onComplete?.Invoke();
            }
            completeCalled = true;
        }
        public int CalculateExtraCoinAmountOnBoard() // 0x1118ea0: only active, unexploded special items.
        {
            int amount = 0; var iterator = new OriginalGridIterator(traversal);
            while (iterator.MoveNext() != null)
            {
                if (!(iterator.Cell.Mediation.CurrentItem is OriginalSpecialBoardItem item) || item.IsExploded || !item.IsActive) continue;
                if (item is OriginalRocketBoardItem rocket) amount = unchecked(amount + rocket.RemainingMoveCoins);
                else if (item is OriginalTntBoardItem tnt) amount = unchecked(amount + tnt.RemainingMoveCoins);
                else if (item is OriginalPropellerBoardItem propeller) amount = unchecked(amount + propeller.RemainingMoveCoins);
                else if (item is OriginalLightballBoardItem) amount = unchecked(amount + 5); // ctor 0x1f3dc60.
                else throw new NotSupportedException("Remaining coin value for this special item is not restored.");
            }
            return amount;
        }
        private IEnumerator ExplodeSpecialItems() // Original coroutine MoveNext 0x1118fd0.
        {
            var iterator = new OriginalGridIterator(traversal); // Original iterator3, TopLeftHorizontal.
            const float steady = .3f, active = .5f, searchDelay = .2f, operationFree = .4f;
            yield return new OriginalGameplayWait(.8f, delta);
            resetTopSorting();
            while (true)
            {
                while (iterator.MoveNext() != null)
                {
                    var cell = iterator.Cell;
                    if (!(cell.Mediation.CurrentItem is OriginalSpecialBoardItem item) || item.IsExploded) continue;
                    bool activeLongEnough = item.IsActive && item.GetActiveDuration() > active;
                    var fall = item.FallComponent as IOriginalItemFallTiming;
                    if (fall == null) throw new NullReferenceException();
                    bool steadyLongEnough = fall.IsItemSteady && time() - fall.StateChangedTime > steady;
                    if (!(activeLongEnough && steadyLongEnough)) continue;
                    cell.ExplodeCurrentItem(OriginalExploder.Next(22));
                    break; // Native restarts the iterator after ONE explosion.
                }
                iterator.Reset();
                if (state.HasAnyOperation()) lastOperationTime = time();
                if (time() - lastOperationTime > operationFree)
                {
                    float operationCheckStart = time();
                    while (state.HasAnyOperation())
                    {
                        // Native logs after2s but keeps waiting; it does not
                        // reset operations or force an unfinished board stable.
                        if (time() - operationCheckStart > 2f)
                            Debug.Log("Remaining-move flow is waiting for active board operations.");
                        yield return null;
                    }
                    onComplete(); completeCalled = true; yield break;
                }
                yield return new OriginalGameplayWait(searchDelay, delta);
            }
        }
    }
}
