using TMPro;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{

    // 입력값 정리
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Dropdown uiLangDropdown;
    [SerializeField] private Dropdown aiLangDropdown;

    [SerializeField] private Slider charSizeSlider;
    [SerializeField] private Slider charSpeedSlider;
    [SerializeField] private Slider charMobilitySlider;

    // 설정 데이터 클래스
    [Serializable]
    public class SettingsData
    {
        public string player_name;
        public int ui_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string ui_language;  
        public int ai_language_idx;  // 0 : ko, 1 : jp, 2: en
        public string ai_language;  
        public string ai_language_in;
        public string ai_language_out;

        public string char_lastUsed;
        public float char_size;
        public float char_mobility;
        public float char_speed;
    }

    // 설정 데이터 인스턴스
    public SettingsData settings = new SettingsData();

    // 싱글톤 인스턴스
    public static SettingManager instance;
        // 싱글톤 인스턴스에 접근하는 속성
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
    public void SetUiLanguage() { int value=uiLangDropdown.value; settings.ui_language_idx = value; settings.ui_language=getLangFromIdx(value); SaveSettings(); }
    public void SetAiLanguage() { int value=aiLangDropdown.value; settings.ai_language_idx = value; settings.ai_language=getLangFromIdx(value); SaveSettings(); }
    public void SetAiLanguageIn(string value) { settings.ai_language_in = value; SaveSettings(); }
    public void SetAiLanguageOut(string value) { settings.ai_language_out = value; SaveSettings(); }

    public void SetCharLastUsed(string value) { settings.char_lastUsed = value; SaveSettings(); }
    public void SetCharSize(float value) { settings.char_size = value; SaveSettings(); }
    public void SetCharSpeed(float value) { settings.char_speed = value; SaveSettings(); }
    public void SetCharMobility(float value) { settings.char_mobility = value; SaveSettings(); }


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
            Destroy(gameObject);
            return;
        }

        // 기본 설정 로딩
        configFilePath = Path.Combine(Application.persistentDataPath, "config/setting.json");
        LoadSettings();
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

        // UI세팅
        playerNameInputField.text = settings.player_name;
        uiLangDropdown.value = settings.ui_language_idx;

        charSizeSlider.value = settings.char_size;
        charSpeedSlider.value = settings.char_speed;
        charMobilitySlider.value = settings.char_mobility;
    }

    // 설정 데이터를 JSON 파일에 저장하는 함수
    public void SaveSettings()
    {
        try
        {
            // 설정 데이터를 JSON으로 변환
            string json = JsonUtility.ToJson(settings, true);

            // 디렉토리가 없을 경우 생성
            string directoryPath = Path.GetDirectoryName(configFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // JSON 파일 쓰기
            File.WriteAllText(configFilePath, json);
            Debug.Log("Settings saved successfully");
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
        settings.ui_language_idx = 0;
        settings.ui_language = "ko";
        settings.ai_language_idx = 0;
        settings.ai_language = "ko";
        settings.ai_language_in = "ko";
        settings.ai_language_out = "ko";

        settings.char_size = 100;
        settings.char_lastUsed = "mari";
        settings.char_mobility = 5;
        settings.char_speed = 100;
    }
}
