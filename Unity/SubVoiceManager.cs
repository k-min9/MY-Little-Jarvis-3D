using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using System.Collections.Generic;

// 중지가 힘듬. 캐릭터별로, 하나씩 voiceManager을 가지게하고 component를 관리하는 방식으로 중지 구현은 쉽게 가능. 
// 그렇게 되어도 효과음 등으로 쓰기 좋으니 지우지 말 것
public class SubVoiceManager : MonoBehaviour
{
    private static SubVoiceManager instance; // 싱글톤 인스턴스
    public static SubVoiceManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SubVoiceManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SubVoiceManager");
                    instance = go.AddComponent<SubVoiceManager>();
                }
            }
            return instance;
        }
    }

    
    private List<AudioSource> audioSources = new List<AudioSource>();  // 여러 개의 AudioSource를 관리할 수 있는 리스트    
    public int maxAudioSources = 20;  // 최대 동시에 재생 가능한 AudioSource 수 (상한선)    
    private int initialAudioSourceCount = 5;  // 초기 AudioSource 개수

    void Start()
    {
        // 처음에 5개의 AudioSource를 생성하여 리스트에 추가
        for (int i = 0; i < initialAudioSourceCount; i++)
        {
            AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
            audioSources.Add(newAudioSource);
        }
    }

    // 경로로부터 오디오를 로드하고 재생하는 함수
    public void PlayAudioFromPath(string audioPath)
    {
        try
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, audioPath);
            StartCoroutine(LoadAudioOGG(fullPath));
        }
        catch
        {
            // 예외 처리
            Debug.Log("Audio Play Error");
        }
    }

    // 오디오 파일을 로드하는 코루틴
    private IEnumerator LoadAudioOGG(string audioPath)
    {
        // 사용 가능한 AudioSource를 찾기
        AudioSource availableSource = GetAvailableAudioSource();
        if (availableSource == null)
        {
            Debug.LogWarning("모든 AudioSource가 사용 중입니다.");
            yield break;
        }

        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.OGGVORBIS))
        {
            yield return uwr.SendWebRequest(); // 요청 전송

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("오디오 로드 실패: " + uwr.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr); // 오디오 클립 가져오기
                availableSource.clip = clip;
                availableSource.volume = 1f; // 기본 볼륨

                try
                {
                    availableSource.volume = SettingManager.Instance.settings.sound_volumeMaster / 100;
                }
                catch
                {
                    Debug.Log("오디오 볼륨 변경 오류");
                }

                availableSource.Play(); // 오디오 재생
            }
        }
    }

    // 사용 가능한 AudioSource를 찾거나 새로운 AudioSource를 생성하는 함수
    private AudioSource GetAvailableAudioSource()
    {
        // 사용 가능한 AudioSource를 먼저 찾기
        foreach (var audioSource in audioSources)
        {
            if (!audioSource.isPlaying)
            {
                return audioSource; // 사용 가능한 AudioSource 반환
            }
        }

        // 사용 가능한 AudioSource가 없으면, 최대 개수 미만일 경우 새로 생성
        if (audioSources.Count < maxAudioSources)
        {
            AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
            audioSources.Add(newAudioSource);
            return newAudioSource; // 새로 만든 AudioSource 반환
        }

        // 만약 이미 최대 개수에 도달한 경우, null 반환
        return null;
    }
}
