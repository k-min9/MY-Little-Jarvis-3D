using UnityEngine;
using UnityEngine.UI;

// 안내 Manager
public class NoticeManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static NoticeManager instance;

    // 싱글톤 인스턴스 접근 속성
    public static NoticeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NoticeManager>();
            }
            return instance;
        }
    }

    // 안내 말풍선 
    public GameObject noticeEmotionBalloonInstance;

    // 안내 행동
    public void Notice(string notice)
    {
        if (notice == "thinking") // "생각 중" 상태
        {
            // 생각 중 말풍선 방식
            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Thinking...");

            // 기존 안내풍선(감정) 삭제 후 재생성
            DeleteNoticeBalloonInstance();
            noticeEmotionBalloonInstance = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Time");
        }
        else if (notice == "webSearch") // "생각 중" 상태
        {
            // 생각 중 말풍선 방식
            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Searching Web...");

            // 기존 안내풍선(감정) 삭제 후 재생성
            DeleteNoticeBalloonInstance();
            noticeEmotionBalloonInstance = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Search");
        }
    }
    
    
    // 안내 말풍선용 get set delete
    public GameObject GetNoticeBalloonInstance()
    {
        return noticeEmotionBalloonInstance;
    }

    public void SetNoticeBalloonInstance(GameObject newInstance)
    {
        noticeEmotionBalloonInstance = newInstance;
    }

    public void DeleteNoticeBalloonInstance()
    {
        if (noticeEmotionBalloonInstance != null)
        {
            Destroy(noticeEmotionBalloonInstance);
            noticeEmotionBalloonInstance = null;
        }
    }

    // 통합 EmotionBalloon 표시 (기존 것 자동 삭제 후 생성)
    public GameObject ShowNoticeEmotionBalloon(string spriteName, float duration = 60f)
    {
        // 기존 것 파괴
        DeleteNoticeBalloonInstance();
        
        // 새로 생성
        noticeEmotionBalloonInstance = EmotionBalloonManager.Instance.ShowEmotionBalloon(
            CharManager.Instance.GetCurrentCharacter(), 
            spriteName, 
            duration
        );
        
        return noticeEmotionBalloonInstance;
    }
}
