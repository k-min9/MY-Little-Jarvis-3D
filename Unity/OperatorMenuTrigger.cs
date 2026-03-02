using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DevionGames.UIWidgets;
using ContextMenu = DevionGames.UIWidgets.ContextMenu;

// Operator Portrait용 메뉴 트리거
// 기존 MenuTrigger.cs의 핵심 로직을 복제하여 동일한 컨텍스트 메뉴 제공
// 우클릭/롱프레스/더블클릭 → 메뉴 표시
public class OperatorMenuTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private ContextMenu m_ContextMenu;
    private ContextMenu m_ContextMenuSub;
    private RadialMenu m_RadialMenuAction;
    private CharAttributes _charAttributes;

    // 롱프레스 감지
    private bool isLeftClickHeld = false;
    private float leftClickHoldTime = 0f;
    private const float longPressThreshold = 0.5f;

    // 더블클릭 감지
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private const float doubleClickTime = 0.3f;

    // 메뉴 상태 추적
    private bool itemChkFlag = false;
    private float chkTimer = 0f;

    private void Start()
    {
        InitializeMenuReferences();
    }

    private void OnEnable()
    {
        InitializeMenuReferences();
    }

    private void InitializeMenuReferences()
    {
        if (m_ContextMenu == null)
            m_ContextMenu = WidgetUtility.Find<ContextMenu>("ContextMenu");
        if (m_ContextMenuSub == null)
            m_ContextMenuSub = WidgetUtility.Find<ContextMenu>("ContextMenuSub");
        if (m_RadialMenuAction == null)
            m_RadialMenuAction = WidgetUtility.Find<RadialMenu>("RadialMenuAction");
        if (_charAttributes == null)
            _charAttributes = FindObjectOfType<CharAttributes>();
    }

    private void Update()
    {
        // Operator 모드가 아니면 무시
        if (!ChatModeManager.Instance.IsOperatorMode())
        {
            return;
        }

        // 메뉴 닫힘 감지
        if (itemChkFlag)
        {
            if (chkTimer > 0f)
            {
                chkTimer -= Time.deltaTime;
                return;
            }

            if (m_ContextMenu != null && !m_ContextMenu.IsVisible)
            {
                StatusManager.Instance.IsOptioning = false;
                itemChkFlag = false;
            }
        }

        // 롱프레스 감지
        if (isLeftClickHeld && !StatusManager.Instance.IsDragging)
        {
            leftClickHoldTime += Time.deltaTime;
            if (leftClickHoldTime >= longPressThreshold)
            {
                isLeftClickHeld = false;
                leftClickHoldTime = 0f;
                TriggerMenu();
            }
        }

        // 더블클릭 타이머 관리
        if (clickCount > 0 && Time.time - lastClickTime > doubleClickTime)
        {
            clickCount = 0;
        }

        // RadialMenu 위치 업데이트
        if (m_RadialMenuAction != null && m_RadialMenuAction.IsVisible)
        {
            UpdateRadialMenuActionPosition();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Operator 모드가 아니면 무시
        if (!ChatModeManager.Instance.IsOperatorMode())
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            TriggerMenu();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 더블클릭 감지
            if (Time.time - lastClickTime < doubleClickTime && clickCount == 1)
            {
                OnDoubleClick();
                clickCount = 0;
            }
            else
            {
                clickCount = 1;
                lastClickTime = Time.time;
            }

            isLeftClickHeld = true;
            leftClickHoldTime = 0f;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isLeftClickHeld = false;
            leftClickHoldTime = 0f;
        }
    }

    private void OnDoubleClick()
    {
        TriggerMenu();
    }

    // 컨텍스트 메뉴 표시 - MenuTrigger.TriggerMenu() 로직 복제
    private void TriggerMenu()
    {
        if (m_ContextMenu == null)
        {
            InitializeMenuReferences();
            if (m_ContextMenu == null)
            {
                Debug.LogError("[OperatorMenuTrigger] ContextMenu not found");
                return;
            }
        }

        // 여기서부터 복제
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
                int slot = OCRManager.Instance.GetActiveSlot();
                ScreenshotOCRManager.Instance.ExecuteOCRWithSlot(options, slot);
            }),
        });

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
                // (LanguageData.Translate("20 Questions Game", targetLang), async delegate {
                //     // Full 버전 이상인지 확인
                //     if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                //     {
                //         return;
                //     }

                //     // 스무고개 게임 모드 토글
                //     MiniGame20QManager.Instance.Toggle20QMode();
                //     string status = MiniGame20QManager.Instance.Is20QMode() ? "활성화" : "비활성화";
                //     Debug.Log($"[20Q] 스무고개 게임 모드 {status}");
                    
                //     // 상태 표시
                //     if (MiniGame20QManager.Instance.Is20QMode())
                //     {
                //         // 게임 시작 메시지는 서버에서 오므로 여기서는 표시만
                //         // AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                //         // AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("스무고개 게임 시작...");

                //         // Panel 표시
                //         UIGame20QPanelManager.Instance.ShowPanel();
                //     }
                //     else
                //     {
                //         AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                //         AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("스무고개 게임 종료됨");

                //         // Panel 숨기기
                //         UIGame20QPanelManager.Instance.HidePanel();
                //     }
                //  }),
                (LanguageData.Translate("AROPLA CHANNEL", targetLang), async delegate {
                    // Full 버전 이상인지 확인
                    if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                    {
                        return;
                    }

                    // 아로프라 채널 모드 토글 
                    ChatModeManager.Instance.ToggleMode(ChatMode.Aropla);
                    string status = ChatModeManager.Instance.IsAroplaMode() ? "활성화" : "비활성화";
                    Debug.Log($"아로프라 채널 모드 {status}");
                    
                    // 상태 표시
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"아로프라 채널 {status}됨");

                }),
                (LanguageData.Translate("OPERATOR MODE", targetLang), delegate {
                    // Operator 모드 토글 
                    ChatModeManager.Instance.ToggleMode(ChatMode.Operator);
                    string status = ChatModeManager.Instance.IsOperatorMode() ? "활성화" : "비활성화";
                    Debug.Log($"Operator 모드 {status}");
                        
                    // 상태 표시
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"Operator 모드 {status}됨");
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
        }

#if UNITY_EDITOR
        if(isDevModeEnabled)
        {
            m_ContextMenu.AddMenuItem(LanguageData.Translate("Debug", targetLang), delegate
            {
                StartCoroutine(ScenarioTutorialManager.Instance.Scenario_A04_2_APIKeyInput2());
            });
            m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Dev", targetLang), new List<(string, UnityAction)>
            {
                (LanguageData.Translate("Test", targetLang), delegate {
                    ClipboardManager.Instance.GetContentFromClipboard();
                }),
                (LanguageData.Translate("Test2", targetLang), delegate { 

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
                })
            });
        }
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

    private void OnPointerDownRadialMenuAction()
    {
        if (m_RadialMenuAction == null) return;
        
        Vector2 characterTransformPos = StatusManager.Instance.characterTransform.anchoredPosition;
        m_RadialMenuAction.characterTransformPos = new Vector2(
            characterTransformPos.x, 
            characterTransformPos.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100
        );
        m_RadialMenuAction.Show();
    }

    private void UpdateRadialMenuActionPosition()
    {
        if (m_RadialMenuAction == null) return;
        
        Vector2 characterTransformPos = StatusManager.Instance.characterTransform.anchoredPosition;
        m_RadialMenuAction.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            characterTransformPos.x, 
            characterTransformPos.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100
        );
    }
}
