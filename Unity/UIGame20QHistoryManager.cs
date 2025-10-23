// 스무고개 게임 대화 이력 관리자
// UIChatHistoryManager를 기반으로 스무고개 전용으로 작성
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGame20QHistoryManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static UIGame20QHistoryManager instance;
    public static UIGame20QHistoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIGame20QHistoryManager>();
            }
            return instance;
        }
    }

    [Header("Chat History")]
    public GameObject chatSlotSample;  // 복사할 Slot 견본 (비활성화)
    public Transform chatSlotParent;  // Slot 복사 위치
    public Sprite userIcon;  // Sensei용 아이콘
    public Sprite aiIcon;  // AI 캐릭터 아이콘
    
    [Header("Settings")]
    [SerializeField] private int maxChatHistory = 50;  // 최대 대화 이력 개수
    [SerializeField] private bool autoScroll = true;  // 자동 스크롤 여부

    private List<GameObject> chatSlots = new List<GameObject>();  // 생성된 슬롯 리스트

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // sample UI 비활성화
        if (chatSlotSample != null)
        {
            chatSlotSample.SetActive(false);
        }
    }

    // UI에 ChatHistory 불러오기 (MiniGame20QManager에서)
    public void LoadChatHistory()
    {
        if (MiniGame20QManager.Instance == null) return;

        // 기존 채팅Slot 제거
        ClearChatHistory();

        // Memory 대신 MiniGame20QManager의 history에서 가져오기
        List<Dictionary<string, string>> history = MiniGame20QManager.Instance.history;
        
        foreach (var turn in history)
        {
            if (turn.ContainsKey("role") && turn.ContainsKey("content"))
            {
                string role = turn["role"];
                string content = turn["content"];
                
                Sprite iconSprite = aiIcon;
                string speakerName = "AI";
                
                try
                {
                    if (role == "user")
                    {
                        speakerName = SettingManager.Instance.settings.player_name;
                        iconSprite = userIcon;
                    }
                    else if (role == "assistant")
                    {
                        speakerName = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
                        iconSprite = CharManager.Instance.GetCharSprite(CharManager.Instance.GetCurrentCharacter());
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Log(ex);
                }
                
                AddChatHistoryLog(iconSprite, speakerName, content);
            }
        }
    }

    // 대화 로그 추가 (개별 추가용)
    public void AddChatLog(string message, bool isUser)
    {
        Sprite iconSprite = isUser ? userIcon : aiIcon;
        string speakerName = "AI";
        
        try
        {
            if (isUser)
            {
                speakerName = SettingManager.Instance.settings.player_name;
            }
            else
            {
                speakerName = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
                iconSprite = CharManager.Instance.GetCharSprite(CharManager.Instance.GetCurrentCharacter());
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log(ex);
        }
        
        AddChatHistoryLog(iconSprite, speakerName, message);
    }

    // ChatHistory의 Chat 추가
    private void AddChatHistoryLog(Sprite iconSprite, string speakerName, string chatMessage)
    {
        if (chatSlotSample == null || chatSlotParent == null) return;

        // 최대 개수 체크
        if (chatSlots.Count >= maxChatHistory)
        {
            // 가장 오래된 슬롯 제거
            GameObject oldestSlot = chatSlots[0];
            chatSlots.RemoveAt(0);
            Destroy(oldestSlot);
        }

        // 슬롯 샘플 복제
        GameObject newSlot = Instantiate(chatSlotSample, chatSlotParent);
        newSlot.SetActive(true);
        chatSlots.Add(newSlot);

        // 아이콘 이미지 설정
        Image iconImage = newSlot.transform.Find("Icon/Icon")?.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = iconSprite;
        }
        
        // 텍스트-Name
        Text nameText = newSlot.transform.Find("Text/Name")?.GetComponent<Text>();
        if (nameText != null)
        {
            nameText.text = speakerName;
        }

        // 텍스트-Chat Log
        Text chatText = newSlot.transform.Find("Text/Chat")?.GetComponent<Text>();
        if (chatText != null)
        {
            chatText.text = chatMessage;
        }
        
        // 자동 스크롤
        if (autoScroll)
        {
            Canvas.ForceUpdateCanvases();
            ScrollToBottom();
        }
    }

    // 스크롤뷰를 최하단으로 이동
    private void ScrollToBottom()
    {
        if (chatSlotParent != null && chatSlotParent.parent.GetComponent<ScrollRect>() != null)
        {
            ScrollRect scrollRect = chatSlotParent.parent.GetComponent<ScrollRect>();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    // 기존 채팅 기록 모두 제거
    public void ClearChatHistory()
    {
        // chatSlotSample은 제외하고 모든 슬롯 삭제
        foreach (GameObject slot in chatSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        chatSlots.Clear();
    }

    // 최대 대화 이력 개수 설정
    public void SetMaxChatHistory(int max)
    {
        maxChatHistory = Mathf.Max(10, max);
    }

    // 자동 스크롤 설정
    public void SetAutoScroll(bool enable)
    {
        autoScroll = enable;
    }

    // 현재 대화 이력 개수 반환
    public int GetChatHistoryCount()
    {
        return chatSlots.Count;
    }
}

