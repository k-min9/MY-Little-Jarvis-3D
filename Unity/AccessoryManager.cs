using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// JSON 직렬화를 위한 리스트 래퍼
[Serializable]
public class AccessorySaveDataList
{
    public List<AccessorySaveData> items = new List<AccessorySaveData>(); // 악세서리 데이터 리스트
}

// 악세서리 JSON IO 및 장착/해제를 관리하는 매니저 클래스
public class AccessoryManager : MonoBehaviour
{
    private static AccessoryManager instance; // 싱글톤 인스턴스
    public static AccessoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<AccessoryManager>();
            }
            return instance;
        }
    }

    private Dictionary<string, AccessorySaveData> accessorySaveDataDict = new Dictionary<string, AccessorySaveData>(); // 로드된 JSON 데이터 딕셔너리

    // 시작 시 JSON 데이터 로드
    private void Start()
    {
        LoadAccessoryData();
    }

    /* JSON 구조 샘플 (accessory_data.json):
    {
        "items": [
            {
                "accessoryName": "arona_a_chipao",
                "target1": "Slot_Head_1",
                "target2": "mixamorig:Head",
                "target3": "",
                "localPosition": {"x":0.0, "y":0.1, "z":0.0},
                "localRotation": {"x":0.0, "y":0.0, "z":0.0}
            }
        ]
    }
    */
    // JSON 데이터 파일 로드 및 인스펙터 기본 데이터 병합
    public void LoadAccessoryData()
    {
        accessorySaveDataDict.Clear();

        // 1. 인스펙터 기본 데이터 병합 (AccessoryData에서 제공)
        List<AccessorySaveData> defaultDataList = AccessoryData.Instance.GetDefaultData();
        foreach (AccessorySaveData data in defaultDataList)
        {
            if (accessorySaveDataDict.ContainsKey(data.accessoryName) == false)
            {
                // 딕셔너리에 없으면 기본 데이터 추가
                accessorySaveDataDict.Add(data.accessoryName, data);
            }
        }

        string filePath = Path.Combine(Application.persistentDataPath, "accessory_data.json"); // 저장 경로 설정

        // 2. JSON 데이터 파일 로드 및 우선 적용
        if (File.Exists(filePath))
        {
            // 파일이 존재하면 읽어서 딕셔너리에 캐싱
            string json = File.ReadAllText(filePath);
            AccessorySaveDataList dataList = JsonUtility.FromJson<AccessorySaveDataList>(json);
            
            foreach (AccessorySaveData data in dataList.items)
            {
                if (accessorySaveDataDict.ContainsKey(data.accessoryName))
                {
                    // JSON 데이터가 우선이므로 기존(인스펙터) 값을 덮어쓰기
                    accessorySaveDataDict[data.accessoryName] = data;
                }
                else
                {
                    // 딕셔너리에 없으면 새로 추가
                    accessorySaveDataDict.Add(data.accessoryName, data);
                }
            }
        }
    }

    // 현재 캐싱된 데이터를 JSON 파일로 저장
    public void SaveAccessoryData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "accessory_data.json"); // 저장 경로 설정
        
        AccessorySaveDataList dataList = new AccessorySaveDataList(); // 저장할 리스트 래퍼 생성
        dataList.items = new List<AccessorySaveData>(accessorySaveDataDict.Values);
        
        string json = JsonUtility.ToJson(dataList, true); // JSON 문자열로 변환 (보기 좋게)
        File.WriteAllText(filePath, json);
    }

    // 악세서리 장착 및 기존 악세서리 해제
    public void Equip(GameObject target, string accessoryName, string targetName = null, Vector3? localPosition = null, Vector3? localRotation = null)
    {
        AccessorySaveData savedData = null; // JSON에서 로드된 세팅 데이터

        if (accessorySaveDataDict.ContainsKey(accessoryName))
        {
            // 저장된 데이터가 있으면 가져오기
            savedData = accessorySaveDataDict[accessoryName];
        }

        Vector3 finalPos = Vector3.zero; // 적용될 최종 로컬 위치
        if (localPosition.HasValue)
        {
            // 파라미터가 있으면 최우선 적용
            finalPos = localPosition.Value;
        }
        else if (savedData != null)
        {
            // 없으면 JSON 데이터 사용
            finalPos = savedData.localPosition;
        }

        Vector3 finalRot = Vector3.zero; // 적용될 최종 로컬 회전값
        if (localRotation.HasValue)
        {
            // 파라미터가 있으면 최우선 적용
            finalRot = localRotation.Value;
        }
        else if (savedData != null)
        {
            // 없으면 JSON 데이터 사용
            finalRot = savedData.localRotation;
        }

        Transform finalTargetBone = null; // 최종 부모가 될 Transform

        if (string.IsNullOrEmpty(targetName) == false)
        {
            // 파라미터로 주어진 타겟 이름이 있으면 해당 타겟만 검색
            finalTargetBone = getSlotTransformFromName(target, targetName);
        }
        else if (savedData != null)
        {
            // JSON 데이터의 target 1, 2, 3 우선순위로 탐색 (Fallback)
            if (string.IsNullOrEmpty(savedData.target1) == false)
            {
                // 1순위 탐색
                finalTargetBone = getSlotTransformFromName(target, savedData.target1);
            }
            
            if (finalTargetBone == null && string.IsNullOrEmpty(savedData.target2) == false)
            {
                // 2순위 탐색
                finalTargetBone = getSlotTransformFromName(target, savedData.target2);
            }
            
            if (finalTargetBone == null && string.IsNullOrEmpty(savedData.target3) == false)
            {
                // 3순위 탐색
                finalTargetBone = getSlotTransformFromName(target, savedData.target3);
            }
        }

        if (finalTargetBone == null)
        {
            // 최종적으로 타겟을 찾지 못했으면 중단
            return;
        }

        if (HasEquippedAccessory(finalTargetBone))
        {
            // 해당 부위에 이미 악세서리가 있으면 파괴 (Unequip 대체)
            RemoveEquippedAccessory(finalTargetBone);
        }

        if (string.IsNullOrEmpty(accessoryName))
        {
            // 장착할 악세서리 이름이 없거나 빈 문자열이면 기존 것을 벗기는 것으로 종료
            return;
        }

        // AccessoryData 싱글톤은 항상 존재한다고 가정하므로 바로 사용 (null 체크 금지 규칙)
        GameObject prefab = AccessoryData.Instance.GetPrefab(accessoryName); // 프리팹 가져오기
        
        if (prefab == null)
        {
            // 등록된 프리팹이 없으면 새로 장착하지 않고 종료
            return;
        }

        // 새로운 악세서리 인스턴스화 후 부모 설정
        GameObject newAccessory = Instantiate(prefab, finalTargetBone);
        newAccessory.transform.localPosition = finalPos;
        newAccessory.transform.localRotation = Quaternion.Euler(finalRot);
    }

    // 지정된 대상(Slot/Bone)의 악세서리를 명시적으로 해제
    public void UnEquip(GameObject target, string targetName)
    {
        if (target == null || string.IsNullOrEmpty(targetName))
        {
            // 대상이나 대상 이름이 없으면 중단
            return;
        }

        Transform targetBone = getSlotTransformFromName(target, targetName); // 타겟 검색

        if (targetBone != null)
        {
            if (HasEquippedAccessory(targetBone))
            {
                // 악세서리가 장착되어 있으면 파괴
                RemoveEquippedAccessory(targetBone);
            }
        }
    }

    // 대상 오브젝트 하위에서 이름으로 슬롯(Transform)을 찾아 반환
    public Transform getSlotTransformFromName(GameObject target, string slotName)
    {
        if (target == null)
        {
            // 타겟이 없으면 null 반환
            return null;
        }

        // 재귀 탐색을 통해 뼈대 찾기
        return FindBoneRecursive(target.transform, slotName);
    }

    // 이름으로 계층 구조를 재귀 탐색하여 Transform 반환
    private Transform FindBoneRecursive(Transform current, string name)
    {
        if (current.name == name)
        {
            // 현재 노드 이름이 일치하면 반환
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform child = current.GetChild(i); // 자식 노드 가져오기
            Transform result = FindBoneRecursive(child, name); // 재귀 호출
            
            if (result != null)
            {
                // 하위에서 찾았으면 반환
                return result;
            }
        }

        // 모두 탐색해도 없으면 null 반환
        return null;
    }

    // 해당 슬롯(본) 하위에 장착된 악세서리가 있는지 확인
    private bool HasEquippedAccessory(Transform slot)
    {
        if (slot.childCount > 0)
        {
            // 자식이 존재하면 악세서리가 있다고 판단
            return true;
        }
        else
        {
            // 자식이 없으면 없다고 판단
            return false;
        }
    }

    // 해당 슬롯(본) 하위의 모든 악세서리 게임오브젝트 파괴
    private void RemoveEquippedAccessory(Transform slot)
    {
        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            // 리스트의 역순으로 오브젝트 파괴
            Destroy(slot.GetChild(i).gameObject);
        }
    }
}
