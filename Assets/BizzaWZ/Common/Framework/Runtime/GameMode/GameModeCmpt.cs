public abstract class GameModeCmpt<T> : ActorCmpt<GameModeBase> where T : GameModeCmpt<T>
{
    public static T Instance
    {
        get;
        private set;
    }

    protected override void OnInitBegin(Actor actor)
    {
        base.OnInitBegin(actor);
        Instance = this as T;
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        if (Instance == this)
        {
            Instance = null;
        }
    }
}