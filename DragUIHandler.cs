using UnityEngine;
using UnityEngine.EventSystems;
using UnityWeld.Binding;

[Binding] 
public class DragUIHandler : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private Canvas _canvas;

    private Vector2 startMousePos;
    private Vector3 startUIPos; // Vector3로 변경

    void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransform rectTransform = transform.parent.GetComponent<RectTransform>();

        // UI 최초 위치 (로컬 포지션 사용)
        startUIPos = rectTransform.localPosition;

        // 마우스 위치 초기화
        var mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            mousePos,
            _canvas.worldCamera,
            out startMousePos
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 현재 마우스 위치 계산
        var mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            mousePos,
            _canvas.worldCamera,
            out Vector2 pos
        );

        // 이동 오프셋 계산 및 적용
        Vector3 offset = pos - startMousePos;
        transform.parent.localPosition = startUIPos + offset; // 로컬 포지션 기준 적용
    }
}
