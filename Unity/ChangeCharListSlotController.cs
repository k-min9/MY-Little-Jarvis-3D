using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChangeCharListSlotController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image characterIcon;     // 캐릭터 아이콘 (현재 입고 있는 의상 기준)
    [SerializeField] private TextMeshProUGUI nameText; // 캐릭터 이름 텍스트
    
    [Header("Favorite")]
    [SerializeField] private Button favoriteBtn;
    [SerializeField] private Image favoriteImage;     // 별 이미지

    // 현재 슬롯에 할당된 데이터 참조
    private ChangeCharInfo charData;
    public ChangeCharInfo CharData => charData;

    // Manager에서 슬롯 생성 후 데이터를 주입하는 초기화 함수
    public void InitSlot(ChangeCharInfo data)
    {
        charData = data;

        // 이름 셋팅
            nameText.text = charData.name;

        // 아이콘은 현재 저장된(혹은 첫 번째) 의상 스프라이트를 사용
        // 리스트에서는 의상을 안 바꾸므로 첫 번째 기본 의상을 보여준다고 가정
        if (charData.clothesList.Count > 0)
        {
            LoadSpriteFromAddressable(charData.clothesList[0].spriteAddress);
        }

        UpdateFavoriteUI();
    }

    private async void LoadSpriteFromAddressable(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            ApplyFallbackSprite();
            return;
        }

        // 다운로드된 경우만 로드, 미다운로드면 null → fallback
        Sprite sprite = await AddressableManager.Instance.LoadIfExist<Sprite>(address);
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
        if (charData == null) return;

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
        // 리스트 슬롯에서는 의상 변경 기능 없이 기본(첫 번째) 의상만 사용하므로 Index 0으로 고정
        if (charData != null && charData.clothesList.Count > 0)
        {
            LoadAndChangeCharacter(charData.clothesList[0]);
        }
    }

    private async void LoadAndChangeCharacter(ChangeCharClothesInfo clothes)
    {
        if (string.IsNullOrEmpty(clothes.prefabAddress)) return;

        // 공용 2d_general
        if (clothes.prefabAddress == "2d_general")
        {
            // 2d_general DLC 에셋(애니메이터)이 미다운로드 상태면 먼저 다운로드
            if (!string.IsNullOrEmpty(clothes.animatorControllerAddress))
            {
                var ac = await AddressableManager.Instance.LoadWithDownloadableAsync<RuntimeAnimatorController>(clothes.animatorControllerAddress);
                if (ac == null)
                {
                    Debug.LogWarning($"[DLC] 2d_general 에셋 다운로드 취소: {clothes.animatorControllerAddress}");
                    return; // 다운로드 취소 시 변경 중단
                }
            }

            await CharManager.Instance.ChangeCharacter2DGeneral(clothes);
            LoadSpriteFromAddressable(clothes.spriteAddress);
            return;
        }

        // 없으면 다운로드, 있으면 바로 로드
        AddressableManager.Instance.LoadWithDownloadable<GameObject>(clothes.prefabAddress, (success, prefab) =>
        {
            if (success)
            {
                CharManager.Instance.ChangeCharacterFromDLC(prefab);
                LoadSpriteFromAddressable(clothes.spriteAddress); // 다운로드 완료 후 스프라이트 갱신
            }
            else
            {
                Debug.LogWarning($"[DLC] 다운로드 취소 또는 실패: {clothes.prefabAddress}");
            }
        });
    }
}
