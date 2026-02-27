using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

// 다중 캐릭터 대화용 메모리 매니저 (team/participants 기반 파일명 지원)
public class MemoryMultiManager : MonoBehaviour
{
    private static MemoryMultiManager _instance;
    public static MemoryMultiManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MemoryMultiManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("MemoryMultiManager");
                    _instance = obj.AddComponent<MemoryMultiManager>();
                }
            }
            return _instance;
        }
    }

    private string currentTeam = "aropla";  // 현재 활성화된 team
    private List<string> currentParticipants = new List<string> { "sensei", "arona", "plana" };  // 현재 참여자

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            return;
        }
    }

    // 현재 team 설정
    public void SetTeam(string team)
    {
        currentTeam = team;
    }

    // 현재 participants 설정
    public void SetParticipants(List<string> participants)
    {
        currentParticipants = participants ?? new List<string> { "sensei", "arona", "plana" };
    }

    // 파일명 생성 (team 있으면 team 기반, 없으면 participants 정렬 후 join)
    public string GetFileName(string team = null, List<string> participants = null)
    {
        string filename;
        
        if (!string.IsNullOrEmpty(team))
        {
            // team 기반 파일명
            filename = $"conversation_multi_memory_{team}.json";
        }
        else if (participants != null && participants.Count > 0)
        {
            // participants 정렬해서 _로 join
            var sortedParticipants = participants.OrderBy(p => p).ToList();
            string participantsKey = string.Join("_", sortedParticipants);
            filename = $"conversation_multi_memory_{participantsKey}.json";
        }
        else if (!string.IsNullOrEmpty(currentTeam))
        {
            // 현재 team 사용
            filename = $"conversation_multi_memory_{currentTeam}.json";
        }
        else
        {
            // 현재 participants 사용
            var sortedParticipants = currentParticipants.OrderBy(p => p).ToList();
            string participantsKey = string.Join("_", sortedParticipants);
            filename = $"conversation_multi_memory_{participantsKey}.json";
        }

        return Path.Combine(Application.persistentDataPath, filename);
    }

    // 대화 저장 (다국어 지원)
    public void SaveConversationMemory(string speaker, string role, string message, 
        string messageKo, string messageJp, string messageEn, 
        string team = null, List<string> participants = null)
    {
        string targetFile = GetFileName(team, participants);
        List<Conversation> data = GetAllMemory(team, participants);

        // 새 대화 추가
        data.Add(new Conversation
        {
            speaker = speaker,
            role = role,
            message = message,
            message_trans = message,
            messageKo = messageKo,
            messageJp = messageJp,
            messageEn = messageEn,
            type = "conversation",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        // 파일 저장
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetFile, json);
    }

    // 시스템 메시지 저장
    public void SaveSystemMemory(string speaker, string role, string message,
        string messageKo, string messageJp, string messageEn,
        string team = null, List<string> participants = null)
    {
        string targetFile = GetFileName(team, participants);
        List<Conversation> data = GetAllMemory(team, participants);

        // 시스템 메시지 추가
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

        // 파일 저장
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetFile, json);
    }

    // 모든 메모리 불러오기 (시스템 메시지 포함)
    public List<Conversation> GetAllMemory(string team = null, List<string> participants = null)
    {
        string fileName = GetFileName(team, participants);
        if (!File.Exists(fileName))
        {
            return new List<Conversation>();
        }

        string json = File.ReadAllText(fileName);
        return JsonConvert.DeserializeObject<List<Conversation>>(json) ?? new List<Conversation>();
    }

    // 대화만 불러오기 (type="conversation")
    public List<Conversation> GetAllConversationMemory(string team = null, List<string> participants = null)
    {
        var allData = GetAllMemory(team, participants);
        return allData.Where(c => string.IsNullOrEmpty(c.type) || c.type == "conversation").ToList();
    }

    // 최신 대화 가져오기
    public List<Conversation> GetLatestConversationMemory(int count, string team = null, List<string> participants = null)
    {
        List<Conversation> data = GetAllConversationMemory(team, participants);
        return data.Skip(Mathf.Max(0, data.Count - count)).ToList();
    }

    // API 전송용 memory_multi JSON 문자열 반환
    public string GetMemoryMultiJson(string team = null, List<string> participants = null, int? maxCount = null)
    {
        List<Conversation> data;
        
        if (maxCount.HasValue)
        {
            data = GetLatestConversationMemory(maxCount.Value, team, participants);
        }
        else
        {
            data = GetAllConversationMemory(team, participants);
        }

        return JsonConvert.SerializeObject(data);
    }

    // 대화 초기화
    public void ResetConversationMemory(string team = null, List<string> participants = null)
    {
        string targetFile = GetFileName(team, participants);
        File.WriteAllText(targetFile, JsonConvert.SerializeObject(new List<Conversation>(), Formatting.Indented));
    }

    // 마지막 대화 삭제
    public void DeleteRecentDialogue(string team = null, List<string> participants = null)
    {
        string targetFile = GetFileName(team, participants);
        List<Conversation> data = GetAllConversationMemory(team, participants);
        
        if (data.Count > 0)
        {
            data.RemoveAt(data.Count - 1);
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(targetFile, json);
    }

    // 대화 개수 반환
    public int GetConversationCount(string team = null, List<string> participants = null)
    {
        return GetAllConversationMemory(team, participants).Count;
    }

    // 특정 언어로 메시지 가져오기
    public List<Conversation> GetMessagesInLanguage(string language, string team = null, List<string> participants = null)
    {
        var memories = GetAllConversationMemory(team, participants);
        return memories.Where(m =>
        {
            return language switch
            {
                "ko" => !string.IsNullOrEmpty(m.messageKo),
                "jp" => !string.IsNullOrEmpty(m.messageJp),
                "ja" => !string.IsNullOrEmpty(m.messageJp),
                "en" => !string.IsNullOrEmpty(m.messageEn),
                _ => !string.IsNullOrEmpty(m.message)
            };
        }).ToList();
    }
}
