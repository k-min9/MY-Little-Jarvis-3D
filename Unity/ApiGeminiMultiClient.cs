using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ApiGeminiMulti;

// 다중 캐릭터 대화용 Gemini API 직접 호출 클라이언트
// Python util_gemini_multi.py 포팅
public class ApiGeminiMultiClient : MonoBehaviour
{
    // 싱글톤 인스턴스 (Lazy getter)
    public static ApiGeminiMultiClient instance;
    public static ApiGeminiMultiClient Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiGeminiMultiClient>();
            }
            return instance;
        }
    }

    [SerializeField] private string modelName = "gemma-3-27b-it";
    private string apiKey;
    private static readonly System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();  // HttpWebRequest보다 현대적이라는데 음...

    // Stop strings
    // 길이가 긴 문자열부터 우선 매칭되도록 정렬
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
        "AI:",
        "\n선생님:",
        "\n선생:",
        "\nSensei:",
        "\n先生:",
        "[선생님]:",
        "[Sensei]:",
        "[先生]:"
    };

    private static readonly string[] StopStringsSorted = BuildStopStringsSorted();

    // 문장 분리 (util_string.get_punctuation_sentences 포팅 - preserveNewline 지원)
    private static readonly string UNIQUE1 = "#@$";  // 숫자 마침표 보호용
    private static readonly string UNIQUE2 = "#@_";  // 문장 분리용
    private static readonly string UNIQUE3 = "#@^";  // 줄바꿈 보존용

    // 문장 완성 부호
    private static readonly char[] COMPLETE_PUNCS = { '.', '?', '!', '？', '。', '！', '…', '\n' };

    // Stop strings 정렬 (길이가 긴 문자열부터 우선 매칭)
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

    // API 설정
    private const float DEFAULT_TEMPERATURE = 0.7f;
    private const float DEFAULT_TOP_P = 0.9f;
    private const int DEFAULT_MAX_TOKENS = 1024;
    private const int MAX_RETRY_COUNT = 3;

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
    

        // HttpClient 타임아웃은 스트리밍 중 종료되면 안 되므로 무한으로 둡니다.
        httpClient.Timeout = Timeout.InfiniteTimeSpan;
}

    // 메인 스트리밍 호출 함수
    public Task<MultiConversationResult> CallGeminiMultiStreamDirect(
        MultiConversationRequest request,
        OnMultiChunkReceived onChunkReceived = null,
        OnMultiStreamComplete onComplete = null,
        OnMultiStreamError onError = null)
    {
        return CallGeminiMultiStreamDirect(request, CancellationToken.None, onChunkReceived, onComplete, onError);
    }

    // 메인 스트리밍 호출 함수 (취소 지원)
    public async Task<MultiConversationResult> CallGeminiMultiStreamDirect(
        MultiConversationRequest request,
        CancellationToken cancellationToken,
        OnMultiChunkReceived onChunkReceived = null,
        OnMultiStreamComplete onComplete = null,
        OnMultiStreamError onError = null)
    {
        MultiConversationResult result = new MultiConversationResult
        {
            sentences = new List<string>(),
            speaker = request.targetSpeaker,
            isSuccess = false
        };

        int retryCount = 0;
        while (retryCount < MAX_RETRY_COUNT)
        {
            try
            {
                // 취소 체크
                cancellationToken.ThrowIfCancellationRequested();

                // API 키 로드 (검증된 키 사용)
                apiKey = await ApiKei.GetValidatedGeminiKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = ApiKei.GetNextGeminiKey();
                }

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("유효한 API 키를 찾을 수 없습니다");
                }

                // 프롬프트 생성
                string prompt = ApiGeminiMultiPromptBuilder.BuildGemmaMultiPrompt(request);
                Debug.Log($"[ApiGeminiMultiClient] 프롬프트 생성 완료 (길이: {prompt.Length})");

                // API 호출
                await StreamGeminiAPI(apiKey, prompt, request, result, onChunkReceived, cancellationToken);

                // 성공
                result.isSuccess = true;
                onComplete?.Invoke(result);
                return result;
            }
            catch (OperationCanceledException)
            {
                result.errorMessage = "API 호출 취소됨";
                onError?.Invoke(result.errorMessage);
                return result;
            }
            catch (Exception ex)
            {
                retryCount++;
                Debug.LogWarning($"[ApiGeminiMultiClient] 시도 {retryCount} 실패: {ex.Message}");

                if (retryCount >= MAX_RETRY_COUNT)
                {
                    result.errorMessage = $"API 호출 실패 (재시도 {MAX_RETRY_COUNT}회 초과): {ex.Message}";
                    onError?.Invoke(result.errorMessage);
                    return result;
                }

                // 다음 키로 로테이션
                string nextKey = ApiKei.GetNextGeminiKey();
                Debug.Log($"[ApiGeminiMultiClient] API 키 로테이션: {(nextKey != null ? "성공" : "실패")}");

                await Task.Delay(1000);  // 잠시 대기 후 재시도
            }
        }

        return result;
    }




    // Gemini API 스트리밍 호출
    private async Task StreamGeminiAPI(
        string apiKey,
        string prompt,
        MultiConversationRequest request,
        MultiConversationResult result,
        OnMultiChunkReceived onChunkReceived,
        CancellationToken cancellationToken)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";

        // 요청 본문 생성
        JObject requestBody = new JObject
        {
            ["contents"] = new JArray
            {
                new JObject
                {
                    ["parts"] = new JArray
                    {
                        new JObject { ["text"] = prompt }
                    }
                }
            },
            ["generationConfig"] = new JObject
            {
                ["temperature"] = DEFAULT_TEMPERATURE,
                ["topP"] = DEFAULT_TOP_P,
                ["maxOutputTokens"] = DEFAULT_MAX_TOKENS,
                ["stopSequences"] = new JArray { "<end_of_turn>", "<|im_end|>" }
            }
        };

        string jsonBody = requestBody.ToString();

        // HttpWebRequest 생성 (DirectClient 방식)
        HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
        httpRequest.Method = "POST";
        httpRequest.ContentType = "application/json";
        httpRequest.Accept = "text/event-stream";
        httpRequest.Timeout = 120000;

        // Request body 전송
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonBody);
        httpRequest.ContentLength = byteArray.Length;

        using (Stream dataStream = await httpRequest.GetRequestStreamAsync())
        {
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
        }

        // 응답 수신 및 SSE 스트리밍 처리
        StringBuilder accumulator = new StringBuilder();
        int sentenceIndex = 0;

        using (WebResponse response = await httpRequest.GetResponseAsync())
        using (Stream responseStream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(responseStream))
        {
            StringBuilder eventData = new StringBuilder();

            while (true)
            {
                // 취소 체크
                cancellationToken.ThrowIfCancellationRequested();

                string line = await reader.ReadLineAsync();
                if (line == null) break;

                // SSE 이벤트 종료 (빈 줄)
                if (line.Length == 0)
                {
                    if (eventData.Length > 0)
                    {
                        string jsonData = eventData.ToString();
                        eventData.Clear();

                        // JSON 파싱
                        string chunkText = TryExtractChunkText(jsonData);
                        if (!string.IsNullOrEmpty(chunkText))
                        {
                            accumulator.Append(chunkText);
                            string accumulated = accumulator.ToString();

                            // Stop string 체크 및 적용
                            string processed = ApplyStopStrings(accumulated, out bool shouldStop);

                            // 문장 분리 및 콜백 (완성 문장만 방출)
                            List<string> sentences = GetPunctuationSentences(processed, false, false);
                            while (sentences.Count > result.sentences.Count)
                            {
                                int newIdx = result.sentences.Count;
                                string newSentence = sentences[newIdx];

                                // 후처리 적용
                                newSentence = PostProcessReply(newSentence, request.playerName);

                                if (!string.IsNullOrWhiteSpace(newSentence))
                                {
                                    result.sentences.Add(newSentence);
                                    onChunkReceived?.Invoke(newSentence, request.targetSpeaker, sentenceIndex);
                                    sentenceIndex++;
                                }
                            }

                            if (shouldStop) break;
                        }
                    }
                }
                else if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    // SSE data 라인
                    string dataLine = line.Substring(5).Trim();

                    // [DONE] 종료 토큰
                    if (dataLine == "[DONE]") break;

                    // 멀티라인 data 연결
                    if (eventData.Length > 0)
                    {
                        eventData.Append("\n");
                    }
                    eventData.Append(dataLine);
                }
            }

            // 남은 텍스트 처리 (마지막 조각)
            string remaining = accumulator.ToString();
            remaining = ApplyStopStrings(remaining, out _);
            remaining = PostProcessReply(remaining, request.playerName);

            List<string> finalSentences = GetPunctuationSentences(remaining, false, true);
            while (finalSentences.Count > result.sentences.Count)
            {
                int idx = result.sentences.Count;
                string sentence = finalSentences[idx];
                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    result.sentences.Add(sentence);
                    onChunkReceived?.Invoke(sentence, request.targetSpeaker, sentenceIndex);
                    sentenceIndex++;
                }
            }
        }
    }




    // JSON에서 텍스트 추출
    private string TryExtractChunkText(string jsonData)
    {
        try
        {
            JObject json = JObject.Parse(jsonData);
            var candidates = json["candidates"];
            if (candidates == null || !candidates.HasValues) return null;

            var firstCandidate = candidates[0];
            var content = firstCandidate["content"];
            if (content == null) return null;

            var parts = content["parts"];
            if (parts == null || !parts.HasValues) return null;

            var firstPart = parts[0];
            var text = firstPart["text"];
            return text?.ToString();
        }
        catch
        {
            return null;
        }
    }

    // Stop string 적용
    private string ApplyStopStrings(string text, out bool shouldStop)
    {
        (string reply, bool stopFound) = ApplyStoppingStrings(text);
        shouldStop = stopFound;
        return reply;
    }

    // stop string 적용 (부분 매칭 포함)
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

        // 부분 매칭 처리 (예: "\nYo" 같은 tail 제거)
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




    // 문장 분리 (util_string.get_punctuation_sentences 포팅)
    private List<string> GetPunctuationSentences(string inputStrings, bool isPreserveNewline = false, bool isAllowIncompleteLast = false)
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
            string[] separators = { ". ", ".\\", "? ", "?\\", "! ", "!\\", "？", "。", "！" };
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
            if (lastSentence.Length >= 15 || sentences.Count == 1)
            {
                string trimmedLast = lastSentence.TrimEnd();
                if (trimmedLast.Length > 0)
                {
                    char lastPunc = trimmedLast[trimmedLast.Length - 1];
                    bool endsWithComplete = Array.IndexOf(COMPLETE_PUNCS, lastPunc) >= 0;

                    if (endsWithComplete)
                    {
                        // 마침표인 경우 숫자 다음이 아닌지 확인
                        if (lastPunc != '.' || (trimmedLast.Length >= 2 && !char.IsDigit(trimmedLast[trimmedLast.Length - 2])))
                        {
                            isEnded = true;
                        }
                    }
                }
            }
        }

        // 입력이 \n으로 끝났으면 마지막 문장은 완료로 간주
        if (endedWithNewline)
        {
            isEnded = true;
        }

        if (sentences.Count > 0 && !isEnded && !isAllowIncompleteLast)
        {
            sentences.RemoveAt(sentences.Count - 1);
        }

        return sentences;
    }




    // 응답 후처리 (util_gemini_multi.py:post_process_multi_reply 포팅)
    private string PostProcessReply(string reply, string playerName)
    {
        if (string.IsNullOrEmpty(reply)) return reply;

        // 플레이어 이름 치환
        if (!string.IsNullOrEmpty(playerName))
        {
            reply = Regex.Replace(reply, @"(<USER>|<user>|\{\{user\}\})", playerName);
        }
        else
        {
            reply = Regex.Replace(reply, @"(<USER>|<user>|\{\{user\}\})", "You");
        }

        // 줄바꿈 제거
        reply = reply.Replace("\n", "");

        // 괄호 내용 제거
        reply = Regex.Replace(reply, @"\([^)]*\)", "");  // ()와 안의 내용물 제거
        reply = Regex.Replace(reply, @"\[[^\]]*\]", "");  // []와 안의 내용물 제거
        reply = Regex.Replace(reply, @"\*[^*]*\*", "");   // **과 안의 내용물 제거

        // think 태그 제거
        reply = RemoveThinkTag(reply);

        return reply.Trim();
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
                // 실패 시 무시
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

    // 유틸리티: JSON 응답 형식으로 변환 (기존 시스템 호환용)
    public string CreateJsonResponse(string type, string speaker, string reply, string replyKo = null, string replyJp = null, string replyEn = null)
    {
        var responseObj = new Dictionary<string, object>
        {
            { "type", type },
            { "speaker", speaker },
            { "reply", reply }
        };

        if (!string.IsNullOrEmpty(replyKo)) responseObj["reply_ko"] = replyKo;
        if (!string.IsNullOrEmpty(replyJp)) responseObj["reply_jp"] = replyJp;
        if (!string.IsNullOrEmpty(replyEn)) responseObj["reply_en"] = replyEn;

        return JsonConvert.SerializeObject(responseObj);
    }

    // ============================================================================
    // Flow Director 함수들 (Gemini API 직접 호출)
    // Python multi_gemini.py 포팅
    // ============================================================================

    // 사용자 메시지를 분석하여 누구에게 말하고 있는지 판단 (Gemini API)
    public async Task<(string targetSpeaker, string reason)> AnalyzeTargetSpeaker(
        string message,
        string currentSpeaker = "sensei",
        string lang = "ko",
        List<Dictionary<string, string>> memoryList = null)
    {
        Debug.Log($"[Flow Director] AnalyzeTargetSpeaker 시작: '{(message.Length > 30 ? message.Substring(0, 30) + "..." : message)}' ({lang})");

        try
        {
            // API 키 로드
            string key = await ApiKei.GetValidatedGeminiKey();
            if (string.IsNullOrEmpty(key))
            {
                key = ApiKei.GetNextGeminiKey();
            }

            if (string.IsNullOrEmpty(key))
            {
                return ("arona", "API 키 없음 - 기본값 선택");
            }

            // 프롬프트 생성
            string promptBody = BuildTargetSpeakerPrompt(message, memoryList, lang);
            string fullPrompt = $"<bos><start_of_turn>user\n{promptBody}<end_of_turn>\n<start_of_turn>model\n";

            // Gemini API 호출
            string output = await CallGeminiSimple(key, fullPrompt, 50, 0.1f);

            // 파싱
            var (target, reason) = ParseTargetSpeakerResponse(output);

            // 기본값 처리
            if (string.IsNullOrEmpty(target) || (target != "arona" && target != "plana"))
            {
                target = "arona";
                reason = $"잘못된 응답으로 기본 선택 - {reason}";
            }

            Debug.Log($"[Flow Director] AnalyzeTargetSpeaker 완료: {target} - {reason}");
            return (target, $"Gemini 분석: {reason}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Flow Director] AnalyzeTargetSpeaker 오류: {ex.Message}");
            return ("arona", "API 오류 - 기본값 선택");
        }
    }

    // 대화 흐름을 분석하여 다음 발화자를 결정 (Gemini API)
    public async Task<(string nextSpeaker, string reason)> DecideNextSpeaker(
        List<Dictionary<string, string>> memoryList = null,
        string query = "",
        string finalResponse = "",
        string currentSpeaker = null,
        string querySpeaker = null,
        string lang = "ko",
        int maxAiConsecutive = 10)
    {
        Debug.Log($"[Flow Director] DecideNextSpeaker 시작: 메모리 {memoryList?.Count ?? 0}턴 ({lang})");

        try
        {
            // API 키 로드
            string key = await ApiKei.GetValidatedGeminiKey();
            if (string.IsNullOrEmpty(key))
            {
                key = ApiKei.GetNextGeminiKey();
            }

            if (string.IsNullOrEmpty(key))
            {
                return ("sensei", "API 키 없음 - 선생님께 턴 넘김");
            }

            // 프롬프트 생성
            string promptBody = BuildFlowDecisionPrompt(memoryList, query, finalResponse, currentSpeaker, querySpeaker, lang);
            string fullPrompt = $"<bos><start_of_turn>user\n{promptBody}<end_of_turn>\n<start_of_turn>model\nnext_speaker: ";

            // Gemini API 호출
            string output = await CallGeminiSimple(key, fullPrompt, 50, 0.1f);
            output = "next_speaker: " + output;

            // 파싱
            var (nextSpeaker, reason) = ParseFlowDecisionResponse(output);

            // 동일 발화자 방지
            if (nextSpeaker == currentSpeaker)
            {
                string original = nextSpeaker;
                nextSpeaker = "sensei";
                reason = $"동일 발화자 방지: {original} → sensei 자동 변경";
                Debug.Log($"[Flow Director] 동일 발화자 감지! '{original}' → 'sensei'로 변경");
            }

            // AI 연속 대화 방지
            if (nextSpeaker != "sensei" && memoryList != null && memoryList.Count >= maxAiConsecutive)
            {
                bool currentIsUser = querySpeaker == "sensei";

                if (!currentIsUser)
                {
                    int startIdx = Math.Max(0, memoryList.Count - maxAiConsecutive);
                    bool allNonUser = true;
                    for (int i = startIdx; i < memoryList.Count; i++)
                    {
                        if (memoryList[i].ContainsKey("role") && memoryList[i]["role"] == "user")
                        {
                            allNonUser = false;
                            break;
                        }
                    }

                    if (allNonUser)
                    {
                        string original = nextSpeaker;
                        nextSpeaker = "sensei";
                        reason = $"AI 연속 방지: {original} → sensei 강제 변경";
                        Debug.Log($"[Flow Director] AI 연속 감지! '{original}' → 'sensei'로 변경");
                    }
                }
            }

            Debug.Log($"[Flow Director] DecideNextSpeaker 완료: {nextSpeaker}");
            return (nextSpeaker, reason);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Flow Director] DecideNextSpeaker 오류: {ex.Message}");
            return ("sensei", "API 오류 - 선생님께 턴 넘김");
        }
    }

    // 메시지 분석을 통해 target_speaker가 누구에게 응답해야 하는지 결정 (Gemini API)
    public async Task<(string targetListener, string reason)> AnalyzeTargetListener(
        string message,
        string currentSpeaker = "sensei",
        string targetSpeaker = null,
        string lang = "ko",
        List<Dictionary<string, string>> memoryList = null)
    {
        Debug.Log($"[Flow Director] AnalyzeTargetListener 시작: {targetSpeaker} 응답 대상 분석 ({lang})");

        try
        {
            // API 키 로드
            string key = await ApiKei.GetValidatedGeminiKey();
            if (string.IsNullOrEmpty(key))
            {
                key = ApiKei.GetNextGeminiKey();
            }

            if (string.IsNullOrEmpty(key))
            {
                return ("all", "API 키 없음");
            }

            // 프롬프트 생성
            string promptBody = BuildTargetListenerPrompt(message, currentSpeaker, targetSpeaker, memoryList, lang);
            string fullPrompt = $"<bos><start_of_turn>user\n{promptBody}<end_of_turn>\n<start_of_turn>model\n";

            // Gemini API 호출
            string output = await CallGeminiSimple(key, fullPrompt, 50, 0.1f);

            // 파싱
            var (targetListener, reason) = ParseTargetListenerResponse(output);

            // 유효성 검증
            string[] validListeners = { "sensei", "arona", "plana", "all" };
            if (Array.IndexOf(validListeners, targetListener) < 0)
            {
                targetListener = "all";
                reason = $"잘못된 응답으로 기본 선택 - {reason}";
            }

            Debug.Log($"[Flow Director] AnalyzeTargetListener 완료: {targetListener} - {reason}");
            return (targetListener, $"Gemini 분석: {reason}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Flow Director] AnalyzeTargetListener 오류: {ex.Message}");
            return ("all", "API 오류");
        }
    }

    // 대화 맥락에서 청취자 결정 (규칙 기반 - API 미사용)
    public (string targetListener, string reason) DetermineTargetListenerFromContext(
        string currentSpeaker,
        string targetSpeaker,
        string message = "",
        List<Dictionary<string, string>> memoryList = null,
        string lang = "ko")
    {
        Debug.Log($"[Flow Director] Context Listener: {currentSpeaker} -> {targetSpeaker}");

        if (currentSpeaker == "sensei")
        {
            if (targetSpeaker == "arona" || targetSpeaker == "plana")
            {
                return (targetSpeaker, $"선생님 -> {targetSpeaker} 개별 대화");
            }
            else
            {
                return ("all", "선생님의 전체 발언");
            }
        }
        else if (currentSpeaker == "arona" || currentSpeaker == "plana")
        {
            if (targetSpeaker == "sensei")
            {
                return ("sensei", $"{currentSpeaker} -> 선생님 개별 응답");
            }
            else if ((targetSpeaker == "arona" || targetSpeaker == "plana") && targetSpeaker != currentSpeaker)
            {
                return (targetSpeaker, $"{currentSpeaker} -> {targetSpeaker} AI끼리 대화");
            }
            else
            {
                return ("all", $"{currentSpeaker}의 전체 발언");
            }
        }

        return ("all", "맥락 불분명 - 전체 대화로 설정");
    }

    // ============================================================================
    // Flow Director 프롬프트 빌더들
    // ============================================================================

    private string BuildTargetSpeakerPrompt(string message, List<Dictionary<string, string>> memoryList, string lang)
    {
        string memoryContext = BuildMemoryContext(memoryList, lang);

        if (lang == "ko")
        {
            return $@"사용자 메시지를 보고 누구에게 말하고 있는지 빠르게 판단하세요.

판단 기준:
- 특정 이름 호출: ""아로나"", ""프라나"" 등
- 특정 캐릭터 언급: ""선배"", ""후배"", ""프라나쨩"" 등  
- 성격 기반 요청: 활발한 것 → 아로나, 차분한 것 → 프라나
- 과거 대화 맥락: 최근에 누구와 대화했는지, 대화 흐름 고려
- 명확하지 않으면: arona (기본 선택)

{memoryContext}

현재 메시지: ""{message}""

과거 대화 맥락과 현재 메시지를 종합하여, 사용자가 누구에게 말하고 있나요?
target_speaker: [arona/plana]
reason: [짧은 이유]
/no_think";
        }
        else if (lang == "ja" || lang == "jp")
        {
            return $@"ユーザーメッセージを見て誰に話しかけているか素早く判断してください。

判断基準:
- 特定の名前呼び出し: ""アロナ"", ""プラナ""など
- 特定キャラクター言及: ""先輩"", ""後輩"", ""プラナちゃん""など
- 性格ベース依頼: 活発なもの → アロナ、落ち着いたもの → プラナ
- 過去の会話文脈: 最近誰と話していたか
- 明確でなければ: arona (基本選択)

{memoryContext}

現在のメッセージ: ""{message}""

target_speaker: [arona/plana]
reason: [短い理由]
/no_think";
        }
        else
        {
            return $@"Analyze the user message to determine who they are addressing.

Judgment criteria:
- Specific name calls: ""Arona"", ""Plana"", etc.
- Character references: ""senior"", ""junior"", etc.
- Personality-based requests: energetic → Arona, calm → Plana
- Past conversation context
- If unclear: arona (default choice)

{memoryContext}

Current message: ""{message}""

target_speaker: [arona/plana]
reason: [brief reason]
/no_think";
        }
    }

    private string BuildFlowDecisionPrompt(
        List<Dictionary<string, string>> memoryList,
        string query,
        string finalResponse,
        string currentSpeaker,
        string querySpeaker,
        string lang)
    {
        // 대화 히스토리 구성
        StringBuilder conversationHistory = new StringBuilder();

        if (memoryList != null)
        {
            int startIdx = Math.Max(0, memoryList.Count - 4);
            for (int i = startIdx; i < memoryList.Count; i++)
            {
                var entry = memoryList[i];
                string speaker = entry.ContainsKey("speaker") ? entry["speaker"] : "unknown";
                string msg = SelectMessageByLang(entry, lang);
                if (!string.IsNullOrEmpty(msg))
                {
                    conversationHistory.AppendLine($"{speaker}: {msg}");
                }
            }
        }

        if (!string.IsNullOrEmpty(query) && querySpeaker != "arona" && querySpeaker != "plana")
        {
            conversationHistory.AppendLine($"{querySpeaker}: {query}");
        }

        if (!string.IsNullOrEmpty(finalResponse) && !string.IsNullOrEmpty(currentSpeaker))
        {
            conversationHistory.AppendLine($"{currentSpeaker}: {finalResponse}");
        }

        string historyStr = conversationHistory.ToString().Trim();
        if (string.IsNullOrEmpty(historyStr))
        {
            historyStr = lang == "ko" ? "(대화 시작)" : "(conversation start)";
        }

        if (lang == "ko")
        {
            return $@"3명이 참여하는 대화에서 다음에 말할 사람을 자연스럽게 결정해주세요.

참여자:
- sensei (선생님): 사용자
- arona (아로나): 활발하고 적극적인 AI
- plana (프라나): 차분하고 신중한 AI

최근 대화:
{historyStr}

위 대화 흐름과 문맥을 고려하여, 누가 다음에 말하는 것이 가장 자연스러울지 결정해주세요.
(방금 말한 {currentSpeaker}는 제외)

결과 형식:
next_speaker: [arona/plana/sensei]
reason: [간단한 이유]
/no_think";
        }
        else if (lang == "ja" || lang == "jp")
        {
            return $@"3名で行う対話で次に話す人を自然に決めてください。

参加者:
- sensei (先生): ユーザー
- arona (アロナ): 活発で積極的なAI
- plana (プラナ): 落ち着いて慎重なAI

最近の対話:
{historyStr}

次に誰が話すのが最も自然か決めてください。
(直前に話した{currentSpeaker}は除外)

結果形式:
next_speaker: [arona/plana/sensei]
reason: [簡単な理由]
/no_think";
        }
        else
        {
            return $@"Decide who should speak next in a 3-person conversation naturally.

Participants:
- sensei (Sensei): User
- arona (Arona): Energetic and active AI
- plana (Plana): Calm and cautious AI

Recent conversation:
{historyStr}

Decide who should speak next most naturally.
(Exclude {currentSpeaker} who just spoke)

Result format:
next_speaker: [arona/plana/sensei]
reason: [brief reason]
/no_think";
        }
    }

    private string BuildTargetListenerPrompt(
        string message,
        string currentSpeaker,
        string targetSpeaker,
        List<Dictionary<string, string>> memoryList,
        string lang)
    {
        string memoryContext = BuildMemoryContext(memoryList, lang);

        if (lang == "ko")
        {
            return $@"대화 상황을 분석하여 {targetSpeaker}가 응답할 때 누구에게 말해야 하는지 판단하세요.

상황 분석:
- {currentSpeaker}가 메시지를 말했습니다
- {targetSpeaker}가 응답할 예정입니다

판단 기준:
- 개별 대화: {currentSpeaker}가 {targetSpeaker}에게 직접 말했다면 → {currentSpeaker}에게 응답
- 간접 질문: ""{targetSpeaker}야, 프라나는 어떻게 생각해?"" → 프라나에게 질문하도록 유도
- 전체 질문: 모든 사람이 들어도 되는 일반적 내용 → all (전체)
- 불분명한 경우: all (전체) 선택

{memoryContext}

현재 상황:
- 발화자: {currentSpeaker}
- 응답자: {targetSpeaker}
- 메시지: ""{message}""

{targetSpeaker}가 응답할 때 누구에게 말해야 하나요?
target_listener: [sensei/arona/plana/all]
reason: [짧은 이유]
/no_think";
        }
        else if (lang == "ja" || lang == "jp")
        {
            return $@"会話状況を分析して{targetSpeaker}が応答する時に誰に話すべきかを判断してください。

{memoryContext}

現在の状況:
- 発話者: {currentSpeaker}
- 応答者: {targetSpeaker}
- メッセージ: ""{message}""

target_listener: [sensei/arona/plana/all]
reason: [短い理由]
/no_think";
        }
        else
        {
            return $@"Analyze the conversation situation to determine who {targetSpeaker} should address.

{memoryContext}

Current Situation:
- Speaker: {currentSpeaker}
- Responder: {targetSpeaker}
- Message: ""{message}""

target_listener: [sensei/arona/plana/all]
reason: [brief reason]
/no_think";
        }
    }

    private string BuildMemoryContext(List<Dictionary<string, string>> memoryList, string lang)
    {
        if (memoryList == null || memoryList.Count == 0)
        {
            if (lang == "ko") return "(과거 대화 없음)";
            if (lang == "ja" || lang == "jp") return "(過去の会話なし)";
            return "(no past conversation)";
        }

        int startIdx = Math.Max(0, memoryList.Count - 5);
        List<string> memoryLines = new List<string>();

        for (int i = startIdx; i < memoryList.Count; i++)
        {
            var entry = memoryList[i];
            string speaker = entry.ContainsKey("speaker") ? entry["speaker"] : "unknown";
            string msg = SelectMessageByLang(entry, lang);

            if (string.IsNullOrEmpty(msg)) continue;

            if (entry.ContainsKey("role") && entry["role"] == "user")
            {
                string speakerName = lang == "ko" ? "선생님" : (lang == "ja" || lang == "jp" ? "先生" : "Sensei");
                memoryLines.Add($"{speakerName}: {msg}");
            }
            else
            {
                string charName = entry.ContainsKey("character_name") ? entry["character_name"] : speaker;
                memoryLines.Add($"{charName}: {msg}");
            }
        }

        if (memoryLines.Count > 0)
        {
            string header = lang == "ko" ? "최근 대화:" : (lang == "ja" || lang == "jp" ? "最近の会話:" : "Recent conversation:");
            return header + "\n" + string.Join("\n", memoryLines);
        }

        return "";
    }

    private string SelectMessageByLang(Dictionary<string, string> entry, string lang)
    {
        if (lang == "ko")
        {
            return entry.ContainsKey("messageKo") ? entry["messageKo"]
                : entry.ContainsKey("message") ? entry["message"] : "";
        }
        else if (lang == "ja" || lang == "jp")
        {
            return entry.ContainsKey("messageJp") ? entry["messageJp"]
                : entry.ContainsKey("message") ? entry["message"] : "";
        }
        else
        {
            return entry.ContainsKey("messageEn") ? entry["messageEn"]
                : entry.ContainsKey("message") ? entry["message"] : "";
        }
    }

    // ============================================================================
    // Flow Director 응답 파서들
    // ============================================================================

    private (string target, string reason) ParseTargetSpeakerResponse(string response)
    {
        string target = null;
        string reason = "AI 분석";

        if (string.IsNullOrEmpty(response)) return (target, reason);

        string[] lines = response.Split('\n');
        foreach (string line in lines)
        {
            if (line.Contains("target_speaker:"))
            {
                string val = line.Split(new[] { "target_speaker:" }, StringSplitOptions.None)[1].Trim().ToLower();
                val = val.Replace("[", "").Replace("]", "");
                if (val == "arona" || val == "plana")
                {
                    target = val;
                }
            }
            else if (line.Contains("reason:"))
            {
                reason = line.Split(new[] { "reason:" }, StringSplitOptions.None)[1].Trim();
            }
        }

        return (target, reason);
    }

    private (string nextSpeaker, string reason) ParseFlowDecisionResponse(string response)
    {
        string nextSpeaker = "sensei";
        string reason = "AI 모델 결정";

        if (string.IsNullOrEmpty(response)) return (nextSpeaker, reason);

        string[] lines = response.Split('\n');
        foreach (string line in lines)
        {
            if (line.Contains("next_speaker:"))
            {
                string val = line.Split(new[] { "next_speaker:" }, StringSplitOptions.None)[1].Trim().ToLower();
                val = val.Replace("[", "").Replace("]", "");
                if (val == "arona" || val == "plana" || val == "sensei")
                {
                    nextSpeaker = val;
                }
            }
            else if (line.Contains("reason:"))
            {
                reason = line.Split(new[] { "reason:" }, StringSplitOptions.None)[1].Trim();
            }
        }

        return (nextSpeaker, reason);
    }

    private (string targetListener, string reason) ParseTargetListenerResponse(string response)
    {
        string targetListener = "all";
        string reason = "AI 분석";

        if (string.IsNullOrEmpty(response)) return (targetListener, reason);

        string[] lines = response.Split('\n');
        foreach (string line in lines)
        {
            if (line.Contains("target_listener:"))
            {
                string val = line.Split(new[] { "target_listener:" }, StringSplitOptions.None)[1].Trim().ToLower();
                val = val.Replace("[", "").Replace("]", "");
                if (val == "sensei" || val == "arona" || val == "plana" || val == "all")
                {
                    targetListener = val;
                }
            }
            else if (line.Contains("reason:"))
            {
                reason = line.Split(new[] { "reason:" }, StringSplitOptions.None)[1].Trim();
            }
        }

        return (targetListener, reason);
    }

    // ============================================================================
    // 간단한 Gemini API 호출 (Flow Director용)
    // ============================================================================

    private async Task<string> CallGeminiSimple(string apiKey, string prompt, int maxTokens = 50, float temperature = 0.1f)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = temperature,
                maxOutputTokens = maxTokens
            }
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);

        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
        {
            requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using (var response = await httpClient.SendAsync(requestMessage))
            {
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API 응답 오류 ({response.StatusCode}): {errorContent}");
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                return TryExtractChunkText(responseContent) ?? "";
            }
        }
    }
}
