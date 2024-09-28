using System.Diagnostics;
using UnityEngine;
using System.IO;

/**
외부프로그램을 실행하게 하는 코드
*/
public class ServerManager : MonoBehaviour {
    public static Process StartServer()
    {
        // 실행 파일 경로 (Test용)
        string fullPath = Path.Combine(Application.streamingAssetsPath, "Install.exe");

        // 서버를 백그라운드에서 실행
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = false,
            CreateNoWindow = true // 창을 숨기고 실행
        };

        Process process = Process.Start(startInfo);
        // process.WaitForExit();

        return process;
    }
}
