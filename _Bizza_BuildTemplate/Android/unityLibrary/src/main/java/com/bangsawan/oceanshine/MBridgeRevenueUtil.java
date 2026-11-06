package com.bangsawan.oceanshine;

import android.content.Context;
import android.text.TextUtils;
import com.bangsawan.BangsawanLog;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.regex.Matcher;
import java.util.regex.Pattern;

import com.mbridge.msdk.out.reveue.MBridgeRevenueParamsEntityForMax;
import com.mbridge.msdk.out.reveue.MBridgeRevenueManager;

public final class MBridgeRevenueUtil {

    private static Context sApplicationContext;
    private static final String TAG = "[测试日志][归因][MTG]";
    private static final boolean ENABLED = true;

    /**
     * 辅助方法：用正则提取 C# ToString() 里的键值（修复：之前遗漏了这个方法）
     */
    private static String extractValue(String source, String regex) {
        if (TextUtils.isEmpty(source)) return "";
        try {
            Matcher m = Pattern.compile(regex).matcher(source);
            if (m.find()) {
                return m.group(1).trim();
            }
        } catch (Exception e) {
            // ignore
        }
        return "";
    }

    /**
     * 在 Application 或 UnityPlayerActivity 里调用一次
     */
    public static void init(Context context) {
        try {
            BangsawanLog.i(TAG, "初始化 ---- MBridgeRevenueManager", ENABLED);
            if (context != null) {
                sApplicationContext = context.getApplicationContext();
                BangsawanLog.d(TAG, "初始化 ---- context.getApplicationContext", ENABLED);
            }
        } catch (Exception e) {
            BangsawanLog.e(TAG, "初始化 TMG 失败", e, ENABLED);
        }
    }

    /**
     * Unity 调用的方法
     */
    public static void OnAdRevenuePaid(
            String ATTRIBUTION_PLATFORM,
            String adid,
            String maxAdInfo,
            String waterfallInfo,
            double revenue,
            String revenuePrecision) {

        if (sApplicationContext == null) {
            BangsawanLog.d(TAG, "未初始化 Context ---- MBridgeRevenueManager", ENABLED);
            return;
        }

        try {
            // 1. 构建 Mintegral Revenue Entity
            MBridgeRevenueParamsEntityForMax entity = new MBridgeRevenueParamsEntityForMax(
                    ATTRIBUTION_PLATFORM,
                    adid);

            // ==========================================
            // 2. 核心适配层：把 Unity 的字符串转换为 Mintegral 能懂的 JSON
            // ==========================================
            String adaptedAdInfo = maxAdInfo;
            String adaptedWaterfallInfo = waterfallInfo;

            try {
                // 解析 maxAdInfo
                JSONObject adJson = new JSONObject();
                // 提取 C# 中的字段名，并映射为 Mintegral 需要的字段名
                adJson.put("adUnitId", extractValue(maxAdInfo, "adUnitIdentifier:\\s*([^,\\]]+)"));
                adJson.put("format", extractValue(maxAdInfo, "adFormat:\\s*([^,\\]]+)"));
                String networkName = extractValue(maxAdInfo, "networkName:\\s*([^,\\]]+)");
                adJson.put("networkName", networkName);
                
                adaptedAdInfo = adJson.toString();

                // 解析 WaterfallInfo: Mintegral 只关心谁赢了竞价 (AD_LOADED) 及其 credentials
                JSONObject waterfallJson = new JSONObject();
                JSONArray networkResponses = new JSONArray();
                JSONObject loadedResponse = new JSONObject();
                
                loadedResponse.put("adLoadState", "AD_LOADED");
                loadedResponse.put("isBidding", false); // 如果你有确切的 isBidding 状态可以通过参数传过来，否则默认 false
                
                JSONArray credentials = new JSONArray();
                JSONObject cred = new JSONObject();
                cred.put("networkName", networkName);
                credentials.put(cred);
                
                loadedResponse.put("credentials", credentials);
                networkResponses.put(loadedResponse);
                waterfallJson.put("networkResponses", networkResponses);
                
                adaptedWaterfallInfo = waterfallJson.toString();

            } catch (Exception e) {
                BangsawanLog.e(TAG, "JSON Adapter 转换异常，回退使用原始数据", e, ENABLED);
            }

            // 3. 传入转换后干净的 JSON 字符串
            entity.setMaxAdInfo(adaptedAdInfo, adaptedWaterfallInfo);

            // 4. 收益信息
            entity.setMaxRevenueInfo(revenuePrecision, revenue);

            String logMessage1 = "成功上报 ---- MBridgeRevenueManager | OnAdRevenuePaid reported success"
                    + " | adaptedAdInfo=" + adaptedAdInfo
                    + " | adaptedWaterfallInfo=" + adaptedWaterfallInfo
                    + " | revenuePrecision=" + revenuePrecision
                    + " | revenue=" + revenue;

            BangsawanLog.d(TAG, logMessage1, ENABLED);

            // 5. 上报
            MBridgeRevenueManager.track(sApplicationContext, entity);

            // 6. 确认日志
            String logMessage = "成功上报 ---- MBridgeRevenueManager | OnAdRevenuePaid reported success"
                    + " | platform=" + ATTRIBUTION_PLATFORM
                    + " | adid=" + adid
                    + " | revenue=" + revenue
                    + " | precision=" + revenuePrecision;

            BangsawanLog.d(TAG, logMessage, ENABLED);

        } catch (Throwable t) {
            BangsawanLog.e(TAG, "OnAdRevenuePaid exception", t, ENABLED);
        }
    }
}
