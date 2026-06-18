using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterDetailState
{
    public string characterId;
    public int affection = 0;
    public int maxAffection = 300;
    public string affectionLabel = "친밀";
    public string source = "오리지널";
    public string form = "2D";
    public List<string> statusTags = new List<string>();
    public List<string> featureTags = new List<string>();
    public string voiceId = "";
    public Dictionary<string, string> promptsByLanguage = new Dictionary<string, string>();
}

public class CharacterDetailStateManager : MonoBehaviour
{
    private static CharacterDetailStateManager instance;
    public static CharacterDetailStateManager Instance { get { if (instance == null) { instance = FindObjectOfType<CharacterDetailStateManager>(); if (instance == null) { instance = new GameObject("CharacterDetailStateManager").AddComponent<CharacterDetailStateManager>(); } } return instance; } } // 싱글톤 인스턴스

    public event Action<string, CharacterDetailState> StateChanged; // 상태 변경 이벤트

    private CharAttributes GetCharAttributes(string charCode)
    {
        if (CharManager.Instance.charList != null)
        {
            foreach (var obj in CharManager.Instance.charList)
            {
                var attr = obj.GetComponent<CharAttributes>();
                if (attr != null && attr.charcode == charCode)
                    return attr;
            }
        }
        return null;
    }

    public CharacterDetailState GetState(string characterId)
    {
        CharacterDetailState state = new CharacterDetailState { characterId = characterId };
        if (string.IsNullOrEmpty(characterId)) return state;

        CharAttributes attr = GetCharAttributes(characterId);
        if (attr != null)
        {
            state.source = attr.source;
            state.form = attr.form;
            state.statusTags = new List<string>(attr.statusTags);
            state.featureTags = new List<string>(attr.featureTags);
        }

        var setting = SettingCharManager.Instance.GetCharCodeSetting(characterId);
        if (setting != null)
        {
            state.affection = setting.affection;
            state.voiceId = setting.voiceId;
            for (int i = 0; i < setting.promptKeys.Count; i++)
                state.promptsByLanguage[setting.promptKeys[i]] = setting.promptValues[i];
        }

        // 호감도 라벨 동적 계산
        state.affectionLabel = state.affection >= 200 ? "매우 친밀" : state.affection >= 100 ? "친밀" : "보통";

        return state;
    }

    public void AddAffection(string characterId, int amount) { SettingCharManager.Instance.AddAffection(characterId, amount); StateChanged?.Invoke(characterId, GetState(characterId)); } // 호감도 증감
    public void SetVoice(string characterId, string voiceId) { SettingCharManager.Instance.SetVoice(characterId, voiceId); StateChanged?.Invoke(characterId, GetState(characterId)); } // 음성 설정
    public void SetPrompt(string characterId, string language, string prompt) { SettingCharManager.Instance.SetPrompt(characterId, language, prompt); StateChanged?.Invoke(characterId, GetState(characterId)); } // 프롬프트 설정

    public static string BuildCharacterId(ChangeCharInfo charInfo, ChangeCharClothesInfo clothes) { return (clothes != null && !string.IsNullOrEmpty(clothes.charAttr_charcode)) ? clothes.charAttr_charcode : (clothes != null && !string.IsNullOrEmpty(clothes.name)) ? clothes.name : (charInfo != null && !string.IsNullOrEmpty(charInfo.name)) ? charInfo.name : string.Empty; }
}
