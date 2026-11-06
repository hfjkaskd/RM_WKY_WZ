using System;
using System.Collections.Generic;
using System.Linq;
using Bizza;
using Bizza.GameAnalytics;
using Bizza.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 加载流程管理器
/// 负责管理和调度所有加载任务
/// </summary>
 
public class LoadingProcedure
{
    private readonly List<ILoadingTask> _tasks = new List<ILoadingTask>();
    private readonly Dictionary<LoadingTaskName, ILoadingTask> _taskMap = new Dictionary<LoadingTaskName, ILoadingTask>();
    private readonly HashSet<LoadingTaskName> _completedTasks = new HashSet<LoadingTaskName>();
    
    private bool _runStarted;
    private bool _runCompleted;

    /// <summary>
    /// 实时汇总所有任务的当前进度。任务执行期间调用 SetProgress 后，加载 UI 下一帧即可读取到。
    /// </summary>
    public float progress => CalculateProgress();

    /// <summary>
    /// 添加加载任务
    /// </summary>
    public void AddTask(ILoadingTask task)
    {
        if (task == null)
        {
            LogLogger.LogVerbose(LogTag.LOG_Load,"尝试添加空任务");
            return;
        }
        
        if (_taskMap.ContainsKey(task.TaskName))
        {
            LogLogger.LogInfo($"任务 {task.TaskName} 已存在，将被替换");
            _tasks.Remove(_taskMap[task.TaskName]);
        }
        
        _tasks.Add(task);
        _taskMap[task.TaskName] = task;
#if UNITY_EDITOR
        var deps = task.Dependencies == null || task.Dependencies.Length == 0
            ? "none"
            : string.Join(",", task.Dependencies);
        Debug.Log($"[WhiteBootstrap] LoadingProcedure.AddTask {task.TaskName} {task.GetType().Name} deps:{deps}");
#endif
    }

    /// <summary>
    /// 移除加载任务
    /// </summary>
    public void RemoveTask(LoadingTaskName taskName)
    {
        if (_taskMap.TryGetValue(taskName, out var task))
        {
            _tasks.Remove(task);
            _taskMap.Remove(taskName);
        }
    }

    /// <summary>
    /// 执行所有加载任务
    /// </summary>
    public async UniTask Run(Action onComplete = null)
    {
        _runStarted = true;
        _runCompleted = false;
        _completedTasks.Clear();
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] LoadingProcedure.Run begin taskCount:{_tasks.Count}");
#endif
        
        if (_tasks.Count == 0)
        {
            LogLogger.LogInfo("没有加载任务需要执行");
#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadingProcedure.Run no tasks");
#endif
            _runCompleted = true;
            onComplete?.Invoke();
            return;
        }

        // 验证所有依赖任务都存在
        ValidateDependencies();
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] LoadingProcedure.Run dependencies ok");
#endif

        // 根据依赖关系执行任务
        await ExecuteTasksByDependencies();

        _runCompleted = true;
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] LoadingProcedure.Run complete");
#endif
        onComplete?.Invoke();
    }

    /// <summary>
    /// 验证所有任务的依赖关系
    /// </summary>
    private void ValidateDependencies()
    {
        List<string> errors = null;
        foreach (var task in _tasks)
        {
            if (task.Dependencies != null)
            {
                foreach (var depName in task.Dependencies)
                {
                    if (!_taskMap.ContainsKey(depName))
                    {
                        errors ??= new List<string>();
                        errors.Add($"任务 {task.TaskName} 依赖的任务 {depName} 不存在");
                    }
                }
            }
        }

        if (errors != null && errors.Count > 0)
        {
            var msg = string.Join("; ", errors);
            throw new InvalidOperationException($"加载任务依赖配置错误: {msg}");
        }
    }

    /// <summary>
    /// 根据依赖关系执行任务
    /// </summary>
    private async UniTask ExecuteTasksByDependencies()
    {
        var sortedTasks = TopologicalSort(_tasks);
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] LoadingProcedure batches:{sortedTasks.Count}");
#endif
        foreach (var batch in sortedTasks)
        {
            if (batch.Count == 0)
                continue;

#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] LoadingProcedure batch begin {string.Join(",", batch.Select(task => task.TaskName))}");
#endif
            var executions = batch.Select(task => ExecuteTask(task));
            await UniTask.WhenAll(executions);
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] LoadingProcedure batch end {string.Join(",", batch.Select(task => task.TaskName))}");
#endif
        }
    }

    /// <summary>
    /// 拓扑排序：将任务按照依赖关系分组，每组内的任务可以并行执行
    /// </summary>
    private List<List<ILoadingTask>> TopologicalSort(List<ILoadingTask> tasks)
    {
        var result = new List<List<ILoadingTask>>();
        var inDegree = new Dictionary<LoadingTaskName, int>();
        var dependencyMap = new Dictionary<LoadingTaskName, HashSet<LoadingTaskName>>();
        var readyTasks = new Queue<ILoadingTask>();

        foreach (var task in tasks)
        {
            inDegree[task.TaskName] = task.Dependencies?.Length ?? 0;
            dependencyMap[task.TaskName] = new HashSet<LoadingTaskName>(task.Dependencies ?? Array.Empty<LoadingTaskName>());

            if (inDegree[task.TaskName] == 0)
            {
                readyTasks.Enqueue(task);
            }
        }

        while (readyTasks.Count > 0)
        {
            var batch = new List<ILoadingTask>();
            var batchSize = readyTasks.Count;

            for (int i = 0; i < batchSize; i++)
            {
                var task = readyTasks.Dequeue();
                batch.Add(task);
            }

            result.Add(batch);

            foreach (var completedTask in batch)
            {
                foreach (var otherTask in tasks)
                {
                    if (dependencyMap[otherTask.TaskName].Contains(completedTask.TaskName))
                    {
                        inDegree[otherTask.TaskName]--;
                        if (inDegree[otherTask.TaskName] == 0)
                        {
                            readyTasks.Enqueue(otherTask);
                        }
                    }
                }
            }
        }

        if (result.Sum(batch => batch.Count) < tasks.Count)
        {
            List<string> blockedTasks = new();
            foreach (var task in tasks)
            {
                if (inDegree.TryGetValue(task.TaskName, out var degree) && degree > 0)
                {
                    blockedTasks.Add(task.TaskName.ToString());
                }
            }

            var msg = blockedTasks.Count > 0 ? string.Join(",", blockedTasks) : "未知任务";
            throw new InvalidOperationException($"检测到循环依赖: {msg}");
        }

        return result;
    }

    /// <summary>
    /// 执行单个任务
    /// </summary>
    private async UniTask ExecuteTask(ILoadingTask task)
    {
        if (_completedTasks.Contains(task.TaskName))
        {
            return;
        }

        // 等待依赖任务完成
        if (task.Dependencies != null && task.Dependencies.Length > 0)
        {
            await UniTask.WaitUntil(() =>
            {
                return task.Dependencies.All(dep => _completedTasks.Contains(dep));
            });
        }

        string analyticsStageId = null;
        try
        {
            analyticsStageId = BizzaGameAnalytics.TrackGameLoadStageStart(task.TaskName.ToString());
            LogLogger.LogInfo($"开始执行Task:{task.TaskName}  {task.GetType().Name}");
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] LoadingTask begin {task.TaskName} {task.GetType().Name}");
#endif
            await task.Execute();
            BizzaGameAnalytics.TrackGameLoadStageComplete(analyticsStageId);
            LogLogger.LogInfo($"结束执行Task:{task.TaskName}  {task.GetType().Name}");
            _completedTasks.Add(task.TaskName);
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] LoadingTask end {task.TaskName} progress:{progress:F2}");
#endif
        }
        catch (Exception e)
        {
            BizzaGameAnalytics.TrackGameLoadStageComplete(
                analyticsStageId,
                success: false,
                failureReason: e.GetType().Name);
            LogLogger.LogVerbose(LogTag.LOG_Load,$"任务 {task.TaskName} 执行失败: {e.Message}");
#if UNITY_EDITOR
            Debug.LogError($"[WhiteBootstrap] LoadingTask error {task.TaskName} {task.GetType().Name}: {e}");
#endif
            throw;
        }
    }

    /// <summary>
    /// 计算总进度
    /// </summary>
    private float CalculateProgress()
    {
        if (_runCompleted)
        {
            return 1f;
        }

        if (!_runStarted || _tasks.Count == 0)
        {
            return 0f;
        }

        float totalWeight = 0f;
        float completedWeight = 0f;

        foreach (var task in _tasks)
        {
            totalWeight += task.Weight;
            if (_completedTasks.Contains(task.TaskName))
            {
                completedWeight += task.Weight;
            }
            else
            {
                // 如果任务正在进行，加上部分进度
                completedWeight += task.Weight * task.GetProgress();
            }
        }

        float currentProgress = totalWeight > 0
            ? completedWeight / totalWeight
            : 0f;
        return Mathf.Clamp01(currentProgress);
    }
}
