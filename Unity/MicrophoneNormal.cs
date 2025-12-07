using UnityEngine;
using UnityEngine.EventSystems;

// UI 버튼에서 MicrophoneManager를 호출하는 얇은 래퍼
public class MicrophoneNormal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isRecordingFromMouse = false; // 마우스로 시작했는지 구분

    private void Update()
    {
        // 마우스로 시작한 경우에만 마우스 버튼 체크
        if (isRecordingFromMouse && !Input.GetMouseButton(0))
        {
            StopRecording();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartRecording();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Update에서 처리하므로 여기서는 불필요
    }

    // UI 버튼용 (마우스로 시작)
    public void StartRecording()
    {
        if (MicrophoneManager.Instance != null)
        {
            isRecordingFromMouse = true;
            MicrophoneManager.Instance.StartRecording();
        }
        else
        {
            Debug.LogError("MicrophoneManager instance not found!");
        }
    }

    public void StopRecording()
    {
        if (MicrophoneManager.Instance != null)
        {
            isRecordingFromMouse = false;
            MicrophoneManager.Instance.StopRecording();
        }
    }
}

