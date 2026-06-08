using System;
using UnityEngine;

// SFX 및 BGM 사운드 재생 기능을 처리하는 에이전트 기능 클래스
public class ApiAgentFunctionSfx : MonoBehaviour
{
    private static ApiAgentFunctionSfx instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionSfx Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiAgentFunctionSfx>();
            }
            return instance;
        }
    }

    // 단독 SFX 재생 수행
    public void PlaySfx(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[ApiAgentFunctionSfx] 재생할 오디오 경로가 비어 있습니다.");
            return;
        }

        if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            SubVoiceManager.Instance.PlayWavAudioFromPath(path);
        }
        else
        {
            SubVoiceManager.Instance.PlayAudioFromPath(path);
        }
    }

    // TODO: BGM 재생 기능
    public void PlayBgm(string path)
    {
        // TODO: BGM 재생 기능 구현 예정
    }

    // TODO: BGM 정지 기능
    public void StopBgm()
    {
        // TODO: BGM 정지 기능 구현 예정
    }
}
