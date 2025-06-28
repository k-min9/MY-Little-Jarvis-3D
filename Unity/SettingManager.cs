using TMPro;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class SettingManager : MonoBehaviour
{
    private string current_platform; // "Editor", "Standalone", "Android" 등

    [Header("General")]
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Dropdown platformInfoDropdown;
    [SerializeField] private Dropdown uiLangDropdown;
    [SerializeField] private Toggle isAlwaysOnTopToggle;
    [SerializeField] private Toggle isShowChatBoxOnClickToggle;
    [SerializeField] private Toggle isShowTutorialOnChatToggle;

    [Header("Character")]
    [SerializeField] private Slider charSizeSlider;
    [SerializeField] private Slider charSpeedSlider;
    [SerializeField] private Slider charMobilitySlider;
    [SerializeField] private Toggle isGravityToggle;
    [SerializeField] private Toggle isWindowsCollisionToggle;

    [Header("Sound")]
    [SerializeField] private Dropdown soundLanguageDropdown;
    [SerializeField] private Slider soundVolumeMasterSlider;  // 현재는 마스터 볼륨만 있으면
    [SerializeField] private Slider soundSpeedMasterSlider;  // 현재는 마스터 볼륨만 있으면

    [Header("Server")]
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

    [Header("AI")]
    [SerializeField] private Dropdown aiWebSearchDropdown;
    [SerializeField] private Dropdown aiAskIntentDropdown;
    [SerializeField] private Dropdown aiLangDropdown;
    [SerializeField] private Toggle isAskedTurnOnServerToggle;
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

    // 기타 표시용 UI
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
        public int ai_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string ai_language_in;
        public string ai_language_out;
        public bool isAlwaysOnTop;
        public bool isShowChatBoxOnClick;
        public bool isShowTutorialOnChat;

        public string char_lastUsed;
        public float char_size;
        public float char_mobility;
        public float char_speed;
        public bool isGravity;
        public bool isWindowsCollision;

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
        public string ai_language;
        public bool isAskedTurnOnServer;
        public bool isAPITest;
        public bool confirmUserIntent;

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
    public void SetIsShowTutorialOnChat(bool value) {settings.isShowTutorialOnChat = value; SaveSettings(); }

    public void SetCharLastUsed(string value) { settings.char_lastUsed = value; SaveSettings(); }
    public void SetCharSize(float value) { settings.char_size = value; CharManager.Instance.setCharSize(); SaveSettings(); charSizeText.text="Size ("+(int)settings.char_size+")";}
    public void SetCharSpeed(float value) { settings.char_speed = value; SaveSettings(); }
    public void SetCharMobility(float value) { settings.char_mobility = value; SaveSettings(); }
    public void SetIsGravity(bool value) { settings.isGravity = value; SaveSettings(); }
    public void SetIsWindowsCollision(bool value) { settings.isWindowsCollision = value; WindowCollisionManager.Instance.SetWindowsRectChecking(value); SaveSettings(); }

    public void SetSoundLanguageType() { int value=soundLanguageDropdown.value; settings.sound_language_idx = value; settings.sound_language=getLangFromIdx(value); SaveSettings(); }
    public void SetSoundVolumeMaster(float value) { settings.sound_volumeMaster = value; SaveSettings(); }
    public void SetSoundSpeedMaster(float value) { settings.sound_speedMaster = value; SaveSettings(); soundSpeedMasterText.text="Speed (" + (int)settings.sound_speedMaster + "%)";}

    public void SetServerType() { int value=serverTypeDropdown.value; settings.server_type_idx = value; settings.server_type=getServerTypeFromIdx(value); LanguageManager.Instance.SetServerServerTypeInfoTooltip(value); SetServerUIFromServerType(value); SaveSettings(); LanguageManager.Instance.SetUILanguage(); }
    public void SetServerID(string value) { settings.server_id = value; SaveSettings(); }
    public void SetAPIKeyGemini(string value) { settings.api_key_gemini = value; SaveSettings(); }
    public void SetAPIKeyOpenRouter(string value) { settings.api_key_openRouter = value; SaveSettings(); }
    public void SetServerModelType() { int value = serverModelTypeDropdown.value; string displayName = serverModelTypeDropdown.options[value].text; settings.model_type = ServerModelData.GetIdByDisplayName(displayName); SetServerUIFromServerModel(ServerModelData.GetFileNameByDisplayName(displayName)); SaveSettings(); }    
    public void SetAIWebSearch() { int value=aiWebSearchDropdown.value; settings.ai_web_search_idx = value; settings.ai_web_search=getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetAIAskIntent() { int value=aiAskIntentDropdown.value; settings.ai_ask_intent_idx = value; settings.server_type=getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetIsAskedTurnOnServer(bool value) { settings.isAskedTurnOnServer = value; SaveSettings(); }
    public void SetIsAPITest(bool value) { settings.isAPITest = value; SaveSettings(); }
    public void SetConfirmUserIntent(bool value) { settings.confirmUserIntent = value; SaveSettings(); }  // SetAIAskIntent Toggle 버전
    public void SetAiLanguage() { int value=aiLangDropdown.value; settings.ai_language_idx = value; settings.ai_language=getAiLangFromIdx(value); SaveSettings(); }


    // 표시용
    public void SetServerInfoText(string text) {serverInfoText.text=text;}
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
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

        // 저장 경로를 Application.persistentDataPath로 설정
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        configFilePath = Path.Combine(directoryPath, "settings.json");

        // 디렉토리가 없을 경우 생성
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        LoadSettings();

        // 플랫폼별 UI 적용
        # if UNITY_ANDROID
        isAlwaysOnTopToggle.gameObject.SetActive(false);
        # endif
    }

    void Start()
    {
        // 현재 플랫폼 세팅
        SetPlatformInfoDropdown();
        SetServerModelDropdownOptions();

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
        
    private void SetDefaultServerTypeByPlatform()
    {
        serverTypeDropdown.value = 2; // Server
#if UNITY_EDITOR
        serverTypeDropdown.value = 1; // Local
#elif UNITY_STANDALONE
            serverTypeDropdown.value = 1; // Local
#else
            serverTypeDropdown.value = 2; // Server
#endif

        settings.server_type_idx = serverTypeDropdown.value;
        settings.server_type = getServerTypeFromIdx(serverTypeDropdown.value);
    }

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
    
    // idx를 추론설정이름으로 변환; 0 : normal, 1 : prefer, 2 : ko, 3 : jp, 4 : en
    private string getAiLangFromIdx(int idx) {
        string lang = "normal";
        if (idx ==  1) {
            lang = "prefer";
        }
        if (idx ==  2) {
            lang = "ko";
        }
        if (idx ==  3) {
            lang = "jp";
        }
        if (idx ==  4) {
            lang = "en";
        }
        return lang;
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


    // 설정 데이터를 JSON 파일에서 불러오기
    private void LoadSettings()
    {
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
    }

    // UI 세팅 적용
    private void SetUIAfterLoading()
    {
        playerNameInputField.text = settings.player_name;
        uiLangDropdown.value = settings.ui_language_idx;
        aiLangDropdown.value = settings.ai_language_idx;
        isAlwaysOnTopToggle.isOn = settings.isAlwaysOnTop;
        isShowChatBoxOnClickToggle.isOn = settings.isShowChatBoxOnClick;
        isShowTutorialOnChatToggle.isOn = settings.isShowTutorialOnChat;

        charSizeSlider.value = settings.char_size;
        charSpeedSlider.value = settings.char_speed;
        charMobilitySlider.value = settings.char_mobility;
        isGravityToggle.isOn = settings.isGravity;
        isWindowsCollisionToggle.isOn = settings.isWindowsCollision;

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

        // Text 계열
        soundSpeedMasterText.text="Speed (" + (int)settings.sound_speedMaster + "%)";
        charSizeText.text="Size ("+(int)settings.char_size+"%)";
        RefreshAIInfoText("","","","","","","");

        // 초기값일 경우 UI가 반영되지 않으므로 한번 더 호출
        LanguageManager.Instance.SetUILanguage();
    }

    // 설정 데이터를 JSON 파일에 저장하는 함수
    public void SaveSettings()
    {
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
        Debug.Log(modelFileName);
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
        settings.player_name = "Sensei";
        settings.ui_language_idx = 0;
        settings.ui_language = "ko";
        settings.ai_language_idx = 2;
        settings.ai_language = "en";
        settings.ai_language_in = "ko";
        settings.ai_language_out = "ko";
        settings.isAlwaysOnTop = false;
        settings.isShowChatBoxOnClick = false;
        settings.isShowTutorialOnChat = true;

        settings.char_size = 100;
        settings.char_lastUsed = "mari";
        settings.char_mobility = 5;
        settings.char_speed = 100;
        settings.isGravity = true;
        settings.isWindowsCollision = false;

        settings.sound_language_idx = 1;  // jp
        settings.sound_language = "jp";
        settings.sound_volumeMaster = 70;
        settings.sound_speedMaster = 100;

        settings.server_type_idx = 0;  // 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
        settings.server_type = "Auto";
        settings.server_id = "temp";
        settings.api_key_gemini = "";
        settings.api_key_openRouter = "";
        settings.ai_web_search_idx = 0;  // 0 : off, 1 : on, 2: force
        settings.ai_web_search = "OFF";
        settings.ai_ask_intent_idx = 0;  // 0 : off, 1 : on
        settings.ai_ask_intent = "OFF";
        settings.isAskedTurnOnServer = true;
        settings.isAPITest = false;
        settings.confirmUserIntent = false;

        settings.wantFreeServer = false;  // 무료서버연결의향
    }
}
