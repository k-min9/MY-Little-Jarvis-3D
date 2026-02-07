using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json.Linq;

// Gemini API 직접 호출 클라이언트
// Python 서버를 경유하지 않고 Unity에서 직접 Gemini API 호출
public class ApiGeminiDirectClient : MonoBehaviour
{
    // 싱글톤 인스턴스 (Lazy getter)
    public static ApiGeminiDirectClient instance;
    public static ApiGeminiDirectClient Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiGeminiDirectClient>();
            }
            return instance;
        }
    }

    [SerializeField] private string modelName = "gemma-3-27b-it";
    private string apiKey;

    // Stop strings (Python util_gemini.py + util_string 기본값 포함)
    // 길이가 긴 문자열부터 우선 매칭되도록 정렬된 배열을 함께 사용합니다.
    private static readonly string[] StopStrings = new string[]
    {
        "<|im_start|>assistant\n",
        "<|im_start|>assistant",
        "<|im_start|>user",
        "<start_of_turn>user",
        "<end_of_turn>",
        "<|im_end|>",
        "<|eot_id|>",
        "< |",
        "\nYou:",
        "You:",
        "\nAI:",
        "AI:"
    };

    private static readonly string[] StopStringsSorted = BuildStopStringsSorted();

    // 문장 분리 (Python util_string.get_punctuation_sentences 포팅 - 2024 최신 버전)
    // 파라미터: isPreserveNewline - 줄바꿈 보존 모드 (기본값 true)
    private static readonly string UNIQUE1 = "#@$";  // 숫자 마침표 보호용
    private static readonly string UNIQUE2 = "#@_";  // 문장 분리용
    private static readonly string UNIQUE3 = "#@^";  // 줄바꿈 보존용

    // 문장 완성 부호
    private static readonly char[] COMPLETE_PUNCS = { '.', '?', '!', '？', '。', '！', '…', '\n' };

    void Awake()
    {
        // 씬에 여러 개가 생겼을 때는 먼저 생성된 것을 사용합니다.
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
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
        Action<JObject> onChunkReceived,
        Action onComplete
    )
    {
        const int maxRetries = 2;
        int retryCount = 0;
        
        while (retryCount <= maxRetries)
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

                Debug.Log($"[GeminiDirect] Prompt length: {prompt.Length}" + (retryCount > 0 ? $" (retry {retryCount})" : ""));
                
                // 프롬프트 디버그 저장
                if (DebugManager.Instance != null)
                {
                    DebugManager.Instance.SavePromptDebug(prompt, charName, "geminidirect");
                }

                // Gemini API 호출
                await StreamGeminiAPI(prompt, aiLanguage, chatIdx, playerName, onChunkReceived);

                // 완료 콜백
                onComplete?.Invoke();
                return;  // 성공 시 종료
            }
            catch (Exception ex)
            {
                retryCount++;
                Debug.LogError($"[GeminiDirect] Error (attempt {retryCount}/{maxRetries + 1}): {ex.Message}");
                
                if (retryCount <= maxRetries)
                {
                    // 재시도 전 대기 (0.5초 고정)
                    Debug.Log($"[GeminiDirect] Retrying in 500ms with different key...");
                    await Task.Delay(500);
                }
                else
                {
                    // 최종 실패:  onComplete 호출하지 않음, 에러 말풍선 표시
                    Debug.LogError($"[GeminiDirect] All retries failed. Showing error balloon.");
                    EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "No", 2f);
                }
            }
        }
    }

    // Gemini API 스트리밍 호출 (SSE 방식)
    private async Task StreamGeminiAPI(
        string prompt,
        string language,
        string chatIdx,
        string playerName,
        Action<JObject> onChunkReceived
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
        request.Accept = "text/event-stream";
        request.Timeout = 120000;

        // Request body 전송
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonRequest);
        request.ContentLength = byteArray.Length;

        using (Stream dataStream = await request.GetRequestStreamAsync())
        {
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
        }

        // 응답 수신 및 SSE 스트리밍 처리
        StringBuilder responseLog = new StringBuilder();
        
        // 디버그 로그 축적용
        StringBuilder debugLog = new StringBuilder();
        debugLog.AppendLine("=== Gemini Direct Stream Debug Log ===");
        debugLog.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        debugLog.AppendLine($"Model: {modelName}");
        debugLog.AppendLine($"Chat Index: {chatIdx}");
        debugLog.AppendLine("========================================\n");

        string accumulatedText = "";
        List<string> sentencesAlreadySent = new List<string>();

        using (WebResponse response = await request.GetResponseAsync())
        using (Stream responseStream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(responseStream))
        {
            StringBuilder eventData = new StringBuilder();

            while (true)
            {
                string line = await reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

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
                            debugLog.AppendLine($"[Chunk Received] '{chunkText}'");

                            // stop string 확인 및 부분 매칭 제거
                            string cleanedText;
                            bool stopFound;
                            (cleanedText, stopFound) = ApplyStoppingStrings(accumulatedText);
                            
                            debugLog.AppendLine($"[Accumulated] '{accumulatedText}'");
                            debugLog.AppendLine($"[After StopString] '{cleanedText}', ShouldStop={stopFound}");

                            // 문장 분리 (줄바꿈 보존)
                            List<string> sentences = GetPunctuationSentences(cleanedText, true);
                            
                            debugLog.AppendLine($"[Sentences Split] Count={sentences.Count}, AlreadySent={sentencesAlreadySent.Count}");
                            for (int j = 0; j < sentences.Count; j++)
                            {
                                debugLog.AppendLine($"  [Sentence {j}] '{sentences[j]}'");
                            }

                            // 새로운 문장만 전송
                            for (int i = sentencesAlreadySent.Count; i < sentences.Count; i++)
                            {
                                string sentence = sentences[i];
                                debugLog.AppendLine($"[Before NameReplace {i}] '{sentence}'");

                                // 선생님 이름 적용
                                if (!string.IsNullOrEmpty(playerName))
                                {
                                    sentence = Regex.Replace(sentence, @"(<USER>|<user>|{{user}})", playerName);
                                }
                                else
                                {
                                    sentence = Regex.Replace(sentence, @"(<USER>|<user>|{{user}})", "You");
                                }
                                
                                debugLog.AppendLine($"[Before StopClean {i}] '{sentence}'");
                                
                                // Stop strings 잔여물 제거
                                sentence = sentence
                                    .Replace("<end_of_turn>", "")
                                    .Replace("</end_of_turn>", "")
                                    .Replace("<|im_end|>", "")
                                    .Replace("<|eot_id|>", "")
                                    .Replace("<start_of_turn>user", "")
                                    .Replace("<|im_start|>", "")
                                    .Trim();
                                
                                debugLog.AppendLine($"[After StopClean {i}] '{sentence}'");
                                
                                if (string.IsNullOrEmpty(sentence))
                                {
                                    debugLog.AppendLine($"[Filtered Empty {i}]");
                                    continue;
                                }

                                // JObject 생성 (기존 서버 형식과 호환)
                                JObject responseJson = CreateResponseJson(sentence, language, chatIdx);

                                // 콜백 호출
                                onChunkReceived?.Invoke(responseJson);
                                debugLog.AppendLine($"[Sent to Callback {i}] '{sentence}'");

                                sentencesAlreadySent.Add(sentence);
                            }

                            if (stopFound)
                            {
                                break;
                            }
                        }
                    }
                }
                else if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    // SSE data 라인
                    string dataLine = line.Substring("data:".Length).Trim();

                    // [DONE] 같은 종료 토큰
                    if (dataLine == "[DONE]")
                    {
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
        
        // Unity 추가 로직
        // 남은 텍스트 처리 (마지막 조각) - 미완성 문장도 포함
        debugLog.AppendLine($"\n[Final Processing] AccumulatedText: '{accumulatedText}'");
        debugLog.AppendLine($"[Final Processing] AlreadySent Count: {sentencesAlreadySent.Count}");
        
        if (!string.IsNullOrEmpty(accumulatedText))
        {
            string finalCleaned;
            (finalCleaned, _) = ApplyStoppingStrings(accumulatedText);
            debugLog.AppendLine($"[Final After StopString] '{finalCleaned}'");
            
            // 전체 텍스트를 다시 파싱 (미완성 문장 포함)
            List<string> finalSentences = GetPunctuationSentences(finalCleaned, true);
            debugLog.AppendLine($"[Final Sentences] Count={finalSentences.Count}");
            
            // 이미 보낸 개수를 넘는 문장만 전송
            for (int i = sentencesAlreadySent.Count; i < finalSentences.Count; i++)
            {
                string sentence = finalSentences[i];
                debugLog.AppendLine($"[Final Sentence {i}] '{sentence}'");
                
                // 선생님 이름 적용
                if (!string.IsNullOrEmpty(playerName))
                {
                    sentence = Regex.Replace(sentence, @"(<USER>|<user>|{{user}})", playerName);
                }
                else
                {
                    sentence = Regex.Replace(sentence, @"(<USER>|<user>|{{user}})", "You");
                }
                
                // Stop strings 잔여물 제거
                sentence = sentence
                    .Replace("<end_of_turn>", "")
                    .Replace("</end_of_turn>", "")
                    .Replace("<|im_end|>", "")
                    .Replace("<|eot_id|>", "")
                    .Replace("<start_of_turn>user", "")
                    .Replace("<|im_start|>", "")
                    .Trim();
                
                if (!string.IsNullOrEmpty(sentence))
                {
                    JObject responseJson = CreateResponseJson(sentence, language, chatIdx);
                    onChunkReceived?.Invoke(responseJson);
                    debugLog.AppendLine($"[Final Sent {i}] '{sentence}'");
                }
            }
        }

        // 응답 로그 저장
        SaveResponseLog(responseLog.ToString());
        
        // 디버그 로그 저장
        try
        {
            if (DebugManager.Instance != null)
            {
                string charName = "direct";
                DebugManager.Instance.SaveTextToFile(debugLog.ToString(), $"stream_{charName}", "geminidirect_debug");
                Debug.Log($"[GeminiDirect] Debug log saved for {charName}");
            }
            else
            {
                Debug.LogWarning("[GeminiDirect] DebugManager.Instance is null, cannot save debug log");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GeminiDirect] Failed to save debug log: {ex.Message}");
        }
    }

    // SSE JSON에서 텍스트 추출
    private bool TryExtractChunkTextFromJson(string json, out string text)
    {
        text = "";
        try
        {
            JObject obj = JObject.Parse(json);

            var cand = obj["candidates"]?[0];
            if (cand == null)
            {
                return false;
            }

            var parts = cand["content"]?["parts"] as JArray;
            if (parts == null || parts.Count == 0)
            {
                return false;
            }

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

    // Stop strings 제거 (완전 매칭 + 부분 매칭)
    private (string reply, bool stopFound) ApplyStoppingStrings(string reply)
    {
        if (reply == null)
        {
            return ("", false);
        }

        bool stopFound = false;

        // 완전 매칭: 가장 먼저 나타나는 stopString 위치를 찾습니다.
        int bestIdx = -1;

        foreach (string stopString in StopStringsSorted)
        {
            int idx = reply.IndexOf(stopString, StringComparison.Ordinal);
            if (idx == -1)
            {
                continue;
            }

            if (bestIdx == -1 || idx < bestIdx)
            {
                bestIdx = idx;
            }
        }

        if (bestIdx != -1)
        {
            reply = reply.Substring(0, bestIdx);
            stopFound = true;
        }

        // 부분 매칭 처리 (예: "\nYo"가 생성되었을 때 제거)
        if (!stopFound)
        {
            foreach (string stopString in StopStringsSorted)
            {
                int max = stopString.Length - 1;
                for (int j = max; j > 0; j--)
                {
                    if (reply.Length < j)
                    {
                        continue;
                    }

                    string tail = reply.Substring(reply.Length - j);
                    string head = stopString.Substring(0, j);

                    if (tail == head)
                    {
                        reply = reply.Substring(0, reply.Length - j);
                        break;
                    }
                }
            }
        }

        return (reply, stopFound);
    }

    // 문장 분리 (Python util_string.get_punctuation_sentences 포팅 - preserveNewline 최신 버전)
    private List<string> GetPunctuationSentences(string inputStrings, bool isPreserveNewline = true)
    {
        if (string.IsNullOrEmpty(inputStrings))
        {
            return new List<string>();
        }

        // 기본 체크: Windows 줄바꿈 정규화
        inputStrings = inputStrings.Replace("\r\n", "\n").Replace("\r", "\n");

        // 줄바꿈 종료 여부 기록 (정규화 이후에 체크)
        bool endedWithNewline = isPreserveNewline && inputStrings.EndsWith("\n", StringComparison.Ordinal);

        // <think></think> 제거
        inputStrings = RemoveThinkTag(inputStrings);

        // [캐릭터명]: 제거
        inputStrings = RemoveCharacterPrefix(inputStrings);

        // 강조표시 제거
        inputStrings = inputStrings.Replace("**", "");

        // * *과 안의 내용물 제거
        inputStrings = Regex.Replace(inputStrings, @"\s*\*.*?\*\s*", " ");

        // 숫자 마침표 항목을 다른 문자로 교체 (0. ~ 9.)
        for (int i = 0; i <= 9; i++)
        {
            string num = i + ".";
            inputStrings = inputStrings.Replace(num, $"{UNIQUE1}{i}{UNIQUE1}");
        }

        List<string> sentences = new List<string>();
        string inputString = inputStrings;

        // Stop strings 먼저 제거 (separators보다 먼저 처리해야 \nYou: 등이 올바르게 매칭됨)
        foreach (string stopStr in StopStringsSorted)
        {
            if (string.IsNullOrEmpty(stopStr))
            {
                continue;
            }
            inputString = inputString.Replace(stopStr, UNIQUE2);
        }

        if (isPreserveNewline)
        {
            // 줄바꿈 보존 모드: \n을 UNIQUE3로 마킹
            inputString = Regex.Replace(inputString, @"\n+", UNIQUE3);

            // 문장 분리 기준으로 교체 (줄바꿈 제외)
            string[] separators = { ". ", ".\\", "? ", "?\\", "! ", "!\\", "… ", "？", "。", "！" };
            foreach (string sep in separators)
            {
                if (sep.Length == 1)
                {
                    inputString = inputString.Replace(sep, sep + UNIQUE2);
                }
                else
                {
                    inputString = inputString.Replace(sep, sep[0] + UNIQUE2 + sep.Substring(1));
                }
            }

            // UNIQUE3를 UNIQUE2 + \n으로 변환 (분리 후 다음 문장 앞에 \n이 붙음)
            inputString = inputString.Replace(UNIQUE3, UNIQUE2 + "\n");
        }
        else
        {
            // 기존 모드: 줄바꿈도 분리자로 처리
            string[] separators = { ". ", ".\\", ".\n", "? ", "?\\", "?\n", "! ", "!\\", "!\n", "？", "。", "！", "\n" };
            foreach (string sep in separators)
            {
                if (sep.Length == 1)
                {
                    inputString = inputString.Replace(sep, sep + UNIQUE2);
                }
                else
                {
                    inputString = inputString.Replace(sep, sep[0] + UNIQUE2 + sep.Substring(1));
                }
            }
        }

        // 다시 숫자 마침표로 복구
        for (int i = 0; i <= 9; i++)
        {
            inputString = inputString.Replace($"{UNIQUE1}{i}{UNIQUE1}", i + ".");
        }

        // 분리된 문장들을 리스트에 담기
        string[] sentencesSplited = inputString.Split(new string[] { UNIQUE2 }, StringSplitOptions.None);

        // 불필요한 빈 문자열 제거
        List<string> sentencesList = new List<string>();
        foreach (string s in sentencesSplited)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                sentencesList.Add(s);
            }
        }

        // 문장의 최소길이가 15가 되게 합쳐주기 (단, 완성된 문장은 바로 추가)
        string shortSentence = "";

        foreach (string sentence in sentencesList)
        {
            // 줄바꿈 보존 모드일 때 줄바꿈 시작 여부 확인
            bool startsWithNewline = isPreserveNewline && sentence.StartsWith("\n", StringComparison.Ordinal);
            string sentenceStripped = sentence.Trim();

            if (string.IsNullOrEmpty(sentenceStripped))
            {
                continue;
            }

            // 줄바꿈으로 시작했으면 앞에 \n 붙여주기
            if (startsWithNewline)
            {
                sentenceStripped = "\n" + sentenceStripped;
            }

            // 문장이 완성되어 있으면 (. ? ! 등으로 끝나면) 바로 추가
            // 줄바꿈 보존 모드에서 \n으로 시작하면 이전 문장이 완성된 것으로 처리
            char lastChar = sentenceStripped[sentenceStripped.Length - 1];
            bool isComplete = Array.IndexOf(COMPLETE_PUNCS, lastChar) >= 0 || startsWithNewline;

            if (isComplete)
            {
                // 이전에 누적된 shortSentence가 있으면 먼저 추가
                if (!string.IsNullOrEmpty(shortSentence))
                {
                    sentences.Add(shortSentence);
                    shortSentence = "";
                }

                // 줄바꿈으로 시작하면 그대로, 아니면 앞에 공백 추가
                if (startsWithNewline)
                {
                    sentences.Add(sentenceStripped);
                }
                else
                {
                    sentences.Add(" " + sentenceStripped);
                }
            }
            else if ((shortSentence + sentenceStripped).Length < 15)
            {
                if (!string.IsNullOrEmpty(shortSentence))
                {
                    shortSentence = shortSentence + " " + sentenceStripped;
                }
                else
                {
                    shortSentence = sentenceStripped;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(shortSentence))
                {
                    sentences.Add(shortSentence + " " + sentenceStripped);
                }
                else
                {
                    sentences.Add(" " + sentenceStripped);
                }
                shortSentence = "";
            }
        }

        // 남은 shortSentence 처리
        if (!string.IsNullOrEmpty(shortSentence))
        {
            sentences.Add(shortSentence);
        }

        // 마지막 문장이 완성되어있지 않으면 제거
        bool isEnded = false;

        if (sentences.Count > 0)
        {
            string lastSentence = sentences[sentences.Count - 1];
            string trimmedLast = lastSentence.TrimEnd();
            
            if (trimmedLast.Length > 0)
            {
                char lastChar = trimmedLast[trimmedLast.Length - 1];
                bool endsWithComplete = Array.IndexOf(COMPLETE_PUNCS, lastChar) >= 0;

                if (endsWithComplete)
                {
                    // 마침표인 경우 숫자 다음이 아닌지 확인
                    if (lastChar != '.' || (trimmedLast.Length >= 2 && !char.IsDigit(trimmedLast[trimmedLast.Length - 2])))
                    {
                        isEnded = true;
                    }
                }
            }
        }

        // 입력이 \n으로 끝났으면 마지막 문장은 완료로 간주
        if (endedWithNewline)
        {
            isEnded = true;
        }

        if (sentences.Count > 0 && !isEnded)
        {
            sentences.RemoveAt(sentences.Count - 1);
        }

        return sentences;
    }

    // <think></think> 태그 제거
    private string RemoveThinkTag(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        string result = text;

        // <think>가 있고, </think>가 없을 경우: 아직 생각중이므로 반환텍스트 없음
        if (result.Contains("<think>") && !result.Contains("</think>"))
        {
            return "";
        }

        // </think>가 있을 경우
        int endIdx = result.IndexOf("</think>", StringComparison.Ordinal);
        if (endIdx >= 0)
        {
            result = result.Substring(endIdx + "</think>".Length);

            try
            {
                result = RemoveInvalidChars(result.Trim());
            }
            catch
            {
                // 파이썬과 동일하게 실패 시 무시
            }
        }

        return result.Trim();
    }

    // [캐릭터명]: 패턴 제거
    private string RemoveCharacterPrefix(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        string result = text.Trim();

        int idx = result.IndexOf("]:", StringComparison.Ordinal);
        if (idx >= 0)
        {
            result = result.Substring(idx + 2).Trim();
        }

        return result;
    }

    // 깨진 문자 및 제어 문자 제거
    private string RemoveInvalidChars(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        // 대표적인 깨진 문자 제거
        text = text.Replace("�", "");

        // 제어 문자 제거 (단, \n, \r, \t는 보존)
        text = Regex.Replace(text, @"[\x00-\x08\x0B-\x0C\x0E-\x1F\x7F]", "");

        return text.Trim();
    }

    // JSON 요청 저장
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
                    ["answer_jp"] = answerJp,
                    ["answer_origin"] = answer,
                    ["language"] = language
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

    // StopStrings를 길이 내림차순으로 정렬합니다.
    private static string[] BuildStopStringsSorted()
    {
        List<string> list = new List<string>();

        foreach (string s in StopStrings)
        {
            if (!string.IsNullOrEmpty(s))
            {
                list.Add(s);
            }
        }

        list.Sort((a, b) => b.Length.CompareTo(a.Length));
        return list.ToArray();
    }
}
