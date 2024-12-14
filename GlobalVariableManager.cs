using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/**
기본적인 Diaglogue는 json에 저장하고 불러온다.
DialogueManager로 교체
*/
[Serializable]
public struct SettingVariable
{
    public string filePath; // 파일 위치
    public string koreanDialogue; // 한국어 대사
    public string englishDialogue; // 영어 대사
    public string japaneseDialogue; // 일본어 대사
}

// JSON 형식에 맞추기 위해 여러 대사 유형을 감싸는 Wrapper 클래스
[System.Serializable]
public class SettingVariableWrapper
{
    public List<Dialogue> greetings;
    public List<Dialogue> idle;
    public List<Dialogue> select;
    public List<Dialogue> pick;
}

public class GlobalVariableManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GlobalVariableManager instance;

    // 대사 리스트
    public List<Dialogue> greetings = new List<Dialogue>();
    public List<Dialogue> idle = new List<Dialogue>();
    public List<Dialogue> select = new List<Dialogue>();
    public List<Dialogue> pick = new List<Dialogue>();

    private string jsonFilePath;

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 오브젝트 유지
        }
        else
        {
            // Destroy(gameObject); // 이미 인스턴스가 존재하면 파괴
        }
    }

    void Start()
    {
        // StreamingAssets 폴더의 경로 설정
        jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Sound/Mari/Mari_Voiceover.json");

        LoadDialoguesFromJSON();
    }

    // JSON 파일 읽기
    public void LoadDialoguesFromJSON()
    {
        string json = File.ReadAllText(jsonFilePath);
        // Debug.Log(json);
        SettingVariable wrapper = JsonUtility.FromJson<SettingVariable>(json);

        // 각각의 대사 리스트에 데이터 할당
        // greetings = wrapper.greetings;
        // idle = wrapper.idle;
        // select = wrapper.select;
        // pick = wrapper.pick;
    }

    // JSON 파일 저장
    public void SaveDialoguesToJSON()
    {
        SettingVariable wrapper = new SettingVariable
        {
            // greetings = greetings,
            // idle = idle,
            // pick = pick
        };

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(jsonFilePath, json);
        Debug.Log("대사가 성공적으로 저장되었습니다.");
    }

    // greetings 리스트에서 랜덤한 대사를 반환하는 함수
    public Dialogue GetRandomGreeting()
    {
        if (greetings.Count == 0)
        {
            Debug.LogWarning("greetings 리스트가 비어 있습니다.");
            return default(Dialogue); // 기본값 반환
        }

        int randomIndex = UnityEngine.Random.Range(0, greetings.Count);
        return greetings[randomIndex];
    }
    // idle 리스트에서 랜덤한 대사를 반환하는 함수
    public Dialogue GetRandomIdle()
    {
        if (idle.Count == 0)
        {
            Debug.LogWarning("idle 리스트가 비어 있습니다.");
            return default(Dialogue); // 기본값 반환
        }

        int randomIndex = UnityEngine.Random.Range(0, idle.Count);
        return idle[randomIndex];
    }
    // select 리스트에서 랜덤한 대사를 반환하는 함수
    public Dialogue GetRandomSelect()
    {
        if (select.Count == 0)
        {
            Debug.LogWarning("idle 리스트가 비어 있습니다.");
            return default(Dialogue); // 기본값 반환
        }

        int randomIndex = UnityEngine.Random.Range(0, select.Count);
        return select[randomIndex];
    }
}

