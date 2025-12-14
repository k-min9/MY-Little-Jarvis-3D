using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
    public bool detectActor;                // Actor 감지 여부 (STTDataActor 사용)
    public bool ttsAutoDetectLang;          // TTS 언어 자동 감지
    
    // 클릭 옵션
    public bool useAutoClick;               // 자동 클릭 여부
    public bool clickExactMatch;            // true=정확히 일치, false=포함
    public List<string> clickWhitelist;     // 화이트리스트 (비어있으면 모두 허용)
    public List<string> clickBlacklist;     // 블랙리스트
    
    // 기본 생성자 (List 초기화)
    public OCROptions()
    {
        clickWhitelist = new List<string>();
        clickBlacklist = new List<string>();
    }
}

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
                GameObject go = new GameObject("OCRManager");
                instance = go.AddComponent<OCRManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // 현재 활성 슬롯 (1~3)
    private int activeSlot = 1;
    
    // 현재 활성 옵션 (캐시)
    private OCROptions currentOptions;
    
    // 옵션 파일 경로들
    private string configDirectory;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 설정 디렉토리 경로
            configDirectory = Path.Combine(Application.persistentDataPath, "config");
            
            // 디렉토리 생성
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }
            
            // 현재 옵션 로드
            currentOptions = LoadOptions(activeSlot);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // 옵션 파일 경로 가져오기
    private string GetOptionsFilePath(int slot)
    {
        if (slot < 1 || slot > 3)
        {
            Debug.LogError($"[OCRManager] Invalid slot number: {slot}. Must be 1-3.");
            slot = 1;
        }
        
        return Path.Combine(configDirectory, $"ocr_options{slot}.json");
    }
    
    // 옵션 로드
    public OCROptions LoadOptions(int slot)
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
                
                Debug.Log($"[OCRManager] Loaded options from slot {slot}");
                return options;
            }
            else
            {
                Debug.Log($"[OCRManager] No saved options for slot {slot}, using defaults");
                OCROptions defaultOptions = GetDefaultOptions();
                SaveOptions(slot, defaultOptions); // 기본값 저장
                return defaultOptions;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[OCRManager] Failed to load options from slot {slot}: {e.Message}");
            return GetDefaultOptions();
        }
    }
    
    // 옵션 저장
    public void SaveOptions(int slot, OCROptions options)
    {
        string filePath = GetOptionsFilePath(slot);
        
        try
        {
            string json = JsonUtility.ToJson(options, true);
            File.WriteAllText(filePath, json);
            
            Debug.Log($"[OCRManager] Saved options to slot {slot}");
            
            // 현재 활성 슬롯이면 캐시 업데이트
            if (slot == activeSlot)
            {
                currentOptions = options;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[OCRManager] Failed to save options to slot {slot}: {e.Message}");
        }
    }
    
    // 현재 활성 옵션 가져오기
    public OCROptions GetCurrentOptions()
    {
        if (currentOptions == null)
        {
            currentOptions = LoadOptions(activeSlot);
        }
        return currentOptions;
    }
    
    // 활성 슬롯 변경
    public void SetActiveSlot(int slot)
    {
        if (slot < 1 || slot > 3)
        {
            Debug.LogError($"[OCRManager] Invalid slot number: {slot}. Must be 1-3.");
            return;
        }
        
        activeSlot = slot;
        currentOptions = LoadOptions(slot);
        
        Debug.Log($"[OCRManager] Active slot changed to {slot}");
    }
    
    // 현재 활성 슬롯 번호 가져오기
    public int GetActiveSlot()
    {
        return activeSlot;
    }
    
    // 기본값 옵션 반환
    public static OCROptions GetDefaultOptions()
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
            mergeThreshold = -1,  // 서버 기본값 사용
            targetLangAutoDetect = false,
            
            // 표시
            displayResults = true,
            displayOrigin = false,
            
            // TTS
            useTTS = false,
            detectActor = false,
            ttsAutoDetectLang = false,
            
            // 클릭
            useAutoClick = false,
            clickExactMatch = true,
            clickWhitelist = new List<string>(),
            clickBlacklist = new List<string>()
        };
    }
    
    // 모든 슬롯 초기화 (기본값으로)
    public void ResetAllSlots()
    {
        for (int i = 1; i <= 3; i++)
        {
            SaveOptions(i, GetDefaultOptions());
        }
        
        currentOptions = LoadOptions(activeSlot);
        Debug.Log("[OCRManager] All slots reset to default values");
    }
}

