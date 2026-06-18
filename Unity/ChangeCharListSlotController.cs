using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ChangeCharListSlotController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float CharacterDetailLongPressSeconds = 0.5f; // 롱프레스로 Detail 보이기
    private const float CharacterDetailDragCancelThreshold = 18f;

    [Header("UI References")]
    [SerializeField] private Image characterIcon;     // 캐릭터 아이콘 (현재 입고 있는 의상 기준)
    [SerializeField] private TextMeshProUGUI nameText; // 캐릭터 이름 텍스트
    
    [Header("Favorite")]
    [SerializeField] private Button favoriteBtn;
    [SerializeField] private Image favoriteImage;     // 별 이미지

    // 현재 슬롯에 할당된 데이터 참조
    private ChangeCharInfo charData;
    public ChangeCharInfo CharData => charData;
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

    // 슬롯 초기화 및 데이터 주입
    public void InitSlot(ChangeCharInfo data)
    {
        charData = data;

        // 이름 셋팅
        nameText.text = charData.name;

        // 선택 가능 여부 확인
        bool isSelectable = false;
        if (charData.clothesList.Count > 0)
        {
            isSelectable = charData.clothesList[0].isSelectable;
        }

        // 아이콘 표시 및 상호작용 여부 제어
        if (isSelectable)
        {
            // 선택 가능 시 아이콘 활성화 및 이미지 로드
            if (characterIcon != null)
            {
                characterIcon.gameObject.SetActive(true);
            }

            if (charData.clothesList.Count > 0)
            {
                LoadSpriteForClothes(charData.clothesList[0]);
            }

            // 버튼 컴포넌트 활성화
            Button slotButton = GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.interactable = true;
            }
        }
        else
        {
            // 선택 불가능 시 아이콘 비활성화
            if (characterIcon != null)
            {
                characterIcon.gameObject.SetActive(false);
            }

            // 버튼 컴포넌트 비활성화
            Button slotButton = GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.interactable = false;
            }
        }

        UpdateFavoriteUI();
    }

    // 의상 스프라이트 로드
    private async void LoadSpriteForClothes(ChangeCharClothesInfo clothes)
    {
        if (clothes == null)
        {
            ApplyFallbackSprite();
            return;
        }

        if (clothes.isLocal)
        {
            Sprite localSprite = ChangeCharManager.Instance.GetLocalSprite(clothes.spriteAddress);
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

        if (string.IsNullOrEmpty(clothes.spriteAddress))
        {
            ApplyFallbackSprite();
            return;
        }

        // 다운로드된 경우만 로드, 미다운로드면 null → fallback
        Sprite sprite = await AddressableManager.Instance.LoadIfExist<Sprite>(clothes.spriteAddress);
        if (sprite != null)
        {
            characterIcon.sprite = sprite;
        }
        else
        {
            ApplyFallbackSprite();
        }
    }

    // Fallback 스프라이트 적용
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

    // UI 버튼 - 즐겨찾기 별모양
    public void OnClickFavorite()
    {
        // 데이터 상태 반전
        charData.isFavorite = !charData.isFavorite;

        // 저장 후 UI 갱신 (Manager 측에 위임)
        ChangeCharManager.Instance.SaveFavorites();
        ChangeCharManager.Instance.RefreshAllSlotsFavoriteUI();
    }

    // 즐겨찾기 데이터 상태에 맞춰서 별 이미지 업데이트
    public void UpdateFavoriteUI()
    {
        // 널 체크
        if (charData == null)
        {
            return;
        }

        // 인스펙터에 별 이미지가 잘 연결되어 있을 경우 UI 갱신
        if (favoriteImage != null)
        {
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
    }

    // 캐릭터 최종 변경 적용 (버튼 클릭 시 연결됨)
    public void ChangeChar()
    {
        if (suppressClickAfterLongPress)
        {
            suppressClickAfterLongPress = false;
            return;
        }

        // 리스트 슬롯에서는 의상 변경 기능 없이 기본(첫 번째) 의상만 사용하므로 Index 0으로 고정
        if (charData != null)
        {
            if (charData.clothesList.Count > 0)
            {
                ChangeCharClothesInfo defaultClothes = charData.clothesList[0];
                
                // 선택 가능 여부 확인
                if (defaultClothes.isSelectable)
                {
                    // 선택 가능 시 캐릭터 변경 적용
                    LoadAndChangeCharacter(defaultClothes);
                }
            }
        }
    }

    // 캐릭터 프리팹 로드 및 변경
    private async void LoadAndChangeCharacter(ChangeCharClothesInfo clothes)
    {
        if (string.IsNullOrEmpty(clothes.prefabAddress))
        {
            return;
        }

        // 공용 2d_general
        if (clothes.prefabAddress == "2d_general")
        {
            // 2d_general DLC 에셋(애니메이터)이 미다운로드 상태면 먼저 다운로드
            if (!clothes.isLocal && !string.IsNullOrEmpty(clothes.animatorControllerAddress))
            {
                var ac = await AddressableManager.Instance.LoadWithDownloadableAsync<RuntimeAnimatorController>(clothes.animatorControllerAddress);
                if (ac == null)
                {
                    Debug.LogWarning($"[DLC] 2d_general 에셋 다운로드 취소: {clothes.animatorControllerAddress}");
                    return; // 다운로드 취소 시 변경 중단
                }
            }

            await CharManager.Instance.ChangeCharacter2DGeneral(clothes);
            LoadSpriteForClothes(clothes);
            return;
        }

        if (clothes.isLocal)
        {
            GameObject localPrefab = ChangeCharManager.Instance.GetLocalPrefab(clothes.prefabAddress);
            if (localPrefab == null)
            {
                Debug.LogWarning($"[LocalChar] 변경 실패: {clothes.prefabAddress}");
                return;
            }

            CharManager.Instance.ChangeCharacterFromGameObject(localPrefab);
            LoadSpriteForClothes(clothes);
            return;
        }

        // 없으면 다운로드, 있으면 바로 로드
        AddressableManager.Instance.LoadWithDownloadable<GameObject>(clothes.prefabAddress, (success, prefab) =>
        {
            if (success)
            {
                CharManager.Instance.ChangeCharacterFromDLC(prefab);
                LoadSpriteForClothes(clothes); // 다운로드 완료 후 스프라이트 갱신
            }
            else
            {
                Debug.LogWarning($"[DLC] 다운로드 취소 또는 실패: {clothes.prefabAddress}");
            }
        });
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            Debug.Log($"[CharacterDetailLongPress][ListSlot] Ignore non-left pointer down. button={eventData.button}");
            return;
        }

        BeginLongPress(eventData.position, eventData.pointerId, "event");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerPressed = false;
        Debug.Log($"[CharacterDetailLongPress][ListSlot] PointerUp name={name} char={GetDebugCharName()} pos={eventData.position}");
        StopLongPressCoroutine("pointer up");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[CharacterDetailLongPress][ListSlot] PointerExit name={name} char={GetDebugCharName()} pos={eventData.position}");
    }

    private void BeginLongPress(Vector2 screenPosition, int pointerId, string source)
    {
        pointerPressed = true;
        pointerDownScreenPosition = screenPosition;
        Debug.Log($"[CharacterDetailLongPress][ListSlot] PointerDown source={source} name={name} char={GetDebugCharName()} pos={pointerDownScreenPosition}");

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
                Debug.Log($"[CharacterDetailLongPress][ListSlot] Canceled before threshold. reason=pointer released char={GetDebugCharName()}");
                longPressCoroutine = null;
                yield break;
            }

            if (IsPointerMovedBeyondThreshold())
            {
                Debug.Log($"[CharacterDetailLongPress][ListSlot] Canceled before threshold. reason=drag/move char={GetDebugCharName()}");
                longPressCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log($"[CharacterDetailLongPress][ListSlot] Threshold reached pointerId={pointerId} char={GetDebugCharName()}");
        longPressCoroutine = null;
        suppressClickAfterLongPress = true;
        pointerPressed = false;
        ShowCharacterDetail();
    }

    private void ShowCharacterDetail()
    {
        if (charData == null || UIManager.Instance == null)
        {
            Debug.LogWarning($"[CharacterDetailLongPress][ListSlot] Show skipped. charDataNull={charData == null} uiManagerNull={UIManager.Instance == null}");
            return;
        }

        ChangeCharClothesInfo defaultClothes = null;
        if (charData.clothesList != null && charData.clothesList.Count > 0)
        {
            defaultClothes = charData.clothesList[0];
        }

        Debug.Log($"[CharacterDetailLongPress][ListSlot] ShowCharacterDetail char={GetDebugCharName()} clothes={defaultClothes?.text}");
        UIManager.Instance.ShowCharacterDetail(charData, defaultClothes);
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
            Debug.Log($"[CharacterDetailLongPress][ListSlot] Coroutine stopped. reason={reason} char={GetDebugCharName()}");
        }
    }
}
