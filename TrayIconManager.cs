using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO; 
using UnityEngine;
using System.Runtime.InteropServices;

public class TrayIconManager : MonoBehaviour
{

    // 싱글톤 인스턴스
    private static TrayIconManager instance;
    public static TrayIconManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TrayIconManager>();
            }
            return instance;
        }
    }

    private NotifyIcon trayIcon; // Windows Forms NotifyIcon
    private bool isMinimized = false; // 창이 최소화 상태인지 여부
    public UnityEngine.UI.Image iconImage;

    // 외부 DLL(user32.dll)에서 GetActiveWindow 함수를 가져옴. 이 함수는 현재 활성화된 창의 핸들을 반환합니다.
    [DllImport("user32.dll")]
    public static extern IntPtr GetActiveWindow();

    // 외부 DLL(user32.dll)에서 SetWindowLong 함수를 가져옴. 이 함수는 윈도우의 속성 값을 설정합니다.
    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // 외부 DLL(user32.dll)에서 ShowWindow 함수를 가져옴. 이 함수는 창의 표시 상태를 변경합니다.
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // 외부 DLL(user32.dll)에서 GetWindowLong 함수를 가져옴. 이 함수는 윈도우의 스타일 값을 가져옵니다.
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    // 윈도우 스타일 속성 상수
    const int GWL_EXSTYLE = -20;
    const uint WS_EX_APPWINDOW = 0x00040000;
    const uint WS_EX_TOOLWINDOW = 0x00000080;

    private IntPtr _hwnd;

    // 창을 숨기고 작업 표시줄에서 아이콘을 제거하는 함수
    public void HideWindow()
    {
        IntPtr extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        SetWindowLong(_hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt32() | (int)WS_EX_TOOLWINDOW)); // 툴 윈도우로 설정하여 작업 표시줄에서 숨깁니다.
        ShowWindow(_hwnd, SW_HIDE); // 창을 숨깁니다.
    }

    // 창을 다시 보이게 하고 작업 표시줄에 아이콘을 표시하는 함수
    public void ShowWindowManually()
    {
        IntPtr extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        SetWindowLong(_hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt32() & ~(int)WS_EX_TOOLWINDOW)); // 툴 윈도우 스타일 제거
        ShowWindow(_hwnd, SW_SHOW); // 창을 보이게 합니다.
    }

    private void CurrentCharToMiddle(object sender, EventArgs e)
    {
        GameObject currentCharacter = CharManager.Instance.GetCurrentCharacter();
        if (currentCharacter != null)
        {
            RectTransform newRectTransform = currentCharacter.GetComponent<RectTransform>();
            if (newRectTransform != null)
            {
                newRectTransform.anchoredPosition3D = new Vector3(0, 0, -70); // 기본 위치로 이동 (0, 0, -70)
            }
        }
    }

    private void ClearAllSummmonChar(object sender, EventArgs e)
    {
        SubCharManager.Instance.ClearAllSummonChar();
    }

    private void Start()
    {
        _hwnd = GetActiveWindow(); // 현재 활성화된 창의 핸들을 가져옵니다.
        ShowWindowManually();

        // 트레이 아이콘 초기화
        trayIcon = new NotifyIcon
        {
            Visible = true,
            Icon = LoadIcon("icon_arona"), // 기본 아이콘 로드
            Text = "MY-Little-JARVIS" // 기본 툴팁 텍스트
        };

        // ContextMenu (Windows Forms) 생성 및 메뉴 항목 추가
        var contextMenu = new System.Windows.Forms.ContextMenu();
        contextMenu.MenuItems.Add("Show", RestoreWindow); // 열기 버튼
        contextMenu.MenuItems.Add("Hide", MinimizeWindow); // 열기 버튼
        contextMenu.MenuItems.Add("To Middle", CurrentCharToMiddle); // 메인캐릭터 중앙으로 이동
        contextMenu.MenuItems.Add("Clear All Summon Char", ClearAllSummmonChar); // 메인캐릭터 중앙으로 이동
        contextMenu.MenuItems.Add("Exit", ExitApp); // 종료 버튼
        trayIcon.ContextMenu = contextMenu;

        // 트레이 아이콘 더블 클릭 이벤트 연결
        trayIcon.DoubleClick += (sender, args) => ToggleWindowState();

        // 예: 3초 후 툴팁 텍스트 변경
        // Invoke(nameof(ChangeTooltipText), 3f);
    }

    // 아이콘 클릭 시 창 상태 토글 (최소화/복원)
    private void ToggleWindowState()
    {
        if (isMinimized)
        {
            RestoreWindow(null, null);
            trayIcon.Icon = LoadIcon("icon_arona");
        }
        else
        {
            MinimizeWindow(null, null);
            trayIcon.Icon = LoadIcon("icon_arona");
        }
    }

    // 창 최소화
    public void MinimizeWindow(object sender, EventArgs e)
    {
        isMinimized = true;
        trayIcon.Visible = true;
        HideWindow();
        UnityEngine.Debug.Log("창 최소화됨");
        
        
    }

    // 창 복원
    private void RestoreWindow(object sender, EventArgs e)
    {
        isMinimized = false;
        trayIcon.Visible = true;
        ShowWindowManually();
        UnityEngine.Debug.Log("창 복구");
    }

    // 트레이 아이콘의 툴팁 텍스트 변경
    private void ChangeTooltipText()
    {
        UpdateTrayText("새로운 상태: 대기 중...");
    }

    // 툴팁 텍스트 업데이트
    private void UpdateTrayText(string newText)
    {
        try
        {
            trayIcon.Text = newText.Length > 63 ? newText.Substring(0, 63) : newText; // 최대 63자로 제한
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"툴팁 텍스트 변경 실패: {e.Message}");
        }
    }

    // 트레이 아이콘 아이콘 로드
    private Icon LoadIcon(string iconName)
    {
        try
        {
            string iconPath = $"{iconName}.ico";
            string fullPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, iconPath); // 새로운 경로 설정
            Debug.Log("icon setted : " + fullPath);
            return new Icon(fullPath);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"아이콘 '{iconName}.ico' 로드 실패. 기본 아이콘 사용. 오류: {e.Message}");
            return SystemIcons.Application; // 기본 아이콘 설정
        }
    }

    // private Icon LoadIconfromImage()
    // {
    //     Bitmap bt = new Bitmap(iconImage);
    //     Bitmap fitSizeBt = new Bitmap(bt, 16, 16);
    //     return Icon.FromHandle(fitSizeBt.GetHicon());
    // }

    private void ExitApp(object sender, EventArgs e)
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
        UnityEngine.Application.Quit(); // Unity 앱 종료
    }

    private void OnApplicationQuit()
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
    }
}
