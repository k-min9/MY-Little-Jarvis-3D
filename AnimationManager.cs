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

    public void Listen()
    {
        try
        {
            Animator _animator = CharManager.Instance.GetCurrentCharacter().GetComponent<Animator>();

            _animator.SetBool("isWalk", false);
            _animator.SetBool("isRun", false);
            _animator.SetBool("isPick", false);
            _animator.SetBool("isDance", false);
            _animator.SetBool("isListen", true);
        }
        catch
        {
            Debug.Log("No AnimationManager for Listen");
        }
    }

    public void ListenDisable()
    {
        try
        {
            Animator _animator = CharManager.Instance.GetCurrentCharacter().GetComponent<Animator>();
            _animator.SetBool("isListen", false);
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
