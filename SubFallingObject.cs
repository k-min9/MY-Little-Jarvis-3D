using UnityEngine;
using System.Collections.Generic;

public class SubFallingObject : MonoBehaviour
{
    public float fallSpeed = 800f;
    private RectTransform rectTransform;
    private Animator animator;
    private float bottomBoundary;
    private SubStatusManager subStatusManager;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        animator = GetComponent<Animator>();
        subStatusManager = this.gameObject.GetComponentInParent<SubStatusManager>();

        Canvas canvas = GetComponentInParent<Canvas>();
        // animator.SetBool("IsFalling", isFalling);

        // bottomBoundary = -(canvas.GetComponent<RectTransform>().rect.height / 2) + rectTransform.rect.height * 0.5f; // + taskbarHeight; 
        bottomBoundary = -(canvas.GetComponent<RectTransform>().rect.height / 2);  //  + 50f;
    }

    void Update()
    {
        if (!SettingManager.Instance.settings.isGravity) 
        {
            subStatusManager.isFalling = false;
            return;
        }
        if (subStatusManager.isPicking) {
            return;
        }

        Vector2 newPosition = rectTransform.anchoredPosition;
        newPosition.y -= fallSpeed * Time.deltaTime; // 예상 변화 = 800*1/60

        float top = WindowCollisionManager.Instance.GetTopOfCollisionRect(newPosition);
        if(top <= -90000f) {  // 충돌X
            if (!subStatusManager.isFalling) { 
                // 낙하시작
                StartFalling();
            } else {
                // 이미 낙하중 (가속도 로직 넣으려면 여기)

            }
            newPosition.y = Mathf.Max(bottomBoundary, newPosition.y);
            rectTransform.anchoredPosition = newPosition;
        } else {  // 충돌
            if (subStatusManager.isFalling) { 
                // 낙하 중지 + 이동거리 재계산
                StopFalling();
            }
            newPosition.y = Mathf.Max(bottomBoundary, top-0.8f);  // window 좌표는 정수라 약간의 보정이 필요함
            rectTransform.anchoredPosition = newPosition;
            
        }
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
