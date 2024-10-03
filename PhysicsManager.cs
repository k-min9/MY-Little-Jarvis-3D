using UnityEngine;
using System.Collections;

/**
좌우 이동 관리
*/
public class PhysicsManager : MonoBehaviour
{
    public static PhysicsManager instance;


    public Animator animator;
    public float moveSpeed = 120f;
    public float idleProbability = 0.7f;
    public float walkProbability = 0.3f;
    public RectTransform rectTransform;
    public Canvas canvas; 
    private float initialRotationY;
    private Coroutine currentCoroutine;  // StateControlRoutine 제외 현재 작동 코루틴

    // 싱글톤 인스턴스에 접근하는 속성
    public static PhysicsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PhysicsManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 오브젝트 유지
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 파괴
        }
    }

    private void Start()
    {
        walkProbability = 1.0f - idleProbability;
        initialRotationY = rectTransform.localEulerAngles.y;
        StartCoroutine(StateControlRoutine());
    }

    private void Update()
    {
        if (StatusManager.Instance.IsPicking 
        || StatusManager.Instance.IsFalling 
        || StatusManager.Instance.IsChatting 
        || StatusManager.Instance.IsOptioning)
        {
            StopAllAnimations();
            return;
        }
    }

    private IEnumerator StateControlRoutine()
    {
        while (true)
        {
            if (!StatusManager.Instance.IsPicking 
            && !StatusManager.Instance.IsFalling 
            && !StatusManager.Instance.IsPicking
            && !StatusManager.Instance.IsOptioning)
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

    public void SetIdleState()
    {
        animator.SetBool("isWalk", false);
        ResetRotation();
    }

    public void SetWalkLeftState()
    {
        animator.SetBool("isWalk", true);
        WalkLeftStart();
    }

    public void SetWalkRightState()
    {
        animator.SetBool("isWalk", true);
        WalkRightStart();
    }

    private void WalkLeftStart()
    {
        if (currentCoroutine != null) {
            StopCoroutine(currentCoroutine);  // 기존 코루틴 중지
            currentCoroutine = null;
        }
        rectTransform.localEulerAngles = new Vector3(rectTransform.localEulerAngles.x, -90, rectTransform.localEulerAngles.z);
        currentCoroutine = StartCoroutine(MoveLeft());
    }

    private void WalkRightStart()
    {
        if (currentCoroutine != null) {
            StopCoroutine(currentCoroutine);  // 기존 코루틴 중지
            currentCoroutine = null;
        }
        rectTransform.localEulerAngles = new Vector3(rectTransform.localEulerAngles.x, -270, rectTransform.localEulerAngles.z);
        currentCoroutine = StartCoroutine(MoveRight());
    }

    private IEnumerator MoveLeft()
    {
        while (animator.GetBool("isWalk"))
        {
            // 현재 위치 계산
            Vector2 newPosition = rectTransform.anchoredPosition + new Vector2(-moveSpeed * Time.deltaTime * SettingManager.Instance.settings.char_speed / 100f, 0);

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
            Vector2 newPosition = rectTransform.anchoredPosition + new Vector2(moveSpeed * Time.deltaTime * SettingManager.Instance.settings.char_speed / 100f, 0);

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

    public void StopAllAnimations()
    {
        if (currentCoroutine != null) {
            StopCoroutine(currentCoroutine);  // 기존 코루틴 중지
            currentCoroutine = null;
        }

        animator.SetBool("isWalk", false);
        ResetRotation();
    }
}
