using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

// 창 포커스, 프로세스 실행 등 시스템 제어 액션 클래스
public class ApiAgentFunctionSystemAction : MonoBehaviour
{
    private static ApiAgentFunctionSystemAction instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionSystemAction Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<ApiAgentFunctionSystemAction>();
            }
            return instance;
        }
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // 윈도우 타이틀로 창을 찾아 포커스 활성화
    public bool FocusWindow(string windowTitle)
    {
        IntPtr hWnd = FindWindow(null, windowTitle);

        if (hWnd != IntPtr.Zero)
        {
            // 창을 찾았을 때 포커스 설정
            SetForegroundWindow(hWnd);
            Debug.Log($"[ApiAgentFunctionSystemAction] 창 포커스 성공: {windowTitle}");
            return true;
        }
        else
        {
            // 창을 찾지 못했을 때 경고
            Debug.LogWarning($"[ApiAgentFunctionSystemAction] 포커스할 창을 찾을 수 없음: {windowTitle}");
            return false;
        }
    }

    // 지정된 프로그램 실행 (예: notepad.exe, write.exe 등)
    public System.Diagnostics.Process RunProcess(string fileName)
    {
        try
        {
            System.Diagnostics.Process proc = System.Diagnostics.Process.Start(fileName);
            Debug.Log($"[ApiAgentFunctionSystemAction] 프로세스 실행 성공: {fileName}");
            return proc;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ApiAgentFunctionSystemAction] 프로세스 실행 실패: {fileName}\n{e.Message}");
            return null;
        }
    }

    // Process 객체의 MainWindowHandle로 포커스 (언어 독립적)
    public bool FocusProcess(System.Diagnostics.Process proc)
    {
        if (proc == null)
        {
            Debug.LogWarning("[ApiAgentFunctionSystemAction] FocusProcess: proc가 null입니다.");
            return false;
        }

        // 최신 창 핸들 정보 갱신
        proc.Refresh();

        if (proc.MainWindowHandle != IntPtr.Zero)
        {
            // 핸들이 유효하면 바로 포커스
            SetForegroundWindow(proc.MainWindowHandle);
            Debug.Log($"[ApiAgentFunctionSystemAction] 프로세스 포커스 성공: {proc.ProcessName} (Handle: {proc.MainWindowHandle})");
            return true;
        }
        else
        {
            // 핸들이 아직 없을 때 경고
            Debug.LogWarning($"[ApiAgentFunctionSystemAction] MainWindowHandle이 아직 없음: {proc.ProcessName}");
            return false;
        }
    }

    // 프로그램 실행 후 windowTitle로 포커스될 때까지 대기
    public void RunProcessAndFocus(string fileName, string windowTitle, int waitMs = 1500)
    {
        RunProcess(fileName);

        // 프로그램이 뜰 때까지 대기
        Thread.Sleep(waitMs);

        // 포커스 시도
        bool focused = FocusWindow(windowTitle);

        if (!focused)
        {
            // 한 번 더 대기 후 재시도
            Thread.Sleep(500);
            FocusWindow(windowTitle);
        }
    }

    // 클립보드 텍스트 쓰기
    public void WriteClipboardText(string text)
    {
        GUIUtility.systemCopyBuffer = text;
        Debug.Log($"[ApiAgentFunctionSystemAction] 클립보드 텍스트 복사 완료: {text}");
    }

    // 클립보드 텍스트 읽기
    public string ReadClipboardText()
    {
        string text = GUIUtility.systemCopyBuffer;
        Debug.Log($"[ApiAgentFunctionSystemAction] 클립보드 텍스트 읽기 완료: {text}");
        return text;
    }
#else
    public bool FocusWindow(string windowTitle)
    {
        Debug.LogWarning("[ApiAgentFunctionSystemAction] Windows 환경에서만 지원됩니다.");
        return false;
    }

    public System.Diagnostics.Process RunProcess(string fileName)
    {
        Debug.LogWarning("[ApiAgentFunctionSystemAction] Windows 환경에서만 지원됩니다.");
        return null;
    }

    public bool FocusProcess(System.Diagnostics.Process proc)
    {
        Debug.LogWarning("[ApiAgentFunctionSystemAction] Windows 환경에서만 지원됩니다.");
        return false;
    }

    public void RunProcessAndFocus(string fileName, string windowTitle, int waitMs = 1500)
    {
        Debug.LogWarning("[ApiAgentFunctionSystemAction] Windows 환경에서만 지원됩니다.");
    }
#endif
}
