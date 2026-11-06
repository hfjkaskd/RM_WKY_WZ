#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class FontToolWindow : EditorWindow
{
    public enum E_SetType
    {
        设置字体,
        替换字体,
    }
    private TMP_FontAsset _selectedFont;       // 用户选择的字体
    private TMP_FontAsset _sourceFont;       // 用户选择的字体
    private TMP_FontAsset _targetFont;       // 用户选择的字体

    private Material _selectedMaterial;
    private Material _sourceMaterial;       // 用户选择的字体
    private Material _targetMaterial;       // 用户选择的字体

    private E_SetType _setType;

    // 菜单项：在顶部菜单添加字体工具
    [MenuItem("工具/UI/字体工具")]
    public static void ShowWindow()
    {
        GetWindow<FontToolWindow>("字体工具");
    }

    void OnGUI()
    {
        GUILayout.Label("设置方式：");
        _setType = (E_SetType)EditorGUILayout.EnumPopup(_setType);
        EditorGUILayout.Space();

        if (_setType == E_SetType.设置字体)
        {
            GUILayout.Label("选择字体：");
            _selectedFont = (TMP_FontAsset) EditorGUILayout.ObjectField(
                "",
                _selectedFont,
                typeof(TMP_FontAsset),
                false // 不允许选择场景对象（仅资源）
            );

            GUILayout.Label("选择字体预设，不选默认第一个");
            _selectedMaterial = (Material) EditorGUILayout.ObjectField(
                "",
                _selectedMaterial,
                typeof(Material),
                false // 不允许选择场景对象（仅资源）
            );

            GUILayout.Label("目标路径：(手动选Project文件/文件夹夹即可，支持多选)");
            GUILayout.Label($"已选：{Selection.assetGUIDs.Length}");

            if (GUILayout.Button("设置字体"))
            {
                var ret = EditorUtils.GetSelectedObjects<GameObject>("Prefab");
                if (ret != null && ret.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("Processing...", "Start Process", 0);
                    int progress = 0;
                    foreach (var v in ret)
                    {
                        progress++;
                        EditorUtility.DisplayProgressBar("Processing....", "", ((float) progress / ret.Count));
                        bool isChange = false;
                        TMP_Text[] btnList = v.GetComponentsInChildren<TMP_Text>(true);
                        foreach (var item in btnList)
                        {
                            if (_selectedMaterial != null)
                            {
                                if (item.font == null || !item.font != _selectedFont || item.font.material != _selectedMaterial)
                                {
                                    isChange = true;
                                    item.font = _selectedFont;
                                    item.fontSharedMaterial = _selectedMaterial;
                                }
                            }
                            else
                            {
                                if (item.font == null || !item.font != _selectedFont)
                                {
                                    isChange = true;
                                    item.font = _selectedFont;
                                }
                            }
                        }

                        if (isChange)
                        {
                            EditorUtility.SetDirty(v);
                            Debug.Log("修改:" + v.name);
                        }
                    }
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }
        }
        else
        {
            GUILayout.Label("源字体：");
            _sourceFont = (TMP_FontAsset) EditorGUILayout.ObjectField(
                "",
                _sourceFont,
                typeof(TMP_FontAsset),
                false // 不允许选择场景对象（仅资源）
            );

            GUILayout.Label("源字体预设：");
            _sourceMaterial = (Material) EditorGUILayout.ObjectField(
                "",
                _sourceMaterial,
                typeof(Material),
                false // 不允许选择场景对象（仅资源）
            );

            GUILayout.Label("目标字体：");
            _targetFont = (TMP_FontAsset) EditorGUILayout.ObjectField(
                "",
                _targetFont,
                typeof(TMP_FontAsset),
                false // 不允许选择场景对象（仅资源）
            );

            GUILayout.Label("目标字体预设：");
            _targetMaterial = (Material) EditorGUILayout.ObjectField(
                "",
                _targetMaterial,
                typeof(Material),
                false // 不允许选择场景对象（仅资源）
            );

            GUILayout.Label("目标路径：(手动选Project文件/文件夹夹即可，支持多选)");
            GUILayout.Label($"已选：{Selection.assetGUIDs.Length}");

            if (GUILayout.Button("替换字体"))
            {
                var ret = EditorUtils.GetSelectedObjects<GameObject>("Prefab");
                if (ret != null && ret.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("Processing...", "Start Process", 0);
                    int progress = 0;
                    foreach (var v in ret)
                    {
                        progress++;
                        EditorUtility.DisplayProgressBar("Processing....", "", ((float) progress / ret.Count));
                        bool isChange = false;
                        TMP_Text[] tmpList = v.GetComponentsInChildren<TMP_Text>(true);
                        foreach (var item in tmpList)
                        {
                            if (_sourceMaterial == null)
                            {
                                if ((_sourceFont == null && item.font == null) || (item.font == _sourceFont))
                                {
                                    isChange = true;
                                    item.font = _targetFont;
                                    if (_targetMaterial != null)
                                    {
                                        item.fontSharedMaterial = _targetMaterial;
                                    }
                                }
                            }
                            else
                            {
                                if (item.font == _sourceFont && item.fontSharedMaterial == _sourceMaterial)
                                {
                                    isChange = true;
                                    item.font = _targetFont;
                                    item.fontSharedMaterial = _targetMaterial;
                                }
                            }
                        }

                        if (isChange)
                        {
                            EditorUtility.SetDirty(v);
                            Debug.Log("修改:" + v.name);
                        }
                    }
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
