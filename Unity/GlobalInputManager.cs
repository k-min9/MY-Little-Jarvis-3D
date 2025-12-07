using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

/// 전역입력 감지 매니저
/// 단 버벅임이 발생함.필요한 구간에서 필요할때만 사용하기
public class GlobalInputManager : MonoBehaviour
{
    private static GlobalInputManager instance;
    
    // 체크할 키 목록
    private KeyCode[] keysToCheck = new KeyCode[] 
    {
        KeyCode.Return,  // 엔터
        KeyCode.KeypadEnter  // 넘패드 엔터
    };
    
    // 입력 감지 쓰로틀링 설정
    private const float MOUSE_DISTANCE_UPDATE_INTERVAL = 0.1f; // 100ms마다 (초당 10회)
    private float lastMouseDistanceUpdateTime = 0f;
    
    // 마우스 이전 위치 저장
    private Vector3 lastMousePosition;
    
    // 카메라 캐싱 (Camera.main 반복 호출 방지)
    private Camera mainCamera;
    
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
        mainCamera = Camera.main; // 카메라 캐싱
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
        // DetectMouseMovement();
    }

    void DetectMouseInput()
    {
        // 마우스 버튼 클릭 감지 (패스스루)
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = mainCamera != null ? mainCamera.ScreenToWorldPoint(mousePos) : Vector3.zero;
            LogToDebugBalloon($"[GlobalInput] 마우스 왼쪽 클릭 - 스크린: {mousePos}, 월드: {worldPos}");
            
            // GlobalInputVariableManager에 좌클릭 통계 추가
            if (GlobalInputVariableManager.Instance != null)
            {
                GlobalInputVariableManager.Instance.AddLeftClick();
            }
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePos = Input.mousePosition;
            LogToDebugBalloon($"[GlobalInput] 마우스 오른쪽 클릭 - 스크린: {mousePos}");
            
            // GlobalInputVariableManager에 우클릭 통계 추가
            if (GlobalInputVariableManager.Instance != null)
            {
                GlobalInputVariableManager.Instance.AddRightClick();
            }
        }
        
        if (Input.GetMouseButtonDown(2))
        {
            Vector3 mousePos = Input.mousePosition;
            LogToDebugBalloon($"[GlobalInput] 마우스 휠 클릭 - 스크린: {mousePos}");
            
            // GlobalInputVariableManager에 휠클릭 통계 추가
            if (GlobalInputVariableManager.Instance != null)
            {
                GlobalInputVariableManager.Instance.AddMiddleClick();
            }
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
        
        // 마우스 이동거리 통계 (Unity Input 버전) - 쓰로틀링 적용
        float currentTime = Time.time;
        if (GlobalInputVariableManager.Instance != null && 
            currentTime - lastMouseDistanceUpdateTime >= MOUSE_DISTANCE_UPDATE_INTERVAL)
        {
            Vector3 currentMousePos = Input.mousePosition;
            GlobalInputVariableManager.Instance.UpdateMouseDistance(new Vector2(currentMousePos.x, currentMousePos.y));
            lastMouseDistanceUpdateTime = currentTime;
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
        // 필요한 키만 체크 (성능 최적화)
        foreach (KeyCode keyCode in keysToCheck)
        {
            if (Input.GetKeyDown(keyCode))
            {
                LogToDebugBalloon($"[GlobalInput] 키 입력: {keyCode}");
                
                // GlobalInputVariableManager에 키보드 입력 통계 추가
                if (GlobalInputVariableManager.Instance != null)
                {
                    GlobalInputVariableManager.Instance.AddKeyboardInput(keyCode.ToString());
                }
                if (keyCode == KeyCode.F12)
                {
                    PressF12();
                }
            }
        }
        
        // 입력된 문자 표시(로깅 전용). 통계는 KeyDown에서만 집계하여 중복 방지
        foreach (char c in Input.inputString)
        {
            if (c != 0)
            {
                string displayChar = c.ToString();
                if (c == '\b') displayChar = "백스페이스";
                else if (c == '\n' || c == '\r') displayChar = "엔터";
                else if (c == '\t') displayChar = "탭";
                else if (c == ' ') displayChar = "스페이스";
                else if (char.IsControl(c)) displayChar = $"제어문자({(int)c})";

                LogToDebugBalloon($"[GlobalInput] 문자 입력: '{displayChar}' (코드: {(int)c})");
                // 통계 증가 제거: KeyDown에서만 처리
            }
        }
        #endif
        // Windows 빌드에서는 전역 후킹으로 키보드 입력 감지 (HookCallback에서 처리)
    }

    // 마우스 KeyCode 여부 판별 (키보드 집계에서 제외하기 위함)
    private static bool IsMouseKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Mouse0:
            case KeyCode.Mouse1:
            case KeyCode.Mouse2:
            case KeyCode.Mouse3:
            case KeyCode.Mouse4:
            case KeyCode.Mouse5:
            case KeyCode.Mouse6:
                return true;
            default:
                return false;
        }
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
                
                if (vkCode == 0x7B) // F12
                {
                    _instance.PressF12();
                }
                
                // GlobalInputVariableManager에 키보드 입력 통계 추가
                if (GlobalInputVariableManager.Instance != null)
                {
                    GlobalInputVariableManager.Instance.AddKeyboardInput(keyName);
                }
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
                    // GlobalInputVariableManager에 좌클릭 통계 추가
                    if (GlobalInputVariableManager.Instance != null)
                    {
                        GlobalInputVariableManager.Instance.AddLeftClick();
                    }
                    break;
                case WM_LBUTTONUP:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 왼쪽 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_RBUTTONDOWN:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 오른쪽 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    // GlobalInputVariableManager에 우클릭 통계 추가
                    if (GlobalInputVariableManager.Instance != null)
                    {
                        GlobalInputVariableManager.Instance.AddRightClick();
                    }
                    break;
                case WM_RBUTTONUP:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 오른쪽 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_MBUTTONDOWN:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 휠 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    // GlobalInputVariableManager에 휠클릭 통계 추가
                    if (GlobalInputVariableManager.Instance != null)
                    {
                        GlobalInputVariableManager.Instance.AddMiddleClick();
                    }
                    break;
                case WM_MBUTTONUP:
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 휠 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_MOUSEWHEEL:
                    int wheelDelta = (short)((Marshal.ReadInt32(lParam, 8) >> 16) & 0xFFFF);
                    string direction = wheelDelta > 0 ? "위로" : "아래로";
                    _instance.LogToDebugBalloon($"[GlobalInput] 전역 마우스 휠 스크롤 {direction} - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
                case WM_MOUSEMOVE:
                    // 마우스 이동거리 통계 추가 - 쓰로틀링 적용
                    float currentTime = Time.time;
                    if (GlobalInputVariableManager.Instance != null &&
                        currentTime - _instance.lastMouseDistanceUpdateTime >= MOUSE_DISTANCE_UPDATE_INTERVAL)
                    {
                        GlobalInputVariableManager.Instance.UpdateMouseDistanceFromScreenCoords(mousePos.x, mousePos.y);
                        _instance.lastMouseDistanceUpdateTime = currentTime;
                    }
                    break;
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
    
    // DebugBalloon에 로그를 출력하는 메서드(Test용)
    private void LogToDebugBalloon(string message)
    {
        // DebugBalloonManager가 있으면 사용, 없으면 일반 Debug.Log 사용
        if (DebugBalloonManager.Instance != null)
        {
            // InputManager에서는 더 이상 표기 안함
            // DebugBalloonManager.Instance.AddDebugLog(message);
            // Debug.Log(message);
        }
        else
        {
            // Debug.Log(message);
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

    // F12 키 입력 시 호출되는 함수
    public void PressF12()
    {
        if (DevManager.Instance != null)
        {
            DevManager.Instance.ToggleShowSettingDevTab();
        }
    }
}
