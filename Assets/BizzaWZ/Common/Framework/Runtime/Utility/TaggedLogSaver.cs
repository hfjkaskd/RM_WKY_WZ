using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Sirenix.OdinInspector;

public class TaggedLogSaver : MonoBehaviour
{
    [Title("日志筛选")]
    [LabelText("需要保存的 Tag")]
    [InfoBox("只有包含这些 Tag 的日志才会被缓存，例如：[GAME]、[UI]、[LOAD]")]
    public List<string> targetTags = new List<string> { "[NET]" };

    [LabelText("是否保存 Error/Exception 堆栈")]
    public bool saveStackTrace = true;

    [LabelText("是否要求日志以 Tag 开头")]
    public bool mustStartWithTag = false;

    [Title("保存配置")]
    [LabelText("保存目录")]
    [InfoBox("为空时默认保存到 Application.persistentDataPath/TaggedLogs")]
    public string customDirectory = "";

    [LabelText("文件名前缀")]
    public string filePrefix = "TaggedLog";

    [LabelText("保存时是否按日期分文件")]
    public bool useDailyFile = true;

    [LabelText("保存后是否清空缓存")]
    public bool clearCacheAfterSave = false;

    [Title("运行时状态")]
    [ShowInInspector, ReadOnly, LabelText("当前缓存日志数")]
    private int CachedCount => mCachedLogs.Count;

    [ShowInInspector, ReadOnly, LabelText("当前保存目录")]
    private string CurrentDirectory => GetSaveDirectory();

    [ShowInInspector, ReadOnly, LabelText("当前文件路径")]
    private string CurrentFilePath => GetCurrentFilePath();

    private readonly List<string> mCachedLogs = new List<string>(256);

    private void Awake()
    {
#if UNITY_EDITOR
        Application.logMessageReceivedThreaded += OnLogReceived;
        DontDestroyOnLoad(gameObject);
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        Application.logMessageReceivedThreaded -= OnLogReceived;
#endif
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        Application.logMessageReceivedThreaded -= OnLogReceived;
#endif
    }

    private void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        if (!IsMatchTag(condition))
        {
            return;
        }

        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logLine = $"[{time}] [{type}] {condition}";

        if (saveStackTrace &&
            !string.IsNullOrEmpty(stackTrace) &&
            (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            logLine += Environment.NewLine + stackTrace;
        }

        lock (mCachedLogs)
        {
            mCachedLogs.Add(logLine);
        }
    }

    private bool IsMatchTag(string condition)
    {
        if (string.IsNullOrEmpty(condition))
        {
            return false;
        }

        if (targetTags == null || targetTags.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < targetTags.Count; i++)
        {
            string tag = targetTags[i];
            if (string.IsNullOrEmpty(tag))
            {
                continue;
            }

            if (mustStartWithTag)
            {
                if (condition.StartsWith(tag, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            else
            {
                if (condition.Contains(tag))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetSaveDirectory()
    {
        if (!string.IsNullOrEmpty(customDirectory))
        {
            return customDirectory;
        }

        return Path.Combine(Application.persistentDataPath, "TaggedLogs");
    }

    private string GetCurrentFilePath()
    {
        string directory = GetSaveDirectory();

        string fileName = useDailyFile
            ? $"{filePrefix}_{DateTime.Now:yyyyMMdd}.log"
            : $"{filePrefix}.log";

        return Path.Combine(directory, fileName);
    }

    [Button("手动保存日志", ButtonSizes.Large)]
    [GUIColor(0.3f, 0.9f, 0.4f)]
    public void SaveLogsToFile()
    {
        List<string> snapshot;

        lock (mCachedLogs)
        {
            if (mCachedLogs.Count == 0)
            {
                Debug.Log("[TaggedLogSaver] 当前没有可保存的日志。");
                return;
            }

            snapshot = new List<string>(mCachedLogs);
            if (clearCacheAfterSave)
            {
                mCachedLogs.Clear();
            }
        }

        string directory = GetSaveDirectory();
        string filePath = GetCurrentFilePath();

        try
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder sb = new StringBuilder(snapshot.Count * 64);
            sb.AppendLine($"========== Save Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            sb.AppendLine($"Count: {snapshot.Count}");
            sb.AppendLine();

            for (int i = 0; i < snapshot.Count; i++)
            {
                sb.AppendLine(snapshot[i]);
            }

            File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[TaggedLogSaver] 日志已保存到: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TaggedLogSaver] 保存日志失败: {e}");
        }
    }

    [Button("清空缓存日志")]
    [GUIColor(1f, 0.5f, 0.2f)]
    public void ClearCachedLogs()
    {
        lock (mCachedLogs)
        {
            mCachedLogs.Clear();
        }

        Debug.Log("[TaggedLogSaver] 已清空缓存日志。");
    }

    [Button("输出当前保存路径")]
    public void PrintSavePath()
    {
        Debug.Log($"[TaggedLogSaver] 保存目录: {GetSaveDirectory()}");
        Debug.Log($"[TaggedLogSaver] 文件路径: {GetCurrentFilePath()}");
    }

    [Button("写入一条测试日志")]
    public void WriteTestLog()
    {
        if (targetTags == null || targetTags.Count == 0 || string.IsNullOrEmpty(targetTags[0]))
        {
            Debug.Log("[TaggedLogSaver] 请先配置 targetTags。");
            return;
        }

        Debug.Log($"{targetTags[0]} 这是一条测试日志，时间：{DateTime.Now:HH:mm:ss}");
    }
}
