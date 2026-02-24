using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChatBalloonManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static ChatBalloonManager instance;
    public static ChatBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChatBalloonManager>();
            }
            return instance;
        }
    }

    [SerializeField] private Canvas _canvas; // chatBalloon 이미지
    [SerializeField] private GameObject chatBalloon; // chatBalloon 이미지
    [SerializeField] public RectTransform characterTransform; // chatBalloon이 표시될 캐릭터의 Transform
    public GameObject clickedCharacter; // Aropla 모드에서 클릭한 캐릭터 (arona 또는 plana 인스턴스)
    [SerializeField] private RectTransform chatBalloonTransform; // chatBalloon의 Transform
    [SerializeField] public TMP_InputField inputField; 
    [SerializeField] private Toggle imageUseToggle;
    [SerializeField] private GameObject imageUseBtn; // 이미지 사용 버튼 (우클릭으로 이미지 미리보기)
    
    // 이미지 사용 상태 UI
    [SerializeField] private TMP_Text ImageStatusText; // "A" 또는 "F" 표시
    [SerializeField] private GameObject ImageUseAreaImage; // 영역 이미지 표시용
    [SerializeField] private GameObject ImageUseClipboardImage; // 클립보드 이미지 표시용
    [SerializeField] private Image mainImage; // 메인 이미지 (sprite 변경용)
    [SerializeField] private Sprite onSprite; // ON 상태용 sprite
    [SerializeField] private Sprite offSprite; // OFF 상태용 sprite
    
    // 현재 이미지 사용 상태 ("off", "auto", "force")
    private string useImageInfo = "off";

    // chatBalloon 모드 관리 (activeSelf 대신 사용) - "off" / "char" / "bottom"
    public string chatBalloonMode = "off";

    // 모드별 사이즈 (Inspector에서 조절)
    [SerializeField] private float topWidth = 720f;
    [SerializeField] private float topHeight = 480f;
    [SerializeField] private float bottomWidth = 1200f;
    [SerializeField] private float bottomHeight = 800f;
    
    // 수동으로 OFF한 상태인지 (Force에서 버튼 클릭으로 OFF된 경우)
    private bool isManualOff = false;
    
    // OFF를 거쳐서 Clipboard를 무시할지 (OFF를 한번이라도 거친 경우)
    private bool ignoreClipboard = false;
    
    // 마지막으로 변경된 이미지 소스 ("clipboard", "screenshot", "none")
    private string lastImageSource = "none";

    private void Awake()
    {
        HideChatBalloon(); // 시작 시 chatBalloon 숨기기
        
        // imageUseBtn에 우클릭 이벤트 추가
        SetupImageUseBtnRightClick();
    }

    // imageUseBtn에 우클릭 이벤트 설정
    private void SetupImageUseBtnRightClick()
    {
        if (imageUseBtn == null)
        {
            Debug.LogWarning("imageUseBtn is not assigned in ChatBalloonManager");
            return;
        }
        
        EventTrigger trigger = imageUseBtn.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = imageUseBtn.AddComponent<EventTrigger>();
        }
        
        // 우클릭 이벤트 추가
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => {
            PointerEventData pointerData = (PointerEventData)data;
            if (pointerData.button == PointerEventData.InputButton.Right)
            {
                ShowCurrentImage();
            }
        });
        trigger.triggers.Add(entry);
    }

    // 현재 이미지 소스의 이미지 미리보기
    private void ShowCurrentImage()
    {
        string imageSource = GetImageSource();
        ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
        
        if (sm == null)
        {
            Debug.LogWarning("ScreenshotManager not found");
            return;
        }
        
        if (imageSource == "clipboard")
        {
            sm.ShowClipboardImage();
        }
        else if (imageSource == "screenshot")
        {
            sm.ShowScreenshotImage();
        }
        else
        {
            Debug.Log("No image source available");
        }
    }

    // 상태 갱신 로직
    private void Update()
    {
        // ESC로 말풍선 닫기
        if (chatBalloon.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideChatBalloon();
            return;
        }

        // 선택중, 답변중에는 채팅창 숨기기        
        if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsAnswering)
        {
            HideChatBalloon();
            return;
        }

        if (StatusManager.Instance.IsChatting && chatBalloon.activeSelf && chatBalloonMode == "char")  // activeSelf : 활성화 여부
        {
            UpdateChatBalloonPosition();
        }
    }

    // chatBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowChatBalloon()
    {
        SetModeTop(true);
    }
    
    // chatBalloon을 숨기는 함수
    public void HideChatBalloon()
    {
        chatBalloon.SetActive(false);

        if (StatusManager.Instance.IsChatting) {
            StatusManager.Instance.IsChatting = false; 
            
            // Aropla 모드일 때는 clickedCharacter에 ListenDisable 적용
            if (ChatModeManager.Instance.IsAroplaMode() && clickedCharacter != null)
            {
                AnimationManager.Instance.ListenDisable(clickedCharacter);
            }
            else
            {
                AnimationManager.Instance.ListenDisable();
            }
            clickedCharacter = null; // 초기화
        }
        chatBalloonMode = "off";
//         // 안드로이드 테스트용
// #if UNITY_ANDROID && !UNITY_EDITOR
//         chatBalloon.SetActive(true);
//         StatusManager.Instance.IsChatting = true; 
// #endif
    }

    public void ToggleChatBalloon()
    {
        if (chatBalloonMode == "char")
        {
            HideChatBalloon();
        }
        else if (chatBalloonMode == "bottom")
        {
            SetModeTop(false);
        }
        else
        {
            SetModeTop(true);
        }
    }

    // 하단 고정 위치 토글
    public void ToggleChatBalloonBottom()
    {
        if (chatBalloonMode == "bottom")
        {
            HideChatBalloon();
        }
        else if (chatBalloonMode == "char")
        {
            SetModeBottom(false);
        }
        else
        {
            SetModeBottom(true);
        }
    }
    
    public bool GetImageUse()
    {
        if (imageUseToggle.isOn)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // chatBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateChatBalloonPosition()
    {
        // Aropla 모드일 때는 clickedCharacter의 RectTransform 사용
        RectTransform targetTransform = characterTransform;
        if (ChatModeManager.Instance.IsAroplaMode() && clickedCharacter != null)
        {
            RectTransform clickedRect = clickedCharacter.GetComponent<RectTransform>();
            if (clickedRect != null)
            {
                targetTransform = clickedRect;
            }
        }
        
        Vector2 charPosition = targetTransform.anchoredPosition;

        // 캐릭터의 X 위치와 동일하게 설정
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        float leftBound = -canvasRect.rect.width / 2; // 캔버스 왼쪽 끝
        float rightBound = canvasRect.rect.width / 2; // 캔버스 오른쪽 끝
        float charPositionX = Mathf.Clamp(charPosition.x, leftBound + 250, rightBound - 250);

        // chatBalloonTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 270 * SettingManager.Instance.settings.char_size / 100f); // Y축 창크기 270만큼
        chatBalloonTransform.anchoredPosition = new Vector2(charPositionX, charPosition.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
    }

    // chatBalloon 사이즈 적용
    private void ApplyBalloonSize(float width, float height)
    {
        if (chatBalloonTransform != null)
        {
            chatBalloonTransform.sizeDelta = new Vector2(width, height);
        }
    }

    // 공통 초기화 처리
    private void PrepareChatBalloon(bool resetInput)
    {
        if (AnswerBalloonManager.Instance.isAnswered) AnswerBalloonManager.Instance.HideAnswerBalloon();
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

        chatBalloon.SetActive(true);
        if (resetInput) inputField.text = string.Empty;
        StatusManager.Instance.IsChatting = true; // StatusManager 상태 업데이트

        // 이미지 사용 상태 초기화 (SettingManager에서 읽기)
        InitUseImageInfo();

        // InputField 포커스 설정 (Android 제외 - 렉 방지)
#if !UNITY_ANDROID
        inputField.Select();
        inputField.ActivateInputField();
#endif

        // Aropla 모드일 때는 clickedCharacter에 Listen 적용
        if (ChatModeManager.Instance.IsAroplaMode() && clickedCharacter != null)
        {
            AnimationManager.Instance.Listen(clickedCharacter);
        }
        else
        {
            AnimationManager.Instance.Listen();
        }
    }

    // 상단 모드로 전환
    private void SetModeTop(bool resetInput)
    {
        PrepareChatBalloon(resetInput);
        ApplyBalloonSize(topWidth, topHeight);
        UpdateChatBalloonPosition();
        SetBalloonZ(0f);
        chatBalloonMode = "char";
    }

    // 하단 모드로 전환
    private void SetModeBottom(bool resetInput)
    {
        SetBottomWidth();
        PrepareChatBalloon(resetInput);
        ApplyBalloonSize(bottomWidth, bottomHeight);
        MoveToBottomPosition();
        SetBalloonZ(-250f);  // 캐릭터 보다 앞으로 튀어나오게 설정(position 이동 후에 설정해야 함)
        chatBalloonMode = "bottom";
    }

    // z축 위치 설정
    private void SetBalloonZ(float z)
    {
        if (chatBalloonTransform == null) return;
        
        Vector3 pos3D = chatBalloonTransform.anchoredPosition3D;
        pos3D.z = z;
        chatBalloonTransform.anchoredPosition3D = pos3D;
    }

    // bottomWidth 계산: max(1200, canvasWidth * 0.7 - 600)
    private void SetBottomWidth()
    {
        RectTransform canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;
        if (canvasRect != null)
        {
            float canvasWidth = Mathf.Abs(canvasRect.rect.width);
            bottomWidth = Mathf.Max(1200f, canvasWidth * 0.7f - 600f);
        }
    }

    // 하단 위치로 이동
    private void MoveToBottomPosition()
    {
        if (UIPositionManager.Instance != null && chatBalloonTransform != null)
        {
            Vector3 worldPos = UIPositionManager.Instance.GetMenuPosition("chatBalloonBottom");
            chatBalloonTransform.position = worldPos;
        }
    }
    
    // useImageInfo를 설정하고 UI를 갱신
    public void SetUseImageInfo(string mode)
    {
        useImageInfo = mode;
        RefreshUseImageIcon();
    }
    
    // SettingManager 값을 읽어서 useImageInfo 초기화
    public void InitUseImageInfo()
    {
        int imageModeIdx = SettingManager.Instance.settings.ai_use_image_idx; // 0: off, 1: on(auto), 2: force
        
        // 플래그 초기화 (설정에서 다시 읽어오면 수동 상태 해제)
        isManualOff = false;
        ignoreClipboard = false;
        lastImageSource = "none"; // 마지막 이미지 소스 초기화
        
        if (imageModeIdx == 0) SetUseImageInfo("off");
        else if (imageModeIdx == 1) SetUseImageInfo("auto");
        else if (imageModeIdx == 2) SetUseImageInfo("force");
    }
    
    // 현재 useImageInfo 값에 따라 icon과 text만 표시
    public void RefreshUseImageIcon()
    {
        // 매번 Clipboard 이미지 존재 여부 체크
        bool hasClipboardImage = false;
        bool hasScreenshotArea = false;
        
        if (ClipboardManager.Instance != null)
        {
            hasClipboardImage = ClipboardManager.Instance.HasImageInClipboard();
        }
        
        ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
        if (sm != null)
        {
            hasScreenshotArea = sm.IsScreenshotAreaSet();
        }
        
        // 1. ImageStatusText 업데이트 (Auto면 "A", Force면 "F", 그 외에는 없애기)
        if (ImageStatusText != null)
        {
            if (useImageInfo == "auto")
            {
                ImageStatusText.text = "A";
                ImageStatusText.gameObject.SetActive(true);
            }
            else if (useImageInfo == "force")
            {
                ImageStatusText.text = "F";
                ImageStatusText.gameObject.SetActive(true);
            }
            else
            {
                ImageStatusText.gameObject.SetActive(false);
            }
        }
        
        // mainImage sprite 업데이트
        if (mainImage != null)
        {
            if (useImageInfo == "off")
            {
                if (offSprite != null)
                {
                    mainImage.sprite = offSprite;
                }
            }
            else
            {
                // OFF가 아니면 ON sprite로 변경
                if (onSprite != null)
                {
                    mainImage.sprite = onSprite;
                }
            }
        }
        
        // 2. 이미지 상태에 따른 아이콘 표시
        // Force → OFF (수동 OFF): 둘 다 비활성화
        if (isManualOff)
        {
            if (ImageUseClipboardImage != null) ImageUseClipboardImage.SetActive(false);
            if (ImageUseAreaImage != null) ImageUseAreaImage.SetActive(false);
        }
        // OFF를 거쳐서 ignoreClipboard가 true인 경우: Clipboard 무시, 영역만 표시
        else if (ignoreClipboard)
        {
            if (ImageUseClipboardImage != null) ImageUseClipboardImage.SetActive(false);
            if (ImageUseAreaImage != null) ImageUseAreaImage.SetActive(hasScreenshotArea);
        }
        // 정상 상태 (초기 상태): 클립보드 > 영역 우선순위
        else
        {
            if (ImageUseClipboardImage != null) ImageUseClipboardImage.SetActive(hasClipboardImage);
            if (ImageUseAreaImage != null) ImageUseAreaImage.SetActive(!hasClipboardImage && hasScreenshotArea);
        }
    }
    
    // 이미지 클릭 시 호출되는 함수
    public void changeUseImageInfo()
    {
        // 현재 상태에 따라 전환
        if (useImageInfo == "auto")
        {
            // Auto → Force: isManualOff만 해제, ignoreClipboard는 유지
            isManualOff = false;
            SetUseImageInfo("force");
        }
        else if (useImageInfo == "force")
        {
            // Force → OFF: 수동 OFF 플래그 설정 (Clipboard/영역 둘 다 비활성화)
            isManualOff = true;
            SetUseImageInfo("off");
        }
        else if (useImageInfo == "off")
        {
            // OFF → Auto
            // isManualOff가 true면 (Force에서 수동으로 OFF한 경우) Clipboard 무시
            // isManualOff가 false면 (초기 OFF 상태) Clipboard 표시
            ignoreClipboard = isManualOff;
            isManualOff = false;
            SetUseImageInfo("auto");
        }
    }
    
    // 현재 useImageInfo를 반환하는 메서드 (APIManager에서 사용)
    public string GetUseImageInfo()
    {
        return useImageInfo;
    }
    
    // 마지막 이미지 소스 설정 (외부에서 호출)
    public void SetLastImageSource(string source)
    {
        lastImageSource = source;
        Debug.Log($"[ChatBalloonManager] Last image source set to: {source}");
        
        // UI 갱신
        RefreshUseImageIcon();
    }
    
    // 마지막 이미지 소스 반환
    public string GetLastImageSource()
    {
        return lastImageSource;
    }
    
    // 현재 어떤 이미지 소스를 사용해야 하는지 반환 ("clipboard", "screenshot", "none")
    public string GetImageSource()
    {
        // OFF 상태거나 수동으로 OFF한 경우
        if (useImageInfo == "off" || isManualOff)
        {
            return "none";
        }
        
        // Clipboard 무시 플래그가 true면 스크린샷만
        if (ignoreClipboard)
        {
            return "screenshot";
        }
        
        // 그 외 변수 세팅
        bool hasClipboard = ClipboardManager.Instance != null && ClipboardManager.Instance.HasImageInClipboard();
        ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
        bool hasScreenshot = sm != null && sm.IsScreenshotAreaSet();
        
        // 마지막 이미지 소스가 있고 유효한 경우 우선 사용
        if (lastImageSource == "clipboard" && hasClipboard)
        {
            return "clipboard";
        }
        else if (lastImageSource == "screenshot" && hasScreenshot)
        {
            return "screenshot";
        }
        
        // 마지막 소스가 없거나 무효하면 기존 우선순위: 클립보드 > 스크린샷
        if (hasClipboard)
        {
            return "clipboard";
        }
        
        if (hasScreenshot)
        {
            return "screenshot";
        }
        
        return "none";
    }

    // STT 결과를 InputField에 추가 (바로 전송하지 않음)
    public void AppendSTTTextToInputField(string sttText)
    {
        if (inputField == null)
        {
            Debug.LogWarning("[ChatBalloonManager] InputField is null");
            return;
        }

        // 기존 텍스트에 추가 (공백으로 구분)
        if (string.IsNullOrEmpty(inputField.text))
        {
            inputField.text = sttText;
        }
        else
        {
            inputField.text += " " + sttText;
        }

        Debug.Log($"[ChatBalloonManager] STT 텍스트 추가됨: {sttText}");
        
        // InputField 포커스 (Android 제외)
#if !UNITY_ANDROID
        inputField.Select();
        inputField.ActivateInputField();
#endif
    }
    
    // ==================== 번역 테스트 기능 ====================
    
    // 번역하기 버튼 클릭 시 호출
    // InputField 텍스트를 ja, en, ko 3가지 언어로 번역하여 DebugLog
    public async void OnTranslateButtonClick()
    {
        string text = inputField?.text;
        
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[ChatBalloonManager] 번역할 텍스트가 없습니다.");
            return;
        }
        
        Debug.Log($"[ChatBalloonManager] ===== 번역 테스트 시작 =====");
        Debug.Log($"[ChatBalloonManager] 원문: {text}");
        
        // 3개 언어로 동시 번역
        var taskJa = TranslateToLanguage(text, "ja");
        var taskEn = TranslateToLanguage(text, "en");
        var taskKo = TranslateToLanguage(text, "ko");
        
        await Task.WhenAll(taskJa, taskEn, taskKo);
        
        Debug.Log($"[ChatBalloonManager] ===== 번역 테스트 완료 =====");
    }
    
    // 지정 언어로 번역하고 결과를 DebugLog
    private async Task TranslateToLanguage(string text, string targetLang)
    {
        try
        {
            var result = await ApiTranslatorManager.Instance.Translate(text, targetLang);
            
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                Debug.Log($"[ChatBalloonManager] [{targetLang.ToUpper()}] {result.Text} (Source: {result.Source}, Time: {result.Time:F2}s)");
            }
            else
            {
                Debug.LogWarning($"[ChatBalloonManager] [{targetLang.ToUpper()}] 번역 실패");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ChatBalloonManager] [{targetLang.ToUpper()}] 번역 에러: {ex.Message}");
        }
    }
}
