package com.bangsawan.oceanshine;

import com.bangsawan.BangsawanLog;
import com.bangsawan.unity.UnityHelper;

import java.io.IOException;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.Headers;
import okhttp3.MediaType;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.Response;

public final class HttpUtil {

    /* ================= 日志 ================= */

    private static final String TAG = "[流程日志][HTTP]";

    private static String now() {
        return String.valueOf(System.currentTimeMillis());
    }

    private static String thread() {
        return Thread.currentThread().getName();
    }

    /* ================= 基础配置 ================= */

    private static String BUNDLE = "";
    private static String VERSION_NAME = "";
    private static String VERSION_CODE = "";

    private static final String HTTPCallBack = "DispatchHTTPMessage";

    private static final MediaType JSON =
            MediaType.parse("application/json; charset=utf-8");

    private static final OkHttpClient HTTP_CLIENT =
            new OkHttpClient.Builder()
                    .connectTimeout(30, TimeUnit.SECONDS)
                    .readTimeout(30, TimeUnit.SECONDS)
                    .writeTimeout(30, TimeUnit.SECONDS)
                    .build();

    /* ================= 延迟发送调度器 ================= */

    private static final ScheduledExecutorService DELAY_EXECUTOR =
            Executors.newSingleThreadScheduledExecutor();

        private static final boolean ENABLED = false;

    private HttpUtil() {}

    /* ================= 初始化 ================= */

    public static void initialize(
            String bundle,
            String versionName,
            String versionCode
    ) {
        BUNDLE = bundle != null ? bundle : "";
        VERSION_NAME = versionName != null ? versionName : "";
        VERSION_CODE = versionCode != null ? versionCode : "";

        BangsawanLog.d(TAG,
                "[INIT] bundle=" + BUNDLE +
                        " | versionName=" + VERSION_NAME +
                        " | versionCode=" + VERSION_CODE
        , ENABLED);
    }

    /* ================= Header ================= */

    private static Headers buildHeaders() {
        return new Headers.Builder()
                .add("X-Bundle", BUNDLE)
                .add("VERSION_NAME", VERSION_NAME)
                .add("VERSION_CODE", VERSION_CODE)
                .build();
    }

    /* ================= Request 构建 ================= */

    private static Request buildPostRequest(String url, byte[] bodyRaw) {
        RequestBody body = RequestBody.create(bodyRaw, JSON);

        return new Request.Builder()
                .url(url)
                .headers(buildHeaders())
                .post(body)
                .build();
    }

    /* ================= Unity 回调 ================= */

    private static void sendSuccess(int id, int httpCode, String body) {

        BangsawanLog.d(TAG,
                "[UNITY-CALLBACK-成功] id=" + id +
                        " | code=" + httpCode +
                        " | dataLength=" + (body != null ? body.length() : 0)
       , ENABLED );

        Map<String, Object> map = new HashMap<>();
        map.put("success", true);
        map.put("code", httpCode);
        map.put("data", body);
        map.put("id", id);

        UnityHelper.SendUnityMessageForHttp(
                HTTPCallBack,
                HTTPCallBack,
                map
        );
    }

    private static void sendError(int id, int code, String error) {

        BangsawanLog.d(TAG,
                "[UNITY-CALLBACK-失败] id=" + id +
                        " | code=" + code +
                        " | error=" + error
        , ENABLED);

        Map<String, Object> map = new HashMap<>();
        map.put("success", false);
        map.put("code", code);
        map.put("data", error);
        map.put("id", id);

        UnityHelper.SendUnityMessageForHttp(
                HTTPCallBack,
                HTTPCallBack,
                map
        );
    }

    /* ================= 真正执行 HTTP ================= */

    private static void doSendHTTP(
            final String url,
            final byte[] bodyRaw,
            final int id,
            final long triggerTime
    ) {

        long sendTime = System.currentTimeMillis();

        BangsawanLog.d(TAG,
                "[发送] id=" + id +
                        " | triggerTime=" + triggerTime +
                        " | sendTime=" + sendTime +
                        " | delayCost=" + (sendTime - triggerTime) + "ms" +
                        " | thread=" + thread()
        , ENABLED);

        HTTP_CLIENT
                .newCall(buildPostRequest(url, bodyRaw))
                .enqueue(new Callback() {

                    @Override
                    public void onFailure(Call call, IOException e) {

                        long failTime = System.currentTimeMillis();

                        BangsawanLog.e(TAG,
                                "[失败] id=" + id +
                                        " | networkCost=" + (failTime - sendTime) + "ms" +
                                        " | totalCost=" + (failTime - triggerTime) + "ms" +
                                        " | error=" + e.getMessage(),
                                e
                        , ENABLED);

                        sendError(id, 0, e.getMessage());
                    }

                    @Override
                    public void onResponse(Call call, Response response) throws IOException {

                        long responseTime = System.currentTimeMillis();
                        int httpCode = response.code();

                        BangsawanLog.d(TAG,
                                "[回复] id=" + id +
                                        " | httpCode=" + httpCode +
                                        " | networkCost=" + (responseTime - sendTime) + "ms" +
                                        " | totalCost=" + (responseTime - triggerTime) + "ms"
                        , ENABLED);

                        if (!response.isSuccessful()) {

                            sendError(
                                    id,
                                    httpCode,
                                    response.message() != null ? response.message() : ""
                            );
                            return;
                        }

                        String body = response.body() != null
                                ? response.body().string()
                                : "";

                        sendSuccess(id, httpCode, body);
                    }
                });
    }

    /* ================= 对外接口 ================= */

    public static void SendHTTPInfo(
            final String url,
            final byte[] bodyRaw,
            final int id,
            final float delay
    ) {

        long triggerTime = System.currentTimeMillis();
        long delayMs = delay <= 0f ? 0L : (long) (delay * 1000);
        long expectedExecuteTime = triggerTime + delayMs;

        BangsawanLog.d(TAG,
                "[HTTP-计划时间] url=" + url +
                        " | triggerTime=" + triggerTime +
                        " | delayMs=" + delayMs +
                        " | expectedExecuteTime=" + expectedExecuteTime +
                        " | thread=" + Thread.currentThread().getName()
        , ENABLED);

        if (delayMs <= 0L) {

            BangsawanLog.d(TAG,
                    "[HTTP-0延迟时间] url=" + url +
                            " | executeTime=" + triggerTime +
                            " | thread=" + Thread.currentThread().getName()
            , ENABLED);

            doSendHTTP(url, bodyRaw, id, triggerTime);

        } else {

            DELAY_EXECUTOR.schedule(
                    () -> {

                        long realExecuteTime = System.currentTimeMillis();
                        long scheduleDrift = realExecuteTime - expectedExecuteTime;

                        BangsawanLog.d(TAG,
                                "[HTTP-延迟时间] url=" + url +
                                        " | 实际时间=" + realExecuteTime +
                                        " | expectedExecuteTime=" + expectedExecuteTime +
                                        " | drift=" + scheduleDrift + "ms" +
                                        " | thread=" + Thread.currentThread().getName()
                        , ENABLED);

                        doSendHTTP(url, bodyRaw, id, triggerTime);

                    },
                    delayMs,
                    TimeUnit.MILLISECONDS
            );
        }
    }
}
