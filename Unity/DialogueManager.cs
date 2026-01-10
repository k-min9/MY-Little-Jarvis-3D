using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/**
기본적인 Diaglogue는 json에 저장하고 불러온다.
*/
[Serializable]
public struct Dialogue
{
    public string filePath; // 파일 위치
    public string koreanDialogue; // 한국어 대사
    public string englishDialogue; // 영어 대사
    public string japaneseDialogue; // 일본어 대사
    public string trigger;  // 애니메이션 트리거
}
public class DialogueManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static DialogueManager instance;
    public static DialogueManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DialogueManager>();
            }
            return instance;
        }
    }

    // 대사 리스트
    public List<Dialogue> greetings = new List<Dialogue>();
    public List<Dialogue> idle = new List<Dialogue>();
    public List<Dialogue> select = new List<Dialogue>();
    public List<Dialogue> pick = new List<Dialogue>();
    public List<Dialogue> pat = new List<Dialogue>();

    private string jsonFilePath;

    private void Start()
    {
        LoadDialoguesFromJSON();  // 현재 캐릭터의 JSON 파일 읽기 (CharManager 이후에 실행되어야 함. 안그러면 꼬임)
    }

    // 현재 캐릭터의 JSON 파일 읽기
    public void LoadDialoguesFromJSON()
    {
        try
        {
            string voicePath = CharManager.Instance.GetVoicePath(CharManager.Instance.GetCurrentCharacter());
            jsonFilePath = Path.Combine(Application.streamingAssetsPath, voicePath);

    #if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(LoadJsonFile(jsonFilePath, (json) =>
            {
                AssignDialogues(json);
            }));
    #else
            string json = File.ReadAllText(jsonFilePath);
            AssignDialogues(json);
    #endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading dialogues: {ex.Message}");
            InitializeEmptyDialogues();
        }
    }

    public IEnumerator IEnumLoadDialoguesFromJSON()
    {
        string voicePath = CharManager.Instance.GetVoicePath(CharManager.Instance.GetCurrentCharacter());
        jsonFilePath = Path.Combine(Application.streamingAssetsPath, voicePath);

        // Android에서 JSON 파일을 비동기적으로 로드
        yield return StartCoroutine(LoadJsonFile(jsonFilePath, (json) =>
        {
            AssignDialogues(json);
        }));
    }

    // 안드로이드에서 JSON 파일 읽기
    private IEnumerator LoadJsonFile(string filePath, Action<string> callback)
    {
        UnityWebRequest request = UnityWebRequest.Get(filePath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            callback?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"Error reading JSON from {filePath}: {request.error}");
            callback?.Invoke(string.Empty);
        }
    }

    // 대사 데이터를 JSON에서 할당
    private void AssignDialogues(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            InitializeEmptyDialogues();
            return;
        }

        DialogueCategoryWrapper wrapper = JsonUtility.FromJson<DialogueCategoryWrapper>(json);

        // 각각의 대사 리스트에 데이터 할당
        greetings = wrapper.greetings;
        idle = wrapper.idle;
        select = wrapper.select;
        pick = wrapper.pick;
        pat = wrapper.pat;
    }

    // 대사 리스트 초기화
    private void InitializeEmptyDialogues()
    {
        greetings = new List<Dialogue>();
        idle = new List<Dialogue>();
        select = new List<Dialogue>();
        pick = new List<Dialogue>();
        pat = new List<Dialogue>();
    }

    // JSON 파일 저장
    public void SaveDialoguesToJSON()
    {
        DialogueCategoryWrapper wrapper = new DialogueCategoryWrapper
        {
            greetings = greetings,
            idle = idle,
            select = select,
            pick = pick,
            pat = pat
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
    // pick 리스트에서 랜덤한 대사를 반환하는 함수
    public Dialogue GetRandomPick()
    {
        if (pick.Count == 0)
        {
            Debug.LogWarning("pick 리스트가 비어 있습니다.");
            return default(Dialogue); // 기본값 반환
        }

        int randomIndex = UnityEngine.Random.Range(0, pick.Count);
        return pick[randomIndex];
    }
    // pat 리스트에서 랜덤한 대사를 반환하는 함수
    public Dialogue GetRandomPat()
    {
        if (pat.Count == 0)
        {
            Debug.LogWarning("pat 리스트가 비어 있습니다.");
            return default(Dialogue); // 기본값 반환
        }

        int randomIndex = UnityEngine.Random.Range(0, pat.Count);
        return pat[randomIndex];
    }
}

// JSON 형식에 맞추기 위해 여러 대사 유형을 감싸는 Wrapper 클래스
[System.Serializable]
public class DialogueCategoryWrapper
{
    public List<Dialogue> greetings;
    public List<Dialogue> idle;
    public List<Dialogue> select;
    public List<Dialogue> pick;
    public List<Dialogue> pat;
}