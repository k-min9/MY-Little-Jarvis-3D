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

        m_ContextMenu.Clear();

        string targetLang = SettingManager.Instance.settings.ui_language;
        bool isSampleVer = SettingManager.Instance.GetInstallStatus() == 0;
#if UNITY_EDITOR
        isSampleVer = false;
#endif

        bool isDevModeEnabled = false;
#if UNITY_EDITOR
        isDevModeEnabled = true;
#else
        if (DevManager.Instance != null)
        {
            isDevModeEnabled = DevManager.Instance.IsDevModeEnabled();
        }
#endif

        // Settings
        m_ContextMenu.AddMenuItem("Settings", delegate {
            UIManager.Instance.showSettings();
        });

        // Character 서브메뉴
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Character", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Action", targetLang), delegate { OnPointerDownRadialMenuAction(); }),
            (LanguageData.Translate("Change Char", targetLang), delegate { UIManager.Instance.ShowCharChange();}),
            (LanguageData.Translate("Summon Char", targetLang), delegate { UIManager.Instance.ShowCharSummon(); }),
            (LanguageData.Translate("Change Clothes", targetLang),
                (_charAttributes != null && (_charAttributes.toggleClothes != null || _charAttributes.changeClothes != null))
                ? (UnityAction)(() => {
                    CharManager.Instance.ChangeClothes();
                })
                : null
            ),
        });

        // Chat 서브메뉴
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Chat", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Guideline", targetLang), delegate { UIManager.Instance.ShowGuideLine(); }),
            (LanguageData.Translate("Situation", targetLang), delegate { UIManager.Instance.ShowUIChatSituation(); }),
            (LanguageData.Translate("New Chat", targetLang), delegate {
                MemoryManager.Instance.ResetConversationMemoryAndGuide();
            }),
            (LanguageData.Translate("Chat History", targetLang), delegate { UIManager.Instance.ShowChatHistory(); }),
        });

        // Control 서브메뉴
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

        // Talk 서브메뉴
        m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Talk", targetLang), new List<(string, UnityAction)>
        {
            (LanguageData.Translate("Edition 튜토리얼", targetLang), delegate { ScenarioInstallerManager.Instance.StartInstaller(); }),
            (LanguageData.Translate("Idle Talk", targetLang), async delegate {
                if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                {
                    return;
                }
                string purpose = "잡담";
                APIManager.Instance.CallSmallTalkStream(purpose);
            }),
        });

        // Dev 모드 메뉴 (DevMode가 활성화된 경우에만)
        if (isDevModeEnabled)
        {
            m_ContextMenu.AddSubMenuItem(LanguageData.Translate("Experiment", targetLang), new List<(string, UnityAction)>
            {
                (LanguageData.Translate("20 Questions Game", targetLang), async delegate {
                    if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                    {
                        return;
                    }
                    MiniGame20QManager.Instance.Toggle20QMode();
                    string status = MiniGame20QManager.Instance.Is20QMode() ? "활성화" : "비활성화";
                    Debug.Log($"[20Q] 스무고개 게임 모드 {status}");
                    if (MiniGame20QManager.Instance.Is20QMode())
                    {
                        UIGame20QPanelManager.Instance.ShowPanel();
                    }
                    else
                    {
                        AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("스무고개 게임 종료됨");
                        UIGame20QPanelManager.Instance.HidePanel();
                    }
                }),
                (LanguageData.Translate("AROPLA CHANNEL", targetLang), async delegate {
                    if (!await InstallStatusManager.Instance.CheckAndOperateFullAsync())
                    {
                        return;
                    }
                    // ChatModeManager 사용
                    ChatModeManager.Instance.ToggleMode(ChatMode.Aropla);
                    string status = ChatModeManager.Instance.IsAroplaMode() ? "활성화" : "비활성화";
                    Debug.Log($"아로프라 채널 모드 {status}");
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"아로프라 채널 {status}됨");
                }),
                // Operator Mode 종료 옵션 (ChatModeManager 사용)
                ("Exit Operator Mode", delegate {
                    ChatModeManager.Instance.SetMode(ChatMode.Chat);
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Operator 모드 종료됨");
                }),
            });

            m_ContextMenu.AddMenuItem(LanguageData.Translate("Debug", targetLang), delegate
            {
                EmotionManager.Instance.NextEmotion();
                DebugBalloonManager.Instance.ToggleDebugBalloon();
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

        // 메뉴 표시
        m_ContextMenu.Show();
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
