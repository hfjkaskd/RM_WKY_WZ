using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Unity coroutine host for local moves. Capacity is configured in prefab;
    // view transforms and iterators are cached rather than allocated per frame.
    public sealed class OriginalLocalMovePlayer : MonoBehaviour
    {
        [SerializeField] private int initialCapacity;
        private OriginalLocalMoveTween[] tweens;
        // At most one level-clock move per concurrent swap action.
        private readonly List<LevelMove> levelMoves = new List<LevelMove>(2);
        private int levelMoveGeneration;
        private readonly Dictionary<Transform, TransformTargets> targets = new Dictionary<Transform, TransformTargets>();
        public int Capacity => tweens.Length;
        public int GrowthCount { get; private set; }

        private struct LevelMove
        {
            public Transform Target;
            public Vector3 Start, End;
            public float Duration, Elapsed;
            public Action OnComplete;
        }

        private sealed class TransformTarget : IOriginalLocalPositionTarget
        {
            private readonly Transform target;
            private readonly bool scale;
            public TransformTarget(Transform target, bool scale) { this.target = target; this.scale = scale; }
            public OriginalSwapPoint LocalPosition
            {
                get { var p = scale ? target.localScale : target.localPosition; return new OriginalSwapPoint(p.x, p.y, p.z); }
                set
                {
                    var p = new Vector3(value.X, value.Y, value.Z);
                    if (scale) target.localScale = p; else target.localPosition = p;
                }
            }
        }
        private sealed class TransformTargets
        {
            public readonly TransformTarget Position, Scale;
            public TransformTargets(Transform target)
            { Position = new TransformTarget(target, false); Scale = new TransformTarget(target, true); }
        }
        private void Awake()
        {
            if (initialCapacity < 1) throw new InvalidOperationException("Local move capacity must be configured.");
            tweens = new OriginalLocalMoveTween[initialCapacity];
            CreateSlots(0);
        }
        private void CreateSlots(int start)
        {
            for (int i = start; i < tweens.Length; i++) tweens[i] = new OriginalLocalMoveTween(ReadDeltaTime);
        }
        // Non-replay MTween.GetDeltaTime 0x2861b4c uses Unity's scaled delta,
        // rather than the separately quantized level clock.
        private static float ReadDeltaTime() => UnityEngine.Time.deltaTime;
        public void RegisterTarget(Transform target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!targets.ContainsKey(target)) targets.Add(target, new TransformTargets(target));
        }
        internal void CancelAll()
        {
            StopAllCoroutines();
            foreach (var tween in tweens) tween.Cancel();
            levelMoves.Clear();
            levelMoveGeneration = unchecked(levelMoveGeneration + 1);
        }
        internal void MoveToLocalOnLevelClock(Transform target, Vector3 end, float duration, Action onComplete)
        {
            levelMoves.Add(new LevelMove { Target = target, End = end, Duration = duration, OnComplete = onComplete });
        }
        internal void TickLevelMoves(float delta)
        {
            if (delta <= 0f) return;
            // Propeller combo DOLocalMove 0x1ee3e80 uses Linear and Manual.
            // Capture the start on its first tick and complete on the endpoint tick.
            int count = levelMoves.Count;
            int generation = levelMoveGeneration;
            for (int i = 0; i < count;)
            {
                var move = levelMoves[i];
                if (move.Elapsed == 0f) move.Start = move.Target.localPosition;
                move.Elapsed = Mathf.Min(move.Elapsed + delta, move.Duration);
                move.Target.localPosition = Vector3.LerpUnclamped(move.Start, move.End, move.Elapsed / move.Duration);
                if (move.Elapsed < move.Duration)
                {
                    levelMoves[i++] = move;
                    continue;
                }

                // A callback can clear the attempt or enqueue another move.
                // Only tick moves that were present at the start of this update.
                levelMoves.RemoveAt(i);
                count--;
                move.OnComplete?.Invoke();
                if (generation != levelMoveGeneration) return;
            }
        }
        public void MoveToLocal(Transform target, OriginalSwapPoint end, float duration, Action onComplete, int easeType = 0)
        {
            if (!targets.TryGetValue(target, out var pair))
            {
                RegisterTarget(target);
                pair = targets[target];
            }
            Play(pair.Position, end, duration, onComplete, easeType);
        }
        public void ScaleToLocal(Transform target, OriginalSwapPoint end, float duration, Action onComplete, int easeType)
        {
            if (!targets.TryGetValue(target, out var pair))
            {
                RegisterTarget(target);
                pair = targets[target];
            }
            // LocalScaleProcedure 0x2c97ebc has the same Vector3 arithmetic
            // and iterator timing as local position; only the setter differs.
            Play(pair.Scale, end, duration, onComplete, easeType);
        }
        private void Play(IOriginalLocalPositionTarget position, OriginalSwapPoint end, float duration, Action onComplete, int easeType)
        {
            OriginalLocalMoveTween slot = null;
            for (int i = 0; i < tweens.Length; i++)
                if (!tweens[i].IsInUse) { slot = tweens[i]; break; }
            if (slot == null)
            {
                int oldCapacity = tweens.Length;
                Array.Resize(ref tweens, checked(oldCapacity * 2));
                CreateSlots(oldCapacity);
                GrowthCount++;
                slot = tweens[oldCapacity];
                Debug.LogWarning("Local move pool grew; review prefab capacity against concurrent moves.");
            }
            slot.Configure(position, end, duration, onComplete, easeType);
            StartCoroutine(slot);
        }
    }
}
