using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using DevionGames.UIWidgets;
using TMPro;

// OCROptions 기반 리팩토링된 OCR 관리자
public class ScreenshotOCRManager : MonoBehaviour
{
    // Windows API 선언 (GetSystemMetrics만 필요)
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;  // 주 모니터 너비
    private const int SM_CYSCREEN = 1;  // 주 모니터 높이

    // 싱글톤 인스턴스
    private static ScreenshotOCRManager instance;

    // OCR 결과 표시용 UI 오브젝트들
    private GameObject ocrOverlayContainer;
    private Coroutine destroyCoroutine;

    // 화이트/블랙리스트 전역 기본값
    public List<string> defaultWhitelist = new List<string>();
    public List<string> defaultBlacklist = new List<string>();

    // API 호출 제어 변수
    private bool isProcessing = false;
    private float lastCallTime = -999f;
    private const float COOLDOWN_TIME = 5f;

    // 싱글톤 인스턴스 접근
    public static ScreenshotOCRManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ScreenshotOCRManager");
                instance = go.AddComponent<ScreenshotOCRManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // === Entry Points ===

    // 전체화면 OCR 실행 (OCROptions 기반)
    public void ExecuteFullScreenOCR(OCROptions options)
    {
        StartCoroutine(ExecuteFullScreenOCRCoroutine(options));
    }

    // 영역 OCR 실행 (OCROptions 기반)
    public void ExecuteAreaOCR(OCROptions options)
    {
        StartCoroutine(ExecuteAreaOCRCoroutine(options));
    }

    // === 전체화면 OCR 코루틴 ===
    
    private IEnumerator ExecuteFullScreenOCRCoroutine(OCROptions options)
    {
        Debug.Log($"[ScreenshotOCR_new] Starting full screen OCR (useTranslate: {options.useTranslate})");

        // 기존 오버레이 제거
        ClearOCROverlay();

        // ScreenshotManager 찾기
        ScreenshotManager screenshotManager = FindObjectOfType<ScreenshotManager>();
        if (screenshotManager == null)
        {
            Debug.LogError("[ScreenshotOCR_new] ScreenshotManager not found");
            yield break;
        }

        // 전체화면 캡처 (ScreenshotManager의 Hybrid 방식 사용)
        byte[] screenshotBytes = null;
        bool captureCompleted = false;

        yield return screenshotManager.CaptureFullScreenToMemory((bytes) =>
        {
            screenshotBytes = bytes;
            captureCompleted = true;
        });

        while (!captureCompleted)
        {
            yield return null;
        }

        if (screenshotBytes == null || screenshotBytes.Length == 0)
        {
            Debug.LogError("[ScreenshotOCR_new] Failed to capture full screen");
            yield break;
        }

        // OCR API 호출
        int captureWidth = GetSystemMetrics(SM_CXSCREEN);
        int captureHeight = GetSystemMetrics(SM_CYSCREEN);

        yield return CallOCRAPICoroutine(screenshotBytes, options, captureWidth, captureHeight, 0, 0);
    }

    // === 영역 OCR 코루틴 ===
    
    private IEnumerator ExecuteAreaOCRCoroutine(OCROptions options)
    {
        Debug.Log($"[ScreenshotOCR_new] Starting area OCR (useTranslate: {options.useTranslate})");

        // ScreenshotManager 찾기
        ScreenshotManager screenshotManager = FindObjectOfType<ScreenshotManager>();
        if (screenshotManager == null)
        {
            Debug.LogError("[ScreenshotOCR_new] ScreenshotManager not found");
            yield break;
        }

        ClearOCROverlay();

        // 영역 캡처
        byte[] areaBytes = null;
        int captureX = 0, captureY = 0, captureWidth = 0, captureHeight = 0;

        bool captureCompleted = false;
        yield return screenshotManager.CaptureScreenshotToMemoryWithInfo((bytes, x, y, w, h) =>
        {
            areaBytes = bytes;
            captureX = x;
            captureY = y;
            captureWidth = w;
            captureHeight = h;
            captureCompleted = true;
        });

        while (!captureCompleted)
        {
            yield return null;
        }

        if (areaBytes == null || areaBytes.Length == 0)
        {
            Debug.LogError("[ScreenshotOCR_new] Failed to capture area");
            yield break;
        }

        Debug.Log($"[ScreenshotOCR_new] Area captured: {areaBytes.Length} bytes, position: ({captureX}, {captureY}), size: {captureWidth}x{captureHeight}");

        // OCR API 호출
        yield return CallOCRAPICoroutine(areaBytes, options, captureWidth, captureHeight, captureX, captureY);
    }

    // === OCR API 호출 (번역 여부에 따라 분기) ===
    
    private IEnumerator CallOCRAPICoroutine(byte[] imageBytes, OCROptions options, int captureWidth, int captureHeight, int offsetX, int offsetY)
    {
        // 중복 호출 체크
        if (isProcessing)
        {
            Debug.LogWarning("[ScreenshotOCR_new] OCR is already in progress. Request ignored.");
            yield break;
        }

        // 쿨타임 체크
        float timeSinceLastCall = Time.time - lastCallTime;
        if (timeSinceLastCall < COOLDOWN_TIME)
        {
            float remainingTime = COOLDOWN_TIME - timeSinceLastCall;
            Debug.LogWarning($"[ScreenshotOCR_new] OCR cooldown active. Please wait {remainingTime:F1} more seconds.");
            yield break;
        }

        // 처리 시작
        isProcessing = true;
        lastCallTime = Time.time;

        // OCR 작업 시작 시 "Search" 말풍선 표시 (30초)
        GameObject currentChar = CharManager.Instance.GetCurrentCharacter();
        EmotionBalloonManager.Instance.SetEmotionBalloonForTarget(currentChar, "Search", 30f);

        bool apiCallCompleted = false;
        OCRResult result = null;

        // OCR API 호출 (use_translate 파라미터로 번역 여부 제어)
        APIManager.Instance.CallPaddleOCR(
            imageBytes,
            targetLang: options.targetLang,
            autoDetect: options.originLang == "auto",
            useTranslate: options.useTranslate,
            originLang: options.originLang,
            isFormality: options.isFormality,
            isSentence: options.isSentence,
            mergeThreshold: options.mergeThreshold,
            saveResult: options.saveResult,
            saveImage: options.saveImage,
            isDebug: options.isDebug,
            callback: (ocrResult) =>
            {
                result = ocrResult;
                apiCallCompleted = true;
            }
        );

        // API 호출 완료 대기
        while (!apiCallCompleted)
        {
            yield return null;
        }

        // API 호출 완료 시 말풍선 제거
        EmotionBalloonManager.Instance.RemoveEmotionBalloonForTarget(currentChar);
        Debug.Log("[ScreenshotOCR_new] Search balloon removed (API completed)");

        if (result == null)
        {
            Debug.LogError("[ScreenshotOCR_new] OCR API call failed");
            ShowNoResultUI();
            isProcessing = false;
            yield break;
        }

        // 영역 OCR의 경우 좌표 보정 필요
        bool isAdjusted = false;
        if (offsetX != 0 || offsetY != 0)
        {
            AdjustOCRResultCoords(result, offsetX, offsetY);
            isAdjusted = true;
        }

        // 결과 처리
        ProcessOCRResult(result, options, captureWidth, captureHeight, isAdjusted);

        // 처리 완료
        isProcessing = false;
    }

    // === 결과 처리 (표시/TTS/클릭 분기) ===
    
    private void ProcessOCRResult(OCRResult result, OCROptions options, int captureWidth, int captureHeight, bool isAdjusted = false)
    {
        if (result == null || result.labels == null || result.labels.Count == 0)
        {
            Debug.LogWarning("[ScreenshotOCR_new] No OCR results to process");
            if (options.displayResults)
            {
                ShowNoResultUI();
            }
            return;
        }

        Debug.Log($"[ScreenshotOCR_new] Processing {result.labels.Count} OCR results (isAdjusted: {isAdjusted})");

        // 1. UI 표시
        if (options.displayResults)
        {
            DisplayOCRResults(result, captureWidth, captureHeight, isAdjusted);
            
            // 원문도 표시 (번역 사용 + displayOrigin 활성화)
            if (options.displayOrigin && options.useTranslate && result.labels_origin != null && result.labels_origin.Count > 0)
            {
                DisplayOCROriginResults(result, captureWidth, captureHeight, isAdjusted);
            }
        }

        // 2. TTS 실행
        if (options.useTTS)
        {
            ExecuteTTS(result, options);
        }

        // 3. 자동 클릭
        if (options.useAutoClick)
        {
            ExecuteAutoClick(result, options, captureWidth, captureHeight);
        }
    }

    // === TTS 실행 ===
    
    private void ExecuteTTS(OCRResult result, OCROptions options)
    {
        Debug.Log($"[ScreenshotOCR→TTS] Starting TTS (readTranslatedResult: {options.readTranslatedResult}, aiCorrection: {options.aiReadingCorrection})");
        
        // 언어 결정
        string targetLang = options.readTranslatedResult ? options.targetLang : options.originLang;
        bool isJapanese = (targetLang == "ja");
        
        // 텍스트 소스 선택
        List<string> textLabels;
        List<List<List<float>>> textBoxes;
        
        if (options.readTranslatedResult && options.useTranslate)
        {
            // 번역된 텍스트 사용
            textLabels = result.labels;
            textBoxes = result.quad_boxes;
            Debug.Log($"[ScreenshotOCR→TTS] Using translated text (ToLang: {options.targetLang})");
        }
        else
        {
            // 원문 텍스트 사용
            textLabels = (result.labels_origin != null && result.labels_origin.Count > 0) 
                ? result.labels_origin 
                : result.labels;
            textBoxes = (result.quad_boxes_origin != null && result.quad_boxes_origin.Count > 0)
                ? result.quad_boxes_origin
                : result.quad_boxes;
            Debug.Log($"[ScreenshotOCR→TTS] Using original text (FromLang: {options.originLang})");
        }
        
        // 공통 처리: Actor 감지, 필터링, 정렬 → (옵션) Furigana 변환 → TTS
        StartCoroutine(ProcessTTSCoroutine(textLabels, textBoxes, options, isJapanese));
    }
    
    private IEnumerator ProcessTTSCoroutine(List<string> labels, List<List<List<float>>> boxes, OCROptions options, bool isJapanese)
    {
        // 1. Actor 감지, 필터링, 정렬
        string sortedText = GetSortedAndFilteredText(labels, boxes, options, out string detectedActor);
        
        if (string.IsNullOrWhiteSpace(sortedText))
        {
            Debug.LogWarning("[ScreenshotOCR→TTS] No text to process after filtering");
            yield break;
        }
        
        Debug.Log($"[ScreenshotOCR→TTS] Sorted text: '{sortedText}'");
        
        // 2. Furigana 변환 (필요한 경우에만)
        string finalText = sortedText;
        if (options.aiReadingCorrection && isJapanese)
        {
            Debug.Log($"[ScreenshotOCR→TTS] Requesting Furigana conversion...");
            
            bool furiganaCompleted = false;
            string furiganaText = null;
            
            APIManager.Instance.CallFuriganaAPI(sortedText, (result) =>
            {
                furiganaText = result;
                furiganaCompleted = true;
            });
            
            yield return new WaitUntil(() => furiganaCompleted);
            
            if (!string.IsNullOrEmpty(furiganaText))
            {
                Debug.Log($"[ScreenshotOCR→TTS] Furigana converted: {furiganaText}");
                finalText = furiganaText;
            }
            else
            {
                Debug.LogWarning("[ScreenshotOCR→TTS] Furigana conversion failed, using original text");
            }
        }
        
        // 3. TTS 호출
        CallTTSWithText(finalText, detectedActor, options);
    }
    
    // Actor 감지, 필터링, 정렬된 텍스트 반환
    private string GetSortedAndFilteredText(List<string> labels, List<List<List<float>>> boxes, OCROptions options, out string detectedActor)
    {
        // Actor 결정
        detectedActor = "arona"; // 기본값
        if (options.actorType == "Auto")
        {
            // 자동 감지 모드
            foreach (string label in labels)
            {
                string actor = STTDataActor.GetActorFromText(label);
                if (!string.IsNullOrEmpty(actor))
                {
                    detectedActor = actor;
                    Debug.Log($"[ScreenshotOCR→TTS] Actor auto-detected: {detectedActor}");
                    break;
                }
            }
        }
        else
        {
            // 수동 지정 모드
            detectedActor = options.actorType.ToLower();
            Debug.Log($"[ScreenshotOCR→TTS] Actor manually set: {detectedActor}");
        }

        // === TTS용 텍스트 필터링 및 정렬 (검토용: false로 변경 시 원본 텍스트 사용) ===
        if (true)
        // if (options.useTTS) // TODO : 테스트용
        {
            // 텍스트 필터링 및 정렬
            List<(string text, float x, float y)> textItems = new List<(string, float, float)>();
            string detectedActorText = STTDataActor.GetActorTextFromActorId(detectedActor);

            for (int i = 0; i < labels.Count; i++)
            {
                string text = labels[i];
                
                // 길이 필터 (3자 미만 제외)
                if (text.Length < 3) continue;
                
                // 블랙리스트 필터
                if (STTDataActor.IsBlacklisted(text)) continue;
                
                // Actor 텍스트 제외
                if (!string.IsNullOrEmpty(detectedActorText) && text.Contains(detectedActorText)) continue;

                if (boxes != null && i < boxes.Count)
                {
                    float x = boxes[i][0][0];
                    float y = boxes[i][0][1];
                    textItems.Add((text, x, y));
                }
            }

            // Y 좌표 정렬
            textItems.Sort((a, b) => a.y.CompareTo(b.y));

            // Y 임계값(30px)으로 줄 그룹핑 및 X 정렬
            List<List<(string text, float x, float y)>> lines = new List<List<(string, float, float)>>();
            List<(string text, float x, float y)> currentLine = new List<(string, float, float)>();
            const float yThreshold = 30f;

            foreach (var item in textItems)
            {
                if (currentLine.Count == 0)
                {
                    currentLine.Add(item);
                }
                else
                {
                    float avgY = currentLine.Average(t => t.y);
                    if (Mathf.Abs(item.y - avgY) <= yThreshold)
                    {
                        currentLine.Add(item);
                    }
                    else
                    {
                        lines.Add(currentLine);
                        currentLine = new List<(string, float, float)> { item };
                    }
                }
            }
            
            if (currentLine.Count > 0)
            {
                lines.Add(currentLine);
            }

            // 각 줄 내에서 X 좌표 정렬
            List<string> sortedTexts = new List<string>();
            foreach (var line in lines)
            {
                var sortedLine = line.OrderBy(item => item.x).ToList();
                foreach (var item in sortedLine)
                {
                    sortedTexts.Add(item.text);
                }
            }

            string finalText = string.Join(" ", sortedTexts);
            Debug.Log($"[ScreenshotOCR→TTS] Sorted and filtered text: '{finalText}'");
            
            return finalText;
        }
        else
        {
            // 필터링/정렬 없이 원본 텍스트 그대로 사용
            string rawText = string.Join(" ", labels);
            Debug.Log($"[ScreenshotOCR→TTS] Raw text (no filtering): '{rawText}'");
            return rawText;
        }
    }
    
    // 최종 텍스트로 TTS 호출
    private void CallTTSWithText(string finalText, string detectedActor, OCROptions options)
    {
        if (string.IsNullOrWhiteSpace(finalText))
        {
            Debug.LogWarning("[ScreenshotOCR→TTS] No text to speak");
            return;
        }

        // dev_voice 활성화
        if (SettingManager.Instance != null && SettingManager.Instance.settings != null)
        {
            SettingManager.Instance.settings.isDevSound = true;
        }

        Debug.Log($"[ScreenshotOCR→TTS] Final text for TTS: '{finalText}'");

        // TTS 호출
        string chatIdx = GameManager.Instance.chatIdxSuccess;
        
        if (options.ttsAutoDetectLang)
        {
            // 자동 감지 모드
            string soundLang = "en";
            try
            {
                soundLang = SettingManager.Instance.settings.sound_language ?? "en";
            }
            catch { }

            if (soundLang == "ko" || soundLang == "en")
            {
                APIManager.Instance.GetKoWavFromAPI(finalText, chatIdx, detectedActor);
            }
            else if (soundLang == "jp" || soundLang == "ja")
            {
                APIManager.Instance.GetJpWavFromAPI(finalText, chatIdx, detectedActor);
            }
            else
            {
                APIManager.Instance.GetJpWavFromAPI(finalText, chatIdx, detectedActor);
            }
        }
        else
        {
            // 일본어 기본값
            APIManager.Instance.GetJpWavFromAPI(finalText, chatIdx, detectedActor);
        }
    }

    // === 자동 클릭 실행 ===
    
    private void ExecuteAutoClick(OCRResult result, OCROptions options, int captureWidth, int captureHeight)
    {
        Debug.Log($"[ScreenshotOCR_new→Click] Starting auto-click search");

        if (result == null || result.labels == null || result.labels.Count == 0)
        {
            Debug.LogWarning("[ScreenshotOCR_new→Click] No OCR results to search");
            return;
        }

        // 화이트/블랙리스트 준비
        List<string> whitelist = (options.clickWhitelist != null && options.clickWhitelist.Count > 0) 
            ? options.clickWhitelist : defaultWhitelist;
        List<string> blacklist = (options.clickBlacklist != null && options.clickBlacklist.Count > 0) 
            ? options.clickBlacklist : defaultBlacklist;

        // 텍스트 매칭
        for (int i = 0; i < result.labels.Count; i++)
        {
            string label = result.labels[i];

            // 텍스트 필터링
            if (!IsTextAllowed(label, whitelist, blacklist, options.clickExactMatch))
            {
                continue;
            }

            Debug.Log($"[ScreenshotOCR_new→Click] Found allowed text: '{label}' at index {i}");

            if (result.quad_boxes == null || i >= result.quad_boxes.Count)
            {
                Debug.LogError($"[ScreenshotOCR_new→Click] No quad_box data for index {i}");
                continue;
            }

            // quad_box 중심 좌표 계산
            List<List<float>> quadBox = result.quad_boxes[i];
            if (quadBox == null || quadBox.Count < 4)
            {
                continue;
            }

            float sumX = 0f, sumY = 0f;
            int pointCount = 0;
            foreach (var point in quadBox)
            {
                if (point.Count >= 2)
                {
                    sumX += point[0];
                    sumY += point[1];
                    pointCount++;
                }
            }

            if (pointCount == 0) continue;

            float centerX_OCR = sumX / pointCount;
            float centerY_OCR = sumY / pointCount;

            // 좌표 변환 (OCR → Win32 Screen)
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            float scaleX = (float)screenWidth / captureWidth;
            float scaleY = (float)screenHeight / captureHeight;

            int clickX = (int)(centerX_OCR * scaleX);
            int clickY = (int)(centerY_OCR * scaleY);

            Debug.Log($"[ScreenshotOCR_new→Click] Clicking at ({clickX}, {clickY}) for text '{label}'");

            // 클릭 실행
            if (ExecutorMouseAction.Instance != null)
            {
                ExecutorMouseAction.Instance.ClickAtPosition(clickX, clickY);
            }
            else
            {
                Debug.LogError("[ScreenshotOCR_new→Click] ExecutorMouseAction instance not found");
            }

            return; // 첫 번째 매칭만 클릭
        }

        Debug.LogWarning("[ScreenshotOCR_new→Click] No matching text found for auto-click");
    }

    // === 화이트/블랙리스트 필터링 ===
    
    private bool IsTextAllowed(string text, List<string> whitelist, List<string> blacklist, bool exactMatch)
    {
        // 블랙리스트 체크
        foreach (string blocked in blacklist)
        {
            if (text.Contains(blocked))
            {
                Debug.Log($"[ScreenshotOCR_new→Filter] Blocked by blacklist: '{text}' contains '{blocked}'");
                return false;
            }
        }

        // 화이트리스트 체크 (비어있으면 모두 허용)
        if (whitelist.Count == 0)
        {
            return true;
        }

        foreach (string allowed in whitelist)
        {
            bool isMatch = exactMatch ? (text == allowed) : text.Contains(allowed);
            if (isMatch)
            {
                Debug.Log($"[ScreenshotOCR_new→Filter] Allowed by whitelist: '{text}' matches '{allowed}'");
                return true;
            }
        }

        Debug.Log($"[ScreenshotOCR_new→Filter] Not in whitelist: '{text}'");
        return false;
    }

    // === 좌표 보정 ===
    
    private void AdjustOCRResultCoords(OCRResult result, int offsetX, int offsetY)
    {
        if (result == null || result.quad_boxes == null) return;

        for (int i = 0; i < result.quad_boxes.Count; i++)
        {
            for (int j = 0; j < result.quad_boxes[i].Count; j++)
            {
                if (result.quad_boxes[i][j].Count >= 2)
                {
                    result.quad_boxes[i][j][0] += offsetX;
                    result.quad_boxes[i][j][1] += offsetY;
                }
            }
        }

        // quad_boxes_origin도 보정
        if (result.quad_boxes_origin != null)
        {
            for (int i = 0; i < result.quad_boxes_origin.Count; i++)
            {
                for (int j = 0; j < result.quad_boxes_origin[i].Count; j++)
                {
                    if (result.quad_boxes_origin[i][j].Count >= 2)
                    {
                        result.quad_boxes_origin[i][j][0] += offsetX;
                        result.quad_boxes_origin[i][j][1] += offsetY;
                    }
                }
            }
        }

        Debug.Log($"[ScreenshotOCR_new] Adjusted {result.quad_boxes.Count} boxes with offset ({offsetX}, {offsetY})");
    }

    // === UI 표시 (OCR 결과) ===
    
    // === UI 표시 (OCR 결과 - WindowCollisionUI 방식) ===
    
    private void DisplayOCRResults(OCRResult result, int captureWidth, int captureHeight, bool isAdjusted = false)
    {
        if (result == null || result.labels == null || result.labels.Count == 0)
        {
            return;
        }

        Canvas canvas = WindowCollisionManager.Instance.canvas;
        if (canvas == null)
        {
            Debug.LogError("[ScreenshotOCR] Canvas not found in WindowCollisionManager");
            return;
        }

        ocrOverlayContainer = new GameObject("OCROverlayContainer_New");
        ocrOverlayContainer.transform.SetParent(canvas.transform, false);

        Debug.Log($"[ScreenshotOCR] Displaying {result.labels.Count} OCR results (isAdjusted: {isAdjusted})");

        for (int i = 0; i < result.labels.Count; i++)
        {
            string text = result.labels[i];
            
            if (result.quad_boxes != null && i < result.quad_boxes.Count)
            {
                CreateOCRTextUI(text, result.quad_boxes[i], canvas, captureWidth, captureHeight, isAdjusted);
            }
        }

        // 10초 후 자동 제거
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
        }
        destroyCoroutine = StartCoroutine(DestroyOverlayAfterDelay(10f));
    }
    
    // === 개별 텍스트 UI 생성 (WindowCollisionUI 방식) ===
    
    private void CreateOCRTextUI(string text, List<List<float>> quadBox, Canvas canvas, int captureWidth, int captureHeight, bool isAdjusted = false)
    {
        if (quadBox == null || quadBox.Count < 4) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // OCR 좌표의 4개 점을 Canvas 좌표로 변환
        List<Vector2> convertedPoints = new List<Vector2>();
        
        foreach (var point in quadBox)
        {
            if (point.Count >= 2)
            {
                float ocrX = point[0];
                float ocrY = point[1];
                
                float unityScreenX, unityScreenY;
                
                if (isAdjusted)
                {
                    // 이미 전체 화면 기준 절대 좌표 (offsetX/Y가 더해진 상태)
                    unityScreenX = ocrX;
                    unityScreenY = screenHeight - ocrY;  // Y축만 반전
                    Debug.Log($"[OCR-Adjusted] Point: OCR({ocrX},{ocrY}) → Screen({unityScreenX},{unityScreenY}) (no scaling)");
                }
                else
                {
                    // 캡처 이미지 내 상대 좌표 → 전체 화면 좌표로 스케일링
                    float scaleX = screenWidth / captureWidth;
                    float scaleY = screenHeight / captureHeight;
                    unityScreenX = ocrX * scaleX;
                    unityScreenY = screenHeight - (ocrY * scaleY);
                    Debug.Log($"[OCR-Relative] Point: OCR({ocrX},{ocrY}) → Screen({unityScreenX},{unityScreenY}) (scaled)");
                }
                
                Vector2 unityScreenPos = new Vector2(unityScreenX, unityScreenY);
                
                // Unity Screen → Canvas 로컬 좌표 (WindowCollisionUI와 동일)
                Vector2 canvasLocalPos = WindowCollisionManager.Instance.ConvertWinposToUnityPos(unityScreenPos);
                convertedPoints.Add(canvasLocalPos);
                
                Debug.Log($"[OCR] Canvas({canvasLocalPos.x},{canvasLocalPos.y})");
            }
        }
        
        if (convertedPoints.Count < 4) return;

        // WindowCollisionUI 방식: 좌상단/우하단 구하기
        Vector2 start = convertedPoints[0];  // 좌상단
        Vector2 end = convertedPoints[2];    // 우하단 (quad는 보통 [좌상, 우상, 우하, 좌하] 순서)
        
        // 혹시 순서가 다를 수 있으니 min/max로 안전하게
        float minX = convertedPoints.Min(p => p.x);
        float minY = convertedPoints.Min(p => p.y);
        float maxX = convertedPoints.Max(p => p.x);
        float maxY = convertedPoints.Max(p => p.y);
        
        start = new Vector2(minX, minY);
        end = new Vector2(maxX, maxY);

        // WindowCollisionUI 방식: 중앙점과 크기 계산
        Vector2 center = (start + end) / 2f;
        float width = Mathf.Abs(end.x - start.x);
        float height = Mathf.Abs(end.y - start.y);

        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning($"[OCR] Invalid size: width={width}, height={height}");
            return;
        }

        Debug.Log($"[OCR] Box: center({center.x},{center.y}), size({width},{height})");

        GameObject textObj = new GameObject($"OCRText_{text.Substring(0, Math.Min(10, text.Length))}");
        textObj.transform.SetParent(ocrOverlayContainer.transform, false);

        // 배경 패널
        Image bgImage = textObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f); // 검은색 반투명
        bgImage.raycastTarget = false;

        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        // WindowCollisionUI 방식: Anchor/Pivot 기본값 사용 (center)
        rectTransform.localPosition = center;  // ← 중앙점!
        rectTransform.sizeDelta = new Vector2(width, height);

        // 텍스트
        GameObject textChild = new GameObject("Text");
        textChild.transform.SetParent(textObj.transform, false);
        
        TextMeshProUGUI textComponent = textChild.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = null; // TMPro 기본 폰트
        textComponent.color = Color.yellow;
        textComponent.alignment = TextAlignmentOptions.MidlineLeft;  // 좌측 정렬

        textComponent.enableAutoSizing = true;
        textComponent.fontSizeMin = 8;
        textComponent.fontSizeMax = 100;  // 최대 크기 증가

        // 박스가 작으면 패딩 0, 아니면 1
        float marginH = width > 2 ? 1f : 0f;
        float marginV = height > 2 ? 1f : 0f;
        textComponent.margin = new Vector4(marginH, marginV, marginH, marginV);

        RectTransform textRect = textChild.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
    }

    // === UI 표시 (원문 결과 - WindowCollisionUI 방식) ===
    
    private void DisplayOCROriginResults(OCRResult result, int captureWidth, int captureHeight, bool isAdjusted = false)
    {
        if (result == null || result.labels_origin == null || result.labels_origin.Count == 0)
        {
            return;
        }

        Canvas canvas = WindowCollisionManager.Instance.canvas;
        if (canvas == null)
        {
            Debug.LogError("[ScreenshotOCR] Canvas not found in WindowCollisionManager (Origin)");
            return;
        }

        GameObject originContainer = new GameObject("OCROverlayContainer_Origin");
        originContainer.transform.SetParent(canvas.transform, false);

        Debug.Log($"[ScreenshotOCR] Displaying {result.labels_origin.Count} origin OCR results (isAdjusted: {isAdjusted})");

        // quad_boxes_origin 사용 (없으면 일반 quad_boxes 사용)
        List<List<List<float>>> boxesToUse = (result.quad_boxes_origin != null && result.quad_boxes_origin.Count > 0)
            ? result.quad_boxes_origin : result.quad_boxes;

        for (int i = 0; i < result.labels_origin.Count; i++)
        {
            string text = result.labels_origin[i];
            
            if (boxesToUse != null && i < boxesToUse.Count)
            {
                CreateOCROriginTextUI(text, boxesToUse[i], originContainer, captureWidth, captureHeight, isAdjusted);
            }
        }

        // 10초 후 자동 제거
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
        }
        destroyCoroutine = StartCoroutine(DestroyOriginOverlayAfterDelay(originContainer, 10f));
    }
    
    // === 개별 원문 텍스트 UI 생성 (WindowCollisionUI 방식) ===
    
    private void CreateOCROriginTextUI(string text, List<List<float>> quadBox, GameObject parentContainer, int captureWidth, int captureHeight, bool isAdjusted = false)
    {
        if (quadBox == null || quadBox.Count < 4) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // OCR 좌표의 4개 점을 Canvas 좌표로 변환
        List<Vector2> convertedPoints = new List<Vector2>();
        
        foreach (var point in quadBox)
        {
            if (point.Count >= 2)
            {
                float ocrX = point[0];
                float ocrY = point[1];
                
                float unityScreenX, unityScreenY;
                
                if (isAdjusted)
                {
                    // 이미 전체 화면 기준 절대 좌표
                    unityScreenX = ocrX;
                    unityScreenY = screenHeight - ocrY;
                }
                else
                {
                    // 캡처 이미지 내 상대 좌표 → 스케일링
                    float scaleX = screenWidth / captureWidth;
                    float scaleY = screenHeight / captureHeight;
                    unityScreenX = ocrX * scaleX;
                    unityScreenY = screenHeight - (ocrY * scaleY);
                }
                
                Vector2 unityScreenPos = new Vector2(unityScreenX, unityScreenY);
                Vector2 canvasLocalPos = WindowCollisionManager.Instance.ConvertWinposToUnityPos(unityScreenPos);
                convertedPoints.Add(canvasLocalPos);
            }
        }
        
        if (convertedPoints.Count < 4) return;

        float minX = convertedPoints.Min(p => p.x);
        float minY = convertedPoints.Min(p => p.y);
        float maxX = convertedPoints.Max(p => p.x);
        float maxY = convertedPoints.Max(p => p.y);
        
        Vector2 start = new Vector2(minX, minY);
        Vector2 end = new Vector2(maxX, maxY);

        Vector2 center = (start + end) / 2f;
        float width = Mathf.Abs(end.x - start.x);
        float height = Mathf.Abs(end.y - start.y);

        if (width <= 0 || height <= 0) return;

        GameObject textObj = new GameObject($"OCROriginText_{text.Substring(0, Math.Min(10, text.Length))}");
        textObj.transform.SetParent(parentContainer.transform, false);

        // 배경 패널 (원문은 살짝 다른 색)
        Image bgImage = textObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.3f, 0.6f); // 파란색 계열
        bgImage.raycastTarget = false;

        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.localPosition = center;
        rectTransform.sizeDelta = new Vector2(width, height);

        // 텍스트
        GameObject textChild = new GameObject("Text");
        textChild.transform.SetParent(textObj.transform, false);
        
        TextMeshProUGUI textComponent = textChild.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = null;
        textComponent.color = Color.cyan;  // 원문은 cyan
        textComponent.alignment = TextAlignmentOptions.MidlineLeft;  // 좌측 정렬

        textComponent.enableAutoSizing = true;
        textComponent.fontSizeMin = 8;
        textComponent.fontSizeMax = 100;  // 최대 크기 증가

        // 박스가 작으면 패딩 0, 아니면 1
        float marginH = width > 2 ? 1f : 0f;
        float marginV = height > 2 ? 1f : 0f;
        textComponent.margin = new Vector4(marginH, marginV, marginH, marginV);

        RectTransform textRect = textChild.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
    }

    // === No Result UI ===
    
    private void ShowNoResultUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        ocrOverlayContainer = new GameObject("OCROverlayContainer_NoResult");
        ocrOverlayContainer.transform.SetParent(canvas.transform, false);

        Image image = ocrOverlayContainer.AddComponent<Image>();
        image.color = new Color(1f, 0f, 0f, 0.3f);

        RectTransform rectTransform = ocrOverlayContainer.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(600, 300);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        GameObject textObj = new GameObject("NoResultText");
        textObj.transform.SetParent(ocrOverlayContainer.transform, false);
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "No Text Detected";
        tmpText.font = null;
        tmpText.fontSize = 48;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(600, 300);
        textRect.anchoredPosition = Vector2.zero;

        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
        }
        destroyCoroutine = StartCoroutine(DestroyOverlayAfterDelay(5f));
    }

    // === 오버레이 제거 코루틴 ===
    
    private IEnumerator DestroyOverlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearOCROverlay();
        destroyCoroutine = null;
    }

    private IEnumerator DestroyOriginOverlayAfterDelay(GameObject originContainer, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (originContainer != null)
        {
            Destroy(originContainer);
        }
    }

    // === 오버레이 제거 ===
    
    private void ClearOCROverlay()
    {
        if (ocrOverlayContainer != null)
        {
            Destroy(ocrOverlayContainer);
            ocrOverlayContainer = null;
        }

        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            destroyCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        ClearOCROverlay();
    }
}

// OCR 결과 데이터 구조
[System.Serializable]
public class OCRResult
{
    public List<string> labels;
    public List<string> labels_origin;  // 번역 전 원본 (없으면 null)
    public List<List<List<float>>> quad_boxes; // quad_boxes: [[[x1,y1],[x2,y2],[x3,y3],[x4,y4]], ...]
    public List<List<List<float>>> quad_boxes_origin; // 원문 박스 (ocr_with_region_origin)
}
