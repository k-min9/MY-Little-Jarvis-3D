using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class Conversation
{
    public string speaker;      // 캐릭터이름 (sensei, arona, plana, system, player, character)
    public string message;      // 대표메시지 (UI 언어에 따른 메시지)
    public string message_trans; // 기존 호환성 유지용 필드
    
    // 새로운 확장 필드들
    public string role;         // 역할 (user, assistant, system)
    public string type;         // 타입 ("conversation": 일반 대화, "system": 시스템 메시지/트리거/intent)
    public string messageKo;    // 한국어 메시지
    public string messageJp;    // 일본어 메시지  
    public string messageEn;    // 영어 메시지
    public string timestamp;    // 타임스탬프
}

public class MemoryManager : MonoBehaviour
{
    // Singleton instance
    private static MemoryManager _instance;
    public static MemoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MemoryManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("MemoryManager");
                    _instance = obj.AddComponent<MemoryManager>();
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        // 싱글톤 설정
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
        }
    }

    private string GetFileName()
    {
        // Operator 모드일 경우
        if (ChatModeManager.Instance.IsAroplaMode())
        {
            return Path.Combine(Application.persistentDataPath, "aropla_conversation_memory.json");
        }

        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string filename = "conversation_memory_" + nickname + ".json";

        return Path.Combine(Application.persistentDataPath, filename);
    }

    // 대화 저장 (기존 호환성 유지)
    public void SaveConversationMemory(string speaker, string message, string messageTrans = "", string filename = null)
    {
        if (string.IsNullOrEmpty(messageTrans))
        {
            messageTrans = message;
        }

        string targetFile = string.IsNullOrEmpty(filename) ? GetFileName() : filename;
        List<Conversation> data = GetAllMemory(filename);

        data.Add(new Conversation
        {
            speaker = speaker,
            message = message,
            message_trans = messageTrans,
            type = "conversation"
        });

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetFile, json);
        
        RefreshChatHistoryIfActive();
    }
    
    // 확장된 대화 저장 (새로운 구조)
    public void SaveConversationMemory(string speaker, string role, string message, string messageKo, string messageJp, string messageEn, string filename = null)
    {
        string targetFile = string.IsNullOrEmpty(filename) ? GetFileName() : filename;
        List<Conversation> data = GetAllMemory(filename);

        data.Add(new Conversation
        {
            speaker = speaker,
            role = role,
            message = message,
            message_trans = message, // 기존 호환성
            messageKo = messageKo,
            messageJp = messageJp,
            messageEn = messageEn,
            type = "conversation",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetFile, json);
        
        RefreshChatHistoryIfActive();
    }

    // 시스템 메시지 저장 (트리거, intent ask 등)
    public void SaveSystemMemory(string speaker, string role, string message, string messageKo, string messageJp, string messageEn, string filename = null)
    {
        string targetFile = string.IsNullOrEmpty(filename) ? GetFileName() : filename;
        List<Conversation> data = GetAllMemory(filename);

        data.Add(new Conversation
        {
            speaker = speaker,
            role = role,
            message = message,
            message_trans = message,
            messageKo = messageKo,
            messageJp = messageJp,
            messageEn = messageEn,
            type = "system",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetFile, json);
        
        RefreshChatHistoryIfActive();
    }

    // chatHistory가 활성화되어 있을 때만 채팅 이력 자동 갱신 
    private void RefreshChatHistoryIfActive()
    {
        if (UIManager.Instance.chatHistory.activeSelf)
        {
            UIChatHistoryManager uIChatHistoryManager = UIManager.Instance.chatHistory.GetComponent<UIChatHistoryManager>();
            uIChatHistoryManager.LoadChatHistory();
        }
    }

    // 모든 메모리 불러오기 (필터링 없음 - 시스템 메시지 포함)
    public List<Conversation> GetAllMemory(string filename = null)
    {
        string fileName = string.IsNullOrEmpty(filename) ? GetFileName() : filename;
        if (!File.Exists(fileName))
        {
            return new List<Conversation>();
        }

        string json = File.ReadAllText(fileName);
        return JsonConvert.DeserializeObject<List<Conversation>>(json) ?? new List<Conversation>();
    }

    // 모든 대화 불러오기 (type="conversation"만 반환)
    public List<Conversation> GetAllConversationMemory(string filename = null)
    {
        var allData = GetAllMemory(filename);
        
        // type이 null이거나 "conversation"인 것만 반환 (기존 호환성)
        return allData.Where(c => string.IsNullOrEmpty(c.type) || c.type == "conversation").ToList();
    }

    // 최신 대화 가져오기
    public List<Conversation> GetLatestConversationMemory(int conversationMemoryNumber, string filename = null)
    {
        List<Conversation> data = GetAllConversationMemory(filename);
        return data.Skip(Mathf.Max(0, data.Count - conversationMemoryNumber)).ToList();
    }

    // 대화 초기화
    public void ResetConversationMemory(string filename = null)
    {
        string targetFile = string.IsNullOrEmpty(filename) ? GetFileName() : filename;
        File.WriteAllText(targetFile, JsonConvert.SerializeObject(new List<Conversation>(), Formatting.Indented));
    }

    // 대화 내용 초기화 및 안내
    public void ResetConversationMemoryAndGuide(string filename = null)
    {
        // Operator 모드일 경우
        if (ChatModeManager.Instance.IsAroplaMode())
        {
            filename = APIAroPlaManager.Instance.GetFileName();
        }

        ResetConversationMemory(filename);
        AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText("Memory Erased");
        UIChatHistoryManager.Instance.ClearChatHistory();
        GlobalTimeVariableManager.Instance.smallTalkTimer = 0f;
    }

    // 최대 길이만큼의 대화 가져오기
    public (List<Conversation>, int, int) GetTruncatedConversationMemory(int maxLen = 2048, string filename = null)
    {
        var (greetingList, greetingLen) = GetGreetingDialogue();
        maxLen -= greetingLen;

        List<Conversation> conversationMemory = GetAllConversationMemory(filename);
        List<Conversation> truncatedMemory = new List<Conversation>();

        int memoryLen = 0;
        int memoryCnt = 0;

        for (int i = conversationMemory.Count - 1; i >= 0; i--)
        {
            var memory = conversationMemory[i];
            string messageTrans = memory.message_trans;
            if (memoryLen + messageTrans.Length >= maxLen)
                break;

            memoryLen += messageTrans.Length;
            memoryCnt++;
            truncatedMemory.Insert(0, memory);
        }

        greetingList.AddRange(truncatedMemory);
        return (greetingList, greetingLen + memoryLen, memoryCnt);
    }

    // 기본 대화 가져오기
    private (List<Conversation>, int) GetGreetingDialogue()
    {
        List<Conversation> greetingList = new List<Conversation>
        {
            new Conversation
            {
                speaker = "player",
                message = "hello, {char}?",
                message_trans = "hello, {char}?"
            },
            new Conversation
            {
                speaker = "character",
                message = "hello. what can i do for you, sensei?",
                message_trans = "hello. what can i do for you, sensei?"
            }
        };

        int greetingLen = greetingList.Sum(g => g.message.Length);
        return (greetingList, greetingLen);
    }

    // 마지막 대화 삭제
    public void DeleteRecentDialogue(string filename = null)
    {
        string targetFile = string.IsNullOrEmpty(filename) ? GetFileName() : filename;
        List<Conversation> data = GetAllConversationMemory(filename);
        if (data.Count > 0)
        {
            data.RemoveAt(data.Count - 1);
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetFile, json);
    }
    
    // 특정 언어로 메시지 가져오기
    public List<Conversation> GetMessagesInLanguage(string language, string filename = null)
    {
        var memories = GetAllConversationMemory(filename);
        return memories.Where(m => 
        {
            return language switch
            {
                "ko" => !string.IsNullOrEmpty(m.messageKo),
                "jp" => !string.IsNullOrEmpty(m.messageJp),
                "en" => !string.IsNullOrEmpty(m.messageEn),
                _ => !string.IsNullOrEmpty(m.message)
            };
        }).ToList();
    }
}
