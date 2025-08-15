using System.Collections;
using UnityEngine;
using System.IO;

// 짧은/공통 시나리오 전체 정리
public class ScenarioCommonManager : MonoBehaviour
{
    public static ScenarioCommonManager instance;
    public static ScenarioCommonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ScenarioCommonManager>();
            }
            return instance;
        }
    }

    // 시나리오 선택지 반영 (콜백)
    public void OnChoiceSelected(string scenarioId, int index)
    {
        Debug.Log($"[시나리오 {scenarioId}] 선택지에서 {index}번 선택됨");

        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
        VoiceManager.Instance.StopAudio();

        switch (scenarioId)
        {
            case "C02_ask_start_server":
                // <네>
                if (index == 0)
                {
                    StartCoroutine(Scenario_C02_1_ApproveStart());
                }
                // <아니오>
                else
                {
                    StartCoroutine(Scenario_C02_2_DeclineStart());
                }
                break;
            default:
                Debug.LogWarning("정의되지 않은 시나리오 선택 분기");
                break;
        }
    }

    // 외부에서 StartCoroutine으로 직접 호출
    public IEnumerator Run_C01_ServerStarted()
    {
        float d1 = ScenarioUtil.Narration("C01_server_started_1", "선생님, 서버가 준비되었어요!");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d1);
    }

    // 서버 기동 여부 묻는 시나리오
    public IEnumerator Scenario_C02_AskToStartServer()
    {
        float d1 = ScenarioUtil.Narration("C02_ask_start_server_1", "선생님, 안녕하세요!");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("C02_ask_start_server_2", "현재 서버를 기동하지 않으셨는데 기동하셔도 괜찮으실까요?");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(2, "C02_ask_start_server");  // <네>, <아니오>
    }
    
    // <네> 선택 시
    private IEnumerator Scenario_C02_1_ApproveStart()
    {
        float d1 = ScenarioUtil.Narration("C02_approve_start_1", "네! 서버를 기동할게요.");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d1);

        JarvisServerManager.Instance.RunJarvisServerWithCheck();  // 서버 기동
    }

    // <아니오> 선택 시
    private IEnumerator Scenario_C02_2_DeclineStart()
    {
        float d1 = ScenarioUtil.Narration("C02_decline_start_1", "마음이 바뀌시면 언제든지 다시 말 걸어주세요, 선생님!");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);
    }

}
