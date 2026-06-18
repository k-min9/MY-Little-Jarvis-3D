using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChangeCharCardController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float CharacterDetailLongPressSeconds = 0.5f; // 롱프레스로 Detail 보이기
    private const float CharacterDetailDragCancelThreshold = 18f;

    [Header("UI References - Icon Area")]
    [SerializeField] private Image characterIcon;     // 아이콘 (sprite)
    
    [Header("UI References - Name Area")]
    [SerializeField] private TextMeshProUGUI nameText; // 캐릭터 이름 텍스트
    
    [Header("UI References - Favorite Button")]
    [SerializeField] private Button favoriteBtn;
    [SerializeField] private Image favoriteImage;     // 별 이미지
    
    [Header("UI References - Clothes Change Area")]
    [SerializeField] private Button changeLeftBtn;
    [SerializeField] private TextMeshProUGUI clothesText; // 의상 이름
    [SerializeField] private Button changeRightBtn;

    [Header("UI References - Pagination Dots")]
    [SerializeField] private GameObject dotSample;        // 복제할 원본 점 오브젝트
    [SerializeField] private Transform dotParent;         // 점들이 배치될 부모 컨테이너
    
    // 생성된 점들의 Image 컴포넌트를 담을 리스트
    private List<Image> dotImages = new List<Image>();

    // 현재 슬롯에 할당된 데이터 참조
    private ChangeCharInfo charData;
    public ChangeCharInfo CharData => charData;
    private int currentClothesIndex = 0;
    private Coroutine longPressCoroutine;
    private bool suppressClickAfterLongPress;
    private bool pointerPressed;
    private Vector2 pointerDownScreenPosition;

    private void Update()
    {
        if (!pointerPressed && longPressCoroutine == null && TryGetPrimaryPointerBegan(out Vector2 beganPosition) && IsScreenPointInsideSelf(beganPosition))
        {
            BeginLongPress(beganPosition, -1, "polling");
        }

        if (pointerPressed && !IsPrimaryPointerPressed())
        {
            pointerPressed = false;
            StopLongPressCoroutine("polling release");
        }
    }

    // Manager에서 슬롯 생성 후 데이터를 주입하는 초기화 함수
    public void InitSlot(ChangeCharInfo data)
    {
        charData = data;
        currentClothesIndex = 0; // 옷은 항상 첫 번째 (기본 패시브) 인덱스로 시작

        // 캐릭터 데이터 기본 셋팅
        nameText.text = charData.name;
        
        // 페이지네이션 점 생성
        GeneratePaginationDots();

        UpdateFavoriteUI();
        UpdateClothesUI();
    }

    // 의상 개수만큼 페이지네이션 점을 복제하고 원본은 숨기는 함수
    private void GeneratePaginationDots()
    {
        if (dotSample == null || dotParent == null) return;

        dotImages.Clear(); // 초기화

        int clothesCount = charData.clothesList.Count;

        for (int i = 0; i < clothesCount; i++)
        {
            GameObject newDot = Instantiate(dotSample, dotParent);
            newDot.SetActive(true);
            
            Image dotImage = newDot.GetComponent<Image>();
            if (dotImage != null)
            {
                dotImages.Add(dotImage);
            }
        }

        // 인스턴스화 완료 후 원본 샘플 비활성화
        dotSample.SetActive(false);
    }
    
    // UI 버튼 - 왼쪽 화살표
    public void OnClickChangeLeft()
    {
        // 인덱스 감소 (0 미만이면 마지막 인덱스로 루프)
        currentClothesIndex--;
        if (currentClothesIndex < 0)
        {
            currentClothesIndex = charData.clothesList.Count - 1;
        }

        UpdateClothesUI();
    }
    
    // UI 버튼 - 오른쪽 화살표
    public void OnClickChangeRight()
    {
        // 인덱스 증가 (마지막을 넘어서면 0으로 루프)
        currentClothesIndex++;
        if (currentClothesIndex >= charData.clothesList.Count)
        {
            currentClothesIndex = 0;
        }

        UpdateClothesUI();
    }

    // UI 버튼 - 즐겨찾기 별모양
    public void OnClickFavorite()
    {
        // 데이터 상태 반전 (데이터 참조를 공유하므로 1번만 뒤집으면 됨)
        charData.isFavorite = !charData.isFavorite;

        // 저장후 UI 갱신
        ChangeCharManager.Instance.SaveFavorites();
        ChangeCharManager.Instance.RefreshAllSlotsFavoriteUI();
    }

    // 의상 인덱스에 맞춰서 아이콘과 텍스트 업데이트
    private async void UpdateClothesUI()
    {
        ChangeCharClothesInfo currentClothes = charData.clothesList[currentClothesIndex];

        clothesText.text = currentClothes.text;
        UpdatePaginationDotsUI();

        // 선택 가능 여부에 따라 아이콘 노출 및 상호작용 제어
        if (characterIcon != null)
        {
            characterIcon.gameObject.SetActive(true);
        }

        Button slotButton = GetComponent<Button>();
        if (slotButton != null)
        {
            if (currentClothes.isSelectable)
            {
                // 선택 가능 시 버튼 활성화
                slotButton.interactable = true;
            }
            else
            {
                // 선택 불가능 시 버튼 비활성화
                slotButton.interactable = false;
            }
        }

        if (currentClothes.isLocal)
        {
            Sprite localSprite = ChangeCharManager.Instance.GetLocalSprite(currentClothes.spriteAddress);
            if (localSprite != null)
            {
                characterIcon.sprite = localSprite;
            }
            else
            {
                ApplyFallbackSprite();
            }
            return;
        }

        if (string.IsNullOrEmpty(currentClothes.spriteAddress))
        {
            ApplyFallbackSprite();
            return;
        }

        // 다운로드된 경우만 로드, 미다운로드면 null → fallback
        Sprite sprite = await AddressableManager.Instance.LoadIfExist<Sprite>(currentClothes.spriteAddress);
        if (sprite != null)
        {
            characterIcon.sprite = sprite;
        }
        else
        {
            ApplyFallbackSprite();
        }
    }

    private void ApplyFallbackSprite()
    {
        if (ChangeCharManager.Instance.fallbackSprite != null)
        {
            characterIcon.sprite = ChangeCharManager.Instance.fallbackSprite;
        }
        else
        {
            Debug.LogError("CRITICAL: Fallback sprite is missing in ChangeCharManager!");
        }
    }

    // 페이지네이션 점 색상 업데이트 로직
    private void UpdatePaginationDotsUI()
    {
        for (int i = 0; i < dotImages.Count; i++)
        {
            if (i == currentClothesIndex)
            {
                // 현재 선택된 의상 인덱스일 때 (노란색)
                dotImages[i].color = new Color32(255, 255, 0, 255);
            }
            else
            {
                // 선택되지 않은 나머지 인덱스일 때 (회색)
                dotImages[i].color = new Color32(180, 180, 180, 255);
            }
        }
    }

    // 즐겨찾기 데이터 상태에 맞춰서 별 이미지 업데이트 (Manager에서도 호출할 수 있게 public 개방)
    public void UpdateFavoriteUI()
    {
        // 최상단 널 체크: 복제용 원본(Sample)처럼 데이터가 주입되지 않은 빈 슬롯은 갱신 패스
        if (charData == null || favoriteImage == null) return;

        // 삼항 연산자 대신 명시적인 if-else 사용
        if (charData.isFavorite)
        {
            // on 일때
            favoriteImage.color = new Color32(255, 255, 0, 255);
        }
        else
        {
            // off 일때
            favoriteImage.color = new Color32(180, 180, 180, 255);
        }
    }

    // 캐릭터 (의상) 최종 변경 적용
    public async void ChangeChar()
    {
        if (suppressClickAfterLongPress)
        {
            suppressClickAfterLongPress = false;
            return;
        }

        ChangeCharClothesInfo currentClothes = charData.clothesList[currentClothesIndex];

        // 선택 가능 여부 체크
        if (!currentClothes.isSelectable)
        {
            // 선택할 수 없는 의상이면 변경하지 않음
            return;
        }

        if (string.IsNullOrEmpty(currentClothes.prefabAddress)) return;

        // 공용 2d_general
        if (currentClothes.prefabAddress == "2d_general")
        {
            // 2d_general DLC 에셋(애니메이터)이 미다운로드 상태면 먼저 다운로드
            if (!currentClothes.isLocal && !string.IsNullOrEmpty(currentClothes.animatorControllerAddress))
            {
                // 다운로드 포함 로드 → 캐시 확보 후 Inject에서 재사용됨
                var ac = await AddressableManager.Instance.LoadWithDownloadableAsync<RuntimeAnimatorController>(currentClothes.animatorControllerAddress);
                if (ac == null)
                {
                    Debug.LogWarning($"[DLC] 2d_general 에셋 다운로드 취소: {currentClothes.animatorControllerAddress}");
                    return; // 다운로드 취소 시 캐릭터 변경 중단
                }
            }

            await CharManager.Instance.ChangeCharacter2DGeneral(currentClothes);
            UpdateClothesUI();
            return;
        }

        if (currentClothes.isLocal)
        {
            GameObject localPrefab = ChangeCharManager.Instance.GetLocalPrefab(currentClothes.prefabAddress);
            if (localPrefab == null)
            {
                Debug.LogWarning($"[LocalChar] 변경 실패: {currentClothes.prefabAddress}");
                return;
            }

            CharManager.Instance.ChangeCharacterFromGameObject(localPrefab);
            UpdateClothesUI();
            return;
        }

        // 없으면 다운로드, 있으면 바로 로드
        AddressableManager.Instance.LoadWithDownloadable<GameObject>(currentClothes.prefabAddress, (success, prefab) =>
        {
            if (success)
            {
                CharManager.Instance.ChangeCharacterFromDLC(prefab);
                UpdateClothesUI(); // 다운로드 완료 후 스프라이트 갱신
            }
            else
            {
                Debug.LogWarning($"[DLC] 다운로드 취소 또는 실패: {currentClothes.prefabAddress}");
            }
        });
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            Debug.Log($"[CharacterDetailLongPress][Card] Ignore non-left pointer down. button={eventData.button}");
            return;
        }

        BeginLongPress(eventData.position, eventData.pointerId, "event");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerPressed = false;
        Debug.Log($"[CharacterDetailLongPress][Card] PointerUp name={name} char={GetDebugCharName()} pos={eventData.position}");
        StopLongPressCoroutine("pointer up");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[CharacterDetailLongPress][Card] PointerExit name={name} char={GetDebugCharName()} pos={eventData.position}");
    }

    private void BeginLongPress(Vector2 screenPosition, int pointerId, string source)
    {
        pointerPressed = true;
        pointerDownScreenPosition = screenPosition;
        Debug.Log($"[CharacterDetailLongPress][Card] PointerDown source={source} name={name} char={GetDebugCharName()} pos={pointerDownScreenPosition}");

        StopLongPressCoroutine("restart");
        longPressCoroutine = StartCoroutine(LongPressRoutine(pointerId));
    }

    private IEnumerator LongPressRoutine(int pointerId)
    {
        float elapsed = 0f;
        while (elapsed < CharacterDetailLongPressSeconds)
        {
            if (!pointerPressed)
            {
                Debug.Log($"[CharacterDetailLongPress][Card] Canceled before threshold. reason=pointer released char={GetDebugCharName()}");
                longPressCoroutine = null;
                yield break;
            }

            if (IsPointerMovedBeyondThreshold())
            {
                Debug.Log($"[CharacterDetailLongPress][Card] Canceled before threshold. reason=drag/move char={GetDebugCharName()}");
                longPressCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log($"[CharacterDetailLongPress][Card] Threshold reached pointerId={pointerId} char={GetDebugCharName()}");
        longPressCoroutine = null;
        suppressClickAfterLongPress = true;
        pointerPressed = false;
        ShowCharacterDetail();
    }

    private void ShowCharacterDetail()
    {
        if (charData == null || UIManager.Instance == null)
        {
            Debug.LogWarning($"[CharacterDetailLongPress][Card] Show skipped. charDataNull={charData == null} uiManagerNull={UIManager.Instance == null}");
            return;
        }

        ChangeCharClothesInfo currentClothes = null;
        if (charData.clothesList != null && charData.clothesList.Count > 0)
        {
            int index = Mathf.Clamp(currentClothesIndex, 0, charData.clothesList.Count - 1);
            currentClothes = charData.clothesList[index];
        }

        Debug.Log($"[CharacterDetailLongPress][Card] ShowCharacterDetail char={GetDebugCharName()} clothes={currentClothes?.text}");
        UIManager.Instance.ShowCharacterDetail(charData, currentClothes);
    }

    private bool IsPointerMovedBeyondThreshold()
    {
        Vector2 currentPosition = pointerDownScreenPosition;
        if (TryGetPrimaryPointerPosition(out Vector2 pointerPosition))
        {
            currentPosition = pointerPosition;
        }

        return Vector2.Distance(pointerDownScreenPosition, currentPosition) > CharacterDetailDragCancelThreshold;
    }

    private bool TryGetPrimaryPointerBegan(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            position = touch.position;
            return touch.phase == TouchPhase.Began;
        }

        position = Input.mousePosition;
        return Input.GetMouseButtonDown(0);
    }

    private bool TryGetPrimaryPointerPosition(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        position = Input.mousePosition;
        return true;
    }

    private bool IsPrimaryPointerPressed()
    {
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase != TouchPhase.Ended && phase != TouchPhase.Canceled;
        }

        return Input.GetMouseButton(0);
    }

    private bool IsScreenPointInsideSelf(Vector2 screenPosition)
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return false;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
    }

    private string GetDebugCharName()
    {
        return charData != null ? charData.name : "null";
    }

    private void StopLongPressCoroutine(string reason = "restart")
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
            Debug.Log($"[CharacterDetailLongPress][Card] Coroutine stopped. reason={reason} char={GetDebugCharName()}");
        }
    }
}
