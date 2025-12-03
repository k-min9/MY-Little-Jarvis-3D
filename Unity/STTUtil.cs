using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;

// STT(Speech-to-Text) 관련 유틸리티 클래스
// MicrophoneNormal과 VADController에서 중복되던 STT 로직을 통합
public static class STTUtil
{
    // STT API 응답 데이터 구조
    [System.Serializable]
    public class SttResponse
    {
        public string text;
        public string lang;    // ja, ko, en
        public string chatIdx;
    }

    // 음성 화자 분석 API 응답 데이터 구조
    [System.Serializable]
    public class VoiceFilterResponse
    {
        public bool should_ignore;
        public float similarity;
        public string character;
        public string mode;
        public string match_status;
        public float threshold;
        public string error;
    }

    // 음성 필터링 처리 결과
    public class VoiceFilterResult
    {
        public bool shouldIgnore = false;
        public bool hasNetworkError = false;
        public float similarity = 0.0f;
        public string mode = "";
        public string errorMessage = "";

        public void SetNetworkError(string error)
        {
            hasNetworkError = true;
            errorMessage = error;
            shouldIgnore = false; // 네트워크 에러 시에는 무시하지 않고 STT 진행
        }

        public void SetFilterResult(VoiceFilterResponse response)
        {
            hasNetworkError = false;
            shouldIgnore = response.should_ignore;
            similarity = response.similarity;
            mode = response.mode ?? "";
        }

        public void SetParsingError(string error)
        {
            hasNetworkError = false;
            shouldIgnore = false; // 파싱 에러 시에도 안전하게 STT 진행
            errorMessage = error;
        }
    }

    // WAV 파일을 STT 서버로 전송하고 결과를 처리
    public static IEnumerator SendWavFileToSTT(MonoBehaviour monoBehaviour, byte[] wavData, 
        string sttLang = "ko", string sttLevel = "small", bool enableDebugLog = false)
    {
        yield return ProcessSTTWithVoiceFilter(monoBehaviour, wavData, sttLang, sttLevel, enableDebugLog);
    }

    // 음성 필터링 후 STT 처리를 수행하는 코루틴
    private static IEnumerator ProcessSTTWithVoiceFilter(MonoBehaviour monoBehaviour, byte[] wavData, string sttLang, string sttLevel, bool enableDebugLog)
    {
        // Full 버전 확인
        int installStatusIndex = InstallStatusManager.Instance.GetInstallStatusIndex();
        
        // Full 버전이 아니면 필터링 없이 바로 STT 처리
        if (installStatusIndex < 2)
        {
            if (enableDebugLog)
            {
                Debug.Log("Full 버전이 아니므로 음성 필터링을 건너뜁니다.");
            }
            yield return SendWavFileCoroutine(wavData, sttLang, sttLevel, enableDebugLog);
            yield break;
        }

        // 음성 필터 설정 확인
        int aiVoiceFilterIdx = GetVoiceFilterSetting();
        
        if (aiVoiceFilterIdx != 0)
        {
            // 음성 필터링 수행
            VoiceFilterResult filterResult = new VoiceFilterResult();
            yield return CheckVoiceFilter(wavData, aiVoiceFilterIdx, enableDebugLog, filterResult);
            
            // 네트워크 실패 시에는 안전하게 STT 진행
            if (filterResult.hasNetworkError)
            {
                if (enableDebugLog)
                {
                    Debug.Log("음성 필터링 네트워크 오류로 인해 STT를 계속 진행합니다.");
                }
                yield return SendWavFileCoroutine(wavData, sttLang, sttLevel, enableDebugLog);
            }
            // 정상 응답이지만 무시해야 하는 경우
            else if (filterResult.shouldIgnore)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"음성 필터링 결과로 STT 처리를 건너뜁니다. (유사도: {filterResult.similarity})");
                }
            }
            // 필터링 통과 시 STT 처리
            else
            {
                yield return SendWavFileCoroutine(wavData, sttLang, sttLevel, enableDebugLog);
            }
        }
        else
        {
            // 필터링 비활성화 시 바로 STT 처리
            yield return SendWavFileCoroutine(wavData, sttLang, sttLevel, enableDebugLog);
        }
    }

    // 음성 화자 분석을 통한 필터링 체크
    private static IEnumerator CheckVoiceFilter(byte[] wavData, int filterIdx, bool enableDebugLog, VoiceFilterResult result)
    {
        // baseUrl을 비동기로 가져오기
        string baseUrl = null;
        bool urlReceived = false;
        
        ServerManager.Instance.GetBaseUrl((url) =>
        {
            baseUrl = url;
            urlReceived = true;
        });
        
        // URL 응답 대기
        yield return new WaitUntil(() => urlReceived);
        
        // dev_voice 서버 사용 여부 확인 (Android 또는 DevSound 토글 활성화시)
        if (SettingManager.Instance.IsDevSoundEnabled())
        {
            // dev_voice 서버 URL 가져오기
            string devVoiceUrl = null;
            bool devUrlReceived = false;
            
            ServerManager.Instance.GetServerUrlFromServerId("dev_voice", (url) =>
            {
                devVoiceUrl = url;
                devUrlReceived = true;
            });
            
            // URL 응답 대기
            yield return new WaitUntil(() => devUrlReceived);
            
            if (!string.IsNullOrEmpty(devVoiceUrl))
            {
                if (enableDebugLog)
                {
                    Debug.Log("dev_voice 서버 URL 사용 - " + devVoiceUrl);
                }
                baseUrl = devVoiceUrl;
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("dev_voice 서버 URL을 가져올 수 없습니다. 기본 URL을 사용합니다.");
                }
            }
        }
        
        string url = baseUrl + "/speech_diarization";
        
        if (enableDebugLog)
        {
            Debug.Log("음성 필터링 체크 URL : " + url);
        }

        // 캐릭터 및 플레이어 정보 가져오기
        string nickname = CharManager.Instance.GetNickname(CharManager.Instance.GetCurrentCharacter());
        string playerName = SettingManager.Instance.settings.player_name;

        // 폼 데이터 생성 및 전송
        WWWForm formData = new WWWForm();
        formData.AddBinaryData("file", wavData, "voice_check.wav", "audio/wav");
        formData.AddField("player", playerName);
        formData.AddField("char", nickname);
        formData.AddField("ai_voice_filter_idx", filterIdx.ToString());

        UnityWebRequest request = UnityWebRequest.Post(url, formData);
        
        // 요청 전송
        yield return request.SendWebRequest();

        // 결과 처리
        long responseCode = request.responseCode;
        string responseText = request.downloadHandler.text;
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"음성 필터링 네트워크 요청 실패: {request.error}");
            
            // 500 에러인 경우 서버 응답 내용 상세히 출력
            if (responseCode == 500)
            {
                Debug.LogError("=== 500 Internal Server Error 응답 내용 ===");
                Debug.LogError($"Response Code: {responseCode}");
                Debug.LogError($"Response Body: {responseText}");
                Debug.LogError($"Content-Type: {request.GetResponseHeader("Content-Type")}");
                Debug.LogError($"Request URL: {request.url}");
                Debug.LogError("=======================================");
            }
            
            result.SetNetworkError($"Network error (Code: {responseCode}): {request.error}");
        }
        else
        {
            try
            {
                // JSON 응답 파싱
                var filterResponse = JsonUtility.FromJson<VoiceFilterResponse>(responseText);
                
                if (enableDebugLog)
                {
                    Debug.Log($"음성 필터링 결과 - should_ignore: {filterResponse.should_ignore}, " +
                             $"similarity: {filterResponse.similarity}, mode: {filterResponse.mode}");
                }
                
                result.SetFilterResult(filterResponse);
            }
            catch (Exception ex)
            {
                Debug.LogError($"음성 필터링 JSON 응답 파싱 오류: {ex.Message}");
                result.SetParsingError($"JSON parsing error: {ex.Message}");
            }
        }
    }

    // 음성 필터 설정값을 가져오기 (0: None, 1: Skip AI Voice, 2: User Voice Only)
    private static int GetVoiceFilterSetting()
    {
        try
        {
            // SettingManager에서 ai_voice_filter_idx 값 가져오기
            // 0: None (필터링 비활성화)
            // 1: Skip AI Voice (캐릭터 음성 필터링)  
            // 2: User Voice Only (사용자 음성만 허용)
            return SettingManager.Instance.settings.ai_voice_filter_idx;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"음성 필터 설정 가져오기 실패: {ex.Message}");
            return 0; // 에러 시 기본값 (필터링 비활성화)
        }
    }

    // WAV 파일을 STT 서버로 전송
    private static IEnumerator SendWavFileCoroutine(byte[] wavData, string sttLang, string sttLevel, bool enableDebugLog)
    {
        // baseUrl을 비동기로 가져오기
        string baseUrl = null;
        bool urlReceived = false;
        
        ServerManager.Instance.GetBaseUrl((url) =>
        {
            baseUrl = url;
            urlReceived = true;
        });
        
        // URL 응답 대기
        yield return new WaitUntil(() => urlReceived);
        
        // dev_voice 서버 사용 여부 확인 (Android 또는 DevSound 토글 활성화시)
        if (SettingManager.Instance.IsDevSoundEnabled())
        {
            // dev_voice 서버 URL 가져오기
            string devVoiceUrl = null;
            bool devUrlReceived = false;
            
            ServerManager.Instance.GetServerUrlFromServerId("dev_voice", (sttUrl) =>
            {
                devVoiceUrl = sttUrl;
                devUrlReceived = true;
            });
            
            // URL 응답 대기
            yield return new WaitUntil(() => devUrlReceived);
            
            if (!string.IsNullOrEmpty(devVoiceUrl))
            {
                if (enableDebugLog)
                {
                    Debug.Log("STT용 dev_voice 서버 URL 사용 - " + devVoiceUrl);
                }
                baseUrl = devVoiceUrl;
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("STT용 dev_voice 서버 URL을 가져올 수 없습니다. 기본 URL을 사용합니다.");
                }
            }
        }
        
        string url = baseUrl + "/stt"; // http://localhost:5000/stt
        
        if (enableDebugLog)
        {
            Debug.Log("STT URL : " + url);
        }

        // chatIdx 증가
        GameManager.Instance.chatIdx += 1;
        GameManager.Instance.chatIdxRegenerateCount = 0;

        // 폼 데이터 생성 및 전송
        WWWForm formData = new WWWForm();
        formData.AddBinaryData("file", wavData, "stt.wav", "audio/wav");
        formData.AddField("lang", "ko");
        formData.AddField("level", "small");
        formData.AddField("chatIdx", GameManager.Instance.chatIdx);

        UnityWebRequest request = UnityWebRequest.Post(url, formData);
 
        // 요청 전송
        yield return request.SendWebRequest();

        // 결과 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error uploading WAV file: {request.error}");
        }
        else
        {
            try
            {
                // JSON 응답 파싱
                string responseText = request.downloadHandler.text;
                var responseJson = JsonUtility.FromJson<SttResponse>(responseText);
                string query = responseJson.text ?? "";

                // STT 결과 처리
                ProcessSTTResult(query, responseJson.chatIdx, responseJson.lang);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing JSON response: {ex.Message}");
            }
        }
    }

    // STT 결과를 처리
    private static void ProcessSTTResult(string query, string chatIdx, string lang)
    {
        // 풍선에 인식된 텍스트 표시
        NoticeBalloonManager.Instance.ModifyNoticeBalloonText(query);

        // 대화 시작
        APIManager.Instance.CallConversationStream(query, chatIdx, lang);

        // dev : 발언 음성 재생
        if (query != "" && SettingManager.Instance.settings.isDevHowling)
        { 
            APIManager.Instance.GetHowlingFromAPI(query);
        }

        // 기존 음성 중지 및 초기화
        VoiceManager.Instance.ResetAudio();
    }
}
