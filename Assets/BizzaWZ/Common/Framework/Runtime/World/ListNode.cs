
using System;
using System.Collections;
using System.Collections.Generic;

public struct ListNodeEnumerator<TItem> : IEnumerator<TItem>, IEnumerable<TItem>
{
    private readonly ListNode<TItem> _root;
    private ListNode<TItem> _next;

    public ListNodeEnumerator(ListNode<TItem> root)
    {
        _next = null;
        _root = root;
    }

    public readonly TItem Current => _next.value;
    readonly object IEnumerator.Current => _next.value;

    public bool MoveNext()
    {
        _next = _next == null ? _root : _next.next;
        return _next != null;
    }

    public void Reset()
    {
        _next = null;
    }

    public void Dispose()
    {
    }

    public readonly ListNodeEnumerator<TItem> GetEnumerator() => this;
    readonly IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => this;
    readonly IEnumerator IEnumerable.GetEnumerator() => this;
}

public class ListNode<TItem> : IEnumerable<TItem>, IOnRelease
{
    public TItem value;
    public ListNode<TItem> next;

    public ListNode()
    {
    }

    public ListNode(TItem value, ListNode<TItem> next = null)
    {
        this.value = value;
        this.next = next;
    }

    public ListNodeEnumerator<TItem> GetEnumerator() => new(this);
    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void OnRelease()
    {
        value = default;
        next = null;
    }
}

public class SimpleList<T> : IEnumerable<T>
{
    protected ListNode<T> first;
    protected ListNode<T> last;
    protected int count;

    public ListNode<T> First => first;
    public ListNode<T> Last => last;
    public int Count => count;

    public static void AddList(SimpleList<T> list1, SimpleList<T> list2)
    {
        if (list1.last != null)
        {
            list1.last.next = list2.first;
        }
        else
        {
            list1.first = list2.first;
            list1.last = list2.last;
        }

        list2.first = null;
        list2.last = null;
        list1.count += list2.count;
        list2.count = 0;
    }

    public void AddNode(ListNode<T> node)
    {
        if (last != null)
        {
            last.next = node;
        }
        else
        {
            first = node;
        }

        while (node != null)
        {
            last = node;
            count++;
            node = node.next;
        }
    }

    public void AddNodeWithClear(ListNode<T> node)
    {
        if (last != null)
        {
            last.next = node;
        }
        else
        {
            first = node;
        }

        while (node != null)
        {
            node.value = default;
            last = node;
            count++;
            node = node.next;
        }
    }

    public ListNode<T> RemoveFirst()
    {
        var ret = first;
        first = first?.next;

        if (--count <= 0)
        {
            last = null;
        }

        return ret;
    }

    public ListNodeEnumerator<T> GetEnumerator() => new(first);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class ListNodeExtensions
{
    public static ListNode<TItem> AddFirst<TItem>(this ListNode<TItem> target, TItem value,
        SimpleList<TItem> pool = null)
    {
        var childNode = pool is { Count: > 0 } ? pool.RemoveFirst() : new ListNode<TItem>();
        childNode.value = value;
        childNode.next = target;
        return childNode;
    }

    [Obsolete]
    public static void AddNode<TKey, TItem>(this Dictionary<TKey, ListNode<TItem>> map, TKey key, TItem value,
        SimpleList<TItem> pool = null)
    {
        var childNode = pool is { Count: > 0 } ? pool.RemoveFirst() : new ListNode<TItem>();
        childNode.value = value;
        if (map.TryGetValue(key, out var node) && node != null)
        {
            childNode.next = node.next;
            node.next = childNode;
        }
        else
        {
            childNode.next = null;
            map[key] = childNode;
        }
    }
}