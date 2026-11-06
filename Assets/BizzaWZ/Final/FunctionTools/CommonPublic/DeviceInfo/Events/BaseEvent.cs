public abstract class BaseEvent
{
    private static int nextIndex;
    public readonly int index;

    protected BaseEvent()
    {
        index = nextIndex++;
    }
}

public class GameEvent : BaseEvent
{
}

public class GameEvent<T1> : BaseEvent
{
}

public class GameEvent<T1, T2> : BaseEvent
{
}

public class GameEvent<T1, T2, T3> : BaseEvent
{
}

public class GameEvent<T1, T2, T3, T4> : BaseEvent
{
}

public class GameEvent<T1, T2, T3, T4, T5> : BaseEvent
{
}