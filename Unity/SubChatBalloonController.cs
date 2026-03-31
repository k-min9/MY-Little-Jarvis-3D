using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 개별 서브 캐릭터의 말풍선 UI 상태와 위치를 갱신·추적하는 컴포넌트
public class SubChatBalloonController : MonoBehaviour
{
    private GameObject balloonInstance;
    private RectTransform balloonTransform;
    private TMP_InputField inputField;
    
    private RectTransform characterTransform;
    private SubStatusManager subStatusManager;

    public void Init(GameObject prefab, RectTransform charTransform)
    {
        characterTransform = charTransform;
        subStatusManager = GetComponent<SubStatusManager>();

        Canvas mainCanvas = FindObjectOfType<Canvas>();
        balloonInstance = Instantiate(prefab, mainCanvas.transform);
        balloonTransform = balloonInstance.GetComponent<RectTransform>();
        
        // 원본과 이름 구분
        balloonInstance.name = "SubChatBalloon_" + gameObject.name;

        // InputField 확보
        inputField = balloonInstance.GetComponentInChildren<TMP_InputField>();

        // ChatHandler를 찾아 자신을 부모 컨트롤러로 등록 (메인 ChatBalloonManager 대신 이곳의 HideChatBalloon 호출토록)
        ChatHandler chatHandler = balloonInstance.GetComponentInChildren<ChatHandler>();
        if (chatHandler != null)
        {
            chatHandler.subController = this;
        }

        // 초기 설정: 사이즈는 메인 ChatBalloon의 topWidth, topHeight (Inspector 설정치) 참고하여 대략 720x480 등 유니티 UI 기본값 적용
        // 원본을 그대로 복사하므로 RectTransform 사이즈가 유지됨. mode는 "char" 고정이라 가정.

        // Z축 앞으로 튀어나오게 조정 (레이어 겹침 방지)
        Vector3 pos3D = balloonTransform.anchoredPosition3D;
        pos3D.z = 0f;
        balloonTransform.anchoredPosition3D = pos3D;

        balloonInstance.SetActive(false);
    }

    private void Update()
    {
        if (balloonInstance == null || subStatusManager == null) return;

        // 비활성화 상태면 추적 안 함
        if (!balloonInstance.activeSelf) return;

        // ESC로 말풍선 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HideChatBalloon();
            return;
        }

        // 메인의 로직에 맞춰, 캐릭터가 피킹당하거나 이동 중일 때 숨김 처리
        if (subStatusManager.isPicking || subStatusManager.isWalking)
        {
            HideChatBalloon();
            return;
        }

        // 활성화 상태면 위치 지속 추적
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (balloonTransform == null || characterTransform == null) return;

        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // 실제 스케일 비율 = 현재 localScale.y / 고유 스케일(initLocalScale)
        // (SettingManager의 글로벌 퍼센트 설정과 휠 스크롤 퍼센트가 모두 반영된 값)
        float scaleRatio = SettingManager.Instance.settings.char_size / 100f; // 기본값
        CharAttributes attrs = characterTransform.GetComponent<CharAttributes>();
        if (attrs != null && attrs.initLocalScale > 0)
        {
            scaleRatio = characterTransform.localScale.y / attrs.initLocalScale;
        }
        
        // 캔버스 경계 확인 및 조정
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        float charPositionX = charPosition.x;
        
        if (mainCanvas != null)
        {
            RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
            float leftBound = -canvasRect.rect.width / 2; // 캔버스 왼쪽 끝
            float rightBound = canvasRect.rect.width / 2; // 캔버스 오른쪽 끝
            
            // 실제 채팅 말풍선 넓이 기준의 절반 + 여백 적용
            float padding = (balloonTransform.rect.width > 0 ? balloonTransform.rect.width / 2 : 360f) + 20f;
            charPositionX = Mathf.Clamp(charPosition.x, leftBound + padding, rightBound - padding);
        }

        // y 오프셋: 200 * 동적 스케일 비율 + 100
        balloonTransform.anchoredPosition = new Vector2(charPositionX, charPosition.y + (200f * scaleRatio) + 100f);
    }

    public void ShowChatBalloon()
    {
        if (balloonInstance == null) return;
        
        // 만약 Answer(서브) 말풍선이 떠있다면 닫기
        SubAnswerBalloonSimpleController ansController = GetComponent<SubAnswerBalloonSimpleController>();
        if (ansController != null)
        {
            // 아직 HideAnswerBalloon이 직접 접근 안 될 수 있으므로 내부적으로 SubAnswerManager를 통해 Hide하거나, 타이머 0 설정
            // 이 컴포넌트는 타이머 기반이므로 직접 제어하는게 좋지만 일단 쿨하게 패스하거나 직접 컨트롤러 참조 활용
        }

        balloonInstance.SetActive(true);
        if (inputField != null) inputField.text = string.Empty;

#if !UNITY_ANDROID
        if(inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
#endif

        // 서브 캐릭터 애니메이션 Listen 모드
        AnimationManager.Instance.Listen(this.gameObject);
    }

    public void HideChatBalloon()
    {
        if (balloonInstance == null) return;

        balloonInstance.SetActive(false);

        // 서브 캐릭터 애니메이션 Listen 해제
        AnimationManager.Instance.ListenDisable(this.gameObject);
        
        CharManager.Instance.SetActiveCharacter(null); // 타겟을 메인으로 원복
    }

    public void ToggleChatBalloon()
    {
        if (balloonInstance != null && balloonInstance.activeSelf)
        {
            HideChatBalloon();
        }
        else
        {
            ShowChatBalloon();
        }
    }

    public void DestroyBalloon()
    {
        if (balloonInstance != null)
        {
            Destroy(balloonInstance);
        }
        Destroy(this);
    }
}
