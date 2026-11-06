using Cysharp.Threading.Tasks;

namespace Bizza.Loading
{
    [Obfuz.ObfuzIgnore]
    /// <summary>
    /// 加载任务接口
    /// </summary>
    public interface ILoadingTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        LoadingTaskName TaskName { get; }
        
        /// <summary>
        /// 任务权重（用于计算进度，总和应该为1）
        /// </summary>
        float Weight { get; }
        
        /// <summary>
        /// 依赖的任务名称列表（这些任务完成后才能执行此任务）
        /// </summary>
        LoadingTaskName[] Dependencies { get; }
        
        /// <summary>
        /// 执行任务
        /// </summary>
        UniTask Execute();
        
        /// <summary>
        /// 获取任务进度（0-1）
        /// </summary>
        float GetProgress();
    }
}

