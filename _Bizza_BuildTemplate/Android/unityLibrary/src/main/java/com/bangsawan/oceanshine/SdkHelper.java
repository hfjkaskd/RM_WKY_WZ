
package com.bangsawan.oceanshine;
import android.util.Log;


import android.widget.Toast;

import com.unity3d.player.UnityPlayer;

public final class SdkHelper {
    public static final String Tag = SdkHelper.class.getSimpleName();

    public static void ShowTips(String text) {
            if (UnityPlayer.currentActivity == null) {
            Log.e(Tag, "显示提示 Activity is null, cannot show Toast");
            return;
        }
        // Log.e(Tag, "显示提示");
        Toast.makeText(UnityPlayer.currentActivity, text, Toast.LENGTH_SHORT).show();
    }
}
