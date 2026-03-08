using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OCRAutoMapSlotController : MonoBehaviour
{
    public int slotKey;

    public TMP_InputField leftInputField;
    public TMP_Dropdown rightDropdown;

    public GameObject activeOnVisual;
    public GameObject activeOffVisual;

    private OCRAutoMapManager manager;
    private List<string> actorIdByIndex = new List<string>();

    // 슬롯 초기화 진입점입니다. manager, slotKey, 슬롯 데이터, 드롭다운 캐시를 받아 UI에 반영합니다.
    public void Init(
        OCRAutoMapManager owner,
        int key,
        OCRAutoMapSlotInfo info,
        List<string> dropdownOptions,
        List<string> dropdownActorIds
    )
    {
        manager = owner;
        slotKey = key;

        actorIdByIndex = new List<string>(dropdownActorIds);

        leftInputField.SetTextWithoutNotify(info.ocrText);

        rightDropdown.ClearOptions();
        rightDropdown.AddOptions(new List<string>(dropdownOptions));

        int idx = FindDropdownIndexByActorId(info.actorId);
        rightDropdown.SetValueWithoutNotify(idx);

        ApplyActiveVisual(info.isActive);
    }

    // OCR 결과 등으로 InputField를 강제 세팅할 때 사용합니다. 이벤트 연쇄를 막기 위해 Notify 없는 세팅을 사용합니다.
    public void SetInputTextWithoutNotify(string text)
    {
        leftInputField.SetTextWithoutNotify(text);
    }

    // 슬롯 isActive 상태에 따른 시각 표현을 적용합니다.
    public void ApplyActiveVisual(bool isActive)
    {
        if (activeOnVisual != null)
        {
            activeOnVisual.SetActive(isActive);
        }

        if (activeOffVisual != null)
        {
            activeOffVisual.SetActive(!isActive);
        }
    }

    // InputField OnValueChanged에 연결합니다. 텍스트 변경을 manager에 전달하여 저장 버튼을 활성화합니다.
    public void OnInputChanged(string newText)
    {
        manager.UpdateSlotText(slotKey, newText);
    }

    // Dropdown OnValueChanged에 연결합니다. index를 actorId로 변환해 manager에 전달하여 저장 버튼을 활성화합니다.
    public void OnDropdownChanged(int optionIndex)
    {
        string actorId = GetActorIdByIndex(optionIndex);
        manager.UpdateSlotActorId(slotKey, actorId);
    }

    // 돋보기 버튼 OnClick에 연결합니다. 해당 슬롯에 대한 OCR 요청을 manager에 위임합니다.
    public void OnClickSearch()
    {
        manager.RequestOcrForSlot(slotKey);
    }

    // 슬롯 외곽 버튼 OnClick에 연결합니다. isActive 토글을 manager에 위임합니다.
    public void OnClickToggleActive()
    {
        manager.ToggleSlotActive(slotKey);
    }

    // Delete 버튼 OnClick에 연결합니다. 해당 슬롯 삭제를 manager에 위임합니다.
    public void OnClickDelete()
    {
        manager.DeleteSlot(slotKey);
    }

    // actorIdByIndex에서 actorId에 해당하는 index를 반환합니다.
    private int FindDropdownIndexByActorId(string actorId)
    {
        for (int i = 0; i < actorIdByIndex.Count; i++)
        {
            if (actorIdByIndex[i] == actorId)
            {
                return i;
            }
        }

        return 0;
    }

    // Dropdown index를 actorId로 변환합니다.
    private string GetActorIdByIndex(int idx)
    {
        if (idx < 0)
        {
            return "Auto";
        }

        if (idx >= actorIdByIndex.Count)
        {
            return "Auto";
        }

        return actorIdByIndex[idx];
    }
}
