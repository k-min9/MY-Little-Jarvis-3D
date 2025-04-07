///
/// Copyright (c) 2019 wakagomo
///
/// 이 소스 코드는 MIT 라이선스로 배포됩니다.
/// http://opensource.org/licenses/mit-license.php
///

using UnityEngine;

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 창을 투명하게 만드는 클래스.
/// </summary>
public class TransparentWindow : MonoBehaviour
{
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN

    #region WINDOWS API
    /// <summary>
    /// GetThemeMargins 함수에서 반환하는 구조체로, 시각적 스타일이 적용된 창의 여백을 정의합니다.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/uxtheme/ns-uxtheme-_margins
    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    /// <summary>
    /// 현재 실행 중인 스레드의 메시지 큐에 연결된 활성 창의 핸들을 가져옵니다.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getactivewindow
    [DllImport("User32.dll")]
    private static extern IntPtr GetActiveWindow();
    /// <summary>
    /// 특정 창의 속성을 변경합니다. 이 함수는 지정된 오프셋의 32비트(long) 값을 설정할 수도 있습니다.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowlonga
    [DllImport("User32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    /// <summary>
    /// 자식 창, 팝업 창 또는 최상위 창의 크기, 위치 및 Z 순서를 변경합니다.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowpos
    [DllImport("User32.dll")]
    private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    /// <summary>
    /// 창 프레임을 클라이언트 영역까지 확장합니다.
    /// </summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/dwmapi/nf-dwmapi-dwmextendframeintoclientarea
    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    #endregion

    /// <summary>
    /// 클릭을 통과하도록 설정할지 여부.
    /// </summary>
    private bool isClickThrough = false;

    /// <summary>
    /// 마우스 포인터가 불투명한 픽셀 위에 있는지 여부.
    /// </summary>
    private bool isOnOpaquePixel = true;

    /// <summary>
    /// 알파 값의 임계값.
    /// </summary>
    private float opaqueThreshold = 0.1f;

    /// <summary>
    /// 현재 카메라 인스턴스.
    /// </summary>
    private Camera currentCamera;

    /// <summary>
    /// 1x1 크기의 텍스처.
    /// </summary>
    private Texture2D colorPickerTexture = null;

    /// <summary>
    /// 창 핸들.
    /// </summary>
    private IntPtr windowHandle;

    private void Awake()
    {
        windowHandle = GetActiveWindow();

        { // 창 스타일 설정
            const int GWL_STYLE = -16;
            const uint WS_POPUP = 0x80000000;

            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP);
        }

        { // 확장된 창 스타일 설정
            SetClickThrough(true);
        }

        { // 창 위치 설정
            IntPtr HWND_TOPMOST = new IntPtr(-1);
            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOACTIVE = 0x0010;
            const uint SWP_SHOWWINDOW = 0x0040;

            SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVE | SWP_SHOWWINDOW);
        }

        { // 클라이언트 영역까지 창 프레임 확장
            MARGINS margins = new MARGINS()
            {
                cxLeftWidth = -1
            };

            DwmExtendFrameIntoClientArea(windowHandle, ref margins);
        }
    }

    /// <summary>
    /// 창을 클릭할 수 있도록 하거나 클릭을 무시하도록 설정합니다.
    /// </summary>
    /// <param name="through"></param>
    private void SetClickThrough(bool through)
    {
        const int GWL_EXSTYLE = -20;
        const uint WS_EX_LAYERD = 0x080000;
        const uint WS_EX_TRANSPARENT = 0x00000020;
        const uint WS_EX_LEFT = 0x00000000;

        if (through)
        {
            SetWindowLong(windowHandle, GWL_EXSTYLE, WS_EX_LAYERD | WS_EX_TRANSPARENT);
        }
        else
        {
            SetWindowLong(windowHandle, GWL_EXSTYLE, WS_EX_LEFT);
        }
    }

    void Start()
    {
        if (!currentCamera)
        {
            // 카메라가 지정되지 않았다면 메인 카메라를 찾음
            currentCamera = Camera.main;

            // 메인 카메라가 없으면 씬에서 카메라를 검색
            if (!currentCamera)
            {
                currentCamera = FindObjectOfType<Camera>();
            }
        }

        // 마우스 하단의 색상을 추출하는 텍스처 생성
        colorPickerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

        // 마우스 커서 아래의 색상을 검사하는 코루틴 시작
        StartCoroutine(PickColorCoroutine());
    }

    void Update()
    {
        // 클릭 가능 여부를 업데이트
        UpdateClickThrough();
    }

    /// <summary>
    /// 픽셀 색상을 기반으로 클릭 가능 상태를 변경합니다.
    /// </summary>
    void UpdateClickThrough()
    {
        if (isClickThrough)
        {
            // 현재가 조작 투명 상태이고, 불투명 픽셀 위에 마우스가 오면 조작 투명 상태를 취소한다.
            if (isOnOpaquePixel)
            {
                SetClickThrough(false);
                isClickThrough = false;
            }
        }
        else
        {
            //현재가 조작 대기 상태이고, 투명 픽셀 위에 마우스가 오면 조작 투명 상태로 전환한다.
            if (!isOnOpaquePixel)
            {
                SetClickThrough(true);
                isClickThrough = true;
            }
        }
    }

    /// <summary>
    /// WaitForEndOfFrame()을 이용한 코루틴으로, 프레임이 끝난 후 화면을 감시합니다.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PickColorCoroutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForEndOfFrame();
            ObservePixelUnderCursor(currentCamera);
        }
        yield return null;
    }

    /// <summary>
    /// 마우스 커서 아래의 픽셀이 투명한지 확인합니다.
    /// </summary>
    /// <param name="cam"></param>
    void ObservePixelUnderCursor(Camera cam)
    {
        // 카메라 불명시 아무작업도 하지 않음
        if (!cam) return;

        Vector2 mousePos = Input.mousePosition;
        Rect camRect = cam.pixelRect;

        // 마우스가 그리기 범위 내에 있으면 확인
        if (camRect.Contains(mousePos))
        {
            try
            {
                // 마우스 하단의 픽셀을 ReadPixel로 가져옴
                // 참고 : http://tsubakit1.hateblo.jp/entry/20131203/1386000440
                colorPickerTexture.ReadPixels(new Rect(mousePos, Vector2.one), 0, 0);
                Color color = colorPickerTexture.GetPixel(0, 0);

                // 알파 값이 임계값 이상이면 불투명 처리
                isOnOpaquePixel = (color.a >= opaqueThreshold);
            }
            catch (System.Exception ex)
            {
                // 가끔 범위 밖이 되어버림
                Debug.LogError(ex.Message);
                isOnOpaquePixel = false;
            }
        }
        else
        {
            isOnOpaquePixel = false;
        }
    }
#endif // !UNITY_EDITOR && UNITY_STANDALONE_WIN


    // 기존 코드
    private int _displayIndex;

    // 외부 DLL(user32.dll)에서 MessageBox 함수를 가져옴. 이 함수는 메시지 박스를 표시합니다.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    // 메시지 박스를 표시하는 함수
    public void ShowMessageBox(string message)
    {
        var box = MessageBox(IntPtr.Zero, message, "알림", 0); // 메시지 박스를 띄우고 반환된 값을 로그에 출력
        Debug.Log(box);
    }

    // 창을 지정한 모니터로 이동시키는 함수
    public void MoveToMonitor(int monitorIndex)
    {
        // Display 정보를 담을 리스트 초기화
        List<DisplayInfo> displayInfos = new();
        Screen.GetDisplayLayout(displayInfos); // 현재 시스템의 모니터 레이아웃 정보를 가져옵니다.
        if(displayInfos.Count <= monitorIndex) // 지정한 인덱스가 모니터 수를 넘으면
        {
            monitorIndex = 0; // 기본 모니터(0번)로 설정
        }
        _displayIndex = monitorIndex; // 현재 인덱스를 업데이트
        DisplayInfo displayInfo = displayInfos[monitorIndex]; // 지정한 인덱스의 모니터 정보를 가져옴
        Screen.MoveMainWindowTo(displayInfo, Vector2Int.zero); // 메인 윈도우를 해당 모니터의 (0,0) 위치로 이동
    }

    // 다음 모니터로 창을 이동시키는 함수
    public void NextMonitor()
    {
        MoveToMonitor(_displayIndex + 1); // 현재 인덱스의 다음 모니터로 이동
    }

    // 응용 프로그램을 종료하는 함수
    public void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Unity 에디터에서 실행 중일 경우, 플레이 상태를 중지
        #else
        Application.Quit(); // 빌드된 애플리케이션일 경우, 애플리케이션 종료
        #endif
    }


}
