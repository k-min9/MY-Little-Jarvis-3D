using UnityEngine;

// ChatMode 열거형 - 대화 모드 종류
public enum ChatMode
{
    Chat,       // 기본: 메인 캐릭터 1:1 대화
    Aropla,     // 아로나+프라나 3자 대화
    Operator    // Operator(아로나)만 표시, 메인 캐릭터 숨김
}

// ChatMode 중앙 관리자 (라우팅 전용)
// 모드 전환 요청을 받아 해당 Manager에 위임
// 외부에서는 이 Manager만 호출해야 함
public class ChatModeManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static ChatModeManager instance;
    public static ChatModeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChatModeManager>();
            }
            return instance;
        }
    }

    // 현재 모드
    public ChatMode CurrentMode { get; private set; } = ChatMode.Chat;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 모드 설정 (전환) - 유일한 진입점
    public void SetMode(ChatMode newMode)
    {
        if (CurrentMode == newMode) return;

        ChatMode previousMode = CurrentMode;
        Debug.Log($"[ChatModeManager] Switching mode: {previousMode} → {newMode}");

        // 1. 현재 모드 종료 (해당 Manager에 위임)
        ExitMode(previousMode);

        // 2. 새 모드 진입 (해당 Manager에 위임)
        EnterMode(newMode);

        CurrentMode = newMode;
    }

    // 특정 모드 토글 (이미 해당 모드면 Chat으로 복귀)
    public void ToggleMode(ChatMode targetMode)
    {
        if (CurrentMode == targetMode)
        {
            SetMode(ChatMode.Chat);
        }
        else
        {
            SetMode(targetMode);
        }
    }

    // 모드 종료 처리 (위임)
    private void ExitMode(ChatMode mode)
    {
        switch (mode)
        {
            case ChatMode.Chat:
                // Chat 모드는 종료 시 특별한 처리 없음
                break;
            case ChatMode.Aropla:
                // APIAroPlaManager에 위임
                APIAroPlaManager.Instance.StopAroplaChannel();
                break;
            case ChatMode.Operator:
                // OperatorModeManager에 위임
                OperatorModeManager.Instance.ExitOperatorMode();
                break;
        }
    }

    // 모드 진입 처리 (위임)
    private void EnterMode(ChatMode mode)
    {
        switch (mode)
        {
            case ChatMode.Chat:
                // Chat 모드는 진입 시 특별한 처리 없음 (각 Exit에서 복원 처리)
                break;
            case ChatMode.Aropla:
                // APIAroPlaManager에 위임
                APIAroPlaManager.Instance.StartAroplaChannel();
                break;
            case ChatMode.Operator:
                // OperatorModeManager에 위임
                OperatorModeManager.Instance.EnterOperatorMode();
                break;
        }
    }

    // ============ 편의 메서드 ============
    public bool IsOperatorMode() => CurrentMode == ChatMode.Operator;
    public bool IsAroplaMode() => CurrentMode == ChatMode.Aropla;
    public bool IsChatMode() => CurrentMode == ChatMode.Chat;
}
