
package com.bangsawan.oceanshine;
import android.os.Bundle;

import com.bangsawan.BangsawanLog;
import com.unity3d.player.UnityPlayerActivity;

public class MainActivity extends UnityPlayerActivity {
    public static final String TAG = MainActivity.class.getSimpleName();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        BangsawanLog.d("广告归因", "onCreate: ", true);
        MBridgeRevenueUtil.init(getApplicationContext());
        IBridgeRevenueUtil.init(getApplicationContext());
    }


}
