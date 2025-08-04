// From ContextMenuTrigger
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames.UIWidgets;
using ContextMenu = DevionGames.UIWidgets.ContextMenu;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

public class MenuTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private TransparentWindow _transparentWindow;
    private CharAttributes _charAttributes;
    private ContextMenu m_ContextMenu;
    private ContextMenu m_ContextMenuSub;
    private bool itemChkFlag = false;
    private float chkTimer = 0f; // 타이머 변수 추가
    private bool isLeftClickHeld = false; // 좌클릭 상태
    private float leftClickHoldTime = 0f; // 좌클릭 누른 시간

    private RadialMenu m_RadialMenuAction;

    // Start is called before the first frame update
    private void Start()
    {
        this.m_ContextMenu = WidgetUtility.Find<ContextMenu>("ContextMenu");
        this.m_ContextMenuSub = WidgetUtility.Find<ContextMenu>("ContextMenuSub"); // SubMenu 인스턴스 직접 참조
        this._transparentWindow = FindObjectOfType<TransparentWindow>();  // GameObject에 있음
        this._charAttributes = FindObjectOfType<CharAttributes>();
        this.m_RadialMenuAction = WidgetUtility.Find<RadialMenu>("RadialMenuAction");

    }

    private void Update()
    {
        // itemCheck가 null이 아닌데, active가 아님 = 메뉴가 꺼짐
        if (itemChkFlag)
        {
            // 타이머 갱신
            if (chkTimer > 0f)
            {
                chkTimer -= Time.deltaTime;
                return;
            }

            // 메뉴가 보이는 중
            if (!m_ContextMenu.IsVisible)  // 자체제공함수
            {
                StatusManager.Instance.IsOptioning = false;
                itemChkFlag = false; // 한번 처리 후 flag 초기화
            }
        }

        // 좌클릭 상태 체크
        if (isLeftClickHeld && !StatusManager.Instance.IsDragging)
        {
            leftClickHoldTime += Time.deltaTime;
            if (leftClickHoldTime >= 0.5f) // 0.5초 이상 누르면 우클릭 동작 실행
            {
                isLeftClickHeld = false; // 상태 초기화
                leftClickHoldTime = 0f;
                TriggerMenu();
            }
        }

        // Radial Menu Action이 보이는 중
        if (m_RadialMenuAction.IsVisible)  // 자체제공함수
        {
            UpdateRadialMenuActionPosition();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            TriggerMenu();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            isLeftClickHeld = true;
            leftClickHoldTime = 0f; // 타이머 초기화
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isLeftClickHeld = false;
            leftClickHoldTime = 0f; // 타이머 초기화
        }
    }

    /*
    Context Menu
    ├ Settings(세팅 창 열기)
    ├ Character
    │   ├ Action (원형 메뉴: 이동/행동 등)
    │   ├ Change Char (캐릭터 변경)
    │   ├ Summon Char (서브 캐릭터 소환)
    │   └ Change Clothes (의상 변경)
    ├ Chat
    │   ├ Guideline (주의사항)
    │   ├ Situation (퍼스트메시지)
    │   ├ Chat History (채팅 이력 보기)
    │   └ Erase Memory (대화 메모리 초기화)
    ├ Control
    │   ├ Show TalkMenu (음성입력 리모컨)
    │   └ Show/Hide TalkInfo (음성 텍스트 토글)
    ├ Screen
    │   ├ Set Screenshot Area (스크린샷 범위 설정)
    │   └ Show Screenshot (스크린샷 결과 보기)
    ├ Talk
    │   ├ Show Tutorial (튜토리얼 시작하기)
    │   └ Idle Talk (자율 발화 시작)
    ├ Util
    │   ├ Alarm (알람 기능)
    │   └ Minigame1 (미니게임 실행)
    ├ Dev (Editor 전용)
    │   ├ Debug (디버그 로그 실행)
    │   ├ Test / Test2 / Test3 (서브 메뉴 테스트)
    ├ Version (버전 창 열기) 
    └ Exit (프로그램 종료) 
    */
    private void TriggerMenu()
    {
        this.m_ContextMenu.Clear();

        // 메뉴 추가
        string menuName = "";
        string targetLang = SettingManager.Instance.settings.ui_language; // 0 : ko, 1 : jp, 2: en 

        // setting
        // menuName = LanguageData.Translate("Settings", targetLang);  // Setting은 언어 상관없이 영어로
        m_ContextMenu.AddMenuItem("Settings", delegate {
            UIManager.Instance.showSettings();
        });

        // 캐릭터 분류
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Character", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Action", targetLang), delegate { OnPointerDownRadialMenuAction(); }),
            (LanguageData.Translate("Change Char", targetLang), delegate { UIManager.Instance.ShowCharChange(); }),
            (LanguageData.Translate("Summon Char", targetLang), delegate { UIManager.Instance.ShowCharAdd(); }),
            (LanguageData.Translate("Change Clothes", targetLang), delegate {
                if (_charAttributes.toggleClothes != null || _charAttributes.changeClothes != null)
                {
                    CharManager.Instance.ChangeClothes();
                }
            }),
        });

        // Chat
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Chat", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Guideline", targetLang), delegate { UIManager.Instance.ShowGuideLine(); }), 
            (LanguageData.Translate("Situation", targetLang), delegate { Debug.Log("Situation 선택됨"); }), // TODO : 구현 예정
            (LanguageData.Translate("New Chat", targetLang), delegate {
                MemoryManager.Instance.ResetConversationMemory();
                AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Memory Erased");
            }),
            (LanguageData.Translate("Chat History", targetLang), delegate { UIManager.Instance.ShowChatHistory(); }),
        });

        // Control
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Control", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Show Voice Panel", targetLang), delegate { TalkMenuManager.Instance.ShowTalkMenu(); }),
            (
                NoticeBalloonManager.Instance.noticeBalloon.activeSelf ?
                LanguageData.Translate("Hide TalkInfo", targetLang) :
                LanguageData.Translate("Show TalkInfo", targetLang),
                delegate {
                    if (NoticeBalloonManager.Instance.noticeBalloon.activeSelf)
                    {
                        NoticeBalloonManager.Instance.HideNoticeBalloon();
                    }
                    else
                    {
                        NoticeBalloonManager.Instance.ShowNoticeBalloon();
                    }
                }
            )
        });

        // Screen
#if UNITY_STANDALONE_WIN
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Screen", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Set Screenshot Area", targetLang), delegate {
                ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
                if (sm != null) sm.SetScreenshotArea();
            }),
            (LanguageData.Translate("Show Screenshot", targetLang), delegate {
                ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
                if (sm != null) {
                    sm.SaveScreenshot();
                    sm.ShowScreenshotImage();
                }
            })
        });
#endif
        // Talk
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Talk", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Show Tutorial", targetLang), delegate { ScenarioTutorialManager.Instance.StartTutorial(); }),
            (LanguageData.Translate("Idle Talk", targetLang), delegate { Debug.Log("Idle Talk 시작"); }) // 구현 예정
        });

        // Util
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Util", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Alarm", targetLang), delegate { Debug.Log("[Alarm] 알람 기능 호출됨"); }),
            (LanguageData.Translate("Minigame1", targetLang), delegate { Debug.Log("[Minigame] 미니게임1 호출됨"); })
        });

        // Dev
#if UNITY_EDITOR
        m_ContextMenu.AddMenuItem(LanguageData.Translate("Debug", targetLang), delegate {
            // EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), 10.0f);

            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Testing...");
            // StartCoroutine(ScenarioCommonManager.Instance.Scenario_C02_AskToStartServer());
            // CharManager.Instance.ChangeCharacterFromCharCode("ch0139");
        });
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Dev", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Test", targetLang), delegate {
                Debug.Log("Test 실행");
            }),
            (LanguageData.Translate("Test2", targetLang), delegate { Debug.Log("Test2 실행"); }),
            (LanguageData.Translate("Test3", targetLang), delegate { Debug.Log("Test3 실행"); })
        });
#endif

        // Version
        m_ContextMenu.AddMenuItem(LanguageData.Translate("Version", targetLang), delegate {
            UIManager.Instance.ShowVersion();
        });

        // Exit
        m_ContextMenu.AddMenuItem(LanguageData.Translate("Exit", targetLang), delegate {
            Application.Quit();
        });

        // 메뉴 보이기
        this.m_ContextMenu.Show();

        // StatusManager 관리 (1초 후)
        StatusManager.Instance.IsOptioning = true;

        chkTimer = 1f;
        itemChkFlag = true;
    }

    // Sub - RadialMenu를 위한 전용 함수들

    // Action
    private void OnPointerDownRadialMenuAction() {
        Vector2 characterTransformPos = StatusManager.Instance.characterTransform.anchoredPosition;
        m_RadialMenuAction.characterTransformPos = new Vector2(characterTransformPos.x, characterTransformPos.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
        m_RadialMenuAction.Show();
    }

    // AnswerBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateRadialMenuActionPosition()
    {
        Vector2 characterTransformPos = StatusManager.Instance.characterTransform.anchoredPosition;
        m_RadialMenuAction.GetComponent<RectTransform>().anchoredPosition = new Vector2(characterTransformPos.x, characterTransformPos.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
    }
}

