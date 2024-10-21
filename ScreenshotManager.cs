using System;
using System.Drawing;
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

    public void GetBlueFullScreen()
    {
        // Load the sprite from the assets folder (make sure Square.png is imported in Unity)
        Sprite squareSprite = Resources.Load<Sprite>("Sprites/Square");

        if (squareSprite == null)
        {
            Debug.LogError("Square sprite not found. Make sure it's located at Assets/Sprites/Square.png.");
            return;
        }

        // Access the Image component of the backgroundOverlayPanel
        UnityEngine.UI.Image bgImage = backgroundOverlayPanel.GetComponent<UnityEngine.UI.Image>();

        if (bgImage != null)
        {
            // Apply the Square sprite
            bgImage.sprite = squareSprite;

            // Set the color to fully opaque blue
            bgImage.color = new UnityEngine.Color(0, 0, 1, 1);  // RGB for blue and Alpha for full opacity

            // Ensure that the panel is active and visible
            backgroundOverlayPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No Image component found on backgroundOverlayPanel.");
        }
    }

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
            Vector2 panelSize = panelRectTransform.sizeDelta;
            Vector3 screenPos = panelRectTransform.anchoredPosition;
            float x = screenPos.x - panelRectTransform.sizeDelta.x / 2;
            float y = -screenPos.y - panelRectTransform.sizeDelta.y / 2;

            string filePath = "./Screenshots/panel_capture.png";
            CaptureDesktopArea((int)x, (int)y, (int)panelSize.x, (int)panelSize.y, filePath);  // x, y 기점으로 width, height 만큼 screenshot

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
        using (Bitmap bmp = Bitmap.FromHbitmap(bitmap))
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
}
