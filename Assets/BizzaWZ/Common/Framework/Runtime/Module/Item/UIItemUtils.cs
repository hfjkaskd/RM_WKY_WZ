using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class UIItemUtils
{
    struct Entry
    {
        public TMP_Text text;
        public E_ItemType itemType;
    }

    private static List<Entry> _texts = new();
    private static bool _registerBindItemText = false;
    public static void Bind(E_ItemType itemType, TMP_Text text)
    {
        if (text == null) return;
        if (!_registerBindItemText)
        {
            _registerBindItemText = true;
            BizzaEventSystem.Set(EventDefine.Item.ItemChanged, OnItemChange, true);
        }

        int existing = -1;
        for (int i = 0; i < _texts.Count; i++)
            if (_texts[i].text == text) { existing = i; break; }
        var entry = new Entry { itemType = itemType, text = text };
        if (existing >= 0) _texts[existing] = entry;
        else _texts.Add(entry);

        text.text = ItemUtils.GetItemText(itemType);
    }

    public static void Unbind(TMP_Text text)
    {
        for (var i = _texts.Count - 1; i >= 0; i--)
        {
            var v = _texts[i];
            if (v.text == text)
            {
                _texts.RemoveAt(i);
            }
        }
    }

    private static void OnItemChange()
    {
        foreach (var v in _texts)
        {
            if (v.text != null)
            {
                v.text.text = ItemUtils.GetItemText(v.itemType);
            }
        }
    }
}
