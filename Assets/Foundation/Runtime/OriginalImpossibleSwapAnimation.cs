using System;

namespace Whitebox.Foundation
{
    public interface IOriginalImpossibleSwapServices
    {
        void PlaySfx(int id, float delay);
        void PlayHaptic(int id, int type, float strength, float duration, bool isReplay);
        IOriginalSwapAnimationView GetView(OriginalBoardItem item);
        OriginalSwapPoint GetCellLocalPosition(OriginalGridCell cell);
        OriginalSwapPoint GetLocalScale(IOriginalSwapAnimationView view);
        OriginalSwapSorting GetSwapStartSorting();
        void SetSorting(IOriginalSwapAnimationView view, OriginalSwapSorting sorting);
        void ArrangeSelfSorting(IOriginalSwapAnimationView view);
        void MoveToLocal(IOriginalSwapAnimationView view, OriginalSwapPoint target, float duration, int ease, Action complete);
        void ScaleToLocal(IOriginalSwapAnimationView view, OriginalSwapPoint target, float duration, int ease, Action complete);
        void TryPlayStaticInvalidMoveAnimation(OriginalCellPoint target);
    }

    // SwapAction.StartImpossibleSwapAnimation 0x1ee2558 and its five callbacks
    // 0x1ee4b00..0x1ee4f1c. A context belongs to this gesture, as in the native
    // closure: a newly selected action must not overwrite an older scale tail.
    public sealed class OriginalImpossibleSwapAnimation
    {
        private readonly OriginalSwapActionState action;
        private readonly IOriginalImpossibleSwapServices services;
        private readonly OriginalGridCell from;
        private readonly OriginalCellPoint destination;
        private OriginalBoardItem item;
        private IOriginalSwapAnimationView view;
        private OriginalSwapPoint origin, direction, originalScale, secondScale, scaledDirection;
        private float squareX, squareY, length;
        public OriginalImpossibleSwapAnimation(OriginalSwapActionState action, OriginalGridCell from,
            OriginalCellPoint destination, IOriginalImpossibleSwapServices services)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.destination = destination;
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public void Play()
        {
            services.PlaySfx(193, .04f);
            services.PlayHaptic(17, 1, .7f, 0f, false);
            action.BeginImpossibleAnimation(from);
            item = from.Mediation.CurrentItem;
            view = services.GetView(item);
            origin = services.GetCellLocalPosition(from);
            originalScale = services.GetLocalScale(services.GetView(item));
            direction = new OriginalSwapPoint(Compare(from.X, destination.X), Compare(from.Y, destination.Y), 0f);
            squareX = direction.X * direction.X;
            squareY = direction.Y * direction.Y;
            length = squareX + squareY;
            length = (float)Math.Sqrt(length);
            direction = length <= .00001f ? new OriginalSwapPoint(0f, 0f, 0f)
                : new OriginalSwapPoint(direction.X / length, direction.Y / length, 0f / length);
            services.SetSorting(view, services.GetSwapStartSorting());
            view.SwapAnimationStarted(false, 0);
            services.MoveToLocal(view, Offset(.12f), .075f, 3, MoveBack);
            var firstScale = new OriginalSwapPoint(originalScale.X * 1.05f, originalScale.Y * .96f, 0f);
            secondScale = new OriginalSwapPoint(originalScale.X * .95f, originalScale.Y * 1.04f, 0f);
            // Native Approximately uses strict bounds, then swaps the already
            // scaled x/y components (including non-uniform starting scales).
            if (Math.Abs(direction.X) > 1f - .001f && Math.Abs(direction.X) < 1f + .001f)
            {
                firstScale = new OriginalSwapPoint(firstScale.Y, firstScale.X, 0f);
                secondScale = new OriginalSwapPoint(secondScale.Y, secondScale.X, 0f);
            }
            services.ScaleToLocal(view, firstScale, .075f, 3, ScaleBack);
            services.TryPlayStaticInvalidMoveAnimation(destination);
        }
        private static int Compare(int a, int b) => a < b ? 1 : a > b ? -1 : 0;
        private OriginalSwapPoint Offset(float distance)
        {
            // Force the ARM64 multiply-before-add single-precision boundary.
            scaledDirection.X = direction.X * distance;
            scaledDirection.Y = direction.Y * distance;
            scaledDirection.Z = direction.Z * distance;
            return new OriginalSwapPoint(origin.X + scaledDirection.X, origin.Y + scaledDirection.Y, origin.Z + scaledDirection.Z);
        }
        private void MoveBack() => services.MoveToLocal(view, Offset(-.07f), .075f, 4, MoveHome);
        private void MoveHome() => services.MoveToLocal(view, origin, .075f, 4, FinishMove);
        private void FinishMove()
        {
            if (item.Mediator.CurrentCell != null) services.ArrangeSelfSorting(view);
            view.SwapAnimationEnded();
            action.FinishImpossibleAnimation();
        }
        private void ScaleBack() => services.ScaleToLocal(view, secondScale, .075f, 4, ScaleHome);
        private void ScaleHome() => services.ScaleToLocal(view, originalScale, .075f, 3, null);
    }
}
