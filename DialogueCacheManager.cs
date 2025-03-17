using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// DialogueManager을 전역적으로 관리
public class DialogueCacheManager : MonoBehaviour
{
    public static DialogueCacheManager instance;
    private Dictionary<string, DialogueCategoryWrapper> dialogueCache = new Dictionary<string, DialogueCategoryWrapper>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllDialogues();
            // DebugLoadAllDialogues();
            // Debug.Log(GetRandomGreeting("arona").filePath);
        }
        // else
        // {
        //     Destroy(gameObject);
        // }
    }

    private void LoadAllDialogues()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Sound");
        if (!Directory.Exists(path)) return;

        foreach (string directory in Directory.GetDirectories(path))
        {
            string nickname = Path.GetFileName(directory);
            string jsonFilePath = Path.Combine(directory, "voiceover.json");
            
            try
            {
    #if UNITY_ANDROID && !UNITY_EDITOR
                StartCoroutine(LoadJsonFile(jsonFilePath, (json) =>
                {
                    AssignDialogues(nickname, json);
                }));
    #else
                string json = File.ReadAllText(jsonFilePath);
                AssignDialogues(nickname, json);
    #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading dialogues for {nickname}: {ex.Message}");
            }
        }
    }

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

    private void AssignDialogues(string nickname, string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            DialogueCategoryWrapper dialogues = JsonUtility.FromJson<DialogueCategoryWrapper>(json);
            dialogueCache[nickname] = dialogues;
        }
    }

    public DialogueCategoryWrapper GetDialoguesByNickname(string nickname)
    {
        if (dialogueCache.TryGetValue(nickname, out DialogueCategoryWrapper dialogues))
        {
            return dialogues;
        }
        
        Debug.LogWarning($"No dialogues found for nickname: {nickname}");
        return null;
    }

    private void DebugLoadAllDialogues()
    {
        foreach (var entry in dialogueCache)
        {
            Debug.Log($"Nickname: {entry.Key}, Greetings Count: {entry.Value.greetings.Count}");
        }
    }

    public Dialogue GetRandomGreeting(string nickname)
    {
        if (dialogueCache.TryGetValue(nickname, out DialogueCategoryWrapper dialogues) && dialogues.greetings.Count > 0)
        {   
            int randomIndex = UnityEngine.Random.Range(0, dialogues.greetings.Count);
            return dialogues.greetings[randomIndex];
        }
        
        Debug.LogWarning($"No greetings found for nickname: {nickname}");
        return default;
    }

    public Dialogue GetRandomIdle(string nickname)
    {
        if (dialogueCache.TryGetValue(nickname, out DialogueCategoryWrapper dialogues) && dialogues.idle.Count > 0)
        {   
            int randomIndex = UnityEngine.Random.Range(0, dialogues.idle.Count);
            return dialogues.idle[randomIndex];
        }
        
        Debug.LogWarning($"No idle found for nickname: {nickname}");
        return default;
    }

    public Dialogue GetRandomSelect(string nickname)
    {
        if (dialogueCache.TryGetValue(nickname, out DialogueCategoryWrapper dialogues) && dialogues.select.Count > 0)
        {   
            int randomIndex = UnityEngine.Random.Range(0, dialogues.select.Count);
            return dialogues.select[randomIndex];
        }
        
        Debug.LogWarning($"No select found for nickname: {nickname}");
        return default;
    }

    public Dialogue GetRandomPick(string nickname)
    {
        if (dialogueCache.TryGetValue(nickname, out DialogueCategoryWrapper dialogues) && dialogues.pick.Count > 0)
        {   
            int randomIndex = UnityEngine.Random.Range(0, dialogues.pick.Count);
            return dialogues.pick[randomIndex];
        }
        
        Debug.LogWarning($"No pick found for nickname: {nickname}");
        return default;
    }
}
