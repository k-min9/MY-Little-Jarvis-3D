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

    // X초 표시 같은데에도 사용
    public Sprite emotionSpriteYes;      // 긍정
    public Sprite emotionSpriteNo;       // 부정
    
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

    // Yes/No 전용 쿨타임 관리
    private Dictionary<GameObject, float> yesNoCooldownMap = new Dictionary<GameObject, float>();

    public void Start()
    {
        _canvas = FindObjectOfType<Canvas>();
    }

    public GameObject ShowEmotionBalloon(GameObject target, string spriteName = "Love", float duration = 60f)
    {
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
            controller.Initialize(target, duration);
        }

        return emotionBalloonInstance;
    }

    public GameObject ShowYesEmotionBalloonForSec(GameObject target, float duration = 3f, float cooltime = 5f)
    {
        if (!CheckAndSetCooldown(target, cooltime)) return null;
        return ShowEmotionBalloon(target, "Yes", duration);
    }

    public GameObject ShowNoEmotionBalloonForSec(GameObject target, float duration = 3f, float cooltime = 5f)
    {
        if (!CheckAndSetCooldown(target, cooltime)) return null;
        return ShowEmotionBalloon(target, "No", duration);
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
            case "Listen": return emotionSpriteListen;
            case "Write": return emotionSpriteWrite;
            default: return null;
        }
    }
}
