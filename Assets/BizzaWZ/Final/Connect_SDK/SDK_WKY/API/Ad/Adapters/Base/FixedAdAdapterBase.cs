#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// 固定广告的基类：如banner，native
/// </summary>
namespace Bizza.Sdk
{
     
    [Preserve]
    public abstract class FixedAdAdapterBase : AllAdAdapterBase
    {
        /// <summary>
        /// 是否正在展示
        /// </summary>
        public abstract bool IsShowing { get; }

        /// <summary>
        /// 显示
        /// </summary>
        public abstract void Show();

        /// <summary>
        /// 隐藏
        /// </summary>
        public abstract void Hide();
    }

}
#endif
