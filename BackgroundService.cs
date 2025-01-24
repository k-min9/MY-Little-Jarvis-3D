using System;
using UnityEngine;

public class BackgroundService : MonoBehaviour
{
    private AndroidJavaObject unityActivity;  // Unity의 현재 액티비티
    private AndroidJavaObject pluginClass;    // Android 플러그인 클래스

    void Start()
    {
        // 초기화 및 Android Activity 연결
        InitializePlugin();
    }

    /// Android 플러그인 초기화
    private void InitializePlugin()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR  // 안드로이드
        // UnityPlayer 클래스에서 현재 액티비티 가져오기
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // 안드로이드 플러그인 클래스 연결
        pluginClass = new AndroidJavaClass("com.example.mylittlejarvisandroid.Bridge");

        // Unity의 Activity 전달
        Debug.Log("Activity 연결 시작");
        pluginClass.CallStatic("ReceiveActivityInstance", unityActivity);
        Debug.Log("Activity 연결 완료");

        #endif
    }

    // 백그라운드 이동시 작동
    void OnApplicationPause(bool isPaused)
    {
       
        if (isPaused)
        {
            Debug.Log("OnApplicationPause StartService");
            StartService();
        }
        else
        {
            Debug.Log("OnApplicationPause StopService");
            StopService();
        }
    }

    // 유니티 종료시 명시적 AAR 서비스 호출에 의한 종료
    void OnApplicationQuit()
    {
        StopService(); // 앱 종료 시 서비스 중단
    }

    /// 서비스 시작 메서드
    public void StartService()
    {
        // VAD 켜져있지 않으면 return
        if (!VADController.Instance.isVADModeActive) {
            Debug.Log("StartService : VAD not activated");
            return;
        }

        string baseUrl = ServerManager.Instance.baseUrl;

#if UNITY_ANDROID && !UNITY_EDITOR  // 안드로이드
        pluginClass.CallStatic("ReceiveBaseUrl", baseUrl);
        Debug.Log("InitializePlugin Send baseUrl finish : " + baseUrl);

        String nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        pluginClass.CallStatic("ReceiveNickname", nickname);
        Debug.Log("InitializePlugin Send nickname finish : " + nickname);

        String player_name = SettingManager.Instance.settings.player_name;
        pluginClass.CallStatic("ReceivePlayerName", player_name);
        Debug.Log("InitializePlugin Send player_name finish : " + player_name);

        String sound_language = SettingManager.Instance.settings.sound_language;
        pluginClass.CallStatic("ReceiveSoundLanguage", sound_language);
        Debug.Log("InitializePlugin Send sound_language finish : " + sound_language);

        String sound_volume = SettingManager.Instance.settings.sound_volumeMaster.ToString();
        pluginClass.CallStatic("ReceiveSoundVolume", sound_volume);
        Debug.Log("InitializePlugin Send sound_volume finish : " + sound_volume);

        String sound_speed = SettingManager.Instance.settings.sound_speedMaster.ToString();
        pluginClass.CallStatic("ReceiveSoundSpeed", sound_speed);
        Debug.Log("InitializePlugin Send sound_speed finish : " + sound_speed);

        String file_path = Application.persistentDataPath;  // Manager들의 getFile 위치 선정시 null exception을 줄일 수 있음
        pluginClass.CallStatic("ReceiveFilePath", file_path);
        Debug.Log("InitializePlugin Send file_path finish : " + file_path);

        pluginClass.CallStatic("StartService");
        Debug.Log("StartService");
#endif

    }

    /// 서비스 정지 메서드
    public void StopService()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR  // 안드로이드
        pluginClass.CallStatic("StopService");
        Debug.Log("StopService");
        #endif
    }

    public void OpenBatteryOptiSettings()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR  // 안드로이드
        pluginClass.CallStatic("OpenBatteryOptiSettings");
        Debug.Log("OpenBatteryOptiSettings");
        #endif
    }

    // 녹음 시작
    public void StartRecording(string duration)
    {
        string microphoneDevice = Microphone.devices[0]; // 첫 번째 마이크 선택

        Microphone.End(microphoneDevice);
        Debug.Log("[Unity] 녹음 중단!");

        int recordDuration = int.Parse(duration); // 안드로이드에서 전달된 녹음 시간
        Debug.Log(recordDuration);
        Debug.Log("[Unity] 녹음 시작!");
        AudioClip _clip = Microphone.Start(microphoneDevice, true, recordDuration, 44100);

    }

    // 호출 테스트
    public void SayHello(string name)
    {
        Debug.Log("Hello From " + name + "/" + VADController.Instance.isVADModeActive);
        if (VADController.Instance.isVADModeActive)
        {
            VADController.Instance.CheckVAD();
        }
        Debug.Log("Bye From " + name);
    }
}
