using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
/// 变量节点视图
/// </summary>
public class VariableNodeView : NodeViewBase
{
    public override GraphElementBase NodeData => Data;
    public override string DebugName => Data.DebugName;

    // public VariableWrapperBase Data => _data;

    // private VariableWrapperBase _data;
    public VariableBase Data => _data;
    private VariableBase _data;

    public override Vector2 NodePosition
    {
        get
        {
            var pos = GetPosition().position;
            if (pos == default && Data != null)
            {
                return Data.editorPos;
            }

            return pos;
        }
        set
        {
            if (Data != null)
            {
                Data.editorPos = value;
            }

            var rect = GetPosition();
            rect.position = value;
            SetPosition(rect);
        }
    }

    protected override float OutlineWidth => 1f;

    public VariableNodeView(VariableBase variable, FlowChartView.E_CreateVariableType createNew)
    {
        _createNew = createNew;
        _data = variable;
        var rect = GetPosition();
        rect.position = _data.editorPos;
        SetPosition(rect);
        Init();
    }

    public override List<GraphDebugUtils.VariableViewData> GetAllVariable()
    {
        if (_data == null)
        {
            return new();
        }
        var list = GraphDebugUtils.GetAllVariableInClass<VariableWrapperBase>(_data);
        return list;
    }

    public override void Init()
    {
        base.Init();

        var titleEle = this.Q<VisualElement>("title-label");
        if (titleEle != null)
        {
            titleEle.style.color = new StyleColor(new Color(0f, 0.8f, 0.8f, 1.0f));
            titleEle.style.fontSize = new StyleLength(12);
            titleEle.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
        }

        if (GetAllVariable().Count == 0)
        {
            titleContainer.visible = false;
            titleContainer.style.height = 10;
            titleContainer.style.marginBottom = -10;
            _outputPort.portName = $"<color=#00ffff>{GetTitleText()}</color>";
            style.width = ActionGraphDefine.DirectVariableWidth;
            var input = this.Q<VisualElement>("input");
            if (input != null)
            {
                input.style.flexGrow = 0.15f;
                input.style.backgroundColor = new Color(0f, 0.8f, 0.8f, 0.7f);
            }
            // SetBorderColor(new Color(0, 0.5f, 0.5f, 1.0f));
        }

        {
            _propertyTree = PropertyTree.Create(NodeData);
            IMGUIContainer imGUIContainer = new IMGUIContainer(() =>
            {
                var old = GUI.color;
                GUI.color = new Color(1, 1, 1, ActionGraphEditorWindow.flowChart.VariableAlpha);
                if (_data.IsDirectValue)
                {
                    if (NodeData != null)
                    {
                        _propertyTree.Draw(false);
                    }
                }

                if (!string.IsNullOrEmpty(GraphDebugUtils.debugGraphName) && GraphDebugUtils.debugGraphName == ActionGraphEditorWindow.flowChart.Graph.graphName)
                {
                    // Debug.LogError(ActionGraphEditorWindow.flowChart.Graph.GetHashCode());
                    var oldColor = GUI.color;
                    var lerp = Mathf.Clamp01((Time.frameCount - _data.lastHasDebugValueFrame) / 60f) * 0.5f;
                    GUI.color = Color.Lerp(Color.green, Color.white, lerp);
                    GUILayout.BeginHorizontal();
                    if (_data.debugValue != null && _data.debugValue is Object obj)
                    {
                        EditorGUILayout.ObjectField(obj, obj.GetType());
                    }
                    else
                    {
                        GUILayout.TextArea(_data.debugValue == null ? "Null" : _data.debugValue.ToString(), new GUILayoutOption[]{GUILayout.Height(20)});
                    }
                    GUILayout.EndHorizontal();
                    GUI.color = oldColor;
                }

                GUI.color = old;
            });
            // imGUIContainer.style.maxHeight = 200;
            Add(imGUIContainer);
        }
    }

    private Port _outputPort;
    protected override void GenerateOutputPort()
    {
        Port output = GetPortForNode(this, Direction.Output, Port.Capacity.Multi);
        output.portName = $"输出";//{_data.WrapperType.ToString().Replace("Wrapper", "")}
        output.portColor = Color.cyan;
        output.portType = _data.WrapperType;
        output.userData = new PortData() { portType = E_PortType.VariableOutput, variableOutputData = (_data.GetType(), "") };
        // inputContainer.Add(input);
        outputContainer.Add(output);
        _outputPort = output;

        var list = GetAllVariable();

        var rect = GetPosition();
        rect.position = _data.editorPos;
        var size = rect.size;
        size.y += list.Count * 20;
        rect.size = size;
        SetPosition(rect);
    }

    public Type enumType;
    public override void OnPortConnect(Port selfPort, Port otherPort, bool bInput)
    {
        base.OnPortConnect(selfPort, otherPort, bInput);
        var otherPortData = otherPort.userData as PortData;
        if (_data is Variable_Bool_EnumCompare enumCompare && otherPortData != null && otherPortData.portType == E_PortType.VariableOutput)
        {
            var varInst = Activator.CreateInstance(otherPortData.variableOutputData.Item1) as Variable_Enum;
            if (varInst != null && varInst.EnumType != null)
            {
                enumType = varInst.EnumType;
                foreach (var v in inputContainer.Children())
                {
                    if (v is Port port && port != selfPort)
                    {
                        foreach (var e in port.connections)
                        {
                            if (e  != null && e.output.node != null && e.output.node.userData is Variable_Enum_Direct enumDirect2)
                            {
                                enumDirect2.enumType = enumType;
                            }
                        }
                    }
                }
                return;
            }
        }

        if (_data is Variable_Enum_Direct enumDirect && otherPortData.portType == E_PortType.VariableInput)
        {
            if (otherPortData.variableInputData.fieldInfo.FieldType == typeof(EnumWrapper))
            {
                var attr = otherPortData.variableInputData.fieldInfo.GetCustomAttribute<GraphVariableAttribute>();
                //GraphVariable指定类型
                if (attr != null && attr.EnumType != null && attr.EnumType.IsEnum)
                {
                    enumDirect.enumType = attr.EnumType;
                }
                //跟随VariableNodeView类型
                else if (otherPort.node is VariableNodeView varNodeView && varNodeView.enumType != null)
                {
                    enumDirect.enumType = varNodeView.enumType;
                }
                else
                {
                    ActionGraphLog.Error($"未指定正确的枚举类型:{otherPortData.variableInputData.instance.GetType().Name}");
                }
            }
        }
    }

    public override void BeforeSave()
    {
        base.BeforeSave();
    }

    public override void AfterLoad()
    {

    }

    public override void RefreshNodeInfo()
    {
        float alpha = ActionGraphEditorWindow.flowChart.VariableAlpha;
        SetElementAndChildrenOpacity(this, alpha);
        foreach (var v in inputContainer.Children())
        {
            if (v is Port port)
            {
                if (port != null)
                {
                    foreach (var edge in port.connections)
                    {
                        SetElementAndChildrenOpacity(edge, alpha);
                    }
                }
            }
        }
        foreach (var v in outputContainer.Children())
        {
            if (v is Port port)
            {
                if (port != null)
                {
                    foreach (var edge in port.connections)
                    {
                        SetElementAndChildrenOpacity(edge, alpha);
                    }
                }
            }
        }
        base.RefreshNodeInfo();
        var titleStr = GetTitleText();
        title = titleStr;
    }

    public static void SetElementAndChildrenOpacity(VisualElement element, float opacity)
    {
        // 设置当前元素的透明度
        element.style.opacity = opacity;

        // // 递归设置每个子元素的透明度
        // foreach (VisualElement child in element.Children())
        // {
        //     SetElementAndChildrenOpacity(child, opacity);
        // }
    }

    private string GetTitleText()
    {
        var titleStr = _data.DebugName;

        var varAttr = _data.GetType().GetCustomAttribute<GraphElementInfoAttribute>();
        if (string.IsNullOrEmpty(titleStr) && varAttr != null)
        {
            titleStr = varAttr.Text;
        }

        if (enumType != null)
        {
            titleStr += $"[{enumType.ToString()}]";
        }
        titleStr = titleStr + (_hasTips ? "*" : "");
        return titleStr;
    }
}
