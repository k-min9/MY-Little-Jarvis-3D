using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int chatIdx = 0;  // 채팅횟수 + 현재 채팅상황
    public string chatIdxSuccess = "-1";  // 최근 가장 성공한 채팅번호(CallConversationStream 참조)
    public int chatIdxBalloon = -1;  // 답변풍선에 적힌 답변번호

    // 싱글톤 DontDestroyOnLoad 초기화 이슈
    void Start()
    {
        chatIdx = 0;
        chatIdxSuccess = "-1";
        chatIdxBalloon = -1;
    }

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
    }


}
