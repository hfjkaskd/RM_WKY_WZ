using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalGameTouchInput
    {
        Vector2 GetMouseWorldPosition();
        bool GetMouseButtonDown();
        bool GetMouseButton();
        bool GetMouseButtonUp();
    }
    // Bound by the restored UI/move/booster/grid/level-touch contexts. There
    // are no permissive move, booster or UI defaults in the input listener.
    public interface IOriginalGameTouchServices
    {
        bool IsUiTouching { get; }
        bool IsBoosterSelected { get; }
        bool CanUserMakeMove();
        OriginalGridCell GetCellFromWorldPosition(Vector2 position);
        bool ShouldDeselectBoosterOnTouch(OriginalGridCell cell);
        void DeselectBooster();
        void CancelTouchedItemAnimation();
        void TouchCell(OriginalGridCell cell);
        bool SwapCells(OriginalGridCell from, OriginalCellPoint to);
        bool TapCell(OriginalGridCell cell);
    }
    public interface IOriginalGameTouchListenerOwner
    {
        void BindTouchListener(OriginalGameTouchListener listener);
    }

    // GameTouchListener 0x1e3c2c0..0x1e3cf38. Keep state across UI blocking,
    // large-distance release and Clear, as the native implementation does.
    public sealed class OriginalGameTouchListener
    {
        private readonly IOriginalGameTouchInput input;
        private readonly IOriginalGameTouchServices services;
        public bool IsTouching { get; private set; }
        public Vector2 StartPosition { get; private set; }
        public OriginalGridCell SelectedCell { get; private set; }
        public int TouchDisableCount { get; private set; }
        public int LastPressFrame { get; private set; } = -1;
        // Fields force single-precision arithmetic boundaries on Mono.
        private float deltaX, deltaY, squareX, squareY, magnitude, angle, quotient;
        public OriginalGameTouchListener(IOriginalGameTouchInput input, IOriginalGameTouchServices services)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            if (services is IOriginalGameTouchListenerOwner owner) owner.BindTouchListener(this);
        }
        public void Clear(bool gameExit) { TouchDisableCount = 0; }
        public void DisableTouch() { TouchDisableCount = unchecked(TouchDisableCount + 1); }
        public void EnableTouch()
        {
            if (TouchDisableCount != 0) { TouchDisableCount = unchecked(TouchDisableCount - 1); return; }
            Debug.LogError("Game touch enable has no matching disable.");
        }
        public bool IsTouchDisabled() => TouchDisableCount > 0 || !services.CanUserMakeMove();
        public void ManualUpdate()
        {
            if (!services.IsUiTouching)
            {
                var position = input.GetMouseWorldPosition();
                if (!TryDeselectBooster(position) && !IsTouchDisabled())
                {
                    if (input.GetMouseButtonDown()) { TouchDown(position); return; }
                    if (input.GetMouseButton()) { TouchMove(position); return; }
                    if (input.GetMouseButtonUp()) TouchUp(position);
                    return;
                }
            }
            services.CancelTouchedItemAnimation();
        }
        private bool TryDeselectBooster(Vector2 position)
        {
            if (!services.IsBoosterSelected || !input.GetMouseButtonDown()) return false;
            var cell = services.GetCellFromWorldPosition(position);
            if (!services.ShouldDeselectBoosterOnTouch(cell)) return false;
            services.DeselectBooster();
            StopTouch();
            return true;
        }
        public void TouchDown(Vector2 position)
        {
            LastPressFrame = Time.frameCount;
            StopTouch();
            var cell = services.GetCellFromWorldPosition(position);
            if (cell != null) StartTouch(position, cell);
        }
        public void StartTouch(Vector2 position, OriginalGridCell cell)
        {
            SelectedCell = cell;
            IsTouching = true;
            StartPosition = position;
            services.TouchCell(cell);
        }
        public void StopTouch()
        {
            services.CancelTouchedItemAnimation();
            SelectedCell = null;
            IsTouching = false;
            StartPosition = Vector2.zero;
        }
        public void TouchMove(Vector2 position)
        {
            if (services.IsBoosterSelected || !IsTouching) return;
            MeasureDelta(position);
            // Native b.mi rejects ordered LT, allowing NaN to reach the angle
            // calculation (which rejects its unordered comparisons).
            if (!(magnitude < .5f) && GetTowardsCell(new Vector2(deltaX, deltaY), out var point))
            {
                TriggerOnCellsSwapped(SelectedCell, point);
                StopTouch();
            }
        }
        public void TouchUp(Vector2 position)
        {
            services.CancelTouchedItemAnimation();
            MeasureDelta(position);
            if (magnitude > .5f) return; // No StopTouch on this native branch.
            var cell = services.GetCellFromWorldPosition(position);
            if (cell != null && ReferenceEquals(cell, SelectedCell)) TriggerOnTapCell(cell);
            StopTouch();
        }
        private void MeasureDelta(Vector2 position)
        {
            deltaX = position.x - StartPosition.x;
            deltaY = position.y - StartPosition.y;
            squareX = deltaX * deltaX;
            squareY = deltaY * deltaY;
            magnitude = squareX + squareY;
            magnitude = (float)Math.Sqrt(magnitude);
        }
        public bool GetTowardsCell(Vector2 delta, out OriginalCellPoint point)
        {
            quotient = delta.y / delta.x;
            angle = (float)Math.Atan(quotient);
            if (SelectedCell == null) throw new NullReferenceException();
            angle *= 57.295776f;
            if (angle < 0f) angle = -angle;
            int x = SelectedCell.X, y = SelectedCell.Y;
            point = new OriginalCellPoint(x, y);
            if (angle <= 30f) x = unchecked(x + (delta.x <= 0f ? -1 : 1));
            else if (angle >= 60f) y = unchecked(y + (delta.y <= 0f ? -1 : 1));
            else return false;
            point = new OriginalCellPoint(x, y);
            return true;
        }
        // Native methods additionally format debug logs. Formatting/boxing is
        // omitted here; game dispatch and its return-dependent state are kept.
        public void TriggerOnCellsSwapped(OriginalGridCell from, OriginalCellPoint to) => services.SwapCells(from, to);
        public void TriggerOnTapCell(OriginalGridCell cell) => services.TapCell(cell);
    }
}
