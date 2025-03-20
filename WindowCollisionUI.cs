using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindowCollisionUI : MonoBehaviour
{
    private List<GameObject> activeRects = new List<GameObject>();
    private Canvas canvas;
    private GameObject redRectContainer;
    private bool isWindowsRectShowing = false;

    // UI 보여주고 싶을 경우 활성화
    private void Start()
    {
        // Canvas 찾기
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas를 찾을 수 없습니다! 씬에 Canvas가 있어야 합니다.");
            return;
        }

        if (isWindowsRectShowing) 
        {
            StartCoroutine(UpdateWindowRectsRoutine());
        }
    }

    public void ToggleWindowsRectUI() {
        isWindowsRectShowing = !isWindowsRectShowing;
        if (isWindowsRectShowing)
        {
            StartCoroutine(UpdateWindowRectsRoutine());
        }
    }

    private System.Collections.IEnumerator UpdateWindowRectsRoutine()
    {

        // 빈 GameObject 생성 (사각형들을 정리하기 위함)
        if (redRectContainer == null)
        {
            redRectContainer = new GameObject("RedRectangles");
            redRectContainer.transform.SetParent(canvas.transform, false);
        }

        while (isWindowsRectShowing)
        {
            UpdateWindowsUI();
            yield return new WaitForSeconds(0.25f); // 0.25초마다 업데이트
        }

        // 기존 UI 오브젝트 삭제
        foreach (var rect in activeRects)
        {
            Destroy(rect);
        }
        activeRects.Clear();
    }

    // 디버그용 상자 보여주기
    private void UpdateWindowsUI()
    {
        // 기존 UI 오브젝트 삭제
        foreach (var rect in activeRects)
        {
            Destroy(rect);
        }
        activeRects.Clear();

        if (WindowCollisionManager.Instance == null) return;

        // List<Rect> windows = WindowCollisionManager.Instance.GetAllWindowsRect();
        // List<Rect> windows = WindowCollisionManager.Instance.GetTestWindowsRect();
        List<Rect> windows = WindowCollisionManager.Instance.windowRects;

        foreach (Rect winRect in windows)
        {   
            // Debug.Log(winRect);
            CreateRedRectangle(winRect);
        }

        windows = WindowCollisionManager.Instance.windowAllRects;

        foreach (Rect winRect in windows)
        {   
            // Debug.Log(winRect);
            CreateGreyRectangle(winRect);
        }
    }

    private void CreateRedRectangle(Rect winRect)
    {
        GameObject rectObj = new GameObject("WindowRect");
        rectObj.transform.SetParent(redRectContainer.transform, false); // 부모를 redRectContainer로 설정

        RectTransform rectTransform = rectObj.AddComponent<RectTransform>();

        // Windows 좌표를 Canvas 좌표로 변환 : Windows 좌표의 좌상단과 우하단을 구함
        Vector2 start = WindowCollisionManager.Instance.ConvertWinposToUnityPos(new Vector2(winRect.x, winRect.y)); // 좌상단
        Vector2 end = WindowCollisionManager.Instance.ConvertWinposToUnityPos(new Vector2(winRect.x + winRect.width, winRect.y + winRect.height)); // 우하단
        // Debug.Log(start.x + "/" + start.y + "/" +end.x + "/" +end.y);

        // 변환된 좌표를 기준으로 RectTransform 크기와 위치 설정
        rectTransform.localPosition = (start + end) / 2f;
        rectTransform.sizeDelta = new Vector2(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));

        Image image = rectObj.AddComponent<Image>();
        image.color = new Color(1, 0, 0, 0.4f); // 반투명 빨간색 (RGBA)
        // image.color = new Color(0.15f, 0.15f, 0.15f, 0.4f); // 반투명 회색 (RGBA)

        // 이벤트 제거
        image.raycastTarget = false;  
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rectObj.AddComponent<CanvasGroup>(); // 없으면 CanvasGroup 추가
        }
        canvasGroup.blocksRaycasts = false; // 클릭 차단
        canvasGroup.interactable = false;  // 상호작용 비활성화
        canvasGroup.ignoreParentGroups = true;  // 부모 그룹 설정 무시
        rectObj.layer = LayerMask.NameToLayer("Ignore Raycast");  // 현재는 의미 없음

        activeRects.Add(rectObj);
    }

    private void CreateGreyRectangle(Rect winRect)
    {
        GameObject rectObj = new GameObject("WindowAllRect");
        rectObj.transform.SetParent(redRectContainer.transform, false); // 부모를 redRectContainer로 설정

        RectTransform rectTransform = rectObj.AddComponent<RectTransform>();

        // Windows 좌표를 Canvas 좌표로 변환 : Windows 좌표의 좌상단과 우하단을 구함
        Vector2 start = WindowCollisionManager.Instance.ConvertWinposToUnityPos(new Vector2(winRect.x, winRect.y)); // 좌상단
        Vector2 end = WindowCollisionManager.Instance.ConvertWinposToUnityPos(new Vector2(winRect.x + winRect.width, winRect.y + winRect.height)); // 우하단
        // Debug.Log(start.x + "/" + start.y + "/" +end.x + "/" +end.y);

        // 변환된 좌표를 기준으로 RectTransform 크기와 위치 설정
        rectTransform.localPosition = (start + end) / 2f;
        rectTransform.sizeDelta = new Vector2(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));

        Image image = rectObj.AddComponent<Image>();
        // image.color = new Color(1, 0, 0, 0.4f); // 반투명 빨간색 (RGBA)
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.15f); // 반투명 회색 (RGBA)

        // 이벤트 제거
        image.raycastTarget = false;  
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rectObj.AddComponent<CanvasGroup>(); // 없으면 CanvasGroup 추가
        }
        canvasGroup.blocksRaycasts = false; // 클릭 차단
        canvasGroup.interactable = false;  // 상호작용 비활성화
        canvasGroup.ignoreParentGroups = true;  // 부모 그룹 설정 무시
        rectObj.layer = LayerMask.NameToLayer("Ignore Raycast");  // 현재는 의미 없음

        activeRects.Add(rectObj);
    }

    // redRectContainer 하위의 모든 GameObject를 삭제하는 함수
    public void eraseAllRedRectrangles()
    {
        foreach (var rect in activeRects)
        {
            Destroy(rect);
        }
        activeRects.Clear();
    }

}
