using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class OCRAutoMapSlotInfo
{
    public string ocrText = "";
    public string actorId = "Auto";
    public bool isActive = true;
}

public class OCRAutoMapManager : MonoBehaviour
{
    public static OCRAutoMapManager instance;
    public static OCRAutoMapManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OCRAutoMapManager>();
            }
            return instance;
        }
    }

    public GameObject mappingSlotSample;
    public Transform mappingSlotParent;
    public GameObject saveButton;
    public ScrollRect scrollRect;

    private Dictionary<int, OCRAutoMapSlotInfo> customActorMap = new Dictionary<int, OCRAutoMapSlotInfo>();

    private List<string> dropdownDisplayOptions = new List<string>();
    private List<string> dropdownActorIdByIndex = new List<string>();

    // Start에서 샘플 슬롯을 비활성화하고, 드롭다운 캐시 구성 후 json 로드 및 슬롯 UI를 구성합니다.
    private void Start()
    {
        mappingSlotSample.SetActive(false);

        SetSaveButtonState(false);
        BuildDropdownCache();

        LoadFromJson();
        RebuildAllSlots();
    }

    // Save 버튼 클릭 시, 현재 화면 상태(customActorMap 전체)를 통째로 json으로 저장하고 저장 버튼을 비활성화합니다.
    public void OnClickSaveButton()
    {
        SaveToJson();
        SetSaveButtonState(false);
    }

    // Refresh 버튼 클릭 시, json을 다시 로드하여 UI를 재구성하고 저장 버튼을 비활성화합니다.
    public void OnClickRefreshButton()
    {
        LoadFromJson();
        RebuildAllSlots();
        SetSaveButtonState(false);
    }

    // + 버튼 클릭 시, 새 슬롯을 추가하고 저장 버튼을 활성화합니다. 실제 저장은 Save 버튼에서만 수행합니다.
    public void AddMappingSlot()
    {
        int newKey = GetNextKey();
        customActorMap.Add(newKey, new OCRAutoMapSlotInfo());

        AddSlotUI(newKey, customActorMap[newKey]);
        SetSaveButtonState(true);
        ScrollToBottom();
    }

    // 슬롯 외곽 버튼 클릭 시, isActive를 토글하고 저장 버튼을 활성화합니다. 실제 저장은 Save 버튼에서만 수행합니다.
    public void ToggleSlotActive(int slotKey)
    {
        customActorMap[slotKey].isActive = !customActorMap[slotKey].isActive;
        SetSaveButtonState(true);

        OCRAutoMapSlotController controller = FindSlotController(slotKey);
        if (controller != null)
        {
            controller.ApplyActiveVisual(customActorMap[slotKey].isActive);
        }
    }

    // InputField 값 변경 시, 해당 슬롯의 ocrText를 갱신하고 저장 버튼을 활성화합니다. 실제 저장은 Save 버튼에서만 수행합니다.
    public void UpdateSlotText(int slotKey, string newText)
    {
        customActorMap[slotKey].ocrText = newText;
        SetSaveButtonState(true);
    }

    // Dropdown 값 변경 시, 해당 슬롯의 actorId를 갱신하고 저장 버튼을 활성화합니다. 실제 저장은 Save 버튼에서만 수행합니다.
    public void UpdateSlotActorId(int slotKey, string actorId)
    {
        customActorMap[slotKey].actorId = actorId;
        SetSaveButtonState(true);
    }

    // Delete 버튼 클릭 시, 해당 슬롯을 customActorMap에서 제거하고 UI에서도 삭제합니다. 실제 저장은 Save 버튼에서만 수행합니다.
    public void DeleteSlot(int slotKey)
    {
        // customActorMap에서 제거
        if (customActorMap.ContainsKey(slotKey))
        {
            customActorMap.Remove(slotKey);
        }

        // UI에서 해당 슬롯 오브젝트 제거
        OCRAutoMapSlotController controller = FindSlotController(slotKey);
        if (controller != null)
        {
            Destroy(controller.gameObject);
        }

        // 저장 버튼 활성화
        SetSaveButtonState(true);

        Debug.Log($"[OCRAutoMap] Slot {slotKey} deleted (not saved yet)");
    }

    // 돋보기 버튼 클릭 시, 영역 선택 UI를 띄우고 OCR을 실행합니다.
    public void RequestOcrForSlot(int slotKey)
    {
        StartCoroutine(RequestOcrForSlotCoroutine(slotKey));
    }

    // OCR 영역 선택 및 실행 코루틴
    private IEnumerator RequestOcrForSlotCoroutine(int slotKey)
    {
        Debug.Log($"[OCRAutoMap] Starting OCR for slot {slotKey}");

        // 영역 선택 UI 표시
        GameObject backgroundPanel = ScreenshotManager.Instance.backgroundOverlayPanel;
        if (backgroundPanel == null)
        {
            Debug.LogError("[OCRAutoMap] Background panel not found");
            yield break;
        }

        // 임시 영역 선택 UI 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[OCRAutoMap] Canvas not found");
            yield break;
        }

        GameObject areaPanel = new GameObject("TempOCRAreaPanel");
        areaPanel.transform.SetParent(canvas.transform, false);
        
        Image areaPanelImage = areaPanel.AddComponent<Image>();
        areaPanelImage.color = new Color(0f, 1f, 0f, 0.3f); // 반투명 녹색
        areaPanelImage.raycastTarget = false;
        
        RectTransform areaPanelRect = areaPanel.GetComponent<RectTransform>();

        // 배경 패널 활성화
        backgroundPanel.SetActive(true);
        areaPanel.SetActive(false);

        yield return new WaitForSeconds(0.3f); // UI 갱신 대기

        // 영역 선택 대기
        Vector3 startMousePos = Vector3.zero;
        Vector2 startLocalPoint = Vector2.zero;
        bool areaSelected = false;
        int captureX = 0, captureY = 0, captureWidth = 0, captureHeight = 0;

        while (!areaSelected)
        {
            if (Input.GetMouseButtonDown(0))
            {
                startMousePos = Input.mousePosition;
                startMousePos.x = Mathf.Clamp(startMousePos.x, 0, Screen.width);
                startMousePos.y = Mathf.Clamp(startMousePos.y, 0, Screen.height);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    startMousePos,
                    canvas.worldCamera,
                    out startLocalPoint
                );

                areaPanel.SetActive(true);

                // 드래그 중
                while (Input.GetMouseButton(0))
                {
                    Vector3 currentMousePos = Input.mousePosition;
                    currentMousePos.x = Mathf.Clamp(currentMousePos.x, 0, Screen.width);
                    currentMousePos.y = Mathf.Clamp(currentMousePos.y, 0, Screen.height);

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvas.transform as RectTransform,
                        currentMousePos,
                        canvas.worldCamera,
                        out Vector2 currentLocalPoint
                    );

                    Vector3 center = (startLocalPoint + currentLocalPoint) / 2f;
                    Vector2 size = new Vector2(
                        Mathf.Abs(startLocalPoint.x - currentLocalPoint.x),
                        Mathf.Abs(startLocalPoint.y - currentLocalPoint.y)
                    );

                    areaPanelRect.localPosition = center;
                    areaPanelRect.sizeDelta = size;

                    yield return null;
                }

                // 드래그 완료 - 좌표 계산
                Vector2 bottomLeft = new Vector2(
                    areaPanelRect.anchoredPosition.x - areaPanelRect.sizeDelta.x / 2,
                    areaPanelRect.anchoredPosition.y + areaPanelRect.sizeDelta.y / 2
                );
                Vector2 topRight = new Vector2(
                    areaPanelRect.anchoredPosition.x + areaPanelRect.sizeDelta.x / 2,
                    areaPanelRect.anchoredPosition.y - areaPanelRect.sizeDelta.y / 2
                );

                // Canvas 좌표를 Screen 좌표로 변환
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                Vector3 bottomLeftWorld = canvasRect.TransformPoint(bottomLeft);
                Vector3 topRightWorld = canvasRect.TransformPoint(topRight);
                
                Vector2 bottomLeftScreen = Camera.main.WorldToScreenPoint(bottomLeftWorld);
                Vector2 topRightScreen = Camera.main.WorldToScreenPoint(topRightWorld);

                captureX = (int)bottomLeftScreen.x;
                captureY = (int)(Screen.height - bottomLeftScreen.y);
                captureWidth = (int)(topRightScreen.x - bottomLeftScreen.x);
                captureHeight = (int)(bottomLeftScreen.y - topRightScreen.y);

                // 방어 로직
                captureWidth = Mathf.Max(1, Mathf.Abs(captureWidth));
                captureHeight = Mathf.Max(1, Mathf.Abs(captureHeight));

                Debug.Log($"[OCRAutoMap] Selected area: x={captureX}, y={captureY}, w={captureWidth}, h={captureHeight}");

                areaSelected = true;
            }

            yield return null;
        }

        // UI 정리
        backgroundPanel.SetActive(false);
        Destroy(areaPanel);

        // 영역 캡처
        byte[] imageBytes = null;
        bool captureComplete = false;

        yield return ScreenshotManager.Instance.CaptureAreaToMemory(captureX, captureY, captureWidth, captureHeight, (bytes) =>
        {
            imageBytes = bytes;
            captureComplete = true;
        });

        while (!captureComplete)
        {
            yield return null;
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("[OCRAutoMap] Failed to capture area");
            yield break;
        }

        Debug.Log($"[OCRAutoMap] Captured {imageBytes.Length} bytes");

        // OCR 실행 (번역 없이 텍스트만 추출)
        string aiLang = SettingManager.Instance.settings.ai_language ?? "en";
        
        // 현재 슬롯의 actorId 가져오기
        string actorId = customActorMap[slotKey].actorId;
        bool isWhiteOnly = (actorId == "Auto");  // Auto일 때만 true
        
        OCRResult ocrResult = null;
        bool ocrComplete = false;

        APIManager.Instance.CallPaddleOCR(
            imageBytes,
            targetLang: aiLang,
            autoDetect: true,
            useTranslate: false, // 번역 없이 OCR만 수행
            originLang: "auto",
            isFormality: false,
            isSentence: false,
            mergeThreshold: 100,
            saveResult: false,
            saveImage: false,
            isDebug: false,
            isWhiteOnly: isWhiteOnly,
            callback: (result) =>
            {
                ocrResult = result;
                ocrComplete = true;
            }
        );

        while (!ocrComplete)
        {
            yield return null;
        }

        if (ocrResult == null || ocrResult.labels == null || ocrResult.labels.Count == 0)
        {
            Debug.LogWarning("[OCRAutoMap] No OCR results");
            yield break;
        }

        // 텍스트 결합 (가장 긴 텍스트만 선택 또는 모든 텍스트를 스페이스로 결합)
        string resultText = string.Join(" ", ocrResult.labels);
        Debug.Log($"[OCRAutoMap] OCR result: {resultText}");

        // InputField에 반영
        ApplyOcrResultToSlot(slotKey, resultText);
    }

    // OCR 결과를 해당 슬롯에 반영하고 저장 버튼을 활성화합니다. 실제 저장은 Save 버튼에서만 수행합니다.
    public void ApplyOcrResultToSlot(int slotKey, string resultText)
    {
        customActorMap[slotKey].ocrText = resultText;
        SetSaveButtonState(true);

        OCRAutoMapSlotController controller = FindSlotController(slotKey);
        if (controller != null)
        {
            controller.SetInputTextWithoutNotify(resultText);
        }
    }

    // 현재 customActorMap을 반환합니다.
    public Dictionary<int, OCRAutoMapSlotInfo> GetCustomActorMap()
    {
        return customActorMap;
    }

    // isActive 슬롯만 대상으로 custom 텍스트 매핑을 만들고, 같은 텍스트가 있으면 키가 큰 슬롯이 우선되도록 구성합니다.
    // actorId가 Auto여도 그대로 매핑에 포함합니다.
    public Dictionary<string, string> BuildCustomTextToActorMap()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();

        List<int> keys = new List<int>(customActorMap.Keys);
        keys.Sort();

        foreach (int key in keys)
        {
            OCRAutoMapSlotInfo info = customActorMap[key];
            if (!info.isActive)
            {
                continue;
            }

            if (string.IsNullOrEmpty(info.ocrText))
            {
                continue;
            }

            map[info.ocrText] = info.actorId;
        }

        return map;
    }

    // 텍스트에서 캐릭터 actor ID 추출
    // 커스텀 매핑과 기본 매핑을 합친 전체 매핑에서 찾아 반환
    // 매칭되는 캐릭터가 있으면 actor ID 반환, 없으면 null
    public string GetActorFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        
        string trimmed = text.Trim();
        
        // GetActorMapIncludeCustomMap()이 이미 커스텀 + 기본 매핑을 합쳐서 반환
        Dictionary<string, string> fullMap = GetActorMapIncludeCustomMap();
        
        if (fullMap.ContainsKey(trimmed))
        {
            return fullMap[trimmed];
        }
        
        return null;
    }

    // customActorMap과 STTDataActor.actorMap을 합쳐 최종 매핑을 반환합니다.
    // 순서:
    // 1) customActorMap에서 isActive 슬롯을 키 오름차순으로 적용하여 customMap을 생성합니다. 같은 텍스트는 키가 큰 슬롯이 우선됩니다.
    // 2) STTDataActor의 헬퍼(GetAllActorIds, GetAllActorNamesFromActorId)를 사용하여 baseMap을 재구성합니다.
    // 3) baseMap 위에 customMap을 덮어써서 custom 우선 최종 map을 반환합니다.
    public Dictionary<string, string> GetActorMapIncludeCustomMap()
    {
        Dictionary<string, string> customMap = BuildCustomTextToActorMap();
        Dictionary<string, string> baseMap = BuildBaseActorMapFromHelpers();

        foreach (var kv in customMap)
        {
            baseMap[kv.Key] = kv.Value;
        }

        return baseMap;
    }

    // Dropdown 표시 옵션과 index별 actorId 테이블을 구성합니다.
    // index 0은 Auto, 1..N은 actorId 오름차순입니다.
    public void BuildDropdownCache()
    {
        dropdownDisplayOptions.Clear();
        dropdownActorIdByIndex.Clear();

        dropdownDisplayOptions.Add("Auto");
        dropdownActorIdByIndex.Add("Auto");

        List<string> actorIds = STTDataActor.GetAllActorIds();
        foreach (string actorId in actorIds)
        {
            dropdownDisplayOptions.Add(STTDataActor.GetDisplayName(actorId));
            dropdownActorIdByIndex.Add(actorId);
        }
    }

    // json 로드 후 customActorMap을 기준으로 슬롯 UI를 전부 재구성합니다.
    private void RebuildAllSlots()
    {
        ClearAllSlotObjects();

        List<int> keys = new List<int>(customActorMap.Keys);
        keys.Sort();

        foreach (int key in keys)
        {
            AddSlotUI(key, customActorMap[key]);
        }

        ScrollToBottom();
    }

    // 슬롯 오브젝트를 Instantiate하고, 컨트롤러에 필요한 데이터와 드롭다운 캐시를 주입합니다.
    private void AddSlotUI(int slotKey, OCRAutoMapSlotInfo info)
    {
        GameObject slotObj = Instantiate(mappingSlotSample, mappingSlotParent);
        slotObj.SetActive(true);

        OCRAutoMapSlotController controller = slotObj.GetComponent<OCRAutoMapSlotController>();
        controller.Init(this, slotKey, info, dropdownDisplayOptions, dropdownActorIdByIndex);
    }

    // sample 슬롯을 제외한 모든 슬롯 오브젝트를 제거합니다.
    private void ClearAllSlotObjects()
    {
        foreach (Transform child in mappingSlotParent)
        {
            if (child.gameObject == mappingSlotSample)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    // slotKey로 컨트롤러를 찾아 반환합니다.
    private OCRAutoMapSlotController FindSlotController(int slotKey)
    {
        foreach (Transform child in mappingSlotParent)
        {
            if (child.gameObject == mappingSlotSample)
            {
                continue;
            }

            OCRAutoMapSlotController c = child.GetComponent<OCRAutoMapSlotController>();
            if (c != null && c.slotKey == slotKey)
            {
                return c;
            }
        }

        return null;
    }

    // 새 슬롯 키를 반환합니다. 현재 최대 키 + 1을 사용합니다.
    private int GetNextKey()
    {
        int maxKey = 0;
        foreach (int key in customActorMap.Keys)
        {
            if (key > maxKey)
            {
                maxKey = key;
            }
        }

        return maxKey + 1;
    }

    // json 파일이 있으면 로드하고, 없거나 실패하면 기본 슬롯 1개를 생성합니다.
    private void LoadFromJson()
    {
        string path = GetFilePath();
        if (!File.Exists(path))
        {
            customActorMap = new Dictionary<int, OCRAutoMapSlotInfo>();
            customActorMap.Add(1, new OCRAutoMapSlotInfo());
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            Dictionary<int, OCRAutoMapSlotInfo> loaded = JsonConvert.DeserializeObject<Dictionary<int, OCRAutoMapSlotInfo>>(json);

            if (loaded == null || loaded.Count == 0)
            {
                loaded = new Dictionary<int, OCRAutoMapSlotInfo>();
                loaded.Add(1, new OCRAutoMapSlotInfo());
            }

            customActorMap = loaded;
        }
        catch (Exception)
        {
            customActorMap = new Dictionary<int, OCRAutoMapSlotInfo>();
            customActorMap.Add(1, new OCRAutoMapSlotInfo());
        }
    }

    // 현재 customActorMap 전체를 json으로 저장합니다.
    private void SaveToJson()
    {
        string path = GetFilePath();

        try
        {
            string json = JsonConvert.SerializeObject(customActorMap, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (Exception)
        {
        }
    }

    // 저장 파일 경로를 구성합니다.
    private string GetFilePath()
    {
        string filename = "ocr_auto_map.json";
        return Path.Combine(Application.persistentDataPath, filename);
    }

    // ScrollRect가 연결되어 있으면 하단으로 스크롤합니다.
    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    // 저장 버튼의 활성 상태를 토글합니다.
    public void SetSaveButtonState(bool isActive)
    {
        saveButton.SetActive(isActive);
    }

    // STTDataActor의 헬퍼 메서드만 사용하여 base actorMap을 재구성합니다.
    // actorId 목록을 순회하면서 actorId에 해당하는 모든 이름 키를 수집하고 name -> actorId를 구성합니다.
    private Dictionary<string, string> BuildBaseActorMapFromHelpers()
    {
        Dictionary<string, string> baseMap = new Dictionary<string, string>();

        List<string> actorIds = STTDataActor.GetAllActorIds();
        foreach (string actorId in actorIds)
        {
            HashSet<string> names = STTDataActor.GetAllActorNamesFromActorId(actorId);
            foreach (string name in names)
            {
                baseMap[name] = actorId;
            }
        }

        return baseMap;
    }
}
