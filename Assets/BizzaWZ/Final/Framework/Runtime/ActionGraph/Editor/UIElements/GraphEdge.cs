using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityExtensions;

public class GraphEdge : Edge
{
    // public GraphEdge()
    // {
    //     // this.RegisterCallback<MouseUpEvent>();
    //     Debug.LogError("GraphEdge");
    // }
}


public class GraphEdgeConnectorListener : IEdgeConnectorListener
{
    private GraphViewChange m_GraphViewChange;
    private List<Edge> m_EdgesToCreate;
    private List<GraphElement> m_EdgesToDelete;

    public GraphEdgeConnectorListener()
    {
        this.m_EdgesToCreate = new List<Edge>();
        this.m_EdgesToDelete = new List<GraphElement>();
        this.m_GraphViewChange.edgesToCreate = this.m_EdgesToCreate;
    }

    private bool _hasInput;
    private bool _hasOutput;
    private Port _targetPort;

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        _hasInput = false;
        _hasOutput = false;
        _targetPort = null;

        if (edge.input != null)
        {
            _hasInput = true;
            _targetPort = edge.input;
            if (edge.input.portType.IsSubclassOf(typeof(VariableWrapperBase)))
            {

                var typeName = "Variable_" + edge.input.portType.Name.Replace("Wrapper", "");
                var targetType = ReflectionUtils.GetOtherTypeInSameAssembly(typeof(ActionGraphBase), typeName);

                ActionGraphEditorWindow.flowChart.ShowCreateNodeWindow(
                    new List<Type>() {targetType},
                    null,
                    position, (nodeView) => { OnCreateFinish(nodeView, edge); });
            }
        }
        if(edge.output != null)
        {
            _hasOutput = true;
            _targetPort = edge.output;

            if (edge.output.node is ActionNodeView && edge.output.portType == typeof(NodeBase))
            {
                ActionGraphEditorWindow.flowChart.ShowCreateNodeWindow(
                    new List<Type>() {typeof(NodeBase)},
                    new List<Type>() {typeof(OutputActionBase)},
                    position, (nodeView) => { OnCreateFinish(nodeView, edge); });
            }
        }
    }

    private void OnCreateFinish(NodeViewBase nodeView, Edge edge)
    {
        if (_hasInput)
        {
            ActionGraphEditorWindow.flowChart.ConnectPorts(nodeView.outputContainer[0] as Port, _targetPort);
        }
        else if(_hasOutput)
        {
            ActionGraphEditorWindow.flowChart.ConnectPorts(_targetPort, nodeView.inputContainer[0] as Port);
        }
    }

    public void OnDrop(UnityEditor.Experimental.GraphView.GraphView graphView, Edge edge)
    {
        this.m_EdgesToCreate.Clear();
        this.m_EdgesToCreate.Add(edge);
        this.m_EdgesToDelete.Clear();
        if (edge.input.capacity == Port.Capacity.Single)
        {
            foreach (Edge connection in edge.input.connections)
            {
                if (connection != edge)
                    this.m_EdgesToDelete.Add((GraphElement) connection);
            }
        }
        if (edge.output.capacity == Port.Capacity.Single)
        {
            foreach (Edge connection in edge.output.connections)
            {
                if (connection != edge)
                    this.m_EdgesToDelete.Add((GraphElement) connection);
            }
        }
        if (this.m_EdgesToDelete.Count > 0)
            graphView.DeleteElements((IEnumerable<GraphElement>) this.m_EdgesToDelete);
        List<Edge> edgesToCreate = this.m_EdgesToCreate;
        if (graphView.graphViewChanged != null)
            edgesToCreate = graphView.graphViewChanged(this.m_GraphViewChange).edgesToCreate;
        foreach (Edge edge1 in edgesToCreate)
        {
            // graphView.AddElement((GraphElement) edge1);
            // edge.input.Connect(edge1);
            // edge.output.Connect(edge1);
            ActionGraphEditorWindow.flowChart.ConnectPorts(edge.output, edge.input);
        }
    }
}