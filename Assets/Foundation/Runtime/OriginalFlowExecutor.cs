using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public abstract class OriginalFlowAction
    {
        public virtual int Type => 0;
        public virtual bool IsForIconCollect => false;
        public event Action OnComplete, OnReplace;
        public abstract void Execute();
        public abstract void Cancel(OriginalFlowAction interruptAction);
        public virtual void Complete() { OnComplete?.Invoke(); }
        public virtual void Replace(OriginalFlowAction action) { OnReplace?.Invoke(); }
        public void Remove() { OnReplace?.Invoke(); }
        public virtual bool SupportsMultiple() => false;
        public virtual bool SupportsOneExceptRunning() => false;
        public virtual void Clear() { }
        internal void NotifyReplaced() { OnReplace?.Invoke(); }
    }
    public interface IOriginalFlowExecutorContext
    {
        bool ShouldDelayAutoActions();
        bool IsInDelayedActions(OriginalFlowAction action);
        void DelayAutoAction(OriginalFlowAction action);
    }
    // Native FlowExecuter main/auto/next lists and completion protocol.
    // Type identity is used only for equality, never reflected member lookup,
    // string keys or invocation. Original action diagnostics are omitted.
    public sealed class OriginalFlowExecutor
    {
        private readonly LinkedList<OriginalFlowAction> list = new LinkedList<OriginalFlowAction>();
        private readonly LinkedList<OriginalFlowAction> autoList = new LinkedList<OriginalFlowAction>();
        private readonly LinkedList<OriginalFlowAction> nextList = new LinkedList<OriginalFlowAction>();
        private readonly OriginalDisableCounter nextMode = new OriginalDisableCounter();
        private readonly OriginalDisableCounter pause = new OriginalDisableCounter();
        private int nextOnPushDisableCount;
        private OriginalFlowAction running;
        private readonly IOriginalFlowExecutorContext context;
        public OriginalFlowExecutor(IOriginalFlowExecutorContext context) { this.context = context; }
        public bool IsPaused => pause.IsEnabled;
        public int PendingCount => list.Count;
        public int AutoCount => autoList.Count;
        public int NextCount => nextList.Count;
        public OriginalFlowAction GetRunningAction() => running;
        public void ManualUpdate() => NextAction();
        public void Push(OriginalFlowAction action) // 0x35a0934.
        {
            AddActionToList(action);
            if (action == null) throw new NullReferenceException();
            if (action.Type == 2 && running != null) running.Cancel(action);
            if (nextOnPushDisableCount == 0) NextAction();
        }
        private void AddActionToList(OriginalFlowAction action) // 0x35a09b0.
        {
            LinkedList<OriginalFlowAction> target;
            bool auto = false;
            if (nextMode.IsEnabled) target = nextList;
            else
            {
                if (action == null) throw new NullReferenceException();
                auto = action.Type == 3; target = auto ? autoList : list;
            }
            if (action == null) throw new NullReferenceException();
            if (!action.SupportsMultiple() && !nextMode.IsEnabled)
            {
                if (!auto) RemoveAutoEquivalent(action, autoList.First);
                var runningType = running == null ? null : running.GetType();
                if (runningType == action.GetType() && !action.SupportsOneExceptRunning())
                { action.NotifyReplaced(); return; }
                if (ReplaceActionInList(action, target.First)) return;
            }
            bool delayed = (context ?? throw new NullReferenceException()).IsInDelayedActions(action);
            if (action.Type == 1 || action.Type == 2 || delayed) target.AddFirst(action);
            else target.AddLast(action);
        }
        private void RemoveAutoEquivalent(OriginalFlowAction action, LinkedListNode<OriginalFlowAction> node) // 0x35a0d48.
        {
            while (node != null)
            {
                var old = node.Value ?? throw new NullReferenceException();
                if (action.GetType() == old.GetType())
                { old.NotifyReplaced(); autoList.Remove(node); return; }
                node = node.Next;
            }
        }
        private static bool ReplaceActionInList(OriginalFlowAction action, LinkedListNode<OriginalFlowAction> node) // 0x35a0f4c.
        {
            while (node != null)
            {
                var old = node.Value ?? throw new NullReferenceException();
                if (action.GetType() == old.GetType()) { old.Replace(action); node.Value = action; return true; }
                node = node.Next;
            }
            return false;
        }
        private void NextAction() // 0x35a05b4.
        {
            if (pause.IsEnabled || running != null) return;
            if (list.Count > 0)
            { running = list.First.Value; list.RemoveFirst(); }
            else if (autoList.Count > 0)
            {
                var action = autoList.First.Value; autoList.RemoveFirst();
                if ((context ?? throw new NullReferenceException()).ShouldDelayAutoActions())
                { context.DelayAutoAction(action); return; }
                running = action;
            }
            else return;
            if (running == null) throw new NullReferenceException();
            running.OnComplete += ActionCompleted;
            // Execute may synchronously complete and advance the next action.
            running.Execute();
        }
        private void ActionCompleted() // 0x35a1398.
        {
            var action = running ?? throw new NullReferenceException();
            action.OnComplete -= ActionCompleted; running = null; NextAction();
        }
        public void Pause() => pause.Enable();
        public void Resume() => pause.Disable(); // Native waits until Update/Push/Complete to advance.
        public void EnableNextActionOnPush() { if (nextOnPushDisableCount != 0) nextOnPushDisableCount = unchecked(nextOnPushDisableCount - 1); }
        public void DisableNextActionOnPush() { nextOnPushDisableCount = unchecked(nextOnPushDisableCount + 1); }
        public void StartNextActionMode() { pause.Enable(); nextMode.Enable(); }
        public void FinishNextActionMode() // 0x35a1828.
        {
            pause.Disable(); nextMode.Disable();
            while (nextList.Count > 0)
            { var node = nextList.Last; nextList.RemoveLast(); list.AddFirst(node); }
            NextAction();
        }
        public void Clear(bool alsoRunning = true) // 0x35a03c4. nextList/counters intentionally survive.
        {
            list.Clear();
            foreach (var action in autoList) action?.Clear();
            autoList.Clear(); pause.Reset();
            if (alsoRunning && running != null) { running.Clear(); running.Complete(); running = null; }
        }
    }
    // Main-flow owner. Home delay providers and other application/subflow
    // composition are explicit dependencies; no fabricated inactive runtime.
    public sealed class OriginalFlowManager
    {
        private readonly OriginalFlowExecutor main;
        public OriginalFlowManager(IOriginalFlowExecutorContext context) { main = new OriginalFlowExecutor(context); }
        public OriginalFlowExecutor Main => main;
        public void Push(OriginalFlowAction action) => main.Push(action); // 0x35a2448.
        public void ManualUpdate() => main.ManualUpdate();
        public void Clear(bool alsoRunning = true) => main.Clear(alsoRunning);
        public void PushToNext(OriginalFlowAction action)
        { main.StartNextActionMode(); main.Push(action); main.FinishNextActionMode(); }
    }
}
