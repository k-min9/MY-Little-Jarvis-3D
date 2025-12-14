using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

// 키와 액션을 바인딩하고 핫키 입력을 처리
public class HotkeyManager : MonoBehaviour
{
    [Serializable]
    public struct HotkeyBinding
    {
        public HotKeyCatalog catalogKey;
        public string actionName;
    }

    [Serializable]
    public class HotkeysData
    {
        public List<HotkeyBinding> bindings = new List<HotkeyBinding>();
    }

    private static HotkeyManager instance;
    public static HotkeyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HotkeyManager>();
            }
            return instance;
        }
    }

    [SerializeField]
    private List<HotkeyBinding> bindings = new List<HotkeyBinding>()
    {
        new HotkeyBinding { catalogKey = HotKeyCatalog.F12, actionName = "ActionNone" },
        new HotkeyBinding { catalogKey = HotKeyCatalog.F11, actionName = "ActionNone" },
        new HotkeyBinding { catalogKey = HotKeyCatalog.F10, actionName = "ActionNone" },
        new HotkeyBinding { catalogKey = HotKeyCatalog.F9, actionName = "ActionNone" },
    };

    private Dictionary<HotKeyCatalog, string> map = new Dictionary<HotKeyCatalog, string>();
    private string hotkeysFilePath;

    void Awake()
    {
        // 핫키 파일 경로 설정
        string directoryPath = Path.Combine(Application.persistentDataPath, "config");
        hotkeysFilePath = Path.Combine(directoryPath, "hotkeys.json");

        // 디렉토리가 없을 경우 생성
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        LoadHotkeys();
    }

    void Start()
    {
        // 전역 핫키가 활성화되어 있으면 GlobalInputKeyboardManager 후킹 시작
        if (IsGlobalHotkeyEnabled())
        {
            if (GlobalInputKeyboardManager.Instance != null)
            {
                GlobalInputKeyboardManager.Instance.SetInputDetection(true);
            }
        }
    }

    // 핫키 데이터를 JSON 파일에서 불러오기
    public void LoadHotkeys()
    {
        try
        {
            if (File.Exists(hotkeysFilePath))
            {
                string json = File.ReadAllText(hotkeysFilePath);
                HotkeysData data = JsonUtility.FromJson<HotkeysData>(json);
                if (data != null && data.bindings != null)
                {
                    bindings = data.bindings;
                }
            }
            else
            {
                // 파일이 없으면 None으로 초기화
                SetNoneValues();
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to load hotkeys: " + e.Message);
            SetNoneValues();
            return;
        }

        LoadBindings();
    }

    // 핫키 데이터를 JSON 파일에 저장
    public void SaveHotkeys()
    {
        try
        {
            HotkeysData data = new HotkeysData();
            data.bindings = bindings;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(hotkeysFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save hotkeys: " + e.Message);
        }
    }

    // 기본/저장된 바인딩을 맵에 로드
    private void LoadBindings()
    {
        map.Clear();

        for (int i = 0; i < bindings.Count; i++)
        {
            map[bindings[i].catalogKey] = bindings[i].actionName;
        }
    }

    // Update에서 입력 감지 (로컬 입력만 - 게임 창 포커스 필요)
    void Update()
    {
        // 전역 핫키가 활성화되어 있으면 로컬 입력 감지는 건너뜀 (중복 방지)
        if (IsGlobalHotkeyEnabled())
        {
            return;
        }

        // 로컬 핫키 처리 (게임 창 포커스 있을 때만)
        if (HotKeyCatalogManager.Instance != null)
        {
            // KeyDown 처리
            HotKeyCatalog pressedDown = HotKeyCatalogManager.Instance.GetCatalogFromInput();
            if (pressedDown != HotKeyCatalog.None)
            {
                HandleKeyDown(pressedDown);
            }

            // KeyUp 처리
            HotKeyCatalog pressedUp = HotKeyCatalogManager.Instance.GetCatalogFromInputUp();
            if (pressedUp != HotKeyCatalog.None)
            {
                HandleKeyUp(pressedUp);
            }
        }
    }

    // 핫키 입력 처리 (KeyDown)
    public void HandleKeyDown(HotKeyCatalog catalog)
    {
        string actionName;
        if (map.TryGetValue(catalog, out actionName))
        {
            InvokeAction(actionName);
        }
    }

    // 핫키 입력 처리 (KeyUp)
    public void HandleKeyUp(HotKeyCatalog catalog)
    {
        string actionName;
        if (map.TryGetValue(catalog, out actionName))
        {
            InvokeActionUp(actionName);
        }
    }

    // 액션 실행 (KeyDown)
    private void InvokeAction(string actionName)
    {
        if (HotKeyActionManager.Instance != null)
        {
            HotKeyActionManager.Instance.Execute(actionName);
        }
        else
        {
            Debug.LogWarning("HotkeyManager: HotKeyAction 인스턴스를 찾을 수 없습니다.");
        }
    }

    // 액션 실행 (KeyUp)
    private void InvokeActionUp(string actionName)
    {
        if (HotKeyActionManager.Instance != null)
        {
            HotKeyActionManager.Instance.ExecuteOnKeyUp(actionName);
        }
    }

    // 바인딩 설정 및(선택) 저장
    public void SetBinding(HotKeyCatalog catalog, string actionName, bool persist = true)
    {
        map[catalog] = actionName;
        UpdateBindingList(catalog, actionName);
        if (persist)
        {
            SaveHotkeys();
        }
    }

    // 내부 목록의 바인딩 동기화
    private void UpdateBindingList(HotKeyCatalog catalog, string actionName)
    {
        bool found = false;
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].catalogKey == catalog)
            {
                bindings[i] = new HotkeyBinding { catalogKey = catalog, actionName = actionName };
                found = true;
                break;
            }
        }
        if (!found)
        {
            bindings.Add(new HotkeyBinding { catalogKey = catalog, actionName = actionName });
        }
    }

    // 드롭다운용 액션 옵션 배열 반환 (표시 텍스트)
    public string[] GetActionOptions()
    {
        if (HotKeyActionManager.Instance != null)
            return HotKeyActionManager.Instance.GetDisplayTexts();
        return new string[] { "None" };
    }

    // 드롭다운용 키 옵션 배열 반환
    public string[] GetKeyOptions()
    {
        if (HotKeyCatalogManager.Instance == null)
            return new string[] { "None" };

        var names = new List<string>();
        foreach (HotKeyCatalog catalog in Enum.GetValues(typeof(HotKeyCatalog)))
        {
            names.Add(HotKeyCatalogManager.Instance.ToDisplayString(catalog));
        }
        return names.ToArray();
    }

    // 특정 키에 바인딩된 액션 이름 반환
    public string GetBindingAction(HotKeyCatalog catalog)
    {
        string actionName;
        if (map.TryGetValue(catalog, out actionName))
            return actionName;
        return "ActionNone";
    }

    // 현재 바인딩 목록 반환 (읽기 전용)
    public List<HotkeyBinding> GetBindings()
    {
        return new List<HotkeyBinding>(bindings);
    }

    // 전역 핫키 사용 가능 여부 반환 (플랫폼 가드 포함)
    public bool IsGlobalHotkeyEnabled()
    {
#if UNITY_STANDALONE_WIN
        return SettingManager.Instance.settings.hotKeyGlobalInputEnabled;
#else
        return false;
#endif
    }

    // 가상 키 코드(VK Code)를 받아서 핫키 처리 (전역 입력용)
    public void HandleVirtualKeyCode(int vkCode, bool isKeyDown)
    {
        if (HotKeyCatalogManager.Instance == null)
        {
            return;
        }

        // VK Code를 HotKeyCatalog으로 변환
        HotKeyCatalog catalog = HotKeyCatalogManager.Instance.VKCodeToCatalog(vkCode);
        
        if (catalog != HotKeyCatalog.None)
        {
            if (isKeyDown)
            HandleKeyDown(catalog);
            else
                HandleKeyUp(catalog);
        }
    }

    // 전역 핫키 활성화/비활성화 (런타임에서 토글 가능)
    public void SetGlobalHotkeyEnabled(bool enabled)
    {
        // GlobalInputKeyboardManager에 활성화/비활성화 전달
        if (GlobalInputKeyboardManager.Instance != null)
        {
            GlobalInputKeyboardManager.Instance.SetInputDetection(enabled);
        }
    }

    // 기본 핫키 매핑 설정 (추천 설정)
    public void SetDefaultValues()
    {
        bindings.Clear();
        map.Clear();

        // 기본 매핑
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.BackQuote, actionName = HotKeyActionType.ActionChatStart.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.F9, actionName = HotKeyActionType.ActionChatChar.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.F10, actionName = HotKeyActionType.ActionCharAction.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.F11, actionName = HotKeyActionType.ActionDance.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.F12, actionName = HotKeyActionType.ActionChangeClothes.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_1, actionName = HotKeyActionType.ActionChangeChar.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_2, actionName = HotKeyActionType.ActionShowChatHistory.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_3, actionName = HotKeyActionType.ActionStartTalk.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_4, actionName = HotKeyActionType.ActionStartTikitaka.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_5, actionName = HotKeyActionType.ActionSmallTalk.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_6, actionName = HotKeyActionType.ActionSetArea.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_7, actionName = HotKeyActionType.ActionExecuteAreaOCR.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_8, actionName = HotKeyActionType.ActionNone.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_9, actionName = HotKeyActionType.ActionNone.ToString() });
        bindings.Add(new HotkeyBinding { catalogKey = HotKeyCatalog.Ctrl_0, actionName = HotKeyActionType.ActionNone.ToString() });

        LoadBindings();
        SaveHotkeys();
        
        // 드롭다운 UI 갱신
        if (HotKeyCatalogManager.Instance != null)
        {
            HotKeyCatalogManager.Instance.RefreshAllDropdowns();
        }
    }

    // 모든 핫키를 None으로 설정
    public void SetNoneValues()
    {
        bindings.Clear();
        map.Clear();

        // 모든 키를 None으로 매핑
        foreach (HotKeyCatalog catalog in Enum.GetValues(typeof(HotKeyCatalog)))
        {
            if (catalog != HotKeyCatalog.None)
            {
                bindings.Add(new HotkeyBinding { catalogKey = catalog, actionName = HotKeyActionType.ActionNone.ToString() });
            }
        }

        LoadBindings();
        SaveHotkeys();
        
        // 드롭다운 UI 갱신
        if (HotKeyCatalogManager.Instance != null)
        {
            HotKeyCatalogManager.Instance.RefreshAllDropdowns();
        }
    }
}
