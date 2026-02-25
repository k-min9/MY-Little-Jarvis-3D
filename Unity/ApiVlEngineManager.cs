using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;  // UnityWebRequest, UnityWebRequestMultimedia용
using UnityEngine;
using UnityEngine.UI;

public class ApiVlEngineManager : MonoBehaviour
{
    public const string ScenarioBASkip = "BASkip";
    public const string ScenarioBAReader = "BAReader";

    private static ApiVlEngineManager instance;  // 싱글톤 인스턴스
    public static ApiVlEngineManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiVlEngineManager>();
            }
            return instance;
        }
    }

    [Header("Click Effect")]
    [SerializeField] private ParticleSystem fx_click;  // 클릭 이펙트

    [Header("VL Engine Config")]
    [SerializeField] private string defaultScenarioName = ScenarioBASkip;
    [SerializeField] private string engineFormLang = "ja";
    [SerializeField] private string engineFormWavFileName = "vl_engine_form_response.wav";

    private bool isCanceled = false;  // 취소 요청 플래그

    #region 공개 API

    // VL Engine 실행 (단일 요청-응답)
    public void ExecuteVlEngine(
        string query = "",
        JArray previousThinkLog = null,
        JToken previousAgentState = null,
        Action<JObject> onEvent = null,
        Action<JObject> onComplete = null,
        int retryCount = 0,
        int maxRetry = 5,
        bool? verbose = null,  // null이면 DevManager에서 자동 결정
        string scenarioName = ScenarioBASkip
    )
    {
        // verbose: 명시 전달 시 그 값 사용, null이면 DevManager 상태로 결정
        bool verboseValue = verbose ?? DevManager.Instance.IsDevModeEnabled();
        
        isCanceled = false;
        StartCoroutine(ExecuteVlEngineCoroutine(
            query, previousThinkLog, previousAgentState,
            onEvent, onComplete, retryCount, maxRetry, verboseValue, scenarioName
        ));
    }

    // 작업 취소 요청
    public void CancelExecution()
    {
        isCanceled = true;
        Debug.Log("[VlEngine] 취소 요청됨");
    }

    #endregion

    #region 메인 실행 코루틴

    // VL Engine 실행 코루틴: 캡처 → API 호출 → 완료 (또는 재요청)
    private IEnumerator ExecuteVlEngineCoroutine(
        string query,
        JArray previousThinkLog,
        JToken previousAgentState,
        Action<JObject> onEvent,
        Action<JObject> onComplete,
        int retryCount,
        int maxRetry,
        bool verbose,
        string scenarioName
    )
    {
        byte[] imageBytes = null;
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        // 1. 화면 캡처
        yield return CaptureScreenToMemoryWithOffset(
            (bytes, x, y) =>
            {
                imageBytes = bytes;
                captureOffsetX = x;
                captureOffsetY = y;
            },
            (failMsg) =>
            {
                Debug.LogError($"[VlEngine] {failMsg}");
                onComplete?.Invoke(null);
            },
            "[VlEngine]"
        );

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("[VlEngine] 화면 캡처 실패");
            onComplete?.Invoke(null);
            yield break;
        }

        Debug.Log($"[VlEngine] 캡처 완료: {imageBytes.Length} bytes, offset=({captureOffsetX}, {captureOffsetY})");

        // 2. API 호출 (스트리밍)
        JObject lastEventData = null;
        yield return CallEngineStreamApi(
            query, imageBytes, previousThinkLog, previousAgentState,
            retryCount, maxRetry, verbose,
            scenarioName,
            onEvent,
            (lastEvent) => { lastEventData = lastEvent; }
        );

        if (lastEventData == null)
        {
            Debug.LogError("[VlEngine] 응답 없음");
            onComplete?.Invoke(null);
            yield break;
        }

        // 3. 응답 종류에 따른 처리
        string kind = lastEventData["kind"]?.Value<string>() ?? "";

        // done 또는 fail이면 종료
        if (kind == "done" || kind == "fail")
        {
            string endMessage = $"[VlEngine] 작업 {kind} - 종료";
            Debug.Log(endMessage);
            ProcessVlMessage(endMessage);
            
            // 말풍선 제거
            NoticeManager.Instance.DeleteNoticeBalloonInstance();

            // done일 경우 yes 풍선 보여주기.
            if (kind == "done")
            {
                EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "Yes", 2f);
                StartCoroutine(ScenarioCommonManager.Instance.Run_C99_TaskDone());
            }

            // fail일 경우 no 풍선 보여주기.
            if (kind == "fail")
            {
                EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "No", 2f);
                StartCoroutine(ScenarioCommonManager.Instance.Run_C99_Alert_from_planner());
            }
            
            onComplete?.Invoke(lastEventData);
            yield break;
        }

        // act, observe, wait 이벤트는 재요청
        if (kind == "act" || kind == "observe" || kind == "wait")
        {
            var data = lastEventData["data"] as JObject;
            if (data == null)
            {
                Debug.LogError("[VlEngine] data 필드 없음");
                onComplete?.Invoke(lastEventData);
                yield break;
            }

            // think_log, agent_state 추출
            JArray thinkLog = lastEventData["think_log"] as JArray;
            JToken agentState = data["agent_state"];

            // retry_interval, expected_state 추출
            float retryInterval = 2.0f;
            string expectedStateDisplay = "";
            
            if (agentState != null && agentState is JObject agentStateObj)
            {
                retryInterval = agentStateObj["retry_interval"]?.Value<float>() ?? 2.0f;
                
                // expected_state 파싱 (문자열 또는 배열)
                var expectedStateToken = agentStateObj["expected_state"];
                if (expectedStateToken != null)
                {
                    if (expectedStateToken is JArray expectedList)
                    {
                        // 리스트인 경우: ["S4", "S5"] → "S4 또는 S5"
                        var states = new List<string>();
                        foreach (var token in expectedList)
                        {
                            states.Add(token.ToString());
                        }
                        expectedStateDisplay = string.Join(" 또는 ", states);
                    }
                    else if (expectedStateToken.Type != JTokenType.Null)
                    {
                        // 단일 문자열인 경우: "S4"
                        expectedStateDisplay = expectedStateToken.ToString();
                    }
                }
            }
            // wait/observe 이벤트의 경우 data에도 retry_interval이 있을 수 있음
            else if (data["retry_interval"] != null)
            {
                retryInterval = data["retry_interval"].Value<float>();
            }

            if (kind == "act")
            {
                string action = data["action"]?.Value<string>() ?? "";

                if (action == "request_form")
                {
                    // 🆕 새로운 action type: engine_form 호출 → 음성 재생 → 클릭
                    int? x = data["x"]?.Value<int>();
                    int? y = data["y"]?.Value<int>();
                    
                    if (x.HasValue && y.HasValue)
                    {
                        string voiceActor = data["voice_actor"]?.Value<string>() ?? "";
                        string voiceTxt = data["voice_txt"]?.Value<string>() ?? "";
                        
                        // Actor 매핑: OCR 이름 → 캐릭터 ID
                        string mappedActor = MapVoiceActor(voiceActor);
                        
                        // TTS 요청 + 재생 대기 + 클릭 (한 번에 처리)
                        yield return RequestEngineFormVoiceCoroutineWithClick(
                            mappedActor, voiceTxt, 
                            x.Value, y.Value, 
                            captureOffsetX, captureOffsetY,
                            verbose,
                            agentState  // 상위 스코프의 agentState 사용
                        );
                    }
                }
                else if (action == "click")
                {
                    // 기존 방식: click + request_voice 플래그 (하위 호환성)
                    int? x = data["x"]?.Value<int>();
                    int? y = data["y"]?.Value<int>();
                    bool requestVoice = data["request_voice"]?.Value<bool>() ?? false;

                    if (x.HasValue && y.HasValue)
                    {
                        if (requestVoice)
                        {
                            string voiceActor = data["voice_actor"]?.Value<string>() ?? "";
                            string voiceTxt = data["voice_txt"]?.Value<string>() ?? "";

                            // Actor 매핑: OCR 이름 → 캐릭터 ID
                            string mappedActor = MapVoiceActor(voiceActor);
                            
                            // TTS 요청 + 재생 대기 + 클릭 (한 번에 처리)
                            yield return RequestEngineFormVoiceCoroutineWithClick(
                                mappedActor, voiceTxt, 
                                x.Value, y.Value, 
                                captureOffsetX, captureOffsetY,
                                verbose,
                                agentState  // 상위 스코프의 agentState 사용
                            );
                        }
                        else
                        {
                            ExecuteClickFromRelative(x.Value, y.Value, captureOffsetX, captureOffsetY, true, "[VlEngine]");
                        }
                    }
                }
                else if (action == "alert")
                {
                    Debug.Log("[VlEngine] alert 액션 - 알림 효과음 재생 (TODO)");
                }
            }

            string stateInfo = string.IsNullOrEmpty(expectedStateDisplay) ? "" : $" (expected: {expectedStateDisplay})";
            string waitMessage = $"[VlEngine] {kind} 수신{stateInfo} - {retryInterval}초 후 재요청";
            Debug.Log(waitMessage);
            ProcessVlMessage(waitMessage);

            // retry_interval 대기 후 재귀 호출
            yield return new WaitForSeconds(retryInterval);

            // 재귀 호출 (자동 재요청)
            yield return ExecuteVlEngineCoroutine(
                query: "",  // 재요청 시 query는 빈 문자열
                previousThinkLog: thinkLog,
                previousAgentState: agentState,
                onEvent: onEvent,
                onComplete: onComplete,
                retryCount: retryCount,  // retryCount는 서버가 관리하므로 그대로 전달
                maxRetry: maxRetry,
                verbose: verbose,
                scenarioName: scenarioName
            );
            yield break;  // 재귀 호출 후 현재 코루틴 종료
        }

        // 기타 이벤트 (thinking, goal, plan, check, revise 등)는 그냥 완료
        Debug.Log($"[VlEngine] 알 수 없는 이벤트: {kind}");
        onComplete?.Invoke(lastEventData);
    }

    #endregion

    #region API 호출

    // /vl_agent/engine_stream API 호출 (스트리밍)
    private IEnumerator CallEngineStreamApi(
        string query,
        byte[] imageBytes,
        JArray previousThinkLog,
        JToken previousAgentState,
        int retryCount,
        int maxRetry,
        bool verbose,
        string scenarioName,
        Action<JObject> onEvent,
        Action<JObject> onLastEvent
    )
    {
        // BaseUrl 가져오기
        string baseUrl = null;
        yield return GetBaseUrlCoroutine((url) => { baseUrl = url; });

        string apiUrl = baseUrl + "/vl_agent/engine_stream";
        string normalizedScenarioName = !string.IsNullOrWhiteSpace(scenarioName) ? scenarioName.Trim()
            : !string.IsNullOrWhiteSpace(defaultScenarioName) ? defaultScenarioName.Trim()
            : ScenarioBASkip;
        Debug.Log($"[VlEngine] API 호출: {apiUrl}, scenario={normalizedScenarioName}");

        // memory 가져오기 (첫 요청 시)
        string memoryJson = "";
        if (previousThinkLog == null || previousThinkLog.Count == 0)
        {
            var memoryList = MemoryManager.Instance.GetAllConversationMemory();
            memoryJson = JsonConvert.SerializeObject(memoryList);
        }

        // think_log, agent_state JSON 직렬화
        string thinkLogJson = previousThinkLog != null ? previousThinkLog.ToString(Formatting.None) : "";
        string agentStateJson = previousAgentState != null ? previousAgentState.ToString(Formatting.None) : "";

        // Task로 비동기 스트리밍 요청 처리
        var eventQueue = new System.Collections.Concurrent.ConcurrentQueue<JObject>();
        Func<bool> isCanceledProvider = () => isCanceled;

        var task = Task.Run(() => SendEngineStreamRequest(
            apiUrl,
            query,
            memoryJson,
            thinkLogJson,
            agentStateJson,
            retryCount,
            maxRetry,
            verbose,
            isCanceledProvider,
            imageBytes,
            normalizedScenarioName,
            (eventData) => { eventQueue.Enqueue(eventData); }
        ));

        // 이벤트 큐 처리 (메인 스레드에서)
        JObject lastEvent = null;
        while (!task.IsCompleted)
        {
            while (eventQueue.TryDequeue(out JObject eventData))
            {
                ProcessEngineEvent(eventData, onEvent);
                lastEvent = eventData;
            }
            yield return null;
        }

        // 남은 이벤트 처리
        while (eventQueue.TryDequeue(out JObject eventData))
        {
            ProcessEngineEvent(eventData, onEvent);
            lastEvent = eventData;
        }

        // 예외 처리
        if (task.Exception != null)
        {
            string errorMsg = task.Exception.InnerException?.Message ?? task.Exception.Message;
            Debug.LogError($"[VlEngine] API 오류: {errorMsg}");
            yield break;
        }

        // 최종 이벤트 전달
        onLastEvent?.Invoke(lastEvent);
    }

    // /vl_agent/engine_stream 스트리밍 요청 전송
    private (bool success, string errorMsg) SendEngineStreamRequest(
        string apiUrl,
        string query,
        string memoryJson,
        string thinkLogJson,
        string agentStateJson,
        int retryCount,
        int maxRetry,
        bool verbose,
        Func<bool> isCanceledProvider,
        byte[] imageBytes,
        string scenarioName,
        Action<JObject> onEvent
    )
    {
        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Timeout = 120000;

        using (MemoryStream memStream = new MemoryStream())
        using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
        {
            // 텍스트 필드들 (헬퍼 메서드 사용)
            WriteTextField(writer, boundary, "query", query);
            WriteTextField(writer, boundary, "memory", memoryJson);
            WriteTextField(writer, boundary, "think_log", thinkLogJson);
            WriteTextField(writer, boundary, "agent_state", agentStateJson);
            WriteTextField(writer, boundary, "retry_count", retryCount.ToString());
            WriteTextField(writer, boundary, "max_retry", maxRetry.ToString());
            WriteTextField(writer, boundary, "is_canceled", isCanceledProvider != null && isCanceledProvider() ? "true" : "false");
            WriteTextField(writer, boundary, "verbose", verbose ? "true" : "false");
            WriteTextField(writer, boundary, "scenario_name", scenarioName);

            // 이미지 필드
            if (imageBytes != null && imageBytes.Length > 0)
            {
                writer.WriteLine($"--{boundary}");
                writer.WriteLine("Content-Disposition: form-data; name=\"image\"; filename=\"capture.png\"");
                writer.WriteLine("Content-Type: image/png");
                writer.WriteLine();
                writer.Flush();
                memStream.Write(imageBytes, 0, imageBytes.Length);
                writer.WriteLine();
            }

            // 종료 boundary
            writer.WriteLine($"--{boundary}--");
            writer.Flush();

            // 요청 전송
            request.ContentLength = memStream.Length;
            using (Stream requestStream = request.GetRequestStream())
            {
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.CopyTo(requestStream);
            }
        }

        // 스트리밍 응답 처리
        try
        {
            using (WebResponse response = request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
            {
                string lastKind = "";
                string lastErrorMsg = "";

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // 취소 요청 감지
                    if (isCanceledProvider != null && isCanceledProvider())
                    {
                        Debug.Log("[VlEngine] 취소 감지 - 스트리밍 수신 중단");
                        return (false, "canceled");
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        JObject eventData = JObject.Parse(line);
                        lastKind = eventData["kind"]?.Value<string>() ?? "";

                        if (lastKind == "fail")
                        {
                            lastErrorMsg = eventData["message"]?.Value<string>() ?? "Unknown error";
                        }

                        onEvent?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[VlEngine] 이벤트 파싱 오류: {ex.Message}, line={line}");
                    }
                }

                // 최종 결과 판단 (done만 성공)
                bool success = lastKind == "done";
                return (success, success ? "" : lastErrorMsg);
            }
        }
        catch (WebException ex)
        {
            if (ex.Response != null)
            {
                using (Stream errorStream = ex.Response.GetResponseStream())
                using (StreamReader errorReader = new StreamReader(errorStream))
                {
                    string errorResponse = errorReader.ReadToEnd();
                    Debug.LogError($"[VlEngine] 서버 오류: {errorResponse}");
                    return (false, errorResponse);
                }
            }
            throw;
        }
    }

    #endregion

    #region 이벤트 처리

    // Engine 이벤트 처리 (UI 로그/알림 표시 전용)
    private void ProcessEngineEvent(JObject eventData, Action<JObject> onEvent)
    {
        string kind = eventData["kind"]?.Value<string>() ?? "";
        string message = eventData["message"]?.Value<string>() ?? "";

        string eventMessage = $"[VlEngine] 이벤트: [{kind}] {message}";
        Debug.Log(eventMessage);
        
        // RAW JSON 로깅 (ocr_history 확인용)
        Debug.Log($"[VlEngine] RAW JSON: {eventData.ToString(Newtonsoft.Json.Formatting.None)}");
        
        ProcessVlMessage(eventMessage);

        // thinking 이벤트: NoticeManager로 안내 말풍선 표시
        if (kind == "thinking")
        {
            NoticeManager.Instance.Notice("thinking");
        }
        // act 이벤트 실행은 lastEventData 처리 시점에 수행
        else if (kind == "act")
        {
            // no-op
        }

        // 외부 콜백 호출
        onEvent?.Invoke(eventData);
    }

    #endregion

    #region 유틸리티 메서드


    private string ResolveEngineFormLang()
    {
        string lang = engineFormLang;

        if (string.IsNullOrWhiteSpace(lang))
        {
            try
            {
                lang = SettingManager.Instance.settings.sound_language.ToString();
            }
            catch
            {
                lang = "ja";
            }
        }

        lang = lang.Trim().ToLowerInvariant();
        if (lang == "jp")
        {
            lang = "ja";
        }

        if (lang != "ko" && lang != "ja" && lang != "en")
        {
            lang = "ja";
        }

        return lang;
    }

    private float ResolveEngineFormSpeed()
    {
        float speed = 1.0f;
        try
        {
            // sound_speedMaster는 퍼센트 단위 (100 = 100%)
            // 서버는 배율 단위 기대 (1.0 = 정상 속도)
            speed = SettingManager.Instance.settings.sound_speedMaster / 100f;
        }
        catch
        {
            speed = 1.0f;
        }

        if (speed <= 0f)
        {
            speed = 1.0f;
        }

        return speed;
    }

    private string MapVoiceActor(string actorName)
    {
        // 빈 actor는 기본값 "arona"
        if (string.IsNullOrWhiteSpace(actorName))
        {
            Debug.Log("[VlEngine] Actor 비어있음 → 기본값 'arona' 사용");
            return "arona";
        }

        // OCRAutoMapManager로 매핑 (OCR 이름 → 캐릭터 ID)
        try
        {
            var actorMap = OCRAutoMapManager.Instance.GetActorMapIncludeCustomMap();
            
            if (actorMap.TryGetValue(actorName, out string mappedActor))
            {
                Debug.Log($"[VlEngine] Actor 매핑: '{actorName}' → '{mappedActor}'");
                return mappedActor;
            }
            else
            {
                Debug.LogWarning($"[VlEngine] Actor '{actorName}' 매핑 실패 → 기본값 'arona' 사용");
                return "arona";
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VlEngine] Actor 매핑 오류: {ex.Message} → 기본값 'arona' 사용");
            return "arona";
        }
    }

    // Voice 요청 + 재생 대기 + 클릭 (통합 처리)
    private IEnumerator RequestEngineFormVoiceCoroutineWithClick(
        string actor,
        string txt,
        int clickX,
        int clickY,
        int captureOffsetX,
        int captureOffsetY,
        bool verbose,
        JToken agentState
    )
    {
        // WAV 수신 및 재생 시작, 재생 시간은 콜백으로 받음
        float durationSec = 0f;
        yield return RequestEngineFormVoiceCoroutine(actor, txt, verbose, agentState, (d) => durationSec = d);

        // 음성 재생 시간만큼 대기 (X-Audio-Duration 헤더 값 사용)
        float waitTime = durationSec > 0f ? durationSec : 2f;
        Debug.Log($"[VlEngine] 음성 재생 대기: {waitTime:F2}초 (duration={durationSec:F2}s)");
        yield return new WaitForSeconds(waitTime);

        // 클릭 실행
        ExecuteClickFromRelative(clickX, clickY, captureOffsetX, captureOffsetY, true, "[VlEngine]");
    }

    private IEnumerator RequestEngineFormVoiceCoroutine(
        string actor,
        string txt,
        bool verbose,
        JToken agentState,
        Action<float> onDuration = null
    )
    {
        if (string.IsNullOrWhiteSpace(txt))
        {
            Debug.LogWarning("[VlEngine] request_voice=true 이지만 voice_txt가 비어있어 음성 요청을 생략합니다.");
            yield break;
        }

        string baseUrl = null;
        yield return GetBaseUrlCoroutine((url) => { baseUrl = url; });

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogWarning("[VlEngine] BaseUrl 없음 - engine_form 요청 생략");
            yield break;
        }

        string apiUrl = baseUrl + "/vl_agent/engine_form";
        string lang = ResolveEngineFormLang();
        float speed = ResolveEngineFormSpeed();

        // Task로 비동기 실행
        var task = SendEngineFormRequestAsync(apiUrl, actor ?? "", txt, lang, speed, verbose, agentState);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            string error = task.Exception.InnerException?.Message ?? task.Exception.Message;
            Debug.LogError($"[VlEngine] engine_form 오류: {error}");
            yield break;
        }

        var result = task.Result;
        if (!result.success || result.wavData == null || result.wavData.Length == 0)
        {
            Debug.LogWarning($"[VlEngine] engine_form 실패: {result.errorMsg}");
            yield break;
        }

        // WAV 저장 후 비동기 재생 시작
        string fileName = string.IsNullOrWhiteSpace(engineFormWavFileName) ? "vl_engine_form_response.wav" : engineFormWavFileName.Trim();
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        try
        {
            File.WriteAllBytes(filePath, result.wavData);
            Debug.Log($"[VlEngine] WAV 파일 저장: {filePath} (duration: {result.durationSec:F2}s)");

            string fileUri = "file:///" + filePath.Replace("\\", "/");
            StartCoroutine(PlayWavFromUri(fileUri));
            Debug.Log($"[VlEngine] 음성 재생 시작: {fileUri}");

            // 재생 시간 통보 (호출자가 대기 시간 결정에 사용)
            onDuration?.Invoke(result.durationSec);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VlEngine] engine_form WAV 저장/재생 실패: {ex.Message}");
        }
    }

    private async Task<(bool success, byte[] wavData, float durationSec, string errorMsg)> SendEngineFormRequestAsync(
        string apiUrl,
        string actor,
        string txt,
        string lang,
        float speed,
        bool verbose,
        JToken agentState
    )
    {
        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Timeout = 120000;

        using (MemoryStream memStream = new MemoryStream())
        using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
        {
            WriteTextField(writer, boundary, "actor", actor);
            WriteTextField(writer, boundary, "txt", txt);
            WriteTextField(writer, boundary, "lang", lang);
            WriteTextField(writer, boundary, "speed", speed.ToString("0.0###", CultureInfo.InvariantCulture));
            WriteTextField(writer, boundary, "verbose", verbose ? "true" : "false");

            // verbose 모드일 때 ocr_history_json 추가
            if (verbose && agentState != null && agentState["ocr_history"] != null)
            {
                try
                {
                    // {"history": [...]} 형식으로 wrapping
                    var historyWrapper = new JObject
                    {
                        ["history"] = agentState["ocr_history"]
                    };
                    string ocrHistoryJson = historyWrapper.ToString(Formatting.None);
                    WriteTextField(writer, boundary, "ocr_history_json", ocrHistoryJson);
                    
                    int historyCount = (agentState["ocr_history"] as JArray)?.Count ?? 0;
                    Debug.Log($"[VlEngine] ocr_history_json 전송: {historyCount} entries");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[VlEngine] ocr_history_json 직렬화 실패: {ex.Message}");
                }
            }

            writer.WriteLine($"--{boundary}--");
            writer.Flush();

            request.ContentLength = memStream.Length;
            using (Stream requestStream = request.GetRequestStream())
            {
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.CopyTo(requestStream);
            }
        }

        try
        {
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return (false, null, 0f, $"HTTP {response.StatusCode}");
                }

                float durationSec = 0f;
                string durationHeader = response.Headers["X-Audio-Duration"];
                if (!string.IsNullOrEmpty(durationHeader))
                {
                    float.TryParse(durationHeader, NumberStyles.Float, CultureInfo.InvariantCulture, out durationSec);
                }

                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        return (false, null, durationSec, "Empty response stream");
                    }

                    byte[] wavData = ReadFully(responseStream);
                    return (true, wavData, durationSec, "");
                }
            }
        }
        catch (WebException ex)
        {
            if (ex.Response != null)
            {
                using (Stream errorStream = ex.Response.GetResponseStream())
                using (StreamReader errorReader = new StreamReader(errorStream))
                {
                    string errorResponse = errorReader.ReadToEnd();
                    Debug.LogError($"[VlEngine] engine_form 서버 오류: {errorResponse}");
                    return (false, null, 0f, errorResponse);
                }
            }

            return (false, null, 0f, ex.Message);
        }
        catch (Exception ex)
        {
            return (false, null, 0f, ex.Message);
        }
    }


    private byte[] ReadFully(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    private IEnumerator PlayWavFromUri(string fileUri)
    {
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.WAV))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[VlEngine] WAV 재생 실패: {uwr.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
            if (clip == null)
            {
                Debug.LogError("[VlEngine] AudioClip 생성 실패");
                yield break;
            }

            // SubVoiceManager의 AudioSource 풀에서 빈 슬롯을 받아 재생
            AudioSource source = SubVoiceManager.Instance.GetAvailableAudioSource();
            if (source == null)
            {
                Debug.LogWarning("[VlEngine] SubVoiceManager AudioSource 풀 포화 - 재생 건너뜀");
                yield break;
            }

            source.clip = clip;
            source.volume = 1f;
            try { source.volume = SettingManager.Instance.settings.sound_volumeMaster / 100f; }
            catch { /* 볼륨 설정 실패 시 기본값 유지 */ }

            source.Play();
            Debug.Log($"[VlEngine] WAV 재생 시작 (SubVoiceManager)");
        }
    }

    // Screenshot 영역이 있으면 영역 캡처, 없으면 전체화면 캡처를 수행하고 bytes와 offset(x,y)를 반환
    private IEnumerator CaptureScreenToMemoryWithOffset(
        Action<byte[], int, int> onCaptured,
        Action<string> onFail,
        string logPrefix
    )
    {
        byte[] imageBytes = null;
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        if (ScreenshotManager.Instance.IsScreenshotAreaSet())
        {
            Debug.Log($"{logPrefix} Screenshot 영역 캡처");

            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureScreenshotToMemoryWithInfo((bytes, x, y, w, h) =>
            {
                imageBytes = bytes;
                captureOffsetX = x;
                captureOffsetY = y;
                captureComplete = true;
                Debug.Log($"{logPrefix} 캡처 offset: ({x}, {y}), size: {w}x{h}");
            });
            while (!captureComplete)
            {
                yield return null;
            }
        }
        else
        {
            Debug.Log($"{logPrefix} 전체화면 캡처");

            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureFullScreenToMemory((bytes) =>
            {
                imageBytes = bytes;
                captureOffsetX = 0;
                captureOffsetY = 0;
                captureComplete = true;
            });
            while (!captureComplete)
            {
                yield return null;
            }
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            onFail?.Invoke("화면 캡처 실패");
            yield break;
        }

        onCaptured?.Invoke(imageBytes, captureOffsetX, captureOffsetY);
    }

    // ServerManager에서 BaseUrl을 비동기로 가져오기
    private IEnumerator GetBaseUrlCoroutine(Action<string> onReady)
    {
        string baseUrl = null;
        bool urlComplete = false;

        ServerManager.Instance.GetBaseUrl((url) =>
        {
            baseUrl = url;
            urlComplete = true;
        });

        while (!urlComplete)
        {
            yield return null;
        }

        onReady?.Invoke(baseUrl);
    }

    // 상대 좌표 + offset을 절대 좌표로 변환
    private (int absoluteX, int absoluteY) ConvertRelativeToAbsolute(int relativeX, int relativeY, int offsetX, int offsetY)
    {
        int absoluteX = relativeX + offsetX;
        int absoluteY = relativeY + offsetY;
        return (absoluteX, absoluteY);
    }

    // 상대 좌표를 절대 좌표로 변환 후 클릭 이펙트 및 실제 클릭을 수행
    private void ExecuteClickFromRelative(int relativeX, int relativeY, int offsetX, int offsetY, bool isMouseMove, string logPrefix)
    {
        (int absoluteX, int absoluteY) = ConvertRelativeToAbsolute(relativeX, relativeY, offsetX, offsetY);

        Debug.Log($"{logPrefix} 클릭 실행: ({relativeX}, {relativeY}) + offset: ({offsetX}, {offsetY}) = ({absoluteX}, {absoluteY})");

        ShowClickPosition(absoluteX, absoluteY);
        ExecutorMouseAction.Instance.ClickAtPosition(absoluteX, absoluteY, isMouseMove);
    }

    // 클릭 위치에 임시 마커 표시 (디버깅용) - Windows 화면 좌표를 Unity 스크린 좌표로 변환하여 표시
    public void ShowClickPosition(int winX, int winY, float duration = 2f)
    {
        Debug.Log($"[VlEngine] ShowClickPosition: ({winX}, {winY})");
        StartCoroutine(ShowClickPositionCoroutine(winX, winY, duration));
    }

    // 클릭 마커 표시 코루틴
    private IEnumerator ShowClickPositionCoroutine(int winX, int winY, float duration)
    {
        GameObject marker = new GameObject("VL_EngineClickMarker");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[VlEngine] Canvas를 찾을 수 없음 - 마커 생성 실패");
            Destroy(marker);
            yield break;
        }

        marker.transform.SetParent(canvas.transform, false);

        var image = marker.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 0f, 0f, 0.7f);
        image.raycastTarget = false;

        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(30, 30);

        float unityScreenX = winX;
        float unityScreenY = Screen.height - winY;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            new Vector2(unityScreenX, unityScreenY),
            canvas.worldCamera,
            out Vector2 localPoint
        );

        rt.anchoredPosition = localPoint;

        Debug.Log($"[VlEngine] 마커 표시: Windows({winX}, {winY}) → Unity({unityScreenX}, {unityScreenY}) → Local({localPoint.x}, {localPoint.y})");

        if (fx_click != null)
        {
            Vector3 worldPos = canvas.transform.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0));
            fx_click.transform.position = worldPos;
            fx_click.Play();
            Debug.Log($"[VlEngine] 이펙트 재생: World({worldPos.x}, {worldPos.y}, {worldPos.z})");
        }

        yield return new WaitForSeconds(duration);

        if (marker != null)
        {
            Destroy(marker);
        }
    }

    #endregion

    #region Multipart Form Data 헬퍼

    // multipart/form-data 텍스트 필드 작성 헬퍼
    private void WriteTextField(StreamWriter writer, string boundary, string name, string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        writer.WriteLine($"--{boundary}");
        writer.WriteLine($"Content-Disposition: form-data; name=\"{name}\"");
        writer.WriteLine();
        writer.WriteLine(value);
    }

    #endregion

    private void ProcessVlMessage(string message)
    {
        // AnswerBalloonSimple에 표시
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(message);

        // DebugBalloonManager2에 기록
        DebugBalloonManager2.Instance.AddVlAgentLog(message);
    }
}
