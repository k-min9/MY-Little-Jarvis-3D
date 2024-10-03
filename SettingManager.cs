using System;
using System.IO;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
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

    // 설정 데이터 속성들
    public string player_name { get; private set; }
    public string ui_language { get; private set; }
    public string ai_language { get; private set; }  // 간편
    public string ai_language_in { get; private set; }  // 한입
    public string ai_language_out { get; private set; }  // 한출
    public int char_size { get; private set; }
    public string char_lastUsed { get; private set; }
    public string char_mobility { get; private set; }
    public float char_speed { get; private set; }

    // setter
    public void SetPlayerName(string value) { player_name = value; SaveSettings(); }
    public void SetUiLanguage(string value) { ui_language = value; SaveSettings(); }
    public void SetAiLanguage(string value) { ai_language = value; SaveSettings(); }
    public void SetAiLanguageIn(string value) { ai_language_in = value; SaveSettings(); }
    public void SetAiLanguageOut(string value) { ai_language_out = value; SaveSettings(); }
    public void SetCharSize(int value) { char_size = value; SaveSettings(); }
    public void SetCharLastUsed(string value) { char_lastUsed = value; SaveSettings(); }
    public void SetCharMobility(string value) { char_mobility = value; SaveSettings(); }
    public void SetCharSpeed(float value) { char_speed = value; SaveSettings(); }


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
    
    private void Start()
    {

    }


    // 설정 데이터를 JSON 파일에서 불러오기
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(configFilePath))
            {
                // JSON 파일 읽기
                string json = File.ReadAllText(configFilePath);
                JsonUtility.FromJsonOverwrite(json, this);
            }
            else
            {
                // 파일이 없을 경우 기본 값 설정 및 저장
                SetDefaultValues();
                SaveSettings();
            }
        }
        catch (Exception e)
        {
            // 오류 발생 시 기본 값 설정 및 저장
            Debug.LogWarning("Failed to load settings: " + e.Message);
            SetDefaultValues();
            SaveSettings();
        }
    }

    // 설정 데이터 저장
    public void SaveSettings() {
        try {
            // 현재 상태를 JSON으로 변환
            string json = JsonUtility.ToJson(this, true);

            // 디렉토리가 없을 경우 생성
            string directoryPath = Path.GetDirectoryName(configFilePath);
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            // JSON 파일 쓰기
            File.WriteAllText(configFilePath, json);
        } catch (UnauthorizedAccessException e) {
            Debug.LogError("Access denied: " + e.Message); // 권한 문제 발생
        } catch (DirectoryNotFoundException e) {
            Debug.LogError("Directory not found: " + e.Message); // 경로가 유효하지 않은 경우
        } catch (IOException e) {
            Debug.LogError("I/O error occurred: " + e.Message); // 파일 쓰기 실패 등 입출력 문제 발생
        } catch (Exception e) {
            Debug.LogError("An error occurred: " + e.Message); // 기타 예외 처리
        }
    }


    // 기본값 설정
    private void SetDefaultValues()
    {
        player_name = "m9dev";
        ui_language = "en";
        ai_language = "ko";
        ai_language_in = "ko";
        ai_language_out = "ko";

        char_size = 100;
        char_lastUsed = "mari";
        char_mobility = "1";
        char_speed = 100;
    }
}
