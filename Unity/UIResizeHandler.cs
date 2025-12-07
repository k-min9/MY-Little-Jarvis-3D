using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResizeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerMoveHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private RectTransform parentRect;
    private Canvas rootCanvas;
    
    [SerializeField] private float borderThickness = 10f;
    [SerializeField] private float minWidth = 90f;  // 60+30
    [SerializeField] private float minHeight = 150f;  // 120+30

    [Header("Resize Indicator Settings")]
    [SerializeField] private Sprite cursorHorizontal;
    [SerializeField] private Sprite cursorVertical;
    [SerializeField] private Sprite cursorDiagonalRight; // ↘↖ 방향
    [SerializeField] private Sprite cursorDiagonalLeft;  // ↗↙ 방향
    [SerializeField] private float indicatorSize = 32f;
    [SerializeField] private Vector2 indicatorOffset = new Vector2(12f, 12f);
    
    [SerializeField] private float savedWidth = 0f;
    [SerializeField] private float savedHeight = 0f;
    
    private ResizeDirection currentDirection = ResizeDirection.None;
    private ResizeDirection hoveredDirection = ResizeDirection.None;
    private RectTransform indicatorRect;
    private Image indicatorImage;
    private Vector2 initialSize;
    private Vector2 initialPosition;
    private Vector2 initialMousePos;
    private bool isChatBalloon = false;
    
    private enum ResizeDirection
    {
        None,
        Top, Bottom, Left, Right,
        TopLeft, TopRight, BottomLeft, BottomRight
    }
    
    void Awake()
    {
        borderThickness = 24f;
        indicatorSize = 32f;

        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
        }
        
        isChatBalloon = CheckIfChatBalloon();
        SetupIndicator();
        
        if (savedWidth > 0f && savedHeight > 0f)
        {
            Vector2 newSize = new Vector2(savedWidth, savedHeight);
            rectTransform.sizeDelta = newSize;
        }
    }
    
    private bool CheckIfChatBalloon()
    {
        if (gameObject.name == "Chatballoon")
        {
            return true;
        }
        
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.name == "Chatballoon")
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }
    
    public float GetWidth()
    {
        return rectTransform.sizeDelta.x;
    }
    
    public float GetHeight()
    {
        return rectTransform.sizeDelta.y;
    }
    
    public void SaveSize()
    {
        savedWidth = GetWidth();
        savedHeight = GetHeight();
    }
    
    public void LoadSize()
    {
        if (savedWidth > 0f && savedHeight > 0f)
        {
            Vector2 newSize = new Vector2(savedWidth, savedHeight);
            rectTransform.sizeDelta = newSize;
        }
    }
    
    public void SetSize(float width, float height)
    {
        savedWidth = width;
        savedHeight = height;
        Vector2 newSize = new Vector2(width, height);
        rectTransform.sizeDelta = newSize;
    }
    
    public void OnPointerMove(PointerEventData eventData)
    {
        ResizeDirection direction = currentDirection != ResizeDirection.None
            ? currentDirection
            : GetResizeDirection(eventData);
        UpdateIndicator(direction, eventData);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        currentDirection = GetResizeDirection(eventData);
        if (currentDirection != ResizeDirection.None)
        {
            UpdateIndicator(currentDirection, eventData);
            initialSize = rectTransform.sizeDelta;
            initialPosition = rectTransform.anchoredPosition;
            
            if (parentRect != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, eventData.pressEventCamera, out initialMousePos);
            }
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (currentDirection == ResizeDirection.None || parentRect == null) return;
        
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out localMousePos);
        
        Vector2 delta = localMousePos - initialMousePos;
        Vector2 newSize = initialSize;
        Vector2 newPosition = initialPosition;
        
        switch (currentDirection)
        {
            case ResizeDirection.Right:
                newSize.x = Mathf.Max(minWidth, initialSize.x + delta.x);
                newPosition.x = initialPosition.x + (newSize.x - initialSize.x) * 0.5f;
                break;
                
            case ResizeDirection.Left:
                newSize.x = Mathf.Max(minWidth, initialSize.x - delta.x);
                newPosition.x = initialPosition.x - (newSize.x - initialSize.x) * 0.5f;
                break;
                
            case ResizeDirection.Top:
                newSize.y = Mathf.Max(minHeight, initialSize.y + delta.y);
                newPosition.y = initialPosition.y + (newSize.y - initialSize.y) * 0.5f;
                break;
                
            case ResizeDirection.Bottom:
                newSize.y = Mathf.Max(minHeight, initialSize.y - delta.y);
                newPosition.y = initialPosition.y - (newSize.y - initialSize.y) * 0.5f;
                break;
                
            case ResizeDirection.TopRight:
                newSize.x = Mathf.Max(minWidth, initialSize.x + delta.x);
                newSize.y = Mathf.Max(minHeight, initialSize.y + delta.y);
                newPosition.x = initialPosition.x + (newSize.x - initialSize.x) * 0.5f;
                newPosition.y = initialPosition.y + (newSize.y - initialSize.y) * 0.5f;
                break;
                
            case ResizeDirection.TopLeft:
                newSize.x = Mathf.Max(minWidth, initialSize.x - delta.x);
                newSize.y = Mathf.Max(minHeight, initialSize.y + delta.y);
                newPosition.x = initialPosition.x - (newSize.x - initialSize.x) * 0.5f;
                newPosition.y = initialPosition.y + (newSize.y - initialSize.y) * 0.5f;
                break;
                
            case ResizeDirection.BottomRight:
                newSize.x = Mathf.Max(minWidth, initialSize.x + delta.x);
                newSize.y = Mathf.Max(minHeight, initialSize.y - delta.y);
                newPosition.x = initialPosition.x + (newSize.x - initialSize.x) * 0.5f;
                newPosition.y = initialPosition.y - (newSize.y - initialSize.y) * 0.5f;
                break;
                
            case ResizeDirection.BottomLeft:
                newSize.x = Mathf.Max(minWidth, initialSize.x - delta.x);
                newSize.y = Mathf.Max(minHeight, initialSize.y - delta.y);
                newPosition.x = initialPosition.x - (newSize.x - initialSize.x) * 0.5f;
                newPosition.y = initialPosition.y - (newSize.y - initialSize.y) * 0.5f;
                break;
        }
        
        rectTransform.sizeDelta = newSize;
        rectTransform.anchoredPosition = newPosition;
        UpdateIndicator(currentDirection, eventData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isChatBalloon && currentDirection != ResizeDirection.None)
        {
            SaveSize();
        }
        
        currentDirection = ResizeDirection.None;
        UpdateIndicator(GetResizeDirection(eventData), eventData);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        hoveredDirection = ResizeDirection.None;
        HideIndicator();
    }
    
    private ResizeDirection GetResizeDirection(PointerEventData eventData)
    {
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);
        
        Rect rect = rectTransform.rect;
        
        bool nearLeft = Mathf.Abs(localMousePos.x - rect.xMin) < borderThickness;
        bool nearRight = Mathf.Abs(localMousePos.x - rect.xMax) < borderThickness;
        bool nearTop = Mathf.Abs(localMousePos.y - rect.yMax) < borderThickness;
        bool nearBottom = Mathf.Abs(localMousePos.y - rect.yMin) < borderThickness;
        
        if (nearTop && nearLeft) return ResizeDirection.TopLeft;
        if (nearTop && nearRight) return ResizeDirection.TopRight;
        if (nearBottom && nearLeft) return ResizeDirection.BottomLeft;
        if (nearBottom && nearRight) return ResizeDirection.BottomRight;
        
        if (nearTop) return ResizeDirection.Top;
        if (nearBottom) return ResizeDirection.Bottom;
        if (nearLeft) return ResizeDirection.Left;
        if (nearRight) return ResizeDirection.Right;
        
        return ResizeDirection.None;
    }

    private void UpdateIndicator(ResizeDirection direction, PointerEventData eventData)
    {
        if (hoveredDirection == direction && indicatorRect != null && indicatorRect.gameObject.activeSelf)
        {
            UpdateIndicatorPosition(eventData);
            return;
        }

        hoveredDirection = direction;

        if (indicatorRect == null || indicatorImage == null || rootCanvas == null)
        {
            return;
        }

        if (direction == ResizeDirection.None)
        {
            HideIndicator();
            return;
        }

        Sprite targetSprite = null;
        switch (direction)
        {
            case ResizeDirection.Left:
            case ResizeDirection.Right:
                targetSprite = cursorHorizontal;
                break;
            case ResizeDirection.Top:
            case ResizeDirection.Bottom:
                targetSprite = cursorVertical;
                break;
            case ResizeDirection.TopLeft:
            case ResizeDirection.BottomRight:
                targetSprite = cursorDiagonalLeft;
                break;
            case ResizeDirection.TopRight:
            case ResizeDirection.BottomLeft:
                targetSprite = cursorDiagonalRight;
                break;
        }

        if (targetSprite == null)
        {
            HideIndicator();
            return;
        }

        indicatorImage.sprite = targetSprite;
        indicatorRect.sizeDelta = new Vector2(indicatorSize, indicatorSize);
        if (!indicatorRect.gameObject.activeSelf)
        {
            indicatorRect.gameObject.SetActive(true);
        }
        UpdateIndicatorPosition(eventData);
    }

    private void OnDisable()
    {
        hoveredDirection = ResizeDirection.None;
        HideIndicator();
    }

    private void SetupIndicator()
    {
        if (rootCanvas == null) return;

        GameObject indicatorObj = new GameObject("ResizeIndicator");
        indicatorObj.transform.SetParent(rootCanvas.transform, false);
        indicatorImage = indicatorObj.AddComponent<Image>();
        indicatorRect = indicatorObj.GetComponent<RectTransform>();
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.sizeDelta = new Vector2(indicatorSize, indicatorSize);
        indicatorObj.SetActive(false);
    }

    private void UpdateIndicatorPosition(PointerEventData eventData)
    {
        if (indicatorRect == null || rootCanvas == null) return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 screenPos = eventData.position + indicatorOffset + new Vector2(indicatorSize * 0.5f, indicatorSize * -0.8f);
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventData.pressEventCamera, out localPos);
        indicatorRect.anchoredPosition = localPos;
    }

    private void HideIndicator()
    {
        if (indicatorRect != null)
        {
            indicatorRect.gameObject.SetActive(false);
        }
    }
}