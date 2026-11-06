#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Bizza.Sdk
{
    /// <summary>
    /// 所有广告的基类
    /// </summary>
    [Preserve]
    public abstract class AllAdAdapterBase
    {
        public abstract E_AdType adType { get; }

        /// <summary>
        /// 广告id
        /// </summary>
        public abstract string AdUnitId { get; }
        
        /// <summary>
        /// 当前状态是否合法
        /// </summary>
        public abstract bool IsValid { get; }

        /// <summary>
        /// 广告是否准备好
        /// </summary>
        public abstract bool IsReady { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="adUnitId"></param>
        public abstract void Init(string adUnitId);

        /// <summary>
        /// 加载广告
        /// </summary>
        public abstract void Load();
    }
}
#endif
