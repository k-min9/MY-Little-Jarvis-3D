using System;
using System.Collections;
using System.IO;
using UnityEngine;

// 스크린샷 캡처 및 클립보드 복사 액션 클래스
public class ApiAgentFunctionScreenshotAction : MonoBehaviour
{
    private static ApiAgentFunctionScreenshotAction instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionScreenshotAction Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<ApiAgentFunctionScreenshotAction>();
            }
            return instance;
        }
    }

    // 전체 화면 캡처 후 콜백 반환
    public void CaptureScreen(Action<byte[]> onCaptured)
    {
        StartCoroutine(CaptureScreenCoroutine(onCaptured));
    }

    private IEnumerator CaptureScreenCoroutine(Action<byte[]> onCaptured)
    {
        // 렌더링 끝날 때까지 대기
        yield return new WaitForEndOfFrame();

        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();

        byte[] imageBytes = screenTexture.EncodeToPNG();
        Destroy(screenTexture);

        Debug.Log($"[ApiAgentFunctionScreenshotAction] 스크린샷 캡처 완료 ({imageBytes.Length} bytes)");
        onCaptured?.Invoke(imageBytes);
    }

    // 화면 캡처 후 지정된 경로에 저장
    public void CaptureAndSave(string path)
    {
        CaptureScreen((bytes) =>
        {
            if (bytes != null && bytes.Length > 0)
            {
                File.WriteAllBytes(path, bytes);
                Debug.Log($"[ApiAgentFunctionScreenshotAction] 스크린샷 저장 완료: {path}");
            }
            else
            {
                Debug.LogError("[ApiAgentFunctionScreenshotAction] 스크린샷 데이터가 비어 있습니다.");
            }
        });
    }

    // 파일 경로의 이미지를 시스템 클립보드에 복사
    public void CopyScreenshotToClipboard(string imagePath)
    {
#if UNITY_STANDALONE_WIN
        if (File.Exists(imagePath))
        {
            // ClipboardManager 연동이 필요할 수 있으나 기본 로그 처리
            Debug.Log($"[ApiAgentFunctionScreenshotAction] 클립보드 복사 모의 완료: {imagePath}");
        }
        else
        {
            Debug.LogWarning($"[ApiAgentFunctionScreenshotAction] 클립보드 복사 실패 (파일 없음): {imagePath}");
        }
#else
        Debug.LogWarning("[ApiAgentFunctionScreenshotAction] 클립보드 기능은 Windows 환경에서만 지원됩니다.");
#endif
    }
}
