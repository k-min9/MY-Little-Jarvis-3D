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
    private float sessionPlaySeconds = 0f;      // 이번 실행 세션의 플레이타임(초)
    private float totalPlaySeconds = 0f;        // 누적 플레이타임(초) - config/time.json 저장/로드

    // SmallTalk 타이머 
    [SerializeField] private float smallTalkIntervalSeconds = 60f;
    private float smallTalkTimer = 0f;
    private bool smallTalkEnabled = false;

    // 채팅 관련 시간 
    private float currentChatElapsedSeconds = 0f;      // 현재 채팅 경과 시간(초)

    // 기타 옵션
    [SerializeField] private bool autoSavePlaytime = false; // 주기적으로 누적 저장
    private float autosaveTimer = 0f;
    private const float autosaveInterval = 10f; // 10초마다 저장

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
        ResetSmallTalkTimer();
        StartNewChat();
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

        // SmallTalk 타이머 (대화/옵션 중에는 일시정지)
        if (smallTalkEnabled)
        {
            bool shouldPause = StatusManager.Instance != null && (StatusManager.Instance.IsConversationing || StatusManager.Instance.IsOptioning);
            if (!shouldPause)
            {
                smallTalkTimer += dt;
                if (smallTalkTimer >= smallTalkIntervalSeconds)
                {
                    TryTriggerSmallTalk();
                    ResetSmallTalkTimer();
                }
            }
        }

        // 현재 채팅 경과 (FixedUpdate 기반)
        currentChatElapsedSeconds += dt;
    }

    private void OnApplicationQuit()
    {
        SaveTimeData();
    }

    // SmallTalk 호출 시도 (가드 조건 포함)
    private void TryTriggerSmallTalk()
    {
        try
        {
            if (StatusManager.Instance != null)
            {
                if (StatusManager.Instance.IsConversationing || StatusManager.Instance.IsOptioning)
                {
                    return;
                }
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

    // SmallTalk
    public void EnableSmallTalk(bool enable)
    {
        smallTalkEnabled = enable;
        if (enable) ResetSmallTalkTimer();
    }
    public bool IsSmallTalkEnabled() { return smallTalkEnabled; }
    public void SetSmallTalkInterval(float seconds)
    {
        smallTalkIntervalSeconds = Mathf.Max(5f, seconds);
        ResetSmallTalkTimer();
    }
    public float GetSmallTalkInterval() { return smallTalkIntervalSeconds; }
    public void ResetSmallTalkTimer() { smallTalkTimer = 0f; }

    // 플레이타임
    public float GetSessionPlaySeconds() { return sessionPlaySeconds; }
    public float GetTotalPlaySeconds() { return totalPlaySeconds; }
    public void SetTotalPlaySeconds(float seconds)
    {
        totalPlaySeconds = Mathf.Max(0f, seconds);
        SaveTimeData();
    }

    // 채팅 단위 관리
    public void StartNewChat()
    {
        currentChatElapsedSeconds = 0f;
    }
    public float GetCurrentChatElapsedSeconds() { return currentChatElapsedSeconds; }

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
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError("Access denied: " + e.Message);
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.LogError("Directory not found: " + e.Message);
        }
        catch (IOException e)
        {
            Debug.LogError("I/O error occurred: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("An error occurred: " + e.Message);
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
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError("Access denied: " + e.Message);
            totalPlaySeconds = 0f;
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.LogError("Directory not found: " + e.Message);
            totalPlaySeconds = 0f;
        }
        catch (IOException e)
        {
            Debug.LogError("I/O error occurred: " + e.Message);
            totalPlaySeconds = 0f;
        }
        catch (Exception e)
        {
            Debug.LogError("An error occurred: " + e.Message);
            totalPlaySeconds = 0f;
        }
    }
}
