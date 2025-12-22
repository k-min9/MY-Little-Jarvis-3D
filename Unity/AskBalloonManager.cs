using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AskBalloonManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static AskBalloonManager instance;
    public static AskBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AskBalloonManager>();
            }
            return instance;
        }
    }

    [SerializeField] private Canvas _canvas; // askBalloon 이미지
    [SerializeField] private GameObject askBalloon; // askBalloon 이미지
    [SerializeField] private TextMeshProUGUI askText; // askBalloon이 하위의 TMP 텍스트
    [SerializeField] public RectTransform characterTransform; // askBalloon이 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform askBalloonTransform; // askBalloon의 Transform
    public TextMeshProUGUI askBalloonText; // AnswerBalloon Text의 Transform

    // 질문 관련 데이터
    private string currentQuestion = ""; // 현재 묻는 질문
    public struct QuestionInfo
    {
        public string questionKo;  // 한국어 질문
        public string questionJp;  // 일본어 질문
        public string questionEn;  // 영어 질문
    }
    private Dictionary<string, QuestionInfo> questionDict = new Dictionary<string, QuestionInfo>(); // 질문 사전

    public void SetCurrentQuestion(string questionKey) {currentQuestion = questionKey;}

    // 질문 데이터 초기화-등록
    private void InitializeQuestions()
    {
        questionDict.Add("install_ai_server", new QuestionInfo
        {
            questionKo = "AI 서버 프로그램을 설치하겠습니까?",
            questionJp = "AIサーバープログラムをインストールしますか？",
            questionEn = "Do you want to install the AI Server program?"
        });
        questionDict.Add("start_ai_server", new QuestionInfo
        {
            questionKo = "AI 서버를 기동하시겠습니까?",
            questionJp = "AIサーバーを起動しますか？",
            questionEn = "Do you want to start the AI server?"
        });
        questionDict.Add("shutdown_ai_server", new QuestionInfo
        {
            questionKo = "AI 서버를 종료하시겠습니까?",
            questionJp = "AIサーバーを終了しますか？",
            questionEn = "Do you want to shut down the AI server?"
        });
        questionDict.Add("start_dev_server", new QuestionInfo
        {
            questionKo = "선생님? 현재 연결할 수 없는 서버가 없어요. 개발자 서버를 한 번 연결해볼까요?",
            questionJp = "先生、現在接続できるサーバーがありません。 開発者サーバーを試してみましょうか？",
            questionEn = "Sensei? There are currently no servers available to connect to, shall I try the developer server?"
        });
    }

    private void Awake()
    {
        InitializeQuestions();  // question 정보 등록
        HideAskBalloon(); // 시작 시 askBalloon 숨기기
    }

    // 상태 갱신 로직
    private void Update()
    {
        if (StatusManager.Instance.IsAsking && askBalloon.activeSelf)  // activeSelf : 활성화 여부
        {
            UpdateAskBalloonPosition();
        }

        if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsAnswering)
        {
            HideAskBalloon();
        }
    }

    // askBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowAskBalloon()
    {
        askBalloon.SetActive(true);
        StatusManager.Instance.IsAsking = true; // StatusManager 상태 업데이트
        UpdateAskBalloonPosition();  // askBalloon 위치 조정하기
        ModifyAnswerBalloonText();
    }
    
    // askBalloon을 숨기는 함수
    public void HideAskBalloon()
    {
        askBalloon.SetActive(false);
        StatusManager.Instance.IsAsking = false; 
    }

    // askBalloon의 텍스트를 수정
    public void ModifyAnswerBalloonText()
    {
        if (string.IsNullOrEmpty(currentQuestion) || !questionDict.ContainsKey(currentQuestion))
        {
            Debug.LogError("Invalid current question or missing question info.");
            return;
        }

        QuestionInfo questionInfo = questionDict[currentQuestion];
        string answerLanguage = SettingManager.Instance.settings.ui_language; // 표시 언어 초기화[ko, en, jp]

        // 언어에 따른 텍스트 설정
        if (answerLanguage == "ko")
        {
            askText.text = questionInfo.questionKo;
        }
        else if (answerLanguage == "jp")
        {
            askText.text = questionInfo.questionJp;
        }
        else
        {
            askText.text = questionInfo.questionEn;
        }

        // 높이 조정
        float textHeight = askBalloonText.preferredHeight;
        askBalloonTransform.sizeDelta = new Vector2(askBalloonTransform.sizeDelta.x, textHeight + 150);
    }

    // askBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateAskBalloonPosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // 캐릭터의 X 위치와 동일하게 설정
        // askBalloonTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 270 * SettingManager.Instance.settings.char_size / 100f); // Y축 창크기 270만큼
        askBalloonTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
    }

    // Yes 버튼 사용 > 과거의 서버 인스톨 후 시작 흔적
    public void AnswerYes()
    {
        if (currentQuestion == "install_ai_server") {
            // ServerManager.RunInstallExe();

            HideAskBalloon();  // 답변했으니 기존 풍선 숨기기
            return;
        }
        if (currentQuestion == "start_ai_server") {
            // ServerManager.StartServer();

            // 안내
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("AI Server Loading...");

            // TODO : ping체크로 안내문 자동 종료

            HideAskBalloon();  // 답변했으니 기존 풍선 숨기기
            return;
        }

        Debug.Log("No mapped function");
        HideAskBalloon();  // 추가질문없을 경우 그대로 종료

    }

    // No 버튼 사용
    public void AnswerNo()
    {
        HideAskBalloon();  // 추가질문없을 경우 그대로 종료
    }
}
