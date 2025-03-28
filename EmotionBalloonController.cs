using UnityEngine;

// Emotion Balloon Manager에서 추가한 Balloon을 직접 추가.
public class EmotionBalloonController : MonoBehaviour
{
    private RectTransform balloonRect;
    private CapsuleCollider targetCollider;
    private float destroyTime;

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

        
        Vector3 worldHeadPos = GetHeadPosition();  // 머리 끝 부분 좌표
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldHeadPos);  // 월드 좌표 → UI 스크린 좌표

        balloonRect.anchoredPosition = new Vector2(screenPoint.x+40, screenPoint.y+10);
    }

    private Vector3 GetHeadPosition()
    {
        Bounds bounds = targetCollider.bounds;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }
}
