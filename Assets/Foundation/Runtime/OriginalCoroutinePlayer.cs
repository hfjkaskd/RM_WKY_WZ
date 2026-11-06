using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalManualCoroutinePlayer
    {
        Coroutine ManualStartCoroutine(IEnumerator enumerator, Action onKill = null);
    }
    // CoroutinePlayer 0x2d7ca44..0x2d7ced8. The host belongs to the game
    // context, so deactivating a pooled item does not stop its manual routine.
    public sealed class OriginalCoroutinePlayer : IOriginalManualCoroutinePlayer
    {
        private readonly MonoBehaviour mono;
        private readonly List<OriginalRoyalCoroutine> routines = new List<OriginalRoyalCoroutine>();
        public int Count => routines.Count;
        public OriginalCoroutinePlayer(MonoBehaviour mono) { this.mono = mono; }
        public Coroutine ManualStartCoroutine(IEnumerator enumerator, Action onKill = null)
        {
            var wrapper = OriginalRoyalCoroutine.Create(enumerator);
            if (onKill != null) wrapper.SetOnKill(onKill);
            StartCoroutine(wrapper);
            return wrapper.Coroutine;
        }
        public void StartCoroutine(OriginalRoyalCoroutine wrapper)
        {
            wrapper.SetPlayer(this);
            routines.Add(wrapper);
            // Unity runs the first resume before returning the handle. Keep
            // this assignment after it, including synchronous completion.
            wrapper.SetCoroutine(mono.StartCoroutine(wrapper.Start()));
        }
        public void StopCoroutine(Coroutine coroutine)
        {
            for (int i = routines.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(routines[i].Coroutine, coroutine)) continue;
                routines[i].Kill();
                return;
            }
        }
        public void KillAll()
        {
            for (int i = routines.Count - 1; i >= 0; i--) routines[i].Kill();
        }
        public void Remove(OriginalRoyalCoroutine wrapper, Coroutine coroutine)
        {
            if (!ReferenceEquals(coroutine, null)) mono.StopCoroutine(coroutine);
            routines.Remove(wrapper);
        }
    }

    // RoyalCoroutine and <Start>d__19, 0x2d7d1bc/0x2d7d2dc. Clear invokes
    // OnKill even after normal completion; callers rely on that cleanup order.
    public sealed class OriginalRoyalCoroutine
    {
        private readonly IEnumerator enumerator;
        private Action onComplete, onKill;
        private bool isKilled, isCompleted;
        private OriginalCoroutinePlayer player;
        private float startDelay;
        public Coroutine Coroutine { get; private set; }
        private OriginalRoyalCoroutine(IEnumerator enumerator) { this.enumerator = enumerator; }
        public static OriginalRoyalCoroutine Create(IEnumerator enumerator) => new OriginalRoyalCoroutine(enumerator);
        public OriginalRoyalCoroutine SetOnComplete(Action value) { onComplete += value; return this; }
        public OriginalRoyalCoroutine SetOnKill(Action value) { onKill += value; return this; }
        public OriginalRoyalCoroutine SetStartDelay(float value) { startDelay = value; return this; }
        public void SetPlayer(OriginalCoroutinePlayer value) { player = value; }
        public void SetCoroutine(Coroutine value) { Coroutine = value; }
        public IEnumerator Start() => new StartIterator(this);
        public void Kill()
        {
            if (isCompleted || isKilled) return;
            isKilled = true;
            Clear();
        }
        private void Clear()
        {
            player.Remove(this, Coroutine);
            Coroutine = null;
            onKill?.Invoke();
            onComplete = null;
            onKill = null;
        }
        private sealed class StartIterator : IEnumerator
        {
            private readonly OriginalRoyalCoroutine owner;
            private int state;
            public object Current { get; private set; }
            public StartIterator(OriginalRoyalCoroutine owner) { this.owner = owner; }
            public bool MoveNext()
            {
                int previous = state;
                if (unchecked((uint)previous) > 2u) return false;
                state = -1;
                if (previous == 0 && owner.startDelay > 0f)
                {
                    Current = new WaitForSeconds(owner.startDelay);
                    state = 1;
                    return true;
                }
                if (owner.isKilled) return false;
                if (owner.enumerator.MoveNext())
                {
                    Current = owner.enumerator.Current;
                    state = 2;
                    return true;
                }
                if (owner.isKilled) return false;
                owner.isCompleted = true;
                owner.onComplete?.Invoke();
                owner.Clear();
                return false;
            }
            public void Reset() { throw new NotSupportedException(); }
        }
    }
}
