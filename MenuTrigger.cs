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
    private ContextMenu m_ContextMenu;
    private bool itemChkFlag = false;
    private float chkTimer = 0f; // 타이머 변수 추가
    private bool isLeftClickHeld = false; // 좌클릭 상태
    private float leftClickHoldTime = 0f; // 좌클릭 누른 시간

    // Start is called before the first frame update
    private void Start()
    {
        this.m_ContextMenu = WidgetUtility.Find<ContextMenu>("ContextMenu");
        this._transparentWindow = FindObjectOfType<TransparentWindow>();  // GameObject에 있음
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
        m_ContextMenu.AddMenuItem("Settings", delegate { 
            UIManager.Instance.showSettings();
        });
        m_ContextMenu.AddMenuItem("Idle", delegate { 
            StatusManager.Instance.IsFalling = false;
            StatusManager.Instance.IsPicking = false;
            StatusManager.Instance.IsOptioning = false;
            PhysicsManager.Instance.SetIdleState();
        });
        m_ContextMenu.AddMenuItem("Go Left", delegate { 
            StatusManager.Instance.IsFalling = false;
            StatusManager.Instance.IsPicking = false;
            StatusManager.Instance.IsOptioning = false;
            PhysicsManager.Instance.SetWalkLeftState();
        });
        m_ContextMenu.AddMenuItem("Go Right", delegate { 
            StatusManager.Instance.IsFalling = false;
            StatusManager.Instance.IsPicking = false;
            StatusManager.Instance.IsOptioning = false;
            PhysicsManager.Instance.SetWalkRightState();
        });
        m_ContextMenu.AddMenuItem("Change Char", delegate { 
            UIManager.Instance.ShowCharChange();
        });
        m_ContextMenu.AddMenuItem("Change Clothes", delegate { 
            CharManager.Instance.ChangeClothes();
        });
        m_ContextMenu.AddMenuItem("Erase Memory", delegate { 
            MemoryManager.Instance.ResetConversationMemory();

            AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Memory Erased");
        });
        // m_ContextMenu.AddMenuItem("Change Monitor", delegate { 
        //     _transparentWindow.NextMonitor(); 
        // });
        // m_ContextMenu.AddMenuItem("Set Screenshot Area", delegate {  
        //     ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();  // 최상위 ScreenshotManager
        //     sm.SetScreenshotArea();
        // });
        // m_ContextMenu.AddMenuItem("Get Screenshot", delegate { 
        //     ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();  // 최상위 ScreenshotManager
        //     sm.SaveScreenshot(); 
        // });
        // m_ContextMenu.AddMenuItem("Local AI Server", delegate { 
        //     ServerManager.AskStartServer();
        // });
        m_ContextMenu.AddMenuItem("Version", delegate { 
            UIManager.Instance.ShowVersion();
        });
        m_ContextMenu.AddMenuItem("Exit", delegate { Application.Quit(); });

        // 메뉴 보이기
        this.m_ContextMenu.Show();

        // StatusManager 관리 (1초 후)
        StatusManager.Instance.IsOptioning = true;

        chkTimer = 1f;
        itemChkFlag = true;
    }
}
