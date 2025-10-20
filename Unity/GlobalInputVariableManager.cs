using UnityEngine;
using System.Collections;

public class GlobalInputVariableManager : MonoBehaviour
{
    private static GlobalInputVariableManager instance;

    // 입력 통계 변수들
    private int totalClickCount = 0;        // 전체 클릭 횟수
    private int leftClickCount = 0;         // 좌클릭 횟수
    private int rightClickCount = 0;        // 우클릭 횟수
    private float totalMouseDistance = 0f;  // 마우스 이동 거리
    private int keyboardInputCount = 0;     // 키보드 입력 횟수
    private int enterInputCount = 0;        // 엔터 입력 횟수

    // 마우스 이동거리 계산용
    private Vector2 lastMousePosition = Vector2.zero;
    private bool isFirstMouseUpdate = true;

    // 업데이트 주기 관련
    [SerializeField] private float updateInterval = 0.1f; // 1초마다 업데이트
    private float lastUpdateTime = 0f;

    // 싱글톤 인스턴스
    public static GlobalInputVariableManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalInputVariableManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        // 싱글톤 패턴 구현
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

        // 초기화
        ResetAllStats();
        lastUpdateTime = Time.time;
    }

    void Start()
    {
        LogToDebug("[GlobalInputVariableManager] 입력 통계 관리자 시작됨");
        
        // 초기 마우스 위치 설정
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Windows에서는 실제 화면 좌표 사용
        if (GetCursorPos(out POINT mousePos))
        {
            lastMousePosition = new Vector2(mousePos.x, mousePos.y);
        }
        #else
        // Unity Input 사용
        lastMousePosition = Input.mousePosition;
        #endif
        isFirstMouseUpdate = false;
    }

    void Update()
    {
        // 주기적으로 통계 출력
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateStatisticsDisplay();
            lastUpdateTime = Time.time;
        }
    }

    #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    // Windows API for cursor position
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }
    #endif

    // 통계 업데이트 메서드들
    public void AddLeftClick()
    {
        leftClickCount++;
        totalClickCount++;
        LogToDebug($"[InputStats] 좌클릭 추가 - 좌클릭: {leftClickCount}, 총 클릭: {totalClickCount}");

        // 좌클릭 1000회마다 Small Talk 트리거
        if (leftClickCount % 1000 == 0)
        {
            try
            {
                string purpose = "잡담"; // 기본 목적
                string chatIdx = GameManager.Instance.chatIdxSuccess?.ToString() ?? "-1";
                string speaker = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
                string aiLang = SettingManager.Instance.settings.ai_language ?? "ko";
                // 스트리밍으로 받아 즉시 ProcessReply 처리
                APIManager.Instance.CallSmallTalkStream(purpose, speaker, chatIdx, aiLang);
                LogToDebug($"[InputStats] SmallTalk Trigger 호출 (좌클릭 {leftClickCount})");
            }
            catch (System.Exception ex)
            {
                Debug.Log($"SmallTalk Trigger 실패: {ex.Message}");
            }
        }
    }

    public void AddRightClick()
    {
        rightClickCount++;
        totalClickCount++;
        LogToDebug($"[InputStats] 우클릭 추가 - 우클릭: {rightClickCount}, 총 클릭: {totalClickCount}");
    }

    public void AddMiddleClick()
    {
        totalClickCount++;
        LogToDebug($"[InputStats] 휠클릭 추가 - 총 클릭: {totalClickCount}");
    }

    public void AddKeyboardInput(string keyName = "")
    {
        keyboardInputCount++;
        
        // 엔터 키 체크
        if (keyName.Contains("엔터") || keyName.Contains("Enter") || keyName.Contains("Return"))
        {
            enterInputCount++;
        }
        
        LogToDebug($"[InputStats] 키입력 추가 ({keyName}) - 키입력: {keyboardInputCount}, 엔터: {enterInputCount}");
    }

    public void UpdateMouseDistance(Vector2 currentPosition)
    {
        if (!isFirstMouseUpdate)
        {
            float distance = Vector2.Distance(lastMousePosition, currentPosition);
            if (distance > 1f) // 1픽셀 이상 이동할 때만 카운트
            {
                totalMouseDistance += distance;
                LogToDebug($"[InputStats] 마우스 이동 - 거리: {distance:F1}px, 총 이동거리: {totalMouseDistance:F1}px");
            }
        }
        
        lastMousePosition = currentPosition;
        isFirstMouseUpdate = false;
    }

    public void UpdateMouseDistanceFromScreenCoords(int x, int y)
    {
        UpdateMouseDistance(new Vector2(x, y));
    }

    // 통계 출력
    private void UpdateStatisticsDisplay()
    {
        string stats = $"[입력 통계] 총클릭:{totalClickCount} | 좌클릭:{leftClickCount} | 우클릭:{rightClickCount} | " +
                      $"키입력:{keyboardInputCount} | 엔터:{enterInputCount} | 마우스이동:{totalMouseDistance:F0}px";
        
        LogToDebugBalloon(stats);
    }

    // Getter 메서드들
    public int GetTotalClickCount() => totalClickCount;
    public int GetLeftClickCount() => leftClickCount;
    public int GetRightClickCount() => rightClickCount;
    public float GetTotalMouseDistance() => totalMouseDistance;
    public int GetKeyboardInputCount() => keyboardInputCount;
    public int GetEnterInputCount() => enterInputCount;

    // Setter 메서드들 (필요시 사용)
    public void SetTotalClickCount(int count) => totalClickCount = Mathf.Max(0, count);
    public void SetLeftClickCount(int count) => leftClickCount = Mathf.Max(0, count);
    public void SetRightClickCount(int count) => rightClickCount = Mathf.Max(0, count);
    public void SetTotalMouseDistance(float distance) => totalMouseDistance = Mathf.Max(0f, distance);
    public void SetKeyboardInputCount(int count) => keyboardInputCount = Mathf.Max(0, count);
    public void SetEnterInputCount(int count) => enterInputCount = Mathf.Max(0, count);

    // 통계 초기화
    public void ResetAllStats()
    {
        totalClickCount = 0;
        leftClickCount = 0;
        rightClickCount = 0;
        totalMouseDistance = 0f;
        keyboardInputCount = 0;
        enterInputCount = 0;
        LogToDebug("[InputStats] 모든 통계 초기화됨");
    }

    // 개별 통계 초기화
    public void ResetClickStats()
    {
        totalClickCount = 0;
        leftClickCount = 0;
        rightClickCount = 0;
        LogToDebug("[InputStats] 클릭 통계 초기화됨");
    }

    public void ResetKeyboardStats()
    {
        keyboardInputCount = 0;
        enterInputCount = 0;
        LogToDebug("[InputStats] 키보드 통계 초기화됨");
    }

    public void ResetMouseDistanceStats()
    {
        totalMouseDistance = 0f;
        LogToDebug("[InputStats] 마우스 이동거리 통계 초기화됨");
    }

    // 업데이트 주기 설정
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Max(0.1f, interval);
        LogToDebugBalloon($"[InputStats] 업데이트 주기 변경: {updateInterval}초");
    }

    public float GetUpdateInterval() => updateInterval;

    // 통계 정보를 문자열로 반환
    public string GetStatsString()
    {
        return $"총클릭:{totalClickCount}, 좌클릭:{leftClickCount}, 우클릭:{rightClickCount}, " +
               $"키입력:{keyboardInputCount}, 엔터:{enterInputCount}, 마우스이동:{totalMouseDistance:F0}px";
    }

    // 통계 정보를 상세 문자열로 반환
    public string GetDetailedStatsString()
    {
        return $"=== 입력 통계 ===\n" +
               $"총 클릭 횟수: {totalClickCount}\n" +
               $"좌클릭 횟수: {leftClickCount}\n" +
               $"우클릭 횟수: {rightClickCount}\n" +
               $"키보드 입력 횟수: {keyboardInputCount}\n" +
               $"엔터 입력 횟수: {enterInputCount}\n" +
               $"총 마우스 이동거리: {totalMouseDistance:F1}px";
    }

    // DebugBalloon에 로그를 출력하는 메서드
    private void LogToDebugBalloon(string message)
    {
        // DebugBalloonManager가 있으면 사용, 없으면 일반 Debug.Log 사용
        if (DebugBalloonManager.Instance != null)
        {
            DebugBalloonManager.Instance.SetDebugBalloonText(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    // Debug 일괄관리
    private void LogToDebug(string message)
    {
        // Debug.Log(message);
    }
}
