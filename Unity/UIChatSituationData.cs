using System.Collections.Generic;
using System.Linq;
using System;

[Serializable]
public class UIChatSituationInfo
{
    public string key;                     // 상황 키 (예: "beach_trip")
    public string lang;                    // 언어 코드 (예: "ko", "en", "jp")
    public string situationTitle;          // 상황 제목 (예: "별빛 아래에서")
    public string situationDescription;    // 전체 상황 설명 (예: "조용한 밤, 별을 바라보는 낭만적인 분위기")
    public List<string> firstMessages;     // 시작 메시지 리스트 (예: ["선생님, 오늘 일정은 어떠세요?"])
    public List<string> tags;              // 상황 태그들 (예: ["밤", "별", "로맨스"])
    public string mood;                    // 분위기 설정 (예: "낭만적", "친근한")
    public string location;                // 장소 정보 (예: "해변", "샬레 집무실")
    public string time;                    // 시간대 정보 (예: "낮", "밤")
    public string weather;                 // 날씨 정보 (예: "맑음", "비")
}

// LanguageManager에서 관리할 Data를 등록후, LanguageManger에서 GameObject 등록
public static class UIChatSituationData
{
    public static readonly Dictionary<string, Dictionary<string, UIChatSituationInfo>> Situations =
        new Dictionary<string, Dictionary<string, UIChatSituationInfo>>
    {
        {
            "default", new Dictionary<string, UIChatSituationInfo>
            {
                {
                    "ko", new UIChatSituationInfo
                    {
                        key = "default",
                        lang = "ko",
                        situationTitle = "기본 설정",
                        situationDescription = "",
                        firstMessages = new List<string>(),
                        tags = new List<string>(),
                        mood = "",
                        location = "",
                        time = "",
                        weather = ""
                    }
                },
                {
                    "en", new UIChatSituationInfo
                    {
                        key = "default",
                        lang = "en",
                        situationTitle = "Default Setting",
                        situationDescription = "",
                        firstMessages = new List<string>(),
                        tags = new List<string>(),
                        mood = "",
                        location = "",
                        time = "",
                        weather = ""
                    }
                },
                {
                    "jp", new UIChatSituationInfo
                    {
                        key = "default",
                        lang = "jp",
                        situationTitle = "デフォルト設定",
                        situationDescription = "",
                        firstMessages = new List<string>(),
                        tags = new List<string>(),
                        mood = "",
                        location = "",
                        time = "",
                        weather = ""
                    }
                }
            }
        },
        {
            "assistant_classroom", new Dictionary<string, UIChatSituationInfo>
            {
                {
                    "ko", new UIChatSituationInfo
                    {
                        key = "assistant_classroom",
                        lang = "ko",
                        situationTitle = "샬레 도우미",
                        situationDescription = "현재 샬레 집무실에서 근무 중입니다. 캐릭터는 그런 선생님을 위해 방문했습니다.",
                        firstMessages = new List<string>
                        {
                            "선생님, 오늘 업무 목록을 정리해드릴까요?",
                            "필요하신 자료가 있으시면 언제든 말씀해주세요.",
                            "오늘 일정은 어떻게 도와드리면 될까요?",
                            "이렇게 맑은데 조금은 쉬어도 되지 않을까요?"
                        },
                        tags = new List<string>{ "업무", "교실", "도우미", "낮" },
                        mood = "친근한",
                        location = "샬레 집무실",
                        time = "낮",
                        weather = "맑음"
                    }
                },
                {
                    "en", new UIChatSituationInfo
                    {
                        key = "assistant_classroom",
                        lang = "en",
                        situationTitle = "SCHALE Assistant",
                        situationDescription = "You are working in the SCHALE office. The character has come to assist you during your duties.",
                        firstMessages = new List<string>
                        {
                            "Shall I help organize your task list for today?",
                            "Please let me know if there's anything you need.",
                            "How may I assist you with today's schedule?",
                            "It’s such a nice day—perhaps you could take a short break?"
                        },
                        tags = new List<string>{ "work", "classroom", "assistant", "day" },
                        mood = "Friendly",
                        location = "SCHALE Office",
                        time = "Daytime",
                        weather = "Clear"
                    }
                },
                {
                    "jp", new UIChatSituationInfo
                    {
                        key = "assistant_classroom",
                        lang = "jp",
                        situationTitle = "シャーレのアシスタント",
                        situationDescription = "現在、シャーレの執務室で勤務中です。キャラクターは先生をお手伝いするために訪れました。",
                        firstMessages = new List<string>
                        {
                            "先生、本日の業務リストを整理いたしましょうか？",
                            "必要な資料があれば、いつでもお申し付けくださいませ。",
                            "本日の予定は、どのようにお手伝いすればよろしいですか？",
                            "天気がいいですね。少し休憩なさってもよろしいのではありませんか？"
                        },
                        tags = new List<string>{ "仕事", "教室", "アシスタント", "昼間" },
                        mood = "親しみやすい",
                        location = "シャーレ執務室",
                        time = "昼",
                        weather = "晴れ"
                    }
                }
            }
        },
        {
            "beach_trip", new Dictionary<string, UIChatSituationInfo>
            {
                {
                    "ko", new UIChatSituationInfo
                    {
                        key = "beach_trip",
                        lang = "ko",
                        situationTitle = "바닷가에서의 하루",
                        situationDescription = "오늘은 해변으로 놀러 온 날입니다. 비일상적인 분위기 속에서 시원한 바다를 만끽하고 있습니다.",
                        firstMessages = new List<string>
                        {
                            "바다가 정말 예쁘네요! 무엇부터 해볼까요?",
                            "이런 날은 그냥 아무것도 하지 않고 쉬어도 좋을 것 같아요.",
                            "선생님, 물놀이 하실래요?"
                        },
                        tags = new List<string>{ "해변", "여름", "비일상", "낮", "여행", "신남" },
                        mood = "신나는",
                        location = "해변",
                        time = "낮",
                        weather = "맑음"
                    }
                },
                {
                    "en", new UIChatSituationInfo
                    {
                        key = "beach_trip",
                        lang = "en",
                        situationTitle = "A Day at the Beach",
                        situationDescription = "It’s a beach day! You’re here to enjoy the summer breeze and break from the ordinary.",
                        firstMessages = new List<string>
                        {
                            "The ocean looks beautiful today! What should we do first?",
                            "It feels like a perfect day to just relax and do nothing.",
                            "Would you like to go for a swim, Teacher?"
                        },
                        tags = new List<string>{ "beach", "summer", "escape", "day", "travel", "fun" },
                        mood = "Excited",
                        location = "Beach",
                        time = "Daytime",
                        weather = "Clear"
                    }
                },
                {
                    "jp", new UIChatSituationInfo
                    {
                        key = "beach_trip",
                        lang = "jp",
                        situationTitle = "海辺での一日",
                        situationDescription = "今日はビーチに遊びに来た日です。非日常の雰囲気の中で、涼しい海を満喫しています。",
                        firstMessages = new List<string>
                        {
                            "海がとても綺麗ですね！まずは何をしましょうか？",
                            "今日は何もしないで、ただゆっくりするのもいいですね。",
                            "先生、水遊びでもなさいませんか？"
                        },
                        tags = new List<string>{ "ビーチ", "夏", "非日常", "昼", "旅行", "楽しい" },
                        mood = "楽しい",
                        location = "海辺",
                        time = "昼",
                        weather = "晴れ"
                    }
                }
            }
        },
        {
            "stargazing_night", new Dictionary<string, UIChatSituationInfo>
            {
                {
                    "ko", new UIChatSituationInfo
                    {
                        key = "stargazing_night",
                        lang = "ko",
                        situationTitle = "별빛 아래에서",
                        situationDescription = "조용한 밤, 둘이 함께 별을 바라보며 낭만적인 분위기를 느끼고 있습니다.",
                        firstMessages = new List<string>
                        {
                            "별이 이렇게 반짝이는 걸 보신 적 있으신가요?",
                            "선생님과 이렇게 별을 보게 되어 기뻐요.",
                            "오늘은 정말 잊지 못할 밤이 될 것 같아요."
                        },
                        tags = new List<string>{ "별", "밤", "조용함", "로맨스", "진전" },
                        mood = "낭만적",
                        location = "야외",
                        time = "밤",
                        weather = "맑음"
                    }
                },
                {
                    "en", new UIChatSituationInfo
                    {
                        key = "stargazing_night",
                        lang = "en",
                        situationTitle = "Under the Starlight",
                        situationDescription = "It’s a quiet night. The two of you are gazing at the stars together in a romantic setting.",
                        firstMessages = new List<string>
                        {
                            "Have you ever seen the stars shine so brightly?",
                            "I'm glad to be watching the stars with you, Teacher.",
                            "Tonight may become a night we will never forget."
                        },
                        tags = new List<string>{ "stars", "night", "quiet", "romance", "bonding" },
                        mood = "Romantic",
                        location = "Outdoors",
                        time = "Night",
                        weather = "Clear"
                    }
                },
                {
                    "jp", new UIChatSituationInfo
                    {
                        key = "stargazing_night",
                        lang = "jp",
                        situationTitle = "星空の下で",
                        situationDescription = "静かな夜、二人で星を眺めながらロマンチックな雰囲気を感じています。",
                        firstMessages = new List<string>
                        {
                            "こんなに星が輝いているのを見たことがありますか？",
                            "先生と一緒に星を見られて、とても嬉しいです。",
                            "今夜はきっと忘れられない夜になりますね。"
                        },
                        tags = new List<string>{ "星", "夜", "静けさ", "ロマンス", "進展" },
                        mood = "ロマンチック",
                        location = "屋外",
                        time = "夜",
                        weather = "晴れ"
                    }
                }
            }
        }
    };
}
