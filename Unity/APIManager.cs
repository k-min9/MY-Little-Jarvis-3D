using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    // Reply 리스트를 저장할 리스트
    private List<string> replyListKo = new List<string>();
    private List<string> replyListJp = new List<string>();
    private List<string> replyListEn = new List<string>();

    public string query_origin = "";
    private string query_trans = "";
    private string ai_language_out = "en";  // 메모리에 저장할 언어

    private bool isCompleted = false; // 반환이 완료되었는지 여부를 체크하는 플래그
    private bool isResponsedStarted = false; // 첫 반환이 돌아왔는지 여부
    private bool isAnswerStarted = false; // 첫 대답이 시작했는지 여부
    private string logFilePath; // 로그 파일 경로

    private DateTime lastSmallTalkCallTime = DateTime.MinValue;
    private bool isSmallTalkWaiting = false;

    // SmallTalk 트리거 관련
    private string pendingSmallTalkContent = "";  // 대기 중인 잡담 내용
    private DateTime smallTalkTimestamp = DateTime.MinValue;  // 잡담 발생 시간
    private bool isSmallTalkPending = false;  // 트리거 활성 상태

    private GameObject webEmotionBalloonInstance;
    private GameObject questionEmotionBalloonInstance;
    
    // 스크린샷 전송 방식 (true: 캡처→전송→저장, false: 기존 파일 전송)
    private bool isSendScreenshotFirst = true;

    // 서버 관련
    public string ngrokUrl = null;  // 없으면 없는데로 오케이
    public string ngrokStatus = null;  // open, closed  TODO : Close일때 서버켜달라고 요청하기

    // 싱글톤 인스턴스
    private static APIManager instance;
    public static APIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<APIManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 로그 파일 경로 생성 (날짜와 시간 기반으로)
        string dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(Application.persistentDataPath, $"log/api_{dateTime}.txt");

        // 로그 파일 생성
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)); // 디렉토리가 없으면 생성
        File.AppendAllText(logFilePath, $"Log started at: {DateTime.Now}\n");
    }

    private void Start()
    {
        // #if !UNITY_EDITOR
        // supabase에서 ngrok url 가져오기 (에디터에서는 제외)
        CallFetchNgrokJsonData();
        // #endif    
    }

    // Small Talk 호출
    public async void CallSmallTalkStream(string purpose = "잡담", string currentSpeaker = null, string chatIdx = "-1", string aiLanguage = null)
    {
        // 호출 제한 체크
        DateTime now = DateTime.Now;

        if ((now - lastSmallTalkCallTime).TotalSeconds < 3)
        {
            Debug.Log("[SmallTalk] 호출 간격 부족 (3초 미경과)");
            EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "No", 2f);
            return;
        }
        if (isSmallTalkWaiting && (now - lastSmallTalkCallTime).TotalSeconds < 10)
        {
            Debug.Log("[SmallTalk] 답변 대기 중 (10초 미경과)");
            EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "No", 2f);
            return;
        }
        lastSmallTalkCallTime = now;
        isSmallTalkWaiting = true;
        ShowQuestionBalloon();

        // SmallTalk 타이머 리셋 (요청 시작 시)
        GlobalTimeVariableManager.Instance.smallTalkTimer = 0f;

        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;
        
        string url = baseUrl + "/conversation/small_talk";

        // parameter(currentSpeake, chatIdx, aiLanguage)있으면 사용, 없으면 보완
        string resolvedSpeaker = currentSpeaker;
        if (string.IsNullOrEmpty(resolvedSpeaker))
        {
            try
            {
                resolvedSpeaker = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
            }
            catch
            {
                resolvedSpeaker = "arona"; // fallback
            }
        }

        string resolvedAiLanguage = aiLanguage;
        if (string.IsNullOrEmpty(resolvedAiLanguage))
        {
            try
            {
                resolvedAiLanguage = SettingManager.Instance.settings.ai_language ?? "ko";
            }
            catch
            {
                resolvedAiLanguage = "ko";
            }
        }

        string resolvedChatIdx = (!string.IsNullOrEmpty(chatIdx) && chatIdx != "-1")
            ? chatIdx
            : (GameManager.Instance.chatIdxSuccess ?? "-1");

        try
        {
            // 요청전 초기화
            isCompleted = false;
            isResponsedStarted = false;
            isAnswerStarted = false;

            string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
            string contentType = "multipart/form-data; boundary=" + boundary;

            // 요청 준비
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;

            using (MemoryStream memStream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
            {
                // 폼 필드 작성
                void WriteField(string key, string value)
                {
                    writer.WriteLine($"--{boundary}");
                    writer.WriteLine($"Content-Disposition: form-data; name=\"{key}\"");
                    writer.WriteLine();
                    writer.WriteLine(value ?? string.Empty);
                }

                WriteField("query", purpose);
                WriteField("purpose", purpose);
                WriteField("current_speaker", resolvedSpeaker);
                WriteField("ai_language", resolvedAiLanguage);
                WriteField("chatIdx", resolvedChatIdx);
                WriteField("intent_smalltalk", "on");

                // 마지막 boundary
                writer.WriteLine($"--{boundary}--");
                writer.Flush();

                // 본문 전송
                request.ContentLength = memStream.Length;
                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    await memStream.CopyToAsync(requestStream);
                }
            }

            using (WebResponse response = await request.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                string line;
                int curChatIdxNum = int.TryParse(resolvedChatIdx, out var tmpIdx) ? tmpIdx : -1;
                string currentMemoryType = "conversation"; // 응답 타입 추적
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    try
                    {
                        // 풍선기준 최신대화여야 함
                        if (curChatIdxNum >= GameManager.Instance.chatIdxBalloon)
                        {
                            // 최신화 하면서 기존 음성 queue 내용 지워버리기
                            if (GameManager.Instance.chatIdxBalloon != curChatIdxNum)
                            {
                                GameManager.Instance.chatIdxBalloon = curChatIdxNum;
                                VoiceManager.Instance.ResetAudio();
                            }

                            var jsonObject = JObject.Parse(line);
                            Debug.Log("jsonObject Start");
                            Debug.Log(jsonObject.ToString());
                            Debug.Log("jsonObject End");

                            // 생각중 등등의 답변타입체크
                            string replyType = jsonObject["type"]?.ToString() ?? "reply";
                            if (replyType == "thinking")
                            {
                                NoticeManager.Instance.Notice("thinking");
                            }
                            else if (replyType == "webSearch")
                            {
                                NoticeManager.Instance.Notice("webSearch");
                            }
                            else if (replyType == "asking_intent")
                            {
                                // 시스템 메시지로 저장
                                currentMemoryType = "system";
                                
                                // intent_info 확인
                                string intentInfo = jsonObject["intent_info"]?.ToString() ?? "";
                                if (intentInfo == "change_model")
                                {
                                    // 멀티모달 모델 변경 시나리오 시작
                                    StartCoroutine(ScenarioAskManager.Instance.Scenario_S00_AskChangeModel());
                                }
                                else if (intentInfo == "no_image")
                                {
                                    // 이미지 필요 시나리오 시작
                                    StartCoroutine(ScenarioAskManager.Instance.Scenario_S01_AskNeedImage());
                                }
                            }
                            else if (replyType == "trigger")
                            {
                                // 시스템 메시지로 저장
                                currentMemoryType = "system";
                            }
                            else if (replyType == "final")
                            {
                                // final 응답 : Trigger 기동을 위해 ProcessReply 호출하지 않고 다음 응답으로
                                // SmallTalk 타이머 리셋 (대화 종료 시점)
                                GlobalTimeVariableManager.Instance.smallTalkTimer = 0f;
                                continue;
                            }
                            else // reply
                            {
                                if (!isResponsedStarted)
                                {
                                    // 생각 중 말풍선 숨기기
                                    AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

                                    // 안내 말풍선 숨기기
                                    NoticeManager.Instance.DeleteNoticeBalloonInstance();

                                    AnswerBalloonManager.Instance.ShowAnswerBalloonInf();
                                    AnswerBalloonManager.Instance.ChangeAnswerBalloonSpriteLight();
                                    isResponsedStarted = true;

                                    // query 정보 저장
                                    try
                                    {
                                        query_origin = jsonObject["query"]["origin"].ToString();
                                        query_trans = jsonObject["query"]["text"].ToString();
                                    }
                                    catch { }

                                    // AI Info 갱신 시도
                                    try
                                    {
                                        string ai_info_server_type = jsonObject["ai_info"]["server_type"].ToString();
                                        string ai_info_model = jsonObject["ai_info"]["model"].ToString();
                                        string ai_info_prompt = jsonObject["ai_info"]["prompt"].ToString();
                                        string ai_info_lang_used = jsonObject["ai_info"]["lang_used"].ToString();
                                        string ai_info_translator = jsonObject["ai_info"]["translator"].ToString();
                                        string ai_info_time = jsonObject["ai_info"]["time"].ToString();
                                        string ai_info_intent = jsonObject["ai_info"]["time"].ToString();
                                        SettingManager.Instance.RefreshAIInfoText(ai_info_server_type, ai_info_model, ai_info_prompt, ai_info_lang_used, ai_info_translator, ai_info_time, ai_info_intent);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log(ex);
                                    }

                                    // 표정 갱신 시도
                                    try
                                    {
                                        string ai_info_emotion = jsonObject["ai_info"]["emotion"].ToString();
                                        Debug.Log("### emotion : " + ai_info_emotion);
                                        EmotionManager.Instance.ShowEmotionFromEmotion(ai_info_emotion);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log(ex);
                                    }

                                    // Intent 관련 초기화
                                    AnswerBalloonManager.Instance.HideWebImage();
                                    try
                                    {
                                        string intent_info_is_intent_web = jsonObject["intent_info"]["is_intent_web"].ToString();
                                        if (intent_info_is_intent_web == "on") AnswerBalloonManager.Instance.ShowWebImage();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log(ex);
                                    }
                                }

                                // 각 JSON 응답을 기존 로직으로 처리
                                ProcessReply(jsonObject);
                            }
                        }
                        else
                        {
                            Debug.Log("과거대화 : " + curChatIdxNum.ToString() + "/" + GameManager.Instance.chatIdxBalloon.ToString());
                        }
                    }
                    catch (JsonReaderException e)
                    {
                        Debug.Log($"JSON decode error: {e.Message}");
                    }
                }

                if (resolvedChatIdx == GameManager.Instance.chatIdxSuccess)
                {
                    if (currentMemoryType != "system")
                    {
                        // SmallTalk은 메모리에 저장하지 않음 (연관 응답 시에만 저장)
                        // OnFinalResponseReceived() 호출 제거

                        // SmallTalk 트리거 설정 - UI 언어에 맞는 내용 저장
                        string smalltalkText = string.Join(" ", replyListEn);  // 기본값
                        if (SettingManager.Instance.settings.ui_language == "ko")
                        {
                            smalltalkText = string.Join(" ", replyListKo);
                        }
                        else if (SettingManager.Instance.settings.ui_language == "ja" ||
                                 SettingManager.Instance.settings.ui_language == "jp")
                        {
                            smalltalkText = string.Join(" ", replyListJp);
                        }

                        if (!string.IsNullOrEmpty(smalltalkText))
                        {
                            pendingSmallTalkContent = smalltalkText;
                            smallTalkTimestamp = DateTime.Now;
                            isSmallTalkPending = true;
                            Debug.Log($"[SmallTalk] Trigger activated: {smalltalkText}");
                        }
                    }
                    else
                    {
                        Debug.Log("Skipping OnFinalResponseReceived for system type (asking_intent/trigger)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"SmallTalkStream Exception: {ex.Message}");

            // 잡담 실패 오류
            EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "No", 2f);
        }
        finally
        {
            // 잡담 최종 종료
            isSmallTalkWaiting = false;
            isCompleted = true;
            DestroyQuestionBalloon();
            AnswerBalloonManager.Instance.ChangeAnswerBalloonSpriteNormal();
            
            // 말풍선이 현재 활성화되어 있을 때만 30초 후 자동 종료 설정
            if (StatusManager.Instance.IsAnswering)
            {
                AnswerBalloonManager.Instance.hideTimer = 30f;
            }
            
            // SmallTalk 타이머 리셋 (응답 완료 시)
            GlobalTimeVariableManager.Instance.smallTalkTimer = 0f;
        }
    }

    public void CallFetchNgrokJsonData()
    {
        StartCoroutine(FetchNgrokJsonData());
    }

    // 스무고개 게임 호출
    public async void CallMiniGame20QStream(
        string query,
        string secret,
        string themeKey,
        int questionCount,
        int maxQuestions,
        List<Dictionary<string, string>> history,
        List<Dictionary<string, string>> historyQuestion,
        List<string> historySecretList,
        string gameStatus,
        string gameResult,
        string waitingFor,
        string aiLanguage,
        string charName,
        string chatIdx,
        string serverType)
    {
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;
        
        string url = baseUrl + "/game/20q/process";

        Debug.Log($"[20Q API] URL: {url}");
        Debug.Log($"[20Q API] Query: {query}, Secret: {secret}, QuestionCount: {questionCount}");

        try
        {
            // 요청전 초기화
            isCompleted = false;
            isResponsedStarted = false;
            isAnswerStarted = false;

            string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
            string contentType = "multipart/form-data; boundary=" + boundary;

            // history를 JSON 문자열로 변환
            string historyJson = JsonConvert.SerializeObject(history);
            string historyQuestionJson = JsonConvert.SerializeObject(historyQuestion);
            string historySecretListJson = JsonConvert.SerializeObject(historySecretList);

            // 요청 준비
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;

            using (MemoryStream memStream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
            {
                // 폼 필드 작성
                void WriteField(string key, string value)
                {
                    writer.WriteLine($"--{boundary}");
                    writer.WriteLine($"Content-Disposition: form-data; name=\"{key}\"");
                    writer.WriteLine();
                    writer.WriteLine(value ?? string.Empty);
                }

                WriteField("query", query);
                WriteField("secret", secret);
                WriteField("theme_key", themeKey);
                WriteField("question_count", questionCount.ToString());
                WriteField("max_questions", maxQuestions.ToString());
                WriteField("history", historyJson);
                WriteField("history_question", historyQuestionJson);
                WriteField("history_secret_list", historySecretListJson);
                WriteField("game_status", gameStatus);
                WriteField("game_result", gameResult);
                WriteField("waiting_for", waitingFor);
                WriteField("ai_language", aiLanguage);
                WriteField("char", charName);
                WriteField("server_type", serverType);
                WriteField("api_key_Gemini", "");
                WriteField("chatIdx", chatIdx);
                WriteField("show_debug", "false");

                // 마지막 boundary
                writer.WriteLine($"--{boundary}--");
                writer.Flush();

                // 본문 전송
                request.ContentLength = memStream.Length;
                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    await memStream.CopyToAsync(requestStream);
                }
            }

            using (WebResponse response = await request.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                string line;
                int curChatIdxNum = int.TryParse(chatIdx, out var tmpIdx) ? tmpIdx : -1;
                
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    
                    try
                    {
                        // 풍선기준 최신대화여야 함
                        if (curChatIdxNum >= GameManager.Instance.chatIdxBalloon)
                        {
                            // 최신화 하면서 기존 음성 queue 내용 지워버리기
                            if (GameManager.Instance.chatIdxBalloon != curChatIdxNum)
                            {
                                GameManager.Instance.chatIdxBalloon = curChatIdxNum;
                                VoiceManager.Instance.ResetAudio();
                            }

                            var jsonObject = JObject.Parse(line);
                            Debug.Log("[20Q API] Response:");
                            Debug.Log(jsonObject.ToString());

                            // 응답 타입 확인 (서버 상태: thinking, reply)
                            string replyType = jsonObject["type"]?.ToString() ?? "reply";
                            
                            if (replyType == "thinking")
                            {
                                // 서버가 생각중
                                NoticeManager.Instance.Notice("thinking");
                            }
                            else if (replyType == "reply")
                            {                                
                                // 첫 응답 처리
                                if (!isResponsedStarted)
                                {
                                    // 생각 중 말풍선 숨기기
                                    AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();
                                    NoticeManager.Instance.DeleteNoticeBalloonInstance();

                                    AnswerBalloonManager.Instance.ShowAnswerBalloonInf();
                                    AnswerBalloonManager.Instance.ChangeAnswerBalloonSpriteLight();
                                    isResponsedStarted = true;

                                    // AI Info 갱신
                                    try
                                    {
                                        string ai_info_server_type = jsonObject["ai_info"]?["server_type"]?.ToString() ?? "";
                                        string ai_info_model = jsonObject["ai_info"]?["model"]?.ToString() ?? "";
                                        string ai_info_prompt = jsonObject["ai_info"]?["prompt"]?.ToString() ?? "";
                                        string ai_info_lang_used = jsonObject["ai_info"]?["lang_used"]?.ToString() ?? "";
                                        string ai_info_time = jsonObject["ai_info"]?["time"]?.ToString() ?? "";
                                        SettingManager.Instance.RefreshAIInfoText(ai_info_server_type, ai_info_model, ai_info_prompt, ai_info_lang_used, "", ai_info_time, "20q_game");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log($"[20Q API] AI Info 갱신 실패: {ex.Message}");
                                    }
                                }

                                // 답변 처리
                                ProcessReply(jsonObject);
                                
                                // 게임 상태 업데이트 (모든 응답에서 공통 처리)
                                if (MiniGame20QManager.Instance != null && MiniGame20QManager.Instance.Is20QMode())
                                {
                                    // 3-Field System
                                    if (jsonObject["game_status"] != null)
                                    {
                                        MiniGame20QManager.Instance.SetGameStatus(jsonObject["game_status"].ToString());
                                    }
                                    if (jsonObject["game_result"] != null)
                                    {
                                        string result = jsonObject["game_result"].ToString();
                                        // "null" 문자열이면 빈 문자열로 변환
                                        if (result == "null" || result == "None")
                                            result = "";
                                        MiniGame20QManager.Instance.SetGameResult(result);
                                    }
                                    if (jsonObject["waiting_for"] != null)
                                    {
                                        string waiting = jsonObject["waiting_for"].ToString();
                                        // "null" 문자열이면 빈 문자열로 변환
                                        if (waiting == "null" || waiting == "None")
                                            waiting = "";
                                        MiniGame20QManager.Instance.SetWaitingFor(waiting);
                                    }
                                    
                                    // 테마
                                    if (jsonObject["theme_key"] != null)
                                    {
                                        MiniGame20QManager.Instance.SetThemeKey(jsonObject["theme_key"].ToString());
                                    }
                                    if (jsonObject["theme"] != null)
                                    {
                                        MiniGame20QManager.Instance.SetTheme(jsonObject["theme"].ToString());
                                    }
                                    
                                    // 게임 데이터
                                    if (jsonObject["secret"] != null)
                                    {
                                        MiniGame20QManager.Instance.SetSecret(jsonObject["secret"].ToString());
                                    }
                                    if (jsonObject["question_count"] != null)
                                    {
                                        MiniGame20QManager.Instance.SetQuestionCount(int.Parse(jsonObject["question_count"].ToString()));
                                    }
                                    if (jsonObject["max_questions"] != null)
                                    {
                                        MiniGame20QManager.Instance.SetMaxQuestions(int.Parse(jsonObject["max_questions"].ToString()));
                                    }
                                    if (jsonObject["history"] != null)
                                    {
                                        var historyList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonObject["history"].ToString());
                                        MiniGame20QManager.Instance.SetHistory(historyList);
                                    }
                                    if (jsonObject["history_question"] != null)
                                    {
                                        var historyQuestionList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonObject["history_question"].ToString());
                                        MiniGame20QManager.Instance.SetHistoryQuestion(historyQuestionList);
                                    }
                                    if (jsonObject["history_secret_list"] != null)
                                    {
                                        var historySecretListFromServer = JsonConvert.DeserializeObject<List<string>>(jsonObject["history_secret_list"].ToString());
                                        MiniGame20QManager.Instance.SetHistorySecretList(historySecretListFromServer);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.Log($"[20Q API] 과거대화: {curChatIdxNum}/{GameManager.Instance.chatIdxBalloon}");
                        }
                    }
                    catch (JsonReaderException e)
                    {
                        Debug.Log($"[20Q API] JSON decode error: {e.Message}");
                    }
                }

                if (chatIdx == GameManager.Instance.chatIdxSuccess)
                {
                    OnFinalResponseReceived();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"[20Q API] Exception: {ex.Message}");
        }
    }

    // 로그 기록 메서드
    private void LogToFile(string message)
    {
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
    }

    // 이미지를 multipart/form-data 요청에 첨부하고 last_send_image.png로 저장
    private void AttachImageToRequest(StreamWriter writer, MemoryStream memStream, string boundary, byte[] screenshotBytes)
    {
        string sourceFilePath = null; // 파일 경로 추적 (파일에서 읽은 경우)
        byte[] sentImageBytes = null; // 메모리에서 온 경우의 바이트 추적
        
        // 이미지 파일 추가 (screenshotBytes가 있으면 사용, 없으면 기존 파일 확인)
        if (screenshotBytes != null && screenshotBytes.Length > 0)
        {
            Debug.Log($"Sending screenshot from memory: {screenshotBytes.Length} bytes");
            sentImageBytes = screenshotBytes;
            
            // 메모리에서 직접 전송
            writer.WriteLine($"--{boundary}");
            writer.WriteLine($"Content-Disposition: form-data; name=\"image\"; filename=\"panel_capture.png\"");
            writer.WriteLine("Content-Type: image/png");
            writer.WriteLine();
            writer.Flush();
            
            memStream.Write(screenshotBytes, 0, screenshotBytes.Length);
            writer.WriteLine();
            
            // 전송 후 파일로 저장 (로그/백업용)
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string filePath = Path.Combine(directory, "panel_capture.png");
                File.WriteAllBytes(filePath, screenshotBytes);
                Debug.Log($"Screenshot saved at {filePath} (after API call)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save screenshot file: {ex.Message}");
            }
        }
        else
        {
            // 기존 방식: 파일이 존재하면 전송 (하위 호환성)
            string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // clipboard.png 우선 확인, 없으면 panel_capture.png
            string clipboardPath = Path.Combine(directory, "clipboard.png");
            string screenshotPath = Path.Combine(directory, "panel_capture.png");
            
            string filePath = null;
            if (File.Exists(clipboardPath))
            {
                filePath = clipboardPath;
            }
            else if (File.Exists(screenshotPath))
            {
                filePath = screenshotPath;
            }
            
            if (!string.IsNullOrEmpty(filePath))
            {
                Debug.Log($"Sending image from file: {filePath}");
                sourceFilePath = filePath; // 파일 경로 저장 (복사용)
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string fileName = Path.GetFileName(filePath);

                writer.WriteLine($"--{boundary}");
                writer.WriteLine($"Content-Disposition: form-data; name=\"image\"; filename=\"{fileName}\"");
                writer.WriteLine("Content-Type: image/png");
                writer.WriteLine();
                writer.Flush(); // 헤더 부분을 먼저 메모리에 씀

                memStream.Write(fileBytes, 0, fileBytes.Length);
                writer.WriteLine();
            }
        }
        
        // 전송한 이미지를 last_send_image.png로 저장
        SaveLastSentImage(sentImageBytes, sourceFilePath);
    }

    // 마지막으로 전송한 이미지를 last_send_image.png로 저장
    // 파일에서 읽은 경우: File.Copy()로 직접 복사 (빠름)
    // 메모리에서 온 경우: byte[]를 파일로 저장
    private void SaveLastSentImage(byte[] imageBytes, string sourceFilePath = null)
    {
        try
        {
            string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string lastSendImagePath = Path.Combine(directory, "last_send_image.png");
            
            // 파일에서 읽은 경우: 직접 복사 (메모리 사용 없이 빠름)
            if (!string.IsNullOrEmpty(sourceFilePath) && File.Exists(sourceFilePath))
            {
                File.Copy(sourceFilePath, lastSendImagePath, true); // overwrite: true
                Debug.Log($"Last sent image copied from {sourceFilePath} to {lastSendImagePath}");
            }
            // 메모리에서 온 경우: byte[]를 파일로 저장
            else if (imageBytes != null)
            {
                File.WriteAllBytes(lastSendImagePath, imageBytes);
                Debug.Log($"Last sent image saved at {lastSendImagePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to save last_send_image: {ex.Message}");
        }
    }

    // FetchStreamingData에서 호출할 함수
    private void ProcessReply(JObject jsonObject)
    {

        LogToFile("ProcessReply started."); // ProcessReply 시작 로그

        // type 확인
        string type = jsonObject["type"]?.ToString() ?? "reply";
        LogToFile("ProcessReply type : " + type);

        // 초기화
        replyListKo = new List<string>();
        replyListJp = new List<string>();
        replyListEn = new List<string>();

        // 반환된 JSON 객체에서 "reply_list"를 가져오기
        JToken replyToken = jsonObject["reply_list"];
        string chatIdx = jsonObject["chat_idx"]?.ToString() ?? "-1";
        ai_language_out = jsonObject["ai_language_out"]?.ToString() ?? "";
        // Debug.Log("ProcessReply chatIdx chk");
        // Debug.Log(chatIdx + "/" + GameManager.Instance.chatIdxSuccess.ToString());

        // 이중 체크 중... 음성체크와 별개로 대화는 뒤에서 저장 되어야하는데 그게 저지 됨?
        if (chatIdx != GameManager.Instance.chatIdxSuccess.ToString())
        {
            Debug.Log("chatIdx Too Old : " + chatIdx + "/" + GameManager.Instance.chatIdxSuccess.ToString());
            return;  // 현재 대화가 아님
        }

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
            // answerballoon 갱신

            string replyKo = string.Join(" ", replyListKo);
            string replyJp = string.Join(" ", replyListJp);
            string replyEn = string.Join(" ", replyListEn);
            LogToFile("replyKo : " + replyKo); // ProcessReply 시작 로그
            LogToFile("replyJp : " + replyJp); // ProcessReply 시작 로그
            LogToFile("replyEn : " + replyEn); // ProcessReply 시작 로그

            AnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(replyKo, replyJp, replyEn);  // Answerballoon 정보 갱신
            AnswerBalloonManager.Instance.ModifyAnswerBalloonText();  // 정보토대 답변

            // 음성 API 호출
            if (answerVoice != null)
            {
                if (SettingManager.Instance.settings.sound_language == "ko" || SettingManager.Instance.settings.sound_language == "en")
                {
                    GetKoWavFromAPI(answerVoice, chatIdx);
                }
                if (SettingManager.Instance.settings.sound_language == "jp")
                {
                    GetJpWavFromAPI(answerVoice, chatIdx);
                }
            }
        }
    }

    // GeminiDirect 전용 ProcessReply (최초 수신 시 UI 초기화 포함)
    private void ProcessReplyGeminiDirect(JObject jsonObject)
    {
        // 최초 수신 시 UI 초기화 및 리스트 초기화
        if (!isResponsedStarted)
        {
            // 리스트 초기화 (최초 1회만)
            replyListKo = new List<string>();
            replyListJp = new List<string>();
            replyListEn = new List<string>();
            
            // 생각 중 말풍선 숨기기
            AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

            // 안내 말풍선 숨기기
            NoticeManager.Instance.DeleteNoticeBalloonInstance();

            // 전송시작 말풍선 제거
            if (webEmotionBalloonInstance != null)
            {
                Destroy(webEmotionBalloonInstance);
                webEmotionBalloonInstance = null;
            }

            AnswerBalloonManager.Instance.ShowAnswerBalloonInf();
            AnswerBalloonManager.Instance.ChangeAnswerBalloonSpriteLight();
            isResponsedStarted = true;

            // AI Info 내용 갱신
            try
            {
                string ai_info_server_type = jsonObject["ai_info"]["server_type"]?.ToString() ?? "Google-Direct";
                string ai_info_model = jsonObject["ai_info"]["model"]?.ToString() ?? "gemma-3-27b-it";
                string ai_info_prompt = "gemma";
                string ai_info_lang_used = jsonObject["ai_info"]["lang_used"]?.ToString() ?? "";
                string ai_info_translator = "N/A";
                string ai_info_time = jsonObject["ai_info"]["time"]?.ToString() ?? "0 sec";
                string ai_info_intent = "";
                SettingManager.Instance.RefreshAIInfoText(ai_info_server_type, ai_info_model, ai_info_prompt, ai_info_lang_used, ai_info_translator, ai_info_time, ai_info_intent);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeminiDirect] Failed to refresh AI info: {ex.Message}");
            }

            // 표정은 기본값으로 (Gemini에서 감정 분석 미지원)
            EmotionManager.Instance.ShowEmotionFromEmotion("Neutral");

            // Web/Image 의도는 비활성화
            AnswerBalloonManager.Instance.HideWebImage();
        }

        // chatIdx 체크
        string chatIdx = jsonObject["chat_idx"]?.ToString() ?? "-1";
        ai_language_out = jsonObject["ai_language_out"]?.ToString() ?? "";
        
        if (chatIdx != GameManager.Instance.chatIdxSuccess.ToString())
        {
            Debug.Log("chatIdx Too Old : " + chatIdx + "/" + GameManager.Instance.chatIdxSuccess.ToString());
            return;
        }

        // reply_list에서 문장 추가 (누적)
        JToken replyToken = jsonObject["reply_list"];
        if (replyToken != null && replyToken.Type == JTokenType.Array)
        {
            string answerVoice = null;
            foreach (var reply in replyToken)
            {
                string answerJp = reply["answer_jp"]?.ToString() ?? string.Empty;
                string answerKo = reply["answer_ko"]?.ToString() ?? string.Empty;
                string answerEn = reply["answer_en"]?.ToString() ?? string.Empty;

                // 각각의 답변을 리스트에 누적 추가
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
            
            // AnswerBalloon 갱신 (누적된 전체 내용)
            string replyKo = string.Join(" ", replyListKo);
            string replyJp = string.Join(" ", replyListJp);
            string replyEn = string.Join(" ", replyListEn);

            AnswerBalloonManager.Instance.ModifyAnswerBalloonTextInfo(replyKo, replyJp, replyEn);
            AnswerBalloonManager.Instance.ModifyAnswerBalloonText();

            // 음성 API 호출
            if (answerVoice != null)
            {
                // 기존 로직은 번역이 없어서 사용 불가. 향후 보강 필요.
                // if (SettingManager.Instance.settings.sound_language == "ko" || SettingManager.Instance.settings.sound_language == "en")
                // {
                //     GetKoWavFromAPI(answerVoice, chatIdx);
                // }
                // if (SettingManager.Instance.settings.sound_language == "jp")
                // {
                //     GetJpWavFromAPI(answerVoice, chatIdx);
                // }
                
                // GeminiDirect 음성 호출 (기존 로직 대체용)
                // TODO` : 향후 언어로직감지 구현시우선 (현재 번역모듈 없음)
                string answerVoiceLang = SettingManager.Instance.settings.ui_language ?? "en";
                // Debug.Log("answerVoiceLang : " + answerVoiceLang);
                if (answerVoiceLang == "ko" || answerVoiceLang == "en")
                {
                    GetKoWavFromAPI(answerVoice, chatIdx);
                }
                if (answerVoiceLang == "jp")
                {
                    GetJpWavFromAPI(answerVoice, chatIdx);
                }
            }
        }
    }

    // 최종 반환 완료 시 호출될 함수
    private void OnFinalResponseReceived()
    {
        isCompleted = true;
        AnswerBalloonManager.Instance.ChangeAnswerBalloonSpriteNormal();  // 대답완료 sprite

        Debug.Log("All replies have been received.");
        LogToFile("ProcessReply completed."); // ProcessReply 완료 로그
        DestroyQuestionBalloon();

        // 다국어 답변 조립
        string replyKo = string.Join(" ", replyListKo);
        string replyJp = string.Join(" ", replyListJp);
        string replyEn = string.Join(" ", replyListEn);

        // 표시언어에 따른 대표 메시지 선택
        string reply = replyEn; // 기본값
        if (SettingManager.Instance.settings.ui_language == "ja" || SettingManager.Instance.settings.ui_language == "jp")
        {
            reply = replyJp;
        }
        else if (SettingManager.Instance.settings.ui_language == "ko")
        {
            reply = replyKo;
        }

        Debug.Log("Answer Finished : " + reply);
        
        // 일반 대화로 저장
        if (query_trans != "")  // 영어 번역이 필요한 LLM 사용시 번역기 답변
        {
            // 사용자 메시지 저장 (번역된 영어 버전)
            MemoryManager.Instance.SaveConversationMemory("player", "user", query_trans, query_trans, query_trans, query_trans);
            
            // 캐릭터 응답 저장 (다국어 포함)
            MemoryManager.Instance.SaveConversationMemory("character", "assistant", reply, replyKo, replyJp, replyEn);
        }
        else
        {
            // 사용자 메시지 저장 (원본)
            MemoryManager.Instance.SaveConversationMemory("player", "user", query_origin, query_origin, query_origin, query_origin);
            
            // 캐릭터 응답 저장 (다국어 포함)
            MemoryManager.Instance.SaveConversationMemory("character", "assistant", reply, replyKo, replyJp, replyEn);
        }
    }

    // 스트리밍 데이터를 가져오는 메서드
    public async Task FetchStreamingData(string url, Dictionary<string, string> data, byte[] screenshotBytes = null)
    {
        Debug.Log("FetchStreamingData START");
        string jsonData = JsonConvert.SerializeObject(data);
        string curChatIdx = data["chatIdx"];
        int curChatIdxNum = int.Parse(curChatIdx);

        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
        string contentType = "multipart/form-data; boundary=" + boundary;

        // 전송시작 말풍선
        if (webEmotionBalloonInstance != null)
        {
            Destroy(webEmotionBalloonInstance);
            webEmotionBalloonInstance = null;
        }
        webEmotionBalloonInstance = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Question");

        try
        {
            // 요청전 초기화
            isCompleted = false;
            isResponsedStarted = false;
            isAnswerStarted = false;

            // HttpWebRequest 객체를 사용하여 요청 생성
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;

            using (MemoryStream memStream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
            {
                // JSON 데이터 추가
                foreach (var entry in data)
                {
                    writer.WriteLine($"--{boundary}");
                    writer.WriteLine($"Content-Disposition: form-data; name=\"{entry.Key}\"");
                    writer.WriteLine();
                    writer.WriteLine(entry.Value);
                }

                // intent_image가 "off"가 아닐 때만 이미지 전송
                string intent_image = data.ContainsKey("intent_image") ? data["intent_image"] : "off";
                if (intent_image != "off")
                {
                    AttachImageToRequest(writer, memStream, boundary, screenshotBytes);
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

            // 응답을 비동기로 읽기
            using (WebResponse response = await request.GetResponseAsync())
            {
                Debug.Log($"Response Status Code: {(int)((HttpWebResponse)response).StatusCode}");

                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string line;
                    string currentMemoryType = "conversation"; // 응답 타입 추적

                    // SmallTalk 연관성 판단 결과 저장
                    string latestIntentSmallTalkAnswer = "off";
                    string latestSmallTalkQuery = "";

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                // 풍선기준 최신대화여야 함
                                if (curChatIdxNum >= GameManager.Instance.chatIdxBalloon)
                                {
                                    // 최신화 하면서 기존 음성 queue 내용 지워버리기
                                    if (GameManager.Instance.chatIdxBalloon != curChatIdxNum)
                                    {
                                        GameManager.Instance.chatIdxBalloon = curChatIdxNum;
                                        VoiceManager.Instance.ResetAudio();
                                    }

                                    var jsonObject = JObject.Parse(line);
                                    Debug.Log("jsonObject Start");
                                    Debug.Log(jsonObject.ToString());
                                    Debug.Log("jsonObject End");

                                    // intent_info에서 SmallTalk 연관성 저장
                                    try
                                    {
                                        if (jsonObject["intent_info"] != null)
                                        {
                                            latestIntentSmallTalkAnswer = jsonObject["intent_info"]["is_intent_smalltalk_answer"]?.ToString() ?? "off";
                                            latestSmallTalkQuery = jsonObject["intent_info"]["smalltalk_query"]?.ToString() ?? "";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Log($"[SmallTalk] Failed to parse intent_info: {ex.Message}");
                                    }

                                    // 생각중 등등의 답변타입체크
                                    string replyType = jsonObject["type"]?.ToString() ?? "reply";
                                    if (replyType == "thinking") // "생각 중" 상태
                                    {
                                        // 기존의 풍선 있을경우 파괴
                                        if (webEmotionBalloonInstance != null)
                                        {
                                            Destroy(webEmotionBalloonInstance);
                                            webEmotionBalloonInstance = null;
                                        }
                                        
                                        NoticeManager.Instance.Notice("thinking");
                                    }
                                    else if (replyType == "webSearch")
                                    {
                                        NoticeManager.Instance.Notice("webSearch");
                                    }
                                    else if (replyType == "asking_intent")
                                    {
                                        // 시스템 메시지로 저장
                                        currentMemoryType = "system";
                                        
                                        // intent_info 확인
                                        string intentInfo = jsonObject["intent_info"]?.ToString() ?? "";
                                        if (intentInfo == "change_model")
                                        {
                                            // 멀티모달 모델 변경 시나리오 시작
                                            StartCoroutine(ScenarioAskManager.Instance.Scenario_S00_AskChangeModel());
                                        }
                                        else if (intentInfo == "no_image")
                                        {
                                            // 이미지 필요 시나리오 시작
                                            StartCoroutine(ScenarioAskManager.Instance.Scenario_S01_AskNeedImage());
                                        }
                                    }
                                    else if (replyType == "trigger")
                                    {
                                        // 시스템 메시지로 저장
                                        currentMemoryType = "system";
                                    }
                                    else  // replyType == "reply"
                                    {
                                        // 최초 수신
                                        if (!isResponsedStarted)
                                        {
                                            // 생각 중 말풍선 숨기기
                                            AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

                                            // 안내 말풍선 숨기기
                                            NoticeManager.Instance.DeleteNoticeBalloonInstance();

                                            // 전송시작 말풍선 제거
                                            if (webEmotionBalloonInstance != null)
                                            {
                                                Destroy(webEmotionBalloonInstance);
                                                webEmotionBalloonInstance = null;
                                            }

                                            AnswerBalloonManager.Instance.ShowAnswerBalloonInf();
                                            AnswerBalloonManager.Instance.ChangeAnswerBalloonSpriteLight();  // 대답중 sprite
                                            isResponsedStarted = true;

                                            // Debug.Log(jsonObject["query"]);
                                            // Debug.Log(jsonObject["query"]["text"]);
                                            query_origin = jsonObject["query"]["origin"].ToString();
                                            query_trans = jsonObject["query"]["text"].ToString();

                                            // Setting - AI Info 내용 갱신
                                            try
                                            {
                                                string ai_info_server_type = jsonObject["ai_info"]["server_type"].ToString();
                                                string ai_info_model = jsonObject["ai_info"]["model"].ToString();
                                                string ai_info_prompt = jsonObject["ai_info"]["prompt"].ToString();
                                                string ai_info_lang_used = jsonObject["ai_info"]["lang_used"].ToString();
                                                string ai_info_translator = jsonObject["ai_info"]["translator"].ToString();
                                                string ai_info_time = jsonObject["ai_info"]["time"].ToString();
                                                string ai_info_intent = jsonObject["ai_info"]["time"].ToString();
                                                SettingManager.Instance.RefreshAIInfoText(ai_info_server_type, ai_info_model, ai_info_prompt, ai_info_lang_used, ai_info_translator, ai_info_time, ai_info_intent);
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.Log(ex);
                                                // Debug.LogException(ex);
                                            }

                                            // Setting - AI Info 표정 갱신
                                            // Joy/Anger/Confusion/Sadness/Surprise/Neutral
                                            try
                                            {
                                                string ai_info_emotion = jsonObject["ai_info"]["emotion"].ToString();
                                                Debug.Log("### emotion : " + ai_info_emotion);

                                                EmotionManager.Instance.ShowEmotionFromEmotion(ai_info_emotion);

                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.Log(ex);
                                                // Debug.LogException(ex);
                                            }

                                            // 의도(Intent) 관련 정보 갱신
                                            // 우선 AnswerBalloon의 web마크 비활성화
                                            AnswerBalloonManager.Instance.HideWebImage();
                                            try
                                            {
                                                string intent_info_is_intent_web = jsonObject["intent_info"]["is_intent_web"].ToString();  // on, off
                                                string intent_info_web_info = jsonObject["intent_info"]["web_info"].ToString();
                                                string intent_info_is_intent_image = jsonObject["intent_info"]["is_intent_image"].ToString();  // on, off
                                                string intent_info_image_info = jsonObject["intent_info"]["image_info"].ToString();

                                                if (intent_info_is_intent_web == "on") AnswerBalloonManager.Instance.ShowWebImage();
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.Log(ex);
                                                // Debug.LogException(ex);
                                            }

                                        }
                                        ProcessReply(jsonObject); // 각 JSON 응답을 처리
                                    }
                                }
                                else
                                {
                                    Debug.Log("과거대화 : " + curChatIdxNum.ToString() + "/" + GameManager.Instance.chatIdxBalloon.ToString());
                                }
                            }
                            catch (JsonReaderException e)
                            {
                                Debug.Log($"JSON decode error: {e.Message}");
                            }
                        }
                    }

                    if (curChatIdx == GameManager.Instance.chatIdxSuccess)
                    {
                        if (currentMemoryType != "system")
                        {
                            // SmallTalk 연관성이 있으면 잡담을 먼저 메모리에 저장
                            if (latestIntentSmallTalkAnswer == "on" && !string.IsNullOrEmpty(latestSmallTalkQuery))
                            {
                                Debug.Log("[SmallTalk] Related conversation detected. Saving smalltalk first.");
                                MemoryManager.Instance.SaveConversationMemory(
                                    "character",
                                    "assistant",
                                    latestSmallTalkQuery,
                                    latestSmallTalkQuery,
                                    latestSmallTalkQuery,
                                    latestSmallTalkQuery
                                );
                            }

                            OnFinalResponseReceived(); // 최종 반환 완료 시 함수 호출
                        }
                        else
                        {
                            Debug.Log("Skipping OnFinalResponseReceived for system type (asking_intent/trigger)");
                        }
                    }
                }
            }
        }
        catch (WebException ex)
        {
            Debug.LogError($"WebException: {ex.Message}");
            if (ex.Response != null)
            {
                using (Stream errorStream = ex.Response.GetResponseStream())
                using (StreamReader errorReader = new StreamReader(errorStream))
                {
                    string errorResponse = await errorReader.ReadToEndAsync();
                    Debug.LogError($"Error Response: {errorResponse}");
                }
            }

            // 오류 안내 말풍선      
            if (webEmotionBalloonInstance != null)
            {
                Destroy(webEmotionBalloonInstance);
                webEmotionBalloonInstance = null;
            }

            Debug.Log("API error");
            EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "No", 2f);
            return;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");

            // 오류 안내 말풍선      
            if (webEmotionBalloonInstance != null)
            {
                Destroy(webEmotionBalloonInstance);
                webEmotionBalloonInstance = null;
            }

            Debug.Log("API error");
            EmotionBalloonManager.Instance.ShowEmotionBalloonForSec(CharManager.Instance.GetCurrentCharacter(), "No", 2f);
            return;
        }
    }
    
    // === Furigana 변환 API ===
    
    [System.Serializable]
    public class FuriganaResponse
    {
        public string type;
        public string furigana;
        public string original;
        public string time;
    }
    
    // Furigana 변환 API 호출
    public void CallFuriganaAPI(string text, System.Action<string> callback)
    {
        StartCoroutine(CallFuriganaAPICoroutine(text, callback));
    }
    
    private IEnumerator CallFuriganaAPICoroutine(string text, System.Action<string> callback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("[APIManager] Furigana: Empty text provided");
            callback?.Invoke(text);
            yield break;
        }
        
        // Base URL 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        yield return new WaitUntil(() => tcs.Task.IsCompleted);
        string baseUrl = tcs.Task.Result;
        
        string url = baseUrl + "/furigana";
        Debug.Log($"[APIManager] Calling Furigana API: {url}");
        
        // Form 데이터 생성
        WWWForm form = new WWWForm();
        form.AddField("text", text);
        
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 스트리밍 응답 파싱
                string[] lines = request.downloadHandler.text.Split('\n');
                string furiganaResult = null;
                
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    try
                    {
                        var response = JsonUtility.FromJson<FuriganaResponse>(line);
                        if (response.type == "reply" && !string.IsNullOrEmpty(response.furigana))
                        {
                            furiganaResult = response.furigana;
                            Debug.Log($"[APIManager] Furigana result: {furiganaResult}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[APIManager] Failed to parse furigana response line: {e.Message}");
                    }
                }
                
                callback?.Invoke(furiganaResult ?? text);
            }
            else
            {
                Debug.LogError($"[APIManager] Furigana API error: {request.error}");
                callback?.Invoke(text);  // 실패 시 원본 텍스트 반환
            }
        }
    }

    // chatHandler에서 호출
    public async void CallConversationStream(string query, string chatIdx = "-1", string ai_lang_in = "")
    {
        // 빈 쿼리 체크 (방어적 프로그래밍)
        if (string.IsNullOrWhiteSpace(query))
        {
            Debug.LogWarning("[APIManager] Query is empty or whitespace. Showing empty query scenario.");
            StartCoroutine(ScenarioCommonManager.Instance.Run_C99_EmptyQuery());
            return;
        }

        // 공용변수 최신화
        if (chatIdx != "-1")
        {
            GameManager.Instance.chatIdxSuccess = chatIdx;
        }
        // 애니메이션 재생 초기화
        AnimationManager.Instance.Idle();

        // 일단 안드로이드판은 server_type sample로...
#if UNITY_ANDROID
            await CallConversationStreamGeminiDirect(query, chatIdx, ai_lang_in);
            return;
#endif
        // Test용 : 공개시점 0.8.0 + TODO : 외부제어로 끌 수 있는법 검토해야 함. 버전 업데이터와 함께 고민할 것.
        // if (true)
        // {
        //     await CallConversationStreamGeminiDirect(query, chatIdx, ai_lang_in);
        //     return;
        // }

        // server_type이 2 (Google)인 경우 Gemini 전용 함수 호출 [TODO : Sample용. 차후 제거.]
        int server_type_idx = SettingManager.Instance.settings.server_type_idx;  // 0: Auto, 1: Local, 2: Google, 3: OpenRouter
        if (server_type_idx == 2 || server_type_idx == 0)
        {
            await CallConversationStreamGemini(query, chatIdx, ai_lang_in);
            return;
        }


        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;
        
        // API 호출을 위한 URL 구성
        string streamUrl = baseUrl + "/conversation_stream";
        Debug.Log("streamUrl : " + streamUrl);

        // 닉네임 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string player_name = SettingManager.Instance.settings.player_name;
        // string ui_language = SettingManager.Instance.settings.ui_language ?? "";  // 설정 ui에서 쓰는 언어에 따라가는 경향이 있음
        string ai_language = SettingManager.Instance.settings.ai_language ?? "";
        string ai_language_in = ai_lang_in;  // stt 에서 가져온 언어 있으면 사용(en, jp, ko 안에 포함되는지는 서버쪽에서 확인)
        string ai_language_out = SettingManager.Instance.settings.ai_language_out ?? "";
        string ai_web_search = SettingManager.Instance.settings.ai_web_search ?? "off";  // 0 : off, 1 : on, 2: force
        string ai_emotion = SettingManager.Instance.settings.ai_emotion ?? "off";  // 0 : off, 1 : on
        if (GameManager.Instance.isWebSearchForced)  // 강제 검색 메뉴
        {
            GameManager.Instance.isWebSearchForced = false;
            ai_web_search = "force";
        }

        // 실제로 보낼 이미지가 있는지 확인
        bool hasImageToSend = false;

        // 1. 클립보드에 이미지가 있는지 확인
        hasImageToSend = ClipboardManager.Instance.HasImageInClipboard(); 
        
        // 2. 스크린샷 영역이 설정되어 있는지 확인
        if (!hasImageToSend)
        {
            ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
            if (sm != null)
            {
                hasImageToSend = sm.IsScreenshotAreaSet();
            }
        }

        // 이미지 사용 상태 가져오기
        string intent_image = "off";
        if (hasImageToSend) {
            if (StatusManager.Instance.IsChatting)
            {
                // 채팅창을 통한 호출 - changeUseImageInfo()로 변경된 값 사용
                intent_image = ChatBalloonManager.Instance.GetUseImageInfo();
                Debug.Log($"[Image Info] ChatBalloon (IsChatting=true): intent_image={intent_image}");
            }
            else
            {
                // STT 등을 통한 직접 호출 - SettingManager 원본 값 사용
                int ai_use_image_idx = SettingManager.Instance.settings.ai_use_image_idx;
                if (ai_use_image_idx == 0) intent_image = "off";
                else if (ai_use_image_idx == 1) intent_image = "auto";
                else if (ai_use_image_idx == 2) intent_image = "force";
                else intent_image = "off";
                Debug.Log($"[Image Info] Direct call (IsChatting=false): ai_use_image_idx={ai_use_image_idx}, intent_image={intent_image}");
            }
        }

        // 현재 모델의 멀티모달 지원 여부 확인
        string currentModelId = GetCurrentModelId();  // 현재 선택된 모델 ID 가져오기
        bool isModelSupportsImage = ModelDataMultimodal.SupportsImage(currentModelId);
        // Debug.Log($"[Multimodal Check] Model: '{currentModelId}', Supports Image: {isModelSupportsImage}");

        // 이미지 인식이 필요한 경우(auto/force) 멀티모달 체크
        if (intent_image != "off")
        {
            // 멀티모달 미지원 모델인 경우
            if (!isModelSupportsImage)
            {
                Debug.Log($"[Multimodal Check] Current model '{currentModelId}' does not support image. intent_image={intent_image}");
                // 모델 변경할지 물어보기
                if (SettingManager.Instance.settings.isAskChangeToMultimodal)
                {
                    if (hasImageToSend)
                    {
                        StartCoroutine(ScenarioAskManager.Instance.Scenario_S00_AskChangeModel());
                        return;
                    }
                }
            }
        }

        // 멀티모달인데 이미지가 없을 경우
        if (isModelSupportsImage && !hasImageToSend)
        {
            Debug.Log("No Image to Send");
        }

        // 변수 설정 시작
        string intent_confirm = "false";
        if (SettingManager.Instance.settings.confirmUserIntent) intent_confirm = "true";

        // intent_image가 "off"가 아닐 때 이미지 준비
        byte[] screenshotBytes = null;
        if (intent_image != "off")
        {
            // ChatBalloonManager에서 이미지 소스 판단
            string imageSource = ChatBalloonManager.Instance.GetImageSource();
            
            if (isSendScreenshotFirst)
            {
                // 메모리에서 직접 전송 (빠름)
                if (imageSource == "clipboard")
                {
                    screenshotBytes = ClipboardManager.Instance.GetImageBytesFromClipboard();
                    if (screenshotBytes != null)
                    {
                        Debug.Log($"Clipboard image loaded: {screenshotBytes.Length} bytes");
                    }
                }
                else if (imageSource == "screenshot")
                {
                    var tcsScreenshot = new TaskCompletionSource<byte[]>();
                    ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
                    if (sm != null && sm.IsScreenshotAreaSet())
                    {
                        sm.StartCoroutine(sm.CaptureScreenshotToMemory((bytes) => 
                        {
                            tcsScreenshot.SetResult(bytes);
                        }));
                        screenshotBytes = await tcsScreenshot.Task;
                        Debug.Log($"Screenshot captured for API: {(screenshotBytes != null ? screenshotBytes.Length : 0)} bytes");
                    }
                }
            }
            else
            {
                // 파일 저장 후 AttachImageToRequest에서 읽어서 전송
                if (imageSource == "clipboard")
                {
                    ClipboardManager.Instance.SaveImageFromClipboard();
                    Debug.Log("Clipboard image saved to file");
                }
                // screenshot는 파일로 이미 저장되어 있음 (panel_capture.png)
            }
        }

        var memory = MemoryManager.Instance.GetAllConversationMemory();
        string memoryJson = JsonConvert.SerializeObject(memory);
        string guidelineJson = UIUserCardManager.Instance.GetGuidelineListJson();
        string situationJson = UIChatSituationManager.Instance.GetCurUIChatSituationInfoJson();
        string server_type = SettingManager.Instance.settings.server_type ?? "Auto";  // 0: Auto, 1: Local, 2: Google, 3: OpenRouter
        
        // 서비스별 모델명
        // settings에서 model_name_Local (id)를 가져와서 FileName으로 변환
        string modelId = SettingManager.Instance.settings.model_name_Local ?? "";
        string modelFileName = ModelDataLocal.GetFileNameById(modelId);
        string model_name_Local = modelFileName;
        string model_name_Gemini = SettingManager.Instance.settings.model_name_Gemini ?? "";
        string model_name_OpenRouter = SettingManager.Instance.settings.model_name_OpenRouter ?? "";
        string model_name_ChatGPT = SettingManager.Instance.settings.model_name_ChatGPT ?? "";
        
        // Custom 모델명 (설정값 또는 현재 UI 상태 기반 조회)
        // - 드롭다운 선택 시: ModelDataCustom.ModelOptions[index].Id
        // - 직접 입력 시: InputField 텍스트
        string model_name_Custom = SettingManager.Instance.GetCurrentCustomModelName();
        
        string server_local_mode = SettingManager.Instance.settings.server_local_mode ?? "GPU";

        // SmallTalk 트리거 조건 체크
        string intent_smalltalk_answer = "off";
        string query_smalltalk = "";

        if (isSmallTalkPending)
        {
            double secondsSinceSmallTalk = (DateTime.Now - smallTalkTimestamp).TotalSeconds;

            if (secondsSinceSmallTalk <= 30)
            {
                intent_smalltalk_answer = "on";
                query_smalltalk = pendingSmallTalkContent;
                Debug.Log($"[SmallTalk] Trigger sent. Elapsed: {secondsSinceSmallTalk:F1}s");
            }
            else
            {
                Debug.Log($"[SmallTalk] Trigger expired. Elapsed: {secondsSinceSmallTalk:F1}s");
            }

            // 최초 1회 후 무조건 리셋
            pendingSmallTalkContent = "";
            smallTalkTimestamp = DateTime.MinValue;
            isSmallTalkPending = false;
        }



        // 요청 데이터 구성
        var requestData = new Dictionary<string, string>
        {
            { "query", query },  // 질문내용
            { "player", player_name }, // 설정의 플레이어 이름 : 초기값 sensei
            { "char", nickname }, // 닉네임으로 캐릭터 이름 추가 : 초기값 arona
            { "ai_language", ai_language }, // 추론언어로 한입, 영입영출 등 조절(normal, prefer, ko, en, jp)
            { "ai_language_in", ai_language_in }, // 추론언어로 한입, 영입영출 등 조절(ko, en, jp)
            { "ai_language_out", ai_language_out }, // 추론언어로 한출, 영입영출 등 조절(ko, en, jp)
            { "ai_emotion", ai_emotion},
            { "api_key_Gemini", ""},
            { "api_key_OpenRouter", ""},
            { "api_key_ChatGPT", ""},
            { "memory", memoryJson },
            { "guideline_list", guidelineJson },
            { "situation", situationJson} ,
            { "chatIdx", chatIdx},
            { "intent_web", ai_web_search},  // off, on, force
            { "intent_image", intent_image},  // on, off, force
            { "intent_confirm", intent_confirm},  // on, off : 의도행동확인 받기 여부[web검색하실까요 선생님?]
            { "intent_confirm_type", ""},  // "", web, light : 의도행동확인 종류
            { "intent_confirm_answer", ""},  // true, false : 의도행동확인에 대한 답변[재생성시 확인 없이 적용하기 위해]
            { "regenerate_count", GameManager.Instance.chatIdxRegenerateCount.ToString()},
            { "server_type", server_type},
            { "model_name_Local", model_name_Local},
            { "model_name_Gemini", model_name_Gemini},
            { "model_name_OpenRouter", model_name_OpenRouter},
            { "model_name_ChatGPT", model_name_ChatGPT},
            { "model_name_Custom", model_name_Custom},
            { "server_local_mode", server_local_mode},
            { "intent_smalltalk_answer", intent_smalltalk_answer},  // SmallTalk 연관성 플래그
            { "query_smalltalk", query_smalltalk}
        };

        await FetchStreamingData(streamUrl, requestData, screenshotBytes);
    }

    // // Gemini 전용 대화 스트림 호출 함수(For Sample/Deprecated)
    public async Task CallConversationStreamGemini(string query, string chatIdx = "-1", string ai_lang_in = "")
    {
        // 공용변수 최신화
        if (chatIdx != "-1")
        {
            GameManager.Instance.chatIdxSuccess = chatIdx;
        }
        // 애니메이션 재생 초기화
        AnimationManager.Instance.Idle();

        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;

        string streamUrl = baseUrl + "/conversation_stream_gemini";
        Debug.Log("Gemini streamUrl : " + streamUrl);

        // 닉네임 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string player_name = SettingManager.Instance.settings.player_name;
        string ai_language = SettingManager.Instance.settings.ai_language ?? "";
        string ai_language_in = ai_lang_in;  // stt 에서 가져온 언어 있으면 사용
        string ai_language_out = SettingManager.Instance.settings.ai_language_out ?? "";

        // 이미지 사용 상태 가져오기
        string intent_image;
        if (StatusManager.Instance.IsChatting)
        {
            // 채팅창을 통한 호출 - changeUseImageInfo()로 변경된 값 사용
            intent_image = ChatBalloonManager.Instance.GetUseImageInfo();
            Debug.Log($"[Image Info] ChatBalloon (IsChatting=true): intent_image={intent_image}");
        }
        else
        {
            // STT 등을 통한 직접 호출 - SettingManager 원본 값 사용
            int ai_use_image_idx = SettingManager.Instance.settings.ai_use_image_idx;
            if (ai_use_image_idx == 0) intent_image = "off";
            else if (ai_use_image_idx == 1) intent_image = "auto";
            else if (ai_use_image_idx == 2) intent_image = "force";
            else intent_image = "off";
            Debug.Log($"[Image Info] Direct call (IsChatting=false): ai_use_image_idx={ai_use_image_idx}, intent_image={intent_image}");
        }
        
        // intent_image가 "off"가 아닐 때 스크린샷 캡처
        byte[] screenshotBytes = null;
        if (isSendScreenshotFirst && intent_image != "off")
        {
            var tcsScreenshot = new TaskCompletionSource<byte[]>();
            ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
            if (sm != null)
            {
                sm.StartCoroutine(sm.CaptureScreenshotToMemory((bytes) => 
                {
                    tcsScreenshot.SetResult(bytes);
                }));
                screenshotBytes = await tcsScreenshot.Task;
                Debug.Log($"Screenshot captured for Gemini API: {(screenshotBytes != null ? screenshotBytes.Length : 0)} bytes");
            }
            else
            {
                Debug.LogWarning("ScreenshotManager not found. Cannot capture screenshot.");
            }
        }

        var memory = MemoryManager.Instance.GetAllConversationMemory();
        string memoryJson = JsonConvert.SerializeObject(memory);
        string guidelineJson = UIUserCardManager.Instance.GetGuidelineListJson();
        string situationJson = UIChatSituationManager.Instance.GetCurUIChatSituationInfoJson();

        // Gemini 전용 요청 데이터 구성 (간소화된 파라미터)
        var requestData = new Dictionary<string, string>
        {
            { "query", query },
            { "player", player_name },
            { "char", nickname },
            { "ai_language", ai_language },
            { "ai_language_in", ai_language_in },
            { "ai_language_out", ai_language_out },
            { "api_key_Gemini", "" },
            { "memory", memoryJson },
            { "guideline_list", guidelineJson },
            { "situation", situationJson },
            { "chatIdx", chatIdx },
            { "regenerate_count", GameManager.Instance.chatIdxRegenerateCount.ToString() }
        };

        await FetchStreamingData(streamUrl, requestData, screenshotBytes);
    }

    public async Task CallConversationStreamGeminiDirect(string query, string chatIdx = "-1", string ai_lang_in = "")
    {
        // 공용변수 최신화
        if (chatIdx != "-1")
        {
            GameManager.Instance.chatIdxSuccess = chatIdx;
        }
        // 애니메이션 재생 초기화
        AnimationManager.Instance.Idle();
        
        // 요청 전 초기화 (FetchStreamingData와 동일)
        isCompleted = false;
        isResponsedStarted = false;
        query_origin = query;
        query_trans = "";
        
        // 전송시작 말풍선
        if (webEmotionBalloonInstance != null)
        {
            Destroy(webEmotionBalloonInstance);
            webEmotionBalloonInstance = null;
        }
        webEmotionBalloonInstance = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Question");

        // 닉네임 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string player_name = SettingManager.Instance.settings.player_name;
        string ai_language = SettingManager.Instance.settings.ai_language ?? "";
        string ai_language_in = ai_lang_in;  // stt 에서 가져온 언어 있으면 사용
        string ai_language_out = SettingManager.Instance.settings.ai_language_out ?? "";

        // 메모리, 가이드라인, 상황 정보 가져오기
        var memory = MemoryManager.Instance.GetAllConversationMemory();
        
        // JSON을 List로 변환
        List<Dictionary<string, string>> memoryList = new List<Dictionary<string, string>>();
        foreach (var mem in memory)
        {
            memoryList.Add(new Dictionary<string, string>
            {
                { "speaker", mem.speaker },
                { "message", mem.message }
            });
        }
        
        // 가이드라인 리스트 변환
        string guidelineJson = UIUserCardManager.Instance.GetGuidelineListJson();
        List<string> guidelineList = new List<string>();
        try
        {
            var guidelineArray = JArray.Parse(guidelineJson);
            foreach (var item in guidelineArray)
            {
                guidelineList.Add(item.ToString());
            }
        }
        catch
        {
            Debug.LogWarning("[GeminiDirect] Failed to parse guideline list");
        }
        
        // 상황 정보 변환
        string situationJson = UIChatSituationManager.Instance.GetCurUIChatSituationInfoJson();
        Dictionary<string, object> situationDict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(situationJson))
        {
            try
            {
                var situationObj = JObject.Parse(situationJson);
                foreach (var prop in situationObj.Properties())
                {
                    situationDict[prop.Name] = prop.Value.ToObject<object>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeminiDirect] Failed to parse situation dict: {ex.Message}");
            }
        }

        // ApiGeminiDirectClient 호출
        await ApiGeminiDirectClient.Instance.CallGeminiStreamDirect(
            query: query,
            playerName: player_name,
            charName: nickname,
            aiLanguage: ai_language,
            memoryList: memoryList,
            guidelineList: guidelineList,
            situationDict: situationDict,
            chatIdx: chatIdx,
            onChunkReceived: ProcessReplyGeminiDirect,
            onComplete: () => OnFinalResponseReceived()
        );
    }


    // 로컬 서버 모델 Release 호출
    public async void CallReleaseModel()
    {
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;
        
        string url = baseUrl + "/model/release";
        Debug.Log("CallReleaseModel URL: " + url);

        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            // 요청 본문 (빈 내용)
            byte[] byteArray = new byte[0];
            request.ContentLength = byteArray.Length;

            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Debug.Log("Model released successfully");
                }
                else
                {
                    Debug.LogError($"Error releasing model: {response.StatusCode}");
                }
            }
        }
        catch (WebException ex)
        {
            Debug.LogError($"WebException in CallReleaseModel: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in CallReleaseModel: {ex.Message}");
        }
    }

    // 로컬 서버 모델 Load 호출
    public async void CallLoadModel()
    {
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;
        
        string url = baseUrl + "/model/load";
        Debug.Log("CallLoadModel URL: " + url);

        // settings에서 model_name_Local (id)를 가져와서 FileName으로 변환
        string modelId = SettingManager.Instance.settings.model_name_Local ?? "";
        string modelFileName = ModelDataLocal.GetFileNameById(modelId);
        string serverLocalMode = SettingManager.Instance.settings.server_local_mode ?? "GPU";

        Debug.Log($"Loading model: {modelFileName}, Mode: {serverLocalMode}");

        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";

            var data = new { model_name_Local = modelFileName, server_local_mode = serverLocalMode };
            string jsonData = JsonConvert.SerializeObject(data);
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);
            request.ContentLength = byteArray.Length;

            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 응답 본문 읽기
                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string responseText = await reader.ReadToEndAsync();
                        Debug.Log($"Model loaded successfully: {modelFileName} ({serverLocalMode})");
                        Debug.Log($"Response: {responseText}");

                        try
                        {
                            // JSON 파싱하여 process_info 추출
                            var jsonResponse = JObject.Parse(responseText);
                            if (jsonResponse["process_info"] != null)
                            {
                                int llmProcessPid = jsonResponse["process_info"]["llm_process_pid"]?.ToObject<int>() ?? -1;
                                
                                // JarvisServerManager에 PID 정보 저장
                                if (llmProcessPid > 0)
                                {
                                    JarvisServerManager.Instance.SetProcessInfo(llmProcessPid);
                                    Debug.Log($"LLM Process PID saved: {llmProcessPid}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to parse process_info: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Error loading model: {response.StatusCode}");
                }
            }
        }
        catch (WebException ex)
        {
            Debug.LogError($"WebException in CallLoadModel: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception in CallLoadModel: {ex.Message}");
        }
    }

    public async void GetKoWavFromAPI(string text, string chatIdx, string nickname = null)
    {
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;

        // dev_voice 사용 여부 (설치 상태 또는 DevSound 토글)
        bool shouldUseDevServer = SettingManager.Instance.GetInstallStatus() < 2   // no install, lite
                                || SettingManager.Instance.IsDevSoundEnabled();    // DevSound 토글 또는 Android
        if (shouldUseDevServer)
        {
            // TaskCompletionSource를 사용하여 콜백을 async/await로 변환
            var tcs2 = new TaskCompletionSource<string>();
            
            ServerManager.Instance.GetServerUrlFromServerId("dev_voice", (url) =>
            {
                tcs2.SetResult(url);
            });
            
            // dev_voice 서버 URL을 기다림
            string devVoiceUrl = await tcs2.Task;
            
            if (!string.IsNullOrEmpty(devVoiceUrl))
            {
                Debug.Log("dev_voice 서버 URL: " + devVoiceUrl);
                baseUrl = devVoiceUrl;
            }
            else
            {
                Debug.LogWarning("dev_voice 서버 URL을 가져올 수 없습니다. 기본 URL을 사용합니다.");
            }
        }
        
        // baseUrl 유효성 체크
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[GetKoWavFromAPI] baseUrl이 비어있습니다. 음성 합성을 건너뜁니다.");
            return;
        }
        
        // API 호출을 위한 URL 구성
        string url = baseUrl + "/getSound/ko"; // GET + Uri.EscapeDataString(text);

        // 닉네임 가져오기 (optional 파라미터가 있으면 사용, 없으면 현재 캐릭터)
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        }

        // HttpWebRequest 객체 생성
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";

        var requestData = new Dictionary<string, string>
        {
            { "text", text},
            { "char", nickname},
            { "lang", "ko" },  // 현재 두 함수를 합치지 못하는 이유. 서버쪽은 주소 하나로 통합해서 lang으로 ja인지 아닌지만 판단 중.
            // { "lang", SettingManager.Instance.settings.sound_language.ToString() },
            { "speed", SettingManager.Instance.settings.sound_speedMaster.ToString()},
            { "chatIdx", chatIdx}
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);


        try
        {
            // 요청 본문에 데이터 쓰기
            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // 비동기 방식으로 요청 보내기
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                // 요청이 성공했는지 확인
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 헤더에서 Chat-Idx 값을 가져와, 현재 대화보다 과거일 경우에는 queue에 넣지 않음
                    string chatIdxHeader = response.Headers["Chat-Idx"];
                    int chatIdxHeaderNum = int.Parse(chatIdxHeader);
                    if (GameManager.Instance.chatIdxBalloon > chatIdxHeaderNum)
                    {
                        Debug.Log("과거대화 : " + GameManager.Instance.chatIdxBalloon.ToString() + "/" + chatIdxHeaderNum.ToString());
                        return;
                    }

                    // 응답 스트림을 읽어서 파일에 저장
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            byte[] wavData = ReadFully(responseStream);

                            // StreamingAssets 경로에 WAV 파일 저장
                            SaveWavToFile(wavData);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Error fetching WAV file: {response.StatusCode}");
                }
            }
        }
        catch (WebException ex)
        {
            Debug.LogError($"WebException: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");
        }
    }

    public async void GetJpWavFromAPI(string text, string chatIdx, string nickname = null)
    {
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;

        // dev_voice 사용 여부 (설치 상태 또는 DevSound 토글)
        bool shouldUseDevServer = SettingManager.Instance.GetInstallStatus() < 2   // no install, lite
                                || SettingManager.Instance.IsDevSoundEnabled();    // DevSound 토글 또는 Android
        if (shouldUseDevServer)
        {
            // TaskCompletionSource를 사용하여 콜백을 async/await로 변환
            var tcs2 = new TaskCompletionSource<string>();
            
            ServerManager.Instance.GetServerUrlFromServerId("dev_voice", (url) =>
            {
                tcs2.SetResult(url);
            });
            
            // dev_voice 서버 URL을 기다림
            string devVoiceUrl = await tcs2.Task;
            
            if (!string.IsNullOrEmpty(devVoiceUrl))
            {
                Debug.Log("dev_voice 서버 URL: " + devVoiceUrl);
                baseUrl = devVoiceUrl;
            }
            else
            {
                Debug.LogWarning("dev_voice 서버 URL을 가져올 수 없습니다. 기본 URL을 사용합니다.");
            }
        }

        // baseUrl 유효성 체크
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[GetJpWavFromAPI] baseUrl이 비어있습니다. 음성 합성을 건너뜁니다.");
            return;
        }

        string url = baseUrl + "/getSound/jp"; // GET + Uri.EscapeDataString(text);

        // 닉네임 가져오기 (optional 파라미터가 있으면 사용, 없으면 현재 캐릭터)
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        }

        // HttpWebRequest 객체 생성
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";

        var requestData = new Dictionary<string, string>
        {
            { "text", text},
            { "char", nickname},
            { "lang", "ja"},
            { "speed", SettingManager.Instance.settings.sound_speedMaster.ToString()},
            { "chatIdx", chatIdx}
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);


        try
        {
            // 요청 본문에 데이터 쓰기
            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // 비동기 방식으로 요청 보내기
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                // 요청이 성공했는지 확인
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 응답 스트림을 읽어서 파일에 저장
                    using (Stream responseStream = response.GetResponseStream())
                    {

                        // 헤더에서 Chat-Idx 값을 가져와, 현재 대화보다 과거일 경우에는 queue에 넣지 않음
                        string chatIdxHeader = response.Headers["Chat-Idx"];
                        int chatIdxHeaderNum = int.Parse(chatIdxHeader);
                        if (GameManager.Instance.chatIdxBalloon > chatIdxHeaderNum)
                        {
                            Debug.Log("과거대화 : " + GameManager.Instance.chatIdxBalloon.ToString() + "/" + chatIdxHeaderNum.ToString());
                            return;
                        }

                        if (responseStream != null)
                        {
                            byte[] wavData = ReadFully(responseStream);

                            // persistentDataPath에 WAV 파일 저장
                            SaveWavToFile(wavData);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Error fetching WAV file: {response.StatusCode}");
                }
            }
        }
        catch (WebException ex)
        {
            // early stop 등의 거절도 여기로 보냈음
            Debug.LogError($"WebException: {ex.Message}\nerror Text : {text}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");
        }
    }

    
    public async void GetHowlingFromAPI(string text)
    {
        // API 호출을 위한 URL 구성
        // string baseUrl = ServerManager.Instance.GetBaseUrl();
        string baseUrl = "http://127.0.0.1:5050";  // dev->5050포트용
        string url = baseUrl + "/howling"; 
        Debug.Log("url : " + url);

        // HttpWebRequest 객체 생성
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";

        var requestData = new Dictionary<string, string>
        {
            { "text", text},
            { "lang", ""},
            { "is_play", "true"},
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);


        try
        {
            // 요청 본문에 데이터 쓰기
            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // 비동기 방식으로 요청 보내기
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                // 요청이 성공했는지 확인
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Debug.LogError($"Howling : " + text);
                    // 저장 지금은 필요없음
                    // // 응답 스트림을 읽어서 파일에 저장
                    // using (Stream responseStream = response.GetResponseStream())
                    // {
                    //     if (responseStream != null)
                    //     {
                    //         byte[] wavData = ReadFully(responseStream);

                    //         // StreamingAssets 경로에 WAV 파일 저장
                    //         SaveWavToFile(wavData);
                    //     }
                    // }
                }
                else
                {
                    Debug.LogError($"Error fetching WAV file: {response.StatusCode}");
                }
            }
        }
        catch (WebException ex)
        {
            Debug.LogError($"WebException: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception: {ex.Message}");
        }
    }


    // 스트림을 바이트 배열로 변환하는 함수
    private byte[] ReadFully(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    // 파일을 persistentDataPath에 저장
    private void SaveWavToFile(byte[] wavData)
    {
        // WAV 파일의 길이를 계산
        float wavDuration = GetWavDuration(wavData);
        // Debug.Log("wavDuration : " + wavDuration);

        // 10초를 초과하면 저장/재생하지 않음
        // if (wavDuration > 10f)
        // {
        //     Debug.LogWarning("WAV file is longer than 10 seconds. File will not be saved.");
        //     return;
        // }

        string filePath = Path.Combine(Application.persistentDataPath, "response.wav");
        try
        {
            File.WriteAllBytes(filePath, wavData);
            VoiceManager.Instance.LoadAudioWavToQueue();
            // Debug.Log($"WAV file saved to: {filePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"Error saving WAV file: {e.Message}");
        }
    }

    // WAV 데이터에서 길이 계산
    private float GetWavDuration(byte[] wavData)
    {
        // WAV 헤더를 분석
        using (MemoryStream ms = new MemoryStream(wavData))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            // "RIFF" 체크
            string riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
            {
                Debug.LogError("Invalid WAV file: Missing RIFF header.");
                return 0f;
            }

            reader.ReadInt32(); // Chunk Size (무시)
            string wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
            {
                Debug.LogError("Invalid WAV file: Missing WAVE header.");
                return 0f;
            }

            // "fmt " 체크
            string fmt = new string(reader.ReadChars(4));
            if (fmt != "fmt ")
            {
                Debug.LogError("Invalid WAV file: Missing fmt header.");
                return 0f;
            }

            int fmtChunkSize = reader.ReadInt32();
            reader.ReadInt16(); // Audio Format (무시)
            int numChannels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            reader.ReadInt32(); // Byte Rate (무시)
            reader.ReadInt16(); // Block Align (무시)
            int bitsPerSample = reader.ReadInt16();

            // "data" 체크
            string dataHeader = new string(reader.ReadChars(4));
            if (dataHeader != "data")
            {
                Debug.LogError("Invalid WAV file: Missing data header.");
                return 0f;
            }

            int dataSize = reader.ReadInt32();

            // WAV 길이 계산
            int totalSamples = dataSize / (bitsPerSample / 8 * numChannels);
            return (float)totalSamples / sampleRate;
        }
    }

    [System.Serializable]
    public class NgrokJsonResponse
    {
        public string url;
        public string status;  // open, closed
    }

    // JSON 파일을 다운로드하고 url 값을 반환
    private IEnumerator FetchNgrokJsonData()
    {
        // string ngrokSupabaseUrl = "https://lxmkzckwzasvmypfoapl.supabase.co/storage/v1/object/private/json_bucket/my_little_jarvis_plus_ngrok_server.json";

        string server_id = "temp";
        try
        {
            server_id = SettingManager.Instance.settings.server_id;
        }
        catch (Exception ex)
        {
            Debug.Log("Setting server_id failed. use init value");
        }

        Debug.Log("server_id : " + server_id);
        string ngrokSupabaseUrl = "https://lxmkzckwzasvmypfoapl.supabase.co/storage/v1/object/sign/json_bucket/my_little_jarvis_plus_ngrok_server.json?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1cmwiOiJqc29uX2J1Y2tldC9teV9saXR0bGVfamFydmlzX3BsdXNfbmdyb2tfc2VydmVyLmpzb24iLCJpYXQiOjE3MzM4Mzg4MjYsImV4cCI6MjA0OTE5ODgyNn0.ykDVTXYVXNnKJL5lXILSk0iOqt0_7UeKZqOd1Qv_pSY&t=2024-12-10T13%3A53%3A47.907Z";
        string supabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imx4bWt6Y2t3emFzdm15cGZvYXBsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzM4MzUxNzQsImV4cCI6MjA0OTQxMTE3NH0.zmEKHhIcQa4ODekS2skgknlXi8Hbd8JjpjBlFZpPsJ8";

        using (UnityWebRequest request = UnityWebRequest.Get(ngrokSupabaseUrl))
        {
            // 인증 헤더 추가
            request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");

            // 서버 요청
            yield return request.SendWebRequest();

            // 에러 처리
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching JSON data: {request.error}");
            }
            else
            {
                // JSON 데이터를 문자열로 가져옴
                string jsonResponse = request.downloadHandler.text;

                // JSON 데이터 파싱
                var fullData = JsonConvert.DeserializeObject<Dictionary<string, NgrokJsonResponse>>(jsonResponse);
                if (fullData != null && fullData.ContainsKey(server_id))
                {
                    NgrokJsonResponse data = fullData[server_id];
                    Debug.Log($"Fetched URL: {data.url}");

                    ngrokUrl = data.url;
                    ngrokStatus = data.status;

                    if (ngrokStatus == "closed")
                    {
                        NoticeBalloonManager.Instance.ModifyNoticeBalloonText("Supabase Server Closed");
                    }
                    else if (ngrokStatus != "open")
                    {
                        NoticeBalloonManager.Instance.ModifyNoticeBalloonText("Supabase Server Not Opened");
                    }
                }
                else
                {
                    ngrokUrl = null;
                    ngrokStatus = null;
                    Debug.LogError($"Server ID '{server_id}' not found in JSON data.");
                }
            }
        }
    }

    private void ShowQuestionBalloon()
    {
        if (questionEmotionBalloonInstance != null)
        {
            Destroy(questionEmotionBalloonInstance);
            questionEmotionBalloonInstance = null;
        }
        questionEmotionBalloonInstance = EmotionBalloonManager.Instance.ShowEmotionBalloon(CharManager.Instance.GetCurrentCharacter(), "Question");
    }

    private void DestroyQuestionBalloon()
    {
        if (questionEmotionBalloonInstance != null)
        {
            Destroy(questionEmotionBalloonInstance);
            questionEmotionBalloonInstance = null;
        }
    }

    // 현재 선택된 모델 ID 가져오기
    private string GetCurrentModelId()
    {
        string server_type = SettingManager.Instance.settings.server_type ?? "Auto";
        
        if (server_type == "Local")
        {
            return SettingManager.Instance.settings.model_name_Local ?? "";
        }
        else if (server_type == "Google")
        {
            return SettingManager.Instance.settings.model_name_Gemini ?? "";
        }
        else if (server_type == "OpenRouter")
        {
            return SettingManager.Instance.settings.model_name_OpenRouter ?? "";
        }
        else if (server_type == "ChatGPT")
        {
            return SettingManager.Instance.settings.model_name_ChatGPT ?? "";
        }
        else if (server_type == "Custom")
        {
            return SettingManager.Instance.settings.model_name_Custom ?? "";
        }
        else // Auto 또는 기타
        {
            return SettingManager.Instance.settings.model_name_Local ?? "";
        }
    }

    // PaddleOCR API 호출
    public async void CallPaddleOCR(
        byte[] imageBytes, 
        string targetLang = "en", 
        bool autoDetect = false, 
        bool useTranslate = false,
        string originLang = "ja",
        bool isFormality = true,
        bool isSentence = true,
        int mergeThreshold = 0,
        bool saveResult = true,
        bool saveImage = true,
        bool isDebug = true,
        System.Action<OCRResult> callback = null)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("[PaddleOCR] Image bytes is null or empty");
            callback?.Invoke(null);
            return;
        }

        try
        {
            // dev_ocr_translate 서버 URL 가져오기
            var tcs = new TaskCompletionSource<string>();
            ServerManager.Instance.GetServerUrlFromServerId("dev_ocr_translate", (url) =>
            {
                tcs.SetResult(url);
            });
            string baseUrl = await tcs.Task;
            
            if (string.IsNullOrEmpty(baseUrl))
            {
                Debug.LogError("[PaddleOCR] dev_ocr_translate 서버 URL을 가져올 수 없습니다.");
                callback?.Invoke(null);
                return;
            }
            
            string url = baseUrl + "/paddle/ocr";
            Debug.Log($"[PaddleOCR] URL: {url}, useTranslate: {useTranslate}, origin_lang: {originLang}, target_lang: {targetLang}");

            // 파라미터 Dictionary 구성 (사용자 선호 스타일!)
            var parameters = new Dictionary<string, string>
            {
                { "use_translate", useTranslate ? "true" : "false" },
                { "save_result", saveResult ? "true" : "false" },
                { "save_image", saveImage ? "true" : "false" },
                { "is_debug", isDebug ? "true" : "false" },
                { "is_sentence", isSentence ? "true" : "false" },
                { "merge_threshold", mergeThreshold.ToString() }
            };

            // 번역 사용 시 추가 파라미터
            if (useTranslate)
            {
                parameters["target_lang"] = targetLang;
                parameters["origin_lang"] = originLang;
                parameters["is_formality"] = isFormality ? "true" : "false";
            }
            else
            {
                // 번역 미사용 시 origin_lang 설정
                if (!autoDetect)
                {
                    parameters["origin_lang"] = originLang;
                }
                else
                {
                    parameters["origin_lang_auto_detect"] = "true";
                }
            }

            string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
            string contentType = "multipart/form-data; boundary=" + boundary;

            // 요청 준비
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;

            using (MemoryStream memStream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
            {
                // 이미지 파일 전송
                writer.WriteLine($"--{boundary}");
                writer.WriteLine($"Content-Disposition: form-data; name=\"image\"; filename=\"fullscreen.png\"");
                writer.WriteLine("Content-Type: image/png");
                writer.WriteLine();
                writer.Flush();

                memStream.Write(imageBytes, 0, imageBytes.Length);
                writer.WriteLine();

                // Dictionary 파라미터들을 multipart로 전송
                foreach (var param in parameters)
                {
                    writer.WriteLine($"--{boundary}");
                    writer.WriteLine($"Content-Disposition: form-data; name=\"{param.Key}\"");
                    writer.WriteLine();
                    writer.WriteLine(param.Value);
                }

                // 마지막 boundary
                writer.WriteLine($"--{boundary}--");
                writer.Flush();

                // 본문 전송
                request.ContentLength = memStream.Length;
                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    await memStream.CopyToAsync(requestStream);
                }
            }

            Debug.Log("[PaddleOCR] Request sent, waiting for response...");

            // 응답 받기
            using (WebResponse response = await request.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                string responseText = await reader.ReadToEndAsync();
                Debug.Log($"[PaddleOCR] Response received: {responseText.Substring(0, Math.Min(500, responseText.Length))}...");

                try
                {
                    var jsonResponse = JObject.Parse(responseText);
                    
                    // 응답 파싱
                    if (jsonResponse["status"]?.ToString() == "success" && jsonResponse["ocr_with_region"] != null)
                    {
                        var ocrData = jsonResponse["ocr_with_region"];
                        
                        OCRResult ocrResult = new OCRResult
                        {
                            labels = new List<string>(),
                            quad_boxes = new List<List<List<float>>>()
                        };

                        // labels 파싱
                        if (ocrData["labels"] != null)
                        {
                            foreach (var label in ocrData["labels"])
                            {
                                ocrResult.labels.Add(label.ToString());
                            }
                        }

                        // quad_boxes 파싱 및 형식 변환
                        // PaddleOCR: [x1,y1,x2,y2,x3,y3,x4,y4] (평탄 리스트)
                        // 내부 형식: [[[x1,y1],[x2,y2],[x3,y3],[x4,y4]]] (중첩 리스트)
                        if (ocrData["quad_boxes"] != null)
                        {
                            foreach (var quadBox in ocrData["quad_boxes"])
                            {
                                List<List<float>> quad = new List<List<float>>();
                                
                                // PaddleOCR의 평탄 리스트를 중첩 리스트로 변환
                                // [x1,y1,x2,y2,x3,y3,x4,y4] -> [[x1,y1],[x2,y2],[x3,y3],[x4,y4]]
                                List<float> coords = new List<float>();
                                foreach (var coord in quadBox)
                                {
                                    coords.Add((float)coord);
                                }
                                
                                // 8개 좌표를 4개의 점으로 변환
                                if (coords.Count >= 8)
                                {
                                    for (int i = 0; i < 8; i += 2)
                                    {
                                        List<float> point = new List<float> { coords[i], coords[i + 1] };
                                        quad.Add(point);
                                    }
                                    ocrResult.quad_boxes.Add(quad);
                                }
                            }
                        }

                        Debug.Log($"[PaddleOCR] Parsed {ocrResult.labels.Count} text regions");
                        callback?.Invoke(ocrResult);
                    }
                    else
                    {
                        Debug.LogWarning("[PaddleOCR] No ocr_with_region data in response or status is not success");
                        callback?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PaddleOCR] Failed to parse response: {ex.Message}");
                    callback?.Invoke(null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PaddleOCR] Exception: {ex.Message}");
            callback?.Invoke(null);
        }
    }


}

// OCR + 번역 결과 데이터 구조
[System.Serializable]
public class OCRWithTranslateResult
{
    public List<string> labels;         // 번역된 텍스트
    public List<string> labels_origin;  // 원문 텍스트
    public List<List<List<float>>> quad_boxes;
}
