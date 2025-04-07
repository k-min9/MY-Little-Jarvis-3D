using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;  // for deepcopy of List
using UnityEngine;
using UnityEngine.UI; 

public class WindowCollisionManager : MonoBehaviour
{
    private Canvas canvas;
    private bool isWindowsRectChecking = true;


    public static WindowCollisionManager Instance { get; private set; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    private const int SW_SHOWMAXIMIZED = 3; // 최대화된 창
    private const int SW_SHOWMINIMIZED = 2; // 최소화된 창

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public List<Rect> windowRects = new List<Rect>();  // 충돌범위 상위 15%
    public List<Rect> windowAllRects = new List<Rect>();  // 충돌 범위포함 전체

    private void Awake()
    {

        // Canvas 찾기
        canvas = FindObjectOfType<Canvas>();

        // 싱글톤
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (isWindowsRectChecking)
        {
            StartCoroutine(UpdateWindowsRectsRoutine());
        }
    }

    public void SetWindowsRectChecking(bool value) {
        isWindowsRectChecking = value;
        if (isWindowsRectChecking)
        {
            StartCoroutine(UpdateWindowsRectsRoutine());
        }
    }

    public void ToggleWindowsRectChecking() {
        isWindowsRectChecking = !isWindowsRectChecking;
        if (isWindowsRectChecking)
        {
            StartCoroutine(UpdateWindowsRectsRoutine());
        }
    }

    private System.Collections.IEnumerator UpdateWindowsRectsRoutine()
    {
        while (isWindowsRectChecking)
        {
            UpdateWindowsRects();
            yield return new WaitForSeconds(0.25f); // 0.25초마다 업데이트
        }
        windowRects.Clear();
        AddTaskbarRect();  // 일단은 남겨두자.
    }

    private void UpdateWindowsRects()
    {
        windowRects = GetAllWindowsRect();
        AddTaskbarRect();
    }

    public List<Rect> GetAllWindowsRect()
    {
        List<Rect> rects = new List<Rect>();
        string rectText = "";

        windowAllRects.Clear();

        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd)) return true; // 비가시 창 무시

            // 현재 프로세스(자신) 제외
            GetWindowThreadProcessId(hWnd, out uint windowProcessId);
            if (windowProcessId == GetCurrentProcessId()) 
            {
                // 자신 제외
                return true; // 계속 진행
            }

            // 창의 스타일 확인 (WS_EX_TOOLWINDOW 창 필터링)
            const int GWL_EXSTYLE = -20; // 확장 스타일 가져오기 위한 인자
            const int WS_EX_TOOLWINDOW = 0x00000080; // 도구 창(작은 보조 창)
            // const int WS_EX_APPWINDOW = 0x00040000; // 일반 애플리케이션 창
            const int WS_EX_NOACTIVATE = 0x08000000; // 포커스를 받지 않는 창
            // const int WS_EX_TOPMOST = 0x00000008; // 항상 위에 있는 창
            // const int WS_EX_LAYERED = 0x00080000; // 반투명 창
            const int WS_EX_TRANSPARENT = 0x00000020; // 클릭 무시, 투명 처리된 창
            const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000; // UWP, WinRT 창 (무시 가능)
            int style = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((style & WS_EX_TOOLWINDOW) != 0 || // 작은 도구 창
                (style & WS_EX_NOACTIVATE) != 0 || // 포커스를 받지 않는 창
                (style & WS_EX_TRANSPARENT) != 0 || // 클릭 무시 투명 창
                (style & WS_EX_NOREDIRECTIONBITMAP) != 0) // WinRT/UWP 창
            {
                return true; // 무시할 창
            }

            // 창이 최대화된 상태인지 확인
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            {
                // if (placement.showCmd == SW_SHOWMAXIMIZED || placement.showCmd == SW_SHOWMINIMIZED)
                if (placement.showCmd != 0)
                {
                    return true; // 최대화 및 최소화된 창은 무시
                }
            }
            if (GetWindowRect(hWnd, out RECT rect))
            {
                // 윈도우 창 제목 가져오기
                StringBuilder windowTitle = new StringBuilder(256);
                GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

                // 이름 있는 창만
                if (!String.IsNullOrEmpty(windowTitle.ToString().Trim())) {
                    Rect windowRect = ConvertRECTtoRect(rect);

                    // 가로가 너무 작거나 크지 않음
                    if (windowRect.width > 50 && windowRect.width < 0.95f * Screen.width
                    && windowRect.height > 50 && windowRect.height < 0.95f * Screen.width) {
                        windowAllRects.Add(windowRect);

                        // 새로운 windowCollisionRects 추가 (상단 15% 영역만 포함, 최소크기 60)
                        float collisionHeight = windowRect.height * 0.15f;
                        collisionHeight = Math.Max(60, collisionHeight);
                        collisionHeight = Math.Min(windowRect.height, collisionHeight);
                        Rect collisionRect = new Rect(windowRect.x, windowRect.y + windowRect.height - collisionHeight, windowRect.width, collisionHeight);
                        rects.Add(collisionRect);

                        int style2 = GetWindowLong(hWnd, -20);
                        rectText = rectText + windowTitle+"("+style2+"/"+placement.rcNormalPosition.right+")" + ":" + rect.left+","+ (Screen.height - rect.bottom) +","+  windowRect.width+","+ windowRect.height+"\n";  // 확인용
                    }
                }
            }
            return true;
        }, IntPtr.Zero);

        // NoticeBalloonManager.Instance.ModifyNoticeBalloonText(rectText);
        return rects;
    }

    // 테스트용 윈도우 Rect 리스트 반환
    public List<Rect> GetTestWindowsRect()
    {
        List<Rect> testRects = new List<Rect>();

        // 정중앙 (300x200)
        testRects.Add(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 100, 300, 200));

        // 좌측 하단 (400x300)
        testRects.Add(new Rect(0, 0, 400, 300));

        // 작업 표시줄 (가로 전체, 높이 40px)
        testRects.Add(new Rect(0, 0, Screen.width, 40));

        return testRects;
    }

    private void AddTaskbarRect()
    {
        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
        if (taskbarHandle != IntPtr.Zero && GetWindowRect(taskbarHandle, out RECT taskbarRect))
        {
            Rect taskbar = ConvertRECTtoRect(taskbarRect);
            if (taskbar.width > 0 && taskbar.height > 0) {
                windowRects.Add(taskbar);
            }
        }
    }

    // window Resolution(1920*1440 etc)를 Unity Resolution(2560*1440 etc...)로 변경   
    private Rect ConvertRECTtoRect(RECT windowRECT)
    {
        float screenWidth = Screen.currentResolution.width;  // 예외발생시 Display.main.systemWidth나 win32.dll 검토
        float screenHeight = Screen.currentResolution.height;
        float scaleX = Screen.width / screenWidth;
        float scaleY = Screen.height / screenHeight;

        int right = (int) (windowRECT.right * scaleX);
        int left = (int) (windowRECT.left * scaleX);
        int top = (int) (windowRECT.top * scaleY);
        int bottom = (int) (windowRECT.bottom * scaleY);

        int width = right - left;
        int height = bottom - top;  // window : 좌상>우하 / unity : 좌하>우상  ; y축 반전 필요

        return new Rect(left, Screen.height - bottom, width, height);
    }

    // 현재충돌여부 확인
    public bool IsCollisionWithWindowsRect(Vector2 position)
    {
        foreach (var rect in windowRects)
        {
            // Windows 좌표를 Canvas 좌표로 변환 : Windows 좌표의 좌상단과 우하단을 구함
            Vector2 start = ConvertWinposToUnityPos(new Vector2(rect.x, rect.y)); // 좌상단
            Vector2 end = ConvertWinposToUnityPos(new Vector2(rect.x + rect.width, rect.y + rect.height)); // 우하단
            if (start.x <= position.x && start.y <= position.y
                && end.x >= position.x && end.y >= position.y)
            {
                return true;
            }                
        }
        return false;
    }

    // 현재 충돌한 rect의 상단
    public float GetTopOfCollisionRect(Vector2 position)
    {
        // Lock 대신에 deepcopy 채택
        List<Rect> testRects = windowRects.Select(rect => new Rect(rect)).ToList();
        foreach (var rect in testRects)
        {
            // Windows 좌표를 Canvas 좌표로 변환 : Windows 좌표의 좌상단과 우하단을 구함
            Vector2 start = ConvertWinposToUnityPos(new Vector2(rect.x, rect.y)); // 좌상단
            Vector2 end = ConvertWinposToUnityPos(new Vector2(rect.x + rect.width, rect.y + rect.height)); // 우하단
            if (start.x <= position.x && start.y <= position.y
                && end.x >= position.x && end.y >= position.y)
            { 
                // Debug.Log("posY : " + position.y);
                // Debug.Log("x : " + start.x + "/" + position.x + "/" + end.x);
                // Debug.Log("y : " + start.y + "/" + position.y + "/" + end.y);
                return end.y;
            }                
        }

        // taskbar은 무조건 체크
        IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
        if (taskbarHandle != IntPtr.Zero && GetWindowRect(taskbarHandle, out RECT taskbarRect))
        {
            Rect taskbar = ConvertRECTtoRect(taskbarRect);
            if (taskbar.width > 0 && taskbar.height > 0) {
                Vector2 start = ConvertWinposToUnityPos(new Vector2(taskbar.x, taskbar.y)); // 좌상단
                Vector2 end = ConvertWinposToUnityPos(new Vector2(taskbar.x + taskbar.width, taskbar.y + taskbar.height)); // 우하단
                if (start.x <= position.x  // && start.y <= position.y  taskbar 최소높이 체크는 당연히 하지 않는다.
                    && end.x >= position.x && end.y >= position.y)
                {
                    Debug.Log("Hit!!");
                    // Debug.Log("x : " + start.x + "/" + position.x + "/" + end.x);
                    // Debug.Log("y : " + start.y + "/" + position.y + "/" + end.y);
                    return end.y;
                }    
            }
        }
        
        // Debug.Log(testRects.Count);
        // foreach (var rect in testRects)
        // {
        //     // Windows 좌표를 Canvas 좌표로 변환 : Windows 좌표의 좌상단과 우하단을 구함
        //     Vector2 start = ConvertWinposToUnityPos(new Vector2(rect.x, rect.y)); // 좌상단
        //     Vector2 end = ConvertWinposToUnityPos(new Vector2(rect.x + rect.width, rect.y + rect.height)); // 우하단
        //     Debug.Log("x : " + start.x + "/" + position.x + "/" + end.x);
        //     Debug.Log("y : " + start.y + "/" + position.y + "/" + end.y);           
        // }
        return -99999f;
    }

    
    // 창이동 오류 Windows 좌표를 Unity Canvas 좌표로 변환 > mainCanvas 만을 참조할것!
    public Vector2 ConvertWinposToUnityPos(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), 
            screenPos, 
            canvas.worldCamera, 
            out Vector2 localPoint
        );
        return localPoint;
    }
}
