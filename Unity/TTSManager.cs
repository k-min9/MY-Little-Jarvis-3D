using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

// TTS 세션 관리 및 음성 합성 요청을 담당하는 싱글톤 매니저.
// APIManager에서 분리된 TTS 전용 로직.
public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    // TTS 세션 관리
    private SessionDataTTS ttsSession = new SessionDataTTS();
    private int globalSessionId = 0;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        EvaluateNextPlayback();
    }

    #endregion

    #region TTS 순차 재생 관리

    // 새 TTS 세션 시작. 세션 리셋 및 이전 오디오 정리.
    public void BeginTtsSession(int chatIdxNum)
    {
        globalSessionId++;
        ttsSession.Reset(globalSessionId, chatIdxNum);
        VoiceManager.Instance.ResetAudio();
        // Session start log removed - not needed for flow tracking
    }

    // 문장을 등록하고 seq를 할당 (번역 전에 호출하여 순서 확정)
    public int RegisterTtsRequest(string text)
    {
        int seq = ttsSession.nextSeqToAllocate++;
        ttsSession.textBySeq[seq] = text;
        ttsSession.stateBySeq[seq] = "pending";  // 아직 TTS 요청 안함
        return seq;
    }

    // 세션 ID 가져오기
    public int GetSessionId()
    {
        return ttsSession.sessionId;
    }

    // seq의 상태를 in_flight로 변경하고 텍스트 업데이트
    public void MarkTtsInFlight(int seq, string translatedText)
    {
        if (ttsSession.stateBySeq != null && ttsSession.stateBySeq.ContainsKey(seq))
        {
            ttsSession.textBySeq[seq] = translatedText;
            ttsSession.stateBySeq[seq] = "in_flight";
            Debug.Log($"[TTS_Flow] 3.TTS요청 seq={seq} len={translatedText.Length}");
        }
    }

    // 세션이 초기화되어 있는지 확인
    public bool IsSessionInitialized()
    {
        return ttsSession.stateBySeq != null;
    }

    // 순차 재생 코디네이터. nextSeqToPlay만 바라보며 처리
    private void EvaluateNextPlayback()
    {
        // 세션이 초기화되지 않았으면 스킵
        if (ttsSession.stateBySeq == null) return;
        
        int seq = ttsSession.nextSeqToPlay;
        
        // 해당 seq가 등록되지 않았으면 스킵
        if (!ttsSession.stateBySeq.ContainsKey(seq)) return;
        
        string state = ttsSession.stateBySeq[seq];
        
        if (state == "ready")
        {
            // 재생 큐에 삽입
            EnqueueWavData(ttsSession.wavBySeq[seq]);
            ttsSession.stateBySeq[seq] = "played";
            Debug.Log($"[TTS_Flow] 4.TTS수락 seq={seq} → 재생큐 추가");
            ttsSession.nextSeqToPlay++;
            
            // 다음 seq의 타임아웃 카운트 시작 (이 시점부터 2초)
            int nextSeq = seq + 1;
            if (ttsSession.stateBySeq.ContainsKey(nextSeq) && !ttsSession.waitStartTimeBySeq.ContainsKey(nextSeq))
            {
                ttsSession.waitStartTimeBySeq[nextSeq] = Time.time;
            }
        }
        else if (state == "failed")
        {
            // 실패는 즉시 스킵
            // Skip log - not critical for flow tracking
            ttsSession.nextSeqToPlay++;
            
            // 다음 seq의 타임아웃 카운트 시작
            int nextSeq = seq + 1;
            if (ttsSession.stateBySeq.ContainsKey(nextSeq) && !ttsSession.waitStartTimeBySeq.ContainsKey(nextSeq))
            {
                ttsSession.waitStartTimeBySeq[nextSeq] = Time.time;
            }
        }
        else if (state == "skipped" || state == "played")
        {
            // 이미 처리됨
            ttsSession.nextSeqToPlay++;
        }
        else // pending, in_flight
        {
            // seq=0은 타임아웃 없이 무한 대기 (첫 문장은 언제 올지 모름)
            if (seq == 0)
            {
                // 첫 문장 대기 중 (타임아웃 없음)
                return;
            }
            
            // seq=1+ 는 이전 seq가 처리된 후부터 2초 대기
            if (!ttsSession.waitStartTimeBySeq.ContainsKey(seq))
            {
                // 아직 이전 seq가 처리되지 않았으면 대기 시작하지 않음
                return;
            }
            
            float elapsed = Time.time - ttsSession.waitStartTimeBySeq[seq];
            
            if (elapsed > 2f)
            {
                // 2초 타임아웃
                ttsSession.stateBySeq[seq] = "skipped";
                Debug.Log($"[TTS_Flow] 4.TTS스킵 seq={seq} reason=timeout ({elapsed:F2}s)");
                ttsSession.nextSeqToPlay++;
                
                // 다음 seq의 타임아웃 카운트 시작
                int nextSeq = seq + 1;
                if (ttsSession.stateBySeq.ContainsKey(nextSeq) && !ttsSession.waitStartTimeBySeq.ContainsKey(nextSeq))
                {
                    ttsSession.waitStartTimeBySeq[nextSeq] = Time.time;
                }
            }
            // 대기 중 (로그는 너무 많아서 생략)
        }
    }

    // WAV 데이터를 파일로 저장하고 VoiceManager 큐에 추가
    private void EnqueueWavData(byte[] wavData)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "response.wav");
        try
        {
            File.WriteAllBytes(filePath, wavData);
            VoiceManager.Instance.LoadAudioWavToQueue();
        }
        catch (IOException e)
        {
            Debug.LogError($"[TTS_Flow] WAV저장 에러: {e.Message}");
        }
    }

    #endregion

    #region 외부 매니저용 인터페이스

    // 외부 매니저용: TTS 세션 시작
    public void StartTtsSession(int chatIdxNum)
    {
        BeginTtsSession(chatIdxNum);
    }

    // 외부 매니저용: TTS 요청 등록 및 호출
    public void RequestTTS(string text, string chatIdx, string soundLanguage, string nickname = null)
    {
        if (ttsSession.stateBySeq == null)
        {
            // 세션이 없으면 새로 시작
            int chatIdxNum = int.TryParse(chatIdx, out var idx) ? idx : 0;
            BeginTtsSession(chatIdxNum);
        }
        
        int seq = RegisterTtsRequest(text);
        MarkTtsInFlight(seq, text);
        int capturedSessionId = ttsSession.sessionId;
        
        if (soundLanguage == "ko" || soundLanguage == "en")
        {
            GetKoWavFromAPI(text, chatIdx, seq, capturedSessionId, nickname);
        }
        else if (soundLanguage == "jp" || soundLanguage == "ja")
        {
            GetJpWavFromAPI(text, chatIdx, seq, capturedSessionId, nickname);
        }
        else
        {
            // 기본값: 일본어
            GetJpWavFromAPI(text, chatIdx, seq, capturedSessionId, nickname);
        }
    }

    #endregion

    #region TTS API 호출

    public async void GetKoWavFromAPI(string text, string chatIdx, int seq, int capturedSessionId, string nickname = null)
    {
        Debug.Log($"[TTS] TTS start seq={seq} lang=ko");
        
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;

        // local_voice 사용 여부 (isDevSound보다 우선됨, 저장 안함)
        if (SettingManager.Instance.settings.isLocalSound)
        {
            baseUrl = "http://127.0.0.1:5000";
            Debug.Log("[TTS] local_voice 서버 사용: " + baseUrl);
        }
        // dev_voice 사용 여부 (설치 상태 또는 DevSound 토글)
        else
        {
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
        }
        
        // baseUrl 유효성 체크
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[GetKoWavFromAPI] baseUrl이 비어있습니다. 음성 합성을 건너뜁니다.");
            if (capturedSessionId == ttsSession.sessionId && ttsSession.stateBySeq != null)
            {
                ttsSession.stateBySeq[seq] = "failed";
            }
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
                // 세션 불일치 체크 (대화가 이미 바뀐 뒤 도착)
                if (capturedSessionId != ttsSession.sessionId)
                {
                    Debug.Log($"[TTS] Ignore stale result seq={seq} (session mismatch: {capturedSessionId} != {ttsSession.sessionId})");
                    return;
                }
                
                // 요청이 성공했는지 확인
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 헤더에서 Chat-Idx 값을 가져와, 현재 대화보다 과거일 경우에는 queue에 넣지 않음
                    string chatIdxHeader = response.Headers["Chat-Idx"];
                    int chatIdxHeaderNum = int.Parse(chatIdxHeader);
                    if (GameManager.Instance.chatIdxBalloon > chatIdxHeaderNum)
                    {
                        Debug.Log("과거대화 : " + GameManager.Instance.chatIdxBalloon.ToString() + "/" + chatIdxHeaderNum.ToString());
                        if (ttsSession.stateBySeq != null)
                        {
                            ttsSession.stateBySeq[seq] = "skipped";
                        }
                        return;
                    }

                    // 응답 스트림을 읽어서 wavBySeq에 저장
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            byte[] wavData = ReadFully(responseStream);
                            
                            // wavBySeq에 저장하고 상태를 ready로 변경
                            if (ttsSession.stateBySeq != null)
                            {
                                ttsSession.wavBySeq[seq] = wavData;
                                ttsSession.stateBySeq[seq] = "ready";
                                Debug.Log($"[TTS] TTS ready seq={seq} bytes={wavData.Length}");
                            }
                        }
                    }
                }
                else
                {
                    if (ttsSession.stateBySeq != null)
                    {
                        ttsSession.stateBySeq[seq] = "failed";
                    }
                    Debug.LogError($"Error fetching WAV file: {response.StatusCode}");
                }
            }
        }
        catch (WebException ex)
        {
            if (capturedSessionId == ttsSession.sessionId && ttsSession.stateBySeq != null)
            {
                ttsSession.stateBySeq[seq] = "failed";
            }
            Debug.LogError($"WebException: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (capturedSessionId == ttsSession.sessionId && ttsSession.stateBySeq != null)
            {
                ttsSession.stateBySeq[seq] = "failed";
            }
            Debug.LogError($"Exception: {ex.Message}");
        }
    }

    public async void GetJpWavFromAPI(string text, string chatIdx, int seq, int capturedSessionId, string nickname = null)
    {
        Debug.Log($"[TTS] TTS start seq={seq} lang=jp");
        
        // baseUrl을 비동기로 가져오기
        var tcs = new TaskCompletionSource<string>();
        ServerManager.Instance.GetBaseUrl((urlResult) => tcs.SetResult(urlResult));
        string baseUrl = await tcs.Task;

        // local_voice 사용 여부 (isDevSound보다 우선됨, 저장 안함)
        if (SettingManager.Instance.settings.isLocalSound)
        {
            baseUrl = "http://127.0.0.1:5000";
            Debug.Log("[TTS] local_voice 서버 사용: " + baseUrl);
        }
        // dev_voice 사용 여부 (설치 상태 또는 DevSound 토글)
        else
        {
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
        }

        // baseUrl 유효성 체크
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[GetJpWavFromAPI] baseUrl이 비어있습니다. 음성 합성을 건너뜁니다.");
            if (capturedSessionId == ttsSession.sessionId && ttsSession.stateBySeq != null)
            {
                ttsSession.stateBySeq[seq] = "failed";
            }
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
                // 세션 불일치 체크 (대화가 이미 바뀐 뒤 도착)
                if (capturedSessionId != ttsSession.sessionId)
                {
                    Debug.Log($"[TTS] Ignore stale result seq={seq} (session mismatch: {capturedSessionId} != {ttsSession.sessionId})");
                    return;
                }
                
                // 요청이 성공했는지 확인
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // 응답 스트림을 읽어서 wavBySeq에 저장
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        // 헤더에서 Chat-Idx 값을 가져와, 현재 대화보다 과거일 경우에는 queue에 넣지 않음
                        string chatIdxHeader = response.Headers["Chat-Idx"];
                        int chatIdxHeaderNum = int.Parse(chatIdxHeader);
                        if (GameManager.Instance.chatIdxBalloon > chatIdxHeaderNum)
                        {
                            Debug.Log("과거대화 : " + GameManager.Instance.chatIdxBalloon.ToString() + "/" + chatIdxHeaderNum.ToString());
                            if (ttsSession.stateBySeq != null)
                            {
                                ttsSession.stateBySeq[seq] = "skipped";
                            }
                            return;
                        }

                        if (responseStream != null)
                        {
                            byte[] wavData = ReadFully(responseStream);

                            // wavBySeq에 저장하고 상태를 ready로 변경
                            if (ttsSession.stateBySeq != null)
                            {
                                ttsSession.wavBySeq[seq] = wavData;
                                ttsSession.stateBySeq[seq] = "ready";
                                Debug.Log($"[TTS] TTS ready seq={seq} bytes={wavData.Length}");
                            }
                        }
                    }
                }
                else
                {
                    if (ttsSession.stateBySeq != null)
                    {
                        ttsSession.stateBySeq[seq] = "failed";
                    }
                    Debug.LogError($"Error fetching WAV file: {response.StatusCode}");
                }
            }
        }
        catch (WebException ex)
        {
            if (capturedSessionId == ttsSession.sessionId && ttsSession.stateBySeq != null)
            {
                ttsSession.stateBySeq[seq] = "failed";
            }
            // 보낸 주소
            Debug.LogError($"{url}");
            // early stop 등의 거절도 여기로 보냈음
            Debug.LogError($"WebException: {ex.Message}\nerror Text : {text}");
        }
        catch (Exception ex)
        {
            if (capturedSessionId == ttsSession.sessionId && ttsSession.stateBySeq != null)
            {
                ttsSession.stateBySeq[seq] = "failed";
            }
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

    #endregion

    #region 헬퍼 함수

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

    #endregion
}
