using UnityEngine;
using UnityEngine.EventSystems;
using UnityWeld.Binding;

[Binding] 
public class DragUIHandler : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private Canvas _canvas;

    private Vector2 startMousePos;
    private Vector2 startUIPos;

    void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // UI 최초 위치
        RectTransform rectTransform = transform.parent.GetComponent<RectTransform>();
        startUIPos = rectTransform.anchoredPosition;

        var mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, mousePos, _canvas.worldCamera, out startMousePos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 현재 마우스 위치 계산
        var mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, mousePos, _canvas.worldCamera, out Vector2 pos);

        // offset 계산하여 이동
        // Debug.Log("Offset : " + (-startMousePos.x+pos.x) + "/" + (-startMousePos.y+pos.y));
        Vector3 newPos = new Vector3(startUIPos.x-startMousePos.x+pos.x, startUIPos.y-startMousePos.y+pos.y, 0);  // z=0
        transform.parent.position = _canvas.transform.TransformPoint(newPos);
    }
}
