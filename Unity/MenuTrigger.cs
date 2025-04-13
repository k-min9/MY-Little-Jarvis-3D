// From ContextMenuTrigger
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames.UIWidgets;
using ContextMenu = DevionGames.UIWidgets.ContextMenu;
using UnityEngine.EventSystems;
using System;

public class MenuTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private TransparentWindow _transparentWindow;
    private CharAttributes _charAttributes;
    private ContextMenu m_ContextMenu;
    private bool itemChkFlag = false;
    private float chkTimer = 0f; // 타이머 변수 추가
    private bool isLeftClickHeld = false; // 좌클릭 상태
    private float leftClickHoldTime = 0f; // 좌클릭 누른 시간

    private RadialMenu m_RadialMenuAction;

    // Start is called before the first frame update
    private void Start()
    {
        this.m_ContextMenu = WidgetUtility.Find<ContextMenu>("ContextMenu");
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

    private void TriggerMenu()
    {
        this.m_ContextMenu.Clear();

        // 메뉴 추가
        string menuName = "";
        string targetLang = SettingManager.Instance.settings.ui_language; // 0 : ko, 1 : jp, 2: en 

        // setting
        // menuName = LanguageData.Translate("Settings", targetLang);
        m_ContextMenu.AddMenuItem("Settings", delegate {   // Language로 향하는 과정은 전부 영어로만
            UIManager.Instance.showSettings();
        });
        menuName = LanguageData.Translate("Settings", targetLang);
        m_ContextMenu.AddMenuItem("Action", delegate { 
            OnPointerDownRadialMenuAction();
        });
        m_ContextMenu.AddMenuItem("Change Char", delegate { 
            UIManager.Instance.ShowCharChange();
        });
        m_ContextMenu.AddMenuItem("Summon Char", delegate { 
            UIManager.Instance.ShowCharAdd();
        });
        m_ContextMenu.AddMenuItem("Chat History", delegate { 
            UIManager.Instance.ShowChatHistory();
        });

        if (_charAttributes.toggleClothes != null || _charAttributes.changeClothes!=null) {
            m_ContextMenu.AddMenuItem("Change Clothes", delegate { 
                CharManager.Instance.ChangeClothes();
            });
        }

        m_ContextMenu.AddMenuItem("Erase Memory", delegate { 
            MemoryManager.Instance.ResetConversationMemory();

            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Memory Erased");
        });

        if (TalkMenuManager.Instance.isShowing == false) {
            m_ContextMenu.AddMenuItem("Show TalkMenu", delegate { 
                TalkMenuManager.Instance.ShowTalkMenu();
            });
        }

        if (NoticeBalloonManager.Instance.noticeBalloon.activeSelf) {
            m_ContextMenu.AddMenuItem("Hide TalkInfo", delegate { 
                NoticeBalloonManager.Instance.HideNoticeBalloon();
            });
        } else {
            m_ContextMenu.AddMenuItem("Show TalkInfo", delegate { 
                NoticeBalloonManager.Instance.ShowNoticeBalloon();
            });
        }

        #if UNITY_STANDALONE_WIN
        m_ContextMenu.AddMenuItem("Change Monitor", delegate { 
            _transparentWindow.NextMonitor(); 
        });
        m_ContextMenu.AddMenuItem("Set Screenshot Area", delegate {  
            ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();  // 최상위 ScreenshotManager
            sm.SetScreenshotArea();
        });
        m_ContextMenu.AddMenuItem("Get Screenshot", delegate { 
            ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();  // 최상위 ScreenshotManager
            sm.SaveScreenshot(); 
        });
        #endif
        // m_ContextMenu.AddMenuItem("Local AI Server", delegate { 
        //     ServerManager.AskStartServer();
        // });
        m_ContextMenu.AddMenuItem("Version", delegate { 
            UIManager.Instance.ShowVersion();
        });
        // For Test
// #if UNITY_EDITOR
        m_ContextMenu.AddMenuItem("Debug", delegate {  // 테스트용
            Debug.Log("======Test Start======");
            // EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), 10.0f);
            EmotionManager.Instance.NextEmotion();
            Debug.Log("=======Test End=======");
        });
// #endif
        m_ContextMenu.AddMenuItem("Exit", delegate { Application.Quit(); });

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
