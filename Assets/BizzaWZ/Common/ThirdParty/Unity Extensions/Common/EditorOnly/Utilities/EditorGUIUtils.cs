#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// Menu item states.
    /// </summary>
    public enum MenuItemState
    {
        Normal,
        Selected,
        Disabled,
    }


    /// <summary>
    /// Utilities for editor GUI.
    /// </summary>
    public static class EditorGUIUtils
    {
        static List<GUIContent> _tempContents = new List<GUIContent>();

        static GUIStyle _buttonStyle;
        static GUIStyle _buttonLeftStyle;
        static GUIStyle _buttonMiddleStyle;
        static GUIStyle _buttonRightStyle;
        static GUIStyle _middleCenterLabelStyle;
        static GUIStyle _middleLeftLabelStyle;
        static GUIStyle _middleRightLabelStyle;
        static GUIStyle _middleCenterTextFieldStyle;

        static Texture2D _paneOptionsIconDark;
        static Texture2D _paneOptionsIconLight;

        static int _dragState;
        static Vector2 _dragPos;


        public static float indentWidth => EditorGUI.indentLevel * 15f;
        public static float indentedLabelWidth => EditorGUIUtility.labelWidth - indentWidth;


        public static GUIStyle buttonStyle
        {
            get
            {
                if (_buttonStyle == null) _buttonStyle = "Button";
                return _buttonStyle;
            }
        }


        public static GUIStyle buttonLeftStyle
        {
            get
            {
                if (_buttonLeftStyle == null) _buttonLeftStyle = "ButtonLeft";
                return _buttonLeftStyle;
            }
        }


        public static GUIStyle buttonMiddleStyle
        {
            get
            {
                if (_buttonMiddleStyle == null) _buttonMiddleStyle = "ButtonMid";
                return _buttonMiddleStyle;
            }
        }


        public static GUIStyle buttonRightStyle
        {
            get
            {
                if (_buttonRightStyle == null) _buttonRightStyle = "ButtonRight";
                return _buttonRightStyle;
            }
        }


        public static GUIStyle middleCenterLabelStyle
        {
            get
            {
                if (_middleCenterLabelStyle == null)
                {
                    _middleCenterLabelStyle = new GUIStyle(EditorStyles.label);
                    _middleCenterLabelStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _middleCenterLabelStyle;
            }
        }


        public static GUIStyle middleLeftLabelStyle
        {
            get
            {
                if (_middleLeftLabelStyle == null)
                {
                    _middleLeftLabelStyle = new GUIStyle(EditorStyles.label);
                    _middleLeftLabelStyle.alignment = TextAnchor.MiddleLeft;
                }
                return _middleLeftLabelStyle;
            }
        }


        public static GUIStyle middleRightLabelStyle
        {
            get
            {
                if (_middleRightLabelStyle == null)
                {
                    _middleRightLabelStyle = new GUIStyle(EditorStyles.label);
                    _middleRightLabelStyle.alignment = TextAnchor.MiddleRight;
                }
                return _middleRightLabelStyle;
            }
        }


        public static GUIStyle middleCenterTextFieldStyle
        {
            get
            {
                if (_middleCenterTextFieldStyle == null)
                {
                    _middleCenterTextFieldStyle = new GUIStyle(EditorStyles.textField);
                    _middleCenterTextFieldStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _middleCenterTextFieldStyle;
            }
        }


        public static Texture2D paneOptionsIcon
        {
            get
            {
                return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight;
            }
        }


        public static Texture2D paneOptionsIconDark
        {
            get
            {
                if (_paneOptionsIconDark == null)
                {
                    _paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
                }
                return _paneOptionsIconDark;
            }
        }


        public static Texture2D paneOptionsIconLight
        {
            get
            {
                if (_paneOptionsIconLight == null)
                {
                    _paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
                }
                return _paneOptionsIconLight;
            }
        }


        public static Color labelNormalColor
        {
            get { return EditorStyles.label.normal.textColor; }
        }


        public static int skinIndex => EditorGUIUtility.isProSkin ? 1 : 0;


        public static float GetMultipleLinesHeight(int lines)
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            return (EditorGUIUtility.singleLineHeight + spacing) * lines - spacing;
        }


        [InitializeOnLoadMethod]
        static void Initialize()
        {
            float _time = 0;
            int _maxTemp = 0;

            EditorApplication.update += () =>
            {
                foreach (var item in _tempContents)
                    GUIContentPool.global.Despawn(item);

                _maxTemp = Mathf.Max(_tempContents.Count, _maxTemp);
                _tempContents.Clear();

                _time += EditorUtils.deltaTime;
                if (_time >= 10f)
                {
                    int maxCount = _maxTemp * 2;
                    if (GUIContentPool.global.count > maxCount)
                        GUIContentPool.global.count = maxCount;

                    _time = 0;
                    _maxTemp = 0;
                }
            };
        }


        public static Rect GetControlFieldRect(Rect indentedRect)
        {
            indentedRect.xMin += indentedLabelWidth + 2;
            return indentedRect;
        }


        /// <summary>
        /// Get a temporary GUIContent（use this to avoid GC).
        /// </summary>
        public static GUIContent TempContent(string text = null, Texture image = null, string tooltip = null)
        {
            var tempContent = GUIContentPool.global.Spawn();
            _tempContents.Add(tempContent);

            tempContent.text = text;
            tempContent.image = image;
            tempContent.tooltip = tooltip;

            return tempContent;
        }


        /// <summary>
        /// Get a temporary GUIContent（use this to avoid GC).
        /// </summary>
        public static GUIContent TempContent<T>(T obj, Texture image = null, string tooltip = null)
        {
            return TempContent(obj.ToString(), image, tooltip);
        }


        /// <summary>
        /// Draw a rect wireframe.
        /// </summary>
        public static void DrawWireRect(Rect rect, Color color, float borderWidth = 1f)
        {
            Rect border = new Rect(rect.x, rect.y, rect.width, borderWidth);
            EditorGUI.DrawRect(border, color);
            border.y = rect.yMax - borderWidth;
            EditorGUI.DrawRect(border, color);
            border.yMax = border.yMin;
            border.yMin = rect.yMin + borderWidth;
            border.width = borderWidth;
            EditorGUI.DrawRect(border, color);
            border.x = rect.xMax - borderWidth;
            EditorGUI.DrawRect(border, color);
        }


        /// <summary>
        /// Draw a indented button.
        /// </summary>
        public static bool IndentedButton(string text)
        {
            var rect = EditorGUILayout.GetControlRect(true);
            rect.xMin += EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing;
            return GUI.Button(rect, text, EditorStyles.miniButton);
        }


        /// <summary>
        /// Draw a indented toggle-button.
        /// </summary>
        public static bool IndentedToggleButton(string text, bool value)
        {
            var rect = EditorGUILayout.GetControlRect(true);
            rect.xMin += EditorGUIUtility.labelWidth + EditorGUIUtility.standardVerticalSpacing;
            return GUI.Toggle(rect, value, text, EditorStyles.miniButton);
        }


        public static bool DrawReferenceDetails(UnityEngine.Object reference, bool foldout, string label, ref UnityEditor.Editor cachedEditor)
        {
            if (reference)
            {
                var rect = EditorGUILayout.GetControlRect();
                rect.xMin -= 13;

                foldout = GUI.Toggle(rect, foldout, label, EditorStyles.foldout);

                if (foldout)
                {
                    using (VerticalLayoutScope.New(EditorStyles.helpBox))
                    {
                        using (IndentLevelScope.New())
                        {
                            UnityEditor.Editor.CreateCachedEditor(reference, null, ref cachedEditor);
                            cachedEditor.OnInspectorGUI();
                        }
                    }
                }
            }

            return foldout;
        }


        public static Vector2 SingleLineVector2Field(Rect rect, GUIContent label, Vector2 value, string subLabel1 = "X", string subLabel2 = "Y")
        {
            rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(label, FocusType.Keyboard, rect), label);

            using (LabelWidthScope.New(12))
            {
                using (IndentLevelScope.New(0, false))
                {
                    rect.width = (rect.width - 4) * 0.5f;
                    value.x = EditorGUI.FloatField(rect, subLabel1, value.x);
                    rect.x = rect.xMax + 4;
                    value.y = EditorGUI.FloatField(rect, subLabel2, value.y);
                }
            }
            return value;
        }


        public static Vector2Int SingleLineVector2IntField(Rect rect,GUIContent label, Vector2Int value, string subLabel1 = "X", string subLabel2 = "Y")
        {
            rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(label, FocusType.Keyboard, rect), label);

            using (LabelWidthScope.New(12))
            {
                using (IndentLevelScope.New(0, false))
                {
                    rect.width = (rect.width - 4) * 0.5f;
                    value.x = EditorGUI.IntField(rect, subLabel1, value.x);
                    rect.x = rect.xMax + 4;
                    value.y = EditorGUI.IntField(rect, subLabel2, value.y);
                }
            }
            return value;
        }


        public static Vector4 TwoLinesVector4Field(Rect rect, GUIContent label, Vector4 value, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            var xy = new Vector2(value.x, value.y);
            rect.height = EditorGUIUtility.singleLineHeight;
            xy = SingleLineVector2Field(rect, label, xy, subLabel1, subLabel2);

            var zw = new Vector2(value.z, value.w);
            rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
            zw = SingleLineVector2Field(rect, TempContent(), zw, subLabel3, subLabel4);

            return new Vector4(xy.x, xy.y, zw.x, zw.y);
        }


        public static Vector4Int TwoLinesVector4IntField(Rect rect, GUIContent label, Vector4Int value, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            var xy = new Vector2Int(value.x, value.y);
            rect.height = EditorGUIUtility.singleLineHeight;
            xy = SingleLineVector2IntField(rect, label, xy, subLabel1, subLabel2);

            var zw = new Vector2Int(value.z, value.w);
            rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
            zw = SingleLineVector2IntField(rect, TempContent(), zw, subLabel3, subLabel4);

            return new Vector4Int(xy.x, xy.y, zw.x, zw.y);
        }


        public static Rect TwoLinesRectField(Rect rect, GUIContent label, Rect value)
        {
            var v4 = new Vector4(value.x, value.y, value.width, value.height);
            v4 = TwoLinesVector4Field(rect, label, v4, "X", "Y", "W", "H");
            return new Rect(v4.x, v4.y, v4.z, v4.w);
        }


        public static RectInt TwoLinesRectIntField(Rect rect, GUIContent label, RectInt value)
        {
            var v4 = new Vector4Int(value.x, value.y, value.width, value.height);
            v4 = TwoLinesVector4IntField(rect, label, v4, "X", "Y", "W", "H");
            return new RectInt(v4.x, v4.y, v4.z, v4.w);
        }


        /// <summary>
        /// Drag mouse to change value.
        /// </summary>
        public static float DragValue(Rect rect, float value, float sensitivity)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(current.mousePosition) && current.button == 0)
                    {
                        EditorGUIUtility.editingTextField = false;
                        GUIUtility.hotControl = id;
                        _dragState = 1;
                        _dragPos = current.mousePosition;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && _dragState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        _dragState = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                    {
                        break;
                    }
                    switch (_dragState)
                    {
                        case 1:
                            if ((Event.current.mousePosition - _dragPos).sqrMagnitude > 4)
                            {
                                _dragState = 2;
                            }
                            current.Use();
                            break;

                        case 2:
                            value += HandleUtility.niceMouseDelta * sensitivity;
                            value = MathUtils.RoundToSignificantDigitsFloat(value, 6);
                            GUI.changed = true;
                            current.Use();
                            break;
                    }
                    break;

                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.SlideArrow);
                    break;
            }

            return value;
        }


        /// <summary>
        /// Drag mouse to change value.
        /// </summary>
        public static float DragValue(Rect rect, GUIContent content, float value, float sensitivity, GUIStyle style)
        {
            GUI.Label(rect, content, style);
            return DragValue(rect, value, sensitivity);
        }


        /// <summary>
        /// Draw a progress bar that can be dragged.
        /// </summary>
        public static float DragProgress(
            Rect rect,
            float value01,
            Color backgroundColor,
            Color foregroundColor,
            bool draggable = true)
        {
            var progressRect = rect;
            progressRect.width = Mathf.Round(progressRect.width * value01);

            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(progressRect, foregroundColor);

            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(current.mousePosition) && current.button == 0)
                    {
                        EditorGUIUtility.editingTextField = false;
                        GUIUtility.hotControl = id;
                        _dragState = 1;

                        if (draggable)
                        {
                            float offset = current.mousePosition.x - rect.x + 1f;
                            value01 = Mathf.Clamp01(offset / rect.width);
                        }

                        GUI.changed = true;

                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && _dragState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        _dragState = 0;
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                    {
                        break;
                    }
                    if (_dragState != 0)
                    {
                        if (draggable)
                        {
                            float offset = current.mousePosition.x - rect.x + 1f;
                            value01 = Mathf.Clamp01(offset / rect.width);
                        }

                        GUI.changed = true;

                        current.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (draggable) EditorGUIUtility.AddCursorRect(rect, MouseCursor.SlideArrow);
                    break;
            }

            return value01;
        }


        /// <summary>
        /// Draw a progress bar that can be dragged.
        /// </summary>
        public static float DragProgress(
            Rect rect,
            float value01,
            Color backgroundColor,
            Color foregroundColor,
            Color borderColor,
            bool drawForegroundBorder = false,
            bool draggable = true)
        {
            float result = DragProgress(rect, value01, backgroundColor, foregroundColor, draggable);

            DrawWireRect(rect, borderColor);

            if (drawForegroundBorder)
            {
                rect.width = Mathf.Round(rect.width * value01);
                if (rect.width > 0f)
                {
                    rect.xMin = rect.xMax - 1f;
                    EditorGUI.DrawRect(rect, borderColor);
                }
            }

            return result;
        }


        /// <summary>
        /// Create a menu.
        /// </summary>
        /// <param name="itemCount"> Number of items, include separators and childs. </param>
        /// <param name="getItemContent"> Get content of an item, a separator must ends with '/' </param>
        /// <param name="getItemState"> Get state of an item </param>
        /// <returns> The created menu, use DropDown or ShowAsContext to show it. </returns>
        public static GenericMenu CreateMenu(
            int itemCount,
            Func<int, GUIContent> getItemContent,
            Func<int, MenuItemState> getItemState,
            Action<int> onSelect)
        {
            GenericMenu menu = new GenericMenu();
            GUIContent content;
            MenuItemState state;

            for(int i=0; i<itemCount; i++)
            {
                content = getItemContent(i);
                if(content.text.EndsWith("/"))
                {
                    menu.AddSeparator(content.text.Substring(0, content.text.Length - 1));
                }
                else
                {
                    state = getItemState(i);
                    if(state == MenuItemState.Disabled)
                    {
                        menu.AddDisabledItem(content);
                    }
                    else
                    {
                        int index = i;
                        menu.AddItem(content, state == MenuItemState.Selected, () => onSelect(index));
                    }
                }
            }

            return menu;
        }


        /// <summary>
        /// Create a menu.
        /// </summary>
        /// <param name="itemCount"> Number of items, include separators and childs. </param>
        /// <param name="getItemContent"> Get content of an item, a separator must ends with '/' </param>
        /// <param name="getItemState"> Get state of an item </param>
        /// <returns> The created menu, use DropDown or ShowAsContext to show it. </returns>
        public static GenericMenu CreateMenu<T>(
            IEnumerable<T> items,
            Func<T, GUIContent> getItemContent,
            Func<T, MenuItemState> getItemState,
            Action<T> onSelect)
        {
            GenericMenu menu = new GenericMenu();
            GUIContent content;
            MenuItemState state;

            foreach (var i in items)
            {
                content = getItemContent(i);
                if (content.text.EndsWith("/"))
                {
                    menu.AddSeparator(content.text.Substring(0, content.text.Length - 1));
                }
                else
                {
                    state = getItemState(i);
                    if (state == MenuItemState.Disabled)
                    {
                        menu.AddDisabledItem(content);
                    }
                    else
                    {
                        T current = i;
                        menu.AddItem(content, state == MenuItemState.Selected, () => onSelect(current));
                    }
                }
            }

            return menu;
        }

    } // struct EditorGUIUtilities

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR