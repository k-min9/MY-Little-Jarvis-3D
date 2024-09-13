using UnityEngine;
using System.Collections;

public class PhysicsManager : MonoBehaviour
{
    public Animator animator;
    public float moveSpeed = 120f;
    public float idleProbability = 0.7f;
    public float walkProbability = 0.3f;
    public RectTransform rectTransform;
    public Canvas canvas; 
    private float initialRotationY;

    private void Start()
    {
        walkProbability = 1.0f - idleProbability;
        initialRotationY = rectTransform.localEulerAngles.y;
        StartCoroutine(StateControlRoutine());
    }

    private IEnumerator StateControlRoutine()
    {
        while (true)
        {
            if (!StatusManager.Instance.IsFalling)
            {
                float rand = Random.Range(0f, 1f);
                if (rand < idleProbability)
                {
                    SetIdleState();
                }
                else if (rand < idleProbability + walkProbability/2)
                {
                    SetWalkLeftState();
                }
                else
                {
                    SetWalkRightState();
                }
            }
            yield return new WaitForSeconds(3f);
        }
    }

    private void SetIdleState()
    {
        animator.SetBool("isWalk", false);
        ResetRotation();
    }

    private void SetWalkLeftState()
    {
        animator.SetBool("isWalk", true);
        WalkLeftStart();
    }

    private void SetWalkRightState()
    {
        animator.SetBool("isWalk", true);
        WalkRightStart();
    }

    private void WalkLeftStart()
    {
        rectTransform.localEulerAngles = new Vector3(rectTransform.localEulerAngles.x, -90, rectTransform.localEulerAngles.z);
        StartCoroutine(MoveLeft());
    }

    private void WalkRightStart()
    {
        rectTransform.localEulerAngles = new Vector3(rectTransform.localEulerAngles.x, -270, rectTransform.localEulerAngles.z);
        StartCoroutine(MoveRight());
    }

    private IEnumerator MoveLeft()
    {
        while (animator.GetBool("isWalk"))
        {
            // 현재 위치 계산
            Vector2 newPosition = rectTransform.anchoredPosition + new Vector2(-moveSpeed * Time.deltaTime, 0);

            // Canvas의 크기 제한 가져오기
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float leftBound = -canvasRect.rect.width/2; // 캔버스 왼쪽 끝
            float rightBound = canvasRect.rect.width/2; // 캔버스 오른쪽 끝

            // 경계 체크 후 이동
            if (newPosition.x >= leftBound+50)
            {
                rectTransform.anchoredPosition = newPosition;
            } else {
                StopAllAnimations();
            }
            
            yield return null;
        }
        WalkLeftEnd();
    }

    private IEnumerator MoveRight()
    {
        while (animator.GetBool("isWalk"))
        {
            // 현재 위치 계산
            Vector2 newPosition = rectTransform.anchoredPosition + new Vector2(moveSpeed * Time.deltaTime, 0);

            // Canvas의 크기 제한 가져오기
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float leftBound = -canvasRect.rect.width/2; // 캔버스 왼쪽 끝
            float rightBound = canvasRect.rect.width/2; // 캔버스 오른쪽 끝

            // 경계 체크 후 이동
            if (newPosition.x <= rightBound-50)
            {
                rectTransform.anchoredPosition = newPosition;
            } else {
                StopAllAnimations();
            }

            yield return null;
        }
        WalkRightEnd();
    }

    private void WalkLeftEnd()
    {
        ResetRotation();
    }

    private void WalkRightEnd()
    {
        ResetRotation();
    }

    private void ResetRotation()
    {
        rectTransform.localEulerAngles = new Vector3(rectTransform.localEulerAngles.x, initialRotationY, rectTransform.localEulerAngles.z);
    }

    private void Update()
    {
        if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsFalling)
        {
            StopAllAnimations();
            return;
        }
    }

    public void StopAllAnimations()
    {
        animator.SetBool("isWalk", false);
        ResetRotation();
    }
}
