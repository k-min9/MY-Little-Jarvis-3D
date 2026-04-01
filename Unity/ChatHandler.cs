using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;

public class ChatHandler : MonoBehaviour
{
    [SerializeField] public TMP_InputField inputField; // TMP_InputField를 참조할 수 있도록 SerializeField로 선언
    public SubChatBalloonController subController; // 서브 캐릭터의 ChatBalloon 팝업인 경우 할당됨

    private void Start()
    {
        // 입력 필드에 대한 엔터키 이벤트 리스너 추가
        inputField.onEndEdit.AddListener(HandleInputSubmit);

        // 복제된 서브용 말풍선의 경우, Scene 인스턴스 간 복사로 인해 UnityEvent가 
        // 원본 객체를 가리키는 현상을 방지하기 위해 동적으로 로컬 리스너를 덮어씌웁니다.
        if (subController != null)
        {
            BindButton("SendBtn", HandleInputSubmitButton);
            BindButton("WebSearchBtn", HandleInputWebSubmitButton);
            BindButton("JobBtn", HandleVlPlannerRunButton);
            
            BindButton("Button_Close", () => {
                subController.HideChatBalloon();
            });
        }
    }

    private void BindButton(string btnName, UnityEngine.Events.UnityAction action)
    {
        Transform btnTransform = transform.Find(btnName);
        if (btnTransform != null)
        {
            UnityEngine.UI.Button btn = btnTransform.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                // 기존(원본을 가리키는) 리스너 제거 후 현재 인스턴스의 메서드로 교체
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }
    }

    // 입력 제출 처리 함수
    private void HandleInputSubmit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return)) // 엔터키가 눌렸는지 확인
        {
            GameManager.Instance.chatIdx += 1;
            GameManager.Instance.chatIdxRegenerateCount = 0;
            Debug.Log("입력된 텍스트 ("+GameManager.Instance.chatIdx.ToString()+") : " + input);

            // 게임 모드 체크하여 적절한 API 호출
            if (MiniGame20QManager.Instance != null && MiniGame20QManager.Instance.Is20QMode())
            {
                // 스무고개 게임 모드
                MiniGame20QManager.Instance.SendQuestion(input);
            }
            else if (APIAroPlaManager.ShouldUseAroplaManager())
            // else if (ApiMultiConversationManager.IsActive())
            {
                // 다중 캐릭터 대화 모드 (아로프라 채널 등)
                APIAroPlaManager.Instance.SendUserMessage(input);
                // ApiMultiConversationManager.Instance.SendUserMessage(input);
            }
            else
            {
                // 기존 1:1 대화 모드
                APIManager.Instance.CallConversationStream(input, GameManager.Instance.chatIdx.ToString());
            }

            // 말풍선 없애기
            if (subController != null) subController.HideChatBalloon();
            else ChatBalloonManager.Instance.HideChatBalloon();
        }
    }

    // 버튼용 입력 제출 처리 테스트
    public void HandleInputSubmitButton()
    {
        string input = inputField.text;
        GameManager.Instance.chatIdx += 1;
        GameManager.Instance.chatIdxRegenerateCount = 0;
        Debug.Log("입력된 텍스트 (" + GameManager.Instance.chatIdx.ToString() + ") : " + input);

        // 게임 모드 체크하여 적절한 API 호출
        if (MiniGame20QManager.Instance != null && MiniGame20QManager.Instance.Is20QMode())
        {
            // 스무고개 게임 모드
            MiniGame20QManager.Instance.SendQuestion(input);
        }
        else if (APIAroPlaManager.ShouldUseAroplaManager())
        // else if (ApiMultiConversationManager.IsActive())
        {
            // 다중 캐릭터 대화 모드 (아로프라 채널 등)
            APIAroPlaManager.Instance.SendUserMessage(input);
            // ApiMultiConversationManager.Instance.SendUserMessage(input);
        }
        else
        {
            // 기존 1:1 대화 모드
            APIManager.Instance.CallConversationStream(input, GameManager.Instance.chatIdx.ToString());
        }

        // 말풍선 없애기
        if (subController != null) subController.HideChatBalloon();
        else ChatBalloonManager.Instance.HideChatBalloon();
    }

    // 버튼용 입력 제출 처리 테스트 - Web 강제(1회)
    public void HandleInputWebSubmitButton()
    {
        string input = inputField.text;
        GameManager.Instance.chatIdx += 1;
        GameManager.Instance.chatIdxRegenerateCount = 0;
        Debug.Log("입력된 텍스트-web (" + GameManager.Instance.chatIdx.ToString() + ") : " + input);

        // 게임 모드 체크하여 적절한 API 호출
        if (MiniGame20QManager.Instance != null && MiniGame20QManager.Instance.Is20QMode())
        {
            // 스무고개 게임 모드에서는 웹 검색 강제 기능 미지원 (일반 게임 모드로 처리)
            Debug.LogWarning("스무고개 게임에서는 웹 검색 강제 기능이 지원되지 않습니다. 일반 질문으로 처리됩니다.");
            MiniGame20QManager.Instance.SendQuestion(input);
        }
        else if (APIAroPlaManager.ShouldUseAroplaManager())
        // else if (ApiMultiConversationManager.IsActive())
        {
            // 다중 캐릭터 대화 모드에서는 웹 검색 강제 기능 미지원 (일반 모드로 처리)
            Debug.LogWarning("다중 캐릭터 대화에서는 웹 검색 강제 기능이 지원되지 않습니다. 일반 모드로 처리됩니다.");
            APIAroPlaManager.Instance.SendUserMessage(input);
            // ApiMultiConversationManager.Instance.SendUserMessage(input);
        }
        else
        {
            // 기존 1:1 대화 모드 - 웹 검색 강제
            GameManager.Instance.isWebSearchForced = true;  // Web강제 1회
            APIManager.Instance.CallConversationStream(input, GameManager.Instance.chatIdx.ToString());
        }
        
        // 말풍선 없애기
        if (subController != null) subController.HideChatBalloon();
        else ChatBalloonManager.Instance.HideChatBalloon();
    }

    // 버튼용 VL Planner Run 실행
    public void HandleVlPlannerRunButton()
    {
        // 입력 텍스트 확인
        string input = inputField.text;
        if (string.IsNullOrWhiteSpace(input))
        {
            Debug.LogWarning("[VlPlannerRun] 입력 텍스트가 비어있습니다.");
            return;
        }

        Debug.Log($"[VlPlannerRun] 버튼 실행 - query: {input}");

        // VL Planner 실행
        ApiVlPlannerManager.Instance.ExecuteVlPlannerRun(
            query: input,
            onEvent: (eventData) =>
            {
                string kind = (string)eventData["kind"] ?? "unknown";  // 이벤트 종류
                string message = (string)eventData["message"] ?? "";  // 이벤트 메시지
                Debug.Log($"[ChatHandler] Event: kind={kind}, message={message}");
                
                // data 필드가 있으면 출력
                var data = eventData["data"];
                if (data != null)
                {
                    Debug.Log($"[ChatHandler] Event data: {data.ToString()}");
                }
            },
            onComplete: (success, errorMsg) =>
            {
                if (success)
                {
                    Debug.Log("[ChatHandler] VL Planner 실행 완료");
                }
                else
                {
                    Debug.LogWarning($"[ChatHandler] VL Planner 실패: {errorMsg}");
                }
            }
        );

        // 말풍선 없애기
        if (subController != null) subController.HideChatBalloon();
        else ChatBalloonManager.Instance.HideChatBalloon();
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