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

    private IEnumerator ExecuteVlAgentTestCoroutine(string target, int maxResults, Action<JObject> onComplete)
    {
        byte[] imageBytes = null;
        
        // 캡처 영역의 좌상단 offset (전체화면인 경우 0, 0)
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        // 1. Screenshot 영역 체크
        if (ScreenshotManager.Instance.IsScreenshotAreaSet())
        {
            Debug.Log("[VlAgent] Screenshot 영역 캡처");
            
            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureScreenshotToMemoryWithInfo((bytes, x, y, w, h) =>
            {
                imageBytes = bytes;
                captureOffsetX = x;  // 영역 좌상단 X
                captureOffsetY = y;  // 영역 좌상단 Y
                captureComplete = true;
                Debug.Log($"[VlAgent] 캡처 offset: ({x}, {y}), size: {w}x{h}");
            });
            while (!captureComplete) yield return null;
        }
        // 2. 전체화면 캡처
        else
        {
            Debug.Log("[VlAgent] 전체화면 캡처");
            
            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureFullScreenToMemory((bytes) =>
            {
                imageBytes = bytes;
                captureOffsetX = 0;  // 전체화면은 offset 없음
                captureOffsetY = 0;
                captureComplete = true;
            });
            while (!captureComplete) yield return null;
        }

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
        // BaseUrl 가져오기
        string baseUrl = null;
        bool urlComplete = false;
        ServerManager.Instance.GetBaseUrl((url) =>
        {
            baseUrl = url;
            urlComplete = true;
        });
        while (!urlComplete) yield return null;

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

                var result = response["result"];
                if (result == null)
                {
                    Debug.LogError("[VlAgent] result 필드 없음");
                    onComplete?.Invoke(false, 0, 0);
                    return;
                }

                var variables = result["variables"];
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

                // 응답 JSON에서 캡처 offset 읽기 (스레드 안전)
                int offsetX = response["_captureOffsetX"]?.Value<int>() ?? 0;
                int offsetY = response["_captureOffsetY"]?.Value<int>() ?? 0;

                // 이미지 내 상대 좌표 + 캡처 영역 offset = 실제 화면 좌표
                int absoluteX = x.Value + offsetX;
                int absoluteY = y.Value + offsetY;

                Debug.Log($"[VlAgent] 상대좌표: ({x.Value}, {y.Value}) + offset: ({offsetX}, {offsetY}) = 절대좌표: ({absoluteX}, {absoluteY}), isMouseMove={isMouseMove}");
                
                // 디버그: 클릭 위치 시각화
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
        string query,                       // 자연어 쿼리 (예: "노란 버튼을 클릭해줘")
        Action<JObject> onEvent = null,     // 스트리밍 이벤트 수신 콜백
        Action<bool, string> onComplete = null,  // 완료 콜백 (성공 여부, 에러 메시지)
        int maxRetry = 5                    // 최대 재요청 횟수
    )
    {
        // 콜백 저장 (재요청 시 사용)
        currentOnEvent = onEvent;
        currentOnComplete = onComplete;
        isCanceled = false;  // 취소 플래그 초기화
        
        // memory는 MemoryManager에서 직접 가져옴
        var memoryList = MemoryManager.Instance.GetAllConversationMemory();
        string memoryJson = JsonConvert.SerializeObject(memoryList);
        
        // 첫 요청: think_log 없음, retry_count=0
        StartCoroutine(ExecuteVlPlanerRunCoroutine(query, memoryJson, null, 0, maxRetry, onEvent, onComplete));
        
        // 전송시작 말풍선 표시
        if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
        vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Question");
    }
    
    // 작업 취소 요청
    public void CancelVlPlanerRun()
    {
        isCanceled = true;
        shouldRequestNewObservation = false;
        pendingThinkLog = null;
        Debug.Log("[VlPlanerRun] 취소 요청됨");
    }

    // 재요청용 (think_log 포함)
    private void ExecuteVlPlanerRunWithThinkLog(JArray thinkLog, int retryCount, int maxRetry)
    {
        // 재요청: query와 memory는 빈 값, retry_count/max_retry 전달
        StartCoroutine(ExecuteVlPlanerRunCoroutine("", "", thinkLog, retryCount, maxRetry, currentOnEvent, currentOnComplete));
    }

    private IEnumerator ExecuteVlPlanerRunCoroutine(string query, string memoryJson, JArray thinkLog, int retryCount, int maxRetry, Action<JObject> onEvent, Action<bool, string> onComplete)
    {
        byte[] imageBytes = null;
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        // 1. Screenshot 영역 체크
        if (ScreenshotManager.Instance.IsScreenshotAreaSet())
        {
            Debug.Log("[VlPlanerRun] Screenshot 영역 캡처");
            
            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureScreenshotToMemoryWithInfo((bytes, x, y, w, h) =>
            {
                imageBytes = bytes;
                captureOffsetX = x;
                captureOffsetY = y;
                captureComplete = true;
                Debug.Log($"[VlPlanerRun] 캡처 offset: ({x}, {y}), size: {w}x{h}");
            });
            while (!captureComplete) yield return null;
        }
        // 2. 전체화면 캡처
        else
        {
            Debug.Log("[VlPlanerRun] 전체화면 캡처");
            
            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureFullScreenToMemory((bytes) =>
            {
                imageBytes = bytes;
                captureOffsetX = 0;
                captureOffsetY = 0;
                captureComplete = true;
            });
            while (!captureComplete) yield return null;
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("[VlPlanerRun] 화면 캡처 실패");
            onComplete?.Invoke(false, "화면 캡처 실패");
            yield break;
        }

        // offset 저장 (이벤트 처리 시 사용)
        currentOffsetX = captureOffsetX;
        currentOffsetY = captureOffsetY;

        bool isResume = thinkLog != null && thinkLog.Count > 0;
        Debug.Log($"[VlPlanerRun] 캡처 완료: {imageBytes.Length} bytes, {(isResume ? "재요청" : "첫 요청")}");

        // API 호출 (스트리밍)
        yield return CallVlPlanerRunAPI(query, memoryJson, thinkLog, retryCount, maxRetry, imageBytes, captureOffsetX, captureOffsetY, onEvent, onComplete);
    }

    private IEnumerator CallVlPlanerRunAPI(string query, string memoryJson, JArray thinkLog, int retryCount, int maxRetry, byte[] imageBytes, int offsetX, int offsetY, Action<JObject> onEvent, Action<bool, string> onComplete)
    {
        // 상태 표시 - 시작
        AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        bool isResume = thinkLog != null && thinkLog.Count > 0;
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(isResume ? "VL Planer 재시작..." : "VL Planer 시작...");

        // BaseUrl 가져오기
        string baseUrl = null;
        bool urlComplete = false;
        ServerManager.Instance.GetBaseUrl((url) =>
        {
            baseUrl = url;
            urlComplete = true;
        });
        while (!urlComplete) yield return null;

        string apiUrl = baseUrl + "/vl_agent/run";
        Debug.Log($"[VlPlanerRun] API 호출: {apiUrl}");

        // think_log JSON 직렬화
        string thinkLogJson = thinkLog != null ? thinkLog.ToString(Formatting.None) : "";

        // Task로 비동기 스트리밍 요청 처리 (이벤트 큐와 함께)
        var eventQueue = new System.Collections.Concurrent.ConcurrentQueue<JObject>();
        var task = Task.Run(() => SendVlPlanerRunRequestStateless(apiUrl, query, memoryJson, thinkLogJson, retryCount, maxRetry, isCanceled, imageBytes, (eventData) =>
        {
            eventQueue.Enqueue(eventData);
        }));
        
        // 이벤트 큐 처리 (메인 스레드에서)
        while (!task.IsCompleted)
        {
            // 큐에서 이벤트 꺼내서 처리
            while (eventQueue.TryDequeue(out JObject eventData))
            {
                ProcessVlPlanerEventStateless(eventData, offsetX, offsetY, onEvent);
            }
            yield return null;
        }

        // 남은 이벤트 처리
        while (eventQueue.TryDequeue(out JObject eventData))
        {
            ProcessVlPlanerEventStateless(eventData, offsetX, offsetY, onEvent);
        }

        if (task.Exception != null)
        {
            string errorMsg = task.Exception.InnerException?.Message ?? task.Exception.Message;
            Debug.LogError($"[VlPlanerRun] API 오류: {errorMsg}");
            
            // 상태 표시 - 실패
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"VL Planer 오류: {errorMsg}");
            
            onComplete?.Invoke(false, errorMsg);
            yield break;
        }

        var result = task.Result;

        // request_observation이면 재요청 처리
        if (shouldRequestNewObservation && pendingThinkLog != null)
        {
            shouldRequestNewObservation = false;
            Debug.Log("[VlPlanerRun] request_observation 수신 - 재요청 대기");
            
            // 1초 후 새 화면 캡처 및 재요청
            // 검증중 이펙트 (동작 확인을 위해 시간을 대기하며 서버에서 검증 중)
            if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
            vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Verify");
            
            yield return new WaitForSeconds(1.0f);
            
            Debug.Log("[VlPlanerRun] 재요청 시작");
            ExecuteVlPlanerRunWithThinkLog(pendingThinkLog, pendingRetryCount, pendingMaxRetry);
            yield break;  // 현재 코루틴 종료
        }
        
        // 상태 표시 - 완료 (말풍선 정리 후 결과 표시)
        if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
        
        // 성공/실패 말풍선 3초간 표시
        string resultSprite = result.success ? "Yes" : "No";
        EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), resultSprite, 3f, 0f);
        
        string statusText = result.success ? "완료" : "실패";
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"VL Planer {statusText}됨");
        
        onComplete?.Invoke(result.success, result.errorMsg);
    }

    // VL Planer 이벤트 처리 (Stateless 버전)
    private void ProcessVlPlanerEventStateless(JObject eventData, int offsetX, int offsetY, Action<JObject> onEvent)
    {
        string kind = (string)eventData["kind"] ?? "";
        string message = (string)eventData["message"] ?? "";
        
        // 상태 표시 업데이트
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"[{kind}] {message}");
        
        // 이벤트 종류별 이펙트 처리
        if (kind == "goal" || kind == "plan" || kind == "observe")
        {
            // 생각중 이펙트 (AI 서버 작업중)
            if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
            vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Think");
        }
        else if (kind == "check")
        {
            // 검증중 이펙트 (동작 확인을 위해 서버에서 검증 중)
            if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
            vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Verify");
        }
        
        // request_observation: 클릭 후 새 화면 필요
        if (kind == "request_observation")
        {
            var data = eventData["data"] as JObject;
            var thinkLog = eventData["think_log"] as JArray;
            
            Debug.Log($"[VlPlanerRun] request_observation 수신, think_log: {thinkLog?.Count ?? 0}개");
            
            // 요청 수행중 이펙트 (클릭이나 새로운 스크린샷 찍는 행위)
            if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
            vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Execute");
            
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
                        int absoluteX = x.Value + offsetX;
                        int absoluteY = y.Value + offsetY;
                        
                        Debug.Log($"[VlPlanerRun] 클릭 실행: ({x.Value}, {y.Value}) + offset: ({offsetX}, {offsetY}) = ({absoluteX}, {absoluteY})");
                        
                        // 클릭 이펙트 표시
                        ShowClickPosition(absoluteX, absoluteY);
                        
                        // 실제 클릭 수행
                        ExecutorMouseAction.Instance.ClickAtPosition(absoluteX, absoluteY, true);
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
        // done: 작업 완료
        else if (kind == "done")
        {
            Debug.Log("[VlPlanerRun] 작업 완료 (done)");
            shouldRequestNewObservation = false;
            pendingThinkLog = null;
            
            var data = eventData["data"] as JObject;
            if (data != null)
            {
                // request_type으로 요청 종류 구분
                // 상수값 (Python ai_vl_planer_functions.py와 동일):
                // - function_request_click: 클릭 요청
                // - function_play_sfx_alert: 알림 효과음 재생
                // - function_request_dance: 캐릭터 춤 요청
                // - function_request_screenshot: 스크린샷 저장 및 클립보드 복사
                string requestType = data["request_type"]?.Value<string>() ?? "";
                
                switch (requestType)
                {
                    case "function_request_click":
                        // 클릭 수행 (ONE_SHOT 완료 시)
                        var finalClick = data["final_click"] as JObject;
                        if (finalClick != null)
                        {
                            int? x = (int?)finalClick["x"];
                            int? y = (int?)finalClick["y"];
                            
                            if (x.HasValue && y.HasValue)
                            {
                                int absoluteX = x.Value + offsetX;
                                int absoluteY = y.Value + offsetY;
                                
                                Debug.Log($"[VlPlanerRun] 최종 클릭: ({x.Value}, {y.Value}) + offset = ({absoluteX}, {absoluteY})");
                                
                                ShowClickPosition(absoluteX, absoluteY);
                                ExecutorMouseAction.Instance.ClickAtPosition(absoluteX, absoluteY, true);
                            }
                        }
                        break;
                        
                    case "function_play_sfx_alert":
                        Debug.Log("[VlPlanerRun] 알림 효과음 재생 요청");
                        StartCoroutine(ScenarioCommonManager.Instance.Run_C99_Alert_from_planner());
                        break;
                        
                    case "function_request_dance":
                        string danceType = data["dance_type"]?.Value<string>() ?? "default";
                        Debug.Log($"[VlPlanerRun] 춤 요청: {danceType}");
                        AnimationManager.Instance.Dance();
                        break;
                        
                    case "function_request_screenshot":
                        Debug.Log("[VlPlanerRun] 스크린샷 요청");
                        // 전체화면 스크린샷 저장 (ScreenshotManager 사용)
                        ScreenshotManager.Instance.CaptureFullScreen();
                        // 저장된 스크린샷을 클립보드에 복사
                        string screenshotPath = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots", "panel_capture.png");
                        ClipboardManager.Instance.SetImageToClipboard(screenshotPath);
                        break;
                        
                    default:
                        if (!string.IsNullOrEmpty(requestType))
                        {
                            Debug.LogWarning($"[VlPlanerRun] 알 수 없는 request_type: {requestType}");
                        }
                        break;
                }
            }
        }
        // fail: 작업 실패
        else if (kind == "fail")
        {
            Debug.Log("[VlPlanerRun] 작업 실패 (fail)");
            shouldRequestNewObservation = false;
            pendingThinkLog = null;
        }
        // act: 중간 액션 (grounding 결과 미리보기)
        else if (kind == "act")
        {
            var data = eventData["data"] as JObject;
            if (data != null)
            {
                // grounding 결과 candidates가 있으면 첫 번째 좌표에 이펙트 표시
                var result = data["result"] as JObject;
                if (result != null)
                {
                    var candidates = result["candidates"] as JArray;
                    if (candidates != null && candidates.Count > 0)
                    {
                        var firstCandidate = candidates[0] as JObject;
                        if (firstCandidate != null)
                        {
                            int? x = (int?)firstCandidate["x"];
                            int? y = (int?)firstCandidate["y"];
                            
                            if (x.HasValue && y.HasValue)
                            {
                                int absoluteX = x.Value + offsetX;
                                int absoluteY = y.Value + offsetY;
                                
                                Debug.Log($"[VlPlanerRun] Grounding 결과 - 첫 번째 후보: ({x.Value}, {y.Value}) = ({absoluteX}, {absoluteY})");
                                
                                // 클릭 이펙트 표시 (미리보기)
                                ShowClickPosition(absoluteX, absoluteY, 1f);
                            }
                        }
                    }
                }
            }
        }
        
        // 외부 콜백 호출
        onEvent?.Invoke(eventData);
    }

    // VL Planer Run API 스트리밍 요청 전송 (Stateless 버전)
    private (bool success, string errorMsg) SendVlPlanerRunRequestStateless(string apiUrl, string query, string memoryJson, string thinkLogJson, int retryCount, int maxRetry, bool isCanceled, byte[] imageBytes, Action<JObject> onEvent)
    {
        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
        
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Timeout = 120000;  // 2분 타임아웃 (스트리밍용)

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

            // think_log 필드 (재요청 시 - Stateless 핵심!)
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
            writer.WriteLine(isCanceled ? "true" : "false");

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

                // 줄 단위로 스트리밍 이벤트 읽기
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        JObject eventData = JObject.Parse(line);
                        lastKind = eventData["kind"]?.Value<string>() ?? "";
                        
                        // 에러 메시지 추출
                        if (lastKind == "fail")
                        {
                            lastErrorMsg = eventData["message"]?.Value<string>() ?? "Unknown error";
                        }

                        // 이벤트 콜백 호출 (메인 스레드에서 실행되지 않으므로 주의)
                        onEvent?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[VlPlanerRun] 이벤트 파싱 오류: {ex.Message}, line={line}");
                    }
                }

                // 최종 결과 판단 (done, request_observation, max_retry_reached면 성공)
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

    #region VL Planer Run Special (Streaming, Stateless)

    // VL Planer Run Special 스트리밍 실행 (/vl_agent/run_special)
    public void ExecuteVlPlanerRunSpecial(
        string query,                       // 자연어 쿼리 (선택적)
        Action<JObject> onEvent = null,     // 스트리밍 이벤트 수신 콜백
        Action<bool, string> onComplete = null,  // 완료 콜백 (성공 여부, 에러 메시지)
        int maxRetry = 30                   // 최대 재요청 횟수 (Special은 기본값 30)
    )
    {
        // 콜백 저장 (재요청 시 사용)
        currentOnEvent = onEvent;
        currentOnComplete = onComplete;
        isCanceled = false;  // 취소 플래그 초기화
        
        // memory는 MemoryManager에서 직접 가져옴
        var memoryList = MemoryManager.Instance.GetAllConversationMemory();
        string memoryJson = JsonConvert.SerializeObject(memoryList);
        
        // 첫 요청: think_log 없음, retry_count=0
        StartCoroutine(ExecuteVlPlanerRunSpecialCoroutine(query, memoryJson, null, 0, maxRetry, onEvent, onComplete));
        
        // 전송시작 말풍선 표시
        if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
        vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Question");
    }

    // 재요청용 (think_log 포함)
    private void ExecuteVlPlanerRunSpecialWithThinkLog(JArray thinkLog, int retryCount, int maxRetry)
    {
        // 재요청: query와 memory는 빈 값, retry_count/max_retry 전달
        StartCoroutine(ExecuteVlPlanerRunSpecialCoroutine("", "", thinkLog, retryCount, maxRetry, currentOnEvent, currentOnComplete));
    }

    private IEnumerator ExecuteVlPlanerRunSpecialCoroutine(string query, string memoryJson, JArray thinkLog, int retryCount, int maxRetry, Action<JObject> onEvent, Action<bool, string> onComplete)
    {
        byte[] imageBytes = null;
        int captureOffsetX = 0;
        int captureOffsetY = 0;

        // 1. Screenshot 영역 체크
        if (ScreenshotManager.Instance.IsScreenshotAreaSet())
        {
            Debug.Log("[VlPlanerRunSpecial] Screenshot 영역 캡처");
            
            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureScreenshotToMemoryWithInfo((bytes, x, y, w, h) =>
            {
                imageBytes = bytes;
                captureOffsetX = x;
                captureOffsetY = y;
                captureComplete = true;
                Debug.Log($"[VlPlanerRunSpecial] 캡처 offset: ({x}, {y}), size: {w}x{h}");
            });
            while (!captureComplete) yield return null;
        }
        // 2. 전체화면 캡처
        else
        {
            Debug.Log("[VlPlanerRunSpecial] 전체화면 캡처");
            
            bool captureComplete = false;
            yield return ScreenshotManager.Instance.CaptureFullScreenToMemory((bytes) =>
            {
                imageBytes = bytes;
                captureOffsetX = 0;
                captureOffsetY = 0;
                captureComplete = true;
            });
            while (!captureComplete) yield return null;
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("[VlPlanerRunSpecial] 화면 캡처 실패");
            onComplete?.Invoke(false, "화면 캡처 실패");
            yield break;
        }

        // offset 저장 (이벤트 처리 시 사용)
        currentOffsetX = captureOffsetX;
        currentOffsetY = captureOffsetY;

        bool isResume = thinkLog != null && thinkLog.Count > 0;
        Debug.Log($"[VlPlanerRunSpecial] 캡처 완료: {imageBytes.Length} bytes, {(isResume ? "재요청" : "첫 요청")}");

        // API 호출 (스트리밍)
        yield return CallVlPlanerRunSpecialAPI(query, memoryJson, thinkLog, retryCount, maxRetry, imageBytes, captureOffsetX, captureOffsetY, onEvent, onComplete);
    }

    private IEnumerator CallVlPlanerRunSpecialAPI(string query, string memoryJson, JArray thinkLog, int retryCount, int maxRetry, byte[] imageBytes, int offsetX, int offsetY, Action<JObject> onEvent, Action<bool, string> onComplete)
    {
        // 상태 표시 - 시작
        AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
        bool isResume = thinkLog != null && thinkLog.Count > 0;
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(isResume ? "VL Special 재시작..." : "VL Special 시작...");

        // BaseUrl 가져오기
        string baseUrl = null;
        bool urlComplete = false;
        ServerManager.Instance.GetBaseUrl((url) =>
        {
            baseUrl = url;
            urlComplete = true;
        });
        while (!urlComplete) yield return null;

        string apiUrl = baseUrl + "/vl_agent/run_special";
        Debug.Log($"[VlPlanerRunSpecial] API 호출: {apiUrl}");

        // think_log JSON 직렬화
        string thinkLogJson = thinkLog != null ? thinkLog.ToString(Formatting.None) : "";

        // Task로 비동기 스트리밍 요청 처리 (이벤트 큐와 함께)
        var eventQueue = new System.Collections.Concurrent.ConcurrentQueue<JObject>();
        var task = Task.Run(() => SendVlPlanerRunRequestStateless(apiUrl, query, memoryJson, thinkLogJson, retryCount, maxRetry, isCanceled, imageBytes, (eventData) =>
        {
            eventQueue.Enqueue(eventData);
        }));
        
        // 이벤트 큐 처리 (메인 스레드에서)
        while (!task.IsCompleted)
        {
            // 큐에서 이벤트 꺼내서 처리
            while (eventQueue.TryDequeue(out JObject eventData))
            {
                ProcessVlPlanerEventStateless(eventData, offsetX, offsetY, onEvent);
            }
            yield return null;
        }

        // 남은 이벤트 처리
        while (eventQueue.TryDequeue(out JObject eventData))
        {
            ProcessVlPlanerEventStateless(eventData, offsetX, offsetY, onEvent);
        }

        if (task.Exception != null)
        {
            string errorMsg = task.Exception.InnerException?.Message ?? task.Exception.Message;
            Debug.LogError($"[VlPlanerRunSpecial] API 오류: {errorMsg}");
            
            // 상태 표시 - 실패
            AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"VL Special 오류: {errorMsg}");
            
            onComplete?.Invoke(false, errorMsg);
            yield break;
        }

        var result = task.Result;

        // request_observation이면 재요청 처리
        if (shouldRequestNewObservation && pendingThinkLog != null)
        {
            shouldRequestNewObservation = false;
            Debug.Log("[VlPlanerRunSpecial] request_observation 수신 - 재요청 대기");
            
            // 1초 후 새 화면 캡처 및 재요청
            if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
            vlStatusBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Verify");
            
            yield return new WaitForSeconds(1.0f);
            
            Debug.Log("[VlPlanerRunSpecial] 재요청 시작");
            ExecuteVlPlanerRunSpecialWithThinkLog(pendingThinkLog, pendingRetryCount, pendingMaxRetry);
            yield break;
        }
        
        // 상태 표시 - 완료 (말풍선 정리 후 결과 표시)
        if (vlStatusBalloon != null) { Destroy(vlStatusBalloon); vlStatusBalloon = null; }
        
        // 성공/실패 말풍선 3초간 표시
        string resultSprite = result.success ? "Yes" : "No";
        EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), resultSprite, 3f, 0f);
        
        string statusText = result.success ? "완료" : "실패";
        AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"VL Special {statusText}됨");
        
        onComplete?.Invoke(result.success, result.errorMsg);
    }

    #endregion

    private IEnumerator ShowClickPositionCoroutine(int winX, int winY, float duration)
    {
        // 임시 마커 GameObject 생성
        GameObject marker = new GameObject("VL_ClickMarker");
        
        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[VlAgent] Canvas를 찾을 수 없음 - 마커 생성 실패");
            Destroy(marker);
            yield break;
        }

        marker.transform.SetParent(canvas.transform, false);

        // UI Image 추가
        var image = marker.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(1f, 0f, 0f, 0.7f);  // 반투명 빨간색
        image.raycastTarget = false;  // 클릭 통과 (마우스 이벤트 무시)

        // RectTransform 설정
        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(30, 30);  // 30x30 크기
        
        // Windows 좌표 → Unity 스크린 좌표 변환
        // Windows: (0,0)이 좌상단, Y가 아래로 증가
        // Unity Screen: (0,0)이 좌하단, Y가 위로 증가
        float unityScreenX = winX;
        float unityScreenY = Screen.height - winY;

        // 스크린 좌표 → 캔버스 로컬 좌표 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            new Vector2(unityScreenX, unityScreenY),
            canvas.worldCamera,
            out Vector2 localPoint
        );

        rt.anchoredPosition = localPoint;

        Debug.Log($"[VlAgent] 마커 표시: Windows({winX}, {winY}) → Unity({unityScreenX}, {unityScreenY}) → Local({localPoint.x}, {localPoint.y})");

        // 파티클 이펙트 재생 (설정된 경우)
        if (fx_click != null)
        {
            // Canvas 로컬 좌표 → World 좌표 변환
            Vector3 worldPos = canvas.transform.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0));
            fx_click.transform.position = worldPos;
            fx_click.Play();
            Debug.Log($"[VlAgent] 이펙트 재생: World({worldPos.x}, {worldPos.y}, {worldPos.z})");
        }

        // 지정된 시간 후 삭제
        yield return new WaitForSeconds(duration);
        
        if (marker != null)
        {
            Destroy(marker);
        }
    }
}
