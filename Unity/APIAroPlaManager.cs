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


    // 아로프라 채널 상태
    private bool isAroplaMode = false;
    private bool isProcessing = false;
    private string logFilePath;
    private string previousCharCode;  // 아로프라 채널 시작 전 캐릭터 코드
    
    // 아로프라 캐릭터 관리 (CharManager 방식 참고)
    [Header("Aropla Channel Settings")]
    public GameObject planaPrefab;   // 인스펙터에서 설정할 프라나 프리팹
    private GameObject planaInstance;  // 생성된 프라나 인스턴스
    private Canvas canvas;

    // 서브 캐릭터(프라나) 배치 상수
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
            MemoryManager.Instance.SaveConversationMemory(speaker, role, message, messageKo, messageJp, messageEn, "aropla");
            
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
        
        // 현재 캐릭터 저장 (나중에 복원용)
        GameObject currentChar = CharManager.Instance.GetCurrentCharacter();
        if (currentChar != null)
        {
            CharAttributes attr = currentChar.GetComponent<CharAttributes>();
            previousCharCode = attr != null ? attr.charcode : "arona";
        }
        else
        {
            previousCharCode = "arona";
        }
        
        AroplaLog($"Previous character saved: {previousCharCode}");
        
        // 메인 캐릭터를 아로나로 변경
        bool aronaChanged = CharManager.Instance.ChangeCharacterFromCharCode("arona");
        if (!aronaChanged)
        {
            AroplaLogError("Failed to change main character to Arona");
            isAroplaMode = false;
            return;
        }
        
        AroplaLog("Main character changed to Arona");
        
        // 프라나를 서브 캐릭터로 생성
        ShowAroplaChannelUI();
        
        // 항상 인사말 시작 (APIManager 방식과 동일)
        StartInitialGreeting();
    }

    // 아로프라 채널 모드 종료
    public void StopAroplaChannel()
    {
        isAroplaMode = false;
        LogToFile("Aropla Channel Mode Stopped");
        
        // 발화 중인 캐릭터 초기화
        StatusManager.Instance.ClearSpeakingCharacters();
        
        // 프라나 인스턴스 제거
        HideAroplaChannelUI();
        
        // 이전 캐릭터로 복원
        if (!string.IsNullOrEmpty(previousCharCode))
        {
            bool restored = CharManager.Instance.ChangeCharacterFromCharCode(previousCharCode);
            if (restored)
            {
                AroplaLog($"Main character restored to: {previousCharCode}");
            }
            else
            {
                AroplaLogWarning($"Failed to restore character to: {previousCharCode}");
            }
        }
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
            var memory = MemoryManager.Instance.GetAllConversationMemory("aropla");
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

            // 순수 Gemini 모드 / 서버 모드 분기
            if (false)  // TODO: 설정으로 변경 (true = 순수 Gemini, false = 레거시 서버)
            {
                // 순수 Gemini API 직접 호출
                await ProcessWithGeminiDirect(requestData, message, currentSpeaker);
            }
            else
            {
                // 서버 호출 방식
                
                // targetSpeaker에 맞는 캐릭터 위에 생각 말풍선 표시
                string targetSpeakerForBalloon = requestData.GetValueOrDefault("target_speaker", "arona");
                if (string.IsNullOrEmpty(targetSpeakerForBalloon)) targetSpeakerForBalloon = "arona";
                
                GameObject targetCharForBalloon = targetSpeakerForBalloon == "plana" ? planaInstance : CharManager.Instance.GetCurrentCharacter();
                GameObject thinkingBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(targetCharForBalloon, "Time");
                
                var response = await CallAroplaAPI(requestData);
                
                // 생각 말풍선 제거
                if (thinkingBalloon != null)
                {
                    Destroy(thinkingBalloon);
                    thinkingBalloon = null;
                }
                
                if (response != null)
                {
                    ProcessAroplaResponse(response, message, currentSpeaker);
                    
                    if (response.next_speaker != "sensei")
                    {
                        AroplaLog("=== Continuing Conversation ===");
                        AroplaLog($"Next AI Speaker: {response.next_speaker}");
                        AroplaLog("Will continue automatically in 1 second...");
                        AroplaLog("================================");
                        
                        isProcessing = false;
                        await ContinueAroplaConversation(response.speaker, response.next_speaker);
                        return;
                    }
                    else
                    {
                        AroplaLog("=== Waiting for User Input ===");
                        AroplaLog("Next speaker is sensei - conversation paused");
                        AroplaLog("================================");
                    }
                }
                else
                {
                    // API 실패 시 에러 말풍선 표시
                    if (targetCharForBalloon != null)
                    {
                        EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(targetCharForBalloon, "No", 2f);
                    }
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

    // [NEW] 순수 Gemini API 직접 호출 방식
    private async Task ProcessWithGeminiDirect(Dictionary<string, string> requestData, string message, string currentSpeaker)
    {
        AroplaLog("=== Gemini Direct Mode ===");
        string lang = requestData.GetValueOrDefault("ai_language", "ko");
        string targetSpeaker = requestData.GetValueOrDefault("target_speaker", "");

        // 메모리 JSON 파싱
        string memoryJson = requestData.GetValueOrDefault("memory", "[]");
        var memoryList = ParseMemoryJsonToList(memoryJson);

        // 1. target_speaker 결정 (누구에게 말하는지)
        if (string.IsNullOrEmpty(targetSpeaker) && currentSpeaker == "sensei")
        {
            var (analyzedTarget, reason) = await ApiGeminiMultiClient.Instance.AnalyzeTargetSpeaker(
                message, currentSpeaker, lang, memoryList);
            targetSpeaker = analyzedTarget;
            AroplaLog($"[Gemini] Target Speaker: {targetSpeaker} ({reason})");
        }

        // target_speaker 기본값
        if (string.IsNullOrEmpty(targetSpeaker))
        {
            targetSpeaker = "arona";
        }

        // 2. target_listener 결정 (누구에게 응답하는지)
        var (targetListener, listenerReason) = ApiGeminiMultiClient.Instance.DetermineTargetListenerFromContext(
            currentSpeaker, targetSpeaker, message, memoryList, lang);
        AroplaLog($"[Gemini] Target Listener: {targetListener} ({listenerReason})");

        // 생각중 말풍선 표시 (targetSpeaker에 해당하는 캐릭터 위에)
        GameObject targetCharForBalloon = targetSpeaker == "plana" 
            ? planaInstance 
            : CharManager.Instance.GetCurrentCharacter();
        GameObject thinkingBalloon = null;
        if (targetCharForBalloon != null)
        {
            thinkingBalloon = EmotionBalloonManager.Instance.ShowEmotionBalloon(targetCharForBalloon, "Time");
        }

        // 스트리밍 응답 처리용 초기화
        replyListKo.Clear();
        replyListJp.Clear();
        replyListEn.Clear();
        string accumulatedReply = "";

        // 3. Gemini 스트리밍 호출 - MultiConversationRequest 객체로 전달
        var geminiRequest = new ApiGeminiMulti.MultiConversationRequest
        {
            query = message,
            currentSpeaker = currentSpeaker,
            targetSpeaker = targetSpeaker,
            targetListener = targetListener,
            aiLanguage = lang,
            chatIdx = requestData.GetValueOrDefault("chat_idx", "-1"),
            playerName = requestData.GetValueOrDefault("player_name", "sensei"),
            participants = new List<ApiGeminiMulti.MultiParticipant>(),
            memoryList = memoryList,
            guidelineList = new List<string>(),
            situationDict = new Dictionary<string, object>()
        };

        await ApiGeminiMultiClient.Instance.CallGeminiMultiStreamDirect(
            geminiRequest,
            // onChunkReceived 콜백 - 문장 단위 스트리밍 (sentence, speaker, sentenceIndex)
            (sentence, speaker, sentenceIndex) =>
            {
                // 첫 응답 시 생각 말풍선 제거
                if (sentenceIndex == 0 && thinkingBalloon != null)
                {
                    Destroy(thinkingBalloon);
                    thinkingBalloon = null;
                }
                
                accumulatedReply += sentence;
                AroplaLog($"[Gemini] Sentence[{sentenceIndex}]: {sentence}");
                
                // 실시간 표시 (누적된 전체 텍스트)
                DisplayMessage(targetSpeaker, accumulatedReply);
                
                // 답변 리스트에 추가 (TODO: 번역 API 연동 시 분리)
                if (!string.IsNullOrEmpty(sentence))
                {
                    replyListKo.Add(sentence);
                    replyListJp.Add(sentence);
                    replyListEn.Add(sentence);
                }
            },
            // onComplete 콜백 - 완료
            (result) =>
            {
                string fullText = result.sentences != null ? string.Join(" ", result.sentences) : "";
                AroplaLog($"[Gemini] Complete: {fullText}");
            },
            // onError 콜백
            (error) =>
            {
                AroplaLogError($"[Gemini] Error: {error}");
                
                // 생각 말풍선 제거 후 에러 말풍선 표시
                if (thinkingBalloon != null)
                {
                    Destroy(thinkingBalloon);
                    thinkingBalloon = null;
                }
                if (targetCharForBalloon != null)
                {
                    EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(targetCharForBalloon, "No", 2f);
                }
            }
        );

        // 4. 사용자 메시지 저장 (새로운 입력인 경우)
        if (!string.IsNullOrEmpty(message) && currentSpeaker == "sensei")
        {
            SaveAroplaConversationMemory("sensei", "user", message, message, message, message);
        }

        // 5. 캐릭터 응답 저장
        string replyKo = string.Join(" ", replyListKo);
        string replyJp = string.Join(" ", replyListJp);
        string replyEn = string.Join(" ", replyListEn);
        
        if (!string.IsNullOrEmpty(accumulatedReply))
        {
            SaveAroplaConversationMemory(targetSpeaker, "assistant", accumulatedReply, replyKo, replyJp, replyEn);
        }

        // 6. 다음 발화자 결정
        var updatedMemories = MemoryManager.Instance.GetAllConversationMemory("");
        string updatedMemoryJson = JsonConvert.SerializeObject(updatedMemories);
        var updatedMemoryList = ParseMemoryJsonToList(updatedMemoryJson);

        var (nextSpeaker, nextReason) = await ApiGeminiMultiClient.Instance.DecideNextSpeaker(
            updatedMemoryList, message, accumulatedReply,
            targetSpeaker, currentSpeaker, lang);
        AroplaLog($"[Gemini] Next Speaker: {nextSpeaker} ({nextReason})");

        // 다음 발화자 표시
        ShowNextSpeakerBalloon(targetSpeaker, nextSpeaker);

        // 7. 다음 발화자가 AI인 경우 연속 처리
        if (nextSpeaker != "sensei")
        {
            AroplaLog("=== Continuing Conversation ===");
            AroplaLog($"Next AI Speaker: {nextSpeaker}");
            AroplaLog("Will continue automatically in 1 second...");
            AroplaLog("================================");
            
            isProcessing = false;
            await ContinueAroplaConversation(targetSpeaker, nextSpeaker);
        }
        else
        {
            AroplaLog("=== Waiting for User Input ===");
            AroplaLog("Next speaker is sensei - conversation paused");
            AroplaLog("================================");
        }
    }

    // 메모리 JSON을 Flow Director용 List로 파싱
    private List<Dictionary<string, string>> ParseMemoryJsonToList(string memoryJson)
    {
        var result = new List<Dictionary<string, string>>();
        
        if (string.IsNullOrEmpty(memoryJson))
            return result;
        
        try
        {
            var jsonArray = JArray.Parse(memoryJson);
            foreach (var item in jsonArray)
            {
                var dict = new Dictionary<string, string>
                {
                    { "speaker", item["speaker"]?.ToString() ?? "" },
                    { "role", item["role"]?.ToString() ?? "" },
                    { "message", item["message"]?.ToString() ?? "" },
                    { "messageKo", item["messageKo"]?.ToString() ?? item["message_ko"]?.ToString() ?? "" },
                    { "messageJp", item["messageJp"]?.ToString() ?? item["message_jp"]?.ToString() ?? "" },
                    { "messageEn", item["messageEn"]?.ToString() ?? item["message_en"]?.ToString() ?? "" },
                    { "character_name", item["character_name"]?.ToString() ?? item["speaker"]?.ToString() ?? "" }
                };
                result.Add(dict);
            }
        }
        catch (Exception ex)
        {
            AroplaLogWarning($"Memory JSON parsing error: {ex.Message}");
        }
        
        return result;
    }

    // 연속 대화 처리 (AI끼리 대화)
    private async Task ContinueAroplaConversation(string currentSpeaker, string targetSpeaker)
    {
        AroplaLog("=== Auto Continue Triggered ===");
        AroplaLog($"Current isProcessing state: {isProcessing}");
        AroplaLog("Waiting 1 second for natural flow...");
        
        // await Task.Delay(1000); // 1초 대기 : 바빠죽겠는데 왜 대기한거지. 문제 없으면 제거. 260129
        
        if (targetSpeaker != "sensei")
        {
            // 마지막 메시지를 가져와서 전달 (MemoryManager 기반)
            string lastMessage = "";
            var memories = MemoryManager.Instance.GetAllConversationMemory("aropla");
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
        // 이전 발화자의 입 모션 종료 및 현재 발화자 설정
        StatusManager.Instance.ClearSpeakingCharacters();
        
        switch (speaker)
        {
            case "sensei":
                // 기존 사용자 말풍선 표시
                // ChatBalloonManager.Instance.ModifyChatBalloonText(message);
                // ChatBalloonManager.Instance.ShowChatBalloon();
                break;
                
            case "arona":
                // 아로나(메인 캐릭터)를 발화자로 설정
                GameObject aronaChar = CharManager.Instance.GetCurrentCharacter();
                if (aronaChar != null)
                {
                    StatusManager.Instance.AddSpeakingCharacter(aronaChar);
                }
                // 아로나(메인 캐릭터) 말풍선 표시 (다국어 지원)
                ShowAronaMessage(message, messageKo, messageJp, messageEn);
                break;
                
            case "plana":
                // 프라나를 발화자로 설정
                if (planaInstance != null)
                {
                    StatusManager.Instance.AddSpeakingCharacter(planaInstance);
                }
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
        string soundLang = SettingManager.Instance.settings.sound_language ?? "jp";
        
        // 캐릭터별 닉네임 설정 (아로나/프라나 각각의 음성 생성)
        string characterNickname = GetCharacterNickname(speaker);
        
        // 세션 기반 TTS 요청
        APIManager.Instance.RequestTTS(message, chatIdx, soundLang, characterNickname);
        
        LogToFile($"Voice generation requested for {speaker} (nickname: {characterNickname}): {message}");
    }

    // 캐릭터별 닉네임 가져오기
    private string GetCharacterNickname(string speaker)
    {
        switch (speaker)
        {
            case "arona":
                // 메인 캐릭터(아로나) 닉네임 사용
                GameObject mainChar = CharManager.Instance.GetCurrentCharacter();
                if (mainChar != null)
                {
                    string mainNickname = CharManager.Instance.GetNickname(mainChar);
                    if (!string.IsNullOrEmpty(mainNickname))
                        return mainNickname;
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
    private TaskCompletionSource<AroplaConversationResponse> streamingCompletionSource;
    private List<AroplaReply> streamReplyList;
    
    // APIManager 방식과 동일한 스트리밍 답변 조립용 리스트들
    private List<string> replyListKo = new List<string>();
    private List<string> replyListJp = new List<string>();
    private List<string> replyListEn = new List<string>();
    
    // 다음 발화자 표시용 말풍선 (아로프라 채널 전용)
    private GameObject nextSpeakerBalloon;
    
    // 다음 발화자 아이콘 말풍선 표시 (현재 발화자 머리 위에 다음 발화 대상 아이콘 표시)
    private void ShowNextSpeakerBalloon(string speaker, string nextSpeaker)
    {
        // 기존 말풍선 제거
        HideNextSpeakerBalloon();
        
        // 다음 발화자가 없는 경우만 말풍선 표시 안 함
        if (string.IsNullOrEmpty(nextSpeaker))
        {
            return;
        }
        
        // 발화자에 따라 대상 캐릭터 결정
        GameObject targetCharacter = null;
        if (speaker == "arona")
        {
            targetCharacter = CharManager.Instance.GetCurrentCharacter(); // 메인 캐릭터 (아로나)
        }
        else if (speaker == "plana")
        {
            targetCharacter = planaInstance; // 서브 캐릭터 (프라나)
        }
        
        if (targetCharacter == null)
        {
            AroplaLogWarning($"ShowNextSpeakerBalloon: target character not found for speaker '{speaker}'");
            return;
        }
        
        // EmotionBalloonManager를 사용하여 말풍선 표시
        nextSpeakerBalloon = EmotionBalloonManager.Instance.SetEmotionBalloonForTarget(
            targetCharacter, 
            nextSpeaker, 
            3f  // 3초정도만 유지
        );
        
        AroplaLog($"🎯 Next speaker balloon shown: {speaker} -> {nextSpeaker}");
    }
    
    // 다음 발화자 아이콘 말풍선 제거
    private void HideNextSpeakerBalloon()
    {
        if (nextSpeakerBalloon != null)
        {
            Destroy(nextSpeakerBalloon);
            nextSpeakerBalloon = null;
            AroplaLog("🎯 Next speaker balloon hidden");
        }
    }
    
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
        streamingCompletionSource = new TaskCompletionSource<AroplaConversationResponse>();
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

                    // TaskCompletionSource를 통해 스트리밍 완료 대기 (최대 10초 타임아웃)
                    var timeoutTask = Task.Delay(10000);
                    var completedTask = await Task.WhenAny(streamingCompletionSource.Task, timeoutTask);
                    
                    if (completedTask == streamingCompletionSource.Task)
                    {
                        var response2 = await streamingCompletionSource.Task;
                        if (response2 != null)
                        {
                            AroplaLog($"Streaming completed - Final speaker: {response2.speaker}");
                            AroplaLog($"Next speaker: {response2.next_speaker}");
                            LogToFile($"Streaming API completed: {response2.speaker} -> {response2.next_speaker}");
                            return response2;
                        }
                    }
                    
                    AroplaLogError("Streaming timeout or no final response2 received");
                    return null;
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

    // 문장 리스트를 AroplaReply 배열로 변환
    private AroplaReply[] CreateReplyListFromSentences(List<string> sentences, string aiLanguage)
    {
        var replyList = new List<AroplaReply>();
        foreach (var sentence in sentences)
        {
            var reply = new AroplaReply();
            
            // 언어에 따라 적절한 필드에 할당
            if (aiLanguage == "ko")
            {
                reply.answer_ko = sentence;
            }
            else if (aiLanguage == "ja" || aiLanguage == "jp")
            {
                reply.answer_jp = sentence;
            }
            else
            {
                reply.answer_en = sentence;
            }
            
            replyList.Add(reply);
        }
        return replyList.ToArray();
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
            // 다음 답변이 오면 기존 next_speaker 말풍선 제거
            HideNextSpeakerBalloon();
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
            
            // TaskCompletionSource를 통해 완료 시그널 전달
            streamingCompletionSource?.TrySetResult(currentStreamResponse);
            
            // 다음 발화자 아이콘 말풍선 표시 (다음 답변이 올 때까지)
            ShowNextSpeakerBalloon(speaker, nextSpeaker);
            
            LogToFile($"Final response: {speaker} -> {nextSpeaker} ({reasoning})");
        }
        catch (Exception ex)
        {
            AroplaLogError($"Error handling final response: {ex.Message}");
            streamingCompletionSource?.TrySetResult(null); // 에러가 나도 완료로 처리
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
        
        streamingCompletionSource?.TrySetResult(currentStreamResponse);
        LogToFile($"Server error: {errorMessage}");
    }

    // UI 관련 메서드들 (CharManager 방식 참고)
    private void ShowAroplaChannelUI()
    {
        // 프리팹이 설정되지 않은 경우 경고
        if (planaPrefab == null)
        {
            AroplaLogWarning("Plana Prefab이 설정되지 않았습니다. 인스펙터에서 설정해주세요.");
            return;
        }

        // Canvas가 없는 경우 다시 찾기
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                AroplaLogError("Canvas를 찾을 수 없어 프라나를 생성할 수 없습니다.");
                return;
            }
        }

        // 프라나 생성 (서브 캐릭터)
        CreatePlanaInstance();

        AroplaLog("아로프라 채널 UI 표시 - 메인(아로나), 서브(프라나) 구성 완료");
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
        
        // 회전 관리를 위한 PhysicsHandler 추가
        if (planaInstance.GetComponent<PhysicsHandler>() == null)
        {
            planaInstance.AddComponent<PhysicsHandler>();
        }

        LogToFile($"Plana instance created at position: {planaPosition}");
    }

    private void HideAroplaChannelUI()
    {
        // 프라나 인스턴스 제거
        if (planaInstance != null)
        {
            Destroy(planaInstance);
            planaInstance = null;
            LogToFile("Plana instance destroyed");
        }

        AroplaLog("아로프라 채널 UI 숨기기 - 프라나 제거됨");
    }

    // 프라나 크기 설정 (메인 캐릭터와 동일하게 설정)
    private void SetPlanaSize()
    {
        if (planaInstance != null)
        {
            GameObject mainChar = CharManager.Instance.GetCurrentCharacter();
            if (mainChar != null)
            {
                // 메인 캐릭터의 스케일을 그대로 사용
                Vector3 mainScale = mainChar.transform.localScale * 0.9f;
                planaInstance.transform.localScale = mainScale;
                
                LogToFile($"Plana size set to match main character: {mainScale}");
            }
            else
            {
                // 메인 캐릭터가 없으면 SettingManager 기준으로 설정
                float char_size = SettingManager.Instance.settings.char_size;
                float initialScale = planaInstance.transform.localScale.x;
                float scaleFactor = initialScale * char_size / 100f;
                
                planaInstance.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                LogToFile($"Plana size set to: {char_size}%");
            }
        }
    }

    // 프라나 핸들러 설정 (CharManager의 핸들러 설정 방식 참고)
    private void SetPlanaHandlers()
    {
        if (planaInstance == null) return;

        // CharManager의 핸들러 설정 로직 참고하여 프라나에 필요한 핸들러들 설정
        SetPlanaDragHandlerVar();
        SetPlanaClickHandlerVar();
        SetPlanaEmotionFaceController();

        LogToFile("Plana handlers configured");
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
    public int GetAroplaConversationCount()
    {
        return MemoryManager.Instance.GetAllConversationMemory("aropla").Count;
    }

    // 마지막 대화 삭제 (MemoryManager 기반)
    public void DeleteLastAroplaConversation()
    {
        MemoryManager.Instance.DeleteRecentDialogue("aropla");
        LogToFile("Deleted last conversation via MemoryManager");
    }
    
    // 아로프라 메모리에서 특정 언어의 메시지 추출
    public List<string> GetAroplaMessagesInLanguage(string language)
    {
        var messages = new List<string>();
        try
        {
            var memories = MemoryManager.Instance.GetMessagesInLanguage(language, "aropla");
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
        
    // 전체 대화 히스토리 가져오기
    public List<Conversation> GetConversationHistory()
    {
        // 아로프라 방식: MemoryManager를 통해 대화 히스토리 반환
        return MemoryManager.Instance.GetAllConversationMemory("aropla");
    }
    
    // 아로나 위치 업데이트 (캐릭터 변경시 등에 사용) - 메인 캐릭터이므로 불필요
    // public void UpdateAronaPosition() - 제거됨 (아로나는 이제 메인 캐릭터)

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

    // 아로나 크기 업데이트 - 메인 캐릭터이므로 CharManager 사용
    // public void UpdateAronaSize() - 제거됨 (CharManager.setCharSize() 사용)

    // 프라나 크기 업데이트 (캐릭터 크기 변경시 등에 사용)  
    public void UpdatePlanaSize()
    {
        if (!isAroplaMode || planaInstance == null) return;
        SetPlanaSize();
    }

    // 아로나 인스턴스 가져오기 - 메인 캐릭터는 CharManager 사용
    // public GameObject GetAronaInstance() - 제거됨 (CharManager.GetCurrentCharacter() 사용)

    // 아로프라 모드에서 캐릭터들이 활성화되어 있는지 확인
    public bool IsAronaActive()
    {
        // 아로나는 메인 캐릭터이므로 CharManager로 확인
        return isAroplaMode && CharManager.Instance.GetCurrentCharacter() != null;
    }

    public bool IsPlanaActive()
    {
        return isAroplaMode && planaInstance != null;
    }

    public bool IsAroplaInstancesActive()
    {
        return isAroplaMode && CharManager.Instance.GetCurrentCharacter() != null && planaInstance != null;
    }

    // 아로나 핸들러 재설정 - 메인 캐릭터는 CharManager에서 관리
    // public void UpdateAronaHandlers() - 제거됨

    // 프라나 핸들러 재설정 (캐릭터 변경시 등에 사용)
    public void UpdatePlanaHandlers()
    {
        if (!isAroplaMode || planaInstance == null) return;
        SetPlanaHandlers();
        LogToFile("Plana handlers updated");
    }

    // 아로나 완전 재초기화 - 메인 캐릭터는 CharManager에서 관리
    // public void RefreshAronaInstance() - 제거됨

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
        
        // 메인 캐릭터(아로나)는 CharManager.setCharSize() 등으로 관리
        RefreshPlanaInstance();
        
        LogToFile("All Aropla instances fully refreshed");
    }

    // 아로나 메시지 표시 (메인 캐릭터의 말풍선 사용)
    private void ShowAronaMessage(string message, string messageKo = "", string messageJp = "", string messageEn = "")
    {
        // APIManager와 동일한 방식으로 다국어 메시지 처리
        string displayKo = !string.IsNullOrEmpty(messageKo) ? messageKo : message;
        string displayJp = !string.IsNullOrEmpty(messageJp) ? messageJp : message;
        string displayEn = !string.IsNullOrEmpty(messageEn) ? messageEn : message;
        
        // 아로나는 메인 캐릭터이므로 AnswerBalloonManager 사용
        AnswerBalloonManager.Instance.ShowAnswerBalloonInf();
        AnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(displayKo, displayJp, displayEn);
        AnswerBalloonManager.Instance.ModifyAnswerBalloonText();
        
        AroplaLog($"Arona Message (Main Character): {message}");
    }

    // 프라나 인스턴스 가져오기
    public GameObject GetPlanaInstance()
    {
        return planaInstance;
    }
}