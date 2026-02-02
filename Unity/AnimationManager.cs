using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static AnimationManager instance;
    public static AnimationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AnimationManager>();
            }
            return instance;
        }
    }

    private int animationDanceTotalCount = 31;  // 31 - 1

    public void Idle()
    {
        try
        {
            Animator _animator = CharManager.Instance.GetCurrentCharacter().GetComponent<Animator>();

            _animator.Play("idle", 0, 0);  // 현재 애니메이션 중지
            _animator.SetBool("isWalk", false);
            _animator.SetBool("isRun", false);
            _animator.SetBool("isPick", false);
            _animator.SetBool("isDance", false);
            _animator.SetBool("isPat", false);
            _animator.SetBool("isListen", false);
        }
        catch
        {
            Debug.Log("No AnimationManager for Idle");
        }
    }

    public void Dance()
    {
        try
        {
            Animator _animator = CharManager.Instance.GetCurrentCharacter().GetComponent<Animator>();

            _animator.Play("idle", 0, 0);  // 현재 애니메이션 중지
            float randomIndex = Random.Range(0, animationDanceTotalCount);  // 0~30
            _animator.SetFloat("BlendDance", randomIndex);
            _animator.SetBool("isWalk", false);
            _animator.SetBool("isRun", false);
            _animator.SetBool("isPick", false);
            _animator.SetBool("isDance", true);
        }
        catch
        {
            Debug.Log("No AnimationManager for Dance");
        }
    }

    // targetCharacter가 null이면 현재 캐릭터 사용
    public void Listen(GameObject targetCharacter = null)
    {
        try
        {
            if (targetCharacter == null)
            {
                targetCharacter = CharManager.Instance.GetCurrentCharacter();
            }
            
            Animator _animator = targetCharacter.GetComponent<Animator>();

            _animator.SetBool("isWalk", false);
            _animator.SetBool("isRun", false);
            _animator.SetBool("isPick", false);
            _animator.SetBool("isDance", false);
            _animator.SetBool("isListen", true);

            // 얼굴 표정 있을 경우 사용 (targetCharacter에 적용)
            EmotionManager.Instance.ShowEmotionFromAction("listen", targetCharacter);
        }
        catch
        {
            Debug.Log("No AnimationManager for Listen");
        }
    }

    // targetCharacter가 null이면 현재 캐릭터 사용
    public void ListenDisable(GameObject targetCharacter = null)
    {
        try
        {
            if (targetCharacter == null)
            {
                targetCharacter = CharManager.Instance.GetCurrentCharacter();
            }
            
            Animator _animator = targetCharacter.GetComponent<Animator>();
            _animator.SetBool("isListen", false);

            // 얼굴 표정 있을 경우 사용 (targetCharacter에 적용)
            // 고도화 고려 : 현재 Listen 재생일 경우 사용하는 추가 기능도 가능(StatusManager에서 관리 추천)
            EmotionManager.Instance.ShowEmotionFromAction("default", targetCharacter);   // normal도 고려
        }
        catch
        {
            Debug.Log("No AnimationManager for ListenDisable");
        }
    }

    public void Hide()
    {
        try
        {
            Animator _animator = CharManager.Instance.GetCurrentCharacter().GetComponent<Animator>();

            _animator.SetBool("isWalk", false);
            _animator.SetBool("isRun", false);
            _animator.SetBool("isPick", false);
            _animator.SetBool("isDance", false);
            _animator.SetTrigger("doHide");
        }
        catch
        {
            Debug.Log("No AnimationManager for Hide");
        }
    }

    public void Show()
    {
        try
        {
            Animator _animator = CharManager.Instance.GetCurrentCharacter().GetComponent<Animator>();

            _animator.SetBool("isWalk", false);
            _animator.SetBool("isRun", false);
            _animator.SetBool("isPick", false);
            _animator.SetBool("isDance", false);
            _animator.SetTrigger("doShow");
        }
        catch
        {
            Debug.Log("No AnimationManager for Show");
        }
    }
}
