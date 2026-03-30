using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ChangeCharCardController : MonoBehaviour
{
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
    private int currentClothesIndex = 0;

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

        // 점(Dot) 색상 업데이트 호출
        UpdatePaginationDotsUI();

        if (string.IsNullOrEmpty(currentClothes.spriteAddress))
        {
            ApplyFallbackSprite();
            return;
        }

        // 프리팹 다운로드 여부 먼저 확인: 미다운로드면 fallback
        if (!string.IsNullOrEmpty(currentClothes.prefabAddress) && currentClothes.prefabAddress != "2d_general")
        {
            try
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync(currentClothes.prefabAddress);
                await sizeHandle.Task;
                long pendingSize = (sizeHandle.Status == AsyncOperationStatus.Succeeded) ? sizeHandle.Result : 0;
                Addressables.Release(sizeHandle);

                if (pendingSize > 0)
                {
                    Debug.Log($"[DLC] 프리팹 미다운로드 ({pendingSize} bytes) → fallback 표시: {currentClothes.prefabAddress}");
                    ApplyFallbackSprite();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DLC] 다운로드 사이즈 체크 실패, 스프라이트 로드 시도: {e.Message}");
            }
        }

        // 프리팹이 다운로드된 경우에만 실제 스프라이트 로드
        try
        {
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(currentClothes.spriteAddress);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                characterIcon.sprite = handle.Result;
                // 실제 로드 출처 확인 (번들 파일 경로 = 로컬/리모트 판별)
                var locs = Addressables.LoadResourceLocationsAsync(currentClothes.spriteAddress);
                await locs.Task;
                if (locs.Result != null && locs.Result.Count > 0)
                {
                    var loc = locs.Result[0];
                    string assetId  = loc.InternalId;
                    string bundleId = (loc.Dependencies != null && loc.Dependencies.Count > 0)
                                       ? loc.Dependencies[0].InternalId : "no_deps";
                    bool isRemote   = bundleId.StartsWith("http");
                    Debug.Log($"[DLC] 스프라이트 로드 성공: {currentClothes.spriteAddress}\n" +
                              $"  그룹: {(isRemote ? "REMOTE(DLC) ☁" : "LOCAL 📦")}\n" +
                              $"  에셋ID: {assetId}\n" +
                              $"  번들: {bundleId}");
                }
                Addressables.Release(locs);
            }
            else
            {
                Debug.LogWarning($"Failed to load sprite from addressable: {currentClothes.spriteAddress}. Trying fallback...");
                ApplyFallbackSprite();
            }
        }
        catch
        {
            Debug.LogWarning($"Exception loading sprite: {currentClothes.spriteAddress}. Trying fallback...");
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
        ChangeCharClothesInfo currentClothes = charData.clothesList[currentClothesIndex];
        
        if (string.IsNullOrEmpty(currentClothes.prefabAddress)) return;

        // 공용 2d_general
        if (currentClothes.prefabAddress == "2d_general")
        {
            await CharManager.Instance.ChangeCharacter2DGeneral(currentClothes);
            return;
        }

        // DLC 다운로드 필요 여부 체크
        try
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(currentClothes.prefabAddress);
            await sizeHandle.Task;
            long downloadSize = (sizeHandle.Status == AsyncOperationStatus.Succeeded) ? sizeHandle.Result : 0;
            Addressables.Release(sizeHandle);

            if (downloadSize > 0)
            {
                Debug.Log($"[DLC] 중앙 DownloadManager를 통해 다운로드 시도: {currentClothes.prefabAddress} ({downloadSize} bytes)");

                DownloadManager.Instance.RequestAddressableDownload(currentClothes.prefabAddress, downloadSize, (success) =>
                {
                    if (success)
                    {
                        Debug.Log($"[DLC] 다운로드 완료: {currentClothes.prefabAddress}");
                        // UI 갱신 (스프라이트 표시)
                        UpdateClothesUI();
                    }
                    else
                    {
                        Debug.LogWarning($"[DLC] 다운로드 취소 또는 실패: {currentClothes.prefabAddress}");
                    }
                });
                return; // 이번 클릭에서는 다운로드 창 띄우고 종료, 다음 클릭에서 실제로 변경됨
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DLC] 다운로드 체크 스킵: {currentClothes.prefabAddress} - {e.Message}");
        }

        // 일반 프리팹 교체 로직 (로컬 또는 이미 다운로드된 에셋)
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(currentClothes.prefabAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            CharManager.Instance.ChangeCharacterFromDLC(handle.Result);
        }
        else
        {
            Debug.LogError($"Failed to load character prefab from addressable: {currentClothes.prefabAddress}");
        }
    }
}
