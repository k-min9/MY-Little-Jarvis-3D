using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Gemini API 직접 호출 클라이언트
// Python 서버를 경유하지 않고 Unity에서 직접 Gemini API 호출
public class ApiGeminiDirectClient : MonoBehaviour
{
    public static ApiGeminiDirectClient Instance { get; private set; }
    
    private string apiKey;
    private string modelName = "gemma-3-27b-it";
    
    // Stop strings (Python util_gemini.py와 동일)
    private static readonly string[] StopStrings = new string[]
    {
        "\nYou:", "<|im_end|>", "<|im_start|>user",
        "<|im_start|>assistant\n", "\nAI:", "<|eot_id|>",
        "< |", "<start_of_turn>user", "<end_of_turn>"
    };
    
    void Awake()
    {
        Instance = this;
    }
    
    // 스트리밍 대화 호출 (기존 FetchStreamingData와 유사한 패턴)
    public async Task CallGeminiStreamDirect(
        string query,
        string playerName,
        string charName,
        string aiLanguage,
        List<Dictionary<string, string>> memoryList,
        List<string> guidelineList,
        Dictionary<string, object> situationDict,
        string chatIdx,
        System.Action<JObject> onChunkReceived,
        System.Action onComplete
    )
    {
        try
        {
            // API 키 로드 (검증된 키 사용)
            apiKey = await ApiKei.GetValidatedGeminiKey();
            
            // 프롬프트 생성
            string prompt = ApiGeminiPromptBuilder.BuildGemmaPrompt(
                query, playerName, charName, memoryList,
                aiLanguage, guidelineList, situationDict
            );
            
            Debug.Log($"[GeminiDirect] Prompt length: {prompt.Length}");
            
            // Gemini API 호출
            await StreamGeminiAPI(prompt, aiLanguage, chatIdx, onChunkReceived);
            
            // 완료 콜백
            onComplete?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GeminiDirect] Error: {ex.Message}");
            onComplete?.Invoke();
        }
    }
    
    // Gemini API 스트리밍 호출 (SSE 방식)
    private async Task StreamGeminiAPI(
        string prompt,
        string language,
        string chatIdx,
        System.Action<JObject> onChunkReceived
    )
    {
        // SSE 방식: alt=sse 파라미터 추가
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";
        
        // Request body 구성
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
                ["temperature"] = 0.7,
                ["topP"] = 0.9,
                ["maxOutputTokens"] = 2048,
                // 서버 사이드 stop sequences (최대 5개 권장)
                ["stopSequences"] = new JArray 
                { 
                    "<end_of_turn>", 
                    "<|im_end|>",
                    "\nYou:",
                    "\nAI:"
                }
            }
        };
        
        string jsonRequest = requestBody.ToString();
        SaveRequestLog(jsonRequest);
        
        // HTTP 요청 생성
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Accept = "text/event-stream"; // SSE 헤더
        
        // Request body 전송
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonRequest);
        request.ContentLength = byteArray.Length;
        
        using (Stream dataStream = await request.GetRequestStreamAsync())
        {
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
        }
        
        // 응답 수신 및 SSE 스트리밍 처리
        StringBuilder responseLog = new StringBuilder();
        
        using (WebResponse response = await request.GetResponseAsync())
        {
            // Content-Type 확인 (디버깅용)
            string contentType = response.ContentType;
            Debug.Log($"[GeminiDirect] Response Content-Type: {contentType}");
            responseLog.AppendLine($"Content-Type: {contentType}");
            responseLog.AppendLine();
            
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
            {
                string accumulatedText = "";
                List<string> sentencesAlreadySent = new List<string>();
                
                // SSE 이벤트 payload 누적 버퍼
                StringBuilder eventData = new StringBuilder();
                
                while (true)
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null) break;
                    
                    responseLog.AppendLine(line);
                    
                    // SSE 이벤트는 빈 줄에서 종료
                    if (line.Length == 0)
                    {
                        if (eventData.Length > 0)
                        {
                            string payload = eventData.ToString();
                            eventData.Clear();
                            
                            // JSON에서 텍스트 추출
                            if (TryExtractChunkTextFromJson(payload, out string chunkText))
                            {
                                accumulatedText += chunkText;
                                
                                // Stop string 확인
                                string cleanedText;
                                bool stopFound;
                                (cleanedText, stopFound) = ApplyStoppingStrings(accumulatedText);
                                
                                // 문장 분리
                                List<string> sentences = GetPunctuationSentences(cleanedText);
                                
                                // 새로운 문장만 전송
                                for (int i = sentencesAlreadySent.Count; i < sentences.Count; i++)
                                {
                                    string sentence = sentences[i];

                                    // 선생님 이름 적용
                                    if (!string.IsNullOrEmpty(playerName))
                                        sentence = System.Text.RegularExpressions.Regex.Replace(sentence, @"(<USER>|<user>|{{user}})", playerName);
                                    else
                                        sentence = System.Text.RegularExpressions.Regex.Replace(sentence, @"(<USER>|<user>|{{user}})", "You");
                                    
                                    // JObject 생성 (기존 서버 형식과 호환)
                                    JObject responseJson = CreateResponseJson(sentence, language, chatIdx);
                                    
                                    // 콜백 호출
                                    onChunkReceived?.Invoke(responseJson);
                                    
                                    sentencesAlreadySent.Add(sentence);
                                }
                                
                                // Stop string 발견 시 종료
                                if (stopFound)
                                {
                                    Debug.Log("[GeminiDirect] Stop string found");
                                    break;
                                }
                            }
                        }
                        continue;
                    }
                    
                    // SSE 규약: data: 로 시작하는 라인만 payload로 취급
                    if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        string dataLine = line.Substring(5).TrimStart();
                        
                        // [DONE] 시그널 확인
                        if (dataLine == "[DONE]")
                        {
                            Debug.Log("[GeminiDirect] Received [DONE] signal");
                            break;
                        }
                        
                        // 멀티라인 data를 개행으로 연결
                        if (eventData.Length > 0)
                        {
                            eventData.Append("\n");
                        }
                        eventData.Append(dataLine);
                    }
                }
            }
        }
        
        // 응답 로그 저장
        SaveResponseLog(responseLog.ToString());
    }
    
    // SSE JSON에서 텍스트 추출
    private bool TryExtractChunkTextFromJson(string json, out string text)
    {
        text = "";
        try
        {
            JObject obj = JObject.Parse(json);
            
            var cand = obj["candidates"]?[0];
            if (cand == null) return false;
            
            var parts = cand["content"]?["parts"] as JArray;
            if (parts == null || parts.Count == 0) return false;
            
            // parts의 모든 text를 합침 (안전성 향상)
            StringBuilder sb = new StringBuilder();
            foreach (var p in parts)
            {
                string t = p?["text"]?.ToString();
                if (!string.IsNullOrEmpty(t))
                {
                    sb.Append(t);
                }
            }
            
            text = sb.ToString();
            return text.Length > 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GeminiDirect] JSON parse error: {ex.Message}");
            return false;
        }
    }
    
    // 요청 로그 저장
    private void SaveRequestLog(string requestJson)
    {
        try
        {
            string logPath = Path.Combine(Application.persistentDataPath, "gemini_request.json");
            File.WriteAllText(logPath, requestJson, Encoding.UTF8);
            Debug.Log($"[GeminiDirect] Request log saved: {logPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GeminiDirect] Failed to save request log: {ex.Message}");
        }
    }
    
    // 응답 로그 저장
    private void SaveResponseLog(string responseText)
    {
        try
        {
            string logPath = Path.Combine(Application.persistentDataPath, "gemini_response.txt");
            File.WriteAllText(logPath, responseText, Encoding.UTF8);
            Debug.Log($"[GeminiDirect] Response log saved: {logPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GeminiDirect] Failed to save response log: {ex.Message}");
        }
    }
    
    // Stop string 처리 (Python apply_stopping_strings 포팅)
    private (string reply, bool stopFound) ApplyStoppingStrings(string reply)
    {
        bool stopFound = false;
        
        foreach (string stopString in StopStrings)
        {
            int idx = reply.IndexOf(stopString);
            if (idx != -1)
            {
                reply = reply.Substring(0, idx);
                stopFound = true;
                break;
            }
        }
        
        if (!stopFound)
        {
            // 부분 매칭 처리 (예: "\nYo"가 생성되었을 때 제거)
            foreach (string stopString in StopStrings)
            {
                for (int j = stopString.Length - 1; j > 0; j--)
                {
                    if (reply.Length >= j && reply.Substring(reply.Length - j) == stopString.Substring(0, j))
                    {
                        reply = reply.Substring(0, reply.Length - j);
                        break;
                    }
                }
            }
        }
        
        return (reply, stopFound);
    }
    
    // 문장 분리 (Python util_string.get_punctuation_sentences 포팅)
    private List<string> GetPunctuationSentences(string text)
    {
        const string UNIQUE2 = "#@_";
        
        // ** 제거
        text = text.Replace("**", "");
        
        // (내용), [내용], *내용* 제거
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\([^)]*\)", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*[^*]*\*", "");
        text = text.Trim();
        
        // 문장 분리 기준으로 교체
        string[] separators = { ". ", ".\n", "? ", "?\n", "! ", "!\n", "？", "。", "！" };
        foreach (string sep in separators)
        {
            if (sep.Length == 1)
            {
                text = text.Replace(sep, sep + UNIQUE2);
            }
            else
            {
                text = text.Replace(sep, sep[0] + UNIQUE2 + sep.Substring(1));
            }
        }
        
        // Stop strings 제거
        foreach (string stopStr in StopStrings)
        {
            text = text.Replace(stopStr, UNIQUE2);
        }
        
        // 분리된 문장들을 리스트에 담기
        string[] sentencesSplit = text.Split(new string[] { UNIQUE2 }, StringSplitOptions.RemoveEmptyEntries);
        
        List<string> sentences = new List<string>();
        string shortSentence = "";
        
        // 문장의 최소 길이가 15가 되게 합치기
        foreach (string sentence in sentencesSplit)
        {
            string trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;
            
            if ((shortSentence + " " + trimmed).Length < 15)
            {
                shortSentence = shortSentence + " " + trimmed;
            }
            else
            {
                sentences.Add((shortSentence + " " + trimmed).Trim());
                shortSentence = "";
            }
        }
        
        if (!string.IsNullOrEmpty(shortSentence.Trim()))
        {
            sentences.Add(shortSentence.Trim());
        }
        
        // 마지막 문장이 완성되어 있지 않으면 제거
        if (sentences.Count > 0)
        {
            string lastSentence = sentences[sentences.Count - 1];
            bool isEnded = false;
            
            if (lastSentence.Length >= 15 || sentences.Count == 1)
            {
                char[] endPuncs = { '.', '?', '!', '？', '。', '！', '…' };
                foreach (char punc in endPuncs)
                {
                    if (lastSentence.Length > 0 && lastSentence[lastSentence.Length - 1] == punc)
                    {
                        // 숫자 다음의 마침표가 아닌지 확인
                        if (lastSentence.Length < 2 || !char.IsDigit(lastSentence[lastSentence.Length - 2]))
                        {
                            isEnded = true;
                            break;
                        }
                    }
                }
            }
            
            if (!isEnded)
            {
                sentences.RemoveAt(sentences.Count - 1);
            }
        }
        
        return sentences;
    }
    
    // JObject 응답 생성 (기존 서버와 호환)
    private JObject CreateResponseJson(string answer, string language, string chatIdx)
    {
        // 번역 스텁 (현재는 같은 텍스트)
        string answerEn = answer;
        string answerKo = answer;
        string answerJp = answer;
        
        JObject response = new JObject
        {
            ["type"] = "reply",
            ["reply_list"] = new JArray
            {
                new JObject
                {
                    ["answer_en"] = answerEn,
                    ["answer_ko"] = answerKo,
                    ["answer_jp"] = answerJp
                }
            },
            ["ai_info"] = new JObject
            {
                ["server_type"] = "Google-Direct",
                ["model"] = modelName,
                ["lang_used"] = language,
                ["time"] = "0 sec"
            },
            ["chat_idx"] = chatIdx,
            ["ai_language_out"] = language
        };
        
        return response;
    }
}
