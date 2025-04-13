// 스크립트 위치 : ChatHistory GameObject 최상단
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEditor;

// Slot 형태
/*
ChatHistory
ㄴViewport
  ㄴSlots
   ㄴItem_Slot_Sample (복사할 gameobject)
    ㄴIcon
     ㄴIcon (image 아이콘을 변경)
    ㄴText
     ㄴName (Text 변경)
     ㄴChat (Text 변경)
*/

public class UIChatHistoryManager : MonoBehaviour
{
    public GameObject uiChatSlotSample;  // 복사할 Slot 견본 (비활성화)
    public Transform uiChatSlotParent;  // Slot 복사 위치
    public Sprite sampleIcon;  // 실수용 아이콘
    public Sprite userIcon;  // Sensei용 아이콘


    private void Start()
    {
        // sample UI 비활성화
        uiChatSlotSample.SetActive(false);

        // LoadChatHistory 초기화(잉여작업)
        LoadChatHistory();

        // Test 코드
        // TestChatHistory();
    }


    // Ui에 ChatHistory 불러오기
    public void LoadChatHistory()
    {
        // 기존 채팅Slot 제거
        ClearChatHistory();

        // Memory 가져와서 Slot에 내용 추가
        List<Conversation> conversations = MemoryManager.Instance.GetAllConversationMemory();
        foreach (Conversation conversation in conversations)
        {
            Sprite iconSprite = sampleIcon;  // 설정 안했을수도 있으니 맨 마지막에
            string speakerName = "Sensei";  // conversation.speaker == "player"
            string chatMessage = "";
            try 
            {
                chatMessage = conversation.message;
                if (conversation.speaker == "character") {
                    speakerName = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
                    iconSprite = CharManager.Instance.GetCharSprite(CharManager.Instance.GetCurrentCharacter());
                } else {
                    speakerName = SettingManager.Instance.settings.player_name;
                    iconSprite = userIcon;
                }
                
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            // Debug.Log($"{speakerName} : {conversation.message}");
            AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        }
    }

    // ChatHistory의 Chat 추가
    private void AddChatHistoryLog(Sprite iconSprite, string speakerName, string chatMessage)
    {
        // 슬롯 샘플 복제
        GameObject newSlot = Instantiate(uiChatSlotSample, uiChatSlotParent);
        newSlot.SetActive(true);

        // 아이콘 이미지 설정
        Image iconImage = newSlot.transform.Find("Icon/Icon").GetComponent<Image>();
        iconImage.sprite = iconSprite;
        
        // 텍스트-Name
        Text name = newSlot.transform.Find("Text/Name").GetComponent<Text>();
        name.text = speakerName;

        // 텍스트-Chat Log
        Text chat = newSlot.transform.Find("Text/Chat").GetComponent<Text>();
        chat.text = chatMessage;
        
        // 스크롤뷰 최하단으로 스크롤
        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    // 스크롤뷰를 최하단으로 이동
    private void ScrollToBottom()
    {
        if (uiChatSlotParent.parent.GetComponent<ScrollRect>() != null)
        {
            ScrollRect scrollRect = uiChatSlotParent.parent.GetComponent<ScrollRect>();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    // 테스트 Debug 로그
    private void TestChatHistory()
    {
        Sprite iconSprite = sampleIcon;
        string speakerName = "Sensei";  // conversation.speaker == "player"
        string chatMessage = "Text Text\nHello?!";

        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
        AddChatHistoryLog(iconSprite, speakerName, chatMessage);
    }

    // 기존 채팅 기록 모두 제거
    public void ClearChatHistory()
    {
        // uiChatSlotSample은 제외하고 모든 자식 오브젝트 삭제
        foreach (Transform child in uiChatSlotParent)
        {
            // 샘플 슬롯은 삭제하지 않음
            if (child.gameObject != uiChatSlotSample)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
