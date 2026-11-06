#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// Extensions for GenericMenu.
    /// </summary>
    public static class GenericMenuUtils
    {
        public static void AddItem(this GenericMenu menu, GUIContent content, GenericMenu.MenuFunction func, bool disabled = false, bool on = false)
        {
            if (disabled)
            {
                menu.AddDisabledItem(content, on);
            }
            else
            {
                menu.AddItem(content, on, func);
            }
        }

        public static void AddItem(this GenericMenu menu, GUIContent content, GenericMenu.MenuFunction2 func, object userData, bool disabled = false, bool on = false)
        {
            if (disabled)
            {
                menu.AddDisabledItem(content, on);
            }
            else
            {
                menu.AddItem(content, on, func, userData);
            }
        }

        public static void AddSeparator(this GenericMenu menu)
        {
            menu.AddSeparator(string.Empty);
        }

        public static void AddItem(this GenericMenu menu, string content, GenericMenu.MenuFunction func, bool disabled = false, bool on = false)
        {
            AddItem(menu, new GUIContent(content), func, disabled, on);
        }

        public static void AddItem(this GenericMenu menu, string content, GenericMenu.MenuFunction2 func, object userData, bool disabled = false, bool on = false)
        {
            AddItem(menu, new GUIContent(content), func, userData, disabled, on);
        }

        public static void AddDisabledItem(this GenericMenu menu, string content, bool on = false)
        {
            menu.AddDisabledItem(new GUIContent(content), false);
        }

        ///// <summary>
        ///// 简化菜单
        ///// </summary>
        ///// <param name="menu"></param>
        ///// <param name="retainLevels"> 保留级数 </param>
        ///// <param name="firstIndex"> 要简化的第一项 </param>
        //public static void Simplify(this GenericMenu menu, int retainLevels = 0, int firstIndex = 0)
        //{
        //    int items = menu.GetItemCount();
        //    if (items == 0) return;

        //    var list = (IList)menu.GetFieldValue("m_MenuItems");
        //    var field = list[0].GetType().GetInstanceField("content");

        //    using (ListPool<GUIContent>.global.Spawn(out var contents))
        //    {
        //        for (int i = firstIndex; i < items; i++)
        //        {
        //            contents.Add((GUIContent)field.GetValue(list[i]));
        //        }

        //        string text = contents[0].text;
        //        int prefixIndex = text.LastIndexOf('/');
        //        if (prefixIndex < 0) return;

        //        for (int i = 1; i < contents.Count; i++)
        //        {
        //            string item = contents[i].text;

        //            int j = 0;

        //            for (; j <= prefixIndex && j < item.Length; j++)
        //            {
        //                if (text[j] != item[j])
        //                {
        //                    break;
        //                }
        //            }

        //            prefixIndex = j == 0 ? -1 : text.LastIndexOf('/', j - 1);
        //            if (prefixIndex < 0) return;
        //        }

        //        int start = 0;
        //        for (int i = 0; i < retainLevels && start < prefixIndex; i++)
        //        {
        //            start = text.IndexOf('/', start) + 1;
        //        }

        //        if (start < prefixIndex)
        //            foreach (var i in contents)
        //                i.text = i.text.Remove(start, prefixIndex - start + 1);
        //    }
        //}

    } // class Extensions

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR