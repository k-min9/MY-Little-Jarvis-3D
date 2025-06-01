using System;
using System.IO;
// using System.Drawing;  // bitmap
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenshotManager : MonoBehaviour
{
    public GameObject screenshotArea; // Panel acting as screenshot area (with borders)
    public GameObject backgroundOverlayPanel; // Panel for background overlay

    private Canvas _canvas;
    private bool isSelectingArea = false;
    private Vector3 startMousePosition;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
                                      IntPtr hdcSrc, int xSrc, int ySrc, int Rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    private const int SRCCOPY = 0x00CC0020;

    void Start()
    {
        backgroundOverlayPanel.SetActive(false); // Initially, background is disabled
        screenshotArea.SetActive(false); // Initially, screenshot area is hidden
        _canvas = FindObjectOfType<Canvas>();  // 최상위 Canvas
    }

    public void SetScreenshotArea()
    {
        isSelectingArea = true;

        // // Capture the entire screen using Windows API
        // Texture2D screenCapture = CaptureFullScreen();

        // // Apply the captured screenshot as the background of backgroundOverlayPanel
        // UnityEngine.UI.Image bgImage = backgroundOverlayPanel.GetComponent<UnityEngine.UI.Image>();
        // if (bgImage != null)
        // {
        //     bgImage.sprite = Sprite.Create(screenCapture, new Rect(0, 0, screenCapture.width, screenCapture.height), new Vector2(0.5f, 0.5f));
        //     bgImage.raycastTarget = false; // Ensure background doesn't block input
        // }
        backgroundOverlayPanel.SetActive(true); // Activate background

        StartCoroutine(SelectArea());
    }

    // public void GetBlueFullScreen()
    // {
    //     // Load the sprite from the assets folder (make sure Square.png is imported in Unity)
    //     Sprite squareSprite = Resources.Load<Sprite>("Sprites/Square");

    //     if (squareSprite == null)
    //     {
    //         Debug.LogError("Square sprite not found. Make sure it's located at Assets/Sprites/Square.png.");
    //         return;
    //     }

    //     // Access the Image component of the backgroundOverlayPanel
    //     UnityEngine.UI.Image bgImage = backgroundOverlayPanel.GetComponent<UnityEngine.UI.Image>();

    //     if (bgImage != null)
    //     {
    //         // Apply the Square sprite
    //         bgImage.sprite = squareSprite;

    //         // Set the color to fully opaque blue
    //         bgImage.color = new UnityEngine.Color(0, 0, 1, 1);  // RGB for blue and Alpha for full opacity

    //         // Ensure that the panel is active and visible
    //         backgroundOverlayPanel.SetActive(true);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("No Image component found on backgroundOverlayPanel.");
    //     }
    // }

    // Capture the entire screen using Windows API
    private Texture2D CaptureFullScreen()
    {
        int width = Screen.width;
        int height = Screen.height;
        
        Texture2D screenCapture = new Texture2D(width, height, TextureFormat.RGB24, false);
        
        IntPtr hdcSrc = GetDC(IntPtr.Zero);
        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
        IntPtr hOld = SelectObject(hdcDest, hBitmap);
        
        BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, 0x00CC0020); // SRCCOPY
        
        // Copy the captured bitmap to a Texture2D
        screenCapture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenCapture.Apply();
        
        // Cleanup
        SelectObject(hdcDest, hOld);
        DeleteObject(hBitmap);
        ReleaseDC(IntPtr.Zero, hdcSrc);

        return screenCapture;
    }

    private IEnumerator SelectArea()
    {
        yield return new WaitForSeconds(0.3f);  // 0.3초 대기
        while (isSelectingArea)
        {
            if (Input.GetMouseButtonDown(0)) // On left mouse click
            {
                startMousePosition = Input.mousePosition;
                startMousePosition.x = Mathf.Clamp(startMousePosition.x, 0, Screen.width);
                startMousePosition.y = Mathf.Clamp(startMousePosition.y, 0, Screen.height);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, startMousePosition, _canvas.worldCamera, out Vector2 startLocalPoint);
                screenshotArea.SetActive(true); // Show the screenshot area panel

                while (Input.GetMouseButton(0)) // While mouse is held down, update area
                {
                    DrawRect(startLocalPoint);
                    yield return null;
                }

                isSelectingArea = false;
                screenshotArea.SetActive(false);
                backgroundOverlayPanel.SetActive(false); // Deactivate background
            }
            yield return null;
        }
    }

    void DrawRect(Vector2 startLocalPoint)
    {        
        Vector3 endMousePos = Input.mousePosition;
        endMousePos.y = Mathf.Clamp(endMousePos.y, 0, Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, endMousePos, _canvas.worldCamera, out Vector2 endLocalPoint);

        // Calculate the center and size based on the local points
        Vector3 center = (startLocalPoint + endLocalPoint) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(startLocalPoint.x - endLocalPoint.x), Mathf.Abs(startLocalPoint.y - endLocalPoint.y));
        
        // Update the position and size of the screenshotArea (which is the Panel's RectTransform)
        RectTransform panelRectTransform = screenshotArea.GetComponent<RectTransform>();

        if (panelRectTransform != null)
        {
            // Set position and size
            panelRectTransform.localPosition = center;
            panelRectTransform.sizeDelta = size;  // anchors의 min과 max의 x와 y가 각각 같아야 함
        }
        else
        {
            Debug.LogWarning("screenshotArea does not have a RectTransform.");
        }
    }

    public void SaveScreenshot()
    {
        RectTransform panelRectTransform = screenshotArea.GetComponent<RectTransform>();

        if (panelRectTransform.sizeDelta.x > 0 && panelRectTransform.sizeDelta.y > 0)
        {
            // 좌상단, 우하단
            Vector2 bottomLeft = new Vector2(
                panelRectTransform.anchoredPosition.x - panelRectTransform.sizeDelta.x / 2,
                panelRectTransform.anchoredPosition.y + panelRectTransform.sizeDelta.y / 2
            );

            Vector2 topRight = new Vector2(
                panelRectTransform.anchoredPosition.x + panelRectTransform.sizeDelta.x / 2,
                panelRectTransform.anchoredPosition.y - panelRectTransform.sizeDelta.y / 2
            );

            // Local -> Windows 좌표 변환
            Vector2 start = ConvertUnityPosToWinpos(bottomLeft);
            Vector2 end = ConvertUnityPosToWinpos(topRight);

            // 최종 캡처 영역 계산
            float x = start.x;
            float y = Screen.height - start.y;  // Y축 반전 보정
            int width = (int)(end.x - start.x);
            int height = (int)(start.y - end.y);
            Debug.Log("capture:"+x+"/"+y+"/"+width+"/"+height);

            string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string filePath = Path.Combine(directory, "panel_capture.png");
            CaptureDesktopArea((int)x, (int)y, (int)width, (int)height, filePath);  // x, y(좌상단) 기점으로 우하를 width, height 만큼 screenshot

            Debug.Log($"Screenshot saved at {filePath}");
        }
        else
        {
            Debug.LogWarning("Please set the screenshot area first.");
        }
    }

    private void CaptureDesktopArea(int x, int y, int width, int height, string filePath)
    {
        // Get the desktop window and its DC
        IntPtr desktopHwnd = GetDesktopWindow();
        IntPtr desktopDC = GetWindowDC(desktopHwnd);
        IntPtr memoryDC = CreateCompatibleDC(desktopDC);

        // Create a compatible bitmap
        IntPtr bitmap = CreateCompatibleBitmap(desktopDC, width, height);
        IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

        // Copy the desktop content to the memory DC (starting from x, y with size width, height)
        BitBlt(memoryDC, 0, 0, width, height, desktopDC, x, y, SRCCOPY);

        // Create a Bitmap object from the handle
        using (System.Drawing.Bitmap bmp = System.Drawing.Bitmap.FromHbitmap(bitmap))
        {
            // Save the Bitmap as PNG
            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        // Clean up
        SelectObject(memoryDC, oldBitmap);
        DeleteObject(bitmap);
        DeleteDC(memoryDC);
        ReleaseDC(desktopHwnd, desktopDC);
    }

    // Function to set the transparency of a panel
    private void SetPanelAlpha(GameObject panel, float alpha)
    {
        CanvasRenderer canvasRenderer = panel.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            canvasRenderer.SetAlpha(alpha);
        }
        else
        {
            Debug.LogWarning("Panel is missing CanvasRenderer.");
        }
    }

    //  Unity Canvas 좌표를 Windows 좌표로
    // public Vector2 ConvertUnityPosToWinpos(Vector2 localPos)
    // {
    //     Debug.Log("ConvertUnityPosToWinpos Start ");
    //     Debug.Log(localPos);
    //     Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
    //         _canvas.worldCamera, 
    //         // _canvas.GetComponent<RectTransform>().TransformPoint(localPos)
    //         localPos
    //     );
    //     Debug.Log("ConvertUnityPosToWinpos End ");
    //     Debug.Log(screenPoint);
    //     return screenPoint;
    // }

    public Vector2 ConvertUnityPosToWinpos(Vector2 localPoint)
    {
        Debug.Log("ConvertUnityPosToWinpos Start");
        Debug.Log(localPoint);

        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

        // Local 좌표를 World 좌표로 변환
        Vector3 worldPoint = canvasRect.TransformPoint(localPoint);

        // World 좌표를 Screen 좌표로 변환
        // Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, worldPoint);
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        Debug.Log("ConvertUnityPosToWinpos End");
        Debug.Log(screenPoint);

        return screenPoint;
    }

    private GameObject screenshotUI; // 동적으로 생성할 UI 오브젝트
    private Coroutine destroyCoroutine; // 코루틴 참조 저장
    public Font customFont; // 외부에서 설정할 폰트

    public void ShowScreenshotImage()
    {
        // 기존 코루틴이 있으면 중지
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            if (screenshotUI != null)
            {
                Destroy(screenshotUI);
            }
        }

        // 저장 경로 설정
        string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
        string filePath = Path.Combine(directory, "panel_capture.png");

        // 파일 존재 여부 확인
        if (File.Exists(filePath))
        {
            // PNG 파일을 바이트 배열로 읽기
            byte[] bytes = File.ReadAllBytes(filePath);

            // 텍스처 생성
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);

            // Canvas가 없으면 생성
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 새로운 UI 오브젝트 생성
            screenshotUI = new GameObject("ScreenshotImage");
            screenshotUI.transform.SetParent(canvas.transform, false);

            // Image 컴포넌트 추가
            Image image = screenshotUI.AddComponent<Image>();

            // Sprite 생성
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            image.sprite = sprite;

            // RectTransform 설정 (이미지 크기로 설정하고 중앙에 배치)
            RectTransform rectTransform = screenshotUI.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            // 5초 후 자동 파괴
            destroyCoroutine = StartCoroutine(DestroyAfterDelay(5f));
        }
        else
        {
            Debug.LogWarning("Screenshot 파일을 찾을 수 없습니다: " + filePath);
            ShowNoImageUI();
        }
    }

    // 5초 후 UI 파괴 코루틴
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (screenshotUI != null)
        {
            Destroy(screenshotUI);
            screenshotUI = null;
        }
        destroyCoroutine = null;
    }

    // 이미지 없을 때 붉은색 투명 상자와 텍스트 표시
    private void ShowNoImageUI()
    {
        // Canvas가 없으면 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 새로운 UI 오브젝트 생성
        screenshotUI = new GameObject("NoImageUI");
        screenshotUI.transform.SetParent(canvas.transform, false);

        // Image 컴포넌트 추가 (붉은색 투명 30% 상자)
        Image image = screenshotUI.AddComponent<Image>();
        image.color = new Color(1f, 0f, 0f, 0.3f); // 붉은색, 투명도 30%

        // RectTransform 설정 (크기를 600x300, 중앙 배치)
        RectTransform rectTransform = screenshotUI.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(600, 300);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        // 텍스트 오브젝트 생성
        GameObject textObj = new GameObject("NoImageText");
        textObj.transform.SetParent(screenshotUI.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = "No Image";
        text.font = customFont != null ? customFont : Resources.GetBuiltinResource<Font>("Arial.ttf"); // 지정된 폰트가 없으면 Arial 사용
        text.fontSize = 48;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter; // 위아래 중앙 정렬

        // 텍스트 RectTransform 설정
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(600, 300); // 상자와 동일한 크기
        textRect.anchoredPosition = Vector2.zero;

        // 5초 후 자동 파괴
        destroyCoroutine = StartCoroutine(DestroyAfterDelay(5f));
    }

    // 스크립트가 파괴될 때 UI 정리
    private void OnDestroy()
    {
        if (screenshotUI != null)
        {
            Destroy(screenshotUI);
        }
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
        }
    }
}
