using UnityEngine;

// Emotion Balloon Manager에서 추가한 Balloon을 직접 추가.
public class EmotionBalloonController : MonoBehaviour
{
    private RectTransform balloonRect;
    private CapsuleCollider targetCollider;
    private float destroyTime;
    
    // Target별 말풍선 관리를 위한 참조
    private GameObject targetObject;

    public void Initialize(GameObject target, float duration)
    {
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

        balloonRect = GetComponent<RectTransform>();
        destroyTime = Time.time + duration;

        UpdateBalloonPosition(); // 초기 위치 설정
    }

    // Target 참조 설정 (SetEmotionBalloonForTarget에서 호출)
    public void SetTarget(GameObject target)
    {
        targetObject = target;
    }

    private void Update()
    {
        if (targetCollider == null || balloonRect == null) return;

        UpdateBalloonPosition();

        // 지정된 시간이 지나면 자동 삭제
        if (Time.time >= destroyTime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateBalloonPosition()
    {
        if (targetCollider == null || balloonRect == null) return;

        Canvas _canvas = FindObjectOfType<Canvas>();
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
