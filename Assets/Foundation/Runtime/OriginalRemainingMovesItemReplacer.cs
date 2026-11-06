using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalRemainingMovesReplacementServices
    {
        int LeftMoves { get; }
        int MatchTypeToCollect { get; }
        Vector3 MovesPosition { get; }
        float UnityDeltaTime { get; }
        float UnityRandomValue { get; }
        int NextRegularRandom(int min, int max);
        void SetDisplayedMoves(int value);
        void SetOrientationEnabled(bool value);
        void ResetTopSorting();
        void PlaySound(int id, float offset);
        Transform SpawnFlightParticles();
        void RecycleFlightParticles(Transform particles);
        OriginalSpecialBoardItem ReplaceWithSpecialItem(OriginalGridCell cell, int index);
    }

    // RemainingMovesItemReplacer 0x1119604..0x111b20c. Conversion changes the
    // original HUD count, never the MoveManager/Session remaining-move budget.
    public sealed class OriginalRemainingMovesItemReplacer
    {
        private sealed class Replacement
        {
            public OriginalGridCell Cell;
            public Coroutine Routine;
            public OriginalSpecialBoardItem CreatedItem;
        }
        private readonly IOriginalRemainingMovesReplacementServices services;
        private readonly OriginalCoroutinePlayer coroutines;
        private OriginalGridIterator iterator;
        private readonly Dictionary<OriginalGridCell, Replacement> replacements = new Dictionary<OriginalGridCell, Replacement>();
        // Native allocates this list per selection. Retain its capacity to avoid
        // repeated allocations during a long remaining-move conversion.
        private readonly List<OriginalGridCell> candidates = new List<OriginalGridCell>();
        private Coroutine sendingRoutine;
        private Action onComplete;
        private bool complete, skipped;
        private readonly int startMoves;
        private int replacedMoves;
        public int StartMoves => startMoves;
        public int ReplacedMoves => replacedMoves;
        public bool IsComplete => complete;
        public OriginalRemainingMovesItemReplacer(IOriginalGridIteratorStrategy strategy,
            OriginalCoroutinePlayer coroutines, IOriginalRemainingMovesReplacementServices services)
        {
            iterator = new OriginalGridIterator(strategy);
            this.coroutines = coroutines ?? throw new ArgumentNullException(nameof(coroutines));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            startMoves = services.LeftMoves;
        }
        public void PrepareToSendMoves(Action completed) // 0x1119930.
        {
            if (skipped || startMoves == 0) { completed(); return; }
            services.SetOrientationEnabled(false);
            onComplete = completed ?? throw new ArgumentNullException(nameof(completed));
            complete = false;
        }
        public void SendMoves(float delay, Action completed)
        { PrepareToSendMoves(completed); StartSendingMoves(delay); }
        public void StartSendingMoves(float delay) // 0x1119bdc.
        {
            if (skipped || startMoves == 0) return;
            sendingRoutine = coroutines.ManualStartCoroutine(SendAllMoves(delay));
        }
        public void Skip() // 0x1119c88. Pending flights are stopped, not completed.
        {
            skipped = true;
            if (sendingRoutine != null) coroutines.StopCoroutine(sendingRoutine);
            foreach (var replacement in replacements.Values)
                if (replacement.Routine != null) coroutines.StopCoroutine(replacement.Routine);
            services.SetOrientationEnabled(true);
            if (!complete) onComplete?.Invoke();
            complete = true;
        }
        public void SkipImmediately() { services.SetDisplayedMoves(0); } // 0x1119ee0.
        public OriginalGridCell GiveRandomCellWithMatchItem(int matchType) // 0x111a0a8.
        {
            candidates.Clear(); iterator.Reset();
            while (iterator.MoveNext() != null)
            {
                var cell = iterator.Cell;
                if (replacements.ContainsKey(cell)) continue;
                var item = cell.Mediation.CurrentItem;
                if (!(item is OriginalMatchBoardItem match) || (matchType != 0 && match.MatchType != matchType) || item.ItemType != 1 || cell.StaticTouch.HasTouchBlockingItem()) continue;
                candidates.Add(cell);
            }
            return candidates.Count == 0 ? null : candidates[services.NextRegularRandom(0, candidates.Count)];
        }
        private IEnumerator SendAllMoves(float delay) // 0x111aa7c.
        {
            yield return new WaitForSeconds(delay);
            services.ResetTopSorting();
            int lastSentIndex = 0;
            for (int index = 0; index < startMoves; index++)
            {
                services.PlaySound(308, .04f);
                services.SetDisplayedMoves(startMoves - index - 1);
                var cell = services.MatchTypeToCollect == 0 ? null : GiveRandomCellWithMatchItem(services.MatchTypeToCollect);
                if (cell == null) cell = GiveRandomCellWithMatchItem(0);
                if (cell == null) break;
                var replacement = new Replacement { Cell = cell };
                lastSentIndex = index;
                replacement.Routine = coroutines.ManualStartCoroutine(SendMove(replacement, index));
                replacements.Add(cell, replacement);
                yield return new WaitForSeconds(.05f);
            }
            while (replacedMoves <= lastSentIndex) yield return null;
            onComplete();
            services.SetOrientationEnabled(true); complete = true;
        }
        private IEnumerator SendMove(Replacement replacement, int index) // 0x111aef8.
        {
            var particles = services.SpawnFlightParticles();
            particles.position = services.MovesPosition;
            float elapsed = 0f;
            var start = services.MovesPosition + new Vector3(.1f * (services.UnityRandomValue * 2f - 1f), .1f * (services.UnityRandomValue * 2f - 1f), 0f);
            var end = replacement.Cell.GetViewPosition();
            var secondControl = (start + end) * .5f;
            while (elapsed < .6f)
            {
                elapsed += services.UnityDeltaTime;
                float t = Mathf.Clamp01(Mathf.Sin(elapsed / .6f * (Mathf.PI * .5f)));
                float u = 1f - t;
                particles.position = u * u * u * start + 3f * u * u * t * start + 3f * u * t * t * secondControl + t * t * t * end;
                yield return null;
            }
            services.PlaySound(services.NextRegularRandom(0, 2) == 1 ? 306 : 307, .04f);
            replacedMoves = unchecked(replacedMoves + 1); // Before mutating the cell.
            replacement.CreatedItem = services.ReplaceWithSpecialItem(replacement.Cell, index);
            services.RecycleFlightParticles(particles);
        }
    }
}
