using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가

public class ChatHandler : MonoBehaviour
{
    [SerializeField] public TMP_InputField inputField; // TMP_InputField를 참조할 수 있도록 SerializeField로 선언

    private void Start()
    {
        // 입력 필드에 대한 엔터키 이벤트 리스너 추가
        inputField.onEndEdit.AddListener(HandleInputSubmit);
    }

    // 입력 제출 처리 함수
    private void HandleInputSubmit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return)) // 엔터키가 눌렸는지 확인
        {
            GameManager.Instance.chatIdx += 1;
            GameManager.Instance.chatIdxRegenerateCount = 0;
            Debug.Log("입력된 텍스트 ("+GameManager.Instance.chatIdx.ToString()+") : " + input);
            APIManager.Instance.CallConversationStream(input, GameManager.Instance.chatIdx.ToString());

            // 말풍선 없애기
            ChatBalloonManager.Instance.HideChatBalloon();
        }
    }

    // 버튼용 입력 제출 처리 테스트
    public void HandleInputSubmitButton()
    {
        string input = inputField.text;
        GameManager.Instance.chatIdx += 1;
        GameManager.Instance.chatIdxRegenerateCount = 0;
        Debug.Log("입력된 텍스트 (" + GameManager.Instance.chatIdx.ToString() + ") : " + input);
        APIManager.Instance.CallConversationStream(input, GameManager.Instance.chatIdx.ToString());

        // 말풍선 없애기
        ChatBalloonManager.Instance.HideChatBalloon();
    }

    // 버튼용 입력 제출 처리 테스트 - Web 강제(1회)
    public void HandleInputWebSubmitButton()
    {
        string input = inputField.text;
        GameManager.Instance.chatIdx += 1;
        GameManager.Instance.chatIdxRegenerateCount = 0;
        Debug.Log("입력된 텍스트-web (" + GameManager.Instance.chatIdx.ToString() + ") : " + input);
        GameManager.Instance.isWebSearchForced = true;  // Web강제 1회
        APIManager.Instance.CallConversationStream(input, GameManager.Instance.chatIdx.ToString());
        
        // 말풍선 없애기
        ChatBalloonManager.Instance.HideChatBalloon();
    }

    // 입력에 따라 수행할 작업을 정의하는 함수
    private void PerformActionBasedOnInput(string input)
    {
        // 예: 특정 명령어에 따라 행동 수행
        if (input == "Hello")
        {
            Debug.Log("Hello 명령어가 입력되었습니다!");
            // 특정 행동 수행
        }
        else
        {
            Debug.Log("알 수 없는 명령어: " + input);
        }
    }
}