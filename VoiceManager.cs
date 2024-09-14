using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class VoiceManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource; // 오디오를 재생할 AudioSource
    private static VoiceManager instance; // 싱글톤 인스턴스

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

        // 시작하자마자 재생
        PlayAudioFromPath("/Voices/Mari/Mari_LogIn_1.ogg");
        // PlayWavFromPersistentPath();
    }

    // 경로로부터 오디오를 로드하고 재생하는 함수
    public void PlayAudioFromPath(string audioPath)
    {
        // string fullPath = "file://" + Application.dataPath + audioPath;  // Assets 패키지화 할 경우 사용
        string fullPath = "file://" + Application.streamingAssetsPath  + audioPath;  // Assets>StreamingAssets 활용시 사용
        StartCoroutine(LoadAudioOGG(fullPath));
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
                Debug.LogError("오디오 로드 실패: " + uwr.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr); // 오디오 클립 가져오기
                audioSource.clip = clip;
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
                audioSource.Play(); // 오디오 재생
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
