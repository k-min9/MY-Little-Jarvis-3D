using System;
using UnityEngine;

// 대화 모드(Chat, Aropla, Operator) 변경 기능을 처리하는 에이전트 기능 클래스
public class ApiAgentFunctionChatMode : MonoBehaviour
{
    private static ApiAgentFunctionChatMode instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionChatMode Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiAgentFunctionChatMode>();
            }
            return instance;
        }
    }

    // 모드 문자열을 ChatMode enum으로 파싱하는 헬퍼 메서드
    private bool TryParseChatMode(string modeStr, out ChatMode result)
    {
        result = ChatMode.Chat;
        if (string.IsNullOrEmpty(modeStr))
        {
            return false;
        }

        string normalized = modeStr.Trim().ToLower();
        if (normalized == "chat")
        {
            result = ChatMode.Chat;
            return true;
        }
        else if (normalized == "aropla")
        {
            result = ChatMode.Aropla;
            return true;
        }
        else if (normalized == "operator")
        {
            result = ChatMode.Operator;
            return true;
        }
        return false;
    }

    // 대화 모드 변경 실행
    public bool SetChatMode(string modeStr)
    {
        if (TryParseChatMode(modeStr, out ChatMode mode))
        {
            ChatModeManager.Instance.SetMode(mode);
            return true;
        }
        Debug.LogWarning($"[ApiAgentFunctionChatMode] 유효하지 않은 ChatMode 문자열: {modeStr}");
        return false;
    }

    // 대화 모드 토글 실행
    public bool ToggleChatMode(string modeStr)
    {
        if (TryParseChatMode(modeStr, out ChatMode mode))
        {
            ChatModeManager.Instance.ToggleMode(mode);
            return true;
        }
        Debug.LogWarning($"[ApiAgentFunctionChatMode] 유효하지 않은 토글 ChatMode 문자열: {modeStr}");
        return false;
    }

    // 현재 대화 모드 반환
    public string GetChatMode()
    {
        return ChatModeManager.Instance.CurrentMode.ToString();
    }
}
