using TMPro;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingManager : MonoBehaviour
{

    // 입력값 정리
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TMP_InputField serverIdInputField;
    [SerializeField] private Dropdown uiLangDropdown;
    [SerializeField] private Dropdown aiLangDropdown;
    [SerializeField] private Toggle isAlwaysOnTopToggle;
    [SerializeField] private Toggle isShowChatBoxOnClickToggle;

    [SerializeField] private Slider charSizeSlider;
    [SerializeField] private Slider charSpeedSlider;
    [SerializeField] private Slider charMobilitySlider;
    [SerializeField] private Toggle isGravityToggle;
    [SerializeField] private Toggle isWindowsCollisionToggle;

    [SerializeField] private Dropdown soundLanguageDropdown;
    [SerializeField] private Slider soundVolumeMasterSlider;  // 현재는 마스터 볼륨만 있으면
    [SerializeField] private Slider soundSpeedMasterSlider;  // 현재는 마스터 볼륨만 있으면

    [SerializeField] private Dropdown serverTypeDropdown;
    [SerializeField] private Dropdown aiWebSearchDropdown;
    [SerializeField] private Dropdown aiAskIntentDropdown;
    [SerializeField] private Toggle isAskedTurnOnServerToggle;
    [SerializeField] private Toggle isAPITest;

    // 표시용 UI
    public Text soundSpeedMasterText;
    public Text serverInfoText;


    // 설정 데이터 클래스
    [Serializable]
    public class SettingsData
    {
        public string player_name;
        public string server_id;
        public int ui_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string ui_language;  
        public int ai_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string ai_language;  
        public string ai_language_in;
        public string ai_language_out;
        public bool isAlwaysOnTop;
        public bool isShowChatBoxOnClick;

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

        public int server_type_idx;  // 0 : GPU, 1 : CPU
        public string server_type;
        public int ai_web_search_idx;  // 0 : off, 1 : on, 2: force
        public string ai_web_search;
        public int ai_ask_intent_idx;  // 0 : off, 1 : on
        public string ai_ask_intent;
        public bool isAskedTurnOnServer;
        public bool isAPITest;
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
    public void SetServerID(string value) { settings.server_id = value; SaveSettings(); }
    public void SetUiLanguage() { int value=uiLangDropdown.value; settings.ui_language_idx = value; settings.ui_language=getLangFromIdx(value); LanguageManager.Instance.SetUILanguage(); SaveSettings(); }
    public void SetAiLanguage() { int value=aiLangDropdown.value; settings.ai_language_idx = value; settings.ai_language=getLangFromIdx(value); SaveSettings(); }
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

    public void SetCharLastUsed(string value) { settings.char_lastUsed = value; SaveSettings(); }
    public void SetCharSize(float value) { settings.char_size = value; CharManager.Instance.setCharSize(); SaveSettings(); }
    public void SetCharSpeed(float value) { settings.char_speed = value; SaveSettings(); }
    public void SetCharMobility(float value) { settings.char_mobility = value; SaveSettings(); }
    public void SetIsGravity(bool value) { settings.isGravity = value; SaveSettings(); }
    public void SetIsWindowsCollision(bool value) { settings.isWindowsCollision = value; WindowCollisionManager.Instance.SetWindowsRectChecking(value); SaveSettings(); }

    public void SetSoundLanguageType() { int value=soundLanguageDropdown.value; settings.sound_language_idx = value; settings.sound_language=getLangFromIdx(value); SaveSettings(); }
    public void SetSoundVolumeMaster(float value) { settings.sound_volumeMaster = value; SaveSettings(); }
    public void SetSoundSpeedMaster(float value) { settings.sound_speedMaster = value; SaveSettings(); soundSpeedMasterText.text="Speed (" + (int)settings.sound_speedMaster + "%)";}

    public void SetServerType() { int value=serverTypeDropdown.value; settings.server_type_idx = value; settings.server_type=getServerTypeFromIdx(value); SaveSettings(); }
    public void SetAIWebSearch() { int value=aiWebSearchDropdown.value; settings.ai_web_search_idx = value; settings.ai_web_search=getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetAIAskIntent() { int value=aiAskIntentDropdown.value; settings.ai_ask_intent_idx = value; settings.server_type=getONOFFTypeFromIdx(value); SaveSettings(); }
    public void SetIsAskedTurnOnServer(bool value) { settings.isAskedTurnOnServer = value; SaveSettings(); }
    public void SetIsAPITest(bool value) { settings.isAPITest = value; SaveSettings(); }

    // 표시용
    public void SetServerInfoText(string text) {serverInfoText.text=text;}


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
        SetUIAfterLoading();

        // 최상위에 두는지 확인하고 세팅
        WindowManager.SetWindowAlwaysOnTop(settings.isAlwaysOnTop); 
    }
    
    // idx를 언어이름으로 변환; 0 : ko, 1 : jp, 2: en
    private string getLangFromIdx(int idx) {
        string lang = "ko";
        if (idx ==  1) {
            lang = "jp";
        }
        if (idx ==  2) {
            lang = "en";
        }
        return lang;
    }

    // idx를 언어이름으로 변환; 0 : ko, 1 : jp, 2: en
    private string getServerTypeFromIdx(int idx) {
        string lang = "GPU";
        if (idx ==  1) {
            lang = "CPU";
        }
        return lang;
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
        serverIdInputField.text = settings.server_id;
        uiLangDropdown.value = settings.ui_language_idx;
        aiLangDropdown.value = settings.ai_language_idx;
        isAlwaysOnTopToggle.isOn = settings.isAlwaysOnTop;
        isShowChatBoxOnClickToggle.isOn = settings.isShowChatBoxOnClick;

        charSizeSlider.value = settings.char_size;
        charSpeedSlider.value = settings.char_speed;
        charMobilitySlider.value = settings.char_mobility;
        isGravityToggle.isOn = settings.isGravity;
        isWindowsCollisionToggle.isOn = settings.isWindowsCollision;

        soundLanguageDropdown.value = settings.sound_language_idx;
        soundVolumeMasterSlider.value = settings.sound_volumeMaster;
        soundSpeedMasterSlider.value = settings.sound_speedMaster;

        serverTypeDropdown.value = settings.server_type_idx;
        aiWebSearchDropdown.value = settings.ai_web_search_idx;
        aiAskIntentDropdown.value = settings.ai_ask_intent_idx;
        isAskedTurnOnServerToggle.isOn = settings.isAskedTurnOnServer;
        isAPITest.isOn = settings.isAPITest;

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


    // 기본값 설정
    private void SetDefaultValues()
    {
        settings.player_name = "Sensei";
        settings.server_id = "temp";
        settings.ui_language_idx = 0;
        settings.ui_language = "ko";
        settings.ai_language_idx = 2;
        settings.ai_language = "en";
        settings.ai_language_in = "ko";
        settings.ai_language_out = "ko";
        settings.isAlwaysOnTop = false;
        settings.isShowChatBoxOnClick = false;

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

        settings.server_type_idx = 0;  // 0 : GPU, 1 : CPU
        settings.server_type = "GPU";
        settings.ai_web_search_idx = 0;  // 0 : off, 1 : on, 2: force
        settings.ai_web_search = "OFF";
        settings.ai_ask_intent_idx = 0;  // 0 : off, 1 : on
        settings.ai_ask_intent = "OFF";
        settings.isAskedTurnOnServer = true;
        settings.isAPITest = false;
    }
}
