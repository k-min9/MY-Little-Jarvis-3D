using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmotionBalloonManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static EmotionBalloonManager instance;
    public static EmotionBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EmotionBalloonManager>();
            }
            return instance;
        }
    }

    private Canvas _canvas;
    public GameObject emotionBalloonPrefab;
    [SerializeField] public RectTransform targetPortraitTransform; // Operator 모드용

    // X초 표시 같은데에도 사용
    public Sprite emotionSpriteYes;      // 긍정
    public Sprite emotionSpriteNo;       // 부정
    public Sprite emotionSpriteCheck;    // 확인
    
    public Sprite emotionSpriteLove;     // 쓰다듬기
    public Sprite emotionSpriteRefresh;  // 재답변
    public Sprite emotionSpriteTime;     // 로딩 중
    public Sprite emotionSpriteAlarm;    // 알람 설정중
    public Sprite emotionSpriteBook;     // 설정 변경, 학습중
    public Sprite emotionSpriteGift;     // 클릭시 반응
    public Sprite emotionSpriteSearch;   // 웹 검색
    public Sprite emotionSpriteQuestion; // 물음표
    public Sprite emotionSpriteListen;   // 듣기
    public Sprite emotionSpriteWrite;    // 작성
    public Sprite emotionSpriteImage;    // 이미지

    // 발화 대상 표시용 캐릭터 아이콘 (아로프라 채널)
    public Sprite emotionSpriteSensei;   // 선생 아이콘
    public Sprite emotionSpriteArona;    // 아로나 아이콘
    public Sprite emotionSpritePlana;    // 프라나 아이콘

    // VL Agent 상태 표시용 아이콘
    public Sprite emotionSpriteThink;    // 생각중 (AI 서버 작업중)
    public Sprite emotionSpriteVerify;   // 검증중 (동작 확인 중)
    public Sprite emotionSpriteExecute;  // 수행중 (클릭/스크린샷 등)

    // Yes/No 전용 쿨타임 관리
    private Dictionary<GameObject, float> yesNoCooldownMap = new Dictionary<GameObject, float>();
    
    // Target별 활성 말풍선 관리 (누수 방지)
    private Dictionary<GameObject, GameObject> activeBalloonMap = new Dictionary<GameObject, GameObject>();

    public void Start()
    {
        _canvas = FindObjectOfType<Canvas>();
    }

    public GameObject ShowEmotionBalloon(GameObject target, string spriteName = "Love", float duration = 60f)
    {
        Debug.Log($"[EmotionBalloon] ShowEmotionBalloon: {spriteName}, {duration}");

        // 말풍선 생성
        GameObject emotionBalloonInstance = Instantiate(emotionBalloonPrefab, _canvas.transform);

        // 말풍선 이미지 변경 ("Image" > "Emotion Bubble Image" 찾기)
        RectTransform emotionBalloonRect = emotionBalloonInstance.GetComponent<RectTransform>();
        Transform imageTransform = emotionBalloonRect.Find("Image");
        if (imageTransform != null)
        {
            Image emotionBubbleImage = imageTransform.Find("Emotion Bubble Image")?.GetComponent<Image>();
            if (emotionBubbleImage != null)
            {
                Sprite selectedSprite = GetSpriteByName(spriteName);
                if (selectedSprite != null)
                {
                    emotionBubbleImage.sprite = selectedSprite;
                }
                else
                {
                    Debug.LogWarning($"정의되지 않은 spriteName: {spriteName}");
                }
            }
            else
            {
                Debug.LogWarning("Emotion Bubble Image를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("Image 오브젝트를 찾을 수 없습니다.");
        }

        // Controller에서 위치와 시간 관리
        EmotionBalloonController controller = emotionBalloonInstance.GetComponent<EmotionBalloonController>();
        if (controller != null)
        {
            // Operator 모드일 때 숨겨진 캐릭터 대신 Portrait에 표시
            if (ChatModeManager.Instance.IsOperatorMode() && OperatorManager.Instance.portraitTransform != null)
            {
                target = OperatorManager.Instance.portraitTransform.gameObject;
                controller.targetPortraitTransform = this.targetPortraitTransform;  // 주입
            }
            controller.Initialize(target, duration);
        }

        return emotionBalloonInstance;
    }

    public GameObject ShowEmotionBalloonForSec(GameObject target, string spriteName, float duration = 3f, float cooltime = 5f)
    {
        if (!CheckAndSetCooldown(target, cooltime)) return null;
        return ShowEmotionBalloon(target, spriteName, duration);
    }

    // Target별로 하나의 말풍선만 유지 (덮어쓰기 방식)
    public GameObject SetEmotionBalloonForTarget(GameObject target, string spriteName = "Love", float duration = 60f)
    {
        if (target == null)
        {
            Debug.LogWarning("[EmotionBalloon] target이 null입니다.");
            return null;
        }

        // 기존 말풍선이 있으면 제거
        if (activeBalloonMap.TryGetValue(target, out GameObject existingBalloon))
        {
            if (existingBalloon != null)
            {
                Destroy(existingBalloon);
            }
            activeBalloonMap.Remove(target);
        }

        // 새 말풍선 생성
        GameObject newBalloon = ShowEmotionBalloon(target, spriteName, duration);
        
        if (newBalloon != null)
        {
            // Dictionary에 등록
            activeBalloonMap[target] = newBalloon;
            
            // Controller에 target 참조 전달
            EmotionBalloonController controller = newBalloon.GetComponent<EmotionBalloonController>();
            if (controller != null)
            {
                controller.SetTarget(target);
            }
        }

        return newBalloon;
    }

    // Target의 말풍선 제거 (SetEmotionBalloonForTarget의 remove 버전)
    public void RemoveEmotionBalloonForTarget(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning("[EmotionBalloon] target이 null입니다.");
            return;
        }

        // 활성 말풍선이 있으면 제거
        if (activeBalloonMap.TryGetValue(target, out GameObject existingBalloon))
        {
            if (existingBalloon != null)
            {
                Destroy(existingBalloon);
            }
            activeBalloonMap.Remove(target); // Dictionary에서 제거
        }
    }

    // Controller의 OnDestroy에서 호출되어 Dictionary 정리
    public void RemoveBalloonFromMap(GameObject target)
    {
        if (target != null && activeBalloonMap.ContainsKey(target))
        {
            activeBalloonMap.Remove(target);
        }
    }

    private bool CheckAndSetCooldown(GameObject target, float cooltime)
    {
        if (target == null) return false;

        float now = Time.time;

        // 쿨타임 체크
        if (yesNoCooldownMap.TryGetValue(target, out float lastTime))
        {
            if (now - lastTime < cooltime)
            {
                Debug.Log($"[EmotionBalloon] 쿨타임 중: {now - lastTime:F1}초 / 필요: {cooltime}초");
                return false;
            }
        }

        yesNoCooldownMap[target] = now;
        return true;
    }

    private Sprite GetSpriteByName(string spriteName)
    {
        switch (spriteName)
        {
            case "Love": return emotionSpriteLove;
            case "Refresh": return emotionSpriteRefresh;
            case "Time": return emotionSpriteTime;
            case "Alarm": return emotionSpriteAlarm;
            case "Book": return emotionSpriteBook;
            case "Gift": return emotionSpriteGift;
            case "Search": return emotionSpriteSearch;
            case "Question": return emotionSpriteQuestion;
            case "Yes": return emotionSpriteYes;
            case "No": return emotionSpriteNo;
            case "Check": return emotionSpriteCheck;
            case "Listen": return emotionSpriteListen;
            case "Write": return emotionSpriteWrite;
            case "Image": return emotionSpriteImage;
            case "sensei": return emotionSpriteSensei;
            case "arona": return emotionSpriteArona;
            case "plana": return emotionSpritePlana;
            // VL Agent
            case "Think": return emotionSpriteThink;
            case "Verify": return emotionSpriteVerify;
            case "Execute": return emotionSpriteExecute;
            default: return null;
        }
    }
}
