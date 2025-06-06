using UnityEngine;

public class EmotionManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static EmotionManager instance;
    public static EmotionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EmotionManager>();
            }
            return instance;
        }
    }

    // 얼굴 표정 보여주기
    public void ShowEmotion(string emotion, GameObject gameObject = null)
    {
        Debug.Log("Show Emotion : " + emotion);

        if (gameObject == null){
            gameObject = CharManager.Instance.GetCurrentCharacter();
        }
        
        EmotionFaceController controller = null;
        switch (emotion)
        {
            case "><":
                controller = gameObject.GetComponent<EmotionFaceController>();
                if (controller != null) {
                    controller.ShowEmotion("><");
                    
                    return;
                }
                
                Debug.Log("No Emotion [><]");
                break;
            default:
                controller = gameObject.GetComponent<EmotionFaceController>();
                if (controller != null) {
                    controller.ShowEmotion(emotion);
                    // controller.ShowEmotion("normal");
                    
                    return;
                }
                
                Debug.Log("No Emotion default-normal");
                break;
        }
    }

    // 표정에 따른 얼굴 변화 Interface 호출
    public void ShowEmotionFromEmotion(string emotion)
    {
        GameObject gameObject = CharManager.Instance.GetCurrentCharacter();
        EmotionFaceController controller = gameObject.GetComponent<EmotionFaceController>();
        controller.ShowEmotionFromEmotion(emotion);
    }

    // Test
    public void NextEmotion()
    {
        GameObject gameObject = CharManager.Instance.GetCurrentCharacter();
        EmotionFaceController controller = gameObject.GetComponent<EmotionFaceController>();
        controller.NextAnimation();
    }

}
