using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class TMPInputFieldMobileHandler : MonoBehaviour, IPointerDownHandler
{
    private TMP_InputField _inputField;

    private void Awake()
    {
        // 현재 객체에 있는 TMP_InputField 컴포넌트를 가져옵니다.
        _inputField = GetComponent<TMP_InputField>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default); // 소프트 키보드 열기
        // 모바일 환경에서만 작동하도록 조건 설정
        if (Application.isMobilePlatform)
        {
            // Input Field가 포커스되지 않았을 경우 활성화
            if (!_inputField.isFocused)
            {
                _inputField.ActivateInputField(); // 입력 필드 포커스
                TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default); // 소프트 키보드 열기
            }
        }
    }
}
