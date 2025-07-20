using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Collections;

public class InstallerManager : MonoBehaviour
{
    private static InstallerManager instance;

    // 싱글톤 패턴으로 InstallerManager 인스턴스를 반환
    public static InstallerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InstallerManager>();
            }
            return instance;
        }
    }

    private Process installerProcess;  // 실행된 인스톨러 프로세스를 저장할 변수

    // jarvis_server.exe가 설치되어 있는지 확인
    public bool IsJarvisServerInstalled()
    {
        UnityEngine.Debug.Log("[Installer] IsJarvisServerInstalled() called");
        
        string executablePath = Application.dataPath;  // Unity 실행 파일이 있는 폴더 경로
        string jarvisServerPath = Path.Combine(Path.GetDirectoryName(executablePath), "server.exe");  // server.exe 파일 경로
        
        bool result = File.Exists(jarvisServerPath);  // 파일 존재 여부 확인
        UnityEngine.Debug.Log("[Installer] IsJarvisServerInstalled() result: " + result);
        return result;
    }

    // Install_Server.exe 프로세스가 현재 실행 중인지 확인
    public bool IsInstallerRunning()
    {
        UnityEngine.Debug.Log("[Installer] IsInstallerRunning() called");
        bool result = Process.GetProcessesByName("Install_Server").Length > 0;  // Install_Server 프로세스 검색
        UnityEngine.Debug.Log("[Installer] IsInstallerRunning() result: " + result);
        return result;
    }

    // 문답무용으로 가장 단순한 lite 버전 설치
    public void RunInstallerLite()
    {
        UnityEngine.Debug.Log("[Installer] RunInstallerLite() start");

        // Install_Server.exe 파일 확인
        string executablePath = Application.dataPath;  // Unity 실행 파일이 있는 폴더 경로
        string installerPath = Path.Combine(Path.GetDirectoryName(executablePath), "Install_Server_Lite.exe");  // Install_Server.exe 파일 경로

        if (File.Exists(installerPath))  // 파일 존재 여부 확인
        {
            installerProcess = RunInstallerProcess(installerPath);  // 인스톨러 프로세스 시작
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();  // 알림창 표시
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Running installer...");  // 알림 텍스트 수정
        }
        else
        {
            UnityEngine.Debug.LogError("[Installer] Installer not found: " + installerPath);  // 에러 로그 출력
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();  // 알림창 표시
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Installer not found");  // 알림 텍스트 수정
        }

        UnityEngine.Debug.Log("[Installer] RunInstallerLite() end");
    }

    // jarvis_server.exe가 없으면 Install_Server.exe를 실행
    public void RunInstaller()
    {
        UnityEngine.Debug.Log("[Installer] RunInstaller() start");

        // 이미 jarvis_server.exe가 있는지 확인
        if (IsJarvisServerInstalled())
        {
            UnityEngine.Debug.Log("[Installer] Jarvis server already installed, skipping installer");
            // ScenarioInstallerManager에서 이미 설치됨 음성 실행
            StartCoroutine(ScenarioInstallerManager.Instance.Scenario_I01_3_AlreadyInstalled());
            return;  // 이미 설치되어 있으면 함수 종료
        }

        // 이미 인스톨러가 실행 중인지 확인
        if (IsInstallerRunning())
        {

            UnityEngine.Debug.Log("[Installer] Launch aborted: installer already running");
            // ScenarioInstallerManager에서 이미 실행 중 음성 실행
            StartCoroutine(ScenarioInstallerManager.Instance.Scenario_I01_4_AlreadyRunning());
            return;  // 이미 실행 중이면 함수 종료
        }

        // Install_Server.exe 파일 확인
        string executablePath = Application.dataPath;  // Unity 실행 파일이 있는 폴더 경로
        string installerPath = Path.Combine(Path.GetDirectoryName(executablePath), "Install_Server.exe");  // Install_Server.exe 파일 경로

        if (File.Exists(installerPath))  // 파일 존재 여부 확인
        {
            installerProcess = RunInstallerProcess(installerPath);  // 인스톨러 프로세스 시작
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();  // 알림창 표시
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Running installer...");  // 알림 텍스트 수정
        }
        else
        {
            UnityEngine.Debug.LogError("[Installer] Installer not found: " + installerPath);  // 에러 로그 출력
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();  // 알림창 표시
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Installer not found");  // 알림 텍스트 수정
        }

        UnityEngine.Debug.Log("[Installer] RunInstaller() end");
    }

    // 실제 Install_Server.exe 프로세스를 시작
    public Process RunInstallerProcess(string exePath)
    {
        UnityEngine.Debug.Log("[Installer] RunInstallerProcess() start: " + exePath);

        ProcessStartInfo startInfo = new ProcessStartInfo  // 프로세스 시작 정보 설정
        {
            FileName = exePath,  // 실행할 파일 경로
            UseShellExecute = true,  // 셸을 통해 실행
            Verb = "runas", // 관리자 권한으로 실행
            CreateNoWindow = false  // 창 표시 (true면 숨김)
        };

        Process process = Process.Start(startInfo);  // 프로세스 시작

        UnityEngine.Debug.Log("[Installer] RunInstallerProcess() end");
        return process;
    }

    // 실행 중인 Install_Server.exe 프로세스를 종료
    public void ShutdownInstaller()
    {
        UnityEngine.Debug.Log("[Installer] ShutdownInstaller() start");

        if (installerProcess != null && !installerProcess.HasExited)  // 프로세스가 존재하고 아직 실행 중인지 확인
        {
            try
            {
                installerProcess.Kill();  // 프로세스 강제 종료
                UnityEngine.Debug.Log("[Installer] Installer process killed.");
            }
            catch
            {
                UnityEngine.Debug.LogWarning("[Installer] Failed to kill installer process.");  // 종료 실패 시 경고 로그
            }
        }
        else
        {
            UnityEngine.Debug.Log("[Installer] No running installer process to kill.");  // 종료할 프로세스가 없음
        }

        UnityEngine.Debug.Log("[Installer] ShutdownInstaller() end");
    }

    // 애플리케이션 종료 시 installer 프로세스도 함께 종료
    private void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("[Installer] OnApplicationQuit() -> ShutdownInstaller()");
        ShutdownInstaller();  // 인스톨러 프로세스 종료
    }
} 