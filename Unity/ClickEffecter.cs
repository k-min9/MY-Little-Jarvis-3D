using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 클릭 시 파티클 VFX를 재생하는 싱글톤 컴포넌트.
// GameManager 등 전역 오브젝트에 붙여 사용.
// Update에서 마우스 입력을 감지하며, TransparentWindow의 픽셀 검사를 통해 허공 클릭을 걸러냄.
public class ClickEffecter : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static ClickEffecter instance;
    public static ClickEffecter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ClickEffecter>();
            }
            return instance;
        }
    }

    [Header("FX 설정")]
    [Tooltip("좌클릭 시 재생할 파티클 프리팹 (transform scale 40/40/40 기본값)")]
    [SerializeField] private GameObject fxPrefabLeft;  // 좌클릭 파티클 프리팹

    [Tooltip("우클릭 시 재생할 파티클 프리팹 (현재는 좌클릭과 동일 프리팹 사용)")]
    [SerializeField] private GameObject fxPrefabRight;  // 우클릭 파티클 프리팹

    [Tooltip("중앙클릭 시 재생할 파티클 프리팹 (현재는 좌클릭과 동일 프리팹 사용)")]
    [SerializeField] private GameObject fxPrefabMiddle;  // 중앙클릭 파티클 프리팹

    [Tooltip("FX를 배치할 Canvas. Screen Space Overlay Canvas를 연결.")]
    [SerializeField] private Canvas targetCanvas;  // FX 부모가 될 Canvas

    [Header("옵션")]
    [Tooltip("드래그 중일 때 FX를 억제")]
    [SerializeField] private bool suppressOnDrag = true;  // 드래그 중 FX 억제 여부

    [Tooltip("동시 최대 FX 수. 초과 시 가장 오래된 것 제거.")]
    [SerializeField] private int maxConcurrentFx = 5;  // 동시 재생 한도

    private Queue<GameObject> _activeFxQueue = new Queue<GameObject>();  // 현재 활성 FX 목록

    // 매 프레임 마우스 클릭 감지
    private void Update()
    {
        // 드래그 억제 옵션 체크
        if (suppressOnDrag && StatusManager.Instance.IsDragging)
        {
            return;
        }

        // Canvas 미할당이면 최상위 canvas 사용
        if (targetCanvas == null)
        {
            targetCanvas = CanvasManager.Instance.canvasUI;
        }

        // 클릭이 없는 프레임은 조기 리턴
        bool isAnyButtonDown = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        if (!isAnyButtonDown)
        {
            return;
        }

        // TransparentWindow를 통해 마우스 커서 아래가 불투명한지 확인 (허공 클릭 방지)
        // basicRule.md: 싱글톤 Instance는 null 체크를 하지 않음
        if (!TransparentWindow.Instance.IsOnOpaquePixel)
        {
            return;
        }

        // 좌클릭
        if (Input.GetMouseButtonDown(0))
        {
            if (fxPrefabLeft != null)
            {
                SpawnFx(Input.mousePosition, fxPrefabLeft);
            }
        }
        // 우클릭
        else if (Input.GetMouseButtonDown(1))
        {
            if (fxPrefabRight != null)
            {
                SpawnFx(Input.mousePosition, fxPrefabRight);
            }
        }
        // 중앙클릭
        else if (Input.GetMouseButtonDown(2))
        {
            if (fxPrefabMiddle != null)
            {
                SpawnFx(Input.mousePosition, fxPrefabMiddle);
            }
        }
    }

    // 지정 프리팹으로 클릭 스크린 좌표에 FX 생성
    private void SpawnFx(Vector2 screenPos, GameObject prefab)
    {
        // 동시 재생 한도 초과 시 가장 오래된 FX 제거
        while (_activeFxQueue.Count >= maxConcurrentFx)
        {
            GameObject old = _activeFxQueue.Dequeue();
            if (old != null)
            {
                Destroy(old);
            }
        }

        // 스크린 좌표 → Canvas RectTransform 로컬 좌표 변환
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            targetCanvas.worldCamera,
            out Vector2 localPoint
        );

        // Canvas 하위에 FX 생성 (RectTransform 로컬 좌표로 배치)
        GameObject fxInstance = Instantiate(prefab, canvasRect);
        
        // 하이어라키 맨 아래로 보내어 UI 요소들 중 가장 마지막에 렌더링 : 이펙트간 순서를 위해 남겨두기
        fxInstance.transform.SetAsLastSibling();

        // 위치 조정 : Z축을 억지로 당길 필요 없이 0으로 설정
        RectTransform fxRect = fxInstance.GetComponent<RectTransform>();
        if (fxRect != null)
        {
            // RectTransform이 있는 프리팹이면 로컬 위치로 배치
            fxRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        }
        else
        {
            // RectTransform이 없는 프리팹이면 월드 좌표로 배치
            fxInstance.transform.position = canvasRect.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0f));
        }

        // 데이터 추가
        _activeFxQueue.Enqueue(fxInstance);

        // 파티클 재생 완료 후 자동 Destroy
        ParticleSystem ps = fxInstance.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(DestroyAfterSeconds(fxInstance, lifetime));
        }
        else
        {
            // ParticleSystem 없으면 2초 후 기본 제거
            StartCoroutine(DestroyAfterSeconds(fxInstance, 2f));
            Debug.LogWarning("[ClickEffecter] fxPrefab에 ParticleSystem이 없습니다. 2초 후 자동 제거합니다.");
        }
    }

    // 지정 시간 후 FX 오브젝트 Destroy
    private IEnumerator DestroyAfterSeconds(GameObject target, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (target != null)
        {
            Destroy(target);
        }
    }

    // 씬 전환 등으로 파괴될 때 남은 FX 정리
    private void OnDestroy()
    {
        foreach (GameObject fx in _activeFxQueue)
        {
            if (fx != null)
            {
                Destroy(fx);
            }
        }
        _activeFxQueue.Clear();
    }
}
