using UnityEngine;

// 감정 표현을 위한 기본 클래스
public abstract class EmotionFaceController : MonoBehaviour
{
    public abstract void ShowEmotion(string emotion);

    public abstract void ShowEmotionFromEmotion(string emotion);     // joy, anger, confusion, sadness, surprise, neutral을 각각 표정변환

    public abstract void ShowEmotionFromAction(string action);     // listen등의 행동시 표정 변환
        
    public abstract void NextAnimation();
}