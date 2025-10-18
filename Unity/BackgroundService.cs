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

        // baseUrl을 비동기로 가져오기
        ServerManager.Instance.GetBaseUrl((baseUrl) =>
        {
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

        // server_type_idx 전송 (server 분기 처리용)
        String server_type_idx = SettingManager.Instance.settings.server_type_idx.ToString();
        pluginClass.CallStatic("ReceiveServerTypeIdx", server_type_idx);
        Debug.Log("InitializePlugin Send server_type_idx finish : " + server_type_idx);

        // dev_voice URL 전송 (server_type_idx=2일 때 TTS용)
        // Unity에서 ServerManager를 통해 dev_voice URL 가져오기
        ServerManager.Instance.GetServerUrlFromServerId("dev_voice", (devVoiceUrl) =>
        {
            if (!string.IsNullOrEmpty(devVoiceUrl))
            {
                pluginClass.CallStatic("ReceiveDevVoiceUrl", devVoiceUrl);
                Debug.Log("InitializePlugin Send dev_voice_url finish : " + devVoiceUrl);
            }
            else
            {
                pluginClass.CallStatic("ReceiveDevVoiceUrl", "");
                Debug.Log("InitializePlugin Send dev_voice_url finish : (empty)");
            }
            
            // dev_voice URL 설정 완료 후 서비스 시작
            pluginClass.CallStatic("StartService");
            Debug.Log("StartService");
        });
#endif
        }); // GetBaseUrl 콜백 종료
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
