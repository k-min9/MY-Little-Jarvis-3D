using UnityEngine;
using UnityEngine.EventSystems;

public class PomodoroUIHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform dragTarget;  // Panel to move
    [SerializeField] private GameObject fullPanel;  // Full mode panel
    [SerializeField] private GameObject compactPanel;  // Compact mode panel
    [SerializeField] private float clickSeconds = 0.8f;  // Max press time for click
    [SerializeField] private float clickMoveLimit = 30f;  // Max movement for click

    private bool pointerDown = false;  // Current press state
    private bool movedTooFarForClick = false;  // Movement exceeded click range
    private float pointerDownTime = 0f;  // Press start time
    private Vector2 startScreenPosition;  // Screen position at press start
    private Vector2 startPointerPosition;  // Pointer position at drag start
    private Vector3 startTargetPosition;  // Target position at drag start

    // Store the initial pointer position.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (dragTarget == null || !TryGetLocalPointerPosition(eventData, out startPointerPosition))
        {
            pointerDown = false;
            return;
        }

        pointerDown = true;
        movedTooFarForClick = false;
        pointerDownTime = Time.unscaledTime;
        startScreenPosition = eventData.position;
        startTargetPosition = dragTarget.localPosition;
    }

    // Move the panel immediately while pressed.
    public void OnDrag(PointerEventData eventData)
    {
        if (!pointerDown)
        {
            return;
        }

        if (dragTarget == null || !TryGetLocalPointerPosition(eventData, out Vector2 currentPointerPosition))
        {
            return;
        }

        UpdateClickMovement(eventData.position);

        Vector3 offset = currentPointerPosition - startPointerPosition;
        dragTarget.localPosition = startTargetPosition + offset;
    }

    // Minimize when released before the click time limit.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pointerDown)
        {
            return;
        }

        UpdateClickMovement(eventData.position);

        bool isShortClick = !movedTooFarForClick && Time.unscaledTime - pointerDownTime < clickSeconds;
        pointerDown = false;
        movedTooFarForClick = false;

        if (isShortClick)
        {
            SetCompactMode(true);
        }
    }

    // Track whether movement has exceeded the click range.
    private void UpdateClickMovement(Vector2 currentScreenPosition)
    {
        if (Vector2.Distance(currentScreenPosition, startScreenPosition) >= clickMoveLimit)
        {
            movedTooFarForClick = true;
        }
    }

    // Toggle full and compact panels.
    private void SetCompactMode(bool compact)
    {
        if (compact)
        {
            SyncPanelPosition(fullPanel, compactPanel);
        }
        else
        {
            SyncPanelPosition(compactPanel, fullPanel);
        }

        if (fullPanel != null)
        {
            fullPanel.SetActive(!compact);
        }

        if (compactPanel != null)
        {
            compactPanel.SetActive(compact);
        }
    }

    // Copy the current panel center to the next panel.
    private void SyncPanelPosition(GameObject sourcePanel, GameObject targetPanel)
    {
        if (sourcePanel == null || targetPanel == null || !sourcePanel.activeSelf)
        {
            return;
        }

        RectTransform sourceTransform = sourcePanel.transform as RectTransform;
        RectTransform targetTransform = targetPanel.transform as RectTransform;
        if (sourceTransform == null || targetTransform == null)
        {
            return;
        }

        targetTransform.anchoredPosition = sourceTransform.anchoredPosition;
    }

    // Convert pointer position into the drag target parent space.
    private bool TryGetLocalPointerPosition(PointerEventData eventData, out Vector2 position)
    {
        position = Vector2.zero;
        if (dragTarget == null || dragTarget.parent == null)
        {
            return false;
        }

        RectTransform parentTransform = dragTarget.parent as RectTransform;
        if (parentTransform == null)
        {
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentTransform,
            eventData.position,
            eventData.pressEventCamera,
            out position
        );
    }
}
