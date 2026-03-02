using UnityEngine;
using System;
using System.IO;

public class GlobalTimeVariableManager : MonoBehaviour
{
    private static GlobalTimeVariableManager instance;
    public static GlobalTimeVariableManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalTimeVariableManager>();
            }
            return instance;
        }
    }

    // 플레이 타임 
    public float sessionPlaySeconds = 0f;
    public float totalPlaySeconds = 0f;

    // SmallTalk 타이머 
    public float smallTalkIntervalSeconds = 60f;
    public float smallTalkTimer = 0f;
    public bool smallTalkEnabled = false;

    // 기타 옵션
    [SerializeField] private bool autoSavePlaytime = false; // 주기적으로 누적 저장
    private float autosaveTimer = 0f;
    private const float autosaveInterval = 10f; // 10초마다 저장
    
    // 설정 동기화
    private float settingsSyncTimer = 0f;
    private const float settingsSyncInterval = 10f;
    private bool lastSyncedEnabled = false;
    private float lastSyncedInterval = 60f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

        LoadTimeData();
        smallTalkTimer = 0f;
        SyncSettingsFromManager();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 플레이타임 갱신 (FixedUpdate 기반)
        sessionPlaySeconds += dt;
        totalPlaySeconds += dt;

        // 자동 저장 주기
        if (autoSavePlaytime)
        {
            autosaveTimer += dt;
            if (autosaveTimer >= autosaveInterval)
            {
                autosaveTimer = 0f;
                SaveTimeData();
            }
        }

        // 10초마다 설정 동기화
        UpdateSettingsSync(dt);

        // 대화 중이면 타이머 리셋
        if (StatusManager.Instance.IsConversationing || StatusManager.Instance.IsOptioning)
        {
            smallTalkTimer = 0f;
            return;
        }

        // SmallTalk 타이머
        UpdateSmallTalkTimer(dt);
    }

    private void OnApplicationQuit()
    {
        SaveTimeData();
    }

    // 설정 동기화 업데이트
    private void UpdateSettingsSync(float dt)
    {
        settingsSyncTimer += dt;
        if (settingsSyncTimer >= settingsSyncInterval)
        {
            settingsSyncTimer = 0f;
            SyncSettingsFromManager();
        }
    }

    // SmallTalk 타이머 업데이트
    private void UpdateSmallTalkTimer(float dt)
    {        
        if (smallTalkEnabled)
        {
            smallTalkTimer += dt;
            if (smallTalkTimer >= smallTalkIntervalSeconds)
            {
                TryTriggerSmallTalk();
                smallTalkTimer = 0f;
            }
        }
    }

    // SmallTalk 호출 시도 (가드 조건 포함)
    private void TryTriggerSmallTalk()
    {
        try
        {
            if (StatusManager.Instance.IsConversationing || StatusManager.Instance.IsOptioning)
            {
                return;
            }

            string purpose = "잡담";
            string chatIdx = GameManager.Instance != null ? (GameManager.Instance.chatIdxSuccess?.ToString() ?? "-1") : "-1";
            string speaker = CharManager.Instance != null ? CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter()) : null;
            string aiLang = SettingManager.Instance != null ? (SettingManager.Instance.settings.ai_language ?? "ko") : "ko";

            if (APIManager.Instance != null)
            {
                APIManager.Instance.CallSmallTalkStream(purpose, speaker, chatIdx, aiLang);
            }
        }
        catch (System.Exception)
        {
            // 조용히 무시 (네트워크/상태 이슈)
        }
    }

    // ===== Public API =====
    
    // 설정 값 가져오기
    private void SyncSettingsFromManager()
    {
        if (SettingManager.Instance == null) return;
        
        bool currentEnabled = SettingManager.Instance.settings.isCharAutoSmallTalk;
        float currentInterval = SettingManager.Instance.settings.charAutoSmallTalkInterval;
        
        // 활성화 상태 변경
        if (currentEnabled != lastSyncedEnabled)
        {
            smallTalkEnabled = currentEnabled;
            if (currentEnabled) smallTalkTimer = 0f;
            lastSyncedEnabled = currentEnabled;
        }
        
        // 간격 변경
        if (Mathf.Abs(currentInterval - lastSyncedInterval) > 0.1f)
        {
            smallTalkIntervalSeconds = Mathf.Max(5f, currentInterval);
            lastSyncedInterval = currentInterval;
        }
    }

    // 내부 저장/로드 - config/time.json
    [Serializable]
    private class TimeData
    {
        public float totalPlaySeconds;
    }

    private void SaveTimeData()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        string timeFilePath = Path.Combine(directoryPath, "time.json");
        try
        {
            Directory.CreateDirectory(directoryPath);
            TimeData data = new TimeData { totalPlaySeconds = totalPlaySeconds };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(timeFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError("SaveTimeData error: " + e.Message);
        }
    }

    private void LoadTimeData()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        string timeFilePath = Path.Combine(directoryPath, "time.json");
        try
        {
            if (File.Exists(timeFilePath))
            {
                string json = File.ReadAllText(timeFilePath);
                TimeData data = JsonUtility.FromJson<TimeData>(json);
                totalPlaySeconds = data != null ? Mathf.Max(0f, data.totalPlaySeconds) : 0f;
            }
            else
            {
                totalPlaySeconds = 0f;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("LoadTimeData error: " + e.Message);
            totalPlaySeconds = 0f;
        }
    }
}
