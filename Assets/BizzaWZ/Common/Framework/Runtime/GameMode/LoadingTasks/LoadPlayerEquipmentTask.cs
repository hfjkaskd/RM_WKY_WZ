using Bizza.Loading;
using Cysharp.Threading.Tasks;

#if BIZZA_REAL_WITHDRAW
public class LoadPlayerEquipmentTask : LoadingTaskBase
{
    public override LoadingTaskName TaskName => LoadingTaskName.LoadPlayerEquipment;

    public override float Weight => 0.1f;

    public override LoadingTaskName[] Dependencies => new[] { LoadingTaskName.Boots };

    public override UniTask Execute()
    {
        LogLogger.LogInfo($"加载任务：{TaskName} start");
        SetProgress(0.1f);

        // 设备信息已由 Boots_Flow 统一初始化，这里只读取同一份完整数据，不重复调用原生接口。
        DeviceInfoUtil.DeviceInfoData deviceInfo = DeviceInfoUtil.Data;

        SetProgress(1f);
        LogLogger.LogInfo(
            $"加载任务：{TaskName} end, locale={deviceInfo.NativeLocale}, " +
            $"country={deviceInfo.NativeLocaleCountry}, vpn={deviceInfo.IsVpnConnected}");

        return UniTask.CompletedTask;
    }
}
#endif
