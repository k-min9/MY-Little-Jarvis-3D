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

    public bool IsJarvisServerRunning()
    {
        UnityEngine.Debug.Log("[Jarvis] IsJarvisServerRunning() called");
        bool result = Process.GetProcessesByName("jarvis_serverXXX").Length > 0;
        UnityEngine.Debug.Log("[Jarvis] IsJarvisServerRunning() result: " + result);
        return result;
    }

    public void RunJarvisServer()
    {
        UnityEngine.Debug.Log("[Jarvis] RunJarvisServer() start");

        // 기존에 켜져있는거 있는지 확인
        if (IsJarvisServerRunning())
        {
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Already Served");
            UnityEngine.Debug.Log("[Jarvis] Launch aborted: already running");
            return;
        }

        // 파일 확인
        string streamingAssetsPath = Application.streamingAssetsPath;
        string executablePath = Application.dataPath;
        string jarvisServerPath = Path.Combine(Path.GetDirectoryName(executablePath), "jarvis_server.exe");

        if (File.Exists(jarvisServerPath))
        {
            jarvisProcess = RunJarvisServerProcess(jarvisServerPath);
            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Init server...");
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

    // /shutdown 방식 vs Process 직접 종료에서 후자 선택
    public void ShutdownServer()
    {
        UnityEngine.Debug.Log("[Jarvis] ShutdownServer() start");

        if (jarvisProcess != null && !jarvisProcess.HasExited)
        {
            try
            {
                jarvisProcess.Kill();
                UnityEngine.Debug.Log("[Jarvis] Server process killed.");
            }
            catch
            {
                UnityEngine.Debug.LogWarning("[Jarvis] Failed to kill process.");
            }
        }
        else
        {
            UnityEngine.Debug.Log("[Jarvis] No running server process to kill.");
        }

        UnityEngine.Debug.Log("[Jarvis] ShutdownServer() end");
    }

    private void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("[Jarvis] OnApplicationQuit() -> ShutdownServer()");
        ShutdownServer();
    }
}
