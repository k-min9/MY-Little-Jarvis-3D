using UnityEngine;

public class AnimationPatLoop : StateMachineBehaviour
{
    [SerializeField] private int startFrame = 44;
    [SerializeField] private int endFrame = 57;
    [SerializeField] private int totalFrame = 96;  // 애니메이션 총 길이 (애니메이션 프레임수로 보는게 편하겠어.)
    [SerializeField] private float animationSpeed = 0.5f;  // 애니메이션 재생 속도
    [SerializeField] private bool pingPong = true;  // start -> end -> start로 반복하게 만들기
    
    private bool reverse = false;
    private float frameRate = 30f;

    private float timer = 0f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 현재 상태의 애니메이션 클립 정보 가져오기
        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);

        if (clipInfos.Length > 0)
        {
            AnimationClip clip = clipInfos[0].clip;
            // float clipLength = clip.length;
            frameRate = clip.frameRate;
            int frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);

            // Debug.Log($"[Pat] Clip name: {clip.name}");
            // Debug.Log($"[Pat] Length: {clipLength} sec");
            // Debug.Log($"[Pat] Frame Rate: {frameRate} fps");
            // Debug.Log($"[Pat] Total Frames: {frameCount}");

            totalFrame = frameCount;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!animator.GetBool("isPat"))
        {
            animator.speed = 1f;
            return;
        }

        float startTime = startFrame / frameRate;
        float endTime = endFrame / frameRate;
        float totalTime = totalFrame / frameRate;

        // 재생 방향 따라 타이머 증가/감소
        timer += Time.deltaTime * animationSpeed * (reverse ? -1f : 1f);

        // pingpong
        if (pingPong)
        {
            if (timer >= endTime)
            {
                timer = endTime;
                reverse = true;
            }
            else if (timer <= startTime)
            {
                timer = startTime;
                reverse = false;
            }
        }
        else
        {
            // 기존 방식 유지
            if (timer >= endTime) timer = startTime;
        }

        timer = Mathf.Clamp(timer, startTime, endTime);
        float normalizedTime = timer / totalTime;

        animator.Play(stateInfo.shortNameHash, layerIndex, normalizedTime);
        animator.speed = 0f;
    }
}
