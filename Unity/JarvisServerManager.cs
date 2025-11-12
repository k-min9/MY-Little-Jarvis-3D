using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class JarvisServerManager : MonoBehaviour
{
    private static JarvisServerManager instance;

    public static JarvisServerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JarvisServerManager>();
            }
            return instance;
        }
    }

    private const int ServerPort = 5000;
    private Process jarvisProcess;  // 실행된 서버 프로세스를 저장할 변수
    private int llmProcessPid = -1;  // LLM 서버 프로세스 PID

    private void Awake()
    {
        UnityEngine.Debug.Log("[Jarvis] JarvisServerManager initialized");
        // 자동 시작 제거 - InstallStatusManager에서 수동으로 호출
    }

    // InstallStatusManager에서 호출할 초기화 함수
    public void InitializeForLiteOrFull()
    {
#if !UNITY_EDITOR
        UnityEngine.Debug.Log("[Jarvis] InitializeForLiteOrFull() called");
        if (SettingManager.Instance.settings.isStartServerOnInit)
        {
            RunJarvisServerWithCheck();
        }
#endif
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
            UnityEngine.Debug.Log("[Jarvis] 서버 연결 성공 - StatusManager 업데이트");
            StartCoroutine(ScenarioCommonManager.Instance.Run_C01_ServerStarted());
        }
        else
        {
            StatusManager.Instance.IsServerConnected = false;
            UnityEngine.Debug.LogWarning("[Jarvis] 서버 응답 없음 - C01 호출 안됨");
        }
    }


    public bool IsJarvisServerRunning()
    {
        UnityEngine.Debug.Log("[Jarvis] IsJarvisServerRunning() called");
        bool result = Process.GetProcessesByName("server").Length > 0;
        UnityEngine.Debug.Log("[Jarvis] IsJarvisServerRunning() result: " + result);
        return result;
    }

    public void RunJarvisServer()
    {
        UnityEngine.Debug.Log("[Jarvis] RunJarvisServer() start");

        // 기존에 켜져있는거 있는지 확인
        if (IsJarvisServerRunning())
        {
            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Already Served");
            UnityEngine.Debug.Log("[Jarvis] Launch aborted: already running");
            return;
        }

        // 파일 확인
        string streamingAssetsPath = Application.streamingAssetsPath;
        string executablePath = Application.dataPath;
        string jarvisServerPath = Path.Combine(Path.GetDirectoryName(executablePath), "server.exe");

        if (File.Exists(jarvisServerPath))
        {
            jarvisProcess = RunJarvisServerProcess(jarvisServerPath);
            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Init server...");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[Jarvis] Executable not found: " + jarvisServerPath);
        }

        UnityEngine.Debug.Log("[Jarvis] RunJarvisServer() end");
    }

    public Process RunJarvisServerProcess(string exePath)
    {
        UnityEngine.Debug.Log("[Jarvis] RunJarvisServerProcess() start: " + exePath);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--is_unity",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        Process process = Process.Start(startInfo);

        UnityEngine.Debug.Log("[Jarvis] RunJarvisServerProcess() end");
        return process;
    }

    private IEnumerator CheckHealth()
    {
        UnityEngine.Debug.Log("[Jarvis] CheckHealth() start");

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
                    UnityEngine.Debug.Log("[Jarvis] Health check OK");
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Server OK");
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
        }

        UnityEngine.Debug.LogWarning("[Jarvis] Health check failed");
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Server Fail");

        UnityEngine.Debug.Log("[Jarvis] CheckHealth() end");
    }

    // 서버 로드 후 받아온 LLM PID 정보를 저장
    public void SetProcessInfo(int llm_process_pid)
    {
        llmProcessPid = llm_process_pid;
        UnityEngine.Debug.Log($"[Jarvis] Process info stored - LLM Process PID: {llmProcessPid}");
    }

    // PID로 프로세스를 종료하는 헬퍼 메서드
    private void KillProcessByPid(int pid, string processName)
    {
        if (pid <= 0) return;

        try
        {
            Process process = Process.GetProcessById(pid);
            if (process != null && !process.HasExited)
            {
                process.Kill();
                UnityEngine.Debug.Log($"[Jarvis] {processName} process (PID: {pid}) killed successfully");
            }
        }
        catch (System.ArgumentException)
        {
            UnityEngine.Debug.Log($"[Jarvis] {processName} process (PID: {pid}) already terminated");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"[Jarvis] Failed to kill {processName} process (PID: {pid}): {e.Message}");
        }
    }

    // /shutdown 방식 vs Process 직접 종료에서 후자 선택
    public void ShutdownServer()
    {
        UnityEngine.Debug.Log("[Jarvis] ShutdownServer() start");

        // 1. jarvisProcess 종료
        if (jarvisProcess != null && !jarvisProcess.HasExited)
        {
            try
            {
                jarvisProcess.Kill();
                UnityEngine.Debug.Log("[Jarvis] Main server process killed");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Jarvis] Failed to kill main process: {e.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.Log("[Jarvis] No running main server process to kill.");
        }

        // 2. LLM 프로세스 종료
        KillProcessByPid(llmProcessPid, "LLM");

        // 3. StatusManager 업데이트 및 PID 초기화
        StatusManager.Instance.IsServerConnected = false;
        llmProcessPid = -1;
        UnityEngine.Debug.Log("[Jarvis] All processes terminated - StatusManager 업데이트");

        UnityEngine.Debug.Log("[Jarvis] ShutdownServer() end");
    }

    private void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("[Jarvis] OnApplicationQuit() -> ShutdownServer()");
        ShutdownServer();
    }
}
