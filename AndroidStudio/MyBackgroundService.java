package com.example.mylittlejarvisandroid;

import android.app.Activity;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.app.PendingIntent;
import android.content.Intent;
import android.os.Build;
import android.os.Handler;
import android.os.IBinder;
import android.util.Log;

import android.media.AudioAttributes;
import android.media.AudioFocusRequest;
import android.media.AudioManager;

import androidx.core.app.NotificationCompat;

public class MyBackgroundService extends Service {
    @Override
    public void onCreate() {
        super.onCreate();
        Log.i("SERVICE", "Service created.");
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
//        createNotificationChannel();
        Log.i("SERVICE", "createNotificationChannel");
        Log.i("SERVICE", "createNotification");

        return START_STICKY;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        Log.i("SERVICE", "Service destroyed.");
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    // 유니티로 메시지 전송
    private void UnitySendMessage(String gameObject, String methodName, String param) {
        try {
            Log.i("SERVICE", "[Android] ["+methodName+"] 발신 시작");
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObject, methodName, param);
            Log.i("SERVICE", "[Android] ["+methodName+"] 발신 완료");
        } catch (Exception e) {
            Log.e("SERVICE", "[Android] 유니티 메시지 전송 오류: " + e.getMessage());
        }
    }

}
