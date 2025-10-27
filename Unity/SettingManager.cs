using TMPro;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;


public class SettingManager : MonoBehaviour
{
    private string current_platform; // "Editor", "Standalone", "Android" 등

    [Header("General")]
    [SerializeField] private Dropdown uiLangDropdown;
    [SerializeField] private Dropdown operatorTypeDropdown;
    [SerializeField] private Dropdown platformInfoDropdown;
    [SerializeField] private Dropdown editionDropdown;
    [SerializeField] private Toggle isAlwaysOnTopToggle;
    [SerializeField] private Toggle isShowChatBoxOnClickToggle;
    [SerializeField] private Toggle isShowTutorialOnChatToggle;
    [SerializeField] private Toggle isStartServerOnInitToggle;

    [Header("Character")]
    [SerializeField] private Slider charSizeSlider;
    [SerializeField] private Slider charSpeedSlider;
    [SerializeField] private Slider charMobilitySlider;
    [SerializeField] private Toggle isGravityToggle;
    [SerializeField] private Toggle isWindowsCollisionToggle;
    [SerializeField] private Toggle isStartWithLastCharToggle;
    [SerializeField] private Toggle isRememberCharOutfitsToggle;

    [Header("Sound")]
    [SerializeField] private Dropdown soundLanguageDropdown;
    [SerializeField] private Slider soundVolumeMasterSlider;  // 현재는 마스터 볼륨만 있으면
    [SerializeField] private Slider soundSpeedMasterSlider;  // 현재는 마스터 볼륨만 있으면

    [Header("Server")]  // 구 Server Field. 변수도 관련 변수로 설정되어있음.
    [SerializeField] private Dropdown serverTypeDropdown;
    [SerializeField] private Dropdown serverModelTypeDropdown;
    [SerializeField] private Image serverModelTypeDropdownImage;
    [SerializeField] private GameObject serverModelTypeDropdownGameObject;
    [SerializeField] private Dropdown geminiModelTypeDropdown;
    [SerializeField] private GameObject geminiModelTypeDropdownGameObject;
    [SerializeField] private Dropdown openRouterModelTypeDropdown;
    [SerializeField] private GameObject openRouterModelTypeDropdownGameObject;
    [SerializeField] private TMP_InputField serverIdInputField;
    [SerializeField] private GameObject serverIdInputGameObject;
    [SerializeField] private TMP_InputField serverGeminiApiKeyInputField;
    [SerializeField] private GameObject serverGeminiApiKeyInputFieldGameObject;
    [SerializeField] private TMP_InputField serverOpenRouterApiKeyInputField;
    [SerializeField] private GameObject serverOpenRouterApiKeyInputFieldGameObject;
    [SerializeField] private GameObject keyTestGameObject;
    [SerializeField] private Text keyTestResultText;
    [SerializeField] private Toggle isAskedTurnOnServerToggle;  // Lite, Full 일때 활성화 되는 무언가. 그냥 문답무용으로 키는 방향으로 진행했을때의 영향도도 파악 필요.

    [Header("Conversation")]  // 구 AI Field. 변수도 관련 변수로 설정되어있음.
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Dropdown aiWebSearchDropdown;
    [SerializeField] private Dropdown aiAskIntentDropdown;
    [SerializeField] private Dropdown aiLangDropdown;
    [SerializeField] private Dropdown aiEmotionDropdown;
    [SerializeField] private Dropdown aiVoiceFilterDropdown;
    [SerializeField] private Toggle isAPITestToggle;
    [SerializeField] private Toggle confirmUserIntentToggle;

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

    // 기타 표시용 UI
    [Header("Extra UI")]
    public Text soundSpeedMasterText;
    public Text serverInfoText;
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

        public string char_lastUsed;
        public float char_size;
        public float char_mobility;
        public float char_speed;
        public bool isGravity;
        public bool isWindowsCollision;
        public bool isStartWithLastChar;
        public bool isRememberCharOutfits;

        public int sound_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string sound_language;
        public float sound_volumeMaster;
        public float sound_speedMaster;

        public int server_type_idx;  // 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
        public string server_type;
        public string server_id;
        public string api_key_gemini;
        public string api_key_openRouter;
        public string model_type;  // qwen-14b 등
        
        public int ai_web_search_idx;  // 0 : off, 1 : on, 2: force
        public string ai_web_search;
        public int ai_ask_intent_idx;  // 0 : off, 1 : on
        public string ai_ask_intent;
        public int ai_emotion_idx;  // 0 : off, 1 : on
        public string ai_emotion;
        public string ai_language;
        public bool isAskedTurnOnServer;
        public bool isAPITest;
        public bool confirmUserIntent;

        // Dev용 데이터
        public bool isDevMode;
        public bool isDevHowling;  

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

    public void SetCharLastUsed(string value) { settings.char_lastUsed = value; SaveSettings(); }
    public void SetCharSize(float value) { settings.char_size = value; CharManager.Instance.setCharSize(); SaveSettings(); charSizeText.text="Size ("+(int)settings.char_size+")";}
    public void SetCharSpeed(float value) { settings.char_speed = value; SaveSettings(); }
    public void SetCharMobility(float value) { settings.char_mobility = value; SaveSettings(); }
    public void SetIsGravity(bool value) { settings.isGravity = value; SaveSettings(); }
    public void SetIsWindowsCollision(bool value) { settings.isWindowsCollision = value; WindowCollisionManager.Instance.SetWindowsRectChecking(value); SaveSettings(); }
    public void SetIsStartWithLastChar(bool value) { settings.isStartWithLastChar = value; SaveSettings(); }
    public void SetIsRememberCharOutfits(bool value) { settings.isRememberCharOutfits = value; SaveSettings(); }

    public void SetSoundLanguageType() { int value=soundLanguageDropdown.value; settings.sound_language_idx = value; settings.sound_language=getLangFromIdx(value); SaveSettings(); }
    public void SetSoundVolumeMaster(float value) { settings.sound_volumeMaster = value; SaveSettings(); }
    public void SetSoundSpeedMaster(float value) { settings.sound_speedMaster = value; SaveSettings(); soundSpeedMasterText.text="Speed (" + (int)settings.sound_speedMaster + "%)";}

    public void SetServerType() { int value=serverTypeDropdown.value; settings.server_type_idx = value; settings.server_type=getServerTypeFromIdx(value); LanguageManager.Instance.SetServerServerTypeInfoTooltip(value); SetServerUIFromServerType(value); SaveSettings(); LanguageManager.Instance.SetUILanguage(); }
    public void SetServerTypeByValue(int value) { serverTypeDropdown.value = value; settings.server_type_idx = value; settings.server_type=getServerTypeFromIdx(value); LanguageManager.Instance.SetServerServerTypeInfoTooltip(value); SetServerUIFromServerType(value); SaveSettings(); LanguageManager.Instance.SetUILanguage(); }
    public void SetServerID(string value) { settings.server_id = value; SaveSettings(); }
    public void SetAPIKeyGemini(string value) { settings.api_key_gemini = value; SaveSettings(); }
    public void SetAPIKeyOpenRouter(string value) { settings.api_key_openRouter = value; SaveSettings(); }
    public void SetServerModelType() { int value = serverModelTypeDropdown.value; string displayName = serverModelTypeDropdown.options[value].text; settings.model_type = ServerModelData.GetIdByDisplayName(displayName); SetServerUIFromServerModel(ServerModelData.GetFileNameByDisplayName(displayName)); SaveSettings(); }

    public void SetAIWebSearch() { int value=aiWebSearchDropdown.value; value = getAiWebSearchFilterScenario(value); aiWebSearchDropdown.value = value; settings.ai_web_search_idx = value; settings.ai_web_search = getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetAIAskIntent() { int value=aiAskIntentDropdown.value; settings.ai_ask_intent_idx = value; settings.server_type=getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetIsAskedTurnOnServer(bool value) { settings.isAskedTurnOnServer = value; SaveSettings(); }
    public void SetIsAPITest(bool value) { settings.isAPITest = value; SaveSettings(); }
    public void SetConfirmUserIntent(bool value) { settings.confirmUserIntent = value; SaveSettings(); }  // SetAIAskIntent Toggle 버전
    public void SetAiLanguage() { int value=aiLangDropdown.value; settings.ai_language_idx = value; settings.ai_language=getAiLangFromIdx(value); SaveSettings(); }
    public void SetAIEmotion() { int value=aiEmotionDropdown.value; value = getAiEmotionFilterScenario(value); aiEmotionDropdown.value = value; settings.ai_emotion_idx = value; settings.ai_emotion = getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetAiVoiceFilter() { int value=aiVoiceFilterDropdown.value; value = getAiVoiceFilterScenario(value); aiVoiceFilterDropdown.value = value; settings.ai_voice_filter_idx = value; SaveSettings(); }

    public void SetIsDevModeToggle(bool value) { settings.isDevMode = value; DevManager.Instance.SetInteractableDev(value);}
    public void SetIsDevHowlingToggle(bool value) { settings.isDevHowling = value; SaveSettings(); }  
    
    // 표시용
    public void SetServerInfoText(string text) { serverInfoText.text = text; }
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

        // 서버 상태 세팅
        InstallStatusManager.Instance.LoadInstallStatus();
        SetInstallStatus();

        // 로딩후 UI
        SetUIAfterLoading();

        // 변동 UI 갱신
        SetServerType();
        SetServerModelType();


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
        serverModelTypeDropdown.ClearOptions();

        List<string> optionNames = new List<string>();
        foreach (var model in ServerModelData.ModelOptions)
        {
            optionNames.Add(model.DisplayName);
        }

        serverModelTypeDropdown.AddOptions(optionNames);

        // 저장된 모델 ID를 기반으로 선택 인덱스를 설정
        int selectedIndex = ServerModelData.ModelOptions.FindIndex(m => m.Id == settings.model_type);
        if (selectedIndex >= 0)
        {
            serverModelTypeDropdown.value = selectedIndex;

            // 다운로드 UI 갱신
            // int value = serverModelTypeDropdown.value;
            // string displayName = serverModelTypeDropdown.options[value].text;
            // string modelFileName = ServerModelData.GetFileNameByDisplayName(displayName);
            // SetServerUIFromServerModel(modelFileName);
            // SaveSettings();
        }
        else
        {
            serverModelTypeDropdown.value = 0;
            settings.model_type = ServerModelData.ModelOptions[0].Id;
        }
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

    private int getAiEmotionFilterScenario(int value)
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
            bool chk = InstallStatusManager.Instance.CheckAndOperateFull();
            if (!chk) 
            {
                // 안내했을 경우, 0으로 반환
                return 0;
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

    private int getAiWebSearchFilterScenario(int value)
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
            bool chk = InstallStatusManager.Instance.CheckAndOperateFull();
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

    // idx를 서버타입으로 변환; 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
    private string getServerTypeFromIdx(int idx)
    {
        switch (idx)
        {
            case 0: return "Auto";
            case 1: return "Server";
            case 2: return "Free_Gemini";
            case 3: return "Free_OpenRouter";
            case 4: return "Paid_Gemini";
            default: return "Auto";
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

        charSizeSlider.value = settings.char_size;
        charSpeedSlider.value = settings.char_speed;
        charMobilitySlider.value = settings.char_mobility;
        isGravityToggle.isOn = settings.isGravity;
        isWindowsCollisionToggle.isOn = settings.isWindowsCollision;
        isStartWithLastCharToggle.isOn = settings.isStartWithLastChar;
        isRememberCharOutfitsToggle.isOn = settings.isRememberCharOutfits;
        
        soundLanguageDropdown.value = settings.sound_language_idx;
        soundVolumeMasterSlider.value = settings.sound_volumeMaster;
        soundSpeedMasterSlider.value = settings.sound_speedMaster;

        serverTypeDropdown.value = settings.server_type_idx;
        serverIdInputField.text = settings.server_id;
        serverGeminiApiKeyInputField.text = settings.api_key_gemini;
        serverOpenRouterApiKeyInputField.text = settings.api_key_openRouter;
        aiWebSearchDropdown.value = settings.ai_web_search_idx;
        aiAskIntentDropdown.value = settings.ai_ask_intent_idx;
        isAskedTurnOnServerToggle.isOn = settings.isAskedTurnOnServer;
        isAPITestToggle.isOn = settings.isAPITest;
        confirmUserIntentToggle.isOn = settings.confirmUserIntent;
        aiLangDropdown.value = settings.ai_language_idx;
        aiEmotionDropdown.value = settings.ai_emotion_idx;
        aiVoiceFilterDropdown.value = settings.ai_voice_filter_idx;

        devHowlingToggle.isOn = settings.isDevHowling;
        devModeToggle.isOn = false; // DevMode는 저장과 무관하게 항상 false

        // Text 계열
        soundSpeedMasterText.text = "Speed (" + (int)settings.sound_speedMaster + "%)";
        charSizeText.text = "Size (" + (int)settings.char_size + "%)";
        RefreshAIInfoText("", "", "", "", "", "", "");

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

    // idx를 서버타입으로 변환; 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
    public void SetServerUIFromServerType(int idx)
    {
        // 모든 UI 요소 비활성화
        serverModelTypeDropdownGameObject.SetActive(false);
        geminiModelTypeDropdownGameObject.SetActive(false);
        openRouterModelTypeDropdownGameObject.SetActive(false);
        serverIdInputGameObject.SetActive(false);
        serverGeminiApiKeyInputFieldGameObject.SetActive(false);
        serverOpenRouterApiKeyInputFieldGameObject.SetActive(false);
        keyTestGameObject.SetActive(false);
        keyTestResultText.text = "";

        switch (idx)
        {
            case 0: // Auto
                break;

            case 1: // Server
                serverModelTypeDropdownGameObject.SetActive(true);
                serverIdInputGameObject.SetActive(true);
                break;

            case 2: // Free_Gemini
            case 4: // Paid_Gemini
                geminiModelTypeDropdownGameObject.SetActive(true);
                serverGeminiApiKeyInputFieldGameObject.SetActive(true);
                keyTestGameObject.SetActive(true);
                break;

            case 3: // Free_OpenRouter
                openRouterModelTypeDropdownGameObject.SetActive(true);
                serverOpenRouterApiKeyInputFieldGameObject.SetActive(true);
                keyTestGameObject.SetActive(true);
                break;

            default:
                break;
        }
    }

    // 서버 모델 보유시 아이콘 변경
    public void SetServerUIFromServerModel(string modelFileName)
    {
        // Debug.Log(modelFileName);
        // 링크설정용
        // string streamingAssetsPath = Application.streamingAssetsPath;  // StreamingAssets 폴더 경로
        // string executablePath = Application.dataPath;  // Unity 실행 파일이 있는 폴더 경로
        // string jarvisServerPath = Path.Combine(Path.GetDirectoryName(executablePath), "jarvis_server_jp.exe");
        string modelPath = Path.Combine(Application.streamingAssetsPath, "model", modelFileName);

        Color notOwnedColor = new Color32(200, 200, 200, 255);
        Color ownedColor = new Color32(0, 0, 0, 255);

        if (File.Exists(modelPath))
        {
            serverModelTypeDropdownImage.color = ownedColor;
            LanguageManager.Instance.SetServerModelTypeDownloadTooltip(1);  // 0: Download 1: Owned
        }
        else
        {
            serverModelTypeDropdownImage.color = notOwnedColor;
            LanguageManager.Instance.SetServerModelTypeDownloadTooltip(0);  // 0: Download 1: Owned
        }
        
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

        settings.char_size = 100;
        settings.char_lastUsed = "mari";
        settings.char_mobility = 5;
        settings.char_speed = 100;
        settings.isGravity = true;
        settings.isWindowsCollision = false;
        settings.isStartWithLastChar = true;
        settings.isRememberCharOutfits = false;

        // 사운드 언어는 기본값으로 일본어 음성 사용 (가장 품질이 좋음)
        settings.sound_language_idx = 1;  // jp
        settings.sound_language = "jp";
        settings.sound_volumeMaster = 70;
        settings.sound_speedMaster = 100;

        settings.server_type_idx = 2;  // 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
        settings.server_type = "Auto";
        settings.server_id = "temp";
        settings.api_key_gemini = "";
        settings.api_key_openRouter = "";
        settings.ai_web_search_idx = 0;  // 0 : off, 1 : on, 2: force
        settings.ai_web_search = "OFF";
        settings.ai_ask_intent_idx = 0;  // 0 : off, 1 : on
        settings.ai_ask_intent = "OFF";
        settings.ai_emotion_idx = 0;  // 0 : off, 1 : on
        settings.ai_emotion = "OFF";
        settings.isAskedTurnOnServer = true;
        settings.isAPITest = false;
        settings.confirmUserIntent = false;

        settings.isDevHowling = false;

        settings.wantFreeServer = false;  // 무료서버연결의향
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
