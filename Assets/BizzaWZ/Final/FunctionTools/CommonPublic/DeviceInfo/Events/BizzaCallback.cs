using System;
using System.Runtime.CompilerServices;

public class BizzaCallback
{
    protected Action callback0;

    public void On(Action cb0)
    {
        callback0 += cb0;
    }

    public void Off(Action cb0)
    {
        callback0 -= cb0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Clear()
    {
        callback0 = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke()
    {
        callback0?.Invoke();
    }
}

public class BizzaCallback<T1> : BizzaCallback
{
    protected Action<T1> callback1;

    public void On(Action<T1> cb1)
    {
        callback1 += cb1;
    }

    public void Off(Action<T1> cb1)
    {
        callback1 -= cb1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke(T1 arg1)
    {
        base.Invoke();
        callback1?.Invoke(arg1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Clear()
    {
        callback1 = null;
        base.Clear();
    }
}

public class BizzaCallback<T1, T2> : BizzaCallback<T1>
{
    protected Action<T1, T2> callback2;

    public void On(Action<T1, T2> cb2)
    {
        callback2 += cb2;
    }

    public void Off(Action<T1, T2> cb2)
    {
        callback2 -= cb2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke(T1 arg1, T2 arg2)
    {
        base.Invoke(arg1);
        callback2?.Invoke(arg1, arg2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Clear()
    {
        callback2 = null;
        base.Clear();
    }
}

public class BizzaCallback<T1, T2, T3> : BizzaCallback<T1, T2>
{
    protected Action<T1, T2, T3> callback3;

    public void On(Action<T1, T2, T3> cb3)
    {
        callback3 += cb3;
    }

    public void Off(Action<T1, T2, T3> cb3)
    {
        callback3 -= cb3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke(T1 arg1, T2 arg2, T3 arg3)
    {
        base.Invoke(arg1, arg2);
        callback3?.Invoke(arg1, arg2, arg3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Clear()
    {
        callback3 = null;
        base.Clear();
    }
}

public class BizzaCallback<T1, T2, T3, T4> : BizzaCallback<T1, T2, T3>
{
    protected Action<T1, T2, T3, T4> callback4;

    public void On(Action<T1, T2, T3, T4> cb4)
    {
        callback4 += cb4;
    }

    public void Off(Action<T1, T2, T3, T4> cb4)
    {
        callback4 -= cb4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        base.Invoke(arg1, arg2, arg3);
        callback4?.Invoke(arg1, arg2, arg3, arg4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Clear()
    {
        callback4 = null;
        base.Clear();
    }
}

public class BizzaCallback<T1, T2, T3, T4, T5> : BizzaCallback<T1, T2, T3, T4>
{
    protected Action<T1, T2, T3, T4, T5> callback5;

    public void On(Action<T1, T2, T3, T4, T5> cb5)
    {
        callback5 += cb5;
    }

    public void Off(Action<T1, T2, T3, T4, T5> cb5)
    {
        callback5 -= cb5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        base.Invoke(arg1, arg2, arg3, arg4);
        callback5?.Invoke(arg1, arg2, arg3, arg4, arg5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Clear()
    {
        callback5 = null;
        base.Clear();
    }
}