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
    
    public Sprite emotionSpriteLove;     // 쓰다듬기
    public Sprite emotionSpriteRefresh;  // 재답변
    public Sprite emotionSpriteTime;     // 로딩 중
    public Sprite emotionSpriteAlarm;    // 알람 설정중
    public Sprite emotionSpriteBook;     // 설정 변경, 학습중
    public Sprite emotionSpriteGift;     // 클릭시 반응
    public Sprite emotionSpriteSearch;   // 웹 검색

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
                switch (spriteName)
                {
                    case "Love":
                        emotionBubbleImage.sprite = emotionSpriteLove;
                        break;
                    case "Refresh":
                        emotionBubbleImage.sprite = emotionSpriteRefresh;
                        break;
                    case "Time":
                        emotionBubbleImage.sprite = emotionSpriteTime;
                        break;
                    case "Alarm":
                        emotionBubbleImage.sprite = emotionSpriteAlarm;
                        break;
                    case "Book":
                        emotionBubbleImage.sprite = emotionSpriteBook;
                        break;
                    case "Gift":
                        emotionBubbleImage.sprite = emotionSpriteGift;
                        break;
                    case "Search":
                        emotionBubbleImage.sprite = emotionSpriteSearch;
                        break;
                    default:
                        Debug.LogWarning($"정의되지 않은 spriteName: {spriteName}");
                        break;
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
}
