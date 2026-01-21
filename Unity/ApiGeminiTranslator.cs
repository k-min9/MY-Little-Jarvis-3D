using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;

// Gemini를 사용한 AI 번역
// ai_translate.py의 프롬프트를 Gemma 형식으로 포팅
public class ApiGeminiTranslator : MonoBehaviour
{
    public static ApiGeminiTranslator Instance { get; private set; }
    
    private string apiKey;
    private string modelName = "gemma-3-27b-it";
    
    void Awake()
    {
        Instance = this;
    }
    
    // 번역 메인 함수
    public async Task<TranslationResult> Translate(string text, string targetLang)
    {
        float startTime = Time.time;
        
        try
        {
            // API 키 로드
            apiKey = ApiKei.GetNextGeminiKey();
            
            // 프롬프트 생성
            string prompt = BuildTranslationPrompt(text, targetLang);
            
            // Gemini API 호출
            string translatedText = await CallGeminiAPI(prompt);
            
            // Stop 태그 제거
            translatedText = translatedText
                .Replace("<end_of_turn>", "")
                .Replace("</end_of_turn>", "")
                .Replace("<|im_end|>", "")
                .Replace("<|eot_id|>", "");
            
            // <think> 태그 제거
            translatedText = RemoveThinkTags(translatedText);
            
            float elapsed = Time.time - startTime;
            
            return new TranslationResult
            {
                Origin = text,
                Text = translatedText.Trim(),
                Source = "Gemini",
                Time = elapsed
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ApiGeminiTranslator] Error: {ex.Message}");
            return null;
        }
    }
    
    // Gemma 형식 번역 프롬프트 생성
    private string BuildTranslationPrompt(string text, string targetLang)
    {
        string systemPrompt;
        string userPrefix;
        string assistantPrefix;
        
        targetLang = targetLang.ToLower();
        
        if (targetLang == "ko")
        {
            systemPrompt = GetKoreanTranslationPrompt();
            userPrefix = "입력: ";
            assistantPrefix = "출력: ";
        }
        else if (targetLang == "ja" || targetLang == "jp")
        {
            systemPrompt = GetJapaneseTranslationPrompt();
            userPrefix = "入力: ";
            assistantPrefix = "出力: ";
        }
        else // en
        {
            systemPrompt = GetEnglishTranslationPrompt();
            userPrefix = "Input: ";
            assistantPrefix = "Output: ";
        }
        
        // Gemma 형식으로 조합
        StringBuilder prompt = new StringBuilder();
        prompt.Append("<bos>");
        prompt.Append("<start_of_turn>system\n");
        prompt.Append(systemPrompt);
        prompt.Append("<end_of_turn>\n");
        prompt.Append("<start_of_turn>user\n");
        prompt.Append(userPrefix);
        prompt.Append(text);
        prompt.Append("<end_of_turn>\n");
        prompt.Append("<start_of_turn>model\n");
        prompt.Append(assistantPrefix);
        
        return prompt.ToString();
    }
    
    // 한국어 번역 프롬프트 (ai_translate.py 원본)
    private string GetKoreanTranslationPrompt()
    {
        return @"주어진 텍스트를 한국어로 자연스럽게 번역하세요.

번역 규칙:
1. 원문의 의미를 정확하게 전달
2. 한국어로 자연스럽고 읽기 쉽게 번역
3. 일본어 한자나 영어 단어도 모두 한국어로 번역
4. 문맥에 맞는 적절한 어투 사용
5. 번역 결과만 출력하고 설명이나 주석은 제외

예:

입력: Hello, how are you?
출력: 안녕하세요, 어떻게 지내세요?

입력: The weather is nice today. Let's go for a walk.
출력: 오늘 날씨가 좋네요. 산책하러 가요.

입력: こんにちは、お元気ですか？
출력: 안녕하세요, 잘 지내시나요?

입력: 今日は天気がいいですね。散歩に行きましょう。
출력: 오늘은 날씨가 좋네요. 산책하러 가요.

입력: 昨日は図書館で勉強しました。今日は友達と映画を見に行きます。明日はゆっくり休む予定です。
출력: 어제는 도서관에서 공부했습니다. 오늘은 친구와 영화를 보러 갑니다. 내일은 푹 쉴 예정입니다.

입력: I went to the store yesterday. I bought some bread and milk. The weather was really cold.
출력: 어제 가게에 갔어요. 빵과 우유를 샀어요. 날씨가 정말 추웠어요.";
    }
    
    // 일본어 번역 프롬프트 (ai_translate.py 원본)
    private string GetJapaneseTranslationPrompt()
    {
        return @"与えられたテキストを日本語に自然に翻訳してください。

翻訳ルール:
1. 原文の意味を正確に伝える
2. 日本語として自然で読みやすく翻訳
3. 英語の単語はカタカナに変換
4. 文脈に合った適切な語調を使用
5. 翻訳結果のみを出力し、説明やコメントは除外

例:

入力: Hello, how are you?
出力: こんにちは、お元気ですか？

入力: The weather is nice today. Let's go for a walk.
出力: 今日は天気がいいですね。散歩に行きましょう。

入力: 안녕하세요, 어떻게 지내세요?
出力: こんにちは、お元気ですか？

입력: 오늘 날씨가 좋네요. 산책하러 가요.
出力: 今日は天気がいいですね。散歩に行きましょう。

입력: 어제는 도서관에서 공부했습니다. 오늘은 친구와 영화를 보러 갑니다. 내일은 푹 쉴 예정입니다.
出力: 昨日は図書館で勉強しました。今日は友達と映画を見に行きます。明日はゆっくり休む予定です。

入力: I went to the store yesterday. I bought some bread and milk. The weather was really cold.
出力: 昨日お店に行きました。パンとミルクを買いました。天気は本当に寒かったです。";
    }
    
    // 영어 번역 프롬프트 (ai_translate.py 원본)
    private string GetEnglishTranslationPrompt()
    {
        return @"Translate the given text into natural English.

Translation Rules:
1. Accurately convey the meaning of the original text
2. Translate naturally and readably in English
3. Use appropriate tone for the context
4. Output only the translation result, excluding explanations or comments

Examples:

Input: 안녕하세요, 어떻게 지내세요?
Output: Hello, how are you?

Input: 오늘 날씨가 좋네요. 산책하러 가요.
Output: The weather is nice today. Let's go for a walk.

Input: こんにちは、お元気ですか？
Output: Hello, how are you?

Input: 今日は天気がいいですね。散歩に行きましょう。
Output: The weather is nice today. Let's go for a walk.

Input: 昨日は図書館で勉強しました。今日は友達と映画を見に行きます。明日はゆっくり休む予定です。
Output: I studied at the library yesterday. Today I'm going to watch a movie with my friend. I plan to rest well tomorrow.

Input: 어제는 도서관에서 공부했습니다. 오늘은 친구와 영화를 보러 갑니다. 내일은 푹 쉴 예정입니다.
Output: I studied at the library yesterday. Today I'm going to watch a movie with my friend. I plan to rest well tomorrow.";
    }
    
    // Gemini API 호출
    private async Task<string> CallGeminiAPI(string prompt)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
        
        // Request body 구성 (비스트리밍)
        JObject requestBody = new JObject
        {
            ["contents"] = new JArray
            {
                new JObject
                {
                    ["parts"] = new JArray
                    {
                        new JObject
                        {
                            ["text"] = prompt
                        }
                    }
                }
            },
            ["generationConfig"] = new JObject
            {
                ["temperature"] = 0,  // 결정론적 번역
                ["maxOutputTokens"] = 4096
            }
        };
        
        string jsonRequest = requestBody.ToString();
        
        // HTTP 요청
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";
        
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonRequest);
        request.ContentLength = byteArray.Length;
        
        using (Stream dataStream = await request.GetRequestStreamAsync())
        {
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
        }
        
        // 응답 수신
        using (WebResponse response = await request.GetResponseAsync())
        using (Stream responseStream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(responseStream))
        {
            string responseText = await reader.ReadToEndAsync();
            
            // 응답 파싱
            return ParseGeminiResponse(responseText);
        }
    }
    
    // Gemini 응답 파싱
    private string ParseGeminiResponse(string responseText)
    {
        try
        {
            JObject json = JObject.Parse(responseText);
            
            if (json["candidates"] != null && json["candidates"][0] != null)
            {
                var candidate = json["candidates"][0];
                if (candidate["content"] != null && 
                    candidate["content"]["parts"] != null && 
                    candidate["content"]["parts"][0] != null)
                {
                    return candidate["content"]["parts"][0]["text"]?.ToString() ?? "";
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ApiGeminiTranslator] Parse error: {ex.Message}");
        }
        
        return "";
    }
    
    // <think> 태그 제거 (ai_translate.py와 동일)
    private string RemoveThinkTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        
        // <think>가 있고 </think>가 없으면 빈 문자열
        if (text.Contains("<think>") && !text.Contains("</think>"))
            return "";
        
        // </think>가 있으면 그 이후만 사용
        if (text.Contains("</think>"))
        {
            var parts = text.Split(new string[] { "</think>" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                return parts[1].Trim();
            }
        }
        
        return text.Trim();
    }
}
