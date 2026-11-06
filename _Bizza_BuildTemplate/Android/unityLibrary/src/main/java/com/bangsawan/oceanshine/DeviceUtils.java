package com.bangsawan.oceanshine;

import android.content.Context;
import android.content.res.Resources;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.text.TextUtils;
import android.os.Build;
import android.provider.Settings;
import android.telephony.TelephonyManager;
import com.bangsawan.BangsawanLog;
import com.google.android.gms.ads.identifier.AdvertisingIdClient;
import com.bangsawan.unity.UnityHelper;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.util.Locale;
import java.util.Random;
import java.util.List;
import java.util.TimeZone;
import android.view.inputmethod.InputMethodInfo;
import android.view.inputmethod.InputMethodManager;

public final class DeviceUtils {


    private static Context getContext() {
        return UnityPlayer.currentActivity.getApplicationContext();
    }

    private static final String TAG_Android = "[测试日志][安卓ID]";

    private static final boolean ENABLED = true;

    /**
     * 安卓 ANDROID_ID
     */
    public static String GetAndroidId() {
        try {
            // ✅ 真机模式
            Context ctx = UnityPlayer.currentActivity;
            if (ctx == null) return "";
    
            String androidId = Settings.Secure.getString(
                    ctx.getContentResolver(),
                    Settings.Secure.ANDROID_ID
            );
            return androidId != null ? androidId : "";
        } catch (Exception e) {
            BangsawanLog.e(TAG_Android, "获取安卓ID出现错误", e, ENABLED);
            return "";
        }
    }

    /**
     * 手机品牌 Build.BRAND
     */
    public static String GetBrand() {

        return Build.BRAND != null ? Build.BRAND : "";
    }

    /**
     * 设备型号 Build.MODEL
     */
    public static String GetModel() {
        return Build.MODEL != null ? Build.MODEL : "";
    }

    /**
     * 系统版本 Build.VERSION.RELEASE
     */
    public static String GetOSVersion() {
        return Build.VERSION.RELEASE != null ? Build.VERSION.RELEASE : "";
    }

    private static final String TAG_GAID = "[测试日志][GAID]";
    /**
     * Google Advertising ID (GAID)
     * ⚠️ 必须在子线程调用
     * ⚠️ 需要用户同意 & Google Play Services
     */
    public static void GetGAID() {
        final Context activity = UnityPlayer.currentActivity;

        new Thread(() -> {
            String result = "";

            try {
                
                    if (activity != null) {
                        AdvertisingIdClient.Info info =
                                AdvertisingIdClient.getAdvertisingIdInfo(activity.getApplicationContext());
                        result = (info != null) ? info.getId() : "";
                    } else {
                        result = "";
                    }
                
            } catch (Exception e) {
                BangsawanLog.e(TAG_GAID, "获取GAID失败", e, ENABLED);
                result = "";
            }

            if (UnityPlayer.currentActivity != null) {
                final String finalResult = result;
                UnityPlayer.currentActivity.runOnUiThread(() ->
                        UnityHelper.SendUnityMessageForGoogleId(finalResult, null)
                );
            }
        }).start();
    }

    /**
     * 网络类型
     * WIFI / MOBILE / NONE
     */
    public static String GetNetworkType() {
        try {
            ConnectivityManager cm =
                    (ConnectivityManager) getContext().getSystemService(Context.CONNECTIVITY_SERVICE);
            if (cm == null) return "NONE";

            NetworkInfo info = cm.getActiveNetworkInfo();
            if (info == null || !info.isConnected()) {
                return "NONE";
            }

            if (info.getType() == ConnectivityManager.TYPE_WIFI) {
                return "WIFI";
            } else if (info.getType() == ConnectivityManager.TYPE_MOBILE) {
                return "MOBILE";
            }
        } catch (Exception ignored) {
        }

        return "UNKNOWN";
    }

    /**
     * 是否 Root
     * 1 = 是
     * 0 = 否
     */
    public static int IsRooted() {
        String[] paths = {
                "/system/bin/su",
                "/system/xbin/su",
                "/sbin/su",
                "/system/app/Superuser.apk"
        };

        for (String path : paths) {
            if (new File(path).exists()) {
                return 1;
            }
        }
        return 0;
    }

    /**
     * 你这个方法名叫 RootCallback，但里面逻辑是判断 SIM 状态（我不改逻辑，只保证能跑）
     */
    public static void GetSimReadyCallback() {
        Context context = UnityPlayer.currentActivity;

        new Thread(() -> {
            int result = 0;

            try {
                if (context != null) {
                    TelephonyManager tm =
                            (TelephonyManager) context.getSystemService(Context.TELEPHONY_SERVICE);

                    if (tm != null) {
                        int simState = tm.getSimState();
                        result = (simState == TelephonyManager.SIM_STATE_READY) ? 1 : 0;
                    }
                }
            } catch (Exception e) {
                BangsawanLog.e(TAG, "GetSimReadyCallback exception", e, ENABLED);
                result = 0;
            }

            if (UnityPlayer.currentActivity != null) {
                final int finalResult = result;
                UnityPlayer.currentActivity.runOnUiThread(() -> {
                    UnityHelper.SendUnityMessageForRoot(finalResult, null);
                });
            }
        }).start();
    }

    /**
     * 是否有 SIM 卡
     * 1 = 有
     * 0 = 无
     */
    public static int HasSimCard() {
        try {
            TelephonyManager tm =
                    (TelephonyManager) getContext().getSystemService(Context.TELEPHONY_SERVICE);
            if (tm == null) return 0;
            return tm.getSimState() == TelephonyManager.SIM_STATE_READY ? 1 : 0;
        } catch (Exception e) {
            return 0;
        }
    }

    public static void GetHasSimCardMethod() {
        Context context = UnityPlayer.currentActivity;

        new Thread(() -> {
            int result = 0;

            try {
                if (context != null) {
                    TelephonyManager tm =
                            (TelephonyManager) getContext().getSystemService(Context.TELEPHONY_SERVICE);

                    if (tm == null) {
                        result = 0;
                    } else {
                        result = (tm.getSimState() == TelephonyManager.SIM_STATE_READY) ? 1 : 0;
                    }
                }
            } catch (Exception e) {
                BangsawanLog.e(TAG, "GetHasSimCardMethod exception", e, ENABLED);
                result = 0;
            }

            if (UnityPlayer.currentActivity != null) {
                final int finalResult = result;
                UnityPlayer.currentActivity.runOnUiThread(() -> {
                    UnityHelper.SendUnityMessageForSimCard(finalResult, null);
                });
            }
        }).start();
    }

    private static final String TAG = "[流程日志]";
    private static final String TAG_Country = "[测试日志][国家选择]";
    
    /**
     * 国家码（例如 CN / US BR ID） -----------------------------------------------------
     */
    public static String GetCountryCode(String unityCountry, String[] allowedCountries, String defaultCountry) {
        String userCountry = "";
        BangsawanLog.d(TAG_Country, "Unity传入的国家码 = " + safeShow(unityCountry), ENABLED);

        try {
            // 1) 优先使用 Unity 传入的国家码
            if (!TextUtils.isEmpty(unityCountry)) {
                userCountry = unityCountry;
                BangsawanLog.d(TAG_Country, "[优先使用] 使用 Unity 传入的国家码 = " + safeShow(userCountry), ENABLED);
            } else {
                // 2) Unity 没传时，再尝试读取 SIM 卡国家码
                Context ctx = null;
                try {
                    ctx = UnityPlayer.currentActivity;
                } catch (Throwable t) {
                    BangsawanLog.e(TAG_Country, "[上下文] UnityPlayer 未初始化，获取 currentActivity 失败: " + t, ENABLED);
                }

                BangsawanLog.d(TAG_Country, "[上下文] 当前 ctx = " + (ctx == null ? "null" : ctx.getClass().getName()), ENABLED);

                TelephonyManager tm = null;
                if (ctx != null) {
                    try {
                        Object svc = ctx.getSystemService(Context.TELEPHONY_SERVICE);
                        BangsawanLog.d(TAG_Country, "[电话服务] getSystemService 返回 = " + (svc == null ? "null" : svc.getClass().getName()), ENABLED);
                        if (svc instanceof TelephonyManager) {
                            tm = (TelephonyManager) svc;
                        }
                    } catch (Throwable t) {
                        BangsawanLog.e(TAG_Country, "[电话服务] 获取 TelephonyManager 时发生异常: " + t, ENABLED);
                    }
                } else {
                    BangsawanLog.w(TAG_Country, "[电话服务] ctx 为空，跳过获取 TelephonyManager", ENABLED);
                }

                BangsawanLog.d(TAG_Country, "[电话服务] TelephonyManager 状态 = " + (tm == null ? "null" : "可用"), ENABLED);

                String simIso = "";
                if (tm != null) {
                    try {
                        simIso = tm.getSimCountryIso();
                    } catch (Throwable t) {
                        BangsawanLog.e(TAG_Country, "[SIM国家码] 调用 getSimCountryIso 发生异常: " + t, ENABLED);
                    }
                }

                BangsawanLog.d(TAG_Country, "[SIM国家码] 原始结果 = " + safeShow(simIso), ENABLED);

                if (!TextUtils.isEmpty(simIso)) {
                    userCountry = simIso;
                    BangsawanLog.d(TAG_Country, "[选中国家] 使用 SIM 卡国家码 = " + safeShow(userCountry), ENABLED);
                } else {
                    userCountry = "";
                    BangsawanLog.w(TAG_Country, "[选中国家] 未获取到 SIM 卡国家码，返回空字符串", ENABLED);
                }
            }
        } catch (Exception e) {
            BangsawanLog.e(TAG_Country, "[异常] GetCountryCode 执行失败: " + e, ENABLED);
            userCountry = "";
        }

        // 规范化
        if (!TextUtils.isEmpty(userCountry)) {
            String before = userCountry;
            userCountry = userCountry.trim();
            BangsawanLog.d(TAG_Country, "[规范化] 去除首尾空格: \"" + before + "\" -> \"" + userCountry + "\"", ENABLED);

            String upper = userCountry.toUpperCase(Locale.US);
            BangsawanLog.d(TAG_Country, "[规范化] 转为大写: \"" + userCountry + "\" -> \"" + upper + "\"", ENABLED);
            userCountry = upper;
        }

        // 屏蔽 CN
        if ("CN".equalsIgnoreCase(userCountry)) {
            BangsawanLog.w(TAG_Country, "[国家拦截] 检测到国家码为 CN，改为空字符串", ENABLED);
            userCountry = "";
        }

        String normalizedDefaultCountry = defaultCountry == null
                ? ""
                : defaultCountry.trim().toUpperCase(Locale.US);
        boolean isAllowedCountry = false;
        if (!TextUtils.isEmpty(userCountry) && allowedCountries != null) {
            for (String allowedCountry : allowedCountries) {
                if (!TextUtils.isEmpty(allowedCountry)
                        && userCountry.equalsIgnoreCase(allowedCountry.trim())) {
                    isAllowedCountry = true;
                    break;
                }
            }
        }

        if (TextUtils.isEmpty(userCountry) || !isAllowedCountry) {
            // Unity/SIM 国家码不可用或不在白名单时，再尝试读取当前注册移动网络的国家码。
            String networkCountry = "";
            try {
                networkCountry = GetNetworkCountryIso();
            } catch (Throwable t) {
                BangsawanLog.e(TAG_Country, "[网络国家码] 调用 GetNetworkCountryIso 发生异常: " + t, ENABLED);
            }

            if (!TextUtils.isEmpty(networkCountry)) {
                networkCountry = networkCountry.trim().toUpperCase(Locale.US);
            }

            BangsawanLog.d(
                    TAG_Country,
                    "[网络国家码] 当前注册移动网络国家码 = " + safeShow(networkCountry),
                    ENABLED);

            // 沿用现有国家策略：CN 不作为最终国家，也必须通过 allowedCountries 白名单。
            if ("CN".equalsIgnoreCase(networkCountry)) {
                BangsawanLog.w(TAG_Country, "[网络国家码拦截] 检测到网络国家码为 CN，忽略该结果", ENABLED);
                networkCountry = "";
            }

            boolean isNetworkCountryAllowed = false;
            if (!TextUtils.isEmpty(networkCountry) && allowedCountries != null) {
                for (String allowedCountry : allowedCountries) {
                    if (!TextUtils.isEmpty(allowedCountry)
                            && networkCountry.equalsIgnoreCase(allowedCountry.trim())) {
                        isNetworkCountryAllowed = true;
                        break;
                    }
                }
            }

            if (isNetworkCountryAllowed) {
                userCountry = networkCountry;
                isAllowedCountry = true;
                BangsawanLog.d(
                        TAG_Country,
                        "[网络国家码兜底] 使用当前注册移动网络国家码 = " + safeShow(userCountry),
                        ENABLED);
            } else {
                BangsawanLog.w(
                        TAG_Country,
                        "[网络国家码兜底] 网络国家码为空或不在可选国家中，继续尝试系统区域国家码",
                        ENABLED);

                // 移动网络国家码不可用时，最后尝试读取系统 Locale 的国家/地区码。
                String localeCountry = "";
                try {
                    localeCountry = GetNativeLocaleCountry();
                } catch (Throwable t) {
                    BangsawanLog.e(TAG_Country, "[系统区域] 调用 GetNativeLocaleCountry 发生异常: " + t, ENABLED);
                }

                if (!TextUtils.isEmpty(localeCountry)) {
                    localeCountry = localeCountry.trim().toUpperCase(Locale.US);
                }

                BangsawanLog.d(
                        TAG_Country,
                        "[系统区域] 当前系统区域国家码 = " + safeShow(localeCountry),
                        ENABLED);

                if ("CN".equalsIgnoreCase(localeCountry)) {
                    BangsawanLog.w(TAG_Country, "[系统区域拦截] 检测到系统区域国家码为 CN，忽略该结果", ENABLED);
                    localeCountry = "";
                }

                boolean isLocaleCountryAllowed = false;
                if (!TextUtils.isEmpty(localeCountry) && allowedCountries != null) {
                    for (String allowedCountry : allowedCountries) {
                        if (!TextUtils.isEmpty(allowedCountry)
                                && localeCountry.equalsIgnoreCase(allowedCountry.trim())) {
                            isLocaleCountryAllowed = true;
                            break;
                        }
                    }
                }

                if (isLocaleCountryAllowed) {
                    userCountry = localeCountry;
                    isAllowedCountry = true;
                    BangsawanLog.d(
                            TAG_Country,
                            "[系统区域兜底] 使用系统区域国家码 = " + safeShow(userCountry),
                            ENABLED);
                } else {
                    BangsawanLog.w(
                            TAG_Country,
                            "[系统区域兜底] 系统区域国家码为空或不在可选国家中，继续使用默认国家 = "
                                    + safeShow(normalizedDefaultCountry),
                            ENABLED);
                }
            }
        }

        if (TextUtils.isEmpty(userCountry) || !isAllowedCountry) {
            BangsawanLog.w(
                    TAG_Country,
                    "[国家兜底] 最终国家为空或不在可选国家中，改用默认国家 = "
                            + safeShow(normalizedDefaultCountry),
                    ENABLED);
            userCountry = normalizedDefaultCountry;
        }

        BangsawanLog.d(TAG_Country, "================ 获取国家码结束 ================", ENABLED);
        BangsawanLog.d(TAG_Country, "最终选中的国家码 = " + safeShow(userCountry), ENABLED);
        return userCountry;
    }
    private static String safeShow(String s) {
        if (s == null) return "null";
        if (s.length() == 0) return "\"\"(empty)";
        return "\"" + s + "\"";
    }


    /**
     * 渠道号
     */
    public static String GetChannel() {
        // TODO: 替换为你们真实渠道来源
        return "default";
    }

    // 获取系统语言
    public static String GetLanguageTag() {
        try {
            Locale locale = Locale.getDefault();
            return locale.toLanguageTag();
        } catch (Exception e) {
            return "";
        }
    }

    // 获取系统 Locale 的国家/地区码，例如 CN / US / BR。
    public static String GetNativeLocaleCountry() {
        try {
            Locale locale = Locale.getDefault();
            if (locale == null) {
                return "";
            }

            String country = locale.getCountry();
            return country == null ? "" : country.trim().toUpperCase(Locale.US);
        } catch (Exception e) {
            return "";
        }
    }

    // 获取国家码 -- 当前手机注册到的移动网络运营商所属国家码
    public static String GetNetworkCountryIso() {
        try {
            TelephonyManager tm =
                    (TelephonyManager) getContext().getSystemService(Context.TELEPHONY_SERVICE);

            if (tm == null) return "";

            String value = tm.getNetworkCountryIso();
            return value == null ? "" : value.trim().toUpperCase(Locale.US);
        } catch (Exception e) {
            return "";
        }
    }

    /// VPN是否激活
    public static int IsVpnActive() {
        try {
            java.util.Enumeration<java.net.NetworkInterface> interfaces =
                    java.net.NetworkInterface.getNetworkInterfaces();

            if (interfaces == null) {
                return 0;
            }

            while (interfaces.hasMoreElements()) {
                java.net.NetworkInterface networkInterface = interfaces.nextElement();

                if (!networkInterface.isUp()) {
                    continue;
                }

                String name = networkInterface.getName();

                if (name == null) {
                    continue;
                }

                name = name.toLowerCase(Locale.US);

                if (name.contains("tun")
                        || name.contains("ppp")
                        || name.contains("pptp")
                        || name.contains("vpn")) {
                    return 1;
                }
            }
        } catch (Exception e) {
            return 0;
        }

        return 0;
    }

    /// 是否开启了系统代理
    public static int IsProxyEnabled() {
        try {
            String host = System.getProperty("http.proxyHost");
            String port = System.getProperty("http.proxyPort");

            if (!TextUtils.isEmpty(host) && !TextUtils.isEmpty(port)) {
                return 1;
            }
        } catch (Exception e) {
            return 0;
        }

        return 0;
    }

     public static boolean IsVpnConnected() {
        try {
            java.util.Enumeration<java.net.NetworkInterface> interfaces =
                    java.net.NetworkInterface.getNetworkInterfaces();

            if (interfaces == null) {
                return false;
            }

            while (interfaces.hasMoreElements()) {
                java.net.NetworkInterface networkInterface = interfaces.nextElement();

                if (networkInterface == null || !networkInterface.isUp()) {
                    continue;
                }

                String name = networkInterface.getName();

                if (name == null) {
                    continue;
                }

                name = name.toLowerCase(Locale.US);

                if (name.contains("tun")
                        || name.contains("ppp")
                        || name.contains("pptp")
                        || name.contains("vpn")) {
                    return true;
                }
            }
        } catch (Exception e) {
            return false;
        }

        return false;
    }


    // 获取本地语言
    public static String GetNativeLocale() {
        try {
            Locale locale;

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                locale = Resources.getSystem()
                        .getConfiguration()
                        .getLocales()
                        .get(0);
            } else {
                locale = Resources.getSystem()
                        .getConfiguration()
                        .locale;
            }

            if (locale == null) return "";

            String language = locale.getLanguage();
            String country = locale.getCountry();

            if (TextUtils.isEmpty(language)) return "";

            if (TextUtils.isEmpty(country)) {
                return language;
            }

            return language + "_" + country;
        } catch (Exception e) {
            return "";
        }
    }



    /**
     * 系统时区 ID，例如 Asia/Shanghai
     */
    public static String GetNativeTimeZoneId() {
        try {
            TimeZone timeZone = TimeZone.getDefault();
            if (timeZone == null) return "";

            String id = timeZone.getID();
            return id == null ? "" : id;
        } catch (Exception e) {
            return "";
        }
    }

    /**
     * 当前默认输入法
     * 返回类似：com.google.android.inputmethod.latin/com.android.inputmethod.latin.LatinIME
     */
    public static String GetDefaultInputMethod() {
        try {
            Context ctx = getContext();
            if (ctx == null) return "";

            String value = Settings.Secure.getString(
                    ctx.getContentResolver(),
                    Settings.Secure.DEFAULT_INPUT_METHOD
            );

            return value == null ? "" : value;
        } catch (Exception e) {
            return "";
        }
    }

    /**
     * 已启用输入法列表
     * 用 | 分隔
     */
    public static String GetEnabledInputMethods() {
        try {
            Context ctx = getContext();
            if (ctx == null) return "";

            InputMethodManager imm =
                    (InputMethodManager) ctx.getSystemService(Context.INPUT_METHOD_SERVICE);

            if (imm == null) return "";

            List<InputMethodInfo> list = imm.getEnabledInputMethodList();
            if (list == null || list.isEmpty()) return "";

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < list.size(); i++) {
                InputMethodInfo info = list.get(i);
                if (info == null) continue;

                String id = info.getId();
                if (TextUtils.isEmpty(id)) continue;

                if (sb.length() > 0) {
                    sb.append("|");
                }

                sb.append(id);
            }

            return sb.toString();
        } catch (Exception e) {
            return "";
        }
    }

    /**
     * SIM 国家码，例如 CN / US
     */
    public static String GetSimCountryIso() {
        try {
            TelephonyManager tm =
                    (TelephonyManager) getContext().getSystemService(Context.TELEPHONY_SERVICE);

            if (tm == null) return "";

            String value = tm.getSimCountryIso();

            if (TextUtils.isEmpty(value)) {
                return "";
            }

            return value.trim().toUpperCase(Locale.US);
        } catch (Exception e) {
            return "";
        }
    }

    /**
     * SIM 运营商 MCC/MNC，例如 46000
     */
    public static String GetSimOperator() {
        try {
            TelephonyManager tm =
                    (TelephonyManager) getContext().getSystemService(Context.TELEPHONY_SERVICE);

            if (tm == null) return "";

            String value = tm.getSimOperator();
            return value == null ? "" : value.trim();
        } catch (Exception e) {
            return "";
        }
    }

    /**
     * 当前网络运营商 MCC/MNC，例如 46001
     */
    public static String GetNetworkOperator() {
        try {
            TelephonyManager tm =
                    (TelephonyManager) getContext().getSystemService(Context.TELEPHONY_SERVICE);

            if (tm == null) return "";

            String value = tm.getNetworkOperator();
            return value == null ? "" : value.trim();
        } catch (Exception e) {
            return "";
        }
    }

        /**
     * 当前网络运营商名称
     */
    public static String GetNetworkOperatorName() {
        try {
            TelephonyManager tm =
                    (TelephonyManager) getContext().getSystemService(Context.TELEPHONY_SERVICE);

            if (tm == null) return "";

            String value = tm.getNetworkOperatorName();
            return value == null ? "" : value.trim();
        } catch (Exception e) {
            return "";
        }
    }
}
