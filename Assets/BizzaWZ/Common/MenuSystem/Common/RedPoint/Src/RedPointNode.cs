using System;

public class RedPointNode
{
    public readonly string key;
    private readonly RedPointNode parent;
    private bool _selfActive;
    private int _childrenActiveCount;

    public RedPointNode(string key, bool show, RedPointNode parent)
    {
        this.key = key;
        this.parent = parent;
        _selfActive = show;
    }

    public bool ChildActive => _childrenActiveCount > 0;
    public bool Active => _selfActive || ChildActive;
    public bool SelfActive
    {
        get => _selfActive;
        set
        { 
            if (_selfActive != value)
            {
                _selfActive = value;
                if (!ChildActive)
                {
                    BizzaEventSystem.Emit(EventDefine.RedPoint.Refresh, key);
                    Report(parent, value);
                }
            }
        }
    }

    private bool ReportFunc(bool add)
    {
        int value = _childrenActiveCount + (add ? 1 : -1);
        if (_childrenActiveCount != value && value >= 0)
        {
            int prev = _childrenActiveCount;
            _childrenActiveCount = value;
            if (!_selfActive && Math.Sign(prev) != Math.Sign(value))
            {
                BizzaEventSystem.Emit(EventDefine.RedPoint.Refresh, key);
                return true;
            }
        }
        return false;
    }

    private static void Report(RedPointNode node, bool active)
    {
        while (node != null && node.ReportFunc(active))
        {
            node = node.parent;
        }
    }

    public override string ToString()
    {
        return key + ":" + parent?.key;
    }
}
