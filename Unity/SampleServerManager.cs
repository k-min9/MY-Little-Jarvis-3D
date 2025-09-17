using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SampleServerManager : MonoBehaviour
{
    private static SampleServerManager instance;

    public static SampleServerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SampleServerManager>();
            }
            return instance;
        }
    }

    private const int ServerPort = 5000;
    private Process jarvisProcess;  // 실행된 서버 프로세스를 저장할 변수

    private void Start()
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] SampleServerManager initialized");
        // 자동 시작 제거 - InstallStatusManager에서 수동으로 호출
    }

    /// <summary>
    /// InstallStatusManager에서 호출할 초기화 함수
    /// </summary>
    public void InitializeForSample()
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] SettingManager Fixed");
        SettingManager.Instance.settings.server_type_idx = 2;
        SettingManager.Instance.settings.isShowChatBoxOnClick = true;
        UnityEngine.Debug.Log("[Jarvis_Sample] InitializeForSample() called");
        RunJarvisServerWithCheck();
    }

    public void RunJarvisServerWithCheck()
    {
        RunJarvisServer();
        StartCoroutine(CheckHealthAndNotify());
    }

    private IEnumerator CheckHealthAndNotify()
    {
        string url = $"http://127.0.0.1:{ServerPort}/health";
        float timeout = 5f;
        float timer = 0f;
        bool isAlive = false;

        while (timer < timeout)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    isAlive = true;
                    break;
                }
            }

            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
        }

        if (isAlive)
        {
            StatusManager.Instance.IsServerConnected = true;
            UnityEngine.Debug.Log("[Jarvis_Sample] 서버 연결 성공 - StatusManager 업데이트");
            StartCoroutine(ScenarioCommonManager.Instance.Run_C01_ServerStarted());
        }
        else
        {
            StatusManager.Instance.IsServerConnected = false;
            UnityEngine.Debug.LogWarning("[Jarvis_Sample] 서버 응답 없음 - C01 호출 안됨");
        }
    }


    public bool IsJarvisServerRunning()
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] IsJarvisServerRunning() called");
        bool result = Process.GetProcessesByName("server").Length > 0;
        UnityEngine.Debug.Log("[Jarvis_Sample] IsJarvisServerRunning() result: " + result);
        return result;
    }

    public void RunJarvisServer()
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] RunJarvisServer() start");

        // 기존에 켜져있는거 있는지 확인
        if (IsJarvisServerRunning())
        {
            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Already Served");
            UnityEngine.Debug.Log("[Jarvis_Sample] Launch aborted: already running");
            return;
        }

        // 파일 확인
        string streamingAssetsPath = Application.streamingAssetsPath;
        string executablePath = Application.dataPath;
        string jarvisServerPath = Path.Combine(Path.GetDirectoryName(executablePath), "server_sample.exe");

        if (File.Exists(jarvisServerPath))
        {
            jarvisProcess = RunJarvisServerProcess(jarvisServerPath);
            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Init server...");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[Jarvis_Sample] Executable not found: " + jarvisServerPath);
        }

        UnityEngine.Debug.Log("[Jarvis_Sample] RunJarvisServer() end");
    }

    public Process RunJarvisServerProcess(string exePath)
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] RunJarvisServerProcess() start: " + exePath);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            // Arguments = "--is_unity",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        Process process = Process.Start(startInfo);

        UnityEngine.Debug.Log("[Jarvis_Sample] RunJarvisServerProcess() end");
        return process;
    }

    private IEnumerator CheckHealth()
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] CheckHealth() start");

        string url = $"http://127.0.0.1:{ServerPort}/health";
        float timeout = 5f;
        float timer = 0f;

        while (timer < timeout)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    SettingManager.Instance.settings.server_type_idx = 2;
                    UnityEngine.Debug.Log("[Jarvis_Sample] Health check OK");
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Server OK");
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
        }

        UnityEngine.Debug.LogWarning("[Jarvis_Sample] Health check failed");
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Server Fail");

        UnityEngine.Debug.Log("[Jarvis_Sample] CheckHealth() end");
    }

    // /shutdown 방식 vs Process 직접 종료에서 후자 선택
    public void ShutdownServer()
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] ShutdownServer() start");

        if (jarvisProcess != null && !jarvisProcess.HasExited)
        {
            try
            {
                jarvisProcess.Kill();
                StatusManager.Instance.IsServerConnected = false;
                UnityEngine.Debug.Log("[Jarvis_Sample] Server process killed - StatusManager 업데이트");
            }
            catch
            {
                UnityEngine.Debug.LogWarning("[Jarvis_Sample] Failed to kill process.");
            }
        }
        else
        {
            UnityEngine.Debug.Log("[Jarvis_Sample] No running server process to kill.");
        }

        UnityEngine.Debug.Log("[Jarvis_Sample] ShutdownServer() end");
    }

    private void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("[Jarvis_Sample] OnApplicationQuit() -> ShutdownServer()");
        ShutdownServer();
    }
}
