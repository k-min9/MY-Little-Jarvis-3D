using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

/*
settings_char.json 구조 예시

{
  "last_char": "arona",
  "char_info": [
    {
      "key": "arona",
      "value": { "char_code": "arona", "char_size": 100 }
    },
    {
      "key": "mari",
      "value": { "char_code": "ch002" }
    }
  ],
  "char_code_info": [
    {
      "key": "ch002",
      "value": { "char_size": 95 }
    }
  ]
}
*/

public class SettingCharManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static SettingCharManager instance;
    public static SettingCharManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<SettingCharManager>();
            return instance;
        }
    }

    private string configFilePath;
    public bool isLoaded = false;

    [Serializable]
    public class CharSetting
    {
        public string char_code;
        public float char_size;
    }

    [Serializable]
    public class CharCodeSetting
    {
        public float char_size;
    }

    [Serializable]
    public class CharInfoEntry
    {
        public string key;
        public CharSetting value;
    }

    [Serializable]
    public class CharCodeInfoEntry
    {
        public string key;
        public CharCodeSetting value;
    }

    [Serializable]
    public class SettingsCharData
    {
        public string last_char;
        public List<CharInfoEntry> char_info = new List<CharInfoEntry>();
        public List<CharCodeInfoEntry> char_code_info = new List<CharCodeInfoEntry>();

        [NonSerialized] public Dictionary<string, CharSetting> char_info_dict = new();
        [NonSerialized] public Dictionary<string, CharCodeSetting> char_code_info_dict = new();

        public void SyncDictionaries()
        {
            char_info_dict.Clear();
            foreach (var entry in char_info)
                char_info_dict[entry.key] = entry.value;

            char_code_info_dict.Clear();
            foreach (var entry in char_code_info)
                char_code_info_dict[entry.key] = entry.value;
        }

        public void RebuildListsFromDict()
        {
            char_info.Clear();
            foreach (var kv in char_info_dict)
                char_info.Add(new CharInfoEntry { key = kv.Key, value = kv.Value });

            char_code_info.Clear();
            foreach (var kv in char_code_info_dict)
                char_code_info.Add(new CharCodeInfoEntry { key = kv.Key, value = kv.Value });
        }
    }

    private SettingsCharData settingsCharData = new SettingsCharData();

    public void LoadSettingChar()
    {
        configFilePath = Path.Combine(Application.persistentDataPath, "config", "settings_char.json");

        try
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                settingsCharData = JsonUtility.FromJson<SettingsCharData>(json);
                settingsCharData.SyncDictionaries();
            }
            else
            {
                SaveToFile();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("LoadSettingChar 실패: " + e.Message);
        }

        isLoaded = true;
    }

    private void SaveToFile()
    {
        configFilePath = Path.Combine(Application.persistentDataPath, "config", "settings_char.json");

        try
        {
            string directoryPath = Path.GetDirectoryName(configFilePath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            settingsCharData.RebuildListsFromDict();

            string json = JsonUtility.ToJson(settingsCharData, true);
            File.WriteAllText(configFilePath, json);

            Debug.Log($"[SettingCharManager] settings_char.json 저장 완료:\n{json}");
        }
        catch (Exception e)
        {
            Debug.LogError("SaveToFile 실패: " + e.Message);
        }
    }

    public void SetLastChar(string charName)
    {
        settingsCharData.last_char = charName;
        SaveToFile();
    }

    public string GetLastChar()
    {
        return settingsCharData.last_char;
    }

    public void SaveSettingCharOutfit(string charName, string charCode)
    {
        Debug.Log($"[SettingCharManager] SaveSettingCharOutfit 호출됨: charName={charName}, charCode={charCode}");

        if (!settingsCharData.char_info_dict.ContainsKey(charName))
            settingsCharData.char_info_dict[charName] = new CharSetting();

        settingsCharData.char_info_dict[charName].char_code = charCode;

        SaveToFile();
    }

    public void SaveSettingCharSize(string charName, float charSize)
    {
        if (!settingsCharData.char_info_dict.ContainsKey(charName))
            settingsCharData.char_info_dict[charName] = new CharSetting();

        settingsCharData.char_info_dict[charName].char_size = charSize;
        SaveToFile();
    }

    public void SaveCharCodeSize(string charCode, float charSize)
    {
        if (!settingsCharData.char_code_info_dict.ContainsKey(charCode))
            settingsCharData.char_code_info_dict[charCode] = new CharCodeSetting();

        settingsCharData.char_code_info_dict[charCode].char_size = charSize;
        SaveToFile();
    }

    public CharSetting GetCharSetting(string charName)
    {
        if (settingsCharData.char_info_dict.TryGetValue(charName, out var setting))
            return setting;

        return null;
    }

    public float? GetCharSize(string charName)
    {
        if (settingsCharData.char_info_dict.TryGetValue(charName, out var charSetting))
        {
            if (charSetting.char_size > 0)
                return charSetting.char_size;

            if (!string.IsNullOrEmpty(charSetting.char_code) &&
                settingsCharData.char_code_info_dict.TryGetValue(charSetting.char_code, out var codeSetting) &&
                codeSetting.char_size > 0)
            {
                return codeSetting.char_size;
            }
        }

        return null;
    }

    // last_char을 기반으로 최종 char_code 반환 (InitCharacter용)
    public string GetLastCharCode()
    {
        string lastChar = settingsCharData.last_char;

        if (string.IsNullOrEmpty(lastChar))
            return null;

        foreach (GameObject obj in CharManager.Instance.charList)
        {
            var attr = obj.GetComponent<CharAttributes>();
            if (attr != null && attr.charcode == lastChar)
                return lastChar;
        }

        if (settingsCharData.char_info_dict.TryGetValue(lastChar, out var setting))
        {
            if (!string.IsNullOrEmpty(setting.char_code))
                return setting.char_code;
        }

        return lastChar;
    }
}
