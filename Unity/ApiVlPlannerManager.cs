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

public class ApiVlPlannerManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static ApiVlPlannerManager instance;
    public static ApiVlPlannerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiVlPlannerManager>();
            }
            return instance;
        }
    }

    [Header("Click Effect")]
    [SerializeField] private ParticleSystem fx_click; // 클릭 이펙트

    // Stateless 재요청용 상태 (Unity가 들고 있음)
    private JArray pendingThinkLog = null;  // 재요청용 think_log
    private float pendingCaptureDelaySec = 1.0f;  // 캡처 지연 시간
    private bool shouldRequestNewObservation = false;  // 재요청 플래그
    private int currentOffsetX = 0;  // 현재 캡처 offset X
    private int currentOffsetY = 0;  // 현재 캡처 offset Y
    private int pendingRetryCount = 0;  // 재요청 횟수
    private int pendingMaxRetry = 5;  // 최대 재요청 횟수
    private bool isCanceled = false;  // 취소 요청
    private Action<JObject> currentOnEvent = null;  // 이벤트 콜백
    private Action<bool, string> currentOnComplete = null;  // 완료 콜백
    private bool isVerbose = true;  // 디버그/상세 로그
    private GameObject vlStatusBalloon = null;  // VL Planner 상태 표시용 말풍선

    #region VL 메시지 처리

    // VL Planner 메시지 처리 (AnswerBalloonSimple + DebugBalloonManager2 둘 다 업데이트)
    private void ProcessVlMessage(string message)
    {
        // AnswerBalloonSimple에 표시
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(message);

        // DebugBalloonManager2에 기록
        DebugBalloonManager2.Instance.AddVlAgentLog(message);
    }

    #endregion

    #region VL Planner Run (Streaming, Stateless)

    // VL Planner Run 스트리밍 실행 (/vl_agent/run)
    public void ExecuteVlPlannerRun(
        string query,
        Action<JObject> onEvent = null,
        Action<bool, string> onComplete = null,
        int maxRetry = 5
    )
    {
        // 콜백 및 취소 플래그 초기화
        currentOnEvent = onEvent;
        currentOnComplete = onComplete;
        isCanceled = false;

        // 새 실행이면 재요청 상태 초기화
        pendingThinkLog = null;
        pendingCaptureDelaySec = 1.0f;
        shouldRequestNewObservation = false;
        pendingRetryCount = 0;
        pendingMaxRetry = maxRetry;

        // 메모리 확보 및 JSON 직렬화
        var memoryList = MemoryManager.Instance.GetAllConversationMemory();
        string memoryJson = JsonConvert.SerializeObject(memoryList);

        // 상태 표시 및 코루틴 시작
        SetVlStatusBalloon("Question");
        StartCoroutine(ExecutePlannerRunCoroutine(query, memoryJson, null, 0, maxRetry, "VL Planner 시작...", onEvent, onComplete));
    }

    // 작업 취소 요청
    public void CancelVlPlannerRun()
    {
        // 취소 플래그 설정 및 상태 초기화
        isCanceled = true;
        shouldRequestNewObservation = false;
        pendingThinkLog = null;
        pendingCaptureDelaySec = 1.0f;
        Debug.Log("[VlPlannerRun] 취소 요청됨");
    }

    // think_log 포함 재요청
    private void ExecutePlannerRunWithResumeState(JArray thinkLog, float captureDelaySec, int retryCount, int maxRetry)
    {
        // 캡처 지연 시간 설정 및 코루틴 재시작
        pendingCaptureDelaySec = captureDelaySec;
        StartCoroutine(ExecutePlannerRunCoroutine("", "", thinkLog, retryCount, maxRetry, "VL Planner 재시작...", currentOnEvent, currentOnComplete));
    }

    // Planner Run 코루틴: 캡처 후 스트리밍 API 호출
    private IEnumerator ExecutePlannerRunCoroutine(
        string query,
        string memoryJson,
        JArray thinkLog,
        int retryCount,
        int maxRetry,
        string startText,
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
                Debug.LogError($"[VlPlannerRun] {failMsg}");
                onComplete?.Invoke(false, "화면 캡처 실패");
            },
            "[VlPlannerRun]"
        );

        // 캡처 실패 시 종료
        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("[VlPlannerRun] 화면 캡처 실패");
            onComplete?.Invoke(false, "화면 캡처 실패");
            yield break;
        }

        // offset 저장 (이벤트 처리 시 사용)
        currentOffsetX = captureOffsetX;
        currentOffsetY = captureOffsetY;

        // 재요청 여부 판단 및 로그
        bool isResume = thinkLog != null && thinkLog.Count > 0;
        Debug.Log($"[VlPlannerRun] 캡처 완료: {imageBytes.Length} bytes, {(isResume ? "재요청" : "첫 요청")}");

        // API 호출 (스트리밍)
        yield return CallPlannerRunStreamingApi(query, memoryJson, thinkLog, retryCount, maxRetry, imageBytes, captureOffsetX, captureOffsetY, startText, onEvent, onComplete);
    }

    // Planner Run 스트리밍 API 호출
    private IEnumerator CallPlannerRunStreamingApi(
        string query,
        string memoryJson,
        JArray thinkLog,
        int retryCount,
        int maxRetry,
        byte[] imageBytes,
        int offsetX,
        int offsetY,
        string startText,
        Action<JObject> onEvent,
        Action<bool, string> onComplete
    )
    {
        // 상태 표시 - 시작
        ShowAnswerBalloonStartText(thinkLog, startText);

        // BaseUrl 확보
        string baseUrl = null;
        yield return GetBaseUrlCoroutine((url) =>
        {
            baseUrl = url;
        });

        string apiUrl = baseUrl + "/vl_agent/run";
        Debug.Log($"[VlPlannerRun] API 호출: {apiUrl}");

        // think_log JSON 직렬화
        string thinkLogJson = thinkLog != null ? thinkLog.ToString(Formatting.None) : "";

        // Task로 비동기 스트리밍 요청 처리
        var eventQueue = new System.Collections.Concurrent.ConcurrentQueue<JObject>();
        Func<bool> isCanceledProvider = () => isCanceled;

        var task = Task.Run(() => SendPlannerRunRequest(
            apiUrl,
            query,
            memoryJson,
            thinkLogJson,
            retryCount,
            maxRetry,
            isCanceledProvider,
            imageBytes,
            isVerbose,
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
                ProcessPlannerEvent(eventData, offsetX, offsetY, onEvent);
            }
            yield return null;
        }

        // 남은 이벤트 처리
        while (eventQueue.TryDequeue(out JObject eventData))
        {
            ProcessPlannerEvent(eventData, offsetX, offsetY, onEvent);
        }

        // 예외 처리
        if (task.Exception != null)
        {
            string errorMsg = task.Exception.InnerException?.Message ?? task.Exception.Message;
            Debug.LogError($"[VlPlannerRun] API 오류: {errorMsg}");
            ClearVlStatusBalloon();
            ProcessVlMessage($"VL Planner 오류: {errorMsg}");
            onComplete?.Invoke(false, errorMsg);
            yield break;
        }

        var result = task.Result;

        // wait 이벤트면 재요청 처리
        if (shouldRequestNewObservation && pendingThinkLog != null)
        {
            shouldRequestNewObservation = false;
            Debug.Log($"[VlPlannerRun] wait 수신 - 재요청 대기");

            // 검증중 이펙트
            SetVlStatusBalloon("Verify");

            // 지연 시간 대기
            float waitSec = pendingCaptureDelaySec > 0.0f ? pendingCaptureDelaySec : 1.0f;
            yield return new WaitForSeconds(waitSec);

            // 재요청 시작
            Debug.Log($"[VlPlannerRun] 재요청 시작 (delay={waitSec})");
            ExecutePlannerRunWithResumeState(pendingThinkLog, pendingCaptureDelaySec, pendingRetryCount, pendingMaxRetry);
            yield break;
        }

        // 상태 표시 - 완료
        ClearVlStatusBalloon();

        // 성공/실패 말풍선 3초간 표시
        string resultSprite = result.success ? "Yes" : "No";
        EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), resultSprite, 3f, 0f);

        // 완료 메시지 표시
        string statusText = result.success ? "완료" : "실패";
        ProcessVlMessage($"VL Planner {statusText}됨");

        // 완료 콜백 호출
        onComplete?.Invoke(result.success, result.errorMsg);
    }

    // Planner 이벤트 처리
    private void ProcessPlannerEvent(JObject eventData, int offsetX, int offsetY, Action<JObject> onEvent)
    {
        // 이벤트 정보 추출
        string kind = (string)eventData["kind"] ?? "";
        string message = (string)eventData["message"] ?? "";

        // 이벤트 전체 JSON 로그 (디버깅용)
        Debug.Log($"[VlPlannerRun] 이벤트 전체 JSON: {eventData.ToString()}");
        Debug.Log($"[VlPlannerRun] 현재 offset: ({offsetX}, {offsetY})");

        // 상태 표시 업데이트
        ProcessVlMessage($"[{kind}] {message}");

        // 이벤트 종류별 이펙트 처리
        UpdateVlStatusBalloonByKind(kind);

        // 이벤트별 처리
        switch (kind)
        {
            case "wait":
                HandleWaitEvent(eventData);
                break;
            case "done":
                HandleDoneEvent(eventData, offsetX, offsetY);
                break;
            case "fail":
                HandleFailEvent();
                break;
            case "act":
                HandleActEvent(eventData, offsetX, offsetY);
                break;
            case "max_retry_reached":
                HandleMaxRetryReached();
                break;
        }

        // 외부 콜백 호출
        onEvent?.Invoke(eventData);
    }

    // wait 이벤트: 재요청용 상태 저장 (think_log)
    private void HandleWaitEvent(JObject eventData)
    {
        var data = eventData["data"] as JObject;
        var thinkLog = eventData["think_log"] as JArray;

        Debug.Log($"[VlPlannerRun] wait 수신, think_log: {thinkLog?.Count ?? 0}개");

        // data 전체 출력
        if (data != null)
        {
            Debug.Log($"[VlPlannerRun] wait 이벤트 data: {data.ToString()}");
        }

        // 요청 수행중 이펙트
        SetVlStatusBalloon("Execute");

        if (data != null)
        {
            // retry_count, max_retry 저장
            pendingRetryCount = data["retry_count"]?.Value<int>() ?? 0;
            pendingMaxRetry = data["max_retry"]?.Value<int>() ?? 5;
            Debug.Log($"[VlPlannerRun] retry_count={pendingRetryCount}, max_retry={pendingMaxRetry}");

            // 캡처 지연 시간 파싱
            float delaySec = 1.0f;
            if (data["capture_delay_sec"] != null)
            {
                try
                {
                    delaySec = data["capture_delay_sec"].Value<float>();
                }
                catch (Exception)
                {
                    delaySec = 1.0f;
                }
            }
            pendingCaptureDelaySec = delaySec;
            Debug.Log($"[VlPlannerRun] capture_delay_sec={pendingCaptureDelaySec}");
        }

        // think_log 저장 및 재요청 플래그 설정
        pendingThinkLog = thinkLog;
        shouldRequestNewObservation = true;
    }

    // done 이벤트: 작업 완료 시 함수 호출 처리
    private void HandleDoneEvent(JObject eventData, int offsetX, int offsetY)
    {
        Debug.Log("[VlPlannerRun] 작업 완료 (done)");

        // 재요청 상태 초기화
        shouldRequestNewObservation = false;
        pendingThinkLog = null;
        pendingCaptureDelaySec = 1.0f;

        var data = eventData["data"] as JObject;
        if (data == null)
        {
            Debug.LogWarning("[VlPlannerRun] done 이벤트의 data 필드가 null입니다!");
            return;
        }

        // data 전체 출력
        Debug.Log($"[VlPlannerRun] done 이벤트 data: {data.ToString()}");

        // action 필드 확인 (새 API는 action 사용)
        string action = data["action"]?.Value<string>() ?? "";
        Debug.Log($"[VlPlannerRun] action: {action}");

        // action에 따라 처리
        switch (action)
        {
            case "click":
            {
                int? x = data["x"]?.Value<int>();
                int? y = data["y"]?.Value<int>();
                Debug.Log($"[VlPlannerRun] click - x={x}, y={y}, offset=({offsetX}, {offsetY})");
                
                if (x.HasValue && y.HasValue)
                {
                    ExecuteClickFromRelative(x.Value, y.Value, offsetX, offsetY, true, "[VlPlannerRun] 최종 클릭 (done)");
                }
                else
                {
                    Debug.LogWarning($"[VlPlannerRun] click 좌표가 없습니다! x={x}, y={y}");
                }
                break;
            }
            case "play_sfx_alert":
            {
                Debug.Log("[VlPlannerRun] 알림 효과음 재생 요청");
                StartCoroutine(ScenarioCommonManager.Instance.Run_C99_Alert_from_planner());
                break;
            }
            case "dance":
            {
                Debug.Log("[VlPlannerRun] 춤 요청");
                AnimationManager.Instance.Dance();
                break;
            }
            case "screenshot":
            {
                Debug.Log("[VlPlannerRun] 스크린샷 요청");
                ScreenshotManager.Instance.CaptureFullScreen();
                string screenshotPath = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots", "panel_capture.png");
                ClipboardManager.Instance.SetImageToClipboard(screenshotPath);
                break;
            }
            default:
            {
                if (!string.IsNullOrEmpty(action))
                {
                    Debug.LogWarning($"[VlPlannerRun] 알 수 없는 action: {action}");
                }
                else
                {
                    Debug.Log("[VlPlannerRun] action이 비어있습니다. 별도 액션 없이 완료되었습니다.");
                }
                break;
            }
        }
    }

    // fail 이벤트: 실패 시 재요청 상태 정리
    private void HandleFailEvent()
    {
        Debug.Log("[VlPlannerRun] 작업 실패 (fail)");

        // 재요청 상태 초기화
        shouldRequestNewObservation = false;
        pendingThinkLog = null;
        pendingCaptureDelaySec = 1.0f;
    }

    // max_retry_reached 이벤트: 최대 재시도 도달
    private void HandleMaxRetryReached()
    {
        Debug.LogWarning("[VlPlannerRun] 최대 재시도 횟수 도달");

        // 재요청 상태 초기화
        shouldRequestNewObservation = false;
        pendingThinkLog = null;
        pendingCaptureDelaySec = 1.0f;
    }

    // act 이벤트: 액션 실행 (클릭, Grounding 등)
    private void HandleActEvent(JObject eventData, int offsetX, int offsetY)
    {
        var data = eventData["data"] as JObject;
        if (data == null)
        {
            Debug.LogWarning("[VlPlannerRun] act 이벤트의 data 필드가 null입니다!");
            return;
        }

        // data 전체 출력
        Debug.Log($"[VlPlannerRun] act 이벤트 data: {data.ToString()}");

        string functionName = data["function_name"]?.Value<string>() ?? "";
        Debug.Log($"[VlPlannerRun] act function_name: {functionName}");

        // REQUEST_CLICK: 클릭 수행
        if (functionName == "REQUEST_CLICK")
        {
            int? x = data["x"]?.Value<int>();
            int? y = data["y"]?.Value<int>();
            Debug.Log($"[VlPlannerRun] REQUEST_CLICK (act) - x={x}, y={y}, offset=({offsetX}, {offsetY})");
            
            if (x.HasValue && y.HasValue)
            {
                ExecuteClickFromRelative(x.Value, y.Value, offsetX, offsetY, true, "[VlPlannerRun] 클릭 실행 (act)");
            }
            else
            {
                Debug.LogWarning($"[VlPlannerRun] REQUEST_CLICK (act) 좌표가 없습니다! x={x}, y={y}");
            }
            return;
        }

        // VL_TARGET_FIND: Grounding 결과 미리보기
        if (functionName == "VL_TARGET_FIND")
        {
            var result = data["result"] as JObject;
            if (result == null)
            {
                Debug.LogWarning("[VlPlannerRun] VL_TARGET_FIND result가 null입니다!");
                return;
            }

            Debug.Log($"[VlPlannerRun] VL_TARGET_FIND result: {result.ToString()}");

            bool exists = result["exists"]?.Value<bool>() ?? false;
            Debug.Log($"[VlPlannerRun] VL_TARGET_FIND exists: {exists}");
            
            if (!exists)
            {
                Debug.LogWarning("[VlPlannerRun] VL_TARGET_FIND 대상을 찾지 못했습니다!");
                return;
            }

            int? x = result["x"]?.Value<int>();
            int? y = result["y"]?.Value<int>();
            Debug.Log($"[VlPlannerRun] VL_TARGET_FIND - x={x}, y={y}, offset=({offsetX}, {offsetY})");
            
            if (x.HasValue && y.HasValue)
            {
                // 상대 좌표를 절대 좌표로 변환하여 미리보기
                (int absoluteX, int absoluteY) = ConvertRelativeToAbsolute(x.Value, y.Value, offsetX, offsetY);
                Debug.Log($"[VlPlannerRun] Grounding 결과: ({x.Value}, {y.Value}) = ({absoluteX}, {absoluteY})");
                ShowClickPosition(absoluteX, absoluteY, 1f);
            }
            else
            {
                Debug.LogWarning($"[VlPlannerRun] VL_TARGET_FIND 좌표가 없습니다! x={x}, y={y}");
            }
        }
    }

    // 스트리밍 요청 전송
    private (bool success, string errorMsg) SendPlannerRunRequest(
        string apiUrl,
        string query,
        string memoryJson,
        string thinkLogJson,
        int retryCount,
        int maxRetry,
        Func<bool> isCanceledProvider,
        byte[] imageBytes,
        bool verbose,
        Action<JObject> onEvent
    )
    {
        // multipart boundary 생성
        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");

        // HTTP 요청 생성
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Timeout = 120000;

        // multipart/form-data 작성
        using (MemoryStream memStream = new MemoryStream())
        using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
        {
            // query 필드 (첫 요청 시)
            if (!string.IsNullOrEmpty(query))
            {
                writer.WriteLine($"--{boundary}");
                writer.WriteLine("Content-Disposition: form-data; name=\"query\"");
                writer.WriteLine();
                writer.WriteLine(query);
            }

            // memory 필드 (첫 요청 시)
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

            // verbose 필드
            writer.WriteLine($"--{boundary}");
            writer.WriteLine("Content-Disposition: form-data; name=\"verbose\"");
            writer.WriteLine();
            writer.WriteLine(verbose ? "true" : "false");

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

            // image 필드 (이미지 바이너리)
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

                // JSON Lines 형식 응답을 한 줄씩 처리
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // 취소 요청 감지
                    if (isCanceledProvider != null && isCanceledProvider())
                    {
                        Debug.Log("[VlPlannerRun] 취소 감지 - 스트리밍 수신 중단");
                        return (false, "canceled");
                    }

                    // 빈 줄 건너뛰기
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // JSON 파싱 및 이벤트 처리
                    try
                    {
                        JObject eventData = JObject.Parse(line);
                        lastKind = eventData["kind"]?.Value<string>() ?? "";

                        // fail 이벤트 시 에러 메시지 저장
                        if (lastKind == "fail")
                        {
                            lastErrorMsg = eventData["message"]?.Value<string>() ?? "Unknown error";
                        }

                        // 이벤트 큐에 추가
                        onEvent?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[VlPlannerRun] 이벤트 파싱 오류: {ex.Message}, line={line}");
                    }
                }

                // 최종 결과 판단
                bool success = lastKind == "done" || lastKind == "wait" || lastKind == "max_retry_reached";
                return (success, success ? "" : lastErrorMsg);
            }
        }
        catch (WebException ex)
        {
            // 서버 에러 응답 처리
            if (ex.Response != null)
            {
                using (Stream errorStream = ex.Response.GetResponseStream())
                using (StreamReader errorReader = new StreamReader(errorStream))
                {
                    string errorResponse = errorReader.ReadToEnd();
                    Debug.LogError($"[VlPlannerRun] 서버 오류: {errorResponse}");
                    return (false, errorResponse);
                }
            }
            throw;
        }
    }

    #endregion

    #region 유틸리티 함수

    // Screenshot 영역 또는 전체화면 캡처
    private IEnumerator CaptureScreenToMemoryWithOffset(
        Action<byte[], int, int> onCaptured,
        Action<string> onFail,
        string logPrefix
    )
    {
        byte[] imageBytes = null;
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        // Screenshot 영역 설정 여부에 따라 분기
        if (ScreenshotManager.Instance.IsScreenshotAreaSet())
        {
            Debug.Log($"{logPrefix} Screenshot 영역 캡처");

            // 영역 캡처 (offset 포함)
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

            // 전체화면 캡처 (offset = 0)
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

        // 캡처 실패 시 에러 콜백 호출
        if (imageBytes == null || imageBytes.Length == 0)
        {
            onFail?.Invoke("화면 캡처 실패");
            yield break;
        }

        // 성공 시 결과 반환
        onCaptured?.Invoke(imageBytes, captureOffsetX, captureOffsetY);
    }

    // ServerManager에서 BaseUrl 비동기 획득
    private IEnumerator GetBaseUrlCoroutine(Action<string> onReady)
    {
        string baseUrl = null;
        bool urlComplete = false;

        // BaseUrl 요청
        ServerManager.Instance.GetBaseUrl((url) =>
        {
            baseUrl = url;
            urlComplete = true;
        });

        // 완료 대기
        while (!urlComplete)
        {
            yield return null;
        }

        // 결과 반환
        onReady?.Invoke(baseUrl);
    }

    // kind에 따라 상태 말풍선 갱신
    private void UpdateVlStatusBalloonByKind(string kind)
    {
        // 생각중 이벤트
        if (kind == "goal" || kind == "plan" || kind == "observe")
        {
            SetVlStatusBalloon("Think");
        }
        // 검증중 이벤트
        else if (kind == "check")
        {
            SetVlStatusBalloon("Verify");
        }
    }

    // 상태 말풍선 표시
    private void SetVlStatusBalloon(string spriteKey)
    {
        // 기존 말풍선 제거 후 새로 표시
        NoticeManager.Instance.ShowNoticeEmotionBalloon(spriteKey);
    }

    // 상태 말풍선 제거
    private void ClearVlStatusBalloon()
    {
        NoticeManager.Instance.DeleteNoticeBalloonInstance();
    }

    // AnswerBalloonSimple에 시작 텍스트 표시
    private void ShowAnswerBalloonStartText(JArray thinkLog, string startText)
    {
        // 무한 표시 모드로 시작
        AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();

        // 재요청 여부에 따라 텍스트 변경
        bool isResume = thinkLog != null && thinkLog.Count > 0;
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(isResume ? "VL Planner 재시작..." : startText);
    }

    // 상대 좌표를 절대 좌표로 변환
    private (int absoluteX, int absoluteY) ConvertRelativeToAbsolute(int relativeX, int relativeY, int offsetX, int offsetY)
    {
        int absoluteX = relativeX + offsetX;
        int absoluteY = relativeY + offsetY;
        return (absoluteX, absoluteY);
    }

    // 상대 좌표 클릭 실행
    private void ExecuteClickFromRelative(int relativeX, int relativeY, int offsetX, int offsetY, bool isMouseMove, string logPrefix)
    {
        // 상대 좌표를 절대 좌표로 변환
        (int absoluteX, int absoluteY) = ConvertRelativeToAbsolute(relativeX, relativeY, offsetX, offsetY);

        Debug.Log($"{logPrefix}: ({relativeX}, {relativeY}) + offset: ({offsetX}, {offsetY}) = ({absoluteX}, {absoluteY})");

        // 클릭 위치 표시 및 실행
        ShowClickPosition(absoluteX, absoluteY);
        ExecutorMouseAction.Instance.ClickAtPosition(absoluteX, absoluteY, isMouseMove);
    }

    // 클릭 위치 마커 표시
    public void ShowClickPosition(int winX, int winY, float duration = 2f)
    {
        Debug.Log($"[VlPlannerRun] ShowClickPosition: ({winX}, {winY})");
        StartCoroutine(ShowClickPositionCoroutine(winX, winY, duration));
    }

    // 클릭 마커 표시 코루틴
    private IEnumerator ShowClickPositionCoroutine(int winX, int winY, float duration)
    {
        // 마커 오브젝트 생성
        GameObject marker = new GameObject("VL_ClickMarker");

        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[VlPlannerRun] Canvas를 찾을 수 없음 - 마커 생성 실패");
            Destroy(marker);
            yield break;
        }

        // Canvas에 마커 추가
        marker.transform.SetParent(canvas.transform, false);

        // 빨간색 원형 이미지 생성
        var image = marker.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 0f, 0f, 0.7f);
        image.raycastTarget = false;

        // 크기 설정
        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(30, 30);

        // Windows 좌표를 Unity 스크린 좌표로 변환
        float unityScreenX = winX;
        float unityScreenY = Screen.height - winY;

        // 스크린 좌표를 Canvas 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            new Vector2(unityScreenX, unityScreenY),
            canvas.worldCamera,
            out Vector2 localPoint
        );

        rt.anchoredPosition = localPoint;

        Debug.Log($"[VlPlannerRun] 마커 표시: Windows({winX}, {winY}) → Unity({unityScreenX}, {unityScreenY}) → Local({localPoint.x}, {localPoint.y})");

        // 클릭 이펙트 재생
        if (fx_click != null)
        {
            Vector3 worldPos = canvas.transform.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0));
            fx_click.transform.position = worldPos;
            fx_click.Play();
            Debug.Log($"[VlPlannerRun] 이펙트 재생: World({worldPos.x}, {worldPos.y}, {worldPos.z})");
        }

        // duration 시간 동안 대기
        yield return new WaitForSeconds(duration);

        // 마커 제거
        if (marker != null)
        {
            Destroy(marker);
        }
    }

    #endregion
}
