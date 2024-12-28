using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class Conversation
{
    public string speaker;
    public string message;
    public string message_trans;
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
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string filename = "conversation_memory_" + nickname + ".json";

        return Path.Combine(Application.persistentDataPath, filename);
    }

    // 대화 저장
    public void SaveConversationMemory(string speaker, string message, string messageTrans = "")
    {
        if (string.IsNullOrEmpty(messageTrans))
        {
            messageTrans = message;
        }

        List<Conversation> data = GetAllConversationMemory();

        data.Add(new Conversation
        {
            speaker = speaker,
            message = message,
            message_trans = messageTrans
        });

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(GetFileName(), json);
    }

    // 모든 대화 불러오기
    public List<Conversation> GetAllConversationMemory()
    {
        string fileName = GetFileName();
        if (!File.Exists(fileName))
        {
            return new List<Conversation>();
        }

        string json = File.ReadAllText(fileName);
        return JsonConvert.DeserializeObject<List<Conversation>>(json) ?? new List<Conversation>();
    }

    // 최신 대화 가져오기
    public List<Conversation> GetLatestConversationMemory(int conversationMemoryNumber)
    {
        List<Conversation> data = GetAllConversationMemory();
        return data.Skip(Mathf.Max(0, data.Count - conversationMemoryNumber)).ToList();
    }

    // 대화 초기화
    public void ResetConversationMemory()
    {
        File.WriteAllText(GetFileName(), JsonConvert.SerializeObject(new List<Conversation>(), Formatting.Indented));
    }

    // 최대 길이만큼의 대화 가져오기
    public (List<Conversation>, int, int) GetTruncatedConversationMemory(int maxLen = 2048)
    {
        var (greetingList, greetingLen) = GetGreetingDialogue();
        maxLen -= greetingLen;

        List<Conversation> conversationMemory = GetAllConversationMemory();
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
    public void DeleteRecentDialogue()
    {
        List<Conversation> data = GetAllConversationMemory();
        if (data.Count > 0)
        {
            data.RemoveAt(data.Count - 1);
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(GetFileName(), json);
    }
}
