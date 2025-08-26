using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; 

// Slot 형태
/*
UserCardItemSlot
ㄴTextFrame
  ㄴChat(Text)
  ㄴInputField
ㄴIcon
  ㄴEraseButton
  ㄴFixButton
*/
public class UIUserCardSlotController : MonoBehaviour
{
    public int userCardIdx;  // 카드 식별용 인덱스
    private bool isActive = true;  // 현재 해당 Card text가 활성화 되어있는지 여부

    [Header("UI Elements")]
    public Text userCardText;  // 일반 출력용 텍스트
    public TMP_InputField userCardInputField;  // 편집용 입력 필드
    public Button eraseButton;
    public Button fixButton;
    public Image textFrameImage;  // TextFrame의 Image 컴포넌트
    public Button textFrameButton;  // 클릭 감지용 버튼


    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (userCardText == null)
            userCardText = transform.Find("TextFrame/Chat").GetComponent<Text>();

        if (userCardInputField == null)
            userCardInputField = transform.Find("TextFrame/InputField").GetComponent<TMP_InputField>();

        if (eraseButton == null)
            eraseButton = transform.Find("Icon/EraseButton").GetComponent<Button>();

        if (fixButton == null)
            fixButton = transform.Find("Icon/FixButton").GetComponent<Button>();

        if (textFrameImage == null)
            textFrameImage = transform.Find("TextFrame").GetComponent<Image>();

        if (textFrameButton == null)
            textFrameButton = transform.Find("TextFrame").GetComponent<Button>();

        // 버튼 클릭 이벤트 연결
        eraseButton.onClick.AddListener(OnClickEraseButton);
        fixButton.onClick.AddListener(OnClickFixButton);
        textFrameButton.onClick.AddListener(ToggleActiveState);

        // 초기에는 입력 필드 비활성화
        userCardInputField.gameObject.SetActive(false);
        userCardInputField.lineType = TMP_InputField.LineType.MultiLineNewline; // 줄바꿈 허용
        userCardInputField.onSubmit.AddListener(OnSubmitInputField);
        userCardInputField.onEndEdit.AddListener(OnEndEditInputField);

        userCardInputField.onValueChanged.AddListener((_) => AdjustInputFieldHeight());
    }

    private void AdjustInputFieldHeight()
    {
        // preferredHeight를 기반으로 직접 조절
        float preferredHeight = userCardInputField.textComponent.preferredHeight;
        RectTransform inputRect = userCardInputField.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(inputRect.sizeDelta.x, preferredHeight + 20f); // padding 여유

        // TextFrame도 함께 늘려야 할 경우
        RectTransform frameRect = textFrameImage.GetComponent<RectTransform>();
        frameRect.sizeDelta = new Vector2(frameRect.sizeDelta.x, preferredHeight + 40f); // 더 넉넉하게
    }


    private void OnSubmitInputField(string submittedText)
    {
        FinalizeInputEdit();
    }

    private void Update()
    {
        if (userCardInputField != null && userCardInputField.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    // Shift+Enter 줄바꿈
                    int pos = userCardInputField.caretPosition;
                    userCardInputField.text = userCardInputField.text.Insert(pos, "\n");
                    userCardInputField.caretPosition = pos + 1;
                }
                else
                {
                    // Enter 단독 → 입력 종료 (줄바꿈 X)
                    EventSystem.current.SetSelectedGameObject(null);  // 포커스 강제 해제
                    FinalizeInputEdit();
                }
            }
        }
    }


    // 카드 정보 설정
    public void SetUserCardInfo(int idx, string text, bool active = true)
    {
        userCardIdx = idx;
        userCardText.text = text;
        userCardInputField.text = text;

        isActive = active;
        ApplyActiveStateToVisual();
    }

    private void ToggleActiveState()
    {
        isActive = !isActive;

        // 매니저에 전달
        UIUserCardManager.Instance.SetCardActive(userCardIdx, isActive);

        ApplyActiveStateToVisual();
    }

    private void ApplyActiveStateToVisual()
    {
        textFrameImage.color = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);  // 밝기 조정
    }

    // 삭제 버튼 클릭 시
    private void OnClickEraseButton()
    {
        Debug.Log("OnClickEraseButton");
        UIUserCardManager.Instance.RemoveUserCard(this);
    }


    // 수정 버튼 클릭 시
    private void OnClickFixButton()
    {
        userCardInputField.gameObject.SetActive(true);
        userCardText.gameObject.SetActive(false);

        // 포커스 확보
        userCardInputField.ActivateInputField();
        userCardInputField.Select();
    }

    // 입력 필드 포커스 해제 시 자동 저장
    private void OnEndEditInputField(string newText)
    {
        FinalizeInputEdit();
    }

    private void FinalizeInputEdit()
    {
        string finalText = userCardInputField.text;

        userCardText.text = finalText;
        userCardInputField.gameObject.SetActive(false);
        userCardText.gameObject.SetActive(true);

        UIUserCardManager.Instance.UpdateUserCard(userCardIdx, finalText);
    }

    // UI 제거만 수행 (데이터는 매니저에서 삭제)
    public void deleteUserCardInfo()
    {
        Destroy(this.gameObject);
    }
}
