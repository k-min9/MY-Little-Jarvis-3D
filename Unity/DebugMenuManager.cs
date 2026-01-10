using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DebugMenuManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; 
    [SerializeField] private GameObject debugMenu; // debugMenu 이미지

    public bool isShowing = false;  // EDITOR일때 활성화하는 식으로

    // 싱글톤 인스턴스
    private static DebugMenuManager instance;
    public static DebugMenuManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DebugMenuManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
// #if UNITY_EDITOR
//         isShowing = true;
// #endif
        if (isShowing) {
            ShowDebugMenu();
        } else {
            HideDebugMenu();
        }
        // HideTalkMenu(); // 시작 시 TalkMenu 숨기기
    }

    // 상태 갱신 로직
    private void Update()
    {
        // if (isShowing)
        // {
        //     UpdateTalkMenuPosition();
        // }
    }


    // AnswerBalloon을 타이머 무제한으로 보이기
    public void ShowDebugMenu()
    {
        isShowing = true;
        debugMenu.SetActive(true);
        // UpdateTalkMenuPosition();  // TalkMenu 위치 조정하
    }

    // AnswerBalloon을 숨기는 함수
    public void HideDebugMenu()
    {
        isShowing = false;
        debugMenu.SetActive(false);
    }
}
