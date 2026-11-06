using System;
using System.Collections.Generic;
using Bizza;
using UnityEngine;

public class RedPointSystem : SimpleInstanceClass<RedPointSystem>
{
    private readonly Dictionary<string, RedPointNode> _redPoints = new();

    public bool this[string key]
    {
        get => GetRed(key);
        set => SetRed(key, value);
    }

    public bool GetRed(string key)
    {
        if (_redPoints.TryGetValue(key, out var node))
        {
            return node.Active;
        }

        return false;
    }

    public void SetRed(string key, bool show)
    {
        #if UNITY_EDITOR
        // Debug.LogError(key + "  " + show);
        #endif
        if (!_redPoints.TryGetValue(key, out var node))
        {
            if (!show) return;
            AddRed(key);
            node = _redPoints[key];
        }

        node.SelfActive = show;
    }

    public void AddRed(string key)
    {
        // 1 task
        // 2 task.talent
        // 3 task.talent.item1
        key = key.Trim();
        string parentKey = string.Empty;
        for (int i = 1; i <= key.Length; i++)
        {
            if (i < key.Length && key[i] != '.') continue;
            string k = key[..i];
            if (!_redPoints.ContainsKey(k))
            {
                var parent = _redPoints.GetValueOrDefault(parentKey);
                var node = new RedPointNode(k, false, parent);
                _redPoints.Add(k, node);
            }

            parentKey = k;
        }
    }
}