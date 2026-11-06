using Cysharp.Threading.Tasks;

namespace Bizza.Loading
{
    /// <summary>
    /// 加载任务基类
    /// </summary>
    [Obfuz.ObfuzIgnore]
    public abstract class LoadingTaskBase : ILoadingTask
    {
        public abstract LoadingTaskName TaskName { get; }
        public abstract float Weight { get; }
        public abstract LoadingTaskName[] Dependencies { get; }
        
        protected float _progress = 0f;
        
        public abstract UniTask Execute();
        
        public virtual float GetProgress()
        {
            return _progress;
        }
        
        /// <summary>
        /// 设置任务进度
        /// </summary>
        protected void SetProgress(float progress)
        {
            _progress = UnityEngine.Mathf.Clamp01(progress);
        }
    }
}

