using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[Serializable]
public class ChangeCharClothesInfo
{
    public string name;    // 내부 관리용 의상 이름
    public string text;    // UI에 표시될 의상 텍스트 (예: "< 교복 >")
    public Sprite sprite;  // 해당 의상을 입었을 때의 캐릭터 아이콘
    public GameObject prefab; // 인게임에서 교체될 실제 3D 모델(프리팹)
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
    public List<ChangeCharInfo> characterDatabase = new List<ChangeCharInfo>(); // 인스펙터에서 관리할 전체 데이터 목록

    // Image-Sprite
    [SerializeField] private Image GridToggleBtnImage;
    [SerializeField] private Sprite gridListSprite;
    [SerializeField] private Sprite gridGridSprite;

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

    private void Awake()
    {
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
            // TODO : 리스트 뷰 슬롯 생성 및 데이터 주입
            // GameObject listObj = Instantiate(listSlotSample, listContentParent);
            // listObj.SetActive(true);
            // listObj.GetComponent<ChangeCharCardController>().InitSlot(charInfo);

            // 그리드 뷰 슬롯 생성 및 데이터 주입
            GameObject gridObj = Instantiate(gridSlotSample, gridContentParent);
            gridObj.SetActive(true);
            gridObj.GetComponent<ChangeCharCardController>().InitSlot(charInfo);
        }

        // 인스턴스화 완료 후 원본 샘플 슬롯들을 비활성화
        listSlotSample.SetActive(false);
        gridSlotSample.SetActive(false);
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
    }
}
