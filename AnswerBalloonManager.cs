using System.Collections;
using UnityEngine;
using TMPro;
/**
말풍선을 관리하는 Manager
*/
public class AnswerBalloonManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; // AnswerBalloon 이미지
    [SerializeField] private GameObject answerBalloon; // AnswerBalloon 이미지
    [SerializeField] private TextMeshProUGUI answerText; // AnswerBalloon 하위의 TMP 텍스트
    [SerializeField] private RectTransform characterTransform; // AnswerBalloon이 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform answerBalloonTransform; // AnswerBalloon의 Transform
    public TextMeshProUGUI answerBalloonText; // AnswerBalloon Text의 Transform
    private bool is_answering = false; // 전역 상태 변수

    // 싱글톤 인스턴스
    private static AnswerBalloonManager instance;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        HideAnswerBalloon(); // 시작 시 AnswerBalloon 숨기기
    }

    // 싱글톤 인스턴스에 접근하는 속성
    public static AnswerBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AnswerBalloonManager>();
            }
            return instance;
        }
    }

    // AnswerBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowAnswerBalloon()
    {
        answerBalloon.SetActive(true);
        answerText.text = string.Empty; // 텍스트 초기화
        is_answering = true;

        // AnswerBalloon 위치를 조정하는 함수 호출
        UpdateAnswerBalloonPosition();
    }

    // AnswerBalloon의 텍스트를 수정하고 오디오를 재생하는 함수
    public void ModifyAnswerBalloonText(string text)
    {
        answerText.text = text; // 텍스트 변경

        // 높이 조정
        float textHeight = answerBalloonText.preferredHeight;
        answerBalloonTransform.sizeDelta = new Vector2(answerBalloonTransform.sizeDelta.x, textHeight + 200);

        
    }
    
    // 현재(마지막) 오디오 재생 후 AnswerBalloon을 숨기는 코루틴 호출
    public void HideAnswerBalloonAfterAudio() {
        StartCoroutine(HideAnswerBalloonAfterAudioIEnum());
    }

    // 오디오 재생 후 일정 시간 뒤에 AnswerBalloon을 숨기는 함수
    private IEnumerator HideAnswerBalloonAfterAudioIEnum()
    {
        AudioClip clip = VoiceManager.Instance.GetAudioClip();

        if (clip != null)
        {
            yield return new WaitForSeconds(clip.length + 0.5f); // 오디오 재생 길이 + 0.5초 대기
        }

        HideAnswerBalloon();
    }

    // AnswerBalloon을 숨기는 함수
    public void HideAnswerBalloon()
    {
        answerBalloon.SetActive(false);
        is_answering = false;
    }

    // AnswerBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateAnswerBalloonPosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // float height = answerBalloonTransform.sizeDelta.y;  // anchor을 하단으로 설정하여 사용안함

        charPosition.y += 270; // + 0.5f*height;   // Y축 창크기 200만큼

        answerBalloonTransform.anchoredPosition = charPosition;

    }
}
