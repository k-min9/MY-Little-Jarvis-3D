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
    public string ai_language { get; private set; }
    public string char_size { get; private set; }
    public string char_lastUsed { get; private set; }
    public string char_mobility { get; private set; }
    public string char_speed { get; private set; }

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
    }
    
    private void Start()
    {
        configFilePath = Path.Combine(Application.persistentDataPath, "config/setting.json");
        // 우선 기본값으로 설정
        SetDefaultValues();
    }


    // 설정 데이터를 JSON 파일에서 불러오기
    private void LoadSettings()
    {
        if (File.Exists(configFilePath))
        {
            // JSON 파일 읽기
            string json = File.ReadAllText(configFilePath);
            JsonUtility.FromJsonOverwrite(json, this);
        }
        else
        {
            // 파일이 없을 경우 기본 값 설정
            SetDefaultValues();
            SaveSettings();  // 기본값을 파일에 저장
        }
    }

    // 설정 데이터 저장
    public void SaveSettings()
    {
        // 현재 상태를 JSON으로 변환
        string json = JsonUtility.ToJson(this, true);
        
        // 디렉토리가 없을 경우 생성
        string directoryPath = Path.GetDirectoryName(configFilePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // JSON 파일 쓰기
        File.WriteAllText(configFilePath, json);
    }

    // 기본값 설정
    private void SetDefaultValues()
    {
        player_name = "m9dev";
        ui_language = "en";
        ai_language = "en";

        char_size = "1";
        char_lastUsed = "mari";
        char_mobility = "1";
        char_speed = "200";
    }

    // 설정 변경 시 호출될 메서드들 (예시: player_name 변경)
    public void SetPlayerName(string newName)
    {
        player_name = newName;
        SaveSettings();  // 값 변경 시마다 저장
    }

}
