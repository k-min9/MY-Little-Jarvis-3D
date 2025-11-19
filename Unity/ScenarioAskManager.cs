using System.Collections;
using UnityEngine;

// 질문/선택지 기반 시나리오 전체 정리
public class ScenarioAskManager : MonoBehaviour
{
    public static ScenarioAskManager instance;
    public static ScenarioAskManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ScenarioAskManager>();
            }
            return instance;
        }
    }

    // 시나리오 시작
    public void StartAskScenario()
    {
        Debug.Log("질문 시나리오 시작됨");
        StatusManager.Instance.isScenario = true;
    }

    // 시나리오 종료시 호출
    public void EndAskScenario()
    {
        Debug.Log("[질문 시나리오 종료]");
        StatusManager.Instance.isScenario = false;

        // 시나리오 종료 후 필요한 리셋 작업
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
        VoiceManager.Instance.StopAudio();
    }

    // 시나리오 선택지 반영 (콜백)
    public void OnChoiceSelected(string scenarioId, int index)
    {
        Debug.Log($"[질문 시나리오 {scenarioId}] 선택지에서 {index}번 선택됨");

        // 선택한 선택지 내용을 시스템 메시지로 저장
        if (ChoiceData.Choices.TryGetValue(scenarioId, out var choices) && index >= 0 && index < choices.Count)
        {
            // 현재 UI 언어로 선택지 텍스트 가져오기
            string lang = SettingManager.Instance.settings.ui_language;
            string choiceText = "";
            if (choices[index].ContainsKey(lang))
            {
                choiceText = choices[index][lang];
            }
            else if (choices[index].ContainsKey("ko"))
            {
                choiceText = choices[index]["ko"];
            }
            else if (choices[index].ContainsKey("en"))
            {
                choiceText = choices[index]["en"];
            }
            
            // 번역 없이 선택지 텍스트를 모든 언어 필드에 저장
            if (!string.IsNullOrEmpty(choiceText))
            {
                MemoryManager.Instance.SaveSystemMemory("player", "user", choiceText, choiceText, choiceText, choiceText);
            }
        }

        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
        VoiceManager.Instance.StopAudio();

        switch (scenarioId)
        {
            case "S00_change_model":
                // <멀티모달 모델로 변경>
                if (index == 0)
                {
                    StartCoroutine(Scenario_S00_1_ChangeToMultimodal());
                }
                // <이미지 없이 계속>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_S00_2_ContinueWithoutImage());
                }
                // <취소>
                else
                {
                    StartCoroutine(Scenario_S00_3_Cancel());
                }
                break;
            case "S01_need_image":
                // <예>
                if (index == 0)
                {
                    StartCoroutine(Scenario_S01_1_OpenImageSetting());
                }
                // <아니오>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_S01_2_Decline());
                }
                // <이미지 설정을 OFF로 할게>
                else
                {
                    StartCoroutine(Scenario_S01_3_TurnOffImageSetting());
                }
                break;
            default:
                Debug.LogWarning("정의되지 않은 질문 시나리오 선택 분기");
                break;
        }
    }

    /////////////////////////////////// S00 - 모델 변경 ///////////////////////////////////////////////
    // 모델 변경 질문 시나리오 시작
    public IEnumerator Scenario_S00_AskChangeModel()
    {
        StartAskScenario();

        // 대사 출력
        float d1 = ScenarioUtil.Narration("S00_change_model_1", "현재 모델은 이미지를 처리할 수 없어요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("S00_change_model_2", "멀티모달 모델로 변경할까요?");
        ScenarioUtil.ShowEmotion("question");
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(3, "S00_change_model");
    }

    // <멀티모달 모델로 변경> 선택 시
    private IEnumerator Scenario_S00_1_ChangeToMultimodal()
    {
        // Custom 서버로 변경하고 Qwen3VL 모델 설정
        SettingManager.Instance.SetServerTypeByValue(9); // 9: Custom
        
        // Qwen3VL 모델을 Custom 모델로 설정 (인덱스 0 = Qwen3VL-8B-Instruct-Q4_K_M.gguf)
        SettingManager.Instance.settings.model_name_Custom = "Qwen3VL-8B-Instruct-Q4_K_M.gguf";
        SettingManager.Instance.SaveSettings();
        
        // 시스템 메시지로 설정 변경 기록
        string systemMessage = "\n<설정이 custom / qwen3-8b-VL로 변경되었습니다.>";
        MemoryManager.Instance.SaveSystemMemory("system", "system", systemMessage, systemMessage, systemMessage, systemMessage);

        float d1 = ScenarioUtil.Narration("S00_1_change_to_multimodal_1", "멀티모달 모델을 사용하게 설정을 변경할게요");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("S00_1_change_to_multimodal_2", "이제 이미지를 포함한 대화가 가능해요.");
        ScenarioUtil.ShowEmotion("><");
        yield return new WaitForSeconds(d2);

        EndAskScenario();
    }

    // <이미지 없이 계속> 선택 시
    private IEnumerator Scenario_S00_2_ContinueWithoutImage()
    {
        float d1 = ScenarioUtil.Narration("S00_2_continue_without_image_1", "알겠어요. 이미지 없이 계속할게요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);

        

        EndAskScenario();
    }

    // <취소> 선택 시
    private IEnumerator Scenario_S00_3_Cancel()
    {
        float d1 = ScenarioUtil.Narration("S00_3_cancel_1", "알겠어요. 필요하실 때 다시 말씀해주세요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);

        EndAskScenario();
    }

    /////////////////////////////////// S01 - 이미지 필요 안내 ///////////////////////////////////////////////
    // 이미지 필요 안내 시나리오 시작
    public IEnumerator Scenario_S01_AskNeedImage()
    {
        StartAskScenario();

        // 대사 출력
        float d1 = ScenarioUtil.Narration("S01_need_image_1", "그 대답에는 이미지가 필요해보여요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("S01_need_image_2", "이미지 영역을 설정하거나 클립보드에 이미지를 담아주셔야해요.");
        ScenarioUtil.ShowEmotion("neutral");
        yield return new WaitForSeconds(d2);

        float d3 = ScenarioUtil.Narration("S01_need_image_3", "이미지 영역 설정 창을 열어드릴까요, 선생님?");
        ScenarioUtil.ShowEmotion("question");
        yield return new WaitForSeconds(d3);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(3, "S01_need_image");
    }

    // <예> 선택 시
    private IEnumerator Scenario_S01_1_OpenImageSetting()
    {
        float d1 = ScenarioUtil.Narration("S01_1_open_image_setting_1", "잠시만 기다려주세요, 선생님.");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d1);

        yield return new WaitForSeconds(0.5f);

        // 이미지 설정창 열기
        ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
        sm.SetScreenshotArea();
    }

    // <아니오> 선택 시
    private IEnumerator Scenario_S01_2_Decline()
    {
        float d1 = ScenarioUtil.Narration("S01_2_decline_1", "네 다시 질문해주시는걸 기다리고 있을게요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);

        EndAskScenario();
    }

    // <이미지 설정을 OFF로 할게> 선택 시
    private IEnumerator Scenario_S01_3_TurnOffImageSetting()
    {
        // 이미지 설정을 OFF로 변경
        SettingManager.Instance.SetAIUseImageByValue(0); // 0: off

        float d1 = ScenarioUtil.Narration("S01_3_turn_off_image_setting_1", "네 설정을 바꿀게요 선생님.");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d1);

        EndAskScenario();
    }
}

