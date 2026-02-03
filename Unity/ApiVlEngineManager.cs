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

public class ApiVlEngineManager : MonoBehaviour
{
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
        bool verbose = true
    )
    {
        isCanceled = false;
        StartCoroutine(ExecuteVlEngineCoroutine(
            query, previousThinkLog, previousAgentState,
            onEvent, onComplete, retryCount, maxRetry, verbose
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
        bool verbose
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
            retryCount, maxRetry, verbose, captureOffsetX, captureOffsetY,
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

            string stateInfo = string.IsNullOrEmpty(expectedStateDisplay) ? "" : $" (expected: {expectedStateDisplay})";
            string waitMessage = $"[VlEngine] {kind} 수신{stateInfo} - {retryInterval}초 후 재요청";
            Debug.Log(waitMessage);
            ProcessVlMessage(waitMessage);

            // retry_interval 대기
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
                verbose: verbose
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
        int offsetX,
        int offsetY,
        Action<JObject> onEvent,
        Action<JObject> onLastEvent
    )
    {
        // BaseUrl 가져오기
        string baseUrl = null;
        yield return GetBaseUrlCoroutine((url) => { baseUrl = url; });

        string apiUrl = baseUrl + "/vl_agent/engine_stream";
        Debug.Log($"[VlEngine] API 호출: {apiUrl}");

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
            (eventData) => { eventQueue.Enqueue(eventData); }
        ));

        // 이벤트 큐 처리 (메인 스레드에서)
        JObject lastEvent = null;
        while (!task.IsCompleted)
        {
            while (eventQueue.TryDequeue(out JObject eventData))
            {
                ProcessEngineEvent(eventData, offsetX, offsetY, onEvent);
                lastEvent = eventData;
            }
            yield return null;
        }

        // 남은 이벤트 처리
        while (eventQueue.TryDequeue(out JObject eventData))
        {
            ProcessEngineEvent(eventData, offsetX, offsetY, onEvent);
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

    // Engine 이벤트 처리 (act 이벤트만 클릭 실행 + thinking 이벤트는 NoticeManager)
    private void ProcessEngineEvent(JObject eventData, int offsetX, int offsetY, Action<JObject> onEvent)
    {
        string kind = eventData["kind"]?.Value<string>() ?? "";
        string message = eventData["message"]?.Value<string>() ?? "";

        string eventMessage = $"[VlEngine] 이벤트: [{kind}] {message}";
        Debug.Log(eventMessage);
        ProcessVlMessage(eventMessage);

        // thinking 이벤트: NoticeManager로 안내 말풍선 표시
        if (kind == "thinking")
        {
            NoticeManager.Instance.Notice("thinking");
        }
        // act 이벤트: 클릭 또는 alert 실행
        else if (kind == "act")
        {
            var data = eventData["data"] as JObject;
            if (data != null)
            {
                string action = data["action"]?.Value<string>() ?? "";
                if (action == "click")
                {
                    int? x = data["x"]?.Value<int>();
                    int? y = data["y"]?.Value<int>();

                    if (x.HasValue && y.HasValue)
                    {
                        ExecuteClickFromRelative(x.Value, y.Value, offsetX, offsetY, true, "[VlEngine]");
                    }
                }
                else if (action == "alert")
                {
                    Debug.Log("[VlEngine] alert 액션 - 알림 효과음 재생 (TODO)");
                    // TODO: 알림 효과음 재생 로직
                }
            }
        }

        // 외부 콜백 호출
        onEvent?.Invoke(eventData);
    }

    #endregion

    #region 유틸리티 메서드

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
