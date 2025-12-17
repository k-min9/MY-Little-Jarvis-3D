using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class VADController : MonoBehaviour
{
    public Button vadToggleButton; // VAD 상태를 토글할 버튼
    public Image vadStatusImage;  // VAD 상태에서 녹음이 감지되면 초록/아니면 빨강
    public bool isVADModeActive = false;

    private byte[] wavData;

    // VAD Check
    private AudioClip _clip;
    public string microphoneDevice { get; private set; }
    private bool _madeLoopLap;
    public int ClipSamples => _clip.samples * _clip.channels;
    private int _lastMicPos;
    public bool IsVoiceDetected;

    // VAD Record
    private float[] _oldBuffer = Array.Empty<float>();
    private readonly List<float> _newBuffer = new List<float>();
    public float chunksLengthSec = 0.5f;  // 0.5초단위로 buffer 저장
    private int _lastChunkPos;
    private int _chunksLength;
    public float vadStopTime = 2f;  // 2초간 입력 없으면 대화 시작

    private float? _vadStopBegin;

    // 싱글톤 인스턴스
    public static VADController instance;
    public static VADController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VADController>();
            }
            return instance;
        }
    }

    private void Start()
    {
        UpdateButtonColor(); // 초기 버튼 색상 설정
    }

    private void Update()
    {
        if (isVADModeActive)
        {
            CheckVAD();
        }
    }

    public void CheckVAD()
    {
        // Debug.Log("CheckVAD start");
        // lets check current mic position time
        var micPos = Microphone.GetPosition(microphoneDevice);
        // Debug.Log(micPos);
        if (micPos < _lastMicPos)
        {
            _madeLoopLap = true;
        }
        _lastMicPos = micPos;

        UpdateChunks(micPos);
        UpdateVad(micPos);
        // Debug.Log("CheckVAD end");
    }

    void UpdateChunks(int micPos)
    {
        // check if chunks length is valid
        if (_chunksLength <= 0)
            return;

        // get current chunk length
        var chunk = GetMicPosDist(_lastChunkPos, micPos);

        // send new chunks while there has valid size
        while (chunk > _chunksLength)
        {
            var origData = new float[_chunksLength];
            _clip.GetData(origData, _lastChunkPos);

            if (IsVoiceDetected)
            {
                _newBuffer.AddRange(origData);
                getBuffer();
            }

            _lastChunkPos = (_lastChunkPos + _chunksLength) % ClipSamples;
            chunk = GetMicPosDist(_lastChunkPos, micPos);
        }
    }

    void UpdateVad(int micPos)
    {
        float vadLastSec = 1.25f;  // Threshold of VAD energy activation
        float vadThd = 1.0f;  // Threshold of VAD filter frequency
        float vadFreqThd = 100.0f;  // Optional indicator that changes color when speech detected

        float vadContextSec = 30f;  // Window size where VAD tries to detect speech

        var data = GetMicBufferLast(micPos, vadContextSec);
        var vad = AudioUtils.SimpleVad(data, _clip.frequency, vadLastSec, vadThd, vadFreqThd);
        // Debug.Log(vad + " : " + micPos + "/" + data.Length + "/" + _clip.frequency);

        // raise event if vad has changed
        if (vad != IsVoiceDetected)
        {
            // Debug.Log("vad : " + vad); 
            var color = vad ? Color.green : Color.red;
            vadStatusImage.color = color;
            _vadStopBegin = !vad ? Time.realtimeSinceStartup : (float?) null;
            IsVoiceDetected = vad;
        }

        UpdateVadStop(); // xx초 경과시 record
    }

    private void UpdateVadStop()
    {
        if (_vadStopBegin == null)
            return;

        var passedTime = Time.realtimeSinceStartup - _vadStopBegin;
        if (passedTime > vadStopTime)
        {
            _vadStopBegin = null;
            saveBuffer(); 
            ResetBuffer();
        }
    }

    // Original : UpdateSlidingWindow
    void getBuffer()
    {
        var newBufferLen = _newBuffer.Count;
        var oldBufferLen = _oldBuffer.Length;
        int nSamplesTake = oldBufferLen;  // 일단 전부 take

        // copy data from old buffer to temp inference one
        var bufferLen = nSamplesTake + newBufferLen;
        var buffer = new float[bufferLen];
        var oldBufferStart = oldBufferLen - nSamplesTake;
        Array.Copy(_oldBuffer, oldBufferStart, 
            buffer, 0, nSamplesTake);

        // and now add data from new buffer
        _newBuffer.CopyTo(0, buffer, nSamplesTake, newBufferLen);  
        _newBuffer.Clear();

        // 기존 buffer 저장 (실시간으로 TrimSilence 할지는 살짝 고민)
        _oldBuffer = buffer;

        // // TEST용 : 실시간 buffer 갱신
        // Debug.Log("saving buffer.wav");
        // SaveWavFile(buffer, "buffer.wav");  
    }

    void saveBuffer()
    {

        // Debug.Log("before Trim : " + _oldBuffer.Length);
        _oldBuffer = TrimSilence(_oldBuffer, 0);
        // Debug.Log("After Trim : " + _oldBuffer.Length);
        
        if (_oldBuffer!=null && _oldBuffer.Length > 8000)  // 음성인식 0.5초(16000)을 최소 기준으로 도중에 안끊기게
        {
            SaveWavFile(_oldBuffer, "Savebuffer.wav");
        } 
        else 
        {
            Debug.Log("음성인식 최소 단위 이하의 buffer");
            
        }
        ResetBuffer();
    }

    // float의 앞 뒤 침묵 제거
    public float[] TrimSilenceOrigin(float[] samples, float min)
    {
        int startIndex = 0;
        int endIndex = samples.Length - 1;
        Debug.Log(samples[(startIndex+endIndex)/2]);
        // 앞쪽의 침묵 제거
        while (startIndex < samples.Length && Mathf.Abs(samples[startIndex]) <= min)
        {
            startIndex++;
        }

        // 뒤쪽의 침묵 제거
        while (endIndex >= 0 && Mathf.Abs(samples[endIndex]) <= min)
        {
            endIndex--;
        }

        // 유효한 영역이 없으면 빈 배열 반환
        if (startIndex > endIndex)
        {
            return Array.Empty<float>();
        }

        // 트리밍된 배열 생성
        int trimmedLength = endIndex - startIndex + 1;
        float[] trimmedSamples = new float[trimmedLength];
        Array.Copy(samples, startIndex, trimmedSamples, 0, trimmedLength);

        return trimmedSamples;
    }

    public float[] TrimSilence(float[] samples, float min)
    {
        // 임시 리스트로 트리밍된 데이터를 저장
        List<float> trimmedList = new List<float>();

        // 전체 배열 순회
        foreach (var sample in samples)
        {
            // min 값 초과인 경우만 추가
            if (Mathf.Abs(sample) > min)
            {
                trimmedList.Add(sample);
            }
        }

        // 리스트를 배열로 변환하여 반환
        return trimmedList.ToArray();
    }


    private void ResetBuffer()
    {
        _oldBuffer = Array.Empty<float>();
        _newBuffer.Clear();
    }

    /// <summary>
    /// Get last sec of recorded mic buffer.
    /// </summary>
    private float[] GetMicBufferLast(int micPos, float lastSec)
    {
        var len = GetMicBufferLength(micPos);
        if (len == 0) 
            return Array.Empty<float>();
        
        var lastSamples = (int) (_clip.frequency * lastSec);
        var dataLength = Math.Min(lastSamples, len);
        var offset = micPos - dataLength;
        if (offset < 0) offset = len + offset;

        var data = new float[dataLength];
        _clip.GetData(data, offset);
        return data;
    }

    /// <summary>
    /// Get mic buffer length that was actually recorded.
    /// </summary>
    private int GetMicBufferLength(int micPos)
    {
        // looks like we just started recording and stopped it immediately
        // nothing was actually recorded
        if (micPos == 0 && !_madeLoopLap) 
            return 0;
        
        // get length of the mic buffer that we want to return
        // this need to account circular loop buffer
        var len = _madeLoopLap ? ClipSamples : micPos;
        return len;
    }

    // samples : 위에서 clip.GetData한 정보들
    private void SaveWavFile(float[] samples, string fileName)
    {
        // 저장
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            wavData = ConvertToWav(samples, _clip.channels, _clip.frequency);
            fileStream.Write(wavData, 0, wavData.Length);
        }

        // STT Server 사용 여부에 따라 분기
        if (SettingManager.Instance.settings.isSTTServer)
        {
            // 외부 서버 STT 사용 (wav 전송 API 호출)
            Debug.Log("Using external server STT...");
            StartCoroutine(STTUtil.SendWavFileToSTT(this, wavData, "ko", "normal", true));
        }
        else
        {
            // 내부 Unity Whisper STT 사용
            Debug.Log("Using internal Unity Whisper STT...");
            StartCoroutine(WhisperSTTManager.Instance.ProcessSTTFromWavData(wavData));
        }


    }

    private byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        int sampleCount = samples.Length;
        int dataLength = sampleCount * sizeof(short);
        int fileLength = 44 + dataLength; // 44바이트 헤더 + 데이터

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            // RIFF 헤더
            writer.Write("RIFF".ToCharArray());
            writer.Write(fileLength - 8);
            writer.Write("WAVE".ToCharArray());

            // fmt 서브청크
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1); // PCM 포맷
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16); // 비트 심도

            // data 서브청크
            writer.Write("data".ToCharArray());
            writer.Write(dataLength);

            // 샘플 데이터를 short로 변환
            foreach (float sample in samples)
            {
                short shortSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                writer.Write(shortSample);
            }

            return stream.ToArray();
        }
    }

    /// <summary>
    /// Calculate distance between two mic positions.
    /// It takes circular buffer into account.
    /// </summary>
    private int GetMicPosDist(int prevPos, int newPos)
    {
        if (newPos >= prevPos)
            return newPos - prevPos;

        // circular buffer case
        return ClipSamples - prevPos + newPos;
    }


    // UI적 요소 위주
    public void ToggleVAD()
    {
        if (isVADModeActive)
        {
            StopVAD();
        }
        else
        {
            StartVAD();
        }
    }

    private void StartVAD()
    {
        isVADModeActive = true;
        Debug.Log("VAD 활성화");
        UpdateButtonColor();

        // 여기에 VAD 시작 로직 추가
        int maxLengthSec = 60;  // Max length of recorded audio from microphone in seconds
        int frequency = 16000;  // Length of audio chunks in seconds, useful for streaming

        microphoneDevice = Microphone.devices[0]; // 첫 번째 마이크 선택
        _clip = Microphone.Start(microphoneDevice, true, maxLengthSec, frequency); // loop = true하게

        _madeLoopLap = false;
        _lastChunkPos = 0;
        _vadStopBegin = null;
        _chunksLength = (int) (_clip.frequency * _clip.channels * chunksLengthSec);
    }

    private void StopVAD()
    {
        isVADModeActive = false;
        Debug.Log("VAD 비활성화");
        UpdateButtonColor();

        // 여기에 VAD 중지 로직 추가
        Microphone.End(microphoneDevice); // 마이크 중지
        ResetBuffer();
    }

    private void UpdateButtonColor()
    {
        if (vadToggleButton != null)
        {
            var buttonImage = vadToggleButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // 활성화 상태면 붉은색, 비활성화 상태면 연두색
                buttonImage.color = isVADModeActive ? Color.red : Color.green;
                vadStatusImage.color = isVADModeActive ? Color.red : Color.green;
            }
        }
    }

}
