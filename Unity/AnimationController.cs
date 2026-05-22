using UnityEngine;

// AnimationController에서 idle 애니메이션 순환 관리
// PhysicsHandler에서 방향초기화 관리.
public class AnimationController : MonoBehaviour
{
    [SerializeField] public Animator _animator;
    private RectTransform rectTransform;
    private CharAttributes charAttributes;  // 같은 레벨에 있음

    float prevNormalizedTime;
    
    // idleCount를 프로퍼티로 변경
    public int IdleCount { get; set; }

    private int animationTotalCount = 5;  // 0~4.99

    void Start()
    {
        _animator = GetComponent<Animator>();
        rectTransform = GetComponent<RectTransform>();
        charAttributes = GetComponent<CharAttributes>();
        prevNormalizedTime = 0;
        IdleCount = 0; // 초기화
    }

    void Update()
    {
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
        {
            // idle일 경우 주기적 Z축 초기화
            // Debug.Log("Z축 초기화!");
            Vector3 pos3D = rectTransform.anchoredPosition3D;
            // 위치에 따라 Z 다름
            if (charAttributes.type == "3D")
            {
                pos3D.z = -70.0f;  // 캐릭터
            }
            else
            {
                pos3D.z = 0.0f;  // 오퍼레이터
            }

            rectTransform.anchoredPosition3D = pos3D;

            // idle 애니메이션 idleCount 횟수 실행될 경우, 랜덤 blendidle 로 변경
            float currentTime = Mathf.Repeat(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1);
            if (currentTime < prevNormalizedTime) // 1.0 -> 0.0으로 넘어가는 순간
            {
                IdleCount++; // IdleCount 증가
                // Debug.Log("IdleCount: " + IdleCount);

                if (IdleCount >= 5) // IdleCount가 5 이상일 때 실행
                {
                    float randomIndex = Random.Range(0, animationTotalCount);  // 0~4.99
                    _animator.SetFloat("BlendIdle", randomIndex);
                    // Debug.Log("다음 Idle 애니메이션 : " + randomIndex);
                    
                    IdleCount = 0; // IdleCount 초기화
                }
            }
            prevNormalizedTime = currentTime;
        }
    }
}
