using UnityEngine;
using System.Collections.Generic;

public class SubFallingObject : MonoBehaviour
{
    public float fallSpeed = 800f;
    private RectTransform rectTransform;
    private float bottomBoundary;
    public List<RectTransform> otherObjects;
    private Animator animator;
    private SubStatusManager subStatusManager;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        animator = GetComponent<Animator>();
        subStatusManager = this.gameObject.GetComponentInParent<SubStatusManager>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            // 작업 표시줄의 위치를 고려한 하단 경계 계산
            // Rect taskbarRect = TaskbarInfo.GetTaskbarRect();
            // float taskbarHeight = taskbarRect.height;
            // bottomBoundary = -(canvas.GetComponent<RectTransform>().rect.height / 2) + rectTransform.rect.height * 0.5f; // + taskbarHeight;
            bottomBoundary = -(canvas.GetComponent<RectTransform>().rect.height / 2) + 50f;
        }

        // animator.SetBool("IsFalling", isFalling);
    }

    void Update()
    {
        if (!SettingManager.Instance.settings.isGravity) 
        {
            subStatusManager.isFalling = false;
            return;
        }

        if (!IsCollidingWithOtherObjects(rectTransform.anchoredPosition) && !subStatusManager.isFalling)
        {
            StartFalling();
        }

        if (subStatusManager.isFalling && !subStatusManager.isPicking)
        {
            Vector2 newPosition = rectTransform.anchoredPosition;
            newPosition.y -= fallSpeed * Time.deltaTime;

            if (newPosition.y > bottomBoundary && !IsCollidingWithOtherObjects(newPosition))
            {
                rectTransform.anchoredPosition = newPosition;
            }
            else
            {
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, Mathf.Max(bottomBoundary, rectTransform.anchoredPosition.y));
                StopFalling();
            }
        }
    }

    private bool IsCollidingWithOtherObjects(Vector2 newPosition)
    {
        foreach (RectTransform other in otherObjects)
        {
            if (other != rectTransform && RectTransformUtility.RectangleContainsScreenPoint(other, newPosition))
            {
                return true;
            }
        }
        return false;
    }

    public void StartFalling()
    {
        subStatusManager.isFalling = true;
        // animator.SetBool("IsFalling", true);
    }

    public void StopFalling()
    {
        subStatusManager.isFalling = false;
        // animator.SetBool("IsFalling", false);
    }
}
