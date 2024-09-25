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

    private bool itemChekFlag = false;

    // Start is called before the first frame update
    private void Start()
    {
        this.m_ContextMenu = WidgetUtility.Find<ContextMenu>("ContextMenu");
        this._transparentWindow = FindObjectOfType<TransparentWindow>();  // GameObject에 있음
    }

    private void Update()
    {
        // itemCheck가 null이 아닌데, active가 아님 = 메뉴가 꺼짐
        if (itemChekFlag)
        {
            if (!m_ContextMenu.IsVisible)  // 자체 제공 함수
            {
                StatusManager.Instance.IsOptioning = false;
                itemChekFlag = false; // 한번 처리 후 flag 초기화
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            this.m_ContextMenu.Clear();

            // 메뉴 추가
            m_ContextMenu.AddMenuItem("Settings", delegate { 
                AnswerBalloonManager.Instance.ShowAnswerBalloon();
                AnswerBalloonManager.Instance.ModifyAnswerBalloonText("Work in Progress...");
            });
            m_ContextMenu.AddMenuItem("Change Char", delegate { CharManager.Instance.ChangeNextChar(); });
            m_ContextMenu.AddMenuItem("Change Monitor", delegate { _transparentWindow.NextMonitor(); });
            m_ContextMenu.AddMenuItem("Local AI Server", delegate { ServerManager.StartServer(); });
            m_ContextMenu.AddMenuItem("Exit", delegate { Application.Quit(); });

            // 메뉴 보이기
            this.m_ContextMenu.Show();

            // StatusManager 관리
            StatusManager.Instance.IsOptioning = true;
            itemChekFlag = true;
        }
    }
}
