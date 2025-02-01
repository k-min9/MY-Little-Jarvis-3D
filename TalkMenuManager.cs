using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TalkMenuManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; 
    [SerializeField] private GameObject talkMenu; // TalkMenu 이미지
    [SerializeField] public RectTransform characterTransform; // TalkMenu 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform talkMenuTransform; // TalkMenu Transform

    public bool isShowing = true;  // 처음부터 활성화?

    // 싱글톤 인스턴스
    private static TalkMenuManager instance;
    public static TalkMenuManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TalkMenuManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
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
    public void ShowTalkMenu()
    {
        isShowing = true;
        talkMenu.SetActive(true);
        // UpdateTalkMenuPosition();  // TalkMenu 위치 조정하
    }

    // AnswerBalloon을 숨기는 함수
    public void HideTalkMenu()
    {
        isShowing = false;
        talkMenu.SetActive(false);
    }

    // AnswerBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateTalkMenuPosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // 캐릭터의 X 위치와 동일하게 설정
        talkMenuTransform.anchoredPosition = new Vector2(charPosition.x + 100 * SettingManager.Instance.settings.char_size / 100f, charPosition.y + 150 * SettingManager.Instance.settings.char_size / 100f); // Y축 창크기 270만큼
    }
}
