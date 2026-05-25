using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

// 키보드 및 단축키 제어 클래스
public class ApiAgentFunctionKeyboardAction : MonoBehaviour
{
    private static ApiAgentFunctionKeyboardAction instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionKeyboardAction Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<ApiAgentFunctionKeyboardAction>();
            }
            return instance;
        }
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private const uint KEYEVENTF_KEYUP = 0x0002;

    // 가상 키 코드 맵핑 헬퍼
    private byte GetVirtualKeyCode(string keyName)
    {
        keyName = keyName.ToUpper();
        
        if (keyName == "CTRL" || keyName == "CONTROL")
        {
            return 0x11; // VK_CONTROL
        }
        else if (keyName == "ALT")
        {
            return 0x12; // VK_MENU
        }
        else if (keyName == "SHIFT")
        {
            return 0x10; // VK_SHIFT
        }
        else if (keyName == "ENTER" || keyName == "RETURN")
        {
            return 0x0D; // VK_RETURN
        }
        else if (keyName == "TAB")
        {
            return 0x09; // VK_TAB
        }
        else if (keyName.Length == 1)
        {
            // 단일 문자 (A-Z, 0-9 등)
            return (byte)keyName[0];
        }
        else
        {
            Debug.LogWarning($"[ApiAgentFunctionKeyboardAction] 알 수 없는 키보드 키: {keyName}");
            return 0;
        }
    }

    // 텍스트 타이핑
    public void TypeText(string text)
    {
        // 간단한 시뮬레이션용 (단일 문자씩 전송)
        foreach (char c in text)
        {
            string keyName = c.ToString().ToUpper();
            byte vkCode = GetVirtualKeyCode(keyName);
            
            if (vkCode != 0)
            {
                keybd_event(vkCode, 0, 0, 0); // KeyDown
                Thread.Sleep(10);
                keybd_event(vkCode, 0, KEYEVENTF_KEYUP, 0); // KeyUp
                Thread.Sleep(10);
            }
        }
        
        Debug.Log($"[ApiAgentFunctionKeyboardAction] 타이핑 완료: {text}");
    }

    // 단축키 전송 (Modifier + Key)
    public void SendHotkey(string modifier, string key)
    {
        byte modCode = GetVirtualKeyCode(modifier);
        byte keyCode = GetVirtualKeyCode(key);
        
        if (modCode != 0 && keyCode != 0)
        {
            keybd_event(modCode, 0, 0, 0); // Modifier KeyDown
            Thread.Sleep(50);
            
            keybd_event(keyCode, 0, 0, 0); // Main KeyDown
            Thread.Sleep(50);
            
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP, 0); // Main KeyUp
            Thread.Sleep(50);
            
            keybd_event(modCode, 0, KEYEVENTF_KEYUP, 0); // Modifier KeyUp
            
            Debug.Log($"[ApiAgentFunctionKeyboardAction] 단축키 송신: {modifier} + {key}");
        }
    }
#else
    public void TypeText(string text)
    {
        Debug.LogWarning("[ApiAgentFunctionKeyboardAction] Windows 환경에서만 지원됩니다.");
    }
    public void SendHotkey(string modifier, string key)
    {
        Debug.LogWarning("[ApiAgentFunctionKeyboardAction] Windows 환경에서만 지원됩니다.");
    }
#endif
}
