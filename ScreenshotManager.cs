using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenshotManager : MonoBehaviour
{
    public GameObject screenshotArea; // Panel acting as screenshot area (with borders)
    public GameObject backgroundOverlayPanel; // Panel for background overlay
    
    private Rect screenshotRect;
    private bool isSelectingArea = false;
    private Vector3 startMousePosition;

    void Start()
    {
        backgroundOverlayPanel.SetActive(false); // Initially, background is disabled
        screenshotArea.SetActive(false); // Initially, screenshot area is hidden
    }

    public void SetScreenshotArea()
    {
        isSelectingArea = true;
        backgroundOverlayPanel.SetActive(true); // Activate background
        SetPanelAlpha(backgroundOverlayPanel, 0.3f); // Set alpha value to 0.3
        StartCoroutine(SelectArea());
    }

    IEnumerator SelectArea()
    {
        yield return new WaitForSeconds(1f);  // 1초 대기
        while (isSelectingArea)
        {
            if (Input.GetMouseButtonDown(0)) // On left mouse click
            {
                startMousePosition = Input.mousePosition;
                screenshotArea.SetActive(true); // Show the screenshot area panel

                while (Input.GetMouseButton(0)) // While mouse is held down, update area
                {
                    Vector3 endMousePosition = Input.mousePosition;
                    DrawRect(startMousePosition, endMousePosition);
                    yield return null;
                }

                Vector3 finalMousePosition = Input.mousePosition;
                SetRect(startMousePosition, finalMousePosition);
                isSelectingArea = false;
                backgroundOverlayPanel.SetActive(false); // Deactivate background
            }
            yield return null;
        }
    }

    void DrawRect(Vector3 start, Vector3 end)
    {
        // Calculate center and size of the rectangle
        Vector3 center = (start + end) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(start.x - end.x), Mathf.Abs(start.y - end.y));
        
        // Convert screen space to world space for accurate positioning
        Vector3 worldCenter = Camera.main.ScreenToWorldPoint(center);
        worldCenter.z = 0; // Keep it in 2D space
        
        screenshotArea.transform.position = worldCenter;
        screenshotArea.GetComponent<RectTransform>().sizeDelta = size;
    }

    void SetRect(Vector3 start, Vector3 end)
    {
        // Define the rectangle in screen space based on start and end points
        float x1 = Mathf.Min(start.x, end.x);
        float y1 = Mathf.Min(start.y, end.y);
        float x2 = Mathf.Max(start.x, end.x);
        float y2 = Mathf.Max(start.y, end.y);
        screenshotRect = new Rect(x1, y1, x2 - x1, y2 - y1);
    }

    public void SaveScreenshot()
    {
        if (screenshotRect.width > 0 && screenshotRect.height > 0)
        {
            StartCoroutine(CaptureScreenshot("./Screenshots/screenshot.png"));
        }
        else
        {
            Debug.LogWarning("Please set the screenshot area first.");
        }
    }

    IEnumerator CaptureScreenshot(string filePath)
    {
        yield return new WaitForEndOfFrame();
        Texture2D screenshot = new Texture2D((int)screenshotRect.width, (int)screenshotRect.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(screenshotRect, 0, 0);
        screenshot.Apply();

        if (!System.IO.Directory.Exists("./Screenshots"))
        {
            System.IO.Directory.CreateDirectory("./Screenshots");
        }

        System.IO.File.WriteAllBytes(filePath, screenshot.EncodeToPNG());
        Destroy(screenshot);
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
