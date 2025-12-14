using System;
using System.IO;
// using System.Drawing;  // bitmap
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DevionGames.UIWidgets;

public class ScreenshotManager : MonoBehaviour
{
    public GameObject screenshotArea; // Panel acting as screenshot area (with borders)
    public GameObject backgroundOverlayPanel; // Panel for background overlay
    
    // Unity 창 핸들 캐싱
    private IntPtr _unityWindowHandle = IntPtr.Zero;
    
    // Tag 기반 상호 배타적 오브젝트 그룹 (key: Face, Pose 등, value: 해당 그룹의 오브젝트 리스트)
    private Dictionary<string, List<GameObject>> exclusiveObjectGroups;

    private Canvas _canvas;
    private bool isSelectingArea = false;
    private Vector3 startMousePosition;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
                                      IntPtr hdcSrc, int xSrc, int ySrc, int Rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    // Unity 창 캡처 제외를 위한 Windows API
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    private const int SRCCOPY = 0x00CC0020;
    
    // GetSystemMetrics 상수
    private const int SM_CXSCREEN = 0;  // 주 모니터 너비
    private const int SM_CYSCREEN = 1;  // 주 모니터 높이
    
    // SetWindowDisplayAffinity 상수
    private const uint WDA_NONE = 0x00000000;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    #region Helper method
    
    // 캡처 전처리/후처리에 필요한 상태 정보를 담는 구조체
    private struct CaptureState
    {
        public bool shouldExcludeUnity;
        public bool shouldUseLayerDeactivation;
        public List<GameObject> deactivatedCharObjects;
        public List<GameObject> deactivatedUIObjects;
        public Dictionary<GameObject, bool> uiWidgetVisibleStates;
    }
    
    // 캡처 전처리: 설정 판단 + 케이스 분기 + SetCaptureExclusion 또는 레이어 비활성화 수행
    private CaptureState PrepareCapture(string logPrefix)
    {
        CaptureState state = new CaptureState();
        
        // 설정 조회
        bool shouldIncludeChar = SettingManager.Instance.settings.includeCharInScreenshot;
        bool shouldIncludeUI = SettingManager.Instance.settings.includeUIInScreenshot;
        
        // 케이스 분기 판단
        state.shouldExcludeUnity = !shouldIncludeChar && !shouldIncludeUI; // 둘 다 제외
        state.shouldUseLayerDeactivation = (!shouldIncludeChar && shouldIncludeUI) || (shouldIncludeChar && !shouldIncludeUI); // 하나만 제외
        
        // 전처리 수행
        if (state.shouldExcludeUnity)
        {
            // 케이스 1: 둘 다 제외 - Unity 창 전체를 캡처에서 제외 시도
            Debug.Log($"{logPrefix} 모드: Unity 창 전체 제외 시도 (SetCaptureExclusion)");
            bool exclusionSuccess = SetCaptureExclusion(true);
            
            if (!exclusionSuccess)
            {
                // Fallback: SetCaptureExclusion 실패 시 레이어 비활성화 방식으로 전환
                Debug.LogWarning($"{logPrefix} SetCaptureExclusion 실패, 레이어 비활성화 방식으로 폴백");
                state.shouldExcludeUnity = false;
                state.shouldUseLayerDeactivation = true;
                (state.deactivatedCharObjects, state.deactivatedUIObjects, state.uiWidgetVisibleStates) = DeactivateLayersForCapture();
            }
        }
        else if (state.shouldUseLayerDeactivation)
        {
            // 케이스 2: 하나만 제외 - 특정 레이어만 비활성화
            Debug.Log($"{logPrefix} 모드: 레이어별 선택적 비활성화 (SetActive)");
            (state.deactivatedCharObjects, state.deactivatedUIObjects, state.uiWidgetVisibleStates) = DeactivateLayersForCapture();
        }
        else
        {
            // 케이스 3: 둘 다 포함 - 아무 로직도 실행하지 않음
            Debug.Log($"{logPrefix} 모드: 전체 포함 (로직 없음)");
        }
        
        return state;
    }
    
    // 캡처 후처리: 상태 복구 (SetCaptureExclusion 해제 또는 레이어 재활성화)
    private void CleanupCapture(CaptureState state)
    {
        try
        {
            if (state.shouldExcludeUnity)
            {
                SetCaptureExclusion(false);
            }
            else if (state.shouldUseLayerDeactivation)
            {
                if (state.deactivatedCharObjects != null || state.deactivatedUIObjects != null)
                {
                    ReactivateLayersAfterCapture(state.deactivatedCharObjects, state.deactivatedUIObjects, state.uiWidgetVisibleStates);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Screenshot] CleanupCapture 중 예외 발생: {ex.Message}");
        }
    }
    
    // 캡처 전 렌더링 대기 코루틴
    private IEnumerator WaitForCapture(CaptureState state)
    {
        if (state.shouldExcludeUnity)
        {
            // Unity 창 제외 모드: 2프레임 + 50ms 대기
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(0.05f);
        }
        else if (state.shouldUseLayerDeactivation)
        {
            // 레이어 비활성화 모드: 비활성화된 오브젝트가 있으면 2프레임 대기
            bool hasDeactivatedObjects = 
                (state.deactivatedCharObjects != null && state.deactivatedCharObjects.Count > 0) ||
                (state.deactivatedUIObjects != null && state.deactivatedUIObjects.Count > 0);
            
            if (hasDeactivatedObjects)
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
            }
        }
        // 케이스 3 (둘 다 포함): 대기 없음
    }
    
    // screenshotArea 기반 캡처 영역 좌표 계산
    private (int x, int y, int width, int height) CalculateCaptureArea()
    {
        RectTransform panelRectTransform = screenshotArea.GetComponent<RectTransform>();
        
        Vector2 bottomLeft = new Vector2(
            panelRectTransform.anchoredPosition.x - panelRectTransform.sizeDelta.x / 2,
            panelRectTransform.anchoredPosition.y + panelRectTransform.sizeDelta.y / 2
        );

        Vector2 topRight = new Vector2(
            panelRectTransform.anchoredPosition.x + panelRectTransform.sizeDelta.x / 2,
            panelRectTransform.anchoredPosition.y - panelRectTransform.sizeDelta.y / 2
        );

        // Local -> Windows 좌표 변환
        Vector2 start = ConvertUnityPosToWinpos(bottomLeft);
        Vector2 end = ConvertUnityPosToWinpos(topRight);

        // 최종 캡처 영역 계산
        int x = (int)start.x;
        int y = (int)(Screen.height - start.y);
        int width = (int)(end.x - start.x);
        int height = (int)(start.y - end.y);
        
        return (x, y, width, height);
    }
    
    #endregion

    void Start()
    {
        backgroundOverlayPanel.SetActive(false); // Initially, background is disabled
        screenshotArea.SetActive(false); // Initially, screenshot area is hidden
        _canvas = FindObjectOfType<Canvas>();  // 최상위 Canvas
        
        // Unity 창 핸들 획득
        _unityWindowHandle = GetActiveWindow();
        Debug.Log($"Unity Window Handle: {_unityWindowHandle}");
    }

    public void SetScreenshotArea()
    {
        isSelectingArea = true;

        backgroundOverlayPanel.SetActive(true); // Activate background

        StartCoroutine(SelectArea());
    }

    // 스크린샷 영역이 설정되었는지 확인
    public bool IsScreenshotAreaSet()
    {
        if (screenshotArea == null) return false;
        
        RectTransform panelRectTransform = screenshotArea.GetComponent<RectTransform>();
        if (panelRectTransform == null) return false;
        
        return panelRectTransform.sizeDelta.x > 0 && panelRectTransform.sizeDelta.y > 0;
    }

    // Unity 창을 캡처에서 제외/포함 설정
    private bool SetCaptureExclusion(bool exclude)
    {
        // 핸들이 없으면 다시 획득 시도
        if (_unityWindowHandle == IntPtr.Zero)
        {
            _unityWindowHandle = GetActiveWindow();
            Debug.Log($"Reacquired Unity Window Handle: {_unityWindowHandle}");
        }

        // affinity 값 설정
        uint affinity = exclude ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE;
        
        // SetWindowDisplayAffinity 호출
        bool result = SetWindowDisplayAffinity(_unityWindowHandle, affinity);
        
        if (result)
        {
            Debug.Log($"SetCaptureExclusion: {(exclude ? "제외" : "포함")} 모드 설정 성공");
        }
        else
        {
            Debug.LogWarning($"SetCaptureExclusion: {(exclude ? "제외" : "포함")} 모드 설정 실패");
        }
        
        return result;
    }

    #region 레이어별 비활성화 로직 (Hybrid 방식 - 하나만 제외할 경우 사용)
    
    // 캡처를 위해 특정 레이어들을 비활성화
    private (List<GameObject> deactivatedCharObjects, List<GameObject> deactivatedUIObjects, Dictionary<GameObject, bool> uiWidgetVisibleStates) DeactivateLayersForCapture()
    {
        List<GameObject> deactivatedCharObjects = new List<GameObject>();
        List<GameObject> deactivatedUIObjects = new List<GameObject>();
        Dictionary<GameObject, bool> uiWidgetVisibleStates = new Dictionary<GameObject, bool>();
        
        // SettingManager에서 설정 조회
        bool shouldIncludeChar = SettingManager.Instance.settings.includeCharInScreenshot;
        bool shouldIncludeUI = SettingManager.Instance.settings.includeUIInScreenshot;
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true); // true = 비활성 포함
        
        // Char Layer 필터링
        if (!shouldIncludeChar)
        {
            int charLayer = LayerMask.NameToLayer("Char");
            if (charLayer != -1)
            {
                foreach (GameObject obj in allObjects)
                {
                    if (obj.layer == charLayer && obj.activeSelf)
                    {
                        obj.SetActive(false);
                        deactivatedCharObjects.Add(obj);
                    }
                }
                Debug.Log($"Char Layer 오브젝트 {deactivatedCharObjects.Count}개 임시 비활성화");
                
                // Single 태그 기반 상호 배타적 그룹 수집 (활성화/비활성화 모두 포함)
                CollectSingleTaggedObjects();
            }
            else
            {
                Debug.LogWarning("'Char' Layer가 존재하지 않습니다.");
            }
        }
        
        // UI Layer 필터링
        if (!shouldIncludeUI)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1)
            {
                foreach (GameObject obj in allObjects)
                {
                    if (obj.layer == uiLayer && obj.activeSelf)
                    {
                        // UIWidget이 있으면 IsVisible 상태 저장
                        UIWidget widget = obj.GetComponent<UIWidget>();
                        if (widget != null)
                        {
                            uiWidgetVisibleStates[obj] = widget.IsM_IsShowing;
                        }
                        
                        obj.SetActive(false);
                        deactivatedUIObjects.Add(obj);
                    }
                }
                Debug.Log($"UI Layer 오브젝트 {deactivatedUIObjects.Count}개 임시 비활성화");
            }
            else
            {
                Debug.LogWarning("'UI' Layer가 존재하지 않습니다.");
            }
        }
        
        return (deactivatedCharObjects, deactivatedUIObjects, uiWidgetVisibleStates);
    }
    
    // 캡처 후 비활성화했던 레이어들을 재활성화
    private void ReactivateLayersAfterCapture(List<GameObject> deactivatedCharObjects, List<GameObject> deactivatedUIObjects, Dictionary<GameObject, bool> uiWidgetVisibleStates)
    {
        // 원래 활성화되어 있던 Char Layer GameObject들 다시 활성화
        if (deactivatedCharObjects != null && deactivatedCharObjects.Count > 0)
        {
            foreach (GameObject obj in deactivatedCharObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            Debug.Log($"Char Layer 오브젝트 {deactivatedCharObjects.Count}개 재활성화");
            
            // 상호 배타적인 오브젝트 그룹 정리 (중복 활성화 방지)
            RefreshObjects();
        }
        
        // 원래 활성화되어 있던 UI Layer GameObject들 다시 활성화
        if (deactivatedUIObjects != null && deactivatedUIObjects.Count > 0)
        {
            foreach (GameObject obj in deactivatedUIObjects)
            {
                if (obj != null)
                {
                    // UIWidget이 있고 Close 중이었으면 복원하지 않음
                    if (uiWidgetVisibleStates != null && uiWidgetVisibleStates.TryGetValue(obj, out bool wasVisible))
                    {
                        if (wasVisible)
                        {
                            obj.SetActive(true);
                        }
                        else
                        {
                            Debug.Log($"[Screenshot] Skipping restore for closing widget: {obj.name}");
                        }
                    }
                    else
                    {
                        // UIWidget이 없는 일반 UI 오브젝트는 그대로 복원
                        obj.SetActive(true);
                    }
                }
            }
            Debug.Log($"UI Layer 오브젝트 {deactivatedUIObjects.Count}개 재활성화");
        }
    }

    // Char Layer에서 Single로 시작하는 태그를 가진 오브젝트들을 그룹화
    private void CollectSingleTaggedObjects()
    {
        exclusiveObjectGroups = new Dictionary<string, List<GameObject>>();
        
        int charLayer = LayerMask.NameToLayer("Char");
        if (charLayer == -1) return;
        
        // Char Layer의 모든 오브젝트 탐색 (활성화/비활성화 모두 포함)
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true); // true = 비활성 포함
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == charLayer && obj.tag.StartsWith("Single"))
            {
                // "SingleFace" -> "Face" 추출
                string groupKey = obj.tag.Substring(6); // "Single" 제거
                
                if (!exclusiveObjectGroups.ContainsKey(groupKey))
                {
                    exclusiveObjectGroups[groupKey] = new List<GameObject>();
                }
                
                exclusiveObjectGroups[groupKey].Add(obj);
            }
        }
        
        // 수집 결과 로그
        foreach (var kvp in exclusiveObjectGroups)
        {
            Debug.Log($"Single 그룹 수집: {kvp.Key} - {kvp.Value.Count}개 오브젝트");
        }
    }

    // 상호 배타적인 오브젝트 그룹 관리 - 각 그룹에서 2개 이상 활성화되어 있으면 첫 번째만 남기고 나머지 비활성화
    public void RefreshObjects()
    {
        if (exclusiveObjectGroups == null || exclusiveObjectGroups.Count == 0) return;

        foreach (var kvp in exclusiveObjectGroups)
        {
            var group = kvp.Value;
            if (group == null || group.Count == 0) continue;

            // 그룹 내에서 활성화된 오브젝트들을 찾음
            List<GameObject> activeObjects = new List<GameObject>();
            foreach (var obj in group)
            {
                if (obj != null && obj.activeSelf)
                {
                    activeObjects.Add(obj);
                }
            }

            // 2개 이상 활성화되어 있으면 첫 번째만 남기고 나머지 비활성화
            if (activeObjects.Count > 1)
            {
                for (int i = 1; i < activeObjects.Count; i++)
                {
                    activeObjects[i].SetActive(false);
                }
                Debug.Log($"RefreshObjects: {kvp.Key} 그룹에서 {activeObjects.Count}개 활성화 감지, 첫 번째만 유지");
            }
        }
    }
    
    #endregion

    // 영역을 전체로 잡고 Capture
    private Texture2D CaptureFullScreen()
    {
        byte[] imageBytes = CaptureDesktopAreaToMemory(0, 0, Screen.width, Screen.height);
        
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);
        
        return texture;
    }

    private IEnumerator SelectArea()
    {
        yield return new WaitForSeconds(0.3f);  // 0.3초 대기
        while (isSelectingArea)
        {
            if (Input.GetMouseButtonDown(0)) // On left mouse click
            {
                startMousePosition = Input.mousePosition;
                startMousePosition.x = Mathf.Clamp(startMousePosition.x, 0, Screen.width);
                startMousePosition.y = Mathf.Clamp(startMousePosition.y, 0, Screen.height);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, startMousePosition, _canvas.worldCamera, out Vector2 startLocalPoint);
                screenshotArea.SetActive(true); // Show the screenshot area panel

                while (Input.GetMouseButton(0)) // While mouse is held down, update area
                {
                    DrawRect(startLocalPoint);
                    yield return null;
                }

                isSelectingArea = false;
                screenshotArea.SetActive(false);
                backgroundOverlayPanel.SetActive(false); // Deactivate background
                
                // 스크린샷 영역 설정 완료 - ChatBalloonManager에 알림
                if (ChatBalloonManager.Instance != null)
                {
                    ChatBalloonManager.Instance.SetLastImageSource("screenshot");
                }
            }
            yield return null;
        }
    }

    void DrawRect(Vector2 startLocalPoint)
    {        
        Vector3 endMousePos = Input.mousePosition;
        endMousePos.y = Mathf.Clamp(endMousePos.y, 0, Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, endMousePos, _canvas.worldCamera, out Vector2 endLocalPoint);

        // Calculate the center and size based on the local points
        Vector3 center = (startLocalPoint + endLocalPoint) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(startLocalPoint.x - endLocalPoint.x), Mathf.Abs(startLocalPoint.y - endLocalPoint.y));
        
        // Update the position and size of the screenshotArea (which is the Panel's RectTransform)
        RectTransform panelRectTransform = screenshotArea.GetComponent<RectTransform>();

        if (panelRectTransform != null)
        {
            // Set position and size
            panelRectTransform.localPosition = center;
            panelRectTransform.sizeDelta = size;  // anchors의 min과 max의 x와 y가 각각 같아야 함
        }
        else
        {
            Debug.LogWarning("screenshotArea does not have a RectTransform.");
        }
    }

    public void SaveScreenshot()
    {
        StartCoroutine(SaveScreenshotCoroutine(false));
    }

    // 저장하고 이미지 보여주기
    public void SaveAndShowScreenshot()
    {
        StartCoroutine(SaveScreenshotCoroutine(true));
    }

    private IEnumerator SaveScreenshotCoroutine(bool showAfterSave)
    {
        // 유효성 검사
        if (!IsScreenshotAreaSet())
        {
            Debug.LogWarning("Please set the screenshot area first.");
            yield break;
        }
        
        // 전처리
        CaptureState state = PrepareCapture("[Screenshot]");
        
        try
        {
            // 렌더링 대기
            yield return WaitForCapture(state);
            
            // 좌표 계산
            var (x, y, width, height) = CalculateCaptureArea();
            Debug.Log($"capture: {x}/{y}/{width}/{height}");
            
            // 디렉토리 및 파일 경로 설정
            string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string filePath = Path.Combine(directory, "panel_capture.png");
            
            // 캡처 실행
            CaptureDesktopArea(x, y, width, height, filePath);
            
            Debug.Log($"Screenshot saved at {filePath}");
            
            if (showAfterSave)
            {
                ShowScreenshotImage();
            }
        }
        finally
        {
            // 후처리 - 예외 발생 여부와 관계없이 반드시 실행
            CleanupCapture(state);
        }
    }

    private void CaptureDesktopArea(int x, int y, int width, int height, string filePath)
    {
        // Get the desktop window and its DC
        IntPtr desktopHwnd = GetDesktopWindow();
        IntPtr desktopDC = GetWindowDC(desktopHwnd);
        IntPtr memoryDC = CreateCompatibleDC(desktopDC);

        // Create a compatible bitmap
        IntPtr bitmap = CreateCompatibleBitmap(desktopDC, width, height);
        IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

        // Copy the desktop content to the memory DC (starting from x, y with size width, height)
        BitBlt(memoryDC, 0, 0, width, height, desktopDC, x, y, SRCCOPY);

        // Create a Bitmap object from the handle
        using (System.Drawing.Bitmap bmp = System.Drawing.Bitmap.FromHbitmap(bitmap))
        {
            // Save the Bitmap as PNG
            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        // Clean up
        SelectObject(memoryDC, oldBitmap);
        DeleteObject(bitmap);
        DeleteDC(memoryDC);
        ReleaseDC(desktopHwnd, desktopDC);
    }

    // 메모리에 바로 캡처 (파일 저장 없이)
    public IEnumerator CaptureScreenshotToMemory(System.Action<byte[]> callback)
    {
        // 유효성 검사
        if (!IsScreenshotAreaSet())
        {
            Debug.LogWarning("Screenshot area not set.");
            callback?.Invoke(null);
            yield break;
        }
        
        // 전처리
        CaptureState state = PrepareCapture("[Screenshot Memory]");
        
        byte[] imageBytes = null;
        try
        {
            // 렌더링 대기
            yield return WaitForCapture(state);
            
            // 좌표 계산
            var (x, y, width, height) = CalculateCaptureArea();
            Debug.Log($"capture to memory: {x}/{y}/{width}/{height}");
            
            // 메모리에 캡처
            imageBytes = CaptureDesktopAreaToMemory(x, y, width, height);
        }
        finally
        {
            // 후처리 - 예외 발생 여부와 관계없이 반드시 실행
            CleanupCapture(state);
        }
        
        // 콜백 호출
        callback?.Invoke(imageBytes);
    }

    // 메모리에 캡처 + 영역 정보 반환 (OCR용)
    public IEnumerator CaptureScreenshotToMemoryWithInfo(System.Action<byte[], int, int, int, int> callback)
    {
        // 유효성 검사
        if (!IsScreenshotAreaSet())
        {
            Debug.LogWarning("Screenshot area not set.");
            callback?.Invoke(null, 0, 0, 0, 0);
            yield break;
        }
        
        // 전처리
        CaptureState state = PrepareCapture("[OCR Screenshot]");
        
        byte[] imageBytes = null;
        int x = 0, y = 0, width = 0, height = 0;
        
        try
        {
            // 렌더링 대기
            yield return WaitForCapture(state);
            
            // 좌표 계산
            (x, y, width, height) = CalculateCaptureArea();
            Debug.Log($"[OCR] Capture area: x={x}, y={y}, width={width}, height={height}");
            
            // 메모리에 캡처
            imageBytes = CaptureDesktopAreaToMemory(x, y, width, height);
        }
        finally
        {
            // 후처리 - 예외 발생 여부와 관계없이 반드시 실행
            CleanupCapture(state);
        }
        
        // 이미지 바이트 + 영역 정보 반환
        callback?.Invoke(imageBytes, x, y, width, height);
    }

    // 메모리에 직접 캡처하는 메서드
    private byte[] CaptureDesktopAreaToMemory(int x, int y, int width, int height)
    {
        IntPtr desktopHwnd = GetDesktopWindow();
        IntPtr desktopDC = GetWindowDC(desktopHwnd);
        IntPtr memoryDC = CreateCompatibleDC(desktopDC);
        IntPtr bitmap = CreateCompatibleBitmap(desktopDC, width, height);
        IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

        BitBlt(memoryDC, 0, 0, width, height, desktopDC, x, y, SRCCOPY);

        byte[] imageBytes = null;
        using (System.Drawing.Bitmap bmp = System.Drawing.Bitmap.FromHbitmap(bitmap))
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                imageBytes = ms.ToArray();
            }
        }

        // Clean up
        SelectObject(memoryDC, oldBitmap);
        DeleteObject(bitmap);
        DeleteDC(memoryDC);
        ReleaseDC(desktopHwnd, desktopDC);

        return imageBytes;
    }

    // 전체 화면 캡처 (Hybrid 방식 적용)
    public IEnumerator CaptureFullScreenToMemory(System.Action<byte[]> callback)
    {
        // 전처리
        CaptureState state = PrepareCapture("[Screenshot FullScreen]");
        
        byte[] imageBytes = null;
        
        try
        {
            // 렌더링 대기
            yield return WaitForCapture(state);
            
            // 전체 화면 캡처
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);
            Debug.Log($"[Screenshot FullScreen] Capturing primary monitor: {width}x{height}");
            
            imageBytes = CaptureDesktopAreaToMemory(0, 0, width, height);
        }
        finally
        {
            // 후처리 - 예외 발생 여부와 관계없이 반드시 실행
            CleanupCapture(state);
        }
        
        // 콜백 호출
        callback?.Invoke(imageBytes);
    }

    // Function to set the transparency of a panel
    private void SetPanelAlpha(GameObject panel, float alpha)
    {
        CanvasRenderer canvasRenderer = panel.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            canvasRenderer.SetAlpha(alpha);
        }
        else
        {
            Debug.LogWarning("Panel is missing CanvasRenderer.");
        }
    }

    //  Unity Canvas 좌표를 Windows 좌표로
    // public Vector2 ConvertUnityPosToWinpos(Vector2 localPos)
    // {
    //     Debug.Log("ConvertUnityPosToWinpos Start ");
    //     Debug.Log(localPos);
    //     Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
    //         _canvas.worldCamera, 
    //         // _canvas.GetComponent<RectTransform>().TransformPoint(localPos)
    //         localPos
    //     );
    //     Debug.Log("ConvertUnityPosToWinpos End ");
    //     Debug.Log(screenPoint);
    //     return screenPoint;
    // }

    public Vector2 ConvertUnityPosToWinpos(Vector2 localPoint)
    {
        Debug.Log("ConvertUnityPosToWinpos Start");
        Debug.Log(localPoint);

        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

        // Local 좌표를 World 좌표로 변환
        Vector3 worldPoint = canvasRect.TransformPoint(localPoint);

        // World 좌표를 Screen 좌표로 변환
        // Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, worldPoint);
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        Debug.Log("ConvertUnityPosToWinpos End");
        Debug.Log(screenPoint);

        return screenPoint;
    }

    private GameObject screenshotUI; // 동적으로 생성할 UI 오브젝트
    private Coroutine destroyCoroutine; // 코루틴 참조 저장
    public Font customFont; // 외부에서 설정할 폰트

    public void ShowScreenshotImage()
    {
        // 기존 코루틴이 있으면 중지
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            if (screenshotUI != null)
            {
                Destroy(screenshotUI);
            }
        }

        // 저장 경로 설정
        string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
        string filePath = Path.Combine(directory, "panel_capture.png");

        // 파일 존재 여부 확인
        if (File.Exists(filePath))
        {
            // PNG 파일을 바이트 배열로 읽기
            byte[] bytes = File.ReadAllBytes(filePath);

            // 텍스처 생성
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);

            ShowImageTexture(texture, "ScreenshotImage");
        }
        else
        {
            Debug.LogWarning("Screenshot 파일을 찾을 수 없습니다: " + filePath);
            ShowNoImageUI();
        }
    }

    // 클립보드 이미지 표시
    public void ShowClipboardImage()
    {
        // 기존 코루틴이 있으면 중지
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            if (screenshotUI != null)
            {
                Destroy(screenshotUI);
            }
        }

        // 클립보드에서 이미지 byte[] 가져오기
        byte[] imageBytes = ClipboardManager.Instance?.GetImageBytesFromClipboard();
        
        if (imageBytes != null && imageBytes.Length > 0)
        {
            // 텍스처 생성
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);

            ShowImageTexture(texture, "ClipboardImage");
        }
        else
        {
            Debug.LogWarning("Clipboard에 이미지가 없습니다.");
            ShowNoImageUI();
        }
    }

    // 이미지 텍스처를 화면에 표시하는 공통 메서드
    private void ShowImageTexture(Texture2D texture, string objectName)
    {
        // Canvas가 없으면 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 새로운 UI 오브젝트 생성
        screenshotUI = new GameObject(objectName);
        screenshotUI.transform.SetParent(canvas.transform, false);

        // Image 컴포넌트 추가
        Image image = screenshotUI.AddComponent<Image>();

        // Sprite 생성
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        image.sprite = sprite;

        // RectTransform 설정 (이미지 크기로 설정하고 중앙에 배치)
        RectTransform rectTransform = screenshotUI.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        // 5초 후 자동 파괴
        destroyCoroutine = StartCoroutine(DestroyAfterDelay(5f));
    }

    // 5초 후 UI 파괴 코루틴
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (screenshotUI != null)
        {
            Destroy(screenshotUI);
            screenshotUI = null;
        }
        destroyCoroutine = null;
    }

    // 이미지 없을 때 붉은색 투명 상자와 텍스트 표시
    private void ShowNoImageUI()
    {
        // Canvas가 없으면 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 새로운 UI 오브젝트 생성
        screenshotUI = new GameObject("NoImageUI");
        screenshotUI.transform.SetParent(canvas.transform, false);

        // Image 컴포넌트 추가 (붉은색 투명 30% 상자)
        Image image = screenshotUI.AddComponent<Image>();
        image.color = new Color(1f, 0f, 0f, 0.3f); // 붉은색, 투명도 30%

        // RectTransform 설정 (크기를 600x300, 중앙 배치)
        RectTransform rectTransform = screenshotUI.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(600, 300);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        // 텍스트 오브젝트 생성
        GameObject textObj = new GameObject("NoImageText");
        textObj.transform.SetParent(screenshotUI.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = "No Image";
        text.font = customFont != null ? customFont : Resources.GetBuiltinResource<Font>("Arial.ttf"); // 지정된 폰트가 없으면 Arial 사용
        text.fontSize = 48;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter; // 위아래 중앙 정렬

        // 텍스트 RectTransform 설정
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(600, 300); // 상자와 동일한 크기
        textRect.anchoredPosition = Vector2.zero;

        // 5초 후 자동 파괴
        destroyCoroutine = StartCoroutine(DestroyAfterDelay(5f));
    }

    // 스크립트가 파괴될 때 UI 정리
    private void OnDestroy()
    {
        if (screenshotUI != null)
        {
            Destroy(screenshotUI);
        }
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
        }
    }
}