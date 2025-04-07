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
                    controller.ShowEmotion("normal");
                    
                    return;
                }
                
                Debug.Log("No Emotion default-normal");
                break;
        }
    }

    // Test
    public void NextEmotion()
    {
        GameObject gameObject = CharManager.Instance.GetCurrentCharacter();
        // EmotionFaceAronaController controller = gameObject.GetComponent<EmotionFaceAronaController>();
        EmotionFaceController controller = gameObject.GetComponent<EmotionFaceController>();
        controller.NextAnimation();
    }

}
