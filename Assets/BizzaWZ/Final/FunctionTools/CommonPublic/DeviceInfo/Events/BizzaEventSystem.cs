// #define EnableCompatible

using System;
using System.Collections.Generic;

public static class BizzaEventSystem
{
    private static readonly List<BizzaCallback> _list = new();

    public static void Emit(GameEvent eventType)
    {
        if (!_list.HasIndex(eventType.index)) return;
        if (_list[eventType.index] is BizzaCallback callback)
            callback.Invoke();
    }

    public static void Emit<T1>(in GameEvent<T1> eventType,
        in T1 args1)
    {
        if (!_list.HasIndex(eventType.index)) return;
        if (_list[eventType.index] is BizzaCallback<T1> callback)
            callback.Invoke(args1);
    }

    public static void Emit<T1, T2>(in GameEvent<T1, T2> eventType,
        in T1 args1, in T2 args2)
    {
        if (!_list.HasIndex(eventType.index)) return;
        if (_list[eventType.index] is BizzaCallback<T1, T2> callback)
            callback.Invoke(args1, args2);
    }

    public static void Emit<T1, T2, T3>(in GameEvent<T1, T2, T3> eventType,
        in T1 args1, in T2 args2, in T3 args3)
    {
        if (!_list.HasIndex(eventType.index)) return;
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.Invoke(args1, args2, args3);
    }

    public static void Emit<T1, T2, T3, T4>(in GameEvent<T1, T2, T3, T4> eventType,
        in T1 args1, in T2 args2, in T3 args3, in T4 args4)
    {
        if (!_list.HasIndex(eventType.index)) return;
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.Invoke(args1, args2, args3, args4);
    }

    public static void Emit<T1, T2, T3, T4, T5>(in GameEvent<T1, T2, T3, T4, T5> eventType,
        in T1 args1, in T2 args2, in T3 args3, in T4 args4, in T5 args5)
    {
        if (!_list.HasIndex(eventType.index)) return;
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.Invoke(args1, args2, args3, args4, args5);
    }

    public static void On(GameEvent eventType,
        Action action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback();
        if (_list[eventType.index] is BizzaCallback callback)
            callback.On(action);
    }

    public static void On<T1>(GameEvent<T1> eventType,
        Action action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1>();
        if (_list[eventType.index] is BizzaCallback<T1> callback)
            callback.On(action);
    }

    public static void On<T1>(GameEvent<T1> eventType,
        Action<T1> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1>();
        if (_list[eventType.index] is BizzaCallback<T1> callback)
            callback.On(action);
    }

    public static void On<T1, T2>(GameEvent<T1, T2> eventType,
        Action action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2>();
        if (_list[eventType.index] is BizzaCallback<T1, T2> callback)
            callback.On(action);
    }

    public static void On<T1, T2>(GameEvent<T1, T2> eventType,
        Action<T1> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2>();
        if (_list[eventType.index] is BizzaCallback<T1, T2> callback)
            callback.On(action);
    }

    public static void On<T1, T2>(GameEvent<T1, T2> eventType,
        Action<T1, T2> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2>();
        if (_list[eventType.index] is BizzaCallback<T1, T2> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1, T2> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1, T2, T3> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2, T3> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2, T3, T4> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3, T4> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.On(action);
    }

    public static void On<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3, T4, T5> action)
    {
        _list.CompleteList(eventType.index + 1);
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.On(action);
    }

    public static void Off(GameEvent eventType,
        Action action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback();
        if (_list[eventType.index] is BizzaCallback callback)
            callback.Off(action);
    }

    public static void Off<T1>(GameEvent<T1> eventType,
        Action action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1>();
        if (_list[eventType.index] is BizzaCallback<T1> callback)
            callback.Off(action);
    }

    public static void Off<T1>(GameEvent<T1> eventType,
        Action<T1> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1>();
        if (_list[eventType.index] is BizzaCallback<T1> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2>(GameEvent<T1, T2> eventType,
        Action action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2>();
        if (_list[eventType.index] is BizzaCallback<T1, T2> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2>(GameEvent<T1, T2> eventType,
        Action<T1> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2>();
        if (_list[eventType.index] is BizzaCallback<T1, T2> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2>(GameEvent<T1, T2> eventType,
        Action<T1, T2> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2>();
        if (_list[eventType.index] is BizzaCallback<T1, T2> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1, T2> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1, T2, T3> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2, T3> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2, T3, T4> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3, T4> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.Off(action);
    }

    public static void Off<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3, T4, T5> action)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index] ??= new BizzaCallback<T1, T2, T3, T4, T5>();
        if (_list[eventType.index] is BizzaCallback<T1, T2, T3, T4, T5> callback)
            callback.Off(action);
    }

    public static void Clear(GameEvent eventType)
    {
        if (!_list.HasIndex(eventType.index)) return;
        _list[eventType.index]?.Clear();
    }

    public static void Clear()
    {
        _list.Clear();
    }

    public static void Set(GameEvent eventType, Action action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1>(GameEvent<T1> eventType,
        Action action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1>(GameEvent<T1> eventType,
        Action<T1> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2>(GameEvent<T1, T2> eventType,
        Action action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2>(GameEvent<T1, T2> eventType,
        Action<T1> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2>(GameEvent<T1, T2> eventType,
        Action<T1, T2> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1, T2> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3>(GameEvent<T1, T2, T3> eventType,
        Action<T1, T2, T3> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2, T3> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4>(GameEvent<T1, T2, T3, T4> eventType,
        Action<T1, T2, T3, T4> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3, T4> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }

    public static void Set<T1, T2, T3, T4, T5>(GameEvent<T1, T2, T3, T4, T5> eventType,
        Action<T1, T2, T3, T4, T5> action, bool add)
    {
        if (add) On(eventType, action);
        else Off(eventType, action);
    }
}
