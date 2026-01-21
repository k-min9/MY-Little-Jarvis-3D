using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ApiVlAgentManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static ApiVlAgentManager instance;
    public static ApiVlAgentManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiVlAgentManager>();
            }
            return instance;
        }
    }

    [Header("Click Effect")]
    [SerializeField] private ParticleSystem fx_click; // 클릭 이펙트

    // Stateless 재요청용 상태 (Unity가 들고 있음)
    private JArray pendingThinkLog = null;
    private bool shouldRequestNewObservation = false;
    private int currentOffsetX = 0;
    private int currentOffsetY = 0;
    private int pendingRetryCount = 0;      // 재요청 횟수
    private int pendingMaxRetry = 5;        // 최대 재요청 횟수
    private bool isCanceled = false;        // 취소 요청
    private Action<JObject> currentOnEvent = null;
    private Action<bool, string> currentOnComplete = null;

    // VL Agent 상태 표시용 말풍선 (EmotionBalloonManager)
    private GameObject vlStatusBalloon = null;  // 진행 상태 말풍선 (전송시작/생각중/검증중/수행중 - 매번 교체)

    // API 응답 모델
    [System.Serializable]
    public class VlAgentResponse
    {
        public bool ok;
        public string type;
        public string target;
        public int max_results;
        public object result;
        public string error;
    }

    // VL Agent 테스트 실행
    // 우선순위: Screenshot 영역 → 전체화면 (OCR slot 개념 없음)
    public void ExecuteVlAgentTest(string target = "button", int maxResults = 10, Action<JObject> onComplete = null)
    {
        StartCoroutine(ExecuteVlAgentTestCoroutine(target, maxResults, onComplete));
    }

    // VL Agent 테스트 코루틴: 화면 캡처 후 /vl_agent/test 호출 및 offset 정보 추가
    private IEnumerator ExecuteVlAgentTestCoroutine(string target, int maxResults, Action<JObject> onComplete)
    {
        byte[] imageBytes = null;
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        // 화면 캡처 및 offset 확보
        yield return CaptureScreenToMemoryWithOffset(
            (bytes, x, y) =>
            {
                imageBytes = bytes;
                captureOffsetX = x;
                captureOffsetY = y;
            },
            (failMsg) =>
            {
                Debug.LogError($"[VlAgent] {failMsg}");
                onComplete?.Invoke(null);
            },
            "[VlAgent]"
        );

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("[VlAgent] 화면 캡처 실패");
            onComplete?.Invoke(null);
            yield break;
        }

        Debug.Log($"[VlAgent] 캡처 완료: {imageBytes.Length} bytes, offset=({captureOffsetX}, {captureOffsetY}), target={target}");

        // API 호출 (offset 정보도 함께 전달)
        yield return CallVlAgentTestAPI(imageBytes, target, maxResults, captureOffsetX, captureOffsetY, onComplete);
    }

    // /vl_agent/test API 호출
    private IEnumerator CallVlAgentTestAPI(byte[] imageBytes, string target, int maxResults, int offsetX, int offsetY, Action<JObject> onComplete)
    {
        string baseUrl = null;

        // BaseUrl 가져오기
        yield return GetBaseUrlCoroutine((url) =>
        {
            baseUrl = url;
        });

        string apiUrl = baseUrl + "/vl_agent/test";
        Debug.Log($"[VlAgent] API 호출: {apiUrl}");

        // Task로 비동기 요청 처리
        var task = Task.Run(() => SendVlAgentRequest(apiUrl, imageBytes, target, maxResults));

        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.Exception != null)
        {
            Debug.LogError($"[VlAgent] API 오류: {task.Exception.InnerException?.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        string responseText = task.Result;
        if (string.IsNullOrEmpty(responseText))
        {
            Debug.LogError("[VlAgent] 빈 응답");
            onComplete?.Invoke(null);
            yield break;
        }

        try
        {
            JObject response = JObject.Parse(responseText);

            // 캡처 offset 정보를 응답에 추가 (클라이언트 측 정보)
            response["_captureOffsetX"] = offsetX;
            response["_captureOffsetY"] = offsetY;

            Debug.Log($"[VlAgent] 응답 (offset 포함): offsetX={offsetX}, offsetY={offsetY}");
            onComplete?.Invoke(response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VlAgent] 응답 파싱 오류: {ex.Message}");
            onComplete?.Invoke(null);
        }
    }

    // multipart/form-data 요청 전송
    private string SendVlAgentRequest(string apiUrl, byte[] imageBytes, string target, int maxResults)
    {
        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Timeout = 60000;

        using (MemoryStream memStream = new MemoryStream())
        using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
        {
            // 텍스트 필드 추가 (APIManager 패턴)
            var formData = new Dictionary<string, string>
            {
                { "target", target },
                { "max_results", maxResults.ToString() },
                { "debug", "true" }
            };

            foreach (var entry in formData)
            {
                writer.WriteLine($"--{boundary}");
                writer.WriteLine($"Content-Disposition: form-data; name=\"{entry.Key}\"");
                writer.WriteLine();
                writer.WriteLine(entry.Value);
            }

            // 이미지 필드 추가
            writer.WriteLine($"--{boundary}");
            writer.WriteLine("Content-Disposition: form-data; name=\"image\"; filename=\"capture.png\"");
            writer.WriteLine("Content-Type: image/png");
            writer.WriteLine();
            writer.Flush();
            memStream.Write(imageBytes, 0, imageBytes.Length);
            writer.WriteLine();

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

        // 응답 읽기
        try
        {
            using (WebResponse response = request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                return reader.ReadToEnd();
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
                    Debug.LogError($"[VlAgent] 서버 오류: {errorResponse}");
                }
            }
            throw;
        }
    }

    // VL Agent를 실행하고 결과에서 첫 번째 클릭 좌표로 자동 클릭
    public void ExecuteVlAgentAndClick(string target = "button", int maxResults = 10, bool isMouseMove = true, Action<bool, int, int> onComplete = null)
    {
        ExecuteVlAgentTest(target, maxResults, (response) =>
        {
            if (response == null)
            {
                Debug.LogError("[VlAgent] 응답 없음 - 클릭 취소");
                onComplete?.Invoke(false, 0, 0);
                return;
            }

            try
            {
                bool ok = response["ok"]?.Value<bool>() ?? false;
                if (!ok)
                {
                    string error = response["error"]?.Value<string>() ?? "Unknown error";
                    Debug.LogError($"[VlAgent] 서버 오류: {error}");
                    onComplete?.Invoke(false, 0, 0);
                    return;
                }

                var variables = response["result"]?["variables"];
                if (variables == null)
                {
                    Debug.LogError("[VlAgent] variables 필드 없음");
                    onComplete?.Invoke(false, 0, 0);
                    return;
                }

                bool exists = variables["exists"]?.Value<bool>() ?? false;
                if (!exists)
                {
                    Debug.LogWarning($"[VlAgent] '{target}' 대상을 찾지 못함");
                    onComplete?.Invoke(false, 0, 0);
                    return;
                }

                int? x = variables["x"]?.Value<int>();
                int? y = variables["y"]?.Value<int>();

                if (!x.HasValue || !y.HasValue)
                {
                    Debug.LogError("[VlAgent] 좌표 값 없음");
                    onComplete?.Invoke(false, 0, 0);
                    return;
                }

                // 응답 JSON에서 캡처 offset 읽기
                int offsetX = response["_captureOffsetX"]?.Value<int>() ?? 0;
                int offsetY = response["_captureOffsetY"]?.Value<int>() ?? 0;

                // 상대 좌표를 절대 좌표로 변환 후 클릭 수행
                (int absoluteX, int absoluteY) = ConvertRelativeToAbsolute(x.Value, y.Value, offsetX, offsetY);

                Debug.Log($"[VlAgent] 상대좌표: ({x.Value}, {y.Value}) + offset: ({offsetX}, {offsetY}) = 절대좌표: ({absoluteX}, {absoluteY}), isMouseMove={isMouseMove}");

                ShowClickPosition(absoluteX, absoluteY);
                ExecutorMouseAction.Instance.ClickAtPosition(absoluteX, absoluteY, isMouseMove);
                onComplete?.Invoke(true, absoluteX, absoluteY);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VlAgent] 응답 처리 오류: {ex.Message}");
                onComplete?.Invoke(false, 0, 0);
            }
        });
    }

    // 클릭 위치에 임시 마커 표시 (디버깅용)
    // Windows 화면 좌표를 Unity 스크린 좌표로 변환하여 표시
    public void ShowClickPosition(int winX, int winY, float duration = 2f)
    {
        Debug.Log($"[VlAgent] ShowClickPosition: ({winX}, {winY})");
        StartCoroutine(ShowClickPositionCoroutine(winX, winY, duration));
    }

    #region VL Planer Run (Streaming, Stateless)

    // VL Planer Run 스트리밍 실행 (/vl_agent/run)
    public void ExecuteVlPlanerRun(
        string query,
        Action<JObject> onEvent = null,
        Action<bool, string> onComplete = null,
        int maxRetry = 5
    )
    {
        ExecutePlannerRunCore("/vl_agent/run", query, onEvent, onComplete, maxRetry, "VL Planer 시작...", "VL Planer 재시작...");
        SetVlStatusBalloon("Question");
    }

    // VL Planer Run Special 스트리밍 실행 (/vl_agent/run_special)
    public void ExecuteVlPlanerRunSpecial(
        string query,
        Action<JObject> onEvent = null,
        Action<bool, string> onComplete = null,
        int maxRetry = 30
    )
    {
        ExecutePlannerRunCore("/vl_agent/run_special", query, onEvent, onComplete, maxRetry, "VL Special 시작...", "VL Special 재시작...");
        SetVlStatusBalloon("Question");
    }

    // 작업 취소 요청
    public void CancelVlPlanerRun()
    {
        isCanceled = true;
        shouldRequestNewObservation = false;
        pendingThinkLog = null;
        Debug.Log("[VlPlanerRun] 취소 요청됨");
    }

    // Planer Run 공통 진입점: 콜백 저장, 취소 플래그 초기화, memory 확보 후 코루틴 시작
    private void ExecutePlannerRunCore(
        string endpoint,
        string query,
        Action<JObject> onEvent,
        Action<bool, string> onComplete,
        int maxRetry,
        string startText,
        string restartText
    )
    {
        currentOnEvent = onEvent;
        currentOnComplete = onComplete;
        isCanceled = false;

        var memoryList = MemoryManager.Instance.GetAllConversationMemory();
        string memoryJson = JsonConvert.SerializeObject(memoryList);

        StartCoroutine(ExecutePlannerRunCoreCoroutine(endpoint, query, memoryJson, null, 0, maxRetry, startText, restartText, onEvent, onComplete));
    }

    // think_log 포함 재요청
    private void ExecutePlannerRunCoreWithThinkLog(string endpoint, JArray thinkLog, int retryCount, int maxRetry, string restartText)
    {
        StartCoroutine(ExecutePlannerRunCoreCoroutine(endpoint, "", "", thinkLog, retryCount, maxRetry, restartText, restartText, currentOnEvent, currentOnComplete));
    }

    // Planer Run 공통 코루틴: 캡처 후 스트리밍 API 호출
    private IEnumerator ExecutePlannerRunCoreCoroutine(
        string endpoint,
        string query,
        string memoryJson,
        JArray thinkLog,
        int retryCount,
        int maxRetry,
        string startText,
        string restartText,
        Action<JObject> onEvent,
        Action<bool, string> onComplete
    )
    {
        byte[] imageBytes = null;
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        // 화면 캡처 및 offset 확보
        yield return CaptureScreenToMemoryWithOffset(
            (bytes, x, y) =>
            {
                imageBytes = bytes;
                captureOffsetX = x;
                captureOffsetY = y;
            },
            (failMsg) =>
            {
                Debug.LogError($"[{endpoint}] {failMsg}");
                onComplete?.Invoke(false, "화면 캡처 실패");
            },
            $"[{endpoint}]"
        );

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError($"[{endpoint}] 화면 캡처 실패");
            onComplete?.Invoke(false, "화면 캡처 실패");
            yield break;
        }

        // offset 저장 (이벤트 처리 시 사용)
        currentOffsetX = captureOffsetX;
        currentOffsetY = captureOffsetY;

        bool isResume = thinkLog != null && thinkLog.Count > 0;
        Debug.Log($"[{endpoint}] 캡처 완료: {imageBytes.Length} bytes, {(isResume ? "재요청" : "첫 요청")}");

        // API 호출 (스트리밍)
        yield return CallPlannerRunStreamingApi(endpoint, query, memoryJson, thinkLog, retryCount, maxRetry, imageBytes, captureOffsetX, captureOffsetY, startText, restartText, onEvent, onComplete);
    }

    // Planer Run 스트리밍 API 호출 공통: BaseUrl 확보, 스트리밍 이벤트 큐 처리, 재요청 처리, 완료 처리
    private IEnumerator CallPlannerRunStreamingApi(
        string endpoint,
        string query,
        string memoryJson,
        JArray thinkLog,
        int retryCount,
        int maxRetry,
        byte[] imageBytes,
        int offsetX,
        int offsetY,
        string startText,
        string restartText,
        Action<JObject> onEvent,
        Action<bool, string> onComplete
    )
    {
        // 상태 표시 - 시작
        ShowAnswerBalloonStartText(thinkLog, startText, restartText);

        string baseUrl = null;
        yield return GetBaseUrlCoroutine((url) =>
        {
            baseUrl = url;
        });

        string apiUrl = baseUrl + endpoint;
        Debug.Log($"[{endpoint}] API 호출: {apiUrl}");

        // think_log JSON 직렬화
        string thinkLogJson = thinkLog != null ? thinkLog.ToString(Formatting.None) : "";

        // Task로 비동기 스트리밍 요청 처리 (이벤트 큐와 함께)
        var eventQueue = new System.Collections.Concurrent.ConcurrentQueue<JObject>();
        Func<bool> isCanceledProvider = () => isCanceled;

        var task = Task.Run(() => SendPlannerRunRequestStateless(
            apiUrl,
            query,
            memoryJson,
            thinkLogJson,
            retryCount,
            maxRetry,
            isCanceledProvider,
            imageBytes,
            (eventData) =>
            {
                eventQueue.Enqueue(eventData);
            }
        ));

        // 이벤트 큐 처리 (메인 스레드에서)
        while (!task.IsCompleted)
        {
            while (eventQueue.TryDequeue(out JObject eventData))
            {
                ProcessPlannerEventStateless(eventData, offsetX, offsetY, onEvent);
            }
            yield return null;
        }

        // 남은 이벤트 처리
        while (eventQueue.TryDequeue(out JObject eventData))
        {
            ProcessPlannerEventStateless(eventData, offsetX, offsetY, onEvent);
        }

        if (task.Exception != null)
        {
            string errorMsg = task.Exception.InnerException?.Message ?? task.Exception.Message;
            Debug.LogError($"[{endpoint}] API 오류: {errorMsg}");
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"{endpoint} 오류: {errorMsg}");
            onComplete?.Invoke(false, errorMsg);
            yield break;
        }

        var result = task.Result;

        // request_observation이면 재요청 처리
        if (shouldRequestNewObservation && pendingThinkLog != null)
        {
            shouldRequestNewObservation = false;
            Debug.Log($"[{endpoint}] request_observation 수신 - 재요청 대기");

            // 검증중 이펙트
            SetVlStatusBalloon("Verify");

            yield return new WaitForSeconds(1.0f);

            Debug.Log($"[{endpoint}] 재요청 시작");
            ExecutePlannerRunCoreWithThinkLog(endpoint, pendingThinkLog, pendingRetryCount, pendingMaxRetry, restartText);
            yield break;
        }

        // 상태 표시 - 완료
        ClearVlStatusBalloon();

        // 성공/실패 말풍선 3초간 표시
        string resultSprite = result.success ? "Yes" : "No";
        EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), resultSprite, 3f, 0f);

        string statusText = result.success ? "완료" : "실패";
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"{endpoint} {statusText}됨");

        onComplete?.Invoke(result.success, result.errorMsg);
    }

    // Planer 이벤트 처리 (Stateless 공통)
    private void ProcessPlannerEventStateless(JObject eventData, int offsetX, int offsetY, Action<JObject> onEvent)
    {
        string kind = (string)eventData["kind"] ?? "";
        string message = (string)eventData["message"] ?? "";

        // 상태 표시 업데이트
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"[{kind}] {message}");

        // 이벤트 종류별 이펙트 처리
        UpdateVlStatusBalloonByKind(kind);

        // request_observation 처리
        if (kind == "request_observation")
        {
            HandleRequestObservation(eventData, offsetX, offsetY);
        }
        else if (kind == "done")
        {
            HandleDoneEvent(eventData, offsetX, offsetY);
        }
        else if (kind == "fail")
        {
            HandleFailEvent();
        }
        else if (kind == "act")
        {
            HandleActPreview(eventData, offsetX, offsetY);
        }

        // 외부 콜백 호출
        onEvent?.Invoke(eventData);
    }

    // request_observation: 클릭 요청 처리 및 재요청용 상태 저장
    private void HandleRequestObservation(JObject eventData, int offsetX, int offsetY)
    {
        var data = eventData["data"] as JObject;
        var thinkLog = eventData["think_log"] as JArray;

        Debug.Log($"[VlPlanerRun] request_observation 수신, think_log: {thinkLog?.Count ?? 0}개");

        // 요청 수행중 이펙트
        SetVlStatusBalloon("Execute");

        // click_request가 있으면 클릭 수행
        if (data != null)
        {
            var clickRequest = data["click_request"] as JObject;
            if (clickRequest != null)
            {
                int? x = (int?)clickRequest["x"];
                int? y = (int?)clickRequest["y"];
                if (x.HasValue && y.HasValue)
                {
                    ExecuteClickFromRelative(x.Value, y.Value, offsetX, offsetY, true, "[VlPlanerRun] 클릭 실행");
                }
            }

            // retry_count, max_retry 저장
            pendingRetryCount = data["retry_count"]?.Value<int>() ?? 0;
            pendingMaxRetry = data["max_retry"]?.Value<int>() ?? 5;
        }

        // think_log 저장 및 재요청 플래그 설정
        pendingThinkLog = thinkLog;
        shouldRequestNewObservation = true;
    }

    // done: request_type 처리
    private void HandleDoneEvent(JObject eventData, int offsetX, int offsetY)
    {
        Debug.Log("[VlPlanerRun] 작업 완료 (done)");
        shouldRequestNewObservation = false;
        pendingThinkLog = null;

        var data = eventData["data"] as JObject;
        if (data == null)
        {
            return;
        }

        string requestType = data["request_type"]?.Value<string>() ?? "";
        switch (requestType)
        {
            case "function_request_click":
            {
                var finalClick = data["final_click"] as JObject;
                if (finalClick != null)
                {
                    int? x = (int?)finalClick["x"];
                    int? y = (int?)finalClick["y"];
                    if (x.HasValue && y.HasValue)
                    {
                        ExecuteClickFromRelative(x.Value, y.Value, offsetX, offsetY, true, "[VlPlanerRun] 최종 클릭");
                    }
                }
                break;
            }
            case "function_play_sfx_alert":
            {
                Debug.Log("[VlPlanerRun] 알림 효과음 재생 요청");
                StartCoroutine(ScenarioCommonManager.Instance.Run_C99_Alert_from_planner());
                break;
            }
            case "function_request_dance":
            {
                string danceType = data["dance_type"]?.Value<string>() ?? "default";
                Debug.Log($"[VlPlanerRun] 춤 요청: {danceType}");
                AnimationManager.Instance.Dance();
                break;
            }
            case "function_request_screenshot":
            {
                Debug.Log("[VlPlanerRun] 스크린샷 요청");
                ScreenshotManager.Instance.CaptureFullScreen();
                string screenshotPath = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots", "panel_capture.png");
                ClipboardManager.Instance.SetImageToClipboard(screenshotPath);
                break;
            }
            default:
            {
                if (!string.IsNullOrEmpty(requestType))
                {
                    Debug.LogWarning($"[VlPlanerRun] 알 수 없는 request_type: {requestType}");
                }
                break;
            }
        }
    }

    // fail: 실패 시 재요청 상태 정리
    private void HandleFailEvent()
    {
        Debug.Log("[VlPlanerRun] 작업 실패 (fail)");
        shouldRequestNewObservation = false;
        pendingThinkLog = null;
    }

    // act: grounding 후보 미리보기
    private void HandleActPreview(JObject eventData, int offsetX, int offsetY)
    {
        var data = eventData["data"] as JObject;
        if (data == null)
        {
            return;
        }

        var result = data["result"] as JObject;
        if (result == null)
        {
            return;
        }

        var candidates = result["candidates"] as JArray;
        if (candidates == null || candidates.Count <= 0)
        {
            return;
        }

        var firstCandidate = candidates[0] as JObject;
        if (firstCandidate == null)
        {
            return;
        }

        int? x = (int?)firstCandidate["x"];
        int? y = (int?)firstCandidate["y"];
        if (!x.HasValue || !y.HasValue)
        {
            return;
        }

        (int absoluteX, int absoluteY) = ConvertRelativeToAbsolute(x.Value, y.Value, offsetX, offsetY);
        Debug.Log($"[VlPlanerRun] Grounding 결과 - 첫 번째 후보: ({x.Value}, {y.Value}) = ({absoluteX}, {absoluteY})");
        ShowClickPosition(absoluteX, absoluteY, 1f);
    }

    // 스트리밍 요청 전송 (Stateless 버전)
    // 개선안 A 적용: isCanceled를 값으로 받지 않고 Func<bool>로 받아, 스트리밍 수신 루프에서 실시간 취소를 감지합니다.
    private (bool success, string errorMsg) SendPlannerRunRequestStateless(
        string apiUrl,
        string query,
        string memoryJson,
        string thinkLogJson,
        int retryCount,
        int maxRetry,
        Func<bool> isCanceledProvider,
        byte[] imageBytes,
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
            // query 필드
            writer.WriteLine($"--{boundary}");
            writer.WriteLine("Content-Disposition: form-data; name=\"query\"");
            writer.WriteLine();
            writer.WriteLine(query);

            // memory 필드 (선택)
            if (!string.IsNullOrEmpty(memoryJson))
            {
                writer.WriteLine($"--{boundary}");
                writer.WriteLine("Content-Disposition: form-data; name=\"memory\"");
                writer.WriteLine();
                writer.WriteLine(memoryJson);
            }

            // think_log 필드 (재요청 시)
            if (!string.IsNullOrEmpty(thinkLogJson))
            {
                writer.WriteLine($"--{boundary}");
                writer.WriteLine("Content-Disposition: form-data; name=\"think_log\"");
                writer.WriteLine();
                writer.WriteLine(thinkLogJson);
            }

            // retry_count 필드
            writer.WriteLine($"--{boundary}");
            writer.WriteLine("Content-Disposition: form-data; name=\"retry_count\"");
            writer.WriteLine();
            writer.WriteLine(retryCount.ToString());

            // max_retry 필드
            writer.WriteLine($"--{boundary}");
            writer.WriteLine("Content-Disposition: form-data; name=\"max_retry\"");
            writer.WriteLine();
            writer.WriteLine(maxRetry.ToString());

            // is_canceled 필드
            writer.WriteLine($"--{boundary}");
            writer.WriteLine("Content-Disposition: form-data; name=\"is_canceled\"");
            writer.WriteLine();
            writer.WriteLine(isCanceledProvider != null && isCanceledProvider() ? "true" : "false");

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
                    // 취소 요청이 들어오면 스트리밍 수신을 중단하고 종료합니다.
                    if (isCanceledProvider != null && isCanceledProvider())
                    {
                        Debug.Log("[VlPlanerRun] 취소 감지 - 스트리밍 수신 중단");
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
                        Debug.LogWarning($"[VlPlanerRun] 이벤트 파싱 오류: {ex.Message}, line={line}");
                    }
                }

                // 최종 결과 판단
                bool success = lastKind == "done" || lastKind == "request_observation" || lastKind == "max_retry_reached";
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
                    Debug.LogError($"[VlPlanerRun] 서버 오류: {errorResponse}");
                    return (false, errorResponse);
                }
            }
            throw;
        }
    }

    #endregion

    // Screenshot 영역이 있으면 영역 캡처, 없으면 전체화면 캡처를 수행하고 bytes와 offset(x,y)를 반환합니다.
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

    // ServerManager에서 BaseUrl을 비동기로 가져옵니다.
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

    // kind에 따라 상태 말풍선을 갱신합니다.
    private void UpdateVlStatusBalloonByKind(string kind)
    {
        if (kind == "goal" || kind == "plan" || kind == "observe")
        {
            SetVlStatusBalloon("Think");
        }
        else if (kind == "check")
        {
            SetVlStatusBalloon("Verify");
        }
    }

    // 상태 말풍선을 spriteKey로 교체 표시합니다.
    private void SetVlStatusBalloon(string spriteKey)
    {
        ClearVlStatusBalloon();
        vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), spriteKey);
    }

    // 상태 말풍선을 제거합니다.
    private void ClearVlStatusBalloon()
    {
        if (vlStatusBalloon != null)
        {
            Destroy(vlStatusBalloon);
            vlStatusBalloon = null;
        }
    }

    // AnswerBalloonSimple에 시작 또는 재시작 텍스트를 표시합니다.
    private void ShowAnswerBalloonStartText(JArray thinkLog, string startText, string restartText)
    {
        AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        bool isResume = thinkLog != null && thinkLog.Count > 0;
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(isResume ? restartText : startText);
    }

    // 상대 좌표 + offset을 절대 좌표로 변환합니다.
    private (int absoluteX, int absoluteY) ConvertRelativeToAbsolute(int relativeX, int relativeY, int offsetX, int offsetY)
    {
        int absoluteX = relativeX + offsetX;
        int absoluteY = relativeY + offsetY;
        return (absoluteX, absoluteY);
    }

    // 상대 좌표를 절대 좌표로 변환 후 클릭 이펙트 및 실제 클릭을 수행합니다.
    private void ExecuteClickFromRelative(int relativeX, int relativeY, int offsetX, int offsetY, bool isMouseMove, string logPrefix)
    {
        (int absoluteX, int absoluteY) = ConvertRelativeToAbsolute(relativeX, relativeY, offsetX, offsetY);

        Debug.Log($"{logPrefix}: ({relativeX}, {relativeY}) + offset: ({offsetX}, {offsetY}) = ({absoluteX}, {absoluteY})");

        ShowClickPosition(absoluteX, absoluteY);
        ExecutorMouseAction.Instance.ClickAtPosition(absoluteX, absoluteY, isMouseMove);
    }

    // 클릭 마커 표시 코루틴
    private IEnumerator ShowClickPositionCoroutine(int winX, int winY, float duration)
    {
        GameObject marker = new GameObject("VL_ClickMarker");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[VlAgent] Canvas를 찾을 수 없음 - 마커 생성 실패");
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

        Debug.Log($"[VlAgent] 마커 표시: Windows({winX}, {winY}) → Unity({unityScreenX}, {unityScreenY}) → Local({localPoint.x}, {localPoint.y})");

        if (fx_click != null)
        {
            Vector3 worldPos = canvas.transform.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0));
            fx_click.transform.position = worldPos;
            fx_click.Play();
            Debug.Log($"[VlAgent] 이펙트 재생: World({worldPos.x}, {worldPos.y}, {worldPos.z})");
        }

        yield return new WaitForSeconds(duration);

        if (marker != null)
        {
            Destroy(marker);
        }
    }
}
