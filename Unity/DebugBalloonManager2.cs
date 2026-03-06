using System.Collections;
using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;


// VL Agent 스트리밍 디버그 히스토리 관리자
// - 짧은 디버그 목록 (MAX_DEBUG_NUM = 6개) - 화면 표시용
// - 긴 히스토리 (MAX_DEBUG_NUM2 = 30개) - 전체 기록 저장
// - Toggle로 짧은/긴 목록 전환
// - Cancel 버튼으로 스트리밍 취소
public class DebugBalloonManager2 : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public GameObject debugBalloon; // DebugBalloon GameObject
    [SerializeField] private RectTransform debugBalloonTransform; // DebugBalloon의 Transform
    [SerializeField] public TextMeshProUGUI debugBalloonText; // DebugBalloon Text
    [SerializeField] private GameObject webImage; // 웹 검색 관련 내용임을 표시하는 이미지

    [Header("Settings")]
    [SerializeField] private bool autoHide = false; // 자동 숨김 여부
    [SerializeField] private float autoHideDelay = 5f; // 자동 숨김 딜레이

    // 디버그 히스토리 상수
    private const int MAX_DEBUG_NUM = 6;   // 짧은 목록 (화면 표시용)
    private const int MAX_DEBUG_NUM2 = 30; // 긴 히스토리 (전체 저장용)

    // 디버그 로그 저장 큐 (사용처 : VL Agent, Web (모두 통합)
    private Queue<string> shortDebugLogs = new Queue<string>();  // 짧은 목록 (6개)
    private Queue<string> fullDebugLogs = new Queue<string>();   // 긴 히스토리 (30개)

    // 현재 표시 모드 (false: 짧은 목록, true: 긴 히스토리)
    private bool isShowingFullHistory = false;

    private Coroutine autoHideCoroutine; // 자동 숨김 코루틴

    // 마지막 웹 검색 파일 경로 저장
    private string lastWebSearchFilePath = "";

    // 싱글톤 인스턴스
    private static DebugBalloonManager2 instance;
    public static DebugBalloonManager2 Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DebugBalloonManager2>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        debugBalloonText.text = string.Empty; // 텍스트 초기화

        // UI 최초 비활성화
        webImage.SetActive(false);

        // 더블클릭 및 홀드 감지를 위한 컴포넌트 추가
        if (debugBalloon != null)
        {
            var uiHandler = debugBalloon.AddComponent<DebugBalloonUIHandler>();
            uiHandler.Init(this);
        }


        // 윈도우 비활성화
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        HideDebugBalloon(); // 시작 시 DebugBalloon 숨기기
        #endif
    }

    #region 디버그 로그 관리

    // VL Agent 이벤트 로그 추가
    public void AddVlAgentLog(string message)
    {
        // VL Agent 로그에는 웹 이미지 숨김
        HideWebImage();

        string timestamp = DateTime.Now.ToString("HH:mm:ss");  // 타임스탬프
        string formattedLog = $"[{timestamp}] {message}";

        // 짧은 목록에 추가 (FIFO, 최대 6개)
        shortDebugLogs.Enqueue(formattedLog);
        while (shortDebugLogs.Count > MAX_DEBUG_NUM)
        {
            shortDebugLogs.Dequeue();
        }

        // 긴 히스토리에 추가 (FIFO, 최대 30개)
        fullDebugLogs.Enqueue(formattedLog);
        while (fullDebugLogs.Count > MAX_DEBUG_NUM2)
        {
            fullDebugLogs.Dequeue();
        }

        // 보이지 않으면 Show + 위치 설정
        ShowDebugBalloon();

        // 디버그 텍스트 업데이트
        UpdateDebugBalloonText();

        // 자동 숨김 활성화된 경우 타이머 시작
        if (autoHide)
        {
            StartAutoHideTimer();
        }
    }

    // 기존 AddDebugLog 호환용
    public void AddDebugLog(string logMessage)
    {
        AddVlAgentLog(logMessage);
    }

    // 현재 표시 모드에 따라 텍스트 업데이트
    private void UpdateDebugBalloonText()
    {
        Queue<string> targetQueue = isShowingFullHistory ? fullDebugLogs : shortDebugLogs;  // 표시할 큐 선택
        
        string[] logs = targetQueue.ToArray();
        string allLogs = string.Join("\n\n", logs);  // 각 로그 사이에 빈 줄 추가
        debugBalloonText.text = allLogs;

        // 높이 조정 (최대 1200f)
        float textHeight = Mathf.Min(debugBalloonText.preferredHeight, 1200f);
        debugBalloonTransform.sizeDelta = new Vector2(debugBalloonTransform.sizeDelta.x, textHeight + 120);
    }

    // 디버그 로그 모두 초기화
    public void ClearDebugLogs()
    {
        shortDebugLogs.Clear();
        fullDebugLogs.Clear();
        debugBalloonText.text = string.Empty;
        
        // 웹 이미지도 숨김
        HideWebImage();
    }

    #endregion

    #region 웹 로그 관리

    // Web 검색 로그 추가 (키워드, 검색 수단, 검색 내용) - 통합 Queue 사용
    public void AddWebLog(string keyword, string method, string content)
    {
        // 기존 로그 모두 삭제
        ClearDebugLogs();

        // 웹 이미지 표시
        ShowWebImage();

        string timestamp = DateTime.Now.ToString("HH:mm:ss");

        // 짧은 로그: 키워드 + 검색 수단만
        string shortLog = $"[{timestamp}] Web Search\n키워드: {keyword}\n검색수단: {method}";
        shortDebugLogs.Enqueue(shortLog);
        while (shortDebugLogs.Count > MAX_DEBUG_NUM)
        {
            shortDebugLogs.Dequeue();
        }

        // 긴 로그: 키워드 + 검색 수단 + 검색 내용
        string fullLog = $"[{timestamp}] Web Search\n" +
                        $"키워드: {keyword}\n" +
                        $"검색수단: {method}\n" +
                        $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        $"검색내용:\n{content}";
        fullDebugLogs.Enqueue(fullLog);
        while (fullDebugLogs.Count > MAX_DEBUG_NUM2)
        {
            fullDebugLogs.Dequeue();
        }

        // 디버그 텍스트 업데이트 (표시는 하지 않음)
        UpdateDebugBalloonText();

        // 웹 검색 내용을 파일로 저장
        SaveWebSearchToFile(keyword, method, content);
    }

    // 웹 검색 내용을 파일로 저장
    private void SaveWebSearchToFile(string keyword, string method, string content)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logContent = $"[Web Search Log - {timestamp}]\n" +
                              $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                              $"키워드: {keyword}\n" +
                              $"검색수단: {method}\n" +
                              $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                              $"검색내용:\n{content}\n";

            // 파일 경로 생성
            string folderPath = System.IO.Path.Combine(Application.persistentDataPath, "web_search");
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }
            string fileName = $"web_search_{timestamp}_{timestamp}.txt";
            string filePath = System.IO.Path.Combine(folderPath, fileName);

            // 파일 저장
            System.IO.File.WriteAllText(filePath, logContent);

            // 마지막 파일 경로 기억
            lastWebSearchFilePath = filePath;

            Debug.Log($"[WebLog] Saved to file: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebLog] Failed to save file: {ex.Message}");
        }
    }

    // 마지막 웹 검색 파일을 메모장으로 열기
    // 사용 가능 플랫폼: Windows
    public void OpenLastWebSearchFile()
    {
        if (string.IsNullOrEmpty(lastWebSearchFilePath))
        {
            Debug.LogWarning("[WebLog] No web search file saved yet");
            return;
        }

        DebugManager.Instance.OpenTextFile(lastWebSearchFilePath);
    }

    #endregion

    #region 히스토리 토글

    // 짧은 목록 ↔ 긴 히스토리 토글
    public void ToggleDebugHistory()
    {
        isShowingFullHistory = !isShowingFullHistory;
        UpdateDebugBalloonText();
        
        string modeText = isShowingFullHistory ? "전체 히스토리" : "최근 로그";
        Debug.Log($"[DebugBalloon] 표시 모드 변경: {modeText} ({(isShowingFullHistory ? MAX_DEBUG_NUM2 : MAX_DEBUG_NUM)}개)");
    }

    // 현재 전체 히스토리 표시 중인지 반환
    public bool IsShowingFullHistory()
    {
        return isShowingFullHistory;
    }

    #endregion

    #region VL Agent Cancel

    // VL Agent 스트리밍 취소 요청
    public void CancelVlAgentStreaming()
    {
        if (ApiVlAgentManager.Instance != null)
        {
            ApiVlAgentManager.Instance.CancelVlPlanerRun();
            AddVlAgentLog("취소 요청됨");
        }
        else
        {
            Debug.LogWarning("[DebugBalloon] ApiVlAgentManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    #endregion

    #region Balloon 표시/숨김

    // hide, show 전환
    public void ToggleDebugBalloon()
    {
        // DevMode가 아닐 때는 Show 불가
        if (!DevManager.Instance.IsDevModeEnabled())
        {
            HideDebugBalloon();
            return;
        }

        if (debugBalloon.activeSelf)
        {
            HideDebugBalloon();
        }
        else
        {
            ShowDebugBalloon();
        }
    }

    // DebugBalloon 표시
    public void ShowDebugBalloon()
    {
        // DevMode가 아닐 때는 표시 불가
        if (!DevManager.Instance.IsDevModeEnabled()) return;

        // 보이지 않았다면 위치 설정
        if (!debugBalloon.activeSelf)
        {
            debugBalloon.SetActive(true);
            debugBalloonTransform.position = UIPositionManager.Instance.GetMenuPosition("debugBalloon2");
        }
        
        // 자동 숨김 코루틴 중지
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    // DebugBalloon 숨기기
    public void HideDebugBalloon()
    {
        debugBalloon.SetActive(false);
        
        // 자동 숨김 코루틴 중지
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    // DebugBalloon의 텍스트를 완전히 교체
    public void SetDebugBalloonText(string text)
    {
        debugBalloonText.text = text;

        // 높이 조정
        float textHeight = Mathf.Clamp(debugBalloonText.preferredHeight, 100f, 600f);
        textHeight += 120f; // 웹 아이콘용 이미지 길이 조정
        debugBalloonTransform.sizeDelta = new Vector2(debugBalloonTransform.sizeDelta.x, textHeight + 60);
        
        ShowDebugBalloon();
        
        if (autoHide)
        {
            StartAutoHideTimer();
        }
    }

    // Balloon 표시 상태 반환
    public bool IsVisible()
    {
        return debugBalloon != null && debugBalloon.activeInHierarchy;
    }

    #endregion

    #region 자동 숨김

    private void StartAutoHideTimer()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
        autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
    }

    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(autoHideDelay);
        HideDebugBalloon();
        autoHideCoroutine = null;
    }

    // 자동 숨김 설정
    public void SetAutoHide(bool enable, float delay = 5f)
    {
        autoHide = enable;
        autoHideDelay = delay;
    }

    #endregion

    #region 유틸리티

    // 현재 짧은 로그 개수 반환
    public int GetLogCount()
    {
        return shortDebugLogs.Count;
    }

    // 전체 히스토리 개수 반환
    public int GetFullHistoryCount()
    {
        return fullDebugLogs.Count;
    }

    // 웹 이미지 표시
    public void ShowWebImage()
    {
        webImage.SetActive(true);
    }

    // 웹 이미지 숨김
    public void HideWebImage()
    {
        webImage.SetActive(false);
    }

    #endregion
}

public class DebugBalloonUIHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private DebugBalloonManager2 manager;
    private bool isLeftClickHeld = false;
    private float leftClickHoldTime = 0f;
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private const float doubleClickTime = 0.3f;

    public void Init(DebugBalloonManager2 mgr)
    {
        manager = mgr;
    }

    private void Update()
    {
        if (manager == null) return;

        // 좌클릭 상태 체크
        if (isLeftClickHeld)
        {
            leftClickHoldTime += Time.deltaTime;
            if (leftClickHoldTime >= 0.5f) // 0.5초 이상 누르면 홀드 동작 실행
            {
                isLeftClickHeld = false; // 상태 초기화
                leftClickHoldTime = 0f;
                manager.HideDebugBalloon();
            }
        }

        // 더블클릭 타이머 관리
        if (clickCount > 0 && Time.time - lastClickTime > doubleClickTime)
        {
            clickCount = 0; // 더블클릭 시간 초과 시 리셋
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (manager == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 더블클릭 감지 로직
            if (Time.time - lastClickTime < doubleClickTime && clickCount == 1)
            {
                manager.HideDebugBalloon();
                clickCount = 0; // 더블클릭 처리 후 리셋
            }
            else
            {
                clickCount = 1;
                lastClickTime = Time.time;
            }

            isLeftClickHeld = true;
            leftClickHoldTime = 0f; // 타이머 초기화
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isLeftClickHeld = false;
            leftClickHoldTime = 0f; // 타이머 초기화
        }
    }
}
