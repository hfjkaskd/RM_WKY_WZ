using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalDialogClearer { void ClearDialogs(); }
    public interface IOriginalDialogManagerContext
    {
        void DisableTouchForGame();
        void CloseTooltip(bool animate);
        void ShowBackground(int dialogId, OriginalDialogConfig config, int sortingOffset, bool isFirstDialog);
        void HideBackground(int dialogId, OriginalDialogConfig config, Action onHidden);
        void EnableTouchForGame();
    }
    // Show/Close/DialogClosed from 0x25c97f0/0x25ca07c/0x25ca498.
    // Background tweening and scene-specific touch owners remain required.
    public sealed class OriginalDialogManager : IOriginalUiDialogManager, IOriginalDialogClearer
    {
        private readonly IOriginalDialogManagerContext context;
        private readonly SortedDictionary<int, OriginalUiDialog> dialogs = new SortedDictionary<int, OriginalUiDialog>();
        private int dialogIdCounter, sortingOrder;
        private readonly Action onBackgroundHidden;
        public bool HasActiveDialog => dialogs.Count > 0;
        public int Count => dialogs.Count;
        public int SortingOrder => sortingOrder;
        internal OriginalUiDialog GetDialog(int id) => dialogs[id];
        public event Action<OriginalUiDialog> OnDialogShown, OnDialogClosed;
        public OriginalDialogManager(IOriginalDialogManagerContext context)
        { this.context = context; onBackgroundHidden = EnableTouchForGame; }
        public void ShowDialog(OriginalUiDialog dialog)
        {
            if (dialogs.ContainsValue(dialog)) return;
            (context ?? throw new NullReferenceException()).DisableTouchForGame();
            context.CloseTooltip(false);
            dialogIdCounter = unchecked(dialogIdCounter + 1);
            (dialog ?? throw new NullReferenceException()).DialogId = dialogIdCounter;
            dialogs.Add(dialogIdCounter, dialog);
            sortingOrder = unchecked(sortingOrder + 1);
            int offset = unchecked(sortingOrder * 4 - 4);
            dialog.SetSortingOffset(offset);
            var config = dialog.GetConfig();
            context.ShowBackground(dialog.DialogId, config, offset, dialogs.Count <= 1);
            UpdateDialogTouchSortings(dialog);
            OnDialogShown?.Invoke(dialog);
        }
        public bool CloseDialog(OriginalUiDialog dialog)
        {
            if (!dialogs.ContainsKey((dialog ?? throw new NullReferenceException()).DialogId)) return false;
            var config = dialog.GetConfig();
            (context ?? throw new NullReferenceException()).HideBackground(dialog.DialogId, config, onBackgroundHidden);
            return true;
        }
        public void DialogClosed(OriginalUiDialog dialog)
        {
            if (dialog == GetDialogOnTop()) sortingOrder = unchecked(sortingOrder - 1);
            dialogs.Remove((dialog ?? throw new NullReferenceException()).DialogId);
            if (HasActiveDialog) UpdateDialogTouchSortings(GetDialogOnTop());
            else sortingOrder = 0;
            OnDialogClosed?.Invoke(dialog);
        }
        public void CloseDialogs() // 0x25ca8c8: capture ascending values before callbacks mutate registration.
        {
            var snapshot = new List<OriginalUiDialog>(dialogs.Values);
            for (int i = 0; i < snapshot.Count; i++)
                (snapshot[i] ?? throw new NullReferenceException()).ForceClose();
        }
        public void HardReset() // 0x25c948c: no background/touch/event callbacks.
        {
            dialogs.Clear(); dialogIdCounter = sortingOrder = 0;
        }
        public void ClearDialogs() // 0x25ca8b0: a close failure prevents the reset.
        { CloseDialogs(); HardReset(); }
        internal bool PressBack() // 0x25caa0c: the top dialog owns Back regardless of its close policy.
        {
            if (!HasActiveDialog) return false;
            GetDialogOnTop().PressEscape();
            return true;
        }
        private OriginalUiDialog GetDialogOnTop()
        {
            if (dialogs.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
            OriginalUiDialog last = null;
            foreach (var item in dialogs) last = item.Value;
            return last;
        }
        private void UpdateDialogTouchSortings(OriginalUiDialog active)
        {
            foreach (var item in dialogs) (item.Value ?? throw new NullReferenceException()).SetTouchSorting(0);
            (active ?? throw new NullReferenceException()).SetTouchSorting(-11);
        }
        private void EnableTouchForGame() => (context ?? throw new NullReferenceException()).EnableTouchForGame();
    }
}
