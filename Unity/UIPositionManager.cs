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
            canvas = CanvasManager.Instance.canvasUI;
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

    // 캐릭터의 Transform을 기반으로 말풍선의 AnchoredPosition 계산
    public Vector2 GetBalloonAnchoredPosition(RectTransform charTransform)
    {
        Vector2 charPosition = charTransform.anchoredPosition;

        // 캔버스 범위를 벗어나지 않도록 X 좌표 제한
        float leftBound = -canvasRect.rect.width / 2;
        float rightBound = canvasRect.rect.width / 2;
        float charPositionX = Mathf.Clamp(charPosition.x, leftBound + 250, rightBound - 250);

        // Y 좌표는 캐릭터 위치에 비례하여 위로 띄움
        float charSizeScale = SettingManager.Instance.settings.char_size / 100f;
        return new Vector2(charPositionX, charPosition.y + 200 * charSizeScale + 100);
    }

    // 특정 좌표를 기반으로 말풍선의 AnchoredPosition 계산
    public Vector2 GetBalloonAnchoredPositionByPosition(Vector2 targetPosition)
    {
        // 캔버스 범위를 벗어나지 않도록 X 좌표 제한
        float leftBound = -canvasRect.rect.width / 2;
        float rightBound = canvasRect.rect.width / 2;
        float positionX = Mathf.Clamp(targetPosition.x, leftBound + 250, rightBound - 250);

        // Y 좌표는 캐릭터 위치에 비례하여 위로 띄움
        float charSizeScale = SettingManager.Instance.settings.char_size / 100f;
        return new Vector2(positionX, targetPosition.y + 200 * charSizeScale + 100);
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
                return canvas.transform.TransformPoint(new Vector3(400f, -100f, 0f));
            case "chatHistory":
                return canvas.transform.TransformPoint(new Vector3(300f, -100f, 0f));
            case "settings":
                return canvas.transform.TransformPoint(new Vector3(250f, -50f, 0f));
            case "version":
                return canvas.transform.TransformPoint(new Vector3(0f, -200f, 0f));
            case "chatBalloonBottom":
                return canvas.transform.TransformPoint(new Vector3(0f, -canvasRect.rect.height / 2 + 150f, 0f));
            case "debugBalloon2":
                return canvas.transform.TransformPoint(new Vector3(canvasRect.rect.width / 2 - 250f, canvasRect.rect.height / 2 - 200f, 0f));
            case "ocrAutoMapper":
                return canvas.transform.TransformPoint(new Vector3(-300f, 0f, 0f));
            case "choiceInput":
                return GetCanvasPositionCenter(); // 중앙 배치
            default:
                return GetCanvasPositionCenter(); // 기본값은 중앙
        }
    }
}
