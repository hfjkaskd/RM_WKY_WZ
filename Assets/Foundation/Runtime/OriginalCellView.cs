using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCellView : MonoBehaviour, IOriginalPoolable
    {
        [SerializeField] public OriginalCellMask mask;
        [SerializeField] public OriginalCellBorder border;
        [SerializeField] public SpriteRenderer background, front;
        [SerializeField] public OriginalCellViewSettings settings;
        private IOriginalCellViewFactory factory;
        private OriginalCellFreezeCountdown countdown;
        private bool isVisibleOutsideMask;
        public IOriginalCellViewDelegate ViewDelegate { get; private set; }
        public OriginalCellPoint CellPoint => ViewDelegate.Point; // 0x3446cc0.
        public Vector2 Position => transform.position; // 0x343e6bc.
        public void BindFreezeCountdown(OriginalCellFreezeCountdown value) { countdown = value; }

        public void InitializeView(IOriginalCellViewDelegate model, IOriginalCellViewFactory itemFactory) // 0x3446d60.
        {
            ViewDelegate = model;
            factory = itemFactory;
            var own = transform;
            own.SetParent(itemFactory.CellParent);
            float z = own.localPosition.z;
            float x = ViewDelegate.Point.X;
            float y = ViewDelegate.Point.Y;
            own.localPosition = new Vector3(x, y, z);
            own.localScale = Vector3.one;
            var assets = itemFactory.LoadCellAssets();
            SetCellColor();
            bool filling = ViewDelegate.IsFillingCell;
            mask.Enable(filling);
            border.Init(assets, itemFactory.IsWaterLevel);
        }

        private void SetCellColor() // 0x3446fe8.
        {
            front.enabled = true;
            front.color = settings.altColor;
            background.enabled = true;
            background.color = settings.mainColor;
        }

        public void ShowView(bool show) // 0x3447110, ordinary-level branch.
        {
            front.gameObject.SetActive(show);
            background.gameObject.SetActive(show);
            if (show)
            {
                border.ShowParts(0, false);
                if (factory.IsShifterLevel) throw new NotSupportedException("Shifter cell border factory is not restored.");
            }
            else border.HideParts();
        }

        public void SetCellMaskState(bool active) => mask.Enable(active); // 0x343dadc.
        public void ActivateBorders() => border.ActivateBorders(); // 0x344004c.
        public void EnableMaskInteraction() { SetMaskInteraction(SpriteMaskInteraction.VisibleOutsideMask); isVisibleOutsideMask = true; }
        public void DisableMaskInteraction() { SetMaskInteraction(SpriteMaskInteraction.None); isVisibleOutsideMask = false; }
        private void SetMaskInteraction(SpriteMaskInteraction value) // 0x3447cf0.
        {
            front.maskInteraction = value;
            background.maskInteraction = value;
            for (int i = 0; i < border.borderRenderers.Length; i++) border.borderRenderers[i].maskInteraction = value;
            for (int i = 0; i < border.fillRenderers.Length; i++) border.fillRenderers[i].maskInteraction = value;
        }
        public int GetPoolId() => 0; // 0x344738c.
        public void OnSpawn() { } // 0x3447394.
        public void OnRecycle() // 0x3447398, ordinary-level branch; shifter ownership is not installed.
        {
            if (countdown != null) countdown.ResetOnRecycle();
            border.OnRecycle();
            if (isVisibleOutsideMask) DisableMaskInteraction();
        }
        private void Update() { Tick(); }
        internal void Tick() // 0x3446be4; shared countdown is also used by CellModel.FreezeFallForDuration.
        {
            if (countdown != null && countdown.IsCounting) countdown.Update(factory.LevelDeltaTime);
        }
    }
}
