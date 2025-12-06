using UnityEngine;
using System;
using System.Collections.Generic;

// 키와 액션을 바인딩하고 핫키 입력을 처리
public class HotkeyManager : MonoBehaviour
{
    [Serializable]
    public struct HotkeyBinding
    {
        public HotKeyCatalog catalogKey;
        public string actionName;
    }

    private static HotkeyManager instance;
    public static HotkeyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HotkeyManager>();
                if (instance == null)
                {
                    var go = new GameObject("HotkeyManager");
                    instance = go.AddComponent<HotkeyManager>();
                    DontDestroyOnLoad(go);
                }
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

    // 초기화: 싱글톤 설정 및 바인딩 로드
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBindings();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
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

        // 저장된 바인딩 적용
        foreach (HotKeyCatalog catalog in Enum.GetValues(typeof(HotKeyCatalog)))
        {
            if (catalog != HotKeyCatalog.None)
                ApplyPersisted(catalog);
        }
    }

    // PlayerPrefs에 저장된 바인딩을 적용
    private void ApplyPersisted(HotKeyCatalog catalog)
    {
        string prefKey = PrefKeyFor(catalog);
        if (PlayerPrefs.HasKey(prefKey))
        {
            string savedAction = PlayerPrefs.GetString(prefKey);
            if (!string.IsNullOrEmpty(savedAction))
            {
                map[catalog] = savedAction;
            }
        }
    }

    // PlayerPrefs 키 이름 생성
    private string PrefKeyFor(HotKeyCatalog catalog)
    {
        return "Hotkey_" + catalog.ToString();
    }

    // Update에서 입력 감지
    void Update()
    {
        if (HotKeyCatalogManager.Instance != null)
        {
            HotKeyCatalog pressed = HotKeyCatalogManager.Instance.GetCatalogFromInput();
            if (pressed != HotKeyCatalog.None)
            {
                HandleKeyDown(pressed);
            }
        }
    }

    // 핫키 입력 처리
    public void HandleKeyDown(HotKeyCatalog catalog)
    {
        string actionName;
        if (map.TryGetValue(catalog, out actionName))
        {
            InvokeAction(actionName);
        }
    }

    // 액션 실행
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

    // 바인딩 설정 및(선택) 저장
    public void SetBinding(HotKeyCatalog catalog, string actionName, bool persist = true)
    {
        map[catalog] = actionName;
        UpdateBindingList(catalog, actionName);
        if (persist)
        {
            PlayerPrefs.SetString(PrefKeyFor(catalog), actionName);
            PlayerPrefs.Save();
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
}
