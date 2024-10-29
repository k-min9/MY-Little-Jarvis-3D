using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class VoiceManager : MonoBehaviour
{
    [SerializeField] public AudioSource audioSource; // 오디오를 재생할 AudioSource
    private static VoiceManager instance; // 싱글톤 인스턴스
    private Queue<AudioClip> clipQueue = new Queue<AudioClip>(); // AudioClip을 저장하는 Queue
    private bool isQueuePlaying = false;  // 현재 재생 여부를 추적하는 플래그

    private void Awake()
    {
        // 싱글톤 패턴을 적용하여 유일한 인스턴스 유지
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 시작하자마자 재생
        Dialogue greeting = DialogueManager.instance.GetRandomGreeting();
        PlayAudioFromPath(greeting.filePath);
        // PlayWavFromPersistentPath();
    }


    private void Update()
    {
        // AudioSource가 재생 중이지 않고, Queue에 클립이 있으면 다음 클립을 재생
        if (!audioSource.isPlaying && clipQueue.Count > 0)
        {
            PlayNextClip();
        }
    }

    // 경로로부터 오디오를 로드하고 재생하는 함수
    public void PlayAudioFromPath(string audioPath)
    {
        try
        {
            // string fullPath = "file://" + Application.dataPath + audioPath;  // Assets 패키지화 할 경우 사용
            // string fullPath = "file://" + Application.streamingAssetsPath  + audioPath;  // Assets>StreamingAssets 활용시 사용
            string fullPath = Path.Combine(Application.streamingAssetsPath, audioPath);
            StartCoroutine(LoadAudioOGG(fullPath));
        } catch {
            
        }

    }

    public void PlayWavFromPersistentPath()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, "response.wav");  // API wav
        StartCoroutine(LoadAudioWav(fullPath));
    }

    // 오디오 파일을 로드하는 코루틴
    private IEnumerator LoadAudioOGG(string audioPath)
    {
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.OGGVORBIS))
        {
            yield return uwr.SendWebRequest(); // 요청 전송

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                // Debug.LogError("오디오 로드 실패: " + uwr.error);
                Debug.Log("오디오 로드 실패: " + uwr.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr); // 오디오 클립 가져오기
                audioSource.clip = clip;
                audioSource.volume = 1f; // 100%
                try {
                    audioSource.volume = SettingManager.Instance.settings.sound_volumeMaster/100;
                } catch {
                    Debug.Log("ogg volume change error");
                }
                audioSource.Play(); // 오디오 재생
            }
        }
    }

    // 오디오 파일을 로드하는 코루틴. 늘어나면 변수화
    private IEnumerator LoadAudioWav(string audioPath)
    {
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.WAV))
        {
            yield return uwr.SendWebRequest(); // 요청 전송

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("오디오 로드 실패: " + uwr.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr); // 오디오 클립 가져오기
                audioSource.clip = clip;
                audioSource.volume = 1f; // 100%
                try {
                    audioSource.volume = SettingManager.Instance.settings.sound_volumeMaster/100;
                } catch {
                    Debug.Log("wav volume change error");
                }
                audioSource.Play(); // 오디오 재생
            }
        }
    }


    // 오디오 클립을 Queue에 추가하는 함수
    public void AddToQueue(AudioClip clip)
    {
        clipQueue.Enqueue(clip); // 클립을 Queue에 추가

        // 만약 현재 아무것도 재생 중이지 않다면, 바로 재생 시작
        if (!isQueuePlaying)
        {
            PlayNextClip();
        }
    }

    // Queue에 있는 다음 클립을 재생하는 함수
    private void PlayNextClip()
    {
        if (clipQueue.Count > 0)
        {
            isQueuePlaying = true;
            audioSource.clip = clipQueue.Dequeue();  // Queue에서 클립을 가져옴
            audioSource.Play();  // AudioSource로 재생 시작
        }
        else
        {
            isQueuePlaying = false;  // Queue가 비었을 경우 재생 중지
        }
    }

    public void LoadAudioWavToQueue()
    {
        string audioPath = Path.Combine(Application.persistentDataPath, "response.wav");
        StartCoroutine(LoadAudioWavToQueueEnum(audioPath));
    }

    // WAV 파일을 로드하고 Queue에 추가하는 코루틴
    private IEnumerator LoadAudioWavToQueueEnum(string audioPath)
    {
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.WAV))
        {
            yield return uwr.SendWebRequest(); // 요청 전송

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("오디오 로드 실패: " + uwr.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr); // 오디오 클립 가져오기
                AddToQueue(clip); // 클립을 Queue에 추가
            }
        }
    }


    // 오디오 정지 함수
    public void StopAudio()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // 현재 재생(세팅)중인 clip 반환
    public AudioClip GetAudioClip()
    {
        if (audioSource.clip) {
            return audioSource.clip; // 현재 오디오 소스를 반환하거나 적절한 AudioClip 반환
        }

        return null;
    }

    public void ResetAudio()
    {
        // 현재 재생 중인 오디오를 멈춤
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 오디오 큐를 비움
        clipQueue.Clear();

        // 재생 플래그를 false로 설정
        isQueuePlaying = false;

        Debug.Log("Audio playback stopped and queue cleared.");
    }

    // VoiceManager 인스턴스에 접근하는 함수
    public static VoiceManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VoiceManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("VoiceManager");
                    instance = go.AddComponent<VoiceManager>();
                }
            }
            return instance;
        }
    }
}
