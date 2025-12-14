using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

// 핫키로 사용할 키/키 조합 목록
public enum HotKeyCatalog
{
    None = 0,
    F9,
    F10,
    F11,
    F12,
    Ctrl_1,
    Ctrl_2,
    Ctrl_3,
    Ctrl_4,
    Ctrl_5,
    Ctrl_6,
    Ctrl_7,
    Ctrl_8,
    Ctrl_9,
    Ctrl_0,
    BackQuote,  // ` 키
}

// 핫키 카탈로그 관리 및 드롭다운 처리
public class HotKeyCatalogManager : MonoBehaviour
{
    private static HotKeyCatalogManager instance;
    public static HotKeyCatalogManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HotKeyCatalogManager>();
            }
            return instance;
        }
    }

    [Header("Dropdown")]
    public TMP_Dropdown backQuoteDropdown;
    public TMP_Dropdown f9Dropdown;
    public TMP_Dropdown f10Dropdown;
    public TMP_Dropdown f11Dropdown;
    public TMP_Dropdown f12Dropdown;
    public TMP_Dropdown ctrl1Dropdown;
    public TMP_Dropdown ctrl2Dropdown;
    public TMP_Dropdown ctrl3Dropdown;
    public TMP_Dropdown ctrl4Dropdown;
    public TMP_Dropdown ctrl5Dropdown;
    public TMP_Dropdown ctrl6Dropdown;
    public TMP_Dropdown ctrl7Dropdown;
    public TMP_Dropdown ctrl8Dropdown;
    public TMP_Dropdown ctrl9Dropdown;
    public TMP_Dropdown ctrl0Dropdown;

    private string backQuoteSelectedAction = "ActionNone";
    private string f9SelectedAction = "ActionNone";
    private string f10SelectedAction = "ActionNone";
    private string f11SelectedAction = "ActionNone";
    private string f12SelectedAction = "ActionNone";
    private string ctrl1SelectedAction = "ActionNone";
    private string ctrl2SelectedAction = "ActionNone";
    private string ctrl3SelectedAction = "ActionNone";
    private string ctrl4SelectedAction = "ActionNone";
    private string ctrl5SelectedAction = "ActionNone";
    private string ctrl6SelectedAction = "ActionNone";
    private string ctrl7SelectedAction = "ActionNone";
    private string ctrl8SelectedAction = "ActionNone";
    private string ctrl9SelectedAction = "ActionNone";
    private string ctrl0SelectedAction = "ActionNone";

    void Start()
    {
        InitializeDropdowns();
        
        // HotKeyManager에서 로드된 핫키 데이터를 UI에 반영
        RefreshAllDropdowns();
    }

    // Unity 입력에서 현재 눌린 키를 HotKeyCatalog로 변환
    public HotKeyCatalog GetCatalogFromInput()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Ctrl + 숫자키 조합 (메인 키보드와 키패드 모두 체크)
        if (ctrl)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return HotKeyCatalog.Ctrl_1;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return HotKeyCatalog.Ctrl_2;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return HotKeyCatalog.Ctrl_3;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return HotKeyCatalog.Ctrl_4;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return HotKeyCatalog.Ctrl_5;
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) return HotKeyCatalog.Ctrl_6;
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) return HotKeyCatalog.Ctrl_7;
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) return HotKeyCatalog.Ctrl_8;
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) return HotKeyCatalog.Ctrl_9;
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) return HotKeyCatalog.Ctrl_0;
        }

        // 단일 키 (Ctrl 없이)
        if (!ctrl)
        {
            if (Input.GetKeyDown(KeyCode.F9)) return HotKeyCatalog.F9;
            if (Input.GetKeyDown(KeyCode.F10)) return HotKeyCatalog.F10;
            if (Input.GetKeyDown(KeyCode.F11)) return HotKeyCatalog.F11;
            if (Input.GetKeyDown(KeyCode.F12)) return HotKeyCatalog.F12;
            if (Input.GetKeyDown(KeyCode.BackQuote)) return HotKeyCatalog.BackQuote;
        }

        return HotKeyCatalog.None;
    }

    // Unity 입력에서 현재 떼어진 키를 HotKeyCatalog로 변환 (KeyUp 버전)
    public HotKeyCatalog GetCatalogFromInputUp()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Ctrl + 숫자키 조합 (메인 키보드와 키패드 모두 체크)
        if (ctrl)
        {
            if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Keypad1)) return HotKeyCatalog.Ctrl_1;
            if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Keypad2)) return HotKeyCatalog.Ctrl_2;
            if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.Keypad3)) return HotKeyCatalog.Ctrl_3;
            if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Keypad4)) return HotKeyCatalog.Ctrl_4;
            if (Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Keypad5)) return HotKeyCatalog.Ctrl_5;
            if (Input.GetKeyUp(KeyCode.Alpha6) || Input.GetKeyUp(KeyCode.Keypad6)) return HotKeyCatalog.Ctrl_6;
            if (Input.GetKeyUp(KeyCode.Alpha7) || Input.GetKeyUp(KeyCode.Keypad7)) return HotKeyCatalog.Ctrl_7;
            if (Input.GetKeyUp(KeyCode.Alpha8) || Input.GetKeyUp(KeyCode.Keypad8)) return HotKeyCatalog.Ctrl_8;
            if (Input.GetKeyUp(KeyCode.Alpha9) || Input.GetKeyUp(KeyCode.Keypad9)) return HotKeyCatalog.Ctrl_9;
            if (Input.GetKeyUp(KeyCode.Alpha0) || Input.GetKeyUp(KeyCode.Keypad0)) return HotKeyCatalog.Ctrl_0;
        }

        // 단일 키 (Ctrl 없이)
        if (!ctrl)
        {
            if (Input.GetKeyUp(KeyCode.F9)) return HotKeyCatalog.F9;
            if (Input.GetKeyUp(KeyCode.F10)) return HotKeyCatalog.F10;
            if (Input.GetKeyUp(KeyCode.F11)) return HotKeyCatalog.F11;
            if (Input.GetKeyUp(KeyCode.F12)) return HotKeyCatalog.F12;
            if (Input.GetKeyUp(KeyCode.BackQuote)) return HotKeyCatalog.BackQuote;
        }

        return HotKeyCatalog.None;
    }

    // HotKeyCatalog를 사람이 읽기 쉬운 문자열로 변환
    public string ToDisplayString(HotKeyCatalog catalog)
    {
        switch (catalog)
        {
            case HotKeyCatalog.Ctrl_1: return "Ctrl+1";
            case HotKeyCatalog.Ctrl_2: return "Ctrl+2";
            case HotKeyCatalog.Ctrl_3: return "Ctrl+3";
            case HotKeyCatalog.Ctrl_4: return "Ctrl+4";
            case HotKeyCatalog.Ctrl_5: return "Ctrl+5";
            case HotKeyCatalog.Ctrl_6: return "Ctrl+6";
            case HotKeyCatalog.Ctrl_7: return "Ctrl+7";
            case HotKeyCatalog.Ctrl_8: return "Ctrl+8";
            case HotKeyCatalog.Ctrl_9: return "Ctrl+9";
            case HotKeyCatalog.Ctrl_0: return "Ctrl+0";
            case HotKeyCatalog.BackQuote: return "`";
            default: return catalog.ToString();
        }
    }

    // Windows VK Code를 HotKeyCatalog로 변환 (전역 입력용)
    public HotKeyCatalog VKCodeToCatalog(int vkCode)
    {
        bool ctrl = IsCtrlPressed();

        // Ctrl 조합 키
        if (ctrl)
        {
            switch (vkCode)
            {
                case 0x31: return HotKeyCatalog.Ctrl_1; // '1'
                case 0x32: return HotKeyCatalog.Ctrl_2; // '2'
                case 0x33: return HotKeyCatalog.Ctrl_3; // '3'
                case 0x34: return HotKeyCatalog.Ctrl_4; // '4'
                case 0x35: return HotKeyCatalog.Ctrl_5; // '5'
                case 0x36: return HotKeyCatalog.Ctrl_6; // '6'
                case 0x37: return HotKeyCatalog.Ctrl_7; // '7'
                case 0x38: return HotKeyCatalog.Ctrl_8; // '8'
                case 0x39: return HotKeyCatalog.Ctrl_9; // '9'
                case 0x30: return HotKeyCatalog.Ctrl_0; // '0'
            }
        }

        // 단일 키 (Ctrl 없이)
        if (!ctrl)
        {
            switch (vkCode)
            {
                case 0x78: return HotKeyCatalog.F9;   // F9
                case 0x79: return HotKeyCatalog.F10;  // F10
                case 0x7A: return HotKeyCatalog.F11;  // F11
                case 0x7B: return HotKeyCatalog.F12;  // F12
                case 0xC0: return HotKeyCatalog.BackQuote; // ` (Grave accent)
            }
        }

        return HotKeyCatalog.None;
    }

#if UNITY_STANDALONE_WIN
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    // Ctrl 키 눌림 여부 확인
    private bool IsCtrlPressed()
    {
        const int VK_CONTROL = 0x11;
        const int VK_LCONTROL = 0xA2;
        const int VK_RCONTROL = 0xA3;
        
        return (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 ||
               (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0 ||
               (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
    }
#else
    // 에디터나 다른 플랫폼에서는 Unity Input 사용
    private bool IsCtrlPressed()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }
#endif


    // 드롭다운 초기화
    private void InitializeDropdowns()
    {
        if (backQuoteDropdown != null) SetupDropdown(backQuoteDropdown, OnBackQuoteDropdownChanged);
        if (f9Dropdown != null) SetupDropdown(f9Dropdown, OnF9DropdownChanged);
        if (f10Dropdown != null) SetupDropdown(f10Dropdown, OnF10DropdownChanged);
        if (f11Dropdown != null) SetupDropdown(f11Dropdown, OnF11DropdownChanged);
        if (f12Dropdown != null) SetupDropdown(f12Dropdown, OnF12DropdownChanged);
        if (ctrl1Dropdown != null) SetupDropdown(ctrl1Dropdown, OnCtrl1DropdownChanged);
        if (ctrl2Dropdown != null) SetupDropdown(ctrl2Dropdown, OnCtrl2DropdownChanged);
        if (ctrl3Dropdown != null) SetupDropdown(ctrl3Dropdown, OnCtrl3DropdownChanged);
        if (ctrl4Dropdown != null) SetupDropdown(ctrl4Dropdown, OnCtrl4DropdownChanged);
        if (ctrl5Dropdown != null) SetupDropdown(ctrl5Dropdown, OnCtrl5DropdownChanged);
        if (ctrl6Dropdown != null) SetupDropdown(ctrl6Dropdown, OnCtrl6DropdownChanged);
        if (ctrl7Dropdown != null) SetupDropdown(ctrl7Dropdown, OnCtrl7DropdownChanged);
        if (ctrl8Dropdown != null) SetupDropdown(ctrl8Dropdown, OnCtrl8DropdownChanged);
        if (ctrl9Dropdown != null) SetupDropdown(ctrl9Dropdown, OnCtrl9DropdownChanged);
        if (ctrl0Dropdown != null) SetupDropdown(ctrl0Dropdown, OnCtrl0DropdownChanged);
    }

    // 개별 드롭다운 설정
    private void SetupDropdown(TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> onValueChanged)
    {
        dropdown.ClearOptions();

        // HotKeyActionManager에서 표시 텍스트 리스트 가져오기
        string[] displayTexts = GetDisplayTextList();
        
        List<string> options = new List<string>(displayTexts);
        dropdown.AddOptions(options);

        // 기본값 None으로 설정
        int noneIndex = Array.IndexOf(displayTexts, "None");
        if (noneIndex >= 0)
        {
            dropdown.value = noneIndex;
        }

        dropdown.onValueChanged.AddListener(onValueChanged);
    }

    // 드롭다운 표시용 텍스트 리스트 가져오기
    private string[] GetDisplayTextList()
    {
        if (HotKeyActionManager.Instance != null)
        {
            return HotKeyActionManager.Instance.GetDisplayTexts();
        }
        return new string[] { "None" };
    }

    // backQuote 드롭다운 값 변경 이벤트
    private void OnBackQuoteDropdownChanged(int index) { HandleSingleDropdown(index, HotKeyCatalog.BackQuote, ref backQuoteSelectedAction, "BackQuote"); }

    // F9~F12 드롭다운 값 변경 이벤트
    private void OnF9DropdownChanged(int index) { HandleSingleDropdown(index, HotKeyCatalog.F9, ref f9SelectedAction, "F9"); }
    private void OnF10DropdownChanged(int index) { HandleSingleDropdown(index, HotKeyCatalog.F10, ref f10SelectedAction, "F10"); }
    private void OnF11DropdownChanged(int index) { HandleSingleDropdown(index, HotKeyCatalog.F11, ref f11SelectedAction, "F11"); }
    private void OnF12DropdownChanged(int index) { HandleSingleDropdown(index, HotKeyCatalog.F12, ref f12SelectedAction, "F12"); }

    // Ctrl+숫자 드롭다운 값 변경 이벤트
    private void OnCtrl1DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_1, ref ctrl1SelectedAction, "Ctrl+1"); }
    private void OnCtrl2DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_2, ref ctrl2SelectedAction, "Ctrl+2"); }
    private void OnCtrl3DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_3, ref ctrl3SelectedAction, "Ctrl+3"); }
    private void OnCtrl4DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_4, ref ctrl4SelectedAction, "Ctrl+4"); }
    private void OnCtrl5DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_5, ref ctrl5SelectedAction, "Ctrl+5"); }
    private void OnCtrl6DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_6, ref ctrl6SelectedAction, "Ctrl+6"); }
    private void OnCtrl7DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_7, ref ctrl7SelectedAction, "Ctrl+7"); }
    private void OnCtrl8DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_8, ref ctrl8SelectedAction, "Ctrl+8"); }
    private void OnCtrl9DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_9, ref ctrl9SelectedAction, "Ctrl+9"); }
    private void OnCtrl0DropdownChanged(int index) { HandleCtrlDropdown(index, HotKeyCatalog.Ctrl_0, ref ctrl0SelectedAction, "Ctrl+0"); }

    private void HandleCtrlDropdown(int index, HotKeyCatalog catalog, ref string selectedAction, string label)
    {
        string[] displayTexts = GetDisplayTextList();
        if (index >= 0 && index < displayTexts.Length)
        {
            string actionName = HotKeyActionManager.FromDisplayText(displayTexts[index]);
            selectedAction = actionName;
            Debug.Log($"{label} 키 액션 변경: {displayTexts[index]} ({actionName})");
            if (HotkeyManager.Instance != null)
            {
                HotkeyManager.Instance.SetBinding(catalog, selectedAction, true);
            }
        }
    }

    private void HandleSingleDropdown(int index, HotKeyCatalog catalog, ref string selectedAction, string label)
    {
        string[] displayTexts = GetDisplayTextList();
        if (index >= 0 && index < displayTexts.Length)
        {
            string actionName = HotKeyActionManager.FromDisplayText(displayTexts[index]);
            selectedAction = actionName;
            Debug.Log($"{label} 키 액션 변경: {displayTexts[index]} ({actionName})");
            if (HotkeyManager.Instance != null)
            {
                HotkeyManager.Instance.SetBinding(catalog, selectedAction, true);
            }
        }
    }

    // 현재 선택된 액션 가져오기
    public string GetBackQuoteAction() { return backQuoteSelectedAction; }
    public string GetF9Action() { return f9SelectedAction; }
    public string GetF10Action() { return f10SelectedAction; }
    public string GetF11Action() { return f11SelectedAction; }
    public string GetF12Action() { return f12SelectedAction; }
    public string GetCtrl1Action() { return ctrl1SelectedAction; }
    public string GetCtrl2Action() { return ctrl2SelectedAction; }
    public string GetCtrl3Action() { return ctrl3SelectedAction; }
    public string GetCtrl4Action() { return ctrl4SelectedAction; }
    public string GetCtrl5Action() { return ctrl5SelectedAction; }
    public string GetCtrl6Action() { return ctrl6SelectedAction; }
    public string GetCtrl7Action() { return ctrl7SelectedAction; }
    public string GetCtrl8Action() { return ctrl8SelectedAction; }
    public string GetCtrl9Action() { return ctrl9SelectedAction; }
    public string GetCtrl0Action() { return ctrl0SelectedAction; }

    // 버튼으로 호출: 기본 매핑 설정
    public void SetDropDownDefault()
    {
        if (HotkeyManager.Instance != null)
        {
            HotkeyManager.Instance.SetDefaultValues();
            RefreshAllDropdowns();
        }
    }

    // 버튼으로 호출: 전부 None으로 설정
    public void SetDropDownNone()
    {
        if (HotkeyManager.Instance != null)
        {
            HotkeyManager.Instance.SetNoneValues();
            RefreshAllDropdowns();
        }
    }

    // 모든 드롭다운 갱신 (HotKeyManager의 데이터 기준)
    public void RefreshAllDropdowns()
    {
        if (HotkeyManager.Instance == null) return;

        var bindings = HotkeyManager.Instance.GetBindings();
        foreach (var binding in bindings)
        {
            RefreshDropdown(binding.catalogKey, binding.actionName);
        }
    }

    // 개별 드롭다운 갱신
    private void RefreshDropdown(HotKeyCatalog catalog, string actionName)
    {
        TMP_Dropdown dropdown = null;
        
        switch (catalog)
        {
            case HotKeyCatalog.BackQuote: dropdown = backQuoteDropdown; backQuoteSelectedAction = actionName; break;
            case HotKeyCatalog.F9: dropdown = f9Dropdown; f9SelectedAction = actionName; break;
            case HotKeyCatalog.F10: dropdown = f10Dropdown; f10SelectedAction = actionName; break;
            case HotKeyCatalog.F11: dropdown = f11Dropdown; f11SelectedAction = actionName; break;
            case HotKeyCatalog.F12: dropdown = f12Dropdown; f12SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_1: dropdown = ctrl1Dropdown; ctrl1SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_2: dropdown = ctrl2Dropdown; ctrl2SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_3: dropdown = ctrl3Dropdown; ctrl3SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_4: dropdown = ctrl4Dropdown; ctrl4SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_5: dropdown = ctrl5Dropdown; ctrl5SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_6: dropdown = ctrl6Dropdown; ctrl6SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_7: dropdown = ctrl7Dropdown; ctrl7SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_8: dropdown = ctrl8Dropdown; ctrl8SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_9: dropdown = ctrl9Dropdown; ctrl9SelectedAction = actionName; break;
            case HotKeyCatalog.Ctrl_0: dropdown = ctrl0Dropdown; ctrl0SelectedAction = actionName; break;
        }

        if (dropdown != null)
        {
            SetDropdownValue(dropdown, actionName);
        }
    }

    private void ApplyDefault(TMP_Dropdown dropdown, HotKeyCatalog catalog, ref string selectedAction, string actionName)
    {
        selectedAction = actionName;
        if (HotkeyManager.Instance != null)
        {
            HotkeyManager.Instance.SetBinding(catalog, selectedAction, false);
        }
        SetDropdownValue(dropdown, selectedAction);
    }

    private void SetDropdownValue(TMP_Dropdown dropdown, string actionName)
    {
        if (dropdown == null)
            return;

        HotKeyActionType actionType;
        if (!Enum.TryParse(actionName, out actionType))
        {
            actionType = HotKeyActionType.ActionNone;
        }
        string display = HotKeyActionManager.ToDisplayText(actionType);
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == display)
            {
                dropdown.value = i;
                dropdown.RefreshShownValue();
                return;
            }
        }
    }
}
