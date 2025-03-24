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

    public void Dance()
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

}
