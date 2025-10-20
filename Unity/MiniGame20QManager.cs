using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// 미니게임 - 스무고개(20 Questions) 
// 아로프라 채널처럼 토글 방식으로 게임 모드를 활성화/비활성화
public class MiniGame20QManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static MiniGame20QManager instance;
    public static MiniGame20QManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MiniGame20QManager>();
            }
            return instance;
        }
    }

    // 게임 상태
    public bool is20QMode = false;
    
    // 3-Field System
    public string gameStatus = "not_started";    // game_start / playing / game_over
    public string gameResult = "";               // null / user_won / ai_won / user_gave_up / ai_lost
    public string waitingFor = "";               // null / restart / continue_or_giveup
    
    // 게임 데이터
    public string themeKey = "";                 // 테마 영어 키 (예: 'animal', 'fruit')
    public string theme = "";                    // 테마 언어별 이름 (예: '동물', '果物')
    public string secret = "";                   // 비밀 정답
    public int questionCount = 0;
    public int maxQuestions = 20;
    public List<Dictionary<string, string>> history = new List<Dictionary<string, string>>();
    public List<Dictionary<string, string>> historyQuestion = new List<Dictionary<string, string>>();
    public List<string> historySecretList = new List<string>();  // 이미 사용된 정답 리스트 (중복 방지)

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

    // 스무고개 모드 토글
    public void Toggle20QMode()
    {
        is20QMode = !is20QMode;
        
        if (is20QMode)
        {
            Debug.Log("[20Q] 스무고개 모드 활성화");
            StartNewGame();
        }
        else
        {
            Debug.Log("[20Q] 스무고개 모드 비활성화");
            ResetGameState();
        }
    }

    // 현재 스무고개 모드인지 확인
    public bool Is20QMode()
    {
        return is20QMode;
    }

    // 새 게임 시작
    public void StartNewGame()
    {
        ResetGameState();
        gameStatus = "playing";
        
        // 서버에 첫 요청 (secret 생성 및 환영 메시지)
        Debug.Log("[20Q] 새 게임 시작 - 서버에 첫 요청");
        CallGameAPI("");
    }

    // 게임 상태 초기화
    private void ResetGameState()
    {
        gameStatus = "not_started";
        gameResult = "";
        waitingFor = "";
        themeKey = "";
        theme = "";
        secret = "";
        questionCount = 0;
        history.Clear();
        historyQuestion.Clear();
        // historySecretList는 초기화하지 않음 (누적 유지로 중복 방지)
    }

    // ============================================================================
    // Setter 메서드들
    // ============================================================================

    /// <summary>
    /// 게임 상태 설정 (3-Field System)
    /// </summary>
    public void SetGameStatus(string status)
    {
        gameStatus = status;
        Debug.Log($"[20Q] 게임 상태: {gameStatus}");
        
        // UI 갱신
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateGameInfo();
        }
    }

    /// <summary>
    /// 게임 결과 설정
    /// </summary>
    public void SetGameResult(string result)
    {
        gameResult = result;
        if (!string.IsNullOrEmpty(result))
        {
            Debug.Log($"[20Q] 게임 결과: {gameResult}");
        }
        
        // UI 갱신
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateGameInfo();
        }
    }

    /// <summary>
    /// 대기 상태 설정
    /// </summary>
    public void SetWaitingFor(string waiting)
    {
        waitingFor = waiting;
        if (!string.IsNullOrEmpty(waiting))
        {
            Debug.Log($"[20Q] 대기 상태: {waitingFor}");
        }
        
        // UI 갱신
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateGameInfo();
        }
    }

    /// <summary>
    /// 테마 키 설정 (영어 키)
    /// </summary>
    public void SetThemeKey(string key)
    {
        themeKey = key;
        Debug.Log($"[20Q] 테마 키: {themeKey}");
        
        // UI 갱신
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateTheme();
        }
    }

    /// <summary>
    /// 테마 설정 (언어별 이름)
    /// </summary>
    public void SetTheme(string themeName)
    {
        theme = themeName;
        Debug.Log($"[20Q] 테마: {theme}");
        
        // UI 갱신
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateTheme();
        }
    }

    /// <summary>
    /// Secret 설정
    /// </summary>
    public void SetSecret(string newSecret)
    {
        secret = newSecret;
        Debug.Log($"[20Q] Secret 설정: {secret}");
        
        // UI 갱신 (개발자 모드)
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateSecret();
        }
    }

    /// <summary>
    /// 질문 횟수 설정
    /// </summary>
    public void SetQuestionCount(int count)
    {
        questionCount = count;
        Debug.Log($"[20Q] 질문 횟수: {questionCount}/{maxQuestions}");
        
        // UI 갱신
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateQuestionCount();
        }
    }

    /// <summary>
    /// 최대 질문 횟수 설정
    /// </summary>
    public void SetMaxQuestions(int max)
    {
        maxQuestions = max;
        
        // UI 갱신
        if (UIGame20QPanelManager.Instance != null)
        {
            UIGame20QPanelManager.Instance.UpdateQuestionCount();
        }
    }

    /// <summary>
    /// 전체 History 설정
    /// </summary>
    public void SetHistory(List<Dictionary<string, string>> newHistory)
    {
        history = newHistory;
        Debug.Log($"[20Q] History 업데이트: {history.Count}개 항목");
        
        // UI 대화 이력 갱신
        if (UIGame20QHistoryManager.Instance != null)
        {
            UIGame20QHistoryManager.Instance.LoadChatHistory();
        }
    }

    /// <summary>
    /// 질문 History 설정 (일상 대화 제외)
    /// </summary>
    public void SetHistoryQuestion(List<Dictionary<string, string>> newHistoryQuestion)
    {
        historyQuestion = newHistoryQuestion;
        Debug.Log($"[20Q] History Question 업데이트: {historyQuestion.Count}개 항목");
        
        // UI 대화 이력 갱신 (질문 히스토리만 보여주고 싶다면 여기서 처리)
        // 현재는 전체 history를 사용하므로 주석 처리
        // if (UIGame20QPanelManager.Instance != null)
        // {
        //     UIGame20QPanelManager.Instance.LoadChatHistory();
        // }
    }

    /// <summary>
    /// 사용된 정답 리스트 설정 (중복 방지)
    /// </summary>
    public void SetHistorySecretList(List<string> newHistorySecretList)
    {
        historySecretList = newHistorySecretList;
        Debug.Log($"[20Q] History Secret List 업데이트: {historySecretList.Count}개 정답 사용됨");
        // History Secret List는 UI에 직접 표시하지 않으므로 갱신 불필요
    }

    // 사용자 질문 전송
    public void SendQuestion(string query)
    {
        Debug.Log($"[20Q] 사용자 질문 전송: {query}");
        CallGameAPI(query);
    }

    // 게임 API 호출 (APIManager를 통해)
    private void CallGameAPI(string query)
    {
        string chatIdx = GameManager.Instance.chatIdxSuccess ?? "-1";
        string aiLanguage = SettingManager.Instance.settings.ai_language ?? "ko";
        string charName = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string serverType = SettingManager.Instance.settings.server_type ?? "Auto";

        APIManager.Instance.CallMiniGame20QStream(
            query: query,
            secret: secret,
            themeKey: themeKey,
            questionCount: questionCount,
            maxQuestions: maxQuestions,
            history: history,
            historyQuestion: historyQuestion,
            historySecretList: historySecretList,
            gameStatus: gameStatus,
            gameResult: gameResult,
            waitingFor: waitingFor,
            aiLanguage: aiLanguage,
            charName: charName,
            chatIdx: chatIdx,
            serverType: serverType
        );
    }

    /// <summary>
    /// 게임 상태에 따른 처리 (3-Field System)
    /// </summary>
    private void HandleGameStatus()
    {
        // game_status에 따른 처리
        switch (gameStatus)
        {
            case "game_start":
                Debug.Log($"[20Q] 게임 시작: 테마 = {theme}");
                break;
                
            case "playing":
                // 계속 진행 중
                if (waitingFor == "continue_or_giveup")
                {
                    Debug.Log("[20Q] 틀린 추측 - 계속/포기 선택 대기");
                }
                break;
                
            case "game_over":
                // 게임 종료
                Debug.Log($"[20Q] 게임 종료: 결과 = {gameResult}, 대기 = {waitingFor}");
                break;
                
            default:
                Debug.LogWarning($"[20Q] 알 수 없는 게임 상태: {gameStatus}");
                break;
        }
    }

    // 게임 재시작 요청
    public void RequestRestart()
    {
        if (!is20QMode)
        {
            Debug.LogWarning("[20Q] 스무고개 모드가 비활성화되어 있습니다.");
            return;
        }

        Debug.Log("[20Q] 게임 재시작 요청");
        
        // "다시 하고 싶어" 같은 재시작 의도 메시지 전송
        string aiLanguage = SettingManager.Instance.settings.ai_language ?? "ko";
        string restartMessage = aiLanguage == "ko" ? "다시 하고 싶어" : 
                               (aiLanguage == "ja" || aiLanguage == "jp" ? "もう一度やりたい" : 
                                "I want to play again");
        
        CallGameAPI(restartMessage);
    }

    // 포기 요청
    public void RequestGiveUp()
    {
        if (!is20QMode)
        {
            Debug.LogWarning("[20Q] 스무고개 모드가 비활성화되어 있습니다.");
            return;
        }

        Debug.Log("[20Q] 포기 요청");
        
        // "포기할게" 같은 포기 의도 메시지 전송
        string aiLanguage = SettingManager.Instance.settings.ai_language ?? "ko";
        string giveUpMessage = aiLanguage == "ko" ? "포기할게" : 
                              (aiLanguage == "ja" || aiLanguage == "jp" ? "諦めます" : 
                               "I give up");
        
        CallGameAPI(giveUpMessage);
    }

    // 계속하기 요청
    public void RequestContinue()
    {
        if (!is20QMode)
        {
            Debug.LogWarning("[20Q] 스무고개 모드가 비활성화되어 있습니다.");
            return;
        }

        Debug.Log("[20Q] 계속하기 요청");
        
        // "계속할게" 같은 계속 의도 메시지 전송
        string aiLanguage = SettingManager.Instance.settings.ai_language ?? "ko";
        string continueMessage = aiLanguage == "ko" ? "계속할게" : 
                                (aiLanguage == "ja" || aiLanguage == "jp" ? "続けます" : 
                                 "I'll continue");
        
        CallGameAPI(continueMessage);
    }

    /// <summary>
    /// 현재 게임 정보 가져오기 (3-Field System)
    /// </summary>
    public Dictionary<string, object> GetGameInfo()
    {
        return new Dictionary<string, object>
        {
            { "is_active", is20QMode },
            { "game_status", gameStatus },
            { "game_result", gameResult },
            { "waiting_for", waitingFor },
            { "theme_key", themeKey },
            { "theme", theme },
            { "secret", secret },
            { "question_count", questionCount },
            { "max_questions", maxQuestions },
            { "history_secret_list", historySecretList }
        };
    }
}

