using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class GlobalInputManager : MonoBehaviour
{
    private static GlobalInputManager instance;
    
    // 마우스 이전 위치 저장
    private Vector3 lastMousePosition;
    
    // Windows API 키보드 후킹
    #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    
    // 마우스 메시지 상수들
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEMOVE = 0x0200;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    private LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;
    private LowLevelMouseProc _mouseProc = MouseHookCallback;
    private static IntPtr _keyboardHookID = IntPtr.Zero;
    private static IntPtr _mouseHookID = IntPtr.Zero;
    private static GlobalInputManager _instance;
    #endif
    
    void Awake()
    {
        // 싱글톤 패턴
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            _instance = this;
            #endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        lastMousePosition = Input.mousePosition;
        LogToDebugBalloon("[GlobalInputManager] 전역 입력 관리자 시작됨");
        
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // 전역 키보드 후킹 설정
        _keyboardHookID = SetKeyboardHook(_keyboardProc);
        LogToDebugBalloon("[GlobalInputManager] 전역 키보드 후킹 활성화됨");
        
        // 전역 마우스 후킹 설정
        _mouseHookID = SetMouseHook(_mouseProc);
        LogToDebugBalloon("[GlobalInputManager] 전역 마우스 후킹 활성화됨");
        #endif
    }

    void Update()
    {
        // 입력 감지가 비활성화되어 있으면 리턴
        if (!inputDetectionEnabled) return;
        
        // 마우스 클릭 감지
        DetectMouseInput();
        
        // 터치 입력 감지 (안드로이드)
        DetectTouchInput();
        
        // 키보드 입력 감지
        DetectKeyboardInput();
        
        // 마우스 이동 감지
        DetectMouseMovement();
    }

    void DetectMouseInput()
    {
        // 마우스 버튼 클릭 감지 (패스스루)
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            LogToDebugBalloon($"[GlobalInput] 마우스 왼쪽 클릭 - 스크린: {mousePos}, 월드: {worldPos}");
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Input.mousePosition;
            LogToDebugBalloon($"[GlobalInput] 마우스 오른쪽 클릭 - 스크린: {mousePos}");
        }
        
        if (Input.GetMouseButtonDown(2))
        {
            Vector3 mousePos = Input.mousePosition;
            LogToDebugBalloon($"[GlobalInput] 마우스 휠 클릭 - 스크린: {mousePos}");
        }
        
        // 마우스 버튼 릴리즈 감지
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mousePos = Input.mousePosition;
            LogToDebugBalloon($"[GlobalInput] 마우스 왼쪽 버튼 릴리즈 - 스크린: {mousePos}");
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            Vector3 mousePos = Input.mousePosition;
            LogToDebugBalloon($"[GlobalInput] 마우스 오른쪽 버튼 릴리즈 - 스크린: {mousePos}");
        }
        
        // 마우스 휠 스크롤 감지
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            LogToDebugBalloon($"[GlobalInput] 마우스 휠 스크롤: {scroll} (위: +, 아래: -)");
        }
    }

    void DetectTouchInput()
    {
        // 안드로이드 터치 입력 감지
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        LogToDebugBalloon($"[GlobalInput] 터치 시작 [{i}] - 위치: {touch.position}, 델타시간: {touch.deltaTime}");
                        break;
                        
                    case TouchPhase.Moved:
                        LogToDebugBalloon($"[GlobalInput] 터치 이동 [{i}] - 위치: {touch.position}, 델타: {touch.deltaPosition}");
                        break;
                        
                    case TouchPhase.Stationary:
                        // 너무 많은 로그 방지를 위해 주석처리
                        // Debug.Log($"[GlobalInput] 터치 정지 [{i}] - 위치: {touch.position}");
                        break;
                        
                    case TouchPhase.Ended:
                        LogToDebugBalloon($"[GlobalInput] 터치 종료 [{i}] - 위치: {touch.position}, 지속시간: {touch.deltaTime}");
                        break;
                        
                    case TouchPhase.Canceled:
                        LogToDebugBalloon($"[GlobalInput] 터치 취소 [{i}] - 위치: {touch.position}");
                        break;
                }
            }
        }
    }

    void DetectKeyboardInput()
    {
        // 에디터나 Windows가 아닌 환경에서는 기본 Unity Input 사용
        #if UNITY_EDITOR || !UNITY_STANDALONE_WIN
        // 모든 키 입력 감지 (KeyCode 기반)
        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                LogToDebugBalloon($"[GlobalInput] 키 입력: {keyCode}");
            }
        }
        
        // 입력된 문자도 추가로 표시 (한글, 특수문자 등)
        foreach (char c in Input.inputString)
        {
            if (c != 0) // null 문자가 아닌 모든 문자
            {
                string displayChar = c.ToString();
                if (c == '\b') displayChar = "백스페이스";
                else if (c == '\n' || c == '\r') displayChar = "엔터";
                else if (c == '\t') displayChar = "탭";
                else if (c == ' ') displayChar = "스페이스";
                else if (char.IsControl(c)) displayChar = $"제어문자({(int)c})";
                
                LogToDebugBalloon($"[GlobalInput] 문자 입력: '{displayChar}' (코드: {(int)c})");
            }
        }
        #endif
        // Windows 빌드에서는 전역 후킹으로 키보드 입력 감지 (HookCallback에서 처리)
    }

    #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    // 키보드 후킹 설정
    private static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
    {
        using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, (Delegate)proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // 마우스 후킹 설정
    private static IntPtr SetMouseHook(LowLevelMouseProc proc)
    {
        using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, (Delegate)proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // 키보드 후킹 콜백
    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _instance != null && _instance.inputDetectionEnabled)
        {
            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                string keyName = GetKeyName(vkCode);
                _instance.LogToDebugBalloon($"[GlobalInput] 전역 키 입력: {keyName} (VK:{vkCode})");
            }
        }
        
        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
    }

    // 마우스 후킹 콜백
    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _instance != null && _instance.inputDetectionEnabled)
        {
            POINT mousePos;
            GetCursorPos(out mousePos);

            switch ((int)wParam)
            {
                case WM_LBUTTONDOWN:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 왼쪽 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_LBUTTONUP:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 왼쪽 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_RBUTTONDOWN:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 오른쪽 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_RBUTTONUP:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 오른쪽 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_MBUTTONDOWN:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 휠 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_MBUTTONUP:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 휠 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_MOUSEWHEEL:
                    int wheelDelta = (short)((Marshal.ReadInt32(lParam, 8) >> 16) & 0xFFFF);
                    string direction = wheelDelta > 0 ? "위로" : "아래로";
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 휠 스크롤 {direction} - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                // WM_MOUSEMOVE는 너무 많은 로그를 생성하므로 주석처리
                // case WM_MOUSEMOVE:
                //     _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 이동 - 위치: ({mousePos.x}, {mousePos.y})");
                //     break;
            }
        }
        
        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
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

    // 애플리케이션 종료 시 후킹 해제
    void OnApplicationQuit()
    {
        if (_keyboardHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            LogToDebugBalloon("[GlobalInputManager] 전역 키보드 후킹 해제됨");
        }
        
        if (_mouseHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookID);
            LogToDebugBalloon("[GlobalInputManager] 전역 마우스 후킹 해제됨");
        }
    }

    void OnDestroy()
    {
        if (_keyboardHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookID);
        }
        
        if (_mouseHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookID);
        }
    }
    #endif

    void DetectMouseMovement()
    {
        Vector3 currentMousePos = Input.mousePosition;
        
        // 마우스가 움직였을 때만 로그 (너무 많은 로그 방지)
        if (Vector3.Distance(currentMousePos, lastMousePosition) > 5f) // 5픽셀 이상 움직일 때만
        {
            Vector3 delta = currentMousePos - lastMousePosition;
            LogToDebugBalloon($"[GlobalInput] 마우스 이동: {currentMousePos} (델타: {delta})");
            lastMousePosition = currentMousePos;
        }
    }

    // 정적 메서드로 외부에서 접근 가능
    public static GlobalInputManager Instance
    {
        get { return instance; }
    }
    
    // 입력 감지 활성화/비활성화
    private bool inputDetectionEnabled = true;
    
    // DebugBalloon에 로그를 출력하는 메서드
    private void LogToDebugBalloon(string message)
    {
        // DebugBalloonManager가 있으면 사용, 없으면 일반 Debug.Log 사용
        if (DebugBalloonManager.Instance != null)
        {
            DebugBalloonManager.Instance.AddDebugLog(message);
            Debug.Log(message);
        }
        else
        {
            Debug.Log(message);
        }
    }
    
    public void SetInputDetection(bool enabled)
    {
        inputDetectionEnabled = enabled;
        LogToDebugBalloon($"[GlobalInputManager] 입력 감지 {(enabled ? "활성화" : "비활성화")}");
    }
    
    public bool IsInputDetectionEnabled()
    {
        return inputDetectionEnabled;
    }
}
