using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable]
    public struct OriginalMatchHintTiming
    {
        public int stableFrames, initialCycleFrames;
        public float moveDistance, forwardSeconds, returnSeconds, pauseSeconds, repeatSeconds;
        public float comboDistance, comboForwardSeconds, comboReturnSeconds, tapScale, tapPulseSeconds, tapPauseSeconds;
        public float stopSeconds;
        public int tapHintLevelCutoff, tapHintTapLimit;
    }

    public sealed class OriginalMatchHintManager : IDisposable
    {
        private readonly OriginalInitialBoardView board;
        private readonly OriginalPlayableBoard game;
        private readonly CoreLevelProgress progress;
        private readonly OriginalBoardPossibleMoveServices eligibility;
        private readonly OriginalMatchHintTiming timing;
        private readonly OriginalGeneralLevelRandom random;
        private readonly string particlesPrefabPath;
        private readonly List<Hint> candidates = new List<Hint>();
        private readonly HashSet<int> tried = new HashSet<int>();
        private readonly HighlightedItem[] highlighted = new HighlightedItem[14];
        private readonly OriginalHintParticles[] particles = new OriginalHintParticles[14];
        private OriginalGridIterator iterator;
        private Hint active;
        private HintPose moving, second;
        private IOriginalItemFallSortingSurface raisedSorting;
        private OriginalSwapSorting hintSorting;
        private Vector3 direction;
        private int stableAt = -1, firstCycleAt = -1, index = -1;
        private int pulses, bursts, highlightedCount, particleCount;
        private float phaseElapsed, phaseDuration, nextAt;
        private bool searchFinished, hasShown, hadHint;
        private Phase phase, nextPhase;

        private enum Phase { None, PrepareForward, Interpolate, PrepareReturn, PrepareWait, Pause, Fade, StopRequested, StopComplete }
        private bool IsStopping => phase == Phase.StopRequested || phase == Phase.StopComplete ||
            (phase == Phase.Interpolate && nextPhase == Phase.StopComplete);
        private struct HintPose
        {
            private readonly OriginalBoardItem item;
            private readonly OriginalItemSwapView view;
            private readonly OriginalRootTransform root;
            private readonly bool scale;
            private readonly int poolUse;
            private readonly Vector3 rest;
            private Vector3 applied, interpolationStart, interpolationEnd;
            private bool released;

            public HintPose(OriginalBoardItem item, OriginalItemSwapView view, bool scale)
            {
                this.item = item;
                this.view = view;
                this.scale = scale;
                root = view.HintRoot;
                poolUse = view.PoolUse;
                rest = applied = scale ? root.GetScale() : root.GetPosition();
                interpolationStart = interpolationEnd = rest;
                released = false;
            }

            public bool IsLive => view != null && view.PoolUse == poolUse && view.gameObject.activeInHierarchy;
            public bool IsSwapping => view != null && view.IsSwapping;
            public OriginalGridCell Cell => item?.Mediator.CurrentCell?.Owner;
            public bool Owns(OriginalBoardItem model) => model != null && item == model;

            public Vector3 Current => scale ? root.GetScale() : root.GetPosition();
            public Vector3 InterpolationEnd => interpolationEnd;
            public void Prepare(Vector3 start, Vector3 end)
            {
                interpolationStart = start;
                interpolationEnd = end;
            }
            public void SetPosition(Vector3 value) => Write(value);
            public void Sample(float progress) => Write(Vector3.Lerp(interpolationStart, interpolationEnd, progress));
            public void Restore() => Write(rest);

            private void Write(Vector3 value)
            {
                // Fall, merge and activation may take over a root during the
                // stop. A recycled or independently animated view owns its pose.
                if (released || !IsLive || (scale ? root.GetScale() : root.GetPosition()) != applied)
                {
                    released = true;
                    return;
                }
                if (scale) root.SetScale(value);
                else root.SetPosition(value);
                applied = value;
            }
        }

        private struct HighlightedItem
        {
            public OriginalBoardItem Model;
            public OriginalMatchItemSpriteView View;
            public int PoolUse;
            public float InterpolationStart, InterpolationEnd;
            public bool IsLive => View != null && View.PoolUse == PoolUse && View.gameObject.activeInHierarchy;
        }
        private enum Kind { Match, Combo, Tap }
        private readonly struct Hint
        {
            public readonly OriginalGridCell From, To;
            public readonly OriginalMatchCells Cells;
            public readonly Kind Type;
            public readonly int Score, Order, TapDamage;

            public Hint(OriginalGridCell from, OriginalGridCell to, OriginalResolvedMatchGroup group, int order)
            {
                From = from;
                To = to;
                Cells = group.CaptureSnapshot().Cells;
                Type = Kind.Match;
                Order = order;
                TapDamage = 0;
                // Native Hint.PatternScore table at 0x404078c.
                Score = group.PatternType switch { 1 => 1, 2 => 4, 3 or 4 => 3, 5 or 6 => 8, 7 => 11, _ => 0 };
            }

            public Hint(OriginalGridCell from, OriginalGridCell to, int score, int tapDamage, int order)
            {
                From = from;
                To = to;
                Type = to == null ? Kind.Tap : Kind.Combo;
                Score = score;
                TapDamage = tapDamage;
                Order = order;
                var cells = new OriginalMatchCells();
                cells.Add(from);
                if (to != null) cells.Add(to);
                Cells = cells;
            }
        }

        private bool CanSuggestTap => game.Session.Number < timing.tapHintLevelCutoff &&
            progress.PowerHintTapCount < timing.tapHintTapLimit;
        public OriginalGridCell ActiveSource => phase != Phase.None ? active.From : null;
        public OriginalGridCell ActiveDestination => phase != Phase.None ? active.To : null;
        public int ActiveParticleCount => particleCount;

        public OriginalMatchHintManager(OriginalInitialBoardView board, OriginalPlayableBoard game,
            CoreLevelProgress progress, string particlesPrefabPath, OriginalMatchHintTiming timing)
        {
            this.board = board;
            this.game = game;
            this.progress = progress;
            random = game.GeneralRandom;
            this.particlesPrefabPath = particlesPrefabPath;
            this.timing = timing;
            eligibility = new OriginalBoardPossibleMoveServices(board.MatchBlocking);
            iterator = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            game.OnSwap += OnSwap;
            game.OnTap += OnTap;
            game.OnTntOrRocketTapped += OnTntOrRocketTapped;
        }

        public void ManualUpdate(bool blocked)
        {
            if (IsStopping)
            {
                AdvanceAnimation(board.Presentation.LevelDeltaTime);
                return;
            }
            // Native HintManager 0x21893e4 permits only the joint fall/swap mask.
            var operations = board.State.Operations.Container;
            if (blocked || !game.CanUserMakeMove() ||
                (operations.HasAny() && !operations.HasMask(OriginalGameOperationContainer.FallAndSwapMask)))
            {
                StopProcess();
                return;
            }
            if (phase != Phase.None)
            {
                AdvanceAnimation(board.Presentation.LevelDeltaTime);
                return;
            }

            int frame = board.Clock.FrameCount;
            if (stableAt < 0) { stableAt = frame; return; }
            if (!searchFinished)
            {
                ProcessCell();
                if (!searchFinished) ProcessCell();
            }
            if (!searchFinished || candidates.Count == 0 || Time.time < nextAt) return;
            bool immediate = !hasShown && (game.Session.Number == 2 || game.Session.Number == 3);
            if (!immediate && frame <= stableAt + timing.stableFrames) return;
            // ShowHint 0x21895cc shuffles on each eligible frame until the first hint starts.
            if (!hadHint && !CanSuggestTap) ShuffleEqualScores();
            if (firstCycleAt < 0)
                firstCycleAt = frame + (immediate || candidates.Count == 1 ? 0 : timing.initialCycleFrames);
            if (frame < firstCycleAt) return;
            int next = (index + 1) % candidates.Count;
            // PlayNextHint 0x218a578 wraps before line-of-three hints
            // when the current cycle already contains a stronger action.
            if (index >= 0 && candidates[index].Score >= 2 && candidates[next].Score == 1) next = 0;
            index = next;
            StartAnimation(candidates[index]);
        }

        private void ProcessCell()
        {
            var cell = iterator.MoveNext();
            if (cell == null)
            {
                searchFinished = true;
                candidates.Sort(CompareScore);
                return;
            }
            var item = cell.Mediation.CurrentItem;
            if (item is OriginalSpecialBoardItem special && eligibility.CanEnterPossibleMatch(cell))
            {
                ProcessSpecial(cell, special);
                return;
            }
            if (!(item is OriginalMatchBoardItem match) ||
                !eligibility.CanEnterPossibleMatchAcceptingChain(cell)) return;

            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
            {
                var result = board.PossibleMoves.CheckNeighborForPossibleMatches(cell, match.MatchType, direction);
                if (!result.IsValid()) continue;
                TryMatch(result.Top, result.RequiredCell);
                TryMatch(result.Right, result.RequiredCell);
                TryMatch(result.Bottom, result.RequiredCell);
                TryMatch(result.Left, result.RequiredCell);
            }
        }

        private void ProcessSpecial(OriginalGridCell cell, OriginalSpecialBoardItem item)
        {
            // TryAddHintForSpecialItem 0x21897fc uses the first usable neighbor
            // if an existing hint does not already include this source.
            if (OriginalCellTouchRules.CanSwapFrom(cell, board.SwapAvailability) && !ContainsEndpoint(cell))
            {
                foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                {
                    var neighbor = cell.Neighbors.Get(direction);
                    if (neighbor == null || !(neighbor.Mediation.CurrentItem is OriginalSpecialBoardItem other) ||
                        !OriginalCellTouchRules.CanSwapTo(neighbor, board.SwapAvailability)) continue;
                    int score = OriginalPlayableBoard.GetSpecialComboType(item, other) switch
                    {
                        // Native Hint.ComboScore at 0x404073c.
                        1 => 25, 2 => 24, 3 => 23, 4 => 22, 5 => 20, 6 => 19, 7 => 12, 8 => 17, 9 => 16, 10 => 15, _ => 0
                    };
                    if (score == 0) continue;
                    candidates.Add(new Hint(cell, neighbor, score, 0, candidates.Count));
                    return;
                }
            }

            if (!CanSuggestTap || !OriginalCellTap.CanTapCurrentItem(cell, out _)) return;
            int tapDamage = CalculateTapDamage(cell, item);
            if (tapDamage != 0) candidates.Add(new Hint(cell, null, 2, tapDamage, candidates.Count));
        }

        private bool ContainsEndpoint(OriginalGridCell cell)
        {
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].From == cell || candidates[i].To == cell) return true;
            return false;
        }

        private int CalculateTapDamage(OriginalGridCell cell, OriginalSpecialBoardItem item)
        {
            // Hint.GetSpecialItemScore 0x2186f90 scores without creating
            // an incoming exploder or advancing its shared ID.
            var point = new OriginalCellPoint(cell.X, cell.Y);
            if (item is OriginalTntBoardItem)
                return game.AreaTargets.CalculateScore(point, new OriginalExplodeData(13, 0, point, 0, false), out _);
            if (!(item is OriginalRocketBoardItem rocket)) return 0;
            bool column = rocket.Orientation != 0;
            var data = new OriginalExplodeData(column ? 10 : 11, 0, point, 0, false);
            return game.LineTargets.CalculateScore(column, column ? cell.X : cell.Y, data, out _);
        }

        private void TryMatch(OriginalGridCell from, OriginalGridCell to)
        {
            if (from == null || !OriginalCellTouchRules.CanSwapFrom(from, board.SwapAvailability) ||
                !OriginalCellTouchRules.CanSwapTo(to, board.SwapAvailability)) return;
            int width = board.Grid.Width;
            int key = (from.Y * width + from.X) * (width * board.Grid.Height) + to.Y * width + to.X;
            if (!tried.Add(key)) return;
            var moved = from.Mediation.CurrentItem;
            var previous = to.Mediation.CurrentItem;
            try
            {
                if (previous != null) OriginalSwapDecision.SetAllItems(from, previous);
                OriginalSwapDecision.SetAllItems(to, moved);
                var groups = board.Matches.FindMatchesInArea(to.X, to.Y, 2);
                for (int i = 0; i < groups.Count; i++)
                {
                    var group = groups[i];
                    if (!group.ContainsCell(to)) continue;
                    var hint = new Hint(from, to, group, candidates.Count);
                    if (hint.Score > 0) candidates.Add(hint);
                }
            }
            finally
            {
                OriginalSwapDecision.SetAllItems(from, moved);
                if (previous != null) OriginalSwapDecision.SetAllItems(to, previous);
            }
        }

        private static int CompareScore(Hint a, Hint b)
        {
            int score = b.Score.CompareTo(a.Score);
            if (score != 0) return score;
            if (a.Type == Kind.Tap && b.Type == Kind.Tap)
            {
                int damage = b.TapDamage.CompareTo(a.TapDamage);
                if (damage != 0) return damage;
            }
            return a.Order.CompareTo(b.Order);
        }

        private void ShuffleEqualScores()
        {
            int start = 0;
            while (start < candidates.Count)
            {
                int end = start + 1;
                while (end < candidates.Count && candidates[end].Score == candidates[start].Score) end++;
                // ShuffleIndexes 0x218a8b0 also consumes the draw for a singleton.
                for (int i = start; i < end; i++)
                {
                    int other = random.Next(start, end);
                    if (other != i)
                        (candidates[i], candidates[other]) = (candidates[other], candidates[i]);
                }
                start = end;
            }
        }

        private void StartAnimation(Hint hint)
        {
            var item = hint.From.Mediation.CurrentItem;
            if (!(item?.SwapView is OriginalItemSwapView moved)) return;
            var comboItem = hint.Type == Kind.Combo ? hint.To.Mediation.CurrentItem : null;
            var comboView = comboItem?.SwapView as OriginalItemSwapView;
            if (hint.Type == Kind.Combo && comboView == null) return;
            active = hint;
            hadHint = hasShown = true;
            pulses = bursts = 0;
            moving = new HintPose(item, moved, hint.Type == Kind.Tap);
            if (comboView != null) second = new HintPose(comboItem, comboView, false);
            raisedSorting = item is OriginalSpecialBoardItem special ? special.FallView : moved as IOriginalItemFallSortingSurface;
            if (raisedSorting != null)
            {
                hintSorting = OriginalItemSortingRules.Hint(game.ItemSortingLayer);
                raisedSorting.UpdateSorting(hintSorting);
            }
            if (hint.To != null) direction = new Vector3(hint.To.X - hint.From.X, hint.To.Y - hint.From.Y, 0);
            var pool = board.Presentation.Pool;
            bool useParticles = hint.Type != Kind.Match || hint.Score != 1;
            if (useParticles && !pool.HasPool(OriginalHintParticles.PoolId))
            {
                var prefab = Resources.Load<GameObject>(particlesPrefabPath)?.GetComponent<OriginalHintParticles>();
                if (prefab == null) throw new InvalidOperationException("Missing original hint particles: " + particlesPrefabPath);
                pool.CreatePool(prefab, 0);
            }
            for (int i = 0; i < hint.Cells.Count; i++)
            {
                var cell = hint.Cells.Get(i);
                if (hint.Type == Kind.Match)
                {
                    var model = cell == hint.To ? item : cell.Mediation.CurrentItem;
                    if (model?.SwapView is OriginalMatchItemSpriteView view)
                        highlighted[highlightedCount++] = new HighlightedItem { Model = model, View = view, PoolUse = view.PoolUse };
                }
                if (!useParticles) continue; // Original line-of-three uses the item pulse alone.
                var effect = pool.Spawn<OriginalHintParticles>(OriginalHintParticles.PoolId, true);
                effect.transform.SetParent(board.BoardRoot, true);
                effect.transform.position = cell.GetViewPosition();
                effect.Show(AdjacentMask(hint.Cells, cell));
                particles[particleCount++] = effect;
            }
            phaseElapsed = phaseDuration = 0f;
            nextPhase = Phase.None;
            phase = Phase.PrepareForward;
        }

        private static int AdjacentMask(OriginalMatchCells cells, OriginalGridCell cell)
        {
            int mask = 0;
            var directions = OriginalCellNeighbors.TouchingNeighborTypes;
            for (int edge = 0; edge < directions.Length; edge++)
            {
                var neighbor = cell.Neighbors.Get(directions[edge]);
                for (int i = 0; i < cells.Count; i++)
                    if (cells.Get(i) == neighbor) { mask |= 1 << edge; break; }
            }
            return mask;
        }

        private void AdvanceAnimation(float delta)
        {
            // Native Match/Combo/TapHintAnimation keep preparation, the final
            // interpolation sample and the following transition on separate frames.
            switch (phase)
            {
                case Phase.PrepareForward:
                    PrepareInterpolation(false, false);
                    if (bursts == 2 && pulses == 1) HideParticles();
                    break;
                case Phase.PrepareReturn:
                    pulses++;
                    PrepareInterpolation(true, false);
                    break;
                case Phase.Interpolate:
                    if (phaseElapsed >= phaseDuration)
                    {
                        phase = nextPhase;
                        break;
                    }
                    phaseElapsed += delta;
                    float progress = Mathf.Clamp01(phaseElapsed / phaseDuration);
                    moving.Sample(progress);
                    if (active.Type == Kind.Combo) second.Sample(progress);
                    for (int i = 0; i < highlightedCount; i++)
                    {
                        ref var entry = ref highlighted[i];
                        if (entry.IsLive)
                            entry.View.SetHighlightRatio(Mathf.Lerp(entry.InterpolationStart, entry.InterpolationEnd, progress));
                    }
                    break;
                case Phase.PrepareWait:
                    phaseElapsed = 0f;
                    pulses = 0;
                    if (active.Type == Kind.Match) moving.SetPosition(Vector3.zero);
                    if (++bursts > 2)
                    {
                        bursts = 0;
                        phase = Phase.Fade;
                        HideParticles();
                        nextAt = Time.time + timing.repeatSeconds;
                    }
                    else phase = Phase.Pause;
                    break;
                case Phase.Pause:
                    float pause = active.Type == Kind.Tap ? timing.tapPauseSeconds : timing.pauseSeconds;
                    if (phaseElapsed < pause) phaseElapsed += delta;
                    else phase = Phase.PrepareForward;
                    break;
                case Phase.Fade:
                    if (phaseElapsed < timing.pauseSeconds) phaseElapsed += delta;
                    else phase = Phase.StopComplete;
                    break;
                case Phase.StopRequested:
                    StartStop();
                    break;
                case Phase.StopComplete:
                    FinishAnimation();
                    break;
            }
        }

        private void PrepareInterpolation(bool returning, bool stopping)
        {
            phaseElapsed = 0f;
            phaseDuration = stopping ? timing.stopSeconds : active.Type switch
            {
                Kind.Tap => timing.tapPulseSeconds,
                Kind.Combo => returning ? timing.comboReturnSeconds : timing.comboForwardSeconds,
                _ => returning ? timing.returnSeconds : timing.forwardSeconds
            };
            if (active.Type == Kind.Tap)
            {
                // Native tap stop starts and ends at neutral scale; the first
                // interpolation frame applies it, rather than the request frame.
                var peak = Vector3.one * timing.tapScale;
                moving.Prepare(returning && !stopping ? peak : Vector3.one,
                    returning || stopping ? Vector3.one : peak);
            }
            else if (active.Type == Kind.Combo)
            {
                var end = direction * (returning || stopping ? 0f : timing.comboDistance);
                moving.Prepare(moving.Current, end);
                second.Prepare(second.Current, -end);
            }
            else
            {
                var current = moving.Current;
                var start = stopping ? current : returning ? moving.InterpolationEnd : Vector3.zero;
                var end = stopping ? Vector3.zero : returning
                    ? start - direction * timing.moveDistance
                    : current + direction * timing.moveDistance;
                moving.SetPosition(returning ? start : current);
                moving.Prepare(start, end);
                float ratio = stopping ? 0f : returning ? (pulses == 2 ? 0f : .5f) : 1f;
                for (int i = 0; i < highlightedCount; i++)
                {
                    ref var entry = ref highlighted[i];
                    if (!entry.IsLive) continue;
                    entry.InterpolationStart = entry.View.GetHighlightRatio();
                    entry.InterpolationEnd = entry.View.GetMaxHighlightRatio() * ratio;
                }
            }
            phase = Phase.Interpolate;
            nextPhase = stopping ? Phase.StopComplete : returning
                ? (pulses == 2 ? Phase.PrepareWait : Phase.PrepareForward)
                : Phase.PrepareReturn;
        }

        private void HideParticles()
        {
            for (int i = 0; i < particleCount; i++) particles[i].Hide();
        }

        private void FinishAnimation()
        {
            if (phase == Phase.None) return;
            moving.Restore();
            if (active.Type == Kind.Combo) second.Restore();
            if (raisedSorting != null && moving.IsLive && !moving.IsSwapping && moving.Cell is OriginalGridCell cell)
            {
                var current = raisedSorting.GetSorting();
                if (current.Layer == hintSorting.Layer && current.Order == hintSorting.Order)
                    raisedSorting.UpdateSorting(OriginalItemSortingRules.AnimatedItem(current.Layer, cell.Y));
            }
            for (int i = 0; i < highlightedCount; i++)
            {
                ref var entry = ref highlighted[i];
                if (entry.IsLive) entry.View.SetHighlightRatio(0f);
                entry = default;
            }
            for (int i = 0; i < particleCount; i++)
            {
                board.Presentation.Pool.Recycle(particles[i]);
                particles[i] = null;
            }
            highlightedCount = particleCount = 0;
            moving = second = default;
            raisedSorting = null;
            phase = nextPhase = Phase.None;
        }

        private void StopProcess()
        {
            ResetSearch();
            if (phase != Phase.None && !IsStopping) phase = Phase.StopRequested;
        }

        private void StartStop()
        {
            // Match hides before capturing its root/highlights; combo and tap
            // prepare their interpolation first, matching the native owners.
            if (active.Type == Kind.Match) HideParticles();
            PrepareInterpolation(false, true);
            if (active.Type != Kind.Match) HideParticles();
        }

        private bool IsHinted(OriginalBoardItem item)
        {
            if (item == null || !hadHint || phase == Phase.None) return false;
            if (moving.Owns(item) || second.Owns(item)) return true;
            for (int i = 0; i < highlightedCount; i++)
                if (highlighted[i].Model == item) return true;
            return false;
        }

        private void ResetSearch()
        {
            if (stableAt < 0) return;
            if (hadHint)
            {
                game.Session.ShownHintCount++;
                hadHint = false;
            }
            stableAt = firstCycleAt = index = -1;
            searchFinished = false;
            nextAt = 0f;
            candidates.Clear();
            tried.Clear();
            iterator.Reset();
        }

        private void OnSwap(OriginalGridCell from, OriginalGridCell to, bool valid)
        {
            if (valid && hadHint && ((active.From == from && active.To == to) || (active.From == to && active.To == from)))
                game.Session.UsedHintCount++;
            if (valid || IsHinted(from.Mediation.CurrentItem) || IsHinted(to?.Mediation.CurrentItem)) StopProcess();
        }

        private void OnTap(OriginalGridCell cell)
        {
            // CheckAndUpdateUsedHintCount 0x2189098 uses the selected hint's first cell.
            if (index >= 0 && candidates[index].From == cell) game.Session.UsedHintCount++;
            if (IsHinted(cell.Mediation.CurrentItem)) StopProcess();
        }

        private void OnTntOrRocketTapped()
        {
            if (game.Session.Number <= timing.tapHintLevelCutoff) progress.RecordPowerHintTap();
        }

        public void Dispose()
        {
            game.OnSwap -= OnSwap;
            game.OnTap -= OnTap;
            game.OnTntOrRocketTapped -= OnTntOrRocketTapped;
            StopProcess();
            FinishAnimation();
        }
    }
}
