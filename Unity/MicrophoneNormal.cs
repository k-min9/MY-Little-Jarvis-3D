using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

public class MicrophoneNormal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private AudioClip audioClip;
    private string microphoneDevice;
    private bool isRecording = false;
    private float startRecordingTime;
    private const float maxRecordingDuration = 30f; // 최대 30초 녹음

    private byte[] wavData;

    public Button recordButton; // UI 버튼 참조
    public GameObject overlayFilter; // 버튼 누르는 동안 이미지 (UI 요소로 사용)

    private void Start()
    {
        overlayFilter.SetActive(false);

#if UNITY_ANDROID
        // 마이크 권한 확인 및 요청
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            Debug.Log("Microphone permission not granted. Requesting permission...");
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
#endif
    }

    private void Update()
    {
        // 버튼을 눌렀을 때만 녹음을 진행
        if (isRecording)
        {
            if (Time.time - startRecordingTime >= maxRecordingDuration)
            {
                StopRecording();
            }
            else if (!Input.GetMouseButton(0)) // 버튼에서 손을 뗀 경우
            {
                StopRecording();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isRecording)
        {
            StartRecording();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 이중 체크 중
        // if (isRecording)
        // {
        //     StopRecording();
        // }
    }

    private void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected. Please connect a microphone.");
            return;
        }

        Debug.Log("Recording Started");
        overlayFilter.SetActive(true);

        microphoneDevice = Microphone.devices[0]; // 첫 번째 마이크 선택
        audioClip = Microphone.Start(microphoneDevice, false, (int)maxRecordingDuration, 44100); // 최대 30초 녹음
        startRecordingTime = Time.time;
        isRecording = true;
    }

    private void StopRecording()
    {
        overlayFilter.SetActive(false);

        if (!isRecording || audioClip == null)
        {
            Debug.LogError("No recording found to stop.");
            return;
        }

        Microphone.End(microphoneDevice); // 마이크 중지
        Debug.Log("Recording stopped.");
        isRecording = false;

        // audioClip 공백 제거
        try
        {
            audioClip = TrimSilence(audioClip, 0);
        }  
        catch (Exception ex)
        {
            Debug.Log(ex);
            return;
        }
        

        // WAV 파일 저장
        SaveWavFile(audioClip, "output.wav");
        Debug.Log("Recording saved to output.wav.");
    }

    public AudioClip TrimSilence(AudioClip clip, float min) {
		var samples = new float[clip.samples];

		clip.GetData(samples, 0);

		return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
	}

    public AudioClip TrimSilence(List<float> samples, float min, int channels, int hz) {
		return TrimSilence(samples, min, channels, hz, false, false);
	}

	public AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream) {
		int i;

		for (i=0; i<samples.Count; i++) {
			if (Mathf.Abs(samples[i]) > min) {
				break;
			}
		}

		samples.RemoveRange(0, i);

		for (i=samples.Count - 1; i>0; i--) {
			if (Mathf.Abs(samples[i]) > min) {
				break;
			}
		}

		samples.RemoveRange(i, samples.Count - i);

		// var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);  // obsolete ver
        var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, false);

		clip.SetData(samples.ToArray(), 0);

		return clip;
	}

    private void SaveWavFile(AudioClip clip, string fileName)
    {
        if (clip == null)
        {
            Debug.LogError("AudioClip is null. Cannot save WAV file.");
            return;
        }

        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            int sampleCount = clip.samples * clip.channels;
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);

            wavData = ConvertToWav(samples, clip.channels, clip.frequency);
            fileStream.Write(wavData, 0, wavData.Length);
        }
        Debug.Log($"WAV file saved at: {filePath}");

        // // TODO : internal STT 분기 처리 - server_type_idx에 따라 분기, try error시 분기
        // if (SettingManager.Instance.settings.server_type_idx == 2)
        // {
        //     // 내부 Whisper STT 사용
        //     Debug.Log("Using internal Whisper STT...");
        //     StartCoroutine(WhisperSTTManager.Instance.ProcessSTTFromWavData(wavData));
        // }
        // else
        // {
        //     // 기존 외부 서버 STT 사용(wav 전송 API 호출)
        //     Debug.Log("Using external server STT...");
        //     StartCoroutine(STTUtil.SendWavFileToSTT(this, wavData, "ko", "normal", true));
        // }
        StartCoroutine(STTUtil.SendWavFileToSTT(this, wavData, "ko", "normal", true));
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

}
