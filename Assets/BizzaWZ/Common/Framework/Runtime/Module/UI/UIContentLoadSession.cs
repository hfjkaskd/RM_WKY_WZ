using System;

public enum UIContentState { Closed, Loading, Ready, Empty, Failed }

/// <summary>
/// Main-thread content request lifetime. Pass an unscaled clock to Begin/Accept/Tick.
/// Each refresh gets a new version; closing, failure and timeout reject late callbacks.
/// This guards presentation only and does not cancel the underlying HTTP operation.
/// </summary>
public sealed class UIContentLoadSession
{
    public UIContentState State { get; private set; } = UIContentState.Closed;
    public int Version { get; private set; }
    public event Action<UIContentState> StateChanged;
    private float deadline;

    public int Begin(float now, float timeoutSeconds)
    {
        if (timeoutSeconds <= 0 || float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds))
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        Version++;
        deadline = now + timeoutSeconds;
        SetState(UIContentState.Loading);
        return Version;
    }

    public bool Accept(int version, float now)
    {
        if (version != Version || State != UIContentState.Loading) return false;
        Tick(now); // A callback arriving before Update must still respect the deadline.
        return State == UIContentState.Loading;
    }

    public bool Complete(int version, UIContentState state, float now)
    {
        if (state != UIContentState.Ready && state != UIContentState.Empty && state != UIContentState.Failed)
            throw new ArgumentOutOfRangeException(nameof(state));
        if (!Accept(version, now)) return false;
        SetState(state);
        return true;
    }

    public void Tick(float now)
    {
        if (State == UIContentState.Loading && now >= deadline) SetState(UIContentState.Failed);
    }

    public void Close()
    {
        Version++;
        SetState(UIContentState.Closed);
    }

    private void SetState(UIContentState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
