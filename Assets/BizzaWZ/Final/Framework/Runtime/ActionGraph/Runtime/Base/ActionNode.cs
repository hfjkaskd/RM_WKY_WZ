// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using Sirenix.OdinInspector;
// using Sirenix.Utilities;
// using Unity.Collections;
// using UnityEngine;
// #if UNITY_EDITOR
// using UnityEditor;
//
// #endif
//
// public partial class OldActionNode
// {
//     [HideInInspector] public string nodeGuid;
//
//     [HideInInspector] public Vector2 editorPos;
//
//     //config
//     [LabelText("执行方式")] public E_ExecuteTimes executeTimes;
//
//     [LabelText("执行次数"), ShowIf("@executeTimes==E_ExecuteTimes.Times")]
//     public int executeTotalTimes = 3;
//
//     [LabelText("节点类型")] public E_ChildNodeExecuteType childExecuteType;
//
//     [LabelText("帧数"), ShowIf("@childExecuteType==E_ChildNodeExecuteType.Frame")]
//     public int totalFrame;
//
//     [LabelText("备注")] public string note;
//
//     internal E_ActionExecuteState _lastFrameRet;
//
//
//     public string ActionName
//     {
//         get
//         {
//             if (action == null) return condition?.DebugName ?? "None";
//             return action?.ActionName ?? "None";
//         }
//     }
//
//     public bool bRoot => parent == null;
//     public bool bLeaf => children == null || children.Count == 0;
//     [HideInInspector] public OldActionNode parent;
//     [HideInInspector] public List<OldActionNode> children;
//
//     [HideInInspector]
// #if UNITY_EDITOR
//     [LabelText("条件"), OnInspectorGUI(nameof(Editor_AddCustomButton_Condition))]
//     [GUIColor(0, 1, 0)]
// #endif
//     public ConditionBase condition;
//
//     [HideInInspector]
// #if UNITY_EDITOR
//     [LabelText("行为"), OnInspectorGUI(nameof(Editor_AddCustomButton_Action))]
//     [GUIColor(1, 1, 0)]
// #endif
//     public ActionNodeBase action;
//
//
//     //runtime data
//     // [NonSerialized]
//     private E_ActionExecuteState _curExecuteState
//     {
//         get => __curExecuteState;
//         set
//         {
//             var n = note;
//             __curExecuteState = value;
//         }
//     }
//
//     private E_ActionExecuteState __curExecuteState;
//     private List<OldActionNode> _runningChildren = new();
//     private int _curExecuteTimes;
//     private int _curSequenceIdx;
//     private int _curFrame;
//
//     public int CurFrame
//     {
//         get
//         {
//             if (_curExecuteState == E_ActionExecuteState.Finish)
//             {
//                 return -1;
//             }
//
//             return _curFrame;
//         }
//     }
//
//     public void SortChildren()
//     {
//         // _runningChildren.Sort();
//     }
//
//     //检查条件
//     public bool CheckCondition(in ActionExecuteArgs executeArgs)
//     {
//         UpdateNodeExecuteState(executeArgs, E_ActionExecuteState.Failed);
//
//         //没有条件
//         if (condition == null) return true;
//         if (!condition.revert) return condition.GetResult(this, executeArgs);
//         return !condition.GetResult(this, executeArgs);
//     }
//
//     public E_ActionExecuteState Execute(in ActionExecuteArgs executeArgs)
//     {
//         E_ActionExecuteState ret;
//         if (bLeaf)
//         {
//             ret = Execute_Child(executeArgs);
//         }
//         else
//         {
//             ret = Execute_Parent(executeArgs);
//         }
//
//         UpdateNodeExecuteState(executeArgs, ret);
//
//         return ret;
//     }
//
//     //事件仅支持执行一次
//     public E_ActionExecuteState ExecuteSignal(in ActionExecuteArgs executeArgs)
//     {
//         E_ActionExecuteState ret;
//         if (bLeaf)
//         {
//             ret = Execute_Child(executeArgs);
//             if (ret != E_ActionExecuteState.Finish)
//             {
//                 ActionGraphLog.Error("事件仅支持执行一次");
//             }
//         }
//         else
//         {
//             ret = Execute_Parent(executeArgs);
//             if (ret != E_ActionExecuteState.Finish)
//             {
//                 ActionGraphLog.Error("事件仅支持执行一次");
//             }
//         }
//
//         UpdateNodeExecuteState(executeArgs, ret);
//         return ret;
//     }
//
//     private void UpdateNodeExecuteState(ActionExecuteArgs executeArgs, E_ActionExecuteState ret)
//     {
// #if UNITY_EDITOR
//         // if (executeArgs.graph.graphName == debugGraphName && (executeArgs.Self).gameObject == Selection.activeObject)
//         // {
//         //     if (ret == E_ActionExecuteState.Running && _lastFrameRet != E_ActionExecuteState.Running)
//         //     {
//         //         debugState[nodeGuid] = E_ActionExecuteState.Start;
//         //     }
//         //     else
//         //     {
//         //         debugState[nodeGuid] = ret;
//         //     }
//         // }
// #endif
//
//         _lastFrameRet = ret;
//     }
//
//     private E_ActionExecuteState Execute_Child(in ActionExecuteArgs executeArgs)
//     {
//         if (bLeaf)
//         {
//             if (_curExecuteState == E_ActionExecuteState.Finish)
//             {
//                 return E_ActionExecuteState.Finish;
//             }
//             else if (_curExecuteState == E_ActionExecuteState.None) //这里应该就Execute一下？
//             {
//                 OnEnter(executeArgs);
//                 _curExecuteState = E_ActionExecuteState.Running;
//                 return _curExecuteState;
//             }
//             else if (_curExecuteState == E_ActionExecuteState.Running)
//             {
//                 var ret = OnExecute(executeArgs);
//                 if (ret == E_ActionExecuteState.Finish)
//                 {
//                     _curExecuteState = E_ActionExecuteState.Finish;
//                     OnExit(executeArgs);
//                 }
//
//                 return _curExecuteState;
//             }
//             else
//             {
//                 throw new Exception("error");
//             }
//         }
//         else
//         {
//             throw new Exception("error");
//         }
//     }
//
//     private E_ActionExecuteState Execute_Parent(in ActionExecuteArgs executeArgs)
//     {
//         if (_curExecuteState == E_ActionExecuteState.Finish)
//         {
//             return E_ActionExecuteState.Finish;
//         }
//
//         if (_curExecuteState == E_ActionExecuteState.None)
//         {
//             OnEnter(executeArgs);
//
//             //计算要执行的节点
//             _runningChildren.Clear();
//             switch (childExecuteType)
//             {
//                 case E_ChildNodeExecuteType.Parallel:
//                     _curExecuteState = ExecuteStart_Parallel(executeArgs);
//                     break;
//                 case E_ChildNodeExecuteType.RepeatSelector:
//                     _curExecuteState = ExecuteStart_Selector(executeArgs);
//                     break;
//                 case E_ChildNodeExecuteType.Sequence:
//                     _curExecuteState = ExecuteStart_Sequence(executeArgs);
//                     break;
//                 case E_ChildNodeExecuteType.Frame:
//                     _curExecuteState = ExecuteStart_Frame(executeArgs);
//                     break;
//             }
//
//             return _curExecuteState;
//         }
//         else if (_curExecuteState == E_ActionExecuteState.Running)
//         {
//             (E_ActionExecuteState, E_ActionExecuteState) ret;
//             switch (childExecuteType)
//             {
//                 case E_ChildNodeExecuteType.Parallel:
//                     ret = ExecuteRunning_Parallel(executeArgs);
//                     break;
//                 case E_ChildNodeExecuteType.RepeatSelector:
//                     ret = ExecuteRunning_RepeatSelector(executeArgs);
//                     break;
//                 case E_ChildNodeExecuteType.Sequence:
//                     ret = ExecuteRunning_Sequence(executeArgs);
//                     break;
//                 case E_ChildNodeExecuteType.Frame:
//                     ret = ExecuteRunning_Frame(executeArgs);
//                     break;
//                 default:
//                     throw new Exception("error");
//             }
//
//             _curExecuteState = ret.Item1;
//             if (_runningChildren.Count == 0)
//             {
//                 OnExit(executeArgs);
//             }
//
//             return ret.Item2;
//         }
//         else
//         {
//             throw new Exception($"Unhandle ChildExecuteState:{_curExecuteState}");
//         }
//     }
//
//     #region ExecuteStart
//
//     private E_ActionExecuteState ExecuteStart_Parallel(in ActionExecuteArgs executeArgs)
//     {
//         foreach (var v in children)
//         {
//             if (!v.CheckCondition(executeArgs)) continue;
//             StartRunChild(v);
//         }
//
//         return _runningChildren.Count == 0 ? E_ActionExecuteState.Finish : E_ActionExecuteState.Running;
//     }
//
//     private E_ActionExecuteState ExecuteStart_Selector(in ActionExecuteArgs executeArgs)
//     {
//         foreach (var v in children)
//         {
//             if (!v.CheckCondition(executeArgs)) continue;
//             StartRunChild(v);
//             break;
//         }
//
//         return _runningChildren.Count == 0 ? E_ActionExecuteState.Finish : E_ActionExecuteState.Running;
//     }
//
//     private E_ActionExecuteState ExecuteStart_Sequence(in ActionExecuteArgs executeArgs)
//     {
//         _curSequenceIdx = 0;
//         OnChildFinish(executeArgs);
//         return _runningChildren.Count == 0 ? E_ActionExecuteState.Finish : E_ActionExecuteState.Running;
//     }
//
//     private E_ActionExecuteState ExecuteStart_Frame(in ActionExecuteArgs executeArgs)
//     {
//         _curFrame = 0;
//         return _curFrame >= totalFrame ? E_ActionExecuteState.Finish : E_ActionExecuteState.Running;
//     }
//
//     #endregion
//
//     #region ExecuteRunning
//
//     private (E_ActionExecuteState, E_ActionExecuteState) ExecuteRunning_Parallel(in ActionExecuteArgs executeArgs)
//     {
//         //检查未激活的子节点 todo:这里好像不对，应该单独写一个特殊节点？
//         if (executeTimes == E_ExecuteTimes.Loop)
//         {
//             foreach (var v in children)
//             {
//                 if (v._curExecuteState == E_ActionExecuteState.Running) continue;
//                 if (!v.CheckCondition(executeArgs)) continue;
//                 StartRunChild(v);
//             }
//         }
//
//         //todo:排序_runningChildren
//         {
//         }
//
//         RunningChild(executeArgs);
//         return RunningRet();
//     }
//
//     private (E_ActionExecuteState, E_ActionExecuteState) ExecuteRunning_RepeatSelector(in ActionExecuteArgs executeArgs)
//     {
//         //RunningChild(executeArgs);
//         ActionGraphLog.Assert(_runningChildren.Count <= 1, "ExecuteRunning_RepeatSelector error");
//         var oldChild = _runningChildren.Count == 1 ? _runningChildren[0] : null;
//         OldActionNode newChild = oldChild;
//         for (int i = 0; i < children.Count; i++)
//         {
//             //只检查前面的
//             if (oldChild == children[i])
//             {
//                 break;
//             }
//
//             if (children[i].CheckCondition(executeArgs))
//             {
//                 newChild = children[i];
//                 break;
//             }
//         }
//
//         if (newChild != oldChild)
//         {
//             oldChild?.OnExit(executeArgs);
//             _runningChildren.Clear();
//             if (newChild != null)
//             {
//                 StartRunChild(newChild);
//             }
//         }
//
//         // var ret = newChild?.Execute(executeArgs);
//         RunningChild(executeArgs);
//         return (E_ActionExecuteState.Running, E_ActionExecuteState.Running);
//     }
//
//     private (E_ActionExecuteState, E_ActionExecuteState) ExecuteRunning_Sequence(in ActionExecuteArgs executeArgs)
//     {
//         RunningChild(executeArgs);
//         if (childExecuteType == E_ChildNodeExecuteType.Sequence && _runningChildren.Count == 0)
//         {
//             _curSequenceIdx++;
//             OnChildFinish(executeArgs);
//         }
//
//         return RunningRet();
//     }
//
//     private (E_ActionExecuteState, E_ActionExecuteState) ExecuteRunning_Frame(in ActionExecuteArgs executeArgs)
//     {
//         RunningChild(executeArgs);
//         _curFrame++;
//         return RunningRet();
//     }
//
//     private void StartRunChild(OldActionNode child)
//     {
//         child.ResetData();
//         _runningChildren.Add(child);
//     }
//
//     private void ResetData()
//     {
//         _curExecuteState = E_ActionExecuteState.None;
//         _runningChildren.Clear();
//         _curExecuteTimes = 0;
//         _curSequenceIdx = 0;
//         _curFrame = 0;
//     }
//
//     private void RunningChild(in ActionExecuteArgs executeArgs)
//     {
//         for (int i = 0; i < _runningChildren.Count; i++)
//         {
//             var child = _runningChildren[i];
//             var ret = child.Execute(executeArgs);
// // #if UNITY_EDITOR
// //                 if (executeArgs.graph.graphName == debugGraphName && ((BattleActor)executeArgs.Self).gameObject == Selection.activeObject)
// //                 {
// //                     if (ret == E_ActionExecuteState.Running && child._lastFrameRet != E_ActionExecuteState.Running)
// //                     {
// //                         debugState[child.nodeGuid] = E_ActionExecuteState.Start;
// //                     }
// //                     else
// //                     {
// //                         debugState[child.nodeGuid] = ret;
// //                     }
// //
// //                 }
// // #endif
//
//             ActionGraphLog.Assert(ret != E_ActionExecuteState.None, "error");
//             if (ret == E_ActionExecuteState.Finish)
//             {
//                 //todo:先立即移除，感觉不太安全，先mark，延迟一帧会好一些
//                 _runningChildren.RemoveAt(i);
//                 i--;
//             }
//         }
//     }
//
//     private (E_ActionExecuteState, E_ActionExecuteState) RunningRet()
//     {
//         bool finish;
//         if (childExecuteType == E_ChildNodeExecuteType.Frame)
//         {
//             finish = _curFrame >= totalFrame;
//         }
//         else
//         {
//             finish = _runningChildren.Count == 0;
//         }
//
//         if (executeTimes == E_ExecuteTimes.Once)
//         {
//             var ret = finish ? E_ActionExecuteState.Finish : E_ActionExecuteState.Running;
//             return (ret, ret);
//         }
//         else if (executeTimes == E_ExecuteTimes.Loop)
//         {
//             if (finish)
//             {
//                 return (E_ActionExecuteState.None, E_ActionExecuteState.Running);
//             }
//
//             return (E_ActionExecuteState.Running, E_ActionExecuteState.Running);
//         }
//         else if (executeTimes == E_ExecuteTimes.Times)
//         {
//             if (finish)
//             {
//                 _curExecuteTimes++;
//                 if (_curExecuteTimes >= executeTotalTimes)
//                 {
//                     return (E_ActionExecuteState.Finish, E_ActionExecuteState.Finish);
//                 }
//                 else
//                 {
//                     return (E_ActionExecuteState.None, E_ActionExecuteState.Running);
//                 }
//             }
//             else
//             {
//                 return (E_ActionExecuteState.Running, E_ActionExecuteState.Running);
//             }
//         }
//         else
//         {
//             throw new Exception($"Unhandle E_ExecuteTimes type:{executeTimes}");
//         }
//     }
//
//     #endregion
//
//     public void Stop(in ActionExecuteArgs executeArgs)
//     {
//         for (int i = 0; i < _runningChildren.Count; i++)
//         {
//             _runningChildren[i].Stop(executeArgs);
//         }
//
//         _runningChildren.Clear();
//
//         OnStop();
//     }
//
//     private void OnChildFinish(in ActionExecuteArgs executeArgs)
//     {
//         if (childExecuteType == E_ChildNodeExecuteType.Sequence)
//         {
//             for (var i = _curSequenceIdx; i < children.Count; i++)
//             {
//                 var v = children[i];
//                 if (!v.CheckCondition(executeArgs)) continue;
//                 StartRunChild(v);
//                 _curSequenceIdx = i;
//                 break;
//             }
//         }
//     }
//
//     public E_ActionExecuteState OnExecute(in ActionExecuteArgs executeArgs)
//     {
//         if (action == null)
//         {
//             return E_ActionExecuteState.Finish;
//         }
//
//         return action.OnExecute(executeArgs);
//     }
//
//     public void OnEnter(in ActionExecuteArgs executeArgs)
//     {
//         ActionGraphLog.Verbose($"OnEnter:{ActionName}");
//         _curExecuteTimes = 0;
//         _curSequenceIdx = 0;
//
//         if (!bLeaf)
//         {
//             if (action != null)
//             {
//                 ActionGraphLog.Error("非叶子节点的Action不会被执行");
//             }
//         }
//
//         if (bRoot)
//         {
//             executeArgs.graph.OnEnter();
//         }
//
//         if (action != null)
//         {
//             action.OnEnter(executeArgs);
//         }
//
//         if (children != null)
//         {
//             foreach (var child in children)
//             {
//                 if (child != null && child.condition != null)
//                 {
//                     child.condition.OnEnter(child, executeArgs);
//                 }
//             }
//         }
//     }
//
//     public void OnExit(in ActionExecuteArgs executeArgs)
//     {
//         ActionGraphLog.Verbose($"OnExit:{ActionName}");
//         if (action != null)
//         {
//             action.OnExit(executeArgs);
//         }
//
//         if (bRoot)
//         {
//             executeArgs.graph.OnExit();
//         }
//     }
//
//     public void OnStop()
//     {
//         if (action != null)
//         {
//             action.OnStop();
//         }
//     }
//
//
//     public string ToString(int level)
//     {
//         string indent = new string(' ', level * 8);
//         string result = indent + "--" + DebugName + "\n";
//         if (children != null)
//         {
//             foreach (var child in children)
//             {
//                 result += child.ToString(level + 1);
//             }
//         }
//
//         return result;
//     }
// }