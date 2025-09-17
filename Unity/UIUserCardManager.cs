// 스크립트 위치 : UIUserCardManager에서 UIUserCardSlotController를 통해 제어
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEditor;
using Newtonsoft.Json;
using System.IO;

// Slot 형태
/*
UserCard
ㄴViewport
  ㄴUserCardItemSlot
*/

[Serializable]
public class UserCardInfo
{
    public string userCardText;
    public bool isActive = true;  // 기본값 true
}

public class UIUserCardManager : MonoBehaviour
{
    // Singleton instance
    private static UIUserCardManager _instance;
    public static UIUserCardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIUserCardManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("UserCardManager");
                    _instance = obj.AddComponent<UIUserCardManager>();
                }
            }
            return _instance;
        }
    }

    public GameObject uiUserCardSlotSample;  // 복사할 Slot 견본 (비활성화)
    public Transform uiUserCardSlotParent;  // Slot 복사 위치
    public GameObject saveButton;            // setActive로 활성화/비활성화 제어

    private List<UserCardInfo> userCardList = new List<UserCardInfo>();  // 메모리용 리스트
    private int nextCardIndex = 0;  // 카드 인덱스 관리

    private void Start()
    {
        // sample UI 비활성화
        uiUserCardSlotSample.SetActive(false);
        SetSaveButtonState(false);  // 초기화 시 버튼 비활성화

        // Test 코드 (기존 UI 제거있으니 별도 운용할 것)
        // TestUserCard();
        // InitUserCard();

        // 카드 정보 로드
        LoadUserCardInfo();
    }

    // 테스트 Debug 로그
    private void TestUserCard()
    {
        // 기존 UI 제거
        ClearUserCardInfo();

        // 리스트도 초기화
        // userCardList.Clear();

        // 테스트용 카드 추가 (저장하지 않음)
        userCardList.Add(new UserCardInfo { userCardText = "Teacher is man", isActive = true });
        userCardList.Add(new UserCardInfo { userCardText = "존댓말을 반드시 쓸 것.", isActive = true });
        userCardList.Add(new UserCardInfo { userCardText = "비활성화 된 카드\n가능성", isActive = false });
        userCardList.Add(new UserCardInfo { userCardText = "End your sentence with 'nyannyan'", isActive = true });

        // UI 슬롯 생성
        for (int i = 0; i < userCardList.Count; i++)
        {
            AddUserCardSlotFromInfo(i, userCardList[i]);
        }

        // 인덱스 업데이트 (삭제 후 추가 가능하도록)
        nextCardIndex = userCardList.Count;

        SetSaveButtonState(false);
        ScrollToBottom();
    }

    // Ui에 UserCard 불러오기
    public void LoadUserCardInfo()
    {
        Debug.Log("LoadUserCardInfo start");

        // 기존 채팅Slot 제거
        ClearUserCardInfo();

        // Memory 가져와서 Slot에 내용 추가
        userCardList = GetUserCardInfos();
        
        // userCardList가 비어있으면 자동으로 초기화
        InitUserCard();
        
        nextCardIndex = userCardList.Count;
        Debug.Log("GetUserCardInfos end : " + nextCardIndex);

        for (int i = 0; i < userCardList.Count; i++)
        {
            AddUserCardSlotFromInfo(i, userCardList[i]);
        }

        SetSaveButtonState(false);  // 새로고침 시 저장버튼 off
        // 하단으로 스크롤
        ScrollToBottom();
    }

    // 최초 기동 시 기본 지침 초기화 (리스트가 비어있을 때만)
    public void InitUserCard()
    {
        // userCardList가 이미 있으면 초기화하지 않음
        if (userCardList.Count > 0)
        {
            Debug.Log("UserCardList already has " + userCardList.Count + " cards, skipping initialization");
            return;
        }

        Debug.Log("UserCardList is empty, initializing default cards...");

        // 초기화
        string targetLang = SettingManager.Instance.settings.ui_language; // 0 : ko, 1 : jp, 2: en 
        
        // 활성화 지침
        userCardList.Add(new UserCardInfo { userCardText = LanguageData.Translate("답변은 반드시 3~4문장 정도 길이로만 짧게 답변.", targetLang), isActive = true });
        userCardList.Add(new UserCardInfo { userCardText = LanguageData.Translate("반드시 답변에 괄호를 넣거나 동작을 묘사하지 않음", targetLang), isActive = true });
        
        // 비활성화 지침
        userCardList.Add(new UserCardInfo { userCardText = LanguageData.Translate("모든 문장 끝에 \"~다냥\" 어미 사용", targetLang), isActive = false });
        userCardList.Add(new UserCardInfo { userCardText = LanguageData.Translate("답변할 때마다 관련된 고사성어나 속담 인용", targetLang), isActive = false });
        userCardList.Add(new UserCardInfo { userCardText = LanguageData.Translate("답변 시작을 항상 \"흠... 그렇다면\"으로 시작", targetLang), isActive = false });
        userCardList.Add(new UserCardInfo { userCardText = LanguageData.Translate("어떠한 경우에도 한국어를 유지해야 함", targetLang), isActive = false });
        
        // 인덱스 설정
        nextCardIndex = userCardList.Count;
        
        // JSON으로 저장
        SaveUserCardInfosToJson();
        
        Debug.Log("InitUserCard completed with " + userCardList.Count + " cards");
    }

    // Slot UI를 하나 불러오기
    private void AddUserCardSlotFromInfo(int idx, UserCardInfo cardInfo)
    {
        GameObject slotObj = Instantiate(uiUserCardSlotSample, uiUserCardSlotParent);
        slotObj.SetActive(true);

        var controller = slotObj.GetComponent<UIUserCardSlotController>();
        controller.SetUserCardInfo(idx, cardInfo.userCardText, cardInfo.isActive);
    }

    // 특정 카드 내용 업데이트
    public void UpdateUserCard(int idx, string newText)
    {
        if (idx >= 0 && idx < userCardList.Count)
        {
            userCardList[idx].userCardText = newText;
            SetSaveButtonState(true);  // 수정 시 저장 필요
        }
    }

    // 특정 카드 삭제 요청
    public void RemoveUserCard(UIUserCardSlotController target)
    {
        int idx = target.userCardIdx;

        if (idx >= 0 && idx < userCardList.Count)
        {
            userCardList.RemoveAt(idx);
            target.deleteUserCardInfo();

            // Slot 다시 정렬
            ReloadAllCards();

            SetSaveButtonState(true);  // 삭제 시 저장 필요
        }

        // 인덱스 갱신
        nextCardIndex = userCardList.Count;
    }

    // 특정카드 활성화 변경 (UIUserCardController에서 호출)
    public void SetCardActive(int idx, bool isActive)
    {
        if (idx >= 0 && idx < userCardList.Count)
        {
            userCardList[idx].isActive = isActive;
            SetSaveButtonState(true);  // 변경사항 발생
        }
    }

    // 전체 카드 다시 불러오기 (삭제 후 재정렬용)
    private void ReloadAllCards()
    {
        ClearUserCardInfo();

        for (int i = 0; i < userCardList.Count; i++)
        {
            AddUserCardSlotFromInfo(i, userCardList[i]);
        }

        ScrollToBottom();
    }

    // 현재 카드 리스트를 JSON으로 저장
    public void SaveUserCardInfosToJson()
    {
        string fileName = GetFileName();
        string json = JsonConvert.SerializeObject(userCardList, Formatting.Indented);
        File.WriteAllText(fileName, json);

        SetSaveButtonState(false);  // 저장 완료 시 저장버튼 비활성화
    }

    // conversation_stream에서 쓰일 list-json
    public string GetGuidelineListJson()
    {
        List<string> activeGuidelines = new List<string>();
        foreach (var card in userCardList)
        {
            if (card.isActive && !string.IsNullOrWhiteSpace(card.userCardText))
            {
                activeGuidelines.Add(card.userCardText.Trim());
            }
        }

        return JsonConvert.SerializeObject(activeGuidelines);
    }

    // 모든 대화 불러오기
    public List<UserCardInfo> GetUserCardInfos()
    {
        string fileName = GetFileName();
        if (!File.Exists(fileName))
        {
            return new List<UserCardInfo>();
        }

        string json = File.ReadAllText(fileName);
        return JsonConvert.DeserializeObject<List<UserCardInfo>>(json) ?? new List<UserCardInfo>();
    }

    // 기존 채팅 기록 모두 제거
    public void ClearUserCardInfo()
    {
        // uiUserCardSlotSample은 제외하고 모든 자식 오브젝트 삭제
        foreach (Transform child in uiUserCardSlotParent)
        {
            if (child.gameObject != uiUserCardSlotSample)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // 저장 파일 이름 구성
    private string GetFileName()
    {
        string nickname = "_A";
        string filename = "user_card" + nickname + ".json";

        return Path.Combine(Application.persistentDataPath, filename);
    }

    // 스크롤뷰를 최하단으로 이동
    private void ScrollToBottom()
    {
        if (uiUserCardSlotParent.parent.GetComponent<ScrollRect>() != null)
        {
            ScrollRect scrollRect = uiUserCardSlotParent.parent.GetComponent<ScrollRect>();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    // 새로운 카드 항목 추가 (UserCard 추천시 승인 받고 바로 text 추가 된 형태로 사용)
    public void AddUserCard(string text)
    {
        // 새 항목 리스트에 추가
        UserCardInfo newCard = new UserCardInfo
        {
            userCardText = text,
            isActive = true  // 기본적으로 활성화 상태
        };
        userCardList.Add(newCard);

        // 저장
        SaveUserCardInfosToJson();

        // Slot UI 생성
        AddUserCardSlotFromInfo(userCardList.Count - 1, newCard);

        // 인덱스 갱신
        nextCardIndex = userCardList.Count;

        SetSaveButtonState(true);  // 추가 시 저장 필요

        // 하단으로 스크롤
        ScrollToBottom();
    }

    // 빈 카드 슬롯 UI 추가 (버튼 클릭 시 사용)
    public void AddUserCardSlot()
    {
        // 리스트에 추가
        UserCardInfo newCard = new UserCardInfo
        {
            userCardText = "",
            isActive = true
        };
        userCardList.Add(newCard);

        // Slot 생성
        AddUserCardSlotFromInfo(nextCardIndex, newCard);

        // 인덱스 증가
        nextCardIndex++;

        // 저장
        SaveUserCardInfosToJson();

        SetSaveButtonState(true);  // 추가 시 저장 필요

        // 하단으로 스크롤
        ScrollToBottom();
    }

    // 저장 버튼 On/Off
    public void SetSaveButtonState(bool isActive)
    {
        if (saveButton != null)
            saveButton.SetActive(isActive);
    }
}
