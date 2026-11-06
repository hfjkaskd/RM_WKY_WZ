
/// <summary>
/// 游戏加载阶段
/// 加载数据表=>加载主场景
/// </summary>
public class GameMode_MainMenu : GameModeBase
{
    private bool canShowGear = false;
    public override E_GameModeType GameModeType => E_GameModeType.MainMenu;

    protected override void LateInit()
    {
        // UIModule.Instance.OpenPage(UIPageIds.UI_MenuPage).Forget();
        // AssetModule.LoadSceneWithBlock(D_ScenePath.MainMenu);
        AssetUtils.LoadSceneWithBlock("MainMenu");
        World.Current.ResumeAll();

        // GameUtils.DelayDo(() =>
        // {
            // CurrentWorld.ChangeGameMode(E_GameModeType.GamePlay);
        // }, 2);
        UIModule.Instance.OpenPage(UIPageIds.SeabedMenuPanel);

        LogLogger.LogInfo("CheckContinueGame");
    }

    protected override void OnPreRelease()
    {
        // UIModule.Instance.ClosePage(UIPageIds.UI_MenuPage);
        int layer = UIModule.Instance.GetLayerByName(nameof(E_UILayer.BaseLayer));
        UIModule.Instance.CloseAndDestroyLayerAllPage(layer);
    }
}

public static partial class UIPageIds
{
    public static readonly PageId SeabedMenuPanel = nameof(SeabedMenuPanel);
}
