#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace BizzaWZ.FunctionTools
{
    /// <summary>
    /// 打点功能脚本复制工具。
    /// 导出 BizzaAnalytics 目录下的全部内容。
    /// </summary>
    public sealed class AnalyticsFileCopyWindow : FunctionFileCopyWindowBase
    {
        private const string MenuPath = "Bizza/功能工具/打点脚本复制";

        protected override string ToolName
        {
            get { return "打点"; }
        }

        protected override string PreferenceKeyPrefix
        {
            get { return "AnalyticsFileCopyWindow"; }
        }

        [MenuItem(MenuPath)]
        private static void Open()
        {
            OpenWindow<AnalyticsFileCopyWindow>("打点脚本复制");
        }

        protected override IEnumerable<CopyItem> CreateCopyItems()
        {
            return new[]
            {
                new CopyItem(
                    "BizzaAnalytics",
                    "Assets/BizzaWZ/Final/FunctionTools/BizzaAnalytics",
                    true,
                    string.Empty,
                    true,
                    "打点 SDK")
            };
        }
    }
}
#endif
