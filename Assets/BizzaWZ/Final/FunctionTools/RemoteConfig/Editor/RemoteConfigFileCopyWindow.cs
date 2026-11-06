#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace BizzaWZ.FunctionTools
{
    /// <summary>
    /// 远端配置功能脚本复制工具。
    /// 后续在 CreateCopyItems 中补充要复制的文件或目录即可。
    /// </summary>
    public sealed class RemoteConfigFileCopyWindow : FunctionFileCopyWindowBase
    {
        private const string MenuPath = "Bizza/功能工具/远端配置脚本复制";

        protected override string ToolName
        {
            get { return "远端配置"; }
        }

        protected override string PreferenceKeyPrefix
        {
            get { return "RemoteConfigFileCopyWindow"; }
        }

        [MenuItem(MenuPath)]
        private static void Open()
        {
            OpenWindow<RemoteConfigFileCopyWindow>("远端配置脚本复制");
        }

        protected override IEnumerable<CopyItem> CreateCopyItems()
        {
            // TODO: 后续确定远端配置功能的脚本后，在这里添加 CopyItem。
            return new CopyItem[0];
        }
    }
}
#endif
