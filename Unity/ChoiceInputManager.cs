using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChoiceInputManager : MonoBehaviour
{
    public static ChoiceInputManager instance;
    public static ChoiceInputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChoiceInputManager>();
            }
            return instance;
        }
    }

    [Header("UI Components")]
    public Text labelText;                      // Label 텍스트 (chatgpt, gemini, openrouter 등)
    public TMP_InputField apiKeyInputField;     // InputField 오브젝트
    public Text keyChoiceInputTestResultText;   // Testing... Success Fail
    public TMP_InputField serverGeminiApiKeyInputField;
    public TMP_InputField serverOpenRouterApiKeyInputField;

    private int currentApiTypeIdx = -1;

    private void Start()
    {
        // 초기 비활성화는 UIManager.Awake()에서 처리
    }

    // 입력 패널을 보여주고 라벨을 지정 - ChatGPT, Gemini, OpenRouter
    public void ShowInput(string apiType)
    {
        UIManager.Instance.ShowChoiceInput();
        keyChoiceInputTestResultText.text = "";

        currentApiTypeIdx = GetIndexForApiType(apiType);

        // 라벨 변경
        string label = GetLabelForApiType(apiType);
        labelText.text = label;

        // 이전 입력값 초기화
        Debug.Log("KEY " + apiType + ":" + GetApiKeyForApiType(apiType));
        apiKeyInputField.text = GetApiKeyForApiType(apiType);

        Debug.Log($"[ChoiceInputManager] ShowInput - {label}");
    }

    // InputField 변할때마다 값 갱신
    public void SetApiKey(string value)
    {
        // 양쪽 끝 공백/개행 제거
        string trimmed = value.Trim();
        Debug.Log($"[ChoiceInputManager] SetApiKey idx={currentApiTypeIdx} value='{trimmed}'");

        // 0: Gemini, 1: OpenRouter, 2: ChatGPT
        // serverXxxApiKeyInputField.text 세팅 → 해당 필드의 OnValueChanged → SetAPIKeyGemini/SetAPIKeyOpenRouter 자동 호출
        if (currentApiTypeIdx == 0)
        {
            serverGeminiApiKeyInputField.text = trimmed;
        }
        else if (currentApiTypeIdx == 1)
        {
            serverOpenRouterApiKeyInputField.text = trimmed;
        }
    }

    // 입력 패널을 숨깁니다.
    public void HideInput()
    {
        UIManager.Instance.HideChoiceInput();
    }

    // 종료 버튼으로 전 메뉴로 돌아가기
    public void ExitChoiceInput()
    {
        HideInput();
        StartCoroutine(ScenarioTutorialManager.Instance.Scenario_A04_2_APIKeyInput());  // 선택지로
    }

    // OK 버튼으로 입력 테스트
    public void CallConnectTest()
    {
        HideInput();
        StartCoroutine(ScenarioTutorialManager.Instance.Scenario_A97_ConnectTest(currentApiTypeIdx));  // 선택한 대상 index 전달
    }

    // Test 버튼으로 API 테스트
    public void TestApiKey()
    {
        if (currentApiTypeIdx == 0) // Gemini
        {
            SettingManager.Instance.settings.server_type_idx = 0;
            ServerManager.Instance.CallValidateGeminiAPIKey();
        }
        else if (currentApiTypeIdx == 1) // OpenRouter
        {
            ServerManager.Instance.CallValidateOpenRouterAPIKey();
        }
    }

    // 현재 입력된 API Key를 가져옵니다.
    public string GetApiKey()
    {
        return apiKeyInputField.text;
    }

    // API 타입에 따라 라벨을 반환합니다.
    private string GetLabelForApiType(string apiType)
    {
        switch (apiType.ToLower())
        {
            case "chatgpt":
                return "ChatGPT API Key";
            case "gemini":
                return "Gemini API Key";
            case "openrouter":
                return "OpenRouter API Key";
            default:
                return "Gemini API Key";
        }
    }

    // API 타입 문자열을 index로 변환합니다.
    // 0: Gemini, 1: OpenRouter, 2: ChatGPT
    private int GetIndexForApiType(string apiType)
    {
        switch (apiType.ToLower())
        {
            case "gemini":
                SettingManager.Instance.settings.server_type_idx = 2;
                SettingManager.Instance.SetServerTypeByValue(2);
                return 0;
            case "openrouter":
                SettingManager.Instance.settings.server_type_idx = 3;
                SettingManager.Instance.SetServerTypeByValue(3);
                return 1;
            case "chatgpt":
                // TODO : ChatGPT 추가시
                return 2;
            default:
                SettingManager.Instance.settings.server_type_idx = 2;
                SettingManager.Instance.SetServerTypeByValue(2);
                return 0; // 기본은 Gemini
        }
    }

    // API 타입에 따라 맞는 key를 반환합니다.
    private string GetApiKeyForApiType(string apiType)
    {
        switch (apiType.ToLower())
        {
            case "chatgpt":
                return SettingManager.Instance.settings.api_key_gemini;  // TODO : 차후 chatGPT 추가 확장 고려
            case "gemini":
                return SettingManager.Instance.settings.api_key_gemini;
            case "openrouter":
                return SettingManager.Instance.settings.api_key_openRouter;
            default:
                return SettingManager.Instance.settings.api_key_gemini;
        }
    }
}
