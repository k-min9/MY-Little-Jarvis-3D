using System.Collections;
using System.Collections.Generic;
using DevionGames.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] public GameObject charChange; // CharChange
    [SerializeField] public GameObject charSummon; // charSummon
    [SerializeField] public GameObject version; // version+thanks
    [SerializeField] public Text versionThanksContent; // version+thanks
    [SerializeField] public GameObject settings; // settings
    [SerializeField] public GameObject chatHistory; // chatHistory
    [SerializeField] public GameObject guideLine; // guideLine
    [SerializeField] public GameObject situation; // UIChatSituation

    // 싱글톤 인스턴스
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

        // 자체 함수로 비활성화
        charChange.SetActive(false);
        charSummon.SetActive(false);
        version.SetActive(false);
        settings.SetActive(false);
        chatHistory.SetActive(false);
        // guideLine.SetActive(false);
        // situation.SetActive(false);

        // UIWidget 존재하면 Close
        TryCloseWidget(charChange);
        TryCloseWidget(charSummon);
        TryCloseWidget(version);
        TryCloseWidget(settings);
        TryCloseWidget(chatHistory);
        TryCloseWidget(guideLine);
        TryCloseWidget(situation);

        //         // 안드로이드 or 테스트용
        // #if UNITY_ANDROID || UNITY_EDITOR
        //         charChange.SetActive(true);
        //         settings.SetActive(true);
        // #endif
    }



    // GameObject에 UIWidget이 있으면 Close() 호출
    private void TryCloseWidget(GameObject target)
    {
        if (target == null) return;

        UIWidget widget = target.GetComponent<UIWidget>();
        if (widget != null)
        {
            widget.Close();
        }
    }

    // charChange-UIWidget의 Show 작동
    public void ShowCharChange()
    {
        UIWidget uIWidget = charChange.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!charChange.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("charChange");
            charChange.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();
    }

    // charChange-UIWidget의 Close 작동
    public void CloseCharChange()
    {
        UIWidget uIWidget = charChange.GetComponent<UIWidget>();

        uIWidget.Close();
    }

    // charChange-UIWidget의 Toggle 작동
    public void ToggleCharChange()
    {
        if (charChange.activeSelf)
        {
            CloseCharChange();
        }
        else
        {
            ShowCharChange();
        }
    }

    // GuideLine-UIWidget의 Show 작동
    public void ShowGuideLine()
    {
        UIWidget uIWidget = guideLine.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!guideLine.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("guideline");
            guideLine.GetComponent<RectTransform>().position = position;
        }

        // 값이 없으면 초기값 선언하게 선언
        UIUserCardManager.Instance.InitUserCard();

        uIWidget.Show();
    }

    // GuideLine-UIWidget의 Close 작동
    public void CloseGuideLine()
    {
        UIWidget uIWidget = guideLine.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // GuideLine-UIWidget의 Toggle 작동
    public void ToggleGuideLine()
    {
        if (guideLine.activeSelf)
        {
            CloseGuideLine();
        }
        else
        {
            ShowGuideLine();
        }
    }

    // ChatSituation 활성화 후 -UIWidget의 Show 작동
    public void ShowUIChatSituation()
    {

        UIWidget uIWidget = situation.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!situation.activeSelf)
        {
            situation.SetActive(true);  // 활성화 해야 Load 가능
            UIChatSituationManager.Instance.LoadChatSituationData();  // 언어 ui 변경가능성 있으니 그냥 load (data가 아직은 가벼움)

            Vector3 position = UIPositionManager.Instance.GetCanvasPositionCenter();
            // Vector3 position = UIPositionManager.Instance.GetMenuPosition("situation");
            situation.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();

        // 스크롤 강제 초기화
        UIChatSituationManager.Instance.ResetScrollPosition();
    }

    // ChatSituation-UIWidget의 Close 작동
    public void CloseUIChatSituation()
    {
        UIWidget uIWidget = situation.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // ChatSituation-UIWidget의 Toggle 작동
    public void ToggleUIChatSituation()
    {
        if (situation.activeSelf)
        {
            CloseUIChatSituation();
        }
        else
        {
            ShowUIChatSituation();
        }
    }


    // charSummon-UIWidget의 Show 작동
    public void ShowCharSummon()
    {
        UIWidget uIWidget = charSummon.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!charSummon.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("charSummon");
            Debug.Log(position);
            charSummon.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();
    }

    // charSummon-UIWidget의 Close 작동
    public void CloseCharSummon()
    {
        UIWidget uIWidget = charSummon.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // charSummon-UIWidget의 Toggle 작동
    public void ToggleCharSummon()
    {
        if (charSummon.activeSelf)
        {
            CloseCharSummon();
        }
        else
        {
            ShowCharSummon();
        }
    }

    // version-UIWidget의 Show 작동
    public void ShowVersion()
    {
        UIWidget uIWidget = version.GetComponent<UIWidget>();

        // Special Thanks 문자
        string answerLanguage = SettingManager.Instance.settings.ui_language; // 표시 언어 초기화[ko, en, jp]
        // 언어에 따른 텍스트 설정
        if (answerLanguage == "ko")
        {
            versionThanksContent.text = "이 프로그램은 무료로 사용할 수 있으며\n많은 기부자들의 후원으로 제작되고 있습니다.";
        }
        else if (answerLanguage == "jp")
        {
            versionThanksContent.text = "このプログラムは無料で利用することができ、\n多くのパトロンの後援で制作されています。";
        }
        else
        {
            versionThanksContent.text = "This program is FREE TO USE\nand is supported by many generous donors.";
        }

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!version.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("version");
            Debug.Log(position);
            version.GetComponent<RectTransform>().position = position;
        }
    
        uIWidget.Show();
    }

    // version-UIWidget의 Close 작동
    public void CloseVersion()
    {
        UIWidget uIWidget = version.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // version-UIWidget의 Toggle 작동
    public void ToggleVersion()
    {
        if (version.activeSelf)
        {
            CloseVersion();
        }
        else
        {
            ShowVersion();
        }
    }

    // settings-UIWidget의 Show 작동
    public void showSettings()
    {
        UIWidget uIWidget = settings.GetComponent<UIWidget>();
        uIWidget.Show();
    }

    // settings-UIWidget의 Close 작동
    public void CloseSettings()
    {
        UIWidget uIWidget = settings.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // settings-UIWidget의 Toggle 작동
    public void ToggleSettings()
    {
        if (settings.activeSelf)
        {
            CloseSettings();
        }
        else
        {
            showSettings();
        }
    }

    // chatHistory-UIWidget의 Show 작동
    public void ShowChatHistory()
    {
        UIChatHistoryManager uIChatHistoryManager = chatHistory.GetComponent<UIChatHistoryManager>();
        uIChatHistoryManager.LoadChatHistory();

        UIWidget uIWidget = chatHistory.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!chatHistory.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("chatHistory");
            Debug.Log(position);
            chatHistory.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();
    }

    // chatHistory-UIWidget의 Close 작동
    public void CloseChatHistory()
    {
        UIWidget uIWidget = chatHistory.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // chatHistory-UIWidget의 Toggle 작동
    public void ToggleChatHistory()
    {
        if (chatHistory.activeSelf)
        {
            CloseChatHistory();
        }
        else
        {
            ShowChatHistory();
        }
    }
}
