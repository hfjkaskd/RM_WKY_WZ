using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bizza;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 画布区
/// </summary>
 [Obfuz.ObfuzIgnore]
public class FlowChartView : GraphView
{
    #region 画布

    public new class UxmlFactory : UxmlFactory<FlowChartView, UxmlTraits>
    {
    }

    public ActionGraphBase Graph => _graph;

    public static NodeBase Root
    {
        get { return ActionGraphEditorWindow.flowChart?._graph?.root; }
    }

    public float VariableAlpha => Mathf.Max(Mathf.Clamp01(scale * 3 - 1), 0.1f);

    public string CurGraphFilePath => _filePath + "/" + _fileName + ".asset";
    // private TActionNode _root => _graph.root;
    private ActionGraphBase _graph;
    private string __fileName;

    private string _fileName
    {
        get => __fileName;
        set
        {
            __fileName = value;
            var fileNameInput =
                ActionGraphEditorWindow.Instance.rootVisualElement.Q<UnityEngine.UIElements.TextField>("FileNameInput");
            fileNameInput.value = __fileName;
        }
    }

    private string __filePath;
    private string _filePath
    {
        get => __filePath;
        set
        {
            __filePath = value;
            var filePathInput =
                ActionGraphEditorWindow.Instance.rootVisualElement.Q<UnityEngine.UIElements.TextField>("FilePathInput");
            if (filePathInput != null)
            {
                filePathInput.value = __fileName;
            }
        }
    }

    public NodeViewBase RootView
    {
        get
        {
            return nodes.FirstOrDefault(x => x is ActionNodeView actionNodeView && actionNodeView.Data == _graph.root) as NodeViewBase;
        }
    }

    public ActionGraphEditorWindow window;

    public FlowChartView()
    {
        // var grid = new GridBackground();
        // Insert(0, grid);
        // grid.StretchToParentSize();
        var zoomer = new ContentZoomer();
        zoomer.maxScale = 2f;
        zoomer.minScale = 0.1f;
        this.AddManipulator(zoomer);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        this.RegisterCallback<MouseDownEvent>(OnMouseDown);
        graphViewChanged = OnGraphViewChanged;


        nodeCreationRequest += context =>
        {
            ShowCreateNodeWindow(null, null, context.screenMousePosition);
        };

        GraphDebugUtils.IsSelect = CheckSelected;
        styleSheets.Add(Resources.Load<StyleSheet>("ActionGraphCanvasView"));
        Insert(0, new GridBackground());
    }

    private bool CheckSelected(NodeBase node)
    {
        var nodes = GetSelectedNodeViews();
        foreach (var v in nodes)
        {
            if (v.NodeData == node)
            {
                return true;
            }
        }

        return false;
    }

    private SearchMenuWindowProvider _provider;
    private Action<NodeViewBase> _onMenuCreateNode;
    public void ShowCreateNodeWindow(List<Type> includeTypes, List<Type> excludeTypes, Vector2 pos, Action<NodeViewBase> cb = null)
    {
        SearchMenuWindowProvider.includeTypes = includeTypes;
        SearchMenuWindowProvider.excludeTypes = excludeTypes;
        _onMenuCreateNode = cb;
        if (_provider == null)
        {
            _provider = ScriptableObject.CreateInstance<SearchMenuWindowProvider>();
        }

        // Debug.LogError(pos + " " + MousePos2CanvasPos(pos));
        _mousePos = pos;
        SearchWindow.Open(new SearchWindowContext(_mousePos), _provider);
    }

    public void Init()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= OnApplicationEnd;
        EditorApplication.playModeStateChanged += OnApplicationEnd;

        var graphTypeBtn = ActionGraphEditorWindow.Instance.rootVisualElement.Q<Button>("GraphTypeBtn");
        graphTypeBtn.clicked += SelectGraphType;

        var saveBtn = ActionGraphEditorWindow.Instance.rootVisualElement.Q<Button>("SaveBtn");
        saveBtn.clicked += Save;

        var history = ActionGraphEditorWindow.Instance.rootVisualElement.Q<Button>("History");
        history.clicked += ShowHistory;

        var startDebug = ActionGraphEditorWindow.Instance.rootVisualElement.Q<Button>("StartDebug");
        startDebug.clicked += StartDebugBtn;

        var endDebug = ActionGraphEditorWindow.Instance.rootVisualElement.Q<Button>("EndDebug");
        endDebug.clicked += EndDebug;
    }

    private void OnApplicationEnd(PlayModeStateChange e)
    {
        if (e == PlayModeStateChange.ExitingPlayMode)
        {
            if (IsDebugging())
            {
                EndDebug();
            }
        }
    }

    public void Dispose()
    {
        // GraphDebugUtils.curDebugGraph = null;
        // GraphDebugUtils.subDataToEditor = null;
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= OnApplicationEnd;
    }

    void Update()
    {
        foreach (var node in nodes)
        {
            if (node is NodeViewBase nodeView)
            {
                nodeView?.RefreshNodeInfo();
            }
        }

        var endDebug = ActionGraphEditorWindow.Instance.rootVisualElement.Q<Button>("EndDebug");
        endDebug.style.color = IsDebugging() ? Color.red : Color.white;
    }

    public void OnGUI()
    {
        var cur = Event.current;
        if (cur != null)
        {
            if (cur.type == EventType.KeyDown)
            {
                ActionGraphLog.Info(Event.current.keyCode.ToString());
                if (cur.keyCode == KeyCode.Delete)
                {
                    DeleteSelectedNodes();
                    var list = selection.OfType<Edge>().ToList();
                    foreach (Edge edge in list)
                    {
                        OnEdgeRemove(edge);
                        RemoveElement(edge);
                    }
                }

                if ((cur.keyCode == KeyCode.D || cur.keyCode == KeyCode.V) && cur.control)
                {
                    _mousePos = Event.current.mousePosition;
                    CopySelectedNodes();
                }

                if (cur.keyCode == KeyCode.S && cur.control)
                {
                    Save();
                }

                if (cur.keyCode == KeyCode.UpArrow && cur.alt && cur.control)
                {
                    GotoPrevGraph();
                }

                if (cur.keyCode == KeyCode.DownArrow && cur.alt && cur.control)
                {
                    GotoNextGraph();
                }
            }
        }
    }

    #endregion


    #region ===========画布区操作===========

    #region 节点连线操作

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if (graphViewChange.elementsToRemove != null)
        {
            graphViewChange.elementsToRemove.ForEach(elem =>
            {

            });
        }

        if (graphViewChange.edgesToCreate != null)
        {
            graphViewChange.edgesToCreate.ForEach(edge =>
            {

            });
        }

        return graphViewChange;
    }

    private void RemoveEdge(Edge edge)
    {
        OnEdgeRemove(edge);
        RemoveElement(edge);
    }

    private void OnEdgeRemove(Edge edge)
    {

        if (edge.input != null)
        {
            edge.input.Disconnect(edge);
        }

        if (edge.output != null)
        {
            edge.output.Disconnect(edge);
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node &&
                (startPort.portType == null && endPort.portType == null || startPort.portType == endPort.portType))
            .ToList();
    }

    #endregion


    #region 鼠标键盘事件

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 1) // 鼠标右键
        {
            ShowMenu(evt);
        }
    }

    #endregion


    #region 右键菜单

    private void ShowMenu(MouseDownEvent evt)
    {
        // 创建上下文菜单
        GenericMenu menu = new GenericMenu();
        _mousePos = evt.localMousePosition;
        menu.AddItem(new GUIContent("添加节点"), false, () =>
        {
            ShowCreateNodeWindow(null, null, _mousePos);
        });
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("删除节点"), false, () => DeleteSelectedNodes());
        menu.AddItem(new GUIContent("复制节点"), false, () => CopySelectedNodes());

        // 显示菜单
        menu.ShowAsContext();
    }



    public void MenuAddNode(object userData)
    {
        var delta = Vector2.zero;
        CreateNodeArgs args = (CreateNodeArgs) userData;
        NodeViewBase nodeView;
        if (args.varOrAction)
        {
            nodeView = CreateVariableNode(args.type);
            delta = new Vector2(-120, -50);
        }
        else
        {
            nodeView = CreateActionNode(args.type);
            delta = new Vector2(0, -60);
        }

        if (nodeView != null)
        {
            nodeView.SetPosition(new Rect(MousePos2CanvasPos(_mousePos) + delta, nodeView.GetPosition().size));

            nodeView.GenerateInputPort();
            nodeView.ConnectPort();
        }

        _onMenuCreateNode?.Invoke(nodeView);
        _onMenuCreateNode = null;
    }

    private NodeViewBase CreateActionNode(Type type)
    {
        var data = Activator.CreateInstance(type);
        if (data == null)
        {
            ActionGraphLog.Error($"动态创建{type}失败");
            return null;
        }

        if (data is OutputActionBase eventAction && nodes.FirstOrDefault(x => x is ActionNodeView actionNodeView && actionNodeView.Data.GetType() == data.GetType()) != null)
        {
            ActionGraphLog.Error($"已经存在该事件：{type}");
            return null;
        }

        var pos = MousePos2CanvasPos(_mousePos);
        return CreateActionNode((NodeBase) data, pos, E_CreateVariableType.CreateNew);
    }

    #endregion

    #endregion


    #region 导航区

    private Dictionary<NodeBase, NodeViewBase> _nodeMap = new();

    //根据连接关系设置父子级和变量关系
    private void ProcessConnectsPre()
    {
        var allNodes = GetAllNodeViews();
        foreach (var nodeView in allNodes)
        {
            if (nodeView is ActionNodeView actionNodeView)
            {
                if (actionNodeView.Data.children == null)
                {
                    actionNodeView.Data.children = new List<NodeBase>();
                }
                else
                {
                    actionNodeView.Data.children.Clear();
                }
            }
        }

        var nodes = GetAllNodeViews();
        foreach (var node in nodes)
        {
            GetConnectedPort(nodes, node, out var inputConnect, out var outputConnect);
            foreach (var v in inputConnect)
            {
                var left = node;
                var right = v.Item3;
                var outputPort = left.outputContainer[v.Item2] as Port;
                var inputPort = right.inputContainer[v.Item1] as Port;
                var inputPortData = inputPort.userData as PortData;
                var outputPortData = outputPort.userData as PortData;

                //Action连接
                if (left is ActionNodeView leftAction && right is ActionNodeView rightAction && IsControlPort(inputPort) && IsControlPort(outputPort))
                {
                    leftAction.Data.children.Add(rightAction.Data);
                    rightAction.Data.parent = leftAction.Data;
                }
                else if (left is VariableNodeView varLeft)
                {
                    ActionGraphLog.Assert(inputPortData != null, "error");
                    var wrapper = (VariableWrapperBase)Activator.CreateInstance(varLeft.Data.WrapperType);
                    wrapper.SetInternalValue(varLeft.Data);
                    // varLeft.VariableData.fieldInfo.SetValue(varLeft.VariableData.instance, wrapper);
                    var data = inputPortData.variableInputData;
                    data.fieldInfo.SetValue(data.instance, wrapper);
                }
                //Action节点的输出变量被连接
            }
        }
    }

    private void ProcessVariableConnects()
    {
        var nodes = GetAllNodeViews();
        foreach (var node in nodes)
        {
            GetConnectedPort(nodes, node, out var inputConnect, out var outputConnect);
            foreach (var v in inputConnect)
            {
                var left = node;
                var right = v.Item3;
                var outputPort = left.outputContainer[v.Item2] as Port;
                var inputPort = right.inputContainer[v.Item1] as Port;
                var inputPortData = inputPort.userData as PortData;
                var outputPortData = outputPort.userData as PortData;

                if (left is ActionNodeView actionLeft && actionLeft.Data is IOutputVariableNode eventAction && outputPortData.portType == E_PortType.VariableOutput)
                {
                    var variable = GetOrCreateDefaultVariable(outputPortData.variableOutputData.Item1);
                    var data = inputPortData.variableInputData;

                    var wrapper = (VariableWrapperBase)Activator.CreateInstance(variable.WrapperType);
                    wrapper.SetInternalValue(variable);
                    wrapper.nodeIdx = _graph.GetAllNodes().IndexOf(actionLeft.Data);
                    wrapper.nodeVarIdx = eventAction.OutputVariables.ToList().FindIndex(x=>x.Item1 == variable.GetType());
                    ActionGraphLog.Assert(wrapper.nodeIdx != -1, "error");

                    data.fieldInfo.SetValue(data.instance, wrapper);
                }
            }
        }
    }

    private VariableBase GetOrCreateDefaultVariable(Type type)
    {
        var variable = _graph.defaultVariableList.FirstOrDefault(x => x.GetType() == type);
        if (variable == null)
        {
            variable = (VariableBase) Activator.CreateInstance(type);
            _graph.defaultVariableList.Add(variable);
        }

        return variable;
    }

    private bool IsDebugging()
    {
        return !string.IsNullOrEmpty(GraphDebugUtils.debugGraphName);
    }

    public void Save()
    {
        if (IsDebugging())
        {
            EditorUtility.DisplayDialog("", "先结束调试再保存", "OK");
            return;
        }

        var fileNameInput = ActionGraphEditorWindow.Instance.rootVisualElement.Q<UnityEngine.UIElements.TextField>("FileNameInput");
        Save(fileNameInput.value);

        AddHistory(fileNameInput.value);
    }

    private void StartDebugBtn()
    {
        var selection = Selection.activeObject;
        if (selection != null && selection is GameObject go && go.GetComponent<GameActor>() != null)
        {
            GraphDebugUtils.debugGraph = null;
            GraphDebugUtils.debugOwner = go.GetComponent<GameActor>();
            GraphDebugUtils.debugGraphName = __fileName;
        }
        else
        {
            EditorUtility.DisplayDialog("", "需要先选中目标Actor", "OK");
        }
    }

    private void EndDebug()
    {
        var graphName = GraphDebugUtils.debugGraphName;

        GraphDebugUtils.debugGraph = null;
        GraphDebugUtils.debugOwner = null;
        GraphDebugUtils.debugGraphName = null;

        ActionGraphEditorWindow.ShowWindow(graphName);
    }

    private void ShowHistory()
    {
        var list = GetHistory();
        GenericMenu menu = new GenericMenu();
        foreach (var v in list)
        {
            var shortName = v;
            if (shortName.Contains("/"))
            {
                shortName = shortName.Split('/').Last();
            }
            menu.AddItem(new GUIContent(shortName), shortName == __fileName, OnOpenFile, v);
        }
        menu.ShowAsContext();
    }

    public static int _curHistoryIdx;
    private void GotoPrevGraph()
    {
        _curHistoryIdx--;
        if (_curHistoryIdx <= 0) _curHistoryIdx = GetHistory().Count - 1;
        Debug.LogError(_curHistoryIdx + " " + GetHistory().Count);
        OnOpenFile(GetHistory()[_curHistoryIdx]);
    }

    private void GotoNextGraph()
    {
        _curHistoryIdx = (_curHistoryIdx + 1) % GetHistory().Count;
        Debug.LogError(_curHistoryIdx + " " + GetHistory().Count);
        OnOpenFile(GetHistory()[_curHistoryIdx]);
    }

    private void OnOpenFile(object fileName)
    {
        DonotAddHistory = true;
        GraphDebugUtils.OpenGraph((string)fileName);
        DonotAddHistory = false;
    }

    public void Save(string fileName)
    {
        try
        {
            _graph.EditorInit();
            foreach (var node in nodes)
            {
                if (node is NodeViewBase nodeViewBase)
                {
                    nodeViewBase.BeforeSave();
                }
            }

            //todo:检查合法性
            CheckGraphValid();

            CheckAddDefaultVariables();

            _graph.unconnectedNodes = GetUnconnectedNodes();

            ProcessConnectsPre();

            _graph.variableList = GetVariableList();
            _nodeMap.Clear();
            foreach (var node in nodes)
            {
                if (node is ActionNodeView nodeView)
                {
                    _nodeMap.Add(nodeView.Data, nodeView);
                }
            }

            foreach (var v in _graph.unconnectedNodes)
            {
                RecursiveSortChildren(v);
            }

            RecursiveSortChildren(_graph.root);

            ProcessVariableConnects();
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("", $"保存图表出错：{e.ToString()}", "OK");
            LogLogger.LogError($"保存图表出错：{e.ToString()}");
            return;
        }

        if (_graph != null)
        {
            GraphSaveUtils.Save(_graph, fileName, _filePath);
            _fileName = fileName;
            ActionGraphUtils.newGraphs[_fileName] = _graph;
            GraphPoolUtils.Clear();
        }
    }

    private void CheckAddDefaultVariables()
    {
        _graph.defaultVariableList = new List<VariableBase>();
        foreach (var node in nodes)
        {
            if (node is ActionNodeView actionNodeView && actionNodeView.NodeData is IOutputVariableNode eventAction)
            {
                foreach (var v in eventAction.OutputVariables)
                {
                    _graph.defaultVariableList.Add((VariableBase)Activator.CreateInstance(v.Item1));
                }
            }
        }
    }

    private List<VariableBase> GetVariableList()
    {
        HashSet<VariableBase> variableSet = new();
        List<VariableWrapperBase> wrapperList = new();
        foreach (var node in nodes)
        {
            if (node is NodeViewBase nodeViewBase)
            {
                var varList = nodeViewBase.GetAllVariable();
                foreach (var v in varList)
                {
                    if (v.fieldValue != null)
                    {
                        wrapperList.Add((VariableWrapperBase)v.fieldValue);
                    }
                }
            }

            if (node is VariableNodeView varNodeView)
            {
                // varNodeView.Data.idxInList
                var internalValue = varNodeView.Data;
                if (internalValue != null)
                {
                    variableSet.Add(internalValue);
                }
            }
        }

        var variableList = variableSet.ToList();
        foreach (var v in wrapperList)
        {
            if (v.GetInternalValue() != null)
            {
                v.idxInList = variableList.IndexOf(v.GetInternalValue());
                // v.SetInternalValue(null);
            }
            else
            {
                v.idxInList = -1;
            }
        }

        return variableList;
    }

    private List<NodeBase> GetUnconnectedNodes()
    {
        var unconectedNodes = new List<NodeBase>();
        foreach (var v in nodes)
        {
            if (v is NodeViewBase nodeView)
            {
                //将没有被连接的节点的根节点保存起来
                if (nodeView is ActionNodeView actionNodeView && !HasParent(nodeView) && nodeView.NodeData is not RootNode)
                {
                    if (actionNodeView.Data != null)
                    {
                        unconectedNodes.Add(actionNodeView.Data);
                    }
                }
            }
        }

        return unconectedNodes;
    }

    private bool HasParent(NodeViewBase nodeView)
    {
        foreach (var v in nodeView.inputContainer.Children())
        {
            if (v is Port port && IsControlPort(port))
            {
                foreach (var edge in port.connections)
                {
                    if (edge.output.node is ActionNodeView && IsControlPort(edge.output))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsControlPort(Port port)
    {
        return port.portType == typeof(NodeBase);
    }

    private void CheckGraphValid()
    {

    }

    private void GetAllConnectedNodes(ActionNodeView node, HashSet<ActionNodeView> ret)
    {
        RecursiveGetChild(node, ret);
    }

    private void RecursiveGetChild(ActionNodeView node, HashSet<ActionNodeView> ret)
    {
        ret.Add(node);
        foreach (var v in node.Children())
        {
            if (v is ActionNodeView actionNodeView)
            {
                RecursiveGetChild(actionNodeView, ret);
            }
        }
    }

    public void CreateNew()
    {
        _fileName = "new_action";
        _filePath = GraphSaveUtils.SavePath;
        SetNewGraph(new ActionGraph_Teach(_fileName));
    }

    public void Read(ActionGraphBase graph)
    {
        if (graph == null) return;
        _fileName = graph.graphName;
        SetNewGraph(graph);

        AddHistory(_fileName);
    }

    public void Read(string fileNameOrPath)
    {
        var graph = GraphSaveUtils.EditorRead(fileNameOrPath, out var fileName, out var filePath);
        if (graph == null)
        {
            ActionGraphLog.Error($"无法读取文件：{fileNameOrPath}");
            return;
        }

        _fileName = fileName;
        _filePath = filePath;
        SetNewGraph(graph);

        if (!_fileName.Contains("new_action"))
        {
            EditorPrefs.SetString("ActionGraph_LastOpenFileName", _fileName);
            AddHistory(_fileName);
        }
        ActionGraphLog.Verbose($"读取图表:{fileNameOrPath}");
    }

    private List<string> GetHistory()
    {
        string history = EditorPrefs.GetString("GraphEditor.History", "");
        var list = history.Split(",").ToList();
        return list;
    }

    public static bool DonotAddHistory = false;
    private void AddHistory(string path)
    {
        if (DonotAddHistory) return;

        var list = GetHistory();
        list.Remove(path);
        list.Insert(0, path);
        if (list.Count > 15)
        {
            list.RemoveLast();
        }
        EditorPrefs.SetString("GraphEditor.History", string.Join(',', list));
    }


    public void SelectGraphType()
    {
        // 创建上下文菜单
        GenericMenu menu = new GenericMenu();
        foreach (var type in TypeCache.GetTypesDerivedFrom<ActionGraphBase>())
        {
            var atr = type.GetAttribute<GraphElementInfoAttribute>();
            if (atr != null)
            {
                menu.AddItem(new GUIContent(atr.Text), false, Editor_OnValueChanged, type);
            }
        }

        // 显示菜单
        menu.ShowAsContext();
    }

    private void Editor_OnValueChanged(object value)
    {
        var m_ItemTypeToAdd = value as Type;

        ActionGraphBase ret = null;
        if (m_ItemTypeToAdd != null)
        {
            if (Activator.CreateInstance(m_ItemTypeToAdd, _fileName) is ActionGraphBase inst)
            {
                ret = inst;
            }
        }
        else
        {
            ret = null;
        }

        SetNewGraph(ret);
    }

    #endregion


    #region 创建删除节点

    public struct CreateNodeArgs
    {
        public bool varOrAction;
        public Type type;
    }

    public enum E_CreateVariableType
    {
        None,
        CreateNew,
        Find,
    }

    private NodeViewBase CreateActionNode(NodeBase data, Vector2 pos, E_CreateVariableType createNew)
    {
        if (pos != default)
        {
            data.editorPos = pos;
        }
        var nodeView = new ActionNodeView((NodeBase) data, createNew);

        // nodeView.OnNodeSelected = OnNodeSelected;
        // nodeView.state = (NodeState)userSeletionGo.AddComponent(type);
        nodeView.SetPosition(new Rect(pos, nodeView.GetPosition().size));

        this.AddElement(nodeView);
        if (data != null)
        {
            nodeView.userData = data;
        }

        return nodeView;
    }

    public NodeViewBase FindVariableNode(VariableBase variableBase)
    {
        foreach (var node in nodes)
        {
            if (node is VariableNodeView varNode)
            {
                if (varNode.Data == variableBase)
                {
                    return varNode;
                }
            }
        }

        return null;
    }

    public NodeViewBase CreateNode(object data, Vector2 pos, E_CreateVariableType createNew)
    {
        NodeViewBase node = null;
        if (data is NodeBase nodeBase)
        {
            node = CreateActionNode(nodeBase, pos, createNew);
        }
        else if (data is VariableBase variableBase)
        {
            node = CreateVariableNode(variableBase, pos, createNew);
        }

        node.GenerateInputPort();
        node.ConnectPort();

        return node;
    }

    public NodeViewBase CreateVariableNode(Type type)
    {
        var data = Activator.CreateInstance(type);
        if (data == null)
        {
            ActionGraphLog.Error($"动态创建{type}失败");
            return null;
        }

        var variable = (VariableBase) data;
        var pos = MousePos2CanvasPos(_mousePos);
        return CreateVariableNode(variable, pos, E_CreateVariableType.CreateNew);
    }

    public NodeViewBase CreateVariableNode(VariableBase data, Vector2 pos, E_CreateVariableType createNew)
    {
        var nodeView = new VariableNodeView(data, createNew);
        this.AddElement(nodeView);
        if (data != null)
        {
            nodeView.userData = data;
        }

        if (pos != default)
        {
            nodeView.NodePosition = pos;
        }
        return nodeView;
    }

    private void DeleteSelectedNodes()
    {
        var nodes = GetSelectedNodeViews();
        foreach (var node in nodes)
        {
            DeleteNode(node);
        }
    }

    private void CopySelectedNodes()
    {
        var nodes = GetAvailableSelectedNodeViews();
        if (nodes.Count == 0)
        {
            return;
        }

        var prevCenter = Vector2.zero;
        var total = Vector2.zero;
        foreach (var node in nodes)
        {
            total += node.NodePosition;
        }
        prevCenter = total / nodes.Count;
        var map = new Dictionary<NodeViewBase, NodeViewBase>();
        foreach (var node in nodes)
        {
            // if (node is ActionNodeView actionNodeView)
            {
                var newData = CloneNodeData(node.NodeData);
                var curPos = MousePos2CanvasPos(_mousePos);
                var newNodeView = CreateNode(newData, curPos, E_CreateVariableType.None);
                newNodeView.NodePosition = node.NodePosition + (curPos - prevCenter);
                map.Add(node, newNodeView);

                foreach (var v in node.inputContainer.Children())
                {
                    if (v is Port port)
                    {
                        if (port != null)
                        {
                            foreach (var edge in port.connections)
                            {
                                {
                                    var children = edge.output.node.Children().ToList();
                                    foreach (var child in children)
                                    {
                                        var str = child.GetType().ToString();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        foreach (var node in nodes)
        {
            GetConnectedPort(nodes, node, out var inputConnect, out var outputConnect);
            foreach (var v in inputConnect)
            {
                var left = map[node];
                var right = map[v.Item3];
                // var port1 = map[node].outputContainer[v.Item1] as Port;
                // var port2 = map[v.Item3].inputContainer[v.Item2] as Port;
                var outputPort = left.outputContainer[v.Item2] as Port;
                var inputPort = right.inputContainer[v.Item1] as Port;
                ConnectPorts(outputPort, inputPort);
            }
        }
    }

    public void ConnectPorts(Port outputPort, Port inputPort)
    {
        if (outputPort == null || inputPort == null)
            return;


        if (outputPort.capacity == Port.Capacity.Single && outputPort.connections.Count() >= 1)
        {
            foreach (var edge2 in outputPort.connections.ToList())
            {
                RemoveEdge(edge2);
            }
        }
        if (inputPort.capacity == Port.Capacity.Single && inputPort.connections.Count() >= 1)
        {
            foreach (var edge2 in inputPort.connections.ToList())
            {
                RemoveEdge(edge2);
            }
        }

        // 创建一个新的Edge
        GraphEdge edge = new GraphEdge
        {
            output = outputPort,
            input = inputPort
        };

        // 将Edge连接到两个Port
        edge.input.Connect(edge);
        edge.output.Connect(edge);

        (outputPort.node as NodeViewBase).OnPortConnect(outputPort, inputPort, false);
        (inputPort.node as NodeViewBase).OnPortConnect(inputPort, outputPort, true);

        // 将Edge添加到GraphView
        ActionGraphEditorWindow.flowChart.AddElement(edge);
    }

    private void GetConnectedPort(List<NodeViewBase> inList, NodeViewBase target, out List<(int, int, NodeViewBase)> inputConnect, out List<(int, int, NodeViewBase)> outputConnect)
    {
        inputConnect = new List<(int, int, NodeViewBase)>();
        outputConnect = new List<(int, int, NodeViewBase)>();
        foreach (var node in inList)
        {
            int idx = 0;
            foreach (var v in node.inputContainer.Children ())
            {
                if (v is Port port)
                {
                    if (port != null)
                    {
                        foreach (var edge in port.connections)
                        {
                            if (edge != null && edge.output != null && edge.output.node == target)
                            {
                                int idx2 = 0;
                                var children = edge.output.node.outputContainer.Children().ToList();
                                foreach (var v2 in children)
                                {
                                    if (v2 is Port port2 && port2 == edge.output)
                                    {
                                        inputConnect.Add((idx, idx2, node));
                                    }

                                    idx2++;
                                }

                            }
                        }
                    }
                }
                idx++;
            }
            //
            // idx = 0;
            // foreach (var v in node.outputContainer.Children ())
            // {
            //     if (v is Port port)
            //     {
            //         if (port != null)
            //         {
            //             foreach (var edge in port.connections)
            //             {
            //                 if (edge != null && edge.input != null && edge.input.node == target)
            //                 {
            //                     int idx2 = 0;
            //                     var children = edge.input.node.inputContainer.Children();
            //                     foreach (var v2 in children)
            //                     {
            //                         if (v2 is Port port2 && port2 == edge.input)
            //                         {
            //                             outputConnect.Add((idx, idx2, node));
            //                         }
            //
            //                         idx2++;
            //                     }
            //                 }
            //             }
            //         }
            //     }
            //     idx++;
            // }
        }
    }

    private object CloneNodeData(object data)
    {
        if (data == null)
        {
            return null;
        }

        if (data is NodeBase nodeBase)
        {
            return nodeBase.Clone();
        }

        if (data is VariableBase variableBase)
        {
            return variableBase.Clone();
        }

        return null;
    }

    private void DeleteNode(NodeViewBase nodeView)
    {
        if (nodeView is ActionNodeView actionNodeView && actionNodeView.Data is RootNode)
        {
            ActionGraphLog.Info("根节点不能删除");
            return;
        }

        // List<NodeViewBase> toDeleteList = new();
        // //向右删除Action节点
        // if (nodeView is ActionNodeView actionNodeView2)
        // {
        //     foreach (var v in nodeView.outputContainer.Children())
        //     {
        //         if (v is Port p)
        //         {
        //             var edgeArray = p.connections.ToArray();
        //             foreach (var edge in edgeArray)
        //             {
        //                 if (edge.input.node != null && edge.input.node is ActionNodeView childAction)
        //                 {
        //                     toDeleteList.Add(childAction);
        //                 }
        //             }
        //         }
        //     }
        // }
        //
        // //向左删除Variable节点
        // //todo:需要检测是否需要删除
        // foreach (var v in nodeView.inputContainer.Children())
        // {
        //     if (v is Port p)
        //     {
        //         var edgeArray = p.connections.ToArray();
        //         foreach (var edge in edgeArray)
        //         {
        //             if (edge.input.node != null && edge.output.node is VariableNodeView childAction)
        //             {
        //                 toDeleteList.Add(childAction);
        //             }
        //         }
        //     }
        // }
        //
        // // if (nodeView is ActionNodeView actionNodeView)
        // // {
        // //     var data = actionNodeView.data;
        // //     if (data != null)
        // //     {
        // //         if (data.parent != null && data.parent.children != null)
        // //         {
        // //             data.parent.children.Remove(data);
        // //         }
        // //
        // //         if (data.children != null)
        // //         {
        // //             foreach (var child in data.children)
        // //             {
        // //                 if (child.parent == data)
        // //                 {
        // //                     child.parent = null;
        // //                 }
        // //             }
        // //         }
        // //     }
        // // }
        // //
        // // if (nodeView is VariableNodeView varNodeView)
        // // {
        // //
        // // }
        //
        foreach (var v in nodeView.inputContainer.Children())
        {
            if (v is Port p)
            {
                foreach (var edge in p.connections.ToArray())
                {
                    RemoveEdge(edge);
                }
            }
        }

        foreach (var v in nodeView.outputContainer.Children())
        {
            if (v is Port p)
            {
                foreach (var edge in p.connections.ToArray())
                {
                    RemoveEdge(edge);
                }
            }
        }
        //
        // for (int i = 0; i < toDeleteList.Count; i++)
        // {
        //     DeleteNode(toDeleteList[i]);
        // }
        //
        // nodeView.title = "Delete";
        // nodeView.NodePosition = Vector2.one;
        ActionGraphLog.Info($"删除节点:{nodeView.DebugName}");
        RemoveElement(nodeView);
    }

    #endregion


    #region Debug

    public void StartDebug()
    {
        // GraphDebugUtils
        // NodeBase.debugGraphName = _fileName;
        GraphDebugUtils.subDataToEditor = OnGetDebugData;
    }

    private void OnGetDebugData()
    {
        // foreach (var node in nodes)
        // {
        //     if (node is ActionNodeView nodeView)
        //     {
        //         if (nodeView.Data != null && GraphDebugUtils.actionDebugState != null)
        //         {
        //             E_DebugExecuteState state = E_DebugExecuteState.None;
        //             if (!string.IsNullOrEmpty(nodeView.Data.nodeGuid) && GraphDebugUtils.actionDebugState.TryGetValue(nodeView.Data.nodeGuid, out state))
        //             {
        //                 nodeView.SetDebugState(state);
        //             }
        //             else
        //             {
        //                 nodeView.SetDebugState(state);
        //             }
        //         }
        //         else
        //         {
        //             nodeView.SetDebugState(E_DebugExecuteState.None);
        //         }
        //     }
        // }
    }

    #endregion


    #region private_function

    private Vector2 MousePos2CanvasPos(Vector2 screenPos)
    {
        return this.ChangeCoordinatesTo(this.contentViewContainer, screenPos);
    }

    public bool OnMenuSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        // var type = searchTreeEntry.userData as Type;
        //
        // var windowRoot = window.rootVisualElement;
        // var windowMousePosition =
        //     windowRoot.ChangeCoordinatesTo(windowRoot.parent,
        //         context.screenMousePosition - window.position.position);
        // var graphMousePosition = contentViewContainer.WorldToLocal(windowMousePosition);
        // CreateNode(graphMousePosition);

        Debug.LogError("OnMenuSelectEntry");
        return true;
    }

    private void RecursiveSortChildren(NodeBase node)
    {
        if (node.children != null)
        {
            node.children = node.children.OrderBy(x => _nodeMap[x].GetPosition().position.y).ToList();
            foreach (var child in node.children)
            {
                RecursiveSortChildren(child);
            }
        }
    }

    private void CreateNodeRecersive(NodeBase node, NodeViewBase parentView)
    {
        if (node == null)
        {
            return;
        }

        var selfView = CreateActionNode(node, default, E_CreateVariableType.Find);
        var rect = selfView.GetPosition();
        rect.position = node.editorPos;
        selfView.SetPosition(rect);
        //恢复节点
        if (parentView != null)
        {
            var edge = new GraphEdge()
            {
                output = parentView.outputContainer[0] as Port,
                input = selfView.inputContainer[0] as Port,
            };
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            AddElement(edge);
        }

        if (node.children != null)
        {
            foreach (var v in node.children)
            {
                CreateNodeRecersive(v, selfView);
            }
        }
    }

    public List<NodeViewBase> GetAllNodeViews()
    {
        return nodes.OfType<NodeViewBase>().ToList();
    }

    private List<NodeViewBase> GetAvailableSelectedNodeViews()
    {
        var list = new List<NodeViewBase>();
        foreach (var v in GetSelectedNodeViews())
        {
            if (v is ActionNodeView actionNodeView && (actionNodeView.Data is RootNode ||
                actionNodeView.Data is OutputActionBase))
            {
                continue;
            }
            list.Add(v);
        }

        return list;
    }

    private List<NodeViewBase> GetSelectedNodeViews()
    {
        List<NodeViewBase> selectedNodeViews = new List<NodeViewBase>();
        foreach (GraphElement element in selection)
        {
            if (element is NodeViewBase nodeView)
            {
                selectedNodeViews.Add(nodeView);
            }
        }

        return selectedNodeViews;
    }

    private Vector2 _mousePos;

    private void SetNewGraph(ActionGraphBase newGraph)
    {
        // Clear();
        ClearGraph();
        _graph = newGraph;
        _graph.EditorInit();
        ActionGraphEditorWindow.inspector.LateInit(newGraph);
        ActionDataForEditor.curGraph = _graph;
        RefreshViewByGraph(_graph);
        // Debug.LogError(_graph.GetHashCode());
        // _graph = new ActionGraph_Buff(_graph.graphName);
        // StartDebug();
    }

    private void ClearGraph()
    {
        foreach (var node in nodes)
        {
            if (node is NodeViewBase nodeView)
            {
                RemoveElement(node);
            }
        }

        foreach (var edge in edges)
        {
            RemoveElement(edge);
        }
    }

    private void RefreshViewByGraph(ActionGraphBase graph)
    {
        CreateNodeRecersive(graph.root, null);
        foreach (var v in graph.unconnectedNodes)
        {
            CreateNodeRecersive(v, null);
        }

        CreateVairables(graph.variableList);

        // foreach (var v in graph.unconnectedNodes)
        // {
        //     CreateNodeRecersive(v, null);
        // }

        foreach (var node in nodes)
        {
            if (node is NodeViewBase nodeView)
            {
                nodeView.GenerateInputPort();
            }
        }

        foreach (var node in nodes)
        {
            if (node is NodeViewBase nodeView)
            {
                nodeView.ConnectPort();
            }
        }

        foreach (var node in nodes)
        {
            if (node is NodeViewBase nodeView)
            {
                nodeView.AfterLoad();
            }
        }

        Debug.Log(graph.root.ToString(0));

        int num = 0;
        foreach (var node in nodes)
        {
            if (node is NodeViewBase nodeView)
            {
                num++;
            }
        }

        Debug.Log($"Read:{num}");

        // var callbackNodes = _graph.callbackRoots;
        // if (callbackNodes != null)
        // {
        //     var pos = new Vector2(0, 0);
        //     foreach (var v in callbackNodes)
        //     {
        //         pos.y -= 200;
        //         v.Value.editorPos = pos;
        //         CreateNodeRecersive(v.Value, null);
        //     }
        // }

        var graphTypeBtn = ActionGraphEditorWindow.Instance.rootVisualElement.Q<Button>("GraphTypeBtn");
        graphTypeBtn.text = _graph.GetType().GetCustomAttribute<GraphElementInfoAttribute>().Text + "(切换图表)";
    }

    private void CreateVairables(List<VariableBase> list)
    {
        foreach (var v in list)
        {
            var nodeView = (VariableNodeView)CreateVariableNode(v, default, E_CreateVariableType.Find);
        }

    }

    #endregion
}