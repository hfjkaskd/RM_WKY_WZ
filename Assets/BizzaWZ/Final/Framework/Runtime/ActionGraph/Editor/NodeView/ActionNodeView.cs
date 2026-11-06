using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


public class ActionNodeView : NodeViewBase
{
    private NodeBase _node;
    public NodeBase Data => _node;

    public ActionNodeView(NodeBase node, FlowChartView.E_CreateVariableType createNew)
    {
        _createNew = createNew;
        _node = node;
        Init();
    }

    public override GraphElementBase NodeData => Data;
    public override string DebugName => Data.DebugName;

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

    public override List<GraphDebugUtils.VariableViewData> GetAllVariable()
    {
        var list = GraphDebugUtils.GetAllVariableInClass<VariableWrapperBase>(_node);
        return list;
    }

    public override void Init()
    {
        base.Init();

        bool bRootOrEvent = Data is RootNode or OutputActionBase;

        var titleEle = this.Q<VisualElement>("title-label");
        if (titleEle != null)
        {
            titleEle.style.color = new StyleColor(bRootOrEvent ? new Color(0.9f, 0.2f, 0.9f, 1) : new Color(1f, 0.6f, 0f, 1));
            titleEle.style.fontSize = new StyleLength(18);
            titleEle.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
        }

        {
            var titleLabel = titleContainer.Q("title-label");
            titleLabel.style.marginTop = new StyleLength(0f);
            titleLabel.style.marginBottom = new StyleLength(0f);

            var label = new Label();
            label.style.fontSize = 13;
            label.text = "";
            label.style.marginLeft = new StyleLength(8);
            label.style.marginTop = new StyleLength(-10);
            label.style.color = new StyleColor(new Color(0, 0.8f, 0.4f, 1));
            label.style.maxWidth = new StyleLength(200);
            label.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            // titleContainer.Insert(0, label);
            titleContainer.Add(label);
            _toolTipsLabel = label;

            titleContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
            // titleContainer.style.minHeight = 42;
            titleContainer.style.minWidth = new StyleLength(150);
            titleContainer.style.maxWidth = new StyleLength(210);
        }

        var titleElement = this.Q<VisualElement>("title");
        if (Data is ActionNodeBase)
        {
            SetBgColor(titleElement, new Color(0.541f, 0.506f, 0.18f, 0.66f));
            // SetBgColor(titleElement, new Color(0.254f, 0.254f, 0.254f, 0.8f));
        }
        else if (Data is DecoratorNodeBase)
        {
            SetBgColor(titleElement, new Color(0.18f, 0.54f, 0.54f, 0.83f));
        }
        else if (Data is CompoundNodeBase)
        {
            SetBgColor(titleElement, new Color(0.65f, 0.32f, 0.17f, 0.8f));
        }
        else if (Data is ConditionNodeBase)
        {
            SetBgColor(titleElement, new Color(0.22f, 0.6f, 0.22f, 0.67f));
        }
        else
        {
            SetBgColor(titleElement, new Color(0.48f, 0.22f, 0.43f, 0.76f));
        }
    }

    protected override void GenerateOutputPort()
    {
        bool hasInput = !(Data is RootNode || Data is OutputActionBase);

        if (hasInput)
        {
            Port input = GetPortForNode(this, Direction.Input, Port.Capacity.Single);
            input.portName = "输入";
            inputContainer.Add(input);
            input.portColor = new Color(1f, 0.6f, 0f, 1);
            input.portType = typeof(NodeBase);
            input.userData = new PortData() { portType = E_PortType.Action, actionData = Data};
        }

        var hasOutput = Data.HasControlOutput;
        if (hasOutput)
        {
            Port output = GetPortForNode(this, Direction.Output, Data.multChild ? Port.Capacity.Multi : Port.Capacity.Single);
            output.portName = "输出";
            output.portColor = new Color(1f, 0.6f, 0f, 1);
            output.portType = typeof(NodeBase);
            output.userData = new PortData() {portType = E_PortType.Action, actionData = Data};

            outputContainer.Add(output);
        }


        var list = GetAllVariable();

        var rect = GetPosition();
        rect.position = Data.editorPos;
        var size = rect.size;
        size.y += list.Count * 20;
        rect.size = size;
        SetPosition(rect);

        if (Data is IOutputVariableNode eventActionBase)
        {
            foreach (var v in eventActionBase.OutputVariables)
            {
                var inst = (VariableBase)Activator.CreateInstance(v.Item1);
                var port = GetPortForNode(this, Direction.Output, Port.Capacity.Multi);
                var portName = "";
                var typePrefix = inst.WrapperType.ToString().Replace("Wrapper", "");
                portName = $"{v.Item2}[{typePrefix}]";

                port.portType = inst.WrapperType;
                port.portName = portName;
                port.portColor = Color.cyan;
                port.userData = new PortData()
                {
                    portType = E_PortType.VariableOutput,
                    variableOutputData = v,
                };
                outputContainer.Add(port);
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
        base.RefreshNodeInfo();
        var debugName = _node.DebugName;
        if (string.IsNullOrEmpty(debugName))
        {
            var attr = _node.GetType().GetCustomAttribute<GraphElementInfoAttribute>();
            if (attr != null) debugName = attr.Text;
        }
        title = (!string.IsNullOrEmpty(_node.titleNote) ? _node.titleNote : debugName) + (_hasTips ? "*" : "");
        var titleLabel = titleContainer.Q("title-label");
        titleContainer.style.height = titleLabel.contentRect.size.y + _toolTipsLabel.contentRect.size.y;

        var lerp = 1f;
        if (!string.IsNullOrEmpty(GraphDebugUtils.debugGraphName))
        {
            lerp = Mathf.Clamp01((Time.frameCount - _node.lastHasStateFrame) / 60f) * 0.5f;
        }
        SetDebugState(_node.debugState, lerp);
    }
}