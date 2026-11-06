using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GridLayout3D : MonoBehaviour
{
    [Header("网格尺寸")]
    [Min(1)] public int gridX = 3;  // X轴方向数量
    [Min(1)] public int gridY = 3;  // Y轴方向数量
    [Min(1)] public int gridZ = 3;  // Z轴方向数量

    [Header("间距设置")]
    public Vector3 spacing = new Vector3(2f, 2f, 2f);  // 各轴间距

    [Header("Gizmos设置")]
    public Color gizmoColor = Color.cyan;
    [Range(0.1f, 2f)] public float gizmoSize = 0.5f;
    public bool showGridLines = true;
    public bool showPositionPoints = true;

    [Header("布局控制")]
    [Tooltip("启用后会自动更新子物体位置")]
    public bool autoUpdate = true;

    private Vector3[] gridPositions;
    private bool isDirty = true;

    void OnValidate()
    {
        // 当Inspector值改变时标记为需要更新
        isDirty = true;

        if (autoUpdate && Application.isEditor)
        {
            UpdateGridPositions();
        }
    }

    void Update()
    {
        // 在编辑模式下检查是否需要更新
        if (isDirty && autoUpdate)
        {
            UpdateGridPositions();
            isDirty = false;
        }
    }

    /// <summary>
    /// 更新所有网格位置
    /// </summary>
    public void UpdateGridPositions()
    {
        // 计算总网格数量
        int totalCells = gridX * gridY * gridZ;
        gridPositions = new Vector3[totalCells];

        // 计算起始偏移（使网格以原点为中心）
        Vector3 startOffset = new Vector3(
            -(gridX - 1) * spacing.x * 0.5f,
            -(gridY - 1) * spacing.y * 0.5f,
            -(gridZ - 1) * spacing.z * 0.5f
        );

        // 计算每个网格位置
        int index = 0;
        for (int z = 0; z < gridZ; z++)
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    Vector3 position = new Vector3(
                        startOffset.x + x * spacing.x,
                        startOffset.y + y * spacing.y,
                        startOffset.z + z * spacing.z
                    );

                    gridPositions[index] = position;
                    index++;
                }
            }
        }

        // 更新子物体位置
        UpdateChildrenPositions();
    }

    /// <summary>
    /// 更新所有子物体的位置
    /// </summary>
    private void UpdateChildrenPositions()
    {
        if (gridPositions == null || gridPositions.Length == 0)
            return;

        int childCount = transform.childCount;
        int positionsCount = gridPositions.Length;

        for (int i = 0; i < childCount && i < positionsCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.localPosition = gridPositions[i];
        }
    }

    /// <summary>
    /// 获取指定索引的网格位置
    /// </summary>
    public Vector3 GetGridPosition(int index)
    {
        if (gridPositions == null || index < 0 || index >= gridPositions.Length)
            return Vector3.zero;

        return gridPositions[index];
    }

    /// <summary>
    /// 获取三维索引的网格位置
    /// </summary>
    public Vector3 GetGridPosition(int x, int y, int z)
    {
        if (x < 0 || x >= gridX || y < 0 || y >= gridY || z < 0 || z >= gridZ)
            return Vector3.zero;

        int index = z * (gridX * gridY) + y * gridX + x;
        return GetGridPosition(index);
    }

    /// <summary>
    /// 手动触发布局更新
    /// </summary>
    public void RefreshLayout()
    {
        isDirty = true;
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showGridLines && !showPositionPoints)
            return;

        // 保存原始颜色
        Color originalColor = Gizmos.color;

        // 设置Gizmo颜色
        Gizmos.color = gizmoColor;

        // 计算起始偏移
        Vector3 startOffset = new Vector3(
            -(gridX - 1) * spacing.x * 0.5f,
            -(gridY - 1) * spacing.y * 0.5f,
            -(gridZ - 1) * spacing.z * 0.5f
        );

        // 绘制网格线
        if (showGridLines)
        {
            DrawGridLines(startOffset);
        }

        // 绘制位置点
        if (showPositionPoints)
        {
            DrawPositionPoints(startOffset);
        }

        // 恢复原始颜色
        Gizmos.color = originalColor;
    }

    /// <summary>
    /// 绘制网格线
    /// </summary>
    private void DrawGridLines(Vector3 startOffset)
    {
        // 绘制X方向的线
        for (int y = 0; y < gridY; y++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                Vector3 lineStart = startOffset + new Vector3(0, y * spacing.y, z * spacing.z);
                Vector3 lineEnd = startOffset + new Vector3((gridX - 1) * spacing.x, y * spacing.y, z * spacing.z);
                Gizmos.DrawLine(transform.TransformPoint(lineStart), transform.TransformPoint(lineEnd));
            }
        }

        // 绘制Y方向的线
        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                Vector3 lineStart = startOffset + new Vector3(x * spacing.x, 0, z * spacing.z);
                Vector3 lineEnd = startOffset + new Vector3(x * spacing.x, (gridY - 1) * spacing.y, z * spacing.z);
                Gizmos.DrawLine(transform.TransformPoint(lineStart), transform.TransformPoint(lineEnd));
            }
        }

        // 绘制Z方向的线
        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                Vector3 lineStart = startOffset + new Vector3(x * spacing.x, y * spacing.y, 0);
                Vector3 lineEnd = startOffset + new Vector3(x * spacing.x, y * spacing.y, (gridZ - 1) * spacing.z);
                Gizmos.DrawLine(transform.TransformPoint(lineStart), transform.TransformPoint(lineEnd));
            }
        }
    }

    /// <summary>
    /// 绘制位置点
    /// </summary>
    private void DrawPositionPoints(Vector3 startOffset)
    {
        for (int z = 0; z < gridZ; z++)
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    Vector3 position = startOffset + new Vector3(
                        x * spacing.x,
                        y * spacing.y,
                        z * spacing.z
                    );

                    // 绘制球体表示位置点
                    Gizmos.DrawSphere(transform.TransformPoint(position), gizmoSize * 0.1f);

                    // 绘制坐标轴指示
                    float axisLength = gizmoSize * 0.3f;
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(
                        transform.TransformPoint(position),
                        transform.TransformPoint(position + Vector3.right * axisLength)
                    );

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(
                        transform.TransformPoint(position),
                        transform.TransformPoint(position + Vector3.up * axisLength)
                    );

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(
                        transform.TransformPoint(position),
                        transform.TransformPoint(position + Vector3.forward * axisLength)
                    );

                    Gizmos.color = gizmoColor;
                }
            }
        }
    }
    #endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(GridLayout3D))]
public class GridLayout3DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GridLayout3D gridLayout = (GridLayout3D)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("布局控制", EditorStyles.boldLabel);

        // 添加手动更新按钮
        if (GUILayout.Button("手动更新布局", GUILayout.Height(30)))
        {
            gridLayout.RefreshLayout();
            gridLayout.UpdateGridPositions();
        }

        EditorGUILayout.Space();

        // 显示网格信息
        EditorGUILayout.HelpBox(
            $"网格总数: {gridLayout.gridX * gridLayout.gridY * gridLayout.gridZ}\n" +
            $"子物体数量: {gridLayout.transform.childCount}",
            MessageType.Info
        );

        // 如果子物体数量超过网格数量，显示警告
        if (gridLayout.transform.childCount > gridLayout.gridX * gridLayout.gridY * gridLayout.gridZ)
        {
            EditorGUILayout.HelpBox(
                "子物体数量超过网格数量，部分子物体将不会被分配位置",
                MessageType.Warning
            );
        }
    }
}
#endif