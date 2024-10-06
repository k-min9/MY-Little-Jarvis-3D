using System.Diagnostics;
using UnityEngine;
using System.IO;
using System.Collections;

public class ServerManager : MonoBehaviour 
{
    private bool isConnected = false;

    // 싱글톤 인스턴스
    private static ServerManager instance;

    // 싱글톤 인스턴스에 접근하는 속성
    public static ServerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ServerManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // 서버 실행 함수
    public static Process StartServer()
    {
        // 이미 실행되고 있으면 안내 후 return
        if (IsJarvisServerRunning())
        {
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Already Served");
            return null;
        }
        
        string streamingAssetsPath = Application.streamingAssetsPath;  // StreamingAssets 폴더 경로
        string jarvisServerPath = Path.Combine(streamingAssetsPath, "jarvis_server_jp.exe");
        if (File.Exists(jarvisServerPath))
        {
            // jarvis_server_jp.exe 실행
            Process serverProcess = RunJarvisServer(jarvisServerPath);
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Init server...");

            // StartCoroutine(SendFirstPing()); // TODO : 서버 실행 후 핑 보내기 시작
        }
        else
        {
            // jarvis_server_jp.exe 파일이 없으면 Install_3D.exe 실행할지 여부 묻기
            string installPath = Path.Combine(streamingAssetsPath, "Install_3D.exe");
            if (!File.Exists(installPath))
            {
                if (AskUserToDownload("Install_3D.exe가 없습니다. 다운로드 하시겠습니까?"))
                {
                    DownloadInstall3D(); // 다운로드 함수 (미구현)
                }
                return null;
            }

            if (AskUserToExecute("Install_3D.exe를 실행하시겠습니까?"))
            {
                AskBalloonManager.Instance.SetCurrentQuestion("start_ai_server");  // InitializeQuestions에서 목록 확인(많아질 경우 Enum으로 관리)
                AskBalloonManager.Instance.ShowAskBalloon();  // 들어가기
            }
        }

        return null;
    }

    // jarvis_server.exe 핑 전송 코루틴
    private IEnumerator SendFirstPing()
    {
        int attempts = 0;
        while (attempts < 10)
        {
            // string response = APIManager.sendPing();
            string response = "ok";
            if (response == "ok")
            {
                isConnected = true;
                UnityEngine.Debug.Log("서버와 연결되었습니다.");
                yield break;
            }

            yield return new WaitForSeconds(5f); // 5초 대기
            attempts++;
        }

        if (!IsJarvisServerRunning())
        {
            UnityEngine.Debug.Log("jarvis_server.exe가 실행되어 있지 않습니다.");
        }
        else
        {
            UnityEngine.Debug.Log("서버 응답이 없습니다.");
        }
    }

    // Install_3D.exe 실행 함수
    public static Process RunInstallExe()
    {
        string streamingAssetsPath = Application.streamingAssetsPath;
        string installPath = Path.Combine(streamingAssetsPath, "Install_3D.exe");
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = installPath,
            UseShellExecute = false,
            CreateNoWindow = true // 콘솔 창을 표시하지 않음
        };
        Process installProcess = Process.Start(startInfo);
        // installProcess.WaitForExit();
        UnityEngine.Debug.Log("Install_3D.exe 실행 완료");
        return installProcess;
    }

    // jarvis_server_jp.exe 실행 함수 (콘솔 창 없이)
    public static Process RunJarvisServer(string serverExePath)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = serverExePath,
                UseShellExecute = false,
                CreateNoWindow = true, // 콘솔 창을 표시하지 않음
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process serverProcess = Process.Start(startInfo);
            UnityEngine.Debug.Log("jarvis_server_jp.exe 실행 완료");
            return serverProcess;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log($"jarvis_server_jp.exe 실행 중 오류 발생: {e.Message}");
            return null;
        }
    }

    // jarvis_server.exe 실행 중인지 확인하는 함수
    public static bool IsJarvisServerRunning()
    {
        return Process.GetProcessesByName("jarvis_server_jp").Length > 0 || Process.GetProcessesByName("jarvis_server").Length > 0;
    }

    // 유저에게 실행 여부 물어보는 함수 (가정: UI 팝업 또는 콘솔 입력 방식)
    private static bool AskUserToExecute(string message)
    {
        // 유저에게 물어보고 True/False 반환 (UI 구현 필요)
        UnityEngine.Debug.Log(message);
        return true; // 가정: 항상 실행한다고 가정
    }

    // 유저에게 다운로드 여부 물어보는 함수
    private static bool AskUserToDownload(string message)
    {
        // 유저에게 물어보고 True/False 반환 (UI 구현 필요)
        UnityEngine.Debug.Log(message);
        return true; // 가정: 항상 다운로드 한다고 가정
    }

    // Install_3D.exe 다운로드 함수 (미구현)
    private static void DownloadInstall3D()
    {
        // 다운로드 관련 로직을 여기에 추가
        UnityEngine.Debug.Log("Install_3D.exe 다운로드 중...");
    }

}
