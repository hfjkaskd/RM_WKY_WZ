#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using Bizza.Sdk;

#if !COMMONGAME
using Bizza.Loading;
using Bizza.Channel;
#endif

using Bizza.Unity.Android;
using System.Linq;


[Obfuz.ObfuzIgnore]
public class AccountModule
{
    public static string adJustAdidKey = "adJustAdidKey";
    public static string adJustGooglIdKey = "adJustGoogleIdKey";
    public static string networkKey = "network";
    public static string campaignKey = "campaign";
    public static string adgroupkKey = "adgroup";

    public static AccountModule Instance;

    public AccountModule()
    {
#if !COMMONGAME
        AutoPopupQueue.Shared.Clear();
#endif
        Instance = this;
    }

    public void Login()
    {
        var cfg = ChannelConfig.Instance;
        LogLogger.LOGGameStart($"静态配置 :: " +
                               $"是否为发布阶段 {ChannelConfig.IsRelese_Mode} " +
                               $"GM开启 {cfg.GM}; " +
                               $"ECPM1000 {cfg.real_CustomConfig.testECPM1000}; " +
                               $"测试设备 {cfg.real_CustomConfig.openTestDevice}; " +
                               $"国家类型 {cfg.real_CustomConfig.CountryName} 读取国家开启 {string.Equals(cfg.real_CustomConfig.CountryName, AccountModule.E_CountryType.None)}; " +
                               $"本地国家类型 {cfg.real_CustomConfig.DefaultCountryName} "
                               );

        OnLoginData().Forget();
    }

    #region 登录时加载玩家数据

    public static string m_userIdKey = "userId"; // 用来获取 userId 的 key


    public static string m_Os_RtiKey = "Os_Rti";
    public static string m_Os_AruKey = "Os_Aru";
    public static string m_Os_AcmKey = "Os_Acm";
    public static string m_Os_AiuKey = "Os_Aiu";

    public static double m_Temp_ACM
    {
        get
        {
            return PlayerPrefs.GetFloat("m_Temp_ACM", 0);
        }
        set
        {
            PlayerPrefs.SetFloat("m_Temp_ACM", (float)value);
        }
    }
    public static double m_Temp_AIU
    {
        get
        {
            return PlayerPrefs.GetFloat("m_Temp_AIU", 0);
        }
        set
        {
            PlayerPrefs.SetFloat("m_Temp_AIU", (float)value);
        }
    }

    public static double m_Temp_ShowECPM
    {
        get
        {
            return PlayerPrefs.GetFloat("m_Temp_ShowECPM", 0);
        }
        set
        {
            PlayerPrefs.SetFloat("m_Temp_ShowECPM", (float)value);
        }
    }

    public static string PIX = "pix";
    public static string PAGBANK = "pagbank";
    public static string PAYPAL = "paypal";
    public static string OVO = "ovo";
    public static string DANA = "dana";
    public static string SHOPEE = "Shopeepay";
    public static string GOPAY = "gopay";

    public string ADShow = "ad_show";
    public string ADClick = "ad_click";
    public string ADLoad = "ad_load";
    public string ADRewardType = "Rewarded";
    public string ADInterstitialType = "Interstitial";

    private const float UsMinWithdrawalValue = 0.2f;
    private const float IDMinWithdrawalValue = 50;
    private const float BRMinWithdrawalValue = 0.3f;

    #region 国家类型

    public static string CURRENCY_Usa = "US"; //美元
    public static string CURRENCY_Indonesia = "ID"; //印尼盾
    public static string CURRENCY_Brasil = "BR"; // 巴西雷亚尔
    public const string COUNTRY_Japan = "JP";
    public const string COUNTRY_Korea = "KR";

    private static E_CountryType countryType = E_CountryType.None;

    public static E_CountryType CountryType
    {
        set { countryType = value; }
        get
        {
            if (countryType == E_CountryType.None)
            {
                LogLogger.LogVerbose(LogTag.LOG_Account, "没有选择到服务器下发的国家");
                countryType = E_CountryType.None;
            }

            return countryType;
        }
    }

    public static E_CountryType SetCountryTypeByServer(string CountryCode)
    {
        LogLogger.LogVerbose(LogTag.LOG_Account, "CountryCode " + CountryCode + "--");

        if (string.Equals(CountryCode, CURRENCY_Usa, StringComparison.OrdinalIgnoreCase))
        {
            CountryType = E_CountryType.US;
        }
        else if (string.Equals(CountryCode, CURRENCY_Indonesia, StringComparison.OrdinalIgnoreCase))
        {
            CountryType = E_CountryType.ID;
        }
        else if (string.Equals(CountryCode, CURRENCY_Brasil, StringComparison.OrdinalIgnoreCase))
        {
            CountryType = E_CountryType.BR;
        }
#if !BIZZA_HTTP_AD
        else if (string.Equals(CountryCode, COUNTRY_Japan, StringComparison.OrdinalIgnoreCase))
        {
            CountryType = E_CountryType.JP;
        }
        else if (string.Equals(CountryCode, COUNTRY_Korea, StringComparison.OrdinalIgnoreCase))
        {
            CountryType = E_CountryType.KR;
        }
#endif

        LogLogger.LogVerbose(LogTag.LOG_Account, "国家 " + CountryType);
        return CountryType;
    }

    public static void SetLanguageByCountryType()
    {
#if !COMMONGAME
        switch (CountryType)
        {
            case E_CountryType.US:
                LanguageUtils.SelectedLanguage = "en-US";
                break;
            case E_CountryType.BR:
                LanguageUtils.SelectedLanguage = "pt-BR";
                break;
            case E_CountryType.ID:
                LanguageUtils.SelectedLanguage = "id-ID";
                break;
#if !BIZZA_HTTP_AD
            case E_CountryType.JP:
                LanguageUtils.SelectedLanguage = "ja-JP";
                break;
            case E_CountryType.KR:
                LanguageUtils.SelectedLanguage = "ko-KR";
                break;
            case E_CountryType.MX:
                LanguageUtils.SelectedLanguage = "es-MX";
                break;
            case E_CountryType.SA:
                LanguageUtils.SelectedLanguage = "ar-SA";
                break;
            case E_CountryType.DE:
                LanguageUtils.SelectedLanguage = "de-DE";
                break;
#endif
        }

        LogLogger.LogVerbose(LogTag.LOG_Account, "LanguageUtils.SelectedLanguage " + LanguageUtils.SelectedLanguage);
#endif
    }

    public static float PointsToUs = 10000;
    public static float UsToIndonesia = 15151;
    public static float UsToBrazil = 5;
    [Obfuz.ObfuzIgnore]

    public enum E_CountryType
    {

        None,

        US,
   
        BR,
       
        ID,
#if !BIZZA_HTTP_AD
       
        JP,
      
        KR,
      
        
        SA,
      
        DE
#endif
    }

    #endregion


    private const int MaxLoadingUserDataRetryTimes = 0;
    private int retryUserIdTimes = MaxLoadingUserDataRetryTimes;
    private int retryUserInfoTimes = MaxLoadingUserDataRetryTimes;

    public bool LoadingFinish
    {
        get
        {
            return loginUserIdFinish && loginUserInfoFinish;
        }
    }

    //public bool deviceInfoFinish => ;

    private bool loginUserIdFinish = false;
    public bool LoginUserIdFinish => loginUserIdFinish;
    private bool loginUserInfoFinish = false;
    public bool LoginUserInfoFinish => loginUserInfoFinish;
    private bool loginAttributeFinish = false;

    private bool loadingUserDataFailed = false;
    public bool LoadingUserDataFailed => loadingUserDataFailed;

    public string LoadingUserDataErrorCode { get; private set; }

    private void MarkLoadingUserDataFailed(string errorCode)
    {
        loadingUserDataFailed = true;
        LoadingUserDataErrorCode = errorCode;
    }

    private void MarkLoadingUserDataFailed<T>(FailHttpResponse<T> response)
    {
        MarkLoadingUserDataFailed(response.errorCode);
    }

    private async UniTask OnLoginData()
    {
        loginUserIdFinish = false;
        loginUserInfoFinish = false;
        loginAttributeFinish = false;

        loadingUserDataFailed = false;
        LoadingUserDataErrorCode = null;
        retryUserIdTimes = MaxLoadingUserDataRetryTimes;
        retryUserInfoTimes = MaxLoadingUserDataRetryTimes;

        const string ClassName = "com.bangsawan.oceanshine.HttpUtil";
        const string SendHttpMethodName = "initialize";

        JavaBridgeUtils.CallStaticMethodCacheClass(ClassName, SendHttpMethodName, Application.identifier, Application.version, DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Vc.ToString());

        // 先查看是否有 用户id
#if UNITY_EDITOR
        if (ChannelConfig.Instance.real_CustomConfig.useEditorUserId)
        {
            PlayerPrefs.SetString(m_userIdKey, ChannelConfig.Instance.real_CustomConfig.editorUserId);
        }
#endif
        var id = PlayerPrefs.GetString(m_userIdKey);
        // 如果 没有用户id
        if (string.IsNullOrEmpty(id))
        {
            // 首次登录需要 GAID；由 DeviceInfoUtil 统一请求，避免 Account/IP 各自监听回调。
            string googleId = await DeviceInfoUtil.GetGoogleIdAsync();
            LogLogger.LOGUserInstall($" 没有用户id  进入到请求流程");
            login(googleId);
        }
        else
        {
            LogLogger.LOGUserInstall($" 有用户id了 {id}");
            InitUserInfo();
        }
    }

    // 只有第一次登录时才会进来， 如果有 UserId 那么就不会走到这里
    private void login(string id)
    {
        var _deviceInfo = DeviceInfoUtil.Data;
        var deviceInfo = new OceanShineUserLoginRequest();
        deviceInfo.TransformToUserLoginRequest(_deviceInfo, deviceInfo);
        if (string.IsNullOrEmpty(deviceInfo.Os_Gaid))
        {
            deviceInfo.Os_Gaid = id;
        }

        LogLogger.LOGUserInstall($" 设备信息提交，Google ID 可用={!string.IsNullOrEmpty(deviceInfo.Os_Gaid)}");
        deviceInfo.Print();
        Request_LoginRequest(deviceInfo, Response_LoginResponse);
    }

    private void InitUserInfo()
    {
        loginUserIdFinish = true; // 走到用户信息这一步 肯定就是有用户id了
        BizzaSdk.Ad.SetUserId(PlayerPrefs.GetString(m_userIdKey));

        Request_UserInfoRequest(true, false, Response_UserInfoResponse);
#if BIZZA_HTTP_AD
        Request_AppOtherConfigRequest(null, false);
        Request_RoutineTaskLookAdMoneyRequest(true, null, false);
        Request_FeedbackListV2Request(CreateOceanShineFeedbackListV2Request(), (FailHttpResponse<OceanShineFeedbackListV2Response> response) =>
        {
            if (response.success && response.data != null && response.data.Os_Msl != null)
            {
                if (response.data.Os_Msl.Count != SaveDataUtils.GameData.serviceInfoCount)
                {
                    SaveDataUtils.GameData.serviceAlter = true;
                }
                else
                {
                    SaveDataUtils.GameData.serviceAlter = false;
                }
                SaveDataUtils.Save();

            }
        });
#endif
    }

    #endregion

    private UserInfo Os_Uso;
    private bool isLoadingAlter = false;

    public void CheckPendingAutoPopups()
    {
#if !COMMONGAME
        AutoPopupQueue.Shared.Tick(UIModule.Instance);
#endif
    }

    public UserInfo Os_Current_Uso
    {
        get => Os_Uso;
        set
        {
            if (value == null) return;
            LogLogger.LOGUserInfoAlter($"用户信息更新 + {value.GetInfo()} + {isLoadingAlter}");
            if (Os_Uso != null)
            {
                LogLogger.LOGUserInfoAlter($"之前用户信息 + {Os_Uso.GetInfo()} + {isLoadingAlter}");
            }

            if (Os_Uso != null && isLoadingAlter && Os_Uso.Os_Tra < value.Os_Tra)
            {
#if !COMMONGAME
                // Copy values before replacing Os_Uso; deferred UI must not read mutable user info.
                var info = new ExchangeRateInfo
                {
                    beforeBlance = Os_Uso.GetBalance(),
                    beforeClash = Os_Uso.Os_Ewl,
                    nowBlance = value.GetBalance(),
                    nowClash = value.Os_Ewl,
                    beforeRate = Os_Uso.Os_Tra,
                    nowRate = value.Os_Tra,
                };
                AutoPopupQueue.Shared.Enqueue(UIPageIds.ExchangeRatePanel, info,
                    key: FormattableString.Invariant($"ExchangeRate:{info.beforeRate:R}:{info.nowRate:R}"),
                    isValid: () => ReferenceEquals(Instance, this));
#endif
            }

            SaveDataUtils.GameData.todayAdTimes = value.Os_Lgd;
            SaveDataUtils.GameData.fakeWithdrawPanelFirstWithdraw = value.Os_Mny == 0;
            isLoadingAlter = true;
            Os_Uso = value;
            BizzaEventSystem.Emit(EventDefine.Item.ItemChanged);
            SaveDataUtils.Save();
            CheckPendingAutoPopups();
        }
    }

    public static string version => Application.version;

    public string Get_S_Ewl()
    {
        string _ewl = WithdrawalUtil.GetCustomizedValueByCountryType((float)Os_Uso.Os_Ewl);
        return _ewl;
    }

    [Serializable]
    public class UserInfo
    {
        [JsonProperty("SbmBac")]
        public double Os_Bac; // 当前金额 balance - 巴西/印尼

        [JsonProperty("SbmBaci")]
        public int Os_Baci; // 当前积分数 balance_int - 美国

        [JsonProperty("SbmCon")]
        public int Os_Con; // 当前金币数 coin

        [JsonProperty("SbmCrc")]
        public string Os_Crc; // 国家货币 currency

        [JsonProperty("SbmCrcs")]
        public string Os_Crcs; // 国家货币符号 currencySymbols

        [JsonProperty("SbmCty")]
        public string Os_Cty; // 国家 country

        [JsonProperty("SbmEwl")]
        public double Os_Ewl; // 可提现金额 enableWithdrawal

        [JsonProperty("SbmImg")]
        public int Os_Img; // 是否有消息未读，1是，0否

        [JsonProperty("SbmLev")]
        public int Os_Lev; // 当前关卡 level

        [JsonProperty("SbmLgd")]
        public int Os_Lgd; // 用户连续登录天数 loginDay

        [JsonProperty("SbmMny")]
        public double Os_Mny; // 用户真实的钱（货币）

        [JsonProperty("SbmNcm")]
        public bool Os_Ncm; // 是否是新手 newComer

        [JsonProperty("SbmNnm")]
        public string Os_Nnm; // 用户昵称 nickName

        [JsonProperty("SbmRol")]
        public double Os_Rol; // 用户的卷（货币）

        [JsonProperty("SbmRti")]
        public long Os_Rti; // 注册时间 registerTime

        [JsonProperty("SbmRts")]
        public int Os_Rts; // 当前关卡 reachTimes

        [JsonProperty("SbmTra")]
        public double Os_Tra; // 可提现比例 taxRate

        public double GetBalance()
        {
            double balance = CountryType switch
            {
                E_CountryType.BR => Os_Bac,
                E_CountryType.ID => Os_Bac,
                E_CountryType.US => Os_Baci,
                _ => Os_Bac
            };

            return balance;
        }

        public string GetInfo()
        {
            return
                $"Os_Bac (当前金额-巴西/印尼): {Os_Bac}\n" +
                $"Os_Baci (当前积分-美国): {Os_Baci}\n" +
                $"Os_Con (当前金币数): {Os_Con}\n" +
                $"Os_Crc (货币类型): {Os_Crc}\n" +
                $"Os_Crcs (货币符号): {Os_Crcs}\n" +
                $"Os_Cty (国家): {Os_Cty}\n" +
                $"Os_Ewl (可提现金额): {Os_Ewl}\n" +
                $"Os_Img (是否有未读消息): {Os_Img}\n" +
                $"Os_Lev (当前关卡): {Os_Lev}\n" +
                $"Os_Lgd (连续登录天数): {Os_Lgd}\n" +
                $"Os_Mny (真实货币金额): {Os_Mny}\n" +
                $"Os_Ncm (是否新手): {Os_Ncm}\n" +
                $"Os_Nnm (用户昵称): {Os_Nnm}\n" +
                $"Os_Rol (用户卷/货币): {Os_Rol}\n" +
                $"Os_Rti (注册时间戳): {Os_Rti}\n" +
                $"Os_Rts (当前关卡): {Os_Rts}\n" +
                $"Os_Tra (可提现比例/税率): {Os_Tra}\n" +
                $"Balance (根据国家计算的余额): {GetBalance()}";
        }
    }


    #region 发行需要的API

    #region 增加收益 --- 看完广告后获取的内容

    // 请求体数据模型类
    [Serializable]
    public class OceanShineAdRevenueRequest
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmBtid")]
        public string Os_Btid; // 获取收益ID

        [JsonProperty("SbmEcm")]
        public double Os_Ecm; // 广告ECPM

        [JsonProperty("SbmSal")]
        public string Os_Sal; // 特殊标识：ad_show / task_10000 / small

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本


        public string Print()
        {
            return
                $"Os_Apid : {Os_Apid} Os_Btid : {Os_Btid} Os_Ecm : {Os_Ecm} Os_Sal : {Os_Sal} Os_Usid : {Os_Usid} Os_Vn : {Os_Vn} ";
        }
    }


    // 响应体数据模型类
    [Serializable]
    public class OceanShineAdRevenueResponse
    {
        [JsonProperty("SbmBac")]
        public int Os_Bac; // 获取的卷 --- 美国使用

        [JsonProperty("SbmUso")]
        public UserInfo Os_Uso; // 用户信息

        [JsonProperty("SbmPrc")]
        public double Os_Prc; // 获取的金额（当前国家）--- 巴西和印尼

        [JsonProperty("SbmMul")]
        public double Os_Mul; // 原基础上增加倍数，1为1倍，2为2倍，0.1为10%，0.05为5%
        [JsonProperty("SbmCon")]
        public int Os_Con; // 获取的金币-有可能为0->coin

        public double GetBalance()
        {
            LogLogger.LOGUserInfoAlter($"看完广告后的内容 ---- Os_Bac : {Os_Bac} Os_Prc : {Os_Prc}");

            double balance = CountryType switch
            {
                E_CountryType.BR => Os_Prc,
                E_CountryType.ID => Os_Prc,
                E_CountryType.US => Os_Bac,
                _ => Os_Bac
            };

            return balance;
        }

        public string Print()
        {
            return $"Os_Bac : {Os_Bac} Os_Prc : {Os_Prc}";
        }
    }

    public OceanShineAdRevenueResponse AdRevenueResponse = new();

    /// <summary>
    /// 获取用户请求体
    /// </summary>
    /// <param name="_Os_Btid">获取收益 ID</param>
    /// <param name="_Os_Ecm">广告 ECPM</param>Http
    /// <param name="_Os_Sal">特殊标识</param>
    /// <returns></returns>
    public OceanShineAdRevenueRequest GetAdRevenueRequest(double _Os_Ecm, string _Os_Sal, string id)
    {
        OceanShineAdRevenueRequest request = new OceanShineAdRevenueRequest()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
            Os_Vn = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Vn,
            Os_Btid = id,
            Os_Ecm = _Os_Ecm,
            Os_Sal = _Os_Sal,
        };

        LogLogger.LOGUserInfoAlter(" 增加收益结果 " + request.Print());
        return request;
    }

    public void Request_AdRevenueRequest(OceanShineAdRevenueRequest request, E_AdType adType,
        Action<FailHttpResponse<OceanShineAdRevenueResponse>> callback = null, float delay = 0)
    {
        Action<FailHttpResponse<OceanShineAdRevenueResponse>> _callback = null;
        _callback += OnRequest;
        _callback += callback;
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<OceanShineAdRevenueResponse>(AccountModuleCfg.add_ecpm, requestParams, _callback,
            false, delay);

        void OnRequest(FailHttpResponse<OceanShineAdRevenueResponse> response)
        {
            LogLogger.LogVerbose(LogTag.ADReportFlow, "请求用户id 来自于 增加收益");
            // Request_GetAdRevenueReportIdRequest(adType, null, false);
            if (!response.success || response.data == null)
            {
                LogLogger.LogVerbose(LogTag.ADReportFlow, "增加收益回调失败，设置为无id");
                return;
            }

            AdRevenueResponse = response.data;
            Os_Current_Uso = response.data.Os_Uso;
            QueueDailyWithdrawal();
        }
    }

    private void QueueDailyWithdrawal()
    {
#if !COMMONGAME
        var day = DateTime.Today;
        if (!CanShowDailyWithdrawal(day)) return;
        AutoPopupQueue.Shared.Enqueue(UIPageIds.DailyWithdrawPanel,
            key: $"DailyWithdraw:{day:yyyyMMdd}",
            isValid: () => ReferenceEquals(Instance, this) && DateTime.Today == day && CanShowDailyWithdrawal(day),
            onShown: () =>
            {
                SaveDataUtils.GameData.lastGetDailyRewardTime = DateTime.Now;
                SaveDataUtils.gameStrategy.SaveData();
            });
        CheckPendingAutoPopups();
#endif
    }

    private bool CanShowDailyWithdrawal(DateTime day)
    {
        if (Os_Current_Uso == null || SaveDataUtils.GameData.lastGetDailyRewardTime.Date >= day) return false;
        double available = Os_Current_Uso.Os_Ewl;
        return CountryType switch
        {
            E_CountryType.BR => available >= BRMinWithdrawalValue,
            E_CountryType.ID => available >= IDMinWithdrawalValue,
            E_CountryType.US => available >= UsMinWithdrawalValue,
            _ => false,
        };
    }

    #endregion

    #region 去提现 - 全部提现 --- 点击提现后

    // 请求体数据模型类，包含了去提现请求所需的字段
    [Serializable]
    public class OceanShineApplyWithdrawalRequestReal
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmCp")]
        public string Os_Cp; // CPF

        [JsonProperty("SbmGdid")]
        public int Os_Gdid; // 商品ID

        [JsonProperty("SbmMid")]
        public int Os_Mid; // 商品中的支付配置ID

        // [JsonProperty("Mn")]
        // public string Os_Mn; // money现金提现节点 或 task_任务ID

        [JsonProperty("SbmPb")]
        public string Os_Pb; // Pix账号绑定类型 (P E C B)

        [JsonProperty("SbmRa")]
        public string Os_Ra; // ReceiverAccount

        [JsonProperty("SbmRe")]
        public string Os_Re; // ReceiverEmail

        [JsonProperty("SbmRm")]
        public string Os_Rm; // ReceiverMobile

        [JsonProperty("SbmRn")]
        public string Os_Rn; // ReceiverName

        [JsonProperty("SbmSeid")]
        public string Os_Seid; // 客户端UUID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本

        // [JsonProperty("At")]
        // public string Os_At; // 现金或卷提现 applyType：money 或 rolll

        [JsonProperty("bsm_trt")]  // -------------------------------------------多出来的API
        public string Os_Trt; // 选择提现的比例
    }

    // 响应体数据模型类，包含了去提现请求的响应字段
    [Serializable]
    public class OceanShineApplyWithdrawalResponse
    {
        [JsonProperty("SbmOdr")]
        public string Os_Odr; // 订单号 orderNo
    }

    public OceanShineApplyWithdrawalRequestReal GetApplyWithdrawalRequestReal(string _Os_Cp, int _Os_Mid, string _Os_Pb,
        string _Os_Ra, string _Os_Re, string _Os_Rm, string _Os_Rn, string Os_Mn, string _Os_At)
    {
        OceanShineApplyWithdrawalRequestReal request = new OceanShineApplyWithdrawalRequestReal()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Vn = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Vn,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
            Os_Seid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Seid,

            // Os_Mn = Os_Mn,
            Os_Cp = _Os_Cp,
            Os_Gdid = 0,
            Os_Mid = _Os_Mid,
            Os_Pb = _Os_Pb,
            Os_Ra = _Os_Ra,
            Os_Re = _Os_Re,
            Os_Rm = _Os_Rm,
            Os_Rn = _Os_Rn,
            //Os_At = _Os_At,
            Os_Trt = "0"
        };
        return request;
    }

    public void Request_ApplyWithdrawalRequest(OceanShineApplyWithdrawalRequestReal request,
        Action<FailHttpResponse<OceanShineApplyWithdrawalResponse>> callback = null)
    {
        Action<FailHttpResponse<OceanShineApplyWithdrawalResponse>> replace = null;
        replace += OnResponse;
        replace += callback;
        string requestParams = JsonConvert.SerializeObject(request);
        LogLogger.LogVerbose(LogTag.LOG_Account, "提现信息： " + requestParams + "");
        HttpUtil.RequestToServer(AccountModuleCfg.add_order, requestParams, replace, true);

        void OnResponse(FailHttpResponse<OceanShineApplyWithdrawalResponse> response)
        {
            Request_UserInfoRequest(true, true, (r) => { });
        }
    }

    #endregion

    #region 新接口-去提现-现金或奖卷 --- 请求 返回参数 和 "去提现 - 全部提现" 一致
    [Serializable]
    public class OceanShineApplyWithdrawalRequestFake
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmCp")]
        public string Os_Cp; // CPF

        [JsonProperty("SbmGdid")]
        public int Os_Gdid; // 商品ID

        [JsonProperty("SbmMid")]
        public int Os_Mid; // 商品中的支付配置ID

        [JsonProperty("SbmMn")]
        public string Os_Mn; // money现金提现节点 或 task_任务ID

        [JsonProperty("SbmPb")]
        public string Os_Pb; // Pix账号绑定类型 (P E C B)

        [JsonProperty("SbmRa")]
        public string Os_Ra; // ReceiverAccount

        [JsonProperty("SbmRe")]
        public string Os_Re; // ReceiverEmail

        [JsonProperty("SbmRm")]
        public string Os_Rm; // ReceiverMobile

        [JsonProperty("SbmRn")]
        public string Os_Rn; // ReceiverName

        [JsonProperty("SbmSeid")]
        public string Os_Seid; // 客户端UUID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本

        // [JsonProperty("At")] // SportBallsMatch ApplyMoney 请求不定义该字段
        // public string Os_At; // 现金或卷提现 applyType：money 或 rolll
    }

    public OceanShineApplyWithdrawalRequestFake GetApplyWithdrawalRequestFake(string _Os_Cp, int _Os_Mid, string _Os_Pb,
        string _Os_Ra, string _Os_Re, string _Os_Rm, string _Os_Rn, string _Os_Mn, string _Os_At)
    {
        OceanShineApplyWithdrawalRequestFake request = new OceanShineApplyWithdrawalRequestFake()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Vn = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Vn,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
            Os_Seid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Seid,

            Os_Mn = _Os_Mn,
            Os_Cp = _Os_Cp,
            Os_Gdid = 0,
            Os_Mid = _Os_Mid,
            Os_Pb = _Os_Pb,
            Os_Ra = _Os_Ra,
            Os_Re = _Os_Re,
            Os_Rm = _Os_Rm,
            Os_Rn = _Os_Rn,
            // Os_At = _Os_At
        };
        return request;
    }

    public void Request_ApplyWithdrawalMoneyRollReqRequest(OceanShineApplyWithdrawalRequestFake request,
        Action<FailHttpResponse<OceanShineApplyWithdrawalResponse>> callback = null)
    {
        Action<FailHttpResponse<OceanShineApplyWithdrawalResponse>> replace = null;
        replace += OnResponse;
        replace += callback;
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer(AccountModuleCfg.add_order_task, requestParams, replace, true);

        void OnResponse(FailHttpResponse<OceanShineApplyWithdrawalResponse> response)
        {
            Request_UserInfoRequest(true, true, (r) => { });
        }

    }

    #endregion

    #region 其它配置信息

    // 请求体数据模型类，包含了其它配置信息请求所需的字段
    [Serializable]
    public class OceanShineAppOtherConfigRequest
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本

        [JsonProperty("Sbmcfn")]
        public string Os_Cfn; // 配置Key
    }

    public string OceanShineAppOtherConfigResponse;

    public int GetMaxDrawithRatio(bool isCash)
    {
        if (OceanShineAppOtherConfigResponse == null) return 0;

        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(OceanShineAppOtherConfigResponse);
        if (dict == null) return 0;

        int lookedTimes = Mathf.Max(SaveDataUtils.GameData.todayAdTimes,
            AccountModule.Instance.Os_Current_Uso.Os_Lgd); // 已看广告次数

        int _ratioTemp = 0;

        int oneLook = Convert.ToInt32(dict[AccountModuleCfg.one_Count]);
        int oneRatio = Convert.ToInt32(dict[AccountModuleCfg.one_Ratio]);
        if (lookedTimes >= oneLook)
        {
            _ratioTemp = oneRatio;
        }

        int twoLook = Convert.ToInt32(dict[AccountModuleCfg.two_Count]);
        int twoRatio = Convert.ToInt32(dict[AccountModuleCfg.two_Ratio]);
        if (lookedTimes >= twoLook)
        {
            _ratioTemp = twoRatio;
        }

        int threeLook = Convert.ToInt32(dict[AccountModuleCfg.three_Count]);
        int threeRatio = Convert.ToInt32(dict[AccountModuleCfg.three_Ratio]);
        if (lookedTimes >= threeLook)
        {
            _ratioTemp = threeRatio;
        }

        return _ratioTemp;
    }



    public void Request_AppOtherConfigRequest(Action<bool, string> callback = null, bool black = true, bool isRefresh = false)
    {
        if (!string.IsNullOrEmpty(OceanShineAppOtherConfigResponse) && isRefresh == false)
        {
            callback?.Invoke(true, OceanShineAppOtherConfigResponse);
            return;
        }

        OceanShineAppOtherConfigRequest request = new OceanShineAppOtherConfigRequest()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Vn = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Vn,
            Os_Cfn = "look_ad_reward_mul"
        };
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer(AccountModuleCfg.base_list, requestParams, OnRequest, black);

        void OnRequest(bool success, string response)
        {
            OceanShineAppOtherConfigResponse = response;
            callback?.Invoke(success, response);
        }
    }

    #endregion

    #region 用户归因 ---- 用户安装的第一次

    // 请求体数据模型类，包含了用户归因请求所需的字段  -- 实际没有用到不用替换
    [Serializable]
    public class OceanShineUserAttrsRequest
    {
        [JsonProperty("SbmAgp")]
        public string Os_Agp; // 设备当前归因广告组的名称

        [JsonProperty("SbmAid")]
        public string Os_Aid; // 设备的唯一 Adjust ID

        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmCat")]
        public double Os_Cat; // 安装成本 costAmount

        [JsonProperty("SbmCcy")]
        public string Os_Ccy; // 成本相关的货币代码 costCurrency

        [JsonProperty("SbmCkl")]
        public string Os_Ckl; // 安装被标记的点击标签 clickLabel

        [JsonProperty("SbmCmp")]
        public string Os_Cmp; // 设备当前归因 campaign

        [JsonProperty("SbmCti")]
        public string Os_Cti; // 设备当前归因素材名称 creative

        [JsonProperty("SbmCtm")]
        public long Os_Ctm; // 客户端收到回调的时间 clientTime

        [JsonProperty("SbmCty")]
        public string Os_Cty; // 推广活动定价模型 costType

        [JsonProperty("SbmDlu")]
        public string Os_Dlu; // 投放参数 deepLinkUrl

        [JsonProperty("SbmEvt")]
        public string Os_Evt; // 触发归因的事件 event

        [JsonProperty("SbmFbr")]
        public string Os_Fbr; // 回调原始值 fbInstallReferrer

        [JsonProperty("SbmGct")]
        public string Os_Gct; // Google 点击时间 ggClickTime

        [JsonProperty("SbmNet")]
        public string Os_Net; // 归因渠道名称 network

        [JsonProperty("SbmTrn")]
        public string Os_Trn; // 归因跟踪码 trackerName

        [JsonProperty("SbmTrt")]
        public string Os_Trt; // 归因跟踪名称 trackerToken

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("Vn")]
        public string Vn; // 应用版本
    }

#if BIZZA_ENABLE_ADJUST
    public Dictionary<string, object> CreateFromJson(AdjustSdk.AdjustAttribution attribution, string eventName)
    {
        var _oceanShineUserAttrs = new Dictionary<string, object>
        {
                { "SbmAgp", attribution.Adgroup },// 设备当前归因广告组的名称
                { "SbmAid", PlayerPrefs.GetString(adJustAdidKey, "") },// 设备的唯一 Adjust ID
                { "SbmApid", DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid },// 应用 ID
                { "SbmCat", attribution.CostAmount ?? 0 },// 安装成本 - costAmount
                { "SbmCcy", attribution.CostCurrency },// 成本相关的货币代码 - costCurrency
                { "SbmCkl", "" },// 安装被标记的点击标签 - clickLabel
                { "SbmCmp", attribution.Campaign },// 设备当前归因 - campaign
                { "SbmCti", attribution.Creative },// 设备当前归因素材的名称 - creative
                { "SbmCtm", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },// 客户端收到回调的时间 - clientTime
                { "SbmCty", attribution.CostType },// 推广活动定价模型 - costType
                { "SbmDlu", "" },// 投放参数 - deepLinkUrl
                { "SbmEvt", eventName },// 触发归因的事件 - event
                { "SbmFbr", attribution.FbInstallReferrer },// 回调原始值 - fbInstallReferrer
                { "SbmGct", "" },// Google 点击时间 - ggClickTime
                { "SbmNet", attribution.Network },// 归因渠道名称 - network
                { "SbmTrn", attribution.TrackerName }, // 归因跟踪码 - trackerName
                { "SbmTrt", attribution.TrackerToken },// 归因跟踪名称 - trackerToken
                { "SbmUsid", PlayerPrefs.GetString(AccountModule.m_userIdKey) },// 用户 ID
                { "Vn", Application.version }// 应用版本号
        };
        return _oceanShineUserAttrs;
    }
#endif

    /// <summary>
    /// 用户归因 上报请求（无业务返回值）
    /// </summary>
    public void Request_UserAttrsRequest(Dictionary<string, object> request,
        Action<FailHttpResponse<OceanShineUserAttrsRequest>> callback = null)
    {
        LogLogger.LogAttribute($"Adjust归因上报服务器");
        string requestParams = JsonConvert.SerializeObject(request);
        // 无返回体，使用 object
        HttpUtil.RequestToServer<OceanShineUserAttrsRequest>(AccountModuleCfg.from, requestParams, callback, false);
    }

    #endregion

    #region 获取收益ID-定制版参数 ---- 多个广告平台的 获取ID 当前应该不用

    // 请求体数据模型类，包含了获取收益ID请求所需的字段
    [Serializable]
    public class OceanShineGetAdRevenueReportIdRequest
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本
    }

    // 响应体数据模型类，包含了获取收益ID请求的响应字段
    [Serializable]
    public class OceanShineGetAdRevenueReportIdResponse
    {
        [JsonProperty("SbmBtid")]
        public string Os_Btid; // 收益ID batch_id

        [JsonProperty("SbmPfm")]
        public string Os_Pfm; // 广告平台名称
    }

    public void Request_BatchId(Action<FailHttpResponse<OceanShineGetAdRevenueReportIdResponse>> callback = null)
    {
        OceanShineGetAdRevenueReportIdRequest request = new()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
            Os_Vn = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Vn,
        };
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<OceanShineGetAdRevenueReportIdResponse>(AccountModuleCfg.get_ecpm_id, requestParams,
            callback);
    }

    #endregion

    #region 用户登陆-定制版参数 --- 用户如果没有 Uid时才会调用 --- 暂时没有问题

    // 请求体数据模型类，包含了用户登录请求所需的字段
    [Serializable]
    public class OceanShineUserLoginRequest
    {
        [JsonProperty("SbmAnid")]
        public string Os_Anid; // 安卓ID

        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmAtd")]
        public int Os_Atd; // 调试模式

        [JsonProperty("SbmBbd")]
        public string Os_Bbd; // 手机品牌

        [JsonProperty("SbmCal")]
        public string Os_Cal; // 来源

        [JsonProperty("SbmCtr")]
        public int Os_Ctr; // 当前小时值

        [JsonProperty("SbmDtd")]
        public string Os_Dtd; // 屏幕密度

        [JsonProperty("SbmDth")]
        public int Os_Dth; // 屏幕高

        [JsonProperty("SbmDtw")]
        public int Os_Dtw; // 屏幕宽

        [JsonProperty("SbmGaid")]
        public string Os_Gaid; // GoogleId

        [JsonProperty("SbmLag")]
        public string Os_Lag; // 语言

        [JsonProperty("SbmMbl")]
        public string Os_Mbl; // 设备型号

        [JsonProperty("SbmNbt")]
        public string Os_Nbt; // 网络

        [JsonProperty("SbmObv")]
        public string Os_Obv; // 系统版本

        [JsonProperty("SbmRtt")]
        public int Os_Rtt; // 是否root

        [JsonProperty("SbmSbi")]
        public int Os_Sbi; // SIM卡

        [JsonProperty("SbmSeid")]
        public string Os_Seid; // 客户端UUID

        [JsonProperty("SbmTry")]
        public string Os_Try; // 国家码

        [JsonProperty("SbmTtz")]
        public string Os_Ttz; // 时区

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmCpu")]
        public string Os_Cpu; // CPU架构

        [JsonProperty("SbmUa")]
        public string Os_Ua; // 客户端

        [JsonProperty("SbmVc")]
        public int Os_Vc; // App版本号

        [JsonProperty("SbmVn")]
        public string Os_Vn; // App版本名

        /// <summary>
        /// 打印当前对象所有字段
        /// </summary>
        public void Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("===== OceanShineUserLoginRequest =====");
            sb.AppendLine($"Os_Anid (安卓ID): {Os_Anid}");
            sb.AppendLine($"Os_Apid (应用ID): {Os_Apid}");
            sb.AppendLine($"Os_Atd (调试模式): {Os_Atd}");
            sb.AppendLine($"Os_Bbd (手机品牌): {Os_Bbd}");
            sb.AppendLine($"Os_Cal (来源): {Os_Cal}");
            sb.AppendLine($"Os_Cpu (CPU架构): {Os_Cpu}");
            sb.AppendLine($"Os_Ctr (当前小时): {Os_Ctr}");
            sb.AppendLine($"Os_Dtd (屏幕密度): {Os_Dtd}");
            sb.AppendLine($"Os_Dth (屏幕高): {Os_Dth}");
            sb.AppendLine($"Os_Dtw (屏幕宽): {Os_Dtw}");
            sb.AppendLine($"Os_Gaid (GoogleId) 可用: {!string.IsNullOrEmpty(Os_Gaid)}");
            sb.AppendLine($"Os_Lag (语言): {Os_Lag}");
            sb.AppendLine($"Os_Mbl (设备型号): {Os_Mbl}");
            sb.AppendLine($"Os_Nbt (网络): {Os_Nbt}");
            sb.AppendLine($"Os_Obv (系统版本): {Os_Obv}");
            sb.AppendLine($"Os_Rtt (是否Root): {Os_Rtt}");
            sb.AppendLine($"Os_Sbi (SIM卡): {Os_Sbi}");
            sb.AppendLine($"Os_Seid (UUID): {Os_Seid}");
            sb.AppendLine($"Os_Try (国家码): {Os_Try}");
            sb.AppendLine($"Os_Ttz (时区): {Os_Ttz}");
            sb.AppendLine($"Os_Ua (客户端): {Os_Ua}");
            sb.AppendLine($"Os_Usid (用户ID): {Os_Usid}");
            sb.AppendLine($"Os_Vc (版本号): {Os_Vc}");
            sb.AppendLine($"Os_Vn (版本名): {Os_Vn}");
            sb.AppendLine("====================================");

            Debug.Log(sb.ToString());
        }

        public OceanShineUserLoginRequest TransformToUserLoginRequest(
            DeviceInfoUtil.DeviceInfoData deviceInfo,
            OceanShineUserLoginRequest target)
        {
            if (deviceInfo == null)
                return target;

            if (target == null)
                target = new OceanShineUserLoginRequest();

            target.Os_Anid = deviceInfo.Os_Anid;
            target.Os_Apid = deviceInfo.Os_Apid;
            target.Os_Atd = deviceInfo.Os_Atd;
            target.Os_Bbd = deviceInfo.Os_Bbd;
            target.Os_Cal = deviceInfo.Os_Cal;
            target.Os_Ctr = deviceInfo.Os_Ctr;
            target.Os_Dtd = deviceInfo.Os_Dtd;
            target.Os_Dth = deviceInfo.Os_Dth;
            target.Os_Dtw = deviceInfo.Os_Dtw;
            target.Os_Gaid = deviceInfo.Os_Gaid;
            target.Os_Lag = deviceInfo.Os_Lag;
            target.Os_Mbl = deviceInfo.Os_Mbl;
            target.Os_Nbt = deviceInfo.Os_Nbt;
            target.Os_Obv = deviceInfo.Os_Obv;
            target.Os_Rtt = deviceInfo.Os_Rtt;
            target.Os_Sbi = deviceInfo.Os_Sbi;
            target.Os_Seid = deviceInfo.Os_Seid;
            target.Os_Try = deviceInfo.Os_Try;
            target.Os_Ttz = deviceInfo.Os_Ttz;
            target.Os_Usid = deviceInfo.Os_Usid;
            target.Os_Cpu = deviceInfo.Os_Cpu;
            target.Os_Ua = deviceInfo.Os_Ua;
            target.Os_Vc = deviceInfo.Os_Vc;
            target.Os_Vn = deviceInfo.Os_Vn;

            return target;
        }
    }

    // 响应体数据模型类，包含了用户登录请求的响应字段
    [Serializable]
    public class OceanShineLoginResponse
    {
        [JsonProperty("SbmAru")]
        public ActiveRule Os_Aru; // activeRule

        [JsonProperty("SbmRdt")]
        public string Os_Rdt; // 注册日期

        [JsonProperty("SbmRti")]
        public long Os_Rti; // 注册时间戳

        [JsonProperty("SbmRw")]
        public int Os_Rw; // 当前模式，0无模式(开发)，1审核模式，2投放模式

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [Serializable]
        public class ActiveRule
        {
            [JsonProperty("SbmAcm")]
            public double Os_Acm; // ad_cpm

            [JsonProperty("SbmAiu")]
            public double Os_Aiu; // ad_ipu
        }
    }

    public void Request_LoginRequest(OceanShineUserLoginRequest userLoginRequest,
        Action<FailHttpResponse<OceanShineLoginResponse>> callback = null)
    {
        string requestParams = JsonConvert.SerializeObject(userLoginRequest);
        HttpUtil.RequestToServer<OceanShineLoginResponse>(AccountModuleCfg.login, requestParams, callback, false);
    }

    public void Response_LoginResponse(FailHttpResponse<OceanShineLoginResponse> response)
    {
        LogLogger.LogVerbose(LogTag.LOG_Account, $"=== OceanShineLoginResponse ===");
        if (!response.success || response.data == null)
        {
            if (retryUserIdTimes > 0)
            {
                retryUserIdTimes--;
                login("");
                return;
            }
            else
            {
#if !BIZZA_HTTP_AD
                loginUserIdFinish = true;
                loginUserInfoFinish = true;
#endif
            }

            MarkLoadingUserDataFailed(response);
            return;
        }

        if (response.success)
        {
            if (!string.IsNullOrEmpty(response.data.Os_Usid))
            {
                PlayerPrefs.SetString(m_userIdKey, response.data.Os_Usid);
            }

            m_Temp_ACM = response.data.Os_Aru.Os_Acm;
            m_Temp_AIU = response.data.Os_Aru.Os_Aiu;

            InitUserInfo();

            if (response.data.Os_Aru.Os_Aiu == 0)
            {
                // 在这里进行 归因初始化
                AdjustAttributionAdapter.AttributeStart("checkIPU");
            }

        }
        LogLogger.LogVerbose(LogTag.LOG_Account, $"+++ OceanShineLoginResponse +++");
    }


    #endregion

    #region 增加反馈 -- 可能时用户的点击

    // 请求体数据模型类，包含了添加反馈请求所需的字段
    [Serializable]
    public class OceanShineFeedbackRequest
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmDes")]
        public string Os_Des; // 反馈描述

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本
    }

    public OceanShineFeedbackRequest CreateFeedbackRequest(string description)
    {
        return new OceanShineFeedbackRequest
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Des = description,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
            Os_Vn = version
        };
    }

    /// <summary>
    /// 增加反馈 请求（无返回数据）
    /// </summary>
    public void Request_FeedbackRequest(OceanShineFeedbackRequest request,
        Action<FailHttpResponse<OceanShineFeedbackRequest>> callback = null)
    {
        string requestParams = JsonConvert.SerializeObject(request);

        // 无返回值，使用 object / 或者你们内部的 EmptyResponse
        HttpUtil.RequestToServer(AccountModuleCfg.msg, requestParams, callback);
    }

    #endregion

    #region 反馈列表 --- 也是用户点击把

    // 请求体数据模型类，包含了获取反馈列表请求所需的字段
    [Serializable]
    public class OceanShineFeedbackListV2Request
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本
    }

    public OceanShineFeedbackListV2Request CreateOceanShineFeedbackListV2Request()
    {
        return new OceanShineFeedbackListV2Request
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
            Os_Vn = version
        };
    }

    // 响应体数据模型类，包含了反馈列表请求的响应字段
    [Serializable]
    public class OceanShineFeedbackListV2Response
    {
        [JsonProperty("SbmMsl")]
        public List<OceanShineFeedbackListV2ResponseData> Os_Msl; // 反馈信息列表

        [JsonProperty("SbmUso")]
        public UserInfo Os_Uso; // 用户信息部分

        [Serializable]
        public class OceanShineFeedbackListV2ResponseData
        {
            [JsonProperty("SbmApid")]
            public string Os_Apid; // 应用ID

            [JsonProperty("SbmCda")]
            public string Os_Cda; // 提交时间 created_at

            [JsonProperty("SbmCte")]
            public long Os_Cte; // 提交时间毫秒 created_time

            [JsonProperty("SbmCtt")]
            public string Os_Ctt; // 内容 msg/text

            [JsonProperty("SbmId")]
            public long Os_Id; // 数据库ID

            [JsonProperty("SbmTpe")]
            public long Os_Tpe; // 类型，1用户反馈，2后台回答

            [JsonProperty("SbmUda")]
            public string Os_Uda; // 更新时间 updated_at

            [JsonProperty("SbmUsid")]
            public string Os_Usid; // 用户ID

            [JsonProperty("SbmUte")]
            public long Os_Ute; // 更新时间毫秒 updated_time

            [JsonProperty("SbmVn")]
            public string Os_Vn; // 版本号
        }

        public List<OceanShineFeedbackListV2ResponseData> GetSortedFeedbackByCreatedTime()
        {
            if (Os_Msl == null)
                return new List<OceanShineFeedbackListV2ResponseData>();

            // 使用 LINQ 创建新列表并排序
            return Os_Msl;
        }
    }

    // 实例
    public OceanShineFeedbackListV2Request feedbackListV2Request = new();
    public OceanShineFeedbackListV2Response feedbackListV2Response = new();

    /// <summary>
    /// 获取反馈列表 请求
    /// </summary>
    public void Request_FeedbackListV2Request(OceanShineFeedbackListV2Request request,
        Action<FailHttpResponse<OceanShineFeedbackListV2Response>> callback = null)
    {
        callback += OnRequest;
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<OceanShineFeedbackListV2Response>(AccountModuleCfg.msg_list, requestParams, callback);

        void OnRequest(FailHttpResponse<OceanShineFeedbackListV2Response> response)
        {
            if (!response.success || response.data == null)
                return;

            if (response.data.Os_Msl != null)
            {
                SaveDataUtils.GameData.serviceInfoCount = response.data.Os_Msl.Count;
            }
            Os_Current_Uso = response.data.Os_Uso;
        }
    }

    #endregion

    #region 新用户奖励-领取  ---- 第一次登录时的判定  --- 会修改 UserInfo

    // 请求体数据模型类，包含了新用户奖励领取请求所需的字段
    [Serializable]
    public class OceanShineUserReachReportRequest
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID
    }

    // 响应体数据模型类，包含了新用户奖励领取请求的响应字段
    [Serializable]
    public class OceanShineUserReachReportResponse
    {
        [JsonProperty("SbmAcg")]
        public NewcomerReward Os_Acg; // 新手奖励信息

        [JsonProperty("SbmUso")]
        public UserInfo Os_Uso; // 用户信息部分

        [Serializable]
        public class NewcomerReward
        {
            [JsonProperty("SbmCom")]
            public int Os_Com; // commonMerge

            [JsonProperty("SbmNba")]
            public int Os_Nba; // 新手奖励积分数 new_balance

            [JsonProperty("SbmNcc")]
            public int Os_Ncc; // 新手奖励金币 new_comer_coin

            [JsonProperty("SbmRnw")]
            public double Os_Rnw; // 新手奖励金额 newComerReward

            [JsonProperty("SbmUsd")]
            public double Os_Usd; // 转usd比例 toUsd
        }
    }

    /// <summary>
    /// 新用户奖励-领取 请求
    /// </summary>
    public void Request_UserReachReportRequest(
        Action<FailHttpResponse<OceanShineUserReachReportResponse>> callback = null)
    {
        callback += OnRequest;
        var request = new AccountModule.OceanShineUserReachReportRequest()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
        };
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<OceanShineUserReachReportResponse>(AccountModuleCfg.new_user, requestParams, callback,
            false);

        void OnRequest(FailHttpResponse<OceanShineUserReachReportResponse> response)
        {
            if (!response.success || response.data == null)
                return;
            Os_Current_Uso = response.data.Os_Uso;
        }
    }

    #endregion

    #region 关卡，完成次数上报 --- 和 新用户奖励-领取 一致

    /// <summary>
    /// 关卡，完成次数上报 请求
    /// </summary>
    public void Request_UserReachLevelReportRequest(
        Action<FailHttpResponse<OceanShineUserReachReportResponse>> callback = null)
    {
        callback += OnRequest;
        var request = new AccountModule.OceanShineUserReachReportRequest()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
        };
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<OceanShineUserReachReportResponse>(AccountModuleCfg.number, requestParams, callback,
            false);

        void OnRequest(FailHttpResponse<OceanShineUserReachReportResponse> response)
        {
            if (!response.success || response.data == null)
                return;
            Os_Current_Uso = response.data.Os_Uso;
        }
    }

    #endregion

    #region 订单列表 -- 历史提现记录

    // 请求体数据模型类，包含了订单列表请求所需的字段
    [Serializable]
    public class OceanShineUserInfoRequest
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID
    }

    [Serializable]
    public class OceanShineWithdrawalRecord
    {
        [JsonProperty("SbmCp")]
        public string Os_Cp; // CPF

        [JsonProperty("SbmCrc")]
        public string Os_Crc; // 货币码 currency

        [JsonProperty("SbmCrcs")]
        public string Os_Crcs; // 货币符号 currencySymbols

        [JsonProperty("SbmDat")]
        public string Os_Dat; // 提现时间 date

        [JsonProperty("SbmDed")]
        public double Os_Ded; // 扣除金额或现金提现时转roll金额

        [JsonProperty("SbmId")]
        public long Os_Id; // 订单ID

        [JsonProperty("SbmPb")]
        public string Os_Pb; // Pix账号绑定类型 P/E/C/B

        [JsonProperty("SbmPrc")]
        public double Os_Prc; // 金额 amount

        [JsonProperty("SbmPym")]
        public string Os_Pym; // 支付名称 payName

        [JsonProperty("SbmRa")]
        public string Os_Ra; // 收款账户 ReceiverAccount

        [JsonProperty("SbmRe")]
        public string Os_Re; // 收款人电子邮件 ReceiverEmail

        [JsonProperty("SbmRm")]
        public string Os_Rm; // 收款人手机号码 ReceiverMobile

        [JsonProperty("SbmRn")]
        public string Os_Rn; // 收款人姓名 ReceiverName

        [JsonProperty("SbmRrm")]
        public string Os_Rrm; // 提示信息 remarks

        [JsonProperty("SbmSts")]
        public int Os_Sts; // 状态：1进行中，2违规被拒，3成功，4失败

        [JsonProperty("SbmTry")]
        public int Os_Try; // 1现金提现，2roll提现

        [JsonProperty("SbmTsm")]
        public string Os_Tsm; // 提示信息（现金提现失败后）
    }

    /// <summary>
    /// 订单列表 请求
    /// </summary>
    public void Request_WithdrawalRecordRequest(
        Action<FailHttpResponse<List<OceanShineWithdrawalRecord>>> callback = null)
    {
        var request = new AccountModule.OceanShineUserInfoRequest()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
        };
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer(AccountModuleCfg.order_list, requestParams, callback, false);
    }

    #endregion

    #region 提现平台 --- 可能时打开提现界面时需要的数据

    // 请求体数据模型类，包含了提现平台请求所需的字段
    [Serializable]
    public class OceanShineWithdrawalPageRequest
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本
    }

    // 响应体数据模型类，包含了提现平台请求的响应字段 ---
    [Serializable]
    public class OceanShineWithdrawalPageResponse
    {
        // 用户信息部分
        [JsonProperty("SbmUso")]
        public UserInfo Os_Uso;

        // 提现规则
        [JsonProperty("SbmWr")]
        public List<WithdrawalRatio> Os_Wr;

        // 提现平台
        [JsonProperty("SbmWwf")]
        public List<WithdrawalPlatform> Os_Wwf;

        // 提现规则
        [Serializable]
        public class WithdrawalRatio
        {
            [JsonProperty("SbmAdy")]
            public int Os_Ady; // 提现档位 adFrequency

            [JsonProperty("SbmBen")]
            public int Os_Ben; // 开始 begin

            [JsonProperty("SbmEnd")]
            public int Os_End; // 结束 end

            [JsonProperty("SbmWro")]
            public double Os_Wro; // 提现比例 withdrawRatio
        }

        // 提现平台
        [Serializable]
        public class WithdrawalPlatform
        {
            [JsonProperty("SbmCn")]
            public string Os_Cn; // 支付平台图标 icon

            // [JsonProperty("bsm_gdl")] // SportBallsMatch Withdrawal 响应不定义该字段
            // public object Os_Gdl; // 保留旧字段声明，不参与反序列化

            [JsonProperty("SbmMe")]
            public string Os_Me; // 支付平台名称 name

            [JsonProperty("SbmMid")]
            public int Os_Mid; // 配置ID

            [JsonProperty("SbmMlt")]
            public double Os_Mlt; // 最低提现值 minimumLimit

            [JsonProperty("SbmEt")]
            public string Os_Et; // 支付平台描述 text

            [JsonProperty("SbmId")]
            public int Os_Id; // 平台ID
        }
    }

    private OceanShineWithdrawalPageResponse withdrawalPageResponse;

    /// <summary>
    /// 提现平台/提现页面 请求
    /// </summary>
    public void Request_WithdrawalPageRequest(
        Action<FailHttpResponse<OceanShineWithdrawalPageResponse>> callback = null, bool block = true, bool useCach = true)
    {
        if (withdrawalPageResponse != null && withdrawalPageResponse.Os_Wwf.Count > 0 && useCach)
        {
            FailHttpResponse<OceanShineWithdrawalPageResponse> response = new()
            {
                success = true,
                data = withdrawalPageResponse,
            };
            callback?.Invoke(response);
            return;
        }

        callback += OnRequest;
        var request = new OceanShineWithdrawalPageRequest()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(m_userIdKey),
            Os_Vn = version
        };

        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<OceanShineWithdrawalPageResponse>(AccountModuleCfg.plat_from, requestParams, callback,
            block);

        void OnRequest(FailHttpResponse<OceanShineWithdrawalPageResponse> response)
        {
            if (!response.success || response.data == null)
                return;

            withdrawalPageResponse = response.data;
            if (response.data == null) return;
            // Os_Current_Uso = response.data.Os_Uso;
        }
    }

    #endregion

    #region 每日任务-现金-广告提现金

    // 请求体数据模型类，包含了每日任务-现金-广告提现金请求所需的字段
    [Serializable]
    public class Apid_Usid_Vn_Request
    {
        [JsonProperty("SbmApid")]
        public string Os_Apid; // 应用ID

        [JsonProperty("SbmUsid")]
        public string Os_Usid; // 用户ID

        [JsonProperty("SbmVn")]
        public string Os_Vn; // 应用版本
    }

    // 响应体数据模型类，包含了每日任务-现金-广告提现金请求的响应字段
    [Serializable]
    public class RoutineTaskLookAdMoneyResponse
    {
        [JsonProperty("SbmAn")]
        public int Os_An; // 广告次数

        [JsonProperty("SbmCss")]
        public string Os_Css; // 国家货币符号

        [JsonProperty("SbmLn")]
        public int Os_Ln; // 已看次数

        [JsonProperty("SbmMrt")]
        public double Os_Mrt; // 卷转金额的比例

        [JsonProperty("SbmMy")]
        public double Os_My; // 可提现金额

        [JsonProperty("SbmSr")]
        public int Os_Sr; // 排序

        [JsonProperty("SbmSs")]
        public int Os_Ss; // 当前状态：1未达到条件，2可提现，3已提现

        [JsonProperty("SbmTid")]
        public int Os_Tid; // 任务ID
    }

    public RoutineTaskLookAdMoneyResponse routineTaskLookAdMoneyResponse = new();
    public List<RoutineTaskLookAdMoneyResponse> routineTaskLookAdMoneyResponses = new();

    /// <summary>
    /// 每日任务-现金-广告提现金 请求
    /// </summary>
    public void Request_RoutineTaskLookAdMoneyRequest(bool update,
        Action<FailHttpResponse<List<RoutineTaskLookAdMoneyResponse>>> resultCallback, bool block = true)
    {
        if (!update && routineTaskLookAdMoneyResponse != null && routineTaskLookAdMoneyResponse.Os_An != 0)
        {
            routineTaskLookAdMoneyResponses ??= new();
            routineTaskLookAdMoneyResponses.Clear();
            routineTaskLookAdMoneyResponses.Add(routineTaskLookAdMoneyResponse);
            FailHttpResponse<List<RoutineTaskLookAdMoneyResponse>> response = new()
            {
                success = true,
                data = routineTaskLookAdMoneyResponses
            };
            resultCallback?.Invoke(response);
            return;
        }

        resultCallback += OnRoutineTaskLookAdMoneyResponse;
        var request = new Apid_Usid_Vn_Request()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(m_userIdKey),
            Os_Vn = version
        };
        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<List<RoutineTaskLookAdMoneyResponse>>(AccountModuleCfg.task_list, requestParams,
            resultCallback, block);
    }

    private void OnRoutineTaskLookAdMoneyResponse(FailHttpResponse<List<RoutineTaskLookAdMoneyResponse>> response)
    {
        if (response.success && response.data != null)
        {
            if (response.data.Count == 0)
            {
                return;
            }

            routineTaskLookAdMoneyResponse = response.data[0];
            SaveDataUtils.GameData.userLookDailyAdCountMax = response.data[0].Os_An;
            SaveDataUtils.GameData.userLookDailyAdCount = response.data[0].Os_Ln;
            BizzaEventSystem.Emit(EventDefine.RealWithdraw.RefreshDailyMissionPageFalse);
        }
    }

    #endregion

    #region 用户信息-定制版参数 ---- 主要玩家登录时的 请求 --- 会保存用户信息 - UserInfo

    // 请求体数据模型类，包含了用户信息请求所需的字段 ---- 订单列表 一样的请求参数

    // 响应体数据模型类，包含了用户信息请求的响应字段
    [Serializable]
    public class OceanShineUserInfoResponse
    {
        // 新手奖励信息
        [JsonProperty("SbmAcg")]
        public NewcomerReward Os_Acg;

        // 用户信息部分
        [JsonProperty("SbmUso")]
        public UserInfo Os_Uso;

        [Serializable]
        public class NewcomerReward
        {
            [JsonProperty("SbmCom")]
            public int Os_Com; // commonMerge

            [JsonProperty("SbmNba")]
            public int Os_Nba; // 新手奖励积分数 new_balance

            [JsonProperty("SbmNcc")]
            public int Os_Ncc; // 新手奖励金币 new_comer_coin

            [JsonProperty("SbmRnw")]
            public double Os_Rnw; // 新手奖励金额 newComerReward

            [JsonProperty("SbmUsd")]
            public double Os_Usd; // 转 USD 比例 toUsd

            public double GetBalance()
            {
                double balance = CountryType switch
                {
                    E_CountryType.BR => Os_Rnw,
                    E_CountryType.ID => Os_Rnw,
                    E_CountryType.US => Os_Nba,
                    _ => Os_Rnw
                };

                return balance;
            }
        }
    }

    private OceanShineUserInfoResponse userInfoResponse;

    /// <summary>
    /// 用户信息 请求
    /// </summary>
    public void Request_UserInfoRequest(bool isUpdate, bool block = true,
        Action<FailHttpResponse<OceanShineUserInfoResponse>> callback = null)
    {
        if (!isUpdate && userInfoResponse != null && !string.IsNullOrEmpty(Os_Current_Uso.Os_Cty))
        {
            var response = new FailHttpResponse<OceanShineUserInfoResponse>()
            {
                success = true,
                data = userInfoResponse,
            };
            callback?.Invoke(response);
            return;
        }

        callback += OnRequest;
        OceanShineUserInfoRequest request = new OceanShineUserInfoRequest()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(m_userIdKey),
        };

        string requestParams = JsonConvert.SerializeObject(request);
        HttpUtil.RequestToServer<OceanShineUserInfoResponse>(AccountModuleCfg.user_info, requestParams, callback, block);

        void OnRequest(FailHttpResponse<OceanShineUserInfoResponse> response)
        {
            if (response.success && response.data != null && isUpdate)
            {
                userInfoResponse = response.data;
                Os_Current_Uso = response.data.Os_Uso;
            }
        }
    }

    /// <summary>
    /// 用户信息 响应
    /// </summary>
    public void Response_UserInfoResponse(FailHttpResponse<OceanShineUserInfoResponse> response)
    {
        LogLogger.LogVerbose(LogTag.LOG_Account, $"=== OceanShineUserInfoResponse === {response.success}");

        if (response.success && response.data != null)
        {
            this.Os_Current_Uso = response.data.Os_Uso;
            SetCountryTypeByServer(Os_Uso.Os_Cty);
            SetLanguageByCountryType();
            BizzaEventSystem.Emit(EventDefine.Login.InitContentByCountry);
            loginUserInfoFinish = true;

            LogLogger.LogVerbose(LogTag.LOG_Account, $"+++ OceanShineUserInfoResponse +++");
#if BIZZA_HTTP_AD
            Request_WithdrawalPageRequest(null, false);
#endif
            return;
        }

        if (retryUserInfoTimes > 0)
        {
            retryUserInfoTimes--;
            Request_UserInfoRequest(true, false, Response_UserInfoResponse);
            return;
        }

#if !BIZZA_HTTP_AD
        loginUserInfoFinish = true;
#endif

        MarkLoadingUserDataFailed(response);
    }

    #endregion

    #region 广告上报

    // 请求体数据模型类，包含了广告日志报告请求所需的字段
    [Serializable]
    public class OceanShineAdLogReportRequest
    {
        // 公共信息部分
        [JsonProperty("SbmCnf")]
        public CommonInfo Os_Cnf;

        // 扩展参数部分
        [JsonProperty("SbmEpm")]
        public ExtendParam Os_Epm;

        public OceanShineAdLogReportRequest()
        {
            Os_Cnf = new CommonInfo();
            Os_Epm = new ExtendParam();
        }

        public void Clear()
        {
            Os_Cnf?.Clear();
            Os_Epm?.Clear();
        }

        [Serializable]
        public class CommonInfo
        {
            [JsonProperty("SbmApid")]
            public string Os_Apid; // 应用ID

            [JsonProperty("SbmUsid")]
            public string Os_Usid; // 用户ID

            [JsonProperty("SbmVn")]
            public string Os_Vn; // 应用版本

            public void Clear()
            {
                Os_Apid = default;
                Os_Usid = default;
                Os_Vn = default;
            }
        }

        [Serializable]
        public class ExtendParam
        {
            [JsonProperty("SbmAbd")]
            public string Os_Abd; // adType

            [JsonProperty("SbmAdgp")]
            public string Os_Adgp; // adgroup

            [JsonProperty("SbmBtid")]
            public string Os_Btid; // batchId

            [JsonProperty("SbmCid")]
            public string Os_Cid; // codeId

            [JsonProperty("SbmCmp")]
            public string Os_Cmp; // campaign

            [JsonProperty("SbmCtc")]
            public string Os_Ctc; // currencyCode

            [JsonProperty("SbmEcm")]
            public string Os_Ecm; // ecpm

            [JsonProperty("SbmEvt")]
            public string Os_Evt; // event

            [JsonProperty("SbmEvtM")]
            public string Os_EvtM; // eventMsg

            [JsonProperty("SbmMbc")]
            public string Os_Mbc; // mediaCodeId

            [JsonProperty("SbmMbp")]
            public string Os_Mbp; // mediaPlatform

            [JsonProperty("SbmNbt")]
            public string Os_Nbt; // network

            [JsonProperty("SbmPtf")]
            public string Os_Ptf; // platform

            public void Clear()
            {
                Os_Abd = default;
                Os_Adgp = default;
                Os_Btid = default;
                Os_Cid = default;
                Os_Cmp = default;
                Os_Ctc = default;
                Os_Ecm = default;
                Os_Evt = default;
                Os_EvtM = default;
                Os_Mbc = default;
                Os_Mbp = default;
                Os_Nbt = default;
                Os_Ptf = default;
            }
        }

        public string ToLogString()
        {
            var sb = new StringBuilder();

            if (Os_Cnf != null)
            {
                AppendIfNotEmpty(sb, nameof(Os_Cnf.Os_Apid), Os_Cnf.Os_Apid);
                AppendIfNotEmpty(sb, nameof(Os_Cnf.Os_Usid), Os_Cnf.Os_Usid);
                AppendIfNotEmpty(sb, nameof(Os_Cnf.Os_Vn), Os_Cnf.Os_Vn);
            }

            if (Os_Epm != null)
            {
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Abd), Os_Epm.Os_Abd);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Adgp), Os_Epm.Os_Adgp);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Btid), Os_Epm.Os_Btid);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Cid), Os_Epm.Os_Cid);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Cmp), Os_Epm.Os_Cmp);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Ctc), Os_Epm.Os_Ctc);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Ecm), Os_Epm.Os_Ecm);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Evt), Os_Epm.Os_Evt);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_EvtM), Os_Epm.Os_EvtM);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Mbc), Os_Epm.Os_Mbc);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Mbp), Os_Epm.Os_Mbp);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Nbt), Os_Epm.Os_Nbt);
                AppendIfNotEmpty(sb, nameof(Os_Epm.Os_Ptf), Os_Epm.Os_Ptf);
            }

            return sb.ToString().TrimEnd(',', ' ');
        }

        private static void AppendIfNotEmpty(StringBuilder sb, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                sb.Append(key).Append(": ").Append(value).Append(", ");
            }
        }
    }

    /// <summary>
    /// 广告上报 请求（无业务返回值）
    /// </summary>
    public void Request_AdLogReportRequest(E_AdType adType, string type, string betype, string actype, string evt, string MAXadId, string ecmp,
        string Mbc, string Mbp, string id,
        Action<FailHttpResponse<OceanShineAdLogReportRequest>> callback = null)
    {
        OceanShineAdLogReportRequest request = new()
        {
            Os_Cnf = new OceanShineAdLogReportRequest.CommonInfo()
            {
                Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
                Os_Usid = PlayerPrefs.GetString(m_userIdKey),
                Os_Vn = version
            },
            Os_Epm = new()
            {
                Os_Abd = type,
                Os_Adgp = PlayerPrefs.GetString(adgroupkKey, ""),
                Os_Btid = id,
                Os_Cid = MAXadId,
                Os_Cmp = PlayerPrefs.GetString(campaignKey, ""),
                Os_Ctc = DeviceInfoUtil.deviceInfoDataForCloud.Os_Ctr.ToString(),
                Os_Ecm = ecmp, //(adInfo != null ? (float)(adInfo.Revenue *1000) : 0f).ToString(),
                Os_Evt = evt,
                Os_EvtM = "",
                Os_Mbc = Mbc, //adInfo != null ? adInfo.NetworkPlacement : "",
                Os_Mbp = Mbp, //adInfo != null ? adInfo.NetworkName : "",
                Os_Nbt = PlayerPrefs.GetString(networkKey, ""),
                Os_Ptf = "Max",
            }
        };

        bool _isAdShow = string.Equals(evt, Instance.ADShow);
        if (_isAdShow)
        {
            SaveDataUtils.GameData.totalFinishAdTimes++;
            SaveDataUtils.GameData.todayAdTimes++;
        }

        double.TryParse(ecmp, out double ecm);
        activeAdjust(ecm, type);
        string requestParams = JsonConvert.SerializeObject(request);
        LogLogger.LogVerbose(LogTag.ADReportFlow, type + "广告上报 ：" + id + " evt " + evt);
        LogLogger.LogVerbose(LogTag.ADReportFlow, ">>> OceanShineAdLogReportRequest: ");
        LogLogger.LogADPR($"{type}广告上报 ：{id} evt {evt} ecmp {ecmp} Mbc {Mbc} Mbp ;;; 具体信息 {request.ToLogString()}");

        // 无返回体
        HttpUtil.RequestToServer(AccountModuleCfg.ad_info, requestParams, callback, false);
    }

    private string ResolveAdReportPos(E_AdType adType, string betype, string actype)
    {
        if (TryResolveAdReportPos(betype, out string adPos))
        {
            return adPos;
        }

        if (TryResolveAdReportPos(actype, out adPos))
        {
            return adPos;
        }

        return adType == E_AdType.RewardAd ? BizzaSdk.Ad.curRewardAdPos : BizzaSdk.Ad.curInterAdPos;
    }

    private bool TryResolveAdReportPos(string adType, out string adPos)
    {
        if (string.Equals(adType, AdType.Reward.ToString(), StringComparison.Ordinal))
        {
            adPos = BizzaSdk.Ad.curRewardAdPos;
            return true;
        }

        if (string.Equals(adType, AdType.Interstitial.ToString(), StringComparison.Ordinal))
        {
            adPos = BizzaSdk.Ad.curInterAdPos;
            return true;
        }

        adPos = "";
        return false;
    }

    public void activeAdjust(double ecpm, string eventName)
    {
        var userEcpm = m_Temp_ACM;
        var userIpu = m_Temp_AIU;
        var userShowEcpm = m_Temp_ShowECPM; // 最开始本地是一个 0
        var adShowTimes = SaveDataUtils.GameData.totalFinishAdTimes;
        LogLogger.LogAttribute($"激活归因 : adShowTimes:{adShowTimes}; userEcpm:{userEcpm}; userIpu:{userIpu}; userShowEcpm:{userShowEcpm}; ecpm:{ecpm}");
        if (ecpm >= userShowEcpm) // 当前广告的ECPM 》 本地的ECPM
        {
            LogLogger.LogAttribute($"激活归因 进入 ecpm >= userShowEcpm : {ecpm >= userShowEcpm}");
            userShowEcpm = ecpm;
            m_Temp_ShowECPM = ecpm;
        }

        if (0.0 != userEcpm && 0.0 == userIpu) // ECPM不为0 且 看广告次数为0
        {
            LogLogger.LogAttribute($"激活归因 进入 0.0 != userEcpm && 0.0 == userIpu : {0.0 != userEcpm && 0.0 == userIpu}");
            if (userShowEcpm >= userEcpm)// 本地 大于等于 服务器ECPm
            {
                LogLogger.LogAttribute($"激活归因 进入 0.0 != userEcpm && 0.0 == userIpu : {userShowEcpm >= userEcpm}");
                AdjustAttributionAdapter.AttributeStart(eventName);
            }
        }
        else if (0.0 == userEcpm && 0.0 != userIpu)
        {
            LogLogger.LogAttribute($"激活归因 进入 0.0 == userEcpm && 0.0 != userIpu : {0.0 == userEcpm && 0.0 != userIpu}");
            if (adShowTimes >= userIpu)
            {
                LogLogger.LogAttribute($"激活归因 进入 0.0 == userEcpm && 0.0 != userIpu : {adShowTimes >= userIpu}");
                AdjustAttributionAdapter.AttributeStart(eventName);
            }
        }
        else
        {
            LogLogger.LogAttribute($"激活归因 都没有进入:");
            if (userShowEcpm >= userEcpm && adShowTimes >= userIpu)
            {
                LogLogger.LogAttribute($"userShowEcpm >= userEcpm && adShowTimes >= userIpu");
                AdjustAttributionAdapter.AttributeStart(eventName);
            }
        }
    }

    #endregion

    #region 日志上报

    // 请求体数据模型类，包含了日志上报请求所需的字段
    [Serializable]
    public class OceanShineAppEventReportRequest
    {
        [JsonProperty("SbmCnf")]
        public CommonInfo Os_Cnf; // 公共信息部分

        [JsonProperty("SbmEpm")]
        public ExtendParam Os_Epm; // 扩展参数部分

        [Serializable]
        public class CommonInfo
        {
            [JsonProperty("SbmApid")]
            public string Os_Apid; // 应用ID

            [JsonProperty("SbmUsid")]
            public string Os_Usid; // 用户ID

            [JsonProperty("SbmVn")]
            public string Os_Vn; // 应用版本
        }

        [Serializable]
        public class ExtendParam
        {
            [JsonProperty("SbmBgd")]
            public string Os_Bgd; // 页面ID

            [JsonProperty("SbmEvt")]
            public string Os_Evt; // event

            [JsonProperty("SbmEvtE")]
            public string Os_EvtE; // eventExt
        }


    }

    public OceanShineAppEventReportRequest GetLogRequest(string bgd, string evt, string evtE)
    {
        OceanShineAppEventReportRequest request = new();
        request.Os_Cnf = new OceanShineAppEventReportRequest.CommonInfo()
        {
            Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
            Os_Usid = PlayerPrefs.GetString(m_userIdKey, "NoUid"),
            Os_Vn = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Vn,
        };
        request.Os_Epm = new OceanShineAppEventReportRequest.ExtendParam()
        {
            Os_Bgd = bgd,
            Os_Evt = evt,
            Os_EvtE = evtE,
        };
        return request;
    }

    /// <summary>
    /// 日志上报 请求（无业务返回值）
    /// </summary>
    public void Request_AppEventReportRequest(OceanShineAppEventReportRequest request,
Action<FailHttpResponse<OceanShineAppEventReportRequest>> callback = null)
    {
        string requestParams = JsonConvert.SerializeObject(request);
        // 无返回体
        HttpUtil.RequestToServer(AccountModuleCfg.app_info, requestParams, callback);
    }

    #endregion

    #endregion
}

#endif

