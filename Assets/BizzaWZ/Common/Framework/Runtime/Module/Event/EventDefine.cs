using System.Collections.Generic;

public static partial class EventDefine
{
    public static partial class Frame
    {
        public static readonly GameEvent CancelAllTouch = new();
#if !COMMONGAME
        public static readonly GameEvent<PageId> OpenPage = new();
        public static readonly GameEvent<PageId> ClosePage = new();


        public static readonly GameEvent<E_GameModeType, E_GameModeType> ChangeGameMode = new();
#endif
        public static readonly GameEvent<string, float> ShowTips = new();

        public static readonly GameEvent LanguageChange = new();

        public static readonly GameEvent DataTableLoaded = new();

        public static readonly GameEvent LocalDataLoaded = new();
        public static readonly GameEvent GameDataLoaded = new();
        public static readonly GameEvent LoadingLineComplete = new();
        public static readonly GameEvent LoadingStageComplete = new();//整个加载流程完成
        public static readonly GameEvent LoadingAllGraphsComplete = new();
        public static readonly GameEvent<float> LoadingProgress = new();

        public static readonly GameEvent OnNewDayLogin = new(); //新的一天登录

        public static readonly GameEvent<int> LevelStart = new();
        public static readonly GameEvent<int, bool> LevelEnd = new();

        public static readonly GameEvent PausePanelClose = new();
    }
}
