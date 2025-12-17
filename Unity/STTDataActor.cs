using System.Collections.Generic;
using System;

// 캐릭터 이름 → actor ID 매핑 데이터
public static class STTDataActor
{
    // 캐릭터 이름 매핑 테이블
    private static Dictionary<string, string> actorMap = new Dictionary<string, string>()
    {
        // arona
        {"アロナ", "arona"},
        {"arona", "arona"},
        {"Arona", "arona"},
        {"아로나", "arona"},
        
        // kisaki
        {"キサキ", "kisaki"},
        {"kisaki", "kisaki"},
        {"Kisaki", "kisaki"},
        {"키사키", "kisaki"},
        
        // koyuki
        {"コユキ", "koyuki"},
        {"koyuki", "koyuki"},
        {"Koyuki", "koyuki"},
        {"코유키", "koyuki"},
        
        // mari
        {"マリー", "mari"},
        {"mari", "mari"},
        {"Mari", "mari"},
        {"마리", "mari"},
        
        // mika
        {"ミカ", "mika"},
        {"mika", "mika"},
        {"Mika", "mika"},
        {"미카", "mika"},
        
        // miyako
        {"ミヤコ", "miyako"},
        {"miyako", "miyako"},
        {"Miyako", "miyako"},
        {"미야코", "miyako"},
        
        // nagisa
        {"ナギサ", "nagisa"},
        {"nagisa", "nagisa"},
        {"Nagisa", "nagisa"},
        {"나기사", "nagisa"},
        {"ナギサ（水着）", "nagisa"},
        
        // noa
        {"ノア", "noa"},
        {"noa", "noa"},
        {"Noa", "noa"},
        {"노아", "noa"},
        
        // plana
        {"プラナ", "plana"},
        {"plana", "plana"},
        {"Plana", "plana"},
        {"플라나", "plana"},
        
        // seia
        {"セイア", "seia"},
        {"seia", "seia"},
        {"Seia", "seia"},
        {"세이아", "seia"},
        
        // ui
        {"ウイ", "ui"},
        {"ui", "ui"},
        {"Ui", "ui"},
        {"우이", "ui"},
        
        // yuuka
        {"ユウカ", "yuuka"},
        {"yuuka", "yuuka"},
        {"Yuuka", "yuuka"},
        {"유우카", "yuuka"},
        
        // 추가 캐릭터는 필요시 여기에 추가
    };
    
    // 블랙리스트 (메타 정보, 불필요한 단어 등)
    private static HashSet<string> wordBlacklist = new HashSet<string>()
    {
        // 그룹/조직 이름
        "ティーパーティー",  // Tea Party (나기사 소속)
        "ティーパーティ",
        "tea party",
        "teaparty",
        
        // 추가 블랙리스트는 필요시 여기에 추가
    };
    
    // 텍스트에서 캐릭터 actor ID 추출
    // 매칭되는 캐릭터가 있으면 actor ID 반환, 없으면 null
    public static string GetActorFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        
        string trimmed = text.Trim();
        
        if (actorMap.ContainsKey(trimmed))
        {
            return actorMap[trimmed];
        }
        
        return null;
    }
    
    // 텍스트가 블랙리스트에 포함되는지 확인
    public static bool IsBlacklisted(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        string trimmed = text.Trim();
        return wordBlacklist.Contains(trimmed);
    }
    
    // actor ID로부터 원본 텍스트(키) 가져오기 (첫 번째 매칭)
    // 예: "nagisa" → "ナギサ" 반환
    public static string GetActorTextFromActorId(string actorId)
    {
        if (string.IsNullOrEmpty(actorId)) return null;
        
        foreach (var kvp in actorMap)
        {
            if (kvp.Value == actorId)
            {
                return kvp.Key;
            }
        }
        
        return null;
    }
    
    // 사용 가능한 모든 Actor ID 목록 반환 (중복 제거)
    // UI Dropdown 등에서 사용
    public static List<string> GetAllActorIds()
    {
        HashSet<string> uniqueActors = new HashSet<string>();
        
        foreach (var kvp in actorMap)
        {
            uniqueActors.Add(kvp.Value);
        }
        
        // 정렬된 리스트로 반환
        List<string> actorList = new List<string>(uniqueActors);
        actorList.Sort();
        return actorList;
    }
    
    // Actor ID를 표시용 이름으로 변환 (그대로 반환)
    // 예: "arona" → "arona", "mika" → "mika"
    public static string GetDisplayName(string actorId)
    {
        if (string.IsNullOrEmpty(actorId)) return "";
        
        // actorId를 그대로 반환 (소문자 형태 유지)
        return actorId;
    }
}

