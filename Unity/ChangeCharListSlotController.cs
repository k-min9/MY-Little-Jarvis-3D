using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        if (string.IsNullOrEmpty(address)) return;

        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            characterIcon.sprite = handle.Result;
        }
        else
        {
            Debug.LogError($"Failed to load sprite from addressable: {address}");
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

        // TODO: List 쪽 즐겨찾기 기능은 나중에 추가 예정. (favoriteImage 연결 전 널 에러 방지용으로 감쌈)
        if (favoriteImage != null)
        {
            // if-else 명시적 사용
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
            ChangeCharClothesInfo currentClothes = charData.clothesList[0];
            LoadAndChangeCharacter(currentClothes.prefabAddress);
        }
    }

    private async void LoadAndChangeCharacter(string address)
    {
        if (string.IsNullOrEmpty(address)) return;

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 의존성이 있거나 복잡한 구동방식 때문에, 인스턴스화 하지 않고 CharManager로 원본 프리팹만 전달하도록 가정
            // (만약 기존에도 Asset 참조를 넘겼다면 결과값을 그대로 전달)
            CharManager.Instance.ChangeCharacterFromGameObject(handle.Result);
        }
        else
        {
             Debug.LogError($"Failed to load character prefab from addressable: {address}");
        }
    }
}
