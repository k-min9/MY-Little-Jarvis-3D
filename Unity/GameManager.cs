using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
            }
            return instance;
        }
    }

    public int chatIdx = 0;  // 채팅횟수 + 현재 채팅상황
    public string chatIdxSuccess = "-1";  // 최근 가장 성공한 채팅번호(CallConversationStream 참조)
    public int chatIdxRegenerateCount = 0;  // 최근 가장 성공한 채팅번호(CallConversationStream 참조)
    public int chatIdxBalloon = -1;  // 답변풍선에 적힌 답변번호
    public bool isWebSearchForced = false;

    void Start()
    {
        chatIdx = 0;
        chatIdxSuccess = "-1";
        chatIdxRegenerateCount = 0;
        chatIdxBalloon = -1;
    }
}
