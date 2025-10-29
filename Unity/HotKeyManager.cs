using UnityEngine;
using System;
using System.Collections.Generic;

// 나중에 입력키와 액션을 바인딩할 수 있게 설계
public class HotkeyManager : MonoBehaviour
{
    public enum HotkeyAction
    {
        None = 0,
        ToggleDevTab = 1,
        ToggleInteractableDev = 2,
    }

    [Serializable]
    public struct HotkeyBinding
    {
        public KeyCode key;
        public HotkeyAction action;
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
        new HotkeyBinding { key = KeyCode.F12, action = HotkeyAction.ToggleDevTab },
        new HotkeyBinding { key = KeyCode.F11, action = HotkeyAction.ToggleInteractableDev },
        new HotkeyBinding { key = KeyCode.F10, action = HotkeyAction.None },
    };

    private Dictionary<KeyCode, HotkeyAction> map = new Dictionary<KeyCode, HotkeyAction>();

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
            map[bindings[i].key] = bindings[i].action;
        }

        ApplyPersisted(KeyCode.F10);
        ApplyPersisted(KeyCode.F11);
        ApplyPersisted(KeyCode.F12);
    }

    // PlayerPrefs에 저장된 바인딩을 적용
    private void ApplyPersisted(KeyCode key)
    {
        string prefKey = PrefKeyFor(key);
        if (PlayerPrefs.HasKey(prefKey))
        {
            string saved = PlayerPrefs.GetString(prefKey);
            HotkeyAction parsed;
            if (Enum.TryParse(saved, out parsed))
            {
                map[key] = parsed;
            }
        }
    }

    // PlayerPrefs 키 이름 생성
    private string PrefKeyFor(KeyCode key)
    {
        return "Hotkey_" + key.ToString();
    }

    // 호출하여 액션 실행 (핫키 처리)
    public void HandleKeyDown(KeyCode key)
    {
        HotkeyAction action;
        if (map.TryGetValue(key, out action))
        {
            InvokeAction(action);
        }
    }

    // 호출하여 바인딩 설정 및(선택) 저장
    public void SetBinding(KeyCode key, HotkeyAction action, bool persist = true)
    {
        map[key] = action;
        UpdateBindingList(key, action);
        if (persist)
        {
            PlayerPrefs.SetString(PrefKeyFor(key), action.ToString());
            PlayerPrefs.Save();
        }
    }

    // 내부 목록의 바인딩 동기화
    private void UpdateBindingList(KeyCode key, HotkeyAction action)
    {
        bool found = false;
        for (int i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].key == key)
            {
                bindings[i] = new HotkeyBinding { key = key, action = action };
                found = true;
                break;
            }
        }
        if (!found)
        {
            bindings.Add(new HotkeyBinding { key = key, action = action });
        }
    }

    // 드롭다운용 액션 옵션 배열 반환
    public string[] GetActionOptions()
    {
        return Enum.GetNames(typeof(HotkeyAction));
    }

    // 문자열로 바인딩 설정 (드롭다운 선택 반영)
    public void SetBindingFromString(KeyCode key, string actionName, bool persist = true)
    {
        HotkeyAction parsed;
        if (Enum.TryParse(actionName, out parsed))
        {
            SetBinding(key, parsed, persist);
        }
    }

    // 예시 - 액션을 실제로 실행
    private void InvokeAction(HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.ToggleDevTab:
                if (DevManager.Instance != null)
                {
                    DevManager.Instance.ToggleShowSettingDevTab();
                }
                break;
            case HotkeyAction.ToggleInteractableDev:
                if (DevManager.Instance != null)
                {
                    DevManager.Instance.ToggleShowSettingDevTab();
                }
                break;
            case HotkeyAction.None:
            default:
                break;
        }
    }
}