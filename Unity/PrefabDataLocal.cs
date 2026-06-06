using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalSpriteEntry
{
    public string key;
    public Sprite icon;
}

[Serializable]
public class LocalPrefabEntry
{
    public string key;
    public GameObject prefab;
}

[Serializable]
public class LocalAnimatorControllerEntry
{
    public string key;
    public RuntimeAnimatorController animatorController;
}

[Serializable]
public class LocalPrefabCharacterData
{
    public string name;
    public List<LocalSpriteEntry> sprites = new List<LocalSpriteEntry>();
    public List<LocalPrefabEntry> prefabs = new List<LocalPrefabEntry>();
    public List<LocalAnimatorControllerEntry> animatorControllers = new List<LocalAnimatorControllerEntry>();
}

public class PrefabDataLocal : MonoBehaviour
{
    private static PrefabDataLocal instance; // 싱글톤 인스턴스
    public static PrefabDataLocal Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<PrefabDataLocal>();
            }
            return instance;
        }
    }

    [SerializeField] private List<LocalPrefabCharacterData> characters = new List<LocalPrefabCharacterData>();

    private Dictionary<string, Sprite> spriteMap;
    private Dictionary<string, GameObject> prefabMap;
    private Dictionary<string, RuntimeAnimatorController> animatorControllerMap;

    private void Awake()
    {
        BuildMaps();
        RegisterAllPrefabsToCharList();
    }

    private void OnValidate()
    {
        spriteMap = null;
        prefabMap = null;
        animatorControllerMap = null;
    }

    public Sprite GetSprite(string key)
    {
        EnsureMaps();

        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (spriteMap.TryGetValue(key, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[PrefabDataLocal] Sprite key not found: {key}");
        return null;
    }

    public GameObject GetPrefab(string key)
    {
        EnsureMaps();

        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (prefabMap.TryGetValue(key, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogWarning($"[PrefabDataLocal] Prefab key not found: {key}");
        return null;
    }

    public RuntimeAnimatorController GetAnimatorController(string key)
    {
        EnsureMaps();

        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (animatorControllerMap.TryGetValue(key, out RuntimeAnimatorController animatorController))
        {
            return animatorController;
        }

        Debug.LogWarning($"[PrefabDataLocal] AnimatorController key not found: {key}");
        return null;
    }

    public bool ContainsSprite(string key)
    {
        EnsureMaps();
        return !string.IsNullOrEmpty(key) && spriteMap.ContainsKey(key);
    }

    public bool ContainsPrefab(string key)
    {
        EnsureMaps();
        return !string.IsNullOrEmpty(key) && prefabMap.ContainsKey(key);
    }

    public bool ContainsAnimatorController(string key)
    {
        EnsureMaps();
        return !string.IsNullOrEmpty(key) && animatorControllerMap.ContainsKey(key);
    }

    private void EnsureMaps()
    {
        if (spriteMap == null || prefabMap == null || animatorControllerMap == null)
        {
            BuildMaps();
        }
    }

    private void BuildMaps()
    {
        spriteMap = new Dictionary<string, Sprite>();
        prefabMap = new Dictionary<string, GameObject>();
        animatorControllerMap = new Dictionary<string, RuntimeAnimatorController>();

        foreach (LocalPrefabCharacterData character in characters)
        {
            string characterName = string.IsNullOrWhiteSpace(character.name) ? "(no name)" : character.name;

            foreach (LocalSpriteEntry entry in character.sprites)
            {
                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    Debug.LogWarning($"[PrefabDataLocal] Empty sprite key skipped: {characterName}");
                    continue;
                }

                if (spriteMap.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[PrefabDataLocal] Duplicate sprite key skipped: {characterName}/{entry.key}");
                    continue;
                }

                spriteMap.Add(entry.key, entry.icon);
            }

            foreach (LocalPrefabEntry entry in character.prefabs)
            {
                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    Debug.LogWarning($"[PrefabDataLocal] Empty prefab key skipped: {characterName}");
                    continue;
                }

                if (prefabMap.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[PrefabDataLocal] Duplicate prefab key skipped: {characterName}/{entry.key}");
                    continue;
                }

                prefabMap.Add(entry.key, entry.prefab);
            }

            foreach (LocalAnimatorControllerEntry entry in character.animatorControllers)
            {
                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    Debug.LogWarning($"[PrefabDataLocal] Empty animatorController key skipped: {characterName}");
                    continue;
                }

                if (animatorControllerMap.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[PrefabDataLocal] Duplicate animatorController key skipped: {characterName}/{entry.key}");
                    continue;
                }

                animatorControllerMap.Add(entry.key, entry.animatorController);
            }
        }
    }

    private void RegisterAllPrefabsToCharList()
    {
        int addedCount = 0;

        foreach (LocalPrefabCharacterData character in characters)
        {
            foreach (LocalPrefabEntry entry in character.prefabs)
            {
                if (!CharManager.Instance.charList.Contains(entry.prefab))
                {
                    CharManager.Instance.charList.Add(entry.prefab);
                    addedCount++;
                }
            }
        }

        Debug.Log($"[PrefabDataLocal] Registered local prefabs to charList: {addedCount}");
    }
}
