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

    public Sprite emotionSpriteLove;

    public void Start()
    {
        _canvas = FindObjectOfType<Canvas>();
    }

    public GameObject ShowEmotionBalloon(GameObject target, float duration = 60f)
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
                emotionBubbleImage.sprite = emotionSpriteLove;
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
