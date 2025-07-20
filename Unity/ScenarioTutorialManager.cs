using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Management;


public class ScenarioTutorialManager : MonoBehaviour
{
    public static ScenarioTutorialManager instance;
    public static ScenarioTutorialManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ScenarioTutorialManager>();
            }
            return instance;
        }
    }

    // 튜토리얼 시작
    public void StartTutorial()
    {
        // 기존 시나리오 실행중일 경우 return 
        if (StatusManager.Instance.isScenario) return;

        Debug.Log("튜토리얼 시작됨");
        StatusManager.Instance.isScenario = true;

        if (!SettingManager.Instance.settings.isShowTutorialOnChat)
        {
            Debug.Log("튜토리얼 비활성화 상태");
            return;
        }

        StartCoroutine(Scenario_A00_EntryCondition());
    }

    // 시나리오 종료시 호출
    public void EndTutorial()
    {
        Debug.Log("[튜토리얼 종료]");
        StatusManager.Instance.isScenario = false;

        // 튜토리얼 종료 후 필요한 리셋 작업이 있다면 여기에 추가
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
        VoiceManager.Instance.StopAudio();
    }

    // 시나리오 선택지 반영 (콜백)
    public void OnChoiceSelected(string scenarioId, int index)
    {
        Debug.Log($"[시나리오 {scenarioId}] 선택지에서 {index}번 선택됨");

        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
        VoiceManager.Instance.StopAudio();

        switch (scenarioId)
        {
            // A01 분기 처리
            case "A01_free_server_offer":
                // <응, 다시 해보자>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A01_1_ConnectTry());
                }
                // <아니, 안할래>
                else
                {
                    StartCoroutine(Scenario_A01_2_RefuseFree());
                }
                break;
            case "A01_1_2_connect_failed":
                // <다시 시도할게>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A01_1_2_1_ConnectRetry());
                }
                // <나중에 할래>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_A01_1_2_2_ConnectPending());
                }
                // <그만둘래>
                else
                {
                    StartCoroutine(Scenario_A01_1_2_3_ConnectRefuse());
                }
                break;
            case "A01_2_refuse_free":
                // <아니, 안할래>
                StartCoroutine(Scenario_A01_2_RefuseFree());
                break;
            case "A02_platform_check":
                // <응, 맞아>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A02_1_CheckServerStatus());
                }
                // <아니, PC 맞아>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_A02_2_PCConfirmed());
                }
                // <필요 없어>
                else
                {
                    StartCoroutine(Scenario_A02_3_RefusePlatformCheck());
                }
                break;
            case "A02_1_check_server_status":
                // <연결할 PC가 있어>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A02_1_1_PCIdInput());
                }
                // <외부 플랫폼을 사용하려고 해>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_A04_ExternalServerSelect());
                }
                // <자세히 설명해줄 수 있어?>
                else
                {
                    StartCoroutine(Scenario_A02_1_2_HelpExplain());
                }
                break;
            case "A03_inference_select":
                // <이 PC에서 연산할래>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A03_1_LocalCompute());
                }
                // <외부 서버를 사용할래>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_A04_ExternalServerSelect()); // 다음 시나리오로 연결
                }
                // <그만둘래>
                else
                {
                    StartCoroutine(Scenario_A98_ConfigCancel());
                }
                break;
            case "A03_1_1_cuda_supported":
                // <GPU를 사용>
                if (index == 0 || index == 1)  // GPU 또는 CPU 선택 → 공통 완료
                {
                    if (index == 0)  // GPU 선택
                    { 
                        // TODO 그냥 최대 출력
                    }

                    StartCoroutine(Scenario_A99_ConfigEnd());
                }
                // <취소할래>
                else
                {
                    StartCoroutine(Scenario_A98_ConfigCancel());
                }
                break;
            case "A03_1_2_cuda_not_supported":
                // <진행할게>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A99_ConfigEnd());
                }
                // <취소할래>
                else
                {
                    StartCoroutine(Scenario_A98_ConfigCancel());
                }
                break;
            case "A04_external_server_select":
                // <무료 서버 연결>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A04_1_FreeServerExternal());
                }
                // <API 키 입력할래>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_A04_2_APIKeyInput());
                }
                // <설정 취소할래>
                else
                {
                    StartCoroutine(Scenario_A98_ConfigCancel());
                }
                break;
            case "A04_1_1_connect_failed":
                // <다시 시도>
                if (index == 0)
                {
                    StartCoroutine(Scenario_A04_1_FreeServerExternal());
                }
                // <나중에 할래>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_A04_1_1_ConnectFailedLater()); // TODO : 신규
                }
                // <그만둘래>
                else
                {
                    StartCoroutine(Scenario_A98_ConfigCancel());
                }
                break;
            case "A04_2_api_key_input":
                if (index >= 0 && index <= 2-1)  // 0: Gemini, 1: OpenRouter,  TODO :  2: ChatGPT
                {
                    // API 타입 문자열 매핑
                    string[] apiTypes = { "gemini", "openrouter", "chatgpt" };
                    string selectedApiType = apiTypes[index];

                    ChoiceInputManager.Instance.ShowInput(selectedApiType);
                }
                else
                {
                    StartCoroutine(Scenario_A03_InferenceSelect());
                }
                break;

            case "A97_connect_test_retry":
                if (index == 0)  // <다시 시도>
                {
                    StartCoroutine(Scenario_A97_ConnectTest(lastTriedIndex));  // 이전 시도한 대상으로 재시도
                }
                else
                {
                    StartCoroutine(Scenario_A98_ConfigCancel());
                }
                break;
            case "A98_config_cancel":
                // 선택지 없음
                StartCoroutine(Scenario_A98_ConfigCancel());
                break;

            case "A99_config_end":
                // 선택지 없음
                StartCoroutine(Scenario_A99_ConfigEnd());
                break;
            default:
                Debug.LogWarning("정의되지 않은 시나리오 선택 분기");
                break;
        }
    }

    private IEnumerator ShowChoiceAfterTime(int btnNumber, string choiceScenario, float time)
    {
        yield return new WaitForSeconds(time);
        ChoiceManager.Instance.ShowChoice(btnNumber, choiceScenario);
    }

    /////////////////////////////////// A00 ///////////////////////////////////////////////
    private IEnumerator Scenario_A00_EntryCondition()
    {
        float d1 = ScenarioUtil.Narration("A00_entry_condition_1", "선생님, 안녕하세요!");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        // EmotionManager.Instance.ShowEmotionFromEmotion("Joy");  // TODO : 나중에 전체 캐릭터에도 표정 적용
        yield return new WaitForSeconds(d1);

        // 무료서버의향 = true일 경우 의도 확인
        if (SettingManager.Instance.settings.wantFreeServer)
        {
            StartCoroutine(Scenario_A01_FreeServerOffer());
        }
        else
        {
            // PC인지 아닌지 확인
            RuntimePlatform platform = Application.platform;
            if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
            {
                // PC 플랫폼
                StartCoroutine(Scenario_A03_InferenceSelect());
            }
            else
            {
                // 모바일 또는 기타 플랫폼
                StartCoroutine(Scenario_A02_PlatformCheck());
            }
        }
        
    }

    /////////////////////////////////// A01 ///////////////////////////////////////////////
    private IEnumerator Scenario_A01_FreeServerOffer()
    {
        float d1 = ScenarioUtil.Narration("A01_free_server_offer_1", "전에 무료 서버를 사용하려고 하셨던 것 같아요.");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A01_free_server_offer_2", "다시 연결해볼까요?");
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(2, "A01_free_server_offer");  // <응, 다시 해보자>, <아니, 안할래>
    }

    private IEnumerator Scenario_A01_1_ConnectTry()
    {
        string freeServerUrl = "http://127.0.0.1:5000/health_free_server";
        bool success = false;

        using (UnityWebRequest request = UnityWebRequest.Get(freeServerUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = request.downloadHandler.text;
                    JObject jsonObject = JObject.Parse(jsonText);
                    string status = jsonObject["status"]?.ToString();

                    if (status == "available")
                    {
                        success = true;
                        string serverType = jsonObject["server_type"]?.ToString();
                        Debug.Log($"무료서버 연결 정상, 타입: {serverType}");
                    }
                    else
                    {
                        Debug.Log("무료서버 연결 실패: 상태 비정상");
                    }
                }
                catch
                {
                    Debug.Log("무료서버 응답 파싱 실패");
                }
            }
            else
            {
                Debug.Log("무료서버 연결 실패: 요청 오류");
            }
        }

        // 이후 흐름 동일
        if (success)
        {
            float d1 = ScenarioUtil.Narration("A01_1_1_connect_success_1", "성공적으로 연결되었어요, 선생님.");
            EmotionManager.Instance.ShowEmotion("star");
            yield return new WaitForSeconds(d1);

            float d2 = ScenarioUtil.Narration("A01_1_1_connect_success_2", "다만, 무료 서버는 응답 속도가 느리거나 다시 요청해야 될 수도 있어요.");
            EmotionManager.Instance.ShowEmotion("confused");
            yield return new WaitForSeconds(d2);

            float d3 = ScenarioUtil.Narration("A01_1_1_connect_success_3", "대화가 자연스럽지 않거나, 힘드시면 다른 방법을 시도해주세요.");
            EmotionManager.Instance.ShowEmotion("><");
            yield return new WaitForSeconds(d3);

            StartCoroutine(Scenario_A99_ConfigEnd());
        }
        else
        {
            float d1 = ScenarioUtil.Narration("A01_1_2_connect_failed_1", "연결에 실패했어요, 선생님.");
            EmotionManager.Instance.ShowEmotion("confused");
            yield return new WaitForSeconds(d1);

            float d2 = ScenarioUtil.Narration("A01_1_2_connect_failed_2", "무료 서버는 가끔 연결이 불안정할 수 있어요. 계속 시도해볼까요?");
            EmotionManager.Instance.ShowEmotion("confused");
            yield return new WaitForSeconds(d2);

            ChoiceManager.Instance.ShowChoice(3, "A01_1_2_connect_failed");
        }
    }


    private IEnumerator Scenario_A01_1_2_1_ConnectRetry()
    {
        float d1 = ScenarioUtil.Narration("A01_1_2_1_connect_retry_1", "다시 연결을 시도해볼게요.");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        StartCoroutine(Scenario_A01_1_ConnectTry()); // 재시도 루프
    }


    private IEnumerator Scenario_A01_1_2_2_ConnectPending()
    {
        float d1 = ScenarioUtil.Narration("A01_1_2_2_connect_pending_1", "네. 원하실 때 언제든지 다시 시도하실 수 있어요.");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        // 무료서버의향 = true + 저장
        SettingManager.Instance.settings.wantFreeServer = true;
        SettingManager.Instance.SaveSettings();

        // 다음 단계로 이동
        // PC인지 아닌지 확인
        RuntimePlatform platform = Application.platform;
        if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
        {
            // PC 플랫폼
            StartCoroutine(Scenario_A03_InferenceSelect());
        }
        else
        {
            // 모바일 또는 기타 플랫폼
            StartCoroutine(Scenario_A02_PlatformCheck());
        }
    }

    private IEnumerator Scenario_A01_1_2_3_ConnectRefuse()
    {
        yield return Scenario_A01_2_RefuseFree();
    }

    private IEnumerator Scenario_A01_2_RefuseFree()
    {
        float d1 = ScenarioUtil.Narration("A01_1_2_3_connect_refuse_1", "네. 그러면...");
        EmotionManager.Instance.ShowEmotion("relax");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        // 무료서버의향 = false 저장
        SettingManager.Instance.settings.wantFreeServer = false;
        SettingManager.Instance.SaveSettings();

        // PC인지 아닌지 확인
        RuntimePlatform platform = Application.platform;
        if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
        {
            // PC 플랫폼
            StartCoroutine(Scenario_A03_InferenceSelect());
        }
        else
        {
            // 모바일 또는 기타 플랫폼
            StartCoroutine(Scenario_A02_PlatformCheck());
        }
    }
    /////////////////////////////////// A02 ///////////////////////////////////////////////
    private IEnumerator Scenario_A02_PlatformCheck()
    {
        float d1 = ScenarioUtil.Narration("A02_platform_check_1", "저와 대화하시려면 먼저 환경 설정이 필요해요.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A02_platform_check_2", "제가 대화 전 세팅을 도와드릴게요.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        float d3 = ScenarioUtil.Narration("A02_platform_check_3", "지금 접속하신 기기가 PC는 아닌 것 같은데, 맞으실까요?");
        EmotionManager.Instance.ShowEmotion("listen");  // 아로나만 표정
        yield return new WaitForSeconds(d3);

        ChoiceManager.Instance.ShowChoice(3, "A02_platform_check");  // <응, 맞아>, <아니, PC 맞아>, <필요 없어>
    }

    private IEnumerator Scenario_A02_1_CheckServerStatus()
    {
        float d1 = ScenarioUtil.Narration("A02_1_check_server_status_1", "확인해주셔서 고마워요, 선생님.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A02_1_check_server_status_2", "일단 지금 AI 서버가 실행된 PC 정보가 있으실까요?");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        ChoiceManager.Instance.ShowChoice(3, "A02_1_check_server_status");  // <연결할 PC가 있어>, <외부 플랫폼을 사용하려고 해>, <자세히 설명해줄 수 있어?>
    }

    private IEnumerator Scenario_A02_1_1_PCIdInput()
    {
        float d1 = ScenarioUtil.Narration("A02_1_1_pc_id_input_1", "좋아요! 그럼 연결할 ID를 입력해주시면 바로 설정할게요.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        // ID 입력 UI 트리거 필요 시 연결
        Debug.Log("ID 입력 대기 상태로 전환됩니다.");
    }

    private IEnumerator Scenario_A02_1_2_HelpExplain()
    {
        float d1 = ScenarioUtil.Narration("A02_1_2_help_explain_1", "저를 다운로드 받은 곳에서 PC 버전을 다운로드 받으실 수 있어요.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A02_1_2_help_explain_2", "서버프로그램(`server.exe`) 실행 시 입력한 ID를 제게도 입력해주시면 연결돼요.");
        EmotionManager.Instance.ShowEmotion("relax");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        float d3 = ScenarioUtil.Narration("A02_1_2_help_explain_3", "자세한 내용은 M9Dev 유튜브 채널에서도 확인하실 수 있어요.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d3);
    }

    private IEnumerator Scenario_A02_2_PCConfirmed()
    {
        float d1 = ScenarioUtil.Narration("A02_2_1_pc_confirmed_1", "확인 감사합니다, 선생님.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        // 다음단계 : A03으로
        StartCoroutine(Scenario_A03_InferenceSelect());
    }

    private IEnumerator Scenario_A02_3_RefusePlatformCheck()
    {
        float d1 = ScenarioUtil.Narration("A02_3_1_refuse_platform_check_1", "언제든 저와 이야기하고 싶으실 때 불러주세요, 선생님!");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        EndTutorial();
    }

    /////////////////////////////////// A03 ///////////////////////////////////////////////
    private IEnumerator Scenario_A03_InferenceSelect()
    {
        float d1 = ScenarioUtil.Narration("A03_inference_select_1", "AI가 작동하려면 먼저 서버 설정이 필요해요.");
        EmotionManager.Instance.ShowEmotion("relax");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A03_inference_select_2", "지금 사용 중인 이 PC로 연산하는 것도 가능해요.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(3, "A03_inference_select");  // <이 PC에서 연산할래>, <외부 서버를 사용할래>, <그만둘래>
    }


    private IEnumerator Scenario_A03_1_LocalCompute()
    {
        // lite 버전이면 return
        

        // servertype을 server로 변경
        SettingManager.Instance.SetServerTypeByValue(1);

        float d1 = ScenarioUtil.Narration("A03_1_local_compute_1", "확인해볼게요... CUDA 환경 지원 여부를 검사 중이에요.");
        yield return new WaitForSeconds(d1);

        // NVIDIA GPU 탐지
        bool isCudaAvailable = IsNvidiaGpu();

        if (isCudaAvailable)
        {
            float d2 = ScenarioUtil.Narration("A03_1_1_cuda_supported_1", "CUDA 환경이 감지되었어요.");
            yield return new WaitForSeconds(d2);

            float d3 = ScenarioUtil.Narration("A03_1_1_cuda_supported_2", "선생님의 PC에서 GPU를 사용할 수 있어요. 더 빠른 응답을 원하신다면 좋은 선택이에요. 어떻게 하실래요?");
            yield return new WaitForSeconds(d3);

            yield return new WaitForSeconds(0.2f);
            ChoiceManager.Instance.ShowChoice(3, "A03_1_1_cuda_supported"); // <GPU를 사용>, <CPU만으로 사용>, <취소할래>
        }
        else
        {
            float d2 = ScenarioUtil.Narration("A03_1_2_cuda_not_supported_1", "CUDA 환경을 찾지 못했어요.");
            yield return new WaitForSeconds(d2);

            float d3 = ScenarioUtil.Narration("A03_1_2_cuda_not_supported_2", "아쉽게도 GPU는 지원되지 않지만, CPU로 작동하는 건 가능해요. 이어서 진행하실까요?");
            yield return new WaitForSeconds(d3);

            yield return new WaitForSeconds(0.2f);
            ChoiceManager.Instance.ShowChoice(2, "A03_1_2_cuda_not_supported");  // <진행할게>, <취소할래>
        }
    }

    private bool IsNvidiaGpu()
    {
        string gpuName = SystemInfo.graphicsDeviceName.ToLower();
        return gpuName.Contains("nvidia");
    }



    /////////////////////////////////// A04 ///////////////////////////////////////////////
    private IEnumerator Scenario_A04_ExternalServerSelect()
    {
        float d1 = ScenarioUtil.Narration("A04_external_server_select_1", "그러면 외부 서버와 연결해볼게요.");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A04_external_server_select_2", "무료 서버를 이용하시거나, API 키를 입력해서 외부 플랫폼(Gemini, ChatGPT...)과 연결하실 수 있어요.");
        EmotionManager.Instance.ShowEmotion("listen");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(3, "A04_external_server_select");  // <무료 서버 연결>, <API 키 입력할래>, <설정 취소할래>
    }

    private IEnumerator Scenario_A04_1_FreeServerExternal()
    {
        string freeServerUrl = "http://127.0.0.1:5000/health_free_server";
        string healthUrl = "http://127.0.0.1:5000/health";
        bool isConnected = false;
        bool serverAlive = false;

        // 1차: 무료 서버 연결 체크
        using (UnityWebRequest request = UnityWebRequest.Get(freeServerUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonText = request.downloadHandler.text;
                    JObject jsonObject = JObject.Parse(jsonText);
                    string status = jsonObject["status"]?.ToString();

                    if (status == "available")
                    {
                        isConnected = true;
                        string serverType = jsonObject["server_type"]?.ToString();
                        Debug.Log($"무료서버 연결 정상, 타입 : {serverType}");
                    }
                    else
                    {
                        Debug.Log("무료서버 연결 실패: 상태 비정상");
                    }
                }
                catch
                {
                    Debug.Log("무료서버 응답 파싱 실패");
                }
            }
            else
            {
                Debug.Log("무료서버 연결 실패: 요청 오류");
            }
        }

        // 2차: 서버 살아있는지 확인
        if (!isConnected)
        {
            using (UnityWebRequest healthRequest = UnityWebRequest.Get(healthUrl))
            {
                yield return healthRequest.SendWebRequest();

                if (healthRequest.result == UnityWebRequest.Result.Success)
                {
                    serverAlive = true;
                    Debug.Log("서버는 켜져 있음 (무료서버만 실패)");
                }
                else
                {
                    Debug.Log("서버가 꺼져 있습니다 (127.0.0.1:5000 응답 없음)");  // TODO : 서버를 켜주세요 선생님. 서버를 켤까요 선생님? 서버 키는 기능 만들고 고민.
                }
            }
        }

        // 최종 흐름 분기
        if (isConnected)
        {
            float d1 = ScenarioUtil.Narration("A04_1_free_server_external_success", "무료 서버에 성공적으로 연결되었어요.");
            EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
            yield return new WaitForSeconds(d1);
            StartCoroutine(Scenario_A99_ConfigEnd());
        }
        else
        {
            float d1 = ScenarioUtil.Narration("A04_1_1_connect_failed_1", "연결이 잘 되지 않았어요. 다시 시도해볼까요?");
            EmotionManager.Instance.ShowEmotion("confused");  // 아로나만 표정
            yield return new WaitForSeconds(d1);

            if (!serverAlive)
            {
                Debug.LogWarning("⚠ 서버 자체가 꺼져있습니다. 유저에게 안내 필요.");
            }

            yield return new WaitForSeconds(0.2f);
            ChoiceManager.Instance.ShowChoice(3, "A04_1_1_connect_failed");
        }
    }


    private IEnumerator Scenario_A04_1_1_ConnectFailedLater()
    {
        float d = ScenarioUtil.Narration("A04_1_1_connect_failed_later", "네. 언제든지 말씀만 해주시면 다시 시도해볼게요, 선생님.");
        EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d);

        // 무료서버의향 = true 저장
        SettingManager.Instance.settings.wantFreeServer = true;
        SettingManager.Instance.SaveSettings();
    }

    public IEnumerator Scenario_A04_2_APIKeyInput()
    {
        float d1 = ScenarioUtil.Narration("A04_2_api_key_input_1", "API KEY 관련 모델을 골라주세요");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(3, "A04_2_api_key_input");  // <Gemini>, <OpenRouter>, <전 선택지로>, //TODO : <ChatGPT>
    }
    /////////////////////////////////// A97 ///////////////////////////////////////////////
    private int lastTriedIndex = -1;
    public IEnumerator Scenario_A97_ConnectTest(int targetIndex)  // ChoiceInputManager에서 호출
    {
        lastTriedIndex = targetIndex;
        string[] targetNames = { "Gemini", "OpenRouter", "ChatGPT"};
        string target = (targetIndex >= 0 && targetIndex < targetNames.Length) ? targetNames[targetIndex] : "서버";

        // 음성 출력 (예상 시간)
        float d1 = ScenarioUtil.Narration("A97_connect_test_1", $"{target}에 연결을 시도하고 있어요...");
        EmotionManager.Instance.ShowEmotion("relax");  // 아로나만 표정

        // 연결 시도 전 시간 측정 시작
        float startTime = Time.realtimeSinceStartup;

        // API 키 결정
        string api_key_Gemini = SettingManager.Instance.settings.api_key_gemini;
        string api_key_OpenRouter = SettingManager.Instance.settings.api_key_openRouter;
        string api_key_ChatGPT = "";  // TODO : SettingManager에 입력/저장 가능하게
        string apiKey = "";
        switch (target)
        {
            case "ChatGPT": apiKey = api_key_ChatGPT ?? ""; break;
            case "Gemini": apiKey = api_key_Gemini ?? ""; break;
            case "OpenRouter": apiKey = api_key_OpenRouter ?? ""; break;
        }

        // TODO : 입력 apiKey가 빈 값일 경우 무료서버로 테스트할지 물어보기 추가.

        string url = $"http://127.0.0.1:5000/health_check_platform?target={target}&api_key={UnityWebRequest.EscapeURL(apiKey)}";
        bool isConnected = false;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var json = JObject.Parse(request.downloadHandler.text);
                    if (json["status"]?.ToString() == "available")
                        isConnected = true;
                }
                catch { }
            }
        }

        // 경과 시간 계산
        float elapsedTime = Time.realtimeSinceStartup - startTime;

        // d1보다 빨랐다면 남은 시간 + 0.3초 대기
        if (elapsedTime < d1)
        {
            float waitTime = (d1 - elapsedTime) + 0.3f;
            yield return new WaitForSeconds(waitTime);
        }

        // 결과 분기
        if (isConnected)
        {
            float d2 = ScenarioUtil.Narration("A97_connect_test_2", "성공했어요 선생님");
            EmotionManager.Instance.ShowEmotion("><");  // 아로나만 표정
            yield return new WaitForSeconds(d2);
            StartCoroutine(Scenario_A99_ConfigEnd());
        }
        else
        {
            float d3 = ScenarioUtil.Narration("A97_connect_test_3", "실패했어요 선생님. 다시 시도해볼까요?");
            EmotionManager.Instance.ShowEmotion("confused");  // 아로나만 표정
            yield return new WaitForSeconds(d3);

            yield return new WaitForSeconds(0.2f);
            ChoiceManager.Instance.ShowChoice(2, "A97_connect_test_retry");
        }
    }


    /////////////////////////////////// A98 ///////////////////////////////////////////////
    private IEnumerator Scenario_A98_ConfigCancel()
    {
        float d1 = ScenarioUtil.Narration("A98_config_cancel_1", "알겠어요 선생님.");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A98_config_cancel_2", "필요하실 땐 언제든지 다시 설정하실 수 있어요.");
        yield return new WaitForSeconds(d2);

        EndTutorial();
    }


    /////////////////////////////////// A99 ///////////////////////////////////////////////
    private IEnumerator Scenario_A99_ConfigEnd()
    {
        // 서버 기동하기
        JarvisServerManager.Instance.RunJarvisServerWithCheck();

        float d1 = ScenarioUtil.Narration("A99_config_end_1", "설정이 완료되었어요, 선생님!");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("A99_config_end_2", "필요하실 땐 언제든지 다시 설정하실 수 있어요.");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        float d3 = ScenarioUtil.Narration("A99_config_end_3", "이제 준비가 끝났어요, 선생님. 앞으로 나눌 이야기들이 정말 기대돼요!");
        EmotionManager.Instance.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d3);

        SettingManager.Instance.settings.isTutorialCompleted = true;

        EndTutorial();
    }
}
