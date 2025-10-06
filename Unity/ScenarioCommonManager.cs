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

    ////////////////////////////////////////////////////////////////
    // 외부에서 StartCoroutine으로 직접 호출 : C90대
    ////////////////////////////////////////////////////////////////
    // C90 - 일반 안내
    public IEnumerator Run_C90_unavailable_setting()
    {
        float d1 = ScenarioUtil.Narration("C90_unavailable_setting", "현재 버전에서는 사용할 수 없는 설정이에요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Run_C90_recommend_lite()
    {
        float d1 = ScenarioUtil.Narration("C90_recommend_lite", "Lite 이상의 Edition을 설치하시면 이용하실 수 있어요, 선생님.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Run_C90_recommend_full()
    {
        float d1 = ScenarioUtil.Narration("C90_recommend_full", "Full 이상의 Edition을 설치하시면 이용하실 수 있어요, 선생님.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);
    }

    // C91 - API/Quota/모델 안내
    public IEnumerator Run_C91_free_quota_exceeded()
    {
        float d1 = ScenarioUtil.Narration("C91_free_quota_exceeded", "구글에서 개발자에게 제공된 일일 무료 사용량이 이미 모두 소진된 것 같아요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Run_C91_api_key_needed()
    {
        float d1 = ScenarioUtil.Narration("C91_api_key_needed", "API 키를 발급받아 입력하시면 무료 또는 유료 서버와 연결하실 수 있어요.");
        ScenarioUtil.ShowEmotion("neutral");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Run_C91_free_ai_model_unavailable()
    {
        float d1 = ScenarioUtil.Narration("C91_free_ai_model_unavailable", "구글에서 더 이상 해당 무료 AI 모델을 이용할 수 없는 것 같아요.");
        ScenarioUtil.ShowEmotion("sad");
        yield return new WaitForSeconds(d1);
    }

    // C99 - 준비/안내
    public IEnumerator Run_C99_not_ready_1()
    {
        float d1 = ScenarioUtil.Narration("C99_not_ready_1", "선생님, 죄송해요 아직 준비가 되지 않았어요...");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Run_C99_not_ready_2()
    {
        float d1 = ScenarioUtil.Narration("C99_not_ready_2", "곧 지원할 예정이에요. 기다려주세요 선생님!");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Run_C99_offer_help()
    {
        float d1 = ScenarioUtil.Narration("C99_offer_help", "관련 안내가 필요하실까요, 선생님?");
        ScenarioUtil.ShowEmotion("neutral");
        yield return new WaitForSeconds(d1);
    }

    // 통합 : Run_C99_not_ready_1 + Run_C99_not_ready_2
    public IEnumerator Run_C99_NotReady()
    {
        // 첫 번째 멘트
        float d1 = ScenarioUtil.Narration("C99_not_ready_1", "선생님, 죄송해요 아직 준비가 되지 않았어요...");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);

        // 두 번째 멘트
        float d2 = ScenarioUtil.Narration("C99_not_ready_2", "곧 지원할 예정이에요. 기다려주세요 선생님!");
        ScenarioUtil.ShowEmotion("><");
        yield return new WaitForSeconds(d2);
    }

    // 통합 : Run_C90_unavailable_setting + Run_C90_recommend_lite
    public IEnumerator Run_C90_unavailable_setting_recommend_lite()
    {
        float d1 = ScenarioUtil.Narration("C90_unavailable_setting", "현재 버전에서는 사용할 수 없는 설정이에요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("C90_recommend_lite", "Lite 이상의 Edition을 설치하시면 이용하실 수 있어요, 선생님.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d2);

        // 이후 Lite 버전 설치 안내 시나리오 띄울지 고민
    }

    // 통합 : Run_C90_unavailable_setting + Run_C90_recommendfull
    public IEnumerator Run_C90_unavailable_setting_recommend_full()
    {
        float d1 = ScenarioUtil.Narration("C90_unavailable_setting", "현재 버전에서는 사용할 수 없는 설정이에요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("C90_recommend_full", "Full 이상의 Edition을 설치하시면 이용하실 수 있어요, 선생님.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d2);

        // 이후 Full 버전 설치 안내 시나리오 띄울지 고민
    }
}
