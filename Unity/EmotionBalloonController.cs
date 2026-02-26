using UnityEngine;

// Emotion Balloon Manager에서 추가한 Balloon을 직접 추가.
public class EmotionBalloonController : MonoBehaviour
{
    private RectTransform balloonRect;
    private CapsuleCollider targetCollider;  // 3D 캐릭터용
    public RectTransform targetPortraitTransform;  // UI Portrait용 (Manager에서 주입)
    private float destroyTime;
    
    // Target별 말풍선 관리를 위한 참조
    private GameObject targetObject;

    public void Initialize(GameObject target, float duration)
    {
        balloonRect = GetComponent<RectTransform>();
        destroyTime = Time.time + duration;

        // targetPortraitTransform이 설정되어 있으면 3D target 검사 생략
        if (targetPortraitTransform != null)
        {
            Debug.Log("[EmotionBalloon] Using Inspector-set targetPortraitTransform");
            UpdateBalloonPosition();
            return;
        }

        // 3D GameObject인 경우 (기존 로직)
        GameObject collider = target.transform.Find("Collider")?.gameObject;
        // 없을경우 Collider 본체로 생각
        if (collider == null)
        {
            collider = target;
        }
        
        targetCollider = collider.GetComponent<CapsuleCollider>();
        if (targetCollider == null)
        {
            Debug.LogWarning("대상 오브젝트에 CapsuleCollider가 없습니다.");
            // Destroy(gameObject);
            return;
        }

        UpdateBalloonPosition(); // 초기 위치 설정
    }

    // Target 참조 설정 (SetEmotionBalloonForTarget에서 호출)
    public void SetTarget(GameObject target)
    {
        targetObject = target;
    }

    private void Update()
    {
        if (balloonRect == null) return;
        if (targetPortraitTransform == null && targetCollider == null) return;

        UpdateBalloonPosition();

        // 지정된 시간이 지나면 자동 삭제
        if (Time.time >= destroyTime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateBalloonPosition()
    {
        if (balloonRect == null) return;
        if (targetPortraitTransform == null && targetCollider == null) return;

        Canvas _canvas = FindObjectOfType<Canvas>();
        
        if (targetPortraitTransform != null)
        {
            // Inspector/Manager에서 설정된 UI target 사용
            UpdateBalloonPositionForUI(_canvas, targetPortraitTransform);
        }
        else
        {
            // 3D target: CapsuleCollider 사용
            UpdateBalloonPositionFor3D(_canvas);
        }
    }

    // UI Portrait용 위치 계산 (PortraitBalloonSimpleManager 패턴 참조)
    private void UpdateBalloonPositionForUI(Canvas _canvas, RectTransform targetRect)
    {
        // Operator 모드 UI용 anchor/pivot 설정 (왼쪽 상단 기준)
        balloonRect.anchorMin = new Vector2(0, 1);
        balloonRect.anchorMax = new Vector2(0, 1);
        balloonRect.pivot = new Vector2(0, 0.5f);

        Vector2 charPos = targetRect.anchoredPosition;
        float width = targetRect.sizeDelta.x * targetRect.localScale.x;

        balloonRect.anchoredPosition = new Vector2(
            charPos.x + 240,  // + width + 50,
            charPos.y + 200  // * SettingManager.Instance.settings.char_size / 100f + 100
        );
    }

    // 3D GameObject용 위치 계산 (기존 로직)
    private void UpdateBalloonPositionFor3D(Canvas _canvas)
    {
        Vector3 worldHeadPos = GetHeadPosition();  // 머리 끝 부분 좌표
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldHeadPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, screenPoint, _canvas.worldCamera, out Vector2 pos);
        balloonRect.localPosition = new Vector3(pos.x + 40, pos.y + 10, -250);
    }

    private Vector3 GetHeadPosition()
    {
        Bounds bounds = targetCollider.bounds;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    // 말풍선 파괴 시 자동으로 Dictionary에서 제거
    private void OnDestroy()
    {
        if (targetObject != null && EmotionBalloonManager.Instance != null)
        {
            EmotionBalloonManager.Instance.RemoveBalloonFromMap(targetObject);
        }
    }
}
