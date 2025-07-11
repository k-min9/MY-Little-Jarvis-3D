using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private float btnHeight = 120f;
    private float btnSpacing = 200f; // 버튼 간 간격 (버튼 높이 + 간격 포함)

    private void Start()
    {
        HideAllChoices();
    }

    public void ShowChoice(int btnNumber, string choiceScenario = "00")
    {
        isShowingChoice = true;
        curChoiceScenario = choiceScenario;

        choicePanel.SetActive(true);

        // 중앙 기준 Y 시작점 계산
        float startY = (btnSpacing * (btnNumber - 1) / 2f);

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

                // 버튼 위치 조정
                RectTransform btnRect = choiceButtons[i].GetComponent<RectTransform>();
                btnRect.anchoredPosition = new Vector2(0f, startY - i * btnSpacing);

                // 텍스트 설정 (없는 언어면 영어 fallback)
                TextMeshProUGUI btnText = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                string text = choices[i].ContainsKey(lang) ? choices[i][lang] : choices[i].GetValueOrDefault("en", "Choice");
                btnText.text = text;
            }
            else
            {
                choiceButtons[i].SetActive(false);
            }
        }
    }

    public void HideAllChoices()
    {
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
            default:
                Debug.LogWarning($"Unknown scenario type: {curChoiceScenario}");
                break;
        }
    }
}
