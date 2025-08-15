using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
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

    private string installStatusPath;

    private void Start()
    {
        // 설치 상태 파일 경로 계산
        string executablePath = Application.dataPath;
        installStatusPath = Path.Combine(Path.GetDirectoryName(executablePath), "config/install_status.json");
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


    [Serializable]
    public class InstallStatusData
    {
        public string version;  // "lite" or "full"
    }

    private void CheckInstallStatus()
    {
        if (File.Exists(installStatusPath))
        {
            Debug.Log("설치 상태 파일이 감지되었습니다.");

            string json = File.ReadAllText(installStatusPath);
            InstallStatusData status = JsonUtility.FromJson<InstallStatusData>(json);

            if (status != null && !string.IsNullOrEmpty(status.version))
            {
                if (status.version == "lite" || status.version == "full")
                {
                    Debug.Log($"설치 완료: {status.version} 버전");

                    // 안내 메시지 출력
                    StartCoroutine(Scenario_I02_InstallComplete());

                    // 리스닝 중단
                    isListeningInstallStatus = false;

                    // 설정 값 설정
                    SettingManager.Instance.SetInstallStatus();

                    // 서버 구동
                }
                else
                {
                    Debug.LogWarning($"알 수 없는 설치 버전: {status.version}");
                }
            }
            else
            {
                Debug.LogWarning("설치 상태 정보를 읽을 수 없습니다.");
            }
        }
    }

    // 인스톨러 시나리오 시작
    public void StartInstaller()
    {
        Debug.Log("인스톨러 시나리오 시작됨");
        StatusManager.Instance.isScenario = true;

        StartCoroutine(Scenario_I01_EntryCondition());
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

            case "I01_installer_server_type_check":
                // <Lite(약 2GB)>
                if (index == 0)
                {
                    StartCoroutine(Scenario_I01_1_InstallServerLite());
                }
                // <Full(약 16GB)>
                else if (index == 1)
                {
                    StartCoroutine(Scenario_I01_1_InstallServer());
                }
                // <각 버전에 대해 설명해줘>
                else if (index == 2)
                {
                    StartCoroutine(Scenario_I01_0_InstallServerExplain());
                }
                // <나중에 설치할게>
                else if (index == 3)
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

    /////////////////////////////////// I01 ///////////////////////////////////////////////
    private IEnumerator Scenario_I01_EntryCondition()
    {
        float d1 = ScenarioUtil.Narration("I01_entry_condition_1", "선생님. 대화를 위한 기본적인 파일이 설치되어 있지 않아요.");
        ScenarioUtil.ShowEmotion("confused");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("I01_entry_condition_2", "설치를 위한 프로그램을 구동해도 될까요?");
        ScenarioUtil.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d2);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(2, "I01_installer_check");  // <응>, <아니>
    }

    private IEnumerator Scenario_I01_0_InstallServerType()
    {
        float d1 = ScenarioUtil.Narration("I01_1_install_server_type_1", "Lite 버전과 Full 버전 어느쪽으로 설치할까요?");
        ScenarioUtil.ShowEmotion("star");  // 아로나만 표정
        yield return new WaitForSeconds(d1);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(4, "I01_installer_server_type_check");  // <Lite(약 2GB)>, <Full(약 16GB)>, <각 버전에 대해 설명해줘>, <나중에 설치할게>

        EndInstaller();
    }

    private IEnumerator Scenario_I01_0_InstallServerExplain()
    {
        float d1 = ScenarioUtil.Narration("I01_0_install_server_explain_1", "Lite는 음성 인식 같은 기본 기능만 설치돼요. 연산은 외부 플랫폼에 맡기기 때문에 설치도 빠르고 용량도 가벼워요.");
        ScenarioUtil.ShowEmotion("smile");
        yield return new WaitForSeconds(d1);

        float d2 = ScenarioUtil.Narration("I01_0_install_server_explain_2", "Full은 Lite의 기능이 제공되고, AI 연산도 선생님의 컴퓨터에서 직접 처리해요. 외부 서버 상태에 영향을 받지 않고 안정적인 품질을 기대하실 수 있어요.");
        ScenarioUtil.ShowEmotion("relax");
        yield return new WaitForSeconds(d2);

        float d3 = ScenarioUtil.Narration("I01_0_install_server_explain_3", "그만큼 용량도 크고, 컴퓨터 성능에 따라 답변 속도가 달라질 수 있어요.");
        ScenarioUtil.ShowEmotion("confused");
        yield return new WaitForSeconds(d3);

        float d4 = ScenarioUtil.Narration("I01_0_install_server_explain_4", "두 버전은 나중에 언제든지 바꾸실 수 있으니까, 편하게 골라주세요.");
        ScenarioUtil.ShowEmotion("star");
        yield return new WaitForSeconds(d4);

        yield return new WaitForSeconds(0.2f);
        ChoiceManager.Instance.ShowChoice(4, "I01_installer_server_type_check");  // <Lite(약 2GB)>, <Full(약 16GB)>, <각 버전에 대해 설명해줘>, <나중에 설치할게>
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

        // InstallerManager를 통해 설치 실행
        InstallerManager.Instance.RunInstaller();

        isListeningInstallStatus = true;  // 설치 감지 시작

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

        // 서버 기동
        JarvisServerManager.Instance.RunJarvisServerWithCheck();
    }
} 