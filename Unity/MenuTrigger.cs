// From ContextMenuTrigger
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    // 더블클릭 감지용 변수들
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private const float doubleClickTime = 0.3f; // 더블클릭 판정 시간

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

        // 더블클릭 타이머 관리
        if (clickCount > 0 && Time.time - lastClickTime > doubleClickTime)
        {
            clickCount = 0; // 더블클릭 시간 초과 시 리셋
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
            // 더블클릭 감지 로직
            if (Time.time - lastClickTime < doubleClickTime && clickCount == 1)
            {
                OnDoubleClick();
                clickCount = 0; // 더블클릭 처리 후 리셋
            }
            else
            {
                clickCount = 1;
                lastClickTime = Time.time;
            }

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
        bool isSampleVer = SettingManager.Instance.GetInstallStatus() == 0;  // 0이면 sample ver
#if UNITY_EDITOR
        isSampleVer = false;
#endif

        // DevMode 체크: UNITY_EDITOR이거나 F12를 눌러서 DevMode를 활성화했을 경우 true
        bool isDevModeEnabled = false;
#if UNITY_EDITOR
        isDevModeEnabled = true;
#else
        if (DevManager.Instance != null)
        {
            isDevModeEnabled = DevManager.Instance.IsDevModeEnabled();
        }
#endif

        // setting
        // menuName = LanguageData.Translate("Settings", targetLang);  // Setting은 언어 상관없이 영어로
        m_ContextMenu.AddMenuItem("Settings", delegate {
            UIManager.Instance.showSettings();
        });

        // 캐릭터 분류
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Character", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Action", targetLang), delegate { OnPointerDownRadialMenuAction(); }),
            (LanguageData.Translate("Change Char", targetLang), delegate { UIManager.Instance.ShowCharChange();}),
            (LanguageData.Translate("Summon Char", targetLang), delegate { UIManager.Instance.ShowCharSummon(); }),
            (LanguageData.Translate("Change Clothes", targetLang),
                (_charAttributes.toggleClothes != null || _charAttributes.changeClothes != null)
                ? (UnityAction)(() => {
                    CharManager.Instance.ChangeClothes();
                })
                : null  // 회색 글씨
            ),
        });

        // Chat - 채팅
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Chat", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Guideline", targetLang), delegate { UIManager.Instance.ShowGuideLine(); }), 
            (LanguageData.Translate("Situation", targetLang), delegate { UIManager.Instance.ShowUIChatSituation(); }), 
            (LanguageData.Translate("New Chat", targetLang), delegate {
                MemoryManager.Instance.ResetConversationMemoryAndGuide();
            }),
            (LanguageData.Translate("Chat History", targetLang), delegate { UIManager.Instance.ShowChatHistory(); }),
        });

        // Control - 제어
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
            ),
            (LanguageData.Translate("Set Screenshot Area", targetLang), async delegate {
                // Full 버전 이상인지 확인
                if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                {
                    return;
                }

                ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
                if (sm != null) sm.ToggleScreenshotArea();
            }),
            (LanguageData.Translate("OCR", targetLang), delegate {
                OCROptions options = OCRManager.Instance.GetCurrentOptions();                    
                // Screenshot 영역이 설정되어 있으면 영역 OCR, 아니면 전체 화면 OCR
                if (ScreenshotManager.Instance.IsScreenshotAreaSet())
                {
                    Debug.Log("Screenshot 영역이 설정되어 있음 - 영역 OCR 실행");
                    ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
                }
                else
                {
                    Debug.Log("Screenshot 영역이 설정되지 않음 - 전체 화면 OCR 실행");
                    ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
                }
            }),
        });

        // Screen - 화면 : 조금 더 고급화 해서 돌아오자.
// #if UNITY_STANDALONE_WIN
//         m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Screen", targetLang), new List<(string, UnityAction)>
//         {
//             (LanguageData.Translate("Set Screenshot Area", targetLang), isSampleVer ? null :  delegate {
//                 ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
//                 if (sm != null) sm.SetScreenshotArea();
//             }),
//             (LanguageData.Translate("Show Screenshot Result", targetLang), isSampleVer ? null : delegate {
//                 ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
//                 if (sm != null) {
//                     sm.SaveScreenshot();
//                     sm.ShowScreenshotImage();
//                 }
//             })
//         });
// #endif
        // Talk
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Talk", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Edition 튜토리얼", targetLang), delegate { ScenarioInstallerManager.Instance.StartInstaller(); }),
            // (LanguageData.Translate("Setting 튜토리얼", targetLang), delegate { ScenarioTutorialManager.Instance.StartTutorial(); }),
            (LanguageData.Translate("Idle Talk", targetLang), async delegate { 
                // Full 버전 이상인지 확인
                if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                {
                    return;
                }
                string purpose = "잡담"; // 기본 목적
                APIManager.Instance.CallSmallTalkStream(purpose);
            }), // 잡담
        });

        // // Util
        // m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Experiment", targetLang), new List<(string, UnityAction)>
        // {
        //     // (LanguageData.Translate("Alarm", targetLang), true ? null : delegate { Debug.Log("[Alarm] 알람 기능 호출됨"); }),
        //     (LanguageData.Translate("20 Questions Game", targetLang), async delegate {
        //         // Full 버전 이상인지 확인
        //         if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
        //         {
        //             return;
        //         }

        //         // 스무고개 게임 모드 토글
        //         MiniGame20QManager.Instance.Toggle20QMode();
        //         string status = MiniGame20QManager.Instance.Is20QMode() ? "활성화" : "비활성화";
        //         Debug.Log($"[20Q] 스무고개 게임 모드 {status}");
                
        //         // 상태 표시
        //         if (MiniGame20QManager.Instance.Is20QMode())
        //         {
        //             // 게임 시작 메시지는 서버에서 오므로 여기서는 표시만
        //             // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        //             // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("스무고개 게임 시작...");

        //             // Panel 표시
        //             UIGame20QPanelManager.Instance.ShowPanel();
        //         }
        //         else
        //         {
        //             AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        //             AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("스무고개 게임 종료됨");

        //             // Panel 숨기기
        //             UIGame20QPanelManager.Instance.HidePanel();
        //         }
        //      }),
        //     (LanguageData.Translate("AROPLA CHANNEL", targetLang), async delegate {
        //         // Full 버전 이상인지 확인
        //         if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
        //         {
        //             return;
        //         }

        //         // 아로프라 채널 모드 토글
        //         if (APIAroPlaManager.Instance != null)
        //         {
        //             APIAroPlaManager.Instance.ToggleAroplaMode();
        //             string status = APIAroPlaManager.Instance.IsAroplaMode() ? "활성화" : "비활성화";
        //             Debug.Log($"아로프라 채널 모드 {status}");
                    
        //             // 상태 표시
        //             AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        //             AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"아로프라 채널 {status}됨");
        //         }
        //         else
        //         {
        //             Debug.LogError("APIAroPlaManager 인스턴스를 찾을 수 없습니다.");
        //         }
        //      }),
        //     (LanguageData.Translate("Set Screenshot Area", targetLang), async delegate {
        //         // Full 버전 이상인지 확인
        //         if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
        //         {
        //             return;
        //         }

        //         ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
        //         if (sm != null) sm.ToggleScreenshotArea();
        //     }),
        //     (LanguageData.Translate("Show Screenshot Result", targetLang), async delegate {
        //         // Full 버전 이상인지 확인
        //         if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
        //         {
        //             return;
        //         }

        //         ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
        //         if (sm != null) {
        //             sm.ShowScreenshotImage();
        //         }
        //     }),
        //     (LanguageData.Translate("Save and Show Screenshot", targetLang), async delegate {
        //         // Full 버전 이상인지 확인
        //         if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
        //         {
        //             return;
        //         }

        //         ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
        //         if (sm != null) {
        //             sm.SaveAndShowScreenshot();
        //         }
        //     })
        // });

        // Dev - UNITY_EDITOR이거나 DevMode가 활성화된 경우에만 표시
        if (isDevModeEnabled)
        {
        // Util
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Experiment", targetLang), new List<(string, UnityAction)>
        {
            // (LanguageData.Translate("Alarm", targetLang), true ? null : delegate { Debug.Log("[Alarm] 알람 기능 호출됨"); }),
            (LanguageData.Translate("20 Questions Game", targetLang), async delegate {
                // Full 버전 이상인지 확인
                if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                {
                    return;
                }

                // 스무고개 게임 모드 토글
                MiniGame20QManager.Instance.Toggle20QMode();
                string status = MiniGame20QManager.Instance.Is20QMode() ? "활성화" : "비활성화";
                Debug.Log($"[20Q] 스무고개 게임 모드 {status}");
                
                // 상태 표시
                if (MiniGame20QManager.Instance.Is20QMode())
                {
                    // 게임 시작 메시지는 서버에서 오므로 여기서는 표시만
                    // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("스무고개 게임 시작...");

                    // Panel 표시
                    UIGame20QPanelManager.Instance.ShowPanel();
                }
                else
                {
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("스무고개 게임 종료됨");

                    // Panel 숨기기
                    UIGame20QPanelManager.Instance.HidePanel();
                }
             }),
            (LanguageData.Translate("AROPLA CHANNEL", targetLang), async delegate {
                // Full 버전 이상인지 확인
                if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                {
                    return;
                }

                // 아로프라 채널 모드 토글
                if (APIAroPlaManager.Instance != null)
                {
                    APIAroPlaManager.Instance.ToggleAroplaMode();
                    string status = APIAroPlaManager.Instance.IsAroplaMode() ? "활성화" : "비활성화";
                    Debug.Log($"아로프라 채널 모드 {status}");
                    
                    // 상태 표시
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"아로프라 채널 {status}됨");
                }
                else
                {
                    Debug.LogError("APIAroPlaManager 인스턴스를 찾을 수 없습니다.");
                }
             }),
            (LanguageData.Translate("Show Screenshot Result", targetLang), async delegate {
                // Full 버전 이상인지 확인
                if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                {
                    return;
                }

                ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
                if (sm != null) {
                    sm.ShowScreenshotImage();
                }
            }),
            (LanguageData.Translate("Save and Show Screenshot", targetLang), async delegate {
                // Full 버전 이상인지 확인
                if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                {
                    return;
                }

                ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
                if (sm != null) {
                    sm.SaveAndShowScreenshot();
                }
            }),
        });


        m_ContextMenu.AddMenuItem(LanguageData.Translate("Debug", targetLang), delegate
        {
            // EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), 10.0f);

            // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Testing...");
            // StartCoroutine(ScenarioCommonManager.Instance.Scenario_C02_AskToStartServer());
            // CharManager.Instance.ChangeCharacterFromCharCode("ch0139");
            EmotionManager.Instance.NextEmotion();
            DebugBalloonManager.Instance.ToggleDebugBalloon();
        });
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Dev", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Test", targetLang), delegate {
                ClipboardManager.Instance.GetContentFromClipboard();
            }),
            (LanguageData.Translate("Test2", targetLang), delegate { 
                // 아로프라 채널 모드 토글
                if (APIAroPlaManager.Instance != null)
                {
                    APIAroPlaManager.Instance.ToggleAroplaMode();
                    string status = APIAroPlaManager.Instance.IsAroplaMode() ? "활성화" : "비활성화";
                    Debug.Log($"아로프라 채널 모드 {status}");
                    
                    // 상태 표시
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"아로프라 채널 {status}됨");
                }
                else
                {
                    Debug.LogError("APIAroPlaManager 인스턴스를 찾을 수 없습니다.");
                }
            }),
            (LanguageData.Translate("Test3", targetLang), delegate {
                Debug.Log("Test3 실행 - 전체화면 OCR");

                // OCROptions 생성 (전체화면 OCR)
                OCROptions options = new OCROptions
                {
                    useTranslate = false,
                    displayResults = true,
                    displayOrigin = false,
                    useTTS = false,
                    useAutoClick = false,
                    targetLang = "",
                    targetLangAutoDetect = false
                };
                
                ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
            }),
            (LanguageData.Translate("Test4", targetLang), delegate {
                Debug.Log("Test4 실행 - 영역 OCR");

                // OCROptions 생성 (영역 OCR)
                OCROptions options = new OCROptions
                {
                    useTranslate = false,
                    displayResults = true,
                    displayOrigin = false,
                    useTTS = false,
                    useAutoClick = false,
                    targetLang = "",
                    targetLangAutoDetect = false
                };
                
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }),
            (LanguageData.Translate("Test5", targetLang), delegate {
                Debug.Log("Test5 실행 - 전체화면 OCR + 번역");

                // OCROptions 생성 (전체화면 OCR + 번역)
                OCROptions options = new OCROptions
                {
                    useTranslate = true,
                    displayResults = true,
                    displayOrigin = false,
                    useTTS = false,
                    useAutoClick = false,
                    targetLang = "ko",
                    originLang = "ja",
                    isFormality = true,
                    isSentence = true,
                    mergeThreshold = -1
                };
                
                ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
            }),
            (LanguageData.Translate("Test6", targetLang), delegate {
                Debug.Log("Test6 실행 - 영역 OCR + 번역");

                // OCROptions 생성 (영역 OCR + 번역)
                OCROptions options = new OCROptions
                {
                    useTranslate = true,
                    displayResults = true,
                    displayOrigin = false,
                    useTTS = false,
                    useAutoClick = false,
                    targetLang = "ko",
                    originLang = "ja",
                    isFormality = true,
                    isSentence = true,
                    mergeThreshold = -1
                };
                
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }),
            (LanguageData.Translate("Test7", targetLang), delegate {
                Debug.Log("Test7 실행 - 영역 OCR → TTS (일본어)");

                // dev_voice 활성화
                if (SettingManager.Instance != null && SettingManager.Instance.settings != null)
                {
                    SettingManager.Instance.settings.isDevSound = true;
                    Debug.Log("Test7: dev_voice 활성화");
                }

                // OCROptions 생성 (영역 OCR + TTS 일본어)
                OCROptions options = new OCROptions
                {
                    useTranslate = false,
                    displayResults = false,
                    displayOrigin = false,
                    useTTS = true,
                    actorTypeIdx = 0,
                    actorType = "Auto",
                    ttsAutoDetectLang = false,
                    useAutoClick = false,
                    targetLang = "ja",
                    targetLangAutoDetect = false
                };
                
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }),
            (LanguageData.Translate("Test8", targetLang), delegate {
                Debug.Log("Test8 실행 - 영역 OCR → TTS (자동 언어 감지)");

                // dev_voice 활성화
                if (SettingManager.Instance != null && SettingManager.Instance.settings != null)
                {
                    SettingManager.Instance.settings.isDevSound = true;
                    Debug.Log("Test8: dev_voice 활성화");
                }

                // OCROptions 생성 (영역 OCR + TTS 자동 언어 감지)
                OCROptions options = new OCROptions
                {
                    useTranslate = false,
                    displayResults = false,
                    displayOrigin = false,
                    useTTS = true,
                    actorTypeIdx = 0,
                    actorType = "Auto",
                    ttsAutoDetectLang = true,
                    useAutoClick = false,
                    targetLang = "",
                    targetLangAutoDetect = true
                };
                
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }),
            (LanguageData.Translate("Test9", targetLang), delegate {
                Debug.Log("Test9 실행 - 전체화면 OCR → '안녕' 텍스트 자동 클릭");
                
                // OCROptions 생성 (전체화면 OCR + 자동 클릭)
                OCROptions options = new OCROptions
                {
                    useTranslate = false,
                    displayResults = false,
                    displayOrigin = false,
                    useTTS = false,
                    useAutoClick = true,
                    clickExactMatch = true,
                    clickWhitelist = new List<string> { "안녕" },
                    clickBlacklist = new List<string>(),
                    targetLang = "",
                    targetLangAutoDetect = false
                };
                
                ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
            }),
            (LanguageData.Translate("OCR", targetLang), delegate {
                Debug.Log("OCR");
                
                OCROptions options = OCRManager.Instance.GetCurrentOptions();                    
                // Screenshot 영역이 설정되어 있으면 영역 OCR, 아니면 전체 화면 OCR
                if (ScreenshotManager.Instance.IsScreenshotAreaSet())
                {
                    Debug.Log("Screenshot 영역이 설정되어 있음 - 영역 OCR 실행");
                    ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
                }
                else
                {
                    Debug.Log("Screenshot 영역이 설정되지 않음 - 전체 화면 OCR 실행");
                    ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
                }
            })
            });
        }

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

    // 더블클릭 시 호출되는 메서드 (현재는 메뉴를 띄우지만, 나중에 다른 기능으로 변경될 수 있음)
    private void OnDoubleClick()
    {
        TriggerMenu();
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

