using DevionGames.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class LanguageManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static LanguageManager instance;
    public static LanguageManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LanguageManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Test 코드
        // Debug.Log(Translate("설정"));
        // Debug.Log(Translate("設定"));
        // Debug.Log(Translate("Setting"));
        // Debug.Log(LanguageData.Translate("설정", "en"));
        // Debug.Log(LanguageData.Translate("設定", "ja"));
        // Debug.Log(LanguageData.Translate("설정", "ja"));
        // Debug.Log(LanguageData.Translate("Screenshot area 선택중...", "ja"));
        // Debug.Log(LanguageData.Translate("Screenshot area 선택중...", "en"));
        // Debug.Log(LanguageData.Translate("No Data", "ja"));
        // Debug.Log(LanguageData.Translate("설정", "No Language"));
    }


    // 입력된 단어를 Full Scan하여 목표 언어로 번역.
    public string Translate(string word)
    {
        string targetLang = SettingManager.Instance.settings.ui_language; // 0 : ko, 1 : jp, 2: en 
        string translatedword = LanguageData.Translate(word, targetLang);
        return translatedword;
    }

    // UI들 변경
    public Text settingsHeaderTitle;
    public Text settingsGeneralPlayerNameLabel;
    public Text settingsGeneralServerIDLabel;
    public Text settingsGeneralAlwaysOnTopLabel;
    public Text settingsGeneralShowChatBoxOnClickLabel;

    public TooltipTrigger imageUseToggleTooltipTrigger;

    // Setting 변경시 호출하여 UI에 반영
    public void SetUILanguage()
    {
        string targetLang = SettingManager.Instance.settings.ui_language; // 0 : ko, 1 : jp, 2: en 

        settingsHeaderTitle.text = LanguageData.Translate(settingsHeaderTitle.text, targetLang);;
        settingsGeneralPlayerNameLabel.text = LanguageData.Translate(settingsGeneralPlayerNameLabel.text, targetLang);;
        settingsGeneralServerIDLabel.text = LanguageData.Translate(settingsGeneralServerIDLabel.text, targetLang);;
        settingsGeneralAlwaysOnTopLabel.text = LanguageData.Translate(settingsGeneralAlwaysOnTopLabel.text, targetLang);;
        settingsGeneralShowChatBoxOnClickLabel.text = LanguageData.Translate(settingsGeneralShowChatBoxOnClickLabel.text, targetLang);;

        imageUseToggleTooltipTrigger.tooltip = LanguageData.Translate(imageUseToggleTooltipTrigger.tooltip, targetLang);
    }


}
