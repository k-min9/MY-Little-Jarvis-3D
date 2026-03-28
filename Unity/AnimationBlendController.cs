using UnityEngine;

// 2d_general 기반 캐릭터의 Blend Tree 애니메이션을 관리
// 각 상태(Idle, Talk, Listen, Pat, Walk, Pick)별 클립 갯수를 주입받아
// 10초 간격 및 상태 전환 시 랜덤 Blend 값을 설정
public class AnimationBlendController : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    [Header("Blend Tree 클립 갯수 (JSON에서 주입)")]
    public int blendCount_idle   = 1;
    public int blendCount_talk   = 1;
    public int blendCount_listen = 1;
    public int blendCount_pat    = 1;
    public int blendCount_walk   = 1;
    public int blendCount_pick   = 1;
    public int blendCount_fall   = 1;

    private const float BLEND_INTERVAL = 10f;
    private float blendTimer = 0f;
    private string lastStateName = "";

    // Animator 상태 이름 ↔ Blend 파라미터 이름 매핑
    private static readonly string[] STATE_NAMES  = { "idle", "Talk", "Listen", "Pat", "Walk", "Pick", "Fall" };
    private static readonly string[] BLEND_PARAMS = { "BlendIdle", "BlendTalk", "BlendListen", "BlendPat", "BlendWalk", "BlendPick", "BlendFall" };

    private void Start()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        blendTimer = 0f;
        lastStateName = "";
    }

    private void Update()
    {
        if (_animator == null) return;

        string currentState = GetCurrentStateName();

        // TODO : 랜덤상태재생은 AnimationBlendStateChanger.cs(구현완료)으로 전환하는게 더 성능 좋음(Yuuka 참조)
        if (currentState != lastStateName)
        {
            lastStateName = currentState;
            blendTimer = 0f;
            RandomizeBlend(currentState);
        }

        // "idle" 상태이면서 blend 클립이 2개 이상일 때만 시간에 따라 랜덤하게 바꿈
        if (currentState == "idle" && GetBlendCount(currentState) > 1)
        {
            blendTimer += Time.deltaTime;
            if (blendTimer >= BLEND_INTERVAL)
            {
                blendTimer = 0f;
                RandomizeBlend(currentState);
            }
        }
    }

    // StateMachineBehaviour(StateBlendBehaviour) 에서 상태 진입 시 호출
    public void OnStateEntered(string stateName)
    {
        lastStateName = stateName;
        blendTimer = 0f;
        RandomizeBlend(stateName);
    }


    // 현재 활성 Animator 상태 이름을 반환
    private string GetCurrentStateName()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
        foreach (string name in STATE_NAMES)
        {
            if (info.IsName(name)) return name;
        }
        return "";
    }

    // 주어진 상태에 대응하는 Blend 파라미터를 랜덤 설정
    private void RandomizeBlend(string stateName)
    {
        for (int i = 0; i < STATE_NAMES.Length; i++)
        {
            if (STATE_NAMES[i] != stateName) continue;

            int maxCount = GetBlendCount(stateName);
            if (maxCount <= 1) return;  // 클립이 1개면 변경 불필요

            float value = Random.Range(0, maxCount-0.01f);  // 0 ~ maxCount-1 (float)
            _animator.SetFloat(BLEND_PARAMS[i], value);
            // Debug.Log($"[BlendCtrl] {stateName} → {BLEND_PARAMS[i]} = {value}");
            return;
        }
    }

    private int GetBlendCount(string stateName)
    {
        switch (stateName)
        {
            case "idle":   return blendCount_idle;
            case "Talk":   return blendCount_talk;
            case "Listen": return blendCount_listen;
            case "Pat":    return blendCount_pat;
            case "Walk":   return blendCount_walk;
            case "Pick":   return blendCount_pick;
            case "Fall":   return blendCount_fall;
            default:       return 1;
        }
    }

    // 외부(ChangeCharCardController 등)에서 blendCount를 일괄 주입
    public void InjectBlendCounts(int idle=1, int talk=1, int listen=1, int pat=1, int walk=1, int pick=1, int fall = 1)
    {
        blendCount_idle   = idle;
        blendCount_talk   = talk;
        blendCount_listen = listen;
        blendCount_pat    = pat;
        blendCount_walk   = walk;
        blendCount_pick   = pick;
        blendCount_fall   = fall;

        // 주입 직후 현재 상태에 바로 반영
        lastStateName = "";
    }
}
