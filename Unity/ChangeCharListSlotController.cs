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
        // 1. 주소가 비어있으면 바로 Fallback 시도
        if (string.IsNullOrEmpty(address))
        {
            ApplyFallbackSprite();
            return;
        }

        try
        {
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                characterIcon.sprite = handle.Result;
            }
            else
            {
                Debug.LogWarning($"Failed to load sprite: {address}. Trying fallback...");
                ApplyFallbackSprite();
            }
        }
        catch
        {
            // 예외 발생 시(잘못된 주소 등)에도 Fallback 시도
            Debug.LogWarning($"Exception loading sprite: {address}. Trying fallback...");
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
            LoadAndChangeCharacter(charData.clothesList[0]);
        }
    }

    private async void LoadAndChangeCharacter(ChangeCharClothesInfo clothes)
    {
        if (string.IsNullOrEmpty(clothes.prefabAddress)) return;

        // 공용 2d_general
        if (clothes.prefabAddress == "2d_general")
        {
            await CharManager.Instance.ChangeCharacter2DGeneral(clothes);
            return;
        }

        // DLC 다운로드 필요 여부 체크
        try
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(clothes.prefabAddress);
            await sizeHandle.Task;
            long downloadSize = (sizeHandle.Status == AsyncOperationStatus.Succeeded) ? sizeHandle.Result : 0;
            Addressables.Release(sizeHandle);

            if (downloadSize > 0)
            {
                Debug.Log($"[DLC] 중앙 DownloadManager를 통해 다운로드 시도: {clothes.prefabAddress} ({downloadSize} bytes)");

                DownloadManager.Instance.RequestAddressableDownload(clothes.prefabAddress, downloadSize, (success) =>
                {
                    if (success)
                    {
                        Debug.Log($"[DLC] 다운로드 완료: {clothes.prefabAddress}");
                        // 스프라이트 갱신
                        LoadSpriteFromAddressable(clothes.spriteAddress);
                    }
                    else
                    {
                        Debug.LogWarning($"[DLC] 다운로드 취소 또는 실패: {clothes.prefabAddress}");
                    }
                });
                return; // 이번 클릭에서는 다운로드 창 띄우고 종료, 다음 클릭에서 실제로 변경됨
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DLC] 다운로드 체크 스킵: {clothes.prefabAddress} - {e.Message}");
        }

        // 일반 프리팹 교체 로직 (로컬 또는 이미 다운로드된 에셋)
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(clothes.prefabAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            CharManager.Instance.ChangeCharacterFromDLC(handle.Result);
        }
        else
        {
            Debug.LogError($"Failed to load character prefab from addressable: {clothes.prefabAddress}");
        }
    }
}
