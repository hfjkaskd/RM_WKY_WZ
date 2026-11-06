package com.bangsawan.unity;

import com.bangsawan.BangsawanLog;
import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

import java.util.HashMap;
import java.util.Map;

public final class UnityHelper {

    public static final String gameObjectName = "AndroidJavaMessageDispatcher";

    public static final String funcName = "DispatchMessage";

    public static final String rootName = "DispatchRootMessage";
    public static final String simCardName = "DispatchsimCardMessage";
    public static final String SendUnityMessageForGoogleIdName = "DispatchGoogleIdMessage";

    private static final String TAG =  "[测试日志][HTTP][安卓中转]";

    /**
     * 调用 Unity 中 Name 为 “AndroidJavaMessageDispatcher” 的 GameObject 上的 “DispatchMessage” 方法
     *
     * @param msgName 消息名
     * @param map 参数 会额外加上 msgName 最后转化为 json 传入 unity
     */
    public static void SendUnityMessage(String msgName, Map<String, Object> map) {
        if (map == null) {
            map = new HashMap<>();
        }

        map.put("MessageName", msgName);

        String jsonStr = new JSONObject(map).toString();
        UnityPlayer.UnitySendMessage(gameObjectName, funcName, jsonStr);
    }

    public static void SendUnityMessage(String msgName) {
        SendUnityMessage(msgName, null);
    }

    public static void SendUnityMessageForRoot(int msgName, Map<String, Object> map) {
        if (map == null) {
            map = new HashMap<>();
        }

        map.put("MessageName", msgName);

        String jsonStr = new JSONObject(map).toString();
        UnityPlayer.UnitySendMessage(gameObjectName, rootName, jsonStr);
    }

    public static void SendUnityMessageForSimCard(int msgName, Map<String, Object> map) {
        if (map == null) {
            map = new HashMap<>();
        }

        map.put("MessageName", msgName);

        String jsonStr = new JSONObject(map).toString();
        UnityPlayer.UnitySendMessage(gameObjectName, simCardName, jsonStr);
    }


    public static void SendUnityMessageForGoogleId(String msgName, Map<String, Object> map) {
        if (map == null) {
            map = new HashMap<>();
        }

        map.put("MessageName", msgName);

        String jsonStr = new JSONObject(map).toString();
        UnityPlayer.UnitySendMessage(gameObjectName, SendUnityMessageForGoogleIdName, jsonStr);
    }

    public static void SendUnityMessageForHttp(String msgName, String MethodName, Map<String, Object> map) {
        if (map == null) {
            map = new HashMap<>();
        }

        map.put("MessageName", msgName);

        String jsonStr = new JSONObject(map).toString();
        BangsawanLog.d(TAG, jsonStr, false);
        UnityPlayer.UnitySendMessage(gameObjectName, MethodName, jsonStr);
    }
}
