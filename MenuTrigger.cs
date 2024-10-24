// From ContextMenuTrigger
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames.UIWidgets;
using ContextMenu = DevionGames.UIWidgets.ContextMenu;
using UnityEngine.EventSystems;
using System;

public class MenuTrigger : MonoBehaviour, IPointerDownHandler
{
    private TransparentWindow _transparentWindow;

    private ContextMenu m_ContextMenu;

    private bool itemChkFlag = false;
    private float chkTimer = 0f; // 타이머 변수 추가


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

            if (!m_ContextMenu.IsVisible)  // 자체 제공 함수
            {
                StatusManager.Instance.IsOptioning = false;
                itemChkFlag = false; // 한번 처리 후 flag 초기화
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            this.m_ContextMenu.Clear();

            // 메뉴 추가
            // m_ContextMenu.AddMenuItem("SimpleBalloonTest", delegate { 
            //     AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
            //     AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Work in Progress...");
            // });
            m_ContextMenu.AddMenuItem("Settings", delegate { 
                UIManager.Instance.showSettings();  // 캐릭터 변경 UI 보이기
            });
            m_ContextMenu.AddMenuItem("Idle", delegate { 
                StatusManager.Instance.IsFalling = false;
                StatusManager.Instance.IsPicking = false;
                StatusManager.Instance.IsOptioning = false;
                PhysicsManager.Instance.SetIdleState();  // TODO : 캐릭터 spawn 만들 경우, 싱글톤에서 handler 까지 낮춰야 함
            });
            m_ContextMenu.AddMenuItem("Go Left", delegate { 
                StatusManager.Instance.IsFalling = false;
                StatusManager.Instance.IsPicking = false;
                StatusManager.Instance.IsOptioning = false;
                PhysicsManager.Instance.SetWalkLeftState();  // TODO : 캐릭터 spawn 만들 경우, 싱글톤에서 handler 까지 낮춰야 함
            });
            m_ContextMenu.AddMenuItem("Go Right", delegate { 
                StatusManager.Instance.IsFalling = false;
                StatusManager.Instance.IsPicking = false;
                StatusManager.Instance.IsOptioning = false;
                PhysicsManager.Instance.SetWalkRightState();  // TODO : 캐릭터 spawn 만들 경우, 싱글톤에서 handler 까지 낮춰야 함
            });
            m_ContextMenu.AddMenuItem("Change Char", delegate { 
                UIManager.Instance.ShowCharChange();  // 캐릭터 변경 UI 보이기
            });
            m_ContextMenu.AddMenuItem("Change Clothes", delegate { 
                CharManager.Instance.ChangeClothes();  // 캐릭터 옷 갈아입기
            });
            m_ContextMenu.AddMenuItem("Change Monitor", delegate { 
                _transparentWindow.NextMonitor(); 
            });      
            // m_ContextMenu.AddMenuItem("Set Screenshot Area", delegate {  
            //     ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();  // 최상위 ScreenshotManager
            //     sm.SetScreenshotArea();
            // });
            // m_ContextMenu.AddMenuItem("Get Screenshot", delegate { 
            //     ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();  // 최상위 ScreenshotManager
            //     sm.SaveScreenshot(); 
            // });
            m_ContextMenu.AddMenuItem("Local AI Server", delegate { 
                ServerManager.AskStartServer();
            });
            m_ContextMenu.AddMenuItem("Version", delegate { 
                UIManager.Instance.ShowVersion();  // 캐릭터 변경 UI 보이기
            });
            m_ContextMenu.AddMenuItem("Exit", delegate { Application.Quit(); 
            });

            // 메뉴 보이기
            this.m_ContextMenu.Show();

            // StatusManager 관리 (1초 후)
            StatusManager.Instance.IsOptioning = true;

            chkTimer = 1f;
            itemChkFlag = true;
        }
    }
}
