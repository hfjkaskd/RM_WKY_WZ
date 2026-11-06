using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeViewData
{

}


public abstract class NodeViewBase : UnityEditor.Experimental.GraphView.Node
{
    [Serializable]
    protected class View
    {
        [LabelWidth(60)][InlineProperty][ShowInInspector][LabelText("")][HideReferenceObjectPicker]
        public NodeBase data;

        public View(NodeBase data)
        {
            this.data = data;
        }
    }

    public abstract GraphElementBase NodeData { get; }
    protected FlowChartView.E_CreateVariableType _createNew;
    protected virtual float OutlineWidth => 4f;//

    protected PropertyTree _propertyTree;
    protected View _view;
    private VisualElement borderElement;

    public abstract string DebugName { get; }

    public abstract Vector2 NodePosition
    {
        get;
        set;
    }

    public abstract List<GraphDebugUtils.VariableViewData> GetAllVariable();

    public virtual void OnPortConnect(Port selfPort, Port otherPort, bool bInput) {}

    public virtual void OnPortDisConnect(Port selfPort, Port otherPort, bool bInput) { }

    public virtual void Init()
    {
        GenerateOutputPort();

        borderElement = new VisualElement
        {
            style = {
                position = Position.Absolute,
                left = 0,
                top = 0,
                right = 0,
                bottom = 0,
                marginRight = -5,
                marginBottom = 5,
                borderBottomWidth = OutlineWidth,
                borderTopWidth = OutlineWidth,
                borderLeftWidth = OutlineWidth,
                borderRightWidth = OutlineWidth,
                borderBottomColor = Color.clear,
                borderTopColor = Color.clear,
                borderLeftColor = Color.clear,
                borderRightColor = Color.clear,
                backgroundColor = Color.clear
            }
        };

        // 插入到最底层
        Insert(0, borderElement);


        // titleContainer.style.marginBottom = new StyleLength(-20);
        // titleContainer.style.minHeight = new StyleLength(380);

        // 同步节点尺寸变化
        RegisterCallback<GeometryChangedEvent>(OnSizeChanged);

        // var _mainElement = this.Q<VisualElement>(className: "unity-graph-view-node-main-element");
        // if (_mainElement != null) {
        //     _mainElement.RegisterCallback<PointerEnterEvent>(OnMouseEnter);
        //     _mainElement.RegisterCallback<PointerLeaveEvent>(OnMouseLeave);
        // }
        RegisterCallback<PointerEnterEvent>(OnMouseEnter);
        RegisterCallback<PointerLeaveEvent>(OnMouseLeave);
        RegisterCallback<TooltipEvent>(OnTooltip);

        var collapseBtn = this.Q<VisualElement>("collapse-button");
        if (collapseBtn != null)
        {
            collapseBtn.parent.Remove(collapseBtn);
            // collapseBtn.visible = false;
        }

        _hasTips = !string.IsNullOrEmpty(GetTipsText());
    }

    protected bool _hasTips;
    protected Label _toolTipsLabel;
    private void ShowNodeTypeTipsAndNote()
    {
        if (_toolTipsLabel == null)
        {
            return;
        }

        var note = "";
        NodeBase actionNode = NodeData as NodeBase;
        if (actionNode!=null)
        {
            note = actionNode.note;
        }

        if (actionNode == null)
        {
            return;
        }

        var typeTips = GetTypeTips();
        var tipText = string.IsNullOrEmpty(typeTips) ? "" : $"[{typeTips}]";

        tipText += note;
        _toolTipsLabel.text = tipText;
        _toolTipsLabel.visible = !string.IsNullOrEmpty(tipText);
    }

    public string GetTypeTips()
    {
        NodeBase actionNode = NodeData as NodeBase;
        if (actionNode == null) return "";
        var typeTips = GetParentText(actionNode.GetType());
        var idx = typeTips.IndexOf('.');
        if (idx != -1)
        {
            typeTips = typeTips.Substring(idx + 1);
        }

        return typeTips;
    }

    private string GetParentText(Type type)
    {
        List<Type> baseTypes = GetAllBaseTypes(type);

        //组合分类
        foreach (var baseType in baseTypes)
        {
            var attr = baseType.GetCustomAttribute<GraphElementInfoAttribute>();
            if (attr != null)
            {
                if (!string.IsNullOrEmpty(attr.Category))
                {
                    return attr.Category;
                }
            }
        }

        return "";
    }

    private List<Type> GetAllBaseTypes(Type type)
    {
        List<Type> baseTypes = new List<Type>();
        Type currentBaseType = type.BaseType;

        while (currentBaseType != null)
        {
            baseTypes.Add(currentBaseType);
            currentBaseType = currentBaseType.BaseType;
        }

        return baseTypes;
    }

    protected void OnTooltip(TooltipEvent evt)
    {
        var output = this.Q<VisualElement>("output");
        VisualElement outputLabel = null;
        // if (output != null)
        // {
        //     outputLabel = output.Children().ElementAt(0).Children().ElementAt(1);
        // }
        if (evt.target == titleContainer.Q("title-label"))
        {
            var tipsText = GetTipsText();
            if (string.IsNullOrEmpty(tipsText))
            {
                tipsText = "该节点没有添加介绍";
            }
            evt.tooltip = tipsText;

            var bound = titleContainer.worldBound;
            bound.x = 0;
            bound.y = -50;
            bound.height *= -1;

            evt.rect = titleContainer.LocalToWorld(bound);
        }
    }

    protected string GetTipsText()
    {
        return NodeData.GetTipsText();
    }


    private DateTime _enterTime;
    private bool _mouseEnter;
    private void OnMouseEnter(PointerEnterEvent evt)
    {
        _mouseEnter = true;
        _enterTime = DateTime.Now;
    }

    private void OnMouseLeave(PointerLeaveEvent evt)
    {
        _mouseEnter = false;
    }
    //
    protected void CreateVariable2(GraphDebugUtils.VariableViewData viewData, Port port, int idx)
    {
        if (viewData.fieldValue == null)
        {
            return;
        }

        var wrapper = (VariableWrapperBase) viewData.fieldValue;
        if (wrapper == null)
        {
            return;
        }

        if (_createNew != FlowChartView.E_CreateVariableType.Find)
        {
            var list = ActionGraphEditorWindow.flowChart.Graph.variableList;
            if (wrapper.idxInList >= 0 && wrapper.idxInList < list.Count)
            {
                wrapper.SetInternalValue(list[wrapper.idxInList]);
                if (wrapper.GetInternalValue() != null)
                {
                    var node = ActionGraphEditorWindow.flowChart.FindVariableNode(wrapper.GetInternalValue());
                    ActionGraphEditorWindow.flowChart.ConnectPorts(node.outputContainer[0] as Port, port);
                }
            }
        }
    }

    protected void CreateOrFindVariable(GraphDebugUtils.VariableViewData viewData, Port port, int idx, int count)
    {
        if (viewData.fieldValue == null)
        {
            return;
        }

        var wrapper = (VariableWrapperBase) viewData.fieldValue;
        if (wrapper == null)
        {
            return;
        }

        //连接到事件节点
        if (wrapper.nodeIdx != -1)
        {
            var node = ActionGraphEditorWindow.flowChart.Graph.GetAllNodes()[wrapper.nodeIdx];
            var eventActionView = ActionGraphEditorWindow.flowChart.GetAllNodeViews().FirstOrDefault(x => x.NodeData == node);
            var portIdx = wrapper.nodeVarIdx;
            if (node.HasControlOutput)
            {
                portIdx += 1;
            }

            Debug.Log($"{eventActionView.outputContainer.childCount} {portIdx} {node.GetType().Name} {eventActionView.NodeData.GetType().Name}");
            ActionGraphEditorWindow.flowChart.ConnectPorts(eventActionView.outputContainer[portIdx] as Port, port);
        }
        //创建新变量
        else if (_createNew == FlowChartView.E_CreateVariableType.CreateNew)
        {
            if (wrapper.GetInternalValue() != null)
            {
                var node = ActionGraphEditorWindow.flowChart.CreateVariableNode(wrapper.GetInternalValue(), default, FlowChartView.E_CreateVariableType.CreateNew);
                var internalValue = ((VariableWrapperBase) viewData.fieldValue).internalValue;
                if (internalValue == null || internalValue.editorPos == default)
                {
                    node.NodePosition = NodePosition + new Vector2(-180f, (idx - 1) * 60);
                }
                ActionGraphEditorWindow.flowChart.ConnectPorts(node.outputContainer[0] as Port, port);
            }
        }
        //寻找已有变量
        else if (_createNew == FlowChartView.E_CreateVariableType.Find)
        {
            var list = ActionGraphEditorWindow.flowChart.Graph.variableList;
            if (wrapper.idxInList >= 0 && wrapper.idxInList < list.Count)
            {
                wrapper.SetInternalValue(list[wrapper.idxInList]);
                if (wrapper.GetInternalValue() != null)
                {
                    var node = ActionGraphEditorWindow.flowChart.FindVariableNode(wrapper.GetInternalValue());
                    ActionGraphEditorWindow.flowChart.ConnectPorts(node.outputContainer[0] as Port, port);
                }
            }
        }
    }

    protected abstract void GenerateOutputPort();
    public void GenerateInputPort()
    {
        var list = GetAllVariable();
        int idx = 0;
        foreach (var v in list)
        {
            // Debug.LogError(v.GetType());
            var port = GetPortForNode(this, Direction.Input, Port.Capacity.Single);
            port.portType = v.fieldInfo.FieldType;
            port.portColor = Color.cyan;
            port.userData = new PortData()
            {
                portType = E_PortType.VariableInput,
                variableInputData = v,
            };
            SetPortName(port);
            inputContainer.Add(port);

            idx++;
        }
    }

    private void SetPortName(Port port)
    {
        bool valid = true;
        var portData = port.userData as PortData;
        if (portData != null && portData.portType == E_PortType.VariableInput)
        {
            var attr = portData.variableInputData.fieldInfo.GetCustomAttribute<GraphVariableAttribute>();
            if (attr != null && attr.Required)
            {
                if (port.connections.Count() == 0)
                {
                    valid = false;
                }
            }
        }

        var portName = "";
        var v = portData.variableInputData;
        if (v != null)
        {
            var attr = v.fieldInfo.GetCustomAttribute<GraphVariableAttribute>();
            if (attr != null)
            {
                portName = attr.LabelText;
            }
            else
            {
                portName = v.fieldInfo.Name;
            }
            portName = $"[{v.fieldInfo.FieldType.ToString().Replace("Wrapper", "")}]" + portName;
            port.portName = valid ? portName : $"<color=red>{portName}</color>";
        }
    }

    public void ConnectPort()
    {
        var list = GetAllVariable();
        int idx = 0;
        foreach (var v in list)
        {
            var portIdx = idx;
            if (NodeData.HasControlInput)
            {
                portIdx += 1;
            }
            var port = inputContainer[portIdx] as Port;
            // CreateVariable2(v, port, idx);
            try
            {
                CreateOrFindVariable(v, port, idx, list.Count);
            }
            catch (Exception e)
            {
                LogLogger.LogError(e);
            }
            // CreateVariable2(v, port, idx);
            idx++;
        }
    }


    public virtual void BeforeSave()
    {
        NodeData.editorPos = GetPosition().position;
        foreach (var v in GetAllVariable())
        {
            if (v.fieldValue is VariableWrapperBase wrapperBase)
            {
                wrapperBase.Reset();
            }
        }
    }

    public abstract void AfterLoad();

    private void OnSizeChanged(GeometryChangedEvent evt)
    {
        borderElement.style.width = evt.newRect.width + OutlineWidth * 2;
        borderElement.style.height = evt.newRect.height + OutlineWidth * 2;
    }

    private Color _curDebugColor = Color.clear;
    public void SetDebugState(E_DebugExecuteState state, float lerp)
    {
        Color color;
        if (state == E_DebugExecuteState.Start)
        {
            color = (Color.cyan);
        }
        else if (state == E_DebugExecuteState.Running)
        {
            color = (Color.yellow);
        }
        else if (state == E_DebugExecuteState.Success)
        {
            color = (Color.green);
        }
        else if (state == E_DebugExecuteState.Failed)
        {
            color = new Color(0.6f, 0, 0);
        }
        else
        {
            color = Color.clear;
        }
        _curDebugColor = Color.Lerp(color, Color.clear, lerp);

        SetBorderColor(borderElement, _curDebugColor);
    }

    public void SetBorderColor(VisualElement element, Color color)
    {
        element.style.borderBottomColor = color;
        element.style.borderTopColor = color;
        element.style.borderLeftColor = color;
        element.style.borderRightColor = color;
    }

    public void SetBgColor(VisualElement element, Color color)
    {
        element.style.backgroundColor = color;
    }

    public Port GetPortForNode(NodeViewBase n, Direction portDir, Port.Capacity capacity = Port.Capacity.Single)
    {
        var port = Port.Create<GraphEdge>(Orientation.Horizontal, portDir, capacity, typeof(object));
        port.AddManipulator(new EdgeConnector<Edge>(new GraphEdgeConnectorListener()));
        return port;
    }

    public override void OnSelected()
    {
        base.OnSelected();
        ActionGraphEditorWindow.inspector.UpdateSelection(this);
    }

    public virtual void RefreshNodeInfo()
    {
        ShowNodeTypeTipsAndNote();

        foreach (var v in inputContainer.Children())
        {
            if (v is Port selfPort)
            {
                SetPortName(selfPort);
            }
        }

        if (_mouseEnter)
        {
            var ts = System.DateTime.Now - _enterTime;
            if (ts.TotalSeconds > 2)
            {

            }
        }
    }

    public override Rect GetPosition()
    {
        return base.GetPosition();
    }

    // public void OnEdgeCreate(Edge edge)
    // {
    //     BaseNodeView targetView = edge.input.node as BaseNodeView;
    //     state.nextFlow = targetView.state as MonoState;
    // }
    //
    // public override void OnEdgeRemove(Edge edge)
    // {
    //     base.OnEdgeRemove(edge);
    //
    //     state.nextFlow = null;
    // }
}
