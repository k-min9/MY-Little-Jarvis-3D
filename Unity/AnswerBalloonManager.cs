using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AnswerBalloonManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; // AnswerBalloon 이미지
    [SerializeField] private GameObject answerBalloon; // AnswerBalloon 이미지
    [SerializeField] private TextMeshProUGUI answerText; // AnswerBalloon 하위의 TMP 텍스트
    [SerializeField] public RectTransform characterTransform; // AnswerBalloon이 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform answerBalloonTransform; // AnswerBalloon의 Transform
    public TextMeshProUGUI answerBalloonText; // AnswerBalloon Text의 Transform

    // Image-Sprite
    [SerializeField] private Image answerBalloonImage;
    [SerializeField] private Sprite lightSprite;
    [SerializeField] private Sprite normalSprite;
    public bool isAnswered = false;  // 타 시스템이 해당 balloon 지워도 되는지 체크에 활용

    [SerializeField] private GameObject webImage;  // 답변에 web검색 활용했는지 여부를 보여주는 이미지

    private float hideTimer = 0f; // 타이머 변수 추가

    private string textKo = "";
    private string textJp = "";
    private string textEn = "";
    private string answerLanguage = "ko";

    // 싱글톤 인스턴스
    private static AnswerBalloonManager instance;

    void Start()
    {
        _canvas = FindObjectOfType<Canvas>();  // 최상위 Canvas

        // UI 최초 비활성화
        webImage.SetActive(false);
    }

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

        HideAnswerBalloon(); // 시작 시 AnswerBalloon 숨기기
    }

    // 상태 갱신 로직
    private void Update()
    {
        // 타이머 갱신
        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
        }

        // 타이머가 완료되면 AnswerBalloon 숨기기
        if (hideTimer <= 0f && StatusManager.Instance.IsAnswering)
        {
            HideAnswerBalloon();
        }

        if (StatusManager.Instance.IsAnswering)
        {
            UpdateAnswerBalloonPosition();
        }

        if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsListening || StatusManager.Instance.IsAsking)
        {
            HideAnswerBalloon();
        }
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

    // AnswerBalloon을 타이머 무제한으로 보이기
    public void ShowAnswerBalloonInf()
    {
        hideTimer = 99999f;
        answerBalloon.SetActive(true);
        answerText.text = string.Empty; // 텍스트 초기화
        StatusManager.Instance.IsAnswering = true; // StatusManager 상태 업데이트
        UpdateAnswerBalloonPosition();  // AnswerBalloon 위치 조정하
    }

    // 대답중 sprite
    public void ChangeAnswerBalloonSpriteLight()
    {
        answerBalloonImage.sprite = lightSprite;
        isAnswered = false;
    }

    // 대답 완료 sprite
    public void ChangeAnswerBalloonSpriteNormal()
    {
        answerBalloonImage.sprite = normalSprite;
        isAnswered = true;
    }

    // AnswerBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowAnswerBalloon()
    {
        answerBalloon.SetActive(true);
        answerText.text = string.Empty; // 텍스트 초기화
        StatusManager.Instance.IsAnswering = true; // StatusManager 상태 업데이트
        UpdateAnswerBalloonPosition();  // AnswerBalloon 위치 조정하
    }

    // AnswerBalloon의 텍스트를 수정
    public void ModifyAnswerBalloonText()
    {
        if (answerLanguage == "ko")
        {
            answerText.text = textKo; // 텍스트 변경
        }
        else if (answerLanguage == "jp")
        {
            answerText.text = textJp; // 텍스트 변경
        }
        else
        {
            answerText.text = textEn; // 텍스트 변경
        }

        // 높이 조정
        float textHeight = answerBalloonText.preferredHeight;
        answerBalloonTransform.sizeDelta = new Vector2(answerBalloonTransform.sizeDelta.x, textHeight + 120);
    }

    // 언어전환을 고려한 string setting
    public void ModifyAnswerBalloonTextInfo(string replyKo, string replyJp, string replyEn)
    {
        // Debug.Log("ModifyAnswerBalloonTextInfo Start : " + replyEn);
        answerLanguage = SettingManager.Instance.settings.ui_language; // 표시 언어 초기화[ko, en, jp]
        textKo = replyKo;
        textJp = replyJp;
        textEn = replyEn;
    }

    // 답변풍선 언어 변경
    public void changeAnswerLanguage()
    {
        if (answerLanguage == "ko")
        {
            answerLanguage = "jp";
        }
        else if (answerLanguage == "jp")
        {
            answerLanguage = "en";
        }
        else
        {
            answerLanguage = "ko";
        }
        // 바뀐 언어로 AnswerBalloon 다시 세팅
        ModifyAnswerBalloonText();
    }

    // 최근대화 삭제후 창 종료하기
    public void DeleteRecentDialogue()
    {
        MemoryManager.Instance.DeleteRecentDialogue();
        MemoryManager.Instance.DeleteRecentDialogue();

        HideAnswerBalloon();
    }

    // 대화 재생성
    public void ChatRegenerate()
    {
        // 기존 음성 중지 및 초기화
        VoiceManager.Instance.ResetAudio();

        string input = APIManager.Instance.query_origin;
        GameManager.Instance.chatIdx += 1;
        GameManager.Instance.chatIdxRegenerateCount += 1;
        Debug.Log("Regenerate 텍스트 (" + GameManager.Instance.chatIdx.ToString() + ") : " + input);
        APIManager.Instance.CallConversationStream(input, GameManager.Instance.chatIdx.ToString());

        // 이미 대화 저장했을 경우 삭제
        if (isAnswered)
        {
            DeleteRecentDialogue();
        }
        HideAnswerBalloon();
    }

    // 채팅로그 창 열기
    public void ShowChatHistory()
    {
        UIManager.Instance.ShowChatHistory();
    }

    // 현재(마지막) 오디오 재생 후 AnswerBalloon을 숨기는 코루틴 호출
    public void HideAnswerBalloonAfterAudio()
    {
        AudioClip clip = VoiceManager.Instance.GetAudioClip();

        if (clip != null)
        {
            hideTimer = clip.length + 0.5f; // 타이머를 오디오 재생 시간 + 0.5초로 설정
        }
    }

    // AnswerBalloon을 숨기는 함수
    public void HideAnswerBalloon()
    {
        hideTimer = 0f;  // inf용 초기화
        answerBalloon.SetActive(false);
        StatusManager.Instance.IsAnswering = false;
    }

    // AnswerBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateAnswerBalloonPosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;

        // 캐릭터의 X 위치와 동일하게 설정
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        float leftBound = -canvasRect.rect.width / 2; // 캔버스 왼쪽 끝
        float rightBound = canvasRect.rect.width / 2; // 캔버스 오른쪽 끝
        float charPositionX = Mathf.Clamp(charPosition.x, leftBound + 250, rightBound - 250);

        // answerBalloonTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 270 * SettingManager.Instance.settings.char_size / 100f); // Y축 창크기 270만큼
        answerBalloonTransform.anchoredPosition = new Vector2(charPositionX, charPosition.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
    }

    public void ShowWebImage()
    { 
        webImage.SetActive(true);
    }
    
    public void HideWebImage()
    {
        webImage.SetActive(false);
    }
}
