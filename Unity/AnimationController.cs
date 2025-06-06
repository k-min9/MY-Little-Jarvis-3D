using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] public Animator _animator;
    private RectTransform rectTransform;

    float prevNormalizedTime;
    
    // idleCount를 프로퍼티로 변경
    public int IdleCount { get; set; }

    private int animationTotalCount = 5;  // 0~4.99

    void Start()
    {
        _animator = GetComponent<Animator>();
        rectTransform = GetComponent<RectTransform>();
        prevNormalizedTime = 0;
        IdleCount = 0; // 초기화
    }

    void Update()
    {
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
        {
            float currentTime = Mathf.Repeat(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1);
            
            if (currentTime < prevNormalizedTime) // 1.0 -> 0.0으로 넘어가는 순간
            {
                // Z축 초기화
                // Debug.Log("Z축 초기화!");
                Vector3 pos3D = rectTransform.anchoredPosition3D;
                pos3D.z = 0f;
                rectTransform.anchoredPosition3D = pos3D;


                IdleCount++; // IdleCount 증가
                // Debug.Log("IdleCount: " + IdleCount);

                if (IdleCount >= 10) // IdleCount가 10 이상일 때 실행
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
