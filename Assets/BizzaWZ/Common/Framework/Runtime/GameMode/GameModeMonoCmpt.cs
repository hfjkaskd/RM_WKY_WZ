public abstract class GameModeMonoCmpt<T> : ActorMonoCmpt<GameModeBase> where T : GameModeMonoCmpt<T>
{
    public static T Instance
    {
        get;
        private set;
    }

    protected override void OnInit()
    {
        Instance = this as T;
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        Instance = null;
    }
}
