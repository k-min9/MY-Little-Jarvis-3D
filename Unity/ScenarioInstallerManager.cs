using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class ScenarioInstallerManager : MonoBehaviour
{
    public static ScenarioInstallerManager instance;
    public static ScenarioInstallerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ScenarioInstallerManager>();
            }
            return instance;
        }
    }

    // 설치 상태 리스너
    private bool isListeningInstallStatus = false;
    private float installCheckTimer = 0f;
    private const float installCheckInterval = 1f;
    private string previousInstallStatus = "";

    private void Start()
    {
        // 현재 설치 상태 저장 (변경 감지용)
        previousInstallStatus = InstallStatusManager.Instance.GetInstallStatusString();
    }

    private void Update()
    {
        if (!isListeningInstallStatus) return;

        installCheckTimer += Time.deltaTime;
        if (installCheckTimer >= installCheckInterval)
        {
            installCheckTimer = 0f;
            CheckInstallStatus();
        }
    }

    private void CheckInstallStatus()
    {
        // InstallStatusManager를 통해 최신 설치 상태 로드
        InstallStatusManager.Instance.LoadInstallStatus();
        string currentStatus = InstallStatusManager.Instance.GetInstallStatusString();

        // 설치 상태가 변경되었는지 확인
        if (currentStatus != previousInstallStatus)
        {
            Debug.Log($"설치 상태 변경 감지: {previousInstallStatus} -> {currentStatus}");

            // lite 또는 full로 변경된 경우
            if (currentStatus == "lite" || currentStatus == "full")
            {
                Debug.Log($"설치 완료: {currentStatus} 버전");

                // 안내 메시지 출력
                StartCoroutine(Scenario_I02_InstallComplete());

                // 리스닝 중단
                isListeningInstallStatus = false;

                // 설정 값 설정
                SettingManager.Instance.SetInstallStatus();

                // 이전 상태 업데이트
                previousInstallStatus = currentStatus;

                // 서버 구동 로직은 Scenario_I02_InstallComplete()에서 처리
            }
            else
            {
                Debug.LogWarning($"알 수 없는 설치 버전으로 변경: {currentStatus}");
                previousInstallStatus = currentStatus;
            }
        }
    }

    // 인스톨러 시나리오 시작
    public void StartInstaller()
    {
        Debug.Log("인스톨러 시나리오 시작됨");
        StatusManager.Instance.isScenario = true;

        StartCoroutine(Scenario_I00_Greeting());
    }

    // 인스톨러 시나리오 종료시 호출
    public void EndInstaller()
    {
        Debug.Log("[인스톨러 시나리오 종료]");
        StatusManager.Instance.isScenario = false;

        // 인스톨러 시나리오 종료 후 필요한 리셋 작업이 있다면 여기에 추가
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
        VoiceManager.Instance.StopAudio();
    }

    // 인스톨러 시나리오 선택지 반영 (콜백)
    public void OnChoiceSelected(string scenarioId, int index)
    {
        Debug.Log($"[인스톨러 시나리오 {scenarioId}] 선택지에서 {index}번 선택됨");

        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
        VoiceManager.Instance.StopAudio();

        switch (scenarioId)
        {
            case "I01_installer_check":
                // <응>
                if (index == 0)
                {
                    StartCoroutine(Scenario_I01_0_InstallServerType());
                }
                // <아니>
                else
                {
                    StartCoroutine(Scenario_I01_2_InstallLater());
                }
                break;

            case "I01_installer_server_type_check_lite":
                // <Lite(약 2GB)>
                if (index == 0)
                {
                    // StartCoroutine(Scenario_I01_1_InstallServerLite());
                    // TODO : 0.8.0에서 버전 확장해서 추가하는걸로. (현재 sample이 너무 유능)
                    StartCoroutine(ScenarioCommonManager.Instance.Run_C99_NotReady());
                }
                // <Full(약 16GB)>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_I01_1_InstallServer());
                }
                // <각 Edition에 대해 설명해줘>
                else if (index == 2)
                {
                    StartCoroutine(Scenario_I01_0_InstallServerExplain_Lite());
                }
                // <나중에 설치할게>
                else if (index == 3)
                {
                    StartCoroutine(Scenario_I01_2_InstallLater());
                }
                break;

            case "I01_installer_server_type_check_full":
                // <Full(약 16GB)>
                if (index == 0)
                {
                    StartCoroutine(Scenario_I01_1_InstallServer());
                }
                // <Full Edition에 대해 설명해줘>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_I01_0_InstallServerExplain_Full());
                }
                // <나중에 설치할게>
                else if (index == 2)
                {
                    StartCoroutine(Scenario_I01_2_InstallLater());
                }
                break;

            case "I03_free_key_exhausted":
                // <로컬 서버 설치>
                if (index == 0)
                {
                    StartCoroutine(Scenario_I00_CurrentCheckVersion());
                }
                // <외부 플랫폼 사용 (API 키 입력)>
                else if (index == 1)
                {
                    EndInstaller();
                    StartCoroutine(ScenarioTutorialManager.Instance.Scenario_A04_2_APIKeyInput2());
                }
                // <나중에할게>
                else if (index == 2)
                {
                    StartCoroutine(Scenario_I01_2_InstallLater());
                }
                break;

            default:
                Debug.LogWarning("정의되지 않은 인스톨러 시나리오 선택 분기");
                break;
        }
    }

    private IEnumerator ShowChoiceAfterTime(int btnNumber, string choiceScenario, float time)
    {
        yield return new WaitForSeconds(time);
        ChoiceManager.Instance.ShowChoice(btnNumber, choiceScenario);
    }

    /////////////////////////////////// I00 ///////////////////////////////////////////////
    public IEnumerator Scenario_I00_Greeting()
    {
        float d1 = ScenarioUtil.Narration("I00_greeting_1", "선생님, 안녕하세요.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);

        yield return StartCoroutine(Scenario_I00_CurrentCheckVersion());
    }

    public IEnumerator Scenario_I00_CurrentCheckSample()
    {
        float d1 = ScenarioUtil.Narration("I00_current_check_sample_1", "현재 Sample Edition을 사용 중이시네요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Scenario_I00_CurrentCheckLite()
    {
        float d1 = ScenarioUtil.Narration("I00_current_check_lite_1", "현재 Lite Edition을 사용 중이시네요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Scenario_I00_CurrentCheckFull()
    {
        float d1 = ScenarioUtil.Narration("I00_current_check_full_1", "현재 Full Edition을 사용 중이시네요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Scenario_I00_CurrentCheckVersion()
    {
        // 현재 설치 상태에 따라 사용 안내 달라짐
        string currentVersion = InstallStatusManager.Instance.GetInstallStatusString();
        if (currentVersion == "sample")
        {
            yield return StartCoroutine(Scenario_I00_CurrentCheckSample());
            yield return StartCoroutine(Scenario_I01_PreferHigher());
            yield return StartCoroutine(Scenario_I01_0_InstallServerType());
        }
        else if (currentVersion == "lite")
        {
            yield return StartCoroutine(Scenario_I00_CurrentCheckLite());
            yield return StartCoroutine(Scenario_I01_PreferHigher());
            yield return StartCoroutine(Scenario_I01_0_InstallServerType());
        }
        else if (currentVersion == "full")
        {
            yield return StartCoroutine(Scenario_I00_CurrentCheckFull());
            yield return StartCoroutine(Scenario_I01_AlreadyLatest());
        }
    }

    /////////////////////////////////// I01 ///////////////////////////////////////////////
    public IEnumerator Scenario_I01_PreferHigher()
    {
        float d1 = ScenarioUtil.Narration("I01_prefer_higher_1", "지금보다 더 많은 기능을 사용할 수 있는 상위 Edition을 설치하실 수도 있어요.");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d1);
    }

    public IEnumerator Scenario_I01_AlreadyLatest()
    {
        float d1 = ScenarioUtil.Narration("I01_already_latest_1", "이미 모든 기능을 사용하실 수 있는 상태에요!");
        ScenarioUtil.ShowEmotion("><");
        yield return new WaitForSeconds(d1);
    }

    // 구버전
    private IEnumerator Scenario_I01_0_InstallServerType()
    {
        float d1 = ScenarioUtil.Narration("I01_1_install_server_type_1", "설치를 위한 프로그램을 기동할까요? 원하신다면 설치 가능한 Edition들에 대해 간단히 설명드릴수 있어요.");
        ScenarioUtil.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        yield return new WaitForSeconds(0.2f);

        // 현재 설치 상태에 따라 다른 선택지 표시
        string currentVersion = InstallStatusManager.Instance.GetInstallStatusString();

        if (currentVersion == "sample")
        {
            // Sample → Lite/Full 모두 설치 가능
            ChoiceManager.Instance.ShowChoice(4, "I01_installer_server_type_check_lite");  // <Lite(약 2GB)>, <Full(약 16GB)>, <각 버전에 대해 설명해줘>, <나중에 설치할게>
        }
        else if (currentVersion == "lite")
        {
            // Lite → Full만 업그레이드 가능
            ChoiceManager.Instance.ShowChoice(3, "I01_installer_server_type_check_full");  // <Full(약 16GB)>, <각 버전에 대해 설명해줘>, <나중에 설치할게>
        }
        // else if (currentVersion == "full")  // 여기까지 오는 경우의 수 없음
        // {
        //     // Full → 이미 최신 (선택지 없음, 바로 안내만)
        //     yield return StartCoroutine(Scenario_I01_AlreadyLatest());
        // }
    }

    // Lite + Full 설명
    private IEnumerator Scenario_I01_0_InstallServerExplain_Lite()
    {
        float d1 = ScenarioUtil.Narration("I01_0_install_server_explain_1", "Lite Edition은 Sample Edition에서 다른 캐릭터와 대화할 수 있는 최소 기능만을 추가하여 Full Edition보다 가볍게 이용할 수 있는 edition이에요.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("I01_0_install_server_explain_2", "Full Edition은 기존 기능인 음성 인식을 더 높은 품질로도 사용할 수 있고, 화자 인식 같은 추가 기능도 제공되는 edition이에요.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d2);

        float d3 = ScenarioUtil.Narration("I01_0_install_server_explain_3", "또 저나 프라나짱 외에 다른 캐릭터와 대화하거나 여러 실험적 기능도 써볼 수 있어요!");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d3);

        float d4 = ScenarioUtil.Narration("I01_0_install_server_explain_4", "그리고 선생님 컴퓨터에서 AI를 직접 구동할 수 있는 기능도 제공돼요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d4);

        float d5 = ScenarioUtil.Narration("I01_0_install_server_explain_5", "그러면 인터넷 상태와 무관하게 안정적으로 대화할 수 있고, 설정에 따라 검열 없는 대화도 가능해요!");
        ScenarioUtil.ShowEmotion("><");
        yield return new WaitForSeconds(d5);

        float d6 = ScenarioUtil.Narration("I01_0_install_server_explain_6", "PC 성능이 좋을수록 더 뛰어난 AI 모델도 돌릴 수 있고요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d6);

        float d7 = ScenarioUtil.Narration("I01_0_install_server_explain_7", "설치를 진행해볼까요?");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d7);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(4, "I01_installer_server_type_check_lite");  // <Lite(약 2GB)>, <Full(약 16GB)>, <각 버전에 대해 설명해줘>, <나중에 설치할게>
    }

    // Full 설명 (이미 Lite는 설치되어있음)
    private IEnumerator Scenario_I01_0_InstallServerExplain_Full()
    {
        float d2 = ScenarioUtil.Narration("I01_0_install_server_explain_2", "Full Edition은 기존 기능인 음성 인식을 더 높은 품질로도 사용할 수 있고, 화자 인식 같은 추가 기능도 제공되는 edition이에요.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d2);

        float d3 = ScenarioUtil.Narration("I01_0_install_server_explain_3", "또 저나 프라나짱 외에 다른 캐릭터와 대화하거나 여러 실험적 기능도 써볼 수 있어요!");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d3);

        float d4 = ScenarioUtil.Narration("I01_0_install_server_explain_4", "그리고 선생님 컴퓨터에서 AI를 직접 구동할 수 있는 기능도 제공돼요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d4);

        float d5 = ScenarioUtil.Narration("I01_0_install_server_explain_5", "그러면 인터넷 상태와 무관하게 안정적으로 대화할 수 있고, 설정에 따라 검열 없는 대화도 가능해요!");
        ScenarioUtil.ShowEmotion("><");
        yield return new WaitForSeconds(d5);

        float d6 = ScenarioUtil.Narration("I01_0_install_server_explain_6", "PC 성능이 좋을수록 더 뛰어난 AI 모델도 돌릴 수 있고요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d6);

        float d7 = ScenarioUtil.Narration("I01_0_install_server_explain_7", "설치를 진행해볼까요?");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d7);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(4, "I01_installer_server_type_check_full");  // <Full(약 16GB)>, <Full 버전에 대해 설명해줘>, <나중에 설치할게>
    }

    private IEnumerator Scenario_I01_1_InstallServerLite()
    {
        float d1 = ScenarioUtil.Narration("I01_1_install_server_1", "설치 프로그램을 실행할게요.");
        ScenarioUtil.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);


        isListeningInstallStatus = true;  // 설치 감지 시작

        // InstallerManager를 통해 설치 실행
        InstallerManager.Instance.RunInstallerLite();

        EndInstaller();
    }

    private IEnumerator Scenario_I01_1_InstallServer()
    {
        float d1 = ScenarioUtil.Narration("I01_1_install_server_1", "설치 프로그램을 실행할게요.");
        ScenarioUtil.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        isListeningInstallStatus = true;  // 설치 감지 시작

        // InstallerManager를 통해 설치 실행
        InstallerManager.Instance.RunInstaller();

        EndInstaller();
    }

    private IEnumerator Scenario_I01_2_InstallLater()
    {
        float d1 = ScenarioUtil.Narration("I01_2_install_later_1", "언제든 필요하실 때 다시 말씀해주세요.");
        ScenarioUtil.ShowEmotion("relax");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        EndInstaller();
    }

    public IEnumerator Scenario_I01_3_AlreadyInstalled()
    {
        yield return new WaitForSeconds(1f);  // 기존 말풍선이 꺼지면서 첫 말풍선이 누락되는 것 방지

        float d1 = ScenarioUtil.Narration("I01_3_already_installed_1", "아, 이미 설치되어 있네요!");
        ScenarioUtil.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("I01_3_already_installed_2", "이제 서버를 시작하면 저와 대화할 수 있어요.");
        ScenarioUtil.ShowEmotion("><");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        EndInstaller();
    }

    public IEnumerator Scenario_I01_4_AlreadyRunning()
    {
        yield return new WaitForSeconds(1f);  // 기존 말풍선이 꺼지면서 첫 말풍선이 누락되는 것 방지

        float d1 = ScenarioUtil.Narration("I01_4_already_running_1", "설치 프로그램이 이미 실행 중이에요.");
        ScenarioUtil.ShowEmotion("confused");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("I01_4_already_running_2", "잠시만 기다려주세요.");
        ScenarioUtil.ShowEmotion("relax");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        EndInstaller();
    }

    public IEnumerator Scenario_I02_InstallComplete()
    {
        yield return new WaitForSeconds(1f);  // 이전 말풍선이 꺼지는 시간 확보

        float d1 = ScenarioUtil.Narration("I02_install_complete_1", "설치가 완료되었어요!");
        ScenarioUtil.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("I02_install_complete_2", "바로 서버를 시작해볼게요, 선생님!");
        ScenarioUtil.ShowEmotion("><");  // 아로나 표정
        yield return new WaitForSeconds(d2);

        // 서버 기동 (설치 프로그램 프로세스가 완전히 종료될 시간 확보)
        JarvisServerManager.Instance.ShutdownServer();
        yield return new WaitForSeconds(3f);
        JarvisServerManager.Instance.RunJarvisServerWithCheck();
    }

    /////////////////////////////////// I03 ///////////////////////////////////////////////
    // 무료 키 소진 또는 무료 서버 불안정 시 호출
    public IEnumerator Scenario_I03_FreeKeyExhausted()
    {
        StatusManager.Instance.isScenario = true;

        yield return new WaitForSeconds(0.5f);

        float d1 = ScenarioUtil.Narration("I03_free_key_exhausted_1", "선생님, 무료 서버 상태가 좋지 않은 것 같아요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("I03_free_key_exhausted_2", "로컬 서버를 설치하거나, 외부 플랫폼을 사용해보실래요?");
        ScenarioUtil.ShowEmotion("><");
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        // <로컬 서버 설치>, <외부 플랫폼 사용 (API 키 입력)>, <나중에할게>
        ChoiceManager.Instance.ShowChoice(3, "I03_free_key_exhausted");
    }
} 
