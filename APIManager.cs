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
    private bool isCompleted = false; // 반환이 완료되었는지 여부를 체크하는 플래그
    private bool isResponsedStarted = false; // 첫 반환이 돌아왔는지 여부
    private string logFilePath; // 로그 파일 경로

    // 서버 관련
    public string ngrokUrl = null;  // 없으면 없는데로 오케이

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
            Destroy(gameObject);
            return;
        }

        // 로그 파일 경로 생성 (날짜와 시간 기반으로)
        string dateTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(Application.persistentDataPath, $"log/api_{dateTime}.txt");

        // 로그 파일 생성
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)); // 디렉토리가 없으면 생성
        File.AppendAllText(logFilePath, $"Log started at: {DateTime.Now}\n");

        // supabase에서 ngrok url 가져오기
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
                    // Debug.Log($"English Reply: {answerEn}");
                    replyListEn.Add(answerEn);
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
                if (SettingManager.Instance.settings.sound_language == "ko") {
                    GetKoWavFromAPI(answerVoice);
                }
                if (SettingManager.Instance.settings.sound_language == "jp") {
                    GetJpWavFromAPI(answerVoice);
                }
            }
        }
    }

    // 최종 반환 완료 시 호출될 함수
    private void OnFinalResponseReceived()
    {
        isCompleted = true;
        Debug.Log("All replies have been received.");
        LogToFile("ProcessReply completed."); // ProcessReply 완료 로그


        // foreach (string reply in replyListJp)
        // {
        //     Debug.Log(reply); // 각 reply를 출력
        // }
    }

    // 스트리밍 데이터를 가져오는 메서드
    public async Task FetchStreamingData(string url, Dictionary<string, string> data)
    {
        string jsonData = JsonConvert.SerializeObject(data);

        try
        {
            // 요청전 초기화
            isCompleted = false;
            isResponsedStarted = false;

            // HttpWebRequest 객체를 사용하여 요청 생성
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);

            // 요청 본문에 데이터 쓰기
            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
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
                                if (!isResponsedStarted) {
                                    AnswerBalloonManager.Instance.ShowAnswerBalloonInf();
                                    isResponsedStarted = true;
                                }

                                var jsonObject = JObject.Parse(line);
                                Debug.Log("jsonObject.ToString()");
                                Debug.Log(jsonObject.ToString());
                                ProcessReply(jsonObject); // 각 JSON 응답을 처리
                            }
                            catch (JsonReaderException e)
                            {
                                Debug.Log($"JSON decode error: {e.Message}");
                            }
                        }
                    }

                    OnFinalResponseReceived(); // 최종 반환 완료 시 함수 호출
                }
            }
        }
        catch (WebException ex)
        {
            Debug.Log($"Exception: {ex.Message}");
        }
    }

    // CallConversationStream 함수를 Start에서 호출
    private void Start()
    {
        // Test용 코드
        // string query = "내일 날씨가 어떨까?";

        // Test용 stream 답변생성
        // CallConversationStream(query);

        // Test용 wav 생성
        // GetKoWavFromAPI(query);
    }

    // chatHandler에서 호출
    public async void CallConversationStream(string query)
    {
        // API 호출을 위한 URL 구성
        string baseUrl = "";
#if UNITY_ANDROID && !UNITY_EDITOR
        if (APIManager.Instance.ngrokUrl == null) {
            baseUrl = "https://minmin496969.loca.lt";
        } else {
            baseUrl = APIManager.Instance.ngrokUrl;  // ex) https://8e5c-1-237-90-223.ngrok-free.app
        }
#else
        baseUrl = "http://127.0.0.1:5000";
#endif
        string streamUrl = baseUrl+"/conversation_stream";
        // 닉네임 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string player_name = SettingManager.Instance.settings.player_name;
        string ai_language = SettingManager.Instance.settings.ai_language ?? "";
        string ai_language_in = SettingManager.Instance.settings.ai_language_in ?? "";
        string ai_language_out = SettingManager.Instance.settings.ai_language_out ?? "";

        // 요청 데이터 구성
        var requestData = new Dictionary<string, string>
        {
            { "query", query },
            { "player", player_name }, // 설정의 플레이어 이름
            { "char", nickname }, // 닉네임으로 캐릭터 이름 추가
            { "ai_language", ai_language }, // 추론언어로 한입, 영입영출 등 조절(ko, en, jp)
            { "ai_language_in", ai_language_in }, // 추론언어로 한입, 영입영출 등 조절(ko, en, jp)
            { "ai_language_out", ai_language_out } // 추론언어로 한출, 영입영출 등 조절(ko, en, jp)
        };

        await FetchStreamingData(streamUrl, requestData);
    }

    public async void GetKoWavFromAPI(string text)
    {
        // API 호출을 위한 URL 구성
        string baseUrl = "";
#if UNITY_ANDROID && !UNITY_EDITOR
        if (APIManager.Instance.ngrokUrl == null) {
            baseUrl = "https://minmin496969.loca.lt";
        } else {
            baseUrl = APIManager.instance.ngrokUrl;  // ex) https://8e5c-1-237-90-223.ngrok-free.app
        }
#else
        baseUrl = "http://127.0.0.1:5000";
#endif
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
            { "lang", "ja"},
            { "speed", SettingManager.Instance.settings.sound_speedMaster.ToString()}
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

    public async void GetJpWavFromAPI(string text)
    {
        // API 호출을 위한 URL 구성
        string baseUrl = "";
#if UNITY_ANDROID && !UNITY_EDITOR
        if (APIManager.Instance.ngrokUrl == null) {
            baseUrl = "https://minmin496969.loca.lt";
        } else {
            baseUrl = APIManager.Instance.ngrokUrl;  // ex) https://8e5c-1-237-90-223.ngrok-free.app
        }
#else
        baseUrl = "http://127.0.0.1:5000";
#endif
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
            { "speed", SettingManager.Instance.settings.sound_speedMaster.ToString()}
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
                Debug.Log(response.StatusCode);
                // 요청이 성공했는지 확인
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 응답 스트림을 읽어서 파일에 저장
                    using (Stream responseStream = response.GetResponseStream())
                    {
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
        Debug.Log("wavDuration : " + wavDuration);

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
            Debug.Log($"WAV file saved to: {filePath}");
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
    public class NgorokJsonResponse
    {
        public string url;
    }

    // JSON 파일을 다운로드하고 url 값을 반환
    private IEnumerator FetchNgrokJsonData()
    {
        string ngrokSupabaseUrl = "https://hvqoarxlvxrrtgvisgsi.supabase.co/storage/v1/object/public/json_bucket/my_little_jarvis_plus_ngrok_server.json";
        using (UnityWebRequest request = UnityWebRequest.Get(ngrokSupabaseUrl))
        {
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
                NgorokJsonResponse data = JsonUtility.FromJson<NgorokJsonResponse>(jsonResponse);

                // url 값 반환 및 출력
                if (data != null && !string.IsNullOrEmpty(data.url))
                {
                    Debug.Log($"Fetched URL: {data.url}");
                    ngrokUrl = data.url;
                }
                else
                {
                    Debug.LogError("Invalid JSON data or 'url' field is missing.");
                }
            }
        }
    }
    
}
