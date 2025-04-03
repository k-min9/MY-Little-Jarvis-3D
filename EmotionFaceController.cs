using UnityEngine;

// 감정 표현을 위한 기본 클래스
public abstract class EmotionFaceController : MonoBehaviour
{
    public abstract void ShowEmotion(string emotion);
    
    public abstract void NextAnimation();
}