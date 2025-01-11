package com.example.mylittlejarvisandroid;

import android.app.Activity;
import android.app.AlertDialog;
import android.app.Application;
import android.content.ComponentName;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Handler;
import android.util.Log;

public class Bridge extends Application {
    private static Context context;
    static Activity unityActivity;
    private static final int NOTIFICATION_ID = 1;
    private static final String CHANNEL_ID = "ForegroundServiceChannel";
    private static Handler notificationHandler = new Handler();
    private static Runnable notificationRunnable;

    @Override
    public void onCreate() {
        super.onCreate();
        context = getApplicationContext();
    }

    public static Context getContext() {
        return context;
    }

    // 자동 시작 설정을 위한 인텐트 배열 (특정 제조사 지원)
    public static final Intent[] POWERMANAGER_INTENTS = new Intent[]{
            // 각 제조사별 자동 시작 관리 화면으로 이동하는 인텐트 설정
            new Intent().setComponent(new ComponentName("com.miui.securitycenter", "com.miui.permcenter.autostart.AutoStartManagementActivity")),
            new Intent().setComponent(new ComponentName("com.letv.android.letvsafe", "com.letv.android.letvsafe.AutobootManageActivity")),
            new Intent().setComponent(new ComponentName("com.huawei.systemmanager", "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity")),
            new Intent().setComponent(new ComponentName("com.huawei.systemmanager", "com.huawei.systemmanager.optimize.process.ProtectActivity")),
            new Intent().setComponent(new ComponentName("com.huawei.systemmanager", "com.huawei.systemmanager.appcontrol.activity.StartupAppControlActivity")),
            new Intent().setComponent(new ComponentName("com.coloros.safecenter", "com.coloros.safecenter.permission.startup.StartupAppListActivity")),
            new Intent().setComponent(new ComponentName("com.coloros.safecenter", "com.coloros.safecenter.startupapp.StartupAppListActivity")),
            new Intent().setComponent(new ComponentName("com.oppo.safe", "com.oppo.safe.permission.startup.StartupAppListActivity")),
            new Intent().setComponent(new ComponentName("com.iqoo.secure", "com.iqoo.secure.ui.phoneoptimize.AddWhiteListActivity")),
            new Intent().setComponent(new ComponentName("com.iqoo.secure", "com.iqoo.secure.ui.phoneoptimize.BgStartUpManager")),
            new Intent().setComponent(new ComponentName("com.vivo.permissionmanager", "com.vivo.permissionmanager.activity.BgStartUpManagerActivity")),
            new Intent().setComponent(new ComponentName("com.samsung.android.lool", "com.samsung.android.sm.ui.battery.BatteryActivity")),
            new Intent().setComponent(new ComponentName("com.htc.pitroad", "com.htc.pitroad.landingpage.activity.LandingPageActivity")),
            new Intent().setComponent(new ComponentName("com.asus.mobilemanager", "com.asus.mobilemanager.MainActivity"))
    };

    public static void ReceiveActivityInstance(Activity tempActivity) {
        unityActivity = tempActivity;
        Log.i("BRIDGE", "Activity received.");

//        String[] perms= new String[1];
//        perms[0]= Manifest.permission.ACTIVITY_RECOGNITION;
//        if (ContextCompat.checkSelfPermission(unityActivity, Manifest.permission.ACTIVITY_RECOGNITION)
//                != PackageManager.PERMISSION_GRANTED) {
//            Log.i("BRIDGE", "Permision isnt granted!");
//            ActivityCompat.requestPermissions(Bridge.unityActivity,
//                    perms,
//                    1);
//        }
        Log.i("BRIDGE", "ReceiveActivityInstance ended.");
    }
    public static void StartService() {
        if (unityActivity != null) {
            // 알림 생성 및 반복 발행
            CreateNotification();
            StartNotificationLoop();

            for (final Intent intent : POWERMANAGER_INTENTS) {  // 제조사별 인텐트 검사
                if (unityActivity.getPackageManager().resolveActivity(intent, PackageManager.MATCH_DEFAULT_ONLY) != null) {
                    Log.i("BRIDGE", "Auto start is required");
                    AlertDialog alertDialog = new AlertDialog.Builder(unityActivity).create();
                    alertDialog.setTitle("Auto start is required");
                    alertDialog.setMessage("Please enable auto start to provide correct work");
                    alertDialog.setButton(AlertDialog.BUTTON_NEUTRAL, "OK",
                            new DialogInterface.OnClickListener() {
                                public void onClick(DialogInterface dialog, int which) {
                                    unityActivity.startActivity(intent);
                                }
                            });
                    alertDialog.show();
                    break;
                }
            }

            // SERVICE 로그 확인
            Intent serviceIntent = new Intent(unityActivity, MyBackgroundService.class);
            unityActivity.startForegroundService(serviceIntent);
            Log.i("BRIDGE", "Foreground service started.");

            // SendMessage Test
            UnitySendMessage("GameManager", "SayHello", "Mingu");
        } else {
            Log.e("BRIDGE", "Unity Activity is null. Cannot start service.");
        }
    }

    // 1. 알림 생성 메서드
    private static void CreateNotification() {
        if (unityActivity == null) return;
//        UnitySendMessage("GameManager", "SayHello", "TEST!!!");
//
//        // 1.1 알림 클릭 시 앱 복구 기능
//        Intent notificationIntent = new Intent(unityActivity, unityActivity.getClass());
//        notificationIntent.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
//        PendingIntent pendingIntent = PendingIntent.getActivity(
//                unityActivity, 0, notificationIntent, PendingIntent.FLAG_IMMUTABLE);
//
//        // 1.2 알림 빌더 설정
//        NotificationCompat.Builder builder = new NotificationCompat.Builder(unityActivity, CHANNEL_ID)
//                .setContentTitle("서비스 실행 중")
//                .setContentText("arona is listening...")
//                .setSmallIcon(R.drawable.custom_icon)
//                .setContentIntent(pendingIntent)
//                .setPriority(NotificationCompat.PRIORITY_HIGH) // 알림 우선순위 설정
//                .setAutoCancel(false) // 클릭 시 알림 닫히지 않음
//                .setOngoing(true); // 드래그로 제거 불가
//
//        // 1.3 채널 설정 (안드로이드 8.0 이상)
//        NotificationChannel channel = new NotificationChannel(
//                CHANNEL_ID, "Foreground Service Channel", NotificationManager.IMPORTANCE_HIGH);
//        NotificationManager manager = unityActivity.getSystemService(NotificationManager.class);
//        manager.createNotificationChannel(channel);
//
//
//        // 1.4 알림 매니저 실행
//        NotificationManager manager2 = (NotificationManager) unityActivity.getSystemService(Activity.NOTIFICATION_SERVICE);
//        manager2.notify(NOTIFICATION_ID, builder.build());
    }

    // 2. 10초 간격으로 알림 재발행
    private static void StartNotificationLoop() {
        notificationRunnable = new Runnable() {
            @Override
            public void run() {
                // 알림 재발행
                CreateNotification();
                // 10초 후 다시 실행
                notificationHandler.postDelayed(this, 10000);
            }
        };
        notificationHandler.post(notificationRunnable);
    }

    // 3. 서비스 종료 및 반복 중지
    public static void StopService() {
        if (unityActivity != null) {
            Intent serviceIntent = new Intent(unityActivity, MyBackgroundService.class);
            unityActivity.stopService(serviceIntent);

            // 반복 작업 중지
            notificationHandler.removeCallbacks(notificationRunnable);
        }
    }

    // 유니티로 메시지 전송
    public static void UnitySendMessage(String gameObject, String methodName, String param) {
        try {
            Log.i("BRIDGE", "[Android] ["+methodName+"] 발신 시작");
            com.unity3d.player.UnityPlayer.UnitySendMessage(gameObject, methodName, param);
            Log.i("BRIDGE", "[Android] ["+methodName+"] 발신 완료");
        } catch (Exception e) {
            Log.e("BRIDGE", "[Android] 유니티 메시지 전송 오류: " + e.getMessage());
        }
    }
}
