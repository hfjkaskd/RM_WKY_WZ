#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;

namespace Bizza.Sdk
{
    /// <summary>
    /// 上报/分析 SDK 的基类，定义通用接口。
    /// 业务侧用法示例：
    /// AddCommonParams() / AddLevelCommonParams(levelId) 后 SendCustomEvent("c_begin_game");
    /// SetUserProperty("site_id", value); 后 UploadUserProperty();
    /// </summary>
    public abstract class AnalysisSdkBase
    {
        /// <summary> 使用渠道配置初始化 SDK </summary>
        public abstract void Init(ChannelConfig channelConfig);

        /// <summary> 发送自定义事件（可先通过 AddParam/AddCommonParams 等累积参数后传入） </summary>
        public abstract void SendCustomEvent(string eventName, Dictionary<string, object> @params);

        public abstract void SetUserProperty(string key, object value);

        /// <summary> 设置公共属性（后续每条事件都会自动带上） </summary>
        /// <summary> 上报用户属性（对应 AnalyticsHelper.UploadUserProperty 时调用） </summary>
        public abstract void UploadUserProperty(Dictionary<string, object> @params);
    }
}
#endif
