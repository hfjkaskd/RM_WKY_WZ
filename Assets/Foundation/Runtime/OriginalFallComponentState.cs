using System;

namespace Whitebox.Foundation
{
    // New native components begin in state zero (Steady). Motion requires the
    // actual item-view, grid, level-time, static-item and SFX service owners.
    public sealed partial class OriginalFallComponentState : IOriginalFallUpdateComponent, IOriginalFallSpeed
    {
        public OriginalBoardItem Item { get; }
        public OriginalFillingCell FillingCell { get; private set; }
        internal int CurrentFallState;
        public bool IsItemSteady => CurrentFallState == 0; // 0x2eea560.
        public bool IsFallingCross { get; internal set; } // 0x2eea570, native +0x2c.
        public OriginalFallComponentState(OriginalBoardItem item)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }
        public void SetFillingCell(OriginalFillingCell cell) { FillingCell = cell; } // 0x2ee89f4.
    }
}
