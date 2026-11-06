using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public interface IOriginalRemainingMovesDialogContext
    {
        OriginalRemainingMovesManager GetRemainingMovesManager();
        void UpdateDialogParent(Transform view, OriginalDialogConfig config);
        Vector3 GetCameraCenter();
        Vector3 UiScale { get; }
        void ResetTopUiSorting();
        void SetDialogTouchSorting(int order);
    }
    public sealed class OriginalRemainingMovesDialog : MonoBehaviour, IOriginalFlowDialog, IOriginalRemainingMovesDialog, IOriginalUiDialogHost
    {
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private int dialogSortingLayer;
        [SerializeField] private OriginalDialogConfig config;
        [SerializeField] private float blackFadeOutDuration;
        private IOriginalRemainingMovesDialogContext context;
        private OriginalRemainingMovesManager remaining;
        private RemainingLifecycle lifecycle;
        private int uiLayer, uiOrder;
        private float fadeElapsed;
        private bool fadePending;
        public OriginalUiDialog Lifecycle => lifecycle;
        public void Bind(OriginalFlowAction action, IOriginalUiDialogManager manager, IOriginalRemainingMovesDialogContext context)
        {
            this.context = context; uiLayer = sortingGroup.sortingLayerID; uiOrder = sortingGroup.sortingOrder;
            lifecycle = new RemainingLifecycle(this, new OriginalUiDialogAction(action, manager));
        }
        public void Show() => (lifecycle ?? throw new NullReferenceException()).Show();
        public void CloseRemainingMovesDialog() => (lifecycle ?? throw new NullReferenceException()).Close();
        private void Update()
        {
            if (!fadePending) return;
            fadeElapsed += Time.deltaTime;
            if (fadeElapsed < blackFadeOutDuration) return;
            fadePending = false;
            RequiredContext.ResetTopUiSorting();
            (remaining ?? throw new NullReferenceException()).SkipView?.ShowSkipText();
        }
        private IOriginalRemainingMovesDialogContext RequiredContext => context ?? throw new NullReferenceException();
        void IOriginalUiDialogHost.UpdateParent(OriginalUiDialog dialog) => RequiredContext.UpdateDialogParent(transform, dialog.GetConfig());
        Vector3 IOriginalUiDialogHost.GetCameraCenter() => RequiredContext.GetCameraCenter();
        void IOriginalUiDialogHost.SetActive(bool active) => gameObject.SetActive(active);
        void IOriginalUiDialogHost.StartDialogCoroutine(IEnumerator routine) => StartCoroutine(routine);
        void IOriginalUiDialogHost.DestroyDialog(float delay) => Destroy(gameObject, delay);
        private sealed class RemainingLifecycle : OriginalUiDialog
        {
            private readonly OriginalRemainingMovesDialog owner;
            public RemainingLifecycle(OriginalRemainingMovesDialog owner, IOriginalUiDialogAction action) : base(action, owner) { this.owner = owner; }
            public override OriginalDialogConfig GetConfig() => owner.config;
            public override void SetSortingOffset(int offset)
            { owner.sortingGroup.sortingLayerID = owner.dialogSortingLayer; owner.sortingGroup.sortingOrder = offset; }
            public override void SetTouchSorting(int order) => owner.RequiredContext.SetDialogTouchSorting(order);
            protected override void OnShow(Vector3 position) //0x11187b0.
            {
                owner.remaining = owner.RequiredContext.GetRemainingMovesManager();
                owner.transform.position = position; owner.transform.localScale = owner.RequiredContext.UiScale;
                (owner.remaining ?? throw new NullReferenceException()).StartRemainingMovesWithDialog(owner);
                owner.sortingGroup.sortingLayerID = owner.uiLayer; owner.sortingGroup.sortingOrder = owner.uiOrder;
                owner.fadeElapsed = 0; owner.fadePending = true;
            }
        }
    }
}
