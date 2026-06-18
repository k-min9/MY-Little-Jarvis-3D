using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager instance;
    public static ChoiceManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChoiceManager>();
            }
            return instance;
        }
    }

    [Header("UI Components")]
    public GameObject choicePanel; // 패널
    public List<GameObject> choiceButtons; // ChoiceBtn 오브젝트들

    [Header("Choice Status")]
    public bool isShowingChoice = false; // 현재 선택지 출력 여부
    public string curChoiceScenario = "0"; // 선택지가 나오게 한 시나리오 정보
    public List<string> lastRecommendedChoices = new List<string>(); // 추천 답변 복사본 보관용

    private float btnHeight = 120f;
    private float btnSpacing = 200f; // 버튼 간 간격 (버튼 높이 + 간격 포함)

    [Header("Minimize Layout Settings")]
    public float minimizeBtnSpacingX = 370f; // 가로 모드 시 버튼 간 가로 간격
    public float minimizeBtnStepY = 120f;    // 가로 모드 시 계단식 Y축 상승폭
    public float minimizeScale = 0.85f;      // 가로 모드 시 버튼 크기 비율

    private void Start()
    {
        HideAllChoices();
    }

    private void Update()
    {
        // AI_CHOICE이고 선택지가 띄워져 있을 때, 배경(빈 곳) 클릭 시 창 닫기
        if (isShowingChoice && curChoiceScenario == "AI_CHOICE" && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)))
        {
            var pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                bool hitChoiceBtn = false;
                foreach (var btn in choiceButtons)
                {
                    // 클릭한 오브젝트가 선택지 버튼이거나 그 자식이면 무시 (정상적인 버튼 클릭으로 처리되도록 냅둠)
                    if (raycastResults[i].gameObject.transform.IsChildOf(btn.transform))
                    {
                        hitChoiceBtn = true;
                        break;
                    }
                }
                if (hitChoiceBtn) return;
            }

            // 클릭한 곳에 ChoiceBtn 관련 UI가 없다면 창 닫기
            HideAllChoices();
        }
    }

    public void ShowChoice(int btnNumber, string choiceScenario = "00")
    {
        Debug.Log($"[ChoiceManager] ShowChoice 호출됨 - Scenario: {choiceScenario}, BtnNumber: {btnNumber}");
        isShowingChoice = true;
        curChoiceScenario = choiceScenario;

        choicePanel.SetActive(true);

        // 중앙 기준 Y 시작점 계산 (기본 세로 배치용)
        float startY = (btnSpacing * (btnNumber - 1) / 2f);

        // 가로 중앙 기준 X 시작점 계산 (AI_CHOICE 가로 배치용)
        float startX = -(minimizeBtnSpacingX * (btnNumber - 1) / 2f);
        
        // Canvas 높이를 기반으로 하단 90% (아래에서 10%) 지점 계산
        float fixedBottomY = -350f; 
        if (CanvasManager.Instance != null && CanvasManager.Instance.canvasUI != null)
        {
            RectTransform canvasRect = CanvasManager.Instance.canvasUI.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                // 하단에서 10% 지점 (위로 갈수록 커짐)
                fixedBottomY = canvasRect.rect.yMin + (canvasRect.rect.height * 0.1f);
            }
        }

        // 현재 언어 설정 (ko, jp, en)
        string lang = SettingManager.Instance.settings.ui_language;

        // 선택지 데이터 가져오기
        List<Dictionary<string, string>> choices;
        if (!ChoiceData.Choices.TryGetValue(choiceScenario, out choices))
        {
            Debug.LogWarning($"[ShowChoice] Scenario '{choiceScenario}'에 대한 선택지 데이터를 찾을 수 없습니다.");
            return;
        }

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (i < btnNumber && i < choices.Count)
            {
                choiceButtons[i].SetActive(true);

                // 텍스트 설정 (없는 언어면 영어 fallback)
                TextMeshProUGUI btnText = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                string text = choices[i].ContainsKey(lang) ? choices[i][lang] : choices[i].GetValueOrDefault("en", "Choice");
                btnText.text = text;

                // 텍스트 영역을 버튼 크기에 맞게 꽉 채우기 (여백 포함)
                RectTransform textRect = btnText.rectTransform;
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(100f, 20f); // 좌하단 여백 (좌측 100, 하단 20)
                textRect.offsetMax = new Vector2(-100f, -20f); // 우상단 여백 (우측 100, 상단 20)

                // 버튼 위치 및 크기, 텍스트 속성 조정 (가로 vs 세로)
                RectTransform btnRect = choiceButtons[i].GetComponent<RectTransform>();
                if (choiceScenario == "AI_CHOICE" && SettingManager.Instance.settings.minimize_choice)
                {
                    // 계단식 우상향 배치: 설정된 Y축 상승폭(minimizeBtnStepY) 활용
                    btnRect.anchoredPosition = new Vector2(startX + i * minimizeBtnSpacingX, fixedBottomY + (i * minimizeBtnStepY));
                    btnRect.localScale = new Vector3(minimizeScale, minimizeScale, 1f); // 버튼 축소

                    // 텍스트 두 줄 바꿈 및 크기 자동 조절 활성화
                    btnText.enableWordWrapping = true;
                    btnText.enableAutoSizing = true;
                    btnText.fontSizeMin = 20f;
                    btnText.fontSizeMax = 36f;
                    btnText.alignment = TextAlignmentOptions.CenterGeoAligned; // 중앙 정렬 확실하게
                }
                else
                {
                    btnRect.anchoredPosition = new Vector2(0f, startY - i * btnSpacing);
                    btnRect.localScale = Vector3.one; // 원래 크기 복구

                    // 기본 설정으로 복구
                    btnText.enableWordWrapping = false;
                    btnText.enableAutoSizing = false;
                    btnText.fontSize = 36f;
                    btnText.alignment = TextAlignmentOptions.CenterGeoAligned;
                }
            }
            else
            {
                choiceButtons[i].SetActive(false);
            }
        }
    }

    public void HideAllChoices()
    {
        Debug.Log($"[ChoiceManager] HideAllChoices 호출됨");
        choicePanel.SetActive(false);
        isShowingChoice = false;

        foreach (var btn in choiceButtons)
        {
            btn.SetActive(false);
        }
    }

    // 선택 버튼에서 호출할 이벤트
    public void OnClickChoice(int index)
    {
        if (!isShowingChoice) return;

        isShowingChoice = false;
        HideAllChoices();

        Debug.Log($"Choice selected: {index}");

        // 시나리오 ID 기반으로 분기
        switch (curChoiceScenario)
        {
            case string s when s.StartsWith("A"): // Tutorial 시나리오
                ScenarioTutorialManager.Instance.OnChoiceSelected(curChoiceScenario, index);
                break;
            case string s when s.StartsWith("I"): // Installer 시나리오
                ScenarioInstallerManager.Instance.OnChoiceSelected(curChoiceScenario, index);
                break;
            case string s when s.StartsWith("C"): // Common 시나리오
                ScenarioCommonManager.Instance.OnChoiceSelected(curChoiceScenario, index);
                break;
            case string s when s.StartsWith("S"): // Ask Manager 시나리오
                ScenarioAskManager.Instance.OnChoiceSelected(curChoiceScenario, index);
                break;
            case "AI_CHOICE":
                if (index < ChoiceData.Choices["AI_CHOICE"].Count)
                {
                    string text = ChoiceData.Choices["AI_CHOICE"][index]["ko"];
                    // 선택 시 채팅 인덱스 증가 및 서버 전송
                    GameManager.Instance.chatIdx++;
                    APIManager.Instance.CallConversationStream(text, GameManager.Instance.chatIdx.ToString());
                }
                break;
            default:
                Debug.LogWarning($"Unknown scenario type: {curChoiceScenario}");
                break;
        }
    }

    public void ShowLastAIChoices()
    {
        if (lastRecommendedChoices != null && lastRecommendedChoices.Count > 0)
        {
            List<Dictionary<string, string>> choicesData = new List<Dictionary<string, string>>();
            foreach (string choiceStr in lastRecommendedChoices)
            {
                choicesData.Add(new Dictionary<string, string> { { "ko", choiceStr }, { "jp", choiceStr }, { "en", choiceStr } });
            }
            ChoiceData.Choices["AI_CHOICE"] = choicesData;
            ShowChoice(choicesData.Count, "AI_CHOICE");
        }
    }
}
