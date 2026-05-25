using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

// 물리 마우스 제어(커서 이동 O) 클래스
public class ApiAgentFunctionMouseAction : MonoBehaviour
{
    private static ApiAgentFunctionMouseAction instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionMouseAction Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<ApiAgentFunctionMouseAction>();
            }
            return instance;
        }
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const int MOUSEEVENTF_LEFTUP = 0x0004;
    private const int MOUSEEVENTF_WHEEL = 0x0800;

    // 물리 마우스 클릭
    public void PhysicalClick(int winX, int winY, bool isMouseMove = true)
    {
        POINT originalPos = default;
        
        if (!isMouseMove)
        {
            // 이동 전 위치 저장
            GetCursorPos(out originalPos);
        }

        bool moveSuccess = SetCursorPos(winX, winY);
        
        if (!moveSuccess)
        {
            Debug.LogError($"[ApiAgentFunctionMouseAction] 커서 이동 실패: ({winX}, {winY})");
            return;
        }

        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        Debug.Log($"[ApiAgentFunctionMouseAction] 물리 클릭 성공: ({winX}, {winY})");

        if (!isMouseMove)
        {
            // 원래 위치로 복원
            Thread.Sleep(50);
            SetCursorPos(originalPos.X, originalPos.Y);
        }
    }

    // 물리 마우스 드래그
    public void PhysicalDrag(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        SetCursorPos(startX, startY);
        Thread.Sleep(50);
        
        // 드래그 시작
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(50);
        
        // 목표 위치로 이동
        SetCursorPos(endX, endY);
        Thread.Sleep(durationMs);
        
        // 드래그 종료
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        Debug.Log($"[ApiAgentFunctionMouseAction] 물리 드래그 성공: ({startX}, {startY}) -> ({endX}, {endY})");
    }

    // 물리 마우스 스크롤
    public void PhysicalScroll(int winX, int winY, int scrollAmount)
    {
        SetCursorPos(winX, winY);
        Thread.Sleep(50);
        
        // 스크롤 수행 (양수: 위로, 음수: 아래로)
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, scrollAmount, 0);
        Debug.Log($"[ApiAgentFunctionMouseAction] 물리 스크롤 성공: ({winX}, {winY}), amount: {scrollAmount}");
    }
#else
    public void PhysicalClick(int winX, int winY, bool isMouseMove = true)
    {
        Debug.LogWarning("[ApiAgentFunctionMouseAction] Windows 환경에서만 지원됩니다.");
    }
    public void PhysicalDrag(int startX, int startY, int endX, int endY, int durationMs = 500)
    {
        Debug.LogWarning("[ApiAgentFunctionMouseAction] Windows 환경에서만 지원됩니다.");
    }
    public void PhysicalScroll(int winX, int winY, int scrollAmount)
    {
        Debug.LogWarning("[ApiAgentFunctionMouseAction] Windows 환경에서만 지원됩니다.");
    }
#endif
}
