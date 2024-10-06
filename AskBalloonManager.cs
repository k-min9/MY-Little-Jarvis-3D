using System.Collections;
using UnityEngine;
using TMPro;

public class AskBalloonManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; // askBalloon 이미지
    [SerializeField] private GameObject askBalloon; // askBalloon 이미지
    [SerializeField] private TextMeshProUGUI askText; // askBalloon이 하위의 TMP 텍스트
    [SerializeField] public RectTransform characterTransform; // askBalloon이 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform askBalloonTransform; // askBalloon의 Transform
    public TextMeshProUGUI askBalloonText; // AnswerBalloon Text의 Transform

    // 싱글톤 인스턴스
    private static AskBalloonManager instance;

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


    // 싱글톤 인스턴스에 접근하는 속성
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

    // askBalloon의 텍스트를 수정 (text 하드코딩)
    public void ModifyAnswerBalloonText()
    {
        string answerLanguage = SettingManager.Instance.settings.ui_language; // 표시 언어 초기화[ko, en, jp]
        if (answerLanguage == "ko") {
            askText.text = "AI 서버를 기동하시겠습니까?";
        } else if (answerLanguage == "jp") {
            askText.text = "AIサーバーを起動しますか？"; // 텍스트 변경
        } else {
            askText.text = "Do you want to start the AI server?"; // 텍스트 변경
        }
        
        // 높이 조정
        float textHeight = askBalloonText.preferredHeight;
        askBalloonTransform.sizeDelta = new Vector2(askBalloonTransform.sizeDelta.x, textHeight + 120);       
    }

    // askBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateAskBalloonPosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // 캐릭터의 X 위치와 동일하게 설정
        askBalloonTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 270 * SettingManager.Instance.settings.char_size / 100f); // Y축 창크기 270만큼
    }
}
