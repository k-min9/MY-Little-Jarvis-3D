using System;
using System.Runtime.InteropServices;
using UnityEngine;

// 비침습 프록시 마우스 제어(커서 이동 X) 클래스
public class ApiAgentFunctionProxyMouseAction : MonoBehaviour
{
    private static ApiAgentFunctionProxyMouseAction instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionProxyMouseAction Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<ApiAgentFunctionProxyMouseAction>();
            }
            return instance;
        }
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_MOUSEWHEEL = 0x020A;
    private const int MK_LBUTTON = 0x0001;

    // 좌표를 기반으로 동적 윈도우 핸들 가져오기
    private IntPtr GetTargetWindow(int x, int y, out POINT clientPoint)
    {
        POINT screenPoint = new POINT { X = x, Y = y };
        IntPtr hWnd = WindowFromPoint(screenPoint);
        
        clientPoint = screenPoint;
        
        if (hWnd != IntPtr.Zero)
        {
            // 클라이언트 좌표로 변환
            ScreenToClient(hWnd, ref clientPoint);
        }
        
        return hWnd;
    }

    // LParam 생성을 위한 유틸리티
    private IntPtr MakeLParam(int x, int y)
    {
        return (IntPtr)((y << 16) | (x & 0xFFFF));
    }

    // 비침습 프록시 클릭
    public bool ProxyClick(int winX, int winY)
    {
        POINT clientPt;
        IntPtr hWnd = GetTargetWindow(winX, winY, out clientPt);
        
        if (hWnd == IntPtr.Zero)
        {
            Debug.LogWarning($"[ApiAgentFunctionProxyMouseAction] 타겟 윈도우를 찾을 수 없음: ({winX}, {winY})");
            return false;
        }

        IntPtr lParam = MakeLParam(clientPt.X, clientPt.Y);
        
        // 백그라운드 클릭 메시지 전송
        PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParam);
        PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
        
        Debug.Log($"[ApiAgentFunctionProxyMouseAction] 프록시 클릭 성공: ({winX}, {winY}) -> 윈도우 핸들: {hWnd}");
        return true;
    }

    // 비침습 프록시 드래그
    public bool ProxyDrag(int startX, int startY, int endX, int endY)
    {
        POINT clientStartPt, clientEndPt;
        IntPtr hWndStart = GetTargetWindow(startX, startY, out clientStartPt);
        IntPtr hWndEnd = GetTargetWindow(endX, endY, out clientEndPt);
        
        // 시작과 끝이 같은 윈도우여야 안전하게 메시지 전송 가능
        if (hWndStart == IntPtr.Zero || hWndStart != hWndEnd)
        {
            Debug.LogWarning("[ApiAgentFunctionProxyMouseAction] 드래그 타겟 윈도우가 일치하지 않거나 찾을 수 없음");
            return false;
        }

        IntPtr lParamStart = MakeLParam(clientStartPt.X, clientStartPt.Y);
        IntPtr lParamEnd = MakeLParam(clientEndPt.X, clientEndPt.Y);
        
        // 드래그 메시지 전송
        PostMessage(hWndStart, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, lParamStart);
        PostMessage(hWndStart, WM_MOUSEMOVE, (IntPtr)MK_LBUTTON, lParamEnd);
        PostMessage(hWndStart, WM_LBUTTONUP, IntPtr.Zero, lParamEnd);
        
        Debug.Log($"[ApiAgentFunctionProxyMouseAction] 프록시 드래그 성공: ({startX}, {startY}) -> ({endX}, {endY})");
        return true;
    }

    // 비침습 프록시 스크롤
    public bool ProxyScroll(int winX, int winY, int scrollAmount)
    {
        POINT clientPt;
        IntPtr hWnd = GetTargetWindow(winX, winY, out clientPt);
        
        if (hWnd == IntPtr.Zero)
        {
            Debug.LogWarning($"[ApiAgentFunctionProxyMouseAction] 타겟 윈도우를 찾을 수 없음: ({winX}, {winY})");
            return false;
        }

        // 스크롤 데이터 조합
        IntPtr wParam = (IntPtr)((scrollAmount << 16) | 0);
        IntPtr lParam = MakeLParam(clientPt.X, clientPt.Y);
        
        // 스크롤 메시지 전송
        PostMessage(hWnd, WM_MOUSEWHEEL, wParam, lParam);
        
        Debug.Log($"[ApiAgentFunctionProxyMouseAction] 프록시 스크롤 성공: ({winX}, {winY}), amount: {scrollAmount}");
        return true;
    }
#else
    public bool ProxyClick(int winX, int winY)
    {
        Debug.LogWarning("[ApiAgentFunctionProxyMouseAction] Windows 환경에서만 지원됩니다.");
        return false;
    }
    public bool ProxyDrag(int startX, int startY, int endX, int endY)
    {
        Debug.LogWarning("[ApiAgentFunctionProxyMouseAction] Windows 환경에서만 지원됩니다.");
        return false;
    }
    public bool ProxyScroll(int winX, int winY, int scrollAmount)
    {
        Debug.LogWarning("[ApiAgentFunctionProxyMouseAction] Windows 환경에서만 지원됩니다.");
        return false;
    }
#endif
}
