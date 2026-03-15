using UnityEngine;
using System.Runtime.InteropServices;

// Win32 API를 사용한 마우스 클릭 시뮬레이션 매니저
public class ExecutorMouseAction : MonoBehaviour
{
    private static ExecutorMouseAction instance;
    public static ExecutorMouseAction Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ExecutorMouseAction");
                instance = go.AddComponent<ExecutorMouseAction>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

#if UNITY_STANDALONE_WIN
    // Win32 API: 마우스 커서 위치 설정
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    // Win32 API: 마우스 커서 위치 가져오기
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    // Win32 API: 마우스 이벤트 시뮬레이션
    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    // 마우스 이벤트 상수
    private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const int MOUSEEVENTF_LEFTUP = 0x0004;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const int MOUSEEVENTF_RIGHTUP = 0x0010;

    /// <summary>
    /// 특정 화면 좌표로 마우스를 이동하고 왼쪽 클릭
    /// </summary>
    /// <param name="x">클릭할 X 좌표</param>
    /// <param name="y">클릭할 Y 좌표</param>
    /// <param name="isMouseMove">true: 마우스 커서 이동 유지, false: 클릭 후 원래 위치로 복원</param>
    public void ClickAtPosition(int x, int y, bool isMouseMove = true)
    {
        POINT originalPos = default;
        if (!isMouseMove)
        {
            GetCursorPos(out originalPos);
        }

        Debug.Log($"[ExecutorMouseAction] Moving cursor to ({x}, {y})");
        
        // 마우스 커서 이동
        bool moveSuccess = SetCursorPos(x, y);
        if (!moveSuccess)
        {
            Debug.LogError($"[ExecutorMouseAction] Failed to move cursor to ({x}, {y})");
            return;
        }

        // 약간의 딜레이 (마우스 이동 안정화)
        System.Threading.Thread.Sleep(50);

        // 왼쪽 클릭 시뮬레이션 (Down -> Up)
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

        Debug.Log($"[ExecutorMouseAction] Clicked at ({x}, {y})");

        // 원래 위치로 복원
        if (!isMouseMove)
        {
            System.Threading.Thread.Sleep(50);
            SetCursorPos(originalPos.X, originalPos.Y);
            Debug.Log($"[ExecutorMouseAction] Restored cursor to ({originalPos.X}, {originalPos.Y})");
        }
    }

    /// <summary>
    /// 특정 화면 좌표로 마우스를 이동하고 오른쪽 클릭
    /// </summary>
    /// <param name="x">클릭할 X 좌표</param>
    /// <param name="y">클릭할 Y 좌표</param>
    /// <param name="isMouseMove">true: 마우스 커서 이동 유지, false: 클릭 후 원래 위치로 복원</param>
    public void RightClickAtPosition(int x, int y, bool isMouseMove = true)
    {
        POINT originalPos = default;
        if (!isMouseMove)
        {
            GetCursorPos(out originalPos);
        }

        Debug.Log($"[ExecutorMouseAction] Moving cursor to ({x}, {y}) for right-click");
        
        bool moveSuccess = SetCursorPos(x, y);
        if (!moveSuccess)
        {
            Debug.LogError($"[ExecutorMouseAction] Failed to move cursor to ({x}, {y})");
            return;
        }

        System.Threading.Thread.Sleep(50);

        // 오른쪽 클릭 시뮬레이션
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);

        Debug.Log($"[ExecutorMouseAction] Right-clicked at ({x}, {y})");

        // 원래 위치로 복원
        if (!isMouseMove)
        {
            System.Threading.Thread.Sleep(50);
            SetCursorPos(originalPos.X, originalPos.Y);
            Debug.Log($"[ExecutorMouseAction] Restored cursor to ({originalPos.X}, {originalPos.Y})");
        }
    }

#else
    // Windows가 아닌 플랫폼에서는 경고만 출력
    public void ClickAtPosition(int x, int y, bool isMouseMove = true)
    {
        Debug.LogWarning($"[ExecutorMouseAction] Mouse click simulation is only supported on Windows. Requested position: ({x}, {y})");
    }

    public void RightClickAtPosition(int x, int y, bool isMouseMove = true)
    {
        Debug.LogWarning($"[ExecutorMouseAction] Mouse click simulation is only supported on Windows. Requested position: ({x}, {y})");
    }
#endif
}
