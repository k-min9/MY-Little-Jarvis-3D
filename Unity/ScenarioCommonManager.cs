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

    // 대사를 보여주고 wav 재생하고 길이 반환.
    private float Narration(string scenarioId, string dialogue)
    {
        // 안내문코드 : I01_installer_check_
        // 안내문 : 선생님. 대화를 위한 기본적인 파일이 설치되어 있지 않아요. 설치를 위한 프로그램을 구동해도 될까요?
        AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(dialogue);

        // 음성재생
        string file_name = scenarioId + "_ja.wav";
        if (SettingManager.Instance.settings.sound_language == "ko")
        {
            file_name = scenarioId + "_ko.wav";
        }
        else if (SettingManager.Instance.settings.sound_language == "en")
        {
            file_name = scenarioId + "_en.wav";
        }

        string filePath = Path.Combine("Audio", file_name);
        VoiceManager.Instance.PlayWavAudioFromPath(filePath);  // 음성 재생

        // 선택지 보여주기
        float duration = 3f;
        try
        {
            duration = UtilAudio.GetWavDurationInSeconds(filePath);
            Debug.Log(file_name + " 길이 : " + duration);
        }
        catch (System.Exception)
        {

        }
        duration += 0.5f;

        return duration;
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
        float d1 = Narration("C01_server_started_1", "선생님, 서버가 준비되었어요!");
        EmotionManager.Instance.ShowEmotion("star");
        yield return new WaitForSeconds(d1);
    }

    // 서버 기동 여부 묻는 시나리오
    public IEnumerator Scenario_C02_AskToStartServer()
    {
        float d1 = Narration("C02_ask_start_server_1", "선생님, 안녕하세요!");
        EmotionManager.Instance.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);

        float d2 = Narration("C02_ask_start_server_2", "현재 서버를 기동하지 않으셨는데 기동하셔도 괜찮으실까요?");
        EmotionManager.Instance.ShowEmotion("confused");
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(2, "C02_ask_start_server");  // <네>, <아니오>
    }
    
    // <네> 선택 시
    private IEnumerator Scenario_C02_1_ApproveStart()
    {
        float d1 = Narration("C02_approve_start_1", "네! 서버를 기동할게요.");
        EmotionManager.Instance.ShowEmotion("star");
        yield return new WaitForSeconds(d1);

        JarvisServerManager.Instance.RunJarvisServerWithCheck();  // 서버 기동
    }

    // <아니오> 선택 시
    private IEnumerator Scenario_C02_2_DeclineStart()
    {
        float d1 = Narration("C02_decline_start_1", "마음이 바뀌시면 언제든지 다시 말 걸어주세요, 선생님!");
        EmotionManager.Instance.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);
    }

}
