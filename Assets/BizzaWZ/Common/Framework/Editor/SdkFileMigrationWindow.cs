#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

using BizzaWZ.FunctionTools;

/// <summary>
/// SDK 导出窗口。SDK 文件内容维护在 SDKFileManifest.json，公共复制逻辑由
/// FunctionFileCopyWindowBase 统一处理，框架更新后只需更新清单即可。
/// </summary>
public sealed class SdkFileMigrationWindow : FunctionFileCopyWindowBase
{
    private const string ManifestName = "SDKFileManifest";

    protected override string ToolName => "SDK";
    protected override string PreferenceKeyPrefix => "SdkFileMigrationWindow";

    [MenuItem("Bizza/功能工具/SDK 文件复制")]
    private static void Open()
    {
        OpenWindow<SdkFileMigrationWindow>("SDK 文件复制");
    }

    protected override IEnumerable<CopyItem> CreateCopyItems()
    {
        return LoadManifestItems(ManifestName);
    }
}
#endif
