using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// AnimationPlayerManager 정보성 Class
public class PlayerRuntime
{
    public Animator animator;                   // 대상 Animator 참조
    public PlayableGraph graph;                 // 재사용 그래프
    public AnimationPlayableOutput output;      // Animator 출력 연결
    public AnimationClipPlayable clipPlayable;  // 클립 재생용 Playable
    public List<AnimationClip> clipsCache = new List<AnimationClip>();  // 필터링된 클립 캐시
    public int controllerInstanceId = 0;        // Controller 변경 감지용
    public Vector3 rootPositionBackup;          // 루트 위치 백업
    public Quaternion rootRotationBackup;       // 루트 회전 백업
    public bool isGraphCreated = false;         // 그래프 생성 여부
    public bool isRootBackedUp = false;         // 루트 백업 여부
}

public class AnimationPlayerManager : MonoBehaviour
{
    private static AnimationPlayerManager instance;
    public static AnimationPlayerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AnimationPlayerManager>();
            }
            return instance;
        }
    }

    public List<GameObject> players = new List<GameObject>();
    private Dictionary<GameObject, PlayerRuntime> runtimeMap = new Dictionary<GameObject, PlayerRuntime>();

    private float lastCallTime;
    private bool isMemeModeActive = false;
    private Coroutine memeCoroutine;

    private const float RELEASE_DELAY = 3.0f;
    private const float DEFAULT_INTERVAL = 0.1f;
    private const int DEFAULT_COUNT = 100;

    void Update()
    {
        if (!isMemeModeActive)
            return;

        if (Time.time - lastCallTime >= RELEASE_DELAY)
        {
            ReleaseAllPlayers();
        }
    }
    
    public void RegisterPlayer(GameObject playerObj)
    {
        if (playerObj == null)
        {
            Debug.LogWarning("[AnimationPlayerManager] RegisterPlayer: playerObj is null");
            return;
        }

        Animator anim = playerObj.GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogWarning("[AnimationPlayerManager] RegisterPlayer: Animator component not found");
            return;
        }

        if (players.Contains(playerObj))
        {
            Debug.Log($"[AnimationPlayerManager] Player already registered: {playerObj.name}");
            return;
        }

        players.Add(playerObj);

        PlayerRuntime runtime = new PlayerRuntime();
        runtime.animator = anim;
        runtimeMap[playerObj] = runtime;

        Debug.Log($"[AnimationPlayerManager] Player registered: {playerObj.name} (Total: {players.Count})");
    }

    public void RegisterPlayers(IEnumerable<GameObject> playerObjs)
    {
        if (playerObjs == null)
        {
            Debug.LogWarning("[AnimationPlayerManager] RegisterPlayers: playerObjs is null");
            return;
        }

        foreach (GameObject playerObj in playerObjs)
        {
            RegisterPlayer(playerObj);
        }
    }

    public void UnregisterPlayer(GameObject playerObj)
    {
        if (playerObj == null || !players.Contains(playerObj))
            return;

        if (runtimeMap.ContainsKey(playerObj))
        {
            ReleasePlayer(runtimeMap[playerObj]);
            runtimeMap.Remove(playerObj);
        }

        players.Remove(playerObj);

        Debug.Log($"[AnimationPlayerManager] Player unregistered: {playerObj.name} (Remaining: {players.Count})");
    }

    public void ClearPlayers()
    {
        ReleaseAllPlayers();
        players.Clear();
        runtimeMap.Clear();

        Debug.Log("[AnimationPlayerManager] All players cleared");
    }

    public void StartStopMeme(float interval = DEFAULT_INTERVAL, int count = DEFAULT_COUNT)
    {
        if (memeCoroutine != null)
        {
            StopCoroutine(memeCoroutine);
            memeCoroutine = null;
        }

        memeCoroutine = StartCoroutine(StopMemeCoroutine(interval, count));
    }

    private IEnumerator StopMemeCoroutine(float interval, int count)
    {
        for (int i = 0; i < count; i++)
        {
            StopAtRandomMoment();
            yield return new WaitForSeconds(interval);
        }

        memeCoroutine = null;
    }

    public void StopStopMeme()
    {
        ReleaseAllPlayers();
    }

    public void StopAtRandomMoment()
    {
        if (players.Count == 0)
        {
            Debug.LogWarning("[AnimationPlayerManager] No players registered");
            return;
        }

        int processedCount = 0;

        // 역순 루프로 무효한 플레이어 정리
        for (int i = players.Count - 1; i >= 0; i--)
        {
            GameObject player = players[i];

            if (!ValidatePlayer(player))
            {
                UnregisterPlayer(player);
                continue;
            }

            PlayerRuntime runtime = runtimeMap[player];

            EnsureClipsCache(runtime);

            if (runtime.clipsCache == null || runtime.clipsCache.Count == 0)
            {
                Debug.LogWarning($"[AnimationPlayerManager] No valid clips for {player.name}");
                continue;
            }

            EnsureGraph(runtime);
            BackupRootTransformIfNeeded(runtime);
            ApplyRandomPoseAndFreeze(runtime);
            RestoreRootTransformIfNeeded(runtime);

            processedCount++;
        }

        // 실제로 처리된 캐릭터가 있을 때만 Meme 모드 활성화
        if (processedCount > 0)
        {
            isMemeModeActive = true;
            lastCallTime = Time.time;
        }
    }

    private bool ValidatePlayer(GameObject player)
    {
        if (player == null || !player.activeInHierarchy)
        {
            return false;
        }

        if (!runtimeMap.ContainsKey(player))
        {
            return false;
        }

        PlayerRuntime runtime = runtimeMap[player];
        if (runtime == null || runtime.animator == null)
        {
            return false;
        }

        return true;
    }

    private void EnsureClipsCache(PlayerRuntime runtime)
    {
        RuntimeAnimatorController controller = runtime.animator.runtimeAnimatorController;
        if (controller == null)
        {
            return;
        }

        int currentInstanceId = controller.GetInstanceID();

        if (runtime.clipsCache.Count == 0 || runtime.controllerInstanceId != currentInstanceId)
        {
            runtime.clipsCache.Clear();
            runtime.controllerInstanceId = currentInstanceId;

            AnimationClip[] clips = controller.animationClips;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            HashSet<AnimationClip> uniqueClips = new HashSet<AnimationClip>();
            foreach (AnimationClip clip in clips)
            {
                if (clip == null) continue;
                if (clip.length < 0.1f) continue;

                string clipName = clip.name.ToLower();
                if (clipName.Contains("face")) continue;
                if (clipName.Contains("blink")) continue;
                if (clipName.Contains("eye")) continue;
                if (clipName.Contains("additive")) continue;

                uniqueClips.Add(clip);
            }

            runtime.clipsCache = uniqueClips.ToList();
        }
    }

    private void EnsureGraph(PlayerRuntime runtime)
    {
        // 이미 생성된 경우 Play 상태 보장 후 재사용
        if (runtime.isGraphCreated && runtime.graph.IsValid())
        {
            if (!runtime.graph.IsPlaying())
            {
                runtime.graph.Play();
            }
            return;
        }

        // 새로 생성
        runtime.graph = PlayableGraph.Create($"AnimationPlayerGraph_{runtime.animator.gameObject.name}");
        runtime.output = AnimationPlayableOutput.Create(runtime.graph, "Output", runtime.animator);
        runtime.clipPlayable = AnimationClipPlayable.Create(runtime.graph, null);
        runtime.output.SetSourcePlayable(runtime.clipPlayable);
        runtime.output.SetWeight(1f);

        runtime.graph.Play();
        runtime.isGraphCreated = true;
    }

    private void BackupRootTransformIfNeeded(PlayerRuntime runtime)
    {
        if (runtime.isRootBackedUp)
            return;

        Transform root = runtime.animator.transform;
        runtime.rootPositionBackup = root.localPosition;
        runtime.rootRotationBackup = root.localRotation;
        runtime.isRootBackedUp = true;
    }

    private void ApplyRandomPoseAndFreeze(PlayerRuntime runtime)
    {
        AnimationClip randomClip = runtime.clipsCache[Random.Range(0, runtime.clipsCache.Count)];
        float randomTime = Random.Range(0f, randomClip.length);

        // 기존 clipPlayable 파괴
        if (runtime.clipPlayable.IsValid())
        {
            runtime.clipPlayable.Destroy();
        }

        // 새 clipPlayable 생성 및 연결
        runtime.clipPlayable = AnimationClipPlayable.Create(runtime.graph, randomClip);
        runtime.output.SetSourcePlayable(runtime.clipPlayable);

        // 권장 순서: speed=0 → time → Evaluate
        runtime.clipPlayable.SetSpeed(0);
        runtime.clipPlayable.SetTime(randomTime);
        runtime.graph.Evaluate(0f);
    }

    private void RestoreRootTransformIfNeeded(PlayerRuntime runtime)
    {
        if (!runtime.isRootBackedUp)
            return;

        Transform root = runtime.animator.transform;
        root.localPosition = runtime.rootPositionBackup;
        root.localRotation = runtime.rootRotationBackup;
    }

    private void ReleasePlayer(PlayerRuntime runtime)
    {
        if (runtime == null)
            return;

        if (runtime.isGraphCreated && runtime.graph.IsValid())
        {
            runtime.graph.Destroy();
        }

        // 잔여 참조 정리
        runtime.graph = default;
        runtime.output = default;
        runtime.clipPlayable = default;

        runtime.isGraphCreated = false;
        runtime.isRootBackedUp = false;
    }

    private void ReleaseAllPlayers()
    {
        if (memeCoroutine != null)
        {
            StopCoroutine(memeCoroutine);
            memeCoroutine = null;
        }

        foreach (PlayerRuntime runtime in runtimeMap.Values)
        {
            ReleasePlayer(runtime);
        }

        isMemeModeActive = false;

        Debug.Log("[AnimationPlayerManager] All players released");
    }

    // 외부에 의한 긴급 Manager 초기화 용 
    public void Reset()
    {
        ReleaseAllPlayers();
        players.Clear();
        runtimeMap.Clear();
        isMemeModeActive = false;
        lastCallTime = 0f;
        memeCoroutine = null;
    }
}