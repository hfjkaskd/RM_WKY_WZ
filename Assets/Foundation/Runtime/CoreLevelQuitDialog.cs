using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    public sealed class CoreLevelQuitDialog : MonoBehaviour, IOriginalUiDialogHost
    {
        [SerializeField] private Button closeButton, quitButton;
        [SerializeField] private Canvas[] buttonCanvases;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private OriginalDialogEntrance entrance;
        private CoreGameFlow owner;
        private OriginalUiDialog lifecycle;
        private bool retainBackdrop;

        public Button CloseButton => closeButton;
        public Button QuitButton => quitButton;
        public OriginalUiDialog Lifecycle => lifecycle;

        internal void Open(OriginalFlowAction action, IOriginalUiDialogManager manager, CoreGameFlow flow)
        {
            owner = flow;
            foreach (var canvas in buttonCanvases) canvas.worldCamera = owner.UiCamera;
            closeButton.onClick.AddListener(Cancel);
            quitButton.onClick.AddListener(Confirm);
            lifecycle = new QuitDialog(this, new OriginalUiDialogAction(action, manager));
            lifecycle.Show();
        }
        private void Cancel() => lifecycle.Close();
        private void Confirm()
        {
            if (lifecycle.IsClosing || !owner.TryBeginLevelQuit()) return;
            retainBackdrop = !owner.IsBonus;
            lifecycle.Close(); // 0x1340c64 closes before starting the ordinary quit result.
            owner.CompleteLevelQuit();
        }
        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Cancel);
            quitButton.onClick.RemoveListener(Confirm);
        }

        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog) => owner.UpdateDialogParent(transform, dialog.GetConfig());
        Vector3 IOriginalUiDialogHost.GetCameraCenter() => owner.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active) => gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine) => StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay)
        {
            gameObject.SetActive(false);
            Destroy(gameObject, delay);
        }

        private sealed class QuitDialog : OriginalUiDialog
        {
            private readonly CoreLevelQuitDialog view;
            internal QuitDialog(CoreLevelQuitDialog view, IOriginalUiDialogAction action) : base(action, view) { this.view = view; }
            public override OriginalDialogConfig GetConfig()
            {
                var config = view.config;
                config.ShouldShowBackgroundOnClose = view.retainBackdrop; // 0x1341924.
                return config;
            }
            public override void SetSortingOffset(int offset) => view.sortingGroup.sortingOrder = offset;
            public override void SetTouchSorting(int order) { }
            protected override void OnShow(Vector3 position) =>
                view.StartCoroutine(view.entrance.Play(view.transform, position, view.owner.UiScale, view.owner));
            protected override void OnClose()
            {
                view.gameObject.SetActive(false);
                view.owner.ClearQuitDialog();
            }
        }
    }
}
