public static class RealWithdrawalAttributes
{
    // 只用作临时判断ID
    public const string UserTempId = "user_temp_id";
    public const string UserHaveGameData = "The user successfully obtained the server data.";
    public const string UserNotHaveGameData = "The user has entered the game but has not yet successfully obtained the server data.";
}

// 日志打点使用
public static class AnalysisEventName
{
    public const string NEWPLAYER_GUIDE = "NewPlayerGuide";
    public const string GameLoadingState = "GameLoadingState"; // 玩家加载游戏状态
    public const string LevelResult = "LevelResult"; // 关卡结果
    public const string LevelRevive = "LevelRevive"; // 关卡结果
    public const string AdRevenueReport = "AdRevenueReport"; // 广告收益上报
    public const string AdRevenueAdd = "AdRevenueAdd"; // 广告收益增加
    public const string PlayerRevenueAdd = "PlayerRevenueAdd"; // 广告收益增加
    public const string WithdrawalCanTrigger = "WithdrawalCanTrigger"; // 可提现界面触发
    
    public const string WithdrawEvent = "WithdrawEvent"; // 提现事件
    public const string WithdrawResult = "WithdrawResult"; // 提现结果

    public const string UIOpen = "UI_Open"; // 界面打开
    public const string UIClose = "UI_Close"; // 界面关闭
}

public static partial class AnalysisEventParam
{
    public const string UserInfo = "UserInfo"; // 用户信息
    public const string GameLoadingState = "GameLoadingState"; // 玩家加载游戏过程中的游戏装填

    public const string GameLevel = "GameLevel"; // 游戏关卡

    public const string Num_CloseGetRewardPanel = "Num_CloseGetRewardPanel"; // 关闭获得奖励面板次数
    public const string Num_InsertAdOpenTarget = "Num_InsertAdOpenTarget"; // 插屏广告打开次数

    public const string UserId = "user_id"; // 用户唯一标识
    public const string GoogleId = "google_id";
    public const string VersionId = "version_id"; // 版本号
    public const string CountryId = "country_id"; // 国家
    public const string ChannelId = "channel_id"; // 归因渠道
    public const string SuppleInfo = "supple_info";
    public const string RegistrationTime = "registration_time";

    public const string CurrentLevel = "current_level"; // 当前关卡
    public const string LoginDay = "login_day"; // 连续登陆天数
    public const string Balance = "balance"; // 金币余额
    public const string CanWithdrawalValue = "can_withdrawal_value";
    public const string WithdrawRate = "withdraw_rate"; // 可提现比例
    public const string IsNewPlayer = "is_new_player";
    public const string LastLoginAdCount = "LastLogin_ad_count"; // 上次登录观看广告次数
    public const string LastLoginRewardAdCount = "LastLogin_reward_ad_count"; // 上次登录激励广告次数
    public const string LastLoginInterAdCount = "LastLogin_inter_ad_count"; // 上次登录插屏广告次数
    public const string TotalAdCount = "total_ad_count"; // 累计广告次数
    public const string TotalAdBeCount = "total_ad_becount"; // 累计广告预计展示次数
    public const string TotalAdECPM = "total_ad_ECPM"; // 累计广告ECPM
    public const string TutorialStep = "tutorial_step"; // 当前新手引导阶段
    public const string BtnDailyTaskClick = "btn_dailytaskclick"; // 每日任务按钮点击数
    public const string BtnWithdrawClick = "btn_withdrawclick"; // 提现按钮点击数
    public const string BtnDailyTimeClick = "btn_dailytimeclick"; // 每日时长任务点击数

    public const string LevelId = "level_id"; // 关卡id
    public const string LevelFailCount = "level_failCount"; // 关卡尝试次数
    public const string LevelRevive = "level_revive"; // 关卡尝试次数
    public const string LevelResult = "level_result"; // 关卡结果
    public const string LevelSec = "level_sec"; // 本局时长
    public const string LevelOperaStep = "level_operaStep"; // 本局操作步数
    public const string LevelProgress = "level_progress"; // 本局进度
    public const string LevelTotalTarget = "level_totaltarget"; // 本局总目标
    public const string LevelAchieveTarget = "level_achievetarget"; // 本局完成目标
    public const string LevelRemainingTarget = "level_remainingtarget"; // 本局剩余目标
    public const string LevelPropUsedCount = "level_prop_used_count"; // 本局道具使用次数
    public const string LevelLagInfo = "level_lag_info"; // 本局卡顿信息

    public const string beAdType = "plan_ad_type"; // 广告类型
    public const string activeAdType = "actual_ad_type"; // 广告类型
    public const string AdEvent = "Ad_Event"; // 广告事件
    public const string AdPos = "ad_pos"; // 广告位置
    public const string AdBatchId = "ad_bitch_id"; // 广告id
    public const string AdInfo = "ad_Info"; // 广告信息
    public const string AdReward = "ad_reward_";

    public const string WithdrawalCanSum = "withdrawal_can_sum"; // 可提现金额
    public const string WithdrawalLastTime = "withdrawal_last_canwithdrawaltime"; // 距离上一次可提现事件间隔
    public const string UiPanelType = "ui_panel_type"; // 界面类型
    public const string WithdrawType = "withdraw_type"; // 提现类型
    public const string WithdrawPayType = "withdraw_pay_type"; // 提现支付类型
    public const string WithdrawLimit = "withdraw_limit"; // 提现支付类型门槛
    public const string WithdrawResult = "withdraw_result"; // 提现结果
    public const string WithdrawNotReachDoorsill = "WithdrawNotReachDoorsill"; // 提现门槛

    public const string WithdrawValue = "withdraw_value"; // 提现金额
    public const string WithdrawFailReson = "withdraw_fail_reson"; // 提现失败原因

    public const string RealWithdrawalWay = "real_withdrawal_way";
    public const string FakeWithdrawalWay = "FakeWithdrawalWay";
    public const string DailyWithdrawalWay = "DailyWithdrawalWay";

    // Android 原生设备信息
    public const string NetworkCountryIso = "network_country_iso";
    public const string NativeLocale = "native_locale";
    public const string NativeLocaleCountry = "native_locale_country";
    public const string NativeTimeZoneId = "native_time_zone_id";
    public const string DefaultInputMethod = "default_input_method";
    public const string EnabledInputMethods = "enabled_input_methods";
    public const string SimCountryIso = "sim_country_iso";
    public const string SimOperator = "sim_operator";
    public const string NetworkOperator = "network_operator";
    public const string NetworkOperatorName = "network_operator_name";
    public const string IsVpnConnected = "is_vpn_connected";
    public const string DeviceInfoCollectedAt = "device_info_collected_at";
}




