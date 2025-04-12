// 스크립트 위치 : ChatHistory GameObject 최상단
using System.Collections;
using System.Collections.Generic;
using DevionGames.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UIChatHistoryManager : MonoBehaviour
{
    [SerializeField] private GameObject uiChatSlotSample;

    private void Start()
    {
        // Memory 가져와보기
        List<Conversation> conversations = MemoryManager.Instance.GetAllConversationMemory();
            Debug.Log("===== 대화 내역 시작 =====");
        foreach (Conversation conversation in conversations)
        {
            string speaker = "Sensei";
            if (conversation.speaker == "") {

            }

            Debug.Log($"{speaker} : {conversation.message}");
        }
        Debug.Log("===== 대화 내역 끝 =====");
    }

}
