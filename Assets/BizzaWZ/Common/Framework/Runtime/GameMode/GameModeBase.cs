using System.Collections.Generic;
using Bizza;
using UnityEngine;

public abstract class GameModeBase : Actor
{
    public float StartTime { get; protected set; }
    public float RunningTime => World.Current.GameTime - StartTime;

    public abstract E_GameModeType GameModeType { get; }

    protected override void OnInit()
    {
        StartTime = World.Current.GameTime;
    }

    protected override void GetComponents()
    {
        var tempList = StaticSimplePool<List<ActorMonoCmpt>>.Pool.Get();
        GetComponentsInChildren(tempList);
        foreach (var cmpt in tempList)
        {
            _components.Add(cmpt);
        }

        tempList.Clear();
        StaticSimplePool<List<ActorMonoCmpt>>.Pool.Release(tempList);
    }
}

public abstract class GameModeBase<T> : GameModeBase where T : GameModeBase<T>
{
    public static T Instance => World.Current.CurGameMode as T;
}