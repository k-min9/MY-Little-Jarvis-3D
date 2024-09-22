// From ContextMenuTrigger
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevionGames.UIWidgets;
using ContextMenu = DevionGames.UIWidgets.ContextMenu;
using UnityEngine.EventSystems;

public class MenuTrigger : MonoBehaviour, IPointerDownHandler
{
    private ContextMenu m_ContextMenu;

    // Start is called before the first frame update
    private void Start()
    {
        this.m_ContextMenu = WidgetUtility.Find<ContextMenu>("ContextMenu");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            this.m_ContextMenu.Clear();

            // 메뉴 1
            m_ContextMenu.AddMenuItem("Settings", delegate { Debug.Log("Used - " + "Setting"); });

            // 메뉴 2
            m_ContextMenu.AddMenuItem("Char Change", delegate { CharManager.Instance.ChangeNextChar(); });

            // 메뉴 보이기
            this.m_ContextMenu.Show();
        }
    }
}
