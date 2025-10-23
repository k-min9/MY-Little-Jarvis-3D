using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 스무고개(20 Questions) 게임 전용 UI Panel 관리자
// - 게임 정보 표시 (테마, 질문수, 정답)
// - 게임 상태에 따른 UI 업데이트
// - 대화 이력 창 표시/숨김 제어 (실제 로직은 UIGame20QHistoryManager)
public class UIGame20QPanelManager : MonoBehaviour
{
    // 싱글톤
    private static UIGame20QPanelManager instance;
    public static UIGame20QPanelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIGame20QPanelManager>();
            }
            return instance;
        }
    }

    // UI 오브젝트 참조
    [Header("Panel")]
    [SerializeField] private GameObject gamePanel;  // 전체 게임 패널
    
    [Header("Game Info")]
    [SerializeField] private TextMeshProUGUI gameStatusText;  // 게임 상태 표시 ("진행 중", "게임 종료" 등)
    [SerializeField] private TextMeshProUGUI themeText;  // 테마 표시 ("동물", "과일" 등)
    [SerializeField] private TextMeshProUGUI secretText;  // 정답 표시 (개발자 모드용, 나중에 숨김 처리)
    [SerializeField] private TextMeshProUGUI questionCountText;  // 질문 횟수 표시 ("3 / 20")
    
    [Header("Settings")]
    [SerializeField] private bool showSecret = true;  // 정답 표시 여부 (개발자 모드)

    // Private Fields
    private Coroutine autoUpdateCoroutine;  // 자동 업데이트 코루틴
    private bool secretRevealed = false;  // 정답을 실제로 확인했는지 여부
    private string lastSecret = "";  // 이전 정답 (변경 감지용)

    // Unity Lifecycle
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 패널 초기 상태는 숨김
        HidePanel();
    }

    private void OnDestroy()
    {
        if (autoUpdateCoroutine != null)
        {
            StopCoroutine(autoUpdateCoroutine);
        }
    }

    // Panel 표시/숨김
    
    // 게임 패널 표시
    public void ShowPanel()
    {
        if (gamePanel != null)
        {
            gamePanel.SetActive(true);
        }
        
        // 게임 정보 초기 로드
        UpdateGameInfo();
        
        // 자동 업데이트 시작 (0.5초마다)
        if (autoUpdateCoroutine == null)
        {
            autoUpdateCoroutine = StartCoroutine(AutoUpdateCoroutine());
        }
    }

    // 게임 패널 숨김
    public void HidePanel()
    {
        if (gamePanel != null)
        {
            gamePanel.SetActive(false);
        }
        
        // 자동 업데이트 중지
        if (autoUpdateCoroutine != null)
        {
            StopCoroutine(autoUpdateCoroutine);
            autoUpdateCoroutine = null;
        }
    }

    // 패널 토글
    public void TogglePanel()
    {
        if (gamePanel != null && gamePanel.activeSelf)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    // 패널 표시 여부 반환
    public bool IsPanelVisible()
    {
        return gamePanel != null && gamePanel.activeSelf;
    }

    // 게임 정보 업데이트
    
    // 게임 정보 전체 업데이트
    public void UpdateGameInfo()
    {
        if (MiniGame20QManager.Instance == null)
        {
            return;
        }

        UpdateTheme();
        UpdateQuestionCount();
        UpdateGameStatus();
        UpdateSecret();
    }

    // 테마 업데이트
    public void UpdateTheme()
    {
        if (themeText == null) return;

        string theme = MiniGame20QManager.Instance.theme;
        if (string.IsNullOrEmpty(theme))
        {
            themeText.text = "테마: -";
        }
        else
        {
            themeText.text = $"테마: {theme}";
        }
    }

    // 질문 횟수 업데이트
    public void UpdateQuestionCount()
    {
        if (questionCountText == null) return;

        int current = MiniGame20QManager.Instance.questionCount;
        int max = MiniGame20QManager.Instance.maxQuestions;
        
        questionCountText.text = $"질문: {current} / {max}";
        
        // 질문 횟수에 따라 색상 변경
        if (current >= max)
        {
            questionCountText.color = Color.red;
        }
        else if (current >= max * 0.8f)
        {
            questionCountText.color = Color.yellow;
        }
        else
        {
            questionCountText.color = Color.white;
        }
    }

    // 게임 상태 업데이트
    public void UpdateGameStatus()
    {
        if (gameStatusText == null) return;

        string gameStatus = MiniGame20QManager.Instance.gameStatus;
        string gameResult = MiniGame20QManager.Instance.gameResult;
        string waitingFor = MiniGame20QManager.Instance.waitingFor;

        // 3-Field System 기반 상태 텍스트 생성
        string statusText = "";
        Color statusColor = Color.white;

        switch (gameStatus)
        {
            case "not_started":
                statusText = "대기 중";
                statusColor = Color.gray;
                break;
                
            case "game_start":
                statusText = "게임 시작!";
                statusColor = Color.green;
                break;
                
            case "playing":
                if (waitingFor == "continue_or_giveup")
                {
                    statusText = "계속/포기 선택 대기";
                    statusColor = Color.yellow;
                }
                else if (waitingFor == "restart")
                {
                    statusText = "재시작 확인 대기";
                    statusColor = Color.yellow;
                }
                else
                {
                    statusText = "진행 중";
                    statusColor = Color.green;
                }
                break;
                
            case "game_over":
                switch (gameResult)
                {
                    case "user_won":
                        statusText = "승리! 정답을 맞췄습니다!";
                        statusColor = Color.cyan;
                        break;
                    case "ai_won":
                        statusText = "AI 승리! AI가 정답을 맞췄습니다.";
                        statusColor = Color.magenta;
                        break;
                    case "user_gave_up":
                        statusText = "포기하셨습니다.";
                        statusColor = Color.gray;
                        break;
                    case "max_reached":
                        statusText = "질문 한도 도달!";
                        statusColor = Color.red;
                        break;
                    default:
                        statusText = "게임 종료";
                        statusColor = Color.gray;
                        break;
                }
                break;
                
            default:
                statusText = gameStatus;
                statusColor = Color.white;
                break;
        }

        gameStatusText.text = statusText;
        gameStatusText.color = statusColor;
    }

    // 정답 업데이트 (개발자 모드)
    public void UpdateSecret()
    {
        if (secretText == null) return;

        if (showSecret)
        {
            string secret = MiniGame20QManager.Instance.secret;
            
            // 정답이 없거나 빈 문자열인 경우 - 항상 "정답 : 없음" 표시
            if (string.IsNullOrEmpty(secret))
            {
                secretText.text = "정답 : 없음";
                secretText.color = Color.gray;
                secretText.gameObject.SetActive(true);
                return;
            }
            
            // 정답이 변경되었으면 다시 숨김 처리
            if (secret != lastSecret)
            {
                secretRevealed = false;
                lastSecret = secret;
                Debug.Log($"[20Q] 정답 변경 감지: {lastSecret} -> 숨김 상태로 전환");
            }
            
            if (secretRevealed)
            {
                // 정답을 이미 확인한 경우 - 실제 정답 표시
                secretText.text = $"[정답: {secret}]";
                secretText.color = Color.white;
            }
            else
            {
                // 아직 확인하지 않은 경우 - 클릭 유도 텍스트 표시
                secretText.text = "클릭시\n정답 공개";
                secretText.color = new Color(0.8f, 0.8f, 0.8f);  // 연한 회색
            }
            secretText.gameObject.SetActive(true);
        }
        else
        {
            secretText.gameObject.SetActive(false);
        }
    }

    // 대화 이력 로드 (UIGame20QHistoryManager 호출)
    public void LoadChatHistory()
    {
        if (UIGame20QHistoryManager.Instance != null)
        {
            UIGame20QHistoryManager.Instance.LoadChatHistory();
        }
    }

    // 대화 이력 초기화 (UIGame20QHistoryManager 호출)
    public void ClearChatHistory()
    {
        if (UIGame20QHistoryManager.Instance != null)
        {
            UIGame20QHistoryManager.Instance.ClearChatHistory();
        }
    }

    // 자동 업데이트
    
    // 자동 업데이트 코루틴 (0.5초마다 게임 정보 갱신)
    private IEnumerator AutoUpdateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            
            if (MiniGame20QManager.Instance != null)
            {
                UpdateGameInfo();
            }
        }
    }

    // 유틸리티
    
    // 정답 버튼 클릭 시 호출 (버튼과 연결) - 토글 방식
    public void OnSecretButtonClick()
    {
        // 정답이 없으면 반응하지 않음
        if (MiniGame20QManager.Instance == null) return;
        
        string secret = MiniGame20QManager.Instance.secret;
        if (string.IsNullOrEmpty(secret))
        {
            Debug.Log("[20Q] 정답이 없어 클릭 무반응");
            return;
        }
        
        // 정답이 있을 때만 토글
        secretRevealed = !secretRevealed;
        UpdateSecret();
        Debug.Log($"[20Q] 정답 {(secretRevealed ? "표시" : "숨김")}");
    }
    
    // 정답 확인 상태 리셋 (새 게임 시작 시)
    public void ResetSecretReveal()
    {
        secretRevealed = false;
        UpdateSecret();
        Debug.Log("[20Q] 정답 확인 상태 리셋");
    }
    
    // 정답 표시 토글 (개발자 모드)
    public void ToggleSecretDisplay()
    {
        showSecret = !showSecret;
        UpdateSecret();
    }

    // 정답 표시 설정
    public void SetSecretDisplay(bool show)
    {
        showSecret = show;
        UpdateSecret();
    }

    // Public API
    
    // 게임 시작 시 호출 (패널 초기화 및 표시)
    public void OnGameStart()
    {
        ClearChatHistory();
        ResetSecretReveal();
        lastSecret = "";  // 정답 추적 리셋
        UpdateGameInfo();
    }

    // 게임 종료 시 호출
    public void OnGameEnd()
    {
        UpdateGameInfo();
        // 패널은 유지 (사용자가 수동으로 닫을 수 있도록)
    }

    // 현재 게임 정보 문자열 반환 (디버그용)
    public string GetGameInfoString()
    {
        if (MiniGame20QManager.Instance == null)
        {
            return "게임 매니저 없음";
        }

        var info = MiniGame20QManager.Instance.GetGameInfo();
        string result = "=== 스무고개 게임 정보 ===\n";
        result += $"활성화: {info["is_active"]}\n";
        result += $"상태: {info["game_status"]}\n";
        result += $"결과: {info["game_result"]}\n";
        result += $"대기: {info["waiting_for"]}\n";
        result += $"테마: {info["theme"]} ({info["theme_key"]})\n";
        result += $"질문: {info["question_count"]} / {info["max_questions"]}\n";
        
        if (showSecret)
        {
            string secret = info["secret"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(secret))
            {
                result += "정답: 없음\n";
            }
            else if (secretRevealed)
            {
                result += $"정답: {secret}\n";
            }
            else
            {
                result += "정답: [클릭하여 확인]\n";
            }
        }
        
        return result;
    }
}
