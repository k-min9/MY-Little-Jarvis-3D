// 대상에 TooltipTrigger 추가해야 함
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
    public Text settingsGeneralShowTutorialOnChat;
    public Text settingsGeneralStartServerOnInit;
    public Text settingsCharApplyGravity;
    public Text settingsCharWindowsCollision;
    public Text settingsCharStartWithLastChar;
    public Text settingsCharRememberCharOutfits;
    public Text settingDisplayStyleLabel;
    public Text settingDisplayMenuLabel;
    public TooltipTrigger settingServerServerInfoIconTooltipTrigger;
    public TooltipTrigger settingServerModelTypeDownloadTooltipTrigger;

    public TooltipTrigger imageUseToggleTooltipTrigger;
    public TooltipTrigger webSearchButtonTooltipTrigger;
    public TooltipTrigger serverStatusTutorialButtonTooltipTrigger;

    public TooltipTrigger radialMenuActionDance;
    public TooltipTrigger radialMenuActionGoLeft;
    public TooltipTrigger radialMenuActionGoRight;
    public TooltipTrigger radialMenuActionHide;
    public TooltipTrigger radialMenuActionIdle;

    // idx에 맞는 서버타입에 관한 tooltip 갱신; 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
    public void SetServerServerTypeInfoTooltip(int idx)
    {
        switch (idx)
        {
            case 0: settingServerServerInfoIconTooltipTrigger.tooltip = "Automatically connects to an available server based on priority."; break;
            case 1: settingServerServerInfoIconTooltipTrigger.tooltip = "Performs the model computation locally on the user's PC."; break;
            case 2: settingServerServerInfoIconTooltipTrigger.tooltip = "Attempts to connect to Google's free API.\nEntering an API key increases the response success rate."; break;
            case 3: settingServerServerInfoIconTooltipTrigger.tooltip = "Attempts to connect to OpenRouter's free API.\nEntering an API key increases the response success rate."; break;
            case 4: settingServerServerInfoIconTooltipTrigger.tooltip = "Performs computation using Google's paid API.\nA valid API key is required."; break;
            default: settingServerServerInfoIconTooltipTrigger.tooltip = "Automatically connects to an available server based on priority."; break;
        }
    }

    // idx에 맞는 서버타입모델다운로드에 관한 tooltip 갱신; 0: Download 1: Owned
    public void SetServerModelTypeDownloadTooltip(int idx)
    {
        switch (idx)
        {
            case 0: settingServerModelTypeDownloadTooltipTrigger.tooltip = "Download"; break;
            case 1: settingServerModelTypeDownloadTooltipTrigger.tooltip = "Owned"; break;
            default: settingServerModelTypeDownloadTooltipTrigger.tooltip = "Download"; break;
        }
        SetUILanguage();
    }

    // Setting 변경시 호출하여 UI에 반영
    public void SetUILanguage()
    {
        string targetLang = SettingManager.Instance.settings.ui_language; // 0 : ko, 1 : jp, 2: en 

        // setting
        settingsHeaderTitle.text = LanguageData.Translate(settingsHeaderTitle.text, targetLang);
        settingsGeneralPlayerNameLabel.text = LanguageData.Translate(settingsGeneralPlayerNameLabel.text, targetLang);
        settingsGeneralServerIDLabel.text = LanguageData.Translate(settingsGeneralServerIDLabel.text, targetLang);
        settingsGeneralAlwaysOnTopLabel.text = LanguageData.Translate(settingsGeneralAlwaysOnTopLabel.text, targetLang);
        settingsGeneralShowChatBoxOnClickLabel.text = LanguageData.Translate(settingsGeneralShowChatBoxOnClickLabel.text, targetLang);
        settingsGeneralShowTutorialOnChat.text = LanguageData.Translate(settingsGeneralShowTutorialOnChat.text, targetLang);
        settingsGeneralStartServerOnInit.text = LanguageData.Translate(settingsGeneralStartServerOnInit.text, targetLang);
        settingsCharApplyGravity.text = LanguageData.Translate(settingsCharApplyGravity.text, targetLang);
        settingsCharWindowsCollision.text = LanguageData.Translate(settingsCharWindowsCollision.text, targetLang);
        settingsCharStartWithLastChar.text = LanguageData.Translate(settingsCharStartWithLastChar.text, targetLang);
        settingsCharRememberCharOutfits.text = LanguageData.Translate(settingsCharRememberCharOutfits.text, targetLang);
        settingDisplayStyleLabel.text = LanguageData.Translate(settingDisplayStyleLabel.text, targetLang);
        settingDisplayMenuLabel.text = LanguageData.Translate(settingDisplayMenuLabel.text, targetLang);
        
        settingServerServerInfoIconTooltipTrigger.tooltip = LanguageData.Translate(settingServerServerInfoIconTooltipTrigger.tooltip, targetLang);
        settingServerModelTypeDownloadTooltipTrigger.tooltip = LanguageData.Translate(settingServerModelTypeDownloadTooltipTrigger.tooltip, targetLang);



        // chatBalloon
        imageUseToggleTooltipTrigger.tooltip = LanguageData.Translate(imageUseToggleTooltipTrigger.tooltip, targetLang);
        webSearchButtonTooltipTrigger.tooltip = LanguageData.Translate(webSearchButtonTooltipTrigger.tooltip, targetLang);
        serverStatusTutorialButtonTooltipTrigger.tooltip = LanguageData.Translate(serverStatusTutorialButtonTooltipTrigger.tooltip, targetLang);

        // RadialMenuAction
        radialMenuActionDance.tooltip = LanguageData.Translate(radialMenuActionDance.tooltip, targetLang);
        radialMenuActionGoLeft.tooltip = LanguageData.Translate(radialMenuActionGoLeft.tooltip, targetLang);
        radialMenuActionGoRight.tooltip = LanguageData.Translate(radialMenuActionGoRight.tooltip, targetLang);
        radialMenuActionHide.tooltip = LanguageData.Translate(radialMenuActionHide.tooltip, targetLang);
        radialMenuActionIdle.tooltip = LanguageData.Translate(radialMenuActionIdle.tooltip, targetLang);


        // 모든 TooltipTrigger 자동 번역
        TranslateAllTooltipTriggers(targetLang);
    }


    // 모든 TooltipTrigger 자동 번역
    private void TranslateAllTooltipTriggers(string targetLang)
    {
        TooltipTrigger[] tooltips = FindObjectsOfType<TooltipTrigger>(true); // 비활성화된 오브젝트까지 포함

        foreach (var tooltipTrigger in tooltips)
        {
            if (!string.IsNullOrEmpty(tooltipTrigger.tooltip))
            {
                tooltipTrigger.tooltip = LanguageData.Translate(tooltipTrigger.tooltip, targetLang);
            }
        }
    }
}
