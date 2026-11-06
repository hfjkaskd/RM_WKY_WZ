// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using Sirenix.OdinInspector;
// using Sirenix.Utilities;
// using UnityEngine;
//
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
//
// public partial class OldActionNode
// {
// #if UNITY_EDITOR
//     public static string debugGraphName = "";
//     public static Action subDataToEditor;
//     public static Dictionary<string, E_ActionExecuteState> debugState = new();
// #endif
//
//
//     public string DebugName
//     {
//         get
//         {
//             var attr = childExecuteType.GetCustomAttributeEx<LabelTextAttribute>();
//             var nodeType = attr != null ? attr.Text : childExecuteType.ToString();
//             var ret = $"[{nodeType}]";
//             if (string.IsNullOrEmpty(note))
//             {
//                 ret += ActionName ?? "[None]";
//             }
//             else
//             {
//                 ret += note;
//             }
//
//             return ret;
//         }
//     }
//
//
// #if UNITY_EDITOR
//
//     private void Editor_AddCustomButton_Action()
//     {
//         var shouldSetDirty = false;
//         var addFuncBtn = EditorGUIUtility.TrIconContent("Animation.AddEvent");
//         addFuncBtn.text = "选择行为 ";
//
//         if (EditorGUILayout.DropdownButton(addFuncBtn, FocusType.Keyboard, GUILayout.MinWidth(10)))
//         {
//             var m_Menu = new GenericMenu();
//
//             m_Menu.AddItem(new GUIContent("无"), false, Editor_OnValueChanged_Action, null);
//             foreach (var type in TypeCache.GetTypesDerivedFrom<ActionNodeBase>())
//             {
//                 var atr = type.GetAttribute<GraphElementInfoAttribute>();
//                 if (atr != null)
//                 {
//                     if (!CheckElementSupport(atr))
//                     {
//                         continue;
//                     }
//
//                     m_Menu.AddItem(new GUIContent(atr.Path), false, Editor_OnValueChanged_Action, type);
//                 }
//             }
//
//             m_Menu.ShowAsContext();
//         }
//     }
//
//     private void Editor_OnValueChanged_Action(object value)
//     {
//         var m_ItemTypeToAdd = value as Type;
//
//         if (m_ItemTypeToAdd != null)
//         {
//             if (Activator.CreateInstance(m_ItemTypeToAdd) is ActionNodeBase inst)
//             {
//                 action = inst;
//             }
//         }
//         else
//         {
//             action = null;
//         }
//     }
//
//
//     private void Editor_AddCustomButton_Condition()
//     {
//         var shouldSetDirty = false;
//         var addFuncBtn = EditorGUIUtility.TrIconContent("Animation.AddEvent");
//         addFuncBtn.text = "选择条件 ";
//
//         if (EditorGUILayout.DropdownButton(addFuncBtn, FocusType.Keyboard, GUILayout.MinWidth(10)))
//         {
//             // if (m_Menu == null)
//             var m_Menu = new GenericMenu();
//
//             m_Menu.AddItem(new GUIContent("无"), false, Editor_OnValueChanged_Condition, null);
//             foreach (var type in TypeCache.GetTypesDerivedFrom<ConditionBase>())
//             {
//                 var atr = type.GetAttribute<GraphElementInfoAttribute>();
//                 if (atr != null)
//                 {
//                     if (!CheckElementSupport(atr))
//                     {
//                         continue;
//                     }
//                     m_Menu.AddItem(new GUIContent(atr.Path), false, Editor_OnValueChanged_Condition, type);
//                 }
//             }
//
//             m_Menu.ShowAsContext();
//         }
//     }
//
//     private void Editor_OnValueChanged_Condition(object value)
//     {
//         var m_ItemTypeToAdd = value as Type;
//
//         if (m_ItemTypeToAdd != null)
//         {
//             if (Activator.CreateInstance(m_ItemTypeToAdd) is ConditionBase inst)
//             {
//                 condition = inst;
//             }
//         }
//         else
//         {
//             action = null;
//         }
//     }
//
//     public static bool CheckElementSupport(GraphElementInfoAttribute elementInfo)
//     {
//         if (ActionDataForEditor.curGraph == null)
//         {
//             ActionGraphLog.Error("只能在ActionEditorWindow中使用");
//             return false;
//         }
//
//         if (elementInfo.SupportTypes == null)
//         {
//             return false;
//         }
//
//         return elementInfo.SupportTypes.Any(supportType => supportType.IsInstanceOfType(ActionDataForEditor.curGraph));
//     }
//
// #endif
// }
