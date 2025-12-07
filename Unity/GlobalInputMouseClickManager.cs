using UnityEngine;
using System;
using System.Runtime.InteropServices;

// 전역 마우스 클릭 입력 감지 매니저 (이동 제외)
public class GlobalInputMouseClickManager : MonoBehaviour
{
    private static GlobalInputMouseClickManager instance;
    public static GlobalInputMouseClickManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalInputMouseClickManager>();
            }
            return instance;
        }
    }

#if UNITY_STANDALONE_WIN
    // Windows API for mouse hooking
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private LowLevelMouseProc _mouseProc;
    private static IntPtr _mouseHookID = IntPtr.Zero;
    private static GlobalInputMouseClickManager _instance;
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
        _mouseProc = MouseClickCallback;
#endif
    }

    void Start()
    {
        Debug.Log("[GlobalInputMouseClickManager] 마우스 클릭 입력 관리자 시작됨 (후킹 기본 ON)");
#if UNITY_STANDALONE_WIN
        // 통계 수집을 위해 기본 활성화
        SetInputDetection(true);
#endif
    }

#if UNITY_STANDALONE_WIN
    // 마우스 후킹 시작
    public void StartMouseHook()
    {
        if (_mouseHookID == IntPtr.Zero)
        {
            _mouseHookID = SetMouseHook(_mouseProc);
            inputDetectionEnabled = true;
            Debug.Log("[GlobalInputMouseClickManager] 전역 마우스 클릭 후킹 활성화됨");
        }
    }

    // 마우스 후킹 중지
    public void StopMouseHook()
    {
        if (_mouseHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookID);
            _mouseHookID = IntPtr.Zero;
            inputDetectionEnabled = false;
            Debug.Log("[GlobalInputMouseClickManager] 전역 마우스 클릭 후킹 비활성화됨");
        }
    }

    // 마우스 후킹 설정
    private static IntPtr SetMouseHook(LowLevelMouseProc proc)
    {
        using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // 마우스 후킹 콜백 (클릭만 처리, 이동은 제외)
    private IntPtr MouseClickCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _instance != null && _instance.inputDetectionEnabled)
        {
            POINT mousePos;
            GetCursorPos(out mousePos);

            switch ((int)wParam)
            {
                case WM_LBUTTONDOWN:
                    LogToDebug($"[GlobalInputMouseClick] 전역 마우스 왼쪽 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    // GlobalInputVariableManager 활성화 체크 후 좌클릭 통계 추가
                    if (GlobalInputVariableManager.Instance != null)
                    {
                        GlobalInputVariableManager.Instance.AddLeftClick();
                    }
                    break;

                case WM_LBUTTONUP:
                    LogToDebug($"[GlobalInputMouseClick] 전역 마우스 왼쪽 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;

                case WM_RBUTTONDOWN:
                    LogToDebug($"[GlobalInputMouseClick] 전역 마우스 오른쪽 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    // GlobalInputVariableManager 활성화 체크 후 우클릭 통계 추가
                    if (GlobalInputVariableManager.Instance != null)
                    {
                        GlobalInputVariableManager.Instance.AddRightClick();
                    }
                    break;

                case WM_RBUTTONUP:
                    LogToDebug($"[GlobalInputMouseClick] 전역 마우스 오른쪽 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;

                case WM_MBUTTONDOWN:
                    LogToDebug($"[GlobalInputMouseClick] 전역 마우스 휠 클릭 - 위치: ({mousePos.x}, {mousePos.y})");
                    // GlobalInputVariableManager 활성화 체크 후 휠클릭 통계 추가
                    if (GlobalInputVariableManager.Instance != null)
                    {
                        GlobalInputVariableManager.Instance.AddMiddleClick();
                    }
                    break;

                case WM_MBUTTONUP:
                    LogToDebug($"[GlobalInputMouseClick] 전역 마우스 휠 릴리즈 - 위치: ({mousePos.x}, {mousePos.y})");
                    break;

                case WM_MOUSEWHEEL:
                    int wheelDelta = (short)((Marshal.ReadInt32(lParam, 8) >> 16) & 0xFFFF);
                    string direction = wheelDelta > 0 ? "위로" : "아래로";
                    LogToDebug($"[GlobalInputMouseClick] 전역 마우스 휠 스크롤 {direction} - 위치: ({mousePos.x}, {mousePos.y})");
                    break;
            }
        }

        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
    }

    void OnApplicationQuit()
    {
        StopMouseHook();
    }

    void OnDestroy()
    {
        StopMouseHook();
    }
#endif

    // 입력 감지 활성화/비활성화
    public void SetInputDetection(bool enabled)
    {
#if UNITY_STANDALONE_WIN
        if (enabled)
        {
            StartMouseHook();
        }
        else
        {
            StopMouseHook();
        }
#endif
    }

    public bool IsInputDetectionEnabled()
    {
        return inputDetectionEnabled;
    }

    // Debug 로그 출력
    private void LogToDebug(string message)
    {
        // Debug.Log(message);
    }
}

