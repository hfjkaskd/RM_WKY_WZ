using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    public sealed class CoreNoPossibleMovesDialog : MonoBehaviour, IOriginalUiDialogHost
    {
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private OriginalDialogEntrance entrance;
        [SerializeField] private Button restartButton;
        [SerializeField] private Canvas[] buttonCanvases;
        private CoreGameFlow owner;

        public OriginalUiDialog Lifecycle { get; private set; }
        public Button RestartButton => restartButton;

        public void Bind(OriginalFlowAction action, IOriginalUiDialogManager manager, CoreGameFlow flow)
        {
            owner = flow;
            foreach (var canvas in buttonCanvases) canvas.worldCamera = owner.UiCamera;
            restartButton.onClick.AddListener(RestartLevel);
            Lifecycle = new Dialog(this, new OriginalUiDialogAction(action, manager));
            Lifecycle.Show();
        }

        private void OnDestroy()
        {
            if (owner != null) restartButton.onClick.RemoveListener(RestartLevel);
        }

        private void RestartLevel() => owner.RestartCurrentLevel(Lifecycle);

        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog) => owner.UpdateDialogParent(transform, dialog.GetConfig());
        Vector3 IOriginalUiDialogHost.GetCameraCenter() => owner.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active) => gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine) => StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay) => Destroy(gameObject, delay);

        private sealed class Dialog : OriginalUiDialog
        {
            private readonly CoreNoPossibleMovesDialog view;

            public Dialog(CoreNoPossibleMovesDialog view, IOriginalUiDialogAction action) : base(action, view)
            {
                this.view = view;
            }

            public override OriginalDialogConfig GetConfig() => view.config;
            public override void SetSortingOffset(int offset) => view.sortingGroup.sortingOrder = offset;
            public override void SetTouchSorting(int order) { }

            protected override void OnShow(Vector3 position) =>
                view.StartCoroutine(view.entrance.Play(view.transform, position, view.owner.UiScale, view.owner));

            protected override void CloseByEscape() => view.RestartLevel();
        }
    }
}
