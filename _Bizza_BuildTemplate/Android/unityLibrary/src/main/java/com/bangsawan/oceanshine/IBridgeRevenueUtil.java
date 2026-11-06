package com.bangsawan.oceanshine;

import android.content.Context;
import android.text.TextUtils;
import androidx.annotation.Nullable;

import com.bangsawan.BangsawanLog;
import com.inmobi.sdk.InMobiSdk;
import com.inmobi.sdk.SdkInitializationListener;

import org.json.JSONObject;

import java.util.HashMap;
import java.util.Map;

public final class IBridgeRevenueUtil {

    private static Context sApplicationContext;
    private static final String TAG = "[测试日志][归因][InMobi]";
    private static boolean sInMobiInitialized = false;

    private static final boolean ENABLED = true;

    private static final ThreadLocal<Map<String, Object>> signalsThreadLocal =
            new ThreadLocal<Map<String, Object>>() {
                @Override
                protected Map<String, Object> initialValue() {
                    return new HashMap<>();
                }
            };

    /**
     * 在 Application 或 UnityPlayerActivity 里调用一次
     */
    public static void init(Context context) {
        try {
            BangsawanLog.i(TAG, " 初始化", ENABLED);

            if (context == null) {
                BangsawanLog.e(TAG, " 初始化失败 - 代码流程失败", ENABLED);
                return;
            }

            sApplicationContext = context.getApplicationContext();
            BangsawanLog.d(TAG, "初始化 ---- InMobi 成功", ENABLED);

        } catch (Exception e) {
            BangsawanLog.e(TAG, "Init exception", e, ENABLED);
        }
    }

    /**
     * Unity 调用的方法：接收 json
     */
    public static void OnAdRevenuePaidJson(String adInfoJson) {

        if (sApplicationContext == null) {
            BangsawanLog.d(TAG, " Context 未初始化 --InMobi-- ", ENABLED);
            return;
        }

        if (TextUtils.isEmpty(adInfoJson)) {
            BangsawanLog.d(TAG, "adInfoJson is empty", ENABLED);
            return;
        }

        try {
            JSONObject jsonObject = new JSONObject(adInfoJson);

            double revenue = jsonObject.optDouble("dir_r", 0.0d);
            String networkName = jsonObject.optString("dir_nn", "");
            String adType = jsonObject.optString("dir_type", "");

            Map<String, Object> signals = signalsThreadLocal.get();
            signals.clear();

            signals.put("dir_r", revenue);
            signals.put("dir_nn", networkName);
            signals.put("dir_type", adType);

            InMobiSdk.PublisherSignals.INSTANCE.putPublisherSignals(signals);

            BangsawanLog.d(TAG, "InMobi 成功上报 ---- json=" + adInfoJson
                    + " | dir_r=" + revenue
                    + " | dir_nn=" + networkName
                    + " | dir_type=" + adType, ENABLED);

        } catch (Throwable t) {
            BangsawanLog.e(TAG, "OnAdRevenuePaidJson exception", t, ENABLED);
        }
    }
}
