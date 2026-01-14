using UnityEngine;
using UnityEngine.EventSystems;

// Operator Portrait용 클릭 핸들러
// 기존 ClickHandler.cs의 핵심 로직을 복제하여 동일한 상호작용 제공
// 좌클릭 → 채팅 시작 (ChatBalloon 표시)
public class OperatorClickHandler : MonoBehaviour, IPointerClickHandler
{
    // 포인터 클릭 이벤트 처리
    public void OnPointerClick(PointerEventData eventData)
    {
        // Operator 모드가 아니면 무시
        if (!ChatModeManager.Instance.IsOperatorMode())
        {
            return;
        }

        // 드래그 중이면 무시
        if (StatusManager.Instance.IsDragging)
        {
            return;
        }

        // 좌클릭 처리
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
        // 중앙클릭 (필요시 추가)
        // if (eventData.button == PointerEventData.InputButton.Middle)
        // {
        //     HandleMiddleClick();
        // }
    }

    // 좌클릭 처리 - 채팅 시작
    // ClickHandler.HandleLeftClick() 로직 복제
    private void HandleLeftClick()
    {
        Debug.Log("OperatorClickHandler.HandleLeftClick() 호출");
#if UNITY_EDITOR
        // 에디터에서 baseUrl이 비어있으면 기본값 설정
        if (string.IsNullOrEmpty(ServerManager.Instance.baseUrl))
        {
            ServerManager.Instance.baseUrl = "http://127.0.0.1:5000";
        }
#endif

        // baseUrl이 없으면 서버 관련 처리
        if (string.IsNullOrEmpty(ServerManager.Instance.baseUrl))
        {
            // 앱 실행 후 15초 이내면 무시
            if (Time.time <= 15f) return;
            
            // 시나리오 진행 중이면 무시
            if (StatusManager.Instance.isScenario) return;

            // 서버가 실행 중이 아니면 시작 요청
            if (!InstallStatusManager.Instance.IsServerRunning())
            {
                StartCoroutine(ScenarioCommonManager.Instance.Scenario_C02_AskToStartServer());
            }
            return;
        }

        // 정상 대화 모드
        StatusManager.Instance.isAnswering = false;
        VoiceManager.Instance.ResetAudio();

        // ChatBalloon 위치를 Operator Portrait로 설정
        if (OperatorManager.Instance.portraitTransform != null)
        {
            ChatBalloonManager.Instance.characterTransform = OperatorManager.Instance.portraitTransform;
        }

        // 채팅 입력창 토글 (Operator 모드에서는 항상 하단)
        ChatBalloonManager.Instance.ToggleChatBalloonBottom();
    }
}
