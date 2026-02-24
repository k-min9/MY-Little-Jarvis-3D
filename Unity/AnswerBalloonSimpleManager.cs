using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class AnswerBalloonSimpleManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static AnswerBalloonSimpleManager instance;
    public static AnswerBalloonSimpleManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AnswerBalloonSimpleManager>();
            }
            return instance;
        }
    }

    [SerializeField] private Canvas _canvas; // AnswerBalloonSimple 이미지
    [SerializeField] private GameObject answerBalloonSimple; // AnswerBalloonSimple 이미지
    [SerializeField] private TextMeshProUGUI answerText; // AnswerBalloonSimple 하위의 TMP 텍스트
    [SerializeField] public RectTransform characterTransform; // AnswerBalloonSimple이 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform answerBalloonSimpleTransform; // AnswerBalloonSimple의 Transform
    public TextMeshProUGUI answerBalloonSimpleText; // AnswerBalloonSimple Text의 Transform

    private float hideTimer = 0f; // 타이머 변수 추가

    private string textKo = "";
    private string textJp = "";
    private string textEn = "";

    private void Awake()
    {
        HideAnswerBalloonSimple(); // 시작 시 AnswerBalloonSimple 숨기기
    }

    // 상태 갱신 로직
    private void Update()
    {
        // 타이머 갱신
        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
        }

        // 타이머가 완료되면 AnswerBalloonSimple 숨기기
        if (hideTimer <= 0f && StatusManager.Instance.IsAnsweringSimple)
        {
            HideAnswerBalloonSimple();
        }

        if (StatusManager.Instance.IsAnsweringSimple)
        {
            UpdateAnswerBalloonSimplePosition();
        }

        if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsListening || StatusManager.Instance.IsAsking )
        {
            HideAnswerBalloonSimple();
        }
    }

    // AnswerBalloonSimple을 타이머 무제한으로 보이기
    public void ShowAnswerBalloonSimpleInf()
    {
        // 기존의 balloon이 있을경우 Hide
        if (AnswerBalloonManager.Instance.isAnswered) AnswerBalloonManager.Instance.HideAnswerBalloon();
        ChatBalloonManager.Instance.HideChatBalloon();

        // Operator 모드일 경우 PortraitBalloonSimpleManager로 라우팅
        if (ChatModeManager.Instance.IsOperatorMode())
        {
            PortraitBalloonSimpleManager.Instance.ShowInf();
            return;
        }

        hideTimer = 99999f;
        answerBalloonSimple.SetActive(true);
        answerText.text = string.Empty; // 텍스트 초기화
        StatusManager.Instance.IsAnsweringSimple = true; // StatusManager 상태 업데이트
        UpdateAnswerBalloonSimplePosition();  // AnswerBalloonSimple 위치 조정하
    }

    // AnswerBalloonSimple을 보이고 텍스트를 초기화하는 함수
    public void ShowAnswerBalloonSimple()
    {
        // 기존의 balloon이 있을경우 Hide
        if (AnswerBalloonManager.Instance.isAnswered) AnswerBalloonManager.Instance.HideAnswerBalloon();
        ChatBalloonManager.Instance.HideChatBalloon();

        // Operator 모드일 경우 PortraitBalloonSimpleManager로 라우팅
        if (ChatModeManager.Instance.IsOperatorMode())
        {
            PortraitBalloonSimpleManager.Instance.Show();
            return;
        }

        answerBalloonSimple.SetActive(true);
        answerText.text = string.Empty; // 텍스트 초기화
        StatusManager.Instance.IsAnsweringSimple = true; // StatusManager 상태 업데이트
        UpdateAnswerBalloonSimplePosition();  // AnswerBalloonSimple 위치 조정하
    }

    // AnswerBalloonSimple의 텍스트를 수정하고 오디오를 재생하는 함수
    public void ModifyAnswerBalloonSimpleText(string text)
    {
        // 자동번역 시도
        text = LanguageManager.Instance.Translate(text);

        // Operator 모드일 경우 PortraitBalloonSimpleManager로 라우팅
        if (ChatModeManager.Instance.IsOperatorMode())
        {
            PortraitBalloonSimpleManager.Instance.ModifyText(text);
            return;
        }

        answerText.text = text; // 텍스트 변경

        // 높이 조정
        float textHeight = answerBalloonSimpleText.preferredHeight;
        answerBalloonSimpleTransform.sizeDelta = new Vector2(answerBalloonSimpleTransform.sizeDelta.x, textHeight + 60);

        
    }

    // 언어전환을 고려한 string setting
    public void ModifyAnswerBalloonSimpleTextInfo(string replyKo, string replyJp, string replyEn) 
    {
        textKo = replyKo;
        textJp = replyJp;
        textEn = replyEn;
    }
    
    // 현재(마지막) 오디오 재생 후 AnswerBalloonSimple을 숨기는 코루틴 호출
    public void HideAnswerBalloonSimpleAfterAudio()
    {
        // Operator 모드일 경우 PortraitBalloonSimpleManager로 라우팅
        if (ChatModeManager.Instance.IsOperatorMode())
        {
            PortraitBalloonSimpleManager.Instance.HideAfterAudio();
            return;
        }

        AudioClip clip = VoiceManager.Instance.GetAudioClip();

        if (clip != null)
        {
            hideTimer = clip.length + 0.5f; // 타이머를 오디오 재생 시간 + 0.5초로 설정
        }
    }

    // AnswerBalloonSimple을 숨기는 함수
    public void HideAnswerBalloonSimple()
    {
        // Operator 모드일 경우 PortraitBalloonSimpleManager로 라우팅
        if (ChatModeManager.Instance.IsOperatorMode())
        {
            PortraitBalloonSimpleManager.Instance.Hide();
            StatusManager.Instance.IsAnsweringSimple = false;
            return;
        }

        hideTimer = 0f;  // inf용 초기화
        answerBalloonSimple.SetActive(false);
        StatusManager.Instance.IsAnsweringSimple = false; 
    }

    // AnswerBalloonSimple의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateAnswerBalloonSimplePosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // 캐릭터의 X 위치와 동일하게 설정
        answerBalloonSimpleTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
    }
}
