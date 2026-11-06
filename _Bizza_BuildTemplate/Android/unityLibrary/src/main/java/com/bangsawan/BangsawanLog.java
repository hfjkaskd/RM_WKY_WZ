package com.bangsawan;

import android.util.Log;

public final class BangsawanLog {

    private BangsawanLog() {}

    public static void d(String tag, String message, boolean ENABLED) {
        if (ENABLED) {
            Log.d(tag, message);
        }
    }

    public static void i(String tag, String message, boolean ENABLED) {
        if (ENABLED) {
            Log.i(tag, message);
        }
    }

    public static void w(String tag, String message, boolean ENABLED) {
        if (ENABLED) {
            Log.w(tag, message);
        }
    }

    public static void e(String tag, String message, boolean ENABLED) {
        if (ENABLED) {
            Log.e(tag, message);
        }
    }

    public static void e(String tag, String message, Throwable throwable, boolean ENABLED) {
        if (ENABLED) {
            Log.e(tag, message, throwable);
        }
    }
}
