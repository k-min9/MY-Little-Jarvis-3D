using System;
using System.IO;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static DebugManager instance;
    public static DebugManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DebugManager>();
            }
            return instance;
        }
    }

    // 텍스트를 파일로 저장 (타임스탬프 포함)
    public void SaveTextToFile(string content, string fileName = "debug", string fileFolder = "debug")
    {
        // Unity 에디터 또는 DevMode에서만 실행
#if !UNITY_EDITOR
        if (DevManager.Instance == null || !DevManager.Instance.IsDevModeEnabled())
        {
            return;
        }
#endif

        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, fileFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullFileName = $"{fileName}_{timestamp}.txt";
            string filePath = Path.Combine(folderPath, fullFileName);

            File.WriteAllText(filePath, content);

            Debug.Log($"[DebugManager] File saved: {filePath}");
            Debug.Log($"[DebugManager] File size: {content.Length} characters");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DebugManager] Failed to save file: {ex.Message}");
        }
    }

    // 텍스트를 파일로 저장 (파일명 직접 지정)
    public void SaveTextToFileExact(string content, string exactFileName, string fileFolder = "debug")
    {
        // Unity 에디터 또는 DevMode에서만 실행
#if !UNITY_EDITOR
        if (DevManager.Instance == null || !DevManager.Instance.IsDevModeEnabled())
        {
            return;
        }
#endif

        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, fileFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            string filePath = Path.Combine(folderPath, exactFileName);

            File.WriteAllText(filePath, content);

            Debug.Log($"[DebugManager] File saved: {filePath}");
            Debug.Log($"[DebugManager] File size: {content.Length} characters");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DebugManager] Failed to save file: {ex.Message}");
        }
    }

    // 프롬프트 전용 저장 함수 (Gemini 프롬프트 디버깅용)
    public void SavePromptDebug(string promptContent, string charName = "unknown", string fileFolder = "debug")
    {
        // Unity 에디터 또는 DevMode에서만 실행
#if !UNITY_EDITOR
        if (DevManager.Instance == null || !DevManager.Instance.IsDevModeEnabled())
        {
            return;
        }
#endif

        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, fileFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"prompt_{charName}_{timestamp}.txt";
            string filePath = Path.Combine(folderPath, fileName);

            // 프롬프트 내용과 메타데이터 함께 저장
            string debugContent = $"=== Prompt Debug Log ===\n";
            debugContent += $"Character: {charName}\n";
            debugContent += $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            debugContent += $"Length: {promptContent.Length} characters\n";
            debugContent += $"========================\n\n";
            debugContent += promptContent;

            File.WriteAllText(filePath, debugContent);

            Debug.Log($"[DebugManager] Prompt saved: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DebugManager] Failed to save prompt: {ex.Message}");
        }
    }

    // 디버그 폴더 경로 반환
    public string GetDebugFolderPath(string fileFolder = "debug")
    {
        return Path.Combine(Application.persistentDataPath, fileFolder);
    }

    // 디버그 폴더 열기 (Windows 전용)
    public void OpenDebugFolder(string fileFolder = "debug")
    {
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, fileFolder);
            if (Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
                Debug.Log($"[DebugManager] Opening folder: {folderPath}");
            }
            else
            {
                Debug.LogWarning($"[DebugManager] Folder does not exist: {folderPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DebugManager] Failed to open folder: {ex.Message}");
        }
    }
}
