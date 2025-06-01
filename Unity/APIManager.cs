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
    private string logFilePath; // 로그 파일 경로

    // 서버 관련
    public string ngrokUrl = null;  // 없으면 없는데로 오케이
    public string ngrokStatus = null;  // open, closed  TODO : Close일때 서버켜달라고 요청하기

    // 싱글톤 인스턴스
    private static APIManager instance;

    // 싱글톤 인스턴스에 접근하는 속성
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
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

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

    public void CallFetchNgrokJsonData() {
        StartCoroutine(FetchNgrokJsonData());
    }

    // 로그 기록 메서드
    private void LogToFile(string message)
    {
        File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
    }

    // FetchStreamingData에서 호출할 함수
    private void ProcessReply(JObject jsonObject)
    {

        LogToFile("ProcessReply started."); // ProcessReply 시작 로그

        // 초기화
        replyListKo = new List<string>();
        replyListJp = new List<string>();
        replyListEn = new List<string>();

        // 반환된 JSON 객체에서 "reply_list"를 가져오기
        JToken replyToken = jsonObject["reply_list"];
        string chatIdx = jsonObject["chat_idx"].ToString();
        ai_language_out = jsonObject["ai_language_out"].ToString();
        // Debug.Log("ProcessReply chatIdx chk");
        // Debug.Log(chatIdx + "/" + GameManager.Instance.chatIdxSuccess.ToString());

        // 이중 체크 중... 음성체크와 별개로 대화는 뒤에서 저장 되어야하는데 그게 저지 됨?
        if (chatIdx != GameManager.Instance.chatIdxSuccess.ToString()) {
            Debug.Log("chatIdx Too Old : " + chatIdx + "/"+ GameManager.Instance.chatIdxSuccess.ToString());
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
                    if (SettingManager.Instance.settings.sound_language == "jp") {
                        answerVoice = answerJp;
                    }
                }

                if (!string.IsNullOrEmpty(answerKo))
                {
                    replyListKo.Add(answerKo);
                    if (SettingManager.Instance.settings.sound_language == "ko") {
                        answerVoice = answerKo;
                    }
                }
                if (!string.IsNullOrEmpty(answerEn))
                {
                    replyListEn.Add(answerEn);
                    if (SettingManager.Instance.settings.sound_language == "en") {
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
            if (answerVoice != null) {
                if (SettingManager.Instance.settings.sound_language == "ko" || SettingManager.Instance.settings.sound_language == "en") {
                    GetKoWavFromAPI(answerVoice, chatIdx);
                }
                if (SettingManager.Instance.settings.sound_language == "jp") {
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
        
        
        // 표시언어로 저장 : SettingManager.Instance.settings.ui_language
        string reply = string.Join(" ", replyListEn);

        Debug.Log("Answer Finished : " + reply);
        if (query_trans != "")  // 영어 번역이 필요한 LLM 사용시 번역기 답변
        {
            MemoryManager.Instance.SaveConversationMemory("player", query_trans);
            MemoryManager.Instance.SaveConversationMemory("character", reply);
        }
        else
        {
            MemoryManager.Instance.SaveConversationMemory("player", query_origin);
            if (SettingManager.Instance.settings.ui_language == "ja" || SettingManager.Instance.settings.ui_language == "jp")
            {
                reply = string.Join(" ", replyListJp);
            } else if (SettingManager.Instance.settings.ui_language == "ko")
            { 
                reply = string.Join(" ", replyListKo);
            }
            MemoryManager.Instance.SaveConversationMemory("character", reply);
        }
    }

    // 스트리밍 데이터를 가져오는 메서드
    public async Task FetchStreamingData(string url, Dictionary<string, string> data)
    {
        string jsonData = JsonConvert.SerializeObject(data);
        string curChatIdx = data["chatIdx"];
        int curChatIdxNum = int.Parse(curChatIdx);

        string boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
        string contentType = "multipart/form-data; boundary=" + boundary;

        try
        {
            // 요청전 초기화
            isCompleted = false;
            isResponsedStarted = false;

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

                // 이미지 파일 추가 (파일이 존재할 경우)
                string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string filePath = Path.Combine(directory, "panel_capture.png");
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
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
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                // 풍선기준 최신대화여야 함
                                if (curChatIdxNum >= GameManager.Instance.chatIdxBalloon) {
                                    // 최신화 하면서 기존 음성 queue 내용 지워버리기
                                    if (GameManager.Instance.chatIdxBalloon != curChatIdxNum) {
                                        GameManager.Instance.chatIdxBalloon = curChatIdxNum;
                                        VoiceManager.Instance.ResetAudio();
                                    }

                                    var jsonObject = JObject.Parse(line);
                                    // Debug.Log(jsonObject.ToString());

                                    if (!isResponsedStarted)
                                    {
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
                                } else {
                                    Debug.Log("과거대화 : " + curChatIdxNum.ToString() + "/" + GameManager.Instance.chatIdxBalloon.ToString());
                                }
                            }
                            catch (JsonReaderException e)
                            {
                                Debug.Log($"JSON decode error: {e.Message}");
                            }
                        }
                    }

                    if (curChatIdx == GameManager.Instance.chatIdxSuccess) {
                        OnFinalResponseReceived(); // 최종 반환 완료 시 함수 호출
                    }
                    
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Exception: {ex.Message}");
        }
    }

    // chatHandler에서 호출
    public async void CallConversationStream(string query, string chatIdx="-1", string ai_lang_in = "")
    {
        // 공용변수 최신화
        if(chatIdx!="-1") {
            GameManager.Instance.chatIdxSuccess = chatIdx;
        }     
        // 애니메이션 재생 초기화
        AnimationManager.Instance.Idle();
        // API 호출을 위한 URL 구성
        string baseUrl = ServerManager.Instance.GetBaseUrl();
        string streamUrl = baseUrl+"/conversation_stream";
        // 닉네임 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string player_name = SettingManager.Instance.settings.player_name;
        // string ui_language = SettingManager.Instance.settings.ui_language ?? "";  // 설정 ui에서 쓰는 언어에 따라가는 경향이 있음
        string ai_language = SettingManager.Instance.settings.ai_language ?? "";
        string ai_language_in = ai_lang_in;  // stt 에서 가져온 언어 있으면 사용(en, jp, ko 안에 포함되는지는 서버쪽에서 확인)
        string ai_language_out = SettingManager.Instance.settings.ai_language_out ?? "";
        string ai_web_search = SettingManager.Instance.settings.ai_web_search ?? "off";  // 0 : off, 1 : on, 2: force
        if (GameManager.Instance.isWebSearchForced)  // 강제 검색 메뉴
        {
            GameManager.Instance.isWebSearchForced = false;
            ai_web_search = "force";
        }
        string intent_image = "off";
        if (ChatBalloonManager.Instance.GetImageUse()) intent_image = "force";
        string intent_confirm = "false";
        if (SettingManager.Instance.settings.confirmUserIntent) intent_confirm = "true";

        var memory = MemoryManager.Instance.GetAllConversationMemory();
        string memoryJson = JsonConvert.SerializeObject(memory);

        // 요청 데이터 구성
        var requestData = new Dictionary<string, string>
        {
            { "query", query },  // 질문내용
            { "player", player_name }, // 설정의 플레이어 이름 : 초기값 sensei
            { "char", nickname }, // 닉네임으로 캐릭터 이름 추가 : 초기값 arona
            { "ai_language", ai_language }, // 추론언어로 한입, 영입영출 등 조절(normal, prefer, ko, en, jp)
            { "ai_language_in", ai_language_in }, // 추론언어로 한입, 영입영출 등 조절(ko, en, jp)
            { "ai_language_out", ai_language_out }, // 추론언어로 한출, 영입영출 등 조절(ko, en, jp)
            { "memory", memoryJson },
            { "chatIdx", chatIdx},
            { "intent_web", ai_web_search},  // off, on, force
            { "intent_image", intent_image},  // on, off, force
            { "intent_confirm", intent_confirm},  // on, off : 의도행동확인 받기 여부[web검색하실까요 선생님?]
            { "intent_confirm_type", ""},  // "", web, light : 의도행동확인 종류
            { "intent_confirm_answer", ""},  // true, false : 의도행동확인에 대한 답변[재생성시 확인 없이 적용하기 위해]
            { "regenerate_count", GameManager.Instance.chatIdxRegenerateCount.ToString()}
        };

        await FetchStreamingData(streamUrl, requestData);
    }

    public async void GetKoWavFromAPI(string text, string chatIdx)
    {
        // API 호출을 위한 URL 구성
        string baseUrl = ServerManager.Instance.GetBaseUrl();
        string url = baseUrl+"/getSound/ko"; // GET + Uri.EscapeDataString(text);

        // 닉네임 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        
        // HttpWebRequest 객체 생성
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";

        var requestData = new Dictionary<string, string>
        {
            { "text", text},
            { "char", nickname},
            { "lang", SettingManager.Instance.settings.sound_language.ToString() },
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
                    if (GameManager.Instance.chatIdxBalloon > chatIdxHeaderNum) {
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

    public async void GetJpWavFromAPI(string text, string chatIdx)
    {
        // API 호출을 위한 URL 구성
        string baseUrl = ServerManager.Instance.GetBaseUrl();
        string url = baseUrl+"/getSound/jp"; // GET + Uri.EscapeDataString(text);

        // 닉네임 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());

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
                        if (GameManager.Instance.chatIdxBalloon > chatIdxHeaderNum) {
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
        if (wavDuration > 10f)
        {
            Debug.LogWarning("WAV file is longer than 10 seconds. File will not be saved.");
            return;
        }

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
        try {
            server_id = SettingManager.Instance.settings.server_id;
        } catch (Exception ex) {
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
    
}
