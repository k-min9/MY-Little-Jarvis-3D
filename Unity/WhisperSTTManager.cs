using Whisper;
using UnityEngine;
using System.Collections;
using System;

public class WhisperSTTManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static WhisperSTTManager instance;
    public static WhisperSTTManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<WhisperSTTManager>();
            }
            return instance;
        }
    }

    [Header("Whisper Settings")]
    [SerializeField] private WhisperManager whisperManager;

    private void Awake()
    {
        // 싱글톤 초기화
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeWhisper();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void InitializeWhisper()
    {
        if (whisperManager == null)
        {
            whisperManager = GetComponent<WhisperManager>();
            if (whisperManager == null)
            {
                whisperManager = gameObject.AddComponent<WhisperManager>();
            }
        }

        // 이벤트 구독
        whisperManager.OnNewSegment += OnTranscriptionSegment;
        whisperManager.OnProgress += OnTranscriptionProgress;

        Debug.Log("WhisperSTTManager initialized successfully!");
    }

    // WAV 데이터를 받아서 STT 처리하는 메인 메서드
    public IEnumerator ProcessSTTFromWavData(byte[] wavData, string lang = "ko")
    {
        Debug.Log("Starting internal Whisper STT processing...");
        
        // WAV 데이터를 AudioClip으로 변환
        AudioClip audioClip = WavDataToAudioClip(wavData);
        
        if (audioClip == null)
        {
            Debug.LogError("Failed to convert WAV data to AudioClip");
            yield break;
        }

        Debug.Log($"Audio converted: {audioClip.length}s, {audioClip.frequency}Hz");

        // Whisper STT 실행
        var whisperTask = whisperManager.GetTextAsync(audioClip);
        
        // Task 완료까지 대기
        while (!whisperTask.IsCompleted)
        {
            yield return null;
        }
        
        // 결과 처리
        string result = "";
        
        if (whisperTask.Exception != null)
        {
            Debug.LogError($"Whisper STT failed: {whisperTask.Exception.Message}");
        }
        else if (whisperTask.Result != null && whisperTask.Result.Result != null)
        {
            result = whisperTask.Result.Result;
            Debug.Log($"Whisper STT completed: {result}");
        }
        else
        {
            Debug.LogWarning("Whisper STT returned null result");
        }

        // 결과가 비어있거나 의미없는 경우 처리하지 않음
        if (string.IsNullOrWhiteSpace(result))
        {
            Debug.LogWarning("STT result is empty or whitespace only. Skipping response processing.");
            yield break;
        }

        // SttResponse 생성 및 처리
        SttResponse response = new SttResponse
        {
            text = result,
            lang = lang,
            chatIdx = GameManager.Instance.chatIdx.ToString()
        };

        ProcessSTTResponse(response);
    }

    private AudioClip WavDataToAudioClip(byte[] wavData)
    {
        try
        {
            // WAV 헤더 파싱
            int frequency = System.BitConverter.ToInt32(wavData, 24);
            int channels = System.BitConverter.ToInt16(wavData, 22);
            int bitsPerSample = System.BitConverter.ToInt16(wavData, 34);
            
            // 데이터 청크 찾기
            int dataOffset = FindDataChunk(wavData);
            if (dataOffset == -1)
            {
                Debug.LogError("Invalid WAV file: data chunk not found");
                return null;
            }

            int dataSize = System.BitConverter.ToInt32(wavData, dataOffset + 4);
            int samples = dataSize / (channels * bitsPerSample / 8);
            
            float[] audioData = new float[samples];
            
            // 16비트 PCM 변환
            if (bitsPerSample == 16)
            {
                for (int i = 0; i < samples; i++)
                {
                    short sample = System.BitConverter.ToInt16(wavData, dataOffset + 8 + i * 2);
                    audioData[i] = sample / 32768f;
                }
            }
            // 32비트 float 변환
            else if (bitsPerSample == 32)
            {
                for (int i = 0; i < samples; i++)
                {
                    audioData[i] = System.BitConverter.ToSingle(wavData, dataOffset + 8 + i * 4);
                }
            }
            
            AudioClip clip = AudioClip.Create("InternalSTT", samples / channels, channels, frequency, false);
            clip.SetData(audioData, 0);
            
            return clip;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error converting WAV data to AudioClip: {ex.Message}");
            return null;
        }
    }

    private int FindDataChunk(byte[] wavData)
    {
        // "data" 청크 찾기
        for (int i = 12; i < wavData.Length - 4; i++)
        {
            if (wavData[i] == 'd' && wavData[i + 1] == 'a' && 
                wavData[i + 2] == 't' && wavData[i + 3] == 'a')
            {
                return i;
            }
        }
        return -1;
    }

    private void ProcessSTTResponse(SttResponse response)
    {
        string query = response.text ?? "";

        NoticeBalloonManager.Instance.ModifyNoticeBalloonText(query);

        // 대화 시작 - chatIdx는 string 타입
        APIManager.Instance.CallConversationStream(query, response.chatIdx, response.lang);

        // dev : 발언 음성 재생
        if (query != "" && SettingManager.Instance.settings.isDevHowling)
        { 
            APIManager.Instance.GetHowlingFromAPI(query);
        }

        // 기존 음성 중지 및 초기화
        VoiceManager.Instance.ResetAudio();
    }

    // 이벤트 핸들러들
    private void OnTranscriptionSegment(WhisperSegment segment)
    {
        Debug.Log($"Whisper Segment: {segment.Text} (시작: {segment.Start:F2}s, 끝: {segment.End:F2}s)");
    }

    private void OnTranscriptionProgress(int progress)
    {
        Debug.Log($"Whisper progress: {progress}%");
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (whisperManager != null)
        {
            whisperManager.OnNewSegment -= OnTranscriptionSegment;
            whisperManager.OnProgress -= OnTranscriptionProgress;
        }
    }
}

// SttResponse 구조체 (MicrophoneNormal과 동일한 구조)
[System.Serializable]
public struct SttResponse
{
    public string text;
    public string lang;
    public string chatIdx;
}