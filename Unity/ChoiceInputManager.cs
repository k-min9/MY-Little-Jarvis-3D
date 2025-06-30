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
    public GameObject inputPanel;           // Panel 오브젝트
    public Text labelText;                  // Label 텍스트 (chatgpt, gemini, openrouter 등)
    public TMP_InputField apiKeyInputField; // InputField 오브젝트

    private void Start()
    {
        inputPanel.SetActive(false);
    }

    // 입력 패널을 보여주고 라벨을 지정 - ChatGPT, Gemini, OpenRouter
    public void ShowInput(string apiType)
    {
        inputPanel.SetActive(true);

        // 라벨 변경
        string label = GetLabelForApiType(apiType);
        labelText.text = label;

        // 이전 입력값 초기화
        apiKeyInputField.text = "";

        Debug.Log($"[ChoiceInputManager] ShowInput - {label}");
    }

    // 입력 패널을 숨깁니다.
    public void HideInput()
    {
        inputPanel.SetActive(false);
    }

    /// <summary>
    /// 현재 입력된 API Key를 가져옵니다.
    /// </summary>
    public string GetApiKey()
    {
        return apiKeyInputField.text;
    }

    /// <summary>
    /// API 타입에 따라 라벨을 반환합니다.
    /// </summary>
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
                return "API Key";
        }
    }
}
