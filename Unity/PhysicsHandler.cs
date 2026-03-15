using UnityEngine;

// AnimationController에서 애니메이션 순환 관리
// 1초마다 idle 상태인지 확인하고, idle 상태일 때 원래 방향으로 되돌리기
public class PhysicsHandler : MonoBehaviour
{
    private Animator animator;
    private RectTransform rectTransform;
    private float initialRotationY;
    private float updateTimer = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            initialRotationY = rectTransform.localEulerAngles.y;
        }
    }

    void Update()
    {
        if (animator == null || rectTransform == null) return;

        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            updateTimer = 1f; // 1초 리셋
            
            // 현재 애니메이션이 idle인지 확인
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
            {
                Debug.Log("[PhysicsHandler] idle state");
                ResetRotation();
            }
        }
    }

    private void ResetRotation()
    {
        // idle 상태일 때 원래 방향으로 되돌리기
        rectTransform.localEulerAngles = new Vector3(rectTransform.localEulerAngles.x, initialRotationY, rectTransform.localEulerAngles.z);
    }
}
