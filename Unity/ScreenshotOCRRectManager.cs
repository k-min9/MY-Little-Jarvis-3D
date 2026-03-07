using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

// OCR 슬롯별 커스텀 캡처 영역을 관리하는 매니저
// 게임 재시작 시 항상 초기화됨 (비영속)
public class ScreenshotOCRRectManager : MonoBehaviour
{
    // Windows API 선언
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;  // 주 모니터 너비
    private const int SM_CYSCREEN = 1;  // 주 모니터 높이

    // 싱글톤 인스턴스
    private static ScreenshotOCRRectManager instance;
    public static ScreenshotOCRRectManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ScreenshotOCRRectManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ScreenshotOCRRectManager");
                    instance = go.AddComponent<ScreenshotOCRRectManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    // 슬롯별 커스텀 영역 데이터 (비영속)
    private struct OCRCustomRect
    {
        public bool isEnabled;           // 커스텀 범위 사용 여부
        public int x, y, width, height;  // 캡처 영역 (Windows 좌표)
    }

    private OCRCustomRect[] customRects = new OCRCustomRect[3];  // slot 0~2 (실제 슬롯 1~3)

    // OCR 전용 UI (ScreenshotManager의 배경은 공용 사용)
    [Header("OCR Area Selection UI")]
    [SerializeField] private GameObject ocrAreaPanel;           // 영역 선택 사각형 UI (전용)
    private GameObject BackgroundPanel => ScreenshotManager.Instance.backgroundOverlayPanel;  // 배경 공용
    
    private Canvas _canvas;
    private bool isSelectingArea = false;
    private int selectingSlot = -1;  // 현재 영역 선택 중인 슬롯
    private Coroutine selectAreaCoroutine;
    
    // 선택 완료 콜백
    public Action<int> OnAreaSelectComplete;

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
            return;
        }

        _canvas = FindObjectOfType<Canvas>();
        
        // 모든 슬롯 초기화 (비영속 - 항상 기본설정으로 시작)
        for (int i = 0; i < customRects.Length; i++)
        {
            customRects[i] = new OCRCustomRect { isEnabled = false };
        }
        
        // UI 초기 상태
        if (ocrAreaPanel != null) ocrAreaPanel.SetActive(false);
    }

    // 해당 슬롯에 커스텀 영역이 설정되어 있는지 확인
    public bool HasCustomRect(int slot)
    {
        int idx = slot - 1;  // 슬롯 1~3 → 인덱스 0~2
        if (idx < 0 || idx >= customRects.Length) return false;
        return customRects[idx].isEnabled;
    }

    // 해당 슬롯의 커스텀 영역 좌표 반환
    public (int x, int y, int width, int height) GetCustomRect(int slot)
    {
        int idx = slot - 1;
        if (idx < 0 || idx >= customRects.Length)
        {
            Debug.LogWarning($"[OCRRect] Invalid slot {slot}");
            return (0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
        }
        
        var rect = customRects[idx];
        return (rect.x, rect.y, rect.width, rect.height);
    }

    // "OCR 전용범위" 버튼 클릭 → 영역 선택 시작
    public void SetCustomRectMode(int slot)
    {
        if (isSelectingArea)
        {
            Debug.LogWarning("[OCRRect] Area selection already in progress");
            return;
        }

        selectingSlot = slot;
        isSelectingArea = true;
        
        BackgroundPanel.SetActive(true);
        
        selectAreaCoroutine = StartCoroutine(SelectAreaCoroutine());
        Debug.Log($"[OCRRect] Started area selection for slot {slot}");
    }

    // "기본설정" 버튼 클릭 → 커스텀 영역 해제 및 초기화
    public void ClearCustomRectMode(int slot)
    {
        int idx = slot - 1;
        if (idx < 0 || idx >= customRects.Length) return;

        // 영역 완전 초기화 (isEnabled = false, 좌표도 0으로 리셋)
        customRects[idx] = new OCRCustomRect { isEnabled = false, x = 0, y = 0, width = 0, height = 0 };
        Debug.Log($"[OCRRect] Cleared custom rect for slot {slot}");
    }

    // 영역 선택 취소
    public void CancelAreaSelection()
    {
        if (!isSelectingArea) return;

        isSelectingArea = false;
        selectingSlot = -1;

        if (selectAreaCoroutine != null)
        {
            StopCoroutine(selectAreaCoroutine);
            selectAreaCoroutine = null;
        }

        BackgroundPanel.SetActive(false);
        ocrAreaPanel.SetActive(false);

        Debug.Log("[OCRRect] Area selection cancelled");
    }

    // 영역 선택 코루틴 (ScreenshotManager.SelectArea 참조)
    private IEnumerator SelectAreaCoroutine()
    {
        yield return new WaitForSeconds(0.3f);  // UI 갱신 대기

        Vector3 startMousePosition = Vector3.zero;
        
        while (isSelectingArea)
        {
            if (Input.GetMouseButtonDown(0))  // 마우스 왼쪽 클릭
            {
                startMousePosition = Input.mousePosition;
                startMousePosition.x = Mathf.Clamp(startMousePosition.x, 0, Screen.width);
                startMousePosition.y = Mathf.Clamp(startMousePosition.y, 0, Screen.height);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform, 
                    startMousePosition, 
                    _canvas.worldCamera, 
                    out Vector2 startLocalPoint
                );

                ocrAreaPanel.SetActive(true);

                // 드래그 중
                while (Input.GetMouseButton(0))
                {
                    DrawRect(startLocalPoint);
                    yield return null;
                }

                // 드래그 완료 - 좌표 저장
                SaveSelectedArea();

                isSelectingArea = false;
                ocrAreaPanel.SetActive(false);
                BackgroundPanel.SetActive(false);
                selectAreaCoroutine = null;

                // 완료 콜백
                OnAreaSelectComplete?.Invoke(selectingSlot);
                
                Debug.Log($"[OCRRect] Area selection completed for slot {selectingSlot}");
            }
            
            yield return null;
        }
    }

    // 선택 중 사각형 UI 갱신
    private void DrawRect(Vector2 startLocalPoint)
    {
        if (ocrAreaPanel == null || _canvas == null) return;

        Vector3 endMousePos = Input.mousePosition;
        endMousePos.y = Mathf.Clamp(endMousePos.y, 0, Screen.height);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, 
            endMousePos, 
            _canvas.worldCamera, 
            out Vector2 endLocalPoint
        );

        Vector3 center = (startLocalPoint + endLocalPoint) / 2f;
        Vector2 size = new Vector2(
            Mathf.Abs(startLocalPoint.x - endLocalPoint.x),
            Mathf.Abs(startLocalPoint.y - endLocalPoint.y)
        );

        RectTransform panelRectTransform = ocrAreaPanel.GetComponent<RectTransform>();
        if (panelRectTransform != null)
        {
            panelRectTransform.localPosition = center;
            panelRectTransform.sizeDelta = size;
        }
    }

    // 선택된 영역을 Windows 좌표로 변환하여 저장
    private void SaveSelectedArea()
    {
        if (ocrAreaPanel == null || selectingSlot < 1) return;

        int idx = selectingSlot - 1;
        if (idx < 0 || idx >= customRects.Length) return;

        RectTransform panelRectTransform = ocrAreaPanel.GetComponent<RectTransform>();
        if (panelRectTransform == null) return;

        // Unity Local 좌표 → Windows 스크린 좌표 변환
        Vector2 bottomLeft = new Vector2(
            panelRectTransform.anchoredPosition.x - panelRectTransform.sizeDelta.x / 2,
            panelRectTransform.anchoredPosition.y + panelRectTransform.sizeDelta.y / 2
        );
        Vector2 topRight = new Vector2(
            panelRectTransform.anchoredPosition.x + panelRectTransform.sizeDelta.x / 2,
            panelRectTransform.anchoredPosition.y - panelRectTransform.sizeDelta.y / 2
        );

        // ScreenshotManager의 변환 방식 사용
        Vector2 startWin = ConvertUnityPosToWinpos(bottomLeft);
        Vector2 endWin = ConvertUnityPosToWinpos(topRight);

        int x = (int)startWin.x;
        int y = (int)(Screen.height - startWin.y);
        int width = (int)(endWin.x - startWin.x);
        int height = (int)(startWin.y - endWin.y);

        // 방어 로직
        width = Mathf.Max(1, Mathf.Abs(width));
        height = Mathf.Max(1, Mathf.Abs(height));

        customRects[idx] = new OCRCustomRect
        {
            isEnabled = true,
            x = x,
            y = y,
            width = width,
            height = height
        };

        Debug.Log($"[OCRRect] Saved custom rect for slot {selectingSlot}: x={x}, y={y}, w={width}, h={height}");
    }

    // Unity Canvas 좌표를 Windows 좌표로 변환
    private Vector2 ConvertUnityPosToWinpos(Vector2 localPoint)
    {
        if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null) return localPoint;

        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        Vector3 worldPoint = canvasRect.TransformPoint(localPoint);
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        return screenPoint;
    }

    // 현재 영역 선택 중인지 확인
    public bool IsSelectingArea()
    {
        return isSelectingArea;
    }

    // 선택된 OCR 영역을 캡처해서 3초간 화면에 표시
    public void ShowScreenshotOCRRectImage(int slot)
    {
        var (x, y, width, height) = GetCustomRect(slot);
        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning("[OCRRect] Invalid rect size for preview");
            return;
        }
        
        StartCoroutine(ShowOCRRectPreviewCoroutine(x, y, width, height));
    }

    private Coroutine previewCoroutine;
    private GameObject previewUI;

    private IEnumerator ShowOCRRectPreviewCoroutine(int x, int y, int width, int height)
    {
        // 기존 프리뷰가 있으면 정리
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            if (previewUI != null) Destroy(previewUI);
        }

        // 해당 영역 캡처
        byte[] imageBytes = null;
        bool captureComplete = false;
        
        yield return ScreenshotManager.Instance.CaptureAreaToMemory(x, y, width, height, (bytes) =>
        {
            imageBytes = bytes;
            captureComplete = true;
        });

        while (!captureComplete) yield return null;

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogWarning("[OCRRect] Failed to capture preview");
            yield break;
        }

        // 텍스처 생성
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        // UI 생성 및 표시
        Canvas canvas = _canvas ?? FindObjectOfType<Canvas>();
        if (canvas == null) yield break;

        previewUI = new GameObject("OCRRectPreview");
        previewUI.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = previewUI.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 이미지 크기 조정 (화면의 50% 이내로)
        float maxWidth = Screen.width * 0.5f;
        float maxHeight = Screen.height * 0.5f;
        float scale = Mathf.Min(maxWidth / texture.width, maxHeight / texture.height, 1f);
        rectTransform.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);

        Image image = previewUI.AddComponent<Image>();
        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        // 테두리 효과 (Outline)
        Outline outline = previewUI.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.8f, 0.2f, 1f);  // 녹색 테두리
        outline.effectDistance = new Vector2(3, 3);

        Debug.Log($"[OCRRect] Showing preview for 3 seconds");

        // 3초 후 제거
        yield return new WaitForSeconds(3f);

        if (previewUI != null)
        {
            Destroy(previewUI);
            previewUI = null;
        }
        previewCoroutine = null;
    }
}
