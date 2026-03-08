using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        // 데이터 상태 반전
        charData.isFavorite = !charData.isFavorite;
        UpdateFavoriteUI();

        // [TODO] 실제 저장 로직 추가 (추후 구현)
    }

    // 의상 인덱스에 맞춰서 아이콘과 텍스트 업데이트
    private void UpdateClothesUI()
    {
        ChangeCharClothesInfo currentClothes = charData.clothesList[currentClothesIndex];
        
        clothesText.text = currentClothes.text;
        characterIcon.sprite = currentClothes.sprite;

        // 점(Dot) 색상 업데이트 호출
        UpdatePaginationDotsUI();
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

    // 즐겨찾기 데이터 상태에 맞춰서 별 이미지 업데이트
    private void UpdateFavoriteUI()
    {
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
    public void ChangeChar()
    {
        ChangeCharClothesInfo currentClothes = charData.clothesList[currentClothesIndex];
        CharManager.Instance.ChangeCharacterFromGameObject(currentClothes.prefab);
    }
}
