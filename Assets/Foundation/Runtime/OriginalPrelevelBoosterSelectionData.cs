namespace Whitebox.Foundation
{
    // Native prelevel session state used by initial booster placement and retry.
    // Persistence/preselection readers are restored separately; no synthetic
    // inventory or stored selection is substituted here.
    public sealed class OriginalPrelevelBoosterSelectionData
    {
        public bool DidTouchRocket, DidTouchTnt, DidTouchLightball;
        private bool rocketSelected, tntSelected, lightballSelected;
        private bool rocketHasTime, tntHasTime, lightballHasTime;
        private bool rocketNoPossibleMoveFailed, tntNoPossibleMoveFailed, lightballNoPossibleMoveFailed;
        public int LevelRocketBoosterSource { get; private set; }
        public int LevelTntBoosterSource { get; private set; }
        public int LevelLightballBoosterSource { get; private set; }
        public bool ShouldUseRocket => rocketSelected || rocketHasTime || rocketNoPossibleMoveFailed; // 0x32bab44.
        public bool ShouldUseTnt => tntSelected || tntHasTime || tntNoPossibleMoveFailed; // 0x32bab6c.
        public bool ShouldUseLightBall => lightballSelected || lightballHasTime || lightballNoPossibleMoveFailed; // 0x32bab94.
        public bool IsSelected(int type) // 0x32babbc, original BoosterType1/2/3.
        { switch (type) { case 1:return rocketSelected; case 2:return tntSelected; case 3:return lightballSelected; default:return false; } }
        public bool HasTime(int type) // 0x32babfc.
        { switch (type) { case 1:return rocketHasTime; case 2:return tntHasTime; case 3:return lightballHasTime; default:return false; } }
        public void ToggleForTime(int type, bool hasTime) // 0x32bac3c.
        { switch (type) { case 1:rocketHasTime = hasTime;break; case 2:tntHasTime = hasTime;break; case 3:lightballHasTime = hasTime;break; } }
        public void SetNoPossibleMoveFailed(int type) // 0x32b165c.
        { switch (type) { case 1:rocketNoPossibleMoveFailed = true;break; case 2:tntNoPossibleMoveFailed = true;break; case 3:lightballNoPossibleMoveFailed = true;break; } }
        public void Toggle(int type) // 0x32b2670: does not set didTouch.
        { switch (type) { case 1:rocketSelected = !rocketSelected;break; case 2:tntSelected = !tntSelected;break; case 3:lightballSelected = !lightballSelected;break; } }
        public void SetLevelBoosterUsages() // 0x32bac70: timed/retry source takes priority over selection.
        {
            LevelRocketBoosterSource = rocketHasTime || rocketNoPossibleMoveFailed ? -1 : rocketSelected ? 1 : 0;
            LevelTntBoosterSource = tntHasTime || tntNoPossibleMoveFailed ? -1 : tntSelected ? 1 : 0;
            LevelLightballBoosterSource = lightballHasTime || lightballNoPossibleMoveFailed ? -1 : lightballSelected ? 1 : 0;
        }
        public void ResetBooster(int type) // 0x32bacd4: source value is retained.
        {
            switch (type)
            {
                case 1:rocketSelected = false;rocketHasTime = false;rocketNoPossibleMoveFailed = false;DidTouchRocket = false;break;
                case 2:tntSelected = false;tntHasTime = false;tntNoPossibleMoveFailed = false;DidTouchTnt = false;break;
                case 3:lightballSelected = false;lightballHasTime = false;lightballNoPossibleMoveFailed = false;DidTouchLightball = false;break;
            }
        }
        public void Reset() // 0x32bad28: twelve booleans cleared; three source integers retained.
        {
            DidTouchRocket = false;DidTouchTnt = false;DidTouchLightball = false;
            rocketSelected = false;tntSelected = false;lightballSelected = false;
            rocketHasTime = false;tntHasTime = false;lightballHasTime = false;
            rocketNoPossibleMoveFailed = false;tntNoPossibleMoveFailed = false;lightballNoPossibleMoveFailed = false;
        }
    }
}
