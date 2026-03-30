using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public class CharacterDatabaseData
{
    public List<ChangeCharInfo> characters = new List<ChangeCharInfo>();
}

[Serializable]
public class FavoriteSaveData
{
    public List<string> favoriteCharacterNames = new List<string>();
}

[Serializable]
public class ChangeCharClothesInfo
{
    public string name;           // 내부 관리용 의상 이름
    public string text;           // UI에 표시될 의상 텍스트 (예: "< 교복 >")
    public string spriteAddress;  // 해당 의상을 입었을 때의 캐릭터 아이콘의 Address
    public string prefabAddress;  // 인게임에서 교체될 실제 3D 모델(프리팹)의 Address
                                  // "2d_general" 이면 공용 2D 프리팹 특수 로직 사용

    // 2d_general 전용 - Addressable AnimatorController 주소
    public string animatorControllerAddress;

    // 2d_general 전용 - CharAttributes 주입값 (JSON에서 직접 관리)
    public string charAttr_nickname;
    public string charAttr_charcode;
    public string charAttr_voicePath = "default";
    public string charAttr_type = "2D";
    public bool   charAttr_is2DWalkDirectionRight = true;  // 일반 캐릭터도 JSON에서 관리
    public bool   charAttr_isMain = true;

    // 2d_general 전용 - 각 상태별 Blend Tree 클립 갯수 (AnimationBlendController에 주입)
    public int blendCount_idle   = 1;
    public int blendCount_talk   = 1;
    public int blendCount_listen = 1;
    public int blendCount_pat    = 1;
    public int blendCount_walk   = 1;
    public int blendCount_pick   = 1;

    // 2d_general 전용 - Clothes 매핑 (빈값이면 매핑 없음)
    public string toggleClothesAddress;   // CharAttributes.toggleClothes 에 주입할 GameObject Address
    public string changeClothesAddress;   // CharAttributes.changeClothes 에 주입할 GameObject Address

    // Collider/DragHandler 주입값 (0이면 프리팹 기본값 유지)
    public float  headPatThreshold;       // DragHandler.headPatThreshold
}

[Serializable]
public class ChangeCharInfo
{
    public int id;                // sort용
    public string name;           // 캐릭터 이름 (예: "ARONA")
    public bool isFavorite;       // 즐겨찾기 여부 (저장 연동 예정)
    public List<ChangeCharClothesInfo> clothesList = new List<ChangeCharClothesInfo>(); 
}

public class ChangeCharManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static ChangeCharManager instance;
    public static ChangeCharManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChangeCharManager>();
            }
            return instance;
        }
    }

    [Header("Character Data Database")]
    // 이젠 인스펙터에서 관리하지 않고, JSON을 통해 로드됩니다.
    [HideInInspector] public List<ChangeCharInfo> characterDatabase = new List<ChangeCharInfo>(); 

    // Image-Sprite
    [SerializeField] private Image GridToggleBtnImage;
    [SerializeField] private Sprite gridListSprite;
    [SerializeField] private Sprite gridGridSprite;
    
    // 중앙 관리용 Fallback Sprite
    [HideInInspector] public Sprite fallbackSprite;

    [Header("UI Rect Components")]
    [SerializeField] private RectTransform uIParentRect; // 변경할 UI 패널
    [SerializeField] private RectTransform uIHandleRect; // 변경할 UI 패널

    [Header("Scroll Views")]
    [SerializeField] private GameObject scrollViewGrid; // 바둑판 형태 스크롤 뷰
    [SerializeField] private GameObject scrollViewList; // 리스트 형태 스크롤 뷰

    [Header("Slot Prefabs & Parents")]
    [SerializeField] private GameObject listSlotSample;   // 리스트용 아이템 프리팹
    [SerializeField] private Transform listContentParent; // 리스트 Scroll View의 Content(Slots) 영역
    
    [SerializeField] private GameObject gridSlotSample;   // 바둑판용 아이템 프리팹
    [SerializeField] private Transform gridContentParent; // 그리드 Scroll View의 Content(Slots) 영역

    [Header("Grid Mode Setting")]
    public string currentMode = "Grid"; // "Grid" 또는 "List"
    private readonly string[] availableModes = { "Grid", "List" }; // 확장 가능한 모드 배열

    private Canvas _canvas;
    private string favoritesFilePath;
    private string databaseFilePath;

    private void Awake()
    {
        // 핫키 등과 동일하게 config 폴더 사용
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        favoritesFilePath = Path.Combine(directoryPath, "change_char_favorites.json");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        databaseFilePath = Path.Combine(Application.streamingAssetsPath, "config", "character_database.json");
        LoadDatabase(); // JSON 파일에서 데이터베이스 로드 

        // UI 생성 전에 미리 즐겨찾기 데이터를 디스크에서 로드하여 DB에 덮어씌움
        LoadFavorites();
        
        // 중앙 관리용 Fallback Sprite 동기 로딩
        LoadFallbackSprite();

        // Remote 카탈로그 업데이트 체크 (백그라운드)
        StartCoroutine(CheckCatalogUpdates());
        
        // 원격 카탈로그 초기화 후 DLC 로딩 진단 정보 출력 (개발/테스트용)
        TestDLC();
    }

    private void LoadFallbackSprite()
    {
        try
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>("sensei_sprite");
            fallbackSprite = handle.WaitForCompletion();
            
            if (fallbackSprite == null)
            {
                Debug.LogWarning("Fallback sprite 'sensei_sprite' could not be loaded. Please check Addressables.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception loading fallback sprite: " + e.Message);
        }
    }

    // Remote 카탈로그 업데이트 체크 (GitHub Releases에서 최신 카탈로그 동기화)
    private IEnumerator CheckCatalogUpdates()
    {
        Debug.Log("[DLC] 카탈로그 업데이트 체크 시작...");

        // Addressables 카탈로그 업데이트 체크
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status == AsyncOperationStatus.Succeeded)
        {
            if (checkHandle.Result != null && checkHandle.Result.Count > 0)
            {
                Debug.Log($"[DLC] 카탈로그 업데이트 발견: {checkHandle.Result.Count}개");
                foreach (string catalogId in checkHandle.Result)
                {
                    Debug.Log($"[DLC]   - 카탈로그: {catalogId}");
                }

                var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
                yield return updateHandle;

                if (updateHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("[DLC] 카탈로그 업데이트 완료 (성공)");
                }
                else
                {
                    Debug.LogError($"[DLC] 카탈로그 업데이트 실패: {updateHandle.OperationException?.Message}");
                }
                Addressables.Release(updateHandle);
            }
            else
            {
                Debug.Log("[DLC] 카탈로그 최신 상태 (업데이트 없음)");
            }
        }
        else
        {
            Debug.LogWarning($"[DLC] 카탈로그 체크 실패: {checkHandle.OperationException?.Message}");
        }

        Addressables.Release(checkHandle);
    }

    private async void TestDLC()
    {
        Debug.Log("--- [DLC] plana_ball 진단 시작 ---");
        var locs = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync("plana_ball");
        await locs.Task;
        if (locs.Result != null && locs.Result.Count > 0)
        {
            Debug.Log($"[DLC] 'plana_ball' 등록 위치 ({locs.Result.Count}개):");
            foreach (var loc in locs.Result)
            {
                string bundleId = (loc.Dependencies != null && loc.Dependencies.Count > 0) ? loc.Dependencies[0].InternalId : "no_deps";
                Debug.Log($"[DLC]   - Asset InternalId: {loc.InternalId}");
                Debug.Log($"[DLC]   - Asset Provider: {loc.ProviderId}");
                Debug.Log($"[DLC]   - Bundle 경로: {bundleId}");
                Debug.Log($"[DLC]   - 그룹 위치: {(bundleId.StartsWith("http") ? "REMOTE ☁" : "LOCAL 📦")}");
            }
        }
        else
        {
            Debug.LogWarning("[DLC] 카탈로그에서 'plana_ball'을 찾을 수 없습니다.");
        }
        UnityEngine.AddressableAssets.Addressables.Release(locs);

        var sizeHandle = UnityEngine.AddressableAssets.Addressables.GetDownloadSizeAsync("plana_ball");
        await sizeHandle.Task;
        Debug.Log($"[DLC] 'plana_ball' 다운로드 필요 용량: {sizeHandle.Result} bytes");
        UnityEngine.AddressableAssets.Addressables.Release(sizeHandle);
        Debug.Log("--- [DLC] 진단 종료 ---");
    }

    void Start()
    {
        _canvas = FindObjectOfType<Canvas>();  // 최상위 Canvas

        // 데이터베이스 기반 슬롯 생성
        GenerateCharacterSlots();

        // 시작 시 초기 상태 반영
        UpdateGridModeState();
    }

    // 등록된 캐릭터 데이터를 기반으로 UI 슬롯 생성
    private void GenerateCharacterSlots()
    {
        foreach (ChangeCharInfo charInfo in characterDatabase)
        {
            // 리스트 뷰 슬롯 생성 및 데이터 주입
            GameObject listObj = Instantiate(listSlotSample, listContentParent);
            listObj.SetActive(true);
            listObj.GetComponent<ChangeCharListSlotController>().InitSlot(charInfo);

            // 그리드 뷰 슬롯 생성 및 데이터 주입
            GameObject gridObj = Instantiate(gridSlotSample, gridContentParent);
            gridObj.SetActive(true);
            gridObj.GetComponent<ChangeCharCardController>().InitSlot(charInfo);
        }

        // 인스턴스화 완료 후 원본 샘플 슬롯들을 비활성화
        listSlotSample.SetActive(false);
        gridSlotSample.SetActive(false);
    }

    // 화면(Hierarchy)에 생성된 모든 슬롯의 즐겨찾기 UI를 한 방에 갱신 (리스트/그리드 연동용)
    public void RefreshAllSlotsFavoriteUI()
    {
        // 현재 활성화된 모드에 맞는 부모 컨테이너만 탐색하여 UI 갱신 (성능 개선)
        if (currentMode == "List")
        {
            foreach (ChangeCharListSlotController slot in listContentParent.GetComponentsInChildren<ChangeCharListSlotController>(true))
            {
                slot.UpdateFavoriteUI();
            }
        }
        else if (currentMode == "Grid")
        {
            foreach (ChangeCharCardController slot in gridContentParent.GetComponentsInChildren<ChangeCharCardController>(true))
            {
                slot.UpdateFavoriteUI();
            }
        }
    }

    // UI 버튼의 OnClick 이벤트에 연결 (Inspector에서 설정)
    public void ChangeMode()
    {
        // 현재 모드의 인덱스를 찾아 다음 모드로 변경
        int currentIndex = Array.IndexOf(availableModes, currentMode);
        int nextIndex = (currentIndex + 1) % availableModes.Length;
        currentMode = availableModes[nextIndex];

        UpdateGridModeState();
    }

    private void UpdateGridModeState()
    {
        // 버튼 Sprite 전환 및 패널 가로 크기(Width) 조정
        switch (currentMode)
        {
            case "Grid":
                GridToggleBtnImage.sprite = gridGridSprite;
                uIParentRect.sizeDelta = new Vector2(800f, uIParentRect.sizeDelta.y);
                uIHandleRect.sizeDelta = new Vector2(725f, uIHandleRect.sizeDelta.y);
                break;

            case "List":
                GridToggleBtnImage.sprite = gridListSprite;
                uIParentRect.sizeDelta = new Vector2(340f, uIParentRect.sizeDelta.y);
                uIHandleRect.sizeDelta = new Vector2(265f, uIHandleRect.sizeDelta.y);
                break;
        }

        // 이후 자식 요소들(그리드 목록, 리스트 목록)의 개별 크기나 레이아웃 처리를 위한 공간
        if (currentMode == "Grid")
        {
            // 바둑판 배열 뷰 활성화
            scrollViewGrid.SetActive(true);
            scrollViewList.SetActive(false);
        }
        else if (currentMode == "List")
        {
            // 리스트 배열 뷰 활성화
            scrollViewList.SetActive(true);
            scrollViewGrid.SetActive(false);
        }

        // 뷰 모드가 전환(혹은 초기화)될 때마다, 현재 화면에 보여질 슬롯들의 UI 상태를 최신화
        RefreshAllSlotsFavoriteUI();
    }

    // JSON에서 캐릭터 데이터베이스를 로드합니다.
    private void LoadDatabase()
    {
        if (File.Exists(databaseFilePath))
        {
            try
            {
                string json = File.ReadAllText(databaseFilePath);
                CharacterDatabaseData data = JsonUtility.FromJson<CharacterDatabaseData>(json);
                
                if (data != null && data.characters != null)
                {
                    characterDatabase = data.characters;
                    Debug.Log("Character database loaded successfully from StreamingAssets.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load character database: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Character database JSON not found at " + databaseFilePath + ". Please ensure the file exists in StreamingAssets.");
            characterDatabase = new List<ChangeCharInfo>();
        }
    }

    // JSON에서 즐겨찾기 데이터를 불러와 characterDatabase에 반영
    private void LoadFavorites()
    {
        if (File.Exists(favoritesFilePath))
        {
            try
            {
                string json = File.ReadAllText(favoritesFilePath);
                FavoriteSaveData data = JsonUtility.FromJson<FavoriteSaveData>(json);
                
                if (data != null && data.favoriteCharacterNames != null)
                {
                    // DB(characterDatabase)를 순회하며, 파일에 저장된 name이 있으면 isFavorite를 true로 갱신
                    foreach (ChangeCharInfo charInfo in characterDatabase)
                    {
                        charInfo.isFavorite = data.favoriteCharacterNames.Contains(charInfo.name);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to load char favorites: " + e.Message);
            }
        }
    }

    // 변경된 즐겨찾기 상태를 JSON으로 저장
    public void SaveFavorites()
    {
        try
        {
            FavoriteSaveData data = new FavoriteSaveData();
            foreach (ChangeCharInfo charInfo in characterDatabase)
            {
                // 중복 저장을 방지하기 위해 Contains로 이미 추가된 name인지 체크
                if (charInfo.isFavorite && !data.favoriteCharacterNames.Contains(charInfo.name))
                {
                    data.favoriteCharacterNames.Add(charInfo.name);
                }
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(favoritesFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save char favorites: " + e.Message);
        }
    }
}
