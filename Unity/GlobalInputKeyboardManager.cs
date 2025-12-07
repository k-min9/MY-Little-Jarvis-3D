using UnityEngine;
using System;
using System.Runtime.InteropServices;

// 전역 키보드 입력 감지 매니저
public class GlobalInputKeyboardManager : MonoBehaviour
{
    private static GlobalInputKeyboardManager instance;
    public static GlobalInputKeyboardManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalInputKeyboardManager>();
            }
            return instance;
        }
    }

#if UNITY_STANDALONE_WIN
    // Windows API for keyboard hooking
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private LowLevelKeyboardProc _keyboardProc;
    private static IntPtr _keyboardHookID = IntPtr.Zero;
    private static GlobalInputKeyboardManager _instance;
    
    // 키 상태 추적 (키 반복 방지용)
    private System.Collections.Generic.HashSet<int> pressedKeys = new System.Collections.Generic.HashSet<int>();
#endif

    private bool inputDetectionEnabled = false;

    void Awake()
    {
        // 싱글톤 패턴 (중복이면 컴포넌트만 제거)
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_STANDALONE_WIN
        _instance = this;
        _keyboardProc = KeyboardHookCallback;
#endif
    }

    void Start()
    {
        Debug.Log("[GlobalInputKeyboardManager] 키보드 입력 관리자 시작됨 (후킹 기본 ON)");
#if UNITY_STANDALONE_WIN
        // 통계 수집/핫키용으로 기본 활성화 (HotKeyManager 설정에 따라 처리)
        SetInputDetection(true);
#endif
    }

#if UNITY_STANDALONE_WIN
    // 키보드 후킹 시작
    public void StartKeyboardHook()
    {
        if (_keyboardHookID == IntPtr.Zero)
        {
            _keyboardHookID = SetKeyboardHook(_keyboardProc);
            inputDetectionEnabled = true;
            Debug.Log("[GlobalInputKeyboardManager] 전역 키보드 후킹 활성화됨");
        }
    }

    // 키보드 후킹 중지
    public void StopKeyboardHook()
    {
        if (_keyboardHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            _keyboardHookID = IntPtr.Zero;
            inputDetectionEnabled = false;
            Debug.Log("[GlobalInputKeyboardManager] 전역 키보드 후킹 비활성화됨");
        }
    }

    // 키보드 후킹 설정
    private static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
    {
        using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // 키보드 후킹 콜백
    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _instance != null && _instance.inputDetectionEnabled)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            
            // KeyDown 처리
            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                // 이미 눌려진 키면 무시 (키 반복 방지)
                if (pressedKeys.Contains(vkCode))
                {
                    return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
                }
                
                // 키 상태 추가
                pressedKeys.Add(vkCode);
                
                string keyName = GetKeyName(vkCode);

                // F8 특수 키 처리 (DevManager 토글)
                if (vkCode == 0x77) // F8
                {
                    if (DevManager.Instance != null)
                    {
                        DevManager.Instance.ToggleShowSettingDevTab();
                    }
                }

                // HotKeyManager가 전역 핫키 활성화 상태면 VK Code 전달 (KeyDown)
                if (HotkeyManager.Instance != null && HotkeyManager.Instance.IsGlobalHotkeyEnabled())
                {
                    HotkeyManager.Instance.HandleVirtualKeyCode(vkCode, true);
                }

                // GlobalInputVariableManager 활성화 체크 후 키보드 입력 통계 추가
                if (GlobalInputVariableManager.Instance != null)
                {
                    GlobalInputVariableManager.Instance.AddKeyboardInput(keyName);
                }
            }
            
            // KeyUp 처리
            if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                // 키 상태 제거
                pressedKeys.Remove(vkCode);
                
                // HotKeyManager가 전역 핫키 활성화 상태면 VK Code 전달 (KeyUp)
                if (HotkeyManager.Instance != null && HotkeyManager.Instance.IsGlobalHotkeyEnabled())
                {
                    HotkeyManager.Instance.HandleVirtualKeyCode(vkCode, false);
                }
            }
        }

        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
    }

    void OnApplicationQuit()
    {
        StopKeyboardHook();
    }

    void OnDestroy()
    {
        StopKeyboardHook();
    }
#endif

    // 입력 감지 활성화/비활성화
    public void SetInputDetection(bool enabled)
    {
#if UNITY_STANDALONE_WIN
        if (enabled)
        {
            StartKeyboardHook();
        }
        else
        {
            StopKeyboardHook();
        }
#endif
    }

    public bool IsInputDetectionEnabled()
    {
        return inputDetectionEnabled;
    }

    // Virtual Key Code를 키 이름으로 변환
    private static string GetKeyName(int vkCode)
    {
        switch (vkCode)
        {
            case 0x08: return "백스페이스";
            case 0x09: return "탭";
            case 0x0D: return "엔터";
            case 0x10: return "Shift";
            case 0x11: return "Ctrl";
            case 0x12: return "Alt";
            case 0x1B: return "ESC";
            case 0x20: return "스페이스";
            case 0x25: return "왼쪽화살표";
            case 0x26: return "위쪽화살표";
            case 0x27: return "오른쪽화살표";
            case 0x28: return "아래쪽화살표";
            case 0x2E: return "Delete";
            default:
                if (vkCode >= 0x30 && vkCode <= 0x39) return ((char)vkCode).ToString(); // 0-9
                if (vkCode >= 0x41 && vkCode <= 0x5A) return ((char)vkCode).ToString(); // A-Z
                if (vkCode >= 0x70 && vkCode <= 0x7B) return $"F{vkCode - 0x6F}"; // F1-F12
                return $"VK_{vkCode:X}";
        }
    }
}

