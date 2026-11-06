#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace BizzaWZ.FunctionTools
{
    /// <summary>
    /// IP 检测工具导出窗口。功能清单和公共依赖由
    /// IPBlockFileManifest.json 及其 includes 维护。
    /// </summary>
    public sealed class IPBlockFileCopyWindow : FunctionFileCopyWindowBase
    {
        private const string ManifestName = "IPBlockFileManifest";

        protected override string ToolName => "IP 检测";
        protected override string PreferenceKeyPrefix => "IPBlockFileCopyWindow";
        protected override bool SupportsBackup => false;

        [MenuItem("Bizza/功能工具/IP拦截脚本复制")]
        [MenuItem("Bizza/功能工具/IP检测工具复制")]
        private static void Open()
        {
            OpenWindow<IPBlockFileCopyWindow>("IP 检测工具复制");
        }

        protected override IEnumerable<CopyItem> CreateCopyItems()
        {
            return LoadManifestItems(ManifestName);
        }
    }
}
#endif
