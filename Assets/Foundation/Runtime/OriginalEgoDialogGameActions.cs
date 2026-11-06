using System;

namespace Whitebox.Foundation
{
    // Game-action branches of EgoDialog.OnClose0x1346c60. The dialog's
    // selection/warning/shop/offer/render/close lifecycle remains a required
    // owner; these actions must execute on close, not at button press time.
    public sealed class OriginalEgoDialogGameActions
    {
        private readonly OriginalEgoManager ego;
        private readonly OriginalWinFailManager winFail;
        private readonly IOriginalEgoContext context;
        public OriginalEgoDialogGameActions(OriginalEgoManager ego, OriginalWinFailManager winFail, IOriginalEgoContext context)
        { this.ego = ego; this.winFail = winFail; this.context = context; }
        public void PaidPlayOn() // 0x1346f20..0x1346f68, Clear(false) at0x13474bc.
        {
            (ego ?? throw new NullReferenceException()).PlayOn(false);
            (winFail ?? throw new NullReferenceException()).Clear(false);
        }
        public void FreePlayOn() // 0x1347368..0x13473bc.
        {
            (ego ?? throw new NullReferenceException()).PlayOn(true);
            (winFail ?? throw new NullReferenceException()).Clear(false);
        }
        public void CancelOffer() // 0x1346f6c..0x1346fbc, Clear(true) at0x13474dc.
        {
            (ego ?? throw new NullReferenceException()).CancelOffer();
            (winFail ?? throw new NullReferenceException()).Clear(true);
            (context ?? throw new NullReferenceException()).SetEgoOfferStatus(false, false);
        }
    }
}
