using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UIElements;


public class InspectorView : VisualElement
{
    public new class UxmlFactory : UxmlFactory<InspectorView, UxmlTraits>
    {
    }

    private PropertyTree _nodePropertyTree;
    private PropertyTree _graphTree;

    public InspectorView()
    {
    }


    private IMGUIContainer _container;

    public void LateInit(ActionGraphBase graph)
    {
        if (_container != null)
        {
            Remove(_container);
            _container = null;
        }

        _container = new IMGUIContainer(OnGUI);
        Add(_container);

        _graphTree = PropertyTree.Create(graph);
    }

    private void OnGUI()
    {
        // 整体垂直布局
        GUILayout.BeginVertical(GUILayout.Width(ActionGraphDefine.InspectorWidth));

        if (_graphTree != null)
        {
            _graphTree.Draw(false);
        }

        // 分隔符：使用空白纹理+高度控制
        GUILayout.Space(3);
        GUIStyle separatorStyle = new GUIStyle();
        separatorStyle.fixedHeight = 6;
        separatorStyle.normal.background = MakeTex(2, 2, Color.gray);
        GUILayout.Box(GUIContent.none, separatorStyle);
        GUILayout.Space(3);

        if (_nodeView != null && _nodeView.NodeData is NodeBase node)
        {
            GUILayout.Label("节点标题");
            node.titleNote = GUILayout.TextArea(node.titleNote, GUILayout.Height(50));
            GUILayout.Label("节点注释");
            node.note = GUILayout.TextArea(node.note, GUILayout.Height(50));
        }

        // 下半部分
        if (_nodeView != null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.richText = true;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
            style.fixedWidth = ActionGraphDefine.InspectorWidth;
            // GUILayout.TextArea(_nodeView.NodeData.GetTipsText(), style);
            GUILayout.Box(_nodeView.NodeData.GetTipsText(), style);
        }

        GUILayout.EndVertical();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private NodeViewBase _nodeView;
    internal void UpdateSelection(NodeViewBase nodeView)
    {
        this._nodeView = nodeView;
        // Clear();
        //Debug.Log("显示节点的Inspector面板");

        var hasSelection = nodeView != null;

        // var menuArea = ActionEditorWindow.Instance.rootVisualElement.Q<VisualElement>("MenuView");
        // menuArea.SetEnabled(!hasSelection);

        if (nodeView == null)
        {
            return;
        }

        _nodePropertyTree = null;
        if (nodeView.NodeData is NodeBase node)
        {
            _nodePropertyTree = PropertyTree.Create(node);
        }
    }
}