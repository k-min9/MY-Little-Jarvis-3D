using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// OCR 옵션 데이터 클래스
[System.Serializable]
public class OCROptions
{
    // === 서버 전송 파라미터 ===
    // 번역 관련
    public bool useTranslate;               // 번역 사용 여부
    public string targetLang;               // "ko", "ja", "en" (번역 목표 언어)
    public string originLang;               // "auto", "ja", "ko", "en" (원본 언어, auto=자동감지)
    public bool isFormality;                // 존칭 번역 여부
    
    // OCR 설정
    public bool isSentence;                 // 문장 단위(true) vs 단어 단위(false)
    public int mergeThreshold;              // 텍스트 병합 임계값 (픽셀, -1=서버 기본값)
    public bool targetLangAutoDetect;       // 자동 언어 감지
    
    // === 클라이언트 처리 옵션 ===
    // 표시 옵션
    public bool displayResults;             // OCR 결과 표시 여부
    public bool displayOrigin;              // 원문도 함께 표시 (번역 사용시)
    
    // TTS 옵션
    public bool useTTS;                     // TTS 읽기 여부
    public int actorTypeIdx;                // Actor 타입 인덱스 (드롭다운용)
    public string actorType;                // Actor 타입 ("Auto", "Arona", "Plana" 등)
    public bool ttsAutoDetectLang;          // TTS 언어 자동 감지
    public bool readTranslatedResult;       // 번역 결과 읽기 (True=ToLang, False=FromLang)
    public bool aiReadingCorrection;        // AI 읽기 보정 (Furigana)
    
    // 클릭 옵션
    public bool useAutoClick;               // 자동 클릭 여부
    public int clickTarget;                 // 0: 전체, 1: 최초 1개
    public int clickPriority;               // 0: 좌상(TopLeft), 1: 좌하, 2: 우상, 3: 우하(BottomRight)
    public bool clickExactMatch;            // true=정확히 일치, false=포함
    public List<string> clickWhitelist;     // 화이트리스트 (비어있으면 모두 허용)
    public List<string> clickBlacklist;     // 블랙리스트
    
    // 디버그/로깅 옵션
    public bool saveResult;                 // OCR 결과 JSON 저장
    public bool saveImage;                  // OCR 시각화 이미지 저장
    public bool isDebug;                    // 상세 디버그 로그 저장
    
    // 기본 생성자 (List 초기화)
    public OCROptions()
    {
        clickWhitelist = new List<string>();
        clickBlacklist = new List<string>();
    }
}

/*
Settings > OCR
├ Options (슬롯 선택: Option1 / Option2 / Option3)
│
├ TranslationLanguage
│   ├ From (originLang) - 항상 활성
│   └ To (targetLang) - [Translation Toggle에 따라 interactable 토글]
│
├ TranslationOption
│   └ Translation Toggle (useTranslate)
│       └─ ON: To dropdown.interactable = true
│       └─ OFF: To dropdown.interactable = false
│
├ ShowResultToggle (displayResults)
│
├ ReadResultToggle (useTTS)
│   └─ ON: ReadResultActor.SetActive(true), ReadTranslatedResultToggle.SetActive(true), AIReadingCorrectionToggle.SetActive(true)
│   └─ OFF: ReadResultActor.SetActive(false), ReadTranslatedResultToggle.SetActive(false), AIReadingCorrectionToggle.SetActive(false)
│
├ ReadResultActor (Actor dropdown) - [ReadResultToggle에 따라 SetActive 토글]
├ ReadTranslatedResultToggle (readTranslatedResult) - [ReadResultToggle에 따라 SetActive, TranslationToggle에 따라 interactable]
│   └─ ON: ToLang 사용
│   └─ OFF: FromLang 사용
│
├ AIReadingCorrectionToggle (aiReadingCorrection) - [ReadResultToggle에 따라 SetActive 토글]
│   └─ ON: 일본어일 때 Furigana 변환 후 TTS
│   └─ OFF: 원본 텍스트로 TTS
│
├ MergeTextToggle (isSentence)
│   └─ ON(문장단위): MergeSlider.SetActive(false)
│   └─ OFF(단어단위): MergeSlider.SetActive(true)
│
├ MergeSlider (mergeThreshold: 0~60) - [MergeTextToggle에 따라 SetActive 토글]
│
├ ClickResultToggle (useAutoClick) - Experiment
│   └─ ON: ClickOptionTarget.SetActive(true), ClickOptionMatch.SetActive(true)
│   └─ OFF: ClickOptionTarget.SetActive(false), ClickOptionMatch.SetActive(false)
│
├ ClickOptionTarget (Target dropdown)
├ ClickOptionMatch (From dropdown + Keyword InputField)
│
└ TargetLang (targetLangAutoDetect)
*/

// OCR 옵션 관리자
public class OCRManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static OCRManager instance;
    public static OCRManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OCRManager>();
            }
            return instance;
        }
    }

    [Header("Translation")]
    [SerializeField] private Toggle translationToggle;
    [SerializeField] private TMP_Dropdown fromLangDropdown;
    [SerializeField] private TMP_Dropdown toLangDropdown;

    [Header("Display")]
    [SerializeField] private Toggle showResultToggle;

    [Header("TTS")]
    [SerializeField] private Toggle readResultToggle;
    [SerializeField] private GameObject readResultActorObject;
    [SerializeField] private TMP_Dropdown actorDropdown;
    [SerializeField] private Toggle readTranslatedResultToggle;
    [SerializeField] private Toggle aiReadingCorrectionToggle;

    [Header("Merge")]
    [SerializeField] private Toggle mergeTextToggle;
    [SerializeField] private GameObject mergeSliderObject;
    [SerializeField] private Slider mergeSlider;

    [Header("Click (Experiment)")]
    [SerializeField] private Toggle clickResultToggle;
    [SerializeField] private GameObject clickOptionTargetObject;
    [SerializeField] private GameObject clickOptionMatchObject;
    [SerializeField] private TMP_Dropdown clickTargetDropdown;
    [SerializeField] private TMP_Dropdown clickPriorityDropdown;
    [SerializeField] private TMP_InputField keywordInputField;

    // Data - 캐시만 사용 (currentOption 제거)
    private int activeSlot = 1;  // 현재 활성 슬롯 (1~3)
    private OCROptions options1;  // 슬롯1 옵션
    private OCROptions options2;  // 슬롯2 옵션
    private OCROptions options3;  // 슬롯3 옵션
    
    private bool isInitialized = false;  // 초기화 완료 플래그 (로딩 중 저장 방지)
    
    void Awake()
    {
        // 초기화
        InitializeActorDropdown();  // Actor Dropdown 초기화
        InitializeClickDropdowns();  // Click 관련 Dropdown 초기화

        LoadAllOptions();  // 모든 슬롯 로드
    }
    
    // SettingManager에서 호출할 초기화
    public void InitializeFromSettings(string ocrOptionType)
    {
        // ocrOptionType을 슬롯 번호로 변환 후 활성 슬롯으로 설정
        int slotNumber = 1;
        if (ocrOptionType == "Options2")
            slotNumber = 2;
        else if (ocrOptionType == "Options3")
            slotNumber = 3;
        activeSlot = slotNumber;
        
        // 활성 슬롯의 옵션으로 UI 갱신
        UpdateUIFromOptions(GetCurrentOptions());
        
        // 초기화 완료 - 이제부터 UI 변경 시 저장 허용
        isInitialized = true;
        Debug.Log("[OCRManager] Initialization complete - Save enabled");
    }
    
    // 모든 슬롯 옵션 로드 (게임 시작시 호출)
    private void LoadAllOptions()
    {
        options1 = LoadOptionsFromFile(1);
        options2 = LoadOptionsFromFile(2);
        options3 = LoadOptionsFromFile(3);
        
        Debug.Log("[OCRManager] All slots loaded into cache");
    }
    
    // 옵션 파일 경로 가져오기
    private string GetOptionsFilePath(int slot)
    {
        if (slot < 1 || slot > 3)
        {
            Debug.LogError($"[OCRManager] Invalid slot number: {slot}. Must be 1-3.");
            slot = 1;
        }
        
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        
        // 디렉토리가 없으면 생성
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        return Path.Combine(directoryPath, $"ocr_options{slot}.json");
    }
    
    // 옵션 로드 (파일에서 읽어서 반환)
    private OCROptions LoadOptionsFromFile(int slot)
    {
        string filePath = GetOptionsFilePath(slot);
        
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                OCROptions options = JsonUtility.FromJson<OCROptions>(json);
                
                // List 초기화 확인 (null 방지)
                if (options.clickWhitelist == null) options.clickWhitelist = new List<string>();
                if (options.clickBlacklist == null) options.clickBlacklist = new List<string>();
                
                // 필수 문자열 필드 검증 및 보정
                if (string.IsNullOrEmpty(options.targetLang)) options.targetLang = "ko";
                if (string.IsNullOrEmpty(options.originLang)) options.originLang = "auto";
                if (string.IsNullOrEmpty(options.actorType)) options.actorType = "Auto";
                
                // mergeThreshold 범위 검증 (0~60)
                if (options.mergeThreshold < 0 || options.mergeThreshold > 60)
                {
                    options.mergeThreshold = 0;
                }
                
                Debug.Log($"[OCRManager] Loaded options from slot {slot}");
                return options;
            }
            else
            {
                Debug.Log($"[OCRManager] No saved options for slot {slot}, creating with defaults");
                return GetDefaultOptions(slot);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[OCRManager] Failed to load options from slot {slot}: {e.Message}. Using defaults.");
            return GetDefaultOptions(slot);
        }
    }
    
    // 옵션 저장 (특정 슬롯에 저장)
    private void SaveOptions(int slot)
    {
        if (!isInitialized) return;

        string filePath = GetOptionsFilePath(slot);
        OCROptions options = GetOptions(slot);
        
        try
        {
            string json = JsonUtility.ToJson(options, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[OCRManager] Saved options to slot {slot}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[OCRManager] Failed to save options to slot {slot}: {e.Message}");
        }
    }
    
    // 현재 활성 옵션 가져오기 (활성 슬롯의 캐시 직접 반환)
    public OCROptions GetCurrentOptions()
    {
        return GetOptions(activeSlot);
    }
    
    // 특정 슬롯의 옵션 가져오기 (캐시에서 반환)
    public OCROptions GetOptions(int slot)
    {
        switch (slot)
        {
            case 1: return options1;
            case 2: return options2;
            case 3: return options3;
            default:
                Debug.LogWarning($"[OCRManager] Invalid slot {slot}, returning slot 1");
                return options1;
        }
    }
    
    // 활성 슬롯 변경
    public void SetActiveSlot(int slot)
    {
        activeSlot = slot;
        UpdateUIFromOptions(GetCurrentOptions());
    }
        
    // 현재 활성 슬롯 번호 가져오기
    public int GetActiveSlot()
    {
        return activeSlot;
    }
    
    // === 헬퍼 메서드 ===
    
    // 언어 코드 변환 (From: auto, ko, ja, en)
    private string GetLangCodeFromIdx(int idx, bool includeAuto)
    {
        if (includeAuto)
        {
            switch (idx)
            {
                case 0: return "auto";
                case 1: return "ko";
                case 2: return "ja";
                case 3: return "en";
                default: return "auto";
            }
        }
        else
        {
            switch (idx)
            {
                case 0: return "ko";
                case 1: return "ja";
                case 2: return "en";
                default: return "ko";
            }
        }
    }
    
    private int GetLangIdxFromCode(string code, bool includeAuto)
    {
        if (includeAuto)
        {
            switch (code)
            {
                case "auto": return 0;
                case "ko": return 1;
                case "ja": return 2;
                case "en": return 3;
                default: return 0;
            }
        }
        else
        {
            switch (code)
            {
                case "ko": return 0;
                case "ja": return 1;
                case "en": return 2;
                default: return 0;
            }
        }
    }
    
    // Actor 타입 변환 (Auto + STTDataActor의 모든 actor)
    private string GetActorTypeFromIdx(int idx)
    {
        if (idx == 0) return "Auto";
        
        // STTDataActor에서 actor 목록 가져오기
        List<string> actors = STTDataActor.GetAllActorIds();
        
        // idx가 범위 내에 있으면 해당 actor의 표시명 반환
        if (idx > 0 && idx <= actors.Count)
        {
            return STTDataActor.GetDisplayName(actors[idx - 1]);
        }
        
        return "Auto";
    }
    
    private int GetActorIdxFromType(string actorType)
    {
        if (actorType == "Auto") return 0;
        
        // STTDataActor에서 actor 목록 가져오기
        List<string> actors = STTDataActor.GetAllActorIds();
        
        // actorType을 소문자로 변환하여 검색
        string lowerActorType = actorType.ToLower();
        
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] == lowerActorType)
            {
                return i + 1; // Auto가 0이므로 +1
            }
        }
        
        return 0; // 기본값 Auto
    }
    
    // Actor Dropdown 옵션 초기화
    public static List<string> GetActorDropdownOptions()
    {
        List<string> options = new List<string> { "Auto" };
        
        // STTDataActor에서 actor 목록 가져오기
        List<string> actors = STTDataActor.GetAllActorIds();
        
        foreach (string actorId in actors)
        {
            options.Add(STTDataActor.GetDisplayName(actorId));
        }
        
        return options;
    }
    
    // === UI 동기화 메서드 ===
    
    // 옵션 데이터 → UI 반영
    public void UpdateUIFromOptions(OCROptions options)
    {
        // Translation
        translationToggle.SetIsOnWithoutNotify(options.useTranslate);
        fromLangDropdown.SetValueWithoutNotify(GetLangIdxFromCode(options.originLang, true));
        toLangDropdown.SetValueWithoutNotify(GetLangIdxFromCode(options.targetLang, false));
        toLangDropdown.interactable = options.useTranslate;
        
        // Display
        showResultToggle.SetIsOnWithoutNotify(options.displayResults);
        
        // TTS
        readResultToggle.SetIsOnWithoutNotify(options.useTTS);
        actorDropdown.SetValueWithoutNotify(options.actorTypeIdx);
        readResultActorObject.SetActive(options.useTTS);
        readTranslatedResultToggle.SetIsOnWithoutNotify(options.readTranslatedResult);
        aiReadingCorrectionToggle.SetIsOnWithoutNotify(options.aiReadingCorrection);
        readTranslatedResultToggle.gameObject.SetActive(options.useTTS);
        aiReadingCorrectionToggle.gameObject.SetActive(options.useTTS);
        readTranslatedResultToggle.interactable = options.useTranslate;
        
        // Merge
        mergeTextToggle.SetIsOnWithoutNotify(options.isSentence);
        mergeSlider.SetValueWithoutNotify(options.mergeThreshold);
        mergeSliderObject.SetActive(options.isSentence);
        
        // Click
        clickResultToggle.SetIsOnWithoutNotify(options.useAutoClick);
        clickTargetDropdown.SetValueWithoutNotify(options.clickTarget);
        clickPriorityDropdown.SetValueWithoutNotify(options.clickPriority);
        clickOptionTargetObject.SetActive(options.useAutoClick);
        clickOptionMatchObject.SetActive(options.useAutoClick);
        
        // Keyword InputField
        if (options.clickWhitelist != null && options.clickWhitelist.Count > 0)
            keywordInputField.SetTextWithoutNotify(options.clickWhitelist[0]);
        else
            keywordInputField.SetTextWithoutNotify("");
    }
    
    // UI → 활성 슬롯 캐시에 반영
    private void UpdateOptionsFromUI()
    {
        OCROptions options = GetCurrentOptions();
        
        // Translation
        options.useTranslate = translationToggle.isOn;
        options.originLang = GetLangCodeFromIdx(fromLangDropdown.value, true);
        options.targetLang = GetLangCodeFromIdx(toLangDropdown.value, false);
        
        // Display
        options.displayResults = showResultToggle.isOn;
        
        // TTS
        options.useTTS = readResultToggle.isOn;
        options.actorTypeIdx = actorDropdown.value;
        options.actorType = GetActorTypeFromIdx(options.actorTypeIdx);
        options.readTranslatedResult = readTranslatedResultToggle.isOn;
        options.aiReadingCorrection = aiReadingCorrectionToggle.isOn;
        
        // Merge
        options.isSentence = mergeTextToggle.isOn;
        options.mergeThreshold = (int)mergeSlider.value;
        
        // Click
        options.useAutoClick = clickResultToggle.isOn;
        options.clickTarget = clickTargetDropdown.value;
        options.clickPriority = clickPriorityDropdown.value;
        
        // Keyword InputField → clickWhitelist
        string keyword = keywordInputField.text;
        options.clickWhitelist.Clear();
        if (!string.IsNullOrEmpty(keyword))
        {
            options.clickWhitelist.Add(keyword);
        }
    }
    
    // === UI 이벤트 핸들러 ===
    
    // Translation Toggle 변경 시
    public void OnTranslationToggleChanged(bool value)
    {
        toLangDropdown.interactable = value;
        readTranslatedResultToggle.interactable = value;
        SaveCurrentOptions();
    }
    
    // MergeText Toggle 변경 시
    public void OnMergeTextToggleChanged(bool value)
    {
        mergeSliderObject.SetActive(value);
        SaveCurrentOptions();
    }
    
    // ReadResult Toggle 변경 시
    public void OnReadResultToggleChanged(bool value)
    {
        readResultActorObject.SetActive(value);
        readTranslatedResultToggle.gameObject.SetActive(value);
        aiReadingCorrectionToggle.gameObject.SetActive(value);
        SaveCurrentOptions();
    }
    
    // ClickResult Toggle 변경 시
    public void OnClickResultToggleChanged(bool value)
    {
        clickOptionTargetObject.SetActive(value);
        clickOptionMatchObject.SetActive(value);
        SaveCurrentOptions();
    }
    
    // 모든 UI 변경 시 호출 (UI에서 연결)
    public void SaveCurrentOptions()
    {
        if (!isInitialized)
        {
            Debug.Log("[OCRManager] SaveCurrentOptions ignored - Initialization not complete");
            return;
        }
        
        UpdateOptionsFromUI();
        SaveOptions(activeSlot);
        
        Debug.Log($"[OCRManager] Options saved to slot {activeSlot}");
    }
    
    // Actor Dropdown 초기화
    public void InitializeActorDropdown()
    {
        actorDropdown.ClearOptions();
        List<string> actorOptions = GetActorDropdownOptions();
        actorDropdown.AddOptions(actorOptions);
        Debug.Log($"[OCRManager] Actor Dropdown initialized with {actorOptions.Count} options: {string.Join(", ", actorOptions)}");
    }
    
    // Click 관련 Dropdown 초기화
    public void InitializeClickDropdowns()
    {
        // Click Target Dropdown 초기화
        clickTargetDropdown.ClearOptions();
        List<string> targetOptions = new List<string> { "All", "First" };
        clickTargetDropdown.AddOptions(targetOptions);
        Debug.Log("[OCRManager] Click Target Dropdown initialized: All, First");
        
        // Click Priority Dropdown 초기화
        clickPriorityDropdown.ClearOptions();
        List<string> priorityOptions = new List<string> { "TopLeft", "BottomLeft", "TopRight", "BottomRight" };
        clickPriorityDropdown.AddOptions(priorityOptions);
        Debug.Log("[OCRManager] Click Priority Dropdown initialized: TopLeft, BottomLeft, TopRight, BottomRight");
    }
    
    // === 기본값 설정 메서드 ===
    
    // 기본값 옵션 반환 (슬롯별로 다른 기본값 설정 가능)
    public static OCROptions GetDefaultOptions(int slot = 1)
    {
        switch (slot)
        {
            case 1: return GetDefaultOptionsSlot1();
            case 2: return GetDefaultOptionsSlot2();
            case 3: return GetDefaultOptionsSlot3();
            default: return GetDefaultOptionsSlot1();
        }
    }
    
    // Options1 기본값: 일반 OCR (번역 없음)
    private static OCROptions GetDefaultOptionsSlot1()
    {
        return new OCROptions
        {
            // 번역
            useTranslate = false,
            targetLang = "ko",
            originLang = "auto",
            isFormality = true,
            
            // OCR 설정
            isSentence = true,
            mergeThreshold = 0,
            targetLangAutoDetect = false,
            
            // 표시
            displayResults = true,
            displayOrigin = false,
            
            // TTS
            useTTS = false,
            actorTypeIdx = 0,
            actorType = "Auto",
            ttsAutoDetectLang = false,
            readTranslatedResult = false,
            aiReadingCorrection = false,
            
            // 클릭
            useAutoClick = false,
            clickTarget = 0,
            clickPriority = 0,
            clickExactMatch = true,
            clickWhitelist = new List<string>(),
            clickBlacklist = new List<string>(),
            
            // 디버그/로깅
            saveResult = true,
            saveImage = true,
            isDebug = true
        };
    }
    
    // Options2 기본값: OCR + 번역
    private static OCROptions GetDefaultOptionsSlot2()
    {
        return new OCROptions
        {
            // 번역
            useTranslate = true,
            targetLang = "ko",
            originLang = "ja",
            isFormality = true,
            
            // OCR 설정
            isSentence = true,
            mergeThreshold = 0,
            targetLangAutoDetect = false,
            
            // 표시
            displayResults = true,
            displayOrigin = false,
            
            // TTS
            useTTS = false,
            actorTypeIdx = 0,
            actorType = "Auto",
            ttsAutoDetectLang = false,
            readTranslatedResult = false,
            aiReadingCorrection = false,
            
            // 클릭
            useAutoClick = false,
            clickTarget = 0,
            clickPriority = 0,
            clickExactMatch = true,
            clickWhitelist = new List<string>(),
            clickBlacklist = new List<string>(),
            
            // 디버그/로깅
            saveResult = true,
            saveImage = true,
            isDebug = true
        };
    }
    
    // Options3 기본값: OCR + TTS (번역 없음)
    private static OCROptions GetDefaultOptionsSlot3()
    {
        return new OCROptions
        {
            // 번역
            useTranslate = false,
            targetLang = "ja",
            originLang = "ja",
            isFormality = true,
            
            // OCR 설정
            isSentence = true,
            mergeThreshold = 0,
            targetLangAutoDetect = false,
            
            // 표시
            displayResults = false,
            displayOrigin = false,
            
            // TTS
            useTTS = true,
            actorTypeIdx = 0,
            actorType = "Auto",
            ttsAutoDetectLang = false,
            readTranslatedResult = false,
            aiReadingCorrection = false,
            
            // 클릭
            useAutoClick = false,
            clickTarget = 0,
            clickPriority = 0,
            clickExactMatch = true,
            clickWhitelist = new List<string>(),
            clickBlacklist = new List<string>(),
            
            // 디버그/로깅
            saveResult = true,
            saveImage = true,
            isDebug = true
        };
    }
    
    // 슬롯별 기본값으로 초기화
    public void SetDefaultValues(int slot)
    {
        Debug.Log($"[OCRManager] Setting default values for slot {slot}");
        
        OCROptions defaultOptions = GetDefaultOptions(slot);
        
        // 캐시에 기본값 설정
        switch (slot)
        {
            case 1: options1 = defaultOptions; break;
            case 2: options2 = defaultOptions; break;
            case 3: options3 = defaultOptions; break;
        }
        
        // 파일 저장
        SaveOptions(slot);
        
        // 현재 활성 슬롯이면 UI도 갱신
        if (slot == activeSlot)
        {
            UpdateUIFromOptions(defaultOptions);
        }
    }
    
    // 모든 슬롯 초기화 (각 슬롯별 기본값으로)
    public void ResetAllSlots()
    {
        for (int i = 1; i <= 3; i++)
        {
            SetDefaultValues(i);
        }
        
        Debug.Log("[OCRManager] All slots reset to their default values");
    }
}