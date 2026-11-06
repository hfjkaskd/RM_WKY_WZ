using System;
using System.Threading.Tasks;

internal static partial class Program
{
    private static async Task RunProjectRegressions()
    {
        Reset();
        World.Current.CurGameMode.GameModeType = E_GameModeType.MainMenu;
        Queue.Enqueue("AfterEnteringGame"); UI.Tick();
        Assert(UI.Attempts.Count == 0, "Main menu must not display gameplay automatic popups.");
        World.Current.CurGameMode.GameModeType = E_GameModeType.GamePlay; UI.Tick();
        Assert(UI.LastPopup.PageId == "AfterEnteringGame", "Gameplay readiness did not resume the queue.");
        Pass("BizzaWZ game mode gates automatic popup presentation");

        Reset();
        World.Current.IsPause = true;
        Queue.Enqueue("AfterPause"); UI.Tick();
        Assert(UI.Attempts.Count == 0, "Paused gameplay displayed an automatic popup.");
        World.Current.IsPause = false; UI.Tick();
        Assert(UI.LastPopup.PageId == "AfterPause", "Resuming gameplay did not resume presentation.");
        Pass("BizzaWZ world pause delays new popup presentation");

        var account = Reset(); ShortTimeouts(); account.TriggerDaily();
        var daily = UI.LastPopup;
        TransitionBlock.Instance = new TransitionBlock();
        UI.Tick(); Time.unscaledTime = 100; UI.Tick();
        Assert(UI.Registered(daily), "Scene transition counted against content timeout.");
        TransitionBlock.Instance.gameObject.SetActive(false); UI.Tick();
        account.RespondWithdrawal(true); UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today,
            "Scene transition prevented successful content confirmation after resuming.");
        Pass("BizzaWZ transition overlay pauses content timeout and confirmation");

        account = Reset(); ShortTimeouts(); account.TriggerDaily(); daily = UI.LastPopup;
        World.Current.IsPause = true;
        Time.unscaledTime = 4.1f; UI.Tick();
        Assert(!UI.Registered(daily) && !daily.gameObject.activeInHierarchy,
            "Popup-induced gameplay pause prevented its own content timeout.");
        Pass("BizzaWZ popup-induced pause cannot disable content timeout");

        Reset();
        var manual = await UI.OpenPage("ManualNotice");
        UI.ClosePage(manual);
        Assert(!UI.HasPopup && manual.gameObject.activeInHierarchy, "Test did not enter the hide-animation gap.");
        Queue.Enqueue("AfterManualHide"); UI.Tick();
        Assert(UI.Attempts.Count == 1, "Automatic popup overlapped a manually opened page's hide animation.");
        UI.FinishClose(manual); UI.Tick();
        Assert(UI.LastPopup.PageId == "AfterManualHide", "Finished manual hide still blocked the queue.");
        Pass("BizzaWZ waits for non-queue popup hide animation");

        account = Reset(); account.TriggerDaily();
        FlowModule.SetAutomaticPopupFlowBlocked(true);
        account.RespondWithdrawal(true); UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
            "Settlement recorded a popup as visible before gameplay resumed.");
        FlowModule.SetAutomaticPopupFlowBlocked(false); UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today,
            "Ending settlement did not confirm successfully loaded daily content.");
        Pass("BizzaWZ settlement pauses successful content confirmation");

        Reset(); ShortTimeouts(); int slowShown = 0; TestPage slowPage = null;
        UI.OpenHandler = (id, data) => id == "SlowInit"
            ? Task.FromResult<UIPageBase>(slowPage = new TestPage
                { RawPageId = id, Initialize = _ => Time.unscaledTime = 10 })
            : UIModule.DefaultOpen(id, data);
        Queue.Enqueue("SlowInit", onShown: () => slowShown++); Queue.Enqueue("AfterSlowInit");
        UI.Tick(); await Drain(); UI.Tick(); UI.Tick();
        Assert(!UI.Registered(slowPage) && !slowPage.gameObject.activeInHierarchy && slowShown == 0 &&
            UI.LastPopup.PageId == "AfterSlowInit", "Slow completed initialization bypassed opening timeout.");
        Pass("slow completed synchronous initialization is rolled back");

        Reset(); ShortTimeouts(); int shown = 0;
        Queue.Enqueue("UserReading", onShown: () => shown++); Queue.Enqueue("NextNotice");
        UI.Tick(); UI.Tick(); var reading = UI.LastPopup;
        Time.unscaledTime = 3600; UI.Tick();
        Assert(UI.Registered(reading) && UI.Attempts.Count == 1 && shown == 1,
            "A successfully displayed popup timed out while the user was reading.");
        Close(reading); UI.Tick();
        Assert(UI.LastPopup.PageId == "NextNotice", "Normal dismissal did not advance the queue.");
        Pass("confirmed popup waits for dismissal without a loading deadline");
    }
}
