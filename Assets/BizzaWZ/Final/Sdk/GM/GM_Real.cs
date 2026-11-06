#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
using UnityEditor;
using UnityEngine;

public static class GM_Real
{
    [MenuItem("Tools/Bizza/Test Daily Mission %#d")]
    private static void TestDailyMission()
    {
        if (!ValidateTestDailyMission())
        {
            return;
        }

        var panel = UIModule.Instance.GetPage<DailyMissionPanel>();
        if (panel == null || !panel.isActiveAndEnabled || panel.IsClosing)
        {
            Debug.LogWarning("请先在运行模式下打开每日任务面板，再执行 Test Daily Mission。");
            return;
        }

        panel.EditorTestDailyMission();
    }

    [MenuItem("Tools/Bizza/Test Daily Mission %#d", true)]
    private static bool ValidateTestDailyMission()
    {
        return EditorApplication.isPlaying && UIModule.Instance != null;
    }
}
#endif
