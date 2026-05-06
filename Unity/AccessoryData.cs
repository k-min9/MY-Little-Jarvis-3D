using System;
using System.Collections.Generic;
using UnityEngine;

// JSON 저장용 데이터 구조
[Serializable]
public class AccessorySaveData
{
    public string accessoryName; // 악세서리 식별 이름
    public string target1; // 1순위 본/슬롯 이름
    public string target2; // 2순위 본/슬롯 이름
    public string target3; // 3순위 본/슬롯 이름
    public Vector3 localPosition; // 장착 로컬 위치
    public Vector3 localRotation; // 장착 로컬 회전값 (오일러 각도)
}

// 인스펙터 노출용 프리팹 맵핑 구조체
[Serializable]
public class AccessoryPrefabMapping
{
    public string accessoryName; // 악세서리 이름
    public GameObject prefab; // 연결될 프리팹
}

// 악세서리 프리팹 데이터를 UI에서 매핑하고 Get/Set 하는 전역 컨테이너
public class AccessoryData : MonoBehaviour
{
    private static AccessoryData instance; // 싱글톤 인스턴스
    public static AccessoryData Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<AccessoryData>();
            }
            return instance;
        }
    }

    [SerializeField] private List<AccessoryPrefabMapping> prefabMappings = new List<AccessoryPrefabMapping>(); // 프리팹 맵핑 리스트
    [SerializeField] private List<AccessorySaveData> defaultAccessoryData = new List<AccessorySaveData>(); // 인스펙터 기본 세팅 데이터

    private Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>(); // 런타임 캐싱용 딕셔너리

    // 시작 시 딕셔너리 초기화
    private void Awake()
    {
        foreach (AccessoryPrefabMapping mapping in prefabMappings)
        {
            if (prefabDict.ContainsKey(mapping.accessoryName) == false)
            {
                // 딕셔너리에 없으면 새로 추가
                prefabDict.Add(mapping.accessoryName, mapping.prefab);
            }
        }
    }

    // 이름으로 프리팹 가져오기
    public GameObject GetPrefab(string name)
    {
        if (prefabDict.ContainsKey(name))
        {
            // 있으면 해당 프리팹 반환
            return prefabDict[name];
        }
        else
        {
            // 없으면 null 반환
            return null;
        }
    }

    // 이름과 프리팹을 새롭게 맵핑 (Set 역할)
    public void SetPrefab(string name, GameObject prefab)
    {
        if (prefabDict.ContainsKey(name))
        {
            // 이미 있으면 덮어쓰기
            prefabDict[name] = prefab;
        }
        else
        {
            // 없으면 새로 추가
            prefabDict.Add(name, prefab);
        }
    }

    // 기본 세팅 데이터 리스트 반환
    public List<AccessorySaveData> GetDefaultData()
    {
        return defaultAccessoryData;
    }
}
