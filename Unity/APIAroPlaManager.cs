using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;


[System.Serializable]
public class AroplaConversationResponse
{
    public AroplaReply[] reply_list; // APIManager와 동일한 구조
    public string speaker;          // 실제 응답한 캐릭터
    public string next_speaker;     // 다음 발화자 ("sensei", "arona", "plana")
    public string reasoning;        // 디버깅용 판단 근거
    public string ai_language_out;
    public string chat_idx;         // APIManager와 동일
    public string type;             // "reply", "waiting", "error" 등
}

[System.Serializable]
public class AroplaReply
{
    public string answer_jp;
    public string answer_ko; 
    public string answer_en;
}


public class APIAroPlaManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static APIAroPlaManager instance;
    public static APIAroPlaManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<APIAroPlaManager>();
            }
            return instance;
        }
    }


    private string GetFileName()
    {
        string filename = "aropla_conversation_memory.json";

        return Path.Combine(Application.persistentDataPath, filename);
    }
    
    // 아로프라 채널 상태
    private bool isAroplaMode = false;
    private bool isProcessing = false;
    private string logFilePath;
    
    // 아로프라 캐릭터 관리 (CharManager 방식 참고)
    [Header("Aropla Channel Settings")]
    public GameObject aronaPrefab;   // 인스펙터에서 설정할 아로나 프리팹
    public GameObject planaPrefab;   // 인스펙터에서 설정할 프라나 프리팹
    private GameObject aronaInstance;  // 생성된 아로나 인스턴스
    private GameObject planaInstance;  // 생성된 프라나 인스턴스
    private Canvas canvas;

    // 캐릭터 배치 상수
    private const float ARONA_OFFSET_X = -300f;  // 아로나는 왼쪽으로
    private const float ARONA_OFFSET_Y = 150f;   // 상단으로
    private const float PLANA_OFFSET_X = 300f;   // 프라나는 오른쪽으로  
    private const float PLANA_OFFSET_Y = 150f;   // 상단으로

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeAroplaChannel();
        }
        else
        {
            return;
        }
    }

    private void InitializeAroplaChannel()
    {
        // Canvas 초기화 (CharManager 방식 참고)
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            AroplaLogError("Canvas를 찾을 수 없습니다. 아로프라 채널이 제대로 작동하지 않을 수 있습니다.");
        }
        
        // 로그 파일 경로 생성
        string dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(Application.persistentDataPath, $"log/aropla_{dateTime}.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        File.AppendAllText(logFilePath, $"Aropla Channel Log started at: {DateTime.Now}\n");
    }

    private void LogToFile(string message)
    {
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
    }

    // 아로프라 전용 대화 저장 메서드 (다국어 지원)
    private void SaveAroplaConversationMemory(string speaker, string role, string message, string messageKo, string messageJp, string messageEn)
    {
        try
        {
            // MemoryManager의 새로운 오버로드 메서드 사용
            MemoryManager.Instance.SaveConversationMemory(speaker, role, message, messageKo, messageJp, messageEn, filename: GetFileName());
            
            LogToFile($"Saved to memory: {role} - {speaker} - {message}");
            AroplaLog($"Memory saved: {role}/{speaker} - {message.Substring(0, Math.Min(50, message.Length))}...");
        }
        catch (Exception ex)
        {
            AroplaLogError($"Failed to save conversation memory: {ex.Message}");
        }
    }

    // 아로프라 전용 디버그 로그 함수 (함수명 포함)
    private void AroplaLog(string message, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
    {
        Debug.Log($"[Aropla-{methodName}] {message}");
    }

    // 아로프라 전용 에러 로그 함수
    private void AroplaLogError(string message, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
    {
        Debug.LogError($"[Aropla-{methodName}] {message}");
    }

    // 아로프라 전용 경고 로그 함수
    private void AroplaLogWarning(string message, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
    {
        Debug.LogWarning($"[Aropla-{methodName}] {message}");
    }

    // 아로프라 참여자 목록 생성 (단순한 문자열 리스트)
    private List<string> CreateAroplaParticipants()
    {
        return new List<string> { "sensei", "arona", "plana" };
    }

    // 서브 캐릭터 위치 계산 (상수 기반)
    private Vector3 CalculateSubCharacterPosition(float offsetX, float offsetY)
    {
        GameObject currentCharacter = CharManager.Instance.GetCurrentCharacter();
        if (currentCharacter == null)
        {
            AroplaLogError("현재 캐릭터를 찾을 수 없어 기본 위치를 사용합니다.");
            return new Vector3(offsetX, offsetY, -70);
        }

        RectTransform currentCharRect = currentCharacter.GetComponent<RectTransform>();
        Vector3 currentPosition = currentCharRect != null ? currentCharRect.anchoredPosition3D : Vector3.zero;

        return new Vector3(
            currentPosition.x + offsetX,
            currentPosition.y + offsetY,
            currentPosition.z
        );
    }


    // 아로프라 채널 모드 시작
    public void StartAroplaChannel()
    {
        isAroplaMode = true;
        
        LogToFile("Aropla Channel Mode Started");
        
        // UI 초기화 - 아로나와 프라나 표시
        ShowAroplaChannelUI();
        
        // 항상 인사말 시작 (APIManager 방식과 동일)
        StartInitialGreeting();
    }

    // 아로프라 채널 모드 종료
    public void StopAroplaChannel()
    {
        isAroplaMode = false;
        LogToFile("Aropla Channel Mode Stopped");
        
        // 시스템 메시지로 아로프라 채널 종료를 기록
        // SaveAroplaConversationMemory("system", "system", "아로프라 채널이 종료되었습니다.", 
        //     "아로프라 채널이 종료되었습니다.", 
        //     "アロプラチャンネルが終了されました。", 
        //     "Aropla Channel has been stopped.");
        
        // 기존 단일 캐릭터 모드로 복귀
        HideAroplaChannelUI();
    }

    // 초기 인사말 시작 (아로나가 먼저 시작)
    private async void StartInitialGreeting()
    {
        // 시스템 메시지로 아로프라 채널 시작을 기록
        // SaveAroplaConversationMemory("system", "system", "아로프라 채널이 시작되었습니다.", 
        //     "아로프라 채널이 시작되었습니다.", 
        //     "アロプラチャンネルが開始されました。", 
        //     "Aropla Channel has been started.");
            
        // 아로나가 선생님에게 먼저 인사하게 함 (intent_smalltalk: on)
        await ProcessAroplaConversation("greeting", "arona", "arona", "on");
    }

    // 사용자 메시지 처리
    public async void SendUserMessage(string message)
    {
        if (!isAroplaMode)
        {
            AroplaLog("SendUserMessage cancelled - Aropla mode is not active");
            return;
        }
        
        if (isProcessing)
        {
            AroplaLog("SendUserMessage cancelled - Already processing a conversation");
            return;
        }

        AroplaLog("=== User Message Received ===");
        AroplaLog($"Message: {message}");
        AroplaLog("Target Speaker: Agent will decide");
        AroplaLog("===============================");
        
        LogToFile($"User message: {message}");
        
        // UI에 사용자 메시지 표시 (사용자 메시지는 단일 언어)
        DisplayMessage("sensei", message);
        
        // 대화 처리 시작
        await ProcessAroplaConversation(message, "sensei", null, "off"); // Agent가 결정하도록, 사용자 메시지는 smalltalk off
    }

    // 아로프라 대화 처리 메인 함수
    private async Task ProcessAroplaConversation(string message, string currentSpeaker, string targetSpeaker = null, string intentSmalltalk = "off")
    {
        if (isProcessing)
        {
            AroplaLog("ProcessAroplaConversation cancelled - Already processing a conversation");
            return;
        }
        
        isProcessing = true;
        
        try
        {
            // 아로프라 방식: 확장된 메모리 사용
            var memory = MemoryManager.Instance.GetAllConversationMemory(filename: GetFileName());
            string memoryJson = JsonConvert.SerializeObject(memory);
            
            // APIManager와 동일한 Dictionary<string, string> 방식으로 데이터 구성
            var requestData = new Dictionary<string, string>
            {
                { "query", message },
                { "player_name", SettingManager.Instance.settings.player_name },
                { "current_speaker", currentSpeaker },
                { "target_speaker", targetSpeaker ?? "" },
                { "participants", JsonConvert.SerializeObject(CreateAroplaParticipants()) },
                { "ai_language", SettingManager.Instance.settings.ai_language ?? "" },
                { "ai_emotion", SettingManager.Instance.settings.ai_emotion ?? "off" },
                { "memory", memoryJson },
                { "guideline_list", UIUserCardManager.Instance.GetGuidelineListJson() },
                { "situation", UIChatSituationManager.Instance.GetCurUIChatSituationInfoJson() },
                { "chat_idx", GameManager.Instance.chatIdxSuccess.ToString() },
                { "intent_smalltalk", intentSmalltalk }
            };

            // API 호출
            var response = await CallAroplaAPI(requestData);
            
            if (response != null)
            {
                // 응답 처리 (사용자 메시지 정보와 함께)
                ProcessAroplaResponse(response, message, currentSpeaker);
                
                // 다음 발화자가 AI 캐릭터인 경우 연속 처리
                if (response.next_speaker != "sensei")
                {
                    AroplaLog("=== Continuing Conversation ===");
                    AroplaLog($"Next AI Speaker: {response.next_speaker}");
                    AroplaLog("Will continue automatically in 1 second...");
                    AroplaLog("================================");
                    
                    // 연속 대화를 위해 isProcessing을 먼저 해제
                    isProcessing = false;
                    await ContinueAroplaConversation(response.speaker, response.next_speaker);
                    return; // finally 블록에서 중복 해제 방지
                }
                else
                {
                    AroplaLog("=== Waiting for User Input ===");
                    AroplaLog("Next speaker is sensei - conversation paused");
                    AroplaLog("================================");
                    
                    // 사용자 입력 대기
                }
            }
        }
        catch (Exception ex)
        {
            AroplaLogError($"Aropla conversation error: {ex.Message}");
            LogToFile($"Error: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }

    // 연속 대화 처리 (AI끼리 대화)
    private async Task ContinueAroplaConversation(string currentSpeaker, string targetSpeaker)
    {
        AroplaLog("=== Auto Continue Triggered ===");
        AroplaLog($"Current isProcessing state: {isProcessing}");
        AroplaLog("Waiting 1 second for natural flow...");
        
        await Task.Delay(1000); // 1초 대기 (자연스러운 흐름을 위해)
        
        if (targetSpeaker != "sensei")
        {
            // 마지막 메시지를 가져와서 전달 (MemoryManager 기반)
            string lastMessage = "";
            var memories = MemoryManager.Instance.GetAllConversationMemory(filename: GetFileName());
            if (memories.Count > 0)
            {
                var lastMemory = memories[memories.Count - 1];
                lastMessage = lastMemory.message ?? "";
                AroplaLog($"Using last message for context: {lastMessage}");
            }
            
            AroplaLog($"Current speaker was: {currentSpeaker}");
            AroplaLog($"Continuing with target speaker: {targetSpeaker}");
            AroplaLog("================================");
            await ProcessAroplaConversation(lastMessage, targetSpeaker, targetSpeaker, "off"); // 연속 대화는 smalltalk off, targetSpeaker가 발화
        }
        else
        {
            AroplaLog("Target speaker is sensei - stopping auto continue");
            AroplaLog("================================");
        }
    }

    // 스트리밍 완료 후 최종 응답 처리 (APIManager의 OnFinalResponseReceived 방식 참고)
    private void ProcessAroplaResponse(AroplaConversationResponse response, string userMessage = "", string userSpeaker = "")
    {
        AroplaLog("=== Processing Final Streaming Response ===");
        AroplaLog($"Current Speaker: {response.speaker}");
        AroplaLog($"Next Speaker: {response.next_speaker}");
        AroplaLog($"Response Type: {response.type ?? "reply"}");
        AroplaLog($"Chat IDX: {response.chat_idx}");
        if (!string.IsNullOrEmpty(response.reasoning))
        {
            AroplaLog($"AI Reasoning: {response.reasoning}");
        }
        AroplaLog("============================================");
        
        // 스트리밍에서는 이미 실시간으로 UI 업데이트가 완료되었으므로
        // 여기서는 최종 정리 작업만 수행 (APIManager의 OnFinalResponseReceived와 동일)
        
        // APIManager 방식: 표시 언어로 최종 메시지 조합 (replyList에서)
        string finalMessage = "";
        if (SettingManager.Instance.settings.ui_language == "ja" || SettingManager.Instance.settings.ui_language == "jp")
        {
            finalMessage = string.Join(" ", replyListJp);
        }
        else if (SettingManager.Instance.settings.ui_language == "ko")
        {
            finalMessage = string.Join(" ", replyListKo);
        }
        else
        {
            finalMessage = string.Join(" ", replyListEn);
        }
        
        AroplaLog($"Final message for history: {finalMessage}");
        LogToFile($"Final response from {response.speaker}: {finalMessage}");
        LogToFile($"Next speaker: {response.next_speaker}");
        
        // APIManager 방식: API 성공 후 사용자 메시지와 캐릭터 응답을 함께 저장
        string replyKo = string.Join(" ", replyListKo);
        string replyJp = string.Join(" ", replyListJp);
        string replyEn = string.Join(" ", replyListEn);
        
        // 사용자 메시지가 있는 경우 (새로운 사용자 입력)에만 저장
        if (!string.IsNullOrEmpty(userMessage) && userSpeaker == "sensei")
        {
            SaveAroplaConversationMemory("sensei", "user", userMessage, userMessage, userMessage, userMessage);
        }
        
        // 캐릭터 응답 저장
        SaveAroplaConversationMemory(response.speaker, "assistant", finalMessage, replyKo, replyJp, replyEn);
        
        AroplaLog($"Conversation saved to memory: {finalMessage}");
    }

    // 메시지 UI 표시 (다국어 지원)
    private void DisplayMessage(string speaker, string message, string messageKo = "", string messageJp = "", string messageEn = "")
    {
        switch (speaker)
        {
            case "sensei":
                // 기존 사용자 말풍선 표시
                // ChatBalloonManager.Instance.ModifyChatBalloonText(message);
                // ChatBalloonManager.Instance.ShowChatBalloon();
                break;
                
            case "arona":
                // 아로나 전용 말풍선 표시 (다국어 지원)
                ShowAronaMessage(message, messageKo, messageJp, messageEn);
                break;
                
            case "plana":
                // 프라나 전용 말풍선 표시 (다국어 지원)
                ShowPlanaMessage(message, messageKo, messageJp, messageEn);
                break;
        }
    }

    // 캐릭터별 음성 생성
    private void GenerateVoiceForCharacter(string speaker, string message)
    {
        // 사용자(선생님)는 음성 생성하지 않음
        if (speaker == "sensei") return;
        
        string chatIdx = GameManager.Instance.chatIdxSuccess;
        bool isJapanese = SettingManager.Instance.settings.sound_language == "jp";
        
        // 캐릭터별 닉네임 설정 (아로나/프라나 각각의 음성 생성)
        string characterNickname = GetCharacterNickname(speaker);
        
        if (isJapanese)
        {
            APIManager.Instance.GetJpWavFromAPI(message, chatIdx, characterNickname);
        }
        else
        {
            APIManager.Instance.GetKoWavFromAPI(message, chatIdx, characterNickname);
        }
        
        LogToFile($"Voice generation requested for {speaker} (nickname: {characterNickname}): {message}");
    }

    // 캐릭터별 닉네임 가져오기
    private string GetCharacterNickname(string speaker)
    {
        switch (speaker)
        {
            case "arona":
                // 아로나 인스턴스가 있으면 해당 닉네임 사용, 없으면 기본값
                if (aronaInstance != null)
                {
                    string aronaNickname = CharManager.Instance.GetNickname(aronaInstance);
                    if (!string.IsNullOrEmpty(aronaNickname))
                        return aronaNickname;
                }
                return "arona"; // 기본값
                
            case "plana":
                // 프라나 인스턴스가 있으면 해당 닉네임 사용, 없으면 기본값
                if (planaInstance != null)
                {
                    string planaNickname = CharManager.Instance.GetNickname(planaInstance);
                    if (!string.IsNullOrEmpty(planaNickname))
                        return planaNickname;
                }
                return "plana"; // 기본값
                
            default:
                // 메인 캐릭터 또는 알 수 없는 캐릭터는 null 반환 (기존 로직 사용)
                return null;
        }
    }

    // 스트리밍 응답 데이터 저장용
    private AroplaConversationResponse currentStreamResponse;
    private bool isStreamingComplete;
    private List<AroplaReply> streamReplyList;
    
    // APIManager 방식과 동일한 스트리밍 답변 조립용 리스트들
    private List<string> replyListKo = new List<string>();
    private List<string> replyListJp = new List<string>();
    private List<string> replyListEn = new List<string>();
    
    // 아로프라 스트리밍 API 호출
    private async Task<AroplaConversationResponse> CallAroplaAPI(Dictionary<string, string> requestData)
    {
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((url) => tcs.SetResult(url));
        string baseUrl = await tcs.Task;
        
        string apiUrl = baseUrl + "/aropla/conversation";
        
        AroplaLog("=== Streaming API Request ===");
        AroplaLog($"URL: {apiUrl}");
        AroplaLog($"Current Speaker: {requestData.GetValueOrDefault("current_speaker")}");
        AroplaLog($"Target Speaker: {requestData.GetValueOrDefault("target_speaker", "Agent decides")}");
        AroplaLog($"Message: {requestData.GetValueOrDefault("query")}");
        AroplaLog($"AI Emotion: {requestData.GetValueOrDefault("ai_emotion")}");
        AroplaLog($"Intent Smalltalk: {requestData.GetValueOrDefault("intent_smalltalk")}");
        AroplaLog("===============================");
        
        LogToFile($"Streaming API Call: {apiUrl}");
        LogToFile($"Request: target_speaker={requestData.GetValueOrDefault("target_speaker")}, current_speaker={requestData.GetValueOrDefault("current_speaker")}, ai_emotion={requestData.GetValueOrDefault("ai_emotion")}, intent_smalltalk={requestData.GetValueOrDefault("intent_smalltalk")}");

        // 스트리밍 응답 초기화
        currentStreamResponse = null;
        isStreamingComplete = false;
        streamReplyList = new List<AroplaReply>();
        
        // APIManager 방식과 동일하게 답변 조립용 리스트 초기화
        replyListKo.Clear();
        replyListJp.Clear();
        replyListEn.Clear();

        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
        string contentType = "multipart/form-data; boundary=" + boundary;

        try
        {
            // HttpWebRequest 객체를 사용하여 요청 생성
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
            request.Method = "POST";
            request.ContentType = contentType;

            using (MemoryStream memStream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
            {
                // multipart/form-data 형태로 데이터 추가
                foreach (var entry in requestData)
                {
                    writer.WriteLine($"--{boundary}");
                    writer.WriteLine($"Content-Disposition: form-data; name=\"{entry.Key}\"");
                    writer.WriteLine();
                    writer.WriteLine(entry.Value);
                }

                // 마지막 boundary 추가
                writer.WriteLine($"--{boundary}--");
                writer.Flush();

                // 요청 본문에 데이터 쓰기
                request.ContentLength = memStream.Length;
                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    await memStream.CopyToAsync(requestStream);
                }
            }

            AroplaLog("Starting streaming request...");
            
            // 응답을 비동기로 읽기
            using (WebResponse response = await request.GetResponseAsync())
            {
                AroplaLog($"Response Status Code: {(int)((HttpWebResponse)response).StatusCode}");

                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                var jsonObject = JObject.Parse(line);
                                string type = jsonObject["type"]?.ToString() ?? "unknown";
                                OnStreamingData(type, line);
                            }
                            catch (JsonReaderException e)
                            {
                                AroplaLogWarning($"JSON decode error: {e.Message}");
                            }
                        }
                    }

                    // 스트리밍이 완료될 때까지 대기
                    int waitCount = 0;
                    while (!isStreamingComplete && waitCount < 100) // 최대 10초 대기
                    {
                        await Task.Delay(100);
                        waitCount++;
                    }
                    
                    if (currentStreamResponse != null)
                    {
                        AroplaLog($"Streaming completed - Final speaker: {currentStreamResponse.speaker}");
                        AroplaLog($"Next speaker: {currentStreamResponse.next_speaker}");
                        LogToFile($"Streaming API completed: {currentStreamResponse.speaker} -> {currentStreamResponse.next_speaker}");
                        return currentStreamResponse;
                    }
                    else
                    {
                        AroplaLogError("Streaming completed but no final response received");
                        return null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AroplaLogError($"Streaming API Exception: {ex.Message}");
            LogToFile($"Streaming API Exception: {ex.Message}");
            return null;
        }
    }

    // 스트리밍 데이터 처리 콜백
    private void OnStreamingData(string type, string jsonData)
    {
        AroplaLog($"=== Stream Data Received ===");
        AroplaLog($"Type: {type}");
        AroplaLog($"Data: {jsonData}");
        AroplaLog("============================");
        
        LogToFile($"Stream data: {type} - {jsonData}");
        
        try
        {
            var jsonObj = JObject.Parse(jsonData);
            
            switch (type)
            {
                case "thinking":
                    HandleThinkingResponse(jsonObj);
                    break;
                    
                case "reply":
                    HandleReplyResponse(jsonObj);
                    break;
                    
                case "final":
                    HandleFinalResponse(jsonObj);
                    break;
                    
                case "error":
                    HandleErrorResponse(jsonObj);
                    break;
                    
                default:
                    AroplaLogWarning($"Unknown stream type: {type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            AroplaLogError($"Error processing stream data: {ex.Message}");
        }
    }
    
    // Thinking 단계 응답 처리
    private void HandleThinkingResponse(JObject data)
    {
        string chatIdx = data["chat_idx"]?.ToString() ?? "0";
        
        // APIManager와 동일한 chatIdx 체크
        if (chatIdx != GameManager.Instance.chatIdxSuccess.ToString())
        {
            AroplaLog($"Thinking chatIdx Too Old: {chatIdx}/{GameManager.Instance.chatIdxSuccess.ToString()}");
            return;  // 현재 대화가 아님
        }
        
        AroplaLog("🤔 AI is thinking...");
        LogToFile("AI thinking phase started");
        
        // UI에 "생각 중..." 상태 표시
        // TODO: 필요시 UI 업데이트
    }
    
    // Reply 단계 응답 처리 (APIManager 방식으로 수정)
    private void HandleReplyResponse(JObject data)
    {
        try
        {
            string speaker = data["speaker"]?.ToString() ?? "unknown";
            string chatIdx = data["chat_idx"]?.ToString() ?? "0";
            
            // APIManager와 동일한 chatIdx 체크
            if (chatIdx != GameManager.Instance.chatIdxSuccess.ToString())
            {
                AroplaLog($"chatIdx Too Old: {chatIdx}/{GameManager.Instance.chatIdxSuccess.ToString()}");
                return;  // 현재 대화가 아님
            }
            
            // APIManager와 동일하게 매번 초기화 (서버에서 오는 reply_list는 이미 누적된 전체 답변)
            replyListKo.Clear();
            replyListJp.Clear();
            replyListEn.Clear();
            
            JToken replyToken = data["reply_list"];
            if (replyToken != null && replyToken.Type == JTokenType.Array)
            {
                string answerVoice = null;
                foreach (var reply in replyToken)
                {
                    string answerJp = reply["answer_jp"]?.ToString() ?? string.Empty;
                    string answerKo = reply["answer_ko"]?.ToString() ?? string.Empty;
                    string answerEn = reply["answer_en"]?.ToString() ?? string.Empty;

                    // 각각의 답변을 리스트에 추가
                    if (!string.IsNullOrEmpty(answerJp))
                    {
                        replyListJp.Add(answerJp);
                        if (SettingManager.Instance.settings.sound_language == "jp")
                        {
                            answerVoice = answerJp;
                        }
                    }

                    if (!string.IsNullOrEmpty(answerKo))
                    {
                        replyListKo.Add(answerKo);
                        if (SettingManager.Instance.settings.sound_language == "ko")
                        {
                            answerVoice = answerKo;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(answerEn))
                    {
                        replyListEn.Add(answerEn);
                        if (SettingManager.Instance.settings.sound_language == "en")
                        {
                            answerVoice = answerEn;
                        }
                    }
                }

                string replyKo = string.Join(" ", replyListKo);
                string replyJp = string.Join(" ", replyListJp);
                string replyEn = string.Join(" ", replyListEn);
                
                AroplaLog($"💬 Current reply from {speaker}: KO='{replyKo}', JP='{replyJp}', EN='{replyEn}'");
                LogToFile($"Current reply from {speaker}: KO={replyKo}, JP={replyJp}, EN={replyEn}");

                // UI 표시용 메시지 선택
                string displayMessage = replyEn; // 기본값
                if (SettingManager.Instance.settings.ui_language == "ko")
                {
                    displayMessage = replyKo;
                }
                else if (SettingManager.Instance.settings.ui_language == "ja" || SettingManager.Instance.settings.ui_language == "jp")
                {
                    displayMessage = replyJp;
                }
                
                DisplayMessage(speaker, displayMessage, replyKo, replyJp, replyEn);

                // 음성 API 호출
                if (answerVoice != null)
                {
                    GenerateVoiceForCharacter(speaker, answerVoice);
                }
            }
        }
        catch (Exception ex)
        {
            AroplaLogError($"Error handling reply response: {ex.Message}");
        }
    }
    
    // Final 단계 응답 처리 (다음 발화자 정보)
    private void HandleFinalResponse(JObject data)
    {
        try
        {
            string speaker = data["speaker"]?.ToString() ?? "unknown";
            string nextSpeaker = data["next_speaker"]?.ToString() ?? "sensei";
            string reasoning = data["reasoning"]?.ToString() ?? "";
            string chatIdx = data["chat_idx"]?.ToString() ?? "0";
            
            // APIManager와 동일한 chatIdx 체크
            if (chatIdx != GameManager.Instance.chatIdxSuccess.ToString())
            {
                AroplaLog($"Final chatIdx Too Old: {chatIdx}/{GameManager.Instance.chatIdxSuccess.ToString()}");
                return;  // 현재 대화가 아님
            }
            
            AroplaLog($"✅ Final response: {speaker} -> {nextSpeaker}");
            AroplaLog($"Reasoning: {reasoning}");
            
            // 최종 응답 객체 생성
            currentStreamResponse = new AroplaConversationResponse
            {
                reply_list = streamReplyList.ToArray(),
                speaker = speaker,
                next_speaker = nextSpeaker,
                reasoning = reasoning,
                chat_idx = chatIdx,
                type = "reply"
            };
            
            // 스트리밍 완료 플래그 설정
            isStreamingComplete = true;
            
            LogToFile($"Final response: {speaker} -> {nextSpeaker} ({reasoning})");
        }
        catch (Exception ex)
        {
            AroplaLogError($"Error handling final response: {ex.Message}");
            isStreamingComplete = true; // 에러가 나도 완료로 처리
        }
    }
    
    // Error 응답 처리
    private void HandleErrorResponse(JObject data)
    {
        string chatIdx = data["chat_idx"]?.ToString() ?? "0";
        
        // APIManager와 동일한 chatIdx 체크
        if (chatIdx != GameManager.Instance.chatIdxSuccess.ToString())
        {
            AroplaLog($"Error chatIdx Too Old: {chatIdx}/{GameManager.Instance.chatIdxSuccess.ToString()}");
            return;  // 현재 대화가 아님
        }
        
        string errorMessage = data["error"]?.ToString() ?? "Unknown error";
        string message = data["message"]?.ToString() ?? "서버 오류가 발생했습니다.";
        
        AroplaLogError($"Server error: {errorMessage}");
        
        // 에러 응답 생성
        var errorReply = new AroplaReply
        {
            answer_ko = message,
            answer_jp = "サーバーエラーが発生しました。",
            answer_en = "Server error occurred."
        };
        
        currentStreamResponse = new AroplaConversationResponse
        {
            reply_list = new AroplaReply[] { errorReply },
            speaker = "system",
            next_speaker = "sensei",
            reasoning = "Error occurred",
            type = "error"
        };
        
        isStreamingComplete = true;
        LogToFile($"Server error: {errorMessage}");
    }

    // UI 관련 메서드들 (CharManager 방식 참고)
    private void ShowAroplaChannelUI()
    {
        // 프리팹들이 설정되지 않은 경우 경고
        if (aronaPrefab == null || planaPrefab == null)
        {
            AroplaLogWarning("Arona 또는 Plana Prefab이 설정되지 않았습니다. 인스펙터에서 설정해주세요.");
            return;
        }

        // Canvas가 없는 경우 다시 찾기
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                AroplaLogError("Canvas를 찾을 수 없어 캐릭터들을 생성할 수 없습니다.");
                return;
            }
        }

        // 아로나 생성
        CreateAronaInstance();
        
        // 프라나 생성
        CreatePlanaInstance();

        AroplaLog("아로프라 채널 UI 표시 - 아로나, 프라나 캐릭터 생성됨");
    }

    // 아로나 인스턴스 생성
    private void CreateAronaInstance()
    {
        // 이미 아로나가 생성되어 있으면 제거
        if (aronaInstance != null)
        {
            Destroy(aronaInstance);
            aronaInstance = null;
        }

        // 아로나 위치 계산 (왼쪽 상단)
        Vector3 aronaPosition = CalculateSubCharacterPosition(ARONA_OFFSET_X, ARONA_OFFSET_Y);

        // 아로나 인스턴스 생성
        aronaInstance = Instantiate(aronaPrefab, aronaPosition, aronaPrefab.transform.rotation, canvas.transform);

        // RectTransform 위치 설정
        RectTransform aronaRect = aronaInstance.GetComponent<RectTransform>();
        if (aronaRect != null)
        {
            aronaRect.anchoredPosition3D = aronaPosition;
        }

        // 아로나 크기를 메인 캐릭터와 동일하게 설정
        SetAronaSize();

        // 아로나 핸들러 설정
        SetAronaHandlers();

        LogToFile($"Arona instance created at position: {aronaPosition}");
    }

    // 프라나 인스턴스 생성
    private void CreatePlanaInstance()
    {
        // 이미 프라나가 생성되어 있으면 제거
        if (planaInstance != null)
        {
            Destroy(planaInstance);
            planaInstance = null;
        }

        // 프라나 위치 계산 (오른쪽 상단)
        Vector3 planaPosition = CalculateSubCharacterPosition(PLANA_OFFSET_X, PLANA_OFFSET_Y);

        // 프라나 인스턴스 생성
        planaInstance = Instantiate(planaPrefab, planaPosition, planaPrefab.transform.rotation, canvas.transform);

        // RectTransform 위치 설정
        RectTransform planaRect = planaInstance.GetComponent<RectTransform>();
        if (planaRect != null)
        {
            planaRect.anchoredPosition3D = planaPosition;
        }

        // 프라나 크기를 메인 캐릭터와 동일하게 설정
        SetPlanaSize();

        // 프라나 핸들러 설정
        SetPlanaHandlers();

        LogToFile($"Plana instance created at position: {planaPosition}");
    }

    private void HideAroplaChannelUI()
    {
        // 아로나 인스턴스 제거
        if (aronaInstance != null)
        {
            Destroy(aronaInstance);
            aronaInstance = null;
            LogToFile("Arona instance destroyed");
        }

        // 프라나 인스턴스 제거
        if (planaInstance != null)
        {
            Destroy(planaInstance);
            planaInstance = null;
            LogToFile("Plana instance destroyed");
        }

        AroplaLog("아로프라 채널 UI 숨기기 - 아로나, 프라나 캐릭터 제거됨");
    }

    // 아로나 크기 설정 (CharManager.setCharSize 방식 참고)
    private void SetAronaSize()
    {
        if (aronaInstance != null)
        {
            float char_size = SettingManager.Instance.settings.char_size;
            float initialScale = aronaInstance.transform.localScale.x; // 프리팹의 초기 스케일
            float scaleFactor = initialScale * char_size / 100f; // 퍼센트를 비율로 변환
            
            aronaInstance.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            LogToFile($"Arona size set to: {char_size}%");
        }
    }

    // 프라나 크기 설정 (CharManager.setCharSize 방식 참고)
    private void SetPlanaSize()
    {
        if (planaInstance != null)
        {
            float char_size = SettingManager.Instance.settings.char_size;
            float initialScale = planaInstance.transform.localScale.x; // 프리팹의 초기 스케일
            float scaleFactor = initialScale * char_size / 100f; // 퍼센트를 비율로 변환
            
            planaInstance.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            LogToFile($"Plana size set to: {char_size}%");
        }
    }

    // 아로나 핸들러 설정 (CharManager의 핸들러 설정 방식 참고)
    private void SetAronaHandlers()
    {
        if (aronaInstance == null) return;

        // CharManager의 핸들러 설정 로직 참고하여 아로나에 필요한 핸들러들 설정
        SetAronaDragHandlerVar();
        SetAronaClickHandlerVar();
        SetAronaEmotionFaceController();

        LogToFile("Arona handlers configured");
    }

    // 프라나 핸들러 설정 (CharManager의 핸들러 설정 방식 참고)
    private void SetPlanaHandlers()
    {
        if (planaInstance == null) return;

        // CharManager의 핸들러 설정 로직 참고하여 프라나에 필요한 핸들러들 설정
        SetPlanaDragHandlerVar();
        SetPlanaClickHandlerVar();
        // SetPlanaPhysicsManagerVar(); // 프라나는 물리 적용 안함
        // 말풍선 관련은 ShowPlanaMessage에서 동적으로 처리
        SetPlanaEmotionFaceController();

        LogToFile("Plana handlers configured");
    }

    // 아로나 DragHandler 변수 설정 (CharManager.setDragHandlerVar 방식 참고)
    private void SetAronaDragHandlerVar()
    {
        DragHandler dragHandler = aronaInstance.GetComponentInChildren<DragHandler>();
        if (dragHandler != null)
        {
            // Canvas 할당
            dragHandler._canvas = canvas ?? FindObjectOfType<Canvas>();

            // 부모(아로나) 캐릭터의 Animator 할당
            Animator aronaAnimator = aronaInstance.GetComponent<Animator>();
            if (aronaAnimator != null)
            {
                dragHandler._animator = aronaAnimator;
            }
            else
            {
                AroplaLogWarning("Arona에서 Animator를 찾을 수 없습니다.");
            }
            
            LogToFile("Arona DragHandler variables set");
        }
        else
        {
            AroplaLogWarning("Arona에서 DragHandler를 찾을 수 없습니다.");
        }
    }

    // 아로나 ClickHandler 변수 설정 (CharManager.setClickHandlerVar 방식과 동일)
    private void SetAronaClickHandlerVar()
    {
        // 아로나의 하위에 있는 ClickHandler를 찾아 설정
        ClickHandler clickHandler = aronaInstance.GetComponentInChildren<ClickHandler>();

        if (clickHandler != null)
        {
            // 부모(아로나) 캐릭터의 Animator를 _animator에 할당
            Animator aronaAnimator = aronaInstance.GetComponent<Animator>();
            if (aronaAnimator != null)
            {
                clickHandler._animator = aronaAnimator;
                LogToFile("Arona ClickHandler variables set");
            }
            else
            {
                AroplaLogWarning("Arona에서 Animator를 찾을 수 없습니다.");
            }
        }
        else
        {
            AroplaLogWarning("Arona에서 ClickHandler를 찾을 수 없습니다.");
        }
    }

    // 아로나 EmotionFaceController 설정 
    private void SetAronaEmotionFaceController()
    {
        EmotionFaceController emotionFaceController = aronaInstance.GetComponentInChildren<EmotionFaceController>();
        if (emotionFaceController != null)
        {
            emotionFaceController.SetCharType("Sub"); // Sub 캐릭터로 설정
            LogToFile("Arona EmotionFaceController set to Sub type");
        }
        else
        {
            AroplaLogWarning("Arona에서 EmotionFaceController를 찾을 수 없습니다.");
        }
    }

    // 프라나 DragHandler 변수 설정 (CharManager.setDragHandlerVar 방식 참고)
    private void SetPlanaDragHandlerVar()
    {
        DragHandler dragHandler = planaInstance.GetComponentInChildren<DragHandler>();
        if (dragHandler != null)
        {
            // Canvas 할당
            dragHandler._canvas = canvas ?? FindObjectOfType<Canvas>();

            // 부모(프라나) 캐릭터의 Animator 할당
            Animator planaAnimator = planaInstance.GetComponent<Animator>();
            if (planaAnimator != null)
            {
                dragHandler._animator = planaAnimator;
            }
            else
            {
                AroplaLogWarning("Plana에서 Animator를 찾을 수 없습니다.");
            }
            
            LogToFile("Plana DragHandler variables set");
        }
        else
        {
            AroplaLogWarning("Plana에서 DragHandler를 찾을 수 없습니다.");
        }
    }

    // 프라나 ClickHandler 변수 설정 (CharManager.setClickHandlerVar 방식과 동일)
    private void SetPlanaClickHandlerVar()
    {
        // 프라나의 하위에 있는 ClickHandler를 찾아 설정
        ClickHandler clickHandler = planaInstance.GetComponentInChildren<ClickHandler>();

        if (clickHandler != null)
        {
            // 부모(프라나) 캐릭터의 Animator를 _animator에 할당
            Animator planaAnimator = planaInstance.GetComponent<Animator>();
            if (planaAnimator != null)
            {
                clickHandler._animator = planaAnimator;
                LogToFile("Plana ClickHandler variables set");
            }
            else
            {
                AroplaLogWarning("Plana에서 Animator를 찾을 수 없습니다.");
            }
        }
        else
        {
            AroplaLogWarning("Plana에서 ClickHandler를 찾을 수 없습니다.");
        }
    }

    // 프라나 EmotionFaceController 설정 
    private void SetPlanaEmotionFaceController()
    {
        EmotionFaceController emotionFaceController = planaInstance.GetComponentInChildren<EmotionFaceController>();
        if (emotionFaceController != null)
        {
            emotionFaceController.SetCharType("Sub"); // Sub 캐릭터로 설정
            LogToFile("Plana EmotionFaceController set to Sub type");
        }
        else
        {
            AroplaLogWarning("Plana에서 EmotionFaceController를 찾을 수 없습니다.");
        }
    }


    private void ShowPlanaMessage(string message, string messageKo = "", string messageJp = "", string messageEn = "")
    {
        // APIManager와 동일한 방식으로 다국어 메시지 처리
        string displayKo = !string.IsNullOrEmpty(messageKo) ? messageKo : message;
        string displayJp = !string.IsNullOrEmpty(messageJp) ? messageJp : message;
        string displayEn = !string.IsNullOrEmpty(messageEn) ? messageEn : message;
        
        // 프라나 인스턴스가 있는 경우 해당 위치 기준으로 말풍선 표시
        if (planaInstance != null)
        {
            // SubAnswerBalloonManager를 사용하여 프라나 위치에 말풍선 표시
            RectTransform planaRect = planaInstance.GetComponent<RectTransform>();
            if (planaRect != null)
            {
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(displayKo, displayJp, displayEn);
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonText();
                SubAnswerBalloonManager.Instance.ShowAnswerBalloonInfAtCharacter(planaRect);
                // 음성 로딩 대기 후 오디오 길이에 맞춰 자동 닫기
                // StartCoroutine(HideAfterAudioLoaded());
            }
            else
            {
                // RectTransform을 찾을 수 없는 경우 기본 표시
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(displayKo, displayJp, displayEn);
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonText();
                SubAnswerBalloonManager.Instance.ShowAnswerBalloonInf();
                // 음성 로딩 대기 후 오디오 길이에 맞춰 자동 닫기
                // StartCoroutine(HideAfterAudioLoaded());
            }
        }
        else
        {
            // 프라나 인스턴스가 없으면 기본 방식 사용
            SubAnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(displayKo, displayJp, displayEn);
            SubAnswerBalloonManager.Instance.ModifyAnswerBalloonText();
            SubAnswerBalloonManager.Instance.ShowAnswerBalloonInf();
            // 음성 로딩 대기 후 오디오 길이에 맞춰 자동 닫기
            // StartCoroutine(HideAfterAudioLoaded());
        }
        
        AroplaLog($"Plana Message: {message}");
    }

    // 음성 로딩 완료 후 말풍선 자동 닫기를 위한 코루틴
    private IEnumerator HideAfterAudioLoaded()
    {
        // 음성 파일 로딩 대기 (1초 정도)
        yield return new WaitForSeconds(1.0f);
        
        // 실제 음성 길이에 맞춰 말풍선 자동 닫기
        SubAnswerBalloonManager.Instance.HideAnswerBalloonAfterAudio();
        
        AroplaLog("말풍선 자동 닫기 타이머 설정됨 (음성 길이 기준)");
    }

    // 기존 APIManager와의 통합을 위한 메서드
    public bool IsAroplaMode()
    {
        return isAroplaMode;
    }

    // 채팅시 사용할 매니저 결정 (static 메서드)
    public static bool ShouldUseAroplaManager()
    {
        // TODO: SettingManager.SettingsData에 다음 필드 추가 권장:
        // public bool isAroplaChannelEnabled;  // 아로프라 채널 활성화 여부
        // public bool isAroplaChannelDefault;  // 시작시 아로프라 채널을 기본으로 사용할지
        
        // 현재는 Instance가 있고 활성화된 상태인지 확인
        if (Instance != null)
        {
            return Instance.IsAroplaMode();
        }
        return false;
    }

    // 전역에서 아로프라 채널 사용 여부를 확인하는 메서드
    public static void ProcessUserMessage(string message)
    {
        if (ShouldUseAroplaManager())
        {
            // 아로프라 채널로 처리
            Instance.SendUserMessage(message);
        }
        else
        {
            // 기존 APIManager로 처리
            // APIManager.Instance.처리메서드(message); // 구현 필요
            Debug.Log($"Using standard APIManager for: {message}");
        }
    }

    // 편의 메서드들
    public void ToggleAroplaMode()
    {
        if (isAroplaMode)
        {
            StopAroplaChannel();
        }
        else
        {
            StartAroplaChannel();
        }
    }

    // 외부에서 아로프라 채널 상태 확인
    public static bool IsAroplaChannelActive()
    {
        return Instance != null && Instance.IsAroplaMode();
    }

    // 현재 대화 참여자 정보 제공
    public string GetCurrentParticipants()
    {
        if (!isAroplaMode) return "None";
        return "선생님(Sensei), 아로나(Arona), 프라나(Plana)";
    }

    // 아로프라 전용 메모리 관리 메서드들
    
    // 대화 개수 반환 (MemoryManager 기반)
    public int GetAroplaConversationCount()
    {
        return MemoryManager.Instance.GetAllConversationMemory(filename: GetFileName()).Count;
    }

    // 마지막 대화 삭제 (MemoryManager 기반)
    public void DeleteLastAroplaConversation()
    {
        MemoryManager.Instance.DeleteRecentDialogue(filename: GetFileName());
        LogToFile("Deleted last conversation via MemoryManager");
    }
    
    // 아로프라 메모리에서 특정 언어의 메시지 추출
    public List<string> GetAroplaMessagesInLanguage(string language)
    {
        var messages = new List<string>();
        try
        {
            var memories = MemoryManager.Instance.GetMessagesInLanguage(language, filename: GetFileName());
            foreach (var memory in memories)
            {
                string messageText = language switch
                {
                    "ko" => memory.messageKo,
                    "jp" => memory.messageJp,
                    "en" => memory.messageEn,
                    _ => memory.message
                };
                
                if (!string.IsNullOrEmpty(messageText))
                {
                    messages.Add(messageText);
                }
            }
        }
        catch (Exception ex)
        {
            AroplaLogError($"Error getting messages in language {language}: {ex.Message}");
        }
        return messages;
    }
    
    // 특정 발화자의 메시지만 가져오기
    public List<Conversation> GetAroplaMessagesBySpeaker(string speaker)
    {
        try
        {
            return MemoryManager.Instance.GetMessagesBySpeaker(speaker, filename: GetFileName());
        }
        catch (Exception ex)
        {
            AroplaLogError($"Error getting messages by speaker {speaker}: {ex.Message}");
            return new List<Conversation>();
        }
    }
    
    // 아로프라 메모리 통계 정보
    public (int user, int assistant, int system, int total) GetAroplaMemoryStats()
    {
        try
        {
            return MemoryManager.Instance.GetMemoryStats(filename: GetFileName());
        }
        catch (Exception ex)
        {
            AroplaLogError($"Error getting memory stats: {ex.Message}");
            return (0, 0, 0, 0);
        }
    }
    
    // 전체 대화 히스토리 가져오기
    public List<Conversation> GetConversationHistory()
    {
        // 아로프라 방식: MemoryManager를 통해 대화 히스토리 반환
        return MemoryManager.Instance.GetAllConversationMemory(filename: GetFileName());
    }
    
    // 프라나 인스턴스 가져오기
    public GameObject GetPlanaInstance()
    {
        return planaInstance;
    }

    // 아로나 위치 업데이트 (캐릭터 변경시 등에 사용)
    public void UpdateAronaPosition()
    {
        if (!isAroplaMode || aronaInstance == null) return;

        // 아로나 위치 재계산
        Vector3 newAronaPosition = CalculateSubCharacterPosition(ARONA_OFFSET_X, ARONA_OFFSET_Y);

        RectTransform aronaRect = aronaInstance.GetComponent<RectTransform>();
        if (aronaRect != null)
        {
            aronaRect.anchoredPosition3D = newAronaPosition;
        }

        LogToFile($"Arona position updated to: {newAronaPosition}");
    }

    // 프라나 위치 업데이트 (캐릭터 변경시 등에 사용)
    public void UpdatePlanaPosition()
    {
        if (!isAroplaMode || planaInstance == null) return;

        // 프라나 위치 재계산
        Vector3 newPlanaPosition = CalculateSubCharacterPosition(PLANA_OFFSET_X, PLANA_OFFSET_Y);

        RectTransform planaRect = planaInstance.GetComponent<RectTransform>();
        if (planaRect != null)
        {
            planaRect.anchoredPosition3D = newPlanaPosition;
        }

        LogToFile($"Plana position updated to: {newPlanaPosition}");
    }

    // 아로나 크기 업데이트 (캐릭터 크기 변경시 등에 사용)
    public void UpdateAronaSize()
    {
        if (!isAroplaMode || aronaInstance == null) return;
        SetAronaSize();
    }

    // 프라나 크기 업데이트 (캐릭터 크기 변경시 등에 사용)  
    public void UpdatePlanaSize()
    {
        if (!isAroplaMode || planaInstance == null) return;
        SetPlanaSize();
    }

    // 아로나 인스턴스 가져오기
    public GameObject GetAronaInstance()
    {
        return aronaInstance;
    }

    // 아로프라 모드에서 캐릭터들이 활성화되어 있는지 확인
    public bool IsAronaActive()
    {
        return isAroplaMode && aronaInstance != null;
    }

    public bool IsPlanaActive()
    {
        return isAroplaMode && planaInstance != null;
    }

    public bool IsAroplaInstancesActive()
    {
        return isAroplaMode && aronaInstance != null && planaInstance != null;
    }

    // 아로나 핸들러 재설정 (캐릭터 변경시 등에 사용)
    public void UpdateAronaHandlers()
    {
        if (!isAroplaMode || aronaInstance == null) return;
        SetAronaHandlers();
        LogToFile("Arona handlers updated");
    }

    // 프라나 핸들러 재설정 (캐릭터 변경시 등에 사용)
    public void UpdatePlanaHandlers()
    {
        if (!isAroplaMode || planaInstance == null) return;
        SetPlanaHandlers();
        LogToFile("Plana handlers updated");
    }

    // 아로나 완전 재초기화 (위치, 크기, 핸들러 모두 업데이트)
    public void RefreshAronaInstance()
    {
        if (!isAroplaMode || aronaInstance == null) return;
        
        UpdateAronaPosition();
        UpdateAronaSize();
        UpdateAronaHandlers();
        
        LogToFile("Arona instance fully refreshed");
    }

    // 프라나 완전 재초기화 (위치, 크기, 핸들러 모두 업데이트)
    public void RefreshPlanaInstance()
    {
        if (!isAroplaMode || planaInstance == null) return;
        
        UpdatePlanaPosition();
        UpdatePlanaSize();
        UpdatePlanaHandlers();
        
        LogToFile("Plana instance fully refreshed");
    }

    // 모든 아로프라 인스턴스 완전 재초기화
    public void RefreshAllAroplaInstances()
    {
        if (!isAroplaMode) return;
        
        RefreshAronaInstance();
        RefreshPlanaInstance();
        
        LogToFile("All Aropla instances fully refreshed");
    }

    // 아로나 메시지 표시
    private void ShowAronaMessage(string message, string messageKo = "", string messageJp = "", string messageEn = "")
    {
        // APIManager와 동일한 방식으로 다국어 메시지 처리
        string displayKo = !string.IsNullOrEmpty(messageKo) ? messageKo : message;
        string displayJp = !string.IsNullOrEmpty(messageJp) ? messageJp : message;
        string displayEn = !string.IsNullOrEmpty(messageEn) ? messageEn : message;
        
        // 아로나 인스턴스가 있는 경우 해당 위치 기준으로 말풍선 표시
        if (aronaInstance != null)
        {
            // SubAnswerBalloonManager를 사용하여 아로나 위치에 말풍선 표시
            RectTransform aronaRect = aronaInstance.GetComponent<RectTransform>();
            if (aronaRect != null)
            {
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(displayKo, displayJp, displayEn);
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonText();
                SubAnswerBalloonManager.Instance.ShowAnswerBalloonInfAtCharacter(aronaRect);
                // 음성 로딩 대기 후 오디오 길이에 맞춰 자동 닫기
                // StartCoroutine(HideAfterAudioLoaded());
            }
            else
            {
                // RectTransform을 찾을 수 없는 경우 기본 표시
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(displayKo, displayJp, displayEn);
                SubAnswerBalloonManager.Instance.ModifyAnswerBalloonText();
                SubAnswerBalloonManager.Instance.ShowAnswerBalloonInf();
                // 음성 로딩 대기 후 오디오 길이에 맞춰 자동 닫기
                // StartCoroutine(HideAfterAudioLoaded());
            }
        }
        else
        {
            // 아로나 인스턴스가 없으면 기본 방식 사용
            SubAnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(displayKo, displayJp, displayEn);
            SubAnswerBalloonManager.Instance.ModifyAnswerBalloonText();
            SubAnswerBalloonManager.Instance.ShowAnswerBalloonInf();
            // 음성 로딩 대기 후 오디오 길이에 맞춰 자동 닫기
            // StartCoroutine(HideAfterAudioLoaded());
        }
        
        AroplaLog($"Arona Message: {message}");
    }
}