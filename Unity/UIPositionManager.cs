using UnityEngine;

public class UIPositionManager : MonoBehaviour
{
    private static UIPositionManager instance;

    public static UIPositionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIPositionManager>();
            }
            return instance;
        }
    }

    private Canvas canvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            canvas = FindObjectOfType<Canvas>();
            canvasRect = canvas.GetComponent<RectTransform>();
        }
    }

    // 캔버스 중앙 위치 반환
    public Vector3 GetCanvasPositionCenter()
    {
        return canvas.transform.TransformPoint(Vector3.zero);
    }

    // 캔버스 왼쪽 중앙 위치 (X - Width/2)
    public Vector3 GetCanvasPositionLeft()
    {
        return canvas.transform.TransformPoint(new Vector3(-canvasRect.rect.width / 2 + 200f, 0f, 0f));
    }

    // 캔버스 오른쪽 중앙 위치 (X + Width/2)
    public Vector3 GetCanvasPositionRight()
    {
        return canvas.transform.TransformPoint(new Vector3(canvasRect.rect.width / 2 - 200f, 0f, 0f));
    }

    // 캔버스 상단 중앙 위치
    public Vector3 GetCanvasPositionTop()
    {
        return canvas.transform.TransformPoint(new Vector3(0f, canvasRect.rect.height / 2 - 100f, 0f));
    }

    // 캔버스 하단 중앙 위치
    public Vector3 GetCanvasPositionBottom()
    {
        return canvas.transform.TransformPoint(new Vector3(0f, -canvasRect.rect.height / 2 + 100f, 0f));
    }

    // 캔버스 좌상단
    public Vector3 GetCanvasPositionTopLeft()
    {
        return canvas.transform.TransformPoint(new Vector3(-canvasRect.rect.width / 2 + 100f, canvasRect.rect.height / 2 - 100f, 0f));
    }

    // 캔버스 우상단
    public Vector3 GetCanvasPositionTopRight()
    {
        return canvas.transform.TransformPoint(new Vector3(canvasRect.rect.width / 2 - 100f, canvasRect.rect.height / 2 - 100f, 0f));
    }

    // 캔버스 좌하단
    public Vector3 GetCanvasPositionBottomLeft()
    {
        return canvas.transform.TransformPoint(new Vector3(-canvasRect.rect.width / 2 + 100f, -canvasRect.rect.height / 2 + 100f, 0f));
    }

    // 캔버스 우하단
    public Vector3 GetCanvasPositionBottomRight()
    {
        return canvas.transform.TransformPoint(new Vector3(canvasRect.rect.width / 2 - 100f, -canvasRect.rect.height / 2 + 100f, 0f));
    }

    // 특정 메뉴 이름에 따라 하드코딩된 위치 반환 (예시만)
    public Vector3 GetMenuPosition(string menuName)  
    {
        switch (menuName)
        {
            case "guideline":
                return canvas.transform.TransformPoint(new Vector3(0f, 100f, 0f)); // 중앙보다 위
            case "charChange":
                return canvas.transform.TransformPoint(new Vector3(700f, 150f, 0f));
            case "charSummon":
                return canvas.transform.TransformPoint(new Vector3(1000f, 50f, 0f));
            case "settings":
                return canvas.transform.TransformPoint(new Vector3(250f, -50f, 0f));
            case "version":
                return canvas.transform.TransformPoint(new Vector3(0f, -200f, 0f));
            default:
                return GetCanvasPositionCenter(); // 기본값은 중앙
        }
    }
}
