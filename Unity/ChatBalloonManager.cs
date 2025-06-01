using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatBalloonManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; // chatBalloon 이미지
    [SerializeField] private GameObject chatBalloon; // chatBalloon 이미지
    [SerializeField] public RectTransform characterTransform; // chatBalloon이 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform chatBalloonTransform; // chatBalloon의 Transform
    [SerializeField] public TMP_InputField inputField; 
    [SerializeField] private Toggle imageUseToggle;

    // 싱글톤 인스턴스
    private static ChatBalloonManager instance;

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

        HideChatBalloon(); // 시작 시 chatBalloon 숨기기
    }

    // 상태 갱신 로직
    private void Update()
    {
        if (StatusManager.Instance.IsChatting && chatBalloon.activeSelf)  // activeSelf : 활성화 여부
        {
            UpdateChatBalloonPosition();
        }

        if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsAnswering)
        {
            HideChatBalloon();
        }
    }


    // 싱글톤 인스턴스에 접근하는 속성
    public static ChatBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChatBalloonManager>();
            }
            return instance;
        }
    }

    // chatBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowChatBalloon()
    {
        // 기존의 answerballoon이 있을경우 Hide
        if (AnswerBalloonManager.Instance.isAnswered) AnswerBalloonManager.Instance.HideAnswerBalloon();
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

        chatBalloon.SetActive(true);
        inputField.text = string.Empty; // 텍스트 초기화
        StatusManager.Instance.IsChatting = true; // StatusManager 상태 업데이트
        UpdateChatBalloonPosition();  // chatBalloon 위치 조정하기

        // InputField 포커스 설정
        inputField.Select();
        inputField.ActivateInputField();

        // 기존 애니메이션 중지 및 isChatting 애니메이션 
        AnimationManager.Instance.Listen();
    }
    
    // chatBalloon을 숨기는 함수
    public void HideChatBalloon()
    {
        chatBalloon.SetActive(false);

        if (StatusManager.Instance.IsChatting) {
            StatusManager.Instance.IsChatting = false; 
            AnimationManager.Instance.ListenDisable();
        }
//         // 안드로이드 테스트용
// #if UNITY_ANDROID && !UNITY_EDITOR
//         chatBalloon.SetActive(true);
//         StatusManager.Instance.IsChatting = true; 
// #endif
    }

    public void ToggleChatBalloon()
    {
        if (chatBalloon.activeSelf)
        {
            HideChatBalloon();
        }
        else 
        {
            ShowChatBalloon();
        }
    }
    
    public bool GetImageUse()
    {
        if (imageUseToggle.isOn)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // chatBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateChatBalloonPosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;

        // 캐릭터의 X 위치와 동일하게 설정
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        float leftBound = -canvasRect.rect.width / 2; // 캔버스 왼쪽 끝
        float rightBound = canvasRect.rect.width / 2; // 캔버스 오른쪽 끝
        float charPositionX = Mathf.Clamp(charPosition.x, leftBound + 250, rightBound - 250);

        // chatBalloonTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 270 * SettingManager.Instance.settings.char_size / 100f); // Y축 창크기 270만큼
        chatBalloonTransform.anchoredPosition = new Vector2(charPositionX, charPosition.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
    }
}
