using System.Collections.Generic;
using UnityEngine;

/**
작업표시줄이나 창의 정보를 반환
*/
public class BoundaryManager : MonoBehaviour
{
    public RectTransform rectTransform; // 대상 오브젝트의 RectTransform
    public Canvas canvas; // 대상 Canvas

    public List<Rect> GetCollisionBoundaries()
    {
        List<Rect> boundaries = new List<Rect>();

        if (Application.isEditor)
        {
            // 에디터 환경에서의 경계 설정
            boundaries.Add(GetEditorBoundary());
        }
        else if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            // Windows 환경에서의 경계 설정
            boundaries.Add(GetWindowsBoundary());
        }

        // 추가적인 공통 경계 또는 충돌 요소가 있다면 여기에서 추가 가능
        boundaries.AddRange(GetAdditionalBoundaries());

        return boundaries;
    }

    private Rect GetEditorBoundary()
    {
        // 에디터 환경에서 사용하는 경계 계산 로직
        float bottomBoundary = -(canvas.GetComponent<RectTransform>().rect.height / 2) + (rectTransform.rect.height / 2);
        return new Rect(0, bottomBoundary, canvas.GetComponent<RectTransform>().rect.width, rectTransform.rect.height);
    }

    private Rect GetWindowsBoundary()
    {
        // Windows 환경에서 작업 표시줄을 고려한 경계 계산 로직
        Rect taskbarRect = TaskbarInfo.GetTaskbarRect();
        float taskbarHeight = taskbarRect.height;
        float bottomBoundary = -(canvas.GetComponent<RectTransform>().rect.height / 2) + taskbarHeight + (rectTransform.rect.height / 2);
        return new Rect(0, bottomBoundary, canvas.GetComponent<RectTransform>().rect.width, rectTransform.rect.height);
    }

    private List<Rect> GetAdditionalBoundaries()
    {
        // 추가 경계 또는 충돌 요소를 관리하는 로직
        List<Rect> additionalBoundaries = new List<Rect>();
        
        // 예: 특정 UI 영역의 경계를 추가
        // additionalBoundaries.Add(new Rect(...));

        return additionalBoundaries;
    }
}
