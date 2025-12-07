using UnityEngine;
using System;
using System.Runtime.InteropServices;

// 전역 마우스 이동 입력 감지 매니저 (성능 이슈로 기본 비활성화)
// 주의: 매우 버벅임 발생 가능, 필요시에만 활성화하여 테스트
public class GlobalInputMouseMoveManager : MonoBehaviour
{
    private static GlobalInputMouseMoveManager instance;
    public static GlobalInputMouseMoveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalInputMouseMoveManager>();
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
    private const int WM_MOUSEMOVE = 0x0200;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private LowLevelMouseProc _mouseProc;
    private static IntPtr _mouseHookID = IntPtr.Zero;
    private static GlobalInputMouseMoveManager _instance;
#endif

    private bool inputDetectionEnabled = false;

    // 쓰로틀링 간격 (하이퍼파라미터 조절 가능)
    [SerializeField] private float updateInterval = 0.1f; // 기본 100ms (초당 10회)
    private float lastUpdateTime = 0f;

    void Awake()
    {
        // 싱글톤 패턴
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_STANDALONE_WIN
            _instance = this;
            _mouseProc = MouseMoveCallback;
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("[GlobalInputMouseMoveManager] 마우스 이동 입력 관리자 시작됨 (기본 비활성 상태)");
        Debug.LogWarning("[GlobalInputMouseMoveManager] 주의: 이 매니저는 성능 이슈로 기본 비활성화됩니다. 필요시에만 수동으로 활성화하세요.");
    }

#if UNITY_STANDALONE_WIN
    // 마우스 후킹 시작
    public void StartMouseHook()
    {
        if (_mouseHookID == IntPtr.Zero)
        {
            _mouseHookID = SetMouseHook(_mouseProc);
            inputDetectionEnabled = true;
            lastUpdateTime = Time.time;
            Debug.Log("[GlobalInputMouseMoveManager] 전역 마우스 이동 후킹 활성화됨");
            Debug.LogWarning($"[GlobalInputMouseMoveManager] 쓰로틀링 간격: {updateInterval}초");
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
            Debug.Log("[GlobalInputMouseMoveManager] 전역 마우스 이동 후킹 비활성화됨");
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

    // 마우스 후킹 콜백 (이동만 처리)
    private IntPtr MouseMoveCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _instance != null && _instance.inputDetectionEnabled)
        {
            if ((int)wParam == WM_MOUSEMOVE)
            {
                // 쓰로틀링: 지정된 간격마다만 통계 업데이트
                float currentTime = Time.time;
                if (currentTime - _instance.lastUpdateTime >= _instance.updateInterval)
                {
                    POINT mousePos;
                    GetCursorPos(out mousePos);

                    // GlobalInputVariableManager 활성화 체크 후 마우스 이동거리 통계 추가
                    if (GlobalInputVariableManager.Instance != null)
                    {
                        GlobalInputVariableManager.Instance.UpdateMouseDistanceFromScreenCoords(mousePos.x, mousePos.y);
                    }

                    _instance.lastUpdateTime = currentTime;
                }
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

    // 쓰로틀링 간격 설정 (하이퍼파라미터 조절용)
    public void SetUpdateInterval(float interval)
    {
        if (interval < 0.01f)
        {
            Debug.LogWarning("[GlobalInputMouseMoveManager] 간격이 너무 짧습니다. 최소 0.01초로 설정됩니다.");
            interval = 0.01f;
        }

        updateInterval = interval;
        Debug.Log($"[GlobalInputMouseMoveManager] 쓰로틀링 간격 변경: {updateInterval}초 (초당 {1f / updateInterval:F1}회)");
    }

    public float GetUpdateInterval()
    {
        return updateInterval;
    }
}

