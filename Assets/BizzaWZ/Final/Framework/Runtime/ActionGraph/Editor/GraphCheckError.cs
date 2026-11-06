#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// Graph中一些代码错误检查
/// </summary>
[InitializeOnLoad]
public class GraphCheckError
{
    // 静态构造函数，在Unity启动时自动执行
    static GraphCheckError()
    {
        // 订阅编译结束事件
        CompilationPipeline.compilationStarted += OnCompilationStart;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }

    private static void OnCompilationStart(object obj)
    {
        // EditorPrefs.SetString("Editor_CurGraph", ActionGraphEditorWindow.Instance != null ? ActionGraphEditorWindow.flowChart.CurGraphFilePath : "");
    }

    // 编译结束时调用的方法
    private static void OnCompilationFinished(object obj)
    {
        // var curGraph = EditorPrefs.GetString("Editor_CurGraph", "");
        // if (!string.IsNullOrEmpty(curGraph))
        // {
        //     GraphDebugUtils.OpenGraph(curGraph);
        // }

        List<Type> toCheckTypes = new List<Type>()
        {
            typeof(NodeBase),
            typeof(VariableBase),
        };

        foreach (var checkType in toCheckTypes)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom(checkType))
            {
                if (checkType.IsAssignableFrom(type) && !type.IsAbstract && type != checkType)
                {
                    //检查clone函数
                    {
                        try
                        {
                            var inst = Activator.CreateInstance(type) as GraphElementBase;
                            var instClone = inst.Clone();
                            if (instClone == null || instClone.GetType() != type)
                            {
                                ActionGraphLog.Error($"clone函数错误:{type.Name}");
                            }
                        }
                        catch (Exception e)
                        {
                            ActionGraphLog.Error($"clone函数错误:{type.Name}");
                        }
                    }

                    //检查标签
                    {
                        var attribute = type.GetCustomAttribute<GraphElementInfoAttribute>();
                        if (attribute == null)
                        {
                            ActionGraphLog.Error($"【{type.Name}】没有添加【{nameof(GraphElementInfoAttribute)}】属性，图表中将无法显示");
                        }
                        else if (attribute.SupportTypes == null)
                        {
                            ActionGraphLog.Error($"【{type.Name}】没有配置【{nameof(attribute.SupportTypes)}】字段，图表中将无法显示");
                        }
                    }
                }
            }
        }
    }
}
#endif