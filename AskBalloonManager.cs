using System.Collections;
using UnityEngine;
using TMPro;

public class AskBalloonManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; // askBalloon 이미지
    [SerializeField] private GameObject askBalloon; // askBalloon 이미지
    [SerializeField] public RectTransform characterTransform; // askBalloon이 표시될 캐릭터의 Transform
    [SerializeField] private RectTransform askBalloonTransform; // askBalloon의 Transform
    [SerializeField] public TMP_InputField inputField; 

    // 싱글톤 인스턴스
    private static AskBalloonManager instance;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        HideAskBalloon(); // 시작 시 askBalloon 숨기기
    }

    // 상태 갱신 로직
    private void Update()
    {
        if (StatusManager.Instance.IsAsking && askBalloon.activeSelf)  // activeSelf : 활성화 여부
        {
            UpdateAskBalloonPosition();
        }

        if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsAnswering)
        {
            HideAskBalloon();
        }
    }


    // 싱글톤 인스턴스에 접근하는 속성
    public static AskBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AskBalloonManager>();
            }
            return instance;
        }
    }

    // askBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowAskBalloon()
    {
        askBalloon.SetActive(true);
        inputField.text = string.Empty; // 텍스트 초기화
        StatusManager.Instance.IsAsking = true; // StatusManager 상태 업데이트
        UpdateAskBalloonPosition();  // askBalloon 위치 조정하기

        // 기존 애니메이션 중지 및 isAsking 애니메이션 
    }

    // // askBalloon의 텍스트를 수정하고 오디오를 재생하는 함수
    // public void ModifyaskBalloonText(string text)
    // {
    //     answerText.text = text; // 텍스트 변경
    //     // 높이 조정
    //     float textHeight = askBalloonText.preferredHeight;
    //     askBalloonTransform.sizeDelta = new Vector2(askBalloonTransform.sizeDelta.x, textHeight + 60);
    // }
    
    // askBalloon을 숨기는 함수
    public void HideAskBalloon()
    {
        askBalloon.SetActive(false);
        StatusManager.Instance.IsAsking = false; 
    }

    // askBalloon의 위치를 캐릭터 바로 위로 조정하는 함수
    private void UpdateAskBalloonPosition()
    {
        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // 캐릭터의 X 위치와 동일하게 설정
        askBalloonTransform.anchoredPosition = new Vector2(charPosition.x, charPosition.y + 270 * SettingManager.Instance.settings.char_size / 100f); // Y축 창크기 270만큼
    }
}
