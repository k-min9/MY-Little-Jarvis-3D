using TMPro;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;


public class SettingManager : MonoBehaviour
{
    private string current_platform; // "Editor", "Standalone", "Android" 등 (안쓰이나?)

    [Header("General")]
    [SerializeField] private Dropdown uiLangDropdown;
    [SerializeField] private Dropdown operatorTypeDropdown;
    [SerializeField] private Dropdown platformInfoDropdown;
    [SerializeField] private Dropdown editionDropdown;
    [SerializeField] private Toggle isAlwaysOnTopToggle;
    [SerializeField] private Toggle isShowChatBoxOnClickToggle;
    [SerializeField] private Toggle isShowTutorialOnChatToggle;
    [SerializeField] private Toggle isStartServerOnInitToggle;
    [SerializeField] private Toggle isSTTServerToggle;
    [SerializeField] private Toggle editSttInChatInputToggle;
    [SerializeField] private Toggle enableEditionUpdateSuggestionToggle;

    [Header("Character")]
    [SerializeField] private Slider charSizeSlider;
    [SerializeField] private Slider charSpeedSlider;
    [SerializeField] private Slider charMobilitySlider;
    [SerializeField] private Toggle isCharAutoSmallTalk;
    [SerializeField] private TMP_Text charAutoSmallTalkIntervalText; // "Auto Small Talk : x sec"
    [SerializeField] private Slider charAutoSmallTalkSlider;
    [SerializeField] private GameObject charAutoSmallTalkSliderObject; // 슬라이더를 포함한 부모 GameObject
    [SerializeField] private Toggle isGravityToggle;
    [SerializeField] private Toggle isWindowsCollisionToggle;
    [SerializeField] private Toggle isStartWithLastCharToggle;
    [SerializeField] private Toggle isRememberCharOutfitsToggle;

    [Header("Hotkey")]
    [SerializeField] private Toggle hotKeyGlobalInputToggle;

    [Header("Sound")]
    [SerializeField] private Dropdown soundLanguageDropdown;
    [SerializeField] private Slider soundVolumeMasterSlider;  // 현재는 마스터 볼륨만 있으면
    [SerializeField] private Slider soundSpeedMasterSlider;  // 현재는 마스터 볼륨만 있으면

    [Header("Server")]  // 구 Server Field. 변수도 관련 변수로 설정되어있음.
    [SerializeField] private Dropdown serverTypeDropdown;
    
    // Dropdown 인덱스와 실제 server_type_idx 매핑
    private static readonly int[] DropdownIndexToServerTypeIdx = new int[]
    {
        0,  // Dropdown [0] → Auto
        1,  // Dropdown [1] → Local
        2,  // Dropdown [2] → Google
        // 3,  // Dropdown [3] → OpenRouter
        9   // Dropdown [3] → Custom (실제 idx는 9)
    };
    
    // 역매핑 Dictionary (server_type_idx → Dropdown Index)
    private static readonly Dictionary<int, int> ServerTypeIdxToDropdownIndex = new Dictionary<int, int>
    {
        { 0, 0 },  // Auto → Dropdown [0]
        { 1, 1 },  // Local → Dropdown [1]
        { 2, 2 },  // Google → Dropdown [2]
        // { 3, 3 },  // OpenRouter → Dropdown [3]
        { 9, 3 }   // Custom → Dropdown [4]
    };
    [SerializeField] private Dropdown serverLocalModeTypeDropdown;
    [SerializeField] private Dropdown serverLocalModelTypeDropdown;
    [SerializeField] private Image serverLocalDownloadIconImage;
    [SerializeField] private Button serverLocalLoadButton;
    [SerializeField] private Button serverLocalReleaseButton;
    [SerializeField] private Dropdown geminiModelTypeDropdown;
    [SerializeField] private TMP_InputField serverGeminiApiKeyInputField;
    [SerializeField] public Text serverGeminikeyTestResultText;
    [SerializeField] private Dropdown serverOpenRouterRecommendedDropdown;
    [SerializeField] private TMP_InputField serverOpenRouterModelNameInputField;
    [SerializeField] private TMP_InputField serverOpenRouterApiKeyInputField;
    [SerializeField] public Text serverOpenRouterkeyTestResultText;
    [SerializeField] private Dropdown serverCustomRecommendedDropdown;
    [SerializeField] private TMP_InputField serverCustomModelNameInputField;
    [SerializeField] private Toggle isAPITestToggle;
    [SerializeField] private TMP_InputField serverIdInputField;

    // 서버 타입별 통합 UI GameObject
    [SerializeField] private GameObject serverUIAutoGameObject;
    [SerializeField] private GameObject serverUILocalGameObject;
    [SerializeField] private GameObject serverUIGoogleGameObject;
    [SerializeField] private GameObject serverUIOpenRouterGameObject;
    [SerializeField] private GameObject serverUIChatGPTGameObject;
    [SerializeField] private GameObject serverUICustomGameObject;
    [SerializeField] private Toggle isAskedTurnOnServerToggle;  // Lite, Full 일때 활성화 되는 무언가. 그냥 문답무용으로 키는 방향으로 진행했을때의 영향도도 파악 필요.

    [Header("Conversation")]  // 구 AI Field. 변수도 관련 변수로 설정되어있음.
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Dropdown aiLangDropdown;
    [SerializeField] private Dropdown aiWebSearchDropdown;
    [SerializeField] private Dropdown aiUseImageDropdown;
    [SerializeField] private Dropdown aiAskIntentDropdown;
    [SerializeField] private Dropdown aiEmotionDropdown;
    [SerializeField] private Dropdown aiVoiceFilterDropdown;
    [SerializeField] private Toggle confirmUserIntentToggle;
    [SerializeField] private Toggle includeCharInScreenshotToggle;
    [SerializeField] private Toggle includeUIInScreenshotToggle;
    [SerializeField] private Toggle isAskChangeToMultimodalToggle; // 이미지 첨부설정일 경우, 멀티모달 모델로 변경할지 질문

    [Header("Dialogue Info")]
    [SerializeField] private Text aiInfoServerType;
    [SerializeField] private Text aiInfoModel;
    [SerializeField] private Text aiInfoPrompt;
    [SerializeField] private Text aiInfoLangUsed;
    [SerializeField] private Text aiInfoTranslator;
    [SerializeField] private Text aiInfoTime;
    [SerializeField] private Text aiInfoIntent;

    [Header("Dev")]
    [SerializeField] private Toggle devModeToggle;
    [SerializeField] private Toggle devHowlingToggle;
    [SerializeField] private Toggle devSoundToggle;

    [Header("OCR")]
    [SerializeField] private Button ocrOption1Button;
    [SerializeField] private Button ocrOption2Button;
    [SerializeField] private Button ocrOption3Button;
    [SerializeField] private Image ocrOption1Image;
    [SerializeField] private Image ocrOption2Image;
    [SerializeField] private Image ocrOption3Image;
    [SerializeField] private Sprite ocrOptionActiveSprite;
    [SerializeField] private Sprite ocrOptionInactiveSprite;

    // 기타 표시용 UI
    [Header("Extra UI")]
    public Text soundSpeedMasterText;
    // public Text serverInfoText;  // Auto용??
    public Text charSizeText;

    // 설정 데이터 클래스
    [Serializable]
    public class SettingsData
    {
        public string player_name;
        public int ui_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string ui_language;
        public int operator_type_idx;  // 0 : ARONA, 1: Plana
        public string operator_type;
        public int ai_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string ai_language_in;
        public string ai_language_out;
        public int ai_voice_filter_idx;  // 0 : None, 1 : Skip AI Voice, 2 : User Voice Only
        public bool isAlwaysOnTop;
        public bool isShowChatBoxOnClick;
        public bool isShowTutorialOnChat;
        public bool isTutorialCompleted;
        public bool isStartServerOnInit;
        public bool isSTTServer;
        public bool editSttinChatInput;
        public bool enableEditionUpdateSuggestion;

        public string char_lastUsed;
        public float char_size;
        public float char_mobility;
        public float char_speed;
        public bool isCharAutoSmallTalk;
        public float charAutoSmallTalkInterval; // 초 단위
        public bool isGravity;
        public bool isWindowsCollision;
        public bool isStartWithLastChar;
        public bool isRememberCharOutfits;

        public int sound_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string sound_language;
        public float sound_volumeMaster;
        public float sound_speedMaster;

        public int server_type_idx;  // 0: Auto, 1: Local, 2: Google, 3: OpenRouter
        public string server_type;
        public string server_id;
        public string api_key_gemini;
        public string api_key_openRouter;
        
        // 서비스별 모델명
        public int server_local_mode_idx;    // 0: CPU, 1: GPU
        public string server_local_mode;     // "GPU" or "CPU"
        public string model_name_Local;      // Local 서버 모델 (예: "qwen-14b")
        public string model_name_Gemini;     // Gemini 모델 (예: "gemini-1.5-flash")
        public string model_name_OpenRouter; // OpenRouter 모델 (예: "google/gemini-flash-1.5")
        public string model_name_ChatGPT;    // ChatGPT 모델 (예: "gpt-4-turbo") - 미래 대비
        public string model_name_Custom;     // Custom 모델 (예: "qwen-38b", "claude-sonnet")
        
        public int ai_web_search_idx;  // 0 : off, 1 : on, 2: force
        public string ai_web_search;
        public int ai_use_image_idx;  // 0 : off, 1 : on, 2: force
        public string ai_use_image;
        public int ai_ask_intent_idx;  // 0 : off, 1 : on
        public string ai_ask_intent;
        public int ai_emotion_idx;  // 0 : off, 1 : on
        public string ai_emotion;
        public string ai_language;
        public bool isAskedTurnOnServer;
        public bool isAPITest;
        public bool confirmUserIntent;
        public bool includeCharInScreenshot;
        public bool includeUIInScreenshot;
        public bool isAskChangeToMultimodal;  // 멀티모달 지원하지 않는 모델일 때 변경 여부 물어보기

        // Dev용 데이터
        public bool isDevMode;
        public bool isDevHowling;
        public bool isDevSound;  // dev_voice 서버 사용 여부 (저장 안함)

        // Hotkey
        public bool hotKeyGlobalInputEnabled;

        // OCR
        public string ocrOptionType = "Options1";  // "Options1", "Options2", "Options3"

        // UI외 데이터
        public bool wantFreeServer;  // 무료서버연결의향
    }

    // 설정 데이터 인스턴스
    public SettingsData settings = new SettingsData();

    // 싱글톤 인스턴스
    public static SettingManager instance;
    public static SettingManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SettingManager>();
            }
            return instance;
        }
    }

    // setter
    public void SetPlayerName(string value) { settings.player_name = value; SaveSettings(); }
    public void SetUiLanguage() { int value=uiLangDropdown.value; settings.ui_language_idx = value; settings.ui_language=getLangFromIdx(value); LanguageManager.Instance.SetUILanguage(); SaveSettings(); }
    public void SetOperatorType() { int value=operatorTypeDropdown.value; value = getOperatorTypeFilterScenario(value); operatorTypeDropdown.value = value;  settings.operator_type_idx = value; settings.operator_type=getOperatorTypeFromIdx(value); SaveSettings(); }
    public void SetAiLanguageIn(string value) { settings.ai_language_in = value; SaveSettings(); }
    public void SetAiLanguageOut(string value) { settings.ai_language_out = value; SaveSettings(); }
    public void SetIsAlwaysOnTop(bool value) { 
        settings.isAlwaysOnTop = value; 
        #if !UNITY_ANDROID
        WindowManager.SetWindowAlwaysOnTop(value); 
        # endif
        SaveSettings(); 
    }
    public void SetIsShowChatBoxOnClick(bool value) {settings.isShowChatBoxOnClick = value; SaveSettings(); }
    public void SetIsShowTutorialOnChat(bool value) {settings.isShowTutorialOnChat = value; settings.isTutorialCompleted = !value; SaveSettings(); }
    public void SetIsisStartServerOnInit(bool value) {settings.isStartServerOnInit = value; SaveSettings(); }
    public async void SetIsSTTServer(bool value) { value = await getIsSTTServerFilterScenarioAsync(value); isSTTServerToggle.isOn = value; settings.isSTTServer = value; SaveSettings(); }
    public void SetEditSttinChatInput(bool value) { settings.editSttinChatInput = value; SaveSettings(); }
    public void SetEnableEditionUpdateSuggestion(bool value) { settings.enableEditionUpdateSuggestion = value; SaveSettings(); }

    public void SetCharLastUsed(string value) { settings.char_lastUsed = value; SaveSettings(); }
    public void SetCharSize(float value) { settings.char_size = value; CharManager.Instance.setCharSize(); SaveSettings(); charSizeText.text="Size ("+(int)settings.char_size+")";}
    public void SetCharSpeed(float value) { settings.char_speed = value; SaveSettings(); }
    public void SetCharMobility(float value) { settings.char_mobility = value; SaveSettings(); }
    public async void SetIsCharAutoSmallTalk(bool value) { value = await getIsCharAutoSmallTalkFilterScenarioAsync(value); isCharAutoSmallTalk.isOn = value; settings.isCharAutoSmallTalk = value; UpdateAutoSmallTalkUI(); SaveSettings(); }
    public void SetCharAutoSmallTalkInterval(float value) { settings.charAutoSmallTalkInterval = value; UpdateAutoSmallTalkIntervalText(); SaveSettings(); GlobalTimeVariableManager.Instance.smallTalkTimer = 0f; }
    public void SetIsGravity(bool value) { settings.isGravity = value; SaveSettings(); }
    public void SetIsWindowsCollision(bool value) { settings.isWindowsCollision = value; WindowCollisionManager.Instance.SetWindowsRectChecking(value); SaveSettings(); }
    public void SetIsStartWithLastChar(bool value) { settings.isStartWithLastChar = value; SaveSettings(); }
    public void SetIsRememberCharOutfits(bool value) { settings.isRememberCharOutfits = value; SaveSettings(); }

    public void SetSoundLanguageType() { int value=soundLanguageDropdown.value; settings.sound_language_idx = value; settings.sound_language=getLangFromIdx(value); SaveSettings(); }
    public void SetSoundVolumeMaster(float value) { settings.sound_volumeMaster = value; SaveSettings(); }
    public void SetSoundSpeedMaster(float value) { settings.sound_speedMaster = value; SaveSettings(); soundSpeedMasterText.text="Speed (" + (int)settings.sound_speedMaster + "%)";}

    public async void SetServerType() 
    { 
        int dropdownIndex = serverTypeDropdown.value;
        int serverTypeIdx = DropdownIndexToServerTypeIdx[dropdownIndex];
        
        serverTypeIdx = await getServerTypeFilterScenarioAsync(serverTypeIdx);
        
        // 필터링 후 다시 드롭다운에 반영
        if (ServerTypeIdxToDropdownIndex.ContainsKey(serverTypeIdx))
        {
            serverTypeDropdown.value = ServerTypeIdxToDropdownIndex[serverTypeIdx];
        }
        
        settings.server_type_idx = serverTypeIdx;
        settings.server_type = getServerTypeFromIdx(serverTypeIdx);
        LanguageManager.Instance.SetServerServerTypeInfoTooltip(serverTypeIdx);
        SetServerUIFromServerType(serverTypeIdx);
        SaveSettings();
        LanguageManager.Instance.SetUILanguage();
    }
    
    public async void SetServerTypeByValue(int serverTypeIdx) 
    { 
        serverTypeIdx = await getServerTypeFilterScenarioAsync(serverTypeIdx);
        
        // server_type_idx를 드롭다운 인덱스로 변환
        if (ServerTypeIdxToDropdownIndex.ContainsKey(serverTypeIdx))
        {
            serverTypeDropdown.value = ServerTypeIdxToDropdownIndex[serverTypeIdx];
        }
        
        settings.server_type_idx = serverTypeIdx;
        settings.server_type = getServerTypeFromIdx(serverTypeIdx);
        LanguageManager.Instance.SetServerServerTypeInfoTooltip(serverTypeIdx);
        SetServerUIFromServerType(serverTypeIdx);
        SaveSettings();
        LanguageManager.Instance.SetUILanguage();
    }
    public void SetServerID(string value) { settings.server_id = value; SaveSettings(); }
    public void SetAPIKeyGemini(string value) { settings.api_key_gemini = value; SaveSettings(); }
    public void SetAPIKeyOpenRouter(string value) { settings.api_key_openRouter = value; SaveSettings(); }
    public void SetServerLocalMode() { int value = serverLocalModeTypeDropdown.value; settings.server_local_mode_idx = value; settings.server_local_mode = getLocalServerModeFromIdx(value); SaveSettings(); }
    public void SetServerModelType() { int value = serverLocalModelTypeDropdown.value; string displayName = serverLocalModelTypeDropdown.options[value].text; settings.model_name_Local = ModelDataLocal.GetIdByDisplayName(displayName); SetServerUIFromServerModel(ModelDataLocal.GetFileNameByDisplayName(displayName)); SaveSettings(); }
    public void SetGeminiModelType() { int value = geminiModelTypeDropdown.value; if (value >= 0 && value < ModelDataGemini.ModelOptions.Count) { settings.model_name_Gemini = ModelDataGemini.ModelOptions[value].Id; } SaveSettings(); }
    public void SetOpenRouterRecommendedModel() { int value = serverOpenRouterRecommendedDropdown.value; if (value >= 0 && value < ModelDataOpenRouter.ModelOptions.Count) { string modelId = ModelDataOpenRouter.ModelOptions[value].Id; settings.model_name_OpenRouter = modelId; Debug.Log(modelId); serverOpenRouterModelNameInputField.text = modelId; } SaveSettings(); }
    public void SetOpenRouterModelName(string value) { settings.model_name_OpenRouter = value; SaveSettings(); }
    public void SetCustomRecommendedModel() { int value = serverCustomRecommendedDropdown.value; if (value >= 0 && value < ModelDataCustom.ModelOptions.Count) { string modelId = ModelDataCustom.ModelOptions[value].Id; settings.model_name_Custom = modelId; Debug.Log(modelId); serverCustomModelNameInputField.text = modelId; } SaveSettings(); UpdateCustomInputFieldVisibility(); }
    public void SetCustomModelName(string value) { settings.model_name_Custom = value; SaveSettings(); }
    
    // Custom 모델 InputField 가시성 업데이트
    private void UpdateCustomInputFieldVisibility()
    {
        if (serverCustomRecommendedDropdown == null || serverCustomModelNameInputField == null) return;
        
        int selectedIndex = serverCustomRecommendedDropdown.value;
        // 마지막 옵션("직접 입력")이 선택되면 InputField 활성화
        bool isCustomInput = (selectedIndex == serverCustomRecommendedDropdown.options.Count - 1);
        serverCustomModelNameInputField.gameObject.SetActive(isCustomInput);
    }
    
    // Custom 모델명 조회 (현재 UI 상태 기반)
    public string GetCurrentCustomModelName()
    {
        // 저장된 설정값이 있으면 우선 사용
        if (!string.IsNullOrEmpty(settings.model_name_Custom))
        {
            return settings.model_name_Custom;
        }
        
        // 설정값이 비어있을 경우 현재 UI 상태에서 값 조회
        if (serverCustomRecommendedDropdown != null)
        {
            int selectedIndex = serverCustomRecommendedDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < ModelDataCustom.ModelOptions.Count) // "직접 입력"이 아닌 경우 (드롭다운에서 모델 선택)
            {
                return ModelDataCustom.ModelOptions[selectedIndex].Id;
            }
            else if (serverCustomModelNameInputField != null) // "직접 입력"인 경우 InputField 값 사용
            {
                return serverCustomModelNameInputField.text ?? "";
            }
        }
        
        return "";
    }
    
    // 버튼용 래핑 함수 (5초간 버튼 비활성화)
    public void CallReleaseLocalModel() 
    { 
        if (serverLocalReleaseButton != null)
        {
            APIManager.Instance.CallReleaseModel();
            StartCoroutine(DisableButtonTemporarily(serverLocalReleaseButton, 5f));
        }
    }
    
    public void CallLoadLocalModel() 
    { 
        if (serverLocalLoadButton != null)
        {
            APIManager.Instance.CallLoadModel();
            StartCoroutine(DisableButtonTemporarily(serverLocalLoadButton, 5f));
        }
    }

    // 버튼을 일정 시간 동안 비활성화하는 Coroutine
    private IEnumerator DisableButtonTemporarily(Button button, float duration)
    {
        button.interactable = false;
        yield return new WaitForSeconds(duration);
        button.interactable = true;
    }

    public async void SetAIWebSearch() { int value=aiWebSearchDropdown.value; value = await getAiWebSearchFilterScenarioAsync(value); aiWebSearchDropdown.value = value; settings.ai_web_search_idx = value; settings.ai_web_search = getONOFFTypeFromIdx(value); SaveSettings(); }
    public async void SetAIUseImage() { int value=aiUseImageDropdown.value; value = await getAiUseImageFilterScenarioAsync(value); aiUseImageDropdown.value = value; settings.ai_use_image_idx = value; settings.ai_use_image = getONOFFTypeFromIdx(value); SaveSettings(); if (ChatBalloonManager.Instance != null) ChatBalloonManager.Instance.InitUseImageInfo(); }
    public async void SetAIUseImageByValue(int value) { value = await getAiUseImageFilterScenarioAsync(value); aiUseImageDropdown.value = value; settings.ai_use_image_idx = value; settings.ai_use_image = getONOFFTypeFromIdx(value); SaveSettings(); if (ChatBalloonManager.Instance != null) ChatBalloonManager.Instance.InitUseImageInfo(); }
    public void SetAIAskIntent() { int value=aiAskIntentDropdown.value; settings.ai_ask_intent_idx = value; settings.server_type=getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetIsAskedTurnOnServer(bool value) { settings.isAskedTurnOnServer = value; SaveSettings(); }
    public void SetIsAPITest(bool value) { settings.isAPITest = value; SaveSettings(); }
    public void SetConfirmUserIntent(bool value) { settings.confirmUserIntent = value; SaveSettings(); }  // SetAIAskIntent Toggle 버전
    public void SetIncludeCharInScreenshot(bool value) { settings.includeCharInScreenshot = value; SaveSettings(); }
    public void SetIncludeUIInScreenshot(bool value) { settings.includeUIInScreenshot = value; SaveSettings(); }
    public void SetIsAskChangeToMultimodalToggle(bool value) { settings.isAskChangeToMultimodal = value; SaveSettings(); }
    public void SetAiLanguage() { int value=aiLangDropdown.value; settings.ai_language_idx = value; settings.ai_language=getAiLangFromIdx(value); SaveSettings(); }
    public async void SetAIEmotion() { int value=aiEmotionDropdown.value; value = await getAiEmotionFilterScenarioAsync(value); aiEmotionDropdown.value = value; settings.ai_emotion_idx = value; settings.ai_emotion = getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetAiVoiceFilter() { int value=aiVoiceFilterDropdown.value; value = getAiVoiceFilterScenario(value); aiVoiceFilterDropdown.value = value; settings.ai_voice_filter_idx = value; SaveSettings(); }
    public void SetHotKeyGlobalInput(bool value) 
    { 
        // 윈도우가 아닐 경우 필터링
        value = getHotKeyGlobalInputFilterScenario(value);
        hotKeyGlobalInputToggle.isOn = value;
        
        settings.hotKeyGlobalInputEnabled = value; 
        
        // HotkeyManager에 전역 핫키 활성화/비활성화 알림
        if (HotkeyManager.Instance != null)
        {
            HotkeyManager.Instance.SetGlobalHotkeyEnabled(value);
        }
        
        SaveSettings(); 
    }

    public void SetIsDevModeToggle(bool value) { settings.isDevMode = value; DevManager.Instance.SetInteractableDev(value);}
    public void SetIsDevHowlingToggle(bool value) { settings.isDevHowling = value; SaveSettings(); }
    public void SetIsDevSoundToggle(bool value) { settings.isDevSound = value; }  // dev_voice 서버 사용 여부 (저장 안함)
    
    // Auto Small Talk UI 업데이트
    private void UpdateAutoSmallTalkUI() { charAutoSmallTalkSliderObject.SetActive(settings.isCharAutoSmallTalk); UpdateAutoSmallTalkIntervalText(); }
    private void UpdateAutoSmallTalkIntervalText()
    {
        if (settings.isCharAutoSmallTalk)
        {
            int intervalSec = (int)settings.charAutoSmallTalkInterval;
            charAutoSmallTalkIntervalText.text = "Auto Small Talk : " + intervalSec + " sec";
        }
        else
        {
            charAutoSmallTalkIntervalText.text = "Auto Small Talk";
        }
    }
    
    // OCR 옵션 관련
    public void SetOCROption1() { SetOCROptionType("Options1"); }
    public void SetOCROption2() { SetOCROptionType("Options2"); }
    public void SetOCROption3() { SetOCROptionType("Options3"); }

    // OCR 탭 열릴 때 호출 (Unity에서 탭 버튼에 연결)
    public void OnOCRTabOpened()
    {
        UpdateOCROptionToggles();
        OCRManager.Instance.InitializeFromSettings(settings.ocrOptionType);
        
        Debug.Log($"[SettingManager] OCR Tab opened - ocrOptionType: {settings.ocrOptionType}");
    }
    
    private void SetOCROptionType(string optionType)
    {     
        settings.ocrOptionType = optionType;
        
        // 슬롯 번호 결정 (Options1 -> 1, Options2 -> 2, Options3 -> 3)
        int slotNumber = 1;
        if (optionType == "Options2")
            slotNumber = 2;
        else if (optionType == "Options3")
            slotNumber = 3;
        
        // OCRManager에 슬롯 변경 알림
        OCRManager.Instance.SetActiveSlot(slotNumber);
        
        // UI 갱신 - 버튼 상태
        UpdateOCROptionToggles();
        SaveSettings();
             
        Debug.Log($"[SettingManager] OCR Option changed to {optionType} (Slot {slotNumber})");
    }
    
    private void UpdateOCROptionToggles()
    {     
        // 이미지 스프라이트만 업데이트
        ocrOption1Image.sprite = settings.ocrOptionType == "Options1" ? ocrOptionActiveSprite : ocrOptionInactiveSprite;
        ocrOption2Image.sprite = settings.ocrOptionType == "Options2" ? ocrOptionActiveSprite : ocrOptionInactiveSprite;
        ocrOption3Image.sprite = settings.ocrOptionType == "Options3" ? ocrOptionActiveSprite : ocrOptionInactiveSprite;
    }
    
    // dev_voice 서버 사용 여부 반환 (Android 또는 DevSound 토글 활성화시 true)
    public bool IsDevSoundEnabled()
    {
#if UNITY_ANDROID
        return true;  // Android에서는 항상 dev_voice 사용
#else
        return settings.isDevSound;  // PC에서는 토글 설정값 사용
#endif
    }
    
    // 표시용
    // public void SetServerInfoText(string text) { serverInfoText.text = text; }
    public void RefreshAIInfoText(string ai_info_server_type, string ai_info_model, string ai_info_prompt, string ai_info_lang_used, string ai_info_translator, string ai_info_time, string ai_info_intent) {
    aiInfoServerType.text = ai_info_server_type;
    aiInfoModel.text = ai_info_model;
    aiInfoPrompt.text = ai_info_prompt;
    aiInfoLangUsed.text = ai_info_lang_used;
    aiInfoTranslator.text = ai_info_translator;
    aiInfoTime.text = ai_info_time;
    aiInfoIntent.text = ai_info_intent;
    }

    private string configFilePath;

    void Awake()
    {     
        // Devmode 초기화
        devModeToggle.isOn = false;
        devSoundToggle.isOn = false;
        
        // 저장 경로를 Application.persistentDataPath로 설정
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        configFilePath = Path.Combine(directoryPath, "settings.json");

        // 디렉토리가 없을 경우 생성
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        LoadSettings();
             
        SetUIAfterLoading();

        Debug.Log("###1");
        // 플랫폼별 UI 적용
#if UNITY_ANDROID
        isAlwaysOnTopToggle.gameObject.SetActive(false);
#endif
    }

    void Start()
    {     
        // 현재 플랫폼 세팅
        SetPlatformInfoDropdown();
        SetServerModelDropdownOptions();
        SetGeminiModelDropdownOptions();
        SetOpenRouterModelDropdownOptions();
        SetCustomModelDropdownOptions();

        // 서버 상태 세팅
        InstallStatusManager.Instance.LoadInstallStatus();
        SetInstallStatus();

        // 로딩후 UI
        SetUIAfterLoading();

        // 변동 UI 갱신
        SetServerType();
        SetServerModelType();
        SetGeminiModelType();

        // 최상위에 두는지 확인하고 세팅
        WindowManager.SetWindowAlwaysOnTop(settings.isAlwaysOnTop);
    }
    
    private void SetPlatformInfoDropdown()
    {
        // 현재 플랫폼에 따라 value 설정
        #if UNITY_EDITOR
            platformInfoDropdown.value = 2; // Extra
        #elif UNITY_ANDROID
            platformInfoDropdown.value = 1; // Android
        #elif UNITY_STANDALONE
            platformInfoDropdown.value = 0; // PC
        #else
            platformInfoDropdown.value = 2; // Extra
        #endif
    }
    


    public void SetInstallStatus()
    {
        // InstallStatusManager를 통해 설치 상태를 확인하고 UI에 반영
        int installStatusIndex = InstallStatusManager.Instance.GetInstallStatusIndex();
        
        switch (installStatusIndex)
        {
            case 0: // sample
                editionDropdown.value = 0;
                Debug.Log("Sample 버전으로 설정됨");
                break;
            case 1: // lite
                editionDropdown.value = 1;
                Debug.Log("Lite 버전으로 설정됨");
                break;
            case 2: // full
                editionDropdown.value = 2;
                Debug.Log("Full 버전으로 설정됨");
                break;
            default:
                editionDropdown.value = 0;
                Debug.LogWarning("알 수 없는 설치 상태, 기본값으로 설정");
                break;
        }
    }

    public int GetInstallStatus()
    {
        return InstallStatusManager.Instance.GetInstallStatusIndex();
    }

            
        
//     private void SetDefaultServerTypeByPlatform()
//     {
//         serverTypeDropdown.value = 2; // Server
// #if UNITY_EDITOR
//         serverTypeDropdown.value = 1; // Local
// #elif UNITY_STANDALONE
//             serverTypeDropdown.value = 1; // Local
// #else
//             serverTypeDropdown.value = 2; // Server
// #endif

    //         settings.server_type_idx = serverTypeDropdown.value;
    //         settings.server_type = getServerTypeFromIdx(serverTypeDropdown.value);
    //     }

    private void SetServerModelDropdownOptions()
    {
        serverLocalModelTypeDropdown.ClearOptions();

        List<string> optionNames = new List<string>();
        foreach (var model in ModelDataLocal.ModelOptions)
        {
            optionNames.Add(model.DisplayName);
        }

        serverLocalModelTypeDropdown.AddOptions(optionNames);

        // 저장된 모델 ID를 기반으로 선택 인덱스를 설정
        int selectedIndex = ModelDataLocal.ModelOptions.FindIndex(m => m.Id == settings.model_name_Local);
        if (selectedIndex >= 0)
        {
            serverLocalModelTypeDropdown.value = selectedIndex;

            // 다운로드 UI 갱신
            // int value = serverLocalModelTypeDropdown.value;
            // string displayName = serverLocalModelTypeDropdown.options[value].text;
            // string modelFileName = ModelDataLocal.GetFileNameByDisplayName(displayName);
            // SetServerUIFromServerModel(modelFileName);
            // SaveSettings();
        }
        else
        {
            serverLocalModelTypeDropdown.value = 0;
            settings.model_name_Local = ModelDataLocal.ModelOptions[0].Id;
        }
    }
    
    private void SetGeminiModelDropdownOptions()
    {
        geminiModelTypeDropdown.ClearOptions();

        List<string> optionNames = new List<string>();
        foreach (var model in ModelDataGemini.ModelOptions)
        {
            // Paid일 경우만 (P) 표시
            string displayText = model.PriceType == "Paid" ? $"{model.DisplayName} (P)" : model.DisplayName;
            optionNames.Add(displayText);
        }

        geminiModelTypeDropdown.AddOptions(optionNames);

        // 저장된 모델 ID를 기반으로 선택 인덱스를 설정
        int selectedIndex = ModelDataGemini.ModelOptions.FindIndex(m => m.Id == settings.model_name_Gemini);
        if (selectedIndex >= 0)
        {
            geminiModelTypeDropdown.value = selectedIndex;
        }
        else
        {
            geminiModelTypeDropdown.value = 0;
            settings.model_name_Gemini = ModelDataGemini.ModelOptions[0].Id;
        }
    }
    
    private void SetOpenRouterModelDropdownOptions()
    {
        serverOpenRouterRecommendedDropdown.ClearOptions();

        List<string> optionNames = new List<string>();
        foreach (var model in ModelDataOpenRouter.ModelOptions)
        {
            // Paid일 경우만 (P) 표시
            string displayText = model.PriceType == "Paid" ? $"{model.DisplayName} (P)" : model.DisplayName;
            optionNames.Add(displayText);
        }

        serverOpenRouterRecommendedDropdown.AddOptions(optionNames);

        // 저장된 모델명이 추천 목록에 있는지 확인
        int selectedIndex = ModelDataOpenRouter.ModelOptions.FindIndex(m => m.Id == settings.model_name_OpenRouter);
        if (selectedIndex >= 0)
        {
            serverOpenRouterRecommendedDropdown.value = selectedIndex;
            // InputField에도 모델 ID 반영
            serverOpenRouterModelNameInputField.text = ModelDataOpenRouter.ModelOptions[selectedIndex].Id;
        }
        else
        {
            serverOpenRouterRecommendedDropdown.value = 0;
            // 기본값 InputField에 반영
            if (ModelDataOpenRouter.ModelOptions.Count > 0)
            {
                serverOpenRouterModelNameInputField.text = ModelDataOpenRouter.ModelOptions[0].Id;
            }
        }
    }
    
    private void SetCustomModelDropdownOptions()
    {
        serverCustomRecommendedDropdown.ClearOptions();

        List<string> optionNames = new List<string>();
        foreach (var model in ModelDataCustom.ModelOptions)
        {
            // Paid일 경우만 (P) 표시
            string displayText = model.PriceType == "Paid" ? $"{model.DisplayName} (P)" : model.DisplayName;
            optionNames.Add(displayText);
        }
        
        // 마지막에 "직접 입력" 옵션 추가
        optionNames.Add("직접 입력");

        serverCustomRecommendedDropdown.AddOptions(optionNames);

        // 저장된 모델명이 추천 목록에 있는지 확인
        int selectedIndex = ModelDataCustom.ModelOptions.FindIndex(m => m.Id == settings.model_name_Custom);
        if (selectedIndex >= 0)
        {
            serverCustomRecommendedDropdown.value = selectedIndex;
            // InputField에도 모델 ID 반영
            serverCustomModelNameInputField.text = ModelDataCustom.ModelOptions[selectedIndex].Id;
        }
        else if (!string.IsNullOrEmpty(settings.model_name_Custom))
        {
            // 저장된 값이 목록에 없으면 "직접 입력" 선택
            serverCustomRecommendedDropdown.value = optionNames.Count - 1;
            serverCustomModelNameInputField.text = settings.model_name_Custom;
        }
        else
        {
            serverCustomRecommendedDropdown.value = 0;
            // 기본값 InputField에 반영
            if (ModelDataCustom.ModelOptions.Count > 0)
            {
                serverCustomModelNameInputField.text = ModelDataCustom.ModelOptions[0].Id;
            }
        }
        
        // InputField 가시성 업데이트
        UpdateCustomInputFieldVisibility();
    }
    
    // idx를 언어이름으로 변환; 0 : ko, 1 : jp, 2: en
    private string getLangFromIdx(int idx)
    {
        string lang = "ko";
        if (idx == 1)
        {
            lang = "jp";
        }
        if (idx == 2)
        {
            lang = "en";
        }
        return lang;
    }

    // idx를 오퍼레이터 타입으로 변경
    private string getOperatorTypeFromIdx(int idx)
    { 
        string operatorType = "ARONA";
        if (idx == 1)
        {
            operatorType = "Plana";
        }
        return operatorType;
    }
    
    // idx를 추론설정이름으로 변환; 0 : normal, 1 : prefer, 2 : ko, 3 : jp, 4 : en
    private string getAiLangFromIdx(int idx)
    {
        string lang = "normal";
        if (idx == 1)
        {
            lang = "prefer";
        }
        if (idx == 2)
        {
            lang = "ko";
        }
        if (idx == 3)
        {
            lang = "jp";
        }
        if (idx == 4)
        {
            lang = "en";
        }
        return lang;
    }

    private async Task<int> getAiEmotionFilterScenarioAsync(int value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getAiEmotionFilterScenario - DEV MODE로 통과");
            return value;
        }

        // Emotion 기능 키려는데 현재 Sample 버전일 경우 버전업 요구
        if (value == 1)
        {
            bool chk = await InstallStatusManager.Instance.CheckAndOperateFullAsync();
            if (!chk) 
            {
                // 안내했을 경우, 0으로 반환
                return 0;
            }
        }

        return value;
    }

    private async Task<bool> getIsCharAutoSmallTalkFilterScenarioAsync(bool value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getIsCharAutoSmallTalkFilterScenarioAsync - DEV MODE로 통과");
            return value;
        }

        // Auto Small Talk 기능 키려는데 현재 Sample 버전일 경우 버전업 요구
        if (value)
        {
            bool chk = await InstallStatusManager.Instance.CheckAndOperateFullAsync();
            if (!chk) 
            {
                // 안내했을 경우, false로 반환
                return false;
            }
        }

        return value;
    }

    private int getOperatorTypeFilterScenario(int value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getOperatorTypeFilterScenario - DEV MODE로 통과");
            return value;
        }

        // 현재는 아직 준비 안되었다고 돌려보내기
        if (value == 1)
        {
            StartCoroutine(ScenarioCommonManager.Instance.Run_C99_NotReady());

            return 0;
        }
        return value;
    }

    // Local은 최소 Lite 버전 이상 필요 (서버 설치 여부 확인)
    private async Task<int> getServerTypeFilterScenarioAsync(int value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getServerTypeFilterScenarioAsync - DEV MODE로 통과");
            return value;
        }

        // Local 기능 키려는데 현재 Sample 버전일 경우 버전업 요구
        if (value == 1)
        {
            bool chk = await InstallStatusManager.Instance.CheckAndOperateLiteAsync();
            if (!chk) 
            {
                // 안내했을 경우, 0(Auto)로 반환
                return 0;
            }
        }


        return value;
    }

    private async Task<int> getAiWebSearchFilterScenarioAsync(int value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getAiWebSearchFilterScenario - DEV MODE로 통과");
            return value;
        }

        // WebSearch 기능 키려는데 현재 Sample 버전일 경우 버전업 요구
        if (value >= 1)  // 1: on, 2: force 둘 다 체크
        {
            bool chk = await InstallStatusManager.Instance.CheckAndOperateFullAsync();
            if (!chk) 
            {
                // 안내했을 경우, 0으로 반환
                return 0;
            }
        }

        return value;
    }

    private async Task<int> getAiUseImageFilterScenarioAsync(int value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getAiUseImageFilterScenario - DEV MODE로 통과");
            return value;
        }

        // UseImage 기능 키려는데 현재 Sample 버전일 경우 버전업 요구
        if (value >= 1)  // 1: on, 2: force 둘 다 체크
        {
            bool chk = await InstallStatusManager.Instance.CheckAndOperateFullAsync();
            if (!chk) 
            {
                // 안내했을 경우, 0으로 반환
                return 0;
            }
        }

        return value;
    }

    private int getAiVoiceFilterScenario(int value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getAiVoiceFilterFilterScenario - DEV MODE로 통과");
            return value;
        }

        if (value == 2)
        {
            // TODO : User Only Voice 관련 통신해서 내용있나 확인 또는 갱신 시나리오

            // 현재는 아직 준비 안되었다고 돌려보내기
            StartCoroutine(ScenarioCommonManager.Instance.Run_C99_NotReady());

            return 1;
        }
        return value;
    }

    // 글로벌 핫키 설정 필터 (Windows 전용)
    private bool getHotKeyGlobalInputFilterScenario(bool value)
    {
        // DEV MODE 일경우 로그 출력 후 value return
        if (settings.isDevMode)
        {   
            Debug.Log($"[SettingManager] getHotKeyGlobalInputFilterScenario - DEV MODE로 통과");
            return value;
        }

        // 글로벌 핫키 활성화 시도 시, Windows가 아닐 경우 안내
        if (value)
        {
#if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
            StartCoroutine(ScenarioCommonManager.Instance.Run_C99_WindowsOnly());
            return false;
#endif
        }
        return value;
    }

    // STT Server 설정 필터 (Lite 이상 필요)
    private async Task<bool> getIsSTTServerFilterScenarioAsync(bool value)
    {
        // STT Server 기능 키려는데 현재 Sample 버전일 경우 버전업 요구
        if (!await InstallStatusManager.Instance.CheckAndOperateLiteAsync())
        {
            return false;
        }
        return value;
    }

    // idx를 서버타입으로 변환; 0: Auto, 1: Local, 2: Google, 9: Custom
    private string getServerTypeFromIdx(int idx)
    {
        switch (idx)
        {
            case 0: return "Auto";
            case 1: return "Local";
            case 2: return "Google";
            // case 3: return "OpenRouter";
            case 9: return "Custom";
            default: return "Auto";
        }
    }
    
    // idx를 로컬 서버 모드로 변환; 0: CPU, 1: GPU
    private string getLocalServerModeFromIdx(int idx)
    {
        switch (idx)
        {
            case 0: return "CPU";
            case 1: return "GPU";
            default: return "CPU";
        }
    }

    // idx를 OnOFF로 변환; // 0 : off, 1 : on, 2: force
    private string getONOFFTypeFromIdx(int idx) {
        string lang = "off";
        if (idx ==  1) {
            lang = "on";
        }
        if (idx ==  2) {
            lang = "force";
        }
        return lang;
    }

    // 시스템 언어 자동 감지 (ko, jp, 그외 en)
    private string GetSystemLanguage()
    {
        string detectedLanguage = "en"; // 기본값

        try
        {
            // Unity 기본 시스템 언어 감지 시도
            try
            {
                SystemLanguage systemLang = Application.systemLanguage;
                Debug.Log($"Unity System Language: {systemLang}");
                
                switch (systemLang)
                {
                    case SystemLanguage.Korean:
                        detectedLanguage = "ko";
                        Debug.Log("Detected Korean from Unity SystemLanguage");
                        return detectedLanguage;
                    
                    case SystemLanguage.Japanese:
                        detectedLanguage = "jp";
                        Debug.Log("Detected Japanese from Unity SystemLanguage");
                        return detectedLanguage;
                    
                    default:
                        detectedLanguage = "en";
                        Debug.Log($"Detected other language ({systemLang}) from Unity SystemLanguage, using English");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Unity SystemLanguage detection failed: {ex.Message}");
            }

            // .NET CultureInfo를 이용한 추가 감지 시도
            try
            {
                CultureInfo currentCulture = CultureInfo.CurrentCulture;
                Debug.Log($"Current Culture: {currentCulture.Name}");
                
                string cultureName = currentCulture.Name.ToLower();
                
                if (cultureName.StartsWith("ko") || cultureName.Contains("korea"))
                {
                    detectedLanguage = "ko";
                    Debug.Log("Detected Korean from CultureInfo");
                    return detectedLanguage;
                }
                else if (cultureName.StartsWith("ja") || cultureName.Contains("japan"))
                {
                    detectedLanguage = "jp";
                    Debug.Log("Detected Japanese from CultureInfo");
                    return detectedLanguage;
                }
                else
                {
                    detectedLanguage = "en";
                    Debug.Log($"Detected other culture ({cultureName}), using English");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CultureInfo detection failed: {ex.Message}");
            }

            // UI Culture를 이용한 추가 감지 시도
            try
            {
                CultureInfo uiCulture = CultureInfo.CurrentUICulture;
                Debug.Log($"Current UI Culture: {uiCulture.Name}");
                
                string uiCultureName = uiCulture.Name.ToLower();
                
                if (uiCultureName.StartsWith("ko") || uiCultureName.Contains("korea"))
                {
                    detectedLanguage = "ko";
                    Debug.Log("Detected Korean from UI CultureInfo");
                    return detectedLanguage;
                }
                else if (uiCultureName.StartsWith("ja") || uiCultureName.Contains("japan"))
                {
                    detectedLanguage = "jp";
                    Debug.Log("Detected Japanese from UI CultureInfo");
                    return detectedLanguage;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"UI CultureInfo detection failed: {ex.Message}");
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // Android 전용 추가 감지 시도
            try
            {
                using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
                {
                    using (AndroidJavaObject defaultLocale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
                    {
                        string language = defaultLocale.Call<string>("getLanguage");
                        string country = defaultLocale.Call<string>("getCountry");
                        
                        Debug.Log($"Android Locale - Language: {language}, Country: {country}");
                        
                        if (language.ToLower().Equals("ko") || country.ToLower().Equals("kr"))
                        {
                            detectedLanguage = "ko";
                            Debug.Log("Detected Korean from Android Locale");
                            return detectedLanguage;
                        }
                        else if (language.ToLower().Equals("ja") || country.ToLower().Equals("jp"))
                        {
                            detectedLanguage = "jp";
                            Debug.Log("Detected Japanese from Android Locale");
                            return detectedLanguage;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Android Locale detection failed: {ex.Message}");
            }
#endif

        }
        catch (Exception ex)
        {
            Debug.LogError($"Critical error in GetSystemLanguage: {ex.Message}");
        }

        Debug.Log($"Final detected language: {detectedLanguage}");
        return detectedLanguage;
    }


    // 설정 데이터를 JSON 파일에서 불러오기
    public void LoadSettings()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        configFilePath = Path.Combine(directoryPath, "settings.json");

        try
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                JsonUtility.FromJsonOverwrite(json, settings);
            }
            else
            {
                SetDefaultValues();
                SaveSettings();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to load settings: " + e.Message);
            SetDefaultValues();
            SaveSettings();
        }
        Debug.Log("LoadSettings End");

        // 로딩 후 Dev 값 초기화
        settings.isDevMode = false;
        settings.isDevSound = false;
    }

    // UI 세팅 적용
    private void SetUIAfterLoading()
    {
        playerNameInputField.text = settings.player_name;
        uiLangDropdown.value = settings.ui_language_idx;
        operatorTypeDropdown.value = settings.operator_type_idx;
        isAlwaysOnTopToggle.isOn = settings.isAlwaysOnTop;
        isShowChatBoxOnClickToggle.isOn = settings.isShowChatBoxOnClick;
        isShowTutorialOnChatToggle.isOn = settings.isShowTutorialOnChat;
        isStartServerOnInitToggle.isOn = settings.isStartServerOnInit;
        isSTTServerToggle.isOn = settings.isSTTServer;
        editSttInChatInputToggle.isOn = settings.editSttinChatInput;
        enableEditionUpdateSuggestionToggle.isOn = settings.enableEditionUpdateSuggestion;
        hotKeyGlobalInputToggle.isOn = settings.hotKeyGlobalInputEnabled;

        charSizeSlider.value = settings.char_size;
        charSpeedSlider.value = settings.char_speed;
        charMobilitySlider.value = settings.char_mobility;
        isCharAutoSmallTalk.isOn = settings.isCharAutoSmallTalk;
        charAutoSmallTalkSlider.value = settings.charAutoSmallTalkInterval;
        isGravityToggle.isOn = settings.isGravity;
        isWindowsCollisionToggle.isOn = settings.isWindowsCollision;
        isStartWithLastCharToggle.isOn = settings.isStartWithLastChar;
        isRememberCharOutfitsToggle.isOn = settings.isRememberCharOutfits;
        
        soundLanguageDropdown.value = settings.sound_language_idx;
        soundVolumeMasterSlider.value = settings.sound_volumeMaster;
        soundSpeedMasterSlider.value = settings.sound_speedMaster;

        // server_type_idx를 드롭다운 인덱스로 변환하여 설정
        if (ServerTypeIdxToDropdownIndex.ContainsKey(settings.server_type_idx))
        {
            serverTypeDropdown.value = ServerTypeIdxToDropdownIndex[settings.server_type_idx];
        }
        else
        {
            serverTypeDropdown.value = 0; // 기본값
        }
        serverLocalModeTypeDropdown.value = settings.server_local_mode_idx;
        serverIdInputField.text = settings.server_id;
        serverGeminiApiKeyInputField.text = settings.api_key_gemini;
        serverOpenRouterApiKeyInputField.text = settings.api_key_openRouter;
        serverOpenRouterModelNameInputField.text = settings.model_name_OpenRouter;
        serverCustomModelNameInputField.text = settings.model_name_Custom;
        aiWebSearchDropdown.value = settings.ai_web_search_idx;
        aiUseImageDropdown.value = settings.ai_use_image_idx;
        aiAskIntentDropdown.value = settings.ai_ask_intent_idx;
        isAskedTurnOnServerToggle.isOn = settings.isAskedTurnOnServer;
        isAPITestToggle.isOn = settings.isAPITest;
        confirmUserIntentToggle.isOn = settings.confirmUserIntent;
        includeCharInScreenshotToggle.isOn = settings.includeCharInScreenshot;
        includeUIInScreenshotToggle.isOn = settings.includeUIInScreenshot;
        isAskChangeToMultimodalToggle.isOn = settings.isAskChangeToMultimodal;
        aiLangDropdown.value = settings.ai_language_idx;
        aiEmotionDropdown.value = settings.ai_emotion_idx;
        aiVoiceFilterDropdown.value = settings.ai_voice_filter_idx;

        devHowlingToggle.isOn = settings.isDevHowling;
        devModeToggle.isOn = false; // DevMode는 저장과 무관하게 항상 false
        devSoundToggle.isOn = false; // DevSound는 저장과 무관하게 항상 false

        UpdateOCROptionToggles();

        // Text 계열
        soundSpeedMasterText.text = "Speed (" + (int)settings.sound_speedMaster + "%)";
        charSizeText.text = "Size (" + (int)settings.char_size + "%)";
        RefreshAIInfoText("", "", "", "", "", "", "");
        
        // Auto Small Talk UI 업데이트
        UpdateAutoSmallTalkUI();

        // 초기값일 경우 UI가 반영되지 않으므로 한번 더 호출
        LanguageManager.Instance.SetUILanguage();
    }

    // 설정 데이터를 JSON 파일에 저장하는 함수
    public void SaveSettings()
    {
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        configFilePath = Path.Combine(directoryPath, "settings.json");
        try
        {
            // 설정 데이터를 JSON으로 변환
            string json = JsonUtility.ToJson(settings, true);

            // JSON 파일 쓰기
            File.WriteAllText(configFilePath, json);
            // Debug.Log("Settings saved successfully : \n" + json);
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError("Access denied: " + e.Message);
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.LogError("Directory not found: " + e.Message);
        }
        catch (IOException e)
        {
            Debug.LogError("I/O error occurred: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("An error occurred: " + e.Message);
        }
    }

    // idx를 서버타입으로 변환; 0: Auto, 1: Local, 2: Google, 9: Custom
    public void SetServerUIFromServerType(int idx)
    {
        // 모든 서버 UI 비활성화
        serverUIAutoGameObject.SetActive(false);
        serverUILocalGameObject.SetActive(false);
        serverUIGoogleGameObject.SetActive(false);
        serverUIOpenRouterGameObject.SetActive(false);
        serverUIChatGPTGameObject.SetActive(false);
        serverUICustomGameObject.SetActive(false);
        serverGeminikeyTestResultText.text = "";
        serverOpenRouterkeyTestResultText.text = "";

        // 선택된 서버 타입의 UI만 활성화
        switch (idx)
        {
            case 0: // Auto
                serverUIAutoGameObject.SetActive(true);
                break;

            case 1: // Local
                serverUILocalGameObject.SetActive(true);
                break;

            case 2: // Google
                serverUIGoogleGameObject.SetActive(true);
                break;

            // case 3: // OpenRouter 
            //     serverUIOpenRouterGameObject.SetActive(true);
            //     break;

            case 9: // Custom
                serverUICustomGameObject.SetActive(true);
                break;

            default:
                break;
        }
    }

    // 서버 모델 보유시 아이콘 변경 및 버튼 활성화 제어
    public void SetServerUIFromServerModel(string modelFileName)
    {
        // Debug.Log(modelFileName);
        // 링크설정용
        // string streamingAssetsPath = Application.streamingAssetsPath;  // StreamingAssets 폴더 경로
        // string executablePath = Application.dataPath;  // Unity 실행 파일이 있는 폴더 경로
        // string jarvisServerPath = Path.Combine(Path.GetDirectoryName(executablePath), "jarvis_server_jp.exe");
        string exeDirectory = Path.GetDirectoryName(Application.dataPath);
        string modelPath = Path.Combine(exeDirectory, "model", modelFileName);

        Color notOwnedColor = new Color32(200, 200, 200, 255);
        Color ownedColor = new Color32(0, 0, 0, 255);

        bool modelExists = File.Exists(modelPath);

        if (modelExists)
        {
            serverLocalDownloadIconImage.color = ownedColor;
            LanguageManager.Instance.SetServerModelTypeDownloadTooltip(1);  // 0: Download 1: Owned
        }
        else
        {
            serverLocalDownloadIconImage.color = notOwnedColor;
            LanguageManager.Instance.SetServerModelTypeDownloadTooltip(0);  // 0: Download 1: Owned
        }

        // 모델이 있을 때만 Release/Load 버튼 활성화
        if (serverLocalReleaseButton != null)
            serverLocalReleaseButton.interactable = modelExists;
        
        if (serverLocalLoadButton != null)
            serverLocalLoadButton.interactable = modelExists;
    }

    // 기본값 설정
    private void SetDefaultValues()
    {
        Debug.Log("SetDefaultValues");
        settings.player_name = "Sensei";
        
        // 시스템 언어 자동 감지
        string systemLang = GetSystemLanguage();
        settings.ui_language = systemLang;
        
        // 언어에 따른 인덱스 설정
        switch (systemLang)
        {
            case "ko":
                settings.ui_language_idx = 0;
                break;
            case "jp":
                settings.ui_language_idx = 1;
                break;
            case "en":
            default:
                settings.ui_language_idx = 2;
                break;
        }
        settings.operator_type_idx = 0;
        settings.operator_type = "ARONA";
        settings.ai_language_idx = 2;
        settings.ai_language = "en";
        settings.ai_language_in = systemLang;  // 시스템 언어에 맞춰 설정
        settings.ai_language_out = systemLang;  // 시스템 언어에 맞춰 설정
        settings.ai_voice_filter_idx = 1;  // Skip AI Voice
        settings.isAlwaysOnTop = false;
        settings.isShowChatBoxOnClick = true;
        settings.isShowTutorialOnChat = true;
        settings.isTutorialCompleted = false;
        settings.isStartServerOnInit = true;
        settings.isSTTServer = false;
        settings.editSttinChatInput = true;
        settings.enableEditionUpdateSuggestion = true;

        settings.char_size = 100;
        settings.char_lastUsed = "mari";
        settings.char_mobility = 5;
        settings.char_speed = 100;
        settings.isCharAutoSmallTalk = false;
        settings.charAutoSmallTalkInterval = 60; // 기본값 60초
        settings.isGravity = true;
        settings.isWindowsCollision = false;
        settings.isStartWithLastChar = true;
        settings.isRememberCharOutfits = false;

        // 글로벌 핫키 기본값 (윈도우만 true, 나머지는 false)
        settings.hotKeyGlobalInputEnabled = true;
#if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
        settings.hotKeyGlobalInputEnabled = false;
#endif

        // 사운드 언어는 기본값으로 일본어 음성 사용 (가장 품질이 좋음)
        settings.sound_language_idx = 1;  // jp
        settings.sound_language = "jp";
        settings.sound_volumeMaster = 70;
        settings.sound_speedMaster = 100;

        settings.server_type_idx = 0;  // 0: Auto, 1: Local, 2: Google, 3: OpenRouter
        settings.server_type = "Auto";
        settings.server_id = "temp";
        settings.api_key_gemini = "";
        settings.api_key_openRouter = "";
        settings.server_local_mode_idx = 0;  // 0: CPU, 1: GPU
        settings.server_local_mode = "CPU";
        settings.model_name_Local = "";
        settings.model_name_Gemini = "gemini-1.5-flash";
        settings.model_name_OpenRouter = "google/gemini-flash-1.5";
        settings.model_name_ChatGPT = "";
        settings.model_name_Custom = "";
        settings.ai_web_search_idx = 0;  // 0 : off, 1 : on, 2: force
        settings.ai_web_search = "off";
        settings.ai_use_image_idx = 0;  // 0 : off, 1 : on, 2: force
        settings.ai_use_image = "off";
        settings.ai_ask_intent_idx = 0;  // 0 : off, 1 : on
        settings.ai_ask_intent = "off";
        settings.ai_emotion_idx = 0;  // 0 : off, 1 : on
        settings.ai_emotion = "off";
        settings.isAskedTurnOnServer = true;
        settings.isAPITest = false;
        settings.confirmUserIntent = false;
        settings.includeCharInScreenshot = false;
        settings.includeUIInScreenshot = false;
        settings.isAskChangeToMultimodal = true;

        settings.isDevHowling = false;

        settings.wantFreeServer = false;  // 무료서버연결의향
        
        // OCR
        settings.ocrOptionType = "Options1";  // 기본값: Options1
    }

    public void AskReturnToDefaultValues()
    { 
        StartCoroutine(ScenarioCommonManager.Instance.Run_C98_confirm_return_to_default_proceed());
    }

    // 기본값으로 변경 (Sample 기준)
    public void ReturnToDefaultValues()
    {
        // InstallStatusManager를 통해 설치 상태를 확인하고 UI에 반영
        int installStatusIndex = InstallStatusManager.Instance.GetInstallStatusIndex();

        switch (installStatusIndex)
        {
            case 0: // sample
                    // SetDefaultValues();
            case 1: // lite
                    // SetDefaultValues();
            case 2: // full
                    // SetDefaultValues();
            default:
                SetDefaultValues();

                // 변경 내용 저장
                SaveSettings();

                // 로딩후 UI
                SetUIAfterLoading();

                // 변동 UI 갱신
                SetServerType();
                SetServerModelType();

                // 안내 : 설정 완료되었습니다.
                StartCoroutine(ScenarioCommonManager.Instance.Scenario_C98_1_Approve_ReturnToDefault());
                break;
        }
    }
}
