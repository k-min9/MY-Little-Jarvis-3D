using UnityEngine;

public class FallingObject : MonoBehaviour
{
    public float fallSpeed = 800f; // 떨어지는 속도
    private RectTransform rectTransform;
    private float bottomBoundary;

    void Start()
    {
        // RectTransform 컴포넌트를 가져옵니다.
        rectTransform = GetComponent<RectTransform>();

        // 캔버스 하단 경계선을 설정합니다.
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // 캔버스의 하단 경계선 (y 좌표)을 구합니다.
            bottomBoundary = -(canvas.GetComponent<RectTransform>().rect.height / 2) + rectTransform.rect.height * 1.0f;
        }
    }

    void Update()
    {
        // 오브젝트가 떨어지도록 y 위치를 조정합니다.
        Vector2 newPosition = rectTransform.anchoredPosition;
        newPosition.y -= fallSpeed * Time.deltaTime;

        // 캔버스 하단 경계선에 도달했는지 확인합니다.
        if (newPosition.y > bottomBoundary)
        {
            rectTransform.anchoredPosition = newPosition;
        }
        else
        {
            // 하단 경계선에 도달하면 위치를 정지시킵니다.
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, bottomBoundary);
        }
    }
}
